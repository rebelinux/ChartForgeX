using ChartForgeX.Core;
using ChartForgeX.Svg;

namespace ChartForgeX.Html;

public sealed class HtmlChartRenderer {
    private readonly SvgChartRenderer _svg = new();

    public string RenderFragment(Chart chart) {
        return "<div class=\"chartforgex-chart\">" + _svg.Render(chart) + "</div>";
    }

    public string RenderPage(Chart chart) {
        var bg = chart.Options.TransparentBackground ? "transparent" : chart.Options.Theme.Background.ToCss();
        return "<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<title>" + Escape(chart.Title) + "</title>\n<style>body{margin:0;min-height:100vh;display:grid;place-items:center;background:" + bg + ";font-family:" + chart.Options.Theme.FontFamily + ";}.chartforgex-chart{max-width:100%;padding:24px;box-sizing:border-box}.chartforgex-chart svg{max-width:100%;height:auto;display:block}</style>\n</head>\n<body>\n" + RenderFragment(chart) + "\n</body>\n</html>";
    }

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
