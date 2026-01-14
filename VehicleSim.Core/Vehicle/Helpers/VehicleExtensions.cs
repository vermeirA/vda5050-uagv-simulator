using VehicleSim.Core.VdaModels;

namespace VehicleSim.Core.Vehicle.Helpers
{
    public static class VehicleExtensions
    {       
        public static double GetDistanceTo(this VdaPosition pos, double x, double y)
            => Math.Sqrt(Math.Pow(pos.X - x, 2) + Math.Pow(pos.Y - y, 2));

        public static bool IsAtPosition(this VdaPosition pos, double x, double y, double tolerance = 0.1)
            => pos.GetDistanceTo(x, y) <= tolerance;

        public static bool IsEquivalentTo(this VdaState current, VdaState other)
        {
            if (other == null) return false;

            return current.OrderId == other.OrderId &&
                   current.OrderUpdateId == other.OrderUpdateId &&
                   current.LastNodeId == other.LastNodeId &&
                   current.LastNodeSequenceId == other.LastNodeSequenceId &&
                   current.OperatingMode == other.OperatingMode &&
                   current.Driving == other.Driving &&
                   Math.Abs(current.AgvPosition.X - other.AgvPosition.X) < 0.01 &&
                   Math.Abs(current.AgvPosition.Y - other.AgvPosition.Y) < 0.01;
        }
    }
}
