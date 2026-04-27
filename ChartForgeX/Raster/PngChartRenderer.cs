using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Raster;

/// <summary>
/// Renders charts to dependency-free PNG images.
/// </summary>
public sealed partial class PngChartRenderer {
    [ThreadStatic]
    private static TrueTypeFont? CurrentOutlineFont;

    /// <summary>
    /// Resolves the font that would be used for PNG text rendering.
    /// </summary>
    /// <param name="chart">The chart to inspect.</param>
    /// <returns>The resolved PNG font information.</returns>
    public static PngFontInfo GetFontInfo(Chart chart) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var options = chart.Options;
        return TrueTypeFont.ResolveInfo(options.PngFontPath, options.PngFontCollectionIndex, options.PngFontFaceName);
    }

    /// <summary>
    /// Renders the specified chart to PNG bytes.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <returns>A PNG image.</returns>
    public byte[] Render(Chart chart) => WritePng(RenderCanvas(chart));

    internal RgbaCanvas RenderCanvas(Chart chart) {
        ChartGuards.RenderCompatibility(chart);
        var o = chart.Options; var t = o.Theme;
        var outlineFont = TrueTypeFont.TryLoadFromPath(o.PngFontPath, o.PngFontCollectionIndex, o.PngFontFaceName);
        var previousOutlineFont = CurrentOutlineFont;
        CurrentOutlineFont = outlineFont;
        try {
            var c = new RgbaCanvas(o.Size.Width, o.Size.Height, o.PngSupersamplingScale, outlineFont);
            c.Clear(o.TransparentBackground ? ChartColor.Transparent : t.Background);
            if (o.ShowCard && t.UseCard) DrawCardSurface(c, o, t);
            var plot = ChartLayout.PlotArea(o);
            if (o.ShowHeader) DrawHeader(c, chart);
            if (IsPieLike(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawPieLike(c, chart, plot);
                return c;
            }
            if (IsGaugeChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawGauge(c, chart, plot);
                return c;
            }
            if (IsCircleChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawCircleChart(c, chart, plot);
                return c;
            }
            if (IsRadialBarChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawRadialBar(c, chart, plot);
                return c;
            }
            if (IsBulletChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawBullet(c, chart, plot);
                return c;
            }
            if (IsWaterfallChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawWaterfall(c, chart, plot);
                return c;
            }
            if (IsRadarChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawRadar(c, chart, plot);
                return c;
            }
            if (IsPolarAreaChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawPolarArea(c, chart, plot);
                return c;
            }
            if (IsFunnelChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawFunnel(c, chart, plot);
                return c;
            }
            if (IsTreemapChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawTreemap(c, chart, plot);
                DrawLegend(c, chart);
                return c;
            }
            if (IsHeatmapChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawHeatmap(c, chart, plot);
                return c;
            }
            if (IsTimelineChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawTimeline(c, chart, plot);
                return c;
            }
            if (IsGanttChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawGantt(c, chart, plot);
                return c;
            }
            if (IsSankeyChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawSankey(c, chart, plot);
                return c;
            }
            if (IsTreeChart(chart)) {
                DrawPlotSurface(c, o, t, plot);
                DrawTree(c, chart, plot);
                return c;
            }
            var range = ChartRange.FromChart(chart);
            IReadOnlyList<double> yTicks;
            IReadOnlyList<double> xTicks;
            ChartRange? secondaryRange = null;
            IReadOnlyList<double>? secondaryTicks = null;
            if (IsHorizontalBarChart(chart)) {
                xTicks = ChartTicks.Generate(range.MinX, range.MaxX, o.TickCount);
                ApplyHorizontalValueBounds(chart, range, xTicks);
                yTicks = GetHorizontalCategoryTicks(chart, range);
                plot = ApplyHorizontalBarReserve(chart, plot, yTicks);
                plot = ApplyBottomReserve(chart, plot, xTicks, true);
                DrawPlotSurface(c, o, t, plot);
            } else {
                yTicks = ChartTicks.Generate(range.MinY, range.MaxY, o.TickCount);
                range.SetYBounds(yTicks[0], yTicks[yTicks.Count - 1]);
                plot = ApplyYAxisLabelReserve(chart, plot, yTicks);
                if (HasSecondaryYAxis(chart)) {
                    secondaryRange = ChartRange.FromSecondaryYAxis(chart, range);
                    secondaryTicks = ChartTicks.Generate(secondaryRange.MinY, secondaryRange.MaxY, o.TickCount);
                    secondaryRange.SetYBounds(secondaryTicks[0], secondaryTicks[secondaryTicks.Count - 1]);
                    plot = ApplySecondaryYAxisLabelReserve(chart, plot, secondaryTicks);
                }

                xTicks = GetXTicks(chart, range, plot);
                plot = ApplyBottomReserve(chart, plot, xTicks, false);
                DrawPlotSurface(c, o, t, plot);
            }

            var map = new ChartMapper(plot, range);
            var secondaryMap = secondaryRange == null ? null : new ChartMapper(plot, secondaryRange);
            if (IsHorizontalBarChart(chart)) {
                DrawHorizontalBarGrid(c, chart, plot, map, xTicks, yTicks);
                for (var i = 0; i < chart.Series.Count; i++) DrawSeries(c, chart, i, plot, map);
                if (o.BarMode == ChartBarMode.Stacked && o.ShowStackTotals) DrawHorizontalStackTotals(c, chart, plot, map);
                DrawLegend(c, chart);
                return c;
            }

            DrawAnnotationBands(c, chart, plot, map);
            foreach (var yv in yTicks) {
                var y = map.Y(yv);
                if (o.ShowGrid) c.DrawLine(plot.Left, y, plot.Right, y, t.Grid, 1);
                if (o.ShowAxes) {
                    var label = FormatValue(chart, yv);
                    var fontSize = PngTickFontSize(chart);
                    c.DrawText(Math.Max(2, plot.Left - EstimatePngTextWidth(label, fontSize) - 8), y - fontSize + 4, label, t.MutedText, fontSize);
                }
            }
            var xLabels = XAxisTickLabels(chart, xTicks, false);
            for (var i = 0; i < xTicks.Count; i++) {
                var x = map.X(xTicks[i]);
                var label = xLabels[i];
                if (o.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ChartColor.FromRgba(t.Grid.R, t.Grid.G, t.Grid.B, (byte)(t.Grid.A / 2)), 1);
                if (o.ShowAxes) DrawXAxisTickLabel(c, chart, plot, label, x, xLabels);
            }
            if (o.ShowAxes) {
                var zeroY = map.Y(0);
                if (zeroY > plot.Top && zeroY < plot.Bottom) c.DrawLine(plot.Left, zeroY, plot.Right, zeroY, t.Axis, 1);
                c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis, 1);
                c.DrawLine(plot.Left, plot.Top, plot.Left, plot.Bottom, t.Axis, 1);
                if (secondaryMap != null && secondaryTicks != null) DrawSecondaryYAxis(c, chart, plot, secondaryMap, secondaryTicks);
                DrawAxisTitles(c, chart, plot, xLabels);
            }
            for (var i = 0; i < chart.Series.Count; i++) DrawSeries(c, chart, i, plot, SeriesMap(chart.Series[i], map, secondaryMap));
            if (o.BarMode == ChartBarMode.Stacked && o.ShowStackTotals) DrawStackTotals(c, chart, plot, map);
            DrawAnnotationLines(c, chart, plot, map);
            DrawLegend(c, chart);
            return c;
        } finally {
            CurrentOutlineFont = previousOutlineFont;
        }
    }

    private static void DrawCardSurface(RgbaCanvas c, ChartOptions options, ChartTheme theme) {
        c.FillRoundedRect(14, 14, options.Size.Width - 28, options.Size.Height - 28, theme.CornerRadius, theme.CardBackground);
        if (theme.CardBorder.A > 0) c.StrokeRoundedRect(14, 14, options.Size.Width - 28, options.Size.Height - 28, theme.CornerRadius, theme.CardBorder);
    }

    private static void DrawPlotSurface(RgbaCanvas c, ChartOptions options, ChartTheme theme, ChartRect plot) {
        if (options.ShowPlotBackground) c.FillRoundedRect(plot.X, plot.Y, plot.Width, plot.Height, theme.PlotCornerRadius, theme.PlotBackground);
        if (options.ShowPlotBackground && theme.PlotBorder.A > 0) c.StrokeRoundedRect(plot.X, plot.Y, plot.Width, plot.Height, theme.PlotCornerRadius, theme.PlotBorder);
    }

    private static void DrawHeader(RgbaCanvas c, Chart chart) {
        var theme = chart.Options.Theme;
        var maxWidth = Math.Max(24, chart.Options.Size.Width - 80);
        var titleFontSize = TextFontSizeForEmphasizedWidth(chart.Title, maxWidth, theme.TitleFontSize);
        var title = TrimReadablePngLabelToWidth(chart.Title, titleFontSize, maxWidth);
        if (title.Length > 0) c.DrawTextEmphasized(40, 52 - titleFontSize + 1, title, theme.Text, titleFontSize);
        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) {
            var subtitleMaxWidth = Math.Max(24, chart.Options.Size.Width - 84);
            var subtitleFontSize = TextFontSizeForWidth(chart.Subtitle, subtitleMaxWidth, theme.SubtitleFontSize);
            var subtitle = TrimPngLabelToWidth(chart.Subtitle, subtitleFontSize, subtitleMaxWidth);
            if (subtitle.Length > 0) c.DrawText(42, 79 - subtitleFontSize + 1, subtitle, theme.MutedText, subtitleFontSize);
        }
    }

    private static bool IsLineLikeLegend(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Line || kind == ChartSeriesKind.StepLine || kind == ChartSeriesKind.Area || kind == ChartSeriesKind.StepArea || kind == ChartSeriesKind.StackedArea || kind == ChartSeriesKind.Slope || kind == ChartSeriesKind.RangeBand || kind == ChartSeriesKind.RangeArea || kind == ChartSeriesKind.Lollipop || kind == ChartSeriesKind.Dumbbell || kind == ChartSeriesKind.ErrorBar || kind == ChartSeriesKind.Radar || kind == ChartSeriesKind.TrendLine;

    private static bool ShouldDrawDataLabels(Chart chart, ChartSeries series) => series.ShowDataLabels ?? chart.Options.ShowDataLabels;

    private static bool HasSecondaryYAxis(Chart chart) {
        foreach (var series in chart.Series) {
            if (series.YAxis == ChartAxisSide.Secondary) return true;
        }

        return false;
    }

    private static ChartMapper SeriesMap(ChartSeries series, ChartMapper primaryMap, ChartMapper? secondaryMap) =>
        series.YAxis == ChartAxisSide.Secondary && secondaryMap != null ? secondaryMap : primaryMap;

    private static void DrawAnnotationBands(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map) {
        foreach (var annotation in chart.Annotations) {
            if (!annotation.EndValue.HasValue) continue;
            var color = ApplyOpacity(annotation.Color, annotation.Opacity);
            if (annotation.Kind == ChartAnnotationKind.HorizontalBand) {
                var y1 = Clamp(map.Y(annotation.Value), plot.Top, plot.Bottom);
                var y2 = Clamp(map.Y(annotation.EndValue.Value), plot.Top, plot.Bottom);
                c.FillRect(plot.Left, Math.Min(y1, y2), plot.Width, Math.Abs(y2 - y1), color);
                DrawTinyAnnotationLabel(c, chart, annotation, plot, plot.Left + 8, Math.Min(y1, y2) + 8);
            } else if (annotation.Kind == ChartAnnotationKind.VerticalBand) {
                var x1 = Clamp(map.X(annotation.Value), plot.Left, plot.Right);
                var x2 = Clamp(map.X(annotation.EndValue.Value), plot.Left, plot.Right);
                c.FillRect(Math.Min(x1, x2), plot.Top, Math.Abs(x2 - x1), plot.Height, color);
                DrawTinyAnnotationLabel(c, chart, annotation, plot, Math.Min(x1, x2) + 8, plot.Top + 8);
            }
        }
    }

    private static void DrawAnnotationLines(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map) {
        foreach (var annotation in chart.Annotations) {
            if (annotation.Kind == ChartAnnotationKind.HorizontalLine) {
                var y = Clamp(map.Y(annotation.Value), plot.Top, plot.Bottom);
                c.DrawDashedLine(plot.Left, y, plot.Right, y, annotation.Color, 1);
                DrawTinyAnnotationPill(c, chart, annotation, plot.Right - 4, y - 7, "end");
            } else if (annotation.Kind == ChartAnnotationKind.VerticalLine) {
                var x = Clamp(map.X(annotation.Value), plot.Left, plot.Right);
                c.DrawDashedLine(x, plot.Top, x, plot.Bottom, annotation.Color, 1);
                DrawTinyAnnotationPill(c, chart, annotation, x + 8, plot.Top + 16, "start");
            }
        }
    }

    private static void DrawTinyAnnotationLabel(RgbaCanvas c, Chart chart, ChartAnnotation annotation, ChartRect plot, double x, double y) {
        if (string.IsNullOrWhiteSpace(annotation.Label)) return;
        var theme = chart.Options.Theme;
        var fontSize = PngTickFontSize(chart);
        var textWidth = EstimatePngTextWidth(annotation.Label, fontSize);
        var width = Math.Max(34, textWidth + 16);
        var height = EstimatePngTextHeight(fontSize) + 10;
        var rectX = Clamp(x, plot.Left + 4, plot.Right - width - 4);
        var rectY = Clamp(y, plot.Top + 4, plot.Bottom - height - 4);
        var fillAlpha = theme.CardBackground.A == 0 ? (byte)220 : theme.CardBackground.A;
        var fill = ChartColor.FromRgba(theme.CardBackground.R, theme.CardBackground.G, theme.CardBackground.B, fillAlpha);
        var border = ChartColor.FromRgba(annotation.Color.R, annotation.Color.G, annotation.Color.B, 120);
        c.FillRoundedRect(rectX, rectY, width, height, Math.Min(6, height / 2), fill);
        c.StrokeRoundedRect(rectX, rectY, width, height, Math.Min(6, height / 2), border);
        c.DrawText(rectX + 8, rectY + (height - fontSize) / 2, annotation.Label, annotation.Color, fontSize);
    }

    private static void DrawTinyAnnotationPill(RgbaCanvas c, Chart chart, ChartAnnotation annotation, double x, double y, string anchor) {
        if (string.IsNullOrWhiteSpace(annotation.Label)) return;
        var theme = chart.Options.Theme;
        var fontSize = PngTickFontSize(chart);
        var textWidth = EstimatePngTextWidth(annotation.Label, fontSize);
        var width = Math.Max(34, textWidth + 16);
        var height = EstimatePngTextHeight(fontSize) + 10;
        var rectX = anchor == "end" ? x - width : x;
        rectX = Clamp(rectX, chart.Options.Padding.Left, chart.Options.Size.Width - chart.Options.Padding.Right - width);
        var rectY = Clamp(y - height / 2, 18, chart.Options.Size.Height - chart.Options.Padding.Bottom - height);
        var fill = ChartColor.FromRgba(theme.CardBackground.R, theme.CardBackground.G, theme.CardBackground.B, theme.CardBackground.A == 0 ? (byte)224 : theme.CardBackground.A);
        var border = ChartColor.FromRgba(annotation.Color.R, annotation.Color.G, annotation.Color.B, 110);

        c.FillRoundedRect(rectX, rectY, width, height, Math.Min(6, height / 2), fill);
        c.StrokeRoundedRect(rectX, rectY, width, height, Math.Min(6, height / 2), border);
        c.DrawText(rectX + 8, rectY + (height - fontSize) / 2, annotation.Label, annotation.Color, fontSize);
    }

    private static void DrawHorizontalBarGrid(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map, IReadOnlyList<double> xTicks, IReadOnlyList<double> categories) {
        var o = chart.Options;
        var t = o.Theme;
        var xLabels = XAxisTickLabels(chart, xTicks, true);
        for (var i = 0; i < xTicks.Count; i++) {
            var x = map.X(xTicks[i]);
            var label = xLabels[i];
            if (o.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ChartColor.FromRgba(t.Grid.R, t.Grid.G, t.Grid.B, (byte)(t.Grid.A / 2)), 1);
            if (o.ShowAxes) DrawXAxisTickLabel(c, chart, plot, label, x, xLabels);
        }

        foreach (var category in categories) {
            var y = map.Y(category);
            if (o.ShowGrid) c.DrawLine(plot.Left, y, plot.Right, y, ChartColor.FromRgba(t.Grid.R, t.Grid.G, t.Grid.B, (byte)(t.Grid.A / 3)), 1);
            if (o.ShowAxes) DrawHorizontalCategoryLabel(c, chart, plot, FormatX(chart, category), y);
        }

        if (o.ShowAxes) {
            var zeroX = map.X(0);
            if (zeroX > plot.Left && zeroX < plot.Right) c.DrawLine(zeroX, plot.Top, zeroX, plot.Bottom, t.Axis, 1);
            c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis, 1);
            c.DrawLine(plot.Left, plot.Top, plot.Left, plot.Bottom, t.Axis, 1);
            DrawAxisTitles(c, chart, plot, xLabels);
        }
    }

    private static string FormatNumber(double v) => Math.Abs(v) >= 1000 ? (v / 1000).ToString("0.#", CultureInfo.InvariantCulture) + "K" : v.ToString("0.#", CultureInfo.InvariantCulture);
    private static string FormatValue(Chart chart, double value) {
        var formatter = chart.Options.ValueFormatter;
        if (formatter == null) return FormatNumber(value);
        return formatter(value) ?? string.Empty;
    }

    private static string FormatSecondaryValue(Chart chart, double value) {
        var formatter = chart.Options.SecondaryYAxisValueFormatter;
        if (formatter == null) return FormatValue(chart, value);
        return formatter(value) ?? string.Empty;
    }
    private static string FormatPercent(double v) => v.ToString("0.#%", CultureInfo.InvariantCulture);
    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    private static byte[] WritePng(RgbaCanvas canvas) => PngWriter.WriteRgba(canvas.Width, canvas.Height, canvas.ToOutputPixels());

    private static IReadOnlyList<double> GetXTicks(Chart chart, ChartRange range, ChartRect plot) {
        if (chart.Options.XAxisLabels.Count == 0) return ChartTicks.GenerateInside(range.MinX, range.MaxX, chart.Options.TickCount);
        var labels = new List<ChartAxisLabel>();
        foreach (var label in chart.Options.XAxisLabels) {
            if (label.Value >= range.MinX && label.Value <= range.MaxX) labels.Add(label);
        }

        labels.Sort((left, right) => left.Value.CompareTo(right.Value));
        if (chart.Options.XAxisLabelDensity == ChartLabelDensity.All || labels.Count < 3) return LabelValues(labels);

        var fontSize = PngTickFontSize(chart);
        var widest = 0.0;
        foreach (var label in labels) widest = Math.Max(widest, EstimatePngTextWidth(label.Text, fontSize));
        var densityFactor = chart.Options.XAxisLabelDensity == ChartLabelDensity.Dense ? 0.72 : chart.Options.XAxisLabelDensity == ChartLabelDensity.Relaxed ? 1.35 : 1.0;
        var minSpacing = Math.Max(28, (widest + 18) * densityFactor);
        var maxCount = Math.Max(2, (int)Math.Floor(plot.Width / minSpacing) + 1);
        if (labels.Count <= maxCount && LabelsHaveMinimumLabelGap(labels, range, plot, fontSize, 6)) return LabelValues(labels);

        var lastLabel = labels[labels.Count - 1];
        var step = Math.Max(1, (int)Math.Ceiling((labels.Count - 1) / (double)(maxCount - 1)));
        var selected = new List<ChartAxisLabel>();
        selected.Add(labels[0]);
        for (var i = step; i < labels.Count - 1; i += step) {
            if (LabelGap(selected[selected.Count - 1], labels[i], range, plot, fontSize) >= 6 && LabelGap(labels[i], lastLabel, range, plot, fontSize) >= 6) selected.Add(labels[i]);
        }

        if (selected.Count > 1 && LabelGap(selected[selected.Count - 1], lastLabel, range, plot, fontSize) < 6) selected.RemoveAt(selected.Count - 1);
        selected.Add(lastLabel);
        return LabelValues(selected);
    }

    private static bool LabelsHaveMinimumLabelGap(IReadOnlyList<ChartAxisLabel> labels, ChartRange range, ChartRect plot, double fontSize, double minGap) {
        for (var i = 1; i < labels.Count; i++) {
            if (LabelGap(labels[i - 1], labels[i], range, plot, fontSize) < minGap) return false;
        }

        return true;
    }

    private static double LabelGap(ChartAxisLabel left, ChartAxisLabel right, ChartRange range, ChartRect plot, double fontSize) {
        var leftWidth = EstimatePngTextWidth(left.Text, fontSize);
        var rightWidth = EstimatePngTextWidth(right.Text, fontSize);
        var leftX = Clamp(ProjectX(left.Value, range, plot) - leftWidth / 2.0, plot.Left + 2, plot.Right - leftWidth - 2);
        var rightX = Clamp(ProjectX(right.Value, range, plot) - rightWidth / 2.0, plot.Left + 2, plot.Right - rightWidth - 2);
        return rightX - (leftX + leftWidth);
    }

    private static IReadOnlyList<double> LabelValues(IReadOnlyList<ChartAxisLabel> labels) {
        var values = new List<double>(labels.Count);
        for (var i = 0; i < labels.Count; i++) values.Add(labels[i].Value);
        return values;
    }

    private static IReadOnlyList<double> GetHorizontalCategoryTicks(Chart chart, ChartRange range) {
        var categories = new SortedSet<double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var point in series.Points) {
                if (point.X >= range.MinY && point.X <= range.MaxY) categories.Add(point.X);
            }
        }

        if (categories.Count > 0) {
            var values = new List<double>();
            foreach (var category in categories) values.Add(category);
            return values;
        }

        return ChartTicks.GenerateInside(range.MinY, range.MaxY, chart.Options.TickCount);
    }

    private static string FormatX(Chart chart, double value) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - value) < 0.000001) return label.Text;
        }

        return FormatXAxisValue(chart, value);
    }

    private static IReadOnlyList<string> XAxisTickLabels(Chart chart, IReadOnlyList<double> xTicks, bool valueAxisOnly) {
        var labels = new string[xTicks.Count];
        for (var i = 0; i < xTicks.Count; i++) labels[i] = valueAxisOnly ? FormatXAxisValue(chart, xTicks[i]) : FormatX(chart, xTicks[i]);
        return labels;
    }

    private static string FormatXAxisValue(Chart chart, double value) {
        var formatter = chart.Options.XAxisValueFormatter;
        if (formatter == null) return FormatNumber(value);
        return formatter(value) ?? string.Empty;
    }

    private static bool IsPieLike(Chart chart) => chart.Series.Count > 0 && (chart.Series[0].Kind == ChartSeriesKind.Pie || chart.Series[0].Kind == ChartSeriesKind.Donut);
    private static bool IsPolarAreaChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.PolarArea) return true;
        return false;
    }

    private static bool IsHorizontalBarChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.HorizontalBar) return true;
        return false;
    }
}
