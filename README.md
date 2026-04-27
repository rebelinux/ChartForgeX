# ChartForgeX

Dependency-free chart rendering for .NET reports, dashboards, documentation, and static HTML output.

The goal is not to clone ScottPlot. The goal is to provide a beautiful, embeddable, no-JavaScript charting layer that HtmlForgeX, DomainDetective, TestimoX, GPOZaurr, ADEssentials, and OfficeIMO-style outputs can reuse.

## Targets

ChartForgeX builds for `net472`, `netstandard2.0`, `net8.0`, and `net10.0`.

The library has no runtime package dependencies. The `net472` target uses `Microsoft.NETFramework.ReferenceAssemblies.net472` only as a private build-time reference so the project can compile on non-Windows machines.

## Install

```powershell
dotnet add package ChartForgeX
```

For local development from this repository, reference `ChartForgeX/ChartForgeX.csproj` directly and run the quality loop before publishing packages.

Contribution and release notes live in `CONTRIBUTING.md`, `RELEASING.md`, and `CHANGELOG.md`.
The dependency-free rendering quality direction is documented in `docs/dependency-free-quality-roadmap.md`.

## Quality gates

Warnings are treated as errors from day one, including XML documentation warnings for public APIs. The repository also includes a smoke test suite that verifies static SVG/HTML/PNG rendering behavior, keeps source files under the architecture line budget, rejects `NoWarn`, protects the core package from runtime package dependencies, and guards generated markup against scripts or external resources.

Run the smoke suite directly with:

```powershell
dotnet test .\ChartForgeX.Tests\ChartForgeX.Tests.csproj -c Release
```

Run the full local quality loop with:

```powershell
./Build.ps1
```

That restores, builds, runs smoke tests through `dotnet test`, regenerates example chart outputs and the static example gallery, packs the core library, creates a `.snupkg` symbol package, verifies package contents, verifies the package has no runtime NuGet dependencies, and installs the freshly packed package into a clean temporary console app. The generated demo entry points are `ChartForgeX.Examples/bin/Release/net8.0/output/index.html`, `ChartForgeX.Examples/bin/Release/net8.0/output/catalog.html`, `ChartForgeX.Examples/bin/Release/net8.0/output/quality-dashboard.html`, and `ChartForgeX.Examples/bin/Release/net8.0/output/svg-png-comparison.html`.

The GitHub Actions workflow is configured for private repositories and requires a self-hosted runner with the labels `self-hosted` and `private`.

## Design principles

- Zero runtime dependencies in the core package.
- SVG-first rendering for beautiful static HTML reports.
- PNG export with real alpha transparency.
- One chart model, multiple renderers.
- JavaScript-free by default.
- Public chart, option, theme, and primitive APIs fail fast on invalid values.
- Optional interactive renderers can be added later without changing user code.
- Themes are first-class, especially polished dark/light report modes.

## Current package layout

```text
ChartForgeX
├── ChartForgeX                 # library
│   ├── Core                    # chart model, series, options
│   ├── Primitives              # colors, points, rects, padding
│   ├── Themes                  # light/dark themes
│   ├── Svg                     # beautiful SVG renderer
│   ├── Html                    # standalone page and HTML fragment renderer
│   └── Raster                  # minimal PNG renderer and PNG writer
├── ChartForgeX.Examples         # sample console app
├── docs
└── Build.ps1
```

## Example

