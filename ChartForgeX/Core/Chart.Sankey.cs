using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a dependency-free Sankey flow chart from weighted source-to-target links.
    /// </summary>
    public Chart AddSankey(string name, IEnumerable<ChartSankeyLink> links, ChartColor? color = null) {
        if (links == null) throw new ArgumentNullException(nameof(links));
        var materialized = links.ToList();
        if (materialized.Count == 0) throw new ArgumentException("Sankey charts require at least one link.", nameof(links));
        var nodeIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        var nodeLabels = new List<string>();
        var points = new List<ChartPoint>(materialized.Count * 2);
        foreach (var link in materialized) {
            if (string.Equals(link.Source, link.Target, StringComparison.Ordinal)) throw new ArgumentException("Sankey links must connect distinct source and target nodes.", nameof(links));
            var source = GetSankeyNodeIndex(link.Source, nodeIndexes, nodeLabels);
            var target = GetSankeyNodeIndex(link.Target, nodeIndexes, nodeLabels);
            points.Add(new ChartPoint(source, target));
            points.Add(new ChartPoint(link.Value, link.Value));
        }

        Options.SankeyNodeLabels.Clear();
        Options.SankeyNodeLabels.AddRange(nodeLabels);
        return Add(name, ChartSeriesKind.Sankey, points, color);
    }

    private static int GetSankeyNodeIndex(string label, Dictionary<string, int> nodeIndexes, List<string> nodeLabels) {
        if (nodeIndexes.TryGetValue(label, out var existing)) return existing;
        var index = nodeLabels.Count;
        nodeIndexes.Add(label, index);
        nodeLabels.Add(label);
        return index;
    }
}
