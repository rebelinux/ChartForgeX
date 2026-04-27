using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

/// <summary>
/// Renders chart grids to dependency-free PNG images.
/// </summary>
public sealed class PngChartGridRenderer {
    private readonly PngChartRenderer _chartRenderer = new();

    /// <summary>
    /// Renders a chart grid to PNG bytes.
    /// </summary>
    /// <param name="grid">The chart grid to render.</param>
    /// <returns>A PNG image.</returns>
    public byte[] Render(ChartGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        var layout = ChartGridLayout.FromGrid(grid);
        var theme = grid.Theme ?? grid.Charts[0].Options.Theme;
        var background = theme.Background.A == 0 ? theme.CardBackground : theme.Background;
        var output = new RgbaCanvas(layout.Width, layout.Height, 1);
        output.Clear(background);
        if (layout.HeaderHeight > 0) {
            var headerWidth = Math.Max(8, layout.Width - grid.Padding * 2);
            if (grid.Title.Length > 0) output.DrawTextEmphasized(grid.Padding, Math.Max(0, grid.Padding - 8), FitText(grid.Title, 26, headerWidth), theme.Text, 26);
            if (grid.Subtitle.Length > 0) output.DrawText(grid.Padding + 2, grid.Padding + 30, FitText(grid.Subtitle, 14, headerWidth), theme.MutedText, 14);
        }

        foreach (var cell in layout.Cells) {
            var chartCanvas = _chartRenderer.RenderCanvas(cell.Chart);
            output.DrawImageScaled(cell.X, cell.Y, cell.Width, cell.Height, chartCanvas.Width, chartCanvas.Height, chartCanvas.ToOutputPixels());
        }

        return PngWriter.WriteRgba(output.Width, output.Height, output.Pixels);
    }

    private static string FitText(string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || RgbaCanvas.MeasureTextWidth(value, fontSize, null) <= maxWidth) return value;
        const string suffix = "...";
        if (RgbaCanvas.MeasureTextWidth(suffix, fontSize, null) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (RgbaCanvas.MeasureTextWidth(value.Substring(0, mid) + suffix, fontSize, null) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, low) + suffix;
    }
}
