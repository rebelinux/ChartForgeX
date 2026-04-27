using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a bubble chart series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="bubbles">The bubble values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddBubble(string name, IEnumerable<ChartBubble> bubbles, ChartColor? color = null) {
        if (bubbles == null) throw new ArgumentNullException(nameof(bubbles));
        var points = new List<ChartPoint>();
        foreach (var bubble in bubbles) {
            points.Add(new ChartPoint(bubble.X, bubble.Y));
            points.Add(new ChartPoint(bubble.X, bubble.Size));
        }

        if (points.Count == 0) throw new ArgumentException("Bubble charts must contain at least one bubble.", nameof(bubbles));
        return Add(name, ChartSeriesKind.Bubble, points, color);
    }
}
