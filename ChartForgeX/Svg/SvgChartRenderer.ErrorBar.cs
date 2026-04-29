using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawErrorBars(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var itemCount = Math.Max(1, series.Points.Count / 3);
        var capWidth = Math.Max(9, Math.Min(24, plot.Width / Math.Max(1, itemCount * 8.0)));
        var radius = Math.Max(ChartVisualPrimitives.ErrorBarMarkerMinRadius, chart.Options.Theme.MarkerRadius + ChartVisualPrimitives.ErrorBarMarkerRadiusExtra);
        var reservedLabels = new List<ChartLabelBounds>();

        for (var pointIndex = 0; pointIndex + 2 < series.Points.Count; pointIndex += 3) {
            var center = series.Points[pointIndex];
            var lower = series.Points[pointIndex + 1];
            var upper = series.Points[pointIndex + 2];
            var x = map.X(center.X);
            var y = map.Y(center.Y);
            var yLower = map.Y(lower.Y);
            var yUpper = map.Y(upper.Y);
            var item = pointIndex / 3;
            var color = PointColor(chart, series, index, item);
            var summary = "value " + FormatValue(chart, center.Y) + ", range " + FormatValue(chart, lower.Y) + "-" + FormatValue(chart, upper.Y);

            sb.AppendLine($"<g data-cfx-role=\"error-bar\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-x=\"{F(center.X)}\" data-cfx-value=\"{F(center.Y)}\" data-cfx-lower=\"{F(lower.Y)}\" data-cfx-upper=\"{F(upper.Y)}\" role=\"img\" aria-label=\"{Escape(summary)}\">");
            sb.AppendLine($"<line data-cfx-role=\"error-range\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-lower=\"{F(lower.Y)}\" data-cfx-upper=\"{F(upper.Y)}\" x1=\"{F(x)}\" y1=\"{F(yUpper)}\" x2=\"{F(x)}\" y2=\"{F(yLower)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.ErrorBarStrokeWidth)}\" stroke-linecap=\"round\" opacity=\"0.72\"/>");
            sb.AppendLine($"<line data-cfx-role=\"error-cap\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-bound=\"upper\" data-cfx-value=\"{F(upper.Y)}\" x1=\"{F(x - capWidth / 2)}\" y1=\"{F(yUpper)}\" x2=\"{F(x + capWidth / 2)}\" y2=\"{F(yUpper)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.ErrorBarStrokeWidth)}\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<line data-cfx-role=\"error-cap\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-bound=\"lower\" data-cfx-value=\"{F(lower.Y)}\" x1=\"{F(x - capWidth / 2)}\" y1=\"{F(yLower)}\" x2=\"{F(x + capWidth / 2)}\" y2=\"{F(yLower)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.ErrorBarStrokeWidth)}\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<circle data-cfx-role=\"error-marker\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-value=\"{F(center.Y)}\" cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"{F(radius)}\" fill=\"{color.ToCss()}\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.MarkerStrokeWidth)}\"/>");
            sb.AppendLine("</g>");
            var label = FormatValue(chart, center.Y);
            if (ShouldDrawDataLabels(chart, series)) DrawErrorBarLabel(sb, chart, series, item, plot, reservedLabels, label, x, y, yLower, yUpper, radius, capWidth);
        }
    }

    private static void DrawErrorBarLabel(StringBuilder sb, Chart chart, ChartSeries series, int pointIndex, ChartRect plot, List<ChartLabelBounds> reservedLabels, string label, double x, double y, double yLower, double yUpper, double radius, double capWidth) {
        var placement = DataLabelPlacement(chart, series);
        if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
            var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
            var labelX = placement == ChartDataLabelPlacement.Left ? x - capWidth / 2 - 8 : x + capWidth / 2 + 8;
            if (ReserveSvgHorizontalLabel(label, labelX, y, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, y, anchor, plot, series, pointIndex);
            return;
        }

        var top = Math.Min(yLower, yUpper);
        var bottom = Math.Max(yLower, yUpper);
        var labelY = placement == ChartDataLabelPlacement.Below
            ? bottom + radius + 9
            : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                ? y
                : top - radius - 9;
        if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: pointIndex);
    }
}
