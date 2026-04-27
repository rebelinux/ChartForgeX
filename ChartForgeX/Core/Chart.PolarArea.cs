using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a polar area series with equal-angle radial segments scaled by value.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The segment values. The x values are used for optional segment labels.</param>
    /// <returns>The current chart.</returns>
    public Chart AddPolarArea(string name, IEnumerable<ChartPoint> points) => Add(name, ChartSeriesKind.PolarArea, points, null);
}
