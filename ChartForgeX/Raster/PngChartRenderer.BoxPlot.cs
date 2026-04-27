using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawBoxPlots(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var color = series.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        var boxCount = Math.Max(1, series.Points.Count / 5);
        var boxWidth = Math.Max(14, Math.Min(46, plot.Width / Math.Max(1, boxCount * 5.0)));
        var capWidth = boxWidth * 0.74;

        for (var pointIndex = 0; pointIndex + 4 < series.Points.Count; pointIndex += 5) {
            var minimum = series.Points[pointIndex];
            var q1 = series.Points[pointIndex + 1];
            var median = series.Points[pointIndex + 2];
            var q3 = series.Points[pointIndex + 3];
            var maximum = series.Points[pointIndex + 4];
            var x = map.X(minimum.X);
            var yMin = map.Y(minimum.Y);
            var yQ1 = map.Y(q1.Y);
            var yMedian = map.Y(median.Y);
            var yQ3 = map.Y(q3.Y);
            var yMax = map.Y(maximum.Y);
            var top = Math.Min(yQ1, yQ3);
            var height = Math.Max(2, Math.Abs(yQ3 - yQ1));
            var halo = PngStrokeHalo(color);

            c.DrawLine(x, yMax, x, yMin, halo, 6);
            c.DrawLine(x - capWidth / 2, yMin, x + capWidth / 2, yMin, halo, 6);
            c.DrawLine(x - capWidth / 2, yMax, x + capWidth / 2, yMax, halo, 6);
            c.DrawLine(x, yMax, x, yMin, color, 2);
            c.DrawLine(x - capWidth / 2, yMin, x + capWidth / 2, yMin, color, 2);
            c.DrawLine(x - capWidth / 2, yMax, x + capWidth / 2, yMax, color, 2);
            c.StrokeRoundedRect(x - boxWidth / 2, top, boxWidth, height, 5, halo, 5);
            c.FillRoundedRect(x - boxWidth / 2, top, boxWidth, height, 5, ChartColor.FromRgba(color.R, color.G, color.B, 56));
            c.StrokeRoundedRect(x - boxWidth / 2, top, boxWidth, height, 5, color, 2);
            c.DrawLine(x - boxWidth / 2, yMedian, x + boxWidth / 2, yMedian, halo, 6);
            c.DrawLine(x - boxWidth / 2, yMedian, x + boxWidth / 2, yMedian, color, 2);
            if (ShouldDrawDataLabels(chart, series)) {
                var label = FormatValue(chart, median.Y);
                var fontSize = chart.Options.Theme.DataLabelFontSize;
                DrawReadablePngLabel(c, plot, x - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0, yMedian - fontSize - 4, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
            }
        }
    }
}
