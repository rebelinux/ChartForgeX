using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void WaterfallHonorsAxesVisibility() {
        var compact = WaterfallSample()
            .WithAxes(false)
            .ToSvg();

        Assert(compact.Contains("data-cfx-role=\"waterfall-bar\"", System.StringComparison.Ordinal), "Waterfall bars should still render when axes are disabled.");
        Assert(!compact.Contains("data-cfx-role=\"waterfall-x-axis-label\"", System.StringComparison.Ordinal), "Waterfall x-axis labels should hide when axes are disabled.");
        Assert(!compact.Contains("data-cfx-role=\"waterfall-y-axis-label\"", System.StringComparison.Ordinal), "Waterfall y-axis labels should hide when axes are disabled.");
        Assert(!compact.Contains("data-cfx-role=\"waterfall-x-axis-title\"", System.StringComparison.Ordinal), "Waterfall x-axis titles should hide when axes are disabled.");
        Assert(!compact.Contains("data-cfx-role=\"waterfall-zero-axis\"", System.StringComparison.Ordinal), "Waterfall zero axis should hide when axes are disabled.");

        var full = WaterfallSample().ToSvg();
        Assert(full.Contains("data-cfx-role=\"waterfall-x-axis-label\"", System.StringComparison.Ordinal), "Waterfall x-axis labels should render by default.");
        Assert(full.Contains("data-cfx-role=\"waterfall-zero-axis\"", System.StringComparison.Ordinal), "Waterfall zero axis should render by default when in range.");
        Assert(WaterfallSample().WithAxes(false).ToPng().Length > 64, "Compact waterfall options should render valid PNG output.");
    }

    private static Chart WaterfallSample() => Chart.Create()
        .WithSize(560, 320)
        .WithXAxis("Stage")
        .AddWaterfall("Delta", Points(18, -42, -12, 9));
}
