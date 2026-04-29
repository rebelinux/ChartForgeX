using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Defines optional text styling overrides for a chart text role.
/// </summary>
public sealed class ChartTextStyle {
    private string? _fontFamily;
    private string? _fontWeight;
    private double? _fontSize;

    /// <summary>
    /// Gets or sets the optional text color override.
    /// </summary>
    public ChartColor? Color { get; set; }

    /// <summary>
    /// Gets or sets the optional CSS font-family override.
    /// </summary>
    public string? FontFamily {
        get => _fontFamily;
        set => _fontFamily = value == null || string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Gets or sets the optional CSS font-weight override.
    /// </summary>
    public string? FontWeight {
        get => _fontWeight;
        set => _fontWeight = value == null || string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Gets or sets the optional font size override.
    /// </summary>
    public double? FontSize {
        get => _fontSize;
        set {
            if (value.HasValue) {
                ChartGuards.Finite(value.Value, nameof(value));
                if (value.Value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Font size must be greater than zero.");
            }

            _fontSize = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether text is italic.
    /// </summary>
    public bool Italic { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether text is underlined.
    /// </summary>
    public bool Underline { get; set; }

    /// <summary>
    /// Gets a value indicating whether this style contains any explicit overrides.
    /// </summary>
    public bool HasOverrides => Color.HasValue || FontFamily != null || FontWeight != null || FontSize.HasValue || Italic || Underline;

    /// <summary>
    /// Sets the text color.
    /// </summary>
    public ChartTextStyle WithColor(ChartColor color) { Color = color; return this; }

    /// <summary>
    /// Sets the text color from a hexadecimal color string.
    /// </summary>
    public ChartTextStyle WithColor(string hex) { Color = ChartColor.FromHex(hex); return this; }

    /// <summary>
    /// Sets the CSS font-family.
    /// </summary>
    public ChartTextStyle WithFontFamily(string fontFamily) { FontFamily = fontFamily ?? throw new ArgumentNullException(nameof(fontFamily)); return this; }

    /// <summary>
    /// Sets the font size.
    /// </summary>
    public ChartTextStyle WithFontSize(double fontSize) { FontSize = fontSize; return this; }

    /// <summary>
    /// Sets the CSS font-weight.
    /// </summary>
    public ChartTextStyle WithWeight(string fontWeight) { FontWeight = fontWeight ?? throw new ArgumentNullException(nameof(fontWeight)); return this; }

    /// <summary>
    /// Sets italic text.
    /// </summary>
    public ChartTextStyle WithItalic(bool enabled = true) { Italic = enabled; return this; }

    /// <summary>
    /// Sets underlined text.
    /// </summary>
    public ChartTextStyle WithUnderline(bool enabled = true) { Underline = enabled; return this; }
}
