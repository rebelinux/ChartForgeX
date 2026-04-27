using System;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one weighted flow between two Sankey nodes.
/// </summary>
public readonly struct ChartSankeyLink {
    /// <summary>
    /// Gets the source node label.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the target node label.
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets the positive flow value.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Initializes a new Sankey link.
    /// </summary>
    public ChartSankeyLink(string source, string target, double value) {
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Sankey source must not be empty.", nameof(source));
        if (string.IsNullOrWhiteSpace(target)) throw new ArgumentException("Sankey target must not be empty.", nameof(target));
        ChartGuards.Finite(value, nameof(value));
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Sankey link value must be positive.");
        Source = source;
        Target = target;
        Value = value;
    }
}
