using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawHorizontalBars(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartMapper map, string id) {
        var s = chart.Series[index];
        var layout = HorizontalBarLayout(chart, plot, index);
        var zeroX = Math.Min(plot.Right, Math.Max(plot.Left, map.X(0)));
        for (var pointIndex = 0; pointIndex < s.Points.Count; pointIndex++) {
            var p = s.Points[pointIndex];
            var baseValue = chart.Options.BarMode == ChartBarMode.Stacked ? StackHorizontalBaseValue(chart, index, p) : 0;
            var baseX = chart.Options.BarMode == ChartBarMode.Stacked ? map.X(baseValue) : zeroX;
            var valueX = map.X(baseValue + p.Y);
            var left = Math.Min(baseX, valueX);
            var width = Math.Abs(valueX - baseX);
            var y = map.Y(p.X) + layout.Offset - layout.BarHeight / 2;
            var radius = chart.Options.BarMode == ChartBarMode.Stacked ? Math.Min(3, layout.BarHeight / 2) : Math.Min(7, layout.BarHeight / 2);
            sb.AppendLine($"<rect data-cfx-role=\"horizontal-bar\" data-cfx-series=\"{index}\" data-cfx-point=\"{pointIndex}\" x=\"{F(left)}\" y=\"{F(y)}\" width=\"{F(width)}\" height=\"{F(layout.BarHeight)}\" rx=\"{F(radius)}\" fill=\"url(#{id}-seriesFill{index})\" opacity=\"0.94\"/>");
            if (ShouldDrawDataLabels(chart, s)) {
                var label = FormatValue(chart, p.Y);
                if (chart.Options.BarMode == ChartBarMode.Stacked) {
                    if (width < EstimateTextWidth(label, chart.Options.Theme.DataLabelFontSize) + 8) continue;
                    DrawDataLabel(sb, chart, label, left + width / 2, y + layout.BarHeight / 2, plot);
                } else {
                    var labelX = p.Y >= 0 ? left + width + 8 : left - 8;
                    var anchor = p.Y >= 0 ? "start" : "end";
                    DrawHorizontalValueLabel(sb, chart, label, labelX, y + layout.BarHeight / 2, anchor, plot);
                }
            }
        }
    }

    private static HorizontalBarLayoutInfo HorizontalBarLayout(Chart chart, ChartRect plot, int seriesIndex) {
        var horizontalSeries = chart.Series
            .Select((series, index) => new { series, index })
            .Where(item => item.series.Kind == ChartSeriesKind.HorizontalBar)
            .Select(item => item.index)
            .ToArray();
        var groupCount = chart.Options.BarMode == ChartBarMode.Stacked ? 1 : Math.Max(1, horizontalSeries.Length);
        var groupPosition = chart.Options.BarMode == ChartBarMode.Stacked ? 0 : Math.Max(0, Array.IndexOf(horizontalSeries, seriesIndex));
        var categoryValues = new HashSet<double>();
        foreach (var index in horizontalSeries) {
            foreach (var point in chart.Series[index].Points) categoryValues.Add(point.X);
        }

        var categoryCount = Math.Max(1, categoryValues.Count);
        var slotHeight = plot.Height / categoryCount;
        var groupHeight = slotHeight * (groupCount == 1 ? 0.56 : 0.76);
        var gap = groupCount == 1 ? 0 : Math.Min(4, groupHeight * 0.08);
        var barHeight = Math.Max(3, Math.Min(30, (groupHeight - gap * (groupCount - 1)) / groupCount));
        var offset = (groupPosition - (groupCount - 1) / 2.0) * (barHeight + gap);
        return new HorizontalBarLayoutInfo(barHeight, offset);
    }

    private static double StackHorizontalBaseValue(Chart chart, int seriesIndex, ChartPoint point) {
        var sum = 0.0;
        for (var i = 0; i < seriesIndex; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var candidate in series.Points) {
                if (Math.Abs(candidate.X - point.X) >= 0.000001) continue;
                if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
                break;
            }
        }

        return sum;
    }

    private static void DrawHorizontalStackTotals(StringBuilder sb, Chart chart, ChartRect plot, ChartMapper map) {
        var positiveTotals = new Dictionary<double, double>();
        var negativeTotals = new Dictionary<double, double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
        }

        DrawHorizontalStackTotalSet(sb, chart, positiveTotals, plot, map, 8, "start");
        DrawHorizontalStackTotalSet(sb, chart, negativeTotals, plot, map, -8, "end");
    }

    private static void DrawHorizontalStackTotalSet(StringBuilder sb, Chart chart, Dictionary<double, double> totals, ChartRect plot, ChartMapper map, double offset, string anchor) {
        foreach (var item in totals.OrderBy(item => item.Key)) {
            if (Math.Abs(item.Value) < 0.000001) continue;
            DrawHorizontalValueLabel(sb, chart, FormatValue(chart, item.Value), map.X(item.Value) + offset, map.Y(item.Key), anchor, plot);
        }
    }
}
