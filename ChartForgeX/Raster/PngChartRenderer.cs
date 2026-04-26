using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed class PngChartRenderer {
    public byte[] Render(Chart chart) {
        var o = chart.Options; var t = o.Theme;
        var c = new RgbaCanvas(o.Size.Width, o.Size.Height);
        c.Clear(o.TransparentBackground ? ChartColor.Transparent : t.Background);
        if (t.UseCard) c.FillRect(14, 14, o.Size.Width - 28, o.Size.Height - 28, t.CardBackground);
        var plot = new ChartRect(o.Padding.Left, o.Padding.Top + 34, o.Size.Width - o.Padding.Left - o.Padding.Right, o.Size.Height - o.Padding.Top - o.Padding.Bottom - 34);
        c.FillRect(plot.X, plot.Y, plot.Width, plot.Height, t.PlotBackground);
        c.DrawTextTiny(40, 38, chart.Title, t.Text, 3);
        var range = ChartRange.FromChart(chart); var map = new ChartMapper(plot, range);
        for (var i = 0; i < o.TickCount; i++) {
            var f = i / (double)(o.TickCount - 1);
            var yv = range.MinY + (range.MaxY - range.MinY) * f; var y = map.Y(yv);
            c.DrawLine(plot.Left, y, plot.Right, y, t.Grid, 1);
            c.DrawTextTiny(12, y - 5, FormatNumber(yv), t.MutedText, 1);
        }
        c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis, 1);
        c.DrawLine(plot.Left, plot.Top, plot.Left, plot.Bottom, t.Axis, 1);
        for (var i = 0; i < chart.Series.Count; i++) DrawSeries(c, chart, i, plot, map);
        return PngWriter.WriteRgba(c.Width, c.Height, c.Pixels);
    }

    private static void DrawSeries(RgbaCanvas c, Chart chart, int index, ChartRect plot, ChartMapper map) {
        var s = chart.Series[index]; var color = s.Color ?? chart.Options.Theme.Palette[index % chart.Options.Theme.Palette.Length];
        if (s.Kind == ChartSeriesKind.Bar) {
            var barW = Math.Max(4, plot.Width / Math.Max(1, s.Points.Count) * .58);
            foreach (var p in s.Points) c.FillRect(map.X(p.X) - barW / 2, map.Y(p.Y), barW, plot.Bottom - map.Y(p.Y), color);
            return;
        }
        for (var i = 1; i < s.Points.Count; i++) {
            var a = s.Points[i - 1]; var b = s.Points[i];
            c.DrawLine(map.X(a.X), map.Y(a.Y), map.X(b.X), map.Y(b.Y), color, Math.Max(1, (int)Math.Round(s.StrokeWidth)));
        }
        if (s.Kind == ChartSeriesKind.Scatter || s.Kind == ChartSeriesKind.Line) foreach (var p in s.Points) c.DrawCircle(map.X(p.X), map.Y(p.Y), 4, color);
    }

    private static string FormatNumber(double v) => Math.Abs(v) >= 1000 ? (v / 1000).ToString("0.#") + "K" : v.ToString("0.#");
}
