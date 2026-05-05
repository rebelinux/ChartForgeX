# ChartForgeX Topology

`ChartForgeX.Topology` is a reusable diagram family for deterministic, SVG-first topology views. It is intentionally product-neutral: ChartForgeX owns the model, validation, layout helpers, SVG rendering, and export-ready output; dashboard shells and data collection belong to host projects.

Use it for static or embeddable diagrams such as service maps, SQL/server dependency views, people/team relationship diagrams, network connectivity maps, replication meshes, geographic-style location views, and product-specific topology views supplied by host applications.

## Model

- `TopologyChart` contains viewport, layout mode, layout direction, groups, nodes, edges, legend, and theme.
- `TopologyGroup` represents a cluster, region, tier, team, location, or any other logical grouping.
- `TopologyNode` represents a logical object such as a server, database, service, endpoint, person, team, queue, network segment, or application.
- `TopologyEdge` represents a link, connection, dependency, replication path, trust, mapping, data flow, membership, or certificate chain.

Groups, nodes, and edges can carry `Href`, `Tooltip`, `CssClass`, `Metrics`, and `Metadata`. Edges can also carry explicit `Waypoints` for deterministic manual route bends around dense content. Nodes have an optional `Symbol` for short visual glyphs such as `SQL`, `API`, `DC`, `GC`, initials, or role abbreviations without baking those product roles into ChartForgeX enums. A node can override the chart-wide display mode with `DisplayMode` and can expose a short `Badge`, which is useful for collapsed clusters, counts, roles, or small site markers. The SVG renderer escapes text safely, emits native SVG `<title>` tooltips, emits stable `data-*` attributes, and wraps linked elements in SVG anchors. Unsafe `javascript:`, `data:`, and `vbscript:` hrefs are skipped.

When `TopologyRenderOptions.IncludeDataAttributes` is enabled, which is the default, SVG output emits sanitized `data-cfx-meta-*` and `data-cfx-metric-*` attributes for groups, nodes, and edges. Caller-provided `CssClass` tokens are sanitized and appended to the matching SVG element so host wrappers can target product-specific states without ChartForgeX needing product-specific enums.

Edge labels are planned deterministically from the edge midpoint, then nudged away from node boxes and previously placed labels where possible. SVG edge labels expose `data-label-x` and `data-label-y` so host wrappers can inspect or target final label placement without running their own layout logic.

Use `TopologyView` to render a focused perspective from the same source model. A view can select groups, nodes, or edges, override title/subtitle text, and keep connected edges between visible nodes. This is intended for dashboard cards such as "selected region", "critical paths", or "affected links" without duplicating topology data.

Views can also be composed from generic selectors instead of hardcoded ids. Use `NodeKinds`, `EdgeKinds`, and `HealthStatuses` to render product-neutral perspectives such as dependencies, connectivity, warning paths, or critical links. Use `TopologyView.AroundNode("api", depth: 1)` or `FocusNodeIds` with `NeighborDepth` to render a selected-node connectivity view; `IncludeIncomingEdges` and `IncludeOutgoingEdges` control traversal direction. Matching filtered edges keep their endpoints visible even when endpoint nodes have a different health status, which keeps views such as "critical links" readable.

`TopologyLegend.Default()` is intentionally product-neutral and only adds health-status entries. Add node-kind and edge-kind legend entries explicitly for the domain being rendered, for example service dependencies, transport links, replication paths, mappings, ownership, or team relationships. Legend entries can also use `symbol` overrides.

Set `TopologyRenderOptions.LegendMode` to `Auto`, `AutoWhenMissing`, or `Merge` when the renderer should infer legend items from the statuses, node kinds, node symbols, and edge kinds that are actually present in the chart.

## Layout

V1 layouts are deterministic and report-friendly:

- `Manual` uses explicit coordinates.
- `GroupGrid` arranges groups in a grid and places unpositioned nodes inside their groups.
- `HubAndSpoke` places a hub and branch nodes inside each group.
- `Layered` uses node kind or `Metadata["layer"]` for simple top-to-bottom or left-to-right layers.
- `Matrix` places nodes in a deterministic grid.

Set `TopologyChart.LayoutDirection` or use `.WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)` when a hierarchy should read horizontally, such as namespace -> service -> database, source -> processor -> sink, or selected object -> connected dependencies.

After layout, ChartForgeX runs a deterministic normalization pass. It keeps nodes out of group headers, separates overlapping sibling nodes, expands groups around their members, shifts negative or title-overlapping manual coordinates and edge waypoints into the report-safe area, and expands the viewport when content, explicit edge routes, or the legend would otherwise be clipped. This is a safety net for reusable chart builders; callers should still provide intentional coordinates when using `Manual`.

Force-directed and physics layouts are intentionally not implemented in v1.

