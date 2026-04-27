using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds an error-bar series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="errorBars">The point estimates and bounds to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddErrorBar(string name, IEnumerable<ChartErrorBar> errorBars, ChartColor? color = null) {
        if (errorBars == null) throw new ArgumentNullException(nameof(errorBars));
        var points = new List<ChartPoint>();
        foreach (var errorBar in errorBars) {
            points.Add(new ChartPoint(errorBar.X, errorBar.Y));
            points.Add(new ChartPoint(errorBar.X, errorBar.Lower));
            points.Add(new ChartPoint(errorBar.X, errorBar.Upper));
        }

        if (points.Count == 0) throw new ArgumentException("Error bars must contain at least one value.", nameof(errorBars));
        return Add(name, ChartSeriesKind.ErrorBar, points, color);
    }
}
