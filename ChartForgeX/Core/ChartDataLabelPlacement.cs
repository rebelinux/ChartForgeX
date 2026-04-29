namespace ChartForgeX.Core;

/// <summary>
/// Defines preferred placement for data labels on capable chart renderers.
/// </summary>
public enum ChartDataLabelPlacement {
    /// <summary>
    /// Lets the renderer choose an edge-aware placement.
    /// </summary>
    Auto,

    /// <summary>
    /// Places labels above the mark when possible.
    /// </summary>
    Above,

    /// <summary>
    /// Places labels below the mark when possible.
    /// </summary>
    Below,

    /// <summary>
    /// Places labels inside the mark when possible.
    /// </summary>
    Inside,

    /// <summary>
    /// Places labels outside the mark when possible.
    /// </summary>
    Outside,

    /// <summary>
    /// Places labels centered on the mark when possible.
    /// </summary>
    Center,

    /// <summary>
    /// Places labels to the left of the mark when possible.
    /// </summary>
    Left,

    /// <summary>
    /// Places labels to the right of the mark when possible.
    /// </summary>
    Right
}
