using System;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed class SvgChartRenderer {
    public string Render(Chart chart) {
        var o = chart.Options;
        var t = o.Theme;
        var w = o.Size.Width;
        var h = o.Size.Height;
        var plot = new ChartRect(o.Padding.Left, o.Padding.Top + 34, w - o.Padding.Left - o.Padding.Right, h - o.Padding.Top - o.Padding.Bottom - 34);
        var range = ChartRange.FromChart(chart);
        var map = new ChartMapper(plot, range);
        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w}\" height=\"{h}\" viewBox=\"0 0 {w} {h}\" role=\"img\" aria-label=\"{Escape(chart.Title)}\">");
        sb.AppendLine("<defs>");
        sb.AppendLine("<filter id=\"softShadow\" x=\"-20%\" y=\"-20%\" width=\"140%\" height=\"140%\"><feDropShadow dx=\"0\" dy=\"14\" stdDeviation=\"18\" flood-opacity=\"0.18\"/></filter>");
        for (var i = 0; i < chart.Series.Count; i++) {
            var c = Color(chart, i);
            sb.AppendLine($"<linearGradient id=\"area{i}\" x1=\"0\" x2=\"0\" y1=\"0\" y2=\"1\"><stop offset=\"0%\" stop-color=\"{c.ToHex()}\" stop-opacity=\"0.32\"/><stop offset=\"100%\" stop-color=\"{c.ToHex()}\" stop-opacity=\"0.02\"/></linearGradient>");
        }
        sb.AppendLine("</defs>");
        if (!o.TransparentBackground && t.Background.A > 0) sb.AppendLine($"<rect width=\"100%\" height=\"100%\" fill=\"{t.Background.ToCss()}\"/>");
        if (t.UseCard) sb.AppendLine($"<rect x=\"14\" y=\"14\" width=\"{w-28}\" height=\"{h-28}\" rx=\"{t.CornerRadius}\" fill=\"{t.CardBackground.ToCss()}\" filter=\"url(#softShadow)\"/>");
        sb.AppendLine($"<rect x=\"{F(plot.X)}\" y=\"{F(plot.Y)}\" width=\"{F(plot.Width)}\" height=\"{F(plot.Height)}\" rx=\"18\" fill=\"{t.PlotBackground.ToCss()}\"/>");
        DrawHeader(sb, chart);
        DrawGrid(sb, chart, plot, range, map);
        for (var i = 0; i < chart.Series.Count; i++) DrawSeries(sb, chart, i, plot, range, map);
        DrawLegend(sb, chart, w, h);
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static void DrawHeader(StringBuilder sb, Chart chart) {
        var t = chart.Options.Theme;
        sb.AppendLine($"<text x=\"40\" y=\"52\" fill=\"{t.Text.ToCss()}\" font-family=\"{t.FontFamily}\" font-size=\"25\" font-weight=\"700\">{Escape(chart.Title)}</text>");
        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) sb.AppendLine($"<text x=\"40\" y=\"78\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{t.FontFamily}\" font-size=\"13\">{Escape(chart.Subtitle)}</text>");
    }

    private static void DrawGrid(StringBuilder sb, Chart chart, ChartRect plot, ChartRange range, ChartMapper map) {
        var o = chart.Options; var t = o.Theme; var ticks = Math.Max(2, o.TickCount);
        for (var i = 0; i < ticks; i++) {
            var f = i / (double)(ticks - 1);
            var yv = range.MinY + (range.MaxY - range.MinY) * f;
            var y = map.Y(yv);
            sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(y)}\" x2=\"{F(plot.Right)}\" y2=\"{F(y)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"1\"/>");
            sb.AppendLine($"<text x=\"{F(plot.Left-12)}\" y=\"{F(y+4)}\" text-anchor=\"end\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{t.FontFamily}\" font-size=\"11\">{FormatNumber(yv)}</text>");
        }
        sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(plot.Bottom)}\" x2=\"{F(plot.Right)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.2\"/>");
        sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(plot.Top)}\" x2=\"{F(plot.Left)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.2\"/>");
        if (!string.IsNullOrWhiteSpace(chart.XAxisTitle)) sb.AppendLine($"<text x=\"{F(plot.Left + plot.Width/2)}\" y=\"{F(plot.Bottom+42)}\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{t.FontFamily}\" font-size=\"12\">{Escape(chart.XAxisTitle)}</text>");
        if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) sb.AppendLine($"<text transform=\"translate(26 {F(plot.Top + plot.Height/2)}) rotate(-90)\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{t.FontFamily}\" font-size=\"12\">{Escape(chart.YAxisTitle)}</text>");
    }

    private static void DrawSeries(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartRange range, ChartMapper map) {
        var s = chart.Series[index]; var c = Color(chart, index); if (s.Points.Count == 0) return;
        if (s.Kind == ChartSeriesKind.Bar) { DrawBars(sb, chart, index, plot, range, map); return; }
        var points = string.Join(" ", s.Points.Select(p => $"{F(map.X(p.X))},{F(map.Y(p.Y))}"));
        if (s.Kind == ChartSeriesKind.Area) {
            var first = s.Points[0]; var last = s.Points[s.Points.Count - 1];
            var area = $"M {F(map.X(first.X))} {F(plot.Bottom)} L {points.Replace(",", " ")} L {F(map.X(last.X))} {F(plot.Bottom)} Z";
            sb.AppendLine($"<path d=\"{area}\" fill=\"url(#area{index})\"/>");
        }
        if (s.Kind == ChartSeriesKind.Scatter) {
            foreach (var p in s.Points) sb.AppendLine($"<circle cx=\"{F(map.X(p.X))}\" cy=\"{F(map.Y(p.Y))}\" r=\"4.5\" fill=\"{c.ToCss()}\" opacity=\"0.9\"/>");
        } else {
            sb.AppendLine($"<polyline fill=\"none\" stroke=\"{c.ToCss()}\" stroke-width=\"{F(s.StrokeWidth)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" points=\"{points}\"/>");
            foreach (var p in s.Points) sb.AppendLine($"<circle cx=\"{F(map.X(p.X))}\" cy=\"{F(map.Y(p.Y))}\" r=\"3.5\" fill=\"{c.ToCss()}\" stroke=\"{chart.Options.Theme.CardBackground.ToCss()}\" stroke-width=\"2\"/>");
        }
    }

    private static void DrawBars(StringBuilder sb, Chart chart, int index, ChartRect plot, ChartRange range, ChartMapper map) {
        var s = chart.Series[index]; var c = Color(chart, index); var count = Math.Max(1, s.Points.Count); var barW = Math.Max(4, plot.Width / count * .58);
        foreach (var p in s.Points) {
            var x = map.X(p.X) - barW / 2; var y = map.Y(p.Y); var height = plot.Bottom - y;
            sb.AppendLine($"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(barW)}\" height=\"{F(height)}\" rx=\"7\" fill=\"{c.ToCss()}\" opacity=\"0.9\"/>");
        }
    }

    private static void DrawLegend(StringBuilder sb, Chart chart, int w, int h) {
        if (!chart.Options.ShowLegend) return;
        var t = chart.Options.Theme; var x = 40.0; var y = h - 28.0;
        for (var i = 0; i < chart.Series.Count; i++) {
            var c = Color(chart, i);
            sb.AppendLine($"<circle cx=\"{F(x)}\" cy=\"{F(y-4)}\" r=\"5\" fill=\"{c.ToCss()}\"/>");
            sb.AppendLine($"<text x=\"{F(x+12)}\" y=\"{F(y)}\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{t.FontFamily}\" font-size=\"12\">{Escape(chart.Series[i].Name)}</text>");
            x += 34 + chart.Series[i].Name.Length * 7;
        }
    }

    private static ChartColor Color(Chart chart, int index) => chart.Series[index].Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    private static string FormatNumber(double v) => Math.Abs(v) >= 1000 ? v.ToString("0,.#k", CultureInfo.InvariantCulture) : v.ToString("0.##", CultureInfo.InvariantCulture);
    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
