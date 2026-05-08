using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>
    /// Gets x-axis tick label highlight colors keyed by axis value.
    /// </summary>
    public Dictionary<double, ChartColor> XAxisLabelHighlights { get; } = new();

    internal List<double> XAxisFocusGuideValues { get; } = new();

    internal bool TryGetXAxisLabelHighlight(double value, out ChartColor color) {
        if (XAxisLabelHighlights.TryGetValue(value, out color)) return true;
        foreach (var item in XAxisLabelHighlights) {
            if (!AxisValueEquals(item.Key, value)) continue;
            color = item.Value;
            return true;
        }

        color = default;
        return false;
    }

    internal static bool AxisValueEquals(double left, double right) {
        var tolerance = Math.Max(1e-9, Math.Max(Math.Abs(left), Math.Abs(right)) * 1e-9);
        return Math.Abs(left - right) <= tolerance;
    }
}