```csharp
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

var chart = Chart.Create()
    .WithTitle("Domain Security Checks")
    .WithSubtitle("Dependency-free SVG, HTML and PNG chart rendering")
    .WithXAxis("Run")
    .WithYAxis("Checks")
    .WithTheme(ChartTheme.ReportDark())
    .WithSize(1180, 640)
    .WithTransparentBackground(true)
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun", "Next")
    .AddSmoothArea("Passed", Points(820, 940, 980, 1040, 1120, 1180, 1230, 1260))
    .AddSmoothLine("Warnings", Points(120, 138, 132, 110, 98, 86, 72, 68), ChartColor.FromRgb(251, 191, 36))
    .AddSmoothLine("Failed", Points(22, 30, 28, 21, 18, 15, 13, 10), ChartColor.FromRgb(248, 113, 113))
    .AddHorizontalLine(100, "warning target", ChartColor.FromRgb(251, 191, 36))
    .AddVerticalBand(6, 7, "weekend", ChartColor.FromRgb(96, 165, 250), 0.10);

chart.SaveSvg("chart.svg");
chart.SaveHtml("chart.html");
chart.SavePng("chart.png");

static IEnumerable<ChartPoint> Points(params double[] y) {
    for (var i = 0; i < y.Length; i++) yield return new ChartPoint(i + 1, y[i]);
}
```

Small-multiple reports can compose several charts into one static, responsive HTML page:

```csharp
var report = ChartGrid.Create()
    .WithTitle("Control Scorecards")
    .WithSubtitle("Dependency-free small multiples for generated reports")
    .WithTheme(ChartTheme.ReportLight())
    .WithColumns(2)
    .WithPadding(32)
    .WithPanelSize(520, 320)
    .WithPanelFit(ChartGridPanelFit.Contain)
    .Add(gaugeChart)
    .Add(trendChart)
    .Add(coverageChart)
    .Add(findingsChart)
    .WithSharedAxes();

report.SaveHtml("scorecards.html");
report.SaveSvg("scorecards.svg");
report.SavePng("scorecards.png");
```

For a single chart, `WithXAxisBounds(minimum, maximum)` and `WithYAxisBounds(minimum, maximum)` pin cartesian axes explicitly. `WithAutomaticXAxisBounds()` and `WithAutomaticYAxisBounds()` return them to data-driven scaling.

Generated numeric x-axis ticks can be formatted independently from y-axis values and data labels:

```csharp
var timeline = Chart.Create()
    .WithXAxisBounds(0, 4)
    .WithXAxisValueFormatter(value => "D+" + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
    .AddLine("Incidents", new[] {
        new ChartPoint(0, 12),
        new ChartPoint(2, 18),
        new ChartPoint(4, 16)
    });
```

Data labels can be controlled globally or per series:

```csharp
var labels = Chart.Create()
    .WithDataLabels()
    .AddBar("Visible", Points(42))
    .AddLine("Hidden", Points(84));

labels.Series[1].WithDataLabels(false);
```

Bar-line combo charts add a bar series first and render the line on top:

```csharp
var combo = Chart.Create()
    .WithYAxis("Volume")
    .WithSecondaryYAxis("Pass rate", value => value.ToString("0") + "%")
    .WithSecondaryYAxisBounds(0, 100)
    .WithXLabels("Mon", "Tue", "Wed")
    .AddBarLineCombo(
        "Volume",
        Points(4200, 6400, 5800),
        "Pass rate",
        Points(88, 93, 91),
        smoothLine: true,
        lineAxis: ChartAxisSide.Secondary);
```

Column/area and scatter/line helpers are available for common mixed-report panels:

```csharp
var forecast = Chart.Create()
    .WithXLabels("Q1", "Q2", "Q3")
    .AddColumnAreaCombo("Actual", Points(42, 64, 58), "Projected", Points(46, 68, 62));

var observed = Chart.Create()
    .AddScatterLineCombo("Observed", Points(12, 18, 17), "Target", Points(14, 16, 20));
```

## Chart catalog

