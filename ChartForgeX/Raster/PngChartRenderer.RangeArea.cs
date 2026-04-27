using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawRangeArea(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var color = series.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        var lower = new List<ChartPoint>();
        var upper = new List<ChartPoint>();
        var middle = new List<ChartPoint>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var low = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            lower.Add(new ChartPoint(map.X(low.X), map.Y(low.Y)));
            upper.Add(new ChartPoint(map.X(high.X), map.Y(high.Y)));
            middle.Add(new ChartPoint(map.X(low.X), map.Y((low.Y + high.Y) / 2.0)));
        }

        if (lower.Count == 0) return;
        var upperPath = ChartPathBuilder.FromPoints(upper, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var lowerPath = ChartPathBuilder.FromPoints(lower, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var middlePath = ChartPathBuilder.FromPoints(middle, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var polygon = new List<ChartPoint>(upperPath.Count + lowerPath.Count);
        foreach (var point in upperPath) polygon.Add(point);
        for (var i = lowerPath.Count - 1; i >= 0; i--) polygon.Add(lowerPath[i]);
        c.FillPolygonVerticalGradient(polygon, ChartColor.FromRgba(color.R, color.G, color.B, 118), ChartColor.FromRgba(color.R, color.G, color.B, 18));
        DrawDashedPngPath(c, middlePath, ChartColor.FromRgba(color.R, color.G, color.B, 126), 1, 5, 6);
        DrawPngLinePath(c, upperPath, PngStrokeHalo(color), series.StrokeWidth + 4);
        DrawPngLinePath(c, lowerPath, PngStrokeHalo(color), series.StrokeWidth + 4);
        DrawPngLinePath(c, upperPath, color, series.StrokeWidth);
        DrawPngLinePath(c, lowerPath, ChartColor.FromRgba(color.R, color.G, color.B, 224), Math.Max(1.5, series.StrokeWidth * 0.9));

        if (!ShouldDrawDataLabels(chart, series)) return;
        var reserved = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var low = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            var label = FormatValue(chart, low.Y) + "-" + FormatValue(chart, high.Y);
            var fontSize = chart.Options.Theme.DataLabelFontSize;
            var x = map.X(low.X) - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0;
            var y = Math.Min(map.Y(low.Y), map.Y(high.Y)) - fontSize - 5;
            if (!ReservePngLabel(label, x, y, chart, plot, fontSize, reserved)) continue;
            DrawReadablePngLabel(c, plot, x, y, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
        }
    }

    private static void DrawDashedPngPath(RgbaCanvas c, IReadOnlyList<ChartPoint> points, ChartColor color, int thickness, double dash, double gap) {
        for (var i = 1; i < points.Count; i++) {
            var a = points[i - 1];
            var b = points[i];
            c.DrawDashedLine(a.X, a.Y, b.X, b.Y, color, thickness, dash, gap);
        }
    }
}
