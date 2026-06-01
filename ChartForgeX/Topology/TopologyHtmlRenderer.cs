using System;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Html;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Renders topology charts into simple HTML wrappers with inline SVG.
/// </summary>
public sealed partial class TopologyHtmlRenderer {
    private const string DefaultCssClassPrefix = "cfx-topology";
    private readonly TopologySvgRenderer _svg = new();

    /// <summary>
    /// Renders an embeddable HTML fragment.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>An HTML fragment.</returns>
    public string RenderFragment(TopologyChart chart, TopologyRenderOptions? options = null) {
        return RenderFragmentCore(chart, options, includeAssets: true);
    }

    private string RenderFragmentCore(TopologyChart chart, TopologyRenderOptions? options, bool includeAssets) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyRenderOptions();
        var id = string.IsNullOrWhiteSpace(chart.Id) ? "topology" : chart.Id!;
        if (options.View != null && !string.IsNullOrWhiteSpace(options.View.Id)) id += "-" + options.View.Id;
        var theme = chart.Theme ?? TopologyTheme.Light();
        var cssPrefix = CssClassPrefix(options);
        var wrapperClass = cssPrefix + "-wrapper";
        var viewportClass = cssPrefix + "-viewport";
        var controlsClass = cssPrefix + "-controls";
        var scenariosClass = cssPrefix + "-scenarios";
        var scenarioPanelClass = cssPrefix + "-scenario-panel";
        var forceControlsClass = cssPrefix + "-force-controls";
        var enableViewportControls = options.EnableHtmlInteractions && options.EnableHtmlViewportControls;
        var enableExportControls = options.EnableHtmlInteractions && options.EnableHtmlExportControls;
        var enableFullscreenControl = options.EnableHtmlInteractions && options.EnableHtmlFullscreenControl;
        var enableScenarioInteractions = options.EnableHtmlInteractions && chart.Scenarios.Count > 0;
        var isGraphExplorationLayout = chart.LayoutMode == TopologyLayoutMode.ForceDirected || chart.LayoutMode == TopologyLayoutMode.RelationshipRadial;
        var enableForceGraphControls = options.EnableHtmlInteractions && options.EnableHtmlForceGraphControls && isGraphExplorationLayout;
        var renderScenarioControls = enableScenarioInteractions && options.EnableHtmlScenarioControls;
        var enableScenarioPanel = enableScenarioInteractions && options.EnableHtmlScenarioPanel;
        var enableSelectionPanel = options.EnableHtmlInteractions && options.EnableHtmlSelectionPanel;
        var enableScenarioUrlState = enableScenarioInteractions && options.EnableHtmlScenarioUrlState;
        var enableSync = options.EnableHtmlInteractions && options.EnableHtmlSynchronizedState && !string.IsNullOrWhiteSpace(options.HtmlSyncGroupName);
        var syncGroup = enableSync ? options.HtmlSyncGroupName!.Trim() : string.Empty;
        var activeScenarioId = enableScenarioInteractions ? ResolveActiveScenarioId(chart, options.ActiveScenarioId) : options.ActiveScenarioId;
        var forceControlsChart = enableForceGraphControls ? TopologyLayoutEngine.Prepare(chart, options.View, options) : chart;
        var writer = new HtmlMarkupWriter();
        if (includeAssets) {
            writer.StartElement("style")
                .Attribute("data-cfx-topology-assets", "true")
                .RawTrusted(StyleSheet(cssPrefix, CssFontFamily(theme.FontFamily), theme.Background, includePageShell: false))
                .EndElement();
        }

