using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using VehicleSim.Core.VdaModels;
using VehicleSim.Core.Vehicle;
using VehicleSim.Core.Vehicle.Helpers;

namespace VehicleSim.Application.Contracts
{
    public record VehicleResponseContract(
        string SerialNumber,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        VdaOperatingMode OperatingMode,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        VehicleStatus Status,
        PositionDTO Position);

    public record PositionDTO
    {
        public double X { get; }
        public double Y { get; }

        public PositionDTO(double x, double y)
        {
            X = Math.Round(x, 3);
            Y = Math.Round(y, 3);
        }
    }

    public static class VehicleResponseMapper
    {
        public static VehicleResponseContract ToResponseContract(this IVehicle vehicleDto)
            => new(
                vehicleDto.SerialNumber,
                vehicleDto.OperatingMode,
                vehicleDto.Status,
                new PositionDTO(vehicleDto.Position.x, vehicleDto.Position.y)
            );
    }
}
