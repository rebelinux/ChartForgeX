using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

/// <summary>
/// Renders chart grids to self-contained SVG.
/// </summary>
public sealed class SvgChartGridRenderer {
    private readonly SvgChartRenderer _chartRenderer = new();

    /// <summary>
    /// Renders a chart grid to SVG markup.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>SVG markup.</returns>
    public string Render(ChartGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var layout = ChartGridLayout.FromGrid(grid);
        var theme = grid.Theme ?? grid.Charts[0].Options.Theme;
        var background = theme.Background.A == 0 ? theme.CardBackground.ToCss() : theme.Background.ToCss();
        var id = "cfx-grid-" + StableHash(grid.Title + "|" + grid.Charts.Count.ToString(CultureInfo.InvariantCulture));
        var sb = new StringBuilder();
        sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"" + layout.Width.ToString(CultureInfo.InvariantCulture) + "\" height=\"" + layout.Height.ToString(CultureInfo.InvariantCulture) + "\" viewBox=\"0 0 " + layout.Width.ToString(CultureInfo.InvariantCulture) + " " + layout.Height.ToString(CultureInfo.InvariantCulture) + "\" role=\"img\" aria-labelledby=\"" + id + "-title " + id + "-desc\" preserveAspectRatio=\"xMidYMid meet\">");
        sb.AppendLine("<title id=\"" + id + "-title\">" + Escape(grid.Title.Length == 0 ? "ChartForgeX chart grid" : grid.Title) + "</title>");
        sb.AppendLine("<desc id=\"" + id + "-desc\">" + Escape("Static chart grid containing " + grid.Charts.Count.ToString(CultureInfo.InvariantCulture) + " charts.") + "</desc>");
        sb.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"" + background + "\"/>");
        if (layout.HeaderHeight > 0) {
            var headerWidth = Math.Max(8, layout.Width - grid.Padding * 2);
            if (grid.Title.Length > 0) sb.AppendLine("<text x=\"" + grid.Padding.ToString(CultureInfo.InvariantCulture) + "\" y=\"" + (grid.Padding + 16).ToString(CultureInfo.InvariantCulture) + "\" fill=\"" + theme.Text.ToCss() + "\" font-family=\"" + Escape(theme.FontFamily) + "\" font-size=\"26\" font-weight=\"800\">" + Escape(FitText(grid.Title, 26, headerWidth)) + "</text>");
            if (grid.Subtitle.Length > 0) sb.AppendLine("<text x=\"" + (grid.Padding + 2).ToString(CultureInfo.InvariantCulture) + "\" y=\"" + (grid.Padding + 40).ToString(CultureInfo.InvariantCulture) + "\" fill=\"" + theme.MutedText.ToCss() + "\" font-family=\"" + Escape(theme.FontFamily) + "\" font-size=\"14\">" + Escape(FitText(grid.Subtitle, 14, headerWidth)) + "</text>");
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

    private static string StableHash(string value) {
        unchecked {
            var hash = 2166136261u;
            foreach (var ch in value) {
                hash ^= ch;
                hash *= 16777619u;
            }

            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }
}
