using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>
/// Defines reusable brand tokens that can be applied to charts, grids, and themes.
/// </summary>
public sealed class ChartBrandKit {
    private ChartColor[]? _palette;
    private string? _fontFamily;
    private ChartSurfaceStyle? _surfaceStyle;
    private ChartColor? _background;
    private ChartColor? _cardBackground;
    private ChartColor? _plotBackground;
    private ChartColor? _cardBorder;
    private ChartColor? _plotBorder;
    private ChartColor? _text;
    private ChartColor? _mutedText;
    private ChartColor? _grid;
    private ChartColor? _axis;
    private ChartColor? _positive;
    private ChartColor? _warning;
    private ChartColor? _negative;

    /// <summary>
    /// Gets or sets the optional series palette.
    /// </summary>
    public ChartColor[]? Palette {
        get => _palette == null ? null : (ChartColor[])_palette.Clone();
        set {
            if (value != null && value.Length == 0) throw new ArgumentException("Brand palette must contain at least one color.", nameof(value));
            _palette = value == null ? null : (ChartColor[])value.Clone();
        }
    }

    /// <summary>
    /// Gets or sets the optional CSS font-family stack.
    /// </summary>
    public string? FontFamily {
        get => _fontFamily;
        set {
            if (value != null && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Brand font family must not be empty.", nameof(value));
            _fontFamily = value;
        }
    }

    /// <summary>
    /// Gets or sets the optional surface style preset.
    /// </summary>
    public ChartSurfaceStyle? SurfaceStyle {
        get => _surfaceStyle;
        set {
            if (value.HasValue && !Enum.IsDefined(typeof(ChartSurfaceStyle), value.Value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown chart surface style.");
            _surfaceStyle = value;
        }
    }

    /// <summary>
    /// Creates an empty brand kit.
    /// </summary>
    /// <returns>A brand kit.</returns>
    public static ChartBrandKit Create() => new();

    /// <summary>
    /// Creates a restrained executive report brand kit.
    /// </summary>
    /// <returns>An executive brand kit.</returns>
    public static ChartBrandKit Executive() => Create()
        .WithPalette("#1E40AF", "#0F766E", "#9333EA", "#B45309", "#BE123C", "#475569", "#0284C7", "#16A34A")
        .WithFontFamily(ChartFontStacks.SystemSans)
        .WithSurfaceColors(ChartColor.Transparent, ChartColor.White, ChartColor.FromHex("#F8FAFC"), ChartColor.FromRgba(100, 116, 139, 74), ChartColor.FromRgba(148, 163, 184, 60))
        .WithTextColors(ChartColor.FromHex("#0F172A"), ChartColor.FromHex("#475569"))
        .WithGuideColors(ChartColor.FromRgba(148, 163, 184, 82), ChartColor.FromRgba(71, 85, 105, 150))
        .WithSemanticColors(ChartColor.FromHex("#059669"), ChartColor.FromHex("#D97706"), ChartColor.FromHex("#DC2626"))
        .WithSurfaceStyle(ChartSurfaceStyle.Framed);

    /// <summary>
    /// Creates a vivid product-dashboard brand kit.
    /// </summary>
    /// <returns>A product brand kit.</returns>
    public static ChartBrandKit Product() => Create()
        .WithPalette("#0EA5E9", "#EC4899", "#14B8A6", "#A855F7", "#F59E0B", "#6366F1", "#22C55E", "#F43F5E")
        .WithFontFamily(ChartFontStacks.Geometric)
        .WithSurfaceColors(ChartColor.Transparent, ChartColor.White, ChartColor.FromHex("#F0F9FF"), ChartColor.FromRgba(14, 165, 233, 64), ChartColor.FromRgba(20, 184, 166, 54))
        .WithTextColors(ChartColor.FromHex("#111827"), ChartColor.FromHex("#4F46E5"))
        .WithGuideColors(ChartColor.FromRgba(125, 211, 252, 90), ChartColor.FromRgba(79, 70, 229, 140))
        .WithSemanticColors(ChartColor.FromHex("#14B8A6"), ChartColor.FromHex("#F59E0B"), ChartColor.FromHex("#F43F5E"))
        .WithSurfaceStyle(ChartSurfaceStyle.Floating);

    /// <summary>
    /// Creates a clean cyan and magenta people-infographic brand kit.
    /// </summary>
    /// <returns>A people-infographic brand kit.</returns>
    public static ChartBrandKit PeopleInfographic() => Create()
        .WithPalette(ChartPalettes.PeopleInfographic)
        .WithFontFamily(ChartFontStacks.Geometric)
        .WithSurfaceColors(ChartColor.Transparent, ChartColor.White, ChartColor.FromHex("#F8FAFC"), ChartColor.FromRgba(15, 23, 42, 44), ChartColor.FromRgba(100, 116, 139, 46))
        .WithTextColors(ChartColor.FromHex("#0F172A"), ChartColor.FromHex("#475569"))
        .WithGuideColors(ChartColor.FromRgba(148, 163, 184, 86), ChartColor.FromRgba(51, 65, 85, 150))
        .WithSemanticColors(ChartColor.FromHex("#06B6D4"), ChartColor.FromHex("#FB923C"), ChartColor.FromHex("#DB2777"))
        .WithSurfaceStyle(ChartSurfaceStyle.Framed);

    /// <summary>
    /// Creates a publication-oriented editorial brand kit.
    /// </summary>
    /// <returns>An editorial brand kit.</returns>
    public static ChartBrandKit Editorial() => Create()
        .WithPalette("#1E40AF", "#BE123C", "#059669", "#92400E", "#581C87", "#0F766E", "#7F1D1D", "#334155")
        .WithFontFamily(ChartFontStacks.Serif)
        .WithSurfaceColors(ChartColor.Transparent, ChartColor.FromHex("#FFFCF7"), ChartColor.FromHex("#FAF6EE"), ChartColor.FromRgba(120, 113, 108, 72), ChartColor.FromRgba(168, 162, 158, 58))
        .WithTextColors(ChartColor.FromHex("#1C1917"), ChartColor.FromHex("#57534E"))
        .WithGuideColors(ChartColor.FromRgba(168, 162, 158, 74), ChartColor.FromRgba(68, 64, 60, 140))
        .WithSemanticColors(ChartColor.FromHex("#166534"), ChartColor.FromHex("#B45309"), ChartColor.FromHex("#B91C1C"))
        .WithSurfaceStyle(ChartSurfaceStyle.Framed);

    /// <summary>
    /// Creates a high-contrast colorblind-friendly brand kit.
    /// </summary>
    /// <returns>An accessible brand kit.</returns>
    public static ChartBrandKit Accessible() => Create()
        .WithPalette(ChartPalettes.Colorblind)
        .WithFontFamily(ChartFontStacks.SystemSans)
        .WithSurfaceColors(ChartColor.Transparent, ChartColor.White, ChartColor.FromHex("#FAFAF7"), ChartColor.FromRgba(82, 82, 82, 68), ChartColor.FromRgba(82, 82, 82, 52))
        .WithTextColors(ChartColor.FromHex("#18181B"), ChartColor.FromHex("#52525B"))
        .WithGuideColors(ChartColor.FromRgba(113, 113, 122, 76), ChartColor.FromRgba(39, 39, 42, 160))
        .WithSemanticColors(ChartColor.FromHex("#009E73"), ChartColor.FromHex("#E69F00"), ChartColor.FromHex("#D55E00"))
        .WithSurfaceStyle(ChartSurfaceStyle.Framed);

    /// <summary>
    /// Applies this brand kit to a theme.
    /// </summary>
    /// <param name="theme">The theme to update.</param>
    /// <returns>The updated theme.</returns>
    public ChartTheme ApplyTo(ChartTheme theme) {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        if (_palette != null) theme.Palette = _palette;
        if (_fontFamily != null) theme.FontFamily = _fontFamily;
        if (_background.HasValue) theme.Background = _background.Value;
        if (_cardBackground.HasValue) theme.CardBackground = _cardBackground.Value;
        if (_plotBackground.HasValue) theme.PlotBackground = _plotBackground.Value;
        if (_cardBorder.HasValue) theme.CardBorder = _cardBorder.Value;
        if (_plotBorder.HasValue) theme.PlotBorder = _plotBorder.Value;
        if (_text.HasValue) theme.Text = _text.Value;
        if (_mutedText.HasValue) theme.MutedText = _mutedText.Value;
        if (_grid.HasValue) theme.Grid = _grid.Value;
        if (_axis.HasValue) theme.Axis = _axis.Value;
        if (_positive.HasValue) theme.Positive = _positive.Value;
        if (_warning.HasValue) theme.Warning = _warning.Value;
        if (_negative.HasValue) theme.Negative = _negative.Value;
        if (_surfaceStyle.HasValue) theme.WithSurfaceStyle(_surfaceStyle.Value);
        return theme;
    }

    /// <summary>
    /// Applies a brand palette.
    /// </summary>
    /// <param name="colors">The palette colors.</param>
    /// <returns>The current brand kit.</returns>
    public ChartBrandKit WithPalette(params ChartColor[] colors) {
        Palette = colors;
        return this;
    }

    /// <summary>
    /// Applies a brand palette from hexadecimal color strings.
    /// </summary>
    /// <param name="colors">The color strings in #RGB, #RGBA, #RRGGBB, or #RRGGBBAA notation.</param>
    /// <returns>The current brand kit.</returns>
    public ChartBrandKit WithPalette(params string[] colors) {
        Palette = ChartPalettes.FromHex(colors);
        return this;
    }

    /// <summary>
    /// Applies a CSS font-family stack.
    /// </summary>
    /// <param name="fontFamily">The CSS font-family stack.</param>
    /// <returns>The current brand kit.</returns>
    public ChartBrandKit WithFontFamily(string fontFamily) {
        FontFamily = fontFamily;
        return this;
    }

    /// <summary>
    /// Applies a surface style preset.
    /// </summary>
    /// <param name="style">The surface style preset.</param>
    /// <returns>The current brand kit.</returns>
    public ChartBrandKit WithSurfaceStyle(ChartSurfaceStyle style) {
        SurfaceStyle = style;
        return this;
    }

    /// <summary>
    /// Applies background and surface colors.
    /// </summary>
    /// <param name="background">The full chart background.</param>
    /// <param name="cardBackground">The outer card background.</param>
    /// <param name="plotBackground">The plot area background.</param>
    /// <param name="cardBorder">The outer card border.</param>
    /// <param name="plotBorder">The plot area border.</param>
    /// <returns>The current brand kit.</returns>
    public ChartBrandKit WithSurfaceColors(ChartColor background, ChartColor cardBackground, ChartColor plotBackground, ChartColor cardBorder, ChartColor plotBorder) {
        _background = background;
        _cardBackground = cardBackground;
        _plotBackground = plotBackground;
        _cardBorder = cardBorder;
        _plotBorder = plotBorder;
        return this;
    }

    /// <summary>
    /// Applies primary and secondary text colors.
    /// </summary>
    /// <param name="text">The primary text color.</param>
    /// <param name="mutedText">The secondary text color.</param>
    /// <returns>The current brand kit.</returns>
    public ChartBrandKit WithTextColors(ChartColor text, ChartColor mutedText) {
        _text = text;
        _mutedText = mutedText;
        return this;
    }

    /// <summary>
    /// Applies grid and axis guide colors.
    /// </summary>
    /// <param name="grid">The grid line color.</param>
    /// <param name="axis">The axis line color.</param>
    /// <returns>The current brand kit.</returns>
    public ChartBrandKit WithGuideColors(ChartColor grid, ChartColor axis) {
        _grid = grid;
        _axis = axis;
        return this;
    }

    /// <summary>
    /// Applies semantic status colors.
    /// </summary>
    /// <param name="positive">The positive status color.</param>
    /// <param name="warning">The warning status color.</param>
    /// <param name="negative">The negative status color.</param>
    /// <returns>The current brand kit.</returns>
    public ChartBrandKit WithSemanticColors(ChartColor positive, ChartColor warning, ChartColor negative) {
        _positive = positive;
        _warning = warning;
        _negative = negative;
        return this;
    }
}
