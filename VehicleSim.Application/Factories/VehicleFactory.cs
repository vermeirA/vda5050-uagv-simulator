using Microsoft.Extensions.DependencyInjection;
using VehicleSim.Application.Contracts;
using VehicleSim.Core.Vehicle;

namespace VehicleSim.Application.Factories
{
    public interface IVehicleFactory
    {
        IVehicle Create(VehicleRequestContract config);
    }

    public class VehicleFactory(IServiceProvider serviceProvider) : IVehicleFactory
    {
        public IVehicle Create(VehicleRequestContract config)
        {
            return ActivatorUtilities.CreateInstance<Vehicle>(
                serviceProvider, 
                config.SerialNumber,
                config.Manufacturer,
                config.MapId,
                config.StartX,
                config.StartY);
        }
    }
}
