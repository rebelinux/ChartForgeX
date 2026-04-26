using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Core;

public sealed class ChartOptions {
    public ChartSize Size { get; set; } = new(1000, 560);
    public ChartPadding Padding { get; set; } = new(76, 78, 36, 74);
    public ChartTheme Theme { get; set; } = ChartTheme.Light();
    public bool ShowLegend { get; set; } = true;
    public bool ShowGrid { get; set; } = true;
    public bool ShowAxes { get; set; } = true;
    public int TickCount { get; set; } = 6;
    public bool TransparentBackground { get; set; } = true;
}
