using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using VehicleSim.Application.Contracts;
using VehicleSim.Application.Factories;
using VehicleSim.Core.Vehicle;

namespace VehicleSim.Application.Services
{
    public interface IFleetManager
    {
        void AddVehicle(VehicleRequestContract config);
        void RemoveVehicle(string serialNumber);

        IVehicle? GetVehicle(string serialNumber);
        IEnumerable<IVehicle> GetAllVehicles();

        event EventHandler<IVehicle>? VehicleAdded;
        event EventHandler<IVehicle>? VehicleRemoved;
    }

    public class FleetManager(IVehicleFactory vehicleFactory, ILogger<FleetManager> logger) : IFleetManager
    {
        private readonly ConcurrentDictionary<string, IVehicle> vehicles = new();

        public event EventHandler<IVehicle>? VehicleAdded;
        public event EventHandler<IVehicle>? VehicleRemoved;

        public void AddVehicle(VehicleRequestContract config)
        {
            if (vehicles.ContainsKey(config.SerialNumber))
            {
                logger.LogWarning("Vehicle with serial number {SerialNumber} already exists in the fleet.", config.SerialNumber);
                return;
            }

            var vehicle = vehicleFactory.Create(config);

            if (vehicles.TryAdd(config.SerialNumber, vehicle))
            {
                logger.LogInformation("FleetManager: Added vehicle {Sn}", config.SerialNumber);
                VehicleAdded?.Invoke(this, vehicle);
            }
        }

        public void RemoveVehicle(string serialNumber)
        {
            if (vehicles.TryRemove(serialNumber, out var vehicle))
            {
                logger.LogInformation("FleetManager: Removed vehicle {Sn}", serialNumber);
                VehicleRemoved?.Invoke(this, vehicle);
            }
            else
            {
                logger.LogWarning("FleetManager: Attempted to remove non-existent vehicle {Sn}", serialNumber);
            }
        }

        public IVehicle? GetVehicle(string serialNumber) => vehicles.TryGetValue(serialNumber, out var vehicle) ? vehicle : null;

        public IEnumerable<IVehicle> GetAllVehicles() => vehicles.Values;
    }
}
