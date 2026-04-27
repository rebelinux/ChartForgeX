namespace ChartForgeX.Core;

/// <summary>
/// Defines how charts are fitted into fixed-size chart grid panels.
/// </summary>
public enum ChartGridPanelFit {
    /// <summary>
    /// Preserve each chart's aspect ratio and center it inside the panel.
    /// </summary>
    Contain,

    /// <summary>
    /// Stretch each chart to the full panel size.
    /// </summary>
    Stretch
}
