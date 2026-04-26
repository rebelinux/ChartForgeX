namespace ChartForgeX.Primitives;

public readonly struct ChartPadding {
    public readonly double Left; public readonly double Top; public readonly double Right; public readonly double Bottom;
    public ChartPadding(double left, double top, double right, double bottom) { Left = left; Top = top; Right = right; Bottom = bottom; }
    public static ChartPadding All(double value) => new(value, value, value, value);
}
