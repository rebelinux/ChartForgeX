using System;
using System.Globalization;
using System.Linq;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SvgEscapesText() {
        var svg = SampleChart().ToSvg();
        Assert(svg.StartsWith("<svg", StringComparison.Ordinal), "SVG should start with the svg element.");
        Assert(svg.Contains("A &lt; B &amp; C", StringComparison.Ordinal), "SVG should escape text content.");
        Assert(svg.Contains("<clipPath", StringComparison.Ordinal), "SVG should clip plotted series to the plot area.");
    }

    private static void AreaBaselineDoesNotPolluteXAxis() {
        var svg = Chart.Create().WithSize(640, 360).AddArea("Passed", Points(100, 180, 260)).ToSvg();
        foreach (var line in svg.Split('\n')) {
            if (line.Contains("text-anchor=\"middle\"", StringComparison.Ordinal) && line.Contains(">0</text>", StringComparison.Ordinal)) {
                throw new InvalidOperationException("Area baseline created an unwanted x-axis zero tick.");
            }
        }
    }

    private static void XAxisLabelsRender() {
        var svg = SampleChart().ToSvg();
        Assert(svg.Contains(">Mon</text>", StringComparison.Ordinal), "SVG should render explicit x-axis labels.");
        Assert(svg.Contains(">Tue</text>", StringComparison.Ordinal), "SVG should render explicit x-axis labels.");
    }

    private static void SmoothSeriesRenderAsBezierPaths() {
        Assert(SampleChart().ToSvg().Contains(" C ", StringComparison.Ordinal), "Smooth series should render cubic Bezier path segments.");
    }

    private static void PngScatterDoesNotConnectPoints() {
        var chart = Chart.Create()
            .WithSize(220, 80)
            .WithSparkline()
            .AddScatter("Only points", new[] { new ChartPoint(1, 1), new ChartPoint(4, 1) }, ChartColor.FromRgb(37, 99, 235));
        var pixels = ReadPngRgba(chart.ToPng(), out var width, out var height);
        var markerPixels = CountNearColor(pixels, 37, 99, 235, 24);
        var centerLinePixels = CountNearColorInRect(pixels, width, width / 2 - 24, height / 2 - 10, 48, 20, 37, 99, 235, 24);
        Assert(markerPixels > 20, "PNG scatter charts should render visible markers.");
        Assert(centerLinePixels == 0, $"PNG scatter charts should not connect independent points. Center line pixels: {centerLinePixels}.");
    }

    private static void StepLineSeriesRenderAsStairSteps() {
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 30), new ChartPoint(3, 18), new ChartPoint(4, 42) };
        var line = Chart.Create().WithSize(420, 260).AddLine("Values", points);
        var stepLine = Chart.Create().WithSize(420, 260).AddStepLine("Values", points);
        var lineSvg = line.ToSvg();
        var stepSvg = stepLine.ToSvg();
        Assert(CountOccurrences(stepSvg, " L ") > CountOccurrences(lineSvg, " L "), "SVG step lines should add horizontal and vertical stair-step segments.");
        Assert(!line.ToPng().SequenceEqual(stepLine.ToPng()), "PNG step lines should rasterize differently from straight line series.");
    }

    private static void StepAreaSeriesRenderAsStairStepAreas() {
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 30), new ChartPoint(3, 18), new ChartPoint(4, 42) };
        var area = Chart.Create().WithSize(420, 260).AddArea("Values", points);
        var stepArea = Chart.Create().WithSize(420, 260).WithDataLabels().AddStepArea("Values", points, ChartColor.FromRgb(37, 99, 235));
        var areaSvg = area.ToSvg();
        var stepAreaSvg = stepArea.ToSvg();
        Assert(stepArea.Series[0].Kind == ChartSeriesKind.StepArea, "Step areas should use their own series kind.");
        Assert(stepAreaSvg.Contains("data-cfx-role=\"step-area\"", StringComparison.Ordinal), "SVG step areas should expose a filled step-area role.");
        Assert(stepAreaSvg.Contains("data-cfx-role=\"step-area-line\"", StringComparison.Ordinal), "SVG step areas should expose a readable boundary role.");
        Assert(CountOccurrences(stepAreaSvg, " L ") > CountOccurrences(areaSvg, " L "), "SVG step areas should add horizontal and vertical stair-step area segments.");
        Assert(stepAreaSvg.Contains(">42</text>", StringComparison.Ordinal), "Step-area data labels should render values when enabled.");
        Assert(!area.ToPng().SequenceEqual(stepArea.ToPng()), "PNG step areas should rasterize differently from straight area series.");
    }

    private static void DataLabelsRenderWhenEnabled() {
        var svg = Chart.Create().WithSize(640, 360).WithDataLabels().AddBar("Values", Points(42, 84, 126)).ToSvg();
        Assert(svg.Contains(">42</text>", StringComparison.Ordinal), "Data labels should render numeric values.");
        Assert(svg.Contains(">126</text>", StringComparison.Ordinal), "Data labels should render numeric values.");
    }

    private static void SeriesDataLabelOverridesAreHonored() {
        var chart = Chart.Create()
            .WithSize(420, 280)
            .WithDataLabels()
            .AddBar("Visible", Points(42), ChartColor.FromRgb(37, 99, 235))
            .AddLine("Hidden", Points(84), ChartColor.FromRgb(245, 158, 11));
        chart.Series[1].WithDataLabels(false);

        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"data-label\"") == 1, "Series data-label overrides should hide labels for one series while preserving chart-level labels for others.");

        chart.Series[1].UseChartDataLabels();
        Assert(CountOccurrences(chart.ToSvg(), "data-cfx-role=\"data-label\"") == 2, "Clearing a series data-label override should restore chart-level label behavior.");
    }

    private static void DensePointLabelsAvoidCollisions() {
        var points = new[] {
            new ChartPoint(1, 50),
            new ChartPoint(1, 50.1),
            new ChartPoint(1, 50.2),
            new ChartPoint(1, 50.3),
            new ChartPoint(1, 50.4)
        };
        var svg = Chart.Create().WithSize(320, 220).WithDataLabels().AddScatter("Dense", points).ToSvg();
        var labelCount = CountOccurrences(svg, "data-cfx-role=\"data-label\"");
        Assert(labelCount > 0 && labelCount < points.Length, "Dense point labels should render a useful subset instead of overlapping every label.");
    }

    private static void DataLabelsUseReadableEdgeAwareStyling() {
        var svg = Chart.Create().WithSize(420, 280).WithDataLabels().AddLine("Values", Points(1000, 900, 1000)).ToSvg();
        Assert(svg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Data labels should be identifiable in SVG output.");
        Assert(svg.Contains("paint-order=\"stroke fill\"", StringComparison.Ordinal), "Data labels should render with a text halo for readability.");
        Assert(svg.Contains("dominant-baseline=\"middle\"", StringComparison.Ordinal), "Data labels should use stable vertical alignment.");

        var longSvg = Chart.Create()
            .WithSize(220, 140)
            .WithDataLabels()
            .WithValueFormatter(_ => "Extremely long remediation status label that must fit")
            .AddBar("Values", Points(72))
            .ToSvg();
        Assert(longSvg.Contains("data-cfx-role=\"data-label\"", StringComparison.Ordinal), "Long SVG data labels should still render as identifiable data labels.");
        Assert(longSvg.Contains(">Extremely long...</text>", StringComparison.Ordinal), "SVG data labels should shorten formatter output that cannot fit inside the plot.");
    }

    private static void CustomValueFormatterAffectsSvgValues() {
        var svg = Chart.Create()
            .WithSize(520, 320)
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + " ms")
            .AddBar("Latency", Points(42, 84, 126))
            .ToSvg();
        Assert(svg.Contains(">42 ms</text>", StringComparison.Ordinal), "Custom value formatters should apply to data labels.");
        Assert(svg.Contains(">0 ms</text>", StringComparison.Ordinal), "Custom value formatters should apply to y-axis tick labels.");
    }

    private static void CustomXAxisFormatterAffectsSvgAndPngTicks() {
        var chart = Chart.Create()
            .WithSize(520, 320)
            .WithXAxisBounds(0, 4)
            .WithTickCount(5)
            .WithXAxisValueFormatter(value => "D+" + value.ToString("0", CultureInfo.InvariantCulture))
            .AddLine("Incidents", new[] { new ChartPoint(0, 12), new ChartPoint(2, 18), new ChartPoint(4, 16) });
        var svg = chart.ToSvg();
        Assert(svg.Contains(">D+0</text>", StringComparison.Ordinal), "Custom x-axis formatters should apply to generated SVG x-axis tick labels.");
        Assert(svg.Contains(">D+4</text>", StringComparison.Ordinal), "Custom x-axis formatters should apply to generated SVG x-axis tick labels.");
        Assert(chart.ToPng().Length > 64, "Custom x-axis formatters should render valid PNG output.");

        var labeled = Chart.Create()
            .WithSize(420, 280)
            .WithXAxisValueFormatter(_ => "formatted")
            .WithXLabels("Alpha", "Beta")
            .AddBar("Values", Points(10, 20))
            .ToSvg();
        Assert(labeled.Contains(">Alpha</text>", StringComparison.Ordinal), "Explicit x-axis labels should take precedence over generated x-axis formatters.");
        Assert(!labeled.Contains(">formatted</text>", StringComparison.Ordinal), "Explicit x-axis labels should not be replaced by generated x-axis formatters.");

        var horizontal = Chart.Create()
            .WithSize(520, 320)
            .WithXAxisBounds(0, 100)
            .WithTickCount(6)
            .WithXAxisValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + "%")
            .WithXLabels("Critical", "High")
            .AddHorizontalBar("Risk", Points(70, 40))
            .ToSvg();
        Assert(horizontal.Contains(">100%</text>", StringComparison.Ordinal), "Horizontal bar value axes should use x-axis formatters.");
        Assert(horizontal.Contains(">Critical</text>", StringComparison.Ordinal), "Horizontal bar categories should still use explicit category labels.");

        var plainRotated = Chart.Create()
            .WithSize(420, 280)
            .WithLegend(false)
            .WithXAxis("Elapsed")
            .WithXAxisBounds(0, 4)
            .WithTickCount(5)
            .WithXAxisLabelAngle(-55)
            .AddLine("Incidents", new[] { new ChartPoint(0, 12), new ChartPoint(2, 18), new ChartPoint(4, 16) })
            .ToSvg();
        var longRotatedChart = Chart.Create()
            .WithSize(420, 280)
            .WithLegend(false)
            .WithXAxis("Elapsed")
            .WithXAxisBounds(0, 4)
            .WithTickCount(5)
            .WithXAxisLabelAngle(-55)
            .WithXAxisValueFormatter(value => "Checkpoint " + value.ToString("0", CultureInfo.InvariantCulture))
            .AddLine("Incidents", new[] { new ChartPoint(0, 12), new ChartPoint(2, 18), new ChartPoint(4, 16) });
        var longRotated = longRotatedChart.ToSvg();
        Assert(GetAttribute(longRotated, "<clipPath", "height") < GetAttribute(plainRotated, "<clipPath", "height"), "Rotated generated x-axis labels should reserve more SVG bottom space when formatting makes them longer.");
        Assert(longRotatedChart.ToPng().Length > 64, "Rotated generated x-axis labels should render valid PNG output when formatting makes them longer.");
    }

    private static void LongFormattedYAxisLabelsReservePlotSpace() {
        var svg = Chart.Create()
            .WithSize(420, 280)
            .WithValueFormatter(value => "$" + value.ToString("N0", CultureInfo.InvariantCulture) + " ms")
            .AddLine("Latency budget", Points(1000000, 1120000, 1080000))
            .ToSvg();
        Assert(GetAttribute(svg, "<clipPath", "x") > 76, "Long formatted y-axis labels should push the SVG plot area to the right.");
        Assert(svg.Contains("$1,000,000 ms", StringComparison.Ordinal) || svg.Contains("$1,200,000 ms", StringComparison.Ordinal), "Long formatted y-axis labels should render.");
    }

    private static void LongAxisTitlesFitAvailableSpace() {
        const string longTitle = "Extremely long remediation status axis title that must fit inside the chart";
        var svg = Chart.Create()
            .WithSize(240, 180)
            .WithXAxis(longTitle)
            .WithYAxis(longTitle)
            .AddLine("Values", Points(10, 20, 30))
            .ToSvg();
        Assert(svg.Contains("...</text>", StringComparison.Ordinal), "SVG axis titles should shorten when the chart cannot fit the full title.");
        Assert(Chart.Create().WithSize(240, 180).WithXAxis(longTitle).WithYAxis(longTitle).AddLine("Values", Points(10, 20, 30)).ToPng().Length > 64, "PNG axis titles should render valid output when long titles require fitting.");
    }

    private static void LongHeaderTextFitsAvailableSpace() {
        const string longTitle = "Extremely long remediation posture report title that should fit inside the chart header";
        const string longSubtitle = "Detailed subtitle with enough operational context to exceed a compact chart width";
        var chart = Chart.Create()
            .WithSize(260, 180)
            .WithTitle(longTitle)
            .WithSubtitle(longSubtitle)
            .AddLine("Values", Points(10, 20, 30));
        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"chart-title\"", StringComparison.Ordinal), "SVG header titles should expose a stable role marker.");
        Assert(svg.Contains("data-cfx-role=\"chart-subtitle\"", StringComparison.Ordinal), "SVG header subtitles should expose a stable role marker.");
        Assert(svg.Contains("...</text>", StringComparison.Ordinal), "SVG header text should shorten when it cannot fit in the chart width.");
        Assert(chart.ToPng().Length > 64, "PNG header text should render valid output when long title and subtitle fitting is required.");
    }

    private static void AnnotationsRenderInSvg() {
        var svg = Chart.Create()
            .WithSize(640, 360)
            .AddLine("Values", Points(42, 84, 126))
            .AddHorizontalLine(100, "target", ChartColor.FromRgb(251, 191, 36))
            .AddVerticalBand(1.5, 2.5, "window", ChartColor.FromRgb(96, 165, 250), 0.1)
            .ToSvg();
        Assert(svg.Contains(">target</text>", StringComparison.Ordinal), "Horizontal annotation label should render.");
        Assert(svg.Contains(">window</text>", StringComparison.Ordinal), "Band annotation label should render.");
        Assert(svg.Contains("stroke-dasharray=\"6 5\"", StringComparison.Ordinal), "Line annotations should render as dashed lines.");
    }

    private static void StatisticalOverlaysRenderComputedAnnotations() {
        var points = Points(10, 20, 30, 40);
        var chart = Chart.Create()
            .WithSize(640, 360)
            .AddLine("Values", points)
            .AddMeanLine("mean", points, ChartColor.FromRgb(245, 158, 11))
            .AddMedianLine("median", points, ChartColor.FromRgb(14, 165, 233))
            .AddStandardDeviationBand("1 sigma", points, 1, ChartColor.FromRgb(96, 165, 250), 0.16);
        var svg = chart.ToSvg();
        Assert(svg.Contains(">mean</text>", StringComparison.Ordinal), "Mean overlays should render line labels.");
        Assert(svg.Contains(">median</text>", StringComparison.Ordinal), "Median overlays should render line labels.");
        Assert(svg.Contains(">1 sigma</text>", StringComparison.Ordinal), "Standard deviation bands should render band labels.");
        Assert(svg.Contains("opacity=\"0.16\"", StringComparison.Ordinal), "Standard deviation bands should use the requested opacity.");
        Assert(CountOccurrences(svg, "stroke-dasharray=\"6 5\"") >= 2, "Mean and median overlays should render as annotation lines.");
        Assert(chart.ToPng().Length > 64, "Statistical overlays should render PNG output.");
    }

    private static void AnnotationLabelsStayInsidePlot() {
        var svg = Chart.Create().WithSize(420, 280).AddLine("Values", Points(10, 20, 30)).AddVerticalLine(3, "right edge marker", ChartColor.FromRgb(251, 191, 36)).ToSvg();
        Assert(svg.Contains("data-cfx-role=\"annotation-label\"", StringComparison.Ordinal), "Annotation label pills should be identifiable in SVG output.");
        Assert(svg.Contains(">right edge marker</text>", StringComparison.Ordinal), "Annotation label text should render.");
        Assert(svg.Contains("text-anchor=\"end\"", StringComparison.Ordinal), "Right-edge annotation labels should switch to end alignment.");
    }

    private static void SvgIncludesAccessibilityMetadata() {
        var svg = SampleChart().ToSvg();
        Assert(svg.Contains("role=\"img\"", StringComparison.Ordinal), "SVG should expose image semantics.");
        Assert(svg.Contains("aria-labelledby=\"", StringComparison.Ordinal), "SVG should reference title and description metadata.");
        Assert(svg.Contains("<title id=\"", StringComparison.Ordinal), "SVG should include a title element.");
        Assert(svg.Contains("<desc id=\"", StringComparison.Ordinal), "SVG should include a description element.");
    }

    private static void SvgUsesReportGradeStyling() {
        var svg = Chart.Create().WithTitle("Styled").WithTheme(ChartTheme.ReportDark()).WithSize(640, 360)
            .AddSmoothLine("Values", Points(10, 30, 20), ChartColor.FromRgb(96, 165, 250))
            .AddHorizontalLine(25, "target", ChartColor.FromRgb(251, 191, 36))
            .ToSvg();
        Assert(svg.Contains("-seriesFill0", StringComparison.Ordinal), "SVG should include series fill gradients.");
        Assert(svg.Contains("stroke-opacity=\"0.34\"", StringComparison.Ordinal), "Annotation labels should render as legible pills.");
        Assert(svg.Contains("font-weight=\"750\"", StringComparison.Ordinal), "SVG should use stronger title and label typography.");
    }

    private static void TypographyUsesNativeFontStackAndEscapesCustomFamilies() {
        Assert(SampleChart().ToSvg().Contains("-apple-system, BlinkMacSystemFont", StringComparison.Ordinal), "SVG should default to a native system font stack.");
        var svg = Chart.Create().WithFontFamily("A&B \"Display\"").AddLine("Values", Points(1, 2, 3)).ToSvg();
        Assert(svg.Contains("font-family=\"A&amp;B &quot;Display&quot;\"", StringComparison.Ordinal), "SVG font-family values should be attribute-escaped.");
        var html = Chart.Create().WithFontFamily("A;B{}").AddLine("Values", Points(1, 2, 3)).ToHtmlPage();
        Assert(!html.Contains("font-family:A;B{}", StringComparison.Ordinal), "HTML font-family values should not be able to break the style declaration.");
    }

    private static void RenderingUsesInvariantCulture() {
        var currentCulture = CultureInfo.CurrentCulture;
        var currentUiCulture = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo("pl-PL");
            CultureInfo.CurrentUICulture = new CultureInfo("pl-PL");
            var css = ChartColor.FromRgba(1, 2, 3, 128).ToCss();
            Assert(css == "rgba(1,2,3,0.502)", "CSS alpha values should use invariant decimal separators.");
            var png = Chart.Create().AddDonut("Checks", Points(70, 20, 10)).ToPng();
            Assert(png.Length > 64, "PNG rendering should stay valid under non-invariant cultures.");
        } finally {
            CultureInfo.CurrentCulture = currentCulture;
            CultureInfo.CurrentUICulture = currentUiCulture;
        }
    }

    private static void ReportThemesExposeVisualTokens() {
        var theme = ChartTheme.ReportDark();
        Assert(theme.CardBorder.A > 0, "Report themes should define card borders.");
        Assert(theme.PlotBorder.A > 0, "Report themes should define plot borders.");
        Assert(theme.PlotCornerRadius > 0, "Report themes should define plot corner radius.");
        Assert(theme.Positive.A > 0 && theme.Warning.A > 0 && theme.Negative.A > 0, "Report themes should define semantic status colors.");
        Assert(theme.TitleFontSize > theme.TickLabelFontSize, "Title text should be larger than tick labels.");
    }

    private static void StandaloneHtmlUsesVisibleBackground() {
        var html = SampleChart().ToHtmlPage();
        Assert(!html.Contains("background:transparent", StringComparison.Ordinal), "Standalone pages should use a visible page background.");
        Assert(html.Contains("-webkit-font-smoothing:antialiased", StringComparison.Ordinal), "Standalone pages should request browser font smoothing.");
    }

    private static void HtmlFragmentIsResponsive() {
        var html = SampleChart().ToHtmlFragment();
        Assert(html.Contains("style=\"width:100%;max-width:640px;box-sizing:border-box\"", StringComparison.Ordinal), "HTML fragment should carry responsive wrapper styles.");
        Assert(html.Contains("style=\"max-width:100%;height:auto;display:block\"", StringComparison.Ordinal), "SVG should carry responsive sizing styles.");
    }

    private static void SmallMultipleGridRendersStaticHtml() {
        var coverage = Chart.Create().WithTitle("Coverage").WithSize(320, 220).AddBar("Values", Points(80, 72, 91));
        var readiness = Chart.Create().WithTitle("Readiness").WithSize(320, 220).AddLine("Values", Points(62, 70, 84));
        var grid = ChartGrid.Create()
            .WithTitle("Control scorecards")
            .WithSubtitle("Small multiples for a static report")
            .WithTheme(ChartTheme.ReportLight())
            .WithColumns(2)
            .WithGap(20)
            .WithPadding(30)
            .WithPanelSize(300, 200)
            .Add(coverage)
            .Add(readiness)
            .WithSharedYAxis();

        var html = grid.ToHtmlPage();
        Assert(html.Contains("<section class=\"chartforgex-grid\"", StringComparison.Ordinal), "Chart grids should render a stable report container.");
        Assert(html.Contains("--cfx-grid-columns:2", StringComparison.Ordinal), "Chart grids should expose the requested column count.");
        Assert(html.Contains("--cfx-grid-gap:20px", StringComparison.Ordinal), "Chart grids should expose the requested gap.");
        Assert(html.Contains("--cfx-grid-padding:30px", StringComparison.Ordinal), "Chart grids should expose the requested padding.");
        Assert(html.Contains("--cfx-grid-panel-width:300px", StringComparison.Ordinal), "Chart grids should expose fixed panel widths.");
        Assert(html.Contains("--cfx-grid-panel-height:200px", StringComparison.Ordinal), "Chart grids should expose fixed panel heights.");
        Assert(CountOccurrences(html, "<svg ") == 2, "Chart grids should render each chart as inline SVG.");
        Assert(html.Contains(">Control scorecards</h1>", StringComparison.Ordinal), "Chart grids should render report titles.");
        Assert(!html.Contains("<script", StringComparison.OrdinalIgnoreCase), "Chart grids should remain JavaScript-free.");
        Assert(coverage.Options.YAxisMinimum == readiness.Options.YAxisMinimum && coverage.Options.YAxisMaximum == readiness.Options.YAxisMaximum, "Shared y-axis grids should apply equal y-axis bounds to compatible charts.");
        Assert(coverage.ToPng().Length > 64 && readiness.ToPng().Length > 64, "Shared y-axis bounds should apply to PNG rendering too.");
        var early = Chart.Create().WithSize(300, 200).AddLine("Early", new[] { new ChartPoint(2, 10), new ChartPoint(3, 20) });
        var late = Chart.Create().WithSize(300, 200).AddLine("Late", new[] { new ChartPoint(6, 18), new ChartPoint(8, 28) });
        ChartGrid.Create().Add(early).Add(late).WithSharedAxes();
        Assert(early.Options.XAxisMinimum == late.Options.XAxisMinimum && early.Options.XAxisMaximum == late.Options.XAxisMaximum, "Shared x-axis grids should apply equal x-axis bounds to compatible charts.");
        Assert(early.Options.YAxisMinimum == late.Options.YAxisMinimum && early.Options.YAxisMaximum == late.Options.YAxisMaximum, "Shared-axis grids should apply equal y-axis bounds to compatible charts.");
        Assert(early.ToPng().Length > 64 && late.ToSvg().Contains("<svg", StringComparison.Ordinal), "Shared x-axis bounds should apply to SVG and PNG rendering.");

        var svg = grid.ToSvg();
        Assert(svg.StartsWith("<svg", StringComparison.Ordinal), "Chart grids should export standalone SVG.");
        Assert(CountOccurrences(svg, "data:image/svg+xml;base64,") == 2, "Chart grid SVG should embed each child chart without id collisions.");
        Assert(svg.Contains("width=\"291\" height=\"200\"", StringComparison.Ordinal), "Fixed panel grid exports should contain charts without distorting their aspect ratio.");
        var png = grid.ToPng();
        Assert(ReadBigEndianInt32(png, 16) == 680, "Chart grid PNG should use fixed panel width and custom padding.");
        Assert(ReadBigEndianInt32(png, 20) == 336, "Chart grid PNG should use fixed panel height and custom padding.");

        var stretched = ChartGrid.Create().WithPanelSize(300, 200).WithPanelFit(ChartGridPanelFit.Stretch).Add(coverage);
        Assert(stretched.ToSvg().Contains("width=\"300\" height=\"200\"", StringComparison.Ordinal), "Stretch panel grids should use the full fixed panel size.");

        var compact = ChartGrid.Create()
            .WithTitle("Extremely long small multiple grid title that should not overflow exported report bounds")
            .WithPadding(16)
            .WithPanelSize(220, 140)
            .Add(Chart.Create().WithTitle("Tiny").WithSize(220, 140).AddLine("Values", Points(10, 20, 30)));
        Assert(compact.ToSvg().Contains("...</text>", StringComparison.Ordinal), "Composed SVG grid headers should shorten long titles.");
        Assert(compact.ToPng().Length > 64, "Composed PNG grid headers should render even when long titles require fitting.");

        grid.WithAutomaticPanelSize().WithAutomaticTheme();
        Assert(!grid.PanelSize.HasValue && grid.Theme == null, "Automatic grid panel and theme settings should clear explicit export controls.");
    }

    private static void ExplicitAxisBoundsAffectSvgTicks() {
        var chart = Chart.Create().WithSize(420, 280).WithXAxisBounds(0, 10).WithYAxisBounds(0, 100).AddLine("Values", Points(20, 40, 80));
        var svg = chart.ToSvg();
        Assert(svg.Contains(">10</text>", StringComparison.Ordinal), "Explicit x-axis bounds should affect SVG tick labels.");
        Assert(svg.Contains(">100</text>", StringComparison.Ordinal), "Explicit y-axis bounds should affect SVG tick labels.");
        Assert(chart.Options.XAxisMinimum == 0 && chart.Options.XAxisMaximum == 10, "Explicit x-axis bounds should be stored on chart options.");
        Assert(chart.Options.YAxisMinimum == 0 && chart.Options.YAxisMaximum == 100, "Explicit y-axis bounds should be stored on chart options.");
        chart.WithAutomaticXAxisBounds().WithAutomaticYAxisBounds();
        Assert(!chart.Options.XAxisMinimum.HasValue && !chart.Options.XAxisMaximum.HasValue, "Automatic x-axis bounds should clear explicit bounds.");
        Assert(!chart.Options.YAxisMinimum.HasValue && !chart.Options.YAxisMaximum.HasValue, "Automatic y-axis bounds should clear explicit bounds.");
    }

    private static void DenseXAxisLabelsAreAutomaticallyReduced() {
        var labels = Enumerable.Range(1, 20).Select(value => "Checkpoint " + value.ToString("00", CultureInfo.InvariantCulture)).ToArray();
        var auto = Chart.Create().WithSize(420, 280).WithXLabels(labels).AddLine("Values", Points(Enumerable.Range(1, 20).Select(value => (double)value).ToArray())).ToSvg();
        Assert(auto.Contains(">Checkpoint 01</text>", StringComparison.Ordinal), "Automatic x-axis label thinning should preserve the first label.");
        Assert(auto.Contains(">Checkpoint 20</text>", StringComparison.Ordinal), "Automatic x-axis label thinning should preserve the last label.");
        Assert(!auto.Contains(">Checkpoint 02</text>", StringComparison.Ordinal), "Automatic x-axis label thinning should omit intermediate labels when space is tight.");
        var all = Chart.Create().WithSize(420, 280).WithXAxisLabelDensity(ChartLabelDensity.All).WithXLabels(labels).AddLine("Values", Points(Enumerable.Range(1, 20).Select(value => (double)value).ToArray())).ToSvg();
        Assert(all.Contains(">Checkpoint 02</text>", StringComparison.Ordinal), "All label density should preserve every explicit x-axis label.");
    }

    private static void EdgeXAxisLabelsStayInsidePlot() {
        var svg = Chart.Create().WithSize(420, 280).WithXAxisLabelDensity(ChartLabelDensity.All).WithXLabels("January", "February", "March", "April", "May", "December").AddLine("Values", Points(10, 20, 15, 30, 24, 35)).ToSvg();
        Assert(svg.Contains("text-anchor=\"start\"", StringComparison.Ordinal) && svg.Contains(">January</text>", StringComparison.Ordinal), "First x-axis label should be start-aligned.");
        Assert(svg.Contains("text-anchor=\"end\"", StringComparison.Ordinal) && svg.Contains(">December</text>", StringComparison.Ordinal), "Last x-axis label should be end-aligned.");
    }

    private static void XAxisLabelsCanBeRotated() {
        var svg = Chart.Create().WithSize(520, 340).WithXAxis("Month").WithXAxisLabelAngle(-35).WithXAxisLabelDensity(ChartLabelDensity.All).WithXLabels("January", "February", "March").AddLine("Values", Points(10, 20, 30)).ToSvg();
        Assert(svg.Contains("transform=\"rotate(-35", StringComparison.Ordinal), "SVG should rotate x-axis labels when requested.");
        Assert(svg.Contains("dominant-baseline=\"middle\"", StringComparison.Ordinal), "Rotated labels should use a stable text baseline.");
    }

    private static void LargeSvgValuesUseCompactUnits() {
        var svg = Chart.Create().WithSize(640, 360).AddLine("Values", Points(1200000, 2400000, 3600000)).ToSvg();
        Assert(svg.Contains(">1M</text>", StringComparison.Ordinal) || svg.Contains(">1.2M</text>", StringComparison.Ordinal), "Large SVG values should use M suffixes instead of thousands of k.");
    }

    private static void LegendRowsWrapWithRoleMarkers() {
        var svg = Chart.Create().WithSize(420, 320)
            .AddLine("Primary domain checks", Points(1, 2, 3))
            .AddLine("Certificate transparency drift", Points(2, 3, 4))
            .AddLine("Dnssec policy posture", Points(3, 4, 5))
            .AddLine("Mail authentication alignment", Points(4, 5, 6))
            .ToSvg();
        Assert(svg.Contains("data-cfx-role=\"legend\"", StringComparison.Ordinal), "SVG should expose a semantic legend group.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"legend-row\"") > 1, "Long legends should wrap into multiple rows.");

        var longSvg = Chart.Create().WithSize(320, 220)
            .AddLine("Extremely long certificate transparency drift monitor", Points(1, 2, 3))
            .AddLine("Extremely long DNSSEC posture remediation backlog", Points(2, 3, 4))
            .ToSvg();
        Assert(longSvg.Contains("...</text>", StringComparison.Ordinal), "SVG legends should shorten series names that exceed the bounded legend lane.");
    }

    private static void SvgHasNoInvalidNumbers() {
        var svg = SampleChart().ToSvg();
        Assert(!svg.Contains("NaN", StringComparison.Ordinal), "SVG should not contain NaN values.");
        Assert(!svg.Contains("Infinity", StringComparison.Ordinal), "SVG should not contain infinity values.");
    }

    private static void DateXAxisLabelsRender() {
        var start = new DateTime(2026, 1, 1);
        var dates = new[] { start, start.AddDays(1), start.AddDays(2) };
        var svg = Chart.Create().WithSize(640, 360).WithXDateLabels(dates, "MMM dd").AddLine("Values", DatePoints(dates, 10, 20, 30)).ToSvg();
        Assert(svg.Contains(">Jan 01</text>", StringComparison.Ordinal), "Date x-axis labels should render.");
        Assert(svg.Contains(">Jan 03</text>", StringComparison.Ordinal), "Date x-axis labels should render.");
    }

    private static void SparklineHidesReportChrome() {
        var chart = Chart.Create().WithTitle("Tiny trend").WithSize(240, 64).WithSparkline().AddSmoothArea("Trend", Points(10, 14, 13, 19, 24, 22));
        var svg = chart.ToSvg();
        Assert(chart.Options.IsSparkline, "Sparkline option should be enabled.");
        Assert(!svg.Contains(">Tiny trend</text>", StringComparison.Ordinal), "Sparkline should not render visible title text.");
        Assert(!svg.Contains("font-size=\"11\">0</text>", StringComparison.Ordinal), "Sparkline should not render axis tick labels.");
    }

    private static void PublicApiRejectsInvalidInputs() {
        AssertThrows<ArgumentNullException>(() => Chart.Create().WithTitle(null!), "Chart titles should reject null values.");
        AssertThrows<ArgumentNullException>(() => Chart.Create().Title = null!, "Chart title property should reject null values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithSize(0, 360), "Chart sizes should reject non-positive dimensions.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPadding(1, double.NaN, 1, 1), "Chart padding should reject non-finite values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithTickCount(1), "Tick counts should reject values below two.");
        AssertThrows<ArgumentNullException>(() => Chart.Create().Options.Theme = null!, "Chart options should reject null themes.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.TickCount = 1, "Chart options should reject invalid tick counts.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.XAxisLabelDensity = (ChartLabelDensity)999, "Chart options should reject unknown label density values.");
        AssertThrows<ArgumentException>(() => Chart.Create().WithPngFont(" "), "PNG font paths should reject empty values.");
        AssertThrows<ArgumentException>(() => Chart.Create().WithPngFont("font.ttc", faceName: " "), "PNG font face names should reject empty values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithPngFont("font.ttc", -1), "PNG font collection indexes should reject negative values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartSize(-1, 100), "ChartSize should reject non-positive dimensions.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartPadding.All(double.NegativeInfinity), "ChartPadding should reject non-finite values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartPoint(1, double.NaN), "ChartPoint should reject non-finite values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartRect(0, 0, -1, 10), "ChartRect should reject negative dimensions.");
        AssertThrows<ArgumentNullException>(() => new ChartAxisLabel(1, null!), "Axis labels should reject null text.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartAxisLabel(double.NaN, "bad"), "Axis labels should reject non-finite values.");
        AssertThrows<ArgumentNullException>(() => ChartTheme.Light().Palette = null!, "Themes should reject null palettes.");
        AssertThrows<ArgumentException>(() => ChartTheme.Light().Palette = Array.Empty<ChartColor>(), "Themes should reject empty palettes.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartTheme.Light().TitleFontSize = 0, "Themes should reject non-positive font sizes.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartTheme.Light().ShadowOpacity = 2, "Themes should reject opacity values outside zero to one.");
        AssertThrows<ArgumentNullException>(() => ChartTheme.Light().FontFamily = null!, "Themes should reject null font families.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddLine("Values", new[] { new ChartPoint(1, double.PositiveInfinity) }), "Series should reject non-finite point values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddLine("Values", Points(1, 2, 3)).Series[0].StrokeWidth = 0, "Series stroke widths should reject non-positive values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddLine("Values", Points(1, 2, 3)).Series[0].StrokeWidth = double.NaN, "Series stroke widths should reject non-finite values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddLine("Values", Points(1, 2, 3)).Series[0].YAxis = (ChartAxisSide)99, "Series y-axis side should reject unknown enum values.");
        AssertThrows<ArgumentNullException>(() => Chart.Create().WithSecondaryYAxis(null!), "Secondary y-axis titles should reject null.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithSecondaryYAxisBounds(10, 10), "Secondary y-axis bounds should reject empty numeric domains.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddHistogram("Empty", Array.Empty<double>()), "Histograms should reject empty raw value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddHistogram("Bad", new[] { 1d, 2d }, 0), "Histograms should reject bin counts below one.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddHistogram("Bad", new[] { 1d, double.NaN }), "Histograms should reject non-finite raw values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTrendLine("Empty", Array.Empty<ChartPoint>()), "Trend lines should reject empty source points.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTrendLine("Vertical", new[] { new ChartPoint(1, 2), new ChartPoint(1, 4) }), "Trend lines should reject source points with no x variation.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddCircle("Bad", 50, 100, 100), "Circle charts should reject empty numeric domains.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddCircle("Bad", double.NaN), "Circle charts should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddRangeBar("Empty", Array.Empty<ChartInterval>()), "Range bars should reject empty interval sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartInterval(1, 2, double.NaN), "Chart intervals should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddBubble("Empty", Array.Empty<ChartBubble>()), "Bubble charts should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartBubble(1, 2, 0), "Bubble charts should reject non-positive sizes.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartBubble(1, 2, double.NaN), "Bubble charts should reject non-finite sizes.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddErrorBar("Empty", Array.Empty<ChartErrorBar>()), "Error bars should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartErrorBar(1, 5, 6, 8), "Error bars should reject lower bounds above the point estimate.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartErrorBar(1, 5, 2, 4), "Error bars should reject upper bounds below the point estimate.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartErrorBar(1, 5, 2, double.NaN), "Error bars should reject non-finite bounds.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddCandlestick("Empty", Array.Empty<ChartCandlestick>()), "Candlestick charts should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartCandlestick(1, 5, 4, 2, 3), "Candlestick charts should reject high values below open or close values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartCandlestick(1, 5, 8, 6, 7), "Candlestick charts should reject low values above open or close values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartCandlestick(1, 5, 8, double.NaN, 7), "Candlestick charts should reject non-finite OHLC values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddRangeBand("Empty", Array.Empty<ChartRangeBand>()), "Range bands should reject empty value sets.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddRangeArea("Empty", Array.Empty<ChartRangeBand>()), "Range areas should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartRangeBand(1, 8, 4), "Range bands should reject lower values above upper values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartRangeBand(1, double.NaN, 4), "Range bands should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddDumbbell("Empty", Array.Empty<ChartDumbbell>()), "Dumbbell charts should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartDumbbell(1, double.NaN, 4), "Dumbbell charts should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddPareto("Empty", Array.Empty<ChartParetoItem>()), "Pareto charts should reject empty item sets.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddPareto("Zero", new[] { new ChartParetoItem("A", 0), new ChartParetoItem("B", 0) }), "Pareto charts should reject all-zero item sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartParetoItem("Bad", -1), "Pareto items should reject negative values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartParetoItem("Bad", double.NaN), "Pareto items should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTreemap("Empty", Array.Empty<ChartTreemapItem>()), "Treemaps should reject empty item sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTreemapItem("Bad", -1), "Treemap items should reject negative values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTreemapItem("Bad", double.NaN), "Treemap items should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddBoxPlot("Empty", Array.Empty<ChartBoxPlot>()), "Box plots should reject empty summary sets.");
        AssertThrows<ArgumentNullException>(() => Chart.Create().AddBoxPlot("Raw", 1, null!), "Raw box plots should reject null sample sets.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddBoxPlot("Raw", 1, Array.Empty<double>()), "Raw box plots should reject empty sample sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddBoxPlot("Raw", double.PositiveInfinity, new[] { 1d, 2d }), "Raw box plots should reject non-finite x values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddBoxPlot("Raw", 1, new[] { 1d, double.NaN }), "Raw box plots should reject non-finite sample values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartBoxPlot(1, 5, 4, 3, 2, 1), "Box plots should reject unordered summary values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddGauge("Score", 80, 100, 0), "Gauges should reject inverted scales.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddRadialBar("Empty", Array.Empty<ChartPoint>()), "Radial bars should reject empty value sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddRadialBar("Bad", Points(101)), "Radial bars should reject values above 100.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddRadialBar("Bad", Points(-1)), "Radial bars should reject negative values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddTimelineRange("Task", 10, 2), "Timelines should reject inverted ranges.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddGanttTask("Task", 10, 2), "Gantt tasks should reject inverted ranges.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddGanttTask("Task", 1, 2, 1.1), "Gantt tasks should reject progress above one.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddGanttTask("Task", 1, 2, dependsOn: 0), "Gantt tasks should reject dependencies that do not reference earlier tasks.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithGanttToday(double.NaN), "Gantt today markers should reject non-finite values.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddSankey("Flow", Array.Empty<ChartSankeyLink>()), "Sankey charts should reject empty link sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartSankeyLink("A", "B", 0), "Sankey links should reject non-positive values.");
        AssertThrows<ArgumentException>(() => new ChartSankeyLink("", "B", 1), "Sankey links should reject empty source labels.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddSankey("Flow", new[] { new ChartSankeyLink("A", "A", 1) }), "Sankey charts should reject self links.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTree("Tree", Array.Empty<ChartTreeLink>()), "Tree charts should reject empty link sets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartTreeLink("A", "B", 0), "Tree links should reject non-positive values.");
        AssertThrows<ArgumentException>(() => new ChartTreeLink("", "B"), "Tree links should reject empty parent labels.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTree("Tree", new[] { new ChartTreeLink("A", "A") }), "Tree charts should reject self links.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTree("Tree", new[] { new ChartTreeLink("A", "C"), new ChartTreeLink("B", "C") }), "Tree charts should reject multiple parents for a child.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddTree("Tree", new[] { new ChartTreeLink("A", "B"), new ChartTreeLink("C", "D") }), "Tree charts should reject multiple roots.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddHorizontalBand(1, 2, opacity: 1.5), "Band opacity should reject values outside zero to one.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddMeanLine("Mean", Array.Empty<ChartPoint>()), "Mean overlays should reject empty source points.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddStandardDeviationBand("Sigma", Points(10)), "Standard deviation bands should reject single-point source data.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddStandardDeviationBand("Sigma", Points(10, 20), 0), "Standard deviation bands should reject non-positive deviation counts.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithXAxisBounds(10, 10), "X-axis bounds should reject empty ranges.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithXAxisBounds(double.NaN, 10), "X-axis bounds should reject non-finite minimum values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.XAxisMaximum = double.PositiveInfinity, "X-axis option bounds should reject non-finite maximum values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithYAxisBounds(10, 10), "Y-axis bounds should reject empty ranges.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().WithYAxisBounds(double.NaN, 10), "Y-axis bounds should reject non-finite minimum values.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().Options.YAxisMaximum = double.PositiveInfinity, "Y-axis option bounds should reject non-finite maximum values.");
        AssertThrows<ArgumentNullException>(() => ChartGrid.Create().WithTitle(null!), "Chart grids should reject null titles.");
        AssertThrows<ArgumentNullException>(() => ChartGrid.Create().Add(null!), "Chart grids should reject null charts.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().WithColumns(0), "Chart grids should reject non-positive column counts.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().WithGap(-1), "Chart grids should reject negative gaps.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().WithPadding(-1), "Chart grids should reject negative padding.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().WithPanelSize(0, 200), "Chart grids should reject non-positive panel widths.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartGrid.Create().WithPanelFit((ChartGridPanelFit)999), "Chart grids should reject unknown panel fit values.");
        AssertThrows<ArgumentNullException>(() => ChartGrid.Create().WithTheme(null!), "Chart grids should reject null themes.");
        AssertThrows<InvalidOperationException>(() => ChartGrid.Create().WithSharedXAxis(), "Shared x-axis grids should reject empty grids.");
        AssertThrows<InvalidOperationException>(() => ChartGrid.Create().Add(Chart.Create().AddGauge("Score", 87)).WithSharedXAxis(), "Shared x-axis grids should require cartesian charts.");
        AssertThrows<InvalidOperationException>(() => ChartGrid.Create().WithSharedAxes(), "Shared-axis grids should reject empty grids.");
        AssertThrows<InvalidOperationException>(() => ChartGrid.Create().WithSharedYAxis(), "Shared y-axis grids should reject empty grids.");
        AssertThrows<InvalidOperationException>(() => ChartGrid.Create().Add(Chart.Create().AddGauge("Score", 87)).WithSharedYAxis(), "Shared y-axis grids should require cartesian charts.");
        AssertThrows<ArgumentNullException>(() => ChartExtensions.ToSvg((ChartGrid)null!), "Chart grid SVG export should reject null grids.");
        AssertThrows<ArgumentNullException>(() => ChartExtensions.ToPng((ChartGrid)null!), "Chart grid PNG export should reject null grids.");
        AssertThrows<ArgumentNullException>(() => ChartExtensions.GetPngFontInfo(null!), "PNG font diagnostics should reject null charts.");
        AssertThrows<ArgumentNullException>(() => new HtmlChartRenderer().RenderFragment(null!), "HTML fragment rendering should reject null charts.");
        AssertThrows<ArgumentNullException>(() => new HtmlChartRenderer().RenderPage(null!), "HTML page rendering should reject null charts.");
        AssertThrows<ArgumentNullException>(() => new HtmlChartGridRenderer().RenderPage(null!), "HTML grid rendering should reject null grids.");
        AssertThrows<InvalidOperationException>(() => ChartGrid.Create().ToHtmlPage(), "HTML grid rendering should reject empty grids.");
    }

    private static void SpecializedChartsRejectMixedSeries() {
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddGauge("Score", 87).AddLine("Trend", Points(1, 2, 3)).ToSvg(), "SVG rendering should reject mixed specialized and cartesian series.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddDonut("Checks", Points(70, 20)).AddPie("Other", Points(1, 2)).ToPng(), "PNG rendering should reject multiple pie-like series.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddGauge("Score", 87).AddGauge("Other", 72).ToSvg(), "Single-panel specialized charts should reject multiple series.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddPie("Empty", Points(0, 0, 0)).ToSvg(), "Pie charts should reject data with no positive slice values.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddDonut("Empty", Points(-3, 0, -2)).ToPng(), "Donut charts should reject data with no positive slice values.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddFunnel("Empty", Points(0, -1, 0)).ToSvg(), "Funnel charts should reject data with no positive stage values.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddTreemap("Empty", new[] { new ChartTreemapItem("A", 0), new ChartTreemapItem("B", 0) }).ToPng(), "Treemaps should reject data with no positive tile values.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddPolarArea("Empty", Points(0, -1, 0)).ToSvg(), "Polar-area charts should reject data with no positive segment values.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddWaterfall("Empty", Array.Empty<ChartPoint>()).ToSvg(), "Waterfall charts should reject empty data instead of rendering a blank chart.");
        AssertThrows<InvalidOperationException>(() => Chart.Create().AddRadar("Too small", Points(72, 84)).ToPng(), "Radar charts should reject fewer than three categories instead of rendering a blank chart.");
    }
}
