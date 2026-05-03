using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents a connector line between two longitude/latitude positions on a map chart.
/// </summary>
public readonly struct ChartMapConnector {
    /// <summary>
    /// Gets the connector label.
    /// </summary>
    public readonly string Label;

    /// <summary>
    /// Gets the source longitude in degrees.
    /// </summary>
    public readonly double FromLongitude;

    /// <summary>
    /// Gets the source latitude in degrees.
    /// </summary>
    public readonly double FromLatitude;

    /// <summary>
    /// Gets the target longitude in degrees.
    /// </summary>
    public readonly double ToLongitude;

    /// <summary>
    /// Gets the target latitude in degrees.
    /// </summary>
    public readonly double ToLatitude;

    /// <summary>
    /// Gets the optional connector color.
    /// </summary>
    public readonly ChartColor? Color;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartMapConnector"/> struct.
    /// </summary>
    public ChartMapConnector(string label, double fromLongitude, double fromLatitude, double toLongitude, double toLatitude, ChartColor? color = null) {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Map connector labels must not be empty.", nameof(label));
        ValidateCoordinate(fromLongitude, fromLatitude, nameof(fromLongitude), nameof(fromLatitude));
        ValidateCoordinate(toLongitude, toLatitude, nameof(toLongitude), nameof(toLatitude));
        Label = label.Trim();
        FromLongitude = fromLongitude;
        FromLatitude = fromLatitude;
        ToLongitude = toLongitude;
        ToLatitude = toLatitude;
        Color = color;
    }

    private static void ValidateCoordinate(double longitude, double latitude, string longitudeName, string latitudeName) {
        ChartGuards.Finite(longitude, longitudeName);
        ChartGuards.Finite(latitude, latitudeName);
        if (longitude < -180 || longitude > 180) throw new ArgumentOutOfRangeException(longitudeName, longitude, "Longitude must be between -180 and 180 degrees.");
        if (latitude < -90 || latitude > 90) throw new ArgumentOutOfRangeException(latitudeName, latitude, "Latitude must be between -90 and 90 degrees.");
    }
}
