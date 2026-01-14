using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using VehicleSim.Application.Helpers;
using VehicleSim.Core.Vehicle;

namespace VehicleSim.Application.Services
{
    public class SimulationEngine(
            IFleetManager fleetManager,
            ITimeProvider timeProvider,
            ILogger<SimulationEngine> logger) : BackgroundService
    {

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // Main simulation loop
            var stopwatch = Stopwatch.StartNew();
            double lastTime = stopwatch.Elapsed.TotalSeconds;

            while (!ct.IsCancellationRequested)
            {
                double currentTime = stopwatch.Elapsed.TotalSeconds;
                double deltaTime = (currentTime - lastTime) * timeProvider.TimeScale;
                lastTime = currentTime;

                foreach (var vehicle in fleetManager.GetAllVehicles())
                {
                    using (logger.BeginScope(new Dictionary<string, object>
                    {
                        ["SerialNumber"] = vehicle.SerialNumber
                    }))
                    {
                        try
                        {
                            vehicle.Tick(deltaTime);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "{SerialNumber}: Error occurred during tick.", vehicle.SerialNumber);
                        }
                    }
                }
                await Task.Delay(20, ct); // Arbitrary delay to control simulation speed
            }
        }

        public override async Task StopAsync(CancellationToken ct)
        {
            logger.LogWarning("Stopping simulation engine...");
            await base.StopAsync(ct);
        }
    }
}
