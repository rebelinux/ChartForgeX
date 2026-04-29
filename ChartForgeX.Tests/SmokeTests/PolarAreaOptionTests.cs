using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PolarAreaHonorsGridVisibility() {
        var compact = PolarAreaSample()
            .WithGrid(false)
            .ToSvg();

        Assert(compact.Contains("data-cfx-role=\"polar-area-segment\"", System.StringComparison.Ordinal), "Polar-area segments should still render when grid is disabled.");
        Assert(!compact.Contains("data-cfx-role=\"polar-area-ring\"", System.StringComparison.Ordinal), "Polar-area reference rings should hide when grid is disabled.");

        var defaultSvg = PolarAreaSample().ToSvg();
        Assert(defaultSvg.Contains("data-cfx-role=\"polar-area-ring\"", System.StringComparison.Ordinal), "Polar-area reference rings should render by default.");
        Assert(PolarAreaSample().WithGrid(false).ToPng().Length > 64, "Compact polar-area options should render valid PNG output.");
        var positionedLegend = PolarAreaSample().WithLegendPosition(ChartLegendPosition.BottomRight);
        var positionedSvg = positionedLegend.ToSvg();
        Assert(positionedSvg.Contains("data-cfx-role=\"slice-legend\" data-cfx-position=\"BottomRight\"", System.StringComparison.Ordinal), "Polar-area slice legends should honor configured legend placement.");
        Assert(positionedLegend.ToPng().Length > 64, "Polar-area positioned legends should render valid PNG output.");
    }

    private static Chart PolarAreaSample() => Chart.Create()
        .WithSize(520, 340)
        .WithXLabels("Coverage", "Policy", "Alerts", "Response")
        .AddPolarArea("Control share", Points(92, 74, 88, 96));
}
