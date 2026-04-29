using System;
using System.Globalization;
using System.Text;
using System.Threading;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

/// <summary>
/// Renders chart grids to self-contained SVG.
/// </summary>
public sealed class SvgChartGridRenderer {
    private static long ScopeCounter;
    private readonly SvgChartRenderer _chartRenderer = new();

    /// <summary>
    /// Renders a chart grid to SVG markup.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>SVG markup.</returns>
    public string Render(ChartGrid grid) => Render(grid, NextScope());

    /// <summary>
    /// Renders a chart grid to SVG markup with an additional deterministic ID scope.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <param name="idScope">A caller-provided scope used to keep SVG element IDs unique when embedding multiple SVG grids in one document.</param>
    /// <returns>SVG markup.</returns>
    public string Render(ChartGrid grid, string idScope) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var layout = ChartGridLayout.FromGrid(grid);
        var theme = grid.Theme ?? grid.Charts[0].Options.Theme;
        var background = theme.Background.A == 0 ? theme.CardBackground.ToCss() : theme.Background.ToCss();
        var id = "cfx-grid-" + StableHash(idScope ?? string.Empty, grid.Title, grid.Charts.Count.ToString(CultureInfo.InvariantCulture));
        var sb = new StringBuilder();
        sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"" + layout.Width.ToString(CultureInfo.InvariantCulture) + "\" height=\"" + layout.Height.ToString(CultureInfo.InvariantCulture) + "\" viewBox=\"0 0 " + layout.Width.ToString(CultureInfo.InvariantCulture) + " " + layout.Height.ToString(CultureInfo.InvariantCulture) + "\" role=\"img\" aria-labelledby=\"" + id + "-title " + id + "-desc\" preserveAspectRatio=\"xMidYMid meet\" shape-rendering=\"geometricPrecision\" text-rendering=\"geometricPrecision\" style=\"max-width:100%;height:auto;display:block\">");
        sb.AppendLine("<title id=\"" + id + "-title\">" + Escape(grid.Title.Length == 0 ? "ChartForgeX chart grid" : grid.Title) + "</title>");
        sb.AppendLine("<desc id=\"" + id + "-desc\">" + Escape("Static chart grid containing " + grid.Charts.Count.ToString(CultureInfo.InvariantCulture) + " charts.") + "</desc>");
        sb.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"" + background + "\"/>");
        if (layout.HeaderHeight > 0) {
            var headerWidth = Math.Max(8, layout.Width - grid.Padding * 2);
            var titleFontSize = StyleFontSize(grid.TitleStyle, theme.TitleFontSize);
            var subtitleFontSize = StyleFontSize(grid.SubtitleStyle, theme.SubtitleFontSize);
            if (grid.Title.Length > 0) sb.AppendLine("<text data-cfx-role=\"grid-title\" x=\"" + grid.Padding.ToString(CultureInfo.InvariantCulture) + "\" y=\"" + (grid.Padding + titleFontSize * 0.62).ToString(CultureInfo.InvariantCulture) + "\" fill=\"" + StyleColor(grid.TitleStyle, theme.Text).ToCss() + "\" font-family=\"" + Escape(StyleFontFamily(grid.TitleStyle, theme.FontFamily)) + "\" font-size=\"" + titleFontSize.ToString(CultureInfo.InvariantCulture) + "\" font-weight=\"" + Escape(StyleWeight(grid.TitleStyle, "800")) + "\"" + SvgTextStyleAttributes(grid.TitleStyle) + ">" + Escape(FitText(grid.Title, titleFontSize, headerWidth)) + "</text>");
            if (grid.Subtitle.Length > 0) sb.AppendLine("<text data-cfx-role=\"grid-subtitle\" x=\"" + (grid.Padding + 2).ToString(CultureInfo.InvariantCulture) + "\" y=\"" + (grid.Padding + titleFontSize + subtitleFontSize).ToString(CultureInfo.InvariantCulture) + "\" fill=\"" + StyleColor(grid.SubtitleStyle, theme.MutedText).ToCss() + "\" font-family=\"" + Escape(StyleFontFamily(grid.SubtitleStyle, theme.FontFamily)) + "\" font-size=\"" + subtitleFontSize.ToString(CultureInfo.InvariantCulture) + "\" font-weight=\"" + Escape(StyleWeight(grid.SubtitleStyle, "400")) + "\"" + SvgTextStyleAttributes(grid.SubtitleStyle) + ">" + Escape(FitText(grid.Subtitle, subtitleFontSize, headerWidth)) + "</text>");
        }

        foreach (var cell in layout.Cells) {
            var childSvg = _chartRenderer.Render(cell.Chart);
            var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(childSvg));
            sb.AppendLine("<image x=\"" + cell.X.ToString(CultureInfo.InvariantCulture) + "\" y=\"" + cell.Y.ToString(CultureInfo.InvariantCulture) + "\" width=\"" + cell.Width.ToString(CultureInfo.InvariantCulture) + "\" height=\"" + cell.Height.ToString(CultureInfo.InvariantCulture) + "\" href=\"data:image/svg+xml;base64," + data + "\"/>");
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static ChartColor StyleColor(ChartTextStyle style, ChartColor fallback) => style.Color ?? fallback;

    private static double StyleFontSize(ChartTextStyle style, double fallback) => style.FontSize ?? fallback;

    private static string StyleFontFamily(ChartTextStyle style, string fallback) => style.FontFamily ?? fallback;

    private static string StyleWeight(ChartTextStyle style, string fallback) => style.FontWeight ?? fallback;

    private static string SvgTextStyleAttributes(ChartTextStyle style) {
        var value = string.Empty;
        if (style.Italic) value += " font-style=\"italic\"";
        if (style.Underline) value += " text-decoration=\"underline\"";
        return value;
    }

    private static string NextScope() {
        var value = Interlocked.Increment(ref ScopeCounter);
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FitText(string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || EstimateTextWidth(value, fontSize) <= maxWidth) return value;
        const string suffix = "...";
        if (EstimateTextWidth(suffix, fontSize) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (EstimateTextWidth(value.Substring(0, mid) + suffix, fontSize) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, low) + suffix;
    }

    private static double EstimateTextWidth(string text, double fontSize) {
        var width = 0.0;
        foreach (var ch in text) width += char.IsWhiteSpace(ch) ? 0.32 : char.IsUpper(ch) || char.IsDigit(ch) ? 0.62 : 0.54;
        return width * fontSize;
    }

    private static string StableHash(params string[] values) {
        unchecked {
            var hash = 2166136261u;
            foreach (var value in values) Add(ref hash, value);

            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }

    private static void Add(ref uint hash, string value) {
        AddRaw(ref hash, value.Length.ToString(CultureInfo.InvariantCulture));
        AddRaw(ref hash, ":");
        AddRaw(ref hash, value);
        AddRaw(ref hash, "|");
    }

    private static void AddRaw(ref uint hash, string value) {
        foreach (var ch in value) {
            hash ^= ch;
            hash *= 16777619u;
        }
    }
}
