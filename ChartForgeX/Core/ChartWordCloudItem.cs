using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one weighted term in a word cloud.
/// </summary>
public readonly struct ChartWordCloudItem {
    /// <summary>
    /// Gets the term text.
    /// </summary>
    public readonly string Text;

    /// <summary>
    /// Gets the term weight.
    /// </summary>
    public readonly double Weight;

    /// <summary>
    /// Gets the optional term color.
    /// </summary>
    public readonly ChartColor? Color;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartWordCloudItem"/> struct.
    /// </summary>
    /// <param name="text">The term text.</param>
    /// <param name="weight">The term weight. Negative values are not allowed.</param>
    /// <param name="color">An optional color for this term.</param>
    public ChartWordCloudItem(string text, double weight, ChartColor? color = null) {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Word cloud text must not be empty.", nameof(text));
        ChartGuards.Finite(weight, nameof(weight));
        if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight), weight, "Word cloud weights must be zero or greater.");
        Text = text;
        Weight = weight;
        Color = color;
    }
}