| Chart type | API | Good for |
| --- | --- | --- |
| Line | `AddLine`, `AddSmoothLine` | Trends, time series, thresholds |
| Step line | `AddStepLine` | State changes, threshold-like movement |
| Area | `AddArea`, `AddStepArea`, `AddSmoothArea`, `AddStackedArea`, `AddSmoothStackedArea` | Filled trends, step-filled states, stacked contribution bands, and cumulative-looking report areas |
| Scatter | `AddScatter`, `AddTrendLine` | Independent point distributions and fitted trend lines |
| Statistical overlays | `AddMeanLine`, `AddMedianLine`, `AddStandardDeviationBand` | Computed mean, median, and sigma report annotations |
| Slope | `AddSlope` | Before/after endpoint comparisons |
| Combo | `AddBarLineCombo`, `AddColumnLineCombo`, `AddBarAreaCombo`, `AddColumnAreaCombo`, `AddScatterLineCombo`, `UseSecondaryYAxis` | Shared-axis or secondary-axis mixed charts for volume, targets, forecasts, and observed-vs-trend panels |
| Bar | `AddBar` | Category comparison, grouped and stacked bars |
| Histogram | `AddHistogram` | Raw numeric distributions |
| Lollipop | `AddLollipop` | Light-weight category comparison |
| Bubble | `AddBubble` | X/Y points with a third size value |
| Error bar | `AddErrorBar` | Point estimates with lower and upper bounds |
| Candlestick / OHLC | `AddCandlestick`, `AddOhlc` | Open/high/low/close windows |
| Range band | `AddRangeBand` | Forecast, tolerance, and confidence envelopes |
| Range area | `AddRangeArea` | Smoothed prediction intervals with emphasized upper and lower bounds |
| Dumbbell | `AddDumbbell` | Before/after or paired-value comparisons |
| Pareto | `AddPareto` | Sorted contributions plus cumulative percentage |
| Range bar | `AddRangeBar` | Min/max or observed interval values |
| Box plot | `AddBoxPlot` | Quartiles, median, whiskers, raw sample summaries |
| Horizontal bar | `AddHorizontalBar`, `WithStackedHorizontalBars` | Long category names and stacked composition across long categories |
| Heatmap | `AddHeatmapRow` | Matrix values and coverage grids |
| Gauge | `AddGauge` | Single score or KPI |
| Circle | `AddCircle` | Compact single-value progress KPIs |
| Radial bar | `AddRadialBar` | Circular progress rings for multiple KPI percentages |
| Bullet | `AddBullet` | Value, target, and qualitative ranges |
| Waterfall | `AddWaterfall` | Cumulative positive and negative changes |
| Radar | `AddRadar` | Multi-axis profile comparison |
| Polar area | `AddPolarArea` | Equal-angle radial contribution comparison |
| Funnel | `AddFunnel` | Stage drop-off |
| Treemap | `AddTreemap` | Part-to-whole composition across many labeled items |
| Timeline | `AddTimelineItem`, `AddTimelineRange` | Date or numeric ranges |
| Gantt | `AddGanttTask`, `AddGanttMilestone`, `WithGanttToday` | Project schedules with progress, dependencies, milestones, and current-date markers |
| Sankey | `AddSankey`, `ChartSankeyLink` | Weighted flows between stages, systems, or outcomes |
| Tree | `AddTree`, `ChartTreeLink` | Hierarchies, ownership maps, and control structures |
| Pie and donut | `AddPie`, `AddDonut` | Simple proportional breakdowns |

PNG output uses a dependency-free rasterizer. When a platform TrueType font can be loaded, PNG text is rendered from real font outlines; otherwise ChartForgeX falls back to its built-in tiny chart font. You can prefer a specific `.ttf` file or `.ttc` collection for report consistency:

```csharp
chart.WithPngFont("/Library/Fonts/Arial.ttf")
     .SavePng("chart.png");
```

For TrueType collections, pass the optional zero-based face index when you need a specific face:

```csharp
chart.WithPngFont("/System/Library/Fonts/HelveticaNeue.ttc", collectionIndex: 0)
     .SavePng("chart.png");
```

You can also select by family, subfamily, full, or PostScript face name:

