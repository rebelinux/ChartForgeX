using System;

namespace ChartForgeX.Core;

/// <summary>
/// Describes how many grid tracks a chart panel should occupy in a chart grid.
/// </summary>
public readonly struct ChartGridPanelSpan {
    /// <summary>
    /// Initializes a new panel span.
    /// </summary>
    /// <param name="columnSpan">The number of grid columns occupied by the panel.</param>
    /// <param name="rowSpan">The number of grid rows occupied by the panel.</param>
    public ChartGridPanelSpan(int columnSpan, int rowSpan = 1) {
        if (columnSpan < 1 || columnSpan > 12) throw new ArgumentOutOfRangeException(nameof(columnSpan), columnSpan, "Column span must be between one and twelve.");
        if (rowSpan < 1 || rowSpan > 12) throw new ArgumentOutOfRangeException(nameof(rowSpan), rowSpan, "Row span must be between one and twelve.");
        ColumnSpan = columnSpan;
        RowSpan = rowSpan;
    }

    /// <summary>
    /// Gets the number of grid columns occupied by the panel.
    /// </summary>
    public int ColumnSpan { get; }

    /// <summary>
    /// Gets the number of grid rows occupied by the panel.
    /// </summary>
    public int RowSpan { get; }
}
