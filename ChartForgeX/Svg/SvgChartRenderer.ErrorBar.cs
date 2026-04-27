using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawErrorBars(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var color = Color(chart, index);
        var itemCount = Math.Max(1, series.Points.Count / 3);
        var capWidth = Math.Max(9, Math.Min(24, plot.Width / Math.Max(1, itemCount * 8.0)));
        var radius = Math.Max(3.5, chart.Options.Theme.MarkerRadius + 0.25);

        for (var pointIndex = 0; pointIndex + 2 < series.Points.Count; pointIndex += 3) {
            var center = series.Points[pointIndex];
            var lower = series.Points[pointIndex + 1];
            var upper = series.Points[pointIndex + 2];
            var x = map.X(center.X);
            var y = map.Y(center.Y);
            var yLower = map.Y(lower.Y);
            var yUpper = map.Y(upper.Y);
            var item = pointIndex / 3;
            var summary = "value " + FormatValue(chart, center.Y) + ", range " + FormatValue(chart, lower.Y) + "-" + FormatValue(chart, upper.Y);

            sb.AppendLine($"<g data-cfx-role=\"error-bar\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" role=\"img\" aria-label=\"{Escape(summary)}\">");
            sb.AppendLine($"<line data-cfx-role=\"error-range\" x1=\"{F(x)}\" y1=\"{F(yUpper)}\" x2=\"{F(x)}\" y2=\"{F(yLower)}\" stroke=\"{color.ToCss()}\" stroke-width=\"2.2\" stroke-linecap=\"round\" opacity=\"0.72\"/>");
            sb.AppendLine($"<line data-cfx-role=\"error-cap\" x1=\"{F(x - capWidth / 2)}\" y1=\"{F(yUpper)}\" x2=\"{F(x + capWidth / 2)}\" y2=\"{F(yUpper)}\" stroke=\"{color.ToCss()}\" stroke-width=\"2.2\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<line data-cfx-role=\"error-cap\" x1=\"{F(x - capWidth / 2)}\" y1=\"{F(yLower)}\" x2=\"{F(x + capWidth / 2)}\" y2=\"{F(yLower)}\" stroke=\"{color.ToCss()}\" stroke-width=\"2.2\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<circle data-cfx-role=\"error-marker\" cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"{F(radius)}\" fill=\"{color.ToCss()}\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"2\"/>");
            sb.AppendLine("</g>");
            if (ShouldDrawDataLabels(chart, series)) DrawDataLabel(sb, chart, FormatValue(chart, center.Y), x, y - radius - 9, plot);
        }
    }
}
