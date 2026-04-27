using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawOhlc(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
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
            var item = pointIndex / 4;
            var summary = "open " + FormatValue(chart, open.Y) + ", high " + FormatValue(chart, high.Y) + ", low " + FormatValue(chart, low.Y) + ", close " + FormatValue(chart, close.Y);

            sb.AppendLine($"<g data-cfx-role=\"ohlc\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" role=\"img\" aria-label=\"{Escape(summary)}\">");
            sb.AppendLine($"<line data-cfx-role=\"ohlc-stem\" x1=\"{F(x)}\" y1=\"{F(yHigh)}\" x2=\"{F(x)}\" y2=\"{F(yLow)}\" stroke=\"{color.ToCss()}\" stroke-width=\"2.2\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<line data-cfx-role=\"ohlc-open\" x1=\"{F(x - tickWidth)}\" y1=\"{F(yOpen)}\" x2=\"{F(x)}\" y2=\"{F(yOpen)}\" stroke=\"{color.ToCss()}\" stroke-width=\"2.2\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<line data-cfx-role=\"ohlc-close\" x1=\"{F(x)}\" y1=\"{F(yClose)}\" x2=\"{F(x + tickWidth)}\" y2=\"{F(yClose)}\" stroke=\"{color.ToCss()}\" stroke-width=\"2.2\" stroke-linecap=\"round\"/>");
            sb.AppendLine("</g>");
            if (ShouldDrawDataLabels(chart, series)) DrawDataLabel(sb, chart, FormatValue(chart, close.Y), x + tickWidth + 4, yClose, plot);
        }
    }
}
