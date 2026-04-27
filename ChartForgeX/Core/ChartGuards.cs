using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

internal static class ChartGuards {
    private static readonly ChartSeriesKind[] ExclusiveSeriesKinds = {
        ChartSeriesKind.Heatmap,
        ChartSeriesKind.Gauge,
        ChartSeriesKind.Circle,
        ChartSeriesKind.RadialBar,
        ChartSeriesKind.Bullet,
        ChartSeriesKind.Waterfall,
        ChartSeriesKind.Radar,
        ChartSeriesKind.Funnel,
        ChartSeriesKind.Treemap,
        ChartSeriesKind.Timeline,
        ChartSeriesKind.Gantt,
        ChartSeriesKind.Sankey,
        ChartSeriesKind.Tree,
        ChartSeriesKind.Pie,
        ChartSeriesKind.Donut,
        ChartSeriesKind.PolarArea
    };

    public static void Finite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
    }

    public static void UnitInterval(double value, string parameterName) {
        Finite(value, parameterName);
        if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be between zero and one.");
    }

    public static List<ChartPoint> Points(IEnumerable<ChartPoint> points, string parameterName) {
        if (points == null) throw new ArgumentNullException(parameterName);
        var materialized = points.ToList();
        for (var i = 0; i < materialized.Count; i++) {
            Finite(materialized[i].X, parameterName + "[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "].X");
            Finite(materialized[i].Y, parameterName + "[" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "].Y");
        }

        return materialized;
    }

    public static void RenderCompatibility(Chart chart) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var exclusiveKinds = chart.Series.Select(series => series.Kind).Where(kind => Array.IndexOf(ExclusiveSeriesKinds, kind) >= 0).Distinct().ToArray();
        if (exclusiveKinds.Length == 0) return;
        if (exclusiveKinds.Length > 1 || chart.Series.Any(series => series.Kind != exclusiveKinds[0])) {
            throw new InvalidOperationException("Specialized chart types cannot be mixed with other series kinds in the same chart.");
        }

        if (RequiresSingleSeries(exclusiveKinds[0]) && chart.Series.Count != 1) {
            throw new InvalidOperationException(exclusiveKinds[0].ToString() + " charts support exactly one series.");
        }

        if (RequiresPositiveValues(exclusiveKinds[0]) && !chart.Series[0].Points.Any(point => point.Y > 0)) {
            throw new InvalidOperationException(exclusiveKinds[0].ToString() + " charts require at least one positive value.");
        }

        if (exclusiveKinds[0] == ChartSeriesKind.Waterfall && chart.Series[0].Points.Count == 0) {
            throw new InvalidOperationException("Waterfall charts require at least one value.");
        }

        if (exclusiveKinds[0] == ChartSeriesKind.Radar) {
            var categoryCount = chart.Series
                .Where(series => series.Kind == ChartSeriesKind.Radar)
                .SelectMany(series => series.Points.Select(point => point.X))
                .Distinct()
                .Count();
            if (categoryCount < 3) throw new InvalidOperationException("Radar charts require at least three categories.");
        }
    }

    private static bool RequiresSingleSeries(ChartSeriesKind kind) {
        return kind == ChartSeriesKind.Gauge ||
            kind == ChartSeriesKind.Circle ||
            kind == ChartSeriesKind.RadialBar ||
            kind == ChartSeriesKind.Waterfall ||
            kind == ChartSeriesKind.Funnel ||
            kind == ChartSeriesKind.Treemap ||
            kind == ChartSeriesKind.Sankey ||
            kind == ChartSeriesKind.Tree ||
            kind == ChartSeriesKind.Pie ||
            kind == ChartSeriesKind.Donut ||
            kind == ChartSeriesKind.PolarArea;
    }

    private static bool RequiresPositiveValues(ChartSeriesKind kind) {
        return kind == ChartSeriesKind.Funnel ||
            kind == ChartSeriesKind.Treemap ||
            kind == ChartSeriesKind.Pie ||
            kind == ChartSeriesKind.Donut ||
            kind == ChartSeriesKind.PolarArea;
    }
}
