using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawWordCloud(StringBuilder sb, Chart chart, ChartRect plot) {
        var terms = ChartWordCloudLayout.Compute(chart, plot);
        if (terms.Count == 0) return;
        var series = chart.Series.First(item => item.Kind == ChartSeriesKind.WordCloud);
        var t = chart.Options.Theme;
        var maximumTerms = chart.Options.WordCloudMaximumTerms.HasValue ? chart.Options.WordCloudMaximumTerms.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "all";
        sb.AppendLine($"<g data-cfx-role=\"word-cloud\" data-cfx-density=\"{F(chart.Options.WordCloudDensity)}\" data-cfx-maximum-terms=\"{maximumTerms}\">");
        for (var i = 0; i < terms.Count; i++) {
            var term = terms[i];
            var color = WordCloudTermColor(series, t, term.PointIndex, i);
            var transform = Math.Abs(term.Angle) < 0.001 ? string.Empty : $" transform=\"rotate({F(term.Angle)} {F(term.X)} {F(term.Y)})\"";
            var summary = term.Text + ": " + FormatValue(chart, term.Value);
            sb.AppendLine($"<text data-cfx-role=\"word-cloud-term\" data-cfx-point=\"{term.PointIndex}\" data-cfx-text=\"{Escape(term.Text)}\" data-cfx-value=\"{F(term.Value)}\" role=\"img\" aria-label=\"{Escape(summary)}\" x=\"{F(term.X)}\" y=\"{F(term.Y)}\" text-anchor=\"middle\" dominant-baseline=\"middle\" fill=\"{color.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(term.FontSize)}\" font-weight=\"800\"{transform}>{Escape(term.Text)}</text>");
        }

        sb.AppendLine("</g>");
    }

    private static ChartColor WordCloudTermColor(ChartSeries series, ChartTheme theme, int pointIndex, int renderIndex) {
        if (pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue) return series.PointColors[pointIndex]!.Value;
        return series.Color ?? theme.Palette[renderIndex % theme.Palette.Length];
    }
}
