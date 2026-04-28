using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawDumbbells(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var color = Color(chart, index);
        var startColor = chart.Options.Theme.MutedText;
        var radius = Math.Max(ChartVisualPrimitives.DumbbellMarkerMinRadius, chart.Options.Theme.MarkerRadius + ChartVisualPrimitives.DumbbellMarkerRadiusExtra);
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var start = series.Points[pointIndex];
            var end = series.Points[pointIndex + 1];
            var x = map.X(start.X);
            var yStart = map.Y(start.Y);
            var yEnd = map.Y(end.Y);
            var item = pointIndex / 2;
            var label = FormatValue(chart, start.Y) + "-" + FormatValue(chart, end.Y);

            sb.AppendLine($"<g data-cfx-role=\"dumbbell\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-x=\"{F(start.X)}\" data-cfx-start=\"{F(start.Y)}\" data-cfx-end=\"{F(end.Y)}\" data-cfx-delta=\"{F(end.Y - start.Y)}\" role=\"img\" aria-label=\"{Escape(label)}\">");
            sb.AppendLine($"<line data-cfx-role=\"dumbbell-connector\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" x1=\"{F(x)}\" y1=\"{F(yStart)}\" x2=\"{F(x)}\" y2=\"{F(yEnd)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.DumbbellConnectorStrokeWidth)}\" stroke-linecap=\"round\" opacity=\"0.42\"/>");
            sb.AppendLine($"<circle data-cfx-role=\"dumbbell-start\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-value=\"{F(start.Y)}\" cx=\"{F(x)}\" cy=\"{F(yStart)}\" r=\"{F(radius)}\" fill=\"{startColor.ToCss()}\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.MarkerStrokeWidth)}\"/>");
            sb.AppendLine($"<circle data-cfx-role=\"dumbbell-end\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-value=\"{F(end.Y)}\" cx=\"{F(x)}\" cy=\"{F(yEnd)}\" r=\"{F(radius)}\" fill=\"{color.ToCss()}\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.MarkerStrokeWidth)}\"/>");
            sb.AppendLine("</g>");
            var labelY = Math.Min(yStart, yEnd) - radius - 8;
            if (ShouldDrawDataLabels(chart, series) && ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot);
        }
    }
}
