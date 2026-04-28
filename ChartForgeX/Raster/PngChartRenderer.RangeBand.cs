using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawRangeBand(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var color = series.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        var lower = new List<ChartPoint>();
        var upper = new List<ChartPoint>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var low = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            lower.Add(new ChartPoint(map.X(low.X), map.Y(low.Y)));
            upper.Add(new ChartPoint(map.X(high.X), map.Y(high.Y)));
        }

        if (lower.Count == 0) return;
        var polygon = new List<ChartPoint>(lower.Count + upper.Count);
        foreach (var point in upper) polygon.Add(point);
        for (var i = lower.Count - 1; i >= 0; i--) polygon.Add(lower[i]);
        c.FillPolygonVerticalGradient(polygon, ApplyOpacity(color, ChartVisualPrimitives.RangeBandFillOpacity + 0.06), ApplyOpacity(color, ChartVisualPrimitives.RangeBandFillOpacity * 0.45));
        DrawPngLinePath(c, upper, PngStrokeHalo(color), ChartVisualPrimitives.RangeBandPngHaloStrokeWidth);
        DrawPngLinePath(c, lower, PngStrokeHalo(color), ChartVisualPrimitives.RangeBandPngHaloStrokeWidth);
        var boundary = ApplyOpacity(color, ChartVisualPrimitives.RangeBandBoundaryOpacity);
        for (var i = 1; i < upper.Count; i++) {
            c.DrawLine(upper[i - 1].X, upper[i - 1].Y, upper[i].X, upper[i].Y, boundary, ChartVisualPrimitives.RangeBandBoundaryStrokeWidth);
            c.DrawLine(lower[i - 1].X, lower[i - 1].Y, lower[i].X, lower[i].Y, boundary, ChartVisualPrimitives.RangeBandBoundaryStrokeWidth);
        }

        if (ShouldDrawDataLabels(chart, series)) {
            var reserved = new List<ChartLabelBounds>();
            for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
                var low = series.Points[pointIndex];
                var high = series.Points[pointIndex + 1];
                var label = FormatValue(chart, low.Y) + "-" + FormatValue(chart, high.Y);
                var fontSize = chart.Options.Theme.DataLabelFontSize;
                var x = map.X(low.X) - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0;
                var y = Math.Min(map.Y(low.Y), map.Y(high.Y)) - fontSize - 4;
                if (!ReservePngLabel(label, x, y, chart, plot, fontSize, reserved)) continue;
                DrawReadablePngLabel(c, plot, x, y, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
            }
        }
    }
}
