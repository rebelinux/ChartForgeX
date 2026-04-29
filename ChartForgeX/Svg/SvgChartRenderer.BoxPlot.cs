using System;
using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawBoxPlots(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        var boxCount = Math.Max(1, series.Points.Count / 5);
        var boxWidth = Math.Max(14, Math.Min(46, plot.Width / Math.Max(1, boxCount * 5.0)));
        var capWidth = boxWidth * 0.74;
        var reservedLabels = new List<ChartLabelBounds>();

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
            var item = pointIndex / 5;
            var color = PointColor(chart, series, index, item);
            var summary = FormatValue(chart, minimum.Y) + "-" + FormatValue(chart, maximum.Y) + ", median " + FormatValue(chart, median.Y);

            sb.AppendLine($"<g data-cfx-role=\"box-plot\" data-cfx-series=\"{index}\" data-cfx-point=\"{item}\" data-cfx-x=\"{F(minimum.X)}\" data-cfx-min=\"{F(minimum.Y)}\" data-cfx-q1=\"{F(q1.Y)}\" data-cfx-median=\"{F(median.Y)}\" data-cfx-q3=\"{F(q3.Y)}\" data-cfx-max=\"{F(maximum.Y)}\" role=\"img\" aria-label=\"{Escape(summary)}\">");
            sb.AppendLine($"<title>{Escape(summary)}</title>");
            sb.AppendLine($"<line data-cfx-role=\"box-whisker\" x1=\"{F(x)}\" y1=\"{F(yMax)}\" x2=\"{F(x)}\" y2=\"{F(yMin)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.BoxPlotStrokeWidth)}\" stroke-linecap=\"round\" opacity=\"{F(ChartVisualPrimitives.BoxPlotWhiskerOpacity)}\"/>");
            sb.AppendLine($"<line data-cfx-role=\"box-cap\" x1=\"{F(x - capWidth / 2)}\" y1=\"{F(yMin)}\" x2=\"{F(x + capWidth / 2)}\" y2=\"{F(yMin)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.BoxPlotStrokeWidth)}\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<line data-cfx-role=\"box-cap\" x1=\"{F(x - capWidth / 2)}\" y1=\"{F(yMax)}\" x2=\"{F(x + capWidth / 2)}\" y2=\"{F(yMax)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.BoxPlotStrokeWidth)}\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<rect data-cfx-role=\"box-body\" x=\"{F(x - boxWidth / 2)}\" y=\"{F(top)}\" width=\"{F(boxWidth)}\" height=\"{F(height)}\" rx=\"{F(ChartVisualPrimitives.BoxPlotBodyRadius)}\" fill=\"{color.ToCss()}\" opacity=\"{F(ChartVisualPrimitives.BoxPlotBodyFillOpacity)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.BoxPlotStrokeWidth)}\"/>");
            sb.AppendLine($"<line data-cfx-role=\"box-median\" x1=\"{F(x - boxWidth / 2)}\" y1=\"{F(yMedian)}\" x2=\"{F(x + boxWidth / 2)}\" y2=\"{F(yMedian)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.BoxPlotMedianStrokeWidth)}\" stroke-linecap=\"round\"/>");
            sb.AppendLine("</g>");
            var label = FormatValue(chart, median.Y);
            if (ShouldDrawDataLabels(chart, series)) {
                var placement = DataLabelPlacement(chart, series);
                if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
                    var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
                    var labelX = placement == ChartDataLabelPlacement.Left ? x - boxWidth / 2 - 8 : x + boxWidth / 2 + 8;
                    if (ReserveSvgHorizontalLabel(label, labelX, yMedian, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(sb, chart, label, labelX, yMedian, anchor, plot, series, item);
                } else {
                    var labelY = placement == ChartDataLabelPlacement.Below
                        ? Math.Max(yQ1, yQ3) + 11
                        : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                            ? yMedian
                            : Math.Min(yQ1, yQ3) - 11;
                    if (ReserveSvgLabel(label, x, labelY, chart, plot, reservedLabels)) DrawDataLabel(sb, chart, label, x, labelY, plot, series: series, pointIndex: item);
                }
            }
        }
    }
}
