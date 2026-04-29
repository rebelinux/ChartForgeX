using System;
using System.Collections.Generic;
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
        var reservedLabels = new List<ChartLabelBounds>();
        for (var pointIndex = 0; pointIndex + 3 < series.Points.Count; pointIndex += 4) {
            var open = series.Points[pointIndex];
            var high = series.Points[pointIndex + 1];
            var low = series.Points[pointIndex + 2];
            var close = series.Points[pointIndex + 3];
            var rising = close.Y >= open.Y;
            var item = pointIndex / 4;
            var semanticColor = rising ? chart.Options.Theme.Positive : chart.Options.Theme.Negative;
            var color = item < series.PointColors.Count && series.PointColors[item] is { } pointColor ? pointColor : semanticColor;
            var x = map.X(open.X);
            var yOpen = map.Y(open.Y);
            var yHigh = map.Y(high.Y);
            var yLow = map.Y(low.Y);
            var yClose = map.Y(close.Y);
            var bodyTop = Math.Min(yOpen, yClose);
            var bodyHeight = Math.Max(2, Math.Abs(yClose - yOpen));
            var summary = "open " + FormatValue(chart, open.Y) + ", high " + FormatValue(chart, high.Y) + ", low " + FormatValue(chart, low.Y) + ", close " + FormatValue(chart, close.Y);
            var fillOpacity = rising ? ChartVisualPrimitives.CandlestickRisingFillOpacity : ChartVisualPrimitives.CandlestickFallingFillOpacity;
            var status = rising ? "rising" : "falling";

            sb.AppendLine($"<g data-cfx-role=\"candlestick\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-x=\"{F(open.X)}\" data-cfx-open=\"{F(open.Y)}\" data-cfx-high=\"{F(high.Y)}\" data-cfx-low=\"{F(low.Y)}\" data-cfx-close=\"{F(close.Y)}\" data-cfx-status=\"{status}\" role=\"img\" aria-label=\"{Escape(summary)}\">");
            sb.AppendLine($"<title>{Escape(summary)}</title>");
            sb.AppendLine($"<line data-cfx-role=\"candlestick-wick\" x1=\"{F(x)}\" y1=\"{F(yHigh)}\" x2=\"{F(x)}\" y2=\"{F(yLow)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.CandlestickStrokeWidth)}\" stroke-linecap=\"round\" opacity=\"{F(ChartVisualPrimitives.CandlestickWickOpacity)}\"/>");
            sb.AppendLine($"<rect data-cfx-role=\"candlestick-body\" x=\"{F(x - candleWidth / 2)}\" y=\"{F(bodyTop)}\" width=\"{F(candleWidth)}\" height=\"{F(bodyHeight)}\" rx=\"{F(ChartVisualPrimitives.CandlestickBodyRadius)}\" fill=\"{color.ToCss()}\" fill-opacity=\"{F(fillOpacity)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.CandlestickStrokeWidth)}\"/>");
            sb.AppendLine("</g>");
            var label = FormatValue(chart, close.Y);
            if (ShouldDrawDataLabels(chart, series)) {
                var placement = DataLabelPlacement(chart, series);
                if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
                    var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
                    var labelX = placement == ChartDataLabelPlacement.Left ? x - candleWidth / 2 - 8 : x + candleWidth / 2 + 8;
                    if (ReserveSvgHorizontalLabel(label, labelX, bodyTop + bodyHeight / 2, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, bodyTop + bodyHeight / 2, anchor, plot, series, item);
                } else {
                    var aboveY = bodyTop - chart.Options.Theme.DataLabelFontSize - 4;
                    var belowY = Math.Max(yOpen, yClose) + 5;
                    var labelY = placement == ChartDataLabelPlacement.Below
                        ? belowY
                        : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                            ? bodyTop + bodyHeight / 2
                            : aboveY < plot.Top + 2 ? belowY : aboveY;
                    if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: item);
                }
            }
        }
    }
}
