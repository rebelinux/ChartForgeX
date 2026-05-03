using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>
    /// Gets or sets the longitude/latitude window rendered by dotted map charts.
    /// </summary>
    public ChartMapViewport MapViewport { get; set; } = ChartMapViewport.World();

    /// <summary>
    /// Gets the connector lines rendered on capable map charts.
    /// </summary>
    public List<ChartMapConnector> MapConnectors { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether region labels are rendered on map charts.
    /// </summary>
    public bool ShowMapLabels { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether map scale legends are rendered.
    /// </summary>
    public bool ShowMapScaleLegend { get; set; } = true;
}
