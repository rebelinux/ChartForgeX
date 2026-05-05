using System;
using System.IO;
using System.Text;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Provides convenience rendering and export methods for topology charts.
/// </summary>
public static class TopologyChartExtensions {
    /// <summary>
    /// Sets the topology chart id.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The chart id.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithId(this TopologyChart chart, string id) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Id = id;
        return chart;
    }

    /// <summary>
    /// Sets the topology chart title.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="title">The chart title.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithTitle(this TopologyChart chart, string title) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Title = title;
        return chart;
    }

    /// <summary>
    /// Sets the topology chart subtitle.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="subtitle">The chart subtitle.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithSubtitle(this TopologyChart chart, string subtitle) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Subtitle = subtitle;
        return chart;
    }

    /// <summary>
    /// Sets the topology chart layout mode.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="layoutMode">The layout mode.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithLayout(this TopologyChart chart, TopologyLayoutMode layoutMode) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.LayoutMode = layoutMode;
        return chart;
    }

    /// <summary>
    /// Sets the topology chart layout mode and flow direction.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="layoutMode">The layout mode.</param>
    /// <param name="layoutDirection">The layout flow direction.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithLayout(this TopologyChart chart, TopologyLayoutMode layoutMode, TopologyLayoutDirection layoutDirection) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.LayoutMode = layoutMode;
        chart.LayoutDirection = layoutDirection;
        return chart;
    }

    /// <summary>
    /// Sets the topology chart layout flow direction.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="layoutDirection">The layout flow direction.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithLayoutDirection(this TopologyChart chart, TopologyLayoutDirection layoutDirection) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.LayoutDirection = layoutDirection;
        return chart;
    }

    /// <summary>
    /// Sets the topology viewport.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="width">The viewport width.</param>
    /// <param name="height">The viewport height.</param>
    /// <param name="padding">The viewport padding.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithViewport(this TopologyChart chart, double width, double height, double padding = 24) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Viewport = new TopologyViewport { Width = width, Height = height, Padding = padding };
        return chart;
    }

    /// <summary>
    /// Sets the topology legend.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="legend">The legend to use.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithLegend(this TopologyChart chart, TopologyLegend? legend) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Legend = legend;
        return chart;
    }

    /// <summary>
    /// Sets the topology theme.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="theme">The theme to use.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithTheme(this TopologyChart chart, TopologyTheme theme) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Theme = theme ?? throw new ArgumentNullException(nameof(theme));
        return chart;
    }

    /// <summary>
    /// Adds a group to the topology chart.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The group id.</param>
    /// <param name="label">The group label.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="width">The group width.</param>
    /// <param name="height">The group height.</param>
    /// <param name="status">The group status.</param>
    /// <param name="subtitle">The optional subtitle.</param>
    /// <param name="href">The optional href.</param>
    /// <param name="tooltip">The optional tooltip.</param>
    /// <param name="cssClass">The optional caller-provided CSS class tokens.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart AddGroup(this TopologyChart chart, string id, string label, double x, double y, double width, double height, TopologyHealthStatus status = TopologyHealthStatus.Unknown, string? subtitle = null, string? href = null, string? tooltip = null, string? cssClass = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Groups.Add(new TopologyGroup { Id = id, Label = label, X = x, Y = y, Width = width, Height = height, Status = status, Subtitle = subtitle, Href = href, Tooltip = tooltip, CssClass = cssClass });
        return chart;
    }

    /// <summary>
    /// Adds a node to the topology chart.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The node id.</param>
    /// <param name="label">The node label.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="kind">The node kind.</param>
    /// <param name="status">The node status.</param>
    /// <param name="groupId">The optional group id.</param>
    /// <param name="subtitle">The optional subtitle.</param>
    /// <param name="href">The optional href.</param>
    /// <param name="tooltip">The optional tooltip.</param>
    /// <param name="width">The node width.</param>
    /// <param name="height">The node height.</param>
    /// <param name="symbol">The optional short visual symbol shown inside the node icon.</param>
    /// <param name="cssClass">The optional caller-provided CSS class tokens.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart AddNode(this TopologyChart chart, string id, string label, double x, double y, TopologyNodeKind kind = TopologyNodeKind.Generic, TopologyHealthStatus status = TopologyHealthStatus.Unknown, string? groupId = null, string? subtitle = null, string? href = null, string? tooltip = null, double width = 120, double height = 64, string? symbol = null, string? cssClass = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Nodes.Add(new TopologyNode { Id = id, Label = label, X = x, Y = y, Kind = kind, Symbol = symbol, Status = status, GroupId = groupId, Subtitle = subtitle, Href = href, Tooltip = tooltip, Width = width, Height = height, CssClass = cssClass });
        return chart;
    }

    /// <summary>
    /// Sets a node-specific display mode and optional badge text.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="nodeId">The node id.</param>
    /// <param name="displayMode">The node display mode.</param>
    /// <param name="badge">Optional short badge text.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithNodeDisplay(this TopologyChart chart, string nodeId, TopologyNodeDisplayMode displayMode, string? badge = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        foreach (var node in chart.Nodes) {
            if (!string.Equals(node.Id, nodeId, StringComparison.Ordinal)) continue;
            node.DisplayMode = displayMode;
            node.Badge = badge;
            return chart;
        }

        throw new ArgumentException("Topology node '" + nodeId + "' was not found.", nameof(nodeId));
    }

    /// <summary>
    /// Sets optional short badge text on a topology node.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="nodeId">The node id.</param>
    /// <param name="badge">The badge text.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithNodeBadge(this TopologyChart chart, string nodeId, string? badge) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        foreach (var node in chart.Nodes) {
            if (!string.Equals(node.Id, nodeId, StringComparison.Ordinal)) continue;
            node.Badge = badge;
            return chart;
        }

        throw new ArgumentException("Topology node '" + nodeId + "' was not found.", nameof(nodeId));
    }

    /// <summary>
    /// Adds an edge to the topology chart.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The edge id.</param>
    /// <param name="sourceNodeId">The source node id.</param>
    /// <param name="targetNodeId">The target node id.</param>
    /// <param name="label">The optional edge label.</param>
    /// <param name="kind">The edge kind.</param>
    /// <param name="status">The edge status.</param>
    /// <param name="direction">The edge direction.</param>
    /// <param name="routing">The edge routing.</param>
    /// <param name="secondaryLabel">The optional secondary label.</param>
    /// <param name="href">The optional href.</param>
    /// <param name="tooltip">The optional tooltip.</param>
    /// <param name="cssClass">The optional caller-provided CSS class tokens.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart AddEdge(this TopologyChart chart, string id, string sourceNodeId, string targetNodeId, string? label = null, TopologyEdgeKind kind = TopologyEdgeKind.Generic, TopologyHealthStatus status = TopologyHealthStatus.Unknown, TopologyDirection direction = TopologyDirection.None, TopologyEdgeRouting routing = TopologyEdgeRouting.Orthogonal, string? secondaryLabel = null, string? href = null, string? tooltip = null, string? cssClass = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        chart.Edges.Add(new TopologyEdge { Id = id, SourceNodeId = sourceNodeId, TargetNodeId = targetNodeId, Label = label, Kind = kind, Status = status, Direction = direction, Routing = routing, SecondaryLabel = secondaryLabel, Href = href, Tooltip = tooltip, CssClass = cssClass });
        return chart;
    }

    /// <summary>
    /// Sets explicit route bend points on an edge.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="waypoints">The route bend points.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithEdgeWaypoints(this TopologyChart chart, string edgeId, params ChartPoint[] waypoints) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        foreach (var edge in chart.Edges) {
            if (!string.Equals(edge.Id, edgeId, StringComparison.Ordinal)) continue;
            edge.Waypoints.Clear();
            if (waypoints != null) edge.Waypoints.AddRange(waypoints);
            return chart;
        }

        throw new ArgumentException("Topology edge '" + edgeId + "' was not found.", nameof(edgeId));
    }

    /// <summary>
    /// Renders the topology chart to SVG.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>Complete SVG markup.</returns>
    public static string ToSvg(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologySvgRenderer().Render(chart, options);

    /// <summary>
    /// Renders the topology chart to an HTML fragment.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>An embeddable HTML fragment.</returns>
    public static string ToHtmlFragment(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyHtmlRenderer().RenderFragment(chart, options);

    /// <summary>
    /// Renders the topology chart to a complete HTML page.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A complete HTML page.</returns>
    public static string ToHtmlPage(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyHtmlRenderer().RenderPage(chart, options);

    /// <summary>
    /// Renders the topology chart to PNG bytes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A PNG image.</returns>
    public static byte[] ToPng(this TopologyChart chart, TopologyRenderOptions? options = null) => new TopologyPngRenderer().Render(chart, options);

    /// <summary>
    /// Saves the topology chart as SVG.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    public static void SaveSvg(this TopologyChart chart, string path, TopologyRenderOptions? options = null) => File.WriteAllText(path, chart.ToSvg(options), Encoding.UTF8);

    /// <summary>
    /// Saves the topology chart as a complete HTML page.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    public static void SaveHtml(this TopologyChart chart, string path, TopologyRenderOptions? options = null) => File.WriteAllText(path, chart.ToHtmlPage(options), Encoding.UTF8);

    /// <summary>
    /// Saves the topology chart as PNG.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="path">The output path.</param>
    /// <param name="options">Optional render options.</param>
    public static void SavePng(this TopologyChart chart, string path, TopologyRenderOptions? options = null) => File.WriteAllBytes(path, chart.ToPng(options));

    /// <summary>
    /// Configures the current topology theme.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="configure">The theme customization callback.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart WithTheme(this TopologyChart chart, Action<TopologyTheme> configure) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        chart.Theme ??= TopologyTheme.Light();
        configure(chart.Theme);
        return chart;
    }
}
