using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal readonly struct ChartTreemapTile {
    public ChartTreemapTile(int pointIndex, ChartPoint point, ChartRect rect) {
        PointIndex = pointIndex;
        Point = point;
        Rect = rect;
    }

    public int PointIndex { get; }

    public ChartPoint Point { get; }

    public ChartRect Rect { get; }
}
