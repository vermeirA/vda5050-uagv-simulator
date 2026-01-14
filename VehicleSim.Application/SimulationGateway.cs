using VehicleSim.Application.Contracts;
using VehicleSim.Application.Helpers;
using VehicleSim.Application.Services;
using VehicleSim.Core.VdaModels;

namespace VehicleSim.Application
{
    public interface ISimulationGateway
    {
        Task InitializeAsync();
        void ResetSimulation();
        void AdjustTimeScale(double newTimeScale);
    }

    public interface IVehicleService
    {
        void AddVehicle(VehicleRequestContract config);
        void RemoveVehicle(string serialNumber);
        void PairVehicle(string serialNumber);
        void UnpairVehicle(string serialNumber);
        void InjectError(string serialNumber, VdaError error);
        void ResetVehicle(string serialNumber);

        VehicleResponseContract? GetVehicle(string serialNumber);
        IEnumerable<VehicleResponseContract> GetAllVehicles();
    }

    public class ApplicationGateway(
        IMqttBridge mqttBridge,
        IFleetManager fleetManager,
        ITimeProvider simulationTime) : ISimulationGateway, IVehicleService
    {
        public Task InitializeAsync() => mqttBridge.InitializeAsync();

        public void ResetSimulation()
        {
            foreach (var vehicle in fleetManager.GetAllVehicles())
                vehicle.HardReset();
        }

        public void AdjustTimeScale(double newTimeScale)
            => simulationTime.TimeScale = newTimeScale;

        public void AddVehicle(VehicleRequestContract config)
            => fleetManager.AddVehicle(config);

        public void RemoveVehicle(string serialNumber)
            => fleetManager.RemoveVehicle(serialNumber);

        public void PairVehicle(string serialNumber)
        {
            var vehicle = fleetManager.GetVehicle(serialNumber)
                ?? throw new InvalidOperationException($"Vehicle {serialNumber} not found.");
            mqttBridge.PairVehicle(null, vehicle);
        }

        public void UnpairVehicle(string serialNumber)
        {
            var vehicle = fleetManager.GetVehicle(serialNumber)
                ?? throw new InvalidOperationException($"Vehicle {serialNumber} not found.");
            mqttBridge.UnpairVehicle(null, vehicle);
        }

        public void InjectError(string serialNumber, VdaError error)
        {
            var vehicle = fleetManager.GetVehicle(serialNumber)
                ?? throw new InvalidOperationException($"Vehicle {serialNumber} not found.");
            vehicle.ReportError(error.ErrorType, error.ErrorDescription, error.ErrorLevel);
        }

        public void ResetVehicle(string serialNumber)
        {
            var vehicle = fleetManager.GetVehicle(serialNumber)
                ?? throw new InvalidOperationException($"Vehicle {serialNumber} not found.");
            vehicle.SoftReset();
        }

        public VehicleResponseContract? GetVehicle(string serialNumber)
            => fleetManager.GetVehicle(serialNumber)?.ToResponseContract();

        public IEnumerable<VehicleResponseContract> GetAllVehicles()
            => fleetManager.GetAllVehicles().Select(v => v.ToResponseContract());
    }
}