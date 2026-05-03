using System;
using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SecondaryAxisBubbleRangeIgnoresBubbleSizes() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithSecondaryYAxis("Risk score")
            .AddBubble("Risk clusters", new[] {
                new ChartBubble(1, 18, 800),
                new ChartBubble(2, 34, 1200)
            });
        chart.Series[0].UseSecondaryYAxis();

        var svg = chart.ToSvg();

        Assert(!svg.Contains("data-cfx-role=\"secondary-y-axis-tick\" data-cfx-value=\"1200\"", StringComparison.Ordinal), "Secondary-axis bubble ticks should not include bubble sizes.");
        Assert(svg.Contains("data-cfx-role=\"secondary-y-axis-tick\" data-cfx-value=\"40\"", StringComparison.Ordinal) || svg.Contains("data-cfx-role=\"secondary-y-axis-tick\" data-cfx-value=\"50\"", StringComparison.Ordinal), "Secondary-axis bubble ticks should track bubble y values.");
        Assert(chart.ToPng().Length > 64, "Secondary-axis bubble charts should render PNG output.");
    }
}