```csharp
chart.WithPngFont("/System/Library/Fonts/HelveticaNeue.ttc", faceName: "Helvetica Neue")
     .SavePng("chart.png");
```

PNG edges are supersampled by default. You can raise or lower the internal supersampling scale when you need to trade render time for raster edge quality:

```csharp
chart.WithPngSupersampling(4)
     .SavePng("chart.png");
```

SVG and HTML still use the theme `FontFamily` CSS stack. The PNG font path is optional and falls back automatically when the file is missing or unsupported.

Use `GetPngFontInfo()` when you want to verify which font source PNG rendering will use:

```csharp
var font = chart.GetPngFontInfo();
Console.WriteLine($"{font.Source}: {font.ResolvedFaceName}");
```

Date/time x-values can be used without manual numeric conversion:

```csharp
var dates = new[] {
    new DateTime(2026, 1, 1),
    new DateTime(2026, 1, 2),
    new DateTime(2026, 1, 3)
};

var dated = Chart.Create()
    .WithTitle("Daily Trend")
    .WithXDateLabels(dates, "MMM dd")
    .AddSmoothLine("Checks", dates.Select((date, index) => new ChartPoint(date, 100 + index * 20)));
```

Compact sparklines are available for dashboard cards and table cells:

```csharp
var sparkline = Chart.Create()
    .WithSize(360, 90)
    .WithSparkline()
    .AddSmoothArea("Warnings", Points(120, 138, 132, 110, 98, 86, 72, 68), ChartColor.FromRgb(251, 191, 36));
```

Dense report axes can be kept readable with automatic x-axis label thinning:

```csharp
var monthly = Chart.Create()
    .WithTitle("Monthly Security Posture")
    .WithXAxisLabelDensity(ChartLabelDensity.Auto)
    .WithXLabels("January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December")
    .AddSmoothLine("Primary domain checks", Points(82, 84, 86, 87, 88, 89, 91, 92, 93, 94, 95, 96));
```

Step line charts are available for state changes and threshold-like report series:

```csharp
var stateChanges = Chart.Create()
    .WithTitle("Policy State Changes")
    .WithXLabels("Draft", "Review", "Approved", "Published")
    .AddStepLine("Controls", Points(12, 18, 18, 26));
```

Step-area charts fill discrete state changes when the magnitude matters:

```csharp
var stepArea = Chart.Create()
    .WithTitle("Policy Backlog")
    .WithXLabels("Draft", "Review", "Approved", "Published")
    .AddStepArea("Open controls", Points(12, 18, 18, 26));
```

Long category labels can be rotated when every label matters:

```csharp
var regional = Chart.Create()
    .WithTitle("Certificate Transparency by Region")
    .WithXAxisLabelDensity(ChartLabelDensity.All)
    .WithXAxisLabelAngle(-35)
    .WithXLabels("North America", "Western Europe", "Central Europe", "Asia Pacific", "Latin America", "Middle East")
    .AddBar("Logged certificates", Points(1200000, 2350000, 1840000, 3120000, 980000, 760000));
```

Multiple bar series render as grouped category bars:

```csharp
var grouped = Chart.Create()
    .WithTitle("Security Findings by Severity")
    .WithXLabels("Critical", "High", "Medium", "Low", "Informational")
    .AddBar("Current run", Points(8, 32, 84, 126, 210))
    .AddBar("Previous run", Points(12, 41, 97, 118, 188));
```

Histograms can bin raw values directly:

```csharp
var latencyDistribution = Chart.Create()
    .WithTitle("Endpoint Latency Distribution")
    .WithXAxis("Latency")
    .WithYAxis("Samples")
    .AddHistogram("P95 samples", new[] { 28d, 31d, 36d, 42d, 47d, 58d, 64d, 72d }, binCount: 4);
```

Lollipop charts provide bar-like comparison with lighter visual weight:

