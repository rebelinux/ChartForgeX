using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawUsStateGeoMap(RgbaCanvas c, Chart chart, ChartRect basePlot) {
        ChartSeries? series = null;
        foreach (var item in chart.Series) if (item.Kind == ChartSeriesKind.UsStateGeoMap) { series = item; break; }
        if (series == null || series.Points.Count == 0) return;
        var data = UsStateMapValues(chart, series);
        var t = chart.Options.Theme;
        var bottomReserve = chart.Options.ShowMapScaleLegend ? 46 : 12;
        var plot = new ChartRect(basePlot.Left + 10, basePlot.Top + 8, Math.Max(1, basePlot.Width - 20), Math.Max(1, basePlot.Height - bottomReserve));
        var map = FitUsStateGeoMap(plot);
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var entry in data.Values) { if (entry.Value < min) min = entry.Value; if (entry.Value > max) max = entry.Value; }
        if (double.IsInfinity(min)) { min = 0; max = 1; }
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var hasMissing = false;
        foreach (var shape in UsStateGeoShapes.Shapes) {
            if (!data.ContainsKey(shape.Code)) { hasMissing = true; break; }
        }

        foreach (var shape in UsStateGeoShapes.Shapes) {
            var hasValue = data.TryGetValue(shape.Code, out var entry);
            var value = hasValue ? entry.Value : 0;
            var color = hasValue ? HeatmapColor(chart, entry.Color ?? series.Color ?? t.Palette[0], value, min, max) : UsStateNoDataColor(chart);
            var rings = ProjectUsStateGeoRings(shape.Path, map, out var regionBounds);
            foreach (var points in rings) {
                c.FillPolygon(points, color);
                for (var i = 0; i < points.Count; i++) {
                    var next = points[(i + 1) % points.Count];
                    c.DrawLine(points[i].X, points[i].Y, next.X, next.Y, t.CardBackground, 1.1);
                }
            }
            if (chart.Options.ShowMapLabels) {
                var label = ProjectUsStateGeoPoint(shape.Label, map);
                var fontSize = Math.Min(PngTickFontSize(chart), Math.Max(7, map.Height * 0.032));
                if (ShouldDrawUsStateGeoMapLabel(shape.Code, regionBounds, fontSize)) c.DrawTextEmphasized(label.X - EstimatePngEmphasizedTextWidth(shape.Code, fontSize) / 2, label.Y - fontSize / 2, shape.Code, HeatmapTextColor(color), fontSize);
            }
        }

        if (chart.Options.ShowMapScaleLegend) DrawUsStateGeoMapPngScale(c, chart, series, min, max, hasMissing, plot.Right - 84, plot.Bottom - 14, plot);
    }

    private static void DrawUsStateGeoMapPngScale(RgbaCanvas c, Chart chart, ChartSeries series, double min, double max, bool hasMissing, double x, double y, ChartRect plot) {
        var t = chart.Options.Theme;
        var size = 11.0;
        var gap = 3.0;
        var fontSize = PngTickFontSize(chart);
        if (hasMissing) DrawUsStateMapPngNoDataScale(c, chart, x, y, size, fontSize, plot);
        c.DrawText(x - EstimatePngTextWidth("Less", fontSize) - 8, y + size / 2 - fontSize / 2, "Less", t.MutedText, fontSize);
        for (var i = 0; i < 5; i++) {
            var value = min + (max - min) * (i / 4.0);
            var color = HeatmapColor(chart, series.Color ?? t.Palette[0], value, min, max);
            c.FillRoundedRect(x + i * (size + gap), y, size, size, 2, color);
        }
        c.DrawText(x + 5 * size + 4 * gap + 8, y + size / 2 - fontSize / 2, "More", t.MutedText, fontSize);
    }

    private static List<List<ChartPoint>> ProjectUsStateGeoRings(string path, ChartRect target, out ChartRect bounds) {
        var rings = new List<List<ChartPoint>>();
        List<ChartPoint>? current = null;
        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var minY = double.PositiveInfinity;
        var maxY = double.NegativeInfinity;
        for (var i = 0; i < path.Length; i++) {
            var command = path[i];
            if (command == 'M' || command == 'L') {
                i++;
                var x = ReadUsStateGeoNumber(path, ref i);
                var y = ReadUsStateGeoNumber(path, ref i);
                if (command == 'M') {
                    current = new List<ChartPoint>();
                    rings.Add(current);
                }

                var point = ProjectUsStateGeoPoint(new ChartPoint(x, y), target);
                if (point.X < minX) minX = point.X;
                if (point.X > maxX) maxX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.Y > maxY) maxY = point.Y;
                current?.Add(point);
                i--;
            } else if (command == 'Z') {
                current = null;
            } else if (!char.IsWhiteSpace(command)) {
                throw new InvalidOperationException("Unsupported US state geo path command.");
            }
        }

        for (var i = rings.Count - 1; i >= 0; i--) if (rings[i].Count < 3) rings.RemoveAt(i);
        bounds = double.IsInfinity(minX) ? new ChartRect(0, 0, 0, 0) : new ChartRect(minX, minY, Math.Max(0, maxX - minX), Math.Max(0, maxY - minY));
        return rings;
    }

    private static bool ShouldDrawUsStateGeoMapLabel(string code, ChartRect bounds, double fontSize) {
        if (IsCompactUsStateGeoMapLabel(code)) return false;
        return bounds.Width >= EstimatePngEmphasizedTextWidth(code, fontSize) + 8 && bounds.Height >= fontSize + 5;
    }

    private static bool IsCompactUsStateGeoMapLabel(string code) {
        return code is "CT" or "DC" or "DE" or "MA" or "MD" or "NJ" or "RI";
    }

    private static double ReadUsStateGeoNumber(string path, ref int index) {
        while (index < path.Length && char.IsWhiteSpace(path[index])) index++;
        var start = index;
        while (index < path.Length && (char.IsDigit(path[index]) || path[index] == '-' || path[index] == '.')) index++;
        return double.Parse(path.Substring(start, index - start), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static ChartPoint ProjectUsStateGeoPoint(ChartPoint point, ChartRect target) {
        var bounds = UsStateGeoShapes.Bounds;
        var x = target.Left + (point.X - bounds.Left) / bounds.Width * target.Width;
        var y = target.Top + (point.Y - bounds.Top) / bounds.Height * target.Height;
        return new ChartPoint(x, y);
    }

    private static ChartRect FitUsStateGeoMap(ChartRect plot) {
        var bounds = UsStateGeoShapes.Bounds;
        var aspect = bounds.Width / Math.Max(1, bounds.Height);
        var width = Math.Min(plot.Width, plot.Height * aspect);
        var height = width / aspect;
        if (height > plot.Height) {
            height = plot.Height;
            width = height * aspect;
        }

        return new ChartRect(plot.Left + Math.Max(0, (plot.Width - width) / 2), plot.Top + Math.Max(0, (plot.Height - height) / 2), width, height);
    }

    private static bool IsUsStateGeoMapChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.UsStateGeoMap) return true;
        return false;
    }
}
