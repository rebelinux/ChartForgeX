using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawFunnel(StringBuilder sb, Chart chart, ChartRect basePlot, string id) {
        ChartSeries? series = null;
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i].Kind != ChartSeriesKind.Funnel) continue;
            series = chart.Series[i];
            break;
        }
        if (series == null) return;
        var values = series.Points.Where(point => point.Y > 0).ToArray();
        if (values.Length == 0) return;

        var t = chart.Options.Theme;
        var max = values.Max(point => point.Y);
        var showLabels = series.ShowDataLabels != false;
        var metricsReserve = showLabels && values.Length > 1 ? Math.Min(168, Math.Max(126, basePlot.Width * 0.22)) : 0;
        var innerLeft = basePlot.Left + 44;
        var innerRight = basePlot.Right - 44 - metricsReserve;
        var plot = new ChartRect(innerLeft, basePlot.Top + 18, Math.Max(120, innerRight - innerLeft), Math.Max(90, basePlot.Height - 62));
        var metricsX = Math.Min(basePlot.Right - metricsReserve + 10, plot.Right + 18);
        var gap = Math.Min(10, Math.Max(4, plot.Height / values.Length * 0.08));
        var segmentHeight = Math.Max(18, (plot.Height - gap * (values.Length - 1)) / values.Length);
        sb.AppendLine("<g data-cfx-role=\"funnel-chart\">");
        DrawFunnelPointGradients(sb, id, series);

        for (var i = 0; i < values.Length; i++) {
            var y = plot.Top + i * (segmentHeight + gap);
            var topWidth = FunnelWidth(plot.Width, values[i].Y, max);
            var nextValue = i + 1 < values.Length ? values[i + 1].Y : values[i].Y * 0.82;
            var bottomWidth = FunnelWidth(plot.Width, nextValue, max);
            var topLeft = plot.Left + (plot.Width - topWidth) / 2;
            var topRight = topLeft + topWidth;
            var bottomLeft = plot.Left + (plot.Width - bottomWidth) / 2;
            var bottomRight = bottomLeft + bottomWidth;
            var color = FunnelSegmentColor(chart, series, i);
            var fill = FunnelSegmentFill(chart, series, i, id);
            var label = FormatX(chart, values[i].X);
            var value = FormatValue(chart, values[i].Y);
            var retention = values[i].Y / values[0].Y;
            var previousRetention = i == 0 ? 1 : values[i].Y / values[i - 1].Y;
            var dropOff = i == 0 ? 0 : 1 - previousRetention;
            var summary = label + ": " + value + ", retained " + FormatPercent(retention);
            if (i > 0) summary += ", drop-off " + FormatPercent(dropOff);
            sb.AppendLine($"<path data-cfx-role=\"funnel-segment\" data-cfx-point=\"{i}\" data-cfx-label=\"{Escape(label)}\" data-cfx-value=\"{F(values[i].Y)}\" data-cfx-retention=\"{F(retention)}\" data-cfx-dropoff=\"{F(dropOff)}\" role=\"img\" aria-label=\"{Escape(summary)}\" d=\"M {F(topLeft)} {F(y)} L {F(topRight)} {F(y)} L {F(bottomRight)} {F(y + segmentHeight)} L {F(bottomLeft)} {F(y + segmentHeight)} Z\" fill=\"{fill}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.FunnelSegmentStrokeWidth)}\" stroke-opacity=\"{F(ChartVisualPrimitives.FunnelSegmentStrokeOpacity)}\" stroke-linejoin=\"round\"/>");

            var centerX = plot.Left + plot.Width / 2;
            var centerY = y + segmentHeight / 2;
            var labelColor = FunnelTextColor(color);
            var labelStroke = FunnelTextHalo(labelColor, t.CardBackground);
            var labelWidth = Math.Max(36, Math.Min(topWidth, bottomWidth) - 18);
            if (showLabels) {
                DrawSvgTextCenteredX(sb, chart, "funnel-label", label, centerX, centerY - 4, labelColor, t.LegendFontSize, labelWidth, "800", labelStroke, 1.8);
                DrawSvgTextCenteredX(sb, chart, "funnel-value", value, centerX, centerY + 15, labelColor, t.DataLabelFontSize, labelWidth, "750", labelStroke, 1.8);
            }
            if (showLabels && i > 0) {
                var guideX = Math.Min(metricsX - 10, bottomRight + 8);
                sb.AppendLine($"<line data-cfx-role=\"funnel-dropoff-line\" x1=\"{F(guideX)}\" y1=\"{F(y + gap * -0.35)}\" x2=\"{F(guideX)}\" y2=\"{F(y + segmentHeight * 0.45)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.FunnelDropoffLineStrokeWidth)}\" stroke-dasharray=\"3 4\" opacity=\"{F(ChartVisualPrimitives.FunnelDropoffLineOpacity)}\"/>");
                sb.AppendLine($"<text data-cfx-role=\"funnel-retention\" x=\"{F(metricsX)}\" y=\"{F(centerY - 3)}\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"700\">{Escape(FormatPercent(retention))} retained</text>");
                sb.AppendLine($"<text data-cfx-role=\"funnel-dropoff\" x=\"{F(metricsX)}\" y=\"{F(centerY + 14)}\" fill=\"{t.Negative.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"650\">-{Escape(FormatPercent(dropOff))} from prev</text>");
            }
        }

        sb.AppendLine("</g>");
    }

    private static double FunnelWidth(double plotWidth, double value, double max) {
        var ratio = max <= 0 ? 1 : Clamp(value / max, 0.04, 1);
        return Math.Max(70, plotWidth * (0.22 + ratio * 0.74));
    }

    private static void DrawFunnelPointGradients(StringBuilder sb, string id, ChartSeries series) {
        var wroteDefs = false;
        for (var i = 0; i < series.PointColors.Count; i++) {
            if (!series.PointColors[i].HasValue) continue;
            if (!wroteDefs) {
                sb.AppendLine("<defs>");
                wroteDefs = true;
            }

            var color = series.PointColors[i]!.Value;
            sb.AppendLine($"<linearGradient id=\"{id}-funnelPointFill{i}\" x1=\"0\" x2=\"1\" y1=\"0\" y2=\"1\"><stop offset=\"0%\" stop-color=\"{color.ToHex()}\" stop-opacity=\"1\"/><stop offset=\"100%\" stop-color=\"{color.ToHex()}\" stop-opacity=\"0.78\"/></linearGradient>");
        }

        if (wroteDefs) sb.AppendLine("</defs>");
    }

    private static ChartColor FunnelSegmentColor(Chart chart, ChartSeries series, int pointIndex) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value
            : series.Color ?? chart.Options.Theme.Palette[pointIndex % chart.Options.Theme.Palette.Length];

    private static string FunnelSegmentFill(Chart chart, ChartSeries series, int pointIndex, string id) {
        if (pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue) return $"url(#{id}-funnelPointFill{pointIndex})";
        if (series.Color.HasValue) return series.Color.Value.ToCss();
        return $"url(#{id}-sliceFill{pointIndex % chart.Options.Theme.Palette.Length})";
    }

    private static ChartColor FunnelTextColor(ChartColor background) {
        var luminance = (0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B) / 255;
        return luminance > 0.58 ? ChartColor.FromRgb(15, 23, 42) : ChartColor.White;
    }

    private static ChartColor FunnelTextHalo(ChartColor text, ChartColor cardBackground) =>
        text.R > 240 && text.G > 240 && text.B > 240 ? cardBackground : ChartColor.Transparent;
}
