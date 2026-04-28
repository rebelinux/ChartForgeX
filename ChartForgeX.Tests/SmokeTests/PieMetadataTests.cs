using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void PieAndDonutSvgExposeSliceMetadata() {
        var pie = Chart.Create()
            .WithXLabels("Passed", "Failed")
            .AddPie("Results", Points(75, 25))
            .ToSvg();
        Assert(pie.Contains("data-cfx-role=\"pie-slice\" data-cfx-point=\"0\" data-cfx-label=\"Passed\" data-cfx-value=\"75\" data-cfx-percent=\"0.75\"", System.StringComparison.Ordinal), "Pie slices should expose label, value, and percent metadata.");

        var donut = Chart.Create()
            .WithXLabels("Passed", "Failed")
            .AddDonut("Results", Points(75, 25))
            .ToSvg();
        Assert(donut.Contains("data-cfx-role=\"donut-slice\" data-cfx-point=\"0\" data-cfx-label=\"Passed\" data-cfx-value=\"75\" data-cfx-percent=\"0.75\"", System.StringComparison.Ordinal), "Donut slices should expose label, value, and percent metadata.");
        Assert(donut.Contains("data-cfx-role=\"donut-total-label\"", System.StringComparison.Ordinal), "Donuts should expose center total role metadata.");
    }
}
