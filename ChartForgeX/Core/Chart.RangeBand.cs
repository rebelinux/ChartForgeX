using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a range-band series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="bands">The lower and upper band values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRangeBand(string name, IEnumerable<ChartRangeBand> bands, ChartColor? color = null) {
        if (bands == null) throw new ArgumentNullException(nameof(bands));
        var points = new List<ChartPoint>();
        foreach (var band in bands) {
            points.Add(new ChartPoint(band.X, band.Lower));
            points.Add(new ChartPoint(band.X, band.Upper));
        }

        if (points.Count == 0) throw new ArgumentException("Range bands must contain at least one value.", nameof(bands));
        return Add(name, ChartSeriesKind.RangeBand, points, color);
    }
}
