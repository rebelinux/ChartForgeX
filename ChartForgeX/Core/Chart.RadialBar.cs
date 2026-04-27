using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a radial bar series where each point renders as a circular progress ring.
    /// </summary>
    /// <param name="name">The radial bar group name.</param>
    /// <param name="points">The ring values. The x values identify labels and y values must be between zero and 100.</param>
    /// <param name="color">An optional shared ring color. When null, the theme palette colors rings independently.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRadialBar(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) {
        if (points == null) throw new ArgumentNullException(nameof(points));
        var materialized = ChartGuards.Points(points, nameof(points));
        if (materialized.Count == 0) throw new ArgumentException("Radial bar charts must contain at least one value.", nameof(points));
        for (var i = 0; i < materialized.Count; i++) {
            var value = materialized[i].Y;
            if (value < 0 || value > 100) throw new ArgumentOutOfRangeException(nameof(points), value, "Radial bar values must be between zero and 100.");
        }

        return Add(name, ChartSeriesKind.RadialBar, materialized, color);
    }
}
