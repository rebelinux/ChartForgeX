namespace ChartForgeX.Primitives;

public readonly struct ChartRect {
    public readonly double X; public readonly double Y; public readonly double Width; public readonly double Height;
    public ChartRect(double x, double y, double width, double height) { X = x; Y = y; Width = width; Height = height; }
    public double Left => X;
    public double Right => X + Width;
    public double Top => Y;
    public double Bottom => Y + Height;
}
