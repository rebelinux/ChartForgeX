using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a range-area series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="ranges">The lower and upper interval values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <param name="smooth">A value indicating whether interval boundaries should be smoothed.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRangeArea(string name, IEnumerable<ChartRangeBand> ranges, ChartColor? color = null, bool smooth = true) {
        if (ranges == null) throw new ArgumentNullException(nameof(ranges));
        var points = new List<ChartPoint>();
        foreach (var range in ranges) {
            points.Add(new ChartPoint(range.X, range.Lower));
            points.Add(new ChartPoint(range.X, range.Upper));
        }

        if (points.Count == 0) throw new ArgumentException("Range areas must contain at least one value.", nameof(ranges));
        return Add(name, ChartSeriesKind.RangeArea, points, color, smooth);
    }
}
