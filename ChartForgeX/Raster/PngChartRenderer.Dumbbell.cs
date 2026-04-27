using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawDumbbells(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var color = series.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        var startColor = chart.Options.Theme.MutedText;
        var radius = Math.Max(4.2, chart.Options.Theme.MarkerRadius + 1.25);
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var start = series.Points[pointIndex];
            var end = series.Points[pointIndex + 1];
            var x = map.X(start.X);
            var yStart = map.Y(start.Y);
            var yEnd = map.Y(end.Y);

            c.DrawLine(x, yStart, x, yEnd, ChartColor.FromRgba(color.R, color.G, color.B, 124), 3);
            DrawMarker(c, chart, x, yStart, radius, startColor);
            DrawMarker(c, chart, x, yEnd, radius, color);
            if (ShouldDrawDataLabels(chart, series)) {
                var label = FormatValue(chart, start.Y) + "-" + FormatValue(chart, end.Y);
                var fontSize = chart.Options.Theme.DataLabelFontSize;
                var labelX = x - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0;
                var labelY = Math.Min(yStart, yEnd) - radius - fontSize - 4;
                DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
            }
        }
    }
}
