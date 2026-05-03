using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void UsStateGeoMapRendersGeographicChoroplethRegions() {
        var chart = Chart.Create()
            .WithSize(760, 420)
            .WithTitle("Revenue by state")
            .WithMapLabels(false)
            .AddUsStateGeoMap("Revenue", new[] {
                new ChartRegionMapItem("CA", 95, ChartColor.FromHex("#2563EB")),
                new ChartRegionMapItem("NY", 82),
                new ChartRegionMapItem("TX", 74),
                new ChartRegionMapItem("FL", 61)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"us-state-geo-map\"", StringComparison.Ordinal), "US state geographic maps should expose a role marker.");
        Assert(svg.Contains("data-cfx-role=\"us-state-geo-map\" data-cfx-map-kind=\"us-state-geographic\" data-cfx-label=\"Revenue\" data-cfx-region-count=\"51\" data-cfx-filled-region-count=\"4\" data-cfx-missing-region-count=\"47\"", StringComparison.Ordinal), "US state geographic map containers should expose label and region coverage metadata.");
        Assert(svg.Contains("data-cfx-min-value=\"61\" data-cfx-max-value=\"95\"", StringComparison.Ordinal), "US state geographic map containers should expose the source value range.");
        Assert(svg.Contains("Revenue by state US state geographic map for Revenue with 4 filled regions and 47 missing regions.", StringComparison.Ordinal), "US state geographic map SVG descriptions should summarize the specialized chart shape.");
        Assert(svg.Contains("role=\"group\" aria-label=\"Revenue US state geographic map with 4 filled regions and 47 missing regions\"", StringComparison.Ordinal), "US state geographic map containers should expose a useful group label.");
        Assert(!svg.Contains("data-cfx-role=\"legend\"", StringComparison.Ordinal), "US state geographic maps should not emit generic series legends.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"us-state-geo-map-region\"") == 51, "US state geographic maps should render 50 states plus DC.");
        Assert(svg.Contains("data-cfx-region=\"CA\" data-cfx-region-name=\"California\" data-cfx-value=\"95\"", StringComparison.Ordinal), "US state geographic maps should expose region names and values.");
        Assert(svg.Contains("<title>California (CA): 95</title>", StringComparison.Ordinal), "US state geographic map regions should expose native SVG hover titles.");
        Assert(svg.Contains("class=\"cfx-interactive-region\" tabindex=\"0\" focusable=\"true\" data-cfx-role=\"us-state-geo-map-region\"", StringComparison.Ordinal), "US state geographic map regions should be keyboard-focusable interactive SVG regions.");
        Assert(svg.Contains("data-cfx-region=\"DC\" data-cfx-region-name=\"District of Columbia\" data-cfx-value=\"0\" data-cfx-empty=\"true\"", StringComparison.Ordinal), "US state geographic maps should mark states without data.");
        Assert(svg.Contains("data-cfx-region=\"DC\" data-cfx-region-name=\"District of Columbia\" data-cfx-value=\"0\" data-cfx-empty=\"true\" data-cfx-status=\"empty\"", StringComparison.Ordinal), "US state geographic maps should expose empty status metadata for missing regions.");
        Assert(!svg.Contains("data-cfx-role=\"us-state-geo-map-label\"", StringComparison.Ordinal), "US state geographic maps should allow labels to be hidden.");
        Assert(svg.Contains("data-cfx-role=\"us-state-geo-map-scale-step\"", StringComparison.Ordinal), "US state geographic maps should render a value scale.");
        Assert(svg.Contains("data-cfx-role=\"us-state-geo-map-scale-no-data\"", StringComparison.Ordinal), "US state geographic maps should explain missing regions separately from the value scale.");
        Assert(svg.Contains("data-cfx-role=\"us-state-geo-map-scale-no-data\" data-cfx-status=\"empty\"", StringComparison.Ordinal), "US state geographic maps should tag missing-data scale swatches as empty.");
        Assert(svg.Contains("fill=\"#2E69EC\"", StringComparison.Ordinal), "US state geographic maps should honor per-region colors inside the choropleth scale.");
        Assert(chart.ToPng().Length > 64, "US state geographic maps should render PNG output.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddUsStateGeoMap("Empty", Array.Empty<ChartRegionMapItem>()), "US state geographic maps should reject empty inputs.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddUsStateGeoMap("Bad", new[] { new ChartRegionMapItem("ZZ", 1) }), "US state geographic maps should reject unknown region codes.");
    }

    private static void UsStateTileMapRendersRegionalChoroplethTiles() {
        var chart = Chart.Create()
            .WithSize(760, 420)
            .WithTitle("Revenue by state")
            .AddUsStateTileMap("Revenue", new[] {
                new ChartRegionMapItem("CA", 95, ChartColor.FromHex("#2563EB")),
                new ChartRegionMapItem("NY", 82),
                new ChartRegionMapItem("TX", 74),
                new ChartRegionMapItem("FL", 61),
                new ChartRegionMapItem("WA", 55),
                new ChartRegionMapItem("CA", 5)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"us-state-tile-map\"", StringComparison.Ordinal), "US state tile maps should expose a role marker.");
        Assert(svg.Contains("data-cfx-role=\"us-state-tile-map\" data-cfx-map-kind=\"us-state-tile\" data-cfx-label=\"Revenue\" data-cfx-region-count=\"51\" data-cfx-filled-region-count=\"5\" data-cfx-missing-region-count=\"46\"", StringComparison.Ordinal), "US state tile map containers should expose label and region coverage metadata.");
        Assert(svg.Contains("data-cfx-min-value=\"55\" data-cfx-max-value=\"100\"", StringComparison.Ordinal), "US state tile map containers should expose the source value range.");
        Assert(svg.Contains("Revenue by state US state tile map for Revenue with 5 filled regions and 46 missing regions.", StringComparison.Ordinal), "US state tile map SVG descriptions should summarize the specialized chart shape.");
        Assert(svg.Contains("role=\"group\" aria-label=\"Revenue US state tile map with 5 filled regions and 46 missing regions\"", StringComparison.Ordinal), "US state tile map containers should expose a useful group label.");
        Assert(!svg.Contains("data-cfx-role=\"legend\"", StringComparison.Ordinal), "US state tile maps should not emit generic series legends.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"us-state-tile-map-region\"") == 51, "US state tile maps should render 50 states plus DC.");
        Assert(svg.Contains("data-cfx-region=\"CA\" data-cfx-region-name=\"California\" data-cfx-value=\"100\"", StringComparison.Ordinal), "US state tile maps should expose region names and aggregate duplicate state values.");
        Assert(svg.Contains("<title>California (CA): 100</title>", StringComparison.Ordinal), "US state tile map regions should expose native SVG hover titles.");
        Assert(svg.Contains("class=\"cfx-interactive-region\" tabindex=\"0\" focusable=\"true\" data-cfx-role=\"us-state-tile-map-region\"", StringComparison.Ordinal), "US state tile map regions should be keyboard-focusable interactive SVG regions.");
        Assert(svg.Contains("data-cfx-region=\"DC\" data-cfx-region-name=\"District of Columbia\" data-cfx-value=\"0\" data-cfx-empty=\"true\"", StringComparison.Ordinal), "US state tile maps should mark states without data.");
        Assert(svg.Contains("data-cfx-region=\"DC\" data-cfx-region-name=\"District of Columbia\" data-cfx-value=\"0\" data-cfx-empty=\"true\" data-cfx-status=\"empty\"", StringComparison.Ordinal), "US state tile maps should expose empty status metadata for missing regions.");
        Assert(svg.Contains("data-cfx-role=\"us-state-tile-map-label\"", StringComparison.Ordinal), "US state tile maps should label tiles when there is enough room.");
        Assert(svg.Contains("data-cfx-role=\"us-state-tile-map-scale-step\"", StringComparison.Ordinal), "US state tile maps should render a value scale.");
        Assert(svg.Contains("data-cfx-role=\"us-state-tile-map-scale-no-data\"", StringComparison.Ordinal), "US state tile maps should explain missing regions separately from the value scale.");
        Assert(svg.Contains("data-cfx-role=\"us-state-tile-map-scale-no-data\" data-cfx-status=\"empty\"", StringComparison.Ordinal), "US state tile maps should tag missing-data scale swatches as empty.");
        Assert(svg.Contains("fill=\"#2563EB\"", StringComparison.Ordinal), "US state tile maps should honor per-region colors.");
        Assert(chart.ToPng().Length > 64, "US state tile maps should render PNG output.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddUsStateTileMap("Empty", Array.Empty<ChartRegionMapItem>()), "US state tile maps should reject empty inputs.");
        AssertThrows<ArgumentException>(() => Chart.Create().AddUsStateTileMap("Bad", new[] { new ChartRegionMapItem("ZZ", 1) }), "US state tile maps should reject unknown region codes.");
        AssertThrows<ArgumentOutOfRangeException>(() => new ChartRegionMapItem("CA", -1), "Region map values should reject negatives.");
    }

    private static void UsStateMapsAcceptStateNamesAndDcAliases() {
        var regions = new[] {
            new ChartRegionMapItem("california", 90),
            new ChartRegionMapItem("CA", 10),
            new ChartRegionMapItem("New York", 82),
            new ChartRegionMapItem("Washington D.C.", 12)
        };

        var tileSvg = Chart.Create()
            .WithSize(760, 420)
            .AddUsStateTileMap("Revenue", regions)
            .ToSvg();
        var geoSvg = Chart.Create()
            .WithSize(760, 420)
            .WithMapLabels(false)
            .AddUsStateGeoMap("Revenue", regions)
            .ToSvg();

        Assert(tileSvg.Contains("data-cfx-region=\"CA\" data-cfx-region-name=\"California\" data-cfx-value=\"100\"", StringComparison.Ordinal), "US state tile maps should normalize state names to canonical codes and aggregate duplicates.");
        Assert(tileSvg.Contains("data-cfx-region=\"NY\" data-cfx-region-name=\"New York\" data-cfx-value=\"82\"", StringComparison.Ordinal), "US state tile maps should accept full state names.");
        Assert(tileSvg.Contains("data-cfx-region=\"DC\" data-cfx-region-name=\"District of Columbia\" data-cfx-value=\"12\"", StringComparison.Ordinal), "US state tile maps should accept common District of Columbia aliases.");
        Assert(geoSvg.Contains("data-cfx-region=\"CA\" data-cfx-region-name=\"California\" data-cfx-value=\"100\"", StringComparison.Ordinal), "US state geographic maps should normalize state names to canonical codes and aggregate duplicates.");
        Assert(geoSvg.Contains("data-cfx-region=\"NY\" data-cfx-region-name=\"New York\" data-cfx-value=\"82\"", StringComparison.Ordinal), "US state geographic maps should accept full state names.");
        Assert(geoSvg.Contains("data-cfx-region=\"DC\" data-cfx-region-name=\"District of Columbia\" data-cfx-value=\"12\"", StringComparison.Ordinal), "US state geographic maps should accept common District of Columbia aliases.");
    }

    private static void UsStateTileMapLabelsCanBeHiddenForCompactCards() {
        var chart = Chart.Create()
            .WithSize(360, 220)
            .WithMapLabels(false)
            .AddUsStateTileMap("Revenue", new[] {
                new ChartRegionMapItem("CA", 95),
                new ChartRegionMapItem("NY", 82)
            });

        var svg = chart.ToSvg();
        Assert(!svg.Contains("data-cfx-role=\"us-state-tile-map-label\"", StringComparison.Ordinal), "US state tile maps should allow compact cards to hide tile labels.");
        Assert(svg.Contains("data-cfx-role=\"us-state-tile-map-region\"", StringComparison.Ordinal), "US state tile maps should still render regions when labels are hidden.");
        Assert(chart.ToPng().Length > 64, "US state tile maps without labels should render PNG output.");
    }

    private static void UsStateGeoMapScaleLegendCanBeHiddenForCompactCards() {
        var chart = Chart.Create()
            .WithSize(420, 260)
            .WithMapScaleLegend(false)
            .WithMapLabels(false)
            .AddUsStateGeoMap("Revenue", new[] {
                new ChartRegionMapItem("CA", 95),
                new ChartRegionMapItem("NY", 82)
            });

        var svg = chart.ToSvg();
        Assert(!svg.Contains("data-cfx-role=\"us-state-geo-map-scale-step\"", StringComparison.Ordinal), "US state geographic maps should allow compact cards to hide scale legends.");
        Assert(svg.Contains("data-cfx-role=\"us-state-geo-map-region\"", StringComparison.Ordinal), "US state geographic maps should still render regions when scale legends are hidden.");
        Assert(chart.ToPng().Length > 64, "US state geographic maps without scale legends should render PNG output.");
    }

    private static void UsStateGeoMapLabelsOnlyRenderWhenRegionsHaveRoom() {
        var chart = Chart.Create()
            .WithSize(760, 420)
            .AddUsStateGeoMap("Revenue", new[] {
                new ChartRegionMapItem("CA", 95),
                new ChartRegionMapItem("DC", 42)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"us-state-geo-map-label\" x=", StringComparison.Ordinal), "US state geographic maps should render labels for states that have enough room.");
        Assert(svg.Contains(">CA</text>", StringComparison.Ordinal), "US state geographic maps should label large states by default.");
        Assert(!svg.Contains(">DC</text>", StringComparison.Ordinal), "US state geographic maps should avoid cramped labels for tiny regions.");
        Assert(!svg.Contains(">MA</text>", StringComparison.Ordinal), "US state geographic maps should avoid cramped labels in dense Northeast regions.");
        Assert(svg.Contains("<title>District of Columbia (DC): 42</title>", StringComparison.Ordinal), "US state geographic maps should preserve tiny-region identity through hover titles.");
        Assert(chart.ToPng().Length > 64, "US state geographic maps with fitted labels should render PNG output.");
    }

    private static void UsStateTileMapScaleLegendCanBeHiddenForCompactCards() {
        var chart = Chart.Create()
            .WithSize(360, 220)
            .WithMapScaleLegend(false)
            .AddUsStateTileMap("Revenue", new[] {
                new ChartRegionMapItem("CA", 95),
                new ChartRegionMapItem("NY", 82)
            });

        var svg = chart.ToSvg();
        Assert(!svg.Contains("data-cfx-role=\"us-state-tile-map-scale-step\"", StringComparison.Ordinal), "US state tile maps should allow compact cards to hide scale legends.");
        Assert(svg.Contains("data-cfx-role=\"us-state-tile-map-region\"", StringComparison.Ordinal), "US state tile maps should still render regions when scale legends are hidden.");
        Assert(chart.ToPng().Length > 64, "US state tile maps without scale legends should render PNG output.");
    }

    private static void UsStateMapScaleLegendSeparatesNoDataFromLowValues() {
        var completeRegions = AllUsStateRegions();
        var geo = Chart.Create()
            .WithSize(760, 420)
            .AddUsStateGeoMap("Revenue", completeRegions);
        var tile = Chart.Create()
            .WithSize(760, 420)
            .AddUsStateTileMap("Revenue", completeRegions);

        var geoSvg = geo.ToSvg();
        var tileSvg = tile.ToSvg();
        Assert(!geoSvg.Contains("data-cfx-role=\"us-state-geo-map-scale-no-data\"", StringComparison.Ordinal), "Complete US state geographic maps should not reserve a missing-data legend swatch.");
        Assert(!tileSvg.Contains("data-cfx-role=\"us-state-tile-map-scale-no-data\"", StringComparison.Ordinal), "Complete US state tile maps should not reserve a missing-data legend swatch.");
        Assert(CountOccurrences(geoSvg, "data-cfx-role=\"us-state-geo-map-scale-step\"") == 5, "US state geographic maps should render five value-colored scale steps.");
        Assert(CountOccurrences(tileSvg, "data-cfx-role=\"us-state-tile-map-scale-step\"") == 5, "US state tile maps should render five value-colored scale steps.");
        Assert(geoSvg.Contains("data-cfx-role=\"us-state-geo-map-scale-step\" data-cfx-value=\"10\" data-cfx-status=\"negative\"", StringComparison.Ordinal), "US state geographic map scale steps should expose the low value as data, not as no-data.");
        Assert(tileSvg.Contains("data-cfx-role=\"us-state-tile-map-scale-step\" data-cfx-value=\"10\" data-cfx-status=\"negative\"", StringComparison.Ordinal), "US state tile map scale steps should expose the low value as data, not as no-data.");
        Assert(geo.ToPng().Length > 64, "Complete US state geographic map scale legends should render PNG output.");
        Assert(tile.ToPng().Length > 64, "Complete US state tile map scale legends should render PNG output.");
    }

    private static void UsStateMapNoDataScaleStaysInsideCompactCards() {
        var geo = Chart.Create()
            .WithSize(300, 210)
            .WithMapLabels(false)
            .AddUsStateGeoMap("Revenue", new[] {
                new ChartRegionMapItem("CA", 95),
                new ChartRegionMapItem("NY", 82)
            });
        var tile = Chart.Create()
            .WithSize(300, 210)
            .WithMapLabels(false)
            .AddUsStateTileMap("Revenue", new[] {
                new ChartRegionMapItem("CA", 95),
                new ChartRegionMapItem("NY", 82)
            });

        var geoSvg = geo.ToSvg();
        var tileSvg = tile.ToSvg();
        var geoWidth = GetAttribute(geoSvg, "<svg", "width");
        var tileWidth = GetAttribute(tileSvg, "<svg", "width");
        var geoHeight = GetAttribute(geoSvg, "<svg", "height");
        var tileHeight = GetAttribute(tileSvg, "<svg", "height");
        var geoNoDataX = GetAttribute(geoSvg, "data-cfx-role=\"us-state-geo-map-scale-no-data\"", "x");
        var tileNoDataX = GetAttribute(tileSvg, "data-cfx-role=\"us-state-tile-map-scale-no-data\"", "x");
        var geoNoDataY = GetAttribute(geoSvg, "data-cfx-role=\"us-state-geo-map-scale-no-data\"", "y");
        var tileNoDataY = GetAttribute(tileSvg, "data-cfx-role=\"us-state-tile-map-scale-no-data\"", "y");
        var geoScaleX = GetAttribute(geoSvg, "data-cfx-role=\"us-state-geo-map-scale-step\"", "x");
        var tileScaleX = GetAttribute(tileSvg, "data-cfx-role=\"us-state-tile-map-scale-step\"", "x");

        Assert(geoNoDataX >= 0 && geoNoDataX < geoWidth, "US state geographic map no-data scale swatches should remain inside compact SVG cards.");
        Assert(tileNoDataX >= 0 && tileNoDataX < tileWidth, "US state tile map no-data scale swatches should remain inside compact SVG cards.");
        Assert(geoNoDataY >= 0 && geoNoDataY < geoHeight, "US state geographic map no-data scale swatches should remain vertically inside compact SVG cards.");
        Assert(tileNoDataY >= 0 && tileNoDataY < tileHeight, "US state tile map no-data scale swatches should remain vertically inside compact SVG cards.");
        Assert(geoNoDataX < geoScaleX, "US state geographic map no-data swatches should not overlap the value scale start.");
        Assert(tileNoDataX < tileScaleX, "US state tile map no-data swatches should not overlap the value scale start.");
        Assert(geo.ToPng().Length > 64, "Compact US state geographic no-data scale legends should render PNG output.");
        Assert(tile.ToPng().Length > 64, "Compact US state tile no-data scale legends should render PNG output.");
    }

    private static ChartRegionMapItem[] AllUsStateRegions() {
        var codes = new[] {
            "AK", "AL", "AR", "AZ", "CA", "CO", "CT", "DC", "DE", "FL",
            "GA", "HI", "IA", "ID", "IL", "IN", "KS", "KY", "LA", "MA",
            "MD", "ME", "MI", "MN", "MO", "MS", "MT", "NC", "ND", "NE",
            "NH", "NJ", "NM", "NV", "NY", "OH", "OK", "OR", "PA", "RI",
            "SC", "SD", "TN", "TX", "UT", "VA", "VT", "WA", "WI", "WV",
            "WY"
        };
        var regions = new ChartRegionMapItem[codes.Length];
        for (var i = 0; i < codes.Length; i++) regions[i] = new ChartRegionMapItem(codes[i], 10 + i);
        return regions;
    }
}
