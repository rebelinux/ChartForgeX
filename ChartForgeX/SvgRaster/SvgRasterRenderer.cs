using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static class SvgRasterRenderer {
    public static bool TryRenderFragment(string svgBody, string? viewBox, string? preserveAspectRatio, int width, int height, out byte[] rgba) {
        rgba = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(svgBody) || width <= 0 || height <= 0) return false;

        try {
            var document = SvgRasterParser.ParseFragment(svgBody, viewBox);
            var definitions = SvgRasterDefinitions.From(document);
            var canvas = new RgbaCanvas(width, height, 1);
            var matrix = SvgRasterMatrix.FromFit(document.ViewBox, width, height, preserveAspectRatio);
            foreach (var child in document.Children) RenderElement(canvas, child, SvgRasterStyle.Default, matrix, definitions);
            rgba = canvas.Pixels;
            return HasVisiblePixel(rgba);
        } catch (Exception ex) when (ex is FormatException || ex is InvalidOperationException || ex is ArgumentException || ex is System.Xml.XmlException) {
            return false;
        }
    }

    private static void RenderElement(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle parentStyle, SvgRasterMatrix parentMatrix, SvgRasterDefinitions definitions) {
        var style = SvgRasterStyle.Resolve(parentStyle, element);
        if (!style.Visible) return;

        var matrix = parentMatrix.Multiply(SvgRasterMatrix.ParseTransform(element.Get("transform")));
        if (string.Equals(element.Name, "svg", StringComparison.Ordinal)) matrix = ApplyNestedSvgViewport(element, matrix);
        switch (element.Name) {
            case "g":
            case "svg":
            case "defs":
            case "title":
            case "desc":
                break;
            case "path":
                RenderPath(canvas, element, style, matrix, definitions);
                break;
            case "rect":
                RenderRect(canvas, element, style, matrix, definitions);
                break;
            case "circle":
                RenderEllipse(canvas, element.GetDouble("cx"), element.GetDouble("cy"), element.GetDouble("r"), element.GetDouble("r"), style, matrix, definitions);
                break;
            case "ellipse":
                RenderEllipse(canvas, element.GetDouble("cx"), element.GetDouble("cy"), element.GetDouble("rx"), element.GetDouble("ry"), style, matrix, definitions);
                break;
            case "line":
                RenderLine(canvas, element, style, matrix, definitions);
                break;
            case "polyline":
                RenderPointList(canvas, element, style, matrix, definitions, close: false);
                break;
            case "polygon":
                RenderPointList(canvas, element, style, matrix, definitions, close: true);
                break;
            case "text":
                RenderText(canvas, element, style, matrix);
                break;
        }

        foreach (var child in element.Children) RenderElement(canvas, child, style, matrix, definitions);
    }

    private static void RenderPath(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions) {
        var d = element.Get("d");
        if (string.IsNullOrWhiteSpace(d)) return;
        var rings = TransformRings(ChartMapPathParser.ParseRings(d!), matrix);
        FillAndStroke(canvas, rings, style, PathHasClose(d!), matrix, definitions);
    }

    private static void RenderRect(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions) {
        var x = element.GetDouble("x");
        var y = element.GetDouble("y");
        var width = element.GetDouble("width");
        var height = element.GetDouble("height");
        if (width <= 0 || height <= 0) return;
        var rx = Math.Max(0, Math.Min(element.GetDouble("rx", element.GetDouble("ry")), Math.Min(width, height) / 2.0));
        var ring = rx <= 0 ? RectRing(x, y, width, height) : RoundedRectRing(x, y, width, height, rx);
        FillAndStroke(canvas, new[] { TransformRing(ring, matrix) }, style, true, matrix, definitions);
    }

    private static void RenderEllipse(RgbaCanvas canvas, double cx, double cy, double rx, double ry, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions) {
        if (rx <= 0 || ry <= 0) return;
        FillAndStroke(canvas, new[] { TransformRing(EllipseRing(cx, cy, rx, ry, 36), matrix) }, style, true, matrix, definitions);
    }

    private static void RenderLine(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions) {
        var points = new[] {
            matrix.Transform(new ChartPoint(element.GetDouble("x1"), element.GetDouble("y1"))),
            matrix.Transform(new ChartPoint(element.GetDouble("x2"), element.GetDouble("y2")))
        };
        Stroke(canvas, points, style, matrix.ScaleFactor, definitions);
    }

    private static void RenderPointList(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, bool close) {
        var points = ReadPointList(element.Get("points"));
        if (points.Count == 0) return;
        var transformed = TransformRing(points, matrix);
        FillAndStroke(canvas, new[] { transformed }, style, close, matrix, definitions);
    }

    private static void RenderText(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix) {
        var color = style.FillColor();
        if (color.A == 0 || string.IsNullOrWhiteSpace(element.Text)) return;
        var point = matrix.Transform(new ChartPoint(element.GetDouble("x"), element.GetDouble("y")));
        var text = element.Text.Trim();
        var fontSize = Math.Max(1, style.FontSize * matrix.ScaleFactor);
        var width = RgbaCanvas.MeasureTextWidth(text, fontSize, null);
        var x = point.X;
        if (string.Equals(style.TextAnchor, "middle", StringComparison.OrdinalIgnoreCase)) x -= width / 2.0;
        else if (string.Equals(style.TextAnchor, "end", StringComparison.OrdinalIgnoreCase)) x -= width;
        var y = point.Y - fontSize * 0.82;
        if (IsBold(style.FontWeight)) canvas.DrawTextEmphasized(x, y, text, color, fontSize);
        else canvas.DrawText(x, y, text, color, fontSize);
    }

    private static void FillAndStroke(RgbaCanvas canvas, IEnumerable<List<ChartPoint>> rings, SvgRasterStyle style, bool close, SvgRasterMatrix matrix, SvgRasterDefinitions definitions) {
        var contours = new List<List<ChartPoint>>();
        var strokeRings = new List<List<ChartPoint>>();
        foreach (var ring in rings) {
            if (ring.Count == 0) continue;
            var contour = close ? ClosedRing(ring) : new List<ChartPoint>(ring);
            if (close && contour.Count >= 3) contours.Add(contour);
            strokeRings.Add(contour);
        }

        Fill(canvas, contours, style, matrix, definitions);
        foreach (var ring in strokeRings) Stroke(canvas, ring, style, matrix.ScaleFactor, definitions);
    }

    private static void Fill(RgbaCanvas canvas, IReadOnlyList<List<ChartPoint>> contours, SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions) {
        if (contours.Count == 0 || style.Fill.IsNone) return;
        if (style.Fill.IsReference && definitions.TryGetLinearGradient(style.Fill.ReferenceId, out var gradient)) {
            gradient.Endpoints(contours, matrix, out var start, out var end);
            canvas.FillContoursLinearGradient(contours, start, end, gradient.Stops);
            return;
        }

        var fill = style.FillColor();
        if (fill.A != 0) canvas.FillCompoundPolygon(contours, fill);
    }

    private static void Stroke(RgbaCanvas canvas, IReadOnlyList<ChartPoint> points, SvgRasterStyle style, double scale, SvgRasterDefinitions definitions) {
        var stroke = ResolveColor(style.Stroke, style.Opacity * style.StrokeOpacity, definitions);
        if (stroke.A == 0 || style.StrokeWidth <= 0 || points.Count < 2) return;
        canvas.DrawPolyline(points, stroke, Math.Max(0.5, style.StrokeWidth * scale));
    }

    private static ChartColor ResolveColor(SvgRasterPaint paint, double opacity, SvgRasterDefinitions definitions) {
        if (paint.IsNone) return ChartColor.Transparent;
        if (paint.Color.HasValue) return WithOpacity(paint.Color.Value, opacity);
        if (paint.IsReference && definitions.TryGetLinearGradient(paint.ReferenceId, out var gradient) && gradient.Stops.Count > 0) return WithOpacity(gradient.Stops[0].Color, opacity);
        return ChartColor.Transparent;
    }

    private static SvgRasterMatrix ApplyNestedSvgViewport(SvgRasterElement element, SvgRasterMatrix matrix) {
        var viewBox = element.Get("viewBox");
        if (string.IsNullOrWhiteSpace(viewBox)) return matrix;
        var parsed = SvgRasterViewBox.Parse(viewBox);
        var x = element.GetDouble("x");
        var y = element.GetDouble("y");
        var width = element.GetDouble("width", parsed.Width);
        var height = element.GetDouble("height", parsed.Height);
        if (width <= 0 || height <= 0) return matrix;
        return matrix.Multiply(SvgRasterMatrix.Translate(x, y)).Multiply(SvgRasterMatrix.FromFit(parsed, (int)Math.Round(width), (int)Math.Round(height), element.Get("preserveAspectRatio")));
    }

    private static List<List<ChartPoint>> TransformRings(IReadOnlyList<List<ChartPoint>> rings, SvgRasterMatrix matrix) {
        var transformed = new List<List<ChartPoint>>(rings.Count);
        foreach (var ring in rings) transformed.Add(TransformRing(ring, matrix));
        return transformed;
    }

    private static List<ChartPoint> TransformRing(IReadOnlyList<ChartPoint> points, SvgRasterMatrix matrix) {
        var transformed = new List<ChartPoint>(points.Count);
        foreach (var point in points) transformed.Add(matrix.Transform(point));
        return transformed;
    }

    private static List<ChartPoint> RectRing(double x, double y, double width, double height) =>
        new() { new ChartPoint(x, y), new ChartPoint(x + width, y), new ChartPoint(x + width, y + height), new ChartPoint(x, y + height) };

    private static List<ChartPoint> RoundedRectRing(double x, double y, double width, double height, double radius) {
        var points = new List<ChartPoint>();
        AddArc(points, x + width - radius, y + radius, radius, -Math.PI / 2, 0);
        AddArc(points, x + width - radius, y + height - radius, radius, 0, Math.PI / 2);
        AddArc(points, x + radius, y + height - radius, radius, Math.PI / 2, Math.PI);
        AddArc(points, x + radius, y + radius, radius, Math.PI, Math.PI * 1.5);
        return points;
    }

    private static List<ChartPoint> EllipseRing(double cx, double cy, double rx, double ry, int segments) {
        var points = new List<ChartPoint>(segments);
        for (var i = 0; i < segments; i++) {
            var angle = Math.PI * 2 * i / segments;
            points.Add(new ChartPoint(cx + Math.Cos(angle) * rx, cy + Math.Sin(angle) * ry));
        }

        return points;
    }

    private static void AddArc(List<ChartPoint> points, double cx, double cy, double radius, double start, double end) {
        const int segments = 8;
        for (var i = 0; i <= segments; i++) {
            var angle = start + (end - start) * i / segments;
            points.Add(new ChartPoint(cx + Math.Cos(angle) * radius, cy + Math.Sin(angle) * radius));
        }
    }

    private static List<ChartPoint> ReadPointList(string? value) {
        var numbers = SvgRasterNumbers.ParseList(value);
        var points = new List<ChartPoint>(numbers.Count / 2);
        for (var i = 0; i + 1 < numbers.Count; i += 2) points.Add(new ChartPoint(numbers[i], numbers[i + 1]));
        return points;
    }

    private static List<ChartPoint> ClosedRing(IReadOnlyList<ChartPoint> points) {
        var closed = new List<ChartPoint>(points);
        if (closed.Count > 1 && DistanceSquared(closed[0], closed[closed.Count - 1]) > 0.000001) closed.Add(closed[0]);
        return closed;
    }

    private static bool PathHasClose(string pathData) =>
        pathData.IndexOf('z') >= 0 || pathData.IndexOf('Z') >= 0;

    private static bool IsBold(string value) =>
        string.Equals(value, "bold", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "600", StringComparison.Ordinal) || string.Equals(value, "700", StringComparison.Ordinal) || string.Equals(value, "800", StringComparison.Ordinal) || string.Equals(value, "900", StringComparison.Ordinal);

    private static double DistanceSquared(ChartPoint a, ChartPoint b) {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    private static ChartColor WithOpacity(ChartColor color, double opacity) {
        opacity = Math.Max(0, Math.Min(1, opacity));
        return ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(color.A * opacity));
    }

    private static bool HasVisiblePixel(byte[] rgba) {
        for (var i = 3; i < rgba.Length; i += 4) if (rgba[i] != 0) return true;
        return false;
    }
}
