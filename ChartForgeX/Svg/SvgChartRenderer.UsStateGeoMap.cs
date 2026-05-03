using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawUsStateGeoMap(StringBuilder sb, Chart chart, ChartRect basePlot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.UsStateGeoMap);
        if (series == null || series.Points.Count == 0) return;
        var data = UsStateMapValues(chart, series);
        var t = chart.Options.Theme;
        var bottomReserve = chart.Options.ShowMapScaleLegend ? 46 : 12;
        var plot = new ChartRect(basePlot.Left + 10, basePlot.Top + 8, Math.Max(1, basePlot.Width - 20), Math.Max(1, basePlot.Height - bottomReserve));
        var map = FitUsStateGeoMap(plot);
        var min = data.Count == 0 ? 0 : data.Values.Min(item => item.Value);
        var max = data.Count == 0 ? 1 : data.Values.Max(item => item.Value);
        var sourceMin = min;
        var sourceMax = max;
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var hasMissing = UsStateGeoShapes.Shapes.Any(shape => !data.ContainsKey(shape.Code));
        var missingCount = UsStateGeoShapes.Shapes.Count(shape => !data.ContainsKey(shape.Code));
        var containerSummary = series.Name + " US state geographic map with " + data.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " filled regions and " + missingCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " missing regions";

        sb.AppendLine($"<g data-cfx-role=\"us-state-geo-map\" data-cfx-map-kind=\"us-state-geographic\" data-cfx-label=\"{Escape(series.Name)}\" data-cfx-region-count=\"{UsStateGeoShapes.Shapes.Length}\" data-cfx-filled-region-count=\"{data.Count}\" data-cfx-missing-region-count=\"{missingCount}\" data-cfx-min-value=\"{F(sourceMin)}\" data-cfx-max-value=\"{F(sourceMax)}\" role=\"group\" aria-label=\"{Escape(containerSummary)}\">");
        foreach (var shape in UsStateGeoShapes.Shapes) {
            var hasValue = data.TryGetValue(shape.Code, out var entry);
            var value = hasValue ? entry.Value : 0;
            var ratio = hasValue ? HeatmapRatio(value, min, max) : 0;
            var status = hasValue ? HeatmapStatus(ratio) : "empty";
            var color = hasValue ? HeatmapColor(chart, entry.Color ?? series.Color ?? t.Palette[0], value, min, max) : UsStateNoDataColor(chart);
            var path = ScaleUsStateGeoPath(shape.Path, map, out var regionBounds);
            var regionName = UsStateName(shape.Code);
            var summary = regionName + " (" + shape.Code + "): " + (hasValue ? FormatValue(chart, value) : "No data");
            sb.AppendLine($"<path class=\"cfx-interactive-region\" tabindex=\"0\" focusable=\"true\" data-cfx-role=\"us-state-geo-map-region\" data-cfx-region=\"{shape.Code}\" data-cfx-region-name=\"{Escape(regionName)}\" data-cfx-value=\"{F(value)}\" data-cfx-empty=\"{(!hasValue).ToString().ToLowerInvariant()}\" data-cfx-status=\"{status}\" role=\"img\" aria-label=\"{Escape(summary)}\" d=\"{path}\" fill=\"{color.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"1.1\"><title>{Escape(summary)}</title></path>");
            if (chart.Options.ShowMapLabels) {
                var label = ProjectUsStateGeoPoint(shape.Label, map);
                var fontSize = Math.Min(t.TickLabelFontSize, Math.Max(7, map.Height * 0.032));
                if (ShouldDrawUsStateGeoMapLabel(shape.Code, regionBounds, fontSize)) DrawSvgTextCenteredX(sb, chart, "us-state-geo-map-label", shape.Code, label.X, label.Y + 3, HeatmapTextColor(color), fontSize, 34, "800");
            }
        }

        if (chart.Options.ShowMapScaleLegend) DrawUsStateGeoMapSvgScale(sb, chart, series, min, max, hasMissing, plot.Right - 84, plot.Bottom - 14, plot);
        sb.AppendLine("</g>");
    }

    private static void DrawUsStateGeoMapSvgScale(StringBuilder sb, Chart chart, ChartSeries series, double min, double max, bool hasMissing, double x, double y, ChartRect plot) {
        var t = chart.Options.Theme;
        var size = 11.0;
        var gap = 3.0;
        if (hasMissing) DrawUsStateMapSvgNoDataScale(sb, chart, "us-state-geo-map", x, y, size, plot);
        sb.AppendLine($"<text data-cfx-role=\"us-state-geo-map-scale-label\" x=\"{F(x - 8)}\" y=\"{F(y + size / 2)}\" text-anchor=\"end\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">Less</text>");
        for (var i = 0; i < 5; i++) {
            var value = min + (max - min) * (i / 4.0);
            var ratio = HeatmapRatio(value, min, max);
            var color = HeatmapColor(chart, series.Color ?? t.Palette[0], value, min, max);
            sb.AppendLine($"<rect data-cfx-role=\"us-state-geo-map-scale-step\" data-cfx-value=\"{F(value)}\" data-cfx-status=\"{HeatmapStatus(ratio)}\" x=\"{F(x + i * (size + gap))}\" y=\"{F(y)}\" width=\"{F(size)}\" height=\"{F(size)}\" rx=\"2\" fill=\"{color.ToCss()}\"/>");
        }
        sb.AppendLine($"<text data-cfx-role=\"us-state-geo-map-scale-label\" x=\"{F(x + 5 * size + 4 * gap + 8)}\" y=\"{F(y + size / 2)}\" text-anchor=\"start\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">More</text>");
    }

    private static string ScaleUsStateGeoPath(string path, ChartRect target, out ChartRect bounds) {
        var sb = new StringBuilder();
        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var minY = double.PositiveInfinity;
        var maxY = double.NegativeInfinity;
        for (var i = 0; i < path.Length; i++) {
            var command = path[i];
            if (command == 'M' || command == 'L') {
                i++;
                var x = ReadUsStateGeoNumber(path, ref i);
                var y = ReadUsStateGeoNumber(path, ref i);
                var point = ProjectUsStateGeoPoint(new ChartPoint(x, y), target);
                if (point.X < minX) minX = point.X;
                if (point.X > maxX) maxX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.Y > maxY) maxY = point.Y;
                sb.Append(command).Append(F(point.X)).Append(' ').Append(F(point.Y));
                i--;
            } else if (command == 'Z') {
                sb.Append('Z');
            } else if (!char.IsWhiteSpace(command)) {
                throw new InvalidOperationException("Unsupported US state geo path command.");
            }
        }

        bounds = double.IsInfinity(minX) ? new ChartRect(0, 0, 0, 0) : new ChartRect(minX, minY, Math.Max(0, maxX - minX), Math.Max(0, maxY - minY));
        return sb.ToString();
    }

    private static bool ShouldDrawUsStateGeoMapLabel(string code, ChartRect bounds, double fontSize) {
        if (IsCompactUsStateGeoMapLabel(code)) return false;
        return bounds.Width >= EstimateTextWidth(code, fontSize) + 8 && bounds.Height >= fontSize + 5;
    }

    private static bool IsCompactUsStateGeoMapLabel(string code) {
        return code is "CT" or "DC" or "DE" or "MA" or "MD" or "NJ" or "RI";
    }

    private static double ReadUsStateGeoNumber(string path, ref int index) {
        while (index < path.Length && char.IsWhiteSpace(path[index])) index++;
        var start = index;
        while (index < path.Length && (char.IsDigit(path[index]) || path[index] == '-' || path[index] == '.')) index++;
        return double.Parse(path.Substring(start, index - start), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static ChartPoint ProjectUsStateGeoPoint(ChartPoint point, ChartRect target) {
        var bounds = UsStateGeoShapes.Bounds;
        var x = target.Left + (point.X - bounds.Left) / bounds.Width * target.Width;
        var y = target.Top + (point.Y - bounds.Top) / bounds.Height * target.Height;
        return new ChartPoint(x, y);
    }

    private static ChartRect FitUsStateGeoMap(ChartRect plot) {
        var bounds = UsStateGeoShapes.Bounds;
        var aspect = bounds.Width / Math.Max(1, bounds.Height);
        var width = Math.Min(plot.Width, plot.Height * aspect);
        var height = width / aspect;
        if (height > plot.Height) {
            height = plot.Height;
            width = height * aspect;
        }

        return new ChartRect(plot.Left + Math.Max(0, (plot.Width - width) / 2), plot.Top + Math.Max(0, (plot.Height - height) / 2), width, height);
    }

    private static bool IsUsStateGeoMapChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.UsStateGeoMap);
}
