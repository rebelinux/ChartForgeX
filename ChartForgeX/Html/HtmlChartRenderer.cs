using System;
using System.Globalization;
using System.Threading;
using ChartForgeX.Core;
using ChartForgeX.Svg;

namespace ChartForgeX.Html;

/// <summary>
/// Renders charts as dependency-free HTML with inline SVG.
/// </summary>
public sealed class HtmlChartRenderer {
    private static long ScopeCounter;
    private readonly SvgChartRenderer _svg = new();

    /// <summary>
    /// Renders a chart as an embeddable HTML fragment.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>An HTML fragment containing inline SVG.</returns>
    public string RenderFragment(Chart chart) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        return "<div class=\"chartforgex-chart\" style=\"width:100%;max-width:" + chart.Options.Size.Width + "px;box-sizing:border-box\">" + _svg.Render(chart, NextScope()) + "</div>";
    }

    /// <summary>
    /// Renders a chart as a complete HTML document.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>A complete HTML document.</returns>
    public string RenderPage(Chart chart) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var bg = chart.Options.TransparentBackground ? chart.Options.Theme.CardBackground.ToCss() : chart.Options.Theme.Background.ToCss();
        var title = string.IsNullOrWhiteSpace(chart.Title) ? "ChartForgeX chart" : chart.Title;
        return "<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<title>" + Escape(title) + "</title>\n<style>body{margin:0;min-height:100vh;display:grid;place-items:start center;background:" + bg + ";font-family:" + CssFontFamily(chart.Options.Theme.FontFamily) + ";padding:clamp(20px,5vh,48px) 24px 24px;box-sizing:border-box;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}.chartforgex-chart svg{max-width:100%;height:auto;display:block}@media(max-width:680px){body{padding:16px}}</style>\n</head>\n<body>\n" + RenderFragment(chart) + "\n</body>\n</html>";
    }

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string NextScope() {
        var value = Interlocked.Increment(ref ScopeCounter);
        return "html-chart-fragment-" + value.ToString(CultureInfo.InvariantCulture);
    }

    private static string CssFontFamily(string value) {
        if (string.IsNullOrWhiteSpace(value)) return "system-ui, sans-serif";
        return value.Replace(";", " ").Replace("{", " ").Replace("}", " ").Replace("<", " ").Replace(">", " ");
    }
}
