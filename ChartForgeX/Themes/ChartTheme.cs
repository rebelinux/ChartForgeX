using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

public sealed class ChartTheme {
    public ChartColor Background { get; set; } = ChartColor.Transparent;
    public ChartColor CardBackground { get; set; } = ChartColor.FromRgb(255,255,255);
    public ChartColor PlotBackground { get; set; } = ChartColor.FromRgb(248,250,252);
    public ChartColor Text { get; set; } = ChartColor.FromRgb(15,23,42);
    public ChartColor MutedText { get; set; } = ChartColor.FromRgb(100,116,139);
    public ChartColor Grid { get; set; } = ChartColor.FromRgba(148,163,184,70);
    public ChartColor Axis { get; set; } = ChartColor.FromRgba(71,85,105,160);
    public ChartColor[] Palette { get; set; } = {
        ChartColor.FromRgb(37,99,235), ChartColor.FromRgb(14,165,233), ChartColor.FromRgb(16,185,129),
        ChartColor.FromRgb(245,158,11), ChartColor.FromRgb(239,68,68), ChartColor.FromRgb(139,92,246),
        ChartColor.FromRgb(236,72,153), ChartColor.FromRgb(20,184,166)
    };
    public bool UseCard { get; set; } = true;
    public double CornerRadius { get; set; } = 24;
    public double StrokeWidth { get; set; } = 3;
    public string FontFamily { get; set; } = "Inter, Segoe UI, Arial, sans-serif";

    public static ChartTheme Light() => new();
    public static ChartTheme Dark() => new() {
        Background = ChartColor.Transparent,
        CardBackground = ChartColor.FromRgb(15,23,42),
        PlotBackground = ChartColor.FromRgb(2,6,23),
        Text = ChartColor.FromRgb(248,250,252),
        MutedText = ChartColor.FromRgb(148,163,184),
        Grid = ChartColor.FromRgba(100,116,139,80),
        Axis = ChartColor.FromRgba(203,213,225,150),
        Palette = new[] {
            ChartColor.FromRgb(96,165,250), ChartColor.FromRgb(34,211,238), ChartColor.FromRgb(52,211,153),
            ChartColor.FromRgb(251,191,36), ChartColor.FromRgb(248,113,113), ChartColor.FromRgb(167,139,250),
            ChartColor.FromRgb(244,114,182), ChartColor.FromRgb(45,212,191)
        }
    };
}
