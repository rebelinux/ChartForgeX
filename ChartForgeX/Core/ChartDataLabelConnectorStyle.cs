namespace ChartForgeX.Core;

/// <summary>
/// Defines the connector line shape used by outside and side data labels.
/// </summary>
public enum ChartDataLabelConnectorStyle {
    /// <summary>
    /// Draws a short radial segment followed by a straight leader segment.
    /// </summary>
    Elbow,

    /// <summary>
    /// Draws a single straight leader line from the mark to the label.
    /// </summary>
    Straight,

    /// <summary>
    /// Draws a smooth curved leader line from the mark to the label.
    /// </summary>
    Curve
}
