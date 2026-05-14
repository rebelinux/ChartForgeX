using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.SvgRaster;

internal static class SvgRasterNumbers {
    public static double GetDouble(this SvgRasterElement element, string name, double fallback = 0) {
        return TryParse(element.Get(name), out var value) ? value : fallback;
    }

    public static IReadOnlyList<double> ParseList(string? value) {
        var numbers = new List<double>();
        if (string.IsNullOrWhiteSpace(value)) return numbers;
        var index = 0;
        while (index < value!.Length) {
            Skip(value, ref index);
            if (index >= value.Length) break;
            numbers.Add(ReadNumber(value, ref index));
        }

        return numbers;
    }

    public static bool TryParse(string? value, out double parsed) {
        parsed = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value!.Trim();
        if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase)) trimmed = trimmed.Substring(0, trimmed.Length - 2);
        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
    }

    private static void Skip(string value, ref int index) {
        while (index < value.Length && (char.IsWhiteSpace(value[index]) || value[index] == ',')) index++;
    }

    private static double ReadNumber(string value, ref int index) {
        Skip(value, ref index);
        var start = index;
        if (index < value.Length && (value[index] == '-' || value[index] == '+')) index++;
        var hasDigit = false;
        while (index < value.Length && char.IsDigit(value[index])) { index++; hasDigit = true; }
        if (index < value.Length && value[index] == '.') {
            index++;
            while (index < value.Length && char.IsDigit(value[index])) { index++; hasDigit = true; }
        }

        if (!hasDigit) throw new FormatException("SVG number list contains an invalid number.");
        if (index < value.Length && (value[index] == 'e' || value[index] == 'E')) {
            var exponent = index;
            index++;
            if (index < value.Length && (value[index] == '-' || value[index] == '+')) index++;
            var exponentDigit = false;
            while (index < value.Length && char.IsDigit(value[index])) { index++; exponentDigit = true; }
            if (!exponentDigit) index = exponent;
        }

        return double.Parse(value.Substring(start, index - start), CultureInfo.InvariantCulture);
    }
}
