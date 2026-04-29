using System;
using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void AxisVisibilityCanBeConfiguredIndependently() {
        var horizontal = Chart.Create()
            .WithSize(520, 320)
            .WithXAxis("Score")
            .WithXAxisVisible(false)
            .WithAxisLines(false)
            .WithLegend(false)
            .WithXLabels("Awareness", "Preference")
            .AddHorizontalBar("Metric", Points(82, 64));
        var horizontalSvg = horizontal.ToSvg();
        Assert(horizontal.Options.ShowAxes && !horizontal.Options.ShowXAxis && horizontal.Options.ShowYAxis && !horizontal.Options.ShowAxisLines, "Axis visibility should support hiding one axis while keeping the other and suppressing axis rules separately.");
        Assert(horizontalSvg.Contains(">Awareness</text>", StringComparison.Ordinal), "Hiding the x-axis should keep horizontal bar category labels visible.");
        Assert(!horizontalSvg.Contains(">Score</text>", StringComparison.Ordinal), "Hiding the x-axis should suppress x-axis titles.");
        Assert(horizontal.ToPng().Length > 64, "Independent axis visibility should render PNG output.");

        var vertical = Chart.Create()
            .WithSize(520, 320)
            .WithYAxis("Count")
            .WithYAxisVisible(false)
            .AddBar("Values", Points(12, 24));
        var verticalSvg = vertical.ToSvg();
        Assert(!verticalSvg.Contains(">Count</text>", StringComparison.Ordinal), "Hiding the y-axis should suppress y-axis titles.");
        Assert(verticalSvg.Contains("text-anchor=\"middle\"", StringComparison.Ordinal), "Hiding the y-axis should keep x-axis labels visible.");
        Assert(vertical.ToPng().Length > 64, "Independent y-axis visibility should render PNG output.");
    }
}
