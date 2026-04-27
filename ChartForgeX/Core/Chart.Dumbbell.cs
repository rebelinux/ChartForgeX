using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a dumbbell comparison series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="values">The paired values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddDumbbell(string name, IEnumerable<ChartDumbbell> values, ChartColor? color = null) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var points = new List<ChartPoint>();
        foreach (var value in values) {
            points.Add(new ChartPoint(value.X, value.Start));
            points.Add(new ChartPoint(value.X, value.End));
        }

        if (points.Count == 0) throw new ArgumentException("Dumbbell charts must contain at least one value.", nameof(values));
        return Add(name, ChartSeriesKind.Dumbbell, points, color);
    }
}
