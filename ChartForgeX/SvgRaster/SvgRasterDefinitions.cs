using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal sealed class SvgRasterDefinitions {
    private readonly Dictionary<string, SvgRasterElement> _gradientElements = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SvgRasterLinearGradient> _linearGradients = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SvgRasterRadialGradient> _radialGradients = new(StringComparer.Ordinal);

    public static SvgRasterDefinitions From(SvgRasterDocument document) {
        var definitions = new SvgRasterDefinitions();
        foreach (var child in document.Children) definitions.Collect(child);
        return definitions;
    }

    public bool TryGetLinearGradient(string? id, out SvgRasterLinearGradient gradient) {
        if (id != null && ResolveLinearGradient(id, new HashSet<string>(StringComparer.Ordinal), out gradient!)) return true;
        gradient = null!;
        return false;
    }

    public bool TryGetRadialGradient(string? id, out SvgRasterRadialGradient gradient) {
        if (id != null && ResolveRadialGradient(id, new HashSet<string>(StringComparer.Ordinal), out gradient!)) return true;
        gradient = null!;
        return false;
    }

    private bool ResolveLinearGradient(string id, HashSet<string> visiting, out SvgRasterLinearGradient gradient) {
        if (_linearGradients.TryGetValue(id, out gradient!)) return true;
        if (!visiting.Add(id) || !_gradientElements.TryGetValue(id, out var element) || !string.Equals(element.Name, "linearGradient", StringComparison.Ordinal)) {
            gradient = null!;
            return false;
        }

        SvgRasterLinearGradient? inherited = null;
        var referenceId = ReferenceId(element);
        if (referenceId != null) ResolveLinearGradient(referenceId, visiting, out inherited!);
        gradient = SvgRasterLinearGradient.From(element, inherited);
        _linearGradients[id] = gradient;
        visiting.Remove(id);
        return true;
    }

    private bool ResolveRadialGradient(string id, HashSet<string> visiting, out SvgRasterRadialGradient gradient) {
        if (_radialGradients.TryGetValue(id, out gradient!)) return true;
        if (!visiting.Add(id) || !_gradientElements.TryGetValue(id, out var element) || !string.Equals(element.Name, "radialGradient", StringComparison.Ordinal)) {
            gradient = null!;
            return false;
        }

        SvgRasterRadialGradient? inherited = null;
        var referenceId = ReferenceId(element);
        if (referenceId != null) ResolveRadialGradient(referenceId, visiting, out inherited!);
        gradient = SvgRasterRadialGradient.From(element, inherited);
        _radialGradients[id] = gradient;
        visiting.Remove(id);
        return true;
    }

    private void Collect(SvgRasterElement element) {
        if ((string.Equals(element.Name, "linearGradient", StringComparison.Ordinal) || string.Equals(element.Name, "radialGradient", StringComparison.Ordinal)) && element.TryGet("id", out var id) && !string.IsNullOrWhiteSpace(id)) {
            _gradientElements[id] = element;
        }

        foreach (var child in element.Children) Collect(child);
    }

    private static string? ReferenceId(SvgRasterElement element) {
        var href = element.Get("href");
        if (string.IsNullOrWhiteSpace(href) || !href!.Trim().StartsWith("#", StringComparison.Ordinal)) return null;
        return href.Trim().Substring(1);
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

    public static SvgRasterLinearGradient From(SvgRasterElement element, SvgRasterLinearGradient? inherited) {
        var userSpace = string.Equals(element.Get("gradientUnits"), "userSpaceOnUse", StringComparison.Ordinal) || (element.Get("gradientUnits") == null && inherited?.UserSpaceOnUse == true);
        var stops = SvgRasterGradientValues.ReadStops(element);
        if (stops.Count == 0) stops = inherited?.Stops ?? SvgRasterGradientValues.BlackStops;
        return new SvgRasterLinearGradient(
            SvgRasterGradientValues.ParseCoordinate(element.Get("x1"), inherited?.X1 ?? 0),
            SvgRasterGradientValues.ParseCoordinate(element.Get("y1"), inherited?.Y1 ?? 0),
            SvgRasterGradientValues.ParseCoordinate(element.Get("x2"), inherited?.X2 ?? (userSpace ? 0 : 1)),
            SvgRasterGradientValues.ParseCoordinate(element.Get("y2"), inherited?.Y2 ?? 0),
            userSpace,
            stops);
    }

    public void Endpoints(IReadOnlyList<List<ChartPoint>> contours, SvgRasterMatrix matrix, out ChartPoint start, out ChartPoint end) {
        if (UserSpaceOnUse) {
            start = matrix.Transform(new ChartPoint(X1, Y1));
            end = matrix.Transform(new ChartPoint(X2, Y2));
            return;
        }

        var bounds = SvgRasterGradientValues.Bounds(contours);
        start = new ChartPoint(bounds.Left + bounds.Width * X1, bounds.Top + bounds.Height * Y1);
        end = new ChartPoint(bounds.Left + bounds.Width * X2, bounds.Top + bounds.Height * Y2);
    }
}

internal sealed class SvgRasterRadialGradient {
    private SvgRasterRadialGradient(double cx, double cy, double r, bool userSpaceOnUse, IReadOnlyList<RasterGradientStop> stops) {
        Cx = cx;
        Cy = cy;
        Radius = r;
        UserSpaceOnUse = userSpaceOnUse;
        Stops = stops;
    }

    public double Cx { get; }
    public double Cy { get; }
    public double Radius { get; }
    public bool UserSpaceOnUse { get; }
    public IReadOnlyList<RasterGradientStop> Stops { get; }

    public static SvgRasterRadialGradient From(SvgRasterElement element, SvgRasterRadialGradient? inherited) {
        var userSpace = string.Equals(element.Get("gradientUnits"), "userSpaceOnUse", StringComparison.Ordinal) || (element.Get("gradientUnits") == null && inherited?.UserSpaceOnUse == true);
        var stops = SvgRasterGradientValues.ReadStops(element);
        if (stops.Count == 0) stops = inherited?.Stops ?? SvgRasterGradientValues.BlackStops;
        return new SvgRasterRadialGradient(
            SvgRasterGradientValues.ParseCoordinate(element.Get("cx"), inherited?.Cx ?? 0.5),
            SvgRasterGradientValues.ParseCoordinate(element.Get("cy"), inherited?.Cy ?? 0.5),
            SvgRasterGradientValues.ParseCoordinate(element.Get("r"), inherited?.Radius ?? 0.5),
            userSpace,
            stops);
    }

    public void Circle(IReadOnlyList<List<ChartPoint>> contours, SvgRasterMatrix matrix, out ChartPoint center, out double radius) {
        if (UserSpaceOnUse) {
            center = matrix.Transform(new ChartPoint(Cx, Cy));
            radius = Math.Max(0.000001, Radius * matrix.ScaleFactor);
            return;
        }

        var bounds = SvgRasterGradientValues.Bounds(contours);
        center = new ChartPoint(bounds.Left + bounds.Width * Cx, bounds.Top + bounds.Height * Cy);
        radius = Math.Max(0.000001, Radius * Math.Max(bounds.Width, bounds.Height));
    }
}

internal static class SvgRasterGradientValues {
    public static readonly IReadOnlyList<RasterGradientStop> BlackStops = new[] { new RasterGradientStop(0, ChartColor.Black), new RasterGradientStop(1, ChartColor.Black) };

    public static IReadOnlyList<RasterGradientStop> ReadStops(SvgRasterElement element) {
        return element.Children
            .Where(child => string.Equals(child.Name, "stop", StringComparison.Ordinal))
            .Select(ReadStop)
            .OrderBy(stop => stop.Offset)
            .ToArray();
    }

    public static double ParseCoordinate(string? value, double fallback) {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var trimmed = value!.Trim();
        var percent = trimmed.EndsWith("%", StringComparison.Ordinal);
        if (percent) trimmed = trimmed.Substring(0, trimmed.Length - 1);
        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return fallback;
        return percent ? parsed / 100.0 : parsed;
    }

    public static GradientBounds Bounds(IReadOnlyList<List<ChartPoint>> contours) {
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

    private static RasterGradientStop ReadStop(SvgRasterElement element) {
        var stopColor = element.Get("stop-color");
        var stopOpacity = element.Get("stop-opacity");
        var inline = element.Get("style");
        if (!string.IsNullOrWhiteSpace(inline)) {
            foreach (var declaration in ChartForgeX.Svg.SvgStyleDeclarationList.Parse(inline!).Declarations) {
                if (string.Equals(declaration.Name, "stop-color", StringComparison.Ordinal)) stopColor = declaration.Value;
                else if (string.Equals(declaration.Name, "stop-opacity", StringComparison.Ordinal)) stopOpacity = declaration.Value;
            }
        }

        if (!ChartColor.TryParse(stopColor, out var color)) color = ChartColor.Black;
        return new RasterGradientStop(Math.Max(0, Math.Min(1, ParseCoordinate(element.Get("offset"), 0))), WithOpacity(color, ParseOpacity(stopOpacity, 1)));
    }

    private static double ParseOpacity(string? value, double fallback) =>
        value == null ? fallback : Math.Max(0, Math.Min(1, ParseCoordinate(value, fallback)));

    private static ChartColor WithOpacity(ChartColor color, double opacity) =>
        ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(color.A * opacity));

    public readonly struct GradientBounds {
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
