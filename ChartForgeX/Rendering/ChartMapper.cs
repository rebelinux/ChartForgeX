using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed class ChartMapper {
    private readonly ChartRect _plot;
    private readonly ChartRange _range;
    public ChartMapper(ChartRect plot, ChartRange range) { _plot = plot; _range = range; }
    public double X(double value) => _plot.Left + (value - _range.MinX) / (_range.MaxX - _range.MinX) * _plot.Width;
    public double Y(double value) => _plot.Bottom - (value - _range.MinY) / (_range.MaxY - _range.MinY) * _plot.Height;
}
