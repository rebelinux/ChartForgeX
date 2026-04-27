using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawSeries(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var s = chart.Series[index]; var color = s.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        if (s.Kind == ChartSeriesKind.HorizontalBar) {
            var layout = HorizontalBarLayout(chart, plot, index);
            var zeroX = Math.Min(plot.Right, Math.Max(plot.Left, map.X(0)));
            foreach (var p in s.Points) {
                var baseValue = chart.Options.BarMode == ChartBarMode.Stacked ? StackHorizontalBaseValue(chart, index, p) : 0;
                var baseX = chart.Options.BarMode == ChartBarMode.Stacked ? map.X(baseValue) : zeroX;
                var valueX = map.X(baseValue + p.Y);
                var left = Math.Min(baseX, valueX);
                var width = Math.Abs(valueX - baseX);
                var y = map.Y(p.X) + layout.Offset - layout.BarHeight / 2;
                var radius = chart.Options.BarMode == ChartBarMode.Stacked ? Math.Min(3, layout.BarHeight / 2) : Math.Min(7, layout.BarHeight / 2);
                DrawGradientBar(c, left, y, width, layout.BarHeight, radius, color);
                if (ShouldDrawDataLabels(chart, s)) {
                    var label = FormatValue(chart, p.Y);
                    var labelFontSize = HorizontalValueLabelFontSize(chart);
                    if (chart.Options.BarMode == ChartBarMode.Stacked) {
                        var labelWidth = EstimatePngEmphasizedTextWidth(label, labelFontSize);
                        if (width < labelWidth + 8) continue;
                        DrawReadablePngLabel(c, plot, left + width / 2.0 - labelWidth / 2.0, y + layout.BarHeight / 2 - labelFontSize / 2.0, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), labelFontSize);
                    } else {
                        var labelWidth = EstimatePngEmphasizedTextWidth(label, labelFontSize);
                        var labelX = p.Y >= 0 ? Math.Min(plot.Right - labelWidth - 2, left + width + 8) : Math.Max(plot.Left + 2, left - labelWidth - 8);
                        DrawReadablePngLabel(c, plot, labelX, y + layout.BarHeight / 2 - labelFontSize / 2.0, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), labelFontSize);
                    }
                }
            }

            return;
        }

        if (s.Kind == ChartSeriesKind.Bar) {
            var layout = BarLayout(chart, plot, index);
            var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
            foreach (var p in s.Points) {
                var baseValue = chart.Options.BarMode == ChartBarMode.Stacked ? StackBaseValue(chart, index, p) : 0;
                var y = map.Y(baseValue + p.Y);
                var baseY = chart.Options.BarMode == ChartBarMode.Stacked ? map.Y(baseValue) : zeroY;
                var barX = map.X(p.X) + layout.Offset - layout.BarWidth / 2;
                var barY = Math.Min(y, baseY);
                var barHeight = Math.Abs(baseY - y);
                var radius = chart.Options.BarMode == ChartBarMode.Stacked ? Math.Min(3, layout.BarWidth / 2) : Math.Min(7, layout.BarWidth / 2);
                DrawGradientBar(c, barX, barY, layout.BarWidth, barHeight, radius, color);
                if (ShouldDrawDataLabels(chart, s)) {
                    var label = FormatValue(chart, p.Y);
                    var segmentHeight = barHeight;
                    var fontSize = chart.Options.Theme.DataLabelFontSize;
                    if (chart.Options.BarMode == ChartBarMode.Stacked && segmentHeight < fontSize + 8) continue;
                    var labelY = chart.Options.BarMode == ChartBarMode.Stacked ? barY + segmentHeight / 2 - fontSize / 2.0 : p.Y >= 0 ? barY - 10 - fontSize : barY + segmentHeight + 10 - fontSize;
                    DrawReadablePngLabel(c, plot, map.X(p.X) + layout.Offset - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
                }
            }
            return;
        }
        if (s.Kind == ChartSeriesKind.Lollipop) {
            var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
            var markerRadius = Math.Max(4, chart.Options.Theme.MarkerRadius + 2.25);
            foreach (var p in s.Points) {
                var x = map.X(p.X);
                var y = map.Y(p.Y);
                c.DrawLine(x, zeroY, x, y, color, Math.Max(1, (int)Math.Round(s.StrokeWidth * 0.62)));
                DrawMarker(c, chart, x, y, markerRadius, color);
                if (ShouldDrawDataLabels(chart, s)) {
                    var label = FormatValue(chart, p.Y);
                    var fontSize = chart.Options.Theme.DataLabelFontSize;
                    var labelX = x - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0;
                    var aboveY = y - fontSize - markerRadius - 4;
                    var belowY = y + markerRadius + 4;
                    var labelY = aboveY < plot.Top + 2 ? belowY : aboveY;
                    DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
                }
            }

            return;
        }
        if (s.Kind == ChartSeriesKind.RangeBar) {
            var intervalCount = Math.Max(1, s.Points.Count / 2);
            var barWidth = Math.Max(8, Math.Min(28, plot.Width / Math.Max(1, intervalCount * 4.0)));
            for (var pointIndex = 0; pointIndex + 1 < s.Points.Count; pointIndex += 2) {
                var start = s.Points[pointIndex];
                var end = s.Points[pointIndex + 1];
                var x = map.X(start.X);
                var y1 = map.Y(start.Y);
                var y2 = map.Y(end.Y);
                var top = Math.Min(y1, y2);
                var height = Math.Max(2, Math.Abs(y2 - y1));
                DrawGradientBar(c, x - barWidth / 2.0, top, barWidth, height, Math.Min(7, barWidth / 2), color);
                c.DrawLine(x - barWidth * 0.75, y1, x + barWidth * 0.75, y1, color, 2);
                c.DrawLine(x - barWidth * 0.75, y2, x + barWidth * 0.75, y2, color, 2);
                if (ShouldDrawDataLabels(chart, s)) {
                    var label = FormatValue(chart, Math.Min(start.Y, end.Y)) + "-" + FormatValue(chart, Math.Max(start.Y, end.Y));
                    var fontSize = chart.Options.Theme.DataLabelFontSize;
                    DrawReadablePngLabel(c, plot, x - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0, top - fontSize - 4, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
                }
            }

            return;
        }
        if (s.Kind == ChartSeriesKind.BoxPlot) {
            DrawBoxPlots(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.Bubble) {
            DrawBubbles(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.ErrorBar) {
            DrawErrorBars(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.Candlestick) {
            DrawCandlesticks(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.Ohlc) {
            DrawOhlc(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.RangeBand) {
            DrawRangeBand(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.RangeArea) {
            DrawRangeArea(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.Dumbbell) {
            DrawDumbbells(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.Slope) {
            DrawSlope(c, chart, index, plot, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.TrendLine) {
            DrawTrendLine(c, chart, index, map);
            return;
        }
        if (s.Kind == ChartSeriesKind.StackedArea) {
            DrawStackedArea(c, chart, index, plot, map);
            return;
        }
        if ((s.Kind == ChartSeriesKind.Area || s.Kind == ChartSeriesKind.StepArea) && s.Points.Count > 0) {
            var zeroY = Math.Min(plot.Bottom, Math.Max(plot.Top, map.Y(0)));
            var pathPoints = MapSeriesPathPoints(s, map);
            var polygon = new List<ChartPoint>(pathPoints.Count + 2) {
                new(pathPoints[0].X, zeroY)
            };
            foreach (var point in pathPoints) polygon.Add(point);
            polygon.Add(new ChartPoint(pathPoints[pathPoints.Count - 1].X, zeroY));
            c.FillPolygonVerticalGradient(polygon, ChartColor.FromRgba(color.R, color.G, color.B, 72), ChartColor.FromRgba(color.R, color.G, color.B, 8));
        }
        var linePoints = MapSeriesPathPoints(s, map);
        if (s.Kind != ChartSeriesKind.Scatter) {
            DrawPngLinePath(c, linePoints, PngStrokeHalo(color), s.StrokeWidth + 4);
            DrawPngLinePath(c, linePoints, color, s.StrokeWidth);
        }
        if (s.Kind == ChartSeriesKind.Scatter || (!chart.Options.IsSparkline && (s.Kind == ChartSeriesKind.Line || s.Kind == ChartSeriesKind.StepLine))) foreach (var p in s.Points) DrawMarker(c, chart, map.X(p.X), map.Y(p.Y), 4, color);
        if (ShouldDrawDataLabels(chart, s)) {
            var reserved = new List<ChartLabelBounds>();
            foreach (var p in s.Points) {
                var label = FormatValue(chart, p.Y);
                var fontSize = chart.Options.Theme.DataLabelFontSize;
                var labelX = map.X(p.X) - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0;
                var aboveY = map.Y(p.Y) - fontSize - 4;
                var belowY = map.Y(p.Y) + 8;
                var labelY = aboveY < plot.Top + 2 ? belowY : aboveY;
                if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reserved)) continue;
                DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
            }
        }
    }

    private static void DrawTrendLine(RgbaCanvas c, Chart chart, int index, ChartMapper map) {
        var series = chart.Series[index];
        if (series.Points.Count < 2) return;
        var color = series.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        var start = series.Points[0];
        var end = series.Points[series.Points.Count - 1];
        c.DrawDashedLine(map.X(start.X), map.Y(start.Y), map.X(end.X), map.Y(end.Y), PngStrokeHalo(color), Math.Max(3, (int)Math.Round(series.StrokeWidth + 4)), 8, 6);
        c.DrawDashedLine(map.X(start.X), map.Y(start.Y), map.X(end.X), map.Y(end.Y), color, Math.Max(1, (int)Math.Round(series.StrokeWidth)), 8, 6);
    }

    private static void DrawSlope(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        if (series.Points.Count < 2) return;
        var color = series.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        var start = series.Points[0];
        var end = series.Points[1];
        var xStart = map.X(start.X);
        var yStart = map.Y(start.Y);
        var xEnd = map.X(end.X);
        var yEnd = map.Y(end.Y);
        var radius = Math.Max(4.2, chart.Options.Theme.MarkerRadius + 1.2);

        c.DrawLine(xStart, yStart, xEnd, yEnd, ChartColor.FromRgba(color.R, color.G, color.B, 96), Math.Max(3, (int)Math.Round(series.StrokeWidth + 2)));
        c.DrawLine(xStart, yStart, xEnd, yEnd, color, Math.Max(1, (int)Math.Round(series.StrokeWidth)));
        DrawMarker(c, chart, xStart, yStart, radius, color);
        DrawMarker(c, chart, xEnd, yEnd, radius, color);
        if (!ShouldDrawDataLabels(chart, series)) return;

        var fontSize = chart.Options.Theme.DataLabelFontSize;
        var halo = ReadableLabelHalo(chart);
        var startLabel = FormatValue(chart, start.Y);
        var startX = Math.Max(plot.Left + 2, xStart - EstimatePngEmphasizedTextWidth(startLabel, fontSize) - radius - 8);
        DrawReadablePngLabel(c, plot, startX, yStart - fontSize / 2.0, startLabel, chart.Options.Theme.Text, halo, fontSize);
        var endLabel = FormatValue(chart, end.Y);
        var endX = Math.Min(plot.Right - EstimatePngEmphasizedTextWidth(endLabel, fontSize) - 2, xEnd + radius + 8);
        DrawReadablePngLabel(c, plot, endX, yEnd - fontSize / 2.0, endLabel, chart.Options.Theme.Text, halo, fontSize);
    }

    private static void DrawStackedArea(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var series = chart.Series[index];
        if (series.Points.Count == 0) return;
        var color = series.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        var upper = new List<ChartPoint>(series.Points.Count);
        var lower = new List<ChartPoint>(series.Points.Count);
        foreach (var point in series.Points) {
            var baseValue = StackAreaBaseValue(chart, index, point);
            upper.Add(new ChartPoint(map.X(point.X), map.Y(baseValue + point.Y)));
            lower.Add(new ChartPoint(map.X(point.X), map.Y(baseValue)));
        }

        var upperPath = ChartPathBuilder.FromPoints(upper, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var lowerPath = ChartPathBuilder.FromPoints(lower, ChartSeriesKind.Line, series.Smooth).Flatten(12);
        var polygon = new List<ChartPoint>(upperPath.Count + lowerPath.Count);
        foreach (var point in upperPath) polygon.Add(point);
        for (var i = lowerPath.Count - 1; i >= 0; i--) polygon.Add(lowerPath[i]);
        c.FillPolygonVerticalGradient(polygon, ChartColor.FromRgba(color.R, color.G, color.B, 120), ChartColor.FromRgba(color.R, color.G, color.B, 34));
        DrawPngLinePath(c, upperPath, PngStrokeHalo(color), series.StrokeWidth + 4);
        for (var i = 1; i < upperPath.Count; i++) {
            var a = upperPath[i - 1];
            var b = upperPath[i];
            c.DrawLine(a.X, a.Y, b.X, b.Y, color, Math.Max(1, (int)Math.Round(series.StrokeWidth)));
        }

        if (!ShouldDrawDataLabels(chart, series)) return;
        var reserved = new List<ChartLabelBounds>();
        foreach (var point in series.Points) {
            var baseValue = StackAreaBaseValue(chart, index, point);
            var label = FormatValue(chart, point.Y);
            var fontSize = chart.Options.Theme.DataLabelFontSize;
            var labelX = map.X(point.X) - EstimatePngEmphasizedTextWidth(label, fontSize) / 2.0;
            var labelY = map.Y(baseValue + point.Y) - fontSize - 5;
            if (!ReservePngLabel(label, labelX, labelY, chart, plot, fontSize, reserved)) continue;
            DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
        }
    }

    private static bool ReservePngLabel(string label, double x, double y, Chart chart, ChartRect plot, double fontSize, List<ChartLabelBounds> reserved) {
        var fittedFontSize = TextFontSizeForEmphasizedWidth(label, Math.Max(8, plot.Width - 4), fontSize);
        var fittedLabel = TrimReadablePngLabelToWidth(label, fittedFontSize, Math.Max(8, plot.Width - 4));
        if (fittedLabel.Length == 0) return false;
        var width = EstimatePngEmphasizedTextWidth(fittedLabel, fittedFontSize) + 8;
        var height = fittedFontSize + 6;
        var left = Clamp(x, plot.Left + 2, plot.Right - width - 2);
        var top = Clamp(y, plot.Top + 2, plot.Bottom - height - 2);
        var bounds = new ChartLabelBounds(left, top, width, height);
        foreach (var item in reserved) if (bounds.Intersects(item)) return false;
        reserved.Add(bounds);
        return true;
    }

    private static void DrawGradientBar(RgbaCanvas c, double x, double y, double width, double height, double radius, ChartColor color) {
        if (width <= 0.5 || height <= 0.5) return;
        var top = Blend(ChartColor.White, color, 0.88);
        var bottom = Blend(ChartColor.Black, color, 0.94);
        c.FillRoundedRectVerticalGradient(x, y, width, height, radius, top, bottom);
        c.DrawLine(x + 1, y + 1, x + width - 1, y + 1, ChartColor.FromRgba(255, 255, 255, 38), 1);
    }

    private static void DrawMarker(RgbaCanvas c, Chart chart, double x, double y, double radius, ChartColor color) {
        c.DrawCircle(x, y, radius + 1.8, chart.Options.Theme.CardBackground);
        c.DrawCircle(x, y, radius, color);
    }

    private static void DrawPngLinePath(RgbaCanvas c, IReadOnlyList<ChartPoint> points, ChartColor color, double strokeWidth) {
        var thickness = Math.Max(1, (int)Math.Round(strokeWidth));
        for (var i = 1; i < points.Count; i++) {
            var a = points[i - 1];
            var b = points[i];
            c.DrawLine(a.X, a.Y, b.X, b.Y, color, thickness);
        }
    }

    private static ChartColor PngStrokeHalo(ChartColor color) {
        if (color.A == 0) return color;
        var alpha = Math.Min(color.A, Math.Max(28, Math.Min(92, color.A / 3)));
        return ChartColor.FromRgba(color.R, color.G, color.B, (byte)alpha);
    }

    private static BarLayoutInfo BarLayout(Chart chart, ChartRect plot, int seriesIndex) {
        var barSeries = new List<int>();
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i].Kind == ChartSeriesKind.Bar) barSeries.Add(i);
        }

        var groupCount = chart.Options.BarMode == ChartBarMode.Stacked ? 1 : Math.Max(1, barSeries.Count);
        var groupPosition = chart.Options.BarMode == ChartBarMode.Stacked ? 0 : Math.Max(0, barSeries.IndexOf(seriesIndex));
        var xValues = new HashSet<double>();
        foreach (var index in barSeries) {
            foreach (var point in chart.Series[index].Points) xValues.Add(point.X);
        }

        var categoryCount = Math.Max(1, xValues.Count);
        var slotWidth = plot.Width / categoryCount;
        var groupWidth = slotWidth * (groupCount == 1 ? 0.58 : 0.74);
        var gap = groupCount == 1 ? 0 : Math.Min(4, groupWidth * 0.08);
        var barWidth = Math.Max(3, (groupWidth - gap * (groupCount - 1)) / groupCount);
        var offset = (groupPosition - (groupCount - 1) / 2.0) * (barWidth + gap);
        return new BarLayoutInfo(barWidth, offset);
    }

    private static HorizontalBarLayoutInfo HorizontalBarLayout(Chart chart, ChartRect plot, int seriesIndex) {
        var horizontalSeries = new List<int>();
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i].Kind == ChartSeriesKind.HorizontalBar) horizontalSeries.Add(i);
        }

        var groupCount = chart.Options.BarMode == ChartBarMode.Stacked ? 1 : Math.Max(1, horizontalSeries.Count);
        var groupPosition = chart.Options.BarMode == ChartBarMode.Stacked ? 0 : Math.Max(0, horizontalSeries.IndexOf(seriesIndex));
        var yValues = new HashSet<double>();
        foreach (var index in horizontalSeries) {
            foreach (var point in chart.Series[index].Points) yValues.Add(point.X);
        }

        var categoryCount = Math.Max(1, yValues.Count);
        var slotHeight = plot.Height / categoryCount;
        var groupHeight = slotHeight * (groupCount == 1 ? 0.56 : 0.76);
        var gap = groupCount == 1 ? 0 : Math.Min(4, groupHeight * 0.08);
        var barHeight = Math.Max(3, Math.Min(30, (groupHeight - gap * (groupCount - 1)) / groupCount));
        var offset = (groupPosition - (groupCount - 1) / 2.0) * (barHeight + gap);
        return new HorizontalBarLayoutInfo(barHeight, offset);
    }

    private static double StackBaseValue(Chart chart, int seriesIndex, ChartPoint point) {
        var sum = 0.0;
        for (var i = 0; i < seriesIndex; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.Bar) continue;
            foreach (var candidate in series.Points) {
                if (Math.Abs(candidate.X - point.X) >= 0.000001) continue;
                if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
                break;
            }
        }

        return sum;
    }

    private static double StackHorizontalBaseValue(Chart chart, int seriesIndex, ChartPoint point) {
        var sum = 0.0;
        for (var i = 0; i < seriesIndex; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var candidate in series.Points) {
                if (Math.Abs(candidate.X - point.X) >= 0.000001) continue;
                if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
                break;
            }
        }

        return sum;
    }

    private static double StackAreaBaseValue(Chart chart, int seriesIndex, ChartPoint point) {
        var sum = 0.0;
        for (var i = 0; i < seriesIndex; i++) {
            var series = chart.Series[i];
            if (series.Kind != ChartSeriesKind.StackedArea) continue;
            foreach (var candidate in series.Points) {
                if (Math.Abs(candidate.X - point.X) >= 0.000001) continue;
                if ((point.Y >= 0 && candidate.Y >= 0) || (point.Y < 0 && candidate.Y < 0)) sum += candidate.Y;
                break;
            }
        }

        return sum;
    }

    private static void DrawStackTotals(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map) {
        var positiveTotals = new Dictionary<double, double>();
        var negativeTotals = new Dictionary<double, double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.Bar) continue;
            foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
        }

        DrawStackTotalSet(c, chart, plot, map, positiveTotals, -12);
        DrawStackTotalSet(c, chart, plot, map, negativeTotals, 8);
    }

    private static void DrawHorizontalStackTotals(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map) {
        var positiveTotals = new Dictionary<double, double>();
        var negativeTotals = new Dictionary<double, double>();
        foreach (var series in chart.Series) {
            if (series.Kind != ChartSeriesKind.HorizontalBar) continue;
            foreach (var point in series.Points) AddStackTotal(point.Y >= 0 ? positiveTotals : negativeTotals, point.X, point.Y);
        }

        DrawHorizontalStackTotalSet(c, chart, plot, map, positiveTotals, 8, true);
        DrawHorizontalStackTotalSet(c, chart, plot, map, negativeTotals, -8, false);
    }

    private static void DrawHorizontalStackTotalSet(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map, Dictionary<double, double> totals, double offset, bool positive) {
        foreach (var item in totals) {
            if (Math.Abs(item.Value) < 0.000001) continue;
            var label = FormatValue(chart, item.Value);
            var fontSize = chart.Options.Theme.DataLabelFontSize;
            var width = EstimatePngEmphasizedTextWidth(label, fontSize);
            var x = positive ? map.X(item.Value) + offset : map.X(item.Value) + offset - width;
            x = Clamp(x, plot.Left + 2, plot.Right - width - 2);
            var y = Clamp(map.Y(item.Key) - fontSize / 2.0, plot.Top + 2, plot.Bottom - fontSize - 2);
            DrawReadablePngLabel(c, x, y, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
        }
    }

    private static void DrawStackTotalSet(RgbaCanvas c, Chart chart, ChartRect plot, ChartMapper map, Dictionary<double, double> totals, double offset) {
        foreach (var item in totals) {
            if (Math.Abs(item.Value) < 0.000001) continue;
            var label = FormatValue(chart, item.Value);
            var fontSize = chart.Options.Theme.DataLabelFontSize;
            var width = EstimatePngEmphasizedTextWidth(label, fontSize);
            var x = Clamp(map.X(item.Key) - width / 2.0, plot.Left + 2, plot.Right - width - 2);
            var y = Clamp(map.Y(item.Value) + offset - fontSize / 2.0, plot.Top + 2, plot.Bottom - fontSize - 2);
            DrawReadablePngLabel(c, x, y, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize);
        }
    }

    private static void AddStackTotal(Dictionary<double, double> totals, double x, double y) {
        double current;
        totals.TryGetValue(x, out current);
        totals[x] = current + y;
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
}
