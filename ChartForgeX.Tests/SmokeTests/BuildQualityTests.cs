using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void BuildScriptVerifiesReleaseArtifacts() {
        var buildScript = Path.Combine(FindRepositoryRoot(), "Build.ps1");
        var script = File.ReadAllText(buildScript);
        Assert(script.Contains("ChartForgeX.*.nupkg", StringComparison.Ordinal), "Build script should verify NuGet package creation.");
        Assert(script.Contains("ChartForgeX.*.snupkg", StringComparison.Ordinal), "Build script should verify symbol package creation.");
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
        Assert(script.Contains("$minimumChartPairs = 20", StringComparison.Ordinal), "Build script should enforce the current minimum example chart coverage.");
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

        foreach (var chart in charts) {
            var name = chart.GetProperty("name").GetString() ?? "<unknown>";
            var width = chart.GetProperty("width").GetInt32();
            var height = chart.GetProperty("height").GetInt32();
            var minVisualNodes = chart.GetProperty("svg").GetProperty("minVisualNodes").GetInt32();
            var minVisiblePixels = chart.GetProperty("png").GetProperty("minVisiblePixels").GetInt64();
            var minDistinctColors = chart.GetProperty("png").GetProperty("minDistinctColors").GetInt32();
            Assert(width >= 320 && height >= 90, "Visual baseline dimensions should describe real chart output: " + name + ".");
            Assert(minVisualNodes >= 2, "Visual baseline SVG visual-node minimum should be meaningful: " + name + ".");
            Assert(minVisiblePixels >= 64, "Visual baseline PNG visible-pixel minimum should be meaningful: " + name + ".");
            Assert(minDistinctColors >= 8, "Visual baseline PNG color-diversity minimum should be meaningful: " + name + ".");
        }

        var releaseGuide = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "RELEASING.md"));
        Assert(releaseGuide.Contains("-UpdateVisualBaseline", StringComparison.Ordinal), "Release guidance should explain intentional visual-baseline refreshes.");
    }
}
