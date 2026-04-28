using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawPolarArea(RgbaCanvas c, Chart chart, ChartRect plot) {
        var series = chart.Series[0];
        var values = new List<ChartPoint>();
        foreach (var point in series.Points) if (point.Y > 0) values.Add(point);
        if (values.Count == 0) return;

        var max = 0.0;
        var total = 0.0;
        foreach (var value in values) {
            max = Math.Max(max, value.Y);
            total += value.Y;
        }

        var radius = Math.Max(ChartVisualPrimitives.PolarAreaMinRadius, Math.Min(plot.Width, plot.Height) * ChartVisualPrimitives.PolarAreaRadiusFactor);
        var cx = plot.Left + plot.Width * ChartVisualPrimitives.PolarAreaCenterXFactor;
        var cy = plot.Top + plot.Height / 2;
        var sweep = Math.PI * 2 / values.Count;
        var separator = chart.Options.Theme.CardBackground;

        if (chart.Options.ShowGrid) DrawPolarAreaGrid(c, chart, cx, cy, radius);
        for (var i = 0; i < values.Count; i++) {
            var start = -Math.PI / 2 + i * sweep;
            var end = start + sweep;
            var segmentRadius = radius * Math.Sqrt(values[i].Y / max);
            var color = chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length];
            c.FillRingSlice(cx, cy, segmentRadius, 0, start, end, color);
            DrawSliceSeparator(c, cx, cy, segmentRadius, 0, start, separator);
            c.DrawArc(cx, cy, segmentRadius, start, end, separator, ChartVisualPrimitives.SliceSeparatorStrokeWidth);

            if (ShouldDrawDataLabels(chart, series) && segmentRadius > ChartVisualPrimitives.PolarAreaLabelMinRadius) {
                var mid = start + sweep / 2;
                var label = FormatValue(chart, values[i].Y);
                var fontSize = chart.Options.Theme.DataLabelFontSize;
                var labelRadius = segmentRadius * ChartVisualPrimitives.PolarAreaLabelRadiusFactor;
                DrawReadablePngLabel(c, plot, cx + Math.Cos(mid) * labelRadius - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0, cy + Math.Sin(mid) * labelRadius - fontSize / 2.0, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
            }
        }

        if (chart.Options.ShowGrid) c.DrawCircleOutline(cx, cy, radius, ApplyOpacity(chart.Options.Theme.Grid, ChartVisualPrimitives.PolarAreaGridOpacity), ChartVisualPrimitives.GridStrokeWidth);
        if (chart.Options.ShowLegend) DrawSliceLegend(c, chart, values, plot, total);
    }

    private static void DrawPolarAreaGrid(RgbaCanvas c, Chart chart, double cx, double cy, double radius) {
        var grid = ApplyOpacity(chart.Options.Theme.Grid, ChartVisualPrimitives.PolarAreaGridOpacity);
        for (var i = 1; i <= ChartVisualPrimitives.PolarAreaGridRings; i++) c.DrawCircleOutline(cx, cy, radius * i / ChartVisualPrimitives.PolarAreaGridRings, grid, ChartVisualPrimitives.GridStrokeWidth);
    }
}