        writer.StartElement("div")
            .Attribute("class", wrapperClass)
            .Attribute("data-chart-id", id)
            .Attribute("data-layout-mode", chart.LayoutMode.ToString())
            .Attribute("data-cfx-interactive", options.EnableHtmlInteractions)
            .Attribute("data-cfx-viewport-controls", enableViewportControls)
            .Attribute("data-cfx-export-controls", enableExportControls)
            .Attribute("data-cfx-fullscreen-control", enableFullscreenControl)
            .Attribute("data-cfx-scenario-controls", enableScenarioInteractions)
            .Attribute("data-cfx-scenario-control-mode", options.HtmlScenarioControlMode == TopologyHtmlScenarioControlMode.Checkboxes ? "checkboxes" : "buttons")
            .Attribute("data-cfx-force-graph-controls", enableForceGraphControls)
            .Attribute("data-cfx-scenario-panel", enableScenarioPanel)
            .Attribute("data-cfx-selection-panel", enableSelectionPanel)
            .Attribute("data-cfx-controls-placement", ControlPlacementValue(options.HtmlControlPlacement))
            .Attribute("data-cfx-scenario-url-state", enableScenarioUrlState)
            .Attribute("data-cfx-active-scenario", string.IsNullOrWhiteSpace(activeScenarioId) ? null : activeScenarioId)
            .Attribute("data-cfx-sync-enabled", enableSync)
            .Attribute("data-cfx-sync-group", syncGroup)
            .Attribute("style", "width:100%;max-width:" + chart.Viewport.Width.ToString("0.###", CultureInfo.InvariantCulture) + "px;box-sizing:border-box;overflow:visible")
            .EndStartElement();
        if (renderScenarioControls) WriteScenarioControls(writer, scenariosClass, chart, activeScenarioId, options.HtmlScenarioControlMode);
        if (enableScenarioPanel) WriteScenarioPanel(writer, scenarioPanelClass, chart, activeScenarioId, enableScenarioUrlState);
        if (enableForceGraphControls) WriteForceGraphControls(writer, forceControlsClass, forceControlsChart, options);
        writer.StartElement("div").Attribute("class", viewportClass).EndStartElement();
        if (enableViewportControls || enableExportControls || enableFullscreenControl) {
            writer.StartElement("div").Attribute("class", controlsClass).Attribute("aria-label", "Topology controls").EndStartElement();
            if (enableViewportControls) {
                WriteIconButton(writer, "data-cfx-topology-zoom", "in", "Zoom in", "Zoom in", null, IconPlus());
                WriteIconButton(writer, "data-cfx-topology-zoom", "out", "Zoom out", "Zoom out", null, IconMinus());
                WriteIconButton(writer, "data-cfx-topology-fit", "true", "Fit topology", "Fit topology", null, IconFit());
                WriteIconButton(writer, "data-cfx-topology-reset", "true", "Reset viewport", "Reset viewport", null, IconReset());
            }

            if (enableFullscreenControl) {
                WriteIconButton(writer, "data-cfx-topology-fullscreen", "true", "Open topology fullscreen", "Open topology fullscreen", null, IconFullscreen());
            }

            if (enableExportControls) {
                WriteIconButton(writer, "data-cfx-topology-export", "svg", "Export SVG", "Export SVG", null, IconDownload(), "SVG");
                WriteIconButton(writer, "data-cfx-topology-export", "png", "Export PNG", "Export PNG", null, IconImage(), "PNG");
            }

            writer.EndElement();
        }

        if (enableSelectionPanel) WriteSelectionPanel(writer, cssPrefix + "-selection-panel");

