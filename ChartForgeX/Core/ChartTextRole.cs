namespace ChartForgeX.Core;

/// <summary>
/// Defines the text role styled by a chart text style.
/// </summary>
public enum ChartTextRole {
    /// <summary>Styles the chart title.</summary>
    Title,
    /// <summary>Styles the chart subtitle.</summary>
    Subtitle,
    /// <summary>Styles x-axis, y-axis, and secondary-axis titles.</summary>
    AxisTitle,
    /// <summary>Styles axis tick and category labels.</summary>
    TickLabel,
    /// <summary>Styles legend labels.</summary>
    Legend,
    /// <summary>Styles data labels rendered inside or near marks.</summary>
    DataLabel
}
