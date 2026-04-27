using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds an OHLC series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="values">The open, high, low, and close values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddOhlc(string name, IEnumerable<ChartCandlestick> values, ChartColor? color = null) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        var points = new List<ChartPoint>();
        foreach (var value in values) {
            points.Add(new ChartPoint(value.X, value.Open));
            points.Add(new ChartPoint(value.X, value.High));
            points.Add(new ChartPoint(value.X, value.Low));
            points.Add(new ChartPoint(value.X, value.Close));
        }

        if (points.Count == 0) throw new ArgumentException("OHLC charts must contain at least one value.", nameof(values));
        return Add(name, ChartSeriesKind.Ohlc, points, color);
    }
}