```csharp
var posture = Chart.Create()
    .WithTitle("Control Readiness")
    .WithDataLabels()
    .WithXLabels("SPF", "DMARC", "DNSSEC")
    .AddLollipop("Coverage", Points(96, 88, 74));
```

Bubble charts encode x, y, and marker size in one report panel:

```csharp
var exposure = Chart.Create()
    .WithTitle("Exposure Clusters")
    .WithXAxis("Reachability")
    .WithYAxis("Exploitability")
    .AddBubble("Assets", new[] {
        new ChartBubble(1, 24, 8),
        new ChartBubble(2, 42, 18),
        new ChartBubble(3, 31, 42)
    });
```

Trend lines can be computed from scatter or other cartesian points:

```csharp
var observations = Points(12, 19, 24, 33, 41);
var trend = Chart.Create()
    .WithTitle("Observed Remediation Trend")
    .AddScatter("Observed", observations)
    .AddTrendLine("Trend", observations);
```

Statistical overlays compute report annotations from source points:

```csharp
var observed = Points(12, 19, 24, 33, 41);
var stats = Chart.Create()
    .AddLine("Observed", observed)
    .AddMeanLine("mean", observed)
    .AddMedianLine("median", observed)
    .AddStandardDeviationBand("1 sigma", observed);
```

Error bars show uncertainty around point estimates:

```csharp
var confidence = Chart.Create()
    .WithTitle("Detection Confidence")
    .WithXAxis("Run")
    .WithYAxis("Score")
    .AddErrorBar("Confidence", new[] {
        new ChartErrorBar(1, 42, 35, 51),
        new ChartErrorBar(2, 58, 49, 66),
        new ChartErrorBar(3, 63, 54, 78)
    });
```

Candlestick charts show open, high, low, and close windows:

```csharp
var candles = Chart.Create()
    .WithTitle("Signal Windows")
    .WithXAxis("Window")
    .WithYAxis("Score")
    .AddCandlestick("Window", new[] {
        new ChartCandlestick(1, 42, 51, 35, 48),
        new ChartCandlestick(2, 58, 66, 49, 54),
        new ChartCandlestick(3, 63, 78, 54, 72)
    });
```

OHLC charts use the same `ChartCandlestick` values with compact open and close ticks:

```csharp
var ohlc = Chart.Create()
    .WithTitle("Signal Windows")
    .WithXAxis("Window")
    .WithYAxis("Score")
    .AddOhlc("Window", new[] {
        new ChartCandlestick(1, 42, 51, 35, 48),
        new ChartCandlestick(2, 58, 66, 49, 54),
        new ChartCandlestick(3, 63, 78, 54, 72)
    });
```

Range-band charts draw forecast, tolerance, or confidence envelopes:

```csharp
var forecast = Chart.Create()
    .WithTitle("Forecast Envelope")
    .WithXAxis("Run")
    .WithYAxis("Expected Range")
    .AddRangeBand("Expected", new[] {
        new ChartRangeBand(1, 32, 44),
        new ChartRangeBand(2, 38, 58),
        new ChartRangeBand(3, 51, 72)
    });
```

Range-area charts are interval envelopes with stronger area emphasis and optional smoothing:

```csharp
var interval = Chart.Create()
    .WithTitle("Forecast Interval")
    .WithXAxis("Run")
    .WithYAxis("Expected Range")
    .AddRangeArea("Prediction interval", new[] {
        new ChartRangeBand(1, 32, 44),
        new ChartRangeBand(2, 38, 58),
        new ChartRangeBand(3, 51, 72)
    });
```

Dumbbell charts compare paired values across categories:

```csharp
var remediation = Chart.Create()
    .WithTitle("Remediation Lift")
    .WithXLabels("SPF", "DMARC", "DNSSEC")
    .AddDumbbell("Before/after", new[] {
        new ChartDumbbell(1, 32, 44),
        new ChartDumbbell(2, 38, 58),
        new ChartDumbbell(3, 51, 72)
    });
```

