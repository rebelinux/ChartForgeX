using System;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one parent-child relationship in a tree chart.
/// </summary>
public readonly struct ChartTreeLink {
    /// <summary>
    /// Gets the parent node label.
    /// </summary>
    public string Parent { get; }

    /// <summary>
    /// Gets the child node label.
    /// </summary>
    public string Child { get; }

    /// <summary>
    /// Gets the optional positive node weight.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Initializes a new tree link.
    /// </summary>
    public ChartTreeLink(string parent, string child, double value = 1) {
        if (string.IsNullOrWhiteSpace(parent)) throw new ArgumentException("Tree parent must not be empty.", nameof(parent));
        if (string.IsNullOrWhiteSpace(child)) throw new ArgumentException("Tree child must not be empty.", nameof(child));
        ChartGuards.Finite(value, nameof(value));
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Tree link value must be positive.");
        Parent = parent;
        Child = child;
        Value = value;
    }
}
