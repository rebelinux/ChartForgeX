using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void HeatmapHonorsAxesVisibility() {
        var compact = HeatmapSample()
            .WithAxes(false)
            .WithLegend(false)
            .ToSvg();

        Assert(compact.Contains("data-cfx-role=\"heatmap-cell\"", System.StringComparison.Ordinal), "Compact heatmaps should still render cells.");
        Assert(!compact.Contains("data-cfx-role=\"heatmap-row-label\"", System.StringComparison.Ordinal), "Heatmaps should hide row labels when axes are disabled.");
        Assert(!compact.Contains("data-cfx-role=\"heatmap-column-label\"", System.StringComparison.Ordinal), "Heatmaps should hide column labels when axes are disabled.");
        Assert(!compact.Contains("data-cfx-role=\"heatmap-x-axis-title\"", System.StringComparison.Ordinal), "Heatmaps should hide x-axis titles when axes are disabled.");
        Assert(compact.Contains("data-cfx-role=\"heatmap-scale-label\"", System.StringComparison.Ordinal), "Heatmaps should keep scale labels as heatmap context.");

        var axesOnly = HeatmapSample().WithLegend(false).ToSvg();
        Assert(axesOnly.Contains("data-cfx-role=\"heatmap-row-label\"", System.StringComparison.Ordinal), "Heatmaps should keep axes when only legends are disabled.");
        Assert(axesOnly.Contains("data-cfx-role=\"heatmap-scale-label\"", System.StringComparison.Ordinal), "Heatmap scales should remain visible when series legends are disabled.");
        var withoutScale = HeatmapSample().WithHeatmapScaleLegend(false).ToSvg();
        Assert(!withoutScale.Contains("data-cfx-role=\"heatmap-scale-label\"", System.StringComparison.Ordinal), "Heatmap scale legends should be optional.");
        Assert(HeatmapSample().WithHeatmapScaleLegend(false).ToPng().Length > 64, "Heatmaps without scale legends should render valid PNG output.");
        var withoutColumns = HeatmapSample().WithHeatmapColumnLabels(false).ToSvg();
        Assert(withoutColumns.Contains("data-cfx-role=\"heatmap-row-label\"", System.StringComparison.Ordinal), "Heatmap row labels should remain visible when column labels are hidden.");
        Assert(!withoutColumns.Contains("data-cfx-role=\"heatmap-column-label\"", System.StringComparison.Ordinal), "Heatmap column labels should be optional.");
        Assert(HeatmapSample().WithHeatmapColumnLabels(false).ToPng().Length > 64, "Heatmaps without column labels should render valid PNG output.");
        Assert(HeatmapSample().WithAxes(false).WithLegend(false).ToPng().Length > 64, "Compact heatmap options should render valid PNG output.");
    }

    private static Chart HeatmapSample() => Chart.Create()
        .WithSize(520, 320)
        .WithXAxis("Control")
        .WithYAxis("Group")
        .WithXLabels("SPF", "DMARC")
        .AddHeatmapRow("Primary", Points(80, 70))
        .AddHeatmapRow("Acquired", Points(45, 60));
}
