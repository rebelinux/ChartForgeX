namespace ChartForgeX.Rendering;

internal readonly struct ChartPathCommand {
    public ChartPathCommandKind Kind { get; }
    public double X { get; }
    public double Y { get; }
    public double Control1X { get; }
    public double Control1Y { get; }
    public double Control2X { get; }
    public double Control2Y { get; }

    private ChartPathCommand(ChartPathCommandKind kind, double x, double y, double control1X, double control1Y, double control2X, double control2Y) {
        Kind = kind;
        X = x;
        Y = y;
        Control1X = control1X;
        Control1Y = control1Y;
        Control2X = control2X;
        Control2Y = control2Y;
    }

    public static ChartPathCommand MoveTo(double x, double y) => new(ChartPathCommandKind.MoveTo, x, y, 0, 0, 0, 0);

    public static ChartPathCommand LineTo(double x, double y) => new(ChartPathCommandKind.LineTo, x, y, 0, 0, 0, 0);

    public static ChartPathCommand CubicTo(double control1X, double control1Y, double control2X, double control2Y, double x, double y) =>
        new(ChartPathCommandKind.CubicTo, x, y, control1X, control1Y, control2X, control2Y);
}
