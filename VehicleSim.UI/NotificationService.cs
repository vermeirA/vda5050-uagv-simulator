using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using VehicleSim.Application.Contracts;
using VehicleSim.Application.Services;
using VehicleSim.Core.Events;
using VehicleSim.Core.Vehicle;

namespace VehicleSim.UI
{
    public interface INotificationService
    {
        Task InitializeAsync();
    }

    public sealed class NotificationService(
        IFleetManager fleetManager,
        IHubContext<SignalRHub> hubContext,
        ILogger<NotificationService> logger) : INotificationService, IAsyncDisposable
    {
        private readonly Dictionary<string, VehicleEventHandlers> eventHandlers = new();

        public Task InitializeAsync()
        {
            foreach (var vehicle in fleetManager.GetAllVehicles())
            {
                SubscribeToVehicleEvents(vehicle);
            }

            fleetManager.VehicleAdded += OnVehicleAdded;
            fleetManager.VehicleRemoved += OnVehicleRemoved;

            logger.LogInformation("SignalR NotificationService initialized");
            return Task.CompletedTask;
        }

        private void OnVehicleAdded(object? sender, IVehicle vehicle)
        {
            SubscribeToVehicleEvents(vehicle);

            _ = hubContext.Clients.All.SendAsync("VehicleAdded", vehicle.ToResponseContract());
            logger.LogDebug("SignalR subscribed to vehicle {SerialNumber}", vehicle.SerialNumber);
        }

        private void OnVehicleRemoved(object? sender, IVehicle vehicle)
        {
            UnsubscribeFromVehicleEvents(vehicle);

            _ = hubContext.Clients.All.SendAsync("VehicleRemoved", vehicle.SerialNumber);

            logger.LogDebug("SignalR unsubscribed from vehicle {SerialNumber}", vehicle.SerialNumber);
        }

        private void SubscribeToVehicleEvents(IVehicle vehicle)
        {
            EventHandler<VehicleStateChangedEvent> stateHandler = async (s, e) =>
            {
                try
                {
                    await hubContext.Clients.All.SendAsync("VehicleStateChanged", vehicle.ToResponseContract());
                    logger.LogTrace("Dispatched VehicleStateChanged for {SerialNumber}", e.SerialNumber);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to dispatch VehicleStateChanged for {SerialNumber}", e.SerialNumber);
                }
            };

            EventHandler<VehiclePositionChangedEvent> positionHandler = async (s, e) =>
            {
                try
                {
                    await hubContext.Clients.All.SendAsync("VehiclePositionChanged", new
                    {
                        SerialNumber = e.SerialNumber,
                        X = Math.Round(e.Position.X, 2),
                        Y = Math.Round(e.Position.Y, 2)
                    });
                    logger.LogTrace("Dispatched VehiclePositionChanged for {SerialNumber}", e.SerialNumber);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to dispatch VehiclePositionChanged for {SerialNumber}", e.SerialNumber);
                }
            };

            EventHandler<RouteCompletedEvent> routeCompletedHandler = async (s, e) =>
            {
                try
                {
                    await hubContext.Clients.All.SendAsync("VehicleStateChanged", vehicle.ToResponseContract());
                    logger.LogInformation("Dispatched RouteCompleted for {SerialNumber}", e.SerialNumber);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to dispatch RouteCompleted for {SerialNumber}", e.SerialNumber);
                }
            };

            vehicle.StateChanged += stateHandler;
            vehicle.PositionChanged += positionHandler;
            vehicle.RouteCompleted += routeCompletedHandler;

            eventHandlers[vehicle.SerialNumber] = new VehicleEventHandlers(
                stateHandler,
                positionHandler,
                routeCompletedHandler);
        }

        private void UnsubscribeFromVehicleEvents(IVehicle vehicle)
        {
            if (eventHandlers.Remove(vehicle.SerialNumber, out var handlers))
            {
                vehicle.StateChanged -= handlers.StateHandler;
                vehicle.PositionChanged -= handlers.PositionHandler;
                vehicle.RouteCompleted -= handlers.RouteCompletedHandler;
            }
        }

        public ValueTask DisposeAsync()
        {
            fleetManager.VehicleAdded -= OnVehicleAdded;
            fleetManager.VehicleRemoved -= OnVehicleRemoved;

            foreach (var vehicle in fleetManager.GetAllVehicles())
            {
                UnsubscribeFromVehicleEvents(vehicle);
            }

            eventHandlers.Clear();
            logger.LogInformation("SignalR NotificationService disposed");

            return ValueTask.CompletedTask;
        }

        private record VehicleEventHandlers(
            EventHandler<VehicleStateChangedEvent> StateHandler,
            EventHandler<VehiclePositionChangedEvent> PositionHandler,
            EventHandler<RouteCompletedEvent> RouteCompletedHandler);
    }
}