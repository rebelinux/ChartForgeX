using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawTreemap(RgbaCanvas c, Chart chart, ChartRect basePlot) {
        ChartSeries? series = null;
        foreach (var candidate in chart.Series) {
            if (candidate.Kind == ChartSeriesKind.Treemap) {
                series = candidate;
                break;
            }
        }

        if (series == null) return;
        var showLabels = series.ShowDataLabels != false;
        var tiles = ChartTreemapLayout.Compute(series, TreemapPlot(chart, basePlot));
        foreach (var tile in tiles) {
            var rect = tile.Rect;
            if (rect.Width <= 0 || rect.Height <= 0) continue;
            var color = series.Color ?? chart.Options.Theme.Palette[tile.PointIndex % chart.Options.Theme.Palette.Length];
            var radius = Math.Min(ChartVisualPrimitives.TreemapTileCornerRadiusMax, Math.Min(rect.Width, rect.Height) * ChartVisualPrimitives.TreemapTileCornerRadiusFactor);
            c.FillRoundedRectVerticalGradient(rect.X, rect.Y, rect.Width, rect.Height, radius, TreemapTileGradientTop(color), TreemapTileGradientBottom(color));
            c.StrokeRoundedRect(rect.X, rect.Y, rect.Width, rect.Height, radius, ApplyOpacity(chart.Options.Theme.CardBackground, ChartVisualPrimitives.TreemapTileBorderOpacity), ChartVisualPrimitives.TreemapTileBorderStrokeWidth);
            DrawTreemapTileHighlight(c, rect, radius);
            if (showLabels) DrawTreemapTileLabels(c, chart, rect, FormatX(chart, tile.Point.X), FormatValue(chart, tile.Point.Y), color);
        }
    }

    private static void DrawTreemapTileHighlight(RgbaCanvas c, ChartRect rect, double radius) {
        var highlightInset = Math.Min(radius, rect.Width / 4);
        var highlightEnd = rect.X + rect.Width - highlightInset;
        if (highlightEnd <= rect.X + highlightInset || rect.Height <= 12) return;
        c.DrawLine(rect.X + highlightInset, rect.Y + 1.35, highlightEnd, rect.Y + 1.35, ApplyOpacity(ChartColor.White, ChartVisualPrimitives.TreemapTileHighlightOpacity), ChartVisualPrimitives.TreemapTileHighlightStrokeWidth);
    }

    private static ChartRect TreemapPlot(Chart chart, ChartRect basePlot) {
        var legendReserve = chart.Options.ShowLegend ? 18 + PngLegendRowCount(chart) * (PngLegendFontSize(chart) + 6) : 0;
        return new ChartRect(basePlot.X + 10, basePlot.Y + 12, Math.Max(1, basePlot.Width - 20), Math.Max(1, basePlot.Height - 24 - legendReserve));
    }

    private static void DrawTreemapTileLabels(RgbaCanvas c, Chart chart, ChartRect rect, string label, string value, ChartColor color) {
        if (rect.Width < 42 || rect.Height < 26) return;
        var textColor = HeatmapTextColor(color);
        var maxWidth = Math.Max(8, rect.Width - 14);
        var labelFontSize = TextFontSizeForEmphasizedWidth(label, maxWidth, Math.Min(PngLegendFontSize(chart), Math.Max(8, rect.Height * 0.20)));
        var fittedLabel = TrimReadablePngLabelToWidth(label, labelFontSize, maxWidth);
        if (fittedLabel.Length > 0) {
            DrawReadablePngLabel(c, rect.X + 8, rect.Y + 8, fittedLabel, textColor, color, labelFontSize);
        }

        if (rect.Height < 48) return;
        var valueFontSize = TextFontSizeForEmphasizedWidth(value, maxWidth, Math.Min(chart.Options.Theme.DataLabelFontSize, Math.Max(8, rect.Height * 0.18)));
        var fittedValue = TrimReadablePngLabelToWidth(value, valueFontSize, maxWidth);
        if (fittedValue.Length > 0) {
            DrawReadablePngLabel(c, rect.X + 8, rect.Y + 13 + labelFontSize, fittedValue, textColor, color, valueFontSize);
        }
    }

    private static bool IsTreemapChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.Treemap) return true;
        return false;
    }

    private static ChartColor TreemapTileGradientTop(ChartColor color) => Blend(ChartColor.White, color, ChartVisualPrimitives.TreemapTileGradientTopBlend);

    private static ChartColor TreemapTileGradientBottom(ChartColor color) => Blend(ChartColor.Black, color, ChartVisualPrimitives.TreemapTileGradientBottomBlend);
}
