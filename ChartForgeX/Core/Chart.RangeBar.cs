using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a vertical range bar series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="intervals">The interval values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRangeBar(string name, IEnumerable<ChartInterval> intervals, ChartColor? color = null) {
        if (intervals == null) throw new ArgumentNullException(nameof(intervals));
        var points = new List<ChartPoint>();
        foreach (var interval in intervals) {
            points.Add(new ChartPoint(interval.X, interval.Start));
            points.Add(new ChartPoint(interval.X, interval.End));
        }

        if (points.Count == 0) throw new ArgumentException("Range bar intervals must contain at least one interval.", nameof(intervals));
        return Add(name, ChartSeriesKind.RangeBar, points, color);
    }
}
