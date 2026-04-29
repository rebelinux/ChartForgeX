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
        var values = series.Points.Where(point => point.Y > 0).ToArray();
        if (values.Length == 0) return;

        var t = chart.Options.Theme;
        var max = values.Max(point => point.Y);
        var total = values.Sum(point => point.Y);
        var radius = Math.Max(ChartVisualPrimitives.PolarAreaMinRadius, Math.Min(plot.Width, plot.Height) * ChartVisualPrimitives.PolarAreaRadiusFactor);
        var cx = plot.Left + plot.Width * ChartVisualPrimitives.PolarAreaCenterXFactor;
        var cy = plot.Top + plot.Height / 2;
        var sweep = Math.PI * 2 / values.Length;

        sb.AppendLine("<g data-cfx-role=\"polar-area-chart\">");
        if (chart.Options.ShowGrid) DrawPolarAreaGrid(sb, chart, cx, cy, radius);
        for (var i = 0; i < values.Length; i++) {
            var start = -Math.PI / 2 + i * sweep;
            var end = start + sweep;
            var segmentRadius = radius * Math.Sqrt(values[i].Y / max);
            var label = SliceLabel(chart, values[i], i);
            var percent = values[i].Y / total;
            var summary = label + ": " + FormatValue(chart, values[i].Y) + ", " + FormatPercent(percent);
            sb.AppendLine($"<path data-cfx-role=\"polar-area-segment\" data-cfx-point=\"{i}\" data-cfx-label=\"{Escape(label)}\" data-cfx-value=\"{F(values[i].Y)}\" data-cfx-percent=\"{F(percent)}\" role=\"img\" aria-label=\"{Escape(summary)}\" d=\"{BuildSlicePath(cx, cy, segmentRadius, 0, start, end)}\" fill=\"{PieSliceFill(chart, series, i, id)}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.SliceSeparatorStrokeWidth)}\"/>");

            if (ShouldDrawDataLabels(chart, series) && segmentRadius > ChartVisualPrimitives.PolarAreaLabelMinRadius) {
                var mid = start + sweep / 2;
                var labelRadius = segmentRadius * ChartVisualPrimitives.PolarAreaLabelRadiusFactor;
                DrawDataLabel(sb, chart, FormatValue(chart, values[i].Y), cx + Math.Cos(mid) * labelRadius, cy + Math.Sin(mid) * labelRadius, plot, "polar-area-label");
            }
        }

        if (chart.Options.ShowLegend) DrawSliceLegend(sb, chart, series, values, plot, total);
        sb.AppendLine("</g>");
    }

    private static void DrawPolarAreaGrid(StringBuilder sb, Chart chart, double cx, double cy, double radius) {
        var t = chart.Options.Theme;
        for (var i = 1; i <= ChartVisualPrimitives.PolarAreaGridRings; i++) {
            var r = radius * i / ChartVisualPrimitives.PolarAreaGridRings;
            sb.AppendLine($"<circle data-cfx-role=\"polar-area-ring\" cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"none\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.GridStrokeWidth)}\" opacity=\"{F(ChartVisualPrimitives.PolarAreaGridOpacity)}\"/>");
        }
    }
}
