using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed class RgbaCanvas {
    public int Width { get; }
    public int Height { get; }
    public byte[] Pixels { get; }

    public RgbaCanvas(int width, int height) {
        Width = width;
        Height = height;
        Pixels = new byte[width * height * 4];
    }

    public void Clear(ChartColor color) {
        for (var y = 0; y < Height; y++) for (var x = 0; x < Width; x++) BlendPixel(x, y, color);
    }

    public void FillRect(double x, double y, double width, double height, ChartColor color) {
        var x1 = Math.Max(0, (int)Math.Floor(x)); var y1 = Math.Max(0, (int)Math.Floor(y));
        var x2 = Math.Min(Width, (int)Math.Ceiling(x + width)); var y2 = Math.Min(Height, (int)Math.Ceiling(y + height));
        for (var yy = y1; yy < y2; yy++) for (var xx = x1; xx < x2; xx++) BlendPixel(xx, yy, color);
    }

    public void DrawLine(double x0, double y0, double x1, double y1, ChartColor color, int thickness) {
        var dx = x1 - x0; var dy = y1 - y0; var steps = (int)Math.Max(Math.Abs(dx), Math.Abs(dy));
        if (steps == 0) { DrawCircle(x0, y0, thickness / 2.0, color); return; }
        for (var i = 0; i <= steps; i++) {
            var f = i / (double)steps;
            DrawCircle(x0 + dx * f, y0 + dy * f, Math.Max(1, thickness / 2.0), color);
        }
    }

    public void DrawCircle(double cx, double cy, double radius, ChartColor color) {
        var r2 = radius * radius;
        var x1 = Math.Max(0, (int)Math.Floor(cx - radius)); var y1 = Math.Max(0, (int)Math.Floor(cy - radius));
        var x2 = Math.Min(Width - 1, (int)Math.Ceiling(cx + radius)); var y2 = Math.Min(Height - 1, (int)Math.Ceiling(cy + radius));
        for (var y = y1; y <= y2; y++) for (var x = x1; x <= x2; x++) {
            var dx = x + .5 - cx; var dy = y + .5 - cy;
            if (dx * dx + dy * dy <= r2) BlendPixel(x, y, color);
        }
    }

    public void DrawTextTiny(double x, double y, string text, ChartColor color, int scale = 2) {
        var cursor = (int)x;
        foreach (var ch in text.ToUpperInvariant()) {
            DrawGlyph(cursor, (int)y, ch, color, scale);
            cursor += 4 * scale;
        }
    }

    private void DrawGlyph(int x, int y, char ch, ChartColor color, int scale) {
        var g = TinyFont.Get(ch);
        for (var row = 0; row < 5; row++) for (var col = 0; col < 3; col++) {
            if (((g[row] >> (2 - col)) & 1) == 1) FillRect(x + col * scale, y + row * scale, scale, scale, color);
        }
    }

    private void BlendPixel(int x, int y, ChartColor src) {
        if (x < 0 || y < 0 || x >= Width || y >= Height || src.A == 0) return;
        var i = (y * Width + x) * 4;
        if (src.A == 255) { Pixels[i] = src.R; Pixels[i + 1] = src.G; Pixels[i + 2] = src.B; Pixels[i + 3] = 255; return; }
        var sa = src.A / 255.0;
        var da = Pixels[i + 3] / 255.0;
        var oa = sa + da * (1 - sa);
        if (oa <= 0) return;
        Pixels[i] = (byte)((src.R * sa + Pixels[i] * da * (1 - sa)) / oa);
        Pixels[i + 1] = (byte)((src.G * sa + Pixels[i + 1] * da * (1 - sa)) / oa);
        Pixels[i + 2] = (byte)((src.B * sa + Pixels[i + 2] * da * (1 - sa)) / oa);
        Pixels[i + 3] = (byte)(oa * 255);
    }
}
