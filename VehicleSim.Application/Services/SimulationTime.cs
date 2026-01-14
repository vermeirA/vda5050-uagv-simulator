using Serilog.Core;
using VehicleSim.Application.Helpers;

namespace VehicleSim.Application.Services
{
    public sealed class SimulationTime : ITimeProvider
    {
        private double timeScale = 1.0;
        public event Action<double>? TimeScaleChanged;

        public double TimeScale
        {
            get => timeScale;
            set
            {
                if (value <= 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value), "TimeScale must be between [0.1 - 100]");

                if (Math.Abs(timeScale - value) < double.Epsilon)
                    return;

                timeScale = value;
                TimeScaleChanged?.Invoke(value);
            }
        }
    }
}
