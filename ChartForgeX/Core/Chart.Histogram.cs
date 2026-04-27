using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a histogram series by binning raw numeric values.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="values">The raw numeric values to bin.</param>
    /// <param name="binCount">The number of histogram bins.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddHistogram(string name, IEnumerable<double> values, int binCount = 10, ChartColor? color = null) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (binCount < 1) throw new ArgumentOutOfRangeException(nameof(binCount), binCount, "Histogram bin count must be at least one.");

        var materialized = values.ToArray();
        if (materialized.Length == 0) throw new ArgumentException("Histogram values must contain at least one value.", nameof(values));
        for (var i = 0; i < materialized.Length; i++) ChartGuards.Finite(materialized[i], nameof(values));

        var min = materialized.Min();
        var max = materialized.Max();
        if (Math.Abs(max - min) < 0.000001) {
            Options.XAxisLabels.Clear();
            Options.XAxisLabels.Add(new ChartAxisLabel(min, FormatHistogramNumber(min)));
            return Add(name, ChartSeriesKind.Bar, new[] { new ChartPoint(min, materialized.Length) }, color);
        }

        var counts = new int[binCount];
        var width = (max - min) / binCount;
        foreach (var value in materialized) {
            var index = value >= max ? binCount - 1 : (int)Math.Floor((value - min) / width);
            counts[Math.Max(0, Math.Min(binCount - 1, index))]++;
        }

        var points = new List<ChartPoint>(binCount);
        Options.XAxisLabels.Clear();
        for (var i = 0; i < binCount; i++) {
            var start = min + width * i;
            var end = i == binCount - 1 ? max : start + width;
            var center = start + (end - start) / 2.0;
            points.Add(new ChartPoint(center, counts[i]));
            Options.XAxisLabels.Add(new ChartAxisLabel(center, FormatHistogramNumber(start) + "-" + FormatHistogramNumber(end)));
        }

        return Add(name, ChartSeriesKind.Bar, points, color);
    }

    private static string FormatHistogramNumber(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
