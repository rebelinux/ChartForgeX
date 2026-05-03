using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawUsStateTileMap(RgbaCanvas c, Chart chart, ChartRect basePlot) {
        ChartSeries? series = null;
        foreach (var item in chart.Series) if (item.Kind == ChartSeriesKind.UsStateTileMap) { series = item; break; }
        if (series == null || series.Points.Count == 0) return;
        var data = UsStateMapValues(chart, series);
        var t = chart.Options.Theme;
        var bottomReserve = chart.Options.ShowMapScaleLegend ? 52 : 20;
        var plot = new ChartRect(basePlot.Left + 10, basePlot.Top + 10, Math.Max(1, basePlot.Width - 20), Math.Max(1, basePlot.Height - bottomReserve));
        var maxColumn = 0;
        var maxRow = 0;
        foreach (var tile in UsStateTiles) { if (tile.Column > maxColumn) maxColumn = tile.Column; if (tile.Row > maxRow) maxRow = tile.Row; }
        var gap = 4.0;
        var tileSize = Math.Max(4, Math.Min((plot.Width - gap * maxColumn) / (maxColumn + 1), (plot.Height - gap * maxRow) / (maxRow + 1)));
        var width = (maxColumn + 1) * tileSize + maxColumn * gap;
        var height = (maxRow + 1) * tileSize + maxRow * gap;
        var x0 = plot.Left + Math.Max(0, (plot.Width - width) / 2);
        var y0 = plot.Top + Math.Max(0, (plot.Height - height) / 2);
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var entry in data.Values) { if (entry.Value < min) min = entry.Value; if (entry.Value > max) max = entry.Value; }
        if (double.IsInfinity(min)) { min = 0; max = 1; }
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var hasMissing = false;
        foreach (var tile in UsStateTiles) {
            if (!data.ContainsKey(tile.Code)) { hasMissing = true; break; }
        }

        foreach (var tile in UsStateTiles) {
            var hasValue = data.TryGetValue(tile.Code, out var entry);
            var value = hasValue ? entry.Value : 0;
            var color = hasValue ? HeatmapColor(chart, entry.Color ?? series.Color ?? t.Palette[0], value, min, max) : UsStateNoDataColor(chart);
            var x = x0 + tile.Column * (tileSize + gap);
            var y = y0 + tile.Row * (tileSize + gap);
            var points = HexTilePoints(x, y, tileSize);
            c.FillPolygon(points, color);
            for (var i = 0; i < points.Count; i++) {
                var next = points[(i + 1) % points.Count];
                c.DrawLine(points[i].X, points[i].Y, next.X, next.Y, t.CardBackground, Math.Max(1, tileSize * 0.035));
            }
            if (chart.Options.ShowMapLabels) {
                var fontSize = Math.Min(PngTickFontSize(chart), tileSize * 0.32);
                c.DrawTextEmphasized(x + tileSize / 2 - EstimatePngEmphasizedTextWidth(tile.Code, fontSize) / 2, y + tileSize / 2 - fontSize / 2, tile.Code, HeatmapTextColor(color), fontSize);
            }
        }

        if (chart.Options.ShowMapScaleLegend) {
            var scaleSize = Math.Max(8, Math.Min(13, tileSize * 0.32));
            var scaleY = Clamp(y0 + height + 22, basePlot.Top, basePlot.Bottom - scaleSize - 6);
            DrawUsStateTileMapPngScale(c, chart, series, min, max, hasMissing, x0 + width, scaleY, tileSize, plot);
        }
    }

    private static void DrawUsStateTileMapPngScale(RgbaCanvas c, Chart chart, ChartSeries series, double min, double max, bool hasMissing, double right, double y, double tileSize, ChartRect plot) {
        var t = chart.Options.Theme;
        var size = Math.Max(8, Math.Min(13, tileSize * 0.32));
        var gap = Math.Max(2, size * 0.3);
        var width = 5 * size + 4 * gap;
        var x = right - width;
        var fontSize = PngTickFontSize(chart);
        if (hasMissing) DrawUsStateMapPngNoDataScale(c, chart, x, y, size, fontSize, plot);
        c.DrawText(x - EstimatePngTextWidth("Less", fontSize) - 8, y + size / 2 - fontSize / 2, "Less", t.MutedText, fontSize);
        for (var i = 0; i < 5; i++) {
            var value = min + (max - min) * (i / 4.0);
            var color = HeatmapColor(chart, series.Color ?? t.Palette[0], value, min, max);
            c.FillRoundedRect(x + i * (size + gap), y, size, size, Math.Min(3, size * 0.22), color);
        }
        c.DrawText(x + width + 8, y + size / 2 - fontSize / 2, "More", t.MutedText, fontSize);
    }

    private static void DrawUsStateMapPngNoDataScale(RgbaCanvas c, Chart chart, double valueScaleX, double y, double size, double fontSize, ChartRect plot) {
        const string label = "No data";
        var width = size + 5 + EstimatePngTextWidth(label, fontSize);
        var x = valueScaleX - width - 18;
        if (x < plot.Left) {
            x = plot.Left;
            y = Math.Max(plot.Top, y - size - 9);
        }

        c.FillRoundedRect(x, y, size, size, Math.Min(3, size * 0.22), UsStateNoDataColor(chart));
        c.DrawText(x + size + 5, y + size / 2 - fontSize / 2, label, chart.Options.Theme.MutedText, fontSize);
    }

    private static ChartColor UsStateNoDataColor(Chart chart) => Blend(chart.Options.Theme.PlotBackground, chart.Options.Theme.Grid, 0.58);

    private static List<ChartPoint> HexTilePoints(double x, double y, double size) {
        var inset = size * 0.22;
        return new List<ChartPoint> {
            new(x + inset, y),
            new(x + size - inset, y),
            new(x + size, y + size / 2),
            new(x + size - inset, y + size),
            new(x + inset, y + size),
            new(x, y + size / 2)
        };
    }

    private static Dictionary<string, RegionMapValue> UsStateMapValues(Chart chart, ChartSeries series) {
        var values = new Dictionary<string, RegionMapValue>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < series.Points.Count; i++) {
            var region = i < chart.Options.XAxisLabels.Count ? chart.Options.XAxisLabels[i].Text : string.Empty;
            if (region.Length == 0) continue;
            var color = i < series.PointColors.Count ? series.PointColors[i] : null;
            values[region] = new RegionMapValue(series.Points[i].Y, color);
        }

        return values;
    }

    private static bool IsUsStateTileMapChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.UsStateTileMap) return true;
        return false;
    }

    private readonly struct RegionMapValue {
        public readonly double Value;
        public readonly ChartColor? Color;

        public RegionMapValue(double value, ChartColor? color) {
            Value = value;
            Color = color;
        }
    }

    private readonly struct UsStateTile {
        public readonly string Code;
        public readonly int Column;
        public readonly int Row;

        public UsStateTile(string code, int column, int row) {
            Code = code;
            Column = column;
            Row = row;
        }
    }

    private static readonly UsStateTile[] UsStateTiles = {
        new("AK", 0, 0), new("ME", 11, 0),
        new("VT", 9, 1), new("NH", 10, 1),
        new("WA", 0, 2), new("MT", 1, 2), new("ND", 2, 2), new("MN", 3, 2), new("WI", 4, 2), new("MI", 5, 2), new("NY", 8, 2), new("MA", 10, 2), new("RI", 11, 2),
        new("OR", 0, 3), new("ID", 1, 3), new("SD", 2, 3), new("IA", 3, 3), new("IL", 4, 3), new("IN", 5, 3), new("OH", 6, 3), new("PA", 7, 3), new("NJ", 8, 3), new("CT", 9, 3),
        new("CA", 0, 4), new("NV", 1, 4), new("WY", 2, 4), new("NE", 3, 4), new("MO", 4, 4), new("KY", 5, 4), new("WV", 6, 4), new("VA", 7, 4), new("MD", 8, 4), new("DE", 9, 4),
        new("AZ", 1, 5), new("UT", 2, 5), new("CO", 3, 5), new("KS", 4, 5), new("AR", 5, 5), new("TN", 6, 5), new("NC", 7, 5), new("SC", 8, 5), new("DC", 9, 5),
        new("HI", 0, 6), new("NM", 2, 6), new("OK", 3, 6), new("LA", 4, 6), new("MS", 5, 6), new("AL", 6, 6), new("GA", 7, 6),
        new("TX", 3, 7), new("FL", 8, 7)
    };
}
