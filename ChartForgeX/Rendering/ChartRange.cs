using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed class ChartRange {
    public double MinX { get; private set; } = double.PositiveInfinity;
    public double MaxX { get; private set; } = double.NegativeInfinity;
    public double MinY { get; private set; } = double.PositiveInfinity;
    public double MaxY { get; private set; } = double.NegativeInfinity;

    public static ChartRange FromChart(Chart chart) {
        var range = new ChartRange();
        foreach (var series in chart.Series) foreach (var p in series.Points) range.Include(p);
        if (double.IsInfinity(range.MinX)) { range.MinX = 0; range.MaxX = 1; range.MinY = 0; range.MaxY = 1; }
        if (Math.Abs(range.MaxX - range.MinX) < double.Epsilon) range.MaxX = range.MinX + 1;
        if (Math.Abs(range.MaxY - range.MinY) < double.Epsilon) range.MaxY = range.MinY + 1;
        if (range.MinY > 0) range.MinY = 0;
        var padY = (range.MaxY - range.MinY) * .08;
        range.MaxY += padY;
        return range;
    }

    public void Include(ChartPoint p) {
        if (p.X < MinX) MinX = p.X;
        if (p.X > MaxX) MaxX = p.X;
        if (p.Y < MinY) MinY = p.Y;
        if (p.Y > MaxY) MaxY = p.Y;
    }
}
