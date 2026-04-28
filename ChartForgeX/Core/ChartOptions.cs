using System;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>
/// Defines renderer-independent options for a chart.
/// </summary>
public sealed class ChartOptions {
    private ChartSize _size = new(1000, 560);
    private ChartPadding _padding = new(76, 78, 36, 74);
    private ChartTheme _theme = ChartTheme.Light();
    private int _tickCount = 6;
    private ChartLabelDensity _xAxisLabelDensity = ChartLabelDensity.Auto;
    private double _xAxisLabelAngle;
    private ChartBarMode _barMode = ChartBarMode.Grouped;
    private ChartHeatmapScale _heatmapScale = ChartHeatmapScale.Sequential;
    private string? _pngFontPath;
    private string? _pngFontFaceName;
    private int? _pngFontCollectionIndex;
    private int _pngSupersamplingScale = 2;
    private int _pngOutputScale = 1;
    private double? _xAxisMinimum;
    private double? _xAxisMaximum;
    private double? _yAxisMinimum;
    private double? _yAxisMaximum;
    private double? _secondaryYAxisMinimum;
    private double? _secondaryYAxisMaximum;
    private double? _ganttToday;

    /// <summary>
    /// Gets or sets the rendered chart size in pixels.
    /// </summary>
    public ChartSize Size {
        get => _size;
        set {
            if (value.Width <= 0 || value.Height <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Chart size must have positive dimensions.");
            _size = value;
        }
    }

    /// <summary>
    /// Gets or sets the chart padding around the plot area.
    /// </summary>
    public ChartPadding Padding {
        get => _padding;
        set {
            ChartGuards.Finite(value.Left, nameof(value));
            ChartGuards.Finite(value.Top, nameof(value));
            ChartGuards.Finite(value.Right, nameof(value));
            ChartGuards.Finite(value.Bottom, nameof(value));
            if (value.Left < 0 || value.Top < 0 || value.Right < 0 || value.Bottom < 0) throw new ArgumentOutOfRangeException(nameof(value), "Chart padding values must be non-negative.");
            _padding = value;
        }
    }

    /// <summary>
    /// Gets or sets the visual theme used by renderers.
    /// </summary>
    public ChartTheme Theme {
        get => _theme;
        set => _theme = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the preferred TrueType font file or TrueType collection used by the PNG renderer.
    /// </summary>
    /// <remarks>
    /// SVG and HTML use <see cref="ChartTheme.FontFamily"/>. PNG rendering uses this .ttf or .ttc file when it can be loaded and falls back to an auto-detected platform font or the built-in tiny font.
    /// </remarks>
    public string? PngFontPath {
        get => _pngFontPath;
        set {
            if (value != null && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("PNG font path must not be empty.", nameof(value));
            _pngFontPath = value;
        }
    }

    /// <summary>
    /// Gets or sets the optional family, subfamily, full, or PostScript face name used when selecting a PNG font.
    /// </summary>
    public string? PngFontFaceName {
        get => _pngFontFaceName;
        set {
            if (value != null && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("PNG font face name must not be empty.", nameof(value));
            _pngFontFaceName = value;
        }
    }

    /// <summary>
    /// Gets or sets the optional face index used when <see cref="PngFontPath"/> points to a TrueType collection.
    /// </summary>
    public int? PngFontCollectionIndex {
        get => _pngFontCollectionIndex;
        set {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG font collection index must be non-negative.");
            _pngFontCollectionIndex = value;
        }
    }

    /// <summary>
    /// Gets or sets the internal supersampling scale used by the PNG renderer.
    /// </summary>
    /// <remarks>
    /// Higher values improve antialiased edges at the cost of render time and memory. The output dimensions stay equal to <see cref="Size"/>.
    /// </remarks>
    public int PngSupersamplingScale {
        get => _pngSupersamplingScale;
        set {
            if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG supersampling scale must be between one and four.");
            _pngSupersamplingScale = value;
        }
    }

    /// <summary>
    /// Gets or sets the output pixel multiplier used by the PNG renderer.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="PngSupersamplingScale"/>, this changes the emitted PNG dimensions while preserving the chart's logical layout.
    /// </remarks>
    public int PngOutputScale {
        get => _pngOutputScale;
        set {
            if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG output scale must be between one and four.");
            _pngOutputScale = value;
        }
    }

    /// <summary>
    /// Gets or sets the explicit minimum value for the cartesian x-axis.
    /// </summary>
    public double? XAxisMinimum {
        get => _xAxisMinimum;
        set {
            ValidateNullableFinite(value, nameof(value));
            ValidateAxisBounds(value, _xAxisMaximum, nameof(value), "X-axis");
            _xAxisMinimum = value;
        }
    }

    /// <summary>
    /// Gets or sets the explicit maximum value for the cartesian x-axis.
    /// </summary>
    public double? XAxisMaximum {
        get => _xAxisMaximum;
        set {
            ValidateNullableFinite(value, nameof(value));
            ValidateAxisBounds(_xAxisMinimum, value, nameof(value), "X-axis");
            _xAxisMaximum = value;
        }
    }

    /// <summary>
    /// Gets or sets the explicit minimum value for the cartesian y-axis.
    /// </summary>
    public double? YAxisMinimum {
        get => _yAxisMinimum;
        set {
            ValidateNullableFinite(value, nameof(value));
            ValidateAxisBounds(value, _yAxisMaximum, nameof(value), "Y-axis");
            _yAxisMinimum = value;
        }
    }

    /// <summary>
    /// Gets or sets the explicit maximum value for the cartesian y-axis.
    /// </summary>
    public double? YAxisMaximum {
        get => _yAxisMaximum;
        set {
            ValidateNullableFinite(value, nameof(value));
            ValidateAxisBounds(_yAxisMinimum, value, nameof(value), "Y-axis");
            _yAxisMaximum = value;
        }
    }

    /// <summary>
    /// Gets or sets the explicit minimum value for the secondary cartesian y-axis.
    /// </summary>
    public double? SecondaryYAxisMinimum {
        get => _secondaryYAxisMinimum;
        set {
            ValidateNullableFinite(value, nameof(value));
            ValidateAxisBounds(value, _secondaryYAxisMaximum, nameof(value), "Secondary y-axis");
            _secondaryYAxisMinimum = value;
        }
    }

    /// <summary>
    /// Gets or sets the explicit maximum value for the secondary cartesian y-axis.
    /// </summary>
    public double? SecondaryYAxisMaximum {
        get => _secondaryYAxisMaximum;
        set {
            ValidateNullableFinite(value, nameof(value));
            ValidateAxisBounds(_secondaryYAxisMinimum, value, nameof(value), "Secondary y-axis");
            _secondaryYAxisMaximum = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the legend is rendered.
    /// </summary>
    public bool ShowLegend { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the title and subtitle are rendered.
    /// </summary>
    public bool ShowHeader { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the outer card surface is rendered.
    /// </summary>
    public bool ShowCard { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the plot background surface is rendered.
    /// </summary>
    public bool ShowPlotBackground { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether grid lines are rendered.
    /// </summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether axes, tick labels, and axis titles are rendered.
    /// </summary>
    public bool ShowAxes { get; set; } = true;

    /// <summary>
    /// Gets or sets the preferred number of axis ticks.
    /// </summary>
    public int TickCount {
        get => _tickCount;
        set {
            if (value < 2) throw new ArgumentOutOfRangeException(nameof(value), value, "Tick count must be at least two.");
            _tickCount = value;
        }
    }

    /// <summary>
    /// Gets or sets the density used for explicit x-axis labels.
    /// </summary>
    public ChartLabelDensity XAxisLabelDensity {
        get => _xAxisLabelDensity;
        set {
            if (!Enum.IsDefined(typeof(ChartLabelDensity), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown x-axis label density.");
            _xAxisLabelDensity = value;
        }
    }

    /// <summary>
    /// Gets or sets the x-axis label rotation angle in degrees for capable renderers.
    /// </summary>
    public double XAxisLabelAngle {
        get => _xAxisLabelAngle;
        set {
            ChartGuards.Finite(value, nameof(value));
            _xAxisLabelAngle = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the chart background should be transparent.
    /// </summary>
    public bool TransparentBackground { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether point and bar values are rendered as labels.
    /// </summary>
    public bool ShowDataLabels { get; set; }

    /// <summary>
    /// Gets or sets the optional Gantt current-date marker as an OLE Automation date or numeric schedule value.
    /// </summary>
    public double? GanttToday {
        get => _ganttToday;
        set {
            ValidateNullableFinite(value, nameof(value));
            _ganttToday = value;
        }
    }

    /// <summary>
    /// Gets or sets how multiple bar series are arranged within each category.
    /// </summary>
    public ChartBarMode BarMode {
        get => _barMode;
        set {
            if (!Enum.IsDefined(typeof(ChartBarMode), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown bar mode.");
            _barMode = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether stacked bar totals are rendered above each category.
    /// </summary>
    public bool ShowStackTotals { get; set; }

    /// <summary>
    /// Gets or sets how heatmap values are converted into cell colors.
    /// </summary>
    public ChartHeatmapScale HeatmapScale {
        get => _heatmapScale;
        set {
            if (!Enum.IsDefined(typeof(ChartHeatmapScale), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown heatmap scale.");
            _heatmapScale = value;
        }
    }

    /// <summary>
    /// Gets or sets a formatter used for y-axis ticks, data labels, stack totals, and donut totals.
    /// </summary>
    public Func<double, string>? ValueFormatter { get; set; }

    /// <summary>
    /// Gets or sets a formatter used for secondary y-axis ticks.
    /// </summary>
    public Func<double, string>? SecondaryYAxisValueFormatter { get; set; }

    /// <summary>
    /// Gets or sets a formatter used for generated numeric x-axis tick labels.
    /// </summary>
    public Func<double, string>? XAxisValueFormatter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the chart is rendered as a compact sparkline.
    /// </summary>
    public bool IsSparkline { get; set; }

    /// <summary>
    /// Gets explicit labels for x-axis values.
    /// </summary>
    public List<ChartAxisLabel> XAxisLabels { get; } = new();

    internal List<string> SankeyNodeLabels { get; } = new();

    internal List<string> TreeNodeLabels { get; } = new();

    internal void SetXAxisBounds(double minimum, double maximum) {
        ChartGuards.Finite(minimum, nameof(minimum));
        ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "X-axis maximum must be greater than minimum.");
        _xAxisMinimum = minimum;
        _xAxisMaximum = maximum;
    }

    internal void ClearXAxisBounds() {
        _xAxisMinimum = null;
        _xAxisMaximum = null;
    }

    internal void SetYAxisBounds(double minimum, double maximum) {
        ChartGuards.Finite(minimum, nameof(minimum));
        ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Y-axis maximum must be greater than minimum.");
        _yAxisMinimum = minimum;
        _yAxisMaximum = maximum;
    }

    internal void ClearYAxisBounds() {
        _yAxisMinimum = null;
        _yAxisMaximum = null;
    }

    internal void SetSecondaryYAxisBounds(double minimum, double maximum) {
        ChartGuards.Finite(minimum, nameof(minimum));
        ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Secondary y-axis maximum must be greater than minimum.");
        _secondaryYAxisMinimum = minimum;
        _secondaryYAxisMaximum = maximum;
    }

    internal void ClearSecondaryYAxisBounds() {
        _secondaryYAxisMinimum = null;
        _secondaryYAxisMaximum = null;
    }

    private static void ValidateNullableFinite(double? value, string parameterName) {
        if (value.HasValue) ChartGuards.Finite(value.Value, parameterName);
    }

    private static void ValidateAxisBounds(double? minimum, double? maximum, string parameterName, string axisName) {
        if (minimum.HasValue && maximum.HasValue && maximum.Value <= minimum.Value) {
            throw new ArgumentOutOfRangeException(parameterName, maximum.Value, axisName + " maximum must be greater than minimum.");
        }
    }
}
