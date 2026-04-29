using System;
using System.Collections.Generic;
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
            var summary = "open " + FormatValue(chart, open.Y) + ", high " + FormatValue(chart, high.Y) + ", low " + FormatValue(chart, low.Y) + ", close " + FormatValue(chart, close.Y);
            var status = rising ? "rising" : "falling";

            sb.AppendLine($"<g data-cfx-role=\"ohlc\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-x=\"{F(open.X)}\" data-cfx-open=\"{F(open.Y)}\" data-cfx-high=\"{F(high.Y)}\" data-cfx-low=\"{F(low.Y)}\" data-cfx-close=\"{F(close.Y)}\" data-cfx-status=\"{status}\" role=\"img\" aria-label=\"{Escape(summary)}\">");
            sb.AppendLine($"<title>{Escape(summary)}</title>");
            sb.AppendLine($"<line data-cfx-role=\"ohlc-stem\" x1=\"{F(x)}\" y1=\"{F(yHigh)}\" x2=\"{F(x)}\" y2=\"{F(yLow)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.OhlcStrokeWidth)}\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<line data-cfx-role=\"ohlc-open\" x1=\"{F(x - tickWidth)}\" y1=\"{F(yOpen)}\" x2=\"{F(x)}\" y2=\"{F(yOpen)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.OhlcStrokeWidth)}\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<line data-cfx-role=\"ohlc-close\" x1=\"{F(x)}\" y1=\"{F(yClose)}\" x2=\"{F(x + tickWidth)}\" y2=\"{F(yClose)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.OhlcStrokeWidth)}\" stroke-linecap=\"round\"/>");
            sb.AppendLine("</g>");
            var label = FormatValue(chart, close.Y);
            if (ShouldDrawDataLabels(chart, series)) {
                var placement = DataLabelPlacement(chart, series);
                if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right || placement == ChartDataLabelPlacement.Auto || placement == ChartDataLabelPlacement.Outside) {
                    var left = placement == ChartDataLabelPlacement.Left;
                    var anchor = left ? "end" : "start";
                    var labelX = left ? x - tickWidth - ChartVisualPrimitives.OhlcLabelOffset : x + tickWidth + ChartVisualPrimitives.OhlcLabelOffset;
                    if (ReserveSvgHorizontalLabel(label, labelX, yClose, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, yClose, anchor, plot, series, item);
                } else {
                    var labelY = placement == ChartDataLabelPlacement.Below
                        ? yClose + 11
                        : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                            ? yClose
                            : yClose - 11;
                    if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: item);
                }
            }
        }
    }
}
