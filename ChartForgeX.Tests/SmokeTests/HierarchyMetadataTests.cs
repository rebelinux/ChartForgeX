using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void HierarchyAndFlowSvgExposeDataMetadata() {
        var treemap = Chart.Create().AddTreemap("Findings", new[] {
            new ChartTreemapItem("Critical", 50),
            new ChartTreemapItem("High", 28)
        }).ToSvg();
        Assert(treemap.Contains("data-cfx-role=\"treemap-tile\" data-cfx-point=\"0\" data-cfx-label=\"Critical\" data-cfx-value=\"50\"", System.StringComparison.Ordinal), "Treemap tiles should expose label and value metadata.");

        var tree = Chart.Create().AddTree("Hierarchy", new[] {
            new ChartTreeLink("Root", "Mail", 3),
            new ChartTreeLink("Mail", "SPF", 2)
        }).ToSvg();
        Assert(tree.Contains("data-cfx-role=\"tree-node\" data-cfx-node=\"0\" data-cfx-depth=\"0\" data-cfx-label=\"Root\"", System.StringComparison.Ordinal), "Tree nodes should expose label metadata.");
        Assert(tree.Contains("data-cfx-role=\"tree-link\" data-cfx-parent=\"0\" data-cfx-child=\"1\" data-cfx-value=\"3\" data-cfx-parent-label=\"Root\" data-cfx-child-label=\"Mail\"", System.StringComparison.Ordinal), "Tree links should expose endpoint label metadata.");

        var sankey = Chart.Create().AddSankey("Flow", new[] {
            new ChartSankeyLink("Discovered", "Validated", 70),
            new ChartSankeyLink("Validated", "Remediated", 44)
        }).ToSvg();
        Assert(sankey.Contains("data-cfx-role=\"sankey-node\" data-cfx-node=\"0\" data-cfx-layer=\"0\" data-cfx-label=\"Discovered\" data-cfx-value=\"70\"", System.StringComparison.Ordinal), "Sankey nodes should expose label and value metadata.");
        Assert(sankey.Contains("data-cfx-role=\"sankey-link\" data-cfx-source=\"0\" data-cfx-target=\"1\" data-cfx-value=\"70\" data-cfx-source-label=\"Discovered\" data-cfx-target-label=\"Validated\"", System.StringComparison.Ordinal), "Sankey links should expose endpoint label metadata.");
    }
}
