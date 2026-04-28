using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void ComparisonAndIntervalSvgExposeDataMetadata() {
        var error = Chart.Create()
            .AddErrorBar("Confidence", new[] {
                new ChartErrorBar(1, 42, 35, 51),
                new ChartErrorBar(2, 58, 49, 66)
            })
            .ToSvg();
        Assert(error.Contains("data-cfx-role=\"error-bar\" data-cfx-series=\"0\" data-cfx-point=\"1\" data-cfx-x=\"2\" data-cfx-value=\"58\" data-cfx-lower=\"49\" data-cfx-upper=\"66\"", System.StringComparison.Ordinal), "Error bars should expose x, value, lower, and upper metadata.");

        var bubble = Chart.Create()
            .AddBubble("Reach", new[] {
                new ChartBubble(1, 42, 12),
                new ChartBubble(2, 58, 24)
            })
            .ToSvg();
        Assert(bubble.Contains("data-cfx-role=\"bubble\" data-cfx-series=\"0\" data-cfx-point=\"1\" data-cfx-x=\"2\" data-cfx-y=\"58\" data-cfx-size=\"24\"", System.StringComparison.Ordinal), "Bubbles should expose x, y, and size metadata.");

        var dumbbell = Chart.Create()
            .AddDumbbell("Before/after", new[] {
                new ChartDumbbell(1, 32, 44),
                new ChartDumbbell(2, 38, 58)
            })
            .ToSvg();
        Assert(dumbbell.Contains("data-cfx-role=\"dumbbell\" data-cfx-series=\"0\" data-cfx-point=\"1\" data-cfx-x=\"2\" data-cfx-start=\"38\" data-cfx-end=\"58\" data-cfx-delta=\"20\"", System.StringComparison.Ordinal), "Dumbbells should expose x, start, end, and delta metadata.");

        var rangeBand = Chart.Create()
            .AddRangeBand("Forecast", new[] {
                new ChartRangeBand(1, 32, 44),
                new ChartRangeBand(2, 38, 58),
                new ChartRangeBand(3, 51, 72)
            })
            .ToSvg();
        Assert(rangeBand.Contains("data-cfx-role=\"range-band\" data-cfx-series=\"0\" data-cfx-interval-count=\"3\"", System.StringComparison.Ordinal), "Range bands should expose interval count metadata.");

        var rangeArea = Chart.Create()
            .AddRangeArea("Prediction", new[] {
                new ChartRangeBand(1, 32, 44),
                new ChartRangeBand(2, 38, 58),
                new ChartRangeBand(3, 51, 72)
            })
            .ToSvg();
        Assert(rangeArea.Contains("data-cfx-role=\"range-area-series\" data-cfx-series=\"0\" data-cfx-interval-count=\"3\"", System.StringComparison.Ordinal), "Range areas should expose interval count metadata.");
    }
}
