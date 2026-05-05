using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Renders topology charts to dependency-free PNG images.
/// </summary>
public sealed class TopologyPngRenderer {
    /// <summary>
    /// Renders a topology chart to PNG bytes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>A PNG image.</returns>
    public byte[] Render(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyRenderOptions();
        if (options.Preset != TopologyViewPreset.Default) options.ApplyPreset(options.Preset);
        var prepared = TopologyLayoutEngine.Prepare(chart, options.View, options);
        var validation = new TopologyChartValidator().Validate(prepared);
        if (!validation.IsValid) throw new TopologyValidationException(validation);

        var width = (int)Math.Ceiling(prepared.Viewport.Width);
        var height = (int)Math.Ceiling(prepared.Viewport.Height);
        var theme = prepared.Theme ?? TopologyTheme.Light();
        var highlight = TopologyHighlightState.From(prepared, options);
        var canvas = new RgbaCanvas(width, height, Math.Max(1, options.PngSupersamplingScale), null, Math.Max(1, options.PngOutputScale));
        canvas.Clear(Color(theme.Background));
        if (options.IncludeTitle) DrawHeader(canvas, prepared, theme);
        if (options.IncludeGroups) DrawGroups(canvas, prepared, theme, options, highlight);
        DrawEdges(canvas, prepared, theme, options, highlight);
        if (options.IncludeEdgeLabels) DrawEdgeLabels(canvas, prepared, theme, options, highlight);
        DrawNodes(canvas, prepared, theme, options, highlight);
        if (options.IncludeStatusBadges) DrawStatusBadges(canvas, prepared, theme, options, highlight);
        if (options.IncludeLegend && prepared.Legend != null) DrawLegend(canvas, prepared, theme);
        return PngWriter.WriteRgba(canvas.OutputWidth, canvas.OutputHeight, canvas.ToOutputPixels());
    }

