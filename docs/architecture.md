# Architecture

ChartForgeX uses a renderer-independent model.

```text
User code
  -> Chart / Series / Theme
  -> Layout and scale calculation
  -> Renderer
       -> SVG
       -> HTML
       -> PNG
       -> future PDF / Office / interactive HTML
```

## Core objects

- `Chart` - title, axes, options, and series collection.
- `ChartSeries` - data points and chart kind.
- `ChartTheme` - colors, typography, stroke sizes, visual mood.
- `SvgChartRenderer` - high quality static renderer.
- `HtmlChartRenderer` - wraps SVG in a fragment or standalone document.
- `PngChartRenderer` - dependency-free raster renderer.

## HtmlForgeX reuse

HtmlForgeX should treat a chart as a renderable component:

```csharp
Html.Div(section => {
    section.Raw(chart.ToHtmlFragment());
});
```

Later, this can become a first-class fluent API:

```csharp
page.Section(section => section
    .Header("Domain Security")
    .Chart(chart));
```

The important part is that HtmlForgeX should not know whether the chart is SVG, PNG, or interactive HTML. It should only know that `IHtmlRenderable` or equivalent can emit markup.
