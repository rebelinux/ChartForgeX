using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawCandlesticks(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var itemCount = Math.Max(1, series.Points.Count / 4);
        var candleWidth = Math.Max(8, Math.Min(22, plot.Width / Math.Max(1, itemCount * 5.0)));
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
            var bodyTop = Math.Min(yOpen, yClose);
            var bodyHeight = Math.Max(2, Math.Abs(yClose - yOpen));
            var fill = rising ? ChartColor.FromRgba(color.R, color.G, color.B, 58) : ChartColor.FromRgba(color.R, color.G, color.B, 214);
            var halo = PngStrokeHalo(color);

            c.DrawLine(x, yHigh, x, yLow, halo, 6);
            c.DrawLine(x, yHigh, x, yLow, color, 2);
            c.StrokeRoundedRect(x - candleWidth / 2, bodyTop, candleWidth, bodyHeight, 2.5, halo, 5);
            c.FillRoundedRect(x - candleWidth / 2, bodyTop, candleWidth, bodyHeight, 2.5, fill);
            c.StrokeRoundedRect(x - candleWidth / 2, bodyTop, candleWidth, bodyHeight, 2.5, color, 2);
            if (ShouldDrawDataLabels(chart, series)) {
                var label = FormatValue(chart, close.Y);
                var fontSize = chart.Options.Theme.DataLabelFontSize;
                var labelX = x - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0;
                var aboveY = bodyTop - fontSize - 4;
                var belowY = Math.Max(yOpen, yClose) + 5;
                var labelY = aboveY < plot.Top + 2 ? belowY : aboveY;
                DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
            }
        }
    }
}
