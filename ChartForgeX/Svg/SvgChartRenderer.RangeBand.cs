using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawRangeBand(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var color = Color(chart, index);
        var lower = new List<ChartPoint>();
        var upper = new List<ChartPoint>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var low = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            lower.Add(new ChartPoint(map.X(low.X), map.Y(low.Y)));
            upper.Add(new ChartPoint(map.X(high.X), map.Y(high.Y)));
        }

        if (lower.Count == 0) return;
        var path = BuildRangeBandPath(lower, upper);
        sb.AppendLine($"<path data-cfx-role=\"range-band\" data-cfx-series=\"{index}\" data-cfx-interval-count=\"{lower.Count}\" d=\"{path}\" fill=\"{color.ToCss()}\" opacity=\"{F(ChartVisualPrimitives.RangeBandFillOpacity)}\"/>");
        sb.AppendLine($"<path data-cfx-role=\"range-band-upper\" data-cfx-series=\"{index}\" data-cfx-interval-count=\"{upper.Count}\" d=\"{BuildLinePath(upper, false)}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.RangeBandBoundaryStrokeWidth)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"{F(ChartVisualPrimitives.RangeBandBoundaryOpacity)}\"/>");
        sb.AppendLine($"<path data-cfx-role=\"range-band-lower\" data-cfx-series=\"{index}\" data-cfx-interval-count=\"{lower.Count}\" d=\"{BuildLinePath(lower, false)}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.RangeBandBoundaryStrokeWidth)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"{F(ChartVisualPrimitives.RangeBandBoundaryOpacity)}\"/>");
        if (ShouldDrawDataLabels(chart, series)) {
            var reservedLabels = new List<ChartLabelBounds>();
            for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
                var low = series.Points[pointIndex];
                var high = series.Points[pointIndex + 1];
                var x = map.X(low.X);
                var y = Math.Min(map.Y(low.Y), map.Y(high.Y)) - 10;
                var label = FormatValue(chart, low.Y) + "-" + FormatValue(chart, high.Y);
                if (!ReserveSvgLabel(label, x, y, chart, plot, reservedLabels)) continue;
                DrawDataLabel(sb, chart, label, x, y, plot);
            }
        }
    }

    private static string BuildRangeBandPath(IReadOnlyList<ChartPoint> lower, IReadOnlyList<ChartPoint> upper) {
        var path = BuildLinePath(upper, false);
        for (var i = lower.Count - 1; i >= 0; i--) path += " L " + F(lower[i].X) + " " + F(lower[i].Y);
        return path + " Z";
    }
}
