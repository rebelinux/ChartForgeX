using System;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.SvgRaster;

internal readonly struct SvgRasterMatrix {
    public static readonly SvgRasterMatrix Identity = new(1, 0, 0, 1, 0, 0);

    private readonly double _a, _b, _c, _d, _e, _f;

    public SvgRasterMatrix(double a, double b, double c, double d, double e, double f) {
        _a = a;
        _b = b;
        _c = c;
        _d = d;
        _e = e;
        _f = f;
    }

    public ChartPoint Transform(ChartPoint point) =>
        new(point.X * _a + point.Y * _c + _e, point.X * _b + point.Y * _d + _f);

    public double ScaleFactor {
        get {
            var x = Math.Sqrt(_a * _a + _b * _b);
            var y = Math.Sqrt(_c * _c + _d * _d);
            return Math.Max(0.000001, (x + y) / 2.0);
        }
    }

    public SvgRasterMatrix Multiply(SvgRasterMatrix other) =>
        new(
            _a * other._a + _c * other._b,
            _b * other._a + _d * other._b,
            _a * other._c + _c * other._d,
            _b * other._c + _d * other._d,
            _a * other._e + _c * other._f + _e,
            _b * other._e + _d * other._f + _f);

    public static SvgRasterMatrix Translate(double x, double y) => new(1, 0, 0, 1, x, y);

    public static SvgRasterMatrix Scale(double x, double y) => new(x, 0, 0, y, 0, 0);

    public static SvgRasterMatrix Rotate(double degrees) {
        var radians = degrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return new SvgRasterMatrix(cos, sin, -sin, cos, 0, 0);
    }

    public static SvgRasterMatrix FromFit(SvgRasterViewBox viewBox, int width, int height, string? preserveAspectRatio) {
        var value = string.IsNullOrWhiteSpace(preserveAspectRatio) ? "xMidYMid meet" : preserveAspectRatio!.Trim();
        if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)) {
            return Scale(width / viewBox.Width, height / viewBox.Height).Multiply(Translate(-viewBox.X, -viewBox.Y));
        }

        var slice = value.IndexOf("slice", StringComparison.OrdinalIgnoreCase) >= 0;
        var scaleX = width / viewBox.Width;
        var scaleY = height / viewBox.Height;
        var scale = slice ? Math.Max(scaleX, scaleY) : Math.Min(scaleX, scaleY);
        var drawnWidth = viewBox.Width * scale;
        var drawnHeight = viewBox.Height * scale;
        var align = value.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
        var x = align.IndexOf("xMax", StringComparison.OrdinalIgnoreCase) >= 0 ? width - drawnWidth : align.IndexOf("xMid", StringComparison.OrdinalIgnoreCase) >= 0 ? (width - drawnWidth) / 2.0 : 0;
        var y = align.IndexOf("YMax", StringComparison.OrdinalIgnoreCase) >= 0 ? height - drawnHeight : align.IndexOf("YMid", StringComparison.OrdinalIgnoreCase) >= 0 ? (height - drawnHeight) / 2.0 : 0;
        return Translate(x, y).Multiply(Scale(scale, scale)).Multiply(Translate(-viewBox.X, -viewBox.Y));
    }

    public static SvgRasterMatrix ParseTransform(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return Identity;
        var list = SvgTransformList.Parse(value!);
        var matrix = Identity;
        foreach (var transform in list.Transforms) matrix = matrix.Multiply(ToMatrix(transform));
        return matrix;
    }

    private static SvgRasterMatrix ToMatrix(SvgTransform transform) {
        var values = transform.Values;
        switch (transform.Name) {
            case "matrix":
                return new SvgRasterMatrix(values[0], values[1], values[2], values[3], values[4], values[5]);
            case "translate":
                return Translate(values[0], values.Count > 1 ? values[1] : 0);
            case "scale":
                return Scale(values[0], values.Count > 1 ? values[1] : values[0]);
            case "rotate":
                if (values.Count == 1) return Rotate(values[0]);
                return Translate(values[1], values[2]).Multiply(Rotate(values[0])).Multiply(Translate(-values[1], -values[2]));
            case "skewX":
                return new SvgRasterMatrix(1, 0, Math.Tan(values[0] * Math.PI / 180.0), 1, 0, 0);
            case "skewY":
                return new SvgRasterMatrix(1, Math.Tan(values[0] * Math.PI / 180.0), 0, 1, 0, 0);
            default:
                return Identity;
        }
    }
}
