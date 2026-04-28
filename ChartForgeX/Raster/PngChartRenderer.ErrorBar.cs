using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawErrorBars(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var color = series.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        var itemCount = Math.Max(1, series.Points.Count / 3);
        var capWidth = Math.Max(9, Math.Min(24, plot.Width / Math.Max(1, itemCount * 8.0)));
        var radius = Math.Max(ChartVisualPrimitives.ErrorBarMarkerMinRadius, chart.Options.Theme.MarkerRadius + ChartVisualPrimitives.ErrorBarMarkerRadiusExtra);
        var reserved = new List<ChartLabelBounds>();

        for (var pointIndex = 0; pointIndex + 2 < series.Points.Count; pointIndex += 3) {
            var center = series.Points[pointIndex];
            var lower = series.Points[pointIndex + 1];
            var upper = series.Points[pointIndex + 2];
            var x = map.X(center.X);
            var y = map.Y(center.Y);
            var yLower = map.Y(lower.Y);
            var yUpper = map.Y(upper.Y);
            var halo = PngStrokeHalo(color);

            c.DrawLine(x, yUpper, x, yLower, halo, ChartVisualPrimitives.ErrorBarPngHaloStrokeWidth);
            c.DrawLine(x - capWidth / 2, yUpper, x + capWidth / 2, yUpper, halo, ChartVisualPrimitives.ErrorBarPngHaloStrokeWidth);
            c.DrawLine(x - capWidth / 2, yLower, x + capWidth / 2, yLower, halo, ChartVisualPrimitives.ErrorBarPngHaloStrokeWidth);
            c.DrawLine(x, yUpper, x, yLower, color, ChartVisualPrimitives.ErrorBarStrokeWidth);
            c.DrawLine(x - capWidth / 2, yUpper, x + capWidth / 2, yUpper, color, ChartVisualPrimitives.ErrorBarStrokeWidth);
            c.DrawLine(x - capWidth / 2, yLower, x + capWidth / 2, yLower, color, ChartVisualPrimitives.ErrorBarStrokeWidth);
            DrawMarker(c, chart, x, y, radius, color);
            if (ShouldDrawDataLabels(chart, series)) {
                var label = FormatValue(chart, center.Y);
                var fontSize = chart.Options.Theme.DataLabelFontSize;
                var labelX = x - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0;
                var aboveY = y - radius - fontSize - 5;
                var belowY = y + radius + 5;
                var labelY = aboveY < plot.Top + 2 ? belowY : aboveY;
                if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reserved)) continue;
                DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
            }
        }
    }
}
