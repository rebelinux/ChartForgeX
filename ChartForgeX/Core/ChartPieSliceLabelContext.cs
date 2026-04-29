namespace ChartForgeX.Core;

/// <summary>
/// Provides pie and donut slice values to custom slice label formatters.
/// </summary>
public readonly struct ChartPieSliceLabelContext {
    /// <summary>
    /// Initializes a new instance of the <see cref="ChartPieSliceLabelContext"/> struct.
    /// </summary>
    public ChartPieSliceLabelContext(string seriesName, string label, double value, double percent, string formattedValue, string formattedPercent, int pointIndex) {
        SeriesName = seriesName;
        Label = label;
        Value = value;
        Percent = percent;
        FormattedValue = formattedValue;
        FormattedPercent = formattedPercent;
        PointIndex = pointIndex;
    }

    /// <summary>Gets the series name.</summary>
    public string SeriesName { get; }

    /// <summary>Gets the slice category label.</summary>
    public string Label { get; }

    /// <summary>Gets the raw slice value.</summary>
    public double Value { get; }

    /// <summary>Gets the slice percentage as a ratio from zero to one.</summary>
    public double Percent { get; }

    /// <summary>Gets the slice value formatted with the chart value formatter.</summary>
    public string FormattedValue { get; }

    /// <summary>Gets the slice percentage formatted as display text.</summary>
    public string FormattedPercent { get; }

    /// <summary>Gets the zero-based point index.</summary>
    public int PointIndex { get; }
}
