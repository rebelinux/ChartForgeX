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
    private static void DrawCalendarHeatmap(StringBuilder sb, Chart chart, ChartRect basePlot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.CalendarHeatmap);
        if (series == null || series.Points.Count == 0) return;
        var cells = CalendarHeatmapCells(series);
        if (cells.Count == 0) return;

        var t = chart.Options.Theme;
        var minDate = cells.Min(item => item.Date);
        var maxDate = cells.Max(item => item.Date);
        var start = CalendarWeekStart(minDate);
        var end = CalendarWeekEnd(maxDate);
        var columns = Math.Max(1, ((end - start).Days / 7) + 1);
        var min = cells.Min(item => item.Value);
        var max = cells.Max(item => item.Value);
        var sourceMin = min;
        var sourceMax = max;
        if (Math.Abs(max - min) < 0.000001) max = min + 1;

        var leftReserve = chart.Options.ShowAxes ? 34 : 6;
        var topReserve = chart.Options.ShowAxes ? 24 : 6;
        var bottomReserve = chart.Options.ShowHeatmapScale ? 38 : 8;
        var plot = new ChartRect(basePlot.Left + leftReserve, basePlot.Top + topReserve, Math.Max(1, basePlot.Width - leftReserve - 8), Math.Max(1, basePlot.Height - topReserve - bottomReserve));
        var gap = columns > 32 ? 2.5 : 3.5;
        var cell = Math.Max(1, Math.Min((plot.Width - gap * (columns - 1)) / columns, (plot.Height - gap * 6) / 7));
        var gridWidth = columns * cell + (columns - 1) * gap;
        var gridHeight = 7 * cell + 6 * gap;
        var x0 = plot.Left + Math.Max(0, (plot.Width - gridWidth) / 2);
        var y0 = plot.Top + Math.Max(0, (plot.Height - gridHeight) / 2);
        var radius = Math.Min(4, cell * 0.22);
        var byDate = cells.ToDictionary(item => item.Date, item => item);
        var totalDays = (end - start).Days + 1;
        var filledDays = byDate.Count;
        var emptyDays = Math.Max(0, totalDays - filledDays);
        var hasEmptyCells = emptyDays > 0;
        var startText = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endText = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var containerSummary = series.Name + " calendar heatmap from " + startText + " to " + endText + " with " + filledDays.ToString(CultureInfo.InvariantCulture) + " filled days and " + emptyDays.ToString(CultureInfo.InvariantCulture) + " empty days";

        sb.AppendLine($"<g data-cfx-role=\"calendar-heatmap\" data-cfx-label=\"{Escape(series.Name)}\" data-cfx-start-date=\"{startText}\" data-cfx-end-date=\"{endText}\" data-cfx-day-count=\"{totalDays}\" data-cfx-filled-day-count=\"{filledDays}\" data-cfx-empty-day-count=\"{emptyDays}\" data-cfx-min-value=\"{F(sourceMin)}\" data-cfx-max-value=\"{F(sourceMax)}\" role=\"group\" aria-label=\"{Escape(containerSummary)}\">");
        DrawCalendarHeatmapSvgAxes(sb, chart, start, maxDate, x0, y0, cell, gap);
        for (var day = start; day <= end; day = day.AddDays(1)) {
            var column = (day - start).Days / 7;
            var row = (int)day.DayOfWeek;
            var hasValue = byDate.TryGetValue(day, out var entry);
            var value = hasValue ? entry.Value : 0;
            var ratio = hasValue ? CalendarHeatmapRatio(value, min, max) : 0;
            var level = hasValue ? (int)Math.Ceiling(ratio * 4) : 0;
            var status = hasValue ? HeatmapStatus(ratio) : "empty";
            var color = hasValue ? CalendarHeatmapColor(chart, series, entry.Color, value, min, max) : CalendarHeatmapEmptyColor(chart);
            var x = x0 + column * (cell + gap);
            var y = y0 + row * (cell + gap);
            var dateText = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var summary = hasValue ? series.Name + ", " + dateText + ": " + FormatValue(chart, value) : series.Name + ", " + dateText + ": No data";
            sb.AppendLine($"<rect class=\"cfx-interactive-region\" tabindex=\"0\" focusable=\"true\" data-cfx-role=\"calendar-heatmap-cell\" data-cfx-date=\"{dateText}\" data-cfx-week-index=\"{column}\" data-cfx-weekday-index=\"{row}\" data-cfx-value=\"{F(value)}\" data-cfx-level=\"{level}\" data-cfx-empty=\"{(!hasValue).ToString().ToLowerInvariant()}\" data-cfx-status=\"{status}\" role=\"img\" aria-label=\"{Escape(summary)}\" x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(cell)}\" height=\"{F(cell)}\" rx=\"{F(radius)}\" fill=\"{color.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-opacity=\"{F(ChartVisualPrimitives.HeatmapCellBorderOpacity)}\" stroke-width=\"{F(ChartVisualPrimitives.HeatmapCellBorderStrokeWidth)}\"><title>{Escape(summary)}</title></rect>");
        }

        if (chart.Options.ShowHeatmapScale) DrawCalendarHeatmapSvgScale(sb, chart, series, min, max, x0 + gridWidth, y0 + gridHeight + 20, cell, hasEmptyCells);
        sb.AppendLine("</g>");
    }

    private static void DrawCalendarHeatmapSvgAxes(StringBuilder sb, Chart chart, DateTime start, DateTime end, double x0, double y0, double cell, double gap) {
        if (!chart.Options.ShowAxes) return;
        var t = chart.Options.Theme;
        var rows = new[] { (1, "Mon"), (3, "Wed"), (5, "Fri") };
        foreach (var item in rows) {
            var y = y0 + item.Item1 * (cell + gap) + cell / 2;
            sb.AppendLine($"<text data-cfx-role=\"calendar-heatmap-weekday-label\" x=\"{F(x0 - 8)}\" y=\"{F(y)}\" text-anchor=\"end\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{item.Item2}</text>");
        }

        var month = new DateTime(start.Year, start.Month, 1);
        while (month < start) month = month.AddMonths(1);
        var lastX = x0 - 40;
        while (month <= end) {
            var column = Math.Max(0, (month - start).Days / 7);
            var x = x0 + column * (cell + gap);
            if (x - lastX >= 28) {
                sb.AppendLine($"<text data-cfx-role=\"calendar-heatmap-month-label\" x=\"{F(x)}\" y=\"{F(y0 - 8)}\" text-anchor=\"start\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"650\">{month.ToString("MMM", CultureInfo.InvariantCulture)}</text>");
                lastX = x;
            }

            month = month.AddMonths(1);
        }
    }

    private static void DrawCalendarHeatmapSvgScale(StringBuilder sb, Chart chart, ChartSeries series, double min, double max, double right, double y, double cell, bool showNoData) {
        var t = chart.Options.Theme;
        var size = Math.Max(7, Math.Min(12, cell));
        var gap = Math.Max(2, size * 0.28);
        var width = 5 * size + 4 * gap;
        var noDataWidth = showNoData ? size + gap : 0;
        var x = right - noDataWidth - width - EstimateTextWidth("More", t.TickLabelFontSize) - 10;
        var lessLabelX = x - 8;
        if (showNoData) {
            var noData = CalendarHeatmapEmptyColor(chart);
            sb.AppendLine($"<rect data-cfx-role=\"calendar-heatmap-scale-no-data\" data-cfx-status=\"empty\" x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(size)}\" height=\"{F(size)}\" rx=\"{F(Math.Min(3, size * 0.22))}\" fill=\"{noData.ToCss()}\"><title>No data</title></rect>");
            x += size + gap;
        }

        sb.AppendLine($"<text data-cfx-role=\"calendar-heatmap-scale-label\" x=\"{F(lessLabelX)}\" y=\"{F(y + size / 2)}\" text-anchor=\"end\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">Less</text>");
        for (var i = 0; i < 5; i++) {
            var value = min + (max - min) * (i / 4.0);
            var ratio = CalendarHeatmapRatio(value, min, max);
            var color = CalendarHeatmapColor(chart, series, null, value, min, max);
            sb.AppendLine($"<rect data-cfx-role=\"calendar-heatmap-scale-step\" data-cfx-level=\"{i}\" data-cfx-value=\"{F(value)}\" data-cfx-status=\"{HeatmapStatus(ratio)}\" x=\"{F(x + i * (size + gap))}\" y=\"{F(y)}\" width=\"{F(size)}\" height=\"{F(size)}\" rx=\"{F(Math.Min(3, size * 0.22))}\" fill=\"{color.ToCss()}\"/>");
        }
        sb.AppendLine($"<text data-cfx-role=\"calendar-heatmap-scale-label\" x=\"{F(x + width + 8)}\" y=\"{F(y + size / 2)}\" text-anchor=\"start\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">More</text>");
    }

    private static ChartColor CalendarHeatmapColor(Chart chart, ChartSeries series, ChartColor? pointColor, double value, double min, double max) {
        var ratio = CalendarHeatmapRatio(value, min, max);
        var high = pointColor ?? series.Color ?? chart.Options.Theme.Positive;
        return Blend(chart.Options.Theme.PlotBackground, high, 0.30 + ratio * 0.70);
    }

    private static ChartColor CalendarHeatmapEmptyColor(Chart chart) {
        var t = chart.Options.Theme;
        var light = (0.2126 * t.PlotBackground.R + 0.7152 * t.PlotBackground.G + 0.0722 * t.PlotBackground.B) / 255.0 > 0.70;
        return light ? Blend(t.PlotBackground, t.MutedText, 0.30) : Blend(t.PlotBackground, t.Grid, 0.72);
    }

    private static double CalendarHeatmapRatio(double value, double min, double max) {
        return Clamp((value - min) / Math.Max(0.000001, max - min), 0, 1);
    }

    private static List<CalendarHeatmapCell> CalendarHeatmapCells(ChartSeries series) {
        var cells = new List<CalendarHeatmapCell>();
        for (var i = 0; i < series.Points.Count; i++) {
            var color = i < series.PointColors.Count ? series.PointColors[i] : null;
            cells.Add(new CalendarHeatmapCell(DateTime.FromOADate(series.Points[i].X).Date, series.Points[i].Y, color));
        }

        return cells;
    }

    private static DateTime CalendarWeekStart(DateTime date) => date.Date.AddDays(-(int)date.DayOfWeek);

    private static DateTime CalendarWeekEnd(DateTime date) => date.Date.AddDays(6 - (int)date.DayOfWeek);

    private static bool IsCalendarHeatmapChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.CalendarHeatmap);

    private readonly struct CalendarHeatmapCell {
        public readonly DateTime Date;
        public readonly double Value;
        public readonly ChartColor? Color;

        public CalendarHeatmapCell(DateTime date, double value, ChartColor? color) {
            Date = date;
            Value = value;
            Color = color;
        }
    }
}
