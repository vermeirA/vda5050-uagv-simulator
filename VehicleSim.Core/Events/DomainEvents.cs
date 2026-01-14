using VehicleSim.Core.VdaModels;

namespace VehicleSim.Core.Events
{
    public interface IDomainEvent { }
    public record VehicleStateChangedEvent(string SerialNumber, VdaState State) : IDomainEvent;
    public record VehicleOrderReceivedEvent(string SerialNumber, string OrderId) : IDomainEvent;
    public record VehiclePositionChangedEvent(string SerialNumber, VdaPosition Position) : IDomainEvent;
    public record RouteCompletedEvent(string SerialNumber, VdaState State): IDomainEvent;
}
