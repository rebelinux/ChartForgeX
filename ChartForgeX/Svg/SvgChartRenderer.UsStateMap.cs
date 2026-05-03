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
    private static void DrawUsStateTileMap(StringBuilder sb, Chart chart, ChartRect basePlot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.UsStateTileMap);
        if (series == null || series.Points.Count == 0) return;
        var data = UsStateMapValues(chart, series);
        var t = chart.Options.Theme;
        var bottomReserve = chart.Options.ShowMapScaleLegend ? 52 : 20;
        var plot = new ChartRect(basePlot.Left + 10, basePlot.Top + 10, Math.Max(1, basePlot.Width - 20), Math.Max(1, basePlot.Height - bottomReserve));
        var maxColumn = UsStateTiles.Max(tile => tile.Column);
        var maxRow = UsStateTiles.Max(tile => tile.Row);
        var gap = 4.0;
        var tileSize = Math.Max(4, Math.Min((plot.Width - gap * maxColumn) / (maxColumn + 1), (plot.Height - gap * maxRow) / (maxRow + 1)));
        var width = (maxColumn + 1) * tileSize + maxColumn * gap;
        var height = (maxRow + 1) * tileSize + maxRow * gap;
        var x0 = plot.Left + Math.Max(0, (plot.Width - width) / 2);
        var y0 = plot.Top + Math.Max(0, (plot.Height - height) / 2);
        var min = data.Count == 0 ? 0 : data.Values.Min(item => item.Value);
        var max = data.Count == 0 ? 1 : data.Values.Max(item => item.Value);
        var sourceMin = min;
        var sourceMax = max;
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var hasMissing = UsStateTiles.Any(tile => !data.ContainsKey(tile.Code));
        var missingCount = UsStateTiles.Count(tile => !data.ContainsKey(tile.Code));
        var containerSummary = series.Name + " US state tile map with " + data.Count.ToString(CultureInfo.InvariantCulture) + " filled regions and " + missingCount.ToString(CultureInfo.InvariantCulture) + " missing regions";

        sb.AppendLine($"<g data-cfx-role=\"us-state-tile-map\" data-cfx-map-kind=\"us-state-tile\" data-cfx-label=\"{Escape(series.Name)}\" data-cfx-region-count=\"{UsStateTiles.Length}\" data-cfx-filled-region-count=\"{data.Count}\" data-cfx-missing-region-count=\"{missingCount}\" data-cfx-min-value=\"{F(sourceMin)}\" data-cfx-max-value=\"{F(sourceMax)}\" role=\"group\" aria-label=\"{Escape(containerSummary)}\">");
        foreach (var tile in UsStateTiles) {
            var hasValue = data.TryGetValue(tile.Code, out var entry);
            var value = hasValue ? entry.Value : 0;
            var ratio = hasValue ? HeatmapRatio(value, min, max) : 0;
            var status = hasValue ? HeatmapStatus(ratio) : "empty";
            var color = hasValue ? HeatmapColor(chart, entry.Color ?? series.Color ?? t.Palette[0], value, min, max) : UsStateNoDataColor(chart);
            var x = x0 + tile.Column * (tileSize + gap);
            var y = y0 + tile.Row * (tileSize + gap);
            var points = HexTilePoints(x, y, tileSize);
            var regionName = UsStateName(tile.Code);
            var summary = regionName + " (" + tile.Code + "): " + (hasValue ? FormatValue(chart, value) : "No data");
            sb.AppendLine($"<polygon class=\"cfx-interactive-region\" tabindex=\"0\" focusable=\"true\" data-cfx-role=\"us-state-tile-map-region\" data-cfx-region=\"{tile.Code}\" data-cfx-region-name=\"{Escape(regionName)}\" data-cfx-value=\"{F(value)}\" data-cfx-empty=\"{(!hasValue).ToString().ToLowerInvariant()}\" data-cfx-status=\"{status}\" role=\"img\" aria-label=\"{Escape(summary)}\" points=\"{points}\" fill=\"{color.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"{F(Math.Max(1, tileSize * 0.035))}\"><title>{Escape(summary)}</title></polygon>");
            if (chart.Options.ShowMapLabels) DrawSvgTextCenteredX(sb, chart, "us-state-tile-map-label", tile.Code, x + tileSize / 2, y + tileSize / 2, HeatmapTextColor(color), Math.Min(t.TickLabelFontSize, tileSize * 0.32), tileSize - 6, "800");
        }

        if (chart.Options.ShowMapScaleLegend) {
            var scaleSize = Math.Max(8, Math.Min(13, tileSize * 0.32));
            var scaleY = Clamp(y0 + height + 22, basePlot.Top, basePlot.Bottom - scaleSize - 6);
            DrawUsStateTileMapSvgScale(sb, chart, series, min, max, hasMissing, x0 + width, scaleY, tileSize, plot);
        }
        sb.AppendLine("</g>");
    }

    private static void DrawUsStateTileMapSvgScale(StringBuilder sb, Chart chart, ChartSeries series, double min, double max, bool hasMissing, double right, double y, double tileSize, ChartRect plot) {
        var t = chart.Options.Theme;
        var size = Math.Max(8, Math.Min(13, tileSize * 0.32));
        var gap = Math.Max(2, size * 0.3);
        var width = 5 * size + 4 * gap;
        var x = right - width;
        if (hasMissing) DrawUsStateMapSvgNoDataScale(sb, chart, "us-state-tile-map", x, y, size, plot);
        sb.AppendLine($"<text data-cfx-role=\"us-state-tile-map-scale-label\" x=\"{F(x - 8)}\" y=\"{F(y + size / 2)}\" text-anchor=\"end\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">Less</text>");
        for (var i = 0; i < 5; i++) {
            var value = min + (max - min) * (i / 4.0);
            var ratio = HeatmapRatio(value, min, max);
            var color = HeatmapColor(chart, series.Color ?? t.Palette[0], value, min, max);
            sb.AppendLine($"<rect data-cfx-role=\"us-state-tile-map-scale-step\" data-cfx-value=\"{F(value)}\" data-cfx-status=\"{HeatmapStatus(ratio)}\" x=\"{F(x + i * (size + gap))}\" y=\"{F(y)}\" width=\"{F(size)}\" height=\"{F(size)}\" rx=\"{F(Math.Min(3, size * 0.22))}\" fill=\"{color.ToCss()}\"/>");
        }
        sb.AppendLine($"<text data-cfx-role=\"us-state-tile-map-scale-label\" x=\"{F(x + width + 8)}\" y=\"{F(y + size / 2)}\" text-anchor=\"start\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">More</text>");
    }

    private static void DrawUsStateMapSvgNoDataScale(StringBuilder sb, Chart chart, string rolePrefix, double valueScaleX, double y, double size, ChartRect plot) {
        var t = chart.Options.Theme;
        var noData = UsStateNoDataColor(chart);
        const string label = "No data";
        var labelWidth = EstimateTextWidth(label, t.TickLabelFontSize);
        var width = size + 5 + labelWidth;
        var x = valueScaleX - width - 18;
        if (x < plot.Left) {
            x = plot.Left;
            y = Math.Max(plot.Top, y - size - 9);
        }

        sb.AppendLine($"<rect data-cfx-role=\"{rolePrefix}-scale-no-data\" data-cfx-status=\"empty\" x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(size)}\" height=\"{F(size)}\" rx=\"{F(Math.Min(3, size * 0.22))}\" fill=\"{noData.ToCss()}\"/>");
        sb.AppendLine($"<text data-cfx-role=\"{rolePrefix}-scale-no-data-label\" x=\"{F(x + size + 5)}\" y=\"{F(y + size / 2)}\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{label}</text>");
    }

    private static ChartColor UsStateNoDataColor(Chart chart) => Blend(chart.Options.Theme.PlotBackground, chart.Options.Theme.Grid, 0.58);

    private static string HexTilePoints(double x, double y, double size) {
        var inset = size * 0.22;
        return F(x + inset) + "," + F(y) + " " + F(x + size - inset) + "," + F(y) + " " + F(x + size) + "," + F(y + size / 2) + " " + F(x + size - inset) + "," + F(y + size) + " " + F(x + inset) + "," + F(y + size) + " " + F(x) + "," + F(y + size / 2);
    }

    private static Dictionary<string, RegionMapValue> UsStateMapValues(Chart chart, ChartSeries series) {
        var values = new Dictionary<string, RegionMapValue>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < series.Points.Count; i++) {
            var region = i < chart.Options.XAxisLabels.Count ? chart.Options.XAxisLabels[i].Text : string.Empty;
            if (region.Length == 0) continue;
            var color = i < series.PointColors.Count ? series.PointColors[i] : null;
            values[region] = new RegionMapValue(series.Points[i].Y, color);
        }

        return values;
    }

    private static bool IsUsStateTileMapChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.UsStateTileMap);

    private static string UsStateName(string code) {
        return code switch {
            "AL" => "Alabama",
            "AK" => "Alaska",
            "AZ" => "Arizona",
            "AR" => "Arkansas",
            "CA" => "California",
            "CO" => "Colorado",
            "CT" => "Connecticut",
            "DE" => "Delaware",
            "DC" => "District of Columbia",
            "FL" => "Florida",
            "GA" => "Georgia",
            "HI" => "Hawaii",
            "ID" => "Idaho",
            "IL" => "Illinois",
            "IN" => "Indiana",
            "IA" => "Iowa",
            "KS" => "Kansas",
            "KY" => "Kentucky",
            "LA" => "Louisiana",
            "ME" => "Maine",
            "MD" => "Maryland",
            "MA" => "Massachusetts",
            "MI" => "Michigan",
            "MN" => "Minnesota",
            "MS" => "Mississippi",
            "MO" => "Missouri",
            "MT" => "Montana",
            "NE" => "Nebraska",
            "NV" => "Nevada",
            "NH" => "New Hampshire",
            "NJ" => "New Jersey",
            "NM" => "New Mexico",
            "NY" => "New York",
            "NC" => "North Carolina",
            "ND" => "North Dakota",
            "OH" => "Ohio",
            "OK" => "Oklahoma",
            "OR" => "Oregon",
            "PA" => "Pennsylvania",
            "RI" => "Rhode Island",
            "SC" => "South Carolina",
            "SD" => "South Dakota",
            "TN" => "Tennessee",
            "TX" => "Texas",
            "UT" => "Utah",
            "VT" => "Vermont",
            "VA" => "Virginia",
            "WA" => "Washington",
            "WV" => "West Virginia",
            "WI" => "Wisconsin",
            "WY" => "Wyoming",
            _ => code
        };
    }

    private readonly struct RegionMapValue {
        public readonly double Value;
        public readonly ChartColor? Color;

        public RegionMapValue(double value, ChartColor? color) {
            Value = value;
            Color = color;
        }
    }

    private readonly struct UsStateTile {
        public readonly string Code;
        public readonly int Column;
        public readonly int Row;

        public UsStateTile(string code, int column, int row) {
            Code = code;
            Column = column;
            Row = row;
        }
    }

    private static readonly UsStateTile[] UsStateTiles = {
        new("AK", 0, 0), new("ME", 11, 0),
        new("VT", 9, 1), new("NH", 10, 1),
        new("WA", 0, 2), new("MT", 1, 2), new("ND", 2, 2), new("MN", 3, 2), new("WI", 4, 2), new("MI", 5, 2), new("NY", 8, 2), new("MA", 10, 2), new("RI", 11, 2),
        new("OR", 0, 3), new("ID", 1, 3), new("SD", 2, 3), new("IA", 3, 3), new("IL", 4, 3), new("IN", 5, 3), new("OH", 6, 3), new("PA", 7, 3), new("NJ", 8, 3), new("CT", 9, 3),
        new("CA", 0, 4), new("NV", 1, 4), new("WY", 2, 4), new("NE", 3, 4), new("MO", 4, 4), new("KY", 5, 4), new("WV", 6, 4), new("VA", 7, 4), new("MD", 8, 4), new("DE", 9, 4),
        new("AZ", 1, 5), new("UT", 2, 5), new("CO", 3, 5), new("KS", 4, 5), new("AR", 5, 5), new("TN", 6, 5), new("NC", 7, 5), new("SC", 8, 5), new("DC", 9, 5),
        new("HI", 0, 6), new("NM", 2, 6), new("OK", 3, 6), new("LA", 4, 6), new("MS", 5, 6), new("AL", 6, 6), new("GA", 7, 6),
        new("TX", 3, 7), new("FL", 8, 7)
    };
}
