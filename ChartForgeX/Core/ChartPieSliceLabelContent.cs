namespace ChartForgeX.Core;

/// <summary>
/// Defines the text rendered for pie and donut slice data labels.
/// </summary>
public enum ChartPieSliceLabelContent {
    /// <summary>Renders only the slice percentage.</summary>
    Percent,

    /// <summary>Renders only the formatted slice value.</summary>
    Value,

    /// <summary>Renders only the slice category label.</summary>
    Label,

    /// <summary>Renders the slice category label followed by its percentage.</summary>
    LabelAndPercent,

    /// <summary>Renders the slice category label followed by its formatted value.</summary>
    LabelAndValue
}
