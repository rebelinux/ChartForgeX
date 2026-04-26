using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Core;

public sealed class Chart {
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string XAxisTitle { get; set; } = string.Empty;
    public string YAxisTitle { get; set; } = string.Empty;
    public ChartOptions Options { get; } = new();
    public List<ChartSeries> Series { get; } = new();

    public static Chart Create() => new();
    public Chart WithTitle(string title) { Title = title; return this; }
    public Chart WithSubtitle(string subtitle) { Subtitle = subtitle; return this; }
    public Chart WithXAxis(string title) { XAxisTitle = title; return this; }
    public Chart WithYAxis(string title) { YAxisTitle = title; return this; }
    public Chart WithSize(int width, int height) { Options.Size = new ChartSize(width, height); return this; }
    public Chart WithTheme(ChartTheme theme) { Options.Theme = theme; return this; }
    public Chart WithTransparentBackground(bool transparent = true) { Options.TransparentBackground = transparent; return this; }

    public Chart AddLine(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Line, points, color);
    public Chart AddArea(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Area, points, color);
    public Chart AddScatter(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Scatter, points, color);
    public Chart AddBar(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) => Add(name, ChartSeriesKind.Bar, points, color);

    private Chart Add(string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points, ChartColor? color) {
        Series.Add(new ChartSeries(name, kind, points) { Color = color });
        return this;
    }
}
