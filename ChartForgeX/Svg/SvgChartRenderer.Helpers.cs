using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawLegend(StringBuilder sb, Chart chart, int w, int h) {
        if (!chart.Options.ShowLegend) return;
        var t = chart.Options.Theme;
        var area = LegendArea(chart, w, h);
        var rows = BuildLegendRows(chart, area.Width);
        var y = LegendStartY(chart, area, rows.Count);
        sb.AppendLine($"<g data-cfx-role=\"legend\" data-cfx-position=\"{chart.Options.LegendPosition}\">");
        foreach (var row in rows) {
            if (y > area.Bottom - 4) break;
            var xShift = LegendRowX(chart.Options.LegendPosition, area, row.Width);
            sb.AppendLine($"<g data-cfx-role=\"legend-row\" transform=\"translate({F(area.X + xShift)} {F(y)})\">");
            foreach (var item in row.Items) {
                var series = chart.Series[item.SeriesIndex];
                var pointAttribute = item.PointIndex >= 0 ? $" data-cfx-point=\"{item.PointIndex}\"" : string.Empty;
                sb.AppendLine($"<g data-cfx-role=\"legend-item\" data-cfx-series=\"{item.SeriesIndex}\"{pointAttribute} data-cfx-kind=\"{Escape(series.Kind.ToString())}\" data-cfx-label=\"{Escape(item.Label)}\">");
                DrawLegendSymbol(sb, series.Kind, item.X, -4, item.Color, t.CardBackground);
                var style = chart.Options.LegendStyle;
                var labelMaxWidth = Math.Max(8, item.Width - 30);
                var labelFontSize = TextFontSizeForSvgWidth(item.Label, labelMaxWidth, StyleFontSize(style, t.LegendFontSize));
                var label = TrimSvgLabelToWidth(item.Label, labelFontSize, labelMaxWidth);
                if (label.Length > 0) sb.AppendLine($"<text data-cfx-role=\"legend-label\" data-cfx-series=\"{item.SeriesIndex}\"{pointAttribute} x=\"{F(item.X + 26)}\" y=\"0\" fill=\"{StyleColor(style, t.MutedText).ToCss()}\" font-family=\"{SvgFontFamily(StyleFontFamily(chart, style))}\" font-size=\"{F(labelFontSize)}\" font-weight=\"{StyleWeight(style, "600")}\"{SvgTextStyleAttributes(style)}>{Escape(label)}</text>");
                sb.AppendLine("</g>");
            }

            sb.AppendLine("</g>");
            y += LegendRowHeight;
        }
        sb.AppendLine("</g>");
    }

    private static List<LegendRow> BuildLegendRows(Chart chart, double width) {
        var rows = new List<LegendRow>();
        if (chart.Series.Count == 0) return rows;

        var maxX = Math.Max(64, width);
        var vertical = IsVerticalLegend(chart.Options.LegendPosition);
        var row = new LegendRow();
        rows.Add(row);
        var x = 0.0;
        foreach (var entry in BuildLegendEntries(chart, width)) {
            var itemWidth = Math.Min(maxX, 34 + EstimateTextWidth(entry.Label, chart.Options.Theme.LegendFontSize) + 18);
            if (row.Items.Count > 0 && (vertical || x + itemWidth > maxX)) {
                row = new LegendRow();
                rows.Add(row);
                x = 0;
            }

            row.Items.Add(new LegendItem(entry.SeriesIndex, entry.PointIndex, x, itemWidth, entry.Label, entry.Color));
            row.Width = Math.Max(row.Width, x + itemWidth);
            x += itemWidth;
        }

        return rows;
    }

    private static string SvgLegendLabel(Chart chart, int index, double width) =>
        TrimSvgLabelToWidth(chart.Series[index].Name, chart.Options.Theme.LegendFontSize, LegendLabelMaxWidth(width));

    private static List<LegendEntry> BuildLegendEntries(Chart chart, double width) {
        if (!chart.Options.ShowPointLegend || chart.Series.Count != 1 || !CanUsePointLegend(chart.Series[0])) {
            return chart.Series.Select((series, index) => new LegendEntry(index, -1, SvgLegendLabel(chart, index, width), Color(chart, index))).ToList();
        }

        var series0 = chart.Series[0];
        var entries = new List<LegendEntry>();
        var count = VisualPointCount(series0);
        for (var i = 0; i < count; i++) {
            var rawIndex = VisualPointRawIndex(series0, i);
            if (rawIndex < 0 || rawIndex >= series0.Points.Count) continue;
            var label = LegendPointLabel(chart, series0.Points[rawIndex], i);
            label = TrimSvgLabelToWidth(label, chart.Options.Theme.LegendFontSize, LegendLabelMaxWidth(width));
            entries.Add(new LegendEntry(0, i, label, LegendPointColor(chart, series0, 0, i)));
        }

        return entries.Count == 0 ? new List<LegendEntry> { new(0, -1, SvgLegendLabel(chart, 0, width), Color(chart, 0)) } : entries;
    }

    private static ChartColor LegendPointColor(Chart chart, ChartSeries series, int seriesIndex, int pointIndex) {
        if (pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue) return series.PointColors[pointIndex]!.Value;
        if (series.Color.HasValue) return series.Color.Value;
        if (UsesPalettePointColors(series.Kind)) return chart.Options.Theme.Palette[pointIndex % chart.Options.Theme.Palette.Length];
        return Color(chart, seriesIndex);
    }

    private static bool UsesPalettePointColors(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Funnel || kind == ChartSeriesKind.Pictorial || kind == ChartSeriesKind.ProgressBar || kind == ChartSeriesKind.Treemap || kind == ChartSeriesKind.WordCloud;

    private static double LegendLabelMaxWidth(double width) => Math.Max(48, Math.Min(IsVerticalLegendWidth(width) ? 170 : 260, width * 0.72));

    private static bool IsVerticalLegendWidth(double width) => width <= 230;

    private static ChartRect LegendArea(Chart chart, int w, int h) {
        var padding = 32.0;
        var position = chart.Options.LegendPosition;
        if (IsLeftLegend(position)) return new ChartRect(padding, chart.Options.ShowHeader ? 100 : 48, LegendSideReserve(chart), Math.Max(1, h - (chart.Options.ShowHeader ? 130 : 78)));
        if (IsRightLegend(position)) {
            var width = LegendSideReserve(chart);
            return new ChartRect(Math.Max(padding, w - width - padding), chart.Options.ShowHeader ? 100 : 48, width, Math.Max(1, h - (chart.Options.ShowHeader ? 130 : 78)));
        }

        var y = IsTopLegend(position) ? (chart.Options.ShowHeader ? 98 : 44) : Math.Max(44, h - LegendBottomReserve(chart) - 4);
        return new ChartRect(40, y, Math.Max(1, w - 80), LegendBottomReserve(chart));
    }

    private static double LegendStartY(Chart chart, ChartRect area, int rowCount) {
        if (IsBottomLegend(chart.Options.LegendPosition)) return area.Bottom - 24 - Math.Max(0, rowCount - 1) * LegendRowHeight;
        return area.Top + 14;
    }

    private static double LegendRowX(ChartLegendPosition position, ChartRect area, double rowWidth) {
        if (position == ChartLegendPosition.TopRight || position == ChartLegendPosition.BottomRight || position == ChartLegendPosition.Right) return area.Width - Math.Min(area.Width, rowWidth);
        if (position == ChartLegendPosition.Top || position == ChartLegendPosition.Bottom) return Math.Max(0, (area.Width - rowWidth) / 2.0);
        return 0;
    }

    private static double LegendBottomReserve(Chart chart) => 18 + BuildLegendRows(chart, Math.Max(1, chart.Options.Size.Width - 80)).Count * LegendRowHeight + ChartVisualPrimitives.LegendPlotGap;

    private static double LegendSideReserve(Chart chart) {
        if (chart.Series.Count == 0) return 0;
        var t = chart.Options.Theme;
        var widest = BuildLegendEntries(chart, LegendSideReserveMaximumWidth).Max(item => EstimateTextWidth(item.Label, t.LegendFontSize));
        return Math.Min(240, Math.Max(124, widest + 54));
    }

    private const double LegendSideReserveMaximumWidth = 240;

    private static bool IsTopLegend(ChartLegendPosition position) => position == ChartLegendPosition.Top || position == ChartLegendPosition.TopLeft || position == ChartLegendPosition.TopRight;

    private static bool IsBottomLegend(ChartLegendPosition position) => position == ChartLegendPosition.Bottom || position == ChartLegendPosition.BottomLeft || position == ChartLegendPosition.BottomRight;

    private static bool IsLeftLegend(ChartLegendPosition position) => position == ChartLegendPosition.Left;

    private static bool IsRightLegend(ChartLegendPosition position) => position == ChartLegendPosition.Right;

    private static bool IsVerticalLegend(ChartLegendPosition position) => IsLeftLegend(position) || IsRightLegend(position);

    private static void DrawLegendSymbol(StringBuilder sb, ChartSeriesKind kind, double x, double y, ChartColor color, ChartColor background) {
        if (IsLineLikeLegend(kind)) {
            sb.AppendLine($"<line x1=\"{F(x)}\" y1=\"{F(y)}\" x2=\"{F(x + 18)}\" y2=\"{F(y)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.LegendLineStrokeWidth)}\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<circle cx=\"{F(x + 9)}\" cy=\"{F(y)}\" r=\"{F(ChartVisualPrimitives.LegendMarkerRadius)}\" fill=\"{color.ToCss()}\" stroke=\"{background.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.LegendMarkerStrokeWidth)}\"/>");
        } else if (kind == ChartSeriesKind.Scatter || kind == ChartSeriesKind.Bubble) {
            sb.AppendLine($"<circle cx=\"{F(x + 9)}\" cy=\"{F(y)}\" r=\"{F(ChartVisualPrimitives.LegendMarkerRadius)}\" fill=\"{color.ToCss()}\" stroke=\"{background.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.LegendMarkerStrokeWidth)}\"/>");
        } else if (kind == ChartSeriesKind.Candlestick || kind == ChartSeriesKind.Ohlc) {
            sb.AppendLine($"<line x1=\"{F(x + 9)}\" y1=\"{F(y - 6)}\" x2=\"{F(x + 9)}\" y2=\"{F(y + 6)}\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.LegendFinanceStrokeWidth)}\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<rect x=\"{F(x + 4)}\" y=\"{F(y - ChartVisualPrimitives.LegendFinanceBodyHeight / 2)}\" width=\"{F(ChartVisualPrimitives.LegendFinanceBodyWidth)}\" height=\"{F(ChartVisualPrimitives.LegendFinanceBodyHeight)}\" rx=\"1.5\" fill=\"{color.ToCss()}\"/>");
        } else {
            sb.AppendLine($"<rect x=\"{F(x)}\" y=\"{F(y - 5)}\" width=\"10\" height=\"10\" rx=\"2\" fill=\"{color.ToCss()}\"/>");
        }
    }

    private static bool IsLineLikeLegend(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Line || kind == ChartSeriesKind.StepLine || kind == ChartSeriesKind.Area || kind == ChartSeriesKind.StepArea || kind == ChartSeriesKind.StackedArea || kind == ChartSeriesKind.Slope || kind == ChartSeriesKind.RangeBand || kind == ChartSeriesKind.RangeArea || kind == ChartSeriesKind.Lollipop || kind == ChartSeriesKind.Dumbbell || kind == ChartSeriesKind.ErrorBar || kind == ChartSeriesKind.Radar || kind == ChartSeriesKind.TrendLine;

    private static void DrawLabelPill(StringBuilder sb, Chart chart, string label, double x, double y, ChartColor textColor, string anchor, ChartRect plot) {
        var t = chart.Options.Theme;
        var maxWidth = Math.Max(42, PlotLabelMaxWidth(plot));
        var fontSize = TextFontSizeForSvgWidth(label, Math.Max(24, maxWidth - 18), t.TickLabelFontSize);
        label = TrimSvgLabelToWidth(label, fontSize, Math.Max(24, maxWidth - 18));
        if (label.Length == 0) return;
        var width = Math.Min(maxWidth, Math.Max(36, EstimateTextWidth(label, fontSize) + 18));
        var placement = PlaceLabelPill(x, width, anchor, plot);
        var textX = placement.Anchor == "end" ? placement.X - 9 : placement.X + 9;
        var rectY = Clamp(y - 16, plot.Top + 5, plot.Bottom - 27);
        sb.AppendLine($"<rect data-cfx-role=\"annotation-label\" data-cfx-label=\"{Escape(label)}\" x=\"{F(placement.RectX)}\" y=\"{F(rectY)}\" width=\"{F(width)}\" height=\"23\" rx=\"5\" fill=\"{t.CardBackground.ToCss()}\" opacity=\"0.92\" stroke=\"{textColor.ToCss()}\" stroke-opacity=\"0.36\"/>");
        sb.AppendLine($"<text data-cfx-role=\"annotation-label-text\" data-cfx-label=\"{Escape(label)}\" x=\"{F(textX)}\" y=\"{F(rectY + 16)}\" text-anchor=\"{placement.Anchor}\" fill=\"{textColor.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(fontSize)}\" font-weight=\"750\">{Escape(label)}</text>");
    }

    private static void DrawDataLabel(StringBuilder sb, Chart chart, string label, double x, double y, ChartRect plot, string role = "data-label", ChartSeries? series = null, int pointIndex = -1) {
        var t = chart.Options.Theme;
        var style = DataLabelStyle(chart, series, pointIndex);
        var fontSize = StyleFontSize(style, t.DataLabelFontSize);
        label = TrimSvgLabelToWidth(label, fontSize, PlotLabelMaxWidth(plot));
        if (label.Length == 0) return;

        var safeY = Clamp(y, plot.Top + ChartVisualPrimitives.DataLabelPlotInset + fontSize / 2.0, plot.Bottom - ChartVisualPrimitives.DataLabelPlotInset - fontSize / 2.0);
        var anchor = EdgeAwareAnchor(label, x, plot, fontSize);
        var safeX = EdgeAwareTextX(label, x, plot, fontSize);
        sb.AppendLine($"<text data-cfx-role=\"{role}\" x=\"{F(safeX)}\" y=\"{F(safeY)}\" text-anchor=\"{anchor}\" dominant-baseline=\"middle\" fill=\"{StyleColor(style, t.Text).ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"3\" paint-order=\"stroke fill\" stroke-linejoin=\"round\" font-family=\"{SvgFontFamily(StyleFontFamily(chart, style))}\" font-size=\"{F(StyleFontSize(style, fontSize))}\" font-weight=\"{StyleWeight(style, "700")}\"{SvgTextStyleAttributes(style)}>{Escape(label)}</text>");
    }

    private static bool ShouldDrawDataLabels(Chart chart, ChartSeries series) => series.ShowDataLabels ?? chart.Options.ShowDataLabels;

    private static bool HasHorizontalBarDataLabels(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.HorizontalBar && ShouldDrawDataLabels(chart, series));

    private static ChartDataLabelPlacement DataLabelPlacement(Chart chart, ChartSeries? series) => series?.DataLabelPlacement ?? chart.Options.DataLabelPlacement;

    private static ChartColor DataLabelConnectorColor(Chart chart) => chart.Options.DataLabelConnectorColor ?? chart.Options.Theme.MutedText;

    private static ChartTextStyle SeriesDataLabelStyle(Chart chart, ChartSeries? series) => DataLabelStyle(chart, series);

    private static ChartTextStyle DataLabelStyle(Chart chart, ChartSeries? series, int pointIndex = -1) {
        if (series != null && pointIndex >= 0 && pointIndex < series.PointDataLabelStyles.Count) {
            var pointStyle = series.PointDataLabelStyles[pointIndex];
            if (pointStyle != null && pointStyle.HasOverrides) return pointStyle;
        }

        return series != null && series.DataLabelStyle.HasOverrides ? series.DataLabelStyle : chart.Options.DataLabelStyle;
    }

    private static void DrawHorizontalValueLabel(StringBuilder sb, Chart chart, string label, double x, double y, string anchor, ChartRect plot, ChartSeries? series = null, int pointIndex = -1) {
        var t = chart.Options.Theme;
        var style = DataLabelStyle(chart, series, pointIndex);
        var fontSize = StyleFontSize(style, t.DataLabelFontSize);
        label = TrimSvgLabelToWidth(label, fontSize, PlotLabelMaxWidth(plot));
        if (label.Length == 0) return;

        var width = EstimateTextWidth(label, fontSize);
        var effectiveAnchor = anchor == "end" ? "end" : "start";
        var safeX = effectiveAnchor == "end"
            ? Clamp(x, plot.Left + width + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - ChartVisualPrimitives.DataLabelPlotInset)
            : Clamp(x, plot.Left + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - width - ChartVisualPrimitives.DataLabelPlotInset);
        if (safeX < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) {
            effectiveAnchor = "start";
            safeX = plot.Left + ChartVisualPrimitives.DataLabelPlotInset;
        } else if (safeX > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) {
            effectiveAnchor = "end";
            safeX = plot.Right - ChartVisualPrimitives.DataLabelPlotInset;
        }

        var safeY = Clamp(y, plot.Top + ChartVisualPrimitives.DataLabelPlotInset + fontSize / 2.0, plot.Bottom - ChartVisualPrimitives.DataLabelPlotInset - fontSize / 2.0);
        sb.AppendLine($"<text data-cfx-role=\"data-label\" x=\"{F(safeX)}\" y=\"{F(safeY)}\" text-anchor=\"{effectiveAnchor}\" dominant-baseline=\"middle\" fill=\"{StyleColor(style, t.Text).ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"3\" paint-order=\"stroke fill\" stroke-linejoin=\"round\" font-family=\"{SvgFontFamily(StyleFontFamily(chart, style))}\" font-size=\"{F(StyleFontSize(style, fontSize))}\" font-weight=\"{StyleWeight(style, "700")}\"{SvgTextStyleAttributes(style)}>{Escape(label)}</text>");
    }

    private static bool ReserveSvgHorizontalLabel(string label, double x, double y, string anchor, Chart chart, ChartRect plot, List<ChartLabelBounds> reserved) {
        var fontSize = chart.Options.Theme.DataLabelFontSize;
        label = TrimSvgLabelToWidth(label, fontSize, PlotLabelMaxWidth(plot));
        if (label.Length == 0) return false;

        var width = EstimateTextWidth(label, fontSize) + 8;
        var height = fontSize + 6;
        var effectiveAnchor = anchor == "end" ? "end" : "start";
        var safeX = effectiveAnchor == "end"
            ? Clamp(x, plot.Left + width + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - ChartVisualPrimitives.DataLabelPlotInset)
            : Clamp(x, plot.Left + ChartVisualPrimitives.DataLabelPlotInset, plot.Right - width - ChartVisualPrimitives.DataLabelPlotInset);
        if (safeX < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) {
            effectiveAnchor = "start";
            safeX = plot.Left + ChartVisualPrimitives.DataLabelPlotInset;
        } else if (safeX > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) {
            effectiveAnchor = "end";
            safeX = plot.Right - ChartVisualPrimitives.DataLabelPlotInset;
        }

        var left = effectiveAnchor == "end" ? safeX - width : safeX;
        var safeY = Clamp(y, plot.Top + ChartVisualPrimitives.DataLabelPlotInset + height / 2.0, plot.Bottom - ChartVisualPrimitives.DataLabelPlotInset - height / 2.0);
        var bounds = new ChartLabelBounds(left, safeY - height / 2, width, height);
        foreach (var item in reserved) if (bounds.Intersects(item)) return false;
        reserved.Add(bounds);
        return true;
    }

    private static LabelPillPlacement PlaceLabelPill(double x, double width, string anchor, ChartRect plot) {
        var minX = plot.Left + 4;
        var maxX = plot.Right - 4;
        var effectiveAnchor = anchor == "end" ? "end" : "start";
        var effectiveX = Clamp(x, minX, maxX);
        var rectX = effectiveAnchor == "end" ? effectiveX - width : effectiveX;

        if (rectX < minX) {
            effectiveAnchor = "start";
            effectiveX = minX;
            rectX = effectiveX;
        }

        if (rectX + width > maxX) {
            effectiveAnchor = "end";
            effectiveX = maxX;
            rectX = effectiveX - width;
        }

        if (rectX < minX) rectX = minX;
        return new LabelPillPlacement(effectiveX, rectX, effectiveAnchor);
    }

    private static string EdgeAwareAnchor(string label, double x, ChartRect plot, double fontSize) {
        var halfWidth = EstimateTextWidth(label, fontSize) / 2;
        if (x - halfWidth < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) return "start";
        if (x + halfWidth > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) return "end";
        return "middle";
    }

    private static double EdgeAwareTextX(string label, double x, ChartRect plot, double fontSize) {
        var halfWidth = EstimateTextWidth(label, fontSize) / 2;
        if (x - halfWidth < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) return plot.Left + ChartVisualPrimitives.DataLabelPlotInset;
        if (x + halfWidth > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) return plot.Right - ChartVisualPrimitives.DataLabelPlotInset;
        return x;
    }

    private static string RotatedAnchor(string label, double x, ChartRect plot, double angle, double fontSize) {
        var projectedWidth = EstimateTextWidth(label, fontSize) * Math.Abs(Math.Cos(angle * Math.PI / 180));
        if (x - projectedWidth < plot.Left + ChartVisualPrimitives.DataLabelPlotInset) return "start";
        if (x + projectedWidth > plot.Right - ChartVisualPrimitives.DataLabelPlotInset) return "end";
        return angle < 0 ? "end" : "start";
    }

    private static double PlotLabelMaxWidth(ChartRect plot) =>
        Math.Max(8, plot.Width - ChartVisualPrimitives.DataLabelPlotInset * 2);

    private static double EstimateTextWidth(string text, double fontSize) {
        var width = 0.0;
        foreach (var ch in text) width += char.IsWhiteSpace(ch) ? fontSize * 0.34 : char.IsUpper(ch) ? fontSize * 0.62 : fontSize * 0.54;
        return width;
    }

    private static string TrimSvgLabelToWidth(string value, double fontSize, double maxWidth) {
        if (string.IsNullOrEmpty(value) || EstimateTextWidth(value, fontSize) <= maxWidth) return value;
        const string suffix = "...";
        if (EstimateTextWidth(suffix, fontSize) > maxWidth) return string.Empty;

        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = low + (high - low + 1) / 2;
            var candidate = value.Substring(0, mid).TrimEnd() + suffix;
            if (EstimateTextWidth(candidate, fontSize) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return low == 0 ? suffix : value.Substring(0, low).TrimEnd() + suffix;
    }

    private static double TextFontSizeForSvgWidth(string text, double maxWidth, double preferredFontSize, double minFontSize = 8) {
        if (string.IsNullOrEmpty(text) || maxWidth <= 0) return preferredFontSize;
        var fontSize = preferredFontSize;
        while (fontSize > minFontSize && EstimateTextWidth(text, fontSize) > maxWidth) fontSize -= 0.5;
        return Math.Max(minFontSize, fontSize);
    }

    private static void DrawSvgTextCenteredX(StringBuilder sb, Chart chart, string role, string text, double centerX, double y, ChartColor fill, double fontSize, double maxWidth, string fontWeight, ChartColor? stroke = null, double strokeWidth = 0, bool middleBaseline = true, ChartTextStyle? style = null) {
        var preferredFontSize = StyleFontSize(style, fontSize);
        var fittedFontSize = TextFontSizeForSvgWidth(text, Math.Max(8, maxWidth), preferredFontSize);
        var fittedText = TrimSvgLabelToWidth(text, fittedFontSize, Math.Max(8, maxWidth));
        if (fittedText.Length == 0) return;

        var roleAttribute = string.IsNullOrEmpty(role) ? string.Empty : $" data-cfx-role=\"{role}\"";
        var baselineAttribute = middleBaseline ? " dominant-baseline=\"middle\"" : string.Empty;
        var strokeAttribute = stroke.HasValue && strokeWidth > 0
            ? $" stroke=\"{stroke.Value.ToCss()}\" stroke-width=\"{F(strokeWidth)}\" paint-order=\"stroke fill\" stroke-linejoin=\"round\""
            : string.Empty;
        sb.AppendLine($"<text{roleAttribute} x=\"{F(centerX)}\" y=\"{F(y)}\" text-anchor=\"middle\"{baselineAttribute} fill=\"{StyleColor(style, fill).ToCss()}\"{strokeAttribute} font-family=\"{SvgFontFamily(StyleFontFamily(chart, style))}\" font-size=\"{F(fittedFontSize)}\" font-weight=\"{StyleWeight(style, fontWeight)}\"{SvgTextStyleAttributes(style)}>{Escape(fittedText)}</text>");
    }

    private static void DrawSvgTextLeft(StringBuilder sb, Chart chart, string role, string text, double x, double y, ChartColor fill, double fontSize, double maxWidth, string fontWeight, ChartTextStyle? style = null) {
        var preferredFontSize = StyleFontSize(style, fontSize);
        var fittedFontSize = TextFontSizeForSvgWidth(text, Math.Max(8, maxWidth), preferredFontSize);
        var fittedText = TrimSvgLabelToWidth(text, fittedFontSize, Math.Max(8, maxWidth));
        if (fittedText.Length == 0) return;
        var roleAttribute = string.IsNullOrEmpty(role) ? string.Empty : $" data-cfx-role=\"{role}\"";
        sb.AppendLine($"<text{roleAttribute} x=\"{F(x)}\" y=\"{F(y)}\" fill=\"{StyleColor(style, fill).ToCss()}\" font-family=\"{SvgFontFamily(StyleFontFamily(chart, style))}\" font-size=\"{F(fittedFontSize)}\" font-weight=\"{StyleWeight(style, fontWeight)}\"{SvgTextStyleAttributes(style)}>{Escape(fittedText)}</text>");
    }

    private static void DrawSvgXAxisTitle(StringBuilder sb, Chart chart, ChartRect plot, double y, string role = "") {
        if (string.IsNullOrWhiteSpace(chart.XAxisTitle)) return;
        DrawSvgTextCenteredX(sb, chart, role, chart.XAxisTitle, plot.Left + plot.Width / 2, y, chart.Options.Theme.MutedText, chart.Options.Theme.AxisTitleFontSize, plot.Width - 4, "600", middleBaseline: false, style: chart.Options.AxisTitleStyle);
    }

    private static void DrawSvgYAxisTitle(StringBuilder sb, Chart chart, ChartRect plot, double axisX, string role = "") {
        if (string.IsNullOrWhiteSpace(chart.YAxisTitle)) return;
        var t = chart.Options.Theme;
        var maxWidth = Math.Max(40, plot.Height * 0.72);
        var style = chart.Options.AxisTitleStyle;
        var fontSize = TextFontSizeForSvgWidth(chart.YAxisTitle, maxWidth, StyleFontSize(style, t.AxisTitleFontSize));
        var text = TrimSvgLabelToWidth(chart.YAxisTitle, fontSize, maxWidth);
        if (text.Length == 0) return;
        var roleAttribute = string.IsNullOrWhiteSpace(role) ? string.Empty : $" data-cfx-role=\"{role}\"";
        sb.AppendLine($"<text{roleAttribute} transform=\"translate({F(axisX)} {F(plot.Top + plot.Height / 2)}) rotate(-90)\" text-anchor=\"middle\" fill=\"{StyleColor(style, t.MutedText).ToCss()}\" font-family=\"{SvgFontFamily(StyleFontFamily(chart, style))}\" font-size=\"{F(fontSize)}\" font-weight=\"{StyleWeight(style, "600")}\"{SvgTextStyleAttributes(style)}>{Escape(text)}</text>");
    }

    private static ChartColor StyleColor(ChartTextStyle? style, ChartColor fallback) => style?.Color ?? fallback;

    private static double StyleFontSize(ChartTextStyle? style, double fallback) => style?.FontSize ?? fallback;

    private static string StyleWeight(ChartTextStyle? style, string fallback) => style?.FontWeight ?? fallback;

    private static string StyleFontFamily(Chart chart, ChartTextStyle? style) => style?.FontFamily ?? chart.Options.Theme.FontFamily;

    private static string SvgTextStyleAttributes(ChartTextStyle? style) {
        if (style == null) return string.Empty;
        var value = string.Empty;
        if (style.Italic) value += " font-style=\"italic\"";
        if (style.Underline) value += " text-decoration=\"underline\"";
        return value;
    }

    private static ChartColor Color(Chart chart, int index) => chart.Series[index].Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];

    private static ChartColor PointColor(Chart chart, ChartSeries series, int seriesIndex, int pointIndex) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value
            : Color(chart, seriesIndex);

    private static string BarFill(Chart chart, ChartSeries series, int seriesIndex, int pointIndex, string id) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value.ToCss()
            : $"url(#{id}-seriesFill{seriesIndex})";

    private static bool ShowXAxis(Chart chart) => chart.Options.ShowAxes && chart.Options.ShowXAxis;

    private static bool ShowYAxis(Chart chart) => chart.Options.ShowAxes && chart.Options.ShowYAxis;

    private static bool ShowAxisLines(Chart chart) => chart.Options.ShowAxes && chart.Options.ShowAxisLines;

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatNumber(double v) => ChartNumericFormatter.FormatCompact(v);

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

    private static string SvgFontFamily(string value) => Escape(string.IsNullOrWhiteSpace(value) ? "system-ui, sans-serif" : value);

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    private static IReadOnlyList<double> GetXTicks(Chart chart, ChartRange range, ChartRect plot) {
        if (chart.Options.XAxisLabels.Count == 0) {
            var ticks = ChartTicks.GenerateInside(range.MinX, range.MaxX, chart.Options.TickCount);
            if (chart.Options.XAxisLabelDensity == ChartLabelDensity.All || ticks.Count < 3) return ticks;
            var generatedLabels = ticks.Select(tick => new ChartAxisLabel(tick, FormatXAxisValue(chart, tick))).ToArray();
            return SelectXAxisTickValues(chart, range, plot, generatedLabels);
        }

        var labels = chart.Options.XAxisLabels
            .Where(label => label.Value >= range.MinX && label.Value <= range.MaxX)
            .OrderBy(label => label.Value)
            .ToArray();
        return SelectXAxisTickValues(chart, range, plot, labels);
    }

    private static IReadOnlyList<double> SelectXAxisTickValues(Chart chart, ChartRange range, ChartRect plot, IReadOnlyList<ChartAxisLabel> labels) {
        if (chart.Options.XAxisLabelDensity == ChartLabelDensity.All || labels.Count < 3) return labels.Select(label => label.Value).ToArray();
        var widest = labels.Max(label => EstimateTextWidth(label.Text, chart.Options.Theme.TickLabelFontSize));
        var densityFactor = chart.Options.XAxisLabelDensity == ChartLabelDensity.Dense ? 0.72 : chart.Options.XAxisLabelDensity == ChartLabelDensity.Relaxed ? 1.35 : 1.0;
        var minSpacing = Math.Max(28, (widest + 18) * densityFactor);
        var maxCount = Math.Max(2, (int)Math.Floor(plot.Width / minSpacing) + 1);
        if (labels.Count <= maxCount && LabelsHaveMinimumLabelGap(labels, range, plot, chart.Options.Theme.TickLabelFontSize, 6)) return labels.Select(label => label.Value).ToArray();

        var lastLabel = labels[labels.Count - 1];
        var step = Math.Max(1, (int)Math.Ceiling((labels.Count - 1) / (double)(maxCount - 1)));
        var selected = new List<ChartAxisLabel>();
        selected.Add(labels[0]);
        for (var i = step; i < labels.Count - 1; i += step) {
            if (LabelGap(selected[selected.Count - 1], labels[i], range, plot, chart.Options.Theme.TickLabelFontSize) >= 6 &&
                LabelGap(labels[i], lastLabel, range, plot, chart.Options.Theme.TickLabelFontSize) >= 6) selected.Add(labels[i]);
        }

        if (selected.Count > 1 && LabelGap(selected[selected.Count - 1], lastLabel, range, plot, chart.Options.Theme.TickLabelFontSize) < 6) selected.RemoveAt(selected.Count - 1);
        selected.Add(lastLabel);
        return selected.Select(label => label.Value).ToArray();
    }

    private static bool LabelsHaveMinimumLabelGap(IReadOnlyList<ChartAxisLabel> labels, ChartRange range, ChartRect plot, double fontSize, double minGap) {
        for (var i = 1; i < labels.Count; i++) {
            if (LabelGap(labels[i - 1], labels[i], range, plot, fontSize) < minGap) return false;
        }

        return true;
    }

    private static double LabelGap(ChartAxisLabel left, ChartAxisLabel right, ChartRange range, ChartRect plot, double fontSize) {
        var leftWidth = EstimateTextWidth(left.Text, fontSize);
        var rightWidth = EstimateTextWidth(right.Text, fontSize);
        var leftX = Clamp(ProjectX(left.Value, range, plot) - leftWidth / 2.0, plot.Left + 2, plot.Right - leftWidth - 2);
        var rightX = Clamp(ProjectX(right.Value, range, plot) - rightWidth / 2.0, plot.Left + 2, plot.Right - rightWidth - 2);
        return rightX - (leftX + leftWidth);
    }

    private static IReadOnlyList<double> GetHorizontalCategoryTicks(Chart chart, ChartRange range) {
        var categories = new SortedSet<double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var point in series.Points) {
                if (point.X >= range.MinY && point.X <= range.MaxY) categories.Add(point.X);
            }
        }

        if (categories.Count > 0) return categories.ToArray();
        return ChartTicks.GenerateInside(range.MinY, range.MaxY, chart.Options.TickCount);
    }

    private static double ProjectX(double value, ChartRange range, ChartRect plot) {
        var span = range.MaxX - range.MinX;
        if (Math.Abs(span) < 0.0000001) return plot.Left + plot.Width / 2;
        return plot.Left + (value - range.MinX) / span * plot.Width;
    }

    private static string FormatX(Chart chart, double value) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - value) < 0.000001) return label.Text;
        }

        return FormatXAxisValue(chart, value);
    }

    private static string FormatXAxisValue(Chart chart, double value) {
        var formatter = chart.Options.XAxisValueFormatter;
        if (formatter == null) return FormatNumber(value);
        return formatter(value) ?? string.Empty;
    }

    private static string BuildLinePath(IReadOnlyList<ChartPoint> points, bool smooth) {
        return BuildPath(ChartPathBuilder.FromPoints(points, ChartSeriesKind.Line, smooth));
    }

    private static string BuildStepLinePath(IReadOnlyList<ChartPoint> points) {
        return BuildPath(ChartPathBuilder.FromPoints(points, ChartSeriesKind.StepLine, false));
    }

    private static string BuildPath(ChartPath chartPath) {
        if (chartPath.Commands.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        foreach (var command in chartPath.Commands) {
            if (command.Kind == ChartPathCommandKind.MoveTo) {
                sb.Append("M ").Append(F(command.X)).Append(' ').Append(F(command.Y));
            } else if (command.Kind == ChartPathCommandKind.LineTo) {
                sb.Append(" L ").Append(F(command.X)).Append(' ').Append(F(command.Y));
            } else if (command.Kind == ChartPathCommandKind.CubicTo) {
                sb.Append(" C ").Append(F(command.Control1X)).Append(' ').Append(F(command.Control1Y)).Append(' ')
                    .Append(F(command.Control2X)).Append(' ').Append(F(command.Control2Y)).Append(' ')
                    .Append(F(command.X)).Append(' ').Append(F(command.Y));
            }
        }

        return sb.ToString();
    }

    private static string BuildId(Chart chart, string idScope) {
        unchecked {
            uint hash = 2166136261;
            Add(ref hash, idScope ?? string.Empty);
            Add(ref hash, chart.Title);
            Add(ref hash, chart.Subtitle);
            Add(ref hash, chart.Options.Size.Width.ToString(CultureInfo.InvariantCulture));
            Add(ref hash, chart.Options.Size.Height.ToString(CultureInfo.InvariantCulture));
            foreach (var series in chart.Series) {
                Add(ref hash, series.Name);
                Add(ref hash, series.Kind.ToString());
                foreach (var point in series.Points) {
                    Add(ref hash, point.X.ToString("R", CultureInfo.InvariantCulture));
                    Add(ref hash, point.Y.ToString("R", CultureInfo.InvariantCulture));
                }
            }

            return "cfx" + hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }

    private static void Add(ref uint hash, string value) {
        AddRaw(ref hash, value.Length.ToString(CultureInfo.InvariantCulture));
        AddRaw(ref hash, ":");
        AddRaw(ref hash, value);
        AddRaw(ref hash, "|");
    }

    private static void AddRaw(ref uint hash, string value) {
        foreach (var ch in value) {
            hash ^= ch;
            hash *= 16777619;
        }
    }

    private static string BuildDescription(Chart chart) {
        var title = string.IsNullOrWhiteSpace(chart.Title) ? "Chart" : chart.Title;
        if (chart.Series.Count == 0) return title + " with no data series.";
        var names = string.Join(", ", chart.Series.Select(series => series.Name).ToArray());
        return title + " with " + chart.Series.Count.ToString(CultureInfo.InvariantCulture) + " data series: " + names + ".";
    }

    private static bool IsPieLike(Chart chart) => chart.Series.Count > 0 && (chart.Series[0].Kind == ChartSeriesKind.Pie || chart.Series[0].Kind == ChartSeriesKind.Donut);

    private static bool IsHorizontalBarChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.HorizontalBar);

    private static bool IsHeatmapChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Heatmap);

    private static bool IsGaugeChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Gauge);

    private static bool IsRadialBarChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.RadialBar);

    private static bool IsBulletChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Bullet);

    private static bool IsWaterfallChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Waterfall);

    private static bool IsRadarChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Radar);

    private static bool IsPolarAreaChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.PolarArea);

    private static bool IsFunnelChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Funnel);

    private static bool IsTimelineChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Timeline);

    private static bool IsGanttChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Gantt);

    private static bool IsSankeyChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Sankey);

    private static bool IsTreeChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Tree);

    private static bool IsSunburstChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Sunburst);

    private static bool IsPictorialChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Pictorial);

    private static bool IsProgressBarChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.ProgressBar);

    private static bool IsWordCloudChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.WordCloud);

    private static string SliceLabel(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        }

        return "Slice " + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static bool CanUsePointLegend(ChartSeries series) => VisualPointCount(series) > 1;

    private static int VisualPointCount(ChartSeries series) {
        var tupleSize = VisualTupleSize(series.Kind);
        return tupleSize <= 1 ? series.Points.Count : series.Points.Count / tupleSize;
    }

    private static int VisualPointRawIndex(ChartSeries series, int pointIndex) {
        var tupleSize = VisualTupleSize(series.Kind);
        return tupleSize <= 1 ? pointIndex : pointIndex * tupleSize;
    }

    private static int VisualTupleSize(ChartSeriesKind kind) =>
        kind == ChartSeriesKind.Bubble || kind == ChartSeriesKind.RangeBand || kind == ChartSeriesKind.RangeArea || kind == ChartSeriesKind.RangeBar || kind == ChartSeriesKind.Dumbbell
            ? 2
            : kind == ChartSeriesKind.ErrorBar
                ? 3
                : kind == ChartSeriesKind.Candlestick || kind == ChartSeriesKind.Ohlc
                    ? 4
                    : kind == ChartSeriesKind.BoxPlot
                        ? 5
                        : 1;

    private static string LegendPointLabel(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        }

        return "Item " + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private sealed class LegendRow {
        public List<LegendItem> Items { get; } = new();

        public double Width { get; set; }
    }

    private readonly struct LegendItem {
        public LegendItem(int seriesIndex, int pointIndex, double x, double width, string label, ChartColor color) {
            SeriesIndex = seriesIndex;
            PointIndex = pointIndex;
            X = x;
            Width = width;
            Label = label;
            Color = color;
        }

        public int SeriesIndex { get; }

        public int PointIndex { get; }

        public double X { get; }

        public double Width { get; }

        public string Label { get; }

        public ChartColor Color { get; }
    }

    private readonly struct LegendEntry {
        public LegendEntry(int seriesIndex, int pointIndex, string label, ChartColor color) {
            SeriesIndex = seriesIndex;
            PointIndex = pointIndex;
            Label = label;
            Color = color;
        }

        public int SeriesIndex { get; }

        public int PointIndex { get; }

        public string Label { get; }

        public ChartColor Color { get; }
    }

    private readonly struct BarLayoutInfo {
        public BarLayoutInfo(double barWidth, double offset) {
            BarWidth = barWidth;
            Offset = offset;
        }

        public double BarWidth { get; }

        public double Offset { get; }
    }

    private readonly struct HorizontalBarLayoutInfo {
        public HorizontalBarLayoutInfo(double barHeight, double offset) {
            BarHeight = barHeight;
            Offset = offset;
        }

        public double BarHeight { get; }

        public double Offset { get; }
    }

    private readonly struct LabelPillPlacement {
        public LabelPillPlacement(double x, double rectX, string anchor) {
            X = x;
            RectX = rectX;
            Anchor = anchor;
        }

        public double X { get; }

        public double RectX { get; }

        public string Anchor { get; }
    }
}