Pareto charts sort raw category values and add a cumulative percentage line:

```csharp
var pareto = Chart.Create()
    .WithTitle("Findings Pareto")
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddPareto("Findings", new[] {
        new ChartParetoItem("Critical", 50),
        new ChartParetoItem("Medium", 30),
        new ChartParetoItem("Low", 20)
    });
```

Range bars show interval values such as min/max observations:

```csharp
var latencyRanges = Chart.Create()
    .WithTitle("Endpoint Latency Ranges")
    .WithXLabels("DNS", "TCP", "TLS")
    .AddRangeBar("Observed", new[] {
        new ChartInterval(1, 20, 42),
        new ChartInterval(2, 44, 88),
        new ChartInterval(3, 96, 142)
    });
```

Box plots summarize distributions with whiskers, quartiles, and medians:

```csharp
var latencySpread = Chart.Create()
    .WithTitle("Endpoint Latency Spread")
    .WithXLabels("DNS", "TCP")
    .AddBoxPlot("Latency", new[] {
        new ChartBoxPlot(1, 18, 24, 31, 38, 48),
        new ChartBoxPlot(2, 42, 56, 64, 82, 104)
    });
```

Box plots can also compute the five-number summary from raw samples:

```csharp
var rawLatency = Chart.Create()
    .WithTitle("Raw Latency Spread")
    .AddBoxPlot("Latency", 1, new[] { 18d, 24d, 31d, 38d, 48d });
```

Horizontal bars are available for long report categories:

```csharp
var coverage = Chart.Create()
    .WithTitle("Domain Control Coverage")
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF alignment", "DMARC policy enforcement", "DNSSEC coverage")
    .AddHorizontalBar("Coverage", Points(96, 88, 74));
```

Multiple horizontal bar series can be stacked for composition reports:

```csharp
var composition = Chart.Create()
    .WithStackedHorizontalBars()
    .WithStackTotals()
    .WithXLabels("Mail authentication", "Transport security")
    .AddHorizontalBar("Complete", Points(40, 55))
    .AddHorizontalBar("Partial", Points(15, 20))
    .AddHorizontalBar("Missing", Points(5, 10));
```

Heatmaps are available for compact report matrices. Each heatmap series is one row; point `X` chooses the column and point `Y` is the cell value:

```csharp
var matrix = Chart.Create()
    .WithTitle("Control Coverage Matrix")
    .WithDataLabels()
    .WithHeatmapScale(ChartHeatmapScale.Semantic)
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("SPF", "DMARC", "DNSSEC")
    .AddHeatmapRow("Primary domains", Points(96, 88, 74))
    .AddHeatmapRow("Parked domains", Points(74, 62, 51));
```

Gauges are available for single-value dashboard summaries:

```csharp
var score = Chart.Create()
    .WithTitle("Security Posture Score")
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddGauge("Overall domain readiness", 87, 0, 100);
```

Bullet charts are available when a report needs value-versus-target rows with qualitative bands:

```csharp
var targets = Chart.Create()
    .WithTitle("Control Targets")
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .AddBullet("DMARC enforcement", 88, 95, 0, 100, new[] { 60d, 80d })
    .AddBullet("DNSSEC coverage", 74, 90, 0, 100, new[] { 55d, 78d });
```

Circle charts show compact single-value progress KPIs:

```csharp
var ready = Chart.Create()
    .WithTitle("Policy Readiness")
    .AddCircle("Ready", 87, 0, 100);
```

Waterfall charts are available for explaining cumulative change:

```csharp
var impact = Chart.Create()
    .WithTitle("Remediation Impact")
    .WithDataLabels()
    .WithXLabels("Opened", "Resolved", "Suppressed", "Accepted")
    .AddWaterfall("Finding delta", Points(24, -68, -18, -9));
```

Radar charts are available for posture and scorecard comparisons:

```csharp
var posture = Chart.Create()
    .WithTitle("Security Posture Radar")
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
    .WithXLabels("Mail auth", "DNSSEC", "TLS", "CT", "Policy", "Monitoring")
    .AddRadar("Current posture", Points(92, 74, 88, 96, 81, 84))
    .AddRadar("Target posture", Points(96, 90, 94, 98, 92, 90));
```

Polar area charts use equal-angle radial segments whose radius is scaled by value:

```csharp
var polar = Chart.Create()
    .WithTitle("Control Contribution")
    .WithXLabels("Mail auth", "DNSSEC", "TLS", "CT")
    .AddPolarArea("Control share", Points(92, 74, 88, 96));
```

Funnel charts are available for staged report flows:

```csharp
var funnel = Chart.Create()
    .WithTitle("Domain Remediation Funnel")
    .WithXLabels("Discovered", "Verified", "Prioritized", "Remediated")
    .AddFunnel("Domains", Points(420, 318, 174, 96));
```

Timeline charts render date or numeric ranges:

```csharp
var remediation = Chart.Create()
    .WithTitle("Domain Remediation Timeline")
    .WithDataLabels()
    .AddTimelineItem("Certificate renewal", new DateTime(2026, 1, 4), new DateTime(2026, 2, 10))
    .AddTimelineItem("DMARC enforcement", new DateTime(2026, 1, 18), new DateTime(2026, 3, 5));
```

Gantt charts add schedule progress, dependencies, milestones, and current-date markers:

```csharp
var plan = Chart.Create()
    .WithTitle("Domain Remediation Gantt")
    .WithDataLabels()
    .WithGanttToday(new DateTime(2026, 2, 18))
    .AddGanttTask("Inventory scope", new DateTime(2026, 1, 5), new DateTime(2026, 1, 24), 0.9)
    .AddGanttTask("Owner remediation", new DateTime(2026, 1, 20), new DateTime(2026, 2, 24), 0.62, dependsOn: 0)
    .AddGanttMilestone("Executive sign-off", new DateTime(2026, 3, 6), dependsOn: 1);
```

Sankey charts show weighted movement between named stages:

```csharp
var flow = Chart.Create()
    .WithTitle("Finding Flow")
    .WithDataLabels()
    .AddSankey("Findings", new[] {
        new ChartSankeyLink("Discovered", "Validated", 70),
        new ChartSankeyLink("Validated", "Remediated", 44),
        new ChartSankeyLink("Validated", "Monitoring", 26)
    });
```

Tree charts render parent-child hierarchy maps:

```csharp
var hierarchy = Chart.Create()
    .WithTitle("Control Hierarchy")
    .AddTree("Controls", new[] {
        new ChartTreeLink("Security posture", "Mail authentication"),
        new ChartTreeLink("Security posture", "Certificate lifecycle"),
        new ChartTreeLink("Mail authentication", "SPF"),
        new ChartTreeLink("Mail authentication", "DKIM")
    });
```

They can also be stacked when the report needs category totals:

```csharp
var stacked = Chart.Create()
    .WithTitle("Domain Findings Composition")
    .WithStackedBars()
    .WithStackTotals()
    .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri")
    .AddBar("Passed", Points(180, 220, 245, 260, 280))
    .AddBar("Warnings", Points(42, 38, 32, 28, 24))
    .AddBar("Failed", Points(12, 10, 8, 6, 5));
```

Report-specific units can be applied with a custom value formatter:

```csharp
var latency = Chart.Create()
    .WithTitle("Endpoint Latency")
    .WithDataLabels()
    .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms")
    .WithXLabels("DNS", "TCP", "TLS", "HTTP", "Render")
    .AddSmoothLine("P95", Points(28, 64, 118, 146, 182));
```

SVG layout reserves extra plot space when formatted y-axis labels are long, so currency and unit-heavy labels do not spill into the card margin.

