using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawOhlc(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var itemCount = Math.Max(1, series.Points.Count / 4);
        var tickWidth = Math.Max(7, Math.Min(18, plot.Width / Math.Max(1, itemCount * 6.0)));
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 3 < series.Points.Count; pointIndex += 4) {
            var open = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            var low = series.Points[pointIndex + 2];
            var close = series.Points[pointIndex + 3];
            var rising = close.Y >= open.Y;
            var color = rising ? chart.Options.Theme.Positive : chart.Options.Theme.Negative;
            var x = map.X(open.X);
            var yOpen = map.Y(open.Y);
            var yHigh = map.Y(high.Y);
            var yLow = map.Y(low.Y);
            var yClose = map.Y(close.Y);
            var halo = PngStrokeHalo(color);

            c.DrawLine(x, yHigh, x, yLow, halo, ChartVisualPrimitives.OhlcPngHaloStrokeWidth);
            c.DrawLine(x - tickWidth, yOpen, x, yOpen, halo, ChartVisualPrimitives.OhlcPngHaloStrokeWidth);
            c.DrawLine(x, yClose, x + tickWidth, yClose, halo, ChartVisualPrimitives.OhlcPngHaloStrokeWidth);
            c.DrawLine(x, yHigh, x, yLow, color, ChartVisualPrimitives.OhlcStrokeWidth);
            c.DrawLine(x - tickWidth, yOpen, x, yOpen, color, ChartVisualPrimitives.OhlcStrokeWidth);
            c.DrawLine(x, yClose, x + tickWidth, yClose, color, ChartVisualPrimitives.OhlcStrokeWidth);
            if (!ShouldDrawDataLabels(chart, series)) continue;

            var label = FormatValue(chart, close.Y);
            var fontSize = chart.Options.Theme.DataLabelFontSize;
            var labelX = x + tickWidth + ChartVisualPrimitives.OhlcLabelOffset;
            var labelY = yClose - fontSize / 2.0;
            if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reservedLabels)) continue;
            DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
        }
    }
}
