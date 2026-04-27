using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawOhlc(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var itemCount = Math.Max(1, series.Points.Count / 4);
        var tickWidth = Math.Max(7, Math.Min(18, plot.Width / Math.Max(1, itemCount * 6.0)));
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

            c.DrawLine(x, yHigh, x, yLow, halo, 6);
            c.DrawLine(x - tickWidth, yOpen, x, yOpen, halo, 6);
            c.DrawLine(x, yClose, x + tickWidth, yClose, halo, 6);
            c.DrawLine(x, yHigh, x, yLow, color, 2);
            c.DrawLine(x - tickWidth, yOpen, x, yOpen, color, 2);
            c.DrawLine(x, yClose, x + tickWidth, yClose, color, 2);
            if (!ShouldDrawDataLabels(chart, series)) continue;

            var label = FormatValue(chart, close.Y);
            var fontSize = chart.Options.Theme.DataLabelFontSize;
            DrawReadablePngLabel(c, plot, x + tickWidth + 6, yClose - fontSize / 2.0, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
        }
    }
}
