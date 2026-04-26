namespace ChartForgeX.Primitives;

public readonly struct ChartColor {
    public readonly byte R; public readonly byte G; public readonly byte B; public readonly byte A;
    public ChartColor(byte r, byte g, byte b, byte a = 255) { R = r; G = g; B = b; A = a; }
    public static ChartColor FromRgb(byte r, byte g, byte b) => new(r, g, b, 255);
    public static ChartColor FromRgba(byte r, byte g, byte b, byte a) => new(r, g, b, a);
    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";
    public string ToCss() => A == 255 ? ToHex() : $"rgba({R},{G},{B},{A / 255.0:0.###})";
    public static ChartColor Transparent => new(0,0,0,0);
    public static ChartColor White => new(255,255,255);
    public static ChartColor Black => new(0,0,0);
}
