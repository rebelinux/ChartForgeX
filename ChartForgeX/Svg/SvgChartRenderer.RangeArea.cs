using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawRangeArea(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map, string id) {
        var series = chart.Series[index];
        var color = Color(chart, index);
        var lower = new List<ChartPoint>();
        var upper = new List<ChartPoint>();
        var middle = new List<ChartPoint>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var low = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            lower.Add(new ChartPoint(map.X(low.X), map.Y(low.Y)));
            upper.Add(new ChartPoint(map.X(high.X), map.Y(high.Y)));
            middle.Add(new ChartPoint(map.X(low.X), map.Y((low.Y + high.Y) / 2.0)));
        }

        if (lower.Count == 0) return;
        var upperPath = ChartPathBuilder.FromPoints(upper, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var lowerPath = ChartPathBuilder.FromPoints(lower, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var middlePath = ChartPathBuilder.FromPoints(middle, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var upperLine = BuildLinePath(upperPath, false);
        var lowerLine = BuildLinePath(lowerPath, false);
        var middleLine = BuildLinePath(middlePath, false);
        var summary = series.Name + " interval area with " + lower.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " point(s)";

        sb.AppendLine($"<g data-cfx-role=\"range-area-series\" data-cfx-series=\"{index}\" role=\"img\" aria-label=\"{Escape(summary)}\">");
        sb.AppendLine($"<path data-cfx-role=\"range-area\" data-cfx-series=\"{index}\" d=\"{BuildClosedPolygonPath(upperPath, lowerPath)}\" fill=\"url(#{id}-area{index})\" opacity=\"0.96\"/>");
        sb.AppendLine($"<path data-cfx-role=\"range-area-midline\" data-cfx-series=\"{index}\" d=\"{middleLine}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"1.3\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-dasharray=\"5 6\" opacity=\"0.48\"/>");
        sb.AppendLine($"<path data-cfx-role=\"range-area-upper-halo\" data-cfx-series=\"{index}\" d=\"{upperLine}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(series.StrokeWidth + 4)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"0.14\"/>");
        sb.AppendLine($"<path data-cfx-role=\"range-area-lower-halo\" data-cfx-series=\"{index}\" d=\"{lowerLine}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(series.StrokeWidth + 4)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"0.14\"/>");
        sb.AppendLine($"<path data-cfx-role=\"range-area-upper\" data-cfx-series=\"{index}\" d=\"{upperLine}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(Math.Max(1.6, series.StrokeWidth))}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
        sb.AppendLine($"<path data-cfx-role=\"range-area-lower\" data-cfx-series=\"{index}\" d=\"{lowerLine}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(Math.Max(1.6, series.StrokeWidth))}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"0.88\"/>");
        sb.AppendLine("</g>");

        if (!ShouldDrawDataLabels(chart, series)) return;
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var low = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            var x = map.X(low.X);
            var y = Math.Min(map.Y(low.Y), map.Y(high.Y)) - 10;
            DrawDataLabel(sb, chart, FormatValue(chart, low.Y) + "-" + FormatValue(chart, high.Y), x, y, plot);
        }
    }
}
