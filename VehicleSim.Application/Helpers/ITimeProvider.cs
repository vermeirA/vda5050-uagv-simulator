using System;
using System.Collections.Generic;
using System.Text;

namespace VehicleSim.Application.Helpers
{
    public interface ITimeProvider
    {
        double TimeScale { get; set; }
        event Action<double>? TimeScaleChanged;
    }
}
