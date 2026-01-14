using VehicleSim.Core.Events;
using VehicleSim.Core.VdaModels;
using VehicleSim.Core.Vehicle.Helpers;

namespace VehicleSim.Core.Vehicle
{
    public interface IVehicle
    {
        void ProcessOrder(VdaOrder order);
        void ReportError(string type, string description, VdaErrorLevel level);
        void SoftReset();
        void HardReset();
        void Tick(double deltaTime);

        event EventHandler<VehicleStateChangedEvent>? StateChanged;
        event EventHandler<VehiclePositionChangedEvent>? PositionChanged;
        event EventHandler<RouteCompletedEvent>? RouteCompleted;

        VdaConnection BuildConnectionMessage();
        VdaConnection BuildDisconnectionMessage();

        string SerialNumber { get; }
        VdaOperatingMode OperatingMode { get; }
        VehicleStatus Status { get; }
        (double x, double y) Position { get; }
    }
}
