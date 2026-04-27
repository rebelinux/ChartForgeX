using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Svg;

namespace ChartForgeX.Html;

/// <summary>
/// Renders dependency-free small-multiple chart grids as static HTML.
/// </summary>
public sealed class HtmlChartGridRenderer {
    private readonly SvgChartRenderer _svg = new();

    /// <summary>
    /// Renders a chart grid as an embeddable HTML fragment.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>An HTML fragment containing inline SVG charts.</returns>
    public string RenderFragment(ChartGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (grid.Charts.Count == 0) throw new InvalidOperationException("Chart grids must contain at least one chart.");

        var sb = new StringBuilder();
        sb.Append("<section class=\"chartforgex-grid");
        if (grid.PanelFit == ChartGridPanelFit.Stretch) sb.Append(" fit-stretch");
        sb.Append("\" style=\"--cfx-grid-columns:");
        sb.Append(grid.Columns.ToString(CultureInfo.InvariantCulture));
        sb.Append(";--cfx-grid-gap:");
        sb.Append(grid.Gap.ToString(CultureInfo.InvariantCulture));
        sb.Append("px;--cfx-grid-padding:");
        sb.Append(grid.Padding.ToString(CultureInfo.InvariantCulture));
        sb.Append("px");
        if (grid.PanelSize.HasValue) {
            sb.Append(";--cfx-grid-panel-width:");
            sb.Append(grid.PanelSize.Value.Width.ToString(CultureInfo.InvariantCulture));
            sb.Append("px;--cfx-grid-panel-height:");
            sb.Append(grid.PanelSize.Value.Height.ToString(CultureInfo.InvariantCulture));
            sb.Append("px");
        }

        sb.Append("\">");
        if (grid.Title.Length > 0 || grid.Subtitle.Length > 0) {
            sb.Append("<header class=\"chartforgex-grid-header\">");
            if (grid.Title.Length > 0) sb.Append("<h1>").Append(Escape(grid.Title)).Append("</h1>");
            if (grid.Subtitle.Length > 0) sb.Append("<p>").Append(Escape(grid.Subtitle)).Append("</p>");
            sb.Append("</header>");
        }

        sb.Append("<div class=\"chartforgex-grid-body\">");
        foreach (var chart in grid.Charts) {
            sb.Append("<article class=\"chartforgex-grid-panel\" aria-label=\"");
            sb.Append(Escape(AttributeTitle(chart)));
            sb.Append("\">");
            sb.Append(_svg.Render(chart));
            sb.Append("</article>");
        }

        sb.Append("</div></section>");
        return sb.ToString();
    }

    /// <summary>
    /// Renders a chart grid as a complete HTML document.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>A complete HTML document.</returns>
    public string RenderPage(ChartGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (grid.Charts.Count == 0) throw new InvalidOperationException("Chart grids must contain at least one chart.");
        var title = grid.Title.Length == 0 ? "ChartForgeX report" : grid.Title;
        var theme = grid.Theme ?? grid.Charts[0].Options.Theme;
        var bg = theme.Background.A == 0 ? theme.CardBackground.ToCss() : theme.Background.ToCss();
        var fontFamily = CssFontFamily(theme.FontFamily);
        return "<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<title>" + Escape(title) + "</title>\n<style>" + BuildCss(bg, theme.Text.ToCss(), theme.MutedText.ToCss(), fontFamily) + "</style>\n</head>\n<body>\n" + RenderFragment(grid) + "\n</body>\n</html>";
    }

    private static string BuildCss(string background, string text, string mutedText, string fontFamily) {
        return "body{margin:0;min-height:100vh;background:" + background + ";font-family:" + fontFamily + ";padding:var(--cfx-grid-padding,24px);box-sizing:border-box;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}.chartforgex-grid{display:block;width:min(100%,1440px);margin:0 auto}.chartforgex-grid-header{margin:0 0 18px}.chartforgex-grid-header h1{margin:0;color:" + text + ";font-size:26px;line-height:1.15;font-weight:800}.chartforgex-grid-header p{margin:6px 0 0;color:" + mutedText + ";font-size:14px;line-height:1.45}.chartforgex-grid-body{display:grid;grid-template-columns:repeat(var(--cfx-grid-columns),minmax(0,1fr));gap:var(--cfx-grid-gap)}.chartforgex-grid-panel{min-width:0;width:var(--cfx-grid-panel-width,auto);height:var(--cfx-grid-panel-height,auto);display:grid;place-items:center;overflow:hidden}.chartforgex-grid-panel svg{width:auto;height:auto;max-width:100%;max-height:100%;display:block}.chartforgex-grid.fit-stretch .chartforgex-grid-panel svg{width:100%;height:100%;max-width:none;max-height:none}@media(max-width:900px){body{padding:16px}.chartforgex-grid-body{grid-template-columns:1fr}.chartforgex-grid-header h1{font-size:22px}}";
    }

    private static string AttributeTitle(Chart chart) {
        if (chart.Title.Length > 0) return chart.Title;
        return chart.Series.Count == 0 ? "Chart" : chart.Series[0].Name;
    }

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string CssFontFamily(string value) {
        if (string.IsNullOrWhiteSpace(value)) return "system-ui, sans-serif";
        return value.Replace(";", " ").Replace("{", " ").Replace("}", " ").Replace("<", " ").Replace(">", " ");
    }
}
