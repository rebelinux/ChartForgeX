using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one labeled value used to build a Pareto chart.
/// </summary>
public readonly struct ChartParetoItem {
    /// <summary>
    /// Gets the category label.
    /// </summary>
    public readonly string Label;

    /// <summary>
    /// Gets the non-negative item value.
    /// </summary>
    public readonly double Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartParetoItem"/> struct.
    /// </summary>
    /// <param name="label">The category label.</param>
    /// <param name="value">The non-negative item value.</param>
    public ChartParetoItem(string label, double value) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        ChartPrimitiveGuards.Finite(value, nameof(value));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Pareto item values must be non-negative.");
        Value = value;
    }
}
