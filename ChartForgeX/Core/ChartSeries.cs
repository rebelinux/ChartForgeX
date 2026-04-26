using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed class ChartSeries {
    public string Name { get; }
    public ChartSeriesKind Kind { get; }
    public List<ChartPoint> Points { get; } = new();
    public ChartColor? Color { get; set; }
    public bool Smooth { get; set; }
    public double StrokeWidth { get; set; } = 3;
    public ChartSeries(string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points) {
        Name = name;
        Kind = kind;
        Points.AddRange(points);
    }
}
