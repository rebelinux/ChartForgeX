using System;
using System.IO;
using System.Linq;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SourceFilesStayUnderArchitectureLineBudget() {
        const int lineBudget = 800;
        var root = FindRepositoryRoot();
        var oversized = new[] { "ChartForgeX", "ChartForgeX.Examples", "ChartForgeX.Tests" }
            .Where(sourceRoot => Directory.Exists(Path.Combine(root, sourceRoot)))
            .SelectMany(sourceRoot => Directory.EnumerateFiles(Path.Combine(root, sourceRoot), "*.cs", SearchOption.AllDirectories))
            .Where(file => !IsGeneratedPath(file))
            .Select(file => new { File = file, Lines = File.ReadLines(file).Count() })
            .Where(item => item.Lines > lineBudget)
            .Select(item => Path.GetRelativePath(root, item.File) + " (" + item.Lines.ToString(System.Globalization.CultureInfo.InvariantCulture) + " lines)")
            .ToArray();
        Assert(oversized.Length == 0, "Source files should stay under " + lineBudget.ToString(System.Globalization.CultureInfo.InvariantCulture) + " lines. Split: " + string.Join(", ", oversized));
    }

    private static void ProjectFilesKeepStrictBuildSettings() {
        var root = FindRepositoryRoot();
        var projectFiles = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories).Where(file => !IsGeneratedPath(file)).ToArray();
        var projectSettingFiles = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories).Where(IsProjectSettingFile).Where(file => !IsGeneratedPath(file)).ToArray();

        foreach (var file in projectSettingFiles) {
            Assert(!File.ReadAllText(file).Contains("<NoWarn", StringComparison.OrdinalIgnoreCase), "Project files should not suppress warnings with NoWarn: " + Path.GetRelativePath(root, file));
        }

        foreach (var projectFile in projectFiles) {
            Assert(HasXmlProperty(projectFile, "TreatWarningsAsErrors", "true"), "Project should treat warnings as errors: " + Path.GetRelativePath(root, projectFile));
        }

        var libraryProject = Path.Combine(root, "ChartForgeX", "ChartForgeX.csproj");
        Assert(HasXmlProperty(libraryProject, "GenerateDocumentationFile", "true"), "Library project should generate XML documentation.");
        var testProject = Path.Combine(root, "ChartForgeX.Tests", "ChartForgeX.Tests.csproj");
        Assert(HasXmlProperty(testProject, "IsTestProject", "true"), "Smoke suite should be discoverable by dotnet test.");

        foreach (var packageReference in GetXmlElements(libraryProject, "PackageReference")) {
            var include = packageReference.Attribute("Include")?.Value ?? string.Empty;
            var privateAssets = packageReference.Attribute("PrivateAssets")?.Value ?? string.Empty;
            var allowedBuildPackage = string.Equals(include, "Microsoft.NETFramework.ReferenceAssemblies.net472", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(privateAssets, "all", StringComparison.OrdinalIgnoreCase);
            Assert(allowedBuildPackage, "Runtime package dependencies are not allowed in the core library: " + include);
        }

        foreach (var projectFile in projectFiles.Where(file => !string.Equals(file, libraryProject, StringComparison.OrdinalIgnoreCase))) {
            foreach (var packageReference in GetXmlElements(projectFile, "PackageReference")) {
                var privateAssets = packageReference.Attribute("PrivateAssets")?.Value ?? string.Empty;
                Assert(string.Equals(privateAssets, "all", StringComparison.OrdinalIgnoreCase), "Non-library package references should stay private: " + Path.GetRelativePath(root, projectFile));
            }
        }
    }

    private static void NuGetPackageMetadataStaysPublishReady() {
        var libraryProject = Path.Combine(FindRepositoryRoot(), "ChartForgeX", "ChartForgeX.csproj");
        Assert(HasXmlProperty(libraryProject, "PackageId", "ChartForgeX"), "PackageId should remain stable.");
        Assert(HasXmlProperty(libraryProject, "PackageReadmeFile", "README.md"), "Package should include the README.");
        Assert(HasXmlProperty(libraryProject, "PackageProjectUrl", "https://github.com/EvotecIT/ChartForgeX"), "Package should expose the project URL.");
        Assert(HasXmlProperty(libraryProject, "RepositoryUrl", "https://github.com/EvotecIT/ChartForgeX"), "Package should expose the repository URL.");
        Assert(HasXmlProperty(libraryProject, "RepositoryType", "git"), "Package repository type should be git.");
        Assert(HasXmlProperty(libraryProject, "PublishRepositoryUrl", "true"), "Package should publish repository metadata.");
        Assert(HasXmlProperty(libraryProject, "Deterministic", "true"), "Package builds should be deterministic.");
        Assert(HasXmlProperty(libraryProject, "IncludeSymbols", "true"), "Package should include symbol package generation.");
        Assert(HasXmlProperty(libraryProject, "SymbolPackageFormat", "snupkg"), "Package symbols should use snupkg format.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "CHANGELOG.md")), "Repository should include a changelog.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "CONTRIBUTING.md")), "Repository should include contribution guidance.");
        Assert(File.Exists(Path.Combine(FindRepositoryRoot(), "RELEASING.md")), "Repository should include release guidance.");
        var tags = GetXmlValue(libraryProject, "PackageTags");
        foreach (var tag in new[] { "charts", "svg", "reports", "zero-dependency" }) {
            Assert(tags.Contains(tag, StringComparison.OrdinalIgnoreCase), "Package tags should include " + tag + ".");
        }
    }

    private static void ReadmeDocumentsChartCatalog() {
        var readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));
        Assert(readme.Contains("## Chart catalog", StringComparison.Ordinal), "README should include a chart catalog.");
        foreach (var api in new[] {
            "AddLine",
            "AddSmoothLine",
            "AddStepLine",
            "AddArea",
            "AddStepArea",
            "AddSmoothArea",
            "AddStackedArea",
            "AddSmoothStackedArea",
            "AddScatter",
            "AddTrendLine",
            "AddMeanLine",
            "AddMedianLine",
            "AddStandardDeviationBand",
            "AddSlope",
            "AddBarLineCombo",
            "AddColumnLineCombo",
            "AddBarAreaCombo",
            "AddColumnAreaCombo",
            "AddScatterLineCombo",
            "AddBar",
            "AddHistogram",
            "AddLollipop",
            "AddBubble",
            "AddErrorBar",
            "AddCandlestick",
            "AddOhlc",
            "AddRangeBand",
            "AddRangeArea",
            "AddDumbbell",
            "AddPareto",
            "AddRangeBar",
            "AddBoxPlot",
            "AddHorizontalBar",
            "WithStackedHorizontalBars",
            "AddHeatmapRow",
            "AddGauge",
            "AddCircle",
            "AddRadialBar",
            "AddBullet",
            "AddWaterfall",
            "AddRadar",
            "AddPolarArea",
            "AddFunnel",
            "AddTreemap",
            "AddTimelineItem",
            "AddTimelineRange",
            "AddGanttTask",
            "AddGanttMilestone",
            "WithGanttToday",
            "AddSankey",
            "ChartSankeyLink",
            "AddTree",
            "ChartTreeLink",
            "AddPie",
            "AddDonut"
        }) {
            Assert(readme.Contains("`" + api, StringComparison.Ordinal), "README chart catalog should document " + api + ".");
        }
    }

    private static void GitHubActionsUsePrivateRunners() {
        var workflowRoot = Path.Combine(FindRepositoryRoot(), ".github", "workflows");
        Assert(Directory.Exists(workflowRoot), "Repository should include GitHub Actions workflows.");
        var workflows = Directory.EnumerateFiles(workflowRoot, "*.yml", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(workflowRoot, "*.yaml", SearchOption.TopDirectoryOnly))
            .ToArray();
        Assert(workflows.Length > 0, "Repository should include at least one GitHub Actions workflow.");
        foreach (var workflow in workflows) {
            var text = File.ReadAllText(workflow);
            Assert(text.Contains("self-hosted", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should use self-hosted private runners: " + Path.GetFileName(workflow));
            Assert(text.Contains("private", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should require the private runner label: " + Path.GetFileName(workflow));
            Assert(text.Contains("actions/setup-dotnet", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should install the expected .NET SDK: " + Path.GetFileName(workflow));
            Assert(text.Contains("actions/upload-artifact", StringComparison.OrdinalIgnoreCase), "GitHub Actions workflows should preserve packages and gallery output: " + Path.GetFileName(workflow));
            Assert(!ContainsAny(text, "ubuntu-latest", "windows-latest", "macos-latest"), "GitHub Actions workflows should not use public hosted runner labels: " + Path.GetFileName(workflow));
        }
    }
}