        writer.RawTrusted(RenderEmbeddedSvg(chart, options, enableScenarioInteractions, enableForceGraphControls));
        writer.EndElement().EndElement();
        if (includeAssets && options.EnableHtmlInteractions) writer.RawTrusted(InteractionScript(cssPrefix));
        return writer.Build();
    }

    private string RenderEmbeddedSvg(TopologyChart chart, TopologyRenderOptions options, bool interactiveScenarioControls, bool forceGraphControls) {
        if (!interactiveScenarioControls && !forceGraphControls) return _svg.Render(chart, options);
        var activeScenarioId = options.ActiveScenarioId;
        var includeEdgeLabels = options.IncludeEdgeLabels;
        var includeGroups = options.IncludeGroups;
        try {
            if (interactiveScenarioControls) options.ActiveScenarioId = null;
            if (forceGraphControls) {
                options.IncludeEdgeLabels = true;
                options.IncludeGroups = true;
            }

            return _svg.Render(chart, options);
        } finally {
            options.ActiveScenarioId = activeScenarioId;
            options.IncludeEdgeLabels = includeEdgeLabels;
            options.IncludeGroups = includeGroups;
        }
    }

    /// <summary>
    /// Renders a complete static HTML page containing one topology chart.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A complete HTML page.</returns>
    public string RenderPage(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyRenderOptions();
        var theme = chart.Theme ?? TopologyTheme.Light();
        var title = string.IsNullOrWhiteSpace(chart.Title) ? "ChartForgeX topology" : chart.Title!;
        var cssPrefix = CssClassPrefix(options);
        var writer = new HtmlMarkupWriter();
        writer.Doctype().Line()
            .StartElement("html").Attribute("lang", "en").EndStartElement().Line()
            .StartElement("head").EndStartElement().Line();
        HtmlChartRenderer.WriteDocumentHead(writer, title, StyleSheet(cssPrefix, CssFontFamily(theme.FontFamily), theme.Background));
        writer.EndElement().Line()
            .StartElement("body").EndStartElement().Line()
            .RawTrusted(RenderFragmentCore(chart, options, includeAssets: false)).Line();
        if (options.EnableHtmlInteractions) writer.RawTrusted(InteractionScript(cssPrefix)).Line();
        writer.EndElement().Line()
            .EndElement().Line();
        return writer.Build();
    }

    private static void WriteIconButton(HtmlMarkupWriter writer, string dataAttribute, string dataValue, string title, string ariaLabel, bool? pressed, string icon, string? text = null) {
        writer.StartElement("button")
            .Attribute("type", "button")
            .Attribute(dataAttribute, dataValue)
            .Attribute("title", title)
            .Attribute("aria-label", ariaLabel)
            .Attribute("aria-pressed", pressed)
            .EndStartElement()
            .RawTrusted(icon);
        if (!string.IsNullOrWhiteSpace(text)) {
            writer.StartElement("span").EndStartElement().Text(text!).EndElement();
        }

        writer
            .EndElement();
    }

    private static void WriteButton(HtmlMarkupWriter writer, string dataAttribute, string dataValue, string title, string ariaLabel, bool? pressed, string text) {
        writer.StartElement("button")
            .Attribute("type", "button")
            .Attribute(dataAttribute, dataValue)
            .Attribute("title", title)
            .Attribute("aria-label", ariaLabel)
            .Attribute("aria-pressed", pressed)
            .EndStartElement()
            .Text(text)
            .EndElement();
    }

    private static void WriteSelectionPanel(HtmlMarkupWriter writer, string className) {
        writer.StartElement("aside")
            .Attribute("class", className)
            .Attribute("data-cfx-topology-selection-panel", "true")
            .Attribute("aria-live", "polite")
            .BooleanAttribute("hidden")
            .EndStartElement()
            .StartElement("div").Attribute("class", className + "__head").EndStartElement()
            .StartElement("span").Attribute("class", className + "__eyebrow").Attribute("data-cfx-selection-kind", "true").EndStartElement().Text("Selection").EndElement()
            .StartElement("strong").Attribute("class", className + "__title").Attribute("data-cfx-selection-title", "true").EndStartElement().Text("No item selected").EndElement()
            .StartElement("span").Attribute("class", className + "__status").Attribute("data-cfx-selection-status", "true").EndStartElement().EndElement()
            .EndElement()
            .StartElement("dl").Attribute("class", className + "__facts").Attribute("data-cfx-selection-facts", "true").EndStartElement().EndElement()
            .StartElement("div").Attribute("class", className + "__meta").Attribute("data-cfx-selection-meta", "true").EndStartElement().EndElement()
            .StartElement("div").Attribute("class", className + "__related").Attribute("data-cfx-selection-related", "true").EndStartElement().EndElement()
            .EndElement();
    }

    private static string IconPlus() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M12 5v14M5 12h14\"/></svg>";

    private static string IconMinus() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M5 12h14\"/></svg>";

    private static string IconFit() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M8 3H3v5M16 3h5v5M8 21H3v-5M16 21h5v-5M9 9h6v6H9z\"/></svg>";

    private static string IconReset() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M5 12a7 7 0 1 0 2.05-4.95M5 5v5h5\"/></svg>";

    private static string IconFullscreen() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M8 3H3v5M16 3h5v5M8 21H3v-5M16 21h5v-5\"/></svg>";

    private static string IconDownload() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M12 3v12M7 10l5 5 5-5M5 21h14\"/></svg>";

    private static string IconImage() => "<svg viewBox=\"0 0 24 24\" aria-hidden=\"true\"><path d=\"M5 5h14v14H5zM8 15l3-3 2 2 3-4 3 5M8 8h.01\"/></svg>";

    private static void WriteForceGraphControls(HtmlMarkupWriter writer, string className, TopologyChart chart, TopologyRenderOptions options) {
        writer.StartElement("div")
            .Attribute("class", className)
            .Attribute("data-cfx-force-graph-panel", "true")
            .Attribute("aria-label", "Force graph controls")
            .EndStartElement();
        writer.StartElement("div").Attribute("class", className + "__row").EndStartElement();
        writer.StartElement("input")
            .Attribute("type", "search")
            .Attribute("data-cfx-force-search", "true")
            .Attribute("placeholder", "Find node, edge, group")
            .Attribute("aria-label", "Find node, edge, or group")
            .EndVoidElement();
        WriteForceSelect(writer, "data-cfx-force-status", "Status", new[] { "All", "Healthy", "Warning", "Critical", "Unknown", "Disabled" });
        WriteGroupSelect(writer, chart);
        writer.EndElement();
        writer.StartElement("div").Attribute("class", className + "__toggles").EndStartElement();
        WriteToggle(writer, "data-cfx-force-toggle", "edges", "Edges", true);
        WriteToggle(writer, "data-cfx-force-toggle", "focus", "Focus", true);
        WriteToggle(writer, "data-cfx-force-toggle", "edge-labels", "Labels", options.IncludeEdgeLabels);
        WriteToggle(writer, "data-cfx-force-toggle", "groups", "Groups", options.IncludeGroups);
        WriteToggle(writer, "data-cfx-force-toggle", "hide-moving-edges", "Quiet pan", true);
        writer.EndElement();
        writer.StartElement("output").Attribute("data-cfx-force-summary", "true").EndStartElement().Text("Graph ready").EndElement();
        writer.EndElement();
    }

    private static void WriteForceSelect(HtmlMarkupWriter writer, string dataAttribute, string ariaLabel, string[] values) {
        writer.StartElement("select").Attribute(dataAttribute, "true").Attribute("aria-label", ariaLabel).EndStartElement();
        foreach (var value in values) writer.StartElement("option").Attribute("value", value == "All" ? string.Empty : value).EndStartElement().Text(value).EndElement();
        writer.EndElement();
    }

    private static void WriteGroupSelect(HtmlMarkupWriter writer, TopologyChart chart) {
        writer.StartElement("select").Attribute("data-cfx-force-group", "true").Attribute("aria-label", "Group").EndStartElement();
        writer.StartElement("option").Attribute("value", string.Empty).EndStartElement().Text("All groups").EndElement();
        foreach (var group in chart.Groups.Where(group => !string.IsNullOrWhiteSpace(group.Id)).OrderBy(group => group.Label, StringComparer.OrdinalIgnoreCase)) {
            writer.StartElement("option").Attribute("value", group.Id).EndStartElement().Text(string.IsNullOrWhiteSpace(group.Label) ? group.Id : group.Label).EndElement();
        }

        writer.EndElement();
    }

    private static void WriteToggle(HtmlMarkupWriter writer, string dataAttribute, string dataValue, string text, bool isChecked) {
        writer.StartElement("label").EndStartElement();
        writer.StartElement("input")
            .Attribute("type", "checkbox")
            .Attribute(dataAttribute, dataValue)
            .Attribute("checked", isChecked ? "checked" : null)
            .EndVoidElement();
        writer.Text(text).EndElement();
    }

    private static string StyleSheet(string cssPrefix, string fontFamily, string background, bool includePageShell = true) {
        var stylesheet = new StringBuilder();
        if (includePageShell) {
            stylesheet.Append(HtmlSurfacePolish.ReportBodyCss(background, fontFamily, "24px"));
        }

        stylesheet
            .Append(".cfx-topology-wrapper{margin:0 auto;overflow:visible}")
            .Append(".cfx-topology-wrapper{--cfx-topology-control-bg:rgba(255,255,255,.94);--cfx-topology-control-bg-hover:#eff6ff;--cfx-topology-control-text:#1e3a8a;--cfx-topology-control-muted:#475569;--cfx-topology-control-border:rgba(37,99,235,.28);--cfx-topology-control-border-hover:var(--cfx-topology-accent,#2563eb);--cfx-topology-control-shadow:0 10px 24px rgba(15,23,42,.12);--cfx-topology-control-panel-bg:rgba(255,255,255,.78);--cfx-topology-control-focus:0 0 0 3px rgba(37,99,235,.18);--cfx-topology-accent:#2563eb}")
            .Append("@media (prefers-color-scheme:dark){.cfx-topology-wrapper{--cfx-topology-control-bg:rgba(15,23,42,.9);--cfx-topology-control-bg-hover:rgba(37,99,235,.24);--cfx-topology-control-text:#dbeafe;--cfx-topology-control-muted:#a8b8cc;--cfx-topology-control-border:rgba(148,163,184,.32);--cfx-topology-control-shadow:0 12px 28px rgba(0,0,0,.35);--cfx-topology-control-panel-bg:rgba(15,23,42,.74);--cfx-topology-control-focus:0 0 0 3px rgba(96,165,250,.26);--cfx-topology-accent:#60a5fa}}")
            .Append(".cfx-topology-viewport{position:relative}")
            .Append(".cfx-topology-wrapper svg{max-width:100%;height:auto;display:block;overflow:visible}")
            .Append(".cfx-topology-wrapper[data-cfx-viewport-controls='true'] .cfx-topology-viewport{overflow:hidden;touch-action:none}")
            .Append(".cfx-topology-wrapper[data-cfx-viewport-controls='true'] .cfx-topology-viewport>svg{transform:translate(var(--cfx-topology-pan-x,0),var(--cfx-topology-pan-y,0)) scale(var(--cfx-topology-zoom,1));transform-origin:center center;transition:transform .12s ease;will-change:transform}")
            .Append(".cfx-topology-controls{position:absolute;z-index:5;top:12px;left:12px;display:grid;gap:6px}")
            .Append(".cfx-topology-wrapper[data-cfx-controls-placement='top-right'] .cfx-topology-controls{left:auto;right:12px}")
            .Append(".cfx-topology-wrapper[data-cfx-controls-placement='bottom-left'] .cfx-topology-controls{top:auto;bottom:12px}")
            .Append(".cfx-topology-wrapper[data-cfx-controls-placement='bottom-right'] .cfx-topology-controls{top:auto;right:12px;bottom:12px;left:auto}")
            .Append(".cfx-topology-wrapper[data-cfx-controls-placement='left-rail'] .cfx-topology-controls,.cfx-topology-wrapper[data-cfx-controls-placement='right-rail'] .cfx-topology-controls{top:12px;bottom:auto;display:flex;flex-direction:column}")
            .Append(".cfx-topology-wrapper[data-cfx-controls-placement='right-rail'] .cfx-topology-controls{right:12px;left:auto}")
            .Append(".cfx-topology-controls button,.cfx-topology-scenarios button{min-width:34px;height:34px;border:1px solid rgba(37,99,235,.28);border-radius:6px;background:rgba(255,255,255,.92);color:#1e3a8a;cursor:pointer;font:700 12px/1 ")
            .Append(fontFamily)
            .Append(";box-shadow:0 8px 18px rgba(15,23,42,.1)}")
            .Append(".cfx-topology-scenarios{display:flex;flex-wrap:wrap;gap:8px;margin:0 0 10px;align-items:center}")
            .Append(".cfx-topology-scenarios button{height:32px;padding:0 12px;border-color:color-mix(in srgb,var(--cfx-topology-scenario-color,#2563eb) 32%,transparent);color:#0f172a}")
            .Append(".cfx-topology-scenarios button:before{content:'';display:inline-block;width:8px;height:8px;border-radius:50%;margin-right:7px;background:var(--cfx-topology-scenario-color,#64748b)}")
            .Append(".cfx-topology-scenarios label{display:inline-flex;align-items:center;gap:7px;min-height:30px;padding:0 10px;border:1px solid var(--cfx-topology-control-border);border-radius:6px;background:var(--cfx-topology-control-bg);color:var(--cfx-topology-control-text);cursor:pointer;font:700 12px/1 ")
            .Append(fontFamily)
            .Append("}")
            .Append(".cfx-topology-scenarios input[type='checkbox']{width:14px;height:14px;margin:0;accent-color:var(--cfx-topology-scenario-color,var(--cfx-topology-accent,#2563eb))}")
            .Append(".cfx-topology-scenarios label:has(input:checked){border-color:var(--cfx-topology-scenario-color,var(--cfx-topology-accent,#2563eb));background:var(--cfx-topology-control-bg-hover)}")
            .Append(".cfx-topology-scenario-panel{display:grid;gap:8px;max-width:760px;margin:0 0 12px;padding:10px 12px;border:1px solid rgba(148,163,184,.42);border-radius:8px;background:rgba(255,255,255,.88);box-shadow:0 10px 24px rgba(15,23,42,.07);color:#0f172a}")
            .Append(".cfx-topology-scenario-panel__summary{display:grid;gap:2px}")
            .Append(".cfx-topology-scenario-panel__title{font:700 14px/1.25 ")
            .Append(fontFamily)
            .Append(";color:var(--cfx-topology-scenario-color,#0f172a)}")
            .Append(".cfx-topology-scenario-panel__meta{font:500 12px/1.35 ")
            .Append(fontFamily)
            .Append(";color:#475569}")
            .Append(".cfx-topology-scenario-panel__controls{display:flex;flex-wrap:wrap;gap:6px}")
            .Append(".cfx-topology-scenario-panel__controls button{min-height:28px;padding:4px 9px;border:1px solid rgba(148,163,184,.4);border-radius:6px;background:#fff;color:#0f172a;cursor:pointer;font:700 11px/1 ")
            .Append(fontFamily)
            .Append("}")
            .Append(".cfx-topology-scenario-panel__controls button:hover,.cfx-topology-scenario-panel__controls button:focus-visible,.cfx-topology-scenario-panel__controls button[aria-pressed='true']{border-color:var(--cfx-topology-scenario-color,#2563eb);color:var(--cfx-topology-scenario-color,#2563eb);background:#eff6ff}")
            .Append(".cfx-topology-scenario-panel__steps{display:flex;flex-wrap:wrap;gap:6px;margin:0;padding:0;list-style:none}")
            .Append(".cfx-topology-scenario-panel__steps li{display:flex;align-items:center;gap:6px;min-height:24px;padding:3px 8px;border:1px solid rgba(148,163,184,.34);border-radius:6px;background:rgba(248,250,252,.82);cursor:pointer;font:700 11px/1.2 ")
            .Append(fontFamily)
            .Append(";color:#0f172a}")
            .Append(".cfx-topology-scenario-panel__steps li:before{content:attr(data-cfx-scenario-step-index);display:inline-grid;place-items:center;width:16px;height:16px;border-radius:50%;background:var(--cfx-topology-scenario-color,#2563eb);color:#fff;font-size:10px}")
            .Append(".cfx-topology-scenario-panel__steps li[aria-current='step']{border-color:var(--cfx-topology-scenario-color,#2563eb);background:#eff6ff;color:#0f172a}")
            .Append(".cfx-topology-force-controls{display:grid;gap:8px;margin:0 0 12px;padding:10px 12px;border:1px solid rgba(148,163,184,.42);border-radius:8px;background:rgba(255,255,255,.9);box-shadow:0 10px 24px rgba(15,23,42,.07);color:#0f172a}")
            .Append(".cfx-topology-force-controls__row{display:grid;grid-template-columns:minmax(180px,1fr) minmax(118px,.25fr) minmax(140px,.35fr);gap:8px}")
            .Append(".cfx-topology-force-controls input[type='search'],.cfx-topology-force-controls select{min-width:0;height:32px;border:1px solid rgba(148,163,184,.48);border-radius:6px;background:#fff;color:#0f172a;padding:0 9px;font:600 12px/1 ")
            .Append(fontFamily)
            .Append("}")
            .Append(".cfx-topology-force-controls__toggles{display:flex;flex-wrap:wrap;gap:8px 12px;align-items:center}")
            .Append(".cfx-topology-force-controls label{display:inline-flex;gap:6px;align-items:center;font:700 11px/1 ")
            .Append(fontFamily)
            .Append(";color:#334155}")
            .Append(".cfx-topology-force-controls output{font:600 11px/1.35 ")
            .Append(fontFamily)
            .Append(";color:#64748b}")
            .Append(".cfx-topology-controls{display:flex;flex-wrap:wrap;align-items:center;gap:4px;padding:4px;border:1px solid var(--cfx-topology-control-border);border-radius:8px;background:var(--cfx-topology-control-panel-bg);box-shadow:var(--cfx-topology-control-shadow);backdrop-filter:blur(10px)}")
            .Append(".cfx-topology-wrapper[data-cfx-viewport-controls='true'] .cfx-topology-viewport,.cfx-topology-wrapper[data-cfx-export-controls='true'] .cfx-topology-viewport,.cfx-topology-wrapper[data-cfx-fullscreen-control='true'] .cfx-topology-viewport{padding-top:48px}")
            .Append(".cfx-topology-wrapper[data-cfx-controls-placement='left-rail'] .cfx-topology-viewport,.cfx-topology-wrapper[data-cfx-controls-placement='right-rail'] .cfx-topology-viewport,.cfx-topology-wrapper[data-cfx-controls-placement='bottom-left'] .cfx-topology-viewport,.cfx-topology-wrapper[data-cfx-controls-placement='bottom-right'] .cfx-topology-viewport{padding-top:0}")
            .Append(".cfx-topology-wrapper[data-cfx-controls-placement='left-rail'] .cfx-topology-viewport{padding-left:48px}")
            .Append(".cfx-topology-wrapper[data-cfx-controls-placement='right-rail'] .cfx-topology-viewport{padding-right:48px}")
            .Append(".cfx-topology-controls button,.cfx-topology-scenarios button,.cfx-topology-scenario-panel__controls button{appearance:none;display:inline-flex;align-items:center;justify-content:center;gap:6px;min-width:32px;height:32px;border:1px solid var(--cfx-topology-control-border);border-radius:6px;background:var(--cfx-topology-control-bg);color:var(--cfx-topology-control-text);box-shadow:none;cursor:pointer;font:700 12px/1 ")
            .Append(fontFamily)
            .Append(";letter-spacing:0;transition:background-color .14s ease,border-color .14s ease,color .14s ease,box-shadow .14s ease,transform .14s ease}")
            .Append(".cfx-topology-controls button svg{width:16px;height:16px;display:block;fill:none;stroke:currentColor;stroke-width:2;stroke-linecap:round;stroke-linejoin:round;pointer-events:none}")
            .Append(".cfx-topology-controls button span{font-size:11px;line-height:1;pointer-events:none}")
            .Append(".cfx-topology-controls button[data-cfx-topology-export],.cfx-topology-scenarios button{min-width:42px;padding:0 10px}")
            .Append(".cfx-topology-controls button:hover,.cfx-topology-controls button:focus-visible,.cfx-topology-controls button[aria-pressed='true'],.cfx-topology-scenarios button:hover,.cfx-topology-scenarios button:focus-visible,.cfx-topology-scenarios button[aria-pressed='true'],.cfx-topology-scenario-panel__controls button:hover,.cfx-topology-scenario-panel__controls button:focus-visible,.cfx-topology-scenario-panel__controls button[aria-pressed='true']{border-color:var(--cfx-topology-control-border-hover);color:var(--cfx-topology-control-text);background:var(--cfx-topology-control-bg-hover);box-shadow:var(--cfx-topology-control-focus)}")
            .Append(".cfx-topology-controls button:active,.cfx-topology-scenarios button:active,.cfx-topology-scenario-panel__controls button:active{transform:translateY(1px)}")
            .Append(".cfx-topology-wrapper:fullscreen{width:100vw!important;max-width:none!important;height:100vh!important;margin:0!important;padding:16px;box-sizing:border-box;background:var(--cfx-topology-background,#fff);overflow:auto}")
            .Append(".cfx-topology-wrapper:fullscreen .cfx-topology-viewport{min-height:calc(100vh - 32px)}")
            .Append(".cfx-topology-scenario-panel,.cfx-topology-force-controls{border-color:var(--cfx-topology-control-border);background:var(--cfx-topology-control-panel-bg);color:var(--cfx-topology-control-text);box-shadow:var(--cfx-topology-control-shadow);backdrop-filter:blur(10px)}")
            .Append(".cfx-topology-selection-panel{position:absolute;z-index:4;right:12px;top:12px;bottom:12px;width:min(320px,calc(100% - 24px));display:grid;align-content:start;gap:10px;padding:12px;border:1px solid var(--cfx-topology-control-border);border-radius:8px;background:var(--cfx-topology-control-panel-bg);color:var(--cfx-topology-control-text);box-shadow:var(--cfx-topology-control-shadow);backdrop-filter:blur(12px);overflow:auto}")
            .Append(".cfx-topology-selection-panel[hidden]{display:none}")
            .Append(".cfx-topology-selection-panel__head{display:grid;gap:5px;padding-bottom:8px;border-bottom:1px solid var(--cfx-topology-control-border)}")
            .Append(".cfx-topology-selection-panel__eyebrow{font:700 10px/1 ")
            .Append(fontFamily)
            .Append(";text-transform:uppercase;letter-spacing:.04em;color:var(--cfx-topology-control-muted)}")
            .Append(".cfx-topology-selection-panel__title{font:700 14px/1.25 ")
            .Append(fontFamily)
            .Append(";color:var(--cfx-topology-control-text)}")
            .Append(".cfx-topology-selection-panel__status{justify-self:start;min-height:20px;padding:3px 8px;border-radius:999px;background:color-mix(in srgb,var(--cfx-topology-selection-status-color,var(--cfx-topology-accent,#2563eb)) 13%,transparent);color:var(--cfx-topology-selection-status-color,var(--cfx-topology-control-text));font:800 10px/1 ")
            .Append(fontFamily)
            .Append("}")
            .Append(".cfx-topology-selection-panel__facts{display:grid;grid-template-columns:auto 1fr;gap:6px 10px;margin:0;font:600 11px/1.35 ")
            .Append(fontFamily)
            .Append(";color:var(--cfx-topology-control-text)}")
            .Append(".cfx-topology-selection-panel__facts dt{color:var(--cfx-topology-control-muted)}")
            .Append(".cfx-topology-selection-panel__facts dd{margin:0;min-width:0;overflow-wrap:anywhere;text-align:right}")
            .Append(".cfx-topology-selection-panel__meta,.cfx-topology-selection-panel__related{display:grid;gap:6px;font:600 11px/1.35 ")
            .Append(fontFamily)
            .Append(";color:var(--cfx-topology-control-text)}")
            .Append(".cfx-topology-selection-panel__meta:empty,.cfx-topology-selection-panel__related:empty{display:none}")
            .Append(".cfx-topology-selection-panel__row{display:flex;justify-content:space-between;gap:10px;padding:6px 0;border-top:1px solid color-mix(in srgb,var(--cfx-topology-control-border) 72%,transparent)}")
            .Append(".cfx-topology-selection-panel__row span:first-child{color:var(--cfx-topology-control-muted)}")
            .Append(".cfx-topology-selection-panel__row span:last-child{min-width:0;text-align:right;overflow-wrap:anywhere}")
            .Append(".cfx-topology-scenario-panel__meta,.cfx-topology-force-controls label,.cfx-topology-force-controls output{color:var(--cfx-topology-control-muted)}")
            .Append(".cfx-topology-force-controls input[type='search'],.cfx-topology-force-controls select{border-color:var(--cfx-topology-control-border);background:var(--cfx-topology-control-bg);color:var(--cfx-topology-control-text)}")
            .Append(".cfx-topology-controls button:hover,.cfx-topology-controls button:focus-visible,.cfx-topology-controls button[aria-pressed='true'],.cfx-topology-scenarios button:hover,.cfx-topology-scenarios button:focus-visible,.cfx-topology-scenarios button[aria-pressed='true']{border-color:var(--cfx-topology-scenario-color,var(--cfx-topology-control-border-hover,#2563eb));color:var(--cfx-topology-control-text,#0f172a);background:var(--cfx-topology-control-bg-hover,#eff6ff)}")
            .Append(".cfx-topology-wrapper[data-cfx-viewport-controls='true'] .cfx-topology-viewport{cursor:grab}")
            .Append(".cfx-topology-wrapper[data-cfx-viewport-controls='true'][data-cfx-topology-dragging='true'] .cfx-topology-viewport{cursor:grabbing}")
            .Append(".cfx-topology-wrapper [data-cfx-role='topology-node'],.cfx-topology-wrapper [data-cfx-role='topology-edge'],.cfx-topology-wrapper [data-cfx-role='topology-group']{cursor:pointer}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-selected{filter:drop-shadow(0 10px 18px rgba(37,99,235,.22))}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-muted{opacity:.32}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-related{opacity:1}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-hovered{filter:drop-shadow(0 8px 16px rgba(15,23,42,.16))}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-hover-muted{opacity:.42}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-hover-related{opacity:1}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-muted{opacity:.18}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-active{opacity:1;filter:drop-shadow(0 10px 18px rgba(37,99,235,.18))}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-active .cfx-topology__edge{stroke:var(--cfx-topology-active-scenario-color,#2563eb)!important;stroke-width:5!important}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-active .cfx-topology__node-card{stroke:var(--cfx-topology-active-scenario-color,#2563eb)!important;stroke-width:2.4!important}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-preview-muted{opacity:.34}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-preview{opacity:1;filter:drop-shadow(0 8px 14px rgba(15,23,42,.16))}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-preview .cfx-topology__edge{stroke:var(--cfx-topology-preview-scenario-color,#2563eb)!important;stroke-width:4!important}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-preview .cfx-topology__node-card{stroke:var(--cfx-topology-preview-scenario-color,#2563eb)!important;stroke-width:2!important}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-step-muted{opacity:.38}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-step-active{opacity:1;filter:drop-shadow(0 10px 18px rgba(15,23,42,.2))}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-step-active .cfx-topology__edge{stroke-width:6!important}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-scenario-step-active .cfx-topology__node-card{stroke-width:3!important}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-force-hidden{display:none!important}")
            .Append(".cfx-topology-wrapper .cfx-topology-html-force-faded{opacity:.12!important}")
            .Append(".cfx-topology-wrapper[data-cfx-force-focus='true'] .cfx-topology-html-muted,.cfx-topology-wrapper[data-cfx-force-focus='true'] .cfx-topology-html-hover-muted{opacity:.06!important}")
            .Append(".cfx-topology-wrapper[data-cfx-force-focus='true'] .cfx-topology-html-related .cfx-topology__edge,.cfx-topology-wrapper[data-cfx-force-focus='true'] .cfx-topology-html-hover-related .cfx-topology__edge{opacity:.92!important;stroke-width:3.2!important}")
            .Append(".cfx-topology-wrapper[data-cfx-force-focus='true'] [data-cfx-role='topology-edge-label'].cfx-topology-html-force-focus-label{display:block!important;opacity:1!important}")
            .Append(".cfx-topology-wrapper[data-cfx-force-moving-edges='true'] .cfx-topology-html-force-moving .cfx-topology__edge,.cfx-topology-wrapper[data-cfx-force-moving-edges='true'] .cfx-topology-html-force-moving .cfx-topology__edge--premium-layer{opacity:.08!important}")
            .Append(".cfx-topology-wrapper[data-cfx-force-moving-edges='true'] [data-cfx-role='topology-edge-label']{display:none!important}")
            .Append("@media (max-width:720px){.cfx-topology-force-controls__row{grid-template-columns:1fr}.cfx-topology-force-controls{padding:9px}.cfx-topology-selection-panel{top:auto;left:12px;max-height:42%;width:auto}}");
        if (includePageShell) {
            stylesheet
                .Append(HtmlSurfacePolish.ResponsiveCenteredBodyCss)
                .Append(HtmlSurfacePolish.PrintBodyCss("0", ".cfx-topology-wrapper{width:100%;max-width:none}.cfx-topology-wrapper svg{width:100%;height:auto}.cfx-topology-scenarios,.cfx-topology-scenario-panel{display:none}"));
        }

        var css = stylesheet.ToString();
        return css
            .Replace(".cfx-topology-wrapper", "." + cssPrefix + "-wrapper")
            .Replace(".cfx-topology-viewport", "." + cssPrefix + "-viewport")
            .Replace(".cfx-topology-controls", "." + cssPrefix + "-controls")
            .Replace(".cfx-topology-scenarios", "." + cssPrefix + "-scenarios")
            .Replace(".cfx-topology-scenario-panel", "." + cssPrefix + "-scenario-panel")
            .Replace(".cfx-topology-selection-panel", "." + cssPrefix + "-selection-panel")
            .Replace(".cfx-topology-force-controls", "." + cssPrefix + "-force-controls")
            .Replace(".cfx-topology__edge", "." + cssPrefix + "__edge")
            .Replace(".cfx-topology__node-card", "." + cssPrefix + "__node-card")
            .Replace(".cfx-topology-html-", "." + cssPrefix + "-html-");
    }

    private static string CssClassPrefix(TopologyRenderOptions options) {
        return NormalizeCssClassPrefix(options.CssClassPrefix, DefaultCssClassPrefix);
    }

    private static string ControlPlacementValue(TopologyHtmlControlPlacement placement) => placement switch {
        TopologyHtmlControlPlacement.TopRight => "top-right",
        TopologyHtmlControlPlacement.LeftRail => "left-rail",
        TopologyHtmlControlPlacement.RightRail => "right-rail",
        TopologyHtmlControlPlacement.BottomLeft => "bottom-left",
        TopologyHtmlControlPlacement.BottomRight => "bottom-right",
        _ => "top-left"
    };

}
