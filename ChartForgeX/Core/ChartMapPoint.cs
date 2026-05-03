using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one labeled longitude/latitude point in a map chart.
/// </summary>
public readonly struct ChartMapPoint {
    /// <summary>
    /// Gets the point label.
    /// </summary>
    public readonly string Label;

    /// <summary>
    /// Gets the longitude in degrees.
    /// </summary>
    public readonly double Longitude;

    /// <summary>
    /// Gets the latitude in degrees.
    /// </summary>
    public readonly double Latitude;

    /// <summary>
    /// Gets an optional numeric value used by renderers that support weighted map points.
    /// </summary>
    public readonly double? Value;

    /// <summary>
    /// Gets the optional point color.
    /// </summary>
    public readonly ChartColor? Color;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartMapPoint"/> struct.
    /// </summary>
    /// <param name="label">The point label.</param>
    /// <param name="longitude">The longitude in degrees, from -180 to 180.</param>
    /// <param name="latitude">The latitude in degrees, from -90 to 90.</param>
    /// <param name="color">An optional point color.</param>
    public ChartMapPoint(string label, double longitude, double latitude, ChartColor? color = null) {
        Validate(label, longitude, latitude);
        Label = label.Trim();
        Longitude = longitude;
        Latitude = latitude;
        Value = null;
        Color = color;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartMapPoint"/> struct with a numeric value.
    /// </summary>
    /// <param name="label">The point label.</param>
    /// <param name="longitude">The longitude in degrees, from -180 to 180.</param>
    /// <param name="latitude">The latitude in degrees, from -90 to 90.</param>
    /// <param name="value">The non-negative value associated with the point.</param>
    /// <param name="color">An optional point color.</param>
    public ChartMapPoint(string label, double longitude, double latitude, double value, ChartColor? color = null) {
        Validate(label, longitude, latitude);
        ChartGuards.Finite(value, nameof(value));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Map point values must be zero or greater.");
        Label = label.Trim();
        Longitude = longitude;
        Latitude = latitude;
        Value = value;
        Color = color;
    }

    private static void Validate(string label, double longitude, double latitude) {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Map point labels must not be empty.", nameof(label));
        ChartGuards.Finite(longitude, nameof(longitude));
        ChartGuards.Finite(latitude, nameof(latitude));
        if (longitude < -180 || longitude > 180) throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180 degrees.");
        if (latitude < -90 || latitude > 90) throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90 degrees.");
    }
}
