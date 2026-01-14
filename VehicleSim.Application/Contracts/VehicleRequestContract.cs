using System;
using System.Collections.Generic;
using System.Text;

namespace VehicleSim.Application.Contracts
{
    public record VehicleRequestContract(
         string SerialNumber,
         string Manufacturer,
         string MapId,
         double StartX,
         double StartY
    );
}
