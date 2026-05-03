using System;
using System.Globalization;
using System.Text;
using System.Threading;
using ChartForgeX.Core;
using ChartForgeX.Svg;

namespace ChartForgeX.Html;

/// <summary>
/// Renders dependency-free small-multiple chart grids as static HTML.
/// </summary>
public sealed class HtmlChartGridRenderer {
    private static long ScopeCounter;
    private readonly SvgChartRenderer _svg = new();

    /// <summary>
    /// Renders a chart grid as an embeddable HTML fragment.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>An HTML fragment containing inline SVG charts.</returns>
    public string RenderFragment(ChartGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (grid.Charts.Count == 0) throw new InvalidOperationException("Chart grids must contain at least one chart.");
        var gridScope = NextScope();
        var theme = grid.Theme ?? grid.Charts[0].Options.Theme;

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
            if (grid.Title.Length > 0) sb.Append("<h1 style=\"").Append(GridTextStyle(grid.TitleStyle, theme.Text.ToCss(), CssFontFamily(theme.FontFamily), theme.TitleFontSize, "800")).Append("\">").Append(Escape(grid.Title)).Append("</h1>");
            if (grid.Subtitle.Length > 0) sb.Append("<p style=\"").Append(GridTextStyle(grid.SubtitleStyle, theme.MutedText.ToCss(), CssFontFamily(theme.FontFamily), theme.SubtitleFontSize, "400")).Append("\">").Append(Escape(grid.Subtitle)).Append("</p>");
            sb.Append("</header>");
        }

        sb.Append("<div class=\"chartforgex-grid-body\">");
        for (var i = 0; i < grid.Charts.Count; i++) {
            var chart = grid.Charts[i];
            var span = i < grid.PanelSpans.Count ? grid.PanelSpans[i] : new ChartGridPanelSpan(1, 1);
            var columnSpan = Math.Min(span.ColumnSpan, grid.Columns);
            var rowSpan = span.RowSpan;
            sb.Append("<article class=\"chartforgex-grid-panel\" aria-label=\"");
            sb.Append(Escape(AttributeTitle(chart)));
            sb.Append("\"");
            AppendPanelSpanStyle(sb, columnSpan, rowSpan, grid.PanelSize.HasValue);
            sb.Append(">");
            sb.Append(_svg.Render(chart, gridScope + "-cell-" + i.ToString(CultureInfo.InvariantCulture)));
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
        return "<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n<title>" + Escape(title) + "</title>\n<style>" + BuildCss(bg, theme.Text.ToCss(), theme.MutedText.ToCss(), fontFamily, theme.TitleFontSize, theme.SubtitleFontSize) + "</style>\n</head>\n<body>\n" + RenderFragment(grid) + "\n</body>\n</html>";
    }

    private static string BuildCss(string background, string text, string mutedText, string fontFamily, double titleFontSize, double subtitleFontSize) {
        return "body{margin:0;min-height:100vh;background:" + background + ";font-family:" + fontFamily + ";padding:var(--cfx-grid-padding,24px);box-sizing:border-box;-webkit-font-smoothing:antialiased;text-rendering:geometricPrecision}.chartforgex-grid{display:block;width:min(100%,1440px);margin:0 auto}.chartforgex-grid-header{margin:0 0 18px}.chartforgex-grid-header h1{margin:0;color:" + text + ";font-size:" + titleFontSize.ToString(CultureInfo.InvariantCulture) + "px;line-height:1.15;font-weight:800}.chartforgex-grid-header p{margin:6px 0 0;color:" + mutedText + ";font-size:" + subtitleFontSize.ToString(CultureInfo.InvariantCulture) + "px;line-height:1.45}.chartforgex-grid-body{display:grid;grid-template-columns:repeat(var(--cfx-grid-columns),minmax(0,1fr));grid-auto-rows:var(--cfx-grid-panel-height,auto);gap:var(--cfx-grid-gap)}.chartforgex-grid-panel{min-width:0;width:100%;min-height:var(--cfx-grid-panel-height,auto);display:grid;place-items:center;overflow:hidden}.chartforgex-grid-panel svg{width:auto;height:auto;max-width:100%;max-height:100%;display:block}.chartforgex-grid.fit-stretch .chartforgex-grid-panel svg{width:100%;height:100%;max-width:none;max-height:none}@media(max-width:900px){body{padding:16px}.chartforgex-grid-body{grid-template-columns:1fr;grid-auto-rows:auto}.chartforgex-grid-panel{grid-column:auto!important;grid-row:auto!important;min-height:0}.chartforgex-grid-header h1{font-size:" + Math.Max(18, titleFontSize * 0.85).ToString(CultureInfo.InvariantCulture) + "px}}";
    }

    private static void AppendPanelSpanStyle(StringBuilder sb, int columnSpan, int rowSpan, bool hasFixedPanelSize) {
        if (columnSpan == 1 && rowSpan == 1) return;
        sb.Append(" style=\"grid-column:span ");
        sb.Append(columnSpan.ToString(CultureInfo.InvariantCulture));
        sb.Append(";grid-row:span ");
        sb.Append(rowSpan.ToString(CultureInfo.InvariantCulture));
        if (hasFixedPanelSize && rowSpan > 1) {
            sb.Append(";min-height:calc((var(--cfx-grid-panel-height) * ");
            sb.Append(rowSpan.ToString(CultureInfo.InvariantCulture));
            sb.Append(") + (var(--cfx-grid-gap) * ");
            sb.Append((rowSpan - 1).ToString(CultureInfo.InvariantCulture));
            sb.Append("))");
        }

        sb.Append("\"");
    }

    private static string AttributeTitle(Chart chart) {
        if (chart.Title.Length > 0) return chart.Title;
        return chart.Series.Count == 0 ? "Chart" : chart.Series[0].Name;
    }

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string NextScope() {
        var value = Interlocked.Increment(ref ScopeCounter);
        return "html-grid-" + value.ToString(CultureInfo.InvariantCulture);
    }

    private static string CssFontFamily(string value) {
        if (string.IsNullOrWhiteSpace(value)) return "system-ui, sans-serif";
        return value.Replace(";", " ").Replace("{", " ").Replace("}", " ").Replace("<", " ").Replace(">", " ");
    }

    private static string GridTextStyle(ChartTextStyle style, string fallbackColor, string fallbackFontFamily, double fallbackFontSize, string fallbackWeight) {
        var css = new StringBuilder();
        css.Append("color:").Append(style.Color?.ToCss() ?? fallbackColor);
        css.Append(";font-family:").Append(CssFontFamily(style.FontFamily ?? fallbackFontFamily));
        css.Append(";font-size:").Append((style.FontSize ?? fallbackFontSize).ToString(CultureInfo.InvariantCulture)).Append("px");
        css.Append(";font-weight:").Append(CssToken(style.FontWeight ?? fallbackWeight));
        if (style.Italic) css.Append(";font-style:italic");
        if (style.Underline) css.Append(";text-decoration:underline");
        return css.ToString();
    }

    private static string CssToken(string value) => value.Replace(";", " ").Replace("{", " ").Replace("}", " ").Replace("<", " ").Replace(">", " ");
}