    private static void DrawHeader(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme) {
        if (string.IsNullOrWhiteSpace(chart.Title) && string.IsNullOrWhiteSpace(chart.Subtitle)) return;
        var x = chart.Viewport.Padding;
        var y = chart.Viewport.Padding + 8;
        if (!string.IsNullOrWhiteSpace(chart.Title)) canvas.DrawTextEmphasized(x, y, chart.Title!, Color(theme.Foreground), 22);
        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) canvas.DrawText(x, y + 27, chart.Subtitle!, Color(theme.MutedForeground), 13);
    }

    private static void DrawGroups(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        foreach (var group in chart.Groups) {
            var isHighlighted = highlight.IsGroupHighlighted(group);
            var status = Color(theme.StatusColor(group.Status));
            canvas.FillRoundedRect(group.X, group.Y, group.Width, group.Height, 16, Color(StatusFill(theme.StatusColor(group.Status), theme.Background)));
            canvas.StrokeRoundedRect(group.X, group.Y, group.Width, group.Height, 16, WithAlpha(status, 170), 1.2);
            if (!isHighlighted && highlight.IsActive) canvas.FillRoundedRect(group.X, group.Y, group.Width, group.Height, 16, WithAlpha(Color(theme.Background), 180));
            if (options.IncludeGroupLabels) {
                canvas.DrawTextEmphasized(group.X + 24, group.Y + 16, group.Label, status, 16);
                if (!string.IsNullOrWhiteSpace(group.Subtitle)) canvas.DrawText(group.X + 24, group.Y + 38, group.Subtitle!, Color(theme.MutedForeground), 12);
            }
        }
    }

    private static void DrawEdges(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            var points = EdgePoints(chart, edge, nodes);
            var color = highlight.IsEdgeHighlighted(edge) ? Color(theme.StatusColor(edge.Status)) : WithAlpha(Color(theme.StatusColor(edge.Status)), (byte)Math.Round(255 * highlight.DimmedOpacity));
            for (var i = 0; i < points.Count - 1; i++) {
                if (edge.Status is TopologyHealthStatus.Warning or TopologyHealthStatus.Critical or TopologyHealthStatus.Unknown or TopologyHealthStatus.Disabled) {
                    canvas.DrawDashedLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, color, 2.2, 8, 5);
                } else {
                    canvas.DrawLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, color, 2.2);
                }
            }

            if (options.IncludeDirectionMarkers && edge.Direction is TopologyDirection.Forward or TopologyDirection.Bidirectional) DrawArrow(canvas, points[points.Count - 2], points[points.Count - 1], color);
            if (options.IncludeDirectionMarkers && edge.Direction is TopologyDirection.Backward or TopologyDirection.Bidirectional) DrawArrow(canvas, points[1], points[0], color);
        }
    }

    private static void DrawEdgeLabels(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        foreach (var layout in EdgeLabelLayouts(chart, options)) {
            var edge = layout.Edge;
            var cx = layout.CenterX;
            var cy = layout.CenterY;
            canvas.FillRoundedRect(cx - layout.Width / 2, cy - layout.Height / 2, layout.Width, layout.Height, 9, Color(theme.Background));
            canvas.StrokeRoundedRect(cx - layout.Width / 2, cy - layout.Height / 2, layout.Width, layout.Height, 9, Color(theme.Border), 1);
            var color = highlight.IsEdgeHighlighted(edge) ? Color(theme.StatusColor(edge.Status)) : WithAlpha(Color(theme.StatusColor(edge.Status)), (byte)Math.Round(255 * highlight.DimmedOpacity));
            if (!string.IsNullOrWhiteSpace(layout.Label)) DrawCentered(canvas, cx, cy + (string.IsNullOrWhiteSpace(layout.SecondaryLabel) ? -7 : -13), layout.Label, color, 12, true);
            if (!string.IsNullOrWhiteSpace(layout.SecondaryLabel)) DrawCentered(canvas, cx, cy + 3, layout.SecondaryLabel, Color(theme.MutedForeground), 10, false);
        }
    }

    private static void DrawNodes(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        foreach (var node in chart.Nodes) {
            var isHighlighted = highlight.IsNodeHighlighted(node);
            var status = Color(theme.StatusColor(node.Status));
            var displayMode = EffectiveNodeDisplayMode(node, options);
            if (displayMode == TopologyNodeDisplayMode.Dot) {
                var radius = Math.Max(5, Math.Min(node.Width, node.Height) / 2);
                canvas.DrawCircle(CenterX(node), CenterY(node), radius + 2, Color(theme.Background));
                canvas.DrawCircle(CenterX(node), CenterY(node), radius, status);
                DrawNodeBadge(canvas, node, theme, status, displayMode);
                continue;
            }

            var radiusRect = displayMode == TopologyNodeDisplayMode.Pill ? node.Height / 2 : displayMode == TopologyNodeDisplayMode.Icon ? 12 : 10;
            canvas.FillRoundedRect(node.X + 2, node.Y + 5, node.Width, node.Height, 10, ChartColor.FromRgba(15, 23, 42, 18));
            canvas.FillRoundedRect(node.X, node.Y, node.Width, node.Height, radiusRect, Color(theme.Card));
            canvas.StrokeRoundedRect(node.X, node.Y, node.Width, node.Height, radiusRect, status, 1.5);
            if (!isHighlighted && highlight.IsActive) canvas.FillRoundedRect(node.X, node.Y, node.Width, node.Height, radiusRect, WithAlpha(Color(theme.Background), 185));
            DrawNodeIcon(canvas, node, theme, status, displayMode);
            if (options.IncludeNodeLabels && displayMode != TopologyNodeDisplayMode.Icon) {
                var textX = displayMode == TopologyNodeDisplayMode.Pill ? node.X + 34 : node.X + 42;
                var titleY = displayMode == TopologyNodeDisplayMode.Pill ? node.Y + node.Height / 2 - 7 : node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 13 : 17);
                var titleSize = displayMode == TopologyNodeDisplayMode.Pill ? 11.5 : displayMode == TopologyNodeDisplayMode.CompactCard ? 11.5 : 12.5;
                canvas.DrawTextEmphasized(textX, titleY, TrimTo(node.Label, NodeTitleMaxLength(displayMode)), Color(theme.Foreground), titleSize);
                if (displayMode != TopologyNodeDisplayMode.Pill && !string.IsNullOrWhiteSpace(node.Subtitle)) canvas.DrawText(textX, node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 31 : 37), TrimTo(node.Subtitle!, NodeLabelMaxLength), Color(theme.MutedForeground), 10.5);
            }

            DrawNodeBadge(canvas, node, theme, status, displayMode);
        }
    }

    private static void DrawNodeBadge(RgbaCanvas canvas, TopologyNode node, TopologyTheme theme, ChartColor color, TopologyNodeDisplayMode displayMode) {
        var badge = NodeBadge(node);
        if (string.IsNullOrWhiteSpace(badge)) return;
        var width = Math.Max(18, RgbaCanvas.MeasureTextEmphasizedWidth(badge, 8.5, null) + 12);
        var height = 18.0;
        var x = displayMode == TopologyNodeDisplayMode.Dot ? CenterX(node) + 8 : displayMode == TopologyNodeDisplayMode.Icon ? CenterX(node) - width / 2 : node.X + node.Width - width - 6;
        var y = displayMode == TopologyNodeDisplayMode.Dot ? CenterY(node) - 21 : displayMode == TopologyNodeDisplayMode.Icon ? node.Y + node.Height + 4 : node.Y + node.Height - height - 6;
        canvas.FillRoundedRect(x, y, width, height, 9, Color(StatusFill(theme.StatusColor(node.Status), theme.Background)));
        canvas.StrokeRoundedRect(x, y, width, height, 9, color, 1);
        DrawCentered(canvas, x + width / 2, y + 4, badge, color, 8.5, true);
    }

    private static void DrawNodeIcon(RgbaCanvas canvas, TopologyNode node, TopologyTheme theme, ChartColor status, TopologyNodeDisplayMode displayMode) {
        var cx = displayMode == TopologyNodeDisplayMode.Icon ? CenterX(node) : node.X + 22;
        var cy = node.Y + node.Height / 2;
        var size = displayMode == TopologyNodeDisplayMode.Pill ? 18 : displayMode == TopologyNodeDisplayMode.Icon ? 26 : 22;
        canvas.FillRoundedRect(cx - size / 2, cy - size / 2, size, size, 6, Color(StatusFill(theme.StatusColor(node.Status), theme.Background)));
        canvas.StrokeRoundedRect(cx - size / 2, cy - size / 2, size, size, 6, status, 1);
        DrawCentered(canvas, cx, cy - 6, NodeGlyph(node), status, displayMode == TopologyNodeDisplayMode.Pill ? 7.5 : 8.5, true);
    }

    private static int NodeTitleMaxLength(TopologyNodeDisplayMode displayMode) {
        return displayMode switch {
            TopologyNodeDisplayMode.CompactCard => 11,
            TopologyNodeDisplayMode.Pill => 14,
            _ => NodeLabelMaxLength
        };
    }

    private static void DrawStatusBadges(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        foreach (var node in chart.Nodes) {
            if (EffectiveNodeDisplayMode(node, options) == TopologyNodeDisplayMode.Dot) continue;
            if (highlight.IsActive && !highlight.IsNodeHighlighted(node)) continue;
            var color = Color(theme.StatusColor(node.Status));
            var cx = node.X + node.Width - 11;
            var cy = node.Y + 11;
            canvas.DrawCircle(cx, cy, 9, Color(theme.Background));
            canvas.DrawCircle(cx, cy, 7, color);
            DrawCentered(canvas, cx, cy - 5, StatusGlyph(node.Status), ChartColor.White, 8, true);
        }
    }

    private static void DrawLegend(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme) {
        var legend = chart.Legend!;
        var x = chart.Viewport.Padding;
        var height = LegendHeight(legend);
        var y = chart.Viewport.Height - chart.Viewport.Padding - height;
        var width = Math.Min(LegendMaxWidth, chart.Viewport.Width - chart.Viewport.Padding * 2);
        canvas.FillRoundedRect(x, y, width, height, 12, Color(theme.Card));
        canvas.StrokeRoundedRect(x, y, width, height, 12, Color(theme.Border), 1);
        if (!string.IsNullOrWhiteSpace(legend.Title)) canvas.DrawTextEmphasized(x + 16, y + 11, legend.Title!, Color(theme.Foreground), 12);
        for (var i = 0; i < legend.Items.Count; i++) {
            var item = legend.Items[i];
            var col = i % LegendColumns;
            var row = i / LegendColumns;
            var itemX = x + 18 + col * LegendItemColumnWidth;
            var itemY = y + LegendFirstItemOffsetY + row * LegendItemRowHeight;
            var color = Color(item.Color ?? (item.Status.HasValue ? theme.StatusColor(item.Status.Value) : theme.Accent));
            if (item.Kind == TopologyLegendItemKind.Edge) canvas.DrawDashedLine(itemX, itemY - 4, itemX + 24, itemY - 4, color, 2, 6, 4);
            else if (item.Kind == TopologyLegendItemKind.Node && !string.IsNullOrWhiteSpace(item.Symbol)) {
                canvas.FillRoundedRect(itemX, itemY - 13, 16, 16, 4, Color(StatusFill(item.Color ?? theme.Accent, theme.Background)));
                canvas.StrokeRoundedRect(itemX, itemY - 13, 16, 16, 4, color, 1);
                DrawCentered(canvas, itemX + 8, itemY - 11, TrimTo(item.Symbol!.Trim(), 4), color, 6.5, true);
            } else canvas.DrawCircle(itemX + 8, itemY - 5, 6, color);
            canvas.DrawText(itemX + 32, itemY - 14, item.Label, Color(theme.MutedForeground), 11);
        }
    }

    private static void DrawArrow(RgbaCanvas canvas, ChartPoint from, ChartPoint to, ChartColor color) {
        var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
        const double length = 10;
        const double spread = 0.52;
        var p1 = new ChartPoint(to.X, to.Y);
        var p2 = new ChartPoint(to.X - Math.Cos(angle - spread) * length, to.Y - Math.Sin(angle - spread) * length);
        var p3 = new ChartPoint(to.X - Math.Cos(angle + spread) * length, to.Y - Math.Sin(angle + spread) * length);
        canvas.FillPolygon(new[] { p1, p2, p3 }, color);
    }

    private static void DrawCentered(RgbaCanvas canvas, double centerX, double y, string text, ChartColor color, double fontSize, bool emphasized) {
        var width = emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(text, fontSize, null) : RgbaCanvas.MeasureTextWidth(text, fontSize, null);
        if (emphasized) canvas.DrawTextEmphasized(centerX - width / 2, y, text, color, fontSize);
        else canvas.DrawText(centerX - width / 2, y, text, color, fontSize);
    }

    private static ChartColor Color(string value) => ChartColor.FromHex(value);

    private static ChartColor WithAlpha(ChartColor color, byte alpha) => ChartColor.FromRgba(color.R, color.G, color.B, alpha);

}
