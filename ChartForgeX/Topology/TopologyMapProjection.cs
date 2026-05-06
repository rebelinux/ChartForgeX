using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal readonly struct TopologyProjectedPoint {
    public TopologyProjectedPoint(double x, double y, bool isInsideViewport) {
        X = x;
        Y = y;
        IsInsideViewport = isInsideViewport;
    }

    public double X { get; }

    public double Y { get; }

    public bool IsInsideViewport { get; }
}

internal static class TopologyMapProjection {
    public const string ProjectionName = "equirectangular";

    public static ChartRect MapRect(TopologyChart chart) {
        var pad = Math.Max(24, chart.Viewport.Padding);
        var top = pad + (string.IsNullOrWhiteSpace(chart.Title) ? 0 : 72);
        var legendOffset = TopologyRenderPrimitives.LegendReservedHeight(chart.Legend);
        var plot = new ChartRect(
            pad + 8,
            top + 8,
            Math.Max(1, chart.Viewport.Width - pad * 2 - 16),
            Math.Max(1, chart.Viewport.Height - top - pad - legendOffset - 16));
        return FitMap(plot, chart.MapViewport);
    }

    public static TopologyProjectedPoint Project(ChartRect map, ChartMapViewport viewport, double longitude, double latitude) {
        var visible = IsVisible(viewport, longitude, latitude);
        var clampedLongitude = Clamp(longitude, viewport.MinimumLongitude, viewport.MaximumLongitude);
        var clampedLatitude = Clamp(latitude, viewport.MinimumLatitude, viewport.MaximumLatitude);
        var x = map.Left + (clampedLongitude - viewport.MinimumLongitude) / (viewport.MaximumLongitude - viewport.MinimumLongitude) * map.Width;
        var y = map.Top + (viewport.MaximumLatitude - clampedLatitude) / (viewport.MaximumLatitude - viewport.MinimumLatitude) * map.Height;
        return new TopologyProjectedPoint(x, y, visible);
    }

    public static bool IsVisible(ChartMapViewport viewport, double longitude, double latitude) {
        return longitude >= viewport.MinimumLongitude && longitude <= viewport.MaximumLongitude &&
               latitude >= viewport.MinimumLatitude && latitude <= viewport.MaximumLatitude;
    }

    private static ChartRect FitMap(ChartRect plot, ChartMapViewport viewport) {
        var aspect = (viewport.MaximumLongitude - viewport.MinimumLongitude) / (viewport.MaximumLatitude - viewport.MinimumLatitude);
        var width = Math.Min(plot.Width, plot.Height * aspect);
        var height = width / aspect;
        if (height > plot.Height) {
            height = plot.Height;
            width = height * aspect;
        }

        return new ChartRect(
            plot.Left + Math.Max(0, (plot.Width - width) / 2),
            plot.Top + Math.Max(0, (plot.Height - height) / 2),
            width,
            height);
    }

    private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;
}
