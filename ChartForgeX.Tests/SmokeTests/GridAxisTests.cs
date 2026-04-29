using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SharedYAxisHandlesSecondaryOnlyCharts() {
        var secondaryOnlyA = Chart.Create().WithSize(300, 200).WithSecondaryYAxis("Rate").AddLine("Rate A", Points(82, 88, 94));
        var secondaryOnlyB = Chart.Create().WithSize(300, 200).WithSecondaryYAxis("Rate").AddLine("Rate B", Points(70, 76, 81));
        secondaryOnlyA.Series[0].UseSecondaryYAxis();
        secondaryOnlyB.Series[0].UseSecondaryYAxis();

        ChartGrid.Create().Add(secondaryOnlyA).Add(secondaryOnlyB).WithSharedYAxis();

        Assert(!secondaryOnlyA.Options.YAxisMinimum.HasValue && !secondaryOnlyB.Options.YAxisMinimum.HasValue, "Secondary-only shared y-axis grids should not apply fallback primary-axis bounds.");
        Assert(secondaryOnlyA.Options.SecondaryYAxisMinimum == secondaryOnlyB.Options.SecondaryYAxisMinimum && secondaryOnlyA.Options.SecondaryYAxisMaximum == secondaryOnlyB.Options.SecondaryYAxisMaximum, "Secondary-only shared y-axis grids should share secondary-axis bounds.");
        Assert(secondaryOnlyA.Options.SecondaryYAxisMaximum >= 94, "Secondary-only shared y-axis grids should include secondary-series values instead of the primary fallback range.");
    }
}
