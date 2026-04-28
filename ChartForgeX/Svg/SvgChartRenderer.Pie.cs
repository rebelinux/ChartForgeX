using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawPieLike(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var series = chart.Series[0];
        var values = series.Points.Where(p => p.Y > 0).ToArray();
        if (values.Length == 0) return;

        var t = chart.Options.Theme;
        var total = values.Sum(p => p.Y);
        var radius = Math.Max(1, Math.Min(plot.Width, plot.Height) * 0.38);
        var cx = plot.Left + plot.Width * 0.42;
        var cy = plot.Top + plot.Height / 2;
        var inner = series.Kind == ChartSeriesKind.Donut ? radius * 0.58 : 0;
        var start = -Math.PI / 2;
        var sliceRole = series.Kind == ChartSeriesKind.Donut ? "donut-slice" : "pie-slice";

        for (var i = 0; i < values.Length; i++) {
            var sweep = values[i].Y / total * Math.PI * 2;
            var end = start + sweep;
            var label = SliceLabel(chart, values[i], i);
            var percent = values[i].Y / total;
            sb.AppendLine($"<path data-cfx-role=\"{sliceRole}\" data-cfx-point=\"{i}\" data-cfx-label=\"{Escape(label)}\" data-cfx-value=\"{F(values[i].Y)}\" data-cfx-percent=\"{F(percent)}\" d=\"{BuildSlicePath(cx, cy, radius, inner, start, end)}\" fill=\"url(#{id}-sliceFill{i % t.Palette.Length})\" fill-rule=\"evenodd\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.SliceSeparatorStrokeWidth)}\"/>");

            if (ShouldDrawDataLabels(chart, series) && sweep > 0.22) {
                var mid = start + sweep / 2;
                var labelRadius = inner > 0 ? (inner + radius) / 2 : radius * 0.66;
                var x = cx + Math.Cos(mid) * labelRadius;
                var y = cy + Math.Sin(mid) * labelRadius + 4;
                sb.AppendLine($"<text data-cfx-role=\"data-label\" x=\"{F(x)}\" y=\"{F(y)}\" text-anchor=\"middle\" fill=\"{t.CardBackground.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.DataLabelFontSize)}\" font-weight=\"750\">{FormatPercent(percent)}</text>");
            }

            start = end;
        }

        if (series.Kind == ChartSeriesKind.Donut && series.ShowDataLabels != false) {
            var centerLabelWidth = Math.Max(24, inner * 1.55);
            DrawSvgTextCenteredX(sb, chart, "donut-total-label", FormatValue(chart, total), cx, cy - 2, t.Text, 24, centerLabelWidth, "800");
            DrawSvgTextCenteredX(sb, chart, "donut-title", series.Name, cx, cy + 19, t.MutedText, t.TickLabelFontSize, centerLabelWidth, "400");
        }

        if (chart.Options.ShowLegend) DrawSliceLegend(sb, chart, values, plot, total);
    }

    private static void DrawSliceLegend(StringBuilder sb, Chart chart, IReadOnlyList<ChartPoint> values, ChartRect plot, double total) {
        var t = chart.Options.Theme;
        var x = plot.Left + plot.Width * 0.72;
        var y = plot.Top + Math.Max(24, plot.Height * 0.18);
        for (var i = 0; i < values.Count; i++) {
            var color = chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length];
            var label = SliceLabel(chart, values[i], i);
            var percent = FormatPercent(values[i].Y / total);
            var maxLabelWidth = Math.Max(12, plot.Right - 36 - (x + 16) - EstimateTextWidth(percent, t.LegendFontSize));
            label = TrimSvgLabelToWidth(label, t.LegendFontSize, maxLabelWidth);
            sb.AppendLine($"<rect x=\"{F(x)}\" y=\"{F(y - ChartVisualPrimitives.SliceLegendSwatchSize + 1)}\" width=\"{F(ChartVisualPrimitives.SliceLegendSwatchSize)}\" height=\"{F(ChartVisualPrimitives.SliceLegendSwatchSize)}\" rx=\"{F(ChartVisualPrimitives.SliceLegendSwatchRadius)}\" fill=\"{color.ToCss()}\"/>");
            sb.AppendLine($"<text x=\"{F(x + ChartVisualPrimitives.SliceLegendSwatchSize + 6)}\" y=\"{F(y)}\" fill=\"{t.Text.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.LegendFontSize)}\" font-weight=\"650\">{Escape(label)}</text>");
            sb.AppendLine($"<text x=\"{F(plot.Right - 12)}\" y=\"{F(y)}\" text-anchor=\"end\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.LegendFontSize)}\">{percent}</text>");
            y += 22;
        }
    }
}
