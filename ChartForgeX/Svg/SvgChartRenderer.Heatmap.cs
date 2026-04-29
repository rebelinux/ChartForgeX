using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawHeatmap(StringBuilder sb, Chart chart, ChartRect basePlot) {
        var rows = chart.Series.Where(series => series.Kind == ChartSeriesKind.Heatmap).ToArray();
        if (rows.Length == 0) return;

        var columns = rows.SelectMany(series => series.Points.Select(point => point.X)).Distinct().OrderBy(value => value).ToArray();
        if (columns.Length == 0) return;

        var t = chart.Options.Theme;
        var values = rows.SelectMany(series => series.Points.Select(point => point.Y)).ToArray();
        var min = values.Length == 0 ? 0 : values.Min();
        var max = values.Length == 0 ? 1 : values.Max();
        if (Math.Abs(max - min) < 0.000001) max = min + 1;

        var plot = ApplyHeatmapLabelReserve(chart, basePlot, rows, columns);
        var gap = Math.Min(6, Math.Max(2, Math.Min(plot.Width / columns.Length, plot.Height / rows.Length) * 0.05));
        var cellWidth = Math.Max(1, (plot.Width - gap * (columns.Length - 1)) / columns.Length);
        var cellHeight = Math.Max(1, (plot.Height - gap * (rows.Length - 1)) / rows.Length);
        var radius = Math.Min(8, Math.Min(cellWidth, cellHeight) * 0.16);

        sb.AppendLine("<g data-cfx-role=\"heatmap\">");
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++) {
            var series = rows[rowIndex];
            var y = plot.Top + rowIndex * (cellHeight + gap);
            if (chart.Options.ShowAxes) {
                var rowLabel = TrimSvgLabelToWidth(series.Name, t.TickLabelFontSize, Math.Max(8, plot.Left - chart.Options.Padding.Left - 2));
                sb.AppendLine($"<text data-cfx-role=\"heatmap-row-label\" x=\"{F(plot.Left - 12)}\" y=\"{F(y + cellHeight / 2)}\" text-anchor=\"end\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"650\">{Escape(rowLabel)}</text>");
            }
            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++) {
                var column = columns[columnIndex];
                var value = FindHeatmapValue(series, column);
                var x = plot.Left + columnIndex * (cellWidth + gap);
                var ratio = HeatmapRatio(value, min, max);
                var status = HeatmapStatus(ratio);
                var color = HeatmapColor(chart, series.Color, value, min, max);
                var summary = series.Name + ", " + FormatX(chart, column) + ": " + FormatValue(chart, value);
                if (chart.Options.HeatmapScale == ChartHeatmapScale.Semantic) summary += ", " + status;
                sb.AppendLine($"<rect data-cfx-role=\"heatmap-cell\" data-cfx-row=\"{rowIndex}\" data-cfx-column=\"{columnIndex}\" data-cfx-status=\"{status}\" role=\"img\" aria-label=\"{Escape(summary)}\" x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(cellWidth)}\" height=\"{F(cellHeight)}\" rx=\"{F(radius)}\" fill=\"{color.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-opacity=\"{F(ChartVisualPrimitives.HeatmapCellBorderOpacity)}\" stroke-width=\"{F(ChartVisualPrimitives.HeatmapCellBorderStrokeWidth)}\"/>");
                if (ShouldDrawDataLabels(chart, series) && cellWidth >= 34 && cellHeight >= 20) {
                    var label = FormatValue(chart, value);
                    var pointIndex = HeatmapPointIndex(series, column);
                    var placement = DataLabelPlacement(chart, series);
                    if (placement == ChartDataLabelPlacement.Auto || placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center) {
                        DrawSvgTextCenteredX(sb, chart, "data-label", label, x + cellWidth / 2, y + cellHeight / 2, HeatmapTextColor(color), t.DataLabelFontSize, cellWidth - 6, "750", style: DataLabelStyle(chart, series, pointIndex));
                    } else if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right || placement == ChartDataLabelPlacement.Outside) {
                        var labelX = placement == ChartDataLabelPlacement.Left ? x - 8 : x + cellWidth + 8;
                        var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
                        var connectorStartX = placement == ChartDataLabelPlacement.Left ? x : x + cellWidth;
                        var connectorEndX = placement == ChartDataLabelPlacement.Left ? x - 5 : x + cellWidth + 5;
                        DrawHeatmapLabelConnector(sb, chart, rowIndex, columnIndex, connectorStartX, y + cellHeight / 2, connectorEndX);
                        DrawHorizontalValueLabel(sb, chart, label, labelX, y + cellHeight / 2, anchor, basePlot, series, pointIndex);
                    } else {
                        DrawDataLabel(sb, chart, label, x + cellWidth / 2, placement == ChartDataLabelPlacement.Above ? y - 8 : y + cellHeight + 12, plot, series: series, pointIndex: pointIndex);
                    }
                }
            }
        }

        if (chart.Options.ShowAxes && chart.Options.ShowHeatmapColumnLabels) {
            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++) {
                var x = plot.Left + columnIndex * (cellWidth + gap) + cellWidth / 2;
                var label = FormatX(chart, columns[columnIndex]);
                label = TrimSvgLabelToWidth(label, t.TickLabelFontSize, Math.Max(8, cellWidth + gap));
                var anchor = EdgeAwareAnchor(label, x, plot, t.TickLabelFontSize);
                var labelX = EdgeAwareTextX(label, x, plot, t.TickLabelFontSize);
                sb.AppendLine($"<text data-cfx-role=\"heatmap-column-label\" x=\"{F(labelX)}\" y=\"{F(plot.Bottom + 22)}\" text-anchor=\"{anchor}\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"650\">{Escape(label)}</text>");
            }

            DrawSvgXAxisTitle(sb, chart, plot, plot.Bottom + 48, "heatmap-x-axis-title");
            if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) {
                var widestRowLabel = rows.Max(series => EstimateTextWidth(series.Name, t.TickLabelFontSize));
                var axisX = Math.Max(24, plot.Left - widestRowLabel - 48);
                DrawSvgYAxisTitle(sb, chart, plot, axisX, "heatmap-y-axis-title");
            }
        }

        if (chart.Options.ShowHeatmapScale) DrawHeatmapScale(sb, chart, plot, min, max, rows[0].Color);
        sb.AppendLine("</g>");
    }

    private static ChartRect ApplyHeatmapLabelReserve(Chart chart, ChartRect plot, IReadOnlyList<ChartSeries> rows, IReadOnlyList<double> columns) {
        var t = chart.Options.Theme;
        var yAxisReserve = chart.Options.ShowAxes && !string.IsNullOrWhiteSpace(chart.YAxisTitle) ? 28 : 0;
        var rowLabelReserve = chart.Options.ShowAxes ? rows.Max(series => EstimateTextWidth(series.Name, t.TickLabelFontSize)) + yAxisReserve + 18 : 0;
        var leftReserve = chart.Options.ShowAxes ? chart.Options.Padding.Left + rowLabelReserve : plot.Left;
        var sideLabelWidth = HeatmapSideLabelWidth(chart, rows, columns);
        var leftLabelReserve = HasHeatmapSideLabels(chart, rows, ChartDataLabelPlacement.Left) ? sideLabelWidth + 22 : 0;
        var rightLabelReserve = HasHeatmapSideLabels(chart, rows, ChartDataLabelPlacement.Right) || HasHeatmapSideLabels(chart, rows, ChartDataLabelPlacement.Outside) ? sideLabelWidth + 22 : 0;
        var axisBottomBase = chart.Options.ShowHeatmapScale ? 56 : chart.Options.ShowHeatmapColumnLabels ? 36 : 10;
        var bottomReserve = chart.Options.ShowAxes ? axisBottomBase + (string.IsNullOrWhiteSpace(chart.XAxisTitle) ? 0 : 20) : 0;
        var desiredLeft = Math.Max(plot.Left, leftReserve + leftLabelReserve);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 220);
        var shift = Math.Max(0, Math.Min(desiredLeft, maxLeft) - plot.Left);
        var maxColumnLabel = chart.Options.ShowAxes && chart.Options.ShowHeatmapColumnLabels ? columns.Max(column => EstimateTextWidth(FormatX(chart, column), t.TickLabelFontSize)) : 0;
        var axesBottom = Math.Max(bottomReserve, maxColumnLabel > 68 ? 70 : bottomReserve);
        var bottom = chart.Options.ShowHeatmapScale ? Math.Max(axesBottom, 56) : axesBottom;
        return new ChartRect(plot.X + shift, plot.Y, Math.Max(1, plot.Width - shift - rightLabelReserve), Math.Max(1, plot.Height - bottom));
    }

    private static bool HasHeatmapSideLabels(Chart chart, IReadOnlyList<ChartSeries> rows, ChartDataLabelPlacement placement) {
        foreach (var row in rows) if (ShouldDrawDataLabels(chart, row) && DataLabelPlacement(chart, row) == placement) return true;
        return false;
    }

    private static double HeatmapSideLabelWidth(Chart chart, IReadOnlyList<ChartSeries> rows, IReadOnlyList<double> columns) {
        var max = 0.0;
        foreach (var row in rows) {
            if (!ShouldDrawDataLabels(chart, row)) continue;
            var placement = DataLabelPlacement(chart, row);
            if (placement != ChartDataLabelPlacement.Left && placement != ChartDataLabelPlacement.Right && placement != ChartDataLabelPlacement.Outside) continue;
            for (var i = 0; i < columns.Count; i++) {
                var pointIndex = HeatmapPointIndex(row, columns[i]);
                var style = DataLabelStyle(chart, row, pointIndex);
                var fontSize = StyleFontSize(style, chart.Options.Theme.DataLabelFontSize);
                max = Math.Max(max, EstimateTextWidth(FormatValue(chart, FindHeatmapValue(row, columns[i])), fontSize));
            }
        }

        return Math.Min(88, max);
    }

    private static void DrawHeatmapScale(StringBuilder sb, Chart chart, ChartRect plot, double min, double max, ChartColor? highColor) {
        var t = chart.Options.Theme;
        const int steps = ChartVisualPrimitives.HeatmapScaleSteps;
        const double width = ChartVisualPrimitives.HeatmapScaleWidth;
        const double height = ChartVisualPrimitives.HeatmapScaleHeight;
        var x = plot.Right - width;
        var y = plot.Bottom + ChartVisualPrimitives.HeatmapScaleOffsetY;
        for (var i = 0; i < steps; i++) {
            var ratio = i / (double)(steps - 1);
            var value = min + (max - min) * ratio;
            var color = HeatmapColor(chart, highColor, value, min, max);
            sb.AppendLine($"<rect data-cfx-role=\"heatmap-scale-step\" data-cfx-status=\"{HeatmapStatus(HeatmapRatio(value, min, max))}\" x=\"{F(x + i * width / steps)}\" y=\"{F(y)}\" width=\"{F(width / steps + ChartVisualPrimitives.HeatmapScaleStepOverlap)}\" height=\"{F(height)}\" rx=\"{F(ChartVisualPrimitives.HeatmapScaleRadius)}\" fill=\"{color.ToCss()}\"/>");
        }

        sb.AppendLine($"<text data-cfx-role=\"heatmap-scale-label\" x=\"{F(x)}\" y=\"{F(y + ChartVisualPrimitives.HeatmapScaleLabelOffsetY)}\" text-anchor=\"start\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(FormatValue(chart, min))}</text>");
        sb.AppendLine($"<text data-cfx-role=\"heatmap-scale-label\" x=\"{F(x + width)}\" y=\"{F(y + ChartVisualPrimitives.HeatmapScaleLabelOffsetY)}\" text-anchor=\"end\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(FormatValue(chart, max))}</text>");
    }

    private static void DrawHeatmapLabelConnector(StringBuilder sb, Chart chart, int rowIndex, int columnIndex, double startX, double y, double endX) {
        sb.AppendLine($"<line data-cfx-role=\"data-label-connector\" data-cfx-row=\"{rowIndex}\" data-cfx-column=\"{columnIndex}\" x1=\"{F(startX)}\" y1=\"{F(y)}\" x2=\"{F(endX)}\" y2=\"{F(y)}\" stroke=\"{DataLabelConnectorColor(chart).ToCss()}\" stroke-width=\"{F(chart.Options.DataLabelConnectorStrokeWidth)}\" stroke-opacity=\"{F(chart.Options.DataLabelConnectorOpacity)}\" stroke-linecap=\"round\"/>");
    }

    private static ChartColor HeatmapColor(Chart chart, ChartColor? highColor, double value, double min, double max) {
        var ratio = HeatmapRatio(value, min, max);
        if (chart.Options.HeatmapScale == ChartHeatmapScale.Semantic) return SemanticHeatmapColor(chart, ratio);
        return Blend(chart.Options.Theme.PlotBackground, highColor ?? chart.Options.Theme.Palette[0], 0.18 + ratio * 0.82);
    }

    private static ChartColor SemanticHeatmapColor(Chart chart, double ratio) {
        var t = chart.Options.Theme;
        if (ratio < 0.60) return Blend(t.Negative, t.Warning, ratio / 0.60 * 0.42);
        if (ratio < 0.80) return Blend(t.Warning, t.Positive, (ratio - 0.60) / 0.20 * 0.5);
        return Blend(t.Warning, t.Positive, 0.65 + (ratio - 0.80) / 0.20 * 0.35);
    }

    private static double HeatmapRatio(double value, double min, double max) {
        if (min >= -0.000001 && max <= 100.000001) return Clamp(value / 100, 0, 1);
        return Clamp((value - min) / Math.Max(0.000001, max - min), 0, 1);
    }

    private static string HeatmapStatus(double ratio) {
        if (ratio < 0.60) return "negative";
        if (ratio < 0.80) return "warning";
        return "positive";
    }

    private static ChartColor Blend(ChartColor a, ChartColor b, double amount) {
        amount = Clamp(amount, 0, 1);
        return ChartColor.FromRgb(
            (byte)Math.Round(a.R + (b.R - a.R) * amount),
            (byte)Math.Round(a.G + (b.G - a.G) * amount),
            (byte)Math.Round(a.B + (b.B - a.B) * amount));
    }

    private static ChartColor HeatmapTextColor(ChartColor background) {
        var luminance = (0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B) / 255;
        return luminance > 0.54 ? ChartColor.FromRgb(15, 23, 42) : ChartColor.White;
    }

    private static double FindHeatmapValue(ChartSeries series, double column) {
        foreach (var point in series.Points) {
            if (Math.Abs(point.X - column) < 0.000001) return point.Y;
        }

        return 0;
    }

    private static int HeatmapPointIndex(ChartSeries series, double column) {
        for (var i = 0; i < series.Points.Count; i++) {
            if (Math.Abs(series.Points[i].X - column) < 0.000001) return i;
        }

        return -1;
    }
}
