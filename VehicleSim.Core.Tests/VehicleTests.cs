using Microsoft.Extensions.Logging;
using Moq;
using VehicleSim.Core.VdaModels;

namespace VehicleSim.Core.Tests;

public class VehicleTests
{
    private const string SerialNumber = "AGV001";
    private const string Manufacturer = "TestCo";
    private const string MapId = "map1";
    private const double StartX = 0.0;
    private const double StartY = 0.0;

    private readonly Mock<ILogger<Vehicle.Vehicle>> _loggerMock = new();

    private Vehicle.Vehicle CreateVehicle()
    {
        return new(SerialNumber, Manufacturer, MapId, StartX, StartY, _loggerMock.Object);
    }

    private static VdaOrder CreateValidOrder(string orderId = "order1", uint updateId = 1, params (string nodeId, uint seq, double x, double y, bool released)[] nodes)
    {
        var order = new VdaOrder
        {
            HeaderId = 1,
            SerialNumber = SerialNumber,
            OrderId = orderId,
            OrderUpdateId = updateId,
            Nodes = [],
            Edges = [],
            Timestamp = DateTime.UtcNow.ToString()
        };

        for (int i = 0; i < nodes.Length; i++)
        {
            var (nodeId, seq, x, y, released) = nodes[i];
            order.Nodes.Add(new VdaNode
            {
                NodeId = nodeId,
                SequenceId = seq,
                Released = released,
                Position = new VdaPosition { X = x, Y = y, MapId = MapId }
            });

            if (i > 0)
            {
                order.Edges.Add(new VdaEdge
                {
                    EdgeId = $"edge{i}",
                    SequenceId = seq - 1,
                    StartNodeId = nodes[i - 1].nodeId,
                    EndNodeId = nodeId,
                    Released = released
                });
            }
        }

        return order;
    }

    #region BuildConnectionMessage Tests

    [Fact]
    public void BuildConnectionMessage_ReturnsCorrectValues()
    {
        var vehicle = CreateVehicle();

        var connection = vehicle.BuildConnectionMessage();

        Assert.Equal(Manufacturer, connection.Manufacturer);
        Assert.Equal(SerialNumber, connection.SerialNumber);
        Assert.Equal("ONLINE", connection.ConnectionState);
        Assert.Equal("2.0", connection.Version);
    }

    #endregion

    #region BuildCurrentState Tests

    [Fact]
    public void BuildCurrentState_InitialState_ReturnsCorrectDefaults()
    {
        var vehicle = CreateVehicle();

        var state = vehicle.BuildCurrentState();

        Assert.Equal(SerialNumber, state.SerialNumber);
        Assert.Equal(Manufacturer, state.Manufacturer);
        Assert.Equal("none", state.OrderId);
        Assert.Equal(VdaOperatingMode.AUTOMATIC, state.OperatingMode);
        Assert.False(state.Driving);
        Assert.Equal(StartX, state.AgvPosition.X);
        Assert.Equal(StartY, state.AgvPosition.Y);
        Assert.Empty(state.NodeStates);
        Assert.Empty(state.EdgeStates);
        Assert.Empty(state.Errors);
    }

