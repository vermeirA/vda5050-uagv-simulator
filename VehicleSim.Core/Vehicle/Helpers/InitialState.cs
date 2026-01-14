using System;
using System.Collections.Generic;
using System.Text;

namespace VehicleSim.Core.Vehicle.Helpers
{
    internal class InitialState
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Theta { get; set; }
        public required string MapId { get; set; }
    }
}
