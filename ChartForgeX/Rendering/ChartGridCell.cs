using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal readonly struct ChartGridCell {
    public readonly Chart Chart;
    public readonly int X;
    public readonly int Y;
    public readonly int Width;
    public readonly int Height;

    public ChartGridCell(Chart chart, int x, int y, int width, int height) {
        Chart = chart;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}
