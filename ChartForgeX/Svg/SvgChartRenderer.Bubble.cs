using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawBubbles(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var color = Color(chart, index);
        var range = BubbleSizeRange(series);
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 1 < series.Points.Count; pointIndex += 2) {
            var center = series.Points[pointIndex];
            var size = series.Points[pointIndex + 1].Y;
            var x = map.X(center.X);
            var y = map.Y(center.Y);
            var radius = BubbleRadius(size, range.min, range.max, plot);
            var item = pointIndex / 2;
            var summary = "x " + FormatValue(chart, center.X) + ", y " + FormatValue(chart, center.Y) + ", size " + FormatValue(chart, size);

            sb.AppendLine($"<circle data-cfx-role=\"bubble\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-x=\"{F(center.X)}\" data-cfx-y=\"{F(center.Y)}\" data-cfx-size=\"{F(size)}\" cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"{F(radius)}\" fill=\"{color.ToCss()}\" fill-opacity=\"0.30\" stroke=\"{color.ToCss()}\" stroke-opacity=\"0.88\" stroke-width=\"{F(ChartVisualPrimitives.BubbleStrokeWidth)}\" role=\"img\" aria-label=\"{Escape(summary)}\"/>");
            sb.AppendLine($"<circle data-cfx-role=\"bubble-highlight\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-size=\"{F(size)}\" cx=\"{F(x - radius * 0.28)}\" cy=\"{F(y - radius * 0.28)}\" r=\"{F(Math.Max(1.4, radius * 0.18))}\" fill=\"{chart.Options.Theme.CardBackground.ToCss()}\" opacity=\"0.26\"/>");
            var label = FormatValue(chart, size);
            var labelY = y - radius - 12;
            if (ShouldDrawDataLabels(chart, series) && ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot);
        }
    }

    private static (double min, double max) BubbleSizeRange(ChartSeries series) {
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        for (var i = 1; i < series.Points.Count; i += 2) {
            var size = series.Points[i].Y;
            if (size < min) min = size;
            if (size > max) max = size;
        }

        if (double.IsInfinity(min)) return (1, 1);
        return (min, max);
    }

    private static double BubbleRadius(double size, double min, double max, ChartRect plot) {
        var minimumRadius = 6.0;
        var maximumRadius = Math.Min(32, Math.Max(14, Math.Min(plot.Width, plot.Height) * 0.075));
        if (Math.Abs(max - min) < double.Epsilon) return (minimumRadius + maximumRadius) / 2;
        var normalizedArea = (size - min) / (max - min);
        return minimumRadius + Math.Sqrt(Math.Max(0, Math.Min(1, normalizedArea))) * (maximumRadius - minimumRadius);
    }
}
