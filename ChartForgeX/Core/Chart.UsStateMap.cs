using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a United States state tile map with value-colored regions.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="regions">The state values to render. Duplicate state codes or names are summed.</param>
    /// <param name="color">An optional high-intensity region color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddUsStateTileMap(string name, IEnumerable<ChartRegionMapItem> regions, ChartColor? color = null) {
        return AddUsStateRegionMap(name, regions, ChartSeriesKind.UsStateTileMap, "US state tile maps", color);
    }

    /// <summary>
    /// Adds a United States geographic choropleth map with value-colored regions.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="regions">The state values to render. Duplicate state codes or names are summed.</param>
    /// <param name="color">An optional high-intensity region color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddUsStateGeoMap(string name, IEnumerable<ChartRegionMapItem> regions, ChartColor? color = null) {
        return AddUsStateRegionMap(name, regions, ChartSeriesKind.UsStateGeoMap, "US state geographic maps", color);
    }

    private Chart AddUsStateRegionMap(string name, IEnumerable<ChartRegionMapItem> regions, ChartSeriesKind kind, string chartName, ChartColor? color) {
        if (regions == null) throw new ArgumentNullException(nameof(regions));
        var byRegion = new SortedDictionary<string, RegionAggregate>(StringComparer.OrdinalIgnoreCase);
        foreach (var region in regions) {
            var code = NormalizeUsStateRegion(region.Region);
            if (code.Length == 0) throw new ArgumentException("Unknown US state or district code or name: " + region.Region + ".", nameof(regions));
            if (!byRegion.TryGetValue(code, out var aggregate)) aggregate = new RegionAggregate();
            aggregate.Value += region.Value;
            if (region.Color.HasValue) aggregate.Color = region.Color;
            byRegion[code] = aggregate;
        }

        if (byRegion.Count == 0) throw new ArgumentException(chartName + " must contain at least one region value.", nameof(regions));
        var points = new List<ChartPoint>(byRegion.Count);
        var labels = new List<ChartAxisLabel>(byRegion.Count);
        var colors = new List<ChartColor?>(byRegion.Count);
        var index = 1;
        foreach (var entry in byRegion) {
            points.Add(new ChartPoint(index, entry.Value.Value));
            labels.Add(new ChartAxisLabel(index, entry.Key));
            colors.Add(entry.Value.Color);
            index++;
        }

        Options.XAxisLabels.Clear();
        Options.XAxisLabels.AddRange(labels);
        Add(name, kind, points, color);
        Series[Series.Count - 1].PointColors.AddRange(colors);
        return this;
    }

    private struct RegionAggregate {
        public double Value;
        public ChartColor? Color;
    }

    private static string NormalizeUsStateRegion(string region) {
        return UsStateAliases.TryGetValue(region, out var code) ? code : string.Empty;
    }

    private static readonly Dictionary<string, string> UsStateAliases = new(StringComparer.OrdinalIgnoreCase) {
        ["AK"] = "AK", ["ALASKA"] = "AK",
        ["AL"] = "AL", ["ALABAMA"] = "AL",
        ["AR"] = "AR", ["ARKANSAS"] = "AR",
        ["AZ"] = "AZ", ["ARIZONA"] = "AZ",
        ["CA"] = "CA", ["CALIFORNIA"] = "CA",
        ["CO"] = "CO", ["COLORADO"] = "CO",
        ["CT"] = "CT", ["CONNECTICUT"] = "CT",
        ["DC"] = "DC", ["DISTRICT OF COLUMBIA"] = "DC", ["D.C."] = "DC", ["WASHINGTON DC"] = "DC", ["WASHINGTON D.C."] = "DC",
        ["DE"] = "DE", ["DELAWARE"] = "DE",
        ["FL"] = "FL", ["FLORIDA"] = "FL",
        ["GA"] = "GA", ["GEORGIA"] = "GA",
        ["HI"] = "HI", ["HAWAII"] = "HI",
        ["IA"] = "IA", ["IOWA"] = "IA",
        ["ID"] = "ID", ["IDAHO"] = "ID",
        ["IL"] = "IL", ["ILLINOIS"] = "IL",
        ["IN"] = "IN", ["INDIANA"] = "IN",
        ["KS"] = "KS", ["KANSAS"] = "KS",
        ["KY"] = "KY", ["KENTUCKY"] = "KY",
        ["LA"] = "LA", ["LOUISIANA"] = "LA",
        ["MA"] = "MA", ["MASSACHUSETTS"] = "MA",
        ["MD"] = "MD", ["MARYLAND"] = "MD",
        ["ME"] = "ME", ["MAINE"] = "ME",
        ["MI"] = "MI", ["MICHIGAN"] = "MI",
        ["MN"] = "MN", ["MINNESOTA"] = "MN",
        ["MO"] = "MO", ["MISSOURI"] = "MO",
        ["MS"] = "MS", ["MISSISSIPPI"] = "MS",
        ["MT"] = "MT", ["MONTANA"] = "MT",
        ["NC"] = "NC", ["NORTH CAROLINA"] = "NC",
        ["ND"] = "ND", ["NORTH DAKOTA"] = "ND",
        ["NE"] = "NE", ["NEBRASKA"] = "NE",
        ["NH"] = "NH", ["NEW HAMPSHIRE"] = "NH",
        ["NJ"] = "NJ", ["NEW JERSEY"] = "NJ",
        ["NM"] = "NM", ["NEW MEXICO"] = "NM",
        ["NV"] = "NV", ["NEVADA"] = "NV",
        ["NY"] = "NY", ["NEW YORK"] = "NY",
        ["OH"] = "OH", ["OHIO"] = "OH",
        ["OK"] = "OK", ["OKLAHOMA"] = "OK",
        ["OR"] = "OR", ["OREGON"] = "OR",
        ["PA"] = "PA", ["PENNSYLVANIA"] = "PA",
        ["RI"] = "RI", ["RHODE ISLAND"] = "RI",
        ["SC"] = "SC", ["SOUTH CAROLINA"] = "SC",
        ["SD"] = "SD", ["SOUTH DAKOTA"] = "SD",
        ["TN"] = "TN", ["TENNESSEE"] = "TN",
        ["TX"] = "TX", ["TEXAS"] = "TX",
        ["UT"] = "UT", ["UTAH"] = "UT",
        ["VA"] = "VA", ["VIRGINIA"] = "VA",
        ["VT"] = "VT", ["VERMONT"] = "VT",
        ["WA"] = "WA", ["WASHINGTON"] = "WA",
        ["WI"] = "WI", ["WISCONSIN"] = "WI",
        ["WV"] = "WV", ["WEST VIRGINIA"] = "WV",
        ["WY"] = "WY", ["WYOMING"] = "WY"
    };
}
