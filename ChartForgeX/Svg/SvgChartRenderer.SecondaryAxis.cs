using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static bool HasSecondaryYAxis(Chart chart) => chart.Series.Any(series => series.YAxis == ChartAxisSide.Secondary);

    private static ChartMapper SeriesMap(ChartSeries series, ChartMapper primaryMap, ChartMapper? secondaryMap) =>
        series.YAxis == ChartAxisSide.Secondary && secondaryMap != null ? secondaryMap : primaryMap;

    private static void DrawSecondaryYAxis(StringBuilder sb, Chart chart, ChartRect plot, IReadOnlyList<double> yTicks, ChartMapper map) {
        var o = chart.Options;
        if (!o.ShowAxes) return;
        var t = o.Theme;
        foreach (var yv in yTicks) {
            var y = map.Y(yv);
            sb.AppendLine($"<text data-cfx-role=\"secondary-y-axis-tick\" data-cfx-value=\"{F(yv)}\" x=\"{F(plot.Right+12)}\" y=\"{F(y+4)}\" text-anchor=\"start\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(FormatSecondaryValue(chart, yv))}</text>");
        }

        sb.AppendLine($"<line data-cfx-role=\"secondary-y-axis\" x1=\"{F(plot.Right)}\" y1=\"{F(plot.Top)}\" x2=\"{F(plot.Right)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.AxisStrokeWidth)}\"/>");
        if (string.IsNullOrWhiteSpace(chart.SecondaryYAxisTitle)) return;
        var fontSize = t.AxisTitleFontSize;
        var label = TrimSvgLabelToWidth(chart.SecondaryYAxisTitle, fontSize, Math.Max(40, plot.Height * 0.72));
        if (label.Length == 0) return;
        sb.AppendLine($"<text data-cfx-role=\"secondary-y-axis-title\" data-cfx-label=\"{Escape(chart.SecondaryYAxisTitle)}\" transform=\"translate({F(Math.Min(chart.Options.Size.Width - 18, plot.Right + 54))} {F(plot.Top + plot.Height / 2.0)}) rotate(90)\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(fontSize)}\" font-weight=\"700\">{Escape(label)}</text>");
    }

    private static ChartRect ApplySecondaryYAxisLabelReserve(Chart chart, ChartRect plot, IReadOnlyList<double> yTicks) {
        if (!chart.Options.ShowAxes || chart.Options.IsSparkline || IsPieLike(chart) || yTicks.Count == 0) return plot;
        var t = chart.Options.Theme;
        var widest = yTicks.Max(tick => EstimateTextWidth(FormatSecondaryValue(chart, tick), t.TickLabelFontSize));
        var titleReserve = string.IsNullOrWhiteSpace(chart.SecondaryYAxisTitle) ? 0 : t.AxisTitleFontSize + 18;
        var reserve = Math.Min(150, widest + 30 + titleReserve);
        if (reserve <= 0) return plot;
        return new ChartRect(plot.X, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
    }
}
