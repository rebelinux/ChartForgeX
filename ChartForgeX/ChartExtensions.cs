using System.IO;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Raster;
using ChartForgeX.Svg;

namespace ChartForgeX;

public static class ChartExtensions {
    public static string ToSvg(this Chart chart) => new SvgChartRenderer().Render(chart);
    public static string ToHtmlFragment(this Chart chart) => new HtmlChartRenderer().RenderFragment(chart);
    public static string ToHtmlPage(this Chart chart) => new HtmlChartRenderer().RenderPage(chart);
    public static byte[] ToPng(this Chart chart) => new PngChartRenderer().Render(chart);

    public static void SaveSvg(this Chart chart, string path) => File.WriteAllText(path, chart.ToSvg(), Encoding.UTF8);
    public static void SaveHtml(this Chart chart, string path) => File.WriteAllText(path, chart.ToHtmlPage(), Encoding.UTF8);
    public static void SavePng(this Chart chart, string path) => File.WriteAllBytes(path, chart.ToPng());
}
