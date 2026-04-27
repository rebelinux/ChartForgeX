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
    private static void DrawTree(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var model = BuildTreeModel(chart, plot);
        if (model.Nodes.Count == 0 || model.Links.Count == 0) return;
        var t = chart.Options.Theme;
        sb.AppendLine("<g data-cfx-role=\"tree-chart\">");
        foreach (var link in model.Links) DrawTreeLink(sb, chart, model, link);
        foreach (var node in model.Nodes) {
            var fillIndex = node.Depth % Math.Max(1, t.Palette.Length);
            var summary = node.Label + ": level " + node.Depth.ToString(CultureInfo.InvariantCulture);
            sb.AppendLine($"<rect data-cfx-role=\"tree-node\" data-cfx-node=\"{node.Index}\" data-cfx-depth=\"{node.Depth}\" role=\"img\" aria-label=\"{Escape(summary)}\" x=\"{F(node.X)}\" y=\"{F(node.Y)}\" width=\"{F(model.NodeWidth)}\" height=\"{F(model.NodeHeight)}\" rx=\"8\" fill=\"url(#{id}-sliceFill{fillIndex})\"/>");
            sb.AppendLine($"<rect data-cfx-role=\"tree-node-border\" x=\"{F(node.X + 0.5)}\" y=\"{F(node.Y + 0.5)}\" width=\"{F(Math.Max(0, model.NodeWidth - 1))}\" height=\"{F(Math.Max(0, model.NodeHeight - 1))}\" rx=\"7.5\" fill=\"none\" stroke=\"{t.CardBackground.ToCss()}\" stroke-opacity=\"0.72\"/>");
            DrawSvgTextCenteredX(sb, chart, "tree-node-label", node.Label, node.X + model.NodeWidth / 2, node.Y + model.NodeHeight / 2, HeatmapTextColor(t.Palette[fillIndex]), t.TickLabelFontSize, model.NodeWidth - 12, "760", t.CardBackground, 2.2);
        }

        sb.AppendLine("</g>");
    }

    private static void DrawTreeLink(StringBuilder sb, Chart chart, TreeModel model, TreeLink link) {
        var parent = model.Nodes[link.Parent];
        var child = model.Nodes[link.Child];
        var color = chart.Options.Theme.Palette[parent.Depth % chart.Options.Theme.Palette.Length];
        var x0 = parent.X + model.NodeWidth;
        var y0 = parent.Y + model.NodeHeight / 2;
        var x1 = child.X;
        var y1 = child.Y + model.NodeHeight / 2;
        var gap = Math.Max(24, (x1 - x0) / 2);
        var width = Math.Max(1.4, Math.Min(5, 1.4 + link.Value / Math.Max(0.000001, model.MaxLinkValue) * 3.6));
        var path = "M " + F(x0) + " " + F(y0) + " C " + F(x0 + gap) + " " + F(y0) + " " + F(x1 - gap) + " " + F(y1) + " " + F(x1) + " " + F(y1);
        sb.AppendLine($"<path data-cfx-role=\"tree-link\" data-cfx-parent=\"{link.Parent}\" data-cfx-child=\"{link.Child}\" data-cfx-value=\"{F(link.Value)}\" d=\"{path}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-opacity=\"0.44\" stroke-width=\"{F(width)}\" stroke-linecap=\"round\"/>");
    }

    private static TreeModel BuildTreeModel(Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.Tree);
        if (series == null || series.Points.Count < 2) return TreeModel.Empty;
        var nodeCount = chart.Options.TreeNodeLabels.Count;
        var links = new List<TreeLink>();
        for (var i = 0; i + 1 < series.Points.Count; i += 2) {
            var endpoints = series.Points[i];
            var valuePoint = series.Points[i + 1];
            var parent = Math.Max(0, (int)Math.Round(endpoints.X));
            var child = Math.Max(0, (int)Math.Round(endpoints.Y));
            var value = Math.Max(0.000001, valuePoint.Y);
            nodeCount = Math.Max(nodeCount, Math.Max(parent, child) + 1);
            links.Add(new TreeLink(parent, child, value));
        }

        if (nodeCount == 0 || links.Count == 0) return TreeModel.Empty;
        var nodes = new List<TreeNode>();
        for (var i = 0; i < nodeCount; i++) nodes.Add(new TreeNode(i, TreeNodeLabel(chart, i)));
        var children = new List<int>[nodeCount];
        var incoming = new bool[nodeCount];
        for (var i = 0; i < nodeCount; i++) children[i] = new List<int>();
        foreach (var link in links) {
            children[link.Parent].Add(link.Child);
            incoming[link.Child] = true;
        }

        var root = 0;
        for (var i = 0; i < incoming.Length; i++) if (!incoming[i]) { root = i; break; }
        ApplyTreeDepths(nodes, children, root, 0);
        LayoutTreeNodes(nodes, children, root, plot, out var nodeWidth, out var nodeHeight, out var maxDepth);
        var maxLinkValue = links.Max(link => link.Value);
        return new TreeModel(nodes, links, nodeWidth, nodeHeight, maxDepth, maxLinkValue);
    }

    private static void ApplyTreeDepths(List<TreeNode> nodes, List<int>[] children, int node, int depth) {
        nodes[node].Depth = depth;
        foreach (var child in children[node]) ApplyTreeDepths(nodes, children, child, depth + 1);
    }

    private static void LayoutTreeNodes(List<TreeNode> nodes, List<int>[] children, int root, ChartRect plot, out double nodeWidth, out double nodeHeight, out int maxDepth) {
        maxDepth = Math.Max(1, nodes.Max(node => node.Depth));
        var leafCount = Math.Max(1, nodes.Count(node => children[node.Index].Count == 0));
        nodeWidth = Math.Max(96, Math.Min(150, plot.Width / (maxDepth + 1) * 0.54));
        nodeHeight = Math.Max(28, Math.Min(42, plot.Height / Math.Max(1, leafCount) * 0.46));
        var effectiveNodeHeight = nodeHeight;
        var availableHeight = Math.Max(1, plot.Height - effectiveNodeHeight - 18);
        var nextLeaf = 0;
        AssignY(root);
        foreach (var node in nodes) {
            node.X = maxDepth == 0 ? plot.Left + plot.Width / 2 - nodeWidth / 2 : plot.Left + node.Depth / (double)maxDepth * (plot.Width - nodeWidth);
            node.Y -= nodeHeight / 2;
        }

        double AssignY(int nodeIndex) {
            if (children[nodeIndex].Count == 0) {
                var y = leafCount == 1 ? plot.Top + plot.Height / 2 : plot.Top + effectiveNodeHeight / 2 + 9 + nextLeaf / (double)Math.Max(1, leafCount - 1) * availableHeight;
                nodes[nodeIndex].Y = y;
                nextLeaf++;
                return y;
            }

            var total = 0.0;
            foreach (var child in children[nodeIndex]) total += AssignY(child);
            nodes[nodeIndex].Y = total / Math.Max(1, children[nodeIndex].Count);
            return nodes[nodeIndex].Y;
        }
    }

    private static string TreeNodeLabel(Chart chart, int index) =>
        index >= 0 && index < chart.Options.TreeNodeLabels.Count ? chart.Options.TreeNodeLabels[index] : "Node " + (index + 1).ToString(CultureInfo.InvariantCulture);

    private sealed class TreeNode {
        public TreeNode(int index, string label) { Index = index; Label = label; }
        public int Index { get; }
        public string Label { get; }
        public int Depth { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    private readonly struct TreeLink {
        public TreeLink(int parent, int child, double value) { Parent = parent; Child = child; Value = value; }
        public int Parent { get; }
        public int Child { get; }
        public double Value { get; }
    }

    private readonly struct TreeModel {
        public TreeModel(List<TreeNode> nodes, List<TreeLink> links, double nodeWidth, double nodeHeight, int maxDepth, double maxLinkValue) {
            Nodes = nodes;
            Links = links;
            NodeWidth = nodeWidth;
            NodeHeight = nodeHeight;
            MaxDepth = maxDepth;
            MaxLinkValue = maxLinkValue;
        }

        public static TreeModel Empty { get; } = new(new List<TreeNode>(), new List<TreeLink>(), 0, 0, 0, 1);
        public List<TreeNode> Nodes { get; }
        public List<TreeLink> Links { get; }
        public double NodeWidth { get; }
        public double NodeHeight { get; }
        public int MaxDepth { get; }
        public double MaxLinkValue { get; }
    }
}
