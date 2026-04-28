using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SpecializedSvgSegmentsExposeDataMetadata() {
        var polar = Chart.Create()
            .WithXLabels("Coverage", "Policy", "Alerts")
            .AddPolarArea("Control share", Points(60, 30, 10))
            .ToSvg();
        Assert(polar.Contains("data-cfx-role=\"polar-area-segment\" data-cfx-point=\"0\" data-cfx-label=\"Coverage\" data-cfx-value=\"60\" data-cfx-percent=\"0.6\"", System.StringComparison.Ordinal), "Polar-area segments should expose label, value, and percent metadata.");

        var funnel = Chart.Create()
            .WithXLabels("Discovered", "Validated", "Fixed")
            .AddFunnel("Pipeline", Points(100, 70, 35))
            .ToSvg();
        Assert(funnel.Contains("data-cfx-role=\"funnel-segment\" data-cfx-point=\"1\" data-cfx-label=\"Validated\" data-cfx-value=\"70\" data-cfx-retention=\"0.7\" data-cfx-dropoff=\"0.3\"", System.StringComparison.Ordinal), "Funnel segments should expose label, value, retention, and drop-off metadata.");

        var radial = Chart.Create()
            .WithXLabels("Identity", "Device", "Network")
            .AddRadialBar("Coverage", Points(92, 74, 66))
            .ToSvg();
        Assert(radial.Contains("data-cfx-role=\"radial-bar-ring\" data-cfx-point=\"0\" data-cfx-label=\"Identity\" data-cfx-value=\"92\" data-cfx-percent=\"0.92\"", System.StringComparison.Ordinal), "Radial bar rings should expose label, value, and percent metadata.");

        var gauge = Chart.Create()
            .AddGauge("Score", 84, 0, 100)
            .ToSvg();
        Assert(gauge.Contains("data-cfx-role=\"gauge\" data-cfx-status=\"positive\" data-cfx-label=\"Score\" data-cfx-value=\"84\" data-cfx-min=\"0\" data-cfx-max=\"100\" data-cfx-percent=\"0.84\"", System.StringComparison.Ordinal), "Gauge containers should expose label, value, min/max, percent, and status metadata.");

        var circle = Chart.Create()
            .AddCircle("Readiness", 72, 0, 100)
            .ToSvg();
        Assert(circle.Contains("data-cfx-role=\"circle-value\" data-cfx-label=\"Readiness\" data-cfx-value=\"72\" data-cfx-ratio=\"0.72\" data-cfx-percent=\"0.72\"", System.StringComparison.Ordinal), "Circle values should expose label, value, ratio, and percent metadata.");

        var bullet = Chart.Create()
            .AddBullet("DMARC", 82, 90, 0, 100, new[] { 60d, 80d })
            .ToSvg();
        Assert(bullet.Contains("data-cfx-role=\"bullet-row\" data-cfx-series=\"0\" data-cfx-status=\"below-target\" data-cfx-label=\"DMARC\" data-cfx-value=\"82\" data-cfx-target=\"90\" data-cfx-min=\"0\" data-cfx-max=\"100\"", System.StringComparison.Ordinal), "Bullet rows should expose value, target, min/max, and status metadata.");

        var waterfall = Chart.Create()
            .WithXLabels("Opened", "Closed")
            .AddWaterfall("Findings", Points(18, -7))
            .ToSvg();
        Assert(waterfall.Contains("data-cfx-role=\"waterfall-bar\" data-cfx-point=\"1\" data-cfx-label=\"Closed\" data-cfx-start=\"18\" data-cfx-end=\"11\" data-cfx-delta=\"-7\" data-cfx-status=\"negative\"", System.StringComparison.Ordinal), "Waterfall bars should expose label, start, end, delta, and status metadata.");
    }
}
