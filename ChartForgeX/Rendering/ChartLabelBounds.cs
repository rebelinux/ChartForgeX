namespace ChartForgeX.Rendering;

internal readonly struct ChartLabelBounds {
    public readonly double X;
    public readonly double Y;
    public readonly double Width;
    public readonly double Height;

    public ChartLabelBounds(double x, double y, double width, double height) {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Intersects(ChartLabelBounds other) {
        return X < other.X + other.Width &&
            X + Width > other.X &&
            Y < other.Y + other.Height &&
            Y + Height > other.Y;
    }
}