## Rendering

```csharp
using ChartForgeX.Topology;

var chart = TopologyChart.Create()
    .WithId("service-map")
    .WithTitle("Service Dependency Map")
    .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
    .WithLegend(TopologyLegend.Default()
        .AddNodeKind("Service", TopologyNodeKind.Service, symbol: "API")
        .AddNodeKind("Database", TopologyNodeKind.Database, symbol: "SQL")
        .AddEdgeKind("Dependency", TopologyEdgeKind.Dependency))
    .AddNode("api", "API", 0, 0, TopologyNodeKind.Service, TopologyHealthStatus.Healthy, symbol: "API")
    .AddNode("database", "Database", 0, 0, TopologyNodeKind.Database, TopologyHealthStatus.Warning, symbol: "SQL")
    .AddEdge("api-database", "api", "database", "32 ms", TopologyEdgeKind.Dependency, TopologyHealthStatus.Warning, TopologyDirection.Forward);

chart.Nodes[0].Metadata["layer"] = "1";
chart.Nodes[1].Metadata["layer"] = "2";

var svg = chart.ToSvg();
chart.SaveSvg("service-map.svg");
chart.SaveHtml("service-map.html");
chart.SavePng("service-map.png");

var emeaOnly = chart.ToSvg(new TopologyRenderOptions {
    View = new TopologyView {
        Id = "emea",
        Title = "EMEA Topology",
        GroupIds = { "EMEA" }
    }
});

var selectedService = chart.ToSvg(new TopologyRenderOptions {
    View = TopologyView.AroundNode("api", depth: 1)
});

var criticalDependencies = chart.ToSvg(new TopologyRenderOptions {
    View = new TopologyView {
        EdgeKinds = { TopologyEdgeKind.Dependency },
        HealthStatuses = { TopologyHealthStatus.Critical }
    }
});

var denseSvg = chart.ToSvg(TopologyRenderOptions.FromPreset(TopologyViewPreset.Compact));
var offenderSvg = chart.ToSvg(TopologyRenderOptions.FromPreset(TopologyViewPreset.Offenders));

var metricSvg = chart.ToSvg(new TopologyRenderOptions {
    Preset = TopologyViewPreset.MetricLabels,
    EdgeLabelMetricKey = "lag",
    EdgeSecondaryLabelMetricKey = "queue",
    LegendMode = TopologyLegendMode.Merge
});
```

`TopologySvgRenderer` outputs a complete standalone SVG with `viewBox`, `defs`, scoped CSS, groups below edges, edge labels above edges, nodes above labels, status badges, optional legends, and accessibility metadata. `TopologyPngRenderer` draws the same model through ChartForgeX's dependency-free raster canvas for report exports. `TopologyHtmlRenderer` only wraps the generated SVG in a neutral `.cfx-topology-wrapper` div with chart metadata.

`TopologyRenderOptions` also supports reusable render presets and node display modes:

- `TopologyViewPreset.Grouped`, `Ungrouped`, `Connectivity`, `Dependency`, `Offenders`, `Compact`, and `MetricLabels` compose common static dashboard perspectives.
- `TopologyNodeDisplayMode.Card`, `CompactCard`, `Pill`, `Icon`, and `Dot` let the same model render as full report cards or dense dashboard summaries.
- Individual nodes can set `DisplayMode` or use `.WithNodeDisplay("node-id", TopologyNodeDisplayMode.Dot, badge: "+12")` so a single topology can mix full cards, compact markers, icons, and collapsed count badges.
- `EdgeLabelMetricKey` and `EdgeSecondaryLabelMetricKey` allow hosts to switch labels from default edge text to metrics such as `lag`, `queue`, `cost`, `owner`, or `lastSuccess`.

## Host Boundaries

HtmlForgeX can later provide cards, toolbars, sidebars, filters, tabs, inspectors, and event panels around the SVG. TestimoX or another product can later collect and calculate product-specific health, then convert that data into `TopologyChart`. ChartForgeX should not connect to Active Directory, hardcode TestimoX data, or implement dashboard page layout.

The example console app writes sample diagrams to `artifacts/topology-demo/` and to the normal generated example output folder. They intentionally use sample data only and demonstrate how a host can map its own product concepts onto the generic topology model:

- `site-topology.svg`
- `replication-mesh.svg`
- `subnets-site-links.svg`
- `geographic-topology.svg`
- `dc-connectivity.svg`
- `service-dependency.svg`
- `service-dependency-api-neighbors-view.svg`
- `service-dependency-critical-dependencies-view.svg`
- matching `.png` and `.html` files
- focused view examples for EMEA, selected-node neighbors, DC-level replication, offender highlighting, compact service dependencies, critical dependencies, and critical replication paths
- `index.html`
