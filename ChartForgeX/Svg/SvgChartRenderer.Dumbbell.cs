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
            var color = PointColor(chart, series, index, item);
            var label = FormatValue(chart, start.Y) + "-" + FormatValue(chart, end.Y);

            sb.AppendLine($"<g data-cfx-role=\"dumbbell\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-x=\"{F(start.X)}\" data-cfx-start=\"{F(start.Y)}\" data-cfx-end=\"{F(end.Y)}\" data-cfx-delta=\"{F(end.Y - start.Y)}\" role=\"img\" aria-label=\"{Escape(label)}\">");
            sb.AppendLine($"<line data-cfx-role=\"dumbbell-connector\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" x1=\"{F(x)}\" y1=\"{F(yStart)}\" x2=\"{F(x)}\" y2=\"{F(yEnd)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.DumbbellConnectorStrokeWidth)}\" stroke-linecap=\"round\" opacity=\"0.42\"/>");
            sb.AppendLine($"<circle data-cfx-role=\"dumbbell-start\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-value=\"{F(start.Y)}\" cx=\"{F(x)}\" cy=\"{F(yStart)}\" r=\"{F(radius)}\" fill=\"{startColor.ToCss()}\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.MarkerStrokeWidth)}\"/>");
            sb.AppendLine($"<circle data-cfx-role=\"dumbbell-end\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-value=\"{F(end.Y)}\" cx=\"{F(x)}\" cy=\"{F(yEnd)}\" r=\"{F(radius)}\" fill=\"{color.ToCss()}\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.MarkerStrokeWidth)}\"/>");
            sb.AppendLine("</g>");
            if (ShouldDrawDataLabels(chart, series)) {
                var placement = DataLabelPlacement(chart, series);
                var top = Math.Min(yStart, yEnd);
                var bottom = Math.Max(yStart, yEnd);
                if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
                    var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
                    var labelX = placement == ChartDataLabelPlacement.Left ? x - radius - 8 : x + radius + 8;
                    if (ReserveSvgHorizontalLabel(label, labelX, (top + bottom) / 2, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, (top + bottom) / 2, anchor, plot, series, item);
                } else {
                    var labelY = placement == ChartDataLabelPlacement.Below
                        ? bottom + radius + 8
                        : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                            ? (top + bottom) / 2
                            : top - radius - 8;
                    if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: item);
                }
            }
        }
    }
}
