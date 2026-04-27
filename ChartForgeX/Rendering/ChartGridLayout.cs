using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed class ChartGridLayout {
    private ChartGridLayout(int width, int height, int headerHeight, IReadOnlyList<ChartGridCell> cells) {
        Width = width;
        Height = height;
        HeaderHeight = headerHeight;
        Cells = cells;
    }

    public int Width { get; }
    public int Height { get; }
    public int HeaderHeight { get; }
    public IReadOnlyList<ChartGridCell> Cells { get; }

    public static ChartGridLayout FromGrid(ChartGrid grid) {
        if (grid == null) throw new ArgumentNullException(nameof(grid));
        if (grid.Charts.Count == 0) throw new InvalidOperationException("Chart grids must contain at least one chart.");

        var columns = Math.Min(grid.Columns, grid.Charts.Count);
        var rows = (int)Math.Ceiling(grid.Charts.Count / (double)columns);
        var panelWidth = grid.PanelSize.HasValue ? grid.PanelSize.Value.Width : 1;
        var panelHeight = grid.PanelSize.HasValue ? grid.PanelSize.Value.Height : 1;
        foreach (var chart in grid.Charts) {
            if (grid.PanelSize.HasValue) break;
            panelWidth = Math.Max(panelWidth, chart.Options.Size.Width);
            panelHeight = Math.Max(panelHeight, chart.Options.Size.Height);
        }

        var headerHeight = grid.Title.Length == 0 && grid.Subtitle.Length == 0 ? 0 : 76;
        var width = grid.Padding * 2 + columns * panelWidth + (columns - 1) * grid.Gap;
        var height = grid.Padding * 2 + headerHeight + rows * panelHeight + (rows - 1) * grid.Gap;
        var cells = new List<ChartGridCell>(grid.Charts.Count);
        for (var i = 0; i < grid.Charts.Count; i++) {
            var chart = grid.Charts[i];
            var row = i / columns;
            var column = i % columns;
            var panelX = grid.Padding + column * (panelWidth + grid.Gap);
            var panelY = grid.Padding + headerHeight + row * (panelHeight + grid.Gap);
            if (grid.PanelSize.HasValue) {
                cells.Add(FitCell(chart, panelX, panelY, panelWidth, panelHeight, grid.PanelFit));
            } else {
                var x = panelX + (panelWidth - chart.Options.Size.Width) / 2;
                var y = panelY + (panelHeight - chart.Options.Size.Height) / 2;
                cells.Add(new ChartGridCell(chart, x, y, chart.Options.Size.Width, chart.Options.Size.Height));
            }
        }

        return new ChartGridLayout(width, height, headerHeight, cells);
    }

    private static ChartGridCell FitCell(Chart chart, int panelX, int panelY, int panelWidth, int panelHeight, ChartGridPanelFit fit) {
        if (fit == ChartGridPanelFit.Stretch) return new ChartGridCell(chart, panelX, panelY, panelWidth, panelHeight);
        var scale = Math.Min(panelWidth / (double)chart.Options.Size.Width, panelHeight / (double)chart.Options.Size.Height);
        var width = Math.Max(1, (int)Math.Round(chart.Options.Size.Width * scale));
        var height = Math.Max(1, (int)Math.Round(chart.Options.Size.Height * scale));
        var x = panelX + (panelWidth - width) / 2;
        var y = panelY + (panelHeight - height) / 2;
        return new ChartGridCell(chart, x, y, width, height);
    }
}
