using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal sealed class SvgRasterDefinitions {
    private readonly Dictionary<string, SvgRasterLinearGradient> _linearGradients = new(StringComparer.Ordinal);

    public static SvgRasterDefinitions From(SvgRasterDocument document) {
        var definitions = new SvgRasterDefinitions();
        foreach (var child in document.Children) definitions.Collect(child);
        return definitions;
    }

    public bool TryGetLinearGradient(string? id, out SvgRasterLinearGradient gradient) {
        if (id != null && _linearGradients.TryGetValue(id, out gradient!)) return true;
        gradient = null!;
        return false;
    }

    private void Collect(SvgRasterElement element) {
        if (string.Equals(element.Name, "linearGradient", StringComparison.Ordinal) && element.TryGet("id", out var id) && !string.IsNullOrWhiteSpace(id)) {
            _linearGradients[id] = SvgRasterLinearGradient.From(element);
        }

        foreach (var child in element.Children) Collect(child);
    }
}

internal sealed class SvgRasterLinearGradient {
    private SvgRasterLinearGradient(double x1, double y1, double x2, double y2, bool userSpaceOnUse, IReadOnlyList<RasterGradientStop> stops) {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        UserSpaceOnUse = userSpaceOnUse;
        Stops = stops;
    }

    public double X1 { get; }
    public double Y1 { get; }
    public double X2 { get; }
    public double Y2 { get; }
    public bool UserSpaceOnUse { get; }
    public IReadOnlyList<RasterGradientStop> Stops { get; }

    public static SvgRasterLinearGradient From(SvgRasterElement element) {
        var userSpace = string.Equals(element.Get("gradientUnits"), "userSpaceOnUse", StringComparison.Ordinal);
        var stops = element.Children
            .Where(child => string.Equals(child.Name, "stop", StringComparison.Ordinal))
            .Select(ReadStop)
            .OrderBy(stop => stop.Offset)
            .ToArray();
        if (stops.Length == 0) stops = new[] { new RasterGradientStop(0, ChartColor.Black), new RasterGradientStop(1, ChartColor.Black) };
        return new SvgRasterLinearGradient(
            ParseCoordinate(element.Get("x1"), 0),
            ParseCoordinate(element.Get("y1"), 0),
            ParseCoordinate(element.Get("x2"), userSpace ? 0 : 1),
            ParseCoordinate(element.Get("y2"), 0),
            userSpace,
            stops);
    }

    public void Endpoints(IReadOnlyList<List<ChartPoint>> contours, SvgRasterMatrix matrix, out ChartPoint start, out ChartPoint end) {
        if (UserSpaceOnUse) {
            start = matrix.Transform(new ChartPoint(X1, Y1));
            end = matrix.Transform(new ChartPoint(X2, Y2));
            return;
        }

        var bounds = Bounds(contours);
        start = new ChartPoint(bounds.Left + bounds.Width * X1, bounds.Top + bounds.Height * Y1);
        end = new ChartPoint(bounds.Left + bounds.Width * X2, bounds.Top + bounds.Height * Y2);
    }

    private static RasterGradientStop ReadStop(SvgRasterElement element) {
        var styleColor = ChartColor.Black;
        var stopColor = element.Get("stop-color");
        var stopOpacity = element.Get("stop-opacity");
        var inline = element.Get("style");
        if (!string.IsNullOrWhiteSpace(inline)) {
            foreach (var declaration in ChartForgeX.Svg.SvgStyleDeclarationList.Parse(inline!).Declarations) {
                if (string.Equals(declaration.Name, "stop-color", StringComparison.Ordinal)) stopColor = declaration.Value;
                else if (string.Equals(declaration.Name, "stop-opacity", StringComparison.Ordinal)) stopOpacity = declaration.Value;
            }
        }

        if (!ChartColor.TryParse(stopColor, out styleColor)) styleColor = ChartColor.Black;
        return new RasterGradientStop(ParseOffset(element.Get("offset")), WithOpacity(styleColor, ParseOpacity(stopOpacity, 1)));
    }

    private static double ParseCoordinate(string? value, double fallback) {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var trimmed = value!.Trim();
        var percent = trimmed.EndsWith("%", StringComparison.Ordinal);
        if (percent) trimmed = trimmed.Substring(0, trimmed.Length - 1);
        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return fallback;
        return percent ? parsed / 100.0 : parsed;
    }

    private static double ParseOffset(string? value) =>
        Math.Max(0, Math.Min(1, ParseCoordinate(value, 0)));

    private static double ParseOpacity(string? value, double fallback) =>
        value == null ? fallback : Math.Max(0, Math.Min(1, ParseCoordinate(value, fallback)));

    private static ChartColor WithOpacity(ChartColor color, double opacity) =>
        ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(color.A * opacity));

    private static GradientBounds Bounds(IReadOnlyList<List<ChartPoint>> contours) {
        var left = double.PositiveInfinity;
        var top = double.PositiveInfinity;
        var right = double.NegativeInfinity;
        var bottom = double.NegativeInfinity;
        foreach (var contour in contours) foreach (var point in contour) {
            left = Math.Min(left, point.X);
            top = Math.Min(top, point.Y);
            right = Math.Max(right, point.X);
            bottom = Math.Max(bottom, point.Y);
        }

        if (double.IsInfinity(left) || double.IsInfinity(top)) return new GradientBounds(0, 0, 1, 1);
        return new GradientBounds(left, top, Math.Max(0.000001, right - left), Math.Max(0.000001, bottom - top));
    }

    private readonly struct GradientBounds {
        public readonly double Left;
        public readonly double Top;
        public readonly double Width;
        public readonly double Height;

        public GradientBounds(double left, double top, double width, double height) {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
    }
}
