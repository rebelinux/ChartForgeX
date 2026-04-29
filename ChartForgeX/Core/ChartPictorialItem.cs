using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one labeled value in a pictorial chart.
/// </summary>
public readonly struct ChartPictorialItem {
    /// <summary>
    /// Gets the item label.
    /// </summary>
    public readonly string Label;

    /// <summary>
    /// Gets the item value.
    /// </summary>
    public readonly double Value;

    /// <summary>
    /// Gets the optional item color.
    /// </summary>
    public readonly ChartColor? Color;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartPictorialItem"/> struct.
    /// </summary>
    /// <param name="label">The item label.</param>
    /// <param name="value">The item value. Negative values are not allowed.</param>
    /// <param name="color">An optional color for this item.</param>
    public ChartPictorialItem(string label, double value, ChartColor? color = null) {
        if (label == null) throw new ArgumentNullException(nameof(label));
        ChartGuards.Finite(value, nameof(value));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Pictorial item values must be zero or greater.");
        Label = label;
        Value = value;
        Color = color;
    }
}
