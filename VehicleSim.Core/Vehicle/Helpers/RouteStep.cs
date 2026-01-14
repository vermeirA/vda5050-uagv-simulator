using VehicleSim.Core.VdaModels;

namespace VehicleSim.Core.Vehicle.Helpers
{
    public class RouteStep
    {
        public required VdaNode TargetNode { get; set; }
        public VdaEdge? IncomingEdge { get; set; }
        public bool IsReleased { get; set; }
        public VdaPosition TargetPosition => TargetNode.Position;
    }
}
