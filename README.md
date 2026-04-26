# ChartForgeX

Dependency-free chart rendering for .NET reports, dashboards, documentation, and static HTML output.

The goal is not to clone ScottPlot. The goal is to provide a beautiful, embeddable, no-JavaScript charting layer that HtmlForgeX, DomainDetective, TestimoX, GPOZaurr, ADEssentials, and OfficeIMO-style outputs can reuse.

## Design principles

- Zero runtime dependencies in the core package.
- SVG-first rendering for beautiful static HTML reports.
- PNG export with real alpha transparency.
- One chart model, multiple renderers.
- JavaScript-free by default.
- Optional interactive renderers can be added later without changing user code.
- Themes are first-class, especially dark/light report modes.

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
    .WithTheme(ChartTheme.Dark())
    .WithSize(1180, 640)
    .WithTransparentBackground(true)
    .AddArea("Passed", Points(820, 940, 980, 1040, 1120, 1180, 1230, 1260))
    .AddLine("Warnings", Points(120, 138, 132, 110, 98, 86, 72, 68), ChartColor.FromRgb(251, 191, 36))
    .AddLine("Failed", Points(22, 30, 28, 21, 18, 15, 13, 10), ChartColor.FromRgb(248, 113, 113));

chart.SaveSvg("chart.svg");
chart.SaveHtml("chart.html");
chart.SavePng("chart.png");

static IEnumerable<ChartPoint> Points(params double[] y) {
    for (var i = 0; i < y.Length; i++) yield return new ChartPoint(i + 1, y[i]);
}
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

PNG is supported as a dependency-free raster output. The current PNG renderer is intentionally simple and good enough for early report exports. SVG remains the source of truth for beauty.

JPG is not included. JPG has no transparency and is a poor fit for charts with sharp lines/text. If needed later, expose it as optional export by flattening PNG/SVG onto a solid background.

## Roadmap

### v0.1

- Line chart
- Area chart
- Scatter chart
- Bar chart
- SVG renderer
- HTML renderer
- Minimal PNG renderer
- Light/dark themes

### v0.2

- Donut/pie charts
- Gauge charts
- Sparklines
- Timeline charts
- Better axis tick generation
- Date/time axes
- Responsive HTML containers

### v0.3

- Heatmaps
- Stacked bars
- Multiple Y axes
- Data labels
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
