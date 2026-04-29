using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawCircleChart(StringBuilder sb, Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.Circle);
        if (series == null || series.Points.Count == 0) return;

        var t = chart.Options.Theme;
        var min = series.Points[0].X;
        var max = series.Points.Count > 1 ? series.Points[1].X : 100;
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var value = Clamp(series.Points[0].Y, min, max);
        var ratio = Clamp((value - min) / (max - min), 0, 1);
        var status = GaugeStatus(ratio);
        var statusColor = GaugeStatusColor(t, status);
        var color = series.Color ?? statusColor;
        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height * 0.54;
        var radius = Math.Max(24, Math.Min(plot.Width, plot.Height) * 0.28 * chart.Options.CircleRadiusScale);
        var stroke = Math.Max(5, radius * 0.16 * chart.Options.CircleStrokeScale);
        var start = -Math.PI / 2;
        var valueEnd = start + Math.PI * 2 * ratio;
        var valueLabel = FormatValue(chart, value);
        var statusLabel = status.Replace("-", " ");
        var labelWidth = Math.Max(60, Math.Min(plot.Width - 24, radius * 1.65));
        var summary = series.Name + ": " + valueLabel + ", " + statusLabel;
        var showLabels = series.ShowDataLabels != false;

        sb.AppendLine($"<g data-cfx-role=\"circle-chart\" data-cfx-status=\"{status}\" data-cfx-label=\"{Escape(series.Name)}\" data-cfx-value=\"{F(value)}\" data-cfx-min=\"{F(min)}\" data-cfx-max=\"{F(max)}\" data-cfx-percent=\"{F(ratio)}\" data-cfx-radius-scale=\"{F(chart.Options.CircleRadiusScale)}\" data-cfx-stroke-scale=\"{F(chart.Options.CircleStrokeScale)}\" role=\"img\" aria-label=\"{Escape(summary)}\">");
        sb.AppendLine($"<path data-cfx-role=\"circle-track\" d=\"{BuildRadialBarArc(cx, cy, radius, start, start + Math.PI * 2)}\" fill=\"none\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"{F(stroke)}\" stroke-linecap=\"round\" opacity=\"{F(ChartVisualPrimitives.CircleTrackOpacity)}\"/>");
        if (ratio > 0) {
            sb.AppendLine($"<path data-cfx-role=\"circle-value\" data-cfx-label=\"{Escape(series.Name)}\" data-cfx-value=\"{F(value)}\" data-cfx-ratio=\"{F(ratio)}\" data-cfx-percent=\"{F(ratio)}\" d=\"{BuildRadialBarArc(cx, cy, radius, start, valueEnd)}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(stroke)}\" stroke-linecap=\"round\"/>");
        }

        sb.AppendLine($"<circle data-cfx-role=\"circle-center\" cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(Math.Max(18, radius - stroke * 0.82))}\" fill=\"{t.CardBackground.ToCss()}\" fill-opacity=\"{F(ChartVisualPrimitives.CircleCenterFillOpacity)}\" stroke=\"{t.Grid.ToCss()}\" stroke-opacity=\"{F(ChartVisualPrimitives.CircleCenterStrokeOpacity)}\"/>");
        if (showLabels) {
            var valueFontSize = Math.Max(24, Math.Min(t.TitleFontSize * 1.72, radius * 0.72));
            var titleFontSize = Math.Max(9, Math.Min(t.LegendFontSize, radius * 0.22));
            var centerLineGap = Math.Max(4, Math.Min(8, radius * 0.05));
            var centerGroupHeight = valueFontSize + centerLineGap + titleFontSize;
            var valueY = cy - centerGroupHeight / 2.0 + valueFontSize / 2.0;
            var titleY = valueY + valueFontSize / 2.0 + centerLineGap + titleFontSize / 2.0;
            DrawSvgTextCenteredX(sb, chart, "circle-label", valueLabel, cx, valueY, t.Text, valueFontSize, labelWidth, "850", t.CardBackground, 3.2);
            DrawSvgTextCenteredX(sb, chart, "circle-title", series.Name, cx, titleY, t.MutedText, titleFontSize, labelWidth, "700", t.CardBackground, 2.4);
            if (chart.Options.ShowCircleStatusLabel) {
                var statusFontSize = TextFontSizeForSvgWidth(statusLabel, labelWidth, t.TickLabelFontSize);
                statusLabel = TrimSvgLabelToWidth(statusLabel, statusFontSize, labelWidth);
                var statusLeft = cx - EstimateTextWidth(statusLabel, statusFontSize) / 2.0;
                sb.AppendLine($"<circle data-cfx-role=\"circle-status-marker\" data-cfx-status=\"{status}\" cx=\"{F(statusLeft - 9)}\" cy=\"{F(cy + radius + 36)}\" r=\"{F(ChartVisualPrimitives.StatusMarkerRadius)}\" fill=\"{statusColor.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.StatusMarkerStrokeWidth)}\"/>");
                sb.AppendLine($"<text data-cfx-role=\"circle-status-label\" x=\"{F(statusLeft)}\" y=\"{F(cy + radius + 40)}\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(statusFontSize)}\" font-weight=\"650\">{Escape(statusLabel)}</text>");
            }
        }
        sb.AppendLine("</g>");
    }

    private static bool IsCircleChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Circle);
}
