using System;
using System.Globalization;

namespace ChartForgeX.SvgRaster;

internal readonly struct SvgRasterViewBox {
    public readonly double X;
    public readonly double Y;
    public readonly double Width;
    public readonly double Height;

    public SvgRasterViewBox(double x, double y, double width, double height) {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "SVG viewBox width and height must be positive.");
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public static SvgRasterViewBox Parse(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return new SvgRasterViewBox(0, 0, 24, 24);
        var parts = value!.Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4) throw new FormatException("SVG viewBox must contain four numbers.");
        return new SvgRasterViewBox(ParseNumber(parts[0]), ParseNumber(parts[1]), ParseNumber(parts[2]), ParseNumber(parts[3]));
    }

    private static double ParseNumber(string value) =>
        double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
}
