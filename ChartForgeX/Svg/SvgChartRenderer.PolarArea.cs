using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawPolarArea(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var series = chart.Series[0];
        var values = series.Points
            .Select((point, index) => new IndexedPieValue(point, index))
            .Where(item => item.Point.Y > 0)
            .ToArray();
        if (values.Length == 0) return;
        var legendValues = series.Points
            .Select((point, index) => new IndexedPieValue(point, index))
            .ToArray();

        var t = chart.Options.Theme;
        var max = values.Max(item => item.Point.Y);
        var total = values.Sum(item => item.Point.Y);
        var chartPlot = PieChartPlot(chart, plot, legendValues);
        var radius = Math.Max(ChartVisualPrimitives.PolarAreaMinRadius, Math.Min(chartPlot.Width, chartPlot.Height) * ChartVisualPrimitives.PolarAreaRadiusFactor);
        var cx = chartPlot.Left + chartPlot.Width * ChartVisualPrimitives.PolarAreaCenterXFactor;
        var cy = chartPlot.Top + chartPlot.Height / 2;
        var sweep = Math.PI * 2 / series.Points.Count;

        sb.AppendLine("<g data-cfx-role=\"polar-area-chart\">");
        if (chart.Options.ShowGrid) DrawPolarAreaGrid(sb, chart, cx, cy, radius);
        DrawPolarAreaZeroSlots(sb, chart, series, cx, cy, radius, sweep);
        for (var i = 0; i < values.Length; i++) {
            var point = values[i].Point;
            var pointIndex = values[i].PointIndex;
            var start = -Math.PI / 2 + pointIndex * sweep;
            var end = start + sweep;
            var segmentRadius = radius * Math.Sqrt(point.Y / max);
            var label = SliceLabel(chart, point, pointIndex);
            var percent = point.Y / total;
            var summary = label + ": " + FormatValue(chart, point.Y) + ", " + FormatPercent(percent);
            sb.AppendLine($"<path data-cfx-role=\"polar-area-segment\" data-cfx-point=\"{pointIndex}\" data-cfx-label=\"{Escape(label)}\" data-cfx-value=\"{F(point.Y)}\" data-cfx-percent=\"{F(percent)}\" role=\"img\" aria-label=\"{Escape(summary)}\" d=\"{BuildSlicePath(cx, cy, segmentRadius, 0, start, end)}\" fill=\"{PieSliceFill(chart, series, pointIndex, id)}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.SliceSeparatorStrokeWidth)}\"/>");

            if (ShouldDrawDataLabels(chart, series) && segmentRadius > ChartVisualPrimitives.PolarAreaLabelMinRadius) {
                var mid = start + sweep / 2;
                var labelRadius = segmentRadius * ChartVisualPrimitives.PolarAreaLabelRadiusFactor;
                DrawDataLabel(sb, chart, FormatValue(chart, point.Y), cx + Math.Cos(mid) * labelRadius, cy + Math.Sin(mid) * labelRadius, plot, "polar-area-label", series, pointIndex);
            }
        }

        if (chart.Options.ShowLegend) DrawSliceLegend(sb, chart, series, legendValues, plot, total);
        sb.AppendLine("</g>");
    }

    private static void DrawPolarAreaZeroSlots(StringBuilder sb, Chart chart, ChartSeries series, double cx, double cy, double radius, double sweep) {
        var t = chart.Options.Theme;
        for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
            var point = series.Points[pointIndex];
            if (point.Y > 0) continue;
            var start = -Math.PI / 2 + pointIndex * sweep;
            var end = start + sweep;
            var label = SliceLabel(chart, point, pointIndex);
            var summary = label + ": " + FormatValue(chart, point.Y) + ", 0%";
            var color = PieSliceColor(chart, series, pointIndex);
            var inner = radius * ChartVisualPrimitives.PolarAreaZeroSlotInnerRadiusFactor;
            sb.AppendLine($"<path data-cfx-role=\"polar-area-zero-slot\" data-cfx-point=\"{pointIndex}\" data-cfx-label=\"{Escape(label)}\" data-cfx-value=\"{F(point.Y)}\" data-cfx-percent=\"0\" data-cfx-inner-radius-factor=\"{F(ChartVisualPrimitives.PolarAreaZeroSlotInnerRadiusFactor)}\" role=\"img\" aria-label=\"{Escape(summary)}\" d=\"{BuildSlicePath(cx, cy, radius, inner, start, end)}\" fill=\"{color.ToCss()}\" fill-opacity=\"{F(ChartVisualPrimitives.PolarAreaZeroSlotFillOpacity)}\" stroke=\"{color.ToCss()}\" stroke-opacity=\"{F(ChartVisualPrimitives.PolarAreaZeroSlotStrokeOpacity)}\" stroke-width=\"{F(ChartVisualPrimitives.GridStrokeWidth)}\" stroke-dasharray=\"3 4\"/>");
        }
    }

    private static void DrawPolarAreaGrid(StringBuilder sb, Chart chart, double cx, double cy, double radius) {
        var t = chart.Options.Theme;
        for (var i = 1; i <= ChartVisualPrimitives.PolarAreaGridRings; i++) {
            var r = radius * i / ChartVisualPrimitives.PolarAreaGridRings;
            sb.AppendLine($"<circle data-cfx-role=\"polar-area-ring\" cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"none\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.GridStrokeWidth)}\" opacity=\"{F(ChartVisualPrimitives.PolarAreaGridOpacity)}\"/>");
        }
    }
}
