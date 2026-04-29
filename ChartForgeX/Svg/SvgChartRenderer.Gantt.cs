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
    private static void DrawGantt(StringBuilder sb, Chart chart, ChartRect basePlot, string id) {
        var items = BuildGanttItems(chart);
        if (items.Count == 0) return;

        var t = chart.Options.Theme;
        var min = items.Min(item => item.Start);
        var max = items.Max(item => item.End);
        if (chart.Options.GanttToday.HasValue) {
            min = Math.Min(min, chart.Options.GanttToday.Value);
            max = Math.Max(max, chart.Options.GanttToday.Value);
        }

        ApplyTimelineAxisBounds(chart, ref min, ref max);
        var plot = ApplyGanttReserve(chart, basePlot, items);
        var rowHeight = Math.Max(18, Math.Min(30, plot.Height / items.Count * 0.52));
        var slotHeight = plot.Height / items.Count;
        var ticks = ChartTicks.Generate(min, max, Math.Min(7, Math.Max(3, chart.Options.TickCount)));
        var tickLabelWidth = Math.Max(18, plot.Width / Math.Max(1, ticks.Count - 1) - 6);
        var rowLabelWidth = Math.Max(8, plot.Left - 24);
        var rowCenters = new double[items.Count];
        var startXs = new double[items.Count];
        var endXs = new double[items.Count];

        sb.AppendLine("<g data-cfx-role=\"gantt-chart\">");
        DrawGanttItemGradients(sb, id, items);
        foreach (var tick in ticks) {
            var x = ProjectTimelineX(tick, min, max, plot);
            if (chart.Options.ShowGrid) sb.AppendLine($"<line x1=\"{F(x)}\" y1=\"{F(plot.Top)}\" x2=\"{F(x)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.GridStrokeWidth)}\" opacity=\"{F(ChartVisualPrimitives.TimelineGridOpacity)}\"/>");
            if (chart.Options.ShowAxes) {
                var rawLabel = FormatTimelineTick(chart, tick);
                var labelFontSize = TextFontSizeForSvgWidth(rawLabel, tickLabelWidth, t.TickLabelFontSize);
                var label = TrimSvgLabelToWidth(rawLabel, labelFontSize, tickLabelWidth);
                var anchor = EdgeAwareAnchor(label, x, plot, labelFontSize);
                var labelX = EdgeAwareTextX(label, x, plot, labelFontSize);
                sb.AppendLine($"<text data-cfx-role=\"gantt-tick-label\" x=\"{F(labelX)}\" y=\"{F(plot.Bottom + 22)}\" text-anchor=\"{anchor}\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(labelFontSize)}\">{Escape(label)}</text>");
            }
        }

        for (var i = 0; i < items.Count; i++) {
            var item = items[i];
            var centerY = plot.Top + i * slotHeight + slotHeight / 2;
            rowCenters[i] = centerY;
            startXs[i] = ProjectTimelineX(item.Start, min, max, plot);
            endXs[i] = ProjectTimelineX(item.End, min, max, plot);
            if (chart.Options.ShowGrid) sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(centerY)}\" x2=\"{F(plot.Right)}\" y2=\"{F(centerY)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.GridStrokeWidth)}\" opacity=\"{F(ChartVisualPrimitives.TimelineRowGridOpacity)}\"/>");
            if (chart.Options.ShowAxes) {
                var rowLabelFontSize = TextFontSizeForSvgWidth(item.Name, rowLabelWidth, t.TickLabelFontSize);
                var rowLabel = TrimSvgLabelToWidth(item.Name, rowLabelFontSize, rowLabelWidth);
                sb.AppendLine($"<text data-cfx-role=\"gantt-row-label\" x=\"{F(plot.Left - 14)}\" y=\"{F(centerY)}\" text-anchor=\"end\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(rowLabelFontSize)}\" font-weight=\"650\">{Escape(rowLabel)}</text>");
            }
        }

        for (var i = 0; i < items.Count; i++) {
            if (items[i].DependsOn >= 0 && items[i].DependsOn < i) DrawGanttDependency(sb, plot, endXs[items[i].DependsOn], rowCenters[items[i].DependsOn], startXs[i], rowCenters[i], t.Axis);
        }

        if (chart.Options.GanttToday.HasValue) DrawGanttToday(sb, chart, plot, min, max);

        for (var i = 0; i < items.Count; i++) {
            var item = items[i];
            if (item.Milestone) {
                DrawGanttMilestone(sb, chart, item, id, startXs[i], rowCenters[i], rowHeight);
                continue;
            }

            var left = Math.Min(startXs[i], endXs[i]);
            var width = Math.Max(2, Math.Abs(endXs[i] - startXs[i]));
            DrawGanttTask(sb, chart, item, id, i, left, rowCenters[i] - rowHeight / 2, width, rowHeight);
        }

        if (chart.Options.ShowAxes) {
            sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(plot.Bottom)}\" x2=\"{F(plot.Right)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.AxisStrokeWidth)}\"/>");
            DrawSvgXAxisTitle(sb, chart, plot, plot.Bottom + 49, "gantt-x-axis-title");
            if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) {
                var widestLabel = items.Max(item => EstimateTextWidth(item.Name, t.TickLabelFontSize));
                DrawSvgYAxisTitle(sb, chart, plot, Math.Max(24, plot.Left - widestLabel - 46), "gantt-y-axis-title");
            }
        }

        sb.AppendLine("</g>");
    }

    private static void DrawGanttItemGradients(StringBuilder sb, string id, IReadOnlyList<GanttItem> items) {
        sb.AppendLine("<defs>");
        foreach (var item in items) {
            sb.AppendLine($"<linearGradient id=\"{id}-ganttFill{item.SeriesIndex}\" x1=\"0\" x2=\"0\" y1=\"0\" y2=\"1\"><stop offset=\"0%\" stop-color=\"{GanttTaskGradientTop(item.Color).ToHex()}\"/><stop offset=\"100%\" stop-color=\"{GanttTaskGradientBottom(item.Color).ToHex()}\"/></linearGradient>");
        }
        sb.AppendLine("</defs>");
    }

    private static List<GanttItem> BuildGanttItems(Chart chart) {
        var items = new List<GanttItem>();
        for (var i = 0; i < chart.Series.Count; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.Gantt || series.Points.Count < 3) continue;
            var range = series.Points[0];
            var metadata = series.Points[1];
            var flags = series.Points[2];
            items.Add(new GanttItem(i, series.Name, Math.Min(range.X, range.Y), Math.Max(range.X, range.Y), Clamp(metadata.X, 0, 1), (int)Math.Round(metadata.Y), flags.X >= 0.5, series.Color ?? Color(chart, i), ShouldDrawDataLabels(chart, series)));
        }

        return items;
    }

    private static void DrawGanttTask(StringBuilder sb, Chart chart, GanttItem item, string id, int row, double left, double y, double width, double height) {
        var t = chart.Options.Theme;
        var radius = Math.Min(ChartVisualPrimitives.GanttTaskCornerRadiusMax, height / 2);
        var progressWidth = Math.Max(0, width * item.Progress);
        var summary = item.Name + ": " + FormatTimelineTick(chart, item.Start) + " to " + FormatTimelineTick(chart, item.End) + ", " + FormatPercent(item.Progress) + " complete";
        var borderStroke = ChartVisualPrimitives.GanttTaskBorderStrokeWidth;
        var borderInset = borderStroke / 2.0;
        sb.AppendLine($"<rect data-cfx-role=\"gantt-task\" data-cfx-row=\"{row}\" data-cfx-start=\"{F(item.Start)}\" data-cfx-end=\"{F(item.End)}\" data-cfx-progress=\"{F(item.Progress)}\" role=\"img\" aria-label=\"{Escape(summary)}\" x=\"{F(left)}\" y=\"{F(y)}\" width=\"{F(width)}\" height=\"{F(height)}\" rx=\"{F(radius)}\" fill=\"{item.Color.ToCss()}\" opacity=\"{F(ChartVisualPrimitives.GanttTaskTrackOpacity)}\"/>");
        if (progressWidth > 0.5) sb.AppendLine($"<rect data-cfx-role=\"gantt-progress\" x=\"{F(left)}\" y=\"{F(y)}\" width=\"{F(progressWidth)}\" height=\"{F(height)}\" rx=\"{F(radius)}\" fill=\"url(#{id}-ganttFill{item.SeriesIndex})\"/>");
        sb.AppendLine($"<rect data-cfx-role=\"gantt-task-border\" x=\"{F(left + borderInset)}\" y=\"{F(y + borderInset)}\" width=\"{F(Math.Max(0, width - borderStroke))}\" height=\"{F(Math.Max(0, height - borderStroke))}\" rx=\"{F(Math.Max(0, radius - borderInset))}\" fill=\"none\" stroke=\"{t.CardBackground.ToCss()}\" stroke-opacity=\"{F(ChartVisualPrimitives.GanttTaskBorderOpacity)}\" stroke-width=\"{F(borderStroke)}\"/>");
        var inset = Math.Min(radius, width / 3);
        if (width > inset * 2 + 3) sb.AppendLine($"<line data-cfx-role=\"gantt-task-highlight\" x1=\"{F(left + inset)}\" y1=\"{F(y + ChartVisualPrimitives.GanttTaskHighlightOffsetY)}\" x2=\"{F(left + width - inset)}\" y2=\"{F(y + ChartVisualPrimitives.GanttTaskHighlightOffsetY)}\" stroke=\"#fff\" stroke-opacity=\"{F(ChartVisualPrimitives.GanttTaskHighlightOpacity)}\" stroke-width=\"{F(ChartVisualPrimitives.GridStrokeWidth)}\" stroke-linecap=\"round\"/>");
        if (item.ShowDataLabels && width >= 74) DrawSvgTextCenteredX(sb, chart, "gantt-progress-label", FormatPercent(item.Progress), left + width / 2, y + height / 2, HeatmapTextColor(item.Color), t.DataLabelFontSize, width - 8, "750", t.CardBackground, 2.2);
    }

    private static void DrawGanttMilestone(StringBuilder sb, Chart chart, GanttItem item, string id, double x, double centerY, double rowHeight) {
        var size = Math.Max(8, rowHeight * 0.44);
        var summary = item.Name + ": milestone at " + FormatTimelineTick(chart, item.Start);
        var points = F(x) + "," + F(centerY - size) + " " + F(x + size) + "," + F(centerY) + " " + F(x) + "," + F(centerY + size) + " " + F(x - size) + "," + F(centerY);
        sb.AppendLine($"<polygon data-cfx-role=\"gantt-milestone\" data-cfx-start=\"{F(item.Start)}\" role=\"img\" aria-label=\"{Escape(summary)}\" points=\"{points}\" fill=\"url(#{id}-ganttFill{item.SeriesIndex})\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-opacity=\"{F(ChartVisualPrimitives.GanttTaskBorderOpacity)}\" stroke-width=\"{F(ChartVisualPrimitives.GanttTaskBorderStrokeWidth)}\"/>");
    }

    private static void DrawGanttDependency(StringBuilder sb, ChartRect plot, double fromX, double fromY, double toX, double toY, ChartColor color) {
        var midX = Clamp(fromX + Math.Max(ChartVisualPrimitives.GanttDependencyMinMidOffset, (toX - fromX) / 2), plot.Left, plot.Right);
        var endX = Clamp(toX - ChartVisualPrimitives.GanttDependencyEndpointInset, plot.Left, plot.Right);
        var targetX = Clamp(toX, plot.Left, plot.Right);
        var path = "M " + F(Clamp(fromX + ChartVisualPrimitives.GanttDependencyEndpointInset, plot.Left, plot.Right)) + " " + F(fromY) + " H " + F(midX) + " V " + F(toY) + " H " + F(endX);
        sb.AppendLine($"<path data-cfx-role=\"gantt-dependency\" d=\"{path}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.GanttDependencyStrokeWidth)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-dasharray=\"{F(ChartVisualPrimitives.GanttDependencyDash)} {F(ChartVisualPrimitives.GanttDependencyGap)}\" opacity=\"{F(ChartVisualPrimitives.GanttDependencyOpacity)}\"/>");
        sb.AppendLine($"<path data-cfx-role=\"gantt-dependency-arrow\" d=\"M {F(endX)} {F(toY - ChartVisualPrimitives.GanttDependencyArrowSize)} L {F(targetX)} {F(toY)} L {F(endX)} {F(toY + ChartVisualPrimitives.GanttDependencyArrowSize)}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.GanttDependencyStrokeWidth)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" opacity=\"{F(ChartVisualPrimitives.GanttDependencyOpacity)}\"/>");
    }

    private static void DrawGanttToday(StringBuilder sb, Chart chart, ChartRect plot, double min, double max) {
        var t = chart.Options.Theme;
        var x = ProjectTimelineX(chart.Options.GanttToday!.Value, min, max, plot);
        if (x < plot.Left || x > plot.Right) return;
        sb.AppendLine($"<line data-cfx-role=\"gantt-today\" x1=\"{F(x)}\" y1=\"{F(plot.Top)}\" x2=\"{F(x)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Warning.ToCss()}\" stroke-width=\"{F(ChartVisualPrimitives.GanttTodayStrokeWidth)}\" stroke-dasharray=\"6 5\"/>");
        DrawSvgTextCenteredX(sb, chart, "gantt-today-label", "Today", x, plot.Top - 8, t.Warning, t.TickLabelFontSize, 62, "750", t.CardBackground, 2.2, middleBaseline: false);
    }

    private static ChartRect ApplyGanttReserve(Chart chart, ChartRect plot, IReadOnlyList<GanttItem> items) {
        var t = chart.Options.Theme;
        var widest = items.Max(item => EstimateTextWidth(item.Name, t.TickLabelFontSize));
        var yAxisReserve = string.IsNullOrWhiteSpace(chart.YAxisTitle) ? 0 : 28;
        var desiredLeft = Math.Max(plot.Left, widest + yAxisReserve + 64);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 220);
        var shift = Math.Max(0, Math.Min(desiredLeft, maxLeft) - plot.Left);
        var topShift = chart.Options.GanttToday.HasValue ? 12 : 0;
        var bottomReserve = 52 + (string.IsNullOrWhiteSpace(chart.XAxisTitle) ? 0 : 18);
        return new ChartRect(plot.X + shift, plot.Y + topShift, Math.Max(1, plot.Width - shift), Math.Max(1, plot.Height - bottomReserve - topShift));
    }

    private readonly struct GanttItem {
        public GanttItem(int seriesIndex, string name, double start, double end, double progress, int dependsOn, bool milestone, ChartColor color, bool showDataLabels) {
            SeriesIndex = seriesIndex;
            Name = name;
            Start = start;
            End = end;
            Progress = progress;
            DependsOn = dependsOn;
            Milestone = milestone;
            Color = color;
            ShowDataLabels = showDataLabels;
        }

        public int SeriesIndex { get; }
        public string Name { get; }
        public double Start { get; }
        public double End { get; }
        public double Progress { get; }
        public int DependsOn { get; }
        public bool Milestone { get; }
        public ChartColor Color { get; }
        public bool ShowDataLabels { get; }
    }
}
