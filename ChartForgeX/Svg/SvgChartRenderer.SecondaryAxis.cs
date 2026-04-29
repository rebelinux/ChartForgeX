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
        var tickStyle = o.TickLabelStyle;
        var tickFontSize = StyleFontSize(tickStyle, t.TickLabelFontSize);
        foreach (var yv in yTicks) {
            var y = map.Y(yv);
            sb.AppendLine($"<text data-cfx-role=\"secondary-y-axis-tick\" data-cfx-value=\"{F(yv)}\" x=\"{F(plot.Right+12)}\" y=\"{F(y+4)}\" text-anchor=\"start\" fill=\"{StyleColor(tickStyle, t.MutedText).ToCss()}\" font-family=\"{SvgFontFamily(StyleFontFamily(chart, tickStyle))}\" font-size=\"{F(tickFontSize)}\"{SvgTextStyleAttributes(tickStyle)}>{Escape(FormatSecondaryValue(chart, yv))}</text>");
        }

        if (ShowAxisLines(chart)) sb.AppendLine($"<line data-cfx-role=\"secondary-y-axis\" x1=\"{F(plot.Right)}\" y1=\"{F(plot.Top)}\" x2=\"{F(plot.Right)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.AxisStrokeWidth)}\"/>");
        if (string.IsNullOrWhiteSpace(chart.SecondaryYAxisTitle)) return;
        var style = chart.Options.AxisTitleStyle;
        var fontSize = StyleFontSize(style, t.AxisTitleFontSize);
        var label = TrimSvgLabelToWidth(chart.SecondaryYAxisTitle, fontSize, Math.Max(40, plot.Height * 0.72));
        if (label.Length == 0) return;
        sb.AppendLine($"<text data-cfx-role=\"secondary-y-axis-title\" data-cfx-label=\"{Escape(chart.SecondaryYAxisTitle)}\" transform=\"translate({F(Math.Min(chart.Options.Size.Width - 18, plot.Right + 54))} {F(plot.Top + plot.Height / 2.0)}) rotate(90)\" text-anchor=\"middle\" fill=\"{StyleColor(style, t.MutedText).ToCss()}\" font-family=\"{SvgFontFamily(StyleFontFamily(chart, style))}\" font-size=\"{F(fontSize)}\" font-weight=\"{StyleWeight(style, "700")}\"{SvgTextStyleAttributes(style)}>{Escape(label)}</text>");
    }

    private static ChartRect ApplySecondaryYAxisLabelReserve(Chart chart, ChartRect plot, IReadOnlyList<double> yTicks) {
        if (!chart.Options.ShowAxes || chart.Options.IsSparkline || IsPieLike(chart) || yTicks.Count == 0) return plot;
        var t = chart.Options.Theme;
        var widest = yTicks.Max(tick => EstimateTextWidth(FormatSecondaryValue(chart, tick), StyleFontSize(chart.Options.TickLabelStyle, t.TickLabelFontSize)));
        var titleReserve = string.IsNullOrWhiteSpace(chart.SecondaryYAxisTitle) ? 0 : t.AxisTitleFontSize + 18;
        var reserve = Math.Min(150, widest + 30 + titleReserve);
        if (reserve <= 0) return plot;
        return new ChartRect(plot.X, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
    }
}
