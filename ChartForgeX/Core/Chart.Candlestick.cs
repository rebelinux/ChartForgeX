using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a candlestick series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="candles">The open, high, low, and close values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddCandlestick(string name, IEnumerable<ChartCandlestick> candles, ChartColor? color = null) {
        if (candles == null) throw new ArgumentNullException(nameof(candles));
        var points = new List<ChartPoint>();
        foreach (var candle in candles) {
            points.Add(new ChartPoint(candle.X, candle.Open));
            points.Add(new ChartPoint(candle.X, candle.High));
            points.Add(new ChartPoint(candle.X, candle.Low));
            points.Add(new ChartPoint(candle.X, candle.Close));
        }

        if (points.Count == 0) throw new ArgumentException("Candlestick charts must contain at least one value.", nameof(candles));
        return Add(name, ChartSeriesKind.Candlestick, points, color);
    }
}
