using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawSankey(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var model = BuildSankeyModel(chart, plot);
        if (model.Nodes.Count == 0 || model.Links.Count == 0) return;
        var t = chart.Options.Theme;
        sb.AppendLine("<g data-cfx-role=\"sankey-chart\">");
        foreach (var link in model.Links) DrawSankeyLink(sb, chart, model, link);
        foreach (var node in model.Nodes) {
            var labelMaxWidth = Math.Max(64, plot.Width / Math.Max(2, model.MaxLayer + 1) * 0.62);
            var anchor = node.Layer == model.MaxLayer ? "end" : "start";
            var labelX = node.Layer == model.MaxLayer ? node.X - 10 : node.X + model.NodeWidth + 10;
            var label = TrimSvgLabelToWidth(node.Label, t.TickLabelFontSize, labelMaxWidth);
            var summary = node.Label + ": " + FormatValue(chart, node.Value);
            sb.AppendLine($"<rect data-cfx-role=\"sankey-node\" data-cfx-node=\"{node.Index}\" data-cfx-layer=\"{node.Layer}\" role=\"img\" aria-label=\"{Escape(summary)}\" x=\"{F(node.X)}\" y=\"{F(node.Y)}\" width=\"{F(model.NodeWidth)}\" height=\"{F(node.Height)}\" rx=\"{F(Math.Min(7, model.NodeWidth / 2))}\" fill=\"url(#{id}-sliceFill{node.Index % t.Palette.Length})\"/>");
            sb.AppendLine($"<rect data-cfx-role=\"sankey-node-border\" x=\"{F(node.X + 0.5)}\" y=\"{F(node.Y + 0.5)}\" width=\"{F(Math.Max(0, model.NodeWidth - 1))}\" height=\"{F(Math.Max(0, node.Height - 1))}\" rx=\"{F(Math.Min(6.5, model.NodeWidth / 2))}\" fill=\"none\" stroke=\"{t.CardBackground.ToCss()}\" stroke-opacity=\"0.62\"/>");
            if (chart.Options.ShowDataLabels) {
                sb.AppendLine($"<text data-cfx-role=\"sankey-node-label\" x=\"{F(labelX)}\" y=\"{F(node.Y + node.Height / 2)}\" text-anchor=\"{anchor}\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"2.4\" paint-order=\"stroke fill\" stroke-linejoin=\"round\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"700\">{Escape(label)}</text>");
            }
        }

        sb.AppendLine("</g>");
    }

    private static void DrawSankeyLink(StringBuilder sb, Chart chart, SankeyModel model, SankeyLink link) {
        var source = model.Nodes[link.Source];
        var target = model.Nodes[link.Target];
        var color = chart.Options.Theme.Palette[source.Index % chart.Options.Theme.Palette.Length];
        var x0 = source.X + model.NodeWidth;
        var x1 = target.X;
        var midX = x0 + (x1 - x0) * 0.55;
        var half = Math.Max(1, link.Width / 2);
        var path = "M " + F(x0) + " " + F(link.SourceY - half) +
            " C " + F(midX) + " " + F(link.SourceY - half) + " " + F(midX) + " " + F(link.TargetY - half) + " " + F(x1) + " " + F(link.TargetY - half) +
            " L " + F(x1) + " " + F(link.TargetY + half) +
            " C " + F(midX) + " " + F(link.TargetY + half) + " " + F(midX) + " " + F(link.SourceY + half) + " " + F(x0) + " " + F(link.SourceY + half) + " Z";
        var summary = source.Label + " to " + target.Label + ": " + FormatValue(chart, link.Value);
        sb.AppendLine($"<path data-cfx-role=\"sankey-link\" data-cfx-source=\"{link.Source}\" data-cfx-target=\"{link.Target}\" data-cfx-value=\"{F(link.Value)}\" role=\"img\" aria-label=\"{Escape(summary)}\" d=\"{path}\" fill=\"{color.ToCss()}\" fill-opacity=\"0.34\" stroke=\"{color.ToCss()}\" stroke-opacity=\"0.38\" stroke-width=\"0.6\"/>");
    }

    private static SankeyModel BuildSankeyModel(Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.Sankey);
        if (series == null || series.Points.Count < 2) return SankeyModel.Empty;
        var links = new List<SankeyLink>();
        var nodeCount = chart.Options.SankeyNodeLabels.Count;
        for (var i = 0; i + 1 < series.Points.Count; i += 2) {
            var endpoints = series.Points[i];
            var valuePoint = series.Points[i + 1];
            var source = Math.Max(0, (int)Math.Round(endpoints.X));
            var target = Math.Max(0, (int)Math.Round(endpoints.Y));
            var value = Math.Max(0, valuePoint.Y);
            if (value <= 0) continue;
            nodeCount = Math.Max(nodeCount, Math.Max(source, target) + 1);
            links.Add(new SankeyLink(source, target, value));
        }

        if (nodeCount == 0 || links.Count == 0) return SankeyModel.Empty;
        var nodes = new List<SankeyNode>();
        for (var i = 0; i < nodeCount; i++) nodes.Add(new SankeyNode(i, SankeyNodeLabel(chart, i)));
        foreach (var link in links) {
            nodes[link.Source].Outgoing += link.Value;
            nodes[link.Target].Incoming += link.Value;
        }

        ApplySankeyLayers(nodes, links);
        LayoutSankeyNodes(nodes, links, plot, out var nodeWidth, out var scale, out var maxLayer);
        LayoutSankeyLinks(nodes, links, scale);
        return new SankeyModel(nodes, links, nodeWidth, maxLayer);
    }

    private static void ApplySankeyLayers(List<SankeyNode> nodes, List<SankeyLink> links) {
        for (var pass = 0; pass < nodes.Count; pass++) {
            foreach (var link in links) nodes[link.Target].Layer = Math.Max(nodes[link.Target].Layer, nodes[link.Source].Layer + 1);
        }

        var maxLayer = Math.Max(1, nodes.Max(node => node.Layer));
        foreach (var node in nodes) if (node.Outgoing <= 0 && node.Incoming > 0) node.Layer = maxLayer;
    }

    private static void LayoutSankeyNodes(List<SankeyNode> nodes, List<SankeyLink> links, ChartRect plot, out double nodeWidth, out double scale, out int maxLayer) {
        maxLayer = Math.Max(1, nodes.Max(node => node.Layer));
        nodeWidth = Math.Max(14, Math.Min(24, plot.Width / (maxLayer + 1) * 0.08));
        scale = double.PositiveInfinity;
        for (var layer = 0; layer <= maxLayer; layer++) {
            var layerNodes = nodes.Where(node => node.Layer == layer).ToArray();
            if (layerNodes.Length == 0) continue;
            var sum = layerNodes.Sum(node => node.Value);
            scale = Math.Min(scale, (plot.Height - Math.Max(0, layerNodes.Length - 1) * 18) / Math.Max(0.000001, sum));
        }

        if (double.IsInfinity(scale) || scale <= 0) scale = 1;
        var effectiveScale = scale;
        for (var layer = 0; layer <= maxLayer; layer++) {
            var layerNodes = nodes.Where(node => node.Layer == layer).OrderBy(node => node.Index).ToArray();
            var totalHeight = layerNodes.Sum(node => Math.Max(8, node.Value * effectiveScale)) + Math.Max(0, layerNodes.Length - 1) * 18;
            var y = plot.Top + Math.Max(0, (plot.Height - totalHeight) / 2);
            foreach (var node in layerNodes) {
                node.X = maxLayer == 0 ? plot.Left + plot.Width / 2 - nodeWidth / 2 : plot.Left + node.Layer / (double)maxLayer * (plot.Width - nodeWidth);
                node.Height = Math.Max(8, node.Value * effectiveScale);
                node.Y = y;
                y += node.Height + 18;
            }
        }
    }

    private static void LayoutSankeyLinks(List<SankeyNode> nodes, List<SankeyLink> links, double scale) {
        var outgoingOffset = new double[nodes.Count];
        var incomingOffset = new double[nodes.Count];
        foreach (var link in links.OrderBy(link => nodes[link.Source].Layer).ThenBy(link => link.Target)) {
            link.Width = Math.Max(2, link.Value * scale);
            link.SourceY = nodes[link.Source].Y + outgoingOffset[link.Source] + link.Width / 2;
            link.TargetY = nodes[link.Target].Y + incomingOffset[link.Target] + link.Width / 2;
            outgoingOffset[link.Source] += link.Width;
            incomingOffset[link.Target] += link.Width;
        }
    }

    private static string SankeyNodeLabel(Chart chart, int index) =>
        index >= 0 && index < chart.Options.SankeyNodeLabels.Count ? chart.Options.SankeyNodeLabels[index] : "Node " + (index + 1).ToString(CultureInfo.InvariantCulture);

    private sealed class SankeyNode {
        public SankeyNode(int index, string label) { Index = index; Label = label; }
        public int Index { get; }
        public string Label { get; }
        public double Incoming { get; set; }
        public double Outgoing { get; set; }
        public double Value => Math.Max(Incoming, Outgoing);
        public int Layer { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Height { get; set; }
    }

    private sealed class SankeyLink {
        public SankeyLink(int source, int target, double value) { Source = source; Target = target; Value = value; }
        public int Source { get; }
        public int Target { get; }
        public double Value { get; }
        public double Width { get; set; }
        public double SourceY { get; set; }
        public double TargetY { get; set; }
    }

    private readonly struct SankeyModel {
        public SankeyModel(List<SankeyNode> nodes, List<SankeyLink> links, double nodeWidth, int maxLayer) { Nodes = nodes; Links = links; NodeWidth = nodeWidth; MaxLayer = maxLayer; }
        public static SankeyModel Empty { get; } = new(new List<SankeyNode>(), new List<SankeyLink>(), 0, 0);
        public List<SankeyNode> Nodes { get; }
        public List<SankeyLink> Links { get; }
        public double NodeWidth { get; }
        public int MaxLayer { get; }
    }
}
