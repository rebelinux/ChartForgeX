using System;
using System.Globalization;

namespace ChartForgeX.Rendering;

internal static class ChartNumericFormatter {
    public static string FormatCompact(double value) {
        var abs = Math.Abs(value);
        if (abs >= 1000000000) return (value / 1000000000).ToString("0.#", CultureInfo.InvariantCulture) + "B";
        if (abs >= 1000000) return (value / 1000000).ToString("0.#", CultureInfo.InvariantCulture) + "M";
        if (abs >= 1000) return (value / 1000).ToString("0.#", CultureInfo.InvariantCulture) + "k";
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
