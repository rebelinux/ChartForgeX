namespace ChartForgeX.Core;

/// <summary>
/// Defines where a chart legend is placed relative to the plot area.
/// </summary>
public enum ChartLegendPosition {
    /// <summary>Places the legend below the plot and centers rows horizontally.</summary>
    Bottom,
    /// <summary>Places the legend above the plot and centers rows horizontally.</summary>
    Top,
    /// <summary>Places the legend to the left of the plot.</summary>
    Left,
    /// <summary>Places the legend to the right of the plot.</summary>
    Right,
    /// <summary>Places the legend above the plot and aligns rows left.</summary>
    TopLeft,
    /// <summary>Places the legend above the plot and aligns rows right.</summary>
    TopRight,
    /// <summary>Places the legend below the plot and aligns rows left.</summary>
    BottomLeft,
    /// <summary>Places the legend below the plot and aligns rows right.</summary>
    BottomRight
}
