using System.Collections.Generic;
using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one named series of points in a chart.
/// </summary>
public sealed class ChartSeries {
    private double _strokeWidth = 3;
    private ChartAxisSide _yAxis = ChartAxisSide.Primary;

    /// <summary>
    /// Gets the display name shown in legends.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the series rendering kind.
    /// </summary>
    public ChartSeriesKind Kind { get; }

    /// <summary>
    /// Gets the ordered data points in the series.
    /// </summary>
    public List<ChartPoint> Points { get; } = new();

    /// <summary>
    /// Gets or sets the series color. When null, the chart theme palette is used.
    /// </summary>
    public ChartColor? Color { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether capable renderers should smooth connected line segments.
    /// </summary>
    public bool Smooth { get; set; }

    /// <summary>
    /// Gets or sets a series-specific data-label override. When null, the chart-level setting is used.
    /// </summary>
    public bool? ShowDataLabels { get; set; }

    /// <summary>
    /// Gets or sets the vertical axis used for cartesian rendering.
    /// </summary>
    public ChartAxisSide YAxis {
        get => _yAxis;
        set {
            if (!Enum.IsDefined(typeof(ChartAxisSide), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown y-axis side.");
            _yAxis = value;
        }
    }

    /// <summary>
    /// Gets or sets the stroke width for line and area series.
    /// </summary>
    public double StrokeWidth {
        get => _strokeWidth;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Stroke width must be greater than zero.");
            _strokeWidth = value;
        }
    }

    /// <summary>
    /// Sets a series-specific data-label visibility override.
    /// </summary>
    /// <param name="visible">A value indicating whether labels should render for this series.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithDataLabels(bool visible = true) {
        ShowDataLabels = visible;
        return this;
    }

    /// <summary>
    /// Clears the series-specific data-label visibility override and uses the chart-level setting.
    /// </summary>
    /// <returns>The current series.</returns>
    public ChartSeries UseChartDataLabels() {
        ShowDataLabels = null;
        return this;
    }

    /// <summary>
    /// Assigns the series to the primary left y-axis.
    /// </summary>
    /// <returns>The current series.</returns>
    public ChartSeries UsePrimaryYAxis() {
        YAxis = ChartAxisSide.Primary;
        return this;
    }

    /// <summary>
    /// Assigns the series to the secondary right y-axis.
    /// </summary>
    /// <returns>The current series.</returns>
    public ChartSeries UseSecondaryYAxis() {
        YAxis = ChartAxisSide.Secondary;
        return this;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartSeries"/> class.
    /// </summary>
    /// <param name="name">The display name shown in legends.</param>
    /// <param name="kind">The series rendering kind.</param>
    /// <param name="points">The ordered data points.</param>
    public ChartSeries(string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points) {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Kind = kind;
        Points.AddRange(ChartGuards.Points(points, nameof(points)));
    }
}
