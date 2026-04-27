using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawCircleChart(RgbaCanvas c, Chart chart, ChartRect plot) {
        ChartSeries? circle = null;
        foreach (var series in chart.Series) {
            if (series.Kind == ChartSeriesKind.Circle) {
                circle = series;
                break;
            }
        }

        if (circle == null || circle.Points.Count == 0) return;
        var min = circle.Points[0].X;
        var max = circle.Points.Count > 1 ? circle.Points[1].X : 100;
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var value = Clamp(circle.Points[0].Y, min, max);
        var ratio = Clamp((value - min) / (max - min), 0, 1);
        var theme = chart.Options.Theme;
        var status = GaugeStatus(ratio);
        var statusColor = GaugeStatusColor(chart, status);
        var color = circle.Color ?? statusColor;
        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height * 0.54;
        var radius = Math.Max(50, Math.Min(plot.Width, plot.Height) * 0.30);
        var stroke = Math.Max(15, (int)Math.Round(radius * 0.18));
        var start = -Math.PI / 2;
        c.DrawArc(cx, cy, radius, start, start + Math.PI * 2, ApplyOpacity(theme.Grid, 0.50), stroke);
        if (ratio > 0) {
            var end = start + Math.PI * 2 * ratio;
            c.DrawArc(cx, cy, radius, start, end, color, stroke);
            c.DrawCircle(cx + Math.Cos(end) * radius, cy + Math.Sin(end) * radius, Math.Max(2, stroke / 2.0), color);
        }

        var centerRadius = Math.Max(24, radius - stroke * 0.82);
        c.DrawCircle(cx, cy, centerRadius, ApplyOpacity(theme.CardBackground, 0.88));
        c.DrawCircleOutline(cx, cy, centerRadius, ApplyOpacity(theme.Grid, 0.18), 1);
        var valueLabel = FormatValue(chart, value);
        var labelWidth = Math.Max(60, Math.Min(plot.Width - 24, radius * 1.65));
        var valueFontSize = Math.Max(34, theme.TitleFontSize * 1.72);
        var titleFontSize = Math.Max(10, theme.LegendFontSize);
        DrawPngTextEmphasizedCenteredX(c, cx, cy - theme.TitleFontSize * 0.18 - valueFontSize / 2.0, valueLabel, theme.Text, valueFontSize, labelWidth);
        DrawPngTextEmphasizedCenteredX(c, cx, cy + theme.LegendFontSize + 26 - titleFontSize + 1, circle.Name, theme.MutedText, titleFontSize, labelWidth);
        var statusLabel = status.Replace("-", " ");
        var statusFontSize = TextFontSizeForEmphasizedWidth(statusLabel, labelWidth, theme.TickLabelFontSize);
        statusLabel = TrimReadablePngLabelToWidth(statusLabel, statusFontSize, labelWidth);
        var statusLeft = cx - EstimatePngEmphasizedTextWidth(statusLabel, statusFontSize) / 2.0;
        c.DrawCircle(statusLeft - 9, cy + radius + 36, 5.2, theme.CardBackground);
        c.DrawCircle(statusLeft - 9, cy + radius + 36, 2.5, statusColor);
        c.DrawTextEmphasized(statusLeft, cy + radius + 40 - statusFontSize + 1, statusLabel, theme.MutedText, statusFontSize);
    }

    private static bool IsCircleChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.Circle) return true;
        return false;
    }
}
