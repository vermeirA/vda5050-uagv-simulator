using Microsoft.Extensions.Logging;
using System.Text.Json;
using VehicleSim.Core.Events;
using VehicleSim.Core.VdaModels;
using VehicleSim.Core.Vehicle;

namespace VehicleSim.Application.Services
{
    public interface IMqttBridge
    {
        Task InitializeAsync();
        void PairVehicle(object? sender, IVehicle vehicle);
        void UnpairVehicle(object? sender, IVehicle vehicle);
    }

    public sealed class MqttBridge(IMqttAdapter adapter, IFleetManager fleetManager, ILogger<MqttBridge> logger) : IMqttBridge, IAsyncDisposable
    {
        private readonly Dictionary<string, VehicleHandlers> handlers = [];

        public async Task InitializeAsync()
        {
            await adapter.ConnectAsync();
            adapter.MessageReceived += OnMessageReceived;

            foreach (var vehicle in fleetManager.GetAllVehicles())
            {
                await SubscribeVehicleAsync(vehicle);
            }

            fleetManager.VehicleAdded += PairVehicle;
            fleetManager.VehicleRemoved += UnpairVehicle;

            logger.LogInformation("MQTT Bridge initialized");
        }

        public async void PairVehicle(object? sender, IVehicle vehicle)
        {
            try { await SubscribeVehicleAsync(vehicle); }
            catch (Exception ex) { logger.LogError(ex, "Failed to subscribe vehicle {sn}", vehicle.SerialNumber); }
        }

        public async void UnpairVehicle(object? sender, IVehicle vehicle)
        {
            try { await UnsubscribeVehicleAsync(vehicle); }
            catch (Exception ex) { logger.LogError(ex, "Failed to unsubscribe vehicle {sn}", vehicle.SerialNumber); }
        }

        private async Task SubscribeVehicleAsync(IVehicle vehicle)
        {
            if (handlers.ContainsKey(vehicle.SerialNumber))
            {
                logger.LogDebug("Vehicle {sn} already subscribed, skipping", vehicle.SerialNumber);
                return;
            }

            await adapter.SubscribeAsync(vehicle.SerialNumber);

            var connMsg = JsonSerializer.Serialize(vehicle.BuildConnectionMessage());
            await adapter.AnnounceConnectionAsync(vehicle.SerialNumber, connMsg);

            EventHandler<VehicleStateChangedEvent> stateHandler = async (s, e) =>
            {
                try
                {
                    var payload = JsonSerializer.Serialize(e.State);
                    await adapter.PublishStateAsync(e.SerialNumber, payload);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to publish state for {sn}", e.SerialNumber);
                }
            };

            EventHandler<RouteCompletedEvent> routeCompletedHandler = async (s, e) =>
            {
                try
                {
                    var payload = JsonSerializer.Serialize(e.State);
                    await adapter.PublishStateAsync(e.SerialNumber, payload);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to publish state for {sn}", e.SerialNumber);
                }
            };

            EventHandler<VehiclePositionChangedEvent> positionHandler = async (s, e) =>
            {
                try
                {
                    var payload = JsonSerializer.Serialize( new { agvPosition = e.Position });
                    await adapter.PublishVisualizationAsync(e.SerialNumber, payload);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to publish position for {sn}", e.SerialNumber);
                }
            };

            vehicle.StateChanged += stateHandler;
            vehicle.PositionChanged += positionHandler;
            vehicle.RouteCompleted += routeCompletedHandler;

            handlers[vehicle.SerialNumber] = new VehicleHandlers(stateHandler, positionHandler, routeCompletedHandler);

            logger.LogDebug("Vehicle {sn} subscribed to MQTT", vehicle.SerialNumber);
        }

        private async Task UnsubscribeVehicleAsync(IVehicle vehicle)
        {
            if (!handlers.ContainsKey(vehicle.SerialNumber))
            {
                logger.LogDebug("Vehicle {sn} not subscribed, skipping unsubscribe", vehicle.SerialNumber);
                return;
            }

            if (handlers.Remove(vehicle.SerialNumber, out var vehicleHandlers))
            {
                vehicle.StateChanged -= vehicleHandlers.StateHandler;
                vehicle.PositionChanged -= vehicleHandlers.PositionHandler;
                vehicle.RouteCompleted -= vehicleHandlers.RouteCompletedHandler;
            }

            var disconnMsg = JsonSerializer.Serialize(vehicle.BuildDisconnectionMessage());
            await adapter.AnnounceDisconnectionAsync(vehicle.SerialNumber, disconnMsg);
            await adapter.UnsubscribeAsync(vehicle.SerialNumber);

            logger.LogInformation("Vehicle {sn} unsubscribed from MQTT", vehicle.SerialNumber);
        }

        private void OnMessageReceived(string topic, string payload)
        {
            var parts = topic.Split('/');
            if (parts.Length != 5) return;

            var serialNumber = parts[3];
            var vehicle = fleetManager.GetVehicle(serialNumber);

            if (vehicle is null)
            {
                logger.LogWarning("Message for unknown vehicle: {sn}", serialNumber);
                return;
            }

            try
            {
                if (topic.EndsWith("/order"))
                {
                    var order = JsonSerializer.Deserialize<VdaOrder>(payload);
                    if (order is not null) vehicle.ProcessOrder(order);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{SerialNumber}: Error processing MQTT message on {topic}", vehicle.SerialNumber, topic);
            }
        }

        public async ValueTask DisposeAsync()
        {
            fleetManager.VehicleAdded -= PairVehicle;
            fleetManager.VehicleRemoved -= UnpairVehicle;
            adapter.MessageReceived -= OnMessageReceived;

            foreach (var vehicle in fleetManager.GetAllVehicles())
            {
                await UnsubscribeVehicleAsync(vehicle);
            }

            handlers.Clear();
        }

        private record VehicleHandlers(
            EventHandler<VehicleStateChangedEvent> StateHandler,
            EventHandler<VehiclePositionChangedEvent> PositionHandler,
            EventHandler<RouteCompletedEvent> RouteCompletedHandler);
    }
}
