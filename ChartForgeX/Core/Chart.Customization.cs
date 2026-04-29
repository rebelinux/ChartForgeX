using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Configures style overrides for a chart text role.
    /// </summary>
    /// <param name="role">The text role to style.</param>
    /// <param name="configure">The style configuration callback.</param>
    /// <returns>The current chart.</returns>
    public Chart WithTextStyle(ChartTextRole role, System.Action<ChartTextStyle> configure) {
        if (configure == null) throw new System.ArgumentNullException(nameof(configure));
        configure(Options.GetTextStyle(role));
        return this;
    }

    /// <summary>
    /// Configures chart title text styling.
    /// </summary>
    public Chart WithTitleStyle(System.Action<ChartTextStyle> configure) => WithTextStyle(ChartTextRole.Title, configure);

    /// <summary>
    /// Configures chart subtitle text styling.
    /// </summary>
    public Chart WithSubtitleStyle(System.Action<ChartTextStyle> configure) => WithTextStyle(ChartTextRole.Subtitle, configure);

    /// <summary>
    /// Configures axis title text styling.
    /// </summary>
    public Chart WithAxisTitleStyle(System.Action<ChartTextStyle> configure) => WithTextStyle(ChartTextRole.AxisTitle, configure);

    /// <summary>
    /// Configures axis tick and category label text styling.
    /// </summary>
    public Chart WithTickLabelStyle(System.Action<ChartTextStyle> configure) => WithTextStyle(ChartTextRole.TickLabel, configure);

    /// <summary>
    /// Configures legend label text styling.
    /// </summary>
    public Chart WithLegendStyle(System.Action<ChartTextStyle> configure) => WithTextStyle(ChartTextRole.Legend, configure);

    /// <summary>
    /// Configures data-label text styling.
    /// </summary>
    public Chart WithDataLabelStyle(System.Action<ChartTextStyle> configure) => WithTextStyle(ChartTextRole.DataLabel, configure);

    /// <summary>
    /// Sets an optional override color for data-label connector lines.
    /// </summary>
    /// <param name="color">The connector color, or null to use the renderer default. Pie and donut callouts use the matching slice color by default.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDataLabelConnectorColor(ChartColor? color) { Options.DataLabelConnectorColor = color; return this; }

    /// <summary>
    /// Sets an override color for data-label connector lines from hex notation.
    /// </summary>
    /// <param name="hex">The connector color in #RGB, #RGBA, #RRGGBB, or #RRGGBBAA notation.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDataLabelConnectorColor(string hex) { Options.DataLabelConnectorColor = ChartColor.FromHex(hex); return this; }

    /// <summary>
    /// Sets the opacity used by data-label connector lines.
    /// </summary>
    /// <param name="opacity">The opacity from zero to one.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDataLabelConnectorOpacity(double opacity) { Options.DataLabelConnectorOpacity = opacity; return this; }

    /// <summary>
    /// Sets the stroke width used by data-label connector lines.
    /// </summary>
    /// <param name="width">The connector stroke width in pixels.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDataLabelConnectorStrokeWidth(double width) { Options.DataLabelConnectorStrokeWidth = width; return this; }

    /// <summary>
    /// Sets the connector line shape used by outside and side data labels.
    /// </summary>
    /// <param name="style">The connector style.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDataLabelConnectorStyle(ChartDataLabelConnectorStyle style) { Options.DataLabelConnectorStyle = style; return this; }

    /// <summary>
    /// Sets where the legend is placed relative to the plot area.
    /// </summary>
    /// <param name="position">The legend position.</param>
    /// <returns>The current chart.</returns>
    public Chart WithLegendPosition(ChartLegendPosition position) { Options.LegendPosition = position; return this; }

    /// <summary>
    /// Sets whether heatmap scale legends should be rendered.
    /// </summary>
    /// <param name="visible">True to render heatmap scale legends; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithHeatmapScaleLegend(bool visible = true) { Options.ShowHeatmapScale = visible; return this; }

    /// <summary>
    /// Sets whether heatmap column labels should be rendered.
    /// </summary>
    /// <param name="visible">True to render heatmap column labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithHeatmapColumnLabels(bool visible = true) { Options.ShowHeatmapColumnLabels = visible; return this; }

    /// <summary>
    /// Sets the built-in pictorial symbol shape and clears any custom SVG path.
    /// </summary>
    /// <param name="shape">The built-in shape used by SVG and PNG output.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialShape(ChartPictorialShape shape) {
        Options.PictorialShape = shape;
        Options.PictorialSvgPathData = null;
        return this;
    }

    /// <summary>
    /// Sets the number of repeated symbols used per pictorial chart row.
    /// </summary>
    /// <param name="columns">The symbol count from one to one hundred.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialColumns(int columns) { Options.PictorialColumns = columns; return this; }

    /// <summary>
    /// Sets the optional scale maximum used by pictorial chart rows.
    /// </summary>
    /// <param name="maximum">The maximum value represented by a fully filled row. Pass null to scale to the largest item.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialMaximum(double? maximum) { Options.PictorialMaximum = maximum; return this; }

    /// <summary>
    /// Sets the data value represented by one full pictorial symbol.
    /// </summary>
    /// <param name="valuePerSymbol">The value represented by one symbol. Pass null to use proportional scaling.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialValuePerSymbol(double? valuePerSymbol) { Options.PictorialValuePerSymbol = valuePerSymbol; return this; }

    /// <summary>
    /// Sets whether numeric value labels are rendered for pictorial rows.
    /// </summary>
    /// <param name="visible">True to render value labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialValues(bool visible = true) { Options.ShowPictorialValues = visible; return this; }

    /// <summary>
    /// Sets the relative pictorial symbol size inside each symbol slot.
    /// </summary>
    /// <param name="scale">The symbol scale from 0.4 to 1.4.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialSymbolScale(double scale) { Options.PictorialSymbolScale = scale; return this; }

    /// <summary>
    /// Sets the opacity used by unfilled pictorial symbols.
    /// </summary>
    /// <param name="opacity">The opacity from zero to one.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialEmptyOpacity(double opacity) { Options.PictorialEmptyOpacity = opacity; return this; }

    /// <summary>
    /// Sets the shared maximum for progress-bar chart rows.
    /// </summary>
    /// <param name="maximum">The maximum progress value.</param>
    /// <returns>The current chart.</returns>
    public Chart WithProgressMaximum(double maximum) { Options.ProgressMaximum = maximum; return this; }

    /// <summary>
    /// Sets whether progress-bar rows should display value labels.
    /// </summary>
    /// <param name="visible">True to show value labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithProgressValues(bool visible = true) { Options.ShowProgressValues = visible; return this; }

    /// <summary>
    /// Sets whether progress-bar rows should render slider handles.
    /// </summary>
    /// <param name="visible">True to render handles; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithProgressHandles(bool visible = true) { Options.ShowProgressHandles = visible; return this; }

    /// <summary>
    /// Sets the progress-bar thickness as a ratio of each row height.
    /// </summary>
    /// <param name="ratio">The thickness ratio from 0.16 to 0.72.</param>
    /// <returns>The current chart.</returns>
    public Chart WithProgressBarThickness(double ratio) { Options.ProgressBarThicknessRatio = ratio; return this; }

    /// <summary>
    /// Sets the opacity used by progress-bar tracks.
    /// </summary>
    /// <param name="opacity">The opacity from zero to one.</param>
    /// <returns>The current chart.</returns>
    public Chart WithProgressTrackOpacity(double opacity) { Options.ProgressTrackOpacity = opacity; return this; }

    /// <summary>
    /// Sets whether donut charts should display center total and series labels.
    /// </summary>
    /// <param name="visible">True to show center labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDonutCenterLabel(bool visible = true) { Options.ShowDonutCenterLabel = visible; return this; }

    /// <summary>
    /// Sets custom center text for donut charts.
    /// </summary>
    /// <param name="value">The primary center text. Pass null or whitespace to use the formatted total.</param>
    /// <param name="label">The secondary center text. Pass null or whitespace to use the series name.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDonutCenterText(string? value, string? label = null) {
        Options.DonutCenterValue = value;
        Options.DonutCenterLabel = label;
        return this;
    }

    /// <summary>
    /// Sets the donut hole radius as a ratio of the outer radius.
    /// </summary>
    /// <param name="ratio">The inner radius ratio from 0.35 to 0.82.</param>
    /// <returns>The current chart.</returns>
    public Chart WithDonutInnerRadiusRatio(double ratio) { Options.DonutInnerRadiusRatio = ratio; return this; }

    /// <summary>
    /// Sets the text rendered for pie and donut slice data labels.
    /// </summary>
    /// <param name="content">The slice label content mode.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPieSliceLabelContent(ChartPieSliceLabelContent content) { Options.PieSliceLabelContent = content; return this; }

    /// <summary>
    /// Sets a custom formatter for pie and donut slice data labels.
    /// </summary>
    /// <param name="formatter">The slice label formatter. Pass null to use <see cref="ChartOptions.PieSliceLabelContent"/>.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPieSliceLabelFormatter(System.Func<ChartPieSliceLabelContext, string?>? formatter) { Options.PieSliceLabelFormatter = formatter; return this; }

    /// <summary>
    /// Sets the side-lane distance for outside pie and donut labels as a ratio of the outer radius.
    /// </summary>
    /// <param name="ratio">The distance ratio from 0.9 to 1.8.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPieOutsideLabelDistance(double ratio) { Options.PieOutsideLabelDistanceRatio = ratio; return this; }

    /// <summary>
    /// Sets whether radial-bar charts should display center average and series labels.
    /// </summary>
    /// <param name="visible">True to show center labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithRadialBarCenterLabel(bool visible = true) { Options.ShowRadialBarCenterLabel = visible; return this; }

    /// <summary>
    /// Sets the relative radius used by circle charts.
    /// </summary>
    /// <param name="scale">The radius scale from 0.65 to 1.35.</param>
    /// <returns>The current chart.</returns>
    public Chart WithCircleRadiusScale(double scale) { Options.CircleRadiusScale = scale; return this; }

    /// <summary>
    /// Sets the relative stroke thickness used by circle charts.
    /// </summary>
    /// <param name="scale">The stroke scale from 0.55 to 1.8.</param>
    /// <returns>The current chart.</returns>
    public Chart WithCircleStrokeScale(double scale) { Options.CircleStrokeScale = scale; return this; }

    /// <summary>
    /// Sets the relative outer radius used by radial-bar charts.
    /// </summary>
    /// <param name="scale">The radius scale from 0.65 to 1.35.</param>
    /// <returns>The current chart.</returns>
    public Chart WithRadialBarRadiusScale(double scale) { Options.RadialBarRadiusScale = scale; return this; }

    /// <summary>
    /// Sets the relative stroke thickness used by radial-bar charts.
    /// </summary>
    /// <param name="scale">The stroke scale from 0.55 to 1.8.</param>
    /// <returns>The current chart.</returns>
    public Chart WithRadialBarStrokeScale(double scale) { Options.RadialBarStrokeScale = scale; return this; }

    /// <summary>
    /// Sets whether circle charts should display status marker and status labels.
    /// </summary>
    /// <param name="visible">True to show status labels; otherwise false.</param>
    /// <returns>The current chart.</returns>
    public Chart WithCircleStatusLabel(bool visible = true) { Options.ShowCircleStatusLabel = visible; return this; }

    /// <summary>
    /// Sets custom SVG path data for pictorial symbols using a 24 by 24 viewBox.
    /// </summary>
    /// <param name="pathData">The SVG path data.</param>
    /// <param name="pngFallbackShape">The built-in shape used by PNG output.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialSvgPath(string pathData, ChartPictorialShape pngFallbackShape = ChartPictorialShape.Circle) =>
        WithPictorialSvgPath(pathData, new ChartRect(0, 0, 24, 24), pngFallbackShape);

    /// <summary>
    /// Sets custom SVG path data for pictorial symbols.
    /// </summary>
    /// <param name="pathData">The SVG path data.</param>
    /// <param name="viewBox">The SVG path viewBox used to scale the path into each symbol slot.</param>
    /// <param name="pngFallbackShape">The built-in shape used by PNG output.</param>
    /// <returns>The current chart.</returns>
    public Chart WithPictorialSvgPath(string pathData, ChartRect viewBox, ChartPictorialShape pngFallbackShape = ChartPictorialShape.Circle) {
        Options.SetPictorialSvgPath(pathData, viewBox, pngFallbackShape);
        return this;
    }

    /// <summary>
    /// Sets the minimum and maximum font sizes used by word cloud terms.
    /// </summary>
    /// <param name="minimum">The smallest term font size.</param>
    /// <param name="maximum">The largest term font size.</param>
    /// <returns>The current chart.</returns>
    public Chart WithWordCloudFontRange(double minimum, double maximum) {
        Options.SetWordCloudFontRange(minimum, maximum);
        return this;
    }

    /// <summary>
    /// Sets the deterministic rotation angles used by word cloud terms.
    /// </summary>
    /// <param name="angles">One or more angles in degrees. Terms cycle through the supplied values.</param>
    /// <returns>The current chart.</returns>
    public Chart WithWordCloudAngles(params double[] angles) {
        Options.WordCloudAngles = angles;
        return this;
    }

    /// <summary>
    /// Sets the optional maximum number of positive-weight terms rendered in a word cloud.
    /// </summary>
    /// <param name="maximumTerms">The maximum term count. Pass null to render every positive-weight term that fits.</param>
    /// <returns>The current chart.</returns>
    public Chart WithWordCloudMaximumTerms(int? maximumTerms) {
        Options.WordCloudMaximumTerms = maximumTerms;
        return this;
    }

    /// <summary>
    /// Sets the word cloud packing density from 0.5 to 2.0.
    /// </summary>
    /// <param name="density">Lower values keep more breathing room; higher values pack more terms into the plot area.</param>
    /// <returns>The current chart.</returns>
    public Chart WithWordCloudDensity(double density) {
        Options.WordCloudDensity = density;
        return this;
    }
}
