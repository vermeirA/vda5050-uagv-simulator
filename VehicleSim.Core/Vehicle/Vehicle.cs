using Microsoft.Extensions.Logging;
using VehicleSim.Core.Events;
using VehicleSim.Core.VdaModels;
using VehicleSim.Core.Vehicle.Helpers;

namespace VehicleSim.Core.Vehicle;

public partial class Vehicle(
    string serialNumber,
    string manufacturer,
    string mapId,
    double startX,
    double startY,
    ILogger<Vehicle> logger) : IVehicle
{
    // Heartbeat interval for state publishing, even without significant changes
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(10);

    // Minimum distance traveled before emitting a position update
    private const double MinPositionUpdateDistance = 0.1;

    private readonly string serialNumber = serialNumber;
    private readonly string manufacturer = manufacturer;
    private readonly string mapId = mapId;
    private readonly ILogger<Vehicle> logger = logger;

    private double currentX = startX;
    private double currentY = startY;
    private double currentTheta;

    private VdaNode? targetNode;
    private VdaOperatingMode operatingMode = VdaOperatingMode.AUTOMATIC;
    private VehicleStatus status = VehicleStatus.READY;
    private bool connected = false;

    private string? currentOrderId;
    private string? lastNodeId = "initial_pos";
    private uint lastNodeSequenceId;

    private readonly Queue<RouteStep> routeQueue = [];
    private readonly List<VdaError> errors = [];
    private RouteStep? currentStep;

    private uint headerId;
    private uint orderUpdateId;
    private VdaState? lastEmittedState;
    private VdaPosition? lastEmittedPosition;
    private DateTime lastStatePublish = DateTime.MinValue;
    private double distanceSinceLastPositionUpdate;

    private readonly InitialState defaultSettings = new()
    {
        X = startX,
        Y = startY,
        Theta = 0,
        MapId = mapId
    };

    public string SerialNumber => serialNumber;
    public VdaOperatingMode OperatingMode => operatingMode;
    public VehicleStatus Status => status;
    public (double x, double y) Position => (currentX, currentY);

    public event EventHandler<VehicleStateChangedEvent>? StateChanged;
    public event EventHandler<VehiclePositionChangedEvent>? PositionChanged;
    public event EventHandler<VehicleOrderReceivedEvent>? OrderReceived;
    public event EventHandler<RouteCompletedEvent>? RouteCompleted;

    public VdaConnection BuildConnectionMessage()
    {
        connected = true;
        status = VehicleStatus.READY;
        RaiseStateChanged();
        return new()
        {

            HeaderId = 0,
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            Manufacturer = manufacturer,
            SerialNumber = serialNumber,
            Version = "2.0",
            ConnectionState = "ONLINE",
        };
    }



    public VdaConnection BuildDisconnectionMessage()
    {
        connected = false;
        status = VehicleStatus.DISCONNECTED;
        RaiseStateChanged();
        return new()
        {

            HeaderId = 0,
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            Manufacturer = manufacturer,
            SerialNumber = serialNumber,
            Version = "2.0",
            ConnectionState = "OFFLINE",
        };
    }
       

    public void ProcessOrder(VdaOrder order)
    {
        if (!connected)
        {
            logger.LogWarning("{SerialNumber}: Cannot process order {OrderId} while disconnected.", serialNumber, order.OrderId);
            return;
        }
        
        if (errors.Any(e => e.ErrorLevel == VdaErrorLevel.FATAL))
        {
            logger.LogWarning("{SerialNumber}: Rejecting order {OrderId} due to FATAL state.", serialNumber, order.OrderId);
            return;
        }

        bool isUpdate = order.OrderId == currentOrderId && order.OrderUpdateId > orderUpdateId;
        if (!IsOrderValid(order, isUpdate)) return;

        uint currentProgressSeq = currentStep?.TargetNode.SequenceId ?? lastNodeSequenceId;

        routeQueue.Clear();

        headerId = order.HeaderId;
        currentOrderId = order.OrderId;
        orderUpdateId = order.OrderUpdateId;

        var incomingEdges = order.Edges.ToDictionary(e => e.EndNodeId);

        var newNodes = order.Nodes.OrderBy(n => n.SequenceId);
        foreach (var node in newNodes)
        {
            if (node.SequenceId > currentProgressSeq)
            {
                incomingEdges.TryGetValue(node.NodeId, out var edge);
                routeQueue.Enqueue(new RouteStep
                {
                    TargetNode = node,
                    IncomingEdge = edge,
                    IsReleased = node.Released && (edge?.Released ?? true)
                });
            }
            else if (currentStep != null && node.SequenceId == currentProgressSeq)
            {
                currentStep.TargetNode = node;
                targetNode = node;
            }
        }

        logger.LogInformation("{SerialNumber}: Order {Id} updated. New Queue Length: {Count}",serialNumber ,currentOrderId, routeQueue.Count);
        OrderReceived?.Invoke(this, new VehicleOrderReceivedEvent(serialNumber, currentOrderId!));
        RaiseStateChanged();
    }

    public void Tick(double deltaTime)
    {
        if (operatingMode is not (VdaOperatingMode.AUTOMATIC or VdaOperatingMode.SEMIAUTOMATIC) || !connected)
            return;

        bool significantChange = false;

        if (currentStep == null && routeQueue.TryPeek(out var nextStep))
        {
            if (nextStep.IsReleased)
            {
                currentStep = routeQueue.Dequeue();
                targetNode = currentStep.TargetNode;
                status = VehicleStatus.EXECUTING;
                logger.LogInformation("{SerialNumber}: Moving to node {NodeId}", serialNumber, targetNode.NodeId);
                significantChange = true;
            }
            else status = VehicleStatus.READY;
        }
        else if (currentStep == null && status == VehicleStatus.EXECUTING)
        {
            logger.LogInformation("{SerialNumber}: Order {OrderId} complete.", serialNumber, currentOrderId);
            RouteCompleted?.Invoke(this, new RouteCompletedEvent(serialNumber, BuildCurrentState()));
            status = VehicleStatus.READY;
            significantChange = true;
        }

        if (currentStep != null)
        {
            bool reachedNode = MoveTowardsTarget(deltaTime);
            significantChange = significantChange || reachedNode;
        }

        // Raise position change for /visualization (with distance debounce)
        RaisePositionIfChanged(forceEmit: significantChange);

        // Only if significant change or heartbeat we publish to /state topic
        if (significantChange || IsHeartbeatDue())
        {
            RaiseStateChanged();
        }
    }

    private bool MoveTowardsTarget(double deltaTime)
    {
        if (targetNode == null) return false;

        double dx = targetNode.Position.X - currentX;
        double dy = targetNode.Position.Y - currentY;

        double distance = targetNode.Position.GetDistanceTo(currentX, currentY);
        double moveStep = 1.0 * deltaTime;

        if (distance <= moveStep)
        {
            currentX = targetNode.Position.X;
            currentY = targetNode.Position.Y;
            lastNodeId = targetNode.NodeId;
            lastNodeSequenceId = targetNode.SequenceId;
            distanceSinceLastPositionUpdate += distance;
            logger.LogInformation("{SerialNumber}: Reached {NodeId}", serialNumber, lastNodeId);
            currentStep = null;
            return true;
        }
        else
        {
            double ratio = moveStep / distance;
            currentX += dx * ratio;
            currentY += dy * ratio;
            currentTheta = Math.Atan2(dy, dx);
            distanceSinceLastPositionUpdate += moveStep;
            return false;
        }
    }

    private bool IsOrderValid(VdaOrder order, bool isUpdate)
    {
        if (order.SerialNumber != serialNumber) return FailOrder("Serial mismatch");

        var firstNode = order.Nodes.MinBy(n => n.SequenceId);
        if (firstNode == null) return false;

        if (!isUpdate && !firstNode.Position.IsAtPosition(currentX, currentY))
            return FailOrder("Anchor deviation too high");

        if (order.Nodes.Any(n => n.Position.MapId != mapId))
            return FailOrder("Map ID mismatch");

        return true;
    }

    private bool FailOrder(string reason)
    {
        ReportError("orderError", reason, VdaErrorLevel.WARNING);
        return false;
    }

    public void ReportError(string type, string description, VdaErrorLevel level)
    {
        if (!connected)
        {
            logger.LogWarning("{SerialNumber}: Cannot report error while disconnected.", serialNumber);
            return;
        }
        if (level == VdaErrorLevel.FATAL) logger.LogError("{SerialNumber}: FATAL: {Desc}", serialNumber, description);
        else logger.LogWarning("{SerialNumber}: WARN: {Desc}", serialNumber, description);

        if (!errors.Any(e => e.ErrorType == type && e.ErrorDescription == description))
        {
            errors.Add(new VdaError { ErrorType = type, ErrorDescription = description, ErrorLevel = level });
        }

        if (errors.Any(e => e.ErrorLevel == VdaErrorLevel.FATAL))
        {
            status = VehicleStatus.FATAL;
            operatingMode = VdaOperatingMode.MANUAL;
        }
        else if (errors.Any(e => e.ErrorLevel == VdaErrorLevel.WARNING))
        {
            status = VehicleStatus.WARNING;
        }

        RaiseStateChanged();
    }

    public void SoftReset()
    {
        logger.LogInformation("{SerialNumber}: Resetting vehicle...", serialNumber);

        status = VehicleStatus.READY;
        operatingMode = VdaOperatingMode.AUTOMATIC;
        errors.Clear();

        logger.LogInformation("{SerialNumber}: Vehicle reset successful.", serialNumber);
        RaiseStateChanged();
    }

    public void HardReset()
    {
        logger.LogInformation("{SerialNumber}: Hard Reset: Clearing all data and returning to origin.", serialNumber);

        currentX = defaultSettings.X;
        currentY = defaultSettings.Y;
        currentTheta = defaultSettings.Theta;

        routeQueue.Clear();
        currentStep = null;
        targetNode = null;

        currentOrderId = string.Empty;
        orderUpdateId = 0;
        headerId = 0;
        lastNodeId = "initial_node";
        lastNodeSequenceId = 0;
        distanceSinceLastPositionUpdate = 0;

        errors.Clear();
        status = VehicleStatus.READY;
        operatingMode = VdaOperatingMode.AUTOMATIC;

        RaiseStateChanged();
        RaisePositionIfChanged(forceEmit: true);
        logger.LogInformation("{SerialNumber}: Reset Complete. Vehicle at ({X}, {Y}), Status: {Status}.", serialNumber, defaultSettings.X, defaultSettings.Y, status);
    }

    public void UpdateNodeRelease(string nodeId, bool released)
    {
        foreach (var step in routeQueue)
        {
            if (step.TargetNode.NodeId == nodeId)
            {
                bool wasReleased = step.IsReleased;
                step.TargetNode.Released = released;
                step.IsReleased = released && (step.IncomingEdge?.Released ?? true);

                if (wasReleased != step.IsReleased)
                {
                    logger.LogInformation("{SerialNumber}: Node {NodeId} release updated to {IsReleased}.", serialNumber, nodeId, step.IsReleased);
                }
            }
        }
    }

    public void UpdateEdgeRelease(string edgeId, bool released)
    {
        foreach (var step in routeQueue)
        {
            if (step.IncomingEdge?.EdgeId == edgeId)
            {
                bool wasReleased = step.IsReleased;
                step.IncomingEdge.Released = released;
                step.IsReleased = step.TargetNode.Released && released;

                if (wasReleased != step.IsReleased)
                {
                    logger.LogInformation("{SerialNumber}: Edge {EdgeId} release updated to {IsReleased}.", serialNumber, edgeId, step.IsReleased);
                }
            }
        }
    }

    private bool IsHeartbeatDue() => (DateTime.UtcNow - lastStatePublish) >= HeartbeatInterval;

    private void RaiseStateChanged()
    {
        var newState = BuildCurrentState();
        headerId++;
        newState.HeaderId = headerId;
        lastEmittedState = newState;
        lastStatePublish = DateTime.UtcNow;
        StateChanged?.Invoke(this, new VehicleStateChangedEvent(serialNumber, newState));
    }

    private void RaisePositionIfChanged(bool forceEmit = false)
    {
        if (lastEmittedPosition != null &&
            lastEmittedPosition.X == currentX &&
            lastEmittedPosition.Y == currentY &&
            lastEmittedPosition.Theta == currentTheta)
            return;

        // Only emit if we've traveled enough distance or force is requested
        if (!forceEmit && distanceSinceLastPositionUpdate < MinPositionUpdateDistance)
            return;

        var position = new VdaPosition
        {
            PositionInitialized = true,
            X = currentX,
            Y = currentY,
            Theta = currentTheta,
            MapId = mapId
        };

        lastEmittedPosition = position;
        distanceSinceLastPositionUpdate = 0;
        PositionChanged?.Invoke(this, new VehiclePositionChangedEvent(serialNumber, position));
    }

    public VdaState BuildCurrentState()
    {
        var nodeStates = new List<VdaNodeState>();
        var edgeStates = new List<VdaEdgeState>();

        if (currentStep != null)
        {
            nodeStates.Add(new VdaNodeState
            {
                NodeId = currentStep.TargetNode.NodeId,
                SequenceId = currentStep.TargetNode.SequenceId,
                Released = currentStep.TargetNode.Released,
                NodePosition = currentStep.TargetNode.Position
            });

            if (currentStep.IncomingEdge != null)
            {
                edgeStates.Add(new VdaEdgeState
                {
                    EdgeId = currentStep.IncomingEdge.EdgeId,
                    SequenceId = currentStep.IncomingEdge.SequenceId,
                    Released = currentStep.IncomingEdge.Released
                });
            }
        }

        foreach (var step in routeQueue)
        {
            nodeStates.Add(new VdaNodeState
            {
                NodeId = step.TargetNode.NodeId,
                SequenceId = step.TargetNode.SequenceId,
                Released = step.TargetNode.Released,
                NodePosition = step.TargetNode.Position
            });

            if (step.IncomingEdge != null)
            {
                edgeStates.Add(new VdaEdgeState
                {
                    EdgeId = step.IncomingEdge.EdgeId,
                    SequenceId = step.IncomingEdge.SequenceId,
                    Released = step.IncomingEdge.Released
                });
            }
        }

        return new VdaState
        {
            HeaderId = headerId,
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            Manufacturer = manufacturer,
            SerialNumber = serialNumber,
            Version = "2.0",
            OrderId = currentOrderId ?? "none",
            OrderUpdateId = orderUpdateId,
            LastNodeId = lastNodeId ?? "none",
            LastNodeSequenceId = lastNodeSequenceId,
            OperatingMode = operatingMode,
            Driving = status == VehicleStatus.EXECUTING,
            AgvPosition = new VdaPosition
            {
                PositionInitialized = true,
                X = currentX,
                Y = currentY,
                Theta = currentTheta,
                MapId = mapId,
            },
            NodeStates = nodeStates,
            EdgeStates = edgeStates,
            Errors = [.. errors]
        };
    }
}