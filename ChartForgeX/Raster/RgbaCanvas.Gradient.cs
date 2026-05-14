using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal readonly struct RasterGradientStop {
    public readonly double Offset;
    public readonly ChartColor Color;

    public RasterGradientStop(double offset, ChartColor color) {
        Offset = Math.Max(0, Math.Min(1, offset));
        Color = color;
    }
}

internal sealed partial class RgbaCanvas {
    internal void FillContoursLinearGradient(IReadOnlyList<List<ChartPoint>> contours, ChartPoint start, ChartPoint end, IReadOnlyList<RasterGradientStop> stops) {
        if (contours.Count == 0 || stops.Count == 0) return;
        var scaledContours = new List<List<ChartPoint>>(contours.Count);
        foreach (var contour in contours) {
            if (contour.Count < 3) continue;
            var scaled = new List<ChartPoint>(contour.Count);
            foreach (var point in contour) scaled.Add(new ChartPoint(point.X * _scale, point.Y * _scale));
            scaledContours.Add(scaled);
        }

        if (scaledContours.Count == 0) return;
        FillContoursLinearGradientPixels(scaledContours, new ChartPoint(start.X * _scale, start.Y * _scale), new ChartPoint(end.X * _scale, end.Y * _scale), stops);
    }

    private void FillContoursLinearGradientPixels(IReadOnlyList<List<ChartPoint>> contours, ChartPoint start, ChartPoint end, IReadOnlyList<RasterGradientStop> stops) {
        var minY = double.PositiveInfinity;
        var maxY = double.NegativeInfinity;
        foreach (var contour in contours) foreach (var point in contour) {
            minY = Math.Min(minY, point.Y);
            maxY = Math.Max(maxY, point.Y);
        }

        var yStart = Math.Max(0, (int)Math.Floor(minY));
        var yEnd = Math.Min(_pixelHeight - 1, (int)Math.Ceiling(maxY));
        var intersections = new List<double>();
        var gradientX = end.X - start.X;
        var gradientY = end.Y - start.Y;
        var gradientLengthSquared = gradientX * gradientX + gradientY * gradientY;

        for (var y = yStart; y <= yEnd; y++) {
            var scanY = y + 0.5;
            intersections.Clear();
            foreach (var contour in contours) {
                for (var i = 0; i < contour.Count; i++) {
                    var a = contour[i];
                    var b = contour[(i + 1) % contour.Count];
                    if ((a.Y <= scanY && b.Y > scanY) || (b.Y <= scanY && a.Y > scanY)) {
                        intersections.Add(a.X + (scanY - a.Y) * (b.X - a.X) / (b.Y - a.Y));
                    }
                }
            }

            intersections.Sort();
            for (var i = 0; i + 1 < intersections.Count; i += 2) {
                var left = intersections[i];
                var right = intersections[i + 1];
                var xStart = Math.Max(0, (int)Math.Floor(left));
                var xEnd = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(right));
                for (var x = xStart; x <= xEnd; x++) {
                    var coverage = Math.Min(x + 1.0, right) - Math.Max(x, left);
                    if (coverage <= 0) continue;
                    var amount = gradientLengthSquared <= 0.000001 ? 0 : ((x + 0.5 - start.X) * gradientX + (scanY - start.Y) * gradientY) / gradientLengthSquared;
                    var color = SampleGradient(stops, amount);
                    BlendPixel(x, y, coverage >= 1 ? color : WithOpacity(color, coverage));
                }
            }
        }
    }

    private static ChartColor SampleGradient(IReadOnlyList<RasterGradientStop> stops, double amount) {
        amount = Math.Max(0, Math.Min(1, amount));
        var previous = stops[0];
        for (var i = 1; i < stops.Count; i++) {
            var next = stops[i];
            if (amount > next.Offset) {
                previous = next;
                continue;
            }

            var span = Math.Max(0.000001, next.Offset - previous.Offset);
            return Mix(previous.Color, next.Color, (amount - previous.Offset) / span);
        }

        return previous.Color;
    }

    private static ChartColor Mix(ChartColor left, ChartColor right, double amount) {
        amount = Math.Max(0, Math.Min(1, amount));
        var inverse = 1 - amount;
        return ChartColor.FromRgba(
            (byte)Math.Round(left.R * inverse + right.R * amount),
            (byte)Math.Round(left.G * inverse + right.G * amount),
            (byte)Math.Round(left.B * inverse + right.B * amount),
            (byte)Math.Round(left.A * inverse + right.A * amount));
    }
}
