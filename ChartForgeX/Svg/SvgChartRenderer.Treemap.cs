using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawTreemap(StringBuilder sb, Chart chart, ChartRect basePlot, string id) {
        var seriesIndex = -1;
        ChartSeries? series = null;
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i].Kind != ChartSeriesKind.Treemap) continue;
            seriesIndex = i;
            series = chart.Series[i];
            break;
        }

        if (series == null || seriesIndex < 0) return;
        var tiles = ChartTreemapLayout.Compute(series, TreemapPlot(chart, basePlot));
        if (tiles.Count == 0) return;

        var t = chart.Options.Theme;
        sb.AppendLine("<g data-cfx-role=\"treemap\">");
        for (var i = 0; i < tiles.Count; i++) {
            var tile = tiles[i];
            var rect = tile.Rect;
            if (rect.Width <= 0 || rect.Height <= 0) continue;
            var color = series.Color ?? t.Palette[tile.PointIndex % t.Palette.Length];
            var label = FormatX(chart, tile.Point.X);
            var value = FormatValue(chart, tile.Point.Y);
            var summary = label + ": " + value;
            var radius = Math.Min(8, Math.Min(rect.Width, rect.Height) * 0.08);
            DrawTreemapTile(sb, chart, id, seriesIndex, tile.PointIndex, rect, color, radius, summary, series.Color.HasValue);
            DrawTreemapTileLabels(sb, chart, rect, label, value, color);
        }

        sb.AppendLine("</g>");
    }

    private static void DrawTreemapTile(StringBuilder sb, Chart chart, string id, int seriesIndex, int pointIndex, ChartRect rect, ChartColor color, double radius, string summary, bool hasSeriesColor) {
        var fill = hasSeriesColor ? $"url(#{id}-seriesFill{seriesIndex})" : $"url(#{id}-sliceFill{pointIndex % chart.Options.Theme.Palette.Length})";
        var highlightInset = Math.Min(radius, rect.Width / 4);
        var highlightEnd = rect.X + rect.Width - highlightInset;
        sb.AppendLine($"<rect data-cfx-role=\"treemap-tile\" data-cfx-point=\"{pointIndex}\" role=\"img\" aria-label=\"{Escape(summary)}\" x=\"{F(rect.X)}\" y=\"{F(rect.Y)}\" width=\"{F(rect.Width)}\" height=\"{F(rect.Height)}\" rx=\"{F(radius)}\" fill=\"{fill}\"/>");
        sb.AppendLine($"<rect data-cfx-role=\"treemap-tile-border\" data-cfx-point=\"{pointIndex}\" x=\"{F(rect.X + 0.5)}\" y=\"{F(rect.Y + 0.5)}\" width=\"{F(Math.Max(0, rect.Width - 1))}\" height=\"{F(Math.Max(0, rect.Height - 1))}\" rx=\"{F(Math.Max(0, radius - 0.5))}\" fill=\"none\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-opacity=\"0.70\" stroke-width=\"1.4\"/>");
        if (highlightEnd > rect.X + highlightInset && rect.Height > 12) {
            sb.AppendLine($"<line data-cfx-role=\"treemap-tile-highlight\" data-cfx-point=\"{pointIndex}\" x1=\"{F(rect.X + highlightInset)}\" y1=\"{F(rect.Y + 1.35)}\" x2=\"{F(highlightEnd)}\" y2=\"{F(rect.Y + 1.35)}\" stroke=\"#fff\" stroke-opacity=\"0.22\" stroke-width=\"1\" stroke-linecap=\"round\"/>");
        }
    }

    private static ChartRect TreemapPlot(Chart chart, ChartRect basePlot) {
        var legendReserve = chart.Options.ShowLegend ? 18 + LegendRowHeight : 0;
        return new ChartRect(basePlot.X + 10, basePlot.Y + 12, Math.Max(1, basePlot.Width - 20), Math.Max(1, basePlot.Height - 24 - legendReserve));
    }

    private static void DrawTreemapTileLabels(StringBuilder sb, Chart chart, ChartRect rect, string label, string value, ChartColor color) {
        if (rect.Width < 42 || rect.Height < 26) return;
        var t = chart.Options.Theme;
        var textColor = HeatmapTextColor(color);
        var maxWidth = Math.Max(8, rect.Width - 14);
        var labelFontSize = TextFontSizeForSvgWidth(label, maxWidth, Math.Min(t.LegendFontSize, Math.Max(8, rect.Height * 0.20)));
        var fittedLabel = TrimSvgLabelToWidth(label, labelFontSize, maxWidth);
        if (fittedLabel.Length > 0) {
            sb.AppendLine($"<text data-cfx-role=\"treemap-label\" x=\"{F(rect.X + 8)}\" y=\"{F(rect.Y + 8 + labelFontSize)}\" fill=\"{textColor.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(labelFontSize)}\" font-weight=\"800\">{Escape(fittedLabel)}</text>");
        }

        if (rect.Height < 48) return;
        var valueFontSize = TextFontSizeForSvgWidth(value, maxWidth, Math.Min(t.DataLabelFontSize, Math.Max(8, rect.Height * 0.18)));
        var fittedValue = TrimSvgLabelToWidth(value, valueFontSize, maxWidth);
        if (fittedValue.Length > 0) {
            sb.AppendLine($"<text data-cfx-role=\"treemap-value\" x=\"{F(rect.X + 8)}\" y=\"{F(rect.Y + 12 + labelFontSize + valueFontSize)}\" fill=\"{textColor.ToCss()}\" fill-opacity=\"0.82\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(valueFontSize)}\" font-weight=\"700\">{Escape(fittedValue)}</text>");
        }
    }

    private static bool IsTreemapChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Treemap);
}
