using System.Collections.Generic;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawPointLabels(StringBuilder sb, Chart chart, ChartSeries series, IReadOnlyList<ChartPoint> mapped, ChartRect plot) {
        var offset = chart.Options.Theme.MarkerRadius + 12;
        var reserved = new List<ChartLabelBounds>();
        var placement = DataLabelPlacement(chart, series);
        for (var i = 0; i < mapped.Count; i++) {
            var point = mapped[i];
            var label = FormatValue(chart, series.Points[i].Y);
            if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right) {
                var labelX = placement == ChartDataLabelPlacement.Right ? point.X + offset : point.X - offset;
                var anchor = placement == ChartDataLabelPlacement.Right ? "start" : "end";
                if (!ReserveSvgHorizontalLabel(label, labelX, point.Y, anchor, chart, plot, reserved)) continue;
                DrawHorizontalValueLabel(sb, chart, label, labelX, point.Y, anchor, plot, series, i);
                continue;
            }

            var labelY = placement == ChartDataLabelPlacement.Below
                ? point.Y + offset
                : placement == ChartDataLabelPlacement.Center || placement == ChartDataLabelPlacement.Inside
                    ? point.Y
                    : point.Y - offset;
            if (placement == ChartDataLabelPlacement.Auto && labelY < plot.Top + chart.Options.Theme.DataLabelFontSize) labelY = point.Y + offset;
            if (!ReserveSvgLabel(label, point.X, labelY, chart, plot, reserved)) continue;
            DrawDataLabel(sb, chart, label, point.X, labelY, plot, series: series, pointIndex: i);
        }
    }
}