    [Fact]
    public void BuildCurrentState_WithOrder_IncludesNodeAndEdgeStates()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1,
            ("A", 0, 0, 0, true),
            ("B", 2, 5, 0, true),
            ("C", 4, 10, 0, true));

        vehicle.ProcessOrder(order);

        var state = vehicle.BuildCurrentState();

        Assert.Equal(2, state.NodeStates.Count);
        Assert.Equal(2, state.EdgeStates.Count);
        Assert.Contains(state.NodeStates, n => n.NodeId == "B");
        Assert.Contains(state.NodeStates, n => n.NodeId == "C");
    }

    #endregion

    #region ProcessOrder Tests

    [Fact]
    public void ProcessOrder_ValidOrder_RaisesOrderReceivedEvent()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 5, 0, true));
        string? receivedOrderId = null;
        vehicle.OrderReceived += (_, e) => receivedOrderId = e.OrderId;

        vehicle.ProcessOrder(order);

        Assert.Equal("order1", receivedOrderId);
    }

    [Fact]
    public void ProcessOrder_SerialMismatch_RejectsOrder()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true));
        order.SerialNumber = "WRONG";
        bool orderReceived = false;
        vehicle.OrderReceived += (_, _) => orderReceived = true;

        vehicle.ProcessOrder(order);

        Assert.False(orderReceived);
        var state = vehicle.BuildCurrentState();
        Assert.Contains(state.Errors, e => e.ErrorDescription == "Serial mismatch");
    }

    [Fact]
    public void ProcessOrder_MapIdMismatch_RejectsOrder()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true));
        order.Nodes[0].Position.MapId = "wrong_map";

        vehicle.ProcessOrder(order);

        var state = vehicle.BuildCurrentState();
        Assert.Contains(state.Errors, e => e.ErrorDescription == "Map ID mismatch");
    }

    [Fact]
    public void ProcessOrder_AnchorDeviationTooHigh_RejectsOrder()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 100, 100, true)); // Far from origin

        vehicle.ProcessOrder(order);

        var state = vehicle.BuildCurrentState();
        Assert.Contains(state.Errors, e => e.ErrorDescription == "Anchor deviation too high");
    }

    [Fact]
    public void ProcessOrder_InFatalState_RejectsOrder()
    {
        var vehicle = CreateVehicle();
        vehicle.ReportError("test", "Fatal error", VdaErrorLevel.FATAL);
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true));
        bool orderReceived = false;
        vehicle.OrderReceived += (_, _) => orderReceived = true;

        vehicle.ProcessOrder(order);

        Assert.False(orderReceived);
    }

    [Fact]
    public void ProcessOrder_OrderUpdate_AcceptsHigherUpdateId()
    {
        var vehicle = CreateVehicle();
        var order1 = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 5, 0, true));
        var order2 = CreateValidOrder("order1", 2, ("A", 0, 0, 0, true), ("B", 2, 5, 0, true), ("C", 4, 10, 0, true));
        vehicle.ProcessOrder(order1);

        vehicle.ProcessOrder(order2);

        var state = vehicle.BuildCurrentState();
        Assert.Equal(2u, state.OrderUpdateId);
    }

    #endregion

    #region Tick Tests

    [Fact]
    public void Tick_WithReleasedNode_StartsMoving()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 5, 0, true));
        vehicle.ProcessOrder(order);

        vehicle.Tick(0.1);

        var state = vehicle.BuildCurrentState();
        Assert.True(state.Driving);
    }

    [Fact]
    public void Tick_WithUnreleasedNode_DoesNotMove()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 5, 0, false));
        vehicle.ProcessOrder(order);

        vehicle.Tick(0.1);

        var state = vehicle.BuildCurrentState();
        Assert.False(state.Driving);
        Assert.Equal(0, state.AgvPosition.X);
    }

    [Fact]
    public void Tick_ReachesNode_UpdatesLastNodeId()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 1, 0, true)); // 1 unit away
        vehicle.ProcessOrder(order);

        // Tick enough to reach the node (distance=1, speed=1*timeScale)
        vehicle.Tick(2.0);

        var state = vehicle.BuildCurrentState();
        Assert.Equal("B", state.LastNodeId);
        Assert.Equal(2u, state.LastNodeSequenceId);
    }

    [Fact]
    public void Tick_CompletesOrder_RaisesRouteCompletedEvent()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 1, 0, true));
        vehicle.ProcessOrder(order);
        bool routeCompleted = false;
        vehicle.RouteCompleted += (_, _) => routeCompleted = true;

        vehicle.Tick(2.0); // Reach node B
        vehicle.Tick(0.1); // Complete order

        Assert.True(routeCompleted);
    }

    [Fact]
    public void Tick_InManualMode_DoesNotMove()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 5, 0, true));
        vehicle.ProcessOrder(order);
        vehicle.ReportError("test", "error", VdaErrorLevel.FATAL); // Switches to MANUAL

        vehicle.Tick(1.0);

        var state = vehicle.BuildCurrentState();
        Assert.Equal(0, state.AgvPosition.X);
    }

    [Fact]
    public void Tick_MovesTowardsTarget_UpdatesPosition()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 10, 0, true));
        vehicle.ProcessOrder(order);

        vehicle.Tick(1.0);

        var state = vehicle.BuildCurrentState();
        Assert.True(state.AgvPosition.X > 0);
        Assert.True(state.AgvPosition.X < 10);
    }

    #endregion

    #region ReportError Tests

    [Fact]
    public void ReportError_FatalError_SetsNotReadyAndManual()
    {
        var vehicle = CreateVehicle();

        vehicle.ReportError("test", "Fatal error", VdaErrorLevel.FATAL);

        var state = vehicle.BuildCurrentState();
        Assert.Equal(VdaOperatingMode.MANUAL, state.OperatingMode);
        Assert.Contains(state.Errors, e => e.ErrorLevel == VdaErrorLevel.FATAL);
    }

    [Fact]
    public void ReportError_WarningError_DoesNotChangeMode()
    {
        var vehicle = CreateVehicle();

        vehicle.ReportError("test", "Warning", VdaErrorLevel.WARNING);

        var state = vehicle.BuildCurrentState();
        Assert.Equal(VdaOperatingMode.AUTOMATIC, state.OperatingMode);
    }

    [Fact]
    public void ReportError_DuplicateError_DoesNotAddTwice()
    {
        var vehicle = CreateVehicle();

        vehicle.ReportError("test", "Same error", VdaErrorLevel.WARNING);
        vehicle.ReportError("test", "Same error", VdaErrorLevel.WARNING);

        var state = vehicle.BuildCurrentState();
        Assert.Single(state.Errors);
    }

    #endregion

    #region SoftReset Tests

    [Fact]
    public void SoftReset_ClearsErrorsAndRestoresMode()
    {
        var vehicle = CreateVehicle();
        vehicle.ReportError("test", "Fatal", VdaErrorLevel.WARNING);

        vehicle.SoftReset();

        var state = vehicle.BuildCurrentState();
        Assert.Equal(VdaOperatingMode.AUTOMATIC, state.OperatingMode);
        Assert.Empty(state.Errors);
    }

    #endregion

    #region HardReset Tests

    [Fact]
    public void HardReset_RestoresInitialPosition()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 5, 0, true));
        vehicle.ProcessOrder(order);
        vehicle.Tick(1.0); // Move vehicle

        vehicle.HardReset();

        var state = vehicle.BuildCurrentState();
        Assert.Equal(StartX, state.AgvPosition.X);
        Assert.Equal(StartY, state.AgvPosition.Y);
    }

    [Fact]
    public void HardReset_ClearsOrderAndQueue()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 5, 0, true));
        vehicle.ProcessOrder(order);

        vehicle.HardReset();

        var state = vehicle.BuildCurrentState();
        Assert.Empty(state.NodeStates);
        Assert.Empty(state.EdgeStates);
        Assert.Equal(0u, state.OrderUpdateId);
    }

    #endregion

    #region UpdateNodeRelease Tests

    [Fact]
    public void UpdateNodeRelease_ReleasesNode_AllowsMovement()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 5, 0, false));
        order.Edges[0].Released = true;
        vehicle.ProcessOrder(order);
        vehicle.Tick(0.1);
        Assert.False(vehicle.BuildCurrentState().Driving);

        vehicle.UpdateNodeRelease("B", true);
        vehicle.Tick(0.1);

        Assert.True(vehicle.BuildCurrentState().Driving);
    }

    #endregion

    #region UpdateEdgeRelease Tests

    [Fact]
    public void UpdateEdgeRelease_ReleasesEdge_AllowsMovement()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 5, 0, true));
        order.Edges[0].Released = false;
        order.Nodes[1].Released = true;
        vehicle.ProcessOrder(order);
        vehicle.Tick(0.1);
        Assert.False(vehicle.BuildCurrentState().Driving);

        vehicle.UpdateEdgeRelease("edge1", true);
        vehicle.Tick(0.1);

        Assert.True(vehicle.BuildCurrentState().Driving);
    }

    #endregion

    #region StateChanged Event Tests

    [Fact]
    public void StateChanged_OnlyRaisedWhenStateDiffers()
    {
        var vehicle = CreateVehicle();
        int stateChangedCount = 0;
        vehicle.StateChanged += (_, _) => stateChangedCount++;

        vehicle.Tick(0.1); // Initial state emit
        vehicle.Tick(0.1); // No change

        Assert.Equal(1, stateChangedCount);
    }

    #endregion

    #region Scaled DeltaTime Tests

    [Fact]
    public void Tick_WithLargerDeltaTime_MovesFaster()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 10, 0, true));
        vehicle.ProcessOrder(order);

        // Simulate 1x time scale (deltaTime = 1.0)
        vehicle.Tick(1.0);
        var positionAfterNormalTick = vehicle.BuildCurrentState().AgvPosition.X;

        // Simulate 2x time scale (deltaTime = 2.0)
        vehicle.Tick(2.0);
        var positionAfterFastTick = vehicle.BuildCurrentState().AgvPosition.X;

        var firstMove = positionAfterNormalTick;
        var secondMove = positionAfterFastTick - positionAfterNormalTick;

        Assert.True(secondMove > firstMove, $"Expected second move ({secondMove}) to be greater than first move ({firstMove})");
    }

    [Fact]
    public void Tick_WithZeroDeltaTime_DoesNotMove()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 10, 0, true));
        vehicle.ProcessOrder(order);

        vehicle.Tick(0.0);

        var state = vehicle.BuildCurrentState();
        Assert.Equal(0, state.AgvPosition.X);
    }

    [Fact]
    public void Tick_WithLargeDeltaTime_ReachesDestinationFaster()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 4, 0, true)); // 4 units away
        vehicle.ProcessOrder(order);
        bool routeCompleted = false;
        vehicle.RouteCompleted += (_, _) => routeCompleted = true;

        // Small delta - should not complete
        vehicle.Tick(1.0);
        Assert.False(routeCompleted);

        // Large delta (simulates high time scale) - should complete
        vehicle.Tick(10.0);
        vehicle.Tick(0.1);

        Assert.True(routeCompleted);
    }

    [Fact]
    public void Tick_WithSmallerDeltaTime_SlowsMovement()
    {
        var vehicle = CreateVehicle();
        var order = CreateValidOrder("order1", 1, ("A", 0, 0, 0, true), ("B", 2, 20, 0, true));
        vehicle.ProcessOrder(order);

        // First tick with larger delta (simulates 2x scale)
        vehicle.Tick(2.0);
        var positionAfterFastTick = vehicle.BuildCurrentState().AgvPosition.X;

        // Second tick with smaller delta (simulates 0.5x scale)
        vehicle.Tick(0.5);
        var secondMove = vehicle.BuildCurrentState().AgvPosition.X - positionAfterFastTick;

        Assert.True(secondMove < positionAfterFastTick, "Movement should be slower with smaller deltaTime");
    }

    #endregion
}