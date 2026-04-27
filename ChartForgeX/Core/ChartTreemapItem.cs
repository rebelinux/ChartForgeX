using System;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one labeled treemap item.
/// </summary>
public readonly struct ChartTreemapItem {
    /// <summary>
    /// Gets the item label.
    /// </summary>
    public readonly string Label;

    /// <summary>
    /// Gets the item value.
    /// </summary>
    public readonly double Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartTreemapItem"/> struct.
    /// </summary>
    /// <param name="label">The item label.</param>
    /// <param name="value">The item value. Negative values are not allowed.</param>
    public ChartTreemapItem(string label, double value) {
        if (label == null) throw new ArgumentNullException(nameof(label));
        ChartGuards.Finite(value, nameof(value));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Treemap item values must be zero or greater.");
        Label = label;
        Value = value;
    }
}
