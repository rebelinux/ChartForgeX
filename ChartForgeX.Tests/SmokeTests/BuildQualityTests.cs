using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void BuildScriptVerifiesReleaseArtifacts() {
        var buildScript = Path.Combine(FindRepositoryRoot(), "Build.ps1");
        var script = File.ReadAllText(buildScript);
        Assert(script.Contains("ChartForgeX*.nupkg", StringComparison.Ordinal), "Build script should verify NuGet package creation and clean copy-suffixed package artifacts.");
        Assert(script.Contains("ChartForgeX*.snupkg", StringComparison.Ordinal), "Build script should verify symbol package creation and clean copy-suffixed symbol artifacts.");
        Assert(script.Contains("Expected exactly one package", StringComparison.Ordinal), "Build script should reject stale or duplicate packages.");
        Assert(script.Contains("README.md", StringComparison.Ordinal), "Build script should verify README package inclusion.");
        Assert(script.Contains("CHANGELOG.md", StringComparison.Ordinal), "Build script should verify changelog package inclusion.");
        Assert(script.Contains("lib/$framework/ChartForgeX.$extension", StringComparison.Ordinal), "Build script should verify package framework assets.");
        Assert(script.Contains("ChartForgeX-package-consumer", StringComparison.Ordinal), "Build script should verify package consumption from a clean project.");
        Assert(script.Contains("dotnet add package ChartForgeX", StringComparison.Ordinal), "Build script should install the freshly packed package in the consumer smoke test.");
        Assert(script.Contains("<dependency\\s", StringComparison.Ordinal), "Build script should verify the core package has no runtime dependencies.");
        Assert(script.Contains("svg-png-comparison.json", StringComparison.Ordinal), "Build script should verify generated SVG/PNG comparison health.");
        Assert(script.Contains("function Assert-VisualComparisonHealth", StringComparison.Ordinal), "Build script should keep visual comparison health checks isolated.");
        Assert(script.Contains("function New-VisualBaseline", StringComparison.Ordinal), "Build script should keep visual-baseline generation isolated.");
        Assert(script.Contains("function Assert-VisualBaseline", StringComparison.Ordinal), "Build script should keep visual-baseline assertions isolated.");
        Assert(script.Contains("$minimumChartPairs = 50", StringComparison.Ordinal), "Build script should enforce the current minimum example chart coverage.");
        Assert(script.Contains("$Comparison.chartPairs -lt $minimumChartPairs", StringComparison.Ordinal), "Build script should fail when generated SVG/PNG comparison coverage drops.");
        Assert(script.Contains("$Comparison.dimensionMatches -ne $Comparison.chartPairs", StringComparison.Ordinal), "Build script should fail when SVG/PNG dimensions drift apart.");
        Assert(script.Contains("$Comparison.healthySvgs -ne $Comparison.chartPairs", StringComparison.Ordinal), "Build script should fail when SVG health drops.");
        Assert(script.Contains("$Comparison.healthyPngs -ne $Comparison.chartPairs", StringComparison.Ordinal), "Build script should fail when PNG health drops.");
        Assert(script.Contains("$Comparison.warnings -ne 0", StringComparison.Ordinal), "Build script should fail when generated SVG/PNG comparison warnings are present.");
        Assert(script.Contains("visual-baseline.json", StringComparison.Ordinal), "Build script should verify generated visuals against the checked-in baseline.");
        Assert(script.Contains("[switch] $UpdateVisualBaseline", StringComparison.Ordinal), "Build script should offer an intentional visual-baseline refresh switch.");
        Assert(script.Contains("Visual baseline updates require examples to run", StringComparison.Ordinal), "Build script should reject visual-baseline updates when examples are skipped.");
        Assert(script.Contains("ConvertTo-Json -Depth 8", StringComparison.Ordinal), "Build script should write refreshed visual baselines as JSON.");
        Assert(script.Contains("minDistinctColors = [int][Math]::Max", StringComparison.Ordinal), "Build script should keep refreshed PNG color-diversity baselines meaningful.");
        Assert(script.Contains("maxClippedTextNodes", StringComparison.Ordinal), "Build script should baseline SVG text-edge quality.");
        Assert(script.Contains("maxEdgeInkPixels", StringComparison.Ordinal), "Build script should baseline PNG edge-pressure quality.");
        Assert(script.Contains("outputScale = [int]$chart.png.scale", StringComparison.Ordinal), "Build script should record high-DPI PNG output scale in the visual baseline.");
        Assert(script.Contains("$generatedCharts.ContainsKey($expected.name)", StringComparison.Ordinal), "Build script should fail when a baseline chart is missing.");
        Assert(script.Contains("$baselineCharts.ContainsKey($actual.name)", StringComparison.Ordinal), "Build script should fail when a generated chart lacks baseline coverage.");
        Assert(script.Contains("$actual.svg.visualNodes -lt $expected.svg.minVisualNodes", StringComparison.Ordinal), "Build script should fail when SVG visual complexity drops below baseline.");
        Assert(script.Contains("$actual.png.visiblePixels -lt $expected.png.minVisiblePixels", StringComparison.Ordinal), "Build script should fail when PNG visible content drops below baseline.");
    }

    private static void VisualBaselineIsStructuredAndActionable() {
        var baselinePath = Path.Combine(FindRepositoryRoot(), "ChartForgeX.Examples", "visual-baseline.json");
        using var document = JsonDocument.Parse(File.ReadAllText(baselinePath));
        var root = document.RootElement;
        Assert(root.GetProperty("version").GetInt32() == 1, "Visual baseline should use schema version 1.");
        var charts = root.GetProperty("charts").EnumerateArray().ToArray();
        Assert(charts.Length >= 20, "Visual baseline should cover the full generated example set.");

        var names = charts.Select(chart => chart.GetProperty("name").GetString() ?? string.Empty).ToArray();
        Assert(names.All(name => !string.IsNullOrWhiteSpace(name)), "Visual baseline chart names should be non-empty.");
        Assert(names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Length, "Visual baseline chart names should be unique.");
        Assert(names.SequenceEqual(names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)), "Visual baseline chart names should stay sorted for reviewable diffs.");
        Assert(names.Contains("warnings-sparkline", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover compact sparkline charts.");
        Assert(names.Contains("domain-remediation-timeline-light", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover timeline charts.");
        Assert(names.Contains("security-posture-radar-dark", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover radar charts.");
        Assert(names.Contains("control-partition-sunburst-aurora", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover sunburst hierarchy charts.");
        Assert(names.Contains("audience-pictorial-candy", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover pictorial charts.");
        Assert(names.Contains("support-themes-word-cloud-editorial", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover word cloud charts.");
        Assert(names.Contains("theme-font-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover theme and font gallery output.");
        Assert(names.Contains("brand-kit-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover brand kit gallery output.");
        Assert(names.Contains("palette-swatch-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover palette gallery output.");
        Assert(names.Contains("pictorial-symbol-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover pictorial symbol gallery output.");
        Assert(names.Contains("pictorial-isotype-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover pictorial Isotype gallery output.");
        Assert(names.Contains("people-infographic-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover people infographic gallery output.");
        Assert(names.Contains("word-cloud-control-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover word cloud control gallery output.");
        Assert(names.Contains("point-color-customization-showcase-grid", StringComparer.OrdinalIgnoreCase), "Visual baseline should cover point color customization gallery output.");

        foreach (var chart in charts) {
            var name = chart.GetProperty("name").GetString() ?? "<unknown>";
            var width = chart.GetProperty("width").GetInt32();
            var height = chart.GetProperty("height").GetInt32();
            var minVisualNodes = chart.GetProperty("svg").GetProperty("minVisualNodes").GetInt32();
            var maxClippedTextNodes = chart.GetProperty("svg").GetProperty("maxClippedTextNodes").GetInt32();
            var minVisiblePixels = chart.GetProperty("png").GetProperty("minVisiblePixels").GetInt64();
            var minDistinctColors = chart.GetProperty("png").GetProperty("minDistinctColors").GetInt32();
            var outputScale = chart.GetProperty("png").GetProperty("outputScale").GetInt32();
            var maxEdgeInkPixels = chart.GetProperty("png").GetProperty("maxEdgeInkPixels").GetInt64();
            Assert(width >= 320 && height >= 90, "Visual baseline dimensions should describe real chart output: " + name + ".");
            Assert(minVisualNodes >= 2, "Visual baseline SVG visual-node minimum should be meaningful: " + name + ".");
            Assert(maxClippedTextNodes == 0, "Visual baseline should reject clipped SVG text: " + name + ".");
            Assert(outputScale >= 1 && outputScale <= 4, "Visual baseline PNG scale should be explicit: " + name + ".");
            Assert(minVisiblePixels >= 64, "Visual baseline PNG visible-pixel minimum should be meaningful: " + name + ".");
            Assert(minDistinctColors >= 8, "Visual baseline PNG color-diversity minimum should be meaningful: " + name + ".");
            Assert(maxEdgeInkPixels == 0, "Visual baseline should reject PNG edge pressure: " + name + ".");
        }

        var releaseGuide = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "RELEASING.md"));
        Assert(releaseGuide.Contains("-UpdateVisualBaseline", StringComparison.Ordinal), "Release guidance should explain intentional visual-baseline refreshes.");
        Assert(releaseGuide.Contains("clipped SVG text", StringComparison.Ordinal) && releaseGuide.Contains("PNG edge pressure", StringComparison.Ordinal), "Release guidance should explain the visual-baseline quality gates.");
    }
}
