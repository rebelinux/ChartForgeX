using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawRadialBar(RgbaCanvas c, Chart chart, ChartRect plot) {
        ChartSeries? series = null;
        foreach (var candidate in chart.Series) {
            if (candidate.Kind != ChartSeriesKind.RadialBar) continue;
            series = candidate;
            break;
        }

        if (series == null || series.Points.Count == 0) return;

        var count = series.Points.Count;
        var theme = chart.Options.Theme;
        var cx = plot.Left + plot.Width * (chart.Options.ShowLegend ? 0.40 : 0.50);
        var cy = plot.Top + plot.Height * 0.50;
        var outerRadius = Math.Max(36, Math.Min(plot.Width * (chart.Options.ShowLegend ? 0.30 : 0.36), plot.Height * 0.38));
        var gap = Math.Max(5, outerRadius * 0.035);
        var stroke = Math.Max(8, Math.Min(18, (outerRadius - 18) / Math.Max(1, count) - gap));
        if (stroke * count + gap * Math.Max(0, count - 1) > outerRadius - 12) stroke = Math.Max(6, (outerRadius - 12 - gap * Math.Max(0, count - 1)) / Math.Max(1, count));

        var start = -Math.PI / 2;
        var average = 0.0;
        foreach (var point in series.Points) average += point.Y;
        average /= count;

        for (var i = 0; i < count; i++) {
            var point = series.Points[i];
            var ratio = Clamp(point.Y / 100.0, 0, 1);
            var radius = outerRadius - i * (stroke + gap) - stroke / 2;
            if (radius <= stroke / 2) continue;
            var color = series.Color ?? theme.Palette[i % theme.Palette.Length];
            c.DrawArc(cx, cy, radius, start, start + Math.PI * 2, ApplyOpacity(theme.Grid, ChartVisualPrimitives.RadialTrackOpacity), Math.Max(1, stroke));
            if (ratio <= 0) continue;
            var end = start + Math.PI * 2 * ratio;
            c.DrawArc(cx, cy, radius, start, end, color, Math.Max(1, stroke));
        }

        var centerLabel = FormatValue(chart, average);
        var labelWidth = Math.Max(54, Math.Min(plot.Width * 0.32, outerRadius * 1.25));
        var centerDiskRadius = Math.Max(26, outerRadius - count * (stroke + gap) - 2);
        var valueFontSize = Math.Max(26, theme.TitleFontSize * 1.32);
        var nameFontSize = Math.Max(9, theme.LegendFontSize - 1);
        c.DrawCircle(cx, cy, centerDiskRadius, ApplyOpacity(theme.CardBackground, ChartVisualPrimitives.RadialCenterFillOpacity));
        c.DrawCircleOutline(cx, cy, centerDiskRadius, ApplyOpacity(theme.Grid, ChartVisualPrimitives.RadialCenterStrokeOpacity), 1);
        if (series.ShowDataLabels != false) {
            DrawPngTextEmphasizedCenteredX(c, cx, cy - theme.TitleFontSize * 0.42 - valueFontSize / 2.0, centerLabel, theme.Text, valueFontSize, labelWidth);
            DrawPngTextEmphasizedCenteredX(c, cx, cy + theme.LegendFontSize + 14 - nameFontSize + 1, series.Name, theme.MutedText, nameFontSize, labelWidth);
        }
        if (chart.Options.ShowLegend) DrawRadialBarLegend(c, chart, plot, series);
    }

    private static void DrawRadialBarLegend(RgbaCanvas c, Chart chart, ChartRect plot, ChartSeries series) {
        var fontSize = PngLegendFontSize(chart);
        var x = plot.Left + plot.Width * 0.72;
        var y = plot.Top + Math.Max(26, plot.Height * 0.22);
        var valueX = plot.Right - 14;
        for (var i = 0; i < series.Points.Count; i++) {
            var point = series.Points[i];
            var color = series.Color ?? chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length];
            var label = SliceLabel(chart, point, i);
            var value = FormatValue(chart, point.Y);
            var maxLabelWidth = Math.Max(24, valueX - x - 34 - EstimatePngEmphasizedTextWidth(value, fontSize));
            label = TrimReadablePngLabelToWidth(label, fontSize, maxLabelWidth);
            c.DrawCircle(x, y - 4, ChartVisualPrimitives.RadialLegendMarkerRadius, color);
            if (label.Length > 0) c.DrawTextEmphasized(x + 13, y - fontSize + 2, label, chart.Options.Theme.Text, fontSize);
            c.DrawTextEmphasized(valueX - EstimatePngEmphasizedTextWidth(value, fontSize), y - fontSize + 2, value, chart.Options.Theme.MutedText, fontSize);
            y += fontSize + 10;
        }
    }

    private static bool IsRadialBarChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.RadialBar) return true;
        return false;
    }
}
