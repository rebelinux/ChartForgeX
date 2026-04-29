using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawPieLike(RgbaCanvas c, Chart chart, ChartRect plot) {
        var series = chart.Series[0];
        var values = new List<ChartPoint>();
        foreach (var point in series.Points) if (point.Y > 0) values.Add(point);
        if (values.Count == 0) return;

        var total = 0d;
        foreach (var value in values) total += value.Y;
        var chartPlot = PngPieChartPlot(chart, plot, values);
        var radiusFactor = chart.Options.ShowLegend && PngIsTopOrBottomLegend(chart.Options.LegendPosition) ? 0.40 : 0.44;
        var radius = Math.Max(1, Math.Min(chartPlot.Width, chartPlot.Height) * radiusFactor);
        var cx = chartPlot.Left + chartPlot.Width / 2;
        var cy = chartPlot.Top + chartPlot.Height / 2;
        var inner = series.Kind == ChartSeriesKind.Donut ? radius * chart.Options.DonutInnerRadiusRatio : 0;
        var start = -Math.PI / 2;
        var separator = chart.Options.Theme.CardBackground;
        var outsideLabels = new List<PieLabelCandidate>();
        var hasOffsetSlice = false;

        for (var i = 0; i < values.Count; i++) {
            var sweep = values[i].Y / total * Math.PI * 2;
            var end = start + sweep;
            var color = PieSliceColor(chart, series, i);
            var mid = start + sweep / 2;
            var offset = PieSliceOffset(series, i) * radius;
            hasOffsetSlice |= offset > 0;
            var sliceCx = cx + Math.Cos(mid) * offset;
            var sliceCy = cy + Math.Sin(mid) * offset;
            c.FillRingSlice(sliceCx, sliceCy, radius, inner, start, end, color);
            DrawSliceSeparator(c, sliceCx, sliceCy, radius, inner, start, separator);
            if (offset > 0) {
                DrawSliceSeparator(c, sliceCx, sliceCy, radius, inner, end, separator);
                c.DrawArc(sliceCx, sliceCy, radius, start, end, separator, ChartVisualPrimitives.SliceSeparatorStrokeWidth);
                if (inner > 0) c.DrawArc(sliceCx, sliceCy, inner, start, end, separator, ChartVisualPrimitives.SliceSeparatorStrokeWidth);
            }

            if (ShouldDrawDataLabels(chart, series) && sweep > 0.22) {
                var placement = DataLabelPlacement(chart, series);
                var labelRadius = PieLabelRadius(inner, radius, placement);
                var sliceLabel = SliceLabel(chart, values[i], i);
                var label = FormatPieSliceLabel(chart, series, sliceLabel, values[i].Y, values[i].Y / total, i);
                var style = DataLabelStyle(chart, series, i);
                var fontSize = PngDataLabelFontSize(chart, series, i);
                if (placement == ChartDataLabelPlacement.Auto || placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center) {
                    FitReadablePngLabel(label, fontSize, Math.Max(16, radius * 0.85), fontSize + 8, out label, out fontSize);
                    if (label.Length == 0) {
                        start = end;
                        continue;
                    }
                }

                var isSideLabel = IsPieSideLabelPlacement(placement);
                var isLeftSide = placement == ChartDataLabelPlacement.Left || (placement == ChartDataLabelPlacement.Outside && Math.Cos(mid) < 0);
                var labelWidth = EstimatePngEmphasizedTextWidth(label, fontSize);
                var labelHeight = EstimatePngTextHeight(fontSize);
                var labelX = sliceCx + Math.Cos(mid) * labelRadius - labelWidth / 2.0;
                var labelY = sliceCy + Math.Sin(mid) * labelRadius - fontSize / 2.0;
                if (isSideLabel) {
                    var anchorX = cx + (isLeftSide ? -radius * chart.Options.PieOutsideLabelDistanceRatio : radius * chart.Options.PieOutsideLabelDistanceRatio);
                    var maxLabelWidth = isLeftSide ? anchorX - plot.Left - 6 : plot.Right - anchorX - 6;
                    FitReadablePngLabel(label, fontSize, Math.Max(8, maxLabelWidth), fontSize + 8, out label, out fontSize);
                    if (label.Length == 0) {
                        start = end;
                        continue;
                    }

                    labelWidth = EstimatePngEmphasizedTextWidth(label, fontSize);
                    labelHeight = EstimatePngTextHeight(fontSize);
                    labelX = isLeftSide ? anchorX - labelWidth : anchorX;
                }

                if (placement == ChartDataLabelPlacement.Above) labelY = cy - radius * 1.10 - fontSize / 2.0;
                else if (placement == ChartDataLabelPlacement.Below) labelY = cy + radius * 1.10 - fontSize / 2.0;
                if (placement == ChartDataLabelPlacement.Auto || placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center) DrawReadablePngLabel(c, labelX, labelY, label, chart.Options.Theme.CardBackground, chart.Options.Theme.Text, fontSize, style);
                else if (isSideLabel) outsideLabels.Add(new PieLabelCandidate(i, label, mid, isLeftSide ? labelX + labelWidth : labelX, labelY + labelHeight / 2.0, labelWidth, labelHeight, fontSize, style, sliceCx, sliceCy, isLeftSide));
                else {
                    DrawPieLabelConnector(c, chart, series, i, sliceCx, sliceCy, radius, mid, labelX + labelWidth / 2.0, labelY + labelHeight / 2.0);
                    DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, style);
                }
            }

            start = end;
        }

        if (!hasOffsetSlice) {
            c.DrawCircleOutline(cx, cy, radius, separator, ChartVisualPrimitives.SliceSeparatorStrokeWidth);
            if (inner > 0) c.DrawCircleOutline(cx, cy, inner, separator, ChartVisualPrimitives.SliceSeparatorStrokeWidth);
        }

        DrawPieOutsideLabels(c, chart, series, outsideLabels, cx, cy, radius, plot);

        if (series.Kind == ChartSeriesKind.Donut && chart.Options.ShowDonutCenterLabel && series.ShowDataLabels != false) {
            var totalLabel = chart.Options.DonutCenterValue ?? FormatValue(chart, total);
            var nameLabel = chart.Options.DonutCenterLabel ?? series.Name;
            var totalFontSize = Math.Max(14, Math.Min(26, inner * 0.45));
            var nameFontSize = Math.Max(9, Math.Min(chart.Options.Theme.TickLabelFontSize, inner * 0.22));
            var centerLabelWidth = Math.Max(24, inner * 1.55);
            DrawPngTextEmphasizedCenteredX(c, cx, cy - totalFontSize / 2.0 - totalFontSize * 0.08, totalLabel, chart.Options.Theme.Text, totalFontSize, centerLabelWidth);
            DrawPngTextEmphasizedCenteredX(c, cx, cy + totalFontSize * 0.62 - nameFontSize + 1, nameLabel, chart.Options.Theme.MutedText, nameFontSize, centerLabelWidth);
        }

        if (chart.Options.ShowLegend) DrawSliceLegend(c, chart, series, values, plot, total);
    }

    private static void DrawSliceSeparator(RgbaCanvas c, double cx, double cy, double outerRadius, double innerRadius, double angle, ChartColor color) {
        var startRadius = Math.Max(0, innerRadius - 0.5);
        c.DrawLine(cx + Math.Cos(angle) * startRadius, cy + Math.Sin(angle) * startRadius, cx + Math.Cos(angle) * outerRadius, cy + Math.Sin(angle) * outerRadius, color, ChartVisualPrimitives.SliceSeparatorStrokeWidth);
    }

    private static double PieLabelRadius(double inner, double radius, ChartDataLabelPlacement placement) {
        if (placement == ChartDataLabelPlacement.Outside || placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right || placement == ChartDataLabelPlacement.Above || placement == ChartDataLabelPlacement.Below) return radius * 1.10;
        return inner > 0 ? (inner + radius) / 2 : radius * 0.66;
    }

    private static double PieSliceOffset(ChartSeries series, int pointIndex) =>
        pointIndex >= 0 && pointIndex < series.PointSliceOffsets.Count ? series.PointSliceOffsets[pointIndex] : 0;

    private static bool IsPieSideLabelPlacement(ChartDataLabelPlacement placement) =>
        placement == ChartDataLabelPlacement.Outside || placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right;

    private static string FormatPieSliceLabel(Chart chart, ChartSeries series, string label, double value, double percent, int pointIndex) {
        var formattedValue = FormatValue(chart, value);
        var formattedPercent = FormatPercent(percent);
        if (chart.Options.PieSliceLabelFormatter != null) {
            return chart.Options.PieSliceLabelFormatter(new ChartPieSliceLabelContext(series.Name, label, value, percent, formattedValue, formattedPercent, pointIndex)) ?? string.Empty;
        }

        switch (chart.Options.PieSliceLabelContent) {
            case ChartPieSliceLabelContent.Value:
                return formattedValue;
            case ChartPieSliceLabelContent.Label:
                return label;
            case ChartPieSliceLabelContent.LabelAndPercent:
                return label + " " + formattedPercent;
            case ChartPieSliceLabelContent.LabelAndValue:
                return label + " " + formattedValue;
            default:
                return formattedPercent;
        }
    }

    private static void DrawPieOutsideLabels(RgbaCanvas c, Chart chart, ChartSeries series, List<PieLabelCandidate> labels, double cx, double cy, double radius, ChartRect plot) {
        if (labels.Count == 0) return;
        ArrangePieLabelLane(labels.FindAll(static label => label.IsLeftSide), chart, plot);
        ArrangePieLabelLane(labels.FindAll(static label => !label.IsLeftSide), chart, plot);
        foreach (var label in labels) {
            var labelX = label.IsLeftSide ? label.AnchorX - label.Width : label.AnchorX;
            var labelY = label.CenterY - label.Height / 2.0;
            DrawPieLabelConnector(c, chart, series, label.PointIndex, label.SliceCx, label.SliceCy, radius, label.Angle, label.AnchorX, label.CenterY);
            DrawReadablePngLabel(c, plot, labelX, labelY, label.Text, chart.Options.Theme.Text, ReadableLabelHalo(chart), label.FontSize, label.Style);
        }
    }

    private static void ArrangePieLabelLane(List<PieLabelCandidate> labels, Chart chart, ChartRect plot) {
        if (labels.Count == 0) return;
        var gap = Math.Max(14, chart.Options.Theme.DataLabelFontSize + 7);
        var top = plot.Top + gap;
        var bottom = plot.Bottom - gap;
        labels.Sort(static (left, right) => left.CenterY.CompareTo(right.CenterY));
        for (var i = 0; i < labels.Count; i++) {
            labels[i].CenterY = Clamp(labels[i].CenterY, top, bottom);
            if (i > 0) labels[i].CenterY = Math.Max(labels[i].CenterY, labels[i - 1].CenterY + gap);
        }

        var overflow = labels[labels.Count - 1].CenterY - bottom;
        if (overflow > 0) {
            for (var i = 0; i < labels.Count; i++) labels[i].CenterY -= overflow;
        }

        for (var i = 0; i < labels.Count; i++) {
            labels[i].CenterY = Clamp(labels[i].CenterY, top, bottom);
            if (i > 0) labels[i].CenterY = Math.Max(labels[i].CenterY, labels[i - 1].CenterY + gap);
        }
    }

    private static ChartColor PieSliceColor(Chart chart, ChartSeries series, int pointIndex) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value
            : chart.Options.Theme.Palette[pointIndex % chart.Options.Theme.Palette.Length];

    private static ChartColor PieConnectorColor(Chart chart, ChartSeries series, int pointIndex) =>
        chart.Options.DataLabelConnectorColor ?? PieSliceColor(chart, series, pointIndex);

    private static void DrawPieLabelConnector(RgbaCanvas c, Chart chart, ChartSeries series, int pointIndex, double cx, double cy, double radius, double angle, double labelX, double labelY) {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        var startX = cx + cos * radius * 1.01;
        var startY = cy + sin * radius * 1.01;
        var elbowX = cx + cos * radius * 1.11;
        var elbowY = cy + sin * radius * 1.11;
        var side = labelX < cx ? -1 : 1;
        var targetX = labelX - side * 8;
        var tickStartX = targetX - side * Math.Min(16, Math.Max(9, radius * 0.07));
        var color = ApplyOpacity(PieConnectorColor(chart, series, pointIndex), chart.Options.DataLabelConnectorOpacity);
        if (chart.Options.DataLabelConnectorStyle == ChartDataLabelConnectorStyle.Straight) {
            c.DrawLine(startX, startY, targetX, labelY, color, chart.Options.DataLabelConnectorStrokeWidth);
            return;
        }

        if (chart.Options.DataLabelConnectorStyle == ChartDataLabelConnectorStyle.Curve) {
            var control1X = startX + side * radius * 0.16;
            var control1Y = startY;
            var control2X = tickStartX - side * Math.Min(radius * 0.24, Math.Abs(tickStartX - startX) * 0.42);
            var control2Y = labelY;
            var previousX = startX;
            var previousY = startY;
            for (var i = 1; i <= 14; i++) {
                var t = i / 14.0;
                var x = PieConnectorCubic(startX, control1X, control2X, tickStartX, t);
                var y = PieConnectorCubic(startY, control1Y, control2Y, labelY, t);
                c.DrawLine(previousX, previousY, x, y, color, chart.Options.DataLabelConnectorStrokeWidth);
                previousX = x;
                previousY = y;
            }
            c.DrawLine(tickStartX, labelY, targetX, labelY, color, chart.Options.DataLabelConnectorStrokeWidth);
            return;
        }

        c.DrawLine(startX, startY, elbowX, elbowY, color, chart.Options.DataLabelConnectorStrokeWidth);
        c.DrawLine(elbowX, elbowY, tickStartX, labelY, color, chart.Options.DataLabelConnectorStrokeWidth);
        c.DrawLine(tickStartX, labelY, targetX, labelY, color, chart.Options.DataLabelConnectorStrokeWidth);
    }

    private static double PieConnectorCubic(double p0, double p1, double p2, double p3, double t) {
        var u = 1 - t;
        return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
    }

    private sealed class PieLabelCandidate {
        public PieLabelCandidate(int pointIndex, string text, double angle, double anchorX, double centerY, double width, double height, double fontSize, ChartTextStyle style, double sliceCx, double sliceCy, bool isLeftSide) {
            PointIndex = pointIndex;
            Text = text;
            Angle = angle;
            AnchorX = anchorX;
            CenterY = centerY;
            Width = width;
            Height = height;
            FontSize = fontSize;
            Style = style;
            SliceCx = sliceCx;
            SliceCy = sliceCy;
            IsLeftSide = isLeftSide;
        }

        public int PointIndex { get; }

        public string Text { get; }

        public double Angle { get; }

        public double AnchorX { get; }

        public double CenterY { get; set; }

        public double Width { get; }

        public double Height { get; }

        public double FontSize { get; }

        public ChartTextStyle Style { get; }

        public double SliceCx { get; }

        public double SliceCy { get; }

        public bool IsLeftSide { get; }
    }

    private static void DrawSliceLegend(RgbaCanvas c, Chart chart, ChartSeries series, IReadOnlyList<ChartPoint> values, ChartRect plot, double total) {
        var fontSize = PngLegendFontSize(chart);
        const double swatchSize = ChartVisualPrimitives.SliceLegendSwatchSize;
        var area = PngSliceLegendArea(chart, plot, values);
        var rows = BuildPngSliceLegendRows(chart, series, values, total, area.Width);
        var y = PngSliceLegendStartY(chart, area, rows.Count);
        foreach (var row in rows) {
            var x = PngSliceLegendRowX(chart, area, row.Width);
            foreach (var item in row.Items) {
                var itemX = x + item.X;
                c.FillRoundedRect(itemX, y - swatchSize + 1, swatchSize, swatchSize, ChartVisualPrimitives.SliceLegendSwatchRadius, item.Color);
                if (item.Label.Length > 0) c.DrawTextEmphasized(itemX + swatchSize + 8, y - fontSize + 3, item.Label, chart.Options.Theme.Text, fontSize);
                c.DrawText(itemX + item.Width - EstimatePngTextWidth(item.Percent, fontSize) - 10, y - fontSize + 3, item.Percent, chart.Options.Theme.MutedText, fontSize);
            }

            y += PngSliceLegendRowHeight(chart);
        }
    }

    private static ChartRect PngPieChartPlot(Chart chart, ChartRect plot, IReadOnlyList<ChartPoint> values) {
        if (!chart.Options.ShowLegend || values.Count == 0) return plot;
        var reserve = PngSliceLegendReserve(chart, values, plot);
        if (PngIsLeftLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X + reserve, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        if (PngIsRightLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X, plot.Y, Math.Max(1, plot.Width - reserve), plot.Height);
        if (PngIsTopLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X, plot.Y + reserve, plot.Width, Math.Max(1, plot.Height - reserve));
        if (PngIsBottomLegend(chart.Options.LegendPosition)) return new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(1, plot.Height - reserve));
        return plot;
    }

    private static double PngSliceLegendReserve(Chart chart, IReadOnlyList<ChartPoint> values, ChartRect plot) {
        if (PngIsLeftLegend(chart.Options.LegendPosition) || PngIsRightLegend(chart.Options.LegendPosition)) return Math.Min(230, Math.Max(142, PngSliceLegendWidestItem(chart, values) + 22));
        return 18 + BuildPngSliceLegendRows(chart, chart.Series[0], values, PngSliceLegendTotal(values), Math.Max(80, plot.Width - 80)).Count * PngSliceLegendRowHeight(chart);
    }

    private static double PngSliceLegendWidestItem(Chart chart, IReadOnlyList<ChartPoint> values) {
        var widest = 0.0;
        var total = Math.Max(0.000001, PngSliceLegendTotal(values));
        for (var i = 0; i < values.Count; i++) {
            var label = SliceLabel(chart, values[i], i);
            var percent = FormatPercent(values[i].Y / total);
            widest = Math.Max(widest, ChartVisualPrimitives.SliceLegendSwatchSize + EstimatePngEmphasizedTextWidth(label, PngLegendFontSize(chart)) + EstimatePngTextWidth(percent, PngLegendFontSize(chart)) + 36);
        }

        return widest;
    }

    private static ChartRect PngSliceLegendArea(Chart chart, ChartRect plot, IReadOnlyList<ChartPoint> values) {
        var reserve = PngSliceLegendReserve(chart, values, plot);
        if (PngIsLeftLegend(chart.Options.LegendPosition)) return new ChartRect(plot.Left + 18, plot.Top + 20, Math.Max(1, reserve - 28), Math.Max(1, plot.Height - 40));
        if (PngIsRightLegend(chart.Options.LegendPosition)) return new ChartRect(plot.Right - reserve + 10, plot.Top + 20, Math.Max(1, reserve - 28), Math.Max(1, plot.Height - 40));
        var y = PngIsTopLegend(chart.Options.LegendPosition) ? plot.Top + 14 : plot.Bottom - reserve + 12;
        return new ChartRect(plot.Left + 36, y, Math.Max(1, plot.Width - 72), reserve);
    }

    private static List<PngSliceLegendRow> BuildPngSliceLegendRows(Chart chart, ChartSeries series, IReadOnlyList<ChartPoint> values, double total, double width) {
        var rows = new List<PngSliceLegendRow>();
        var row = new PngSliceLegendRow();
        rows.Add(row);
        var x = 0.0;
        var vertical = PngIsLeftLegend(chart.Options.LegendPosition) || PngIsRightLegend(chart.Options.LegendPosition);
        var maxX = Math.Max(48, width);
        for (var i = 0; i < values.Count; i++) {
            var percent = FormatPercent(values[i].Y / Math.Max(0.000001, total));
            var percentWidth = EstimatePngTextWidth(percent, PngLegendFontSize(chart));
            var labelMax = Math.Max(24, maxX - percentWidth - ChartVisualPrimitives.SliceLegendSwatchSize - 32);
            var labelFontSize = TextFontSizeForEmphasizedWidth(SliceLabel(chart, values[i], i), labelMax, PngLegendFontSize(chart));
            var label = TrimReadablePngLabelToWidth(SliceLabel(chart, values[i], i), labelFontSize, labelMax);
            var itemWidth = Math.Min(maxX, ChartVisualPrimitives.SliceLegendSwatchSize + EstimatePngEmphasizedTextWidth(label, PngLegendFontSize(chart)) + percentWidth + 34);
            if (row.Items.Count > 0 && (vertical || x + itemWidth > maxX)) {
                row = new PngSliceLegendRow();
                rows.Add(row);
                x = 0;
            }

            row.Items.Add(new PngSliceLegendItem(x, itemWidth, label, percent, PieSliceColor(chart, series, i)));
            row.Width = Math.Max(row.Width, x + itemWidth);
            x += itemWidth;
        }

        return rows;
    }

    private static double PngSliceLegendStartY(Chart chart, ChartRect area, int rows) =>
        PngIsBottomLegend(chart.Options.LegendPosition) ? area.Bottom - 18 - Math.Max(0, rows - 1) * PngSliceLegendRowHeight(chart) : area.Top + 16;

    private static double PngSliceLegendRowX(Chart chart, ChartRect area, double rowWidth) {
        if (chart.Options.LegendPosition == ChartLegendPosition.TopRight || chart.Options.LegendPosition == ChartLegendPosition.BottomRight || PngIsRightLegend(chart.Options.LegendPosition)) return area.Right - Math.Min(area.Width, rowWidth);
        if (chart.Options.LegendPosition == ChartLegendPosition.Top || chart.Options.LegendPosition == ChartLegendPosition.Bottom) return area.X + Math.Max(0, (area.Width - rowWidth) / 2.0);
        return area.X;
    }

    private static double PngSliceLegendRowHeight(Chart chart) => PngLegendFontSize(chart) + 10;

    private static bool PngIsTopOrBottomLegend(ChartLegendPosition position) => PngIsTopLegend(position) || PngIsBottomLegend(position);

    private static double PngSliceLegendTotal(IReadOnlyList<ChartPoint> values) {
        var total = 0.0;
        foreach (var value in values) total += value.Y;
        return total;
    }

    private static string SliceLabel(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) {
            if (Math.Abs(label.Value - point.X) < 0.000001) return label.Text;
        }

        return "Slice " + (index + 1);
    }

    private sealed class PngSliceLegendRow {
        public List<PngSliceLegendItem> Items { get; } = new();
        public double Width { get; set; }
    }

    private readonly struct PngSliceLegendItem {
        public PngSliceLegendItem(double x, double width, string label, string percent, ChartColor color) {
            X = x;
            Width = width;
            Label = label;
            Percent = percent;
            Color = color;
        }

        public double X { get; }
        public double Width { get; }
        public string Label { get; }
        public string Percent { get; }
        public ChartColor Color { get; }
    }
}
