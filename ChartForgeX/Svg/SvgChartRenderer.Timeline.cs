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
    private static void DrawTimeline(StringBuilder sb, Chart chart, ChartRect basePlot, string id) {
        var items = BuildTimelineItems(chart);
        if (items.Count == 0) return;

        var t = chart.Options.Theme;
        var min = items.Min(item => item.Start);
        var max = items.Max(item => item.End);
        ApplyTimelineAxisBounds(chart, ref min, ref max);
        var plot = ApplyTimelineReserve(chart, basePlot, items);
        var rowHeight = Math.Max(20, Math.Min(34, plot.Height / items.Count * 0.56));
        var slotHeight = plot.Height / items.Count;
        var ticks = ChartTicks.Generate(min, max, Math.Min(6, Math.Max(3, chart.Options.TickCount)));
        var tickLabelWidth = Math.Max(18, plot.Width / Math.Max(1, ticks.Count - 1) - 6);
        var rowLabelWidth = Math.Max(8, plot.Left - chart.Options.Padding.Left - 2);

        sb.AppendLine("<g data-cfx-role=\"timeline\">");
        DrawTimelineItemGradients(sb, id, items);
        foreach (var tick in ticks) {
            var x = ProjectTimelineX(tick, min, max, plot);
            if (chart.Options.ShowGrid) sb.AppendLine($"<line x1=\"{F(x)}\" y1=\"{F(plot.Top)}\" x2=\"{F(x)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.GridStrokeWidth)}\" opacity=\"{F(ChartVisualPrimitives.TimelineGridOpacity)}\"/>");
            if (chart.Options.ShowAxes) {
                var label = FormatTimelineTick(chart, tick);
                label = TrimSvgLabelToWidth(label, t.TickLabelFontSize, tickLabelWidth);
                var anchor = EdgeAwareAnchor(label, x, plot, t.TickLabelFontSize);
                var labelX = EdgeAwareTextX(label, x, plot, t.TickLabelFontSize);
                sb.AppendLine($"<text data-cfx-role=\"timeline-tick-label\" x=\"{F(labelX)}\" y=\"{F(plot.Bottom + 22)}\" text-anchor=\"{anchor}\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(label)}</text>");
            }
        }

        for (var i = 0; i < items.Count; i++) {
            var item = items[i];
            var y = plot.Top + i * slotHeight + (slotHeight - rowHeight) / 2;
            var x1 = ProjectTimelineX(item.Start, min, max, plot);
            var x2 = ProjectTimelineX(item.End, min, max, plot);
            var left = Math.Min(x1, x2);
            var width = Math.Max(2, Math.Abs(x2 - x1));
            var duration = FormatTimelineDuration(chart, item.Start, item.End);
            var summary = BuildTimelineSummary(chart, item, duration);
            if (chart.Options.ShowGrid) sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(y + rowHeight / 2)}\" x2=\"{F(plot.Right)}\" y2=\"{F(y + rowHeight / 2)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.GridStrokeWidth)}\" opacity=\"{F(ChartVisualPrimitives.TimelineRowGridOpacity)}\"/>");
            if (chart.Options.ShowAxes) {
                var rowLabel = TrimSvgLabelToWidth(item.Name, t.TickLabelFontSize, rowLabelWidth);
                sb.AppendLine($"<text data-cfx-role=\"timeline-row-label\" x=\"{F(plot.Left - 14)}\" y=\"{F(y + rowHeight / 2)}\" text-anchor=\"end\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"650\">{Escape(rowLabel)}</text>");
            }
            DrawTimelineRangeBar(sb, chart, item, id, i, left, y, width, rowHeight, duration, summary);
            if (item.ShowDataLabels && width >= 72) {
                DrawSvgTextCenteredX(sb, chart, "data-label", duration, left + width / 2, y + rowHeight / 2, HeatmapTextColor(item.Color), t.DataLabelFontSize, width - 6, "750");
            }
        }

        if (chart.Options.ShowAxes) {
            sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(plot.Bottom)}\" x2=\"{F(plot.Right)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.AxisStrokeWidth)}\"/>");
            DrawSvgXAxisTitle(sb, chart, plot, plot.Bottom + 49, "timeline-x-axis-title");
            if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) {
                var widestLabel = items.Max(item => EstimateTextWidth(item.Name, t.TickLabelFontSize));
                var axisX = Math.Max(24, plot.Left - widestLabel - 46);
                DrawSvgYAxisTitle(sb, chart, plot, axisX, "timeline-y-axis-title");
            }
        }

        sb.AppendLine("</g>");
    }

    private static void DrawTimelineItemGradients(StringBuilder sb, string id, IReadOnlyList<TimelineItem> items) {
        sb.AppendLine("<defs>");
        foreach (var item in items) {
            sb.AppendLine($"<linearGradient id=\"{id}-timelineFill{item.SeriesIndex}\" x1=\"0\" x2=\"0\" y1=\"0\" y2=\"1\"><stop offset=\"0%\" stop-color=\"{TimelineItemGradientTop(item.Color).ToHex()}\"/><stop offset=\"100%\" stop-color=\"{TimelineItemGradientBottom(item.Color).ToHex()}\"/></linearGradient>");
        }
        sb.AppendLine("</defs>");
    }

    private static List<TimelineItem> BuildTimelineItems(Chart chart) {
        var items = new List<TimelineItem>();
        for (var i = 0; i < chart.Series.Count; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.Timeline || series.Points.Count == 0) continue;
            var point = series.Points[0];
            var start = Math.Min(point.X, point.Y);
            var end = Math.Max(point.X, point.Y);
            items.Add(new TimelineItem(i, series.Name, start, end, series.Color ?? Color(chart, i), ShouldDrawDataLabels(chart, series)));
        }

        return items;
    }

    private static void DrawTimelineRangeBar(StringBuilder sb, Chart chart, TimelineItem item, string id, int row, double left, double y, double width, double rowHeight, string duration, string summary) {
        var radius = Math.Min(ChartVisualPrimitives.TimelineItemCornerRadiusMax, rowHeight / 2);
        var highlightInset = Math.Min(radius, width / 3);
        var highlightEnd = left + width - highlightInset;
        var borderStroke = ChartVisualPrimitives.TimelineItemBorderStrokeWidth;
        var borderInset = borderStroke / 2.0;
        sb.AppendLine($"<rect data-cfx-role=\"timeline-item\" data-cfx-row=\"{row}\" data-cfx-start=\"{F(item.Start)}\" data-cfx-end=\"{F(item.End)}\" data-cfx-duration=\"{Escape(duration)}\" role=\"img\" aria-label=\"{Escape(summary)}\" x=\"{F(left)}\" y=\"{F(y)}\" width=\"{F(width)}\" height=\"{F(rowHeight)}\" rx=\"{F(radius)}\" fill=\"url(#{id}-timelineFill{item.SeriesIndex})\"/>");
        sb.AppendLine($"<rect data-cfx-role=\"timeline-item-border\" x=\"{F(left + borderInset)}\" y=\"{F(y + borderInset)}\" width=\"{F(Math.Max(0, width - borderStroke))}\" height=\"{F(Math.Max(0, rowHeight - borderStroke))}\" rx=\"{F(Math.Max(0, radius - borderInset))}\" fill=\"none\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-opacity=\"{F(ChartVisualPrimitives.TimelineItemBorderOpacity)}\" stroke-width=\"{F(borderStroke)}\"/>");
        if (highlightEnd > left + highlightInset) {
            sb.AppendLine($"<line data-cfx-role=\"timeline-item-highlight\" x1=\"{F(left + highlightInset)}\" y1=\"{F(y + ChartVisualPrimitives.TimelineItemHighlightOffsetY)}\" x2=\"{F(highlightEnd)}\" y2=\"{F(y + ChartVisualPrimitives.TimelineItemHighlightOffsetY)}\" stroke=\"#fff\" stroke-opacity=\"{F(ChartVisualPrimitives.TimelineItemHighlightOpacity)}\" stroke-width=\"{F(ChartVisualPrimitives.GridStrokeWidth)}\" stroke-linecap=\"round\"/>");
        }
    }

    private static ChartRect ApplyTimelineReserve(Chart chart, ChartRect plot, IReadOnlyList<TimelineItem> items) {
        var t = chart.Options.Theme;
        var widest = items.Max(item => EstimateTextWidth(item.Name, t.TickLabelFontSize));
        var yAxisReserve = string.IsNullOrWhiteSpace(chart.YAxisTitle) ? 0 : 28;
        var desiredLeft = Math.Max(plot.Left, widest + yAxisReserve + 64);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 180);
        var shift = Math.Max(0, Math.Min(desiredLeft, maxLeft) - plot.Left);
        var bottomReserve = 52 + (string.IsNullOrWhiteSpace(chart.XAxisTitle) ? 0 : 18);
        return new ChartRect(plot.X + shift, plot.Y, Math.Max(1, plot.Width - shift), Math.Max(1, plot.Height - bottomReserve));
    }

    private static double ProjectTimelineX(double value, double min, double max, ChartRect plot) {
        return plot.Left + (value - min) / Math.Max(0.000001, max - min) * plot.Width;
    }

    private static void ApplyTimelineAxisBounds(Chart chart, ref double min, ref double max) {
        var hasMinimum = chart.Options.XAxisMinimum.HasValue;
        var hasMaximum = chart.Options.XAxisMaximum.HasValue;
        if (hasMinimum) min = chart.Options.XAxisMinimum!.Value;
        if (hasMaximum) max = chart.Options.XAxisMaximum!.Value;
        if (max <= min || Math.Abs(max - min) < 0.000001) max = min + 1;
        var span = max - min;
        if (!hasMinimum) min -= span * 0.04;
        if (!hasMaximum) max += span * 0.04;
    }

    private static string FormatTimelineTick(Chart chart, double value) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - value) < 0.000001) return label.Text;
        }

        if (chart.Options.XAxisValueFormatter != null) return chart.Options.XAxisValueFormatter(value) ?? string.Empty;

        try {
            return DateTime.FromOADate(value).ToString("MMM d", CultureInfo.InvariantCulture);
        } catch (ArgumentException) {
            return FormatNumber(value);
        }
    }

    private static string FormatTimelineDuration(Chart chart, double start, double end) {
        var duration = Math.Max(1, Math.Abs(end - start));
        if (chart.Options.ValueFormatter != null) return chart.Options.ValueFormatter(duration) ?? string.Empty;
        return ((int)Math.Round(duration)).ToString(CultureInfo.InvariantCulture) + "d";
    }

    private static string BuildTimelineSummary(Chart chart, TimelineItem item, string duration) =>
        item.Name + ": " + FormatTimelineTick(chart, item.Start) + " to " + FormatTimelineTick(chart, item.End) + ", duration " + duration;

    private static ChartColor TimelineItemGradientTop(ChartColor color) => Blend(ChartColor.White, color, ChartVisualPrimitives.TimelineItemGradientTopBlend);

    private static ChartColor TimelineItemGradientBottom(ChartColor color) => Blend(ChartColor.Black, color, ChartVisualPrimitives.TimelineItemGradientBottomBlend);

    private static ChartColor GanttTaskGradientTop(ChartColor color) => Blend(ChartColor.White, color, ChartVisualPrimitives.GanttTaskGradientTopBlend);

    private static ChartColor GanttTaskGradientBottom(ChartColor color) => Blend(ChartColor.Black, color, ChartVisualPrimitives.GanttTaskGradientBottomBlend);

    private readonly struct TimelineItem {
        public TimelineItem(int seriesIndex, string name, double start, double end, ChartColor color, bool showDataLabels) {
            SeriesIndex = seriesIndex;
            Name = name;
            Start = start;
            End = end;
            Color = color;
            ShowDataLabels = showDataLabels;
        }

        public int SeriesIndex { get; }

        public string Name { get; }

        public double Start { get; }

        public double End { get; }

        public ChartColor Color { get; }

        public bool ShowDataLabels { get; }
    }
}
