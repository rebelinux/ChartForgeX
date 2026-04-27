using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawPieLike(RgbaCanvas c, Chart chart, ChartRect plot) {
        var series = chart.Series[0];
        var values = new List<ChartPoint>();
        foreach (var point in series.Points) if (point.Y > 0) values.Add(point);
        if (values.Count == 0) return;

        var total = 0d;
        foreach (var value in values) total += value.Y;
        var radius = Math.Max(1, Math.Min(plot.Width, plot.Height) * 0.38);
        var cx = plot.Left + plot.Width * 0.42;
        var cy = plot.Top + plot.Height / 2;
        var inner = series.Kind == ChartSeriesKind.Donut ? radius * 0.58 : 0;
        var start = -Math.PI / 2;
        var separator = chart.Options.Theme.CardBackground;

        for (var i = 0; i < values.Count; i++) {
            var sweep = values[i].Y / total * Math.PI * 2;
            var end = start + sweep;
            var color = chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length];
            c.FillRingSlice(cx, cy, radius, inner, start, end, color);
            DrawSliceSeparator(c, cx, cy, radius, inner, start, separator);
            if (chart.Options.ShowDataLabels && sweep > 0.22) {
                var mid = start + sweep / 2;
                var labelRadius = inner > 0 ? (inner + radius) / 2 : radius * 0.66;
                var label = FormatPercent(values[i].Y / total);
                var fontSize = chart.Options.Theme.DataLabelFontSize;
                DrawReadablePngLabel(c, cx + Math.Cos(mid) * labelRadius - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0, cy + Math.Sin(mid) * labelRadius - fontSize / 2.0, label, chart.Options.Theme.CardBackground, chart.Options.Theme.Text, fontSize);
            }

            start = end;
        }

        c.DrawCircleOutline(cx, cy, radius, separator, 2);
        if (inner > 0) c.DrawCircleOutline(cx, cy, inner, separator, 2);

        if (series.Kind == ChartSeriesKind.Donut) {
            var totalLabel = FormatValue(chart, total);
            const double totalFontSize = 24;
            var nameFontSize = chart.Options.Theme.TickLabelFontSize;
            var centerLabelWidth = Math.Max(24, inner * 1.55);
            DrawPngTextEmphasizedCenteredX(c, cx, cy - totalFontSize / 2.0 - 2, totalLabel, chart.Options.Theme.Text, totalFontSize, centerLabelWidth);
            DrawPngTextEmphasizedCenteredX(c, cx, cy + 19 - nameFontSize + 1, series.Name, chart.Options.Theme.MutedText, nameFontSize, centerLabelWidth);
        }

        if (chart.Options.ShowLegend) DrawSliceLegend(c, chart, values, plot, total);
    }

    private static void DrawSliceSeparator(RgbaCanvas c, double cx, double cy, double outerRadius, double innerRadius, double angle, ChartColor color) {
        var startRadius = Math.Max(0, innerRadius - 0.5);
        c.DrawLine(cx + Math.Cos(angle) * startRadius, cy + Math.Sin(angle) * startRadius, cx + Math.Cos(angle) * outerRadius, cy + Math.Sin(angle) * outerRadius, color, 2);
    }

    private static void DrawSliceLegend(RgbaCanvas c, Chart chart, IReadOnlyList<ChartPoint> values, ChartRect plot, double total) {
        var fontSize = PngLegendFontSize(chart);
        const double swatchSize = 10;
        var x = plot.Left + plot.Width * 0.72;
        var y = plot.Top + Math.Max(24, plot.Height * 0.18);
        for (var i = 0; i < values.Count; i++) {
            var color = chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length];
            var percent = FormatPercent(values[i].Y / total);
            var label = SliceLabel(chart, values[i], i);
            var labelMaxWidth = Math.Max(12, plot.Right - 36 - (x + swatchSize + 8) - EstimatePngTextWidth(percent, fontSize));
            var labelFontSize = TextFontSizeForEmphasizedWidth(label, labelMaxWidth, fontSize);
            label = TrimReadablePngLabelToWidth(label, labelFontSize, labelMaxWidth);
            c.FillRect(x, y - swatchSize + 1, swatchSize, swatchSize, color);
            if (label.Length > 0) c.DrawTextEmphasized(x + swatchSize + 8, y - labelFontSize + 3, label, chart.Options.Theme.Text, labelFontSize);
            c.DrawText(plot.Right - EstimatePngTextWidth(percent, fontSize) - 12, y - fontSize + 3, percent, chart.Options.Theme.MutedText, fontSize);
            y += fontSize + 10;
        }
    }

    private static string SliceLabel(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        }

        return "Slice " + (index + 1);
    }
}
