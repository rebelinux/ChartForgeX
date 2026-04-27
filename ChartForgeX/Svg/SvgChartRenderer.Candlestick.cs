using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawCandlesticks(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
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
            var item = pointIndex / 4;
            var summary = "open " + FormatValue(chart, open.Y) + ", high " + FormatValue(chart, high.Y) + ", low " + FormatValue(chart, low.Y) + ", close " + FormatValue(chart, close.Y);
            var fillOpacity = rising ? "0.22" : "0.82";

            sb.AppendLine($"<g data-cfx-role=\"candlestick\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" role=\"img\" aria-label=\"{Escape(summary)}\">");
            sb.AppendLine($"<line data-cfx-role=\"candlestick-wick\" x1=\"{F(x)}\" y1=\"{F(yHigh)}\" x2=\"{F(x)}\" y2=\"{F(yLow)}\" stroke=\"{color.ToCss()}\" stroke-width=\"2\" stroke-linecap=\"round\" opacity=\"0.86\"/>");
            sb.AppendLine($"<rect data-cfx-role=\"candlestick-body\" x=\"{F(x - candleWidth / 2)}\" y=\"{F(bodyTop)}\" width=\"{F(candleWidth)}\" height=\"{F(bodyHeight)}\" rx=\"2.5\" fill=\"{color.ToCss()}\" fill-opacity=\"{fillOpacity}\" stroke=\"{color.ToCss()}\" stroke-width=\"2\"/>");
            sb.AppendLine("</g>");
            if (ShouldDrawDataLabels(chart, series)) DrawDataLabel(sb, chart, FormatValue(chart, close.Y), x, Math.Min(yOpen, yClose) - 11, plot);
        }
    }
}
