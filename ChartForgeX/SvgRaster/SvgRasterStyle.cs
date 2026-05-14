using System;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.SvgRaster;

internal sealed class SvgRasterStyle {
    public static SvgRasterStyle Default => new() {
        Fill = ChartColor.Black,
        Stroke = null,
        Color = ChartColor.Black,
        Opacity = 1,
        FillOpacity = 1,
        StrokeOpacity = 1,
        StrokeWidth = 1,
        FontSize = 16,
        FontWeight = "normal",
        TextAnchor = "start",
        Visible = true
    };

    public ChartColor? Fill { get; set; }
    public ChartColor? Stroke { get; set; }
    public ChartColor Color { get; set; }
    public double Opacity { get; set; }
    public double FillOpacity { get; set; }
    public double StrokeOpacity { get; set; }
    public double StrokeWidth { get; set; }
    public double FontSize { get; set; }
    public string FontWeight { get; set; } = "normal";
    public string TextAnchor { get; set; } = "start";
    public bool Visible { get; set; }

    public SvgRasterStyle Inherit() =>
        new() {
            Fill = Fill,
            Stroke = Stroke,
            Color = Color,
            Opacity = Opacity,
            FillOpacity = FillOpacity,
            StrokeOpacity = StrokeOpacity,
            StrokeWidth = StrokeWidth,
            FontSize = FontSize,
            FontWeight = FontWeight,
            TextAnchor = TextAnchor,
            Visible = Visible
        };

    public static SvgRasterStyle Resolve(SvgRasterStyle parent, SvgRasterElement element) {
        var style = parent.Inherit();
        ApplyPresentation(style, element);
        var inline = element.Get("style");
        if (!string.IsNullOrWhiteSpace(inline)) {
            foreach (var declaration in SvgStyleDeclarationList.Parse(inline!).Declarations) {
                Apply(style, declaration.Name, declaration.Value);
            }
        }

        return style;
    }

    public ChartColor FillColor() => WithOpacity(Fill ?? ChartColor.Transparent, Opacity * FillOpacity);

    public ChartColor StrokeColor() => WithOpacity(Stroke ?? ChartColor.Transparent, Opacity * StrokeOpacity);

    private static void ApplyPresentation(SvgRasterStyle style, SvgRasterElement element) {
        ApplyAttribute(style, element, "color");
        ApplyAttribute(style, element, "fill");
        ApplyAttribute(style, element, "stroke");
        ApplyAttribute(style, element, "stroke-width");
        ApplyAttribute(style, element, "opacity");
        ApplyAttribute(style, element, "fill-opacity");
        ApplyAttribute(style, element, "stroke-opacity");
        ApplyAttribute(style, element, "font-size");
        ApplyAttribute(style, element, "font-weight");
        ApplyAttribute(style, element, "text-anchor");
        ApplyAttribute(style, element, "display");
        ApplyAttribute(style, element, "visibility");
    }

    private static void ApplyAttribute(SvgRasterStyle style, SvgRasterElement element, string name) {
        if (element.TryGet(name, out var value)) Apply(style, name, value);
    }

    private static void Apply(SvgRasterStyle style, string name, string value) {
        if (string.IsNullOrWhiteSpace(value)) return;
        switch (name) {
            case "color":
                style.Color = ParsePaint(value, style.Color) ?? style.Color;
                break;
            case "fill":
                style.Fill = ParsePaint(value, style.Color);
                break;
            case "stroke":
                style.Stroke = ParsePaint(value, style.Color);
                break;
            case "stroke-width":
                style.StrokeWidth = Math.Max(0, ParseLength(value, style.StrokeWidth));
                break;
            case "opacity":
                style.Opacity = ParseOpacity(value, style.Opacity);
                break;
            case "fill-opacity":
                style.FillOpacity = ParseOpacity(value, style.FillOpacity);
                break;
            case "stroke-opacity":
                style.StrokeOpacity = ParseOpacity(value, style.StrokeOpacity);
                break;
            case "font-size":
                style.FontSize = Math.Max(1, ParseLength(value, style.FontSize));
                break;
            case "font-weight":
                style.FontWeight = value.Trim();
                break;
            case "text-anchor":
                style.TextAnchor = value.Trim();
                break;
            case "display":
                if (string.Equals(value.Trim(), "none", StringComparison.OrdinalIgnoreCase)) style.Visible = false;
                break;
            case "visibility":
                if (string.Equals(value.Trim(), "hidden", StringComparison.OrdinalIgnoreCase) || string.Equals(value.Trim(), "collapse", StringComparison.OrdinalIgnoreCase)) style.Visible = false;
                break;
        }
    }

    private static ChartColor? ParsePaint(string value, ChartColor currentColor) {
        var trimmed = value.Trim();
        if (string.Equals(trimmed, "none", StringComparison.OrdinalIgnoreCase)) return null;
        if (string.Equals(trimmed, "currentColor", StringComparison.OrdinalIgnoreCase)) return currentColor;
        if (ChartColor.TryParse(trimmed, out var color)) return color;
        return null;
    }

    private static double ParseOpacity(string value, double fallback) {
        var trimmed = value.Trim();
        var percent = trimmed.EndsWith("%", StringComparison.Ordinal);
        if (percent) trimmed = trimmed.Substring(0, trimmed.Length - 1);
        if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity)) return fallback;
        if (percent) opacity /= 100.0;
        return Math.Max(0, Math.Min(1, opacity));
    }

    private static double ParseLength(string value, double fallback) {
        var trimmed = value.Trim();
        if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase)) trimmed = trimmed.Substring(0, trimmed.Length - 2);
        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        return fallback;
    }

    private static ChartColor WithOpacity(ChartColor color, double opacity) {
        opacity = Math.Max(0, Math.Min(1, opacity));
        return ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(color.A * opacity));
    }
}
