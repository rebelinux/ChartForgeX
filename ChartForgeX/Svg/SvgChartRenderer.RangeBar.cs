using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawRangeBars(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var intervalCount = Math.Max(1, series.Points.Count / 2);
        var barWidth = Math.Max(8, Math.Min(28, plot.Width / Math.Max(1, intervalCount * 4.0)));
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var start = series.Points[pointIndex];
            var end = series.Points[pointIndex + 1];
            var x = map.X(start.X);
            var y1 = map.Y(start.Y);
            var y2 = map.Y(end.Y);
            var top = Math.Min(y1, y2);
            var height = Math.Max(2, Math.Abs(y2 - y1));
            var intervalIndex = pointIndex / 2;
            var color = PointColor(chart, series, index, intervalIndex);
            var summary = FormatValue(chart, Math.Min(start.Y, end.Y)) + "-" + FormatValue(chart, Math.Max(start.Y, end.Y));

            sb.AppendLine($"<rect data-cfx-role=\"range-bar\" data-cfx-series=\"{index}\" data-cfx-point=\"{intervalIndex}\" data-cfx-x=\"{F(start.X)}\" data-cfx-start=\"{F(start.Y)}\" data-cfx-end=\"{F(end.Y)}\" role=\"img\" aria-label=\"{Escape(summary)}\" x=\"{F(x - barWidth / 2)}\" y=\"{F(top)}\" width=\"{F(barWidth)}\" height=\"{F(height)}\" rx=\"{F(Math.Min(7, barWidth / 2))}\" fill=\"{color.ToCss()}\" opacity=\"0.88\"/>");
            sb.AppendLine($"<line data-cfx-role=\"range-bar-cap\" data-cfx-series=\"{index}\" data-cfx-point=\"{intervalIndex}\" data-cfx-bound=\"start\" data-cfx-value=\"{F(start.Y)}\" x1=\"{F(x - barWidth * 0.75)}\" y1=\"{F(y1)}\" x2=\"{F(x + barWidth * 0.75)}\" y2=\"{F(y1)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.RangeBarCapStrokeWidth)}\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<line data-cfx-role=\"range-bar-cap\" data-cfx-series=\"{index}\" data-cfx-point=\"{intervalIndex}\" data-cfx-bound=\"end\" data-cfx-value=\"{F(end.Y)}\" x1=\"{F(x - barWidth * 0.75)}\" y1=\"{F(y2)}\" x2=\"{F(x + barWidth * 0.75)}\" y2=\"{F(y2)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.RangeBarCapStrokeWidth)}\" stroke-linecap=\"round\"/>");
            if (ShouldDrawDataLabels(chart, series)) DrawRangeBarLabel(sb, chart, series, intervalIndex, plot, reservedLabels, summary, x, y1, y2, top, height, barWidth);
        }
    }

    private static void DrawRangeBarLabel(StringBuilder sb, Chart chart, ChartSeries series, int pointIndex, ChartRect plot, List<ChartLabelBounds> reservedLabels, string label, double x, double y1, double y2, double top, double height, double barWidth) {
        var placement = DataLabelPlacement(chart, series);
        if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
            var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
            var labelX = placement == ChartDataLabelPlacement.Left ? x - barWidth * 0.9 - 6 : x + barWidth * 0.9 + 6;
            if (ReserveSvgHorizontalLabel(label, labelX, top + height / 2, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, top + height / 2, anchor, plot, series, pointIndex);
            return;
        }

        var bottom = Math.Max(y1, y2);
        var labelY = placement == ChartDataLabelPlacement.Below
            ? bottom + 10
            : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                ? top + height / 2
                : top - 10;
        if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: pointIndex);
    }
}
