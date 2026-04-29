namespace ChartForgeX.Themes;

/// <summary>
/// Defines reusable card and plot surface presets for chart themes.
/// </summary>
public enum ChartSurfaceStyle {
    /// <summary>
    /// Uses the theme's standard rounded card surface.
    /// </summary>
    Default,

    /// <summary>
    /// Uses square, shadow-free surfaces.
    /// </summary>
    Flat,

    /// <summary>
    /// Uses a bordered, shadow-free report frame.
    /// </summary>
    Framed,

    /// <summary>
    /// Uses a more elevated rounded card surface.
    /// </summary>
    Floating,

    /// <summary>
    /// Uses semi-transparent card and plot surfaces.
    /// </summary>
    Glass,

    /// <summary>
    /// Removes card and plot surfaces for embedding into an existing layout.
    /// </summary>
    Bare,

    /// <summary>
    /// Uses tighter radii, smaller typography, and a lighter shadow for dense report panels.
    /// </summary>
    Compact
}
