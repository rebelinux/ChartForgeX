using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawRadialBar(StringBuilder sb, Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.RadialBar);
        if (series == null || series.Points.Count == 0) return;

        var t = chart.Options.Theme;
        var count = series.Points.Count;
        var cx = plot.Left + plot.Width * (chart.Options.ShowLegend ? 0.40 : 0.50);
        var cy = plot.Top + plot.Height * 0.50;
        var outerRadius = Math.Max(36, Math.Min(plot.Width * (chart.Options.ShowLegend ? 0.30 : 0.36), plot.Height * 0.38));
        var gap = Math.Max(5, outerRadius * 0.035);
        var stroke = Math.Max(8, Math.Min(18, (outerRadius - 18) / Math.Max(1, count) - gap));
        if (stroke * count + gap * Math.Max(0, count - 1) > outerRadius - 12) {
            stroke = Math.Max(6, (outerRadius - 12 - gap * Math.Max(0, count - 1)) / Math.Max(1, count));
        }

        var start = -Math.PI / 2;
        var average = series.Points.Average(point => point.Y);
        var centerLabel = FormatValue(chart, average);
        var labelWidth = Math.Max(54, Math.Min(plot.Width * 0.32, outerRadius * 1.25));
        var centerDiskRadius = Math.Max(26, outerRadius - count * (stroke + gap) - 2);

        sb.AppendLine("<g data-cfx-role=\"radial-bar-chart\">");
        for (var i = 0; i < count; i++) {
            var point = series.Points[i];
            var ratio = Clamp(point.Y / 100.0, 0, 1);
            var radius = outerRadius - i * (stroke + gap) - stroke / 2;
            if (radius <= stroke / 2) continue;
            var color = series.Color ?? t.Palette[i % t.Palette.Length];
            var label = SliceLabel(chart, point, i);
            var valueLabel = FormatValue(chart, point.Y);
            var summary = label + ": " + valueLabel;
            var end = start + Math.PI * 2 * ratio;

            sb.AppendLine($"<path data-cfx-role=\"radial-bar-track\" data-cfx-point=\"{i}\" d=\"{BuildRadialBarArc(cx, cy, radius, start, start + Math.PI * 2)}\" fill=\"none\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"{F(stroke)}\" stroke-linecap=\"round\" opacity=\"0.44\"/>");
            if (ratio > 0) {
                sb.AppendLine($"<path data-cfx-role=\"radial-bar-ring\" data-cfx-point=\"{i}\" data-cfx-value=\"{F(point.Y)}\" role=\"img\" aria-label=\"{Escape(summary)}\" d=\"{BuildRadialBarArc(cx, cy, radius, start, end)}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(stroke)}\" stroke-linecap=\"round\"/>");
            }
        }

        sb.AppendLine($"<circle data-cfx-role=\"radial-bar-center\" cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(centerDiskRadius)}\" fill=\"{t.CardBackground.ToCss()}\" fill-opacity=\"0.86\" stroke=\"{t.Grid.ToCss()}\" stroke-opacity=\"0.20\"/>");
        DrawSvgTextCenteredX(sb, chart, "radial-bar-total", centerLabel, cx, cy - t.TitleFontSize * 0.42, t.Text, Math.Max(26, t.TitleFontSize * 1.32), labelWidth, "850", t.CardBackground, 3.2);
        DrawSvgTextCenteredX(sb, chart, "radial-bar-title", series.Name, cx, cy + t.LegendFontSize + 14, t.MutedText, Math.Max(9, t.LegendFontSize - 1), labelWidth, "700", t.CardBackground, 2.4, middleBaseline: false);
        if (chart.Options.ShowLegend) DrawRadialBarLegend(sb, chart, plot, series);
        sb.AppendLine("</g>");
    }

    private static void DrawRadialBarLegend(StringBuilder sb, Chart chart, ChartRect plot, ChartSeries series) {
        var t = chart.Options.Theme;
        var fontSize = t.LegendFontSize;
        var x = plot.Left + plot.Width * 0.72;
        var y = plot.Top + Math.Max(26, plot.Height * 0.22);
        var valueX = plot.Right - 14;
        for (var i = 0; i < series.Points.Count; i++) {
            var point = series.Points[i];
            var color = series.Color ?? t.Palette[i % t.Palette.Length];
            var label = SliceLabel(chart, point, i);
            var value = FormatValue(chart, point.Y);
            var maxLabelWidth = Math.Max(24, valueX - x - 34 - EstimateTextWidth(value, fontSize));
            label = TrimSvgLabelToWidth(label, fontSize, maxLabelWidth);
            sb.AppendLine($"<circle data-cfx-role=\"radial-bar-legend-marker\" data-cfx-point=\"{i}\" cx=\"{F(x)}\" cy=\"{F(y - 4)}\" r=\"4.5\" fill=\"{color.ToCss()}\"/>");
            sb.AppendLine($"<text data-cfx-role=\"radial-bar-legend-label\" data-cfx-point=\"{i}\" x=\"{F(x + 13)}\" y=\"{F(y)}\" fill=\"{t.Text.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(fontSize)}\" font-weight=\"700\">{Escape(label)}</text>");
            sb.AppendLine($"<text data-cfx-role=\"radial-bar-legend-value\" data-cfx-point=\"{i}\" x=\"{F(valueX)}\" y=\"{F(y)}\" text-anchor=\"end\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(fontSize)}\" font-weight=\"700\">{Escape(value)}</text>");
            y += fontSize + 10;
        }
    }

    private static string BuildRadialBarArc(double cx, double cy, double radius, double start, double end) {
        if (end - start >= Math.PI * 2 - 0.000001) {
            var mid = start + Math.PI;
            var x1 = cx + Math.Cos(start) * radius;
            var y1 = cy + Math.Sin(start) * radius;
            var xm = cx + Math.Cos(mid) * radius;
            var ym = cy + Math.Sin(mid) * radius;
            var x2 = cx + Math.Cos(start + Math.PI * 2) * radius;
            var y2 = cy + Math.Sin(start + Math.PI * 2) * radius;
            return $"M {F(x1)} {F(y1)} A {F(radius)} {F(radius)} 0 0 1 {F(xm)} {F(ym)} A {F(radius)} {F(radius)} 0 0 1 {F(x2)} {F(y2)}";
        }

        var largeArc = end - start > Math.PI ? 1 : 0;
        return BuildRadialBarArcSegment(cx, cy, radius, start, end, largeArc);
    }

    private static string BuildRadialBarArcSegment(double cx, double cy, double radius, double start, double end, int largeArc) {
        var x1 = cx + Math.Cos(start) * radius;
        var y1 = cy + Math.Sin(start) * radius;
        var x2 = cx + Math.Cos(end) * radius;
        var y2 = cy + Math.Sin(end) * radius;
        return $"M {F(x1)} {F(y1)} A {F(radius)} {F(radius)} 0 {largeArc} 1 {F(x2)} {F(y2)}";
    }
}
