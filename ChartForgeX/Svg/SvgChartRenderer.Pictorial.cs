using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawPictorial(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.Pictorial);
        if (series == null || series.Points.Count == 0) return;
        var values = series.Points.ToArray();
        var max = Math.Max(0.000001, chart.Options.PictorialMaximum ?? values.Max(point => point.Y));
        var t = chart.Options.Theme;
        var columns = chart.Options.PictorialColumns;
        var valuePerSymbol = chart.Options.PictorialValuePerSymbol;
        var symbolRows = BuildPictorialSymbolRows(values, columns, valuePerSymbol);
        var totalSymbolRows = Math.Max(1, symbolRows.Sum());
        var itemGap = Math.Min(12, Math.Max(4, plot.Height * 0.018));
        var rowHeight = Math.Max(16, Math.Min(54, (plot.Height - itemGap * (values.Length - 1)) / totalSymbolRows));
        var labelWidth = Math.Min(160, Math.Max(84, plot.Width * 0.26));
        var showValues = chart.Options.ShowPictorialValues;
        var valueWidth = showValues ? Math.Min(88, Math.Max(50, plot.Width * 0.14)) : 0;
        var symbolArea = Math.Max(1, plot.Width - labelWidth - valueWidth - (showValues ? 22 : 10));
        var gap = Math.Max(1.2, Math.Min(7, symbolArea / columns * 0.16));
        var maxSymbolSize = Math.Max(1, (symbolArea - gap * (columns - 1)) / columns);
        var baseSymbolSize = Math.Min(rowHeight * 0.72, maxSymbolSize);
        var symbolSize = Math.Max(3, Math.Min(maxSymbolSize, baseSymbolSize * chart.Options.PictorialSymbolScale));
        var startX = plot.Left + labelWidth + 10;
        var totalHeight = rowHeight * totalSymbolRows + itemGap * (values.Length - 1);
        var startY = plot.Top + Math.Max(0, (plot.Height - totalHeight) / 2);
        var customSymbol = chart.Options.PictorialSvgPathData != null;
        var valuePerSymbolMetadata = valuePerSymbol.HasValue ? F(valuePerSymbol.Value) : "proportional";
        sb.AppendLine($"<g data-cfx-role=\"pictorial-chart\" data-cfx-shape=\"{chart.Options.PictorialShape}\" data-cfx-custom-symbol=\"{(customSymbol ? "true" : "false")}\" data-cfx-png-fallback-shape=\"{chart.Options.PictorialPngFallbackShape}\" data-cfx-columns=\"{columns}\" data-cfx-maximum=\"{F(max)}\" data-cfx-value-per-symbol=\"{valuePerSymbolMetadata}\" data-cfx-show-values=\"{(showValues ? "true" : "false")}\" data-cfx-symbol-scale=\"{F(chart.Options.PictorialSymbolScale)}\" data-cfx-empty-opacity=\"{F(chart.Options.PictorialEmptyOpacity)}\">");
        var symbolRow = 0;
        for (var i = 0; i < values.Length; i++) {
            var rowsForItem = symbolRows[i];
            var itemTop = startY + symbolRow * rowHeight + i * itemGap;
            var labelY = itemTop + rowsForItem * rowHeight / 2;
            var label = TrimSvgLabelToWidth(FormatX(chart, values[i].X), t.TickLabelFontSize, labelWidth - 8);
            sb.AppendLine($"<text data-cfx-role=\"pictorial-label\" data-cfx-point=\"{i}\" x=\"{F(plot.Left + labelWidth - 8)}\" y=\"{F(labelY + t.TickLabelFontSize / 3.0)}\" text-anchor=\"end\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"700\">{Escape(label)}</text>");
            var color = PictorialItemColor(series, t, i);
            var filled = valuePerSymbol.HasValue ? values[i].Y / valuePerSymbol.Value : values[i].Y / max * columns;
            for (var row = 0; row < rowsForItem; row++) {
                var y = itemTop + row * rowHeight + rowHeight / 2;
                for (var column = 0; column < columns; column++) {
                    var amount = Clamp(filled - row * columns - column, 0, 1);
                    var x = startX + column * (symbolSize + gap);
                    var emptyColor = PictorialOpacity(t.Grid, chart.Options.PictorialEmptyOpacity);
                    var symbolId = id + "-pictorial-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + row.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + column.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    DrawPictorialSymbol(sb, chart.Options, symbolId, x + symbolSize / 2, y, symbolSize / 2, color, emptyColor, amount, row);
                }
            }

            if (showValues) {
                var value = FormatValue(chart, values[i].Y);
                sb.AppendLine($"<text data-cfx-role=\"pictorial-value\" data-cfx-point=\"{i}\" data-cfx-label=\"{Escape(label)}\" data-cfx-value=\"{F(values[i].Y)}\" x=\"{F(startX + symbolArea + 12)}\" y=\"{F(labelY + t.DataLabelFontSize / 3.0)}\" fill=\"{t.Text.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.DataLabelFontSize)}\" font-weight=\"800\">{Escape(value)}</text>");
            }

            symbolRow += rowsForItem;
        }

        sb.AppendLine("</g>");
    }

    private static int[] BuildPictorialSymbolRows(ChartPoint[] values, int columns, double? valuePerSymbol) {
        var rows = new int[values.Length];
        for (var i = 0; i < values.Length; i++) {
            rows[i] = valuePerSymbol.HasValue ? Math.Max(1, (int)Math.Ceiling(values[i].Y / valuePerSymbol.Value / columns)) : 1;
        }

        return rows;
    }

    private static void DrawPictorialSymbol(StringBuilder sb, ChartOptions options, string symbolId, double cx, double cy, double radius, ChartColor fillColor, ChartColor emptyColor, double fillAmount, int row) {
        var fill = F(fillAmount);
        sb.AppendLine($"<g data-cfx-role=\"pictorial-symbol\" data-cfx-fill=\"{fill}\" data-cfx-row=\"{row}\" data-cfx-partial-fill=\"{(fillAmount > 0 && fillAmount < 1 ? "clip" : "none")}\">");
        if (fillAmount <= 0) {
            AppendPictorialSymbolShape(sb, options, cx, cy, radius, emptyColor, "empty");
        } else if (fillAmount >= 1) {
            AppendPictorialSymbolShape(sb, options, cx, cy, radius, fillColor, "fill");
        } else {
            AppendPictorialSymbolShape(sb, options, cx, cy, radius, emptyColor, "empty");
            sb.AppendLine($"<clipPath id=\"{symbolId}-clip\"><rect x=\"{F(cx - radius)}\" y=\"{F(cy - radius)}\" width=\"{F(radius * 2 * fillAmount)}\" height=\"{F(radius * 2)}\"/></clipPath>");
            sb.AppendLine($"<g clip-path=\"url(#{symbolId}-clip)\">");
            AppendPictorialSymbolShape(sb, options, cx, cy, radius, fillColor, "partial-fill");
            sb.AppendLine("</g>");
        }

        sb.AppendLine("</g>");
    }

    private static void AppendPictorialSymbolShape(StringBuilder sb, ChartOptions options, double cx, double cy, double radius, ChartColor color, string layer) {
        if (options.PictorialSvgPathData != null) {
            var vb = options.PictorialSvgPathViewBox;
            var scaleX = radius * 2 / vb.Width;
            var scaleY = radius * 2 / vb.Height;
            sb.AppendLine($"<path data-cfx-symbol-layer=\"{layer}\" data-cfx-custom-symbol=\"true\" d=\"{options.PictorialSvgPathData}\" transform=\"translate({F(cx - radius)} {F(cy - radius)}) scale({F(scaleX)} {F(scaleY)}) translate({F(-vb.X)} {F(-vb.Y)})\" fill=\"{color.ToCss()}\"/>");
            return;
        }

        var shape = options.PictorialShape;
        if (shape == ChartPictorialShape.Square) {
            sb.AppendLine($"<rect data-cfx-symbol-layer=\"{layer}\" x=\"{F(cx - radius)}\" y=\"{F(cy - radius)}\" width=\"{F(radius * 2)}\" height=\"{F(radius * 2)}\" rx=\"{F(radius * 0.26)}\" fill=\"{color.ToCss()}\"/>");
        } else if (shape == ChartPictorialShape.Diamond) {
            sb.AppendLine($"<path data-cfx-symbol-layer=\"{layer}\" d=\"M {F(cx)} {F(cy - radius)} L {F(cx + radius)} {F(cy)} L {F(cx)} {F(cy + radius)} L {F(cx - radius)} {F(cy)} Z\" fill=\"{color.ToCss()}\"/>");
        } else if (shape == ChartPictorialShape.Triangle) {
            sb.AppendLine($"<path data-cfx-symbol-layer=\"{layer}\" d=\"M {F(cx)} {F(cy - radius)} L {F(cx + radius)} {F(cy + radius)} L {F(cx - radius)} {F(cy + radius)} Z\" fill=\"{color.ToCss()}\"/>");
        } else if (shape == ChartPictorialShape.Star) {
            sb.AppendLine($"<path data-cfx-symbol-layer=\"{layer}\" d=\"{BuildStarPath(cx, cy, radius, radius * 0.44)}\" fill=\"{color.ToCss()}\"/>");
        } else if (shape == ChartPictorialShape.Heart) {
            sb.AppendLine($"<path data-cfx-symbol-layer=\"{layer}\" d=\"{BuildHeartPath(cx, cy, radius)}\" fill=\"{color.ToCss()}\"/>");
        } else if (shape == ChartPictorialShape.Shield) {
            sb.AppendLine($"<path data-cfx-symbol-layer=\"{layer}\" d=\"M {F(cx)} {F(cy - radius)} C {F(cx + radius * 0.72)} {F(cy - radius * 0.72)} {F(cx + radius * 0.82)} {F(cy - radius * 0.64)} {F(cx + radius * 0.82)} {F(cy - radius * 0.22)} C {F(cx + radius * 0.82)} {F(cy + radius * 0.48)} {F(cx + radius * 0.35)} {F(cy + radius * 0.85)} {F(cx)} {F(cy + radius)} C {F(cx - radius * 0.35)} {F(cy + radius * 0.85)} {F(cx - radius * 0.82)} {F(cy + radius * 0.48)} {F(cx - radius * 0.82)} {F(cy - radius * 0.22)} C {F(cx - radius * 0.82)} {F(cy - radius * 0.64)} {F(cx - radius * 0.72)} {F(cy - radius * 0.72)} {F(cx)} {F(cy - radius)} Z\" fill=\"{color.ToCss()}\"/>");
        } else if (shape == ChartPictorialShape.Check) {
            sb.AppendLine($"<path data-cfx-symbol-layer=\"{layer}\" d=\"M {F(cx - radius * 0.72)} {F(cy - radius * 0.02)} L {F(cx - radius * 0.22)} {F(cy + radius * 0.52)} L {F(cx + radius * 0.76)} {F(cy - radius * 0.56)}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(Math.Max(2, radius * 0.34))}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
        } else if (shape == ChartPictorialShape.Person) {
            sb.AppendLine($"<g data-cfx-symbol-layer=\"{layer}\" fill=\"{color.ToCss()}\"><circle cx=\"{F(cx)}\" cy=\"{F(cy - radius * 0.48)}\" r=\"{F(radius * 0.34)}\"/><path d=\"M {F(cx - radius * 0.64)} {F(cy + radius * 0.74)} C {F(cx - radius * 0.56)} {F(cy + radius * 0.02)} {F(cx - radius * 0.34)} {F(cy - radius * 0.04)} {F(cx)} {F(cy - radius * 0.04)} C {F(cx + radius * 0.34)} {F(cy - radius * 0.04)} {F(cx + radius * 0.56)} {F(cy + radius * 0.02)} {F(cx + radius * 0.64)} {F(cy + radius * 0.74)} Z\"/></g>");
        } else if (shape == ChartPictorialShape.PersonDress) {
            sb.AppendLine($"<g data-cfx-symbol-layer=\"{layer}\" fill=\"{color.ToCss()}\"><circle cx=\"{F(cx)}\" cy=\"{F(cy - radius * 0.55)}\" r=\"{F(radius * 0.30)}\"/><path d=\"M {F(cx)} {F(cy - radius * 0.12)} L {F(cx + radius * 0.62)} {F(cy + radius * 0.72)} L {F(cx - radius * 0.62)} {F(cy + radius * 0.72)} Z\"/><rect x=\"{F(cx - radius * 0.30)}\" y=\"{F(cy - radius * 0.18)}\" width=\"{F(radius * 0.60)}\" height=\"{F(radius * 0.36)}\" rx=\"{F(radius * 0.14)}\"/></g>");
        } else {
            sb.AppendLine($"<circle data-cfx-symbol-layer=\"{layer}\" cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(radius)}\" fill=\"{color.ToCss()}\"/>");
        }
    }

    private static ChartColor PictorialOpacity(ChartColor color, double opacity) =>
        ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(255 * Clamp(opacity, 0, 1)));

    private static ChartColor PictorialItemColor(ChartSeries series, ChartTheme theme, int index) {
        if (index < series.PointColors.Count && series.PointColors[index].HasValue) return series.PointColors[index]!.Value;
        return series.Color ?? theme.Palette[index % theme.Palette.Length];
    }

    private static string BuildStarPath(double cx, double cy, double outer, double inner) {
        var sb = new StringBuilder();
        for (var i = 0; i < 10; i++) {
            var angle = -Math.PI / 2 + i * Math.PI / 5;
            var radius = i % 2 == 0 ? outer : inner;
            var x = cx + Math.Cos(angle) * radius;
            var y = cy + Math.Sin(angle) * radius;
            sb.Append(i == 0 ? "M " : " L ").Append(F(x)).Append(' ').Append(F(y));
        }

        sb.Append(" Z");
        return sb.ToString();
    }

    private static string BuildHeartPath(double cx, double cy, double radius) =>
        $"M {F(cx)} {F(cy + radius * 0.72)} C {F(cx - radius * 1.12)} {F(cy - radius * 0.08)} {F(cx - radius * 0.9)} {F(cy - radius * 0.82)} {F(cx - radius * 0.34)} {F(cy - radius * 0.64)} C {F(cx - radius * 0.12)} {F(cy - radius * 0.56)} {F(cx)} {F(cy - radius * 0.34)} {F(cx)} {F(cy - radius * 0.18)} C {F(cx)} {F(cy - radius * 0.34)} {F(cx + radius * 0.12)} {F(cy - radius * 0.56)} {F(cx + radius * 0.34)} {F(cy - radius * 0.64)} C {F(cx + radius * 0.9)} {F(cy - radius * 0.82)} {F(cx + radius * 1.12)} {F(cy - radius * 0.08)} {F(cx)} {F(cy + radius * 0.72)} Z";
}
