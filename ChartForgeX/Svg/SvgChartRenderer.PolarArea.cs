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
        var radius = Math.Max(24, Math.Min(plot.Width, plot.Height) * 0.36);
        var cx = plot.Left + plot.Width * 0.42;
        var cy = plot.Top + plot.Height / 2;
        var sweep = Math.PI * 2 / values.Length;

        sb.AppendLine("<g data-cfx-role=\"polar-area-chart\">");
        DrawPolarAreaGrid(sb, chart, cx, cy, radius);
        for (var i = 0; i < values.Length; i++) {
            var start = -Math.PI / 2 + i * sweep;
            var end = start + sweep;
            var segmentRadius = radius * Math.Sqrt(values[i].Y / max);
            var color = t.Palette[i % t.Palette.Length];
            var label = SliceLabel(chart, values[i], i);
            var summary = label + ": " + FormatValue(chart, values[i].Y) + ", " + FormatPercent(values[i].Y / total);
            sb.AppendLine($"<path data-cfx-role=\"polar-area-segment\" data-cfx-point=\"{i}\" role=\"img\" aria-label=\"{Escape(summary)}\" d=\"{BuildSlicePath(cx, cy, segmentRadius, 0, start, end)}\" fill=\"url(#{id}-sliceFill{i % t.Palette.Length})\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"2\"/>");

            if (chart.Options.ShowDataLabels && segmentRadius > 34) {
                var mid = start + sweep / 2;
                var labelRadius = segmentRadius * 0.64;
                DrawDataLabel(sb, chart, FormatValue(chart, values[i].Y), cx + Math.Cos(mid) * labelRadius, cy + Math.Sin(mid) * labelRadius, plot, "polar-area-label");
            }
        }

        if (chart.Options.ShowLegend) DrawSliceLegend(sb, chart, values, plot, total);
        sb.AppendLine("</g>");
    }

    private static void DrawPolarAreaGrid(StringBuilder sb, Chart chart, double cx, double cy, double radius) {
        var t = chart.Options.Theme;
        for (var i = 1; i <= 4; i++) {
            var r = radius * i / 4.0;
            sb.AppendLine($"<circle data-cfx-role=\"polar-area-ring\" cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"none\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"1\" opacity=\"0.72\"/>");
        }
    }
}
