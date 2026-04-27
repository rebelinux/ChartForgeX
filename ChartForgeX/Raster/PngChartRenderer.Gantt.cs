using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawGantt(RgbaCanvas c, Chart chart, ChartRect plot) {
        var items = BuildGanttItems(chart);
        if (items.Count == 0) return;

        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var item in items) {
            min = Math.Min(min, item.Start);
            max = Math.Max(max, item.End);
        }

        if (chart.Options.GanttToday.HasValue) {
            min = Math.Min(min, chart.Options.GanttToday.Value);
            max = Math.Max(max, chart.Options.GanttToday.Value);
        }

        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var span = max - min;
        min -= span * 0.04;
        max += span * 0.04;
        var tickFontSize = PngTickFontSize(chart);
        plot = ApplyPngGanttReserve(chart, plot, items, tickFontSize);
        var rowHeight = Math.Max(18, Math.Min(30, plot.Height / items.Count * 0.52));
        var slotHeight = plot.Height / items.Count;
        var ticks = ChartTicks.Generate(min, max, Math.Min(7, Math.Max(3, chart.Options.TickCount)));
        var tickLabelWidth = Math.Max(18, plot.Width / Math.Max(1, ticks.Count - 1) - 6);
        var rowLabelWidth = Math.Max(8, plot.Left - chart.Options.Padding.Left - 2);
        var rowCenters = new double[items.Count];
        var startXs = new double[items.Count];
        var endXs = new double[items.Count];

        foreach (var tick in ticks) {
            var x = ProjectTimelineX(tick, min, max, plot);
            if (chart.Options.ShowGrid) c.DrawLine(x, plot.Top, x, plot.Bottom, ApplyOpacity(chart.Options.Theme.Grid, 0.55), 1);
            if (chart.Options.ShowAxes) {
                var label = TrimReadablePngLabelToWidth(FormatTimelineTick(chart, tick), tickFontSize, tickLabelWidth);
                var width = EstimatePngTextWidth(label, tickFontSize);
                c.DrawText(Clamp(x - width / 2.0, plot.Left + 2, plot.Right - width - 2), plot.Bottom + 22 - tickFontSize + 1, label, chart.Options.Theme.MutedText, tickFontSize);
            }
        }

        for (var i = 0; i < items.Count; i++) {
            var item = items[i];
            var centerY = plot.Top + i * slotHeight + slotHeight / 2;
            rowCenters[i] = centerY;
            startXs[i] = ProjectTimelineX(item.Start, min, max, plot);
            endXs[i] = ProjectTimelineX(item.End, min, max, plot);
            if (chart.Options.ShowGrid) c.DrawLine(plot.Left, centerY, plot.Right, centerY, ApplyOpacity(chart.Options.Theme.Grid, 0.28), 1);
            if (chart.Options.ShowAxes) {
                var rowLabel = TrimReadablePngLabelToWidth(item.Name, tickFontSize, rowLabelWidth);
                if (rowLabel.Length > 0) c.DrawTextEmphasized(plot.Left - EstimatePngEmphasizedTextWidth(rowLabel, tickFontSize) - 14, centerY - tickFontSize / 2, rowLabel, chart.Options.Theme.MutedText, tickFontSize);
            }
        }

        for (var i = 0; i < items.Count; i++) {
            if (items[i].DependsOn >= 0 && items[i].DependsOn < i) DrawGanttDependency(c, chart, plot, endXs[items[i].DependsOn], rowCenters[items[i].DependsOn], startXs[i], rowCenters[i]);
        }

        if (chart.Options.GanttToday.HasValue) DrawGanttToday(c, chart, plot, min, max);

        for (var i = 0; i < items.Count; i++) {
            var item = items[i];
            if (item.Milestone) {
                DrawGanttMilestone(c, chart, item, startXs[i], rowCenters[i], rowHeight);
                continue;
            }

            var left = Math.Min(startXs[i], endXs[i]);
            var width = Math.Max(2, Math.Abs(endXs[i] - startXs[i]));
            DrawGanttTask(c, chart, item, left, rowCenters[i] - rowHeight / 2, width, rowHeight);
        }

        if (chart.Options.ShowAxes) {
            c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, chart.Options.Theme.Axis, 1);
            DrawTimelineAxisTitles(c, chart, plot);
        }
    }

    private static bool IsGanttChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.Gantt) return true;
        return false;
    }

    private static List<GanttItem> BuildGanttItems(Chart chart) {
        var items = new List<GanttItem>();
        for (var i = 0; i < chart.Series.Count; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.Gantt || series.Points.Count < 3) continue;
            var range = series.Points[0];
            var metadata = series.Points[1];
            var flags = series.Points[2];
            items.Add(new GanttItem(series.Name, Math.Min(range.X, range.Y), Math.Max(range.X, range.Y), Clamp(metadata.X, 0, 1), (int)Math.Round(metadata.Y), flags.X >= 0.5, series.Color ?? chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length]));
        }

        return items;
    }

    private static void DrawGanttTask(RgbaCanvas c, Chart chart, GanttItem item, double left, double y, double width, double height) {
        var radius = Math.Min(7, height / 2);
        c.FillRoundedRect(left, y, width, height, radius, ChartColor.FromRgba(item.Color.R, item.Color.G, item.Color.B, 48));
        var progressWidth = Math.Max(0, width * item.Progress);
        if (progressWidth > 0.5) c.FillRoundedRectVerticalGradient(left, y, progressWidth, height, radius, Blend(ChartColor.White, item.Color, 0.88), Blend(ChartColor.Black, item.Color, 0.94));
        c.StrokeRoundedRect(left, y, width, height, radius, ApplyOpacity(chart.Options.Theme.CardBackground, 0.72));
        var inset = Math.Min(radius, width / 3);
        if (width > inset * 2 + 3) c.DrawLine(left + inset, y + 1.2, left + width - inset, y + 1.2, ChartColor.FromRgba(255, 255, 255, 48), 1);
        if (chart.Options.ShowDataLabels && width >= Math.Max(74, EstimatePngEmphasizedTextWidth("100%", chart.Options.Theme.DataLabelFontSize) + 14)) {
            DrawReadablePngLabelCentered(c, new ChartRect(left, y, width, height), FormatPercent(item.Progress), HeatmapTextColor(item.Color), item.Color, chart.Options.Theme.DataLabelFontSize);
        }
    }

    private static void DrawGanttMilestone(RgbaCanvas c, Chart chart, GanttItem item, double x, double centerY, double rowHeight) {
        var size = Math.Max(8, rowHeight * 0.44);
        var points = new[] {
            new ChartPoint(x, centerY - size),
            new ChartPoint(x + size, centerY),
            new ChartPoint(x, centerY + size),
            new ChartPoint(x - size, centerY)
        };
        c.FillPolygonVerticalGradient(points, Blend(ChartColor.White, item.Color, 0.88), Blend(ChartColor.Black, item.Color, 0.94));
        c.DrawLine(x, centerY - size, x + size, centerY, ApplyOpacity(chart.Options.Theme.CardBackground, 0.72), 1);
        c.DrawLine(x + size, centerY, x, centerY + size, ApplyOpacity(chart.Options.Theme.CardBackground, 0.72), 1);
        c.DrawLine(x, centerY + size, x - size, centerY, ApplyOpacity(chart.Options.Theme.CardBackground, 0.72), 1);
        c.DrawLine(x - size, centerY, x, centerY - size, ApplyOpacity(chart.Options.Theme.CardBackground, 0.72), 1);
    }

    private static void DrawGanttDependency(RgbaCanvas c, Chart chart, ChartRect plot, double fromX, double fromY, double toX, double toY) {
        var color = ApplyOpacity(chart.Options.Theme.Axis, 0.72);
        var midX = Clamp(fromX + Math.Max(18, (toX - fromX) / 2), plot.Left, plot.Right);
        var startX = Clamp(fromX + 6, plot.Left, plot.Right);
        var endX = Clamp(toX - 6, plot.Left, plot.Right);
        c.DrawDashedLine(startX, fromY, midX, fromY, color, 1, 5, 5);
        c.DrawDashedLine(midX, fromY, midX, toY, color, 1, 5, 5);
        c.DrawDashedLine(midX, toY, endX, toY, color, 1, 5, 5);
        c.DrawLine(endX, toY - 4, Clamp(toX, plot.Left, plot.Right), toY, color, 1);
        c.DrawLine(endX, toY + 4, Clamp(toX, plot.Left, plot.Right), toY, color, 1);
    }

    private static void DrawGanttToday(RgbaCanvas c, Chart chart, ChartRect plot, double min, double max) {
        var x = ProjectTimelineX(chart.Options.GanttToday!.Value, min, max, plot);
        if (x < plot.Left || x > plot.Right) return;
        c.DrawDashedLine(x, plot.Top, x, plot.Bottom, chart.Options.Theme.Warning, 1, 6, 5);
        var label = "Today";
        var fontSize = chart.Options.Theme.TickLabelFontSize;
        var width = EstimatePngEmphasizedTextWidth(label, fontSize);
        DrawReadablePngLabel(c, Clamp(x - width / 2, plot.Left + 2, plot.Right - width - 2), plot.Top - fontSize - 5, label, chart.Options.Theme.Warning, ReadableLabelHalo(chart), fontSize);
    }

    private static ChartRect ApplyPngGanttReserve(Chart chart, ChartRect plot, IReadOnlyList<GanttItem> items, double tickFontSize) {
        var widest = 0.0;
        foreach (var item in items) widest = Math.Max(widest, EstimatePngEmphasizedTextWidth(item.Name, tickFontSize));
        var yAxisReserve = string.IsNullOrWhiteSpace(chart.YAxisTitle) ? 0 : 28;
        var desiredLeft = Math.Max(plot.Left, widest + yAxisReserve + 64);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 220);
        var shift = Math.Max(0, Math.Min(desiredLeft, maxLeft) - plot.Left);
        var topShift = chart.Options.GanttToday.HasValue ? 12 : 0;
        var bottomReserve = 52 + (string.IsNullOrWhiteSpace(chart.XAxisTitle) ? 0 : 18);
        return new ChartRect(plot.X + shift, plot.Y + topShift, Math.Max(1, plot.Width - shift), Math.Max(1, plot.Height - bottomReserve - topShift));
    }

    private readonly struct GanttItem {
        public GanttItem(string name, double start, double end, double progress, int dependsOn, bool milestone, ChartColor color) {
            Name = name;
            Start = start;
            End = end;
            Progress = progress;
            DependsOn = dependsOn;
            Milestone = milestone;
            Color = color;
        }

        public string Name { get; }
        public double Start { get; }
        public double End { get; }
        public double Progress { get; }
        public int DependsOn { get; }
        public bool Milestone { get; }
        public ChartColor Color { get; }
    }
}
