using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawProgressBar(StringBuilder sb, Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.ProgressBar);
        if (series == null || series.Points.Count == 0) return;
        var t = chart.Options.Theme;
        var maximum = chart.Options.ProgressMaximum;
        var values = series.Points.ToArray();
        var showValues = chart.Options.ShowProgressValues;
        var showHandles = chart.Options.ShowProgressHandles;
        var rowHeight = Math.Max(30, Math.Min(58, plot.Height / Math.Max(1, values.Length)));
        var labelWidth = Math.Min(150, Math.Max(80, plot.Width * 0.24));
        var valueWidth = showValues ? Math.Min(78, Math.Max(48, plot.Width * 0.13)) : 0;
        var barArea = Math.Max(1, plot.Width - labelWidth - valueWidth - (showValues ? 28 : 14));
        var barHeight = Math.Max(6, Math.Min(30, rowHeight * chart.Options.ProgressBarThicknessRatio));
        var startX = plot.Left + labelWidth + 12;
        var startY = plot.Top + Math.Max(0, (plot.Height - rowHeight * values.Length) / 2);
        sb.AppendLine($"<g data-cfx-role=\"progress-bar-chart\" data-cfx-maximum=\"{F(maximum)}\" data-cfx-show-values=\"{(showValues ? "true" : "false")}\" data-cfx-show-handles=\"{(showHandles ? "true" : "false")}\" data-cfx-bar-thickness-ratio=\"{F(chart.Options.ProgressBarThicknessRatio)}\" data-cfx-track-opacity=\"{F(chart.Options.ProgressTrackOpacity)}\">");
        for (var i = 0; i < values.Length; i++) {
            var point = values[i];
            var y = startY + i * rowHeight + rowHeight / 2;
            var labelMaxWidth = Math.Max(8, labelWidth - 8);
            var rawLabel = FormatX(chart, point.X);
            var labelFontSize = TextFontSizeForSvgWidth(rawLabel, labelMaxWidth, t.TickLabelFontSize);
            var label = TrimSvgLabelToWidth(rawLabel, labelFontSize, labelMaxWidth);
            var color = ProgressItemColor(series, t, i);
            var ratio = Clamp(point.Y / maximum, 0, 1);
            var filledWidth = barArea * ratio;
            if (label.Length > 0) {
                sb.AppendLine($"<text data-cfx-role=\"progress-label\" data-cfx-point=\"{i}\" x=\"{F(plot.Left + labelWidth - 8)}\" y=\"{F(y + labelFontSize / 3.0)}\" text-anchor=\"end\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(labelFontSize)}\" font-weight=\"700\">{Escape(label)}</text>");
            }
            sb.AppendLine($"<rect data-cfx-role=\"progress-track\" data-cfx-point=\"{i}\" x=\"{F(startX)}\" y=\"{F(y - barHeight / 2)}\" width=\"{F(barArea)}\" height=\"{F(barHeight)}\" rx=\"{F(barHeight / 2)}\" fill=\"{PictorialOpacity(t.Grid, chart.Options.ProgressTrackOpacity).ToCss()}\"/>");
            sb.AppendLine($"<rect data-cfx-role=\"progress-fill\" data-cfx-point=\"{i}\" data-cfx-value=\"{F(point.Y)}\" data-cfx-ratio=\"{F(ratio)}\" x=\"{F(startX)}\" y=\"{F(y - barHeight / 2)}\" width=\"{F(filledWidth)}\" height=\"{F(barHeight)}\" rx=\"{F(barHeight / 2)}\" fill=\"{color.ToCss()}\"/>");
            if (showHandles) {
                sb.AppendLine($"<circle data-cfx-role=\"progress-handle\" data-cfx-point=\"{i}\" cx=\"{F(startX + filledWidth)}\" cy=\"{F(y)}\" r=\"{F(Math.Max(5, barHeight * 0.62))}\" fill=\"{t.CardBackground.ToCss()}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(Math.Max(2, barHeight * 0.18))}\"/>");
            }
            if (showValues) {
                var valueMaxWidth = Math.Max(8, valueWidth - 4);
                var rawValue = FormatValue(chart, point.Y);
                var valueFontSize = TextFontSizeForSvgWidth(rawValue, valueMaxWidth, t.DataLabelFontSize);
                var value = TrimSvgLabelToWidth(rawValue, valueFontSize, valueMaxWidth);
                if (value.Length > 0) {
                    sb.AppendLine($"<text data-cfx-role=\"progress-value\" data-cfx-point=\"{i}\" x=\"{F(startX + barArea + 12)}\" y=\"{F(y + valueFontSize / 3.0)}\" fill=\"{t.Text.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(valueFontSize)}\" font-weight=\"800\">{Escape(value)}</text>");
                }
            }
        }

        sb.AppendLine("</g>");
    }

    private static ChartColor ProgressItemColor(ChartSeries series, ChartTheme theme, int index) {
        if (index < series.PointColors.Count && series.PointColors[index].HasValue) return series.PointColors[index]!.Value;
        return series.Color ?? theme.Palette[index % theme.Palette.Length];
    }
}