Typography defaults to a native system font stack for crisp SVG output. Reports can override it per chart:

```csharp
chart.WithFontFamily("-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif");
```

Pie and donut charts use the same model and x-axis labels for slice names:

```csharp
var donut = Chart.Create()
    .WithTitle("Domain Check Result Mix")
    .WithXLabels("Passed", "Warnings", "Failed")
    .AddDonut("Checks", Points(1260, 68, 10));
```

## HtmlForgeX integration model

HtmlForgeX should not depend on ApexCharts or Chart.js for static reports. Instead, it can consume ChartForgeX objects directly:

```csharp
var chartHtml = chart.ToHtmlFragment();
```

That gives HtmlForgeX:

- inline SVG charts;
- no external CDN;
- no JavaScript requirement;
- no runtime chart library payload;
- easy email/report/document embedding;
- deterministic output for screenshots and PDFs.

For interactive dashboards, add a separate package later:

```text
ChartForgeX.Interactive.Html
```

That package may optionally emit small vanilla JS for hover, tooltip, zoom, selection, and data toggles. It should not affect the static renderer.

## Why not Chart.js / ApexCharts?

Chart.js and ApexCharts are good interactive web libraries, but for generated HTML reports they create several problems:

- JavaScript dependency;
- CDN/local asset handling;
- harder offline mode;
- harder email embedding;
- harder deterministic rendering;
- heavier reports;
- more moving parts for SharePoint/static hosting.

ChartForgeX should replace them for static report visuals. Keep JavaScript charting only where interaction is genuinely required.

## PNG/JPG approach

PNG is supported as a dependency-free raster output. The current PNG renderer is intentionally simple, but it now shares the important report-layout rules: title/subtitle headers, scaled cartesian legends, rounded theme surfaces and borders, explicit x-axis label density, edge-aware tick labels, long formatted y-axis label space, axis titles, supersampled edges, compressed output, and alpha transparency. SVG remains the source of truth for beauty, typography, gradients, annotation labels, and report-grade visual polish.

JPG is not included. JPG has no transparency and is a poor fit for charts with sharp lines/text. If needed later, expose it as optional export by flattening PNG/SVG onto a solid background.

## Roadmap

### v0.1

- Line chart
- Step line chart
- Area chart
- Stacked area chart
- Scatter chart
- Slope chart
- Bar chart
- Histogram chart
- Lollipop chart
- Bubble chart
- Error-bar chart
- Candlestick chart
- OHLC chart
- Range-band chart
- Dumbbell chart
- Pareto chart helper
- Range bar chart
- Box plot chart
- Horizontal bar chart
- Pie and donut charts
- Static line and band annotations
- SVG renderer
- HTML renderer
- Minimal PNG renderer
- Light/dark themes
- Report theme presets and visual styling tokens
- Automatic x-axis label density for crowded reports
- Rotated x-axis labels for long categories
- Wrapped SVG legends
- Grouped SVG bar rendering for multiple bar series
- Stacked bar rendering for category totals
- Horizontal bar rendering for long categories
- Step line, histogram, lollipop, range bar, range band, dumbbell, and slope charts
- Bubble charts, trend lines, statistical overlays, and error bars
- Box plots, Pareto charts, candlestick charts, and OHLC charts
- Heatmap matrices
- Gauge charts
- Bullet charts
- Waterfall charts
- Radar charts
- Polar area charts
- Funnel charts
- Treemap charts
- Timeline charts
- Gantt charts
- Sankey charts
- Tree charts
- Sparklines
- Date/time axes
- Data labels

### v0.2

- Responsive HTML containers

### v0.3

- Multiple Y axes
- Annotation lines/ranges
- Better PNG antialiasing
- Embedded vector font or compact Hershey-style text

### v1.0

- Stable public API
- HtmlForgeX integration package
- OfficeIMO embedding helpers
- Report theme presets
- Snapshot tests
- Benchmarks
