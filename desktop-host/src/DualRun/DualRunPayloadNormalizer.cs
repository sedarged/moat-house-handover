using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace MoatHouseHandover.Host.DualRun;

public sealed class DualRunPayloadNormalizer
{
    public object? Normalize(object? value, bool normalizePathSeparators = true)
    {
        if (value is null) return null;
        if (value is JsonElement element) return NormalizeJsonElement(element, normalizePathSeparators);
        if (value is string s) return NormalizeString(s, normalizePathSeparators);
        if (value is bool b) return b;
        if (IsNumeric(value)) return NormalizeNumber(value);
        if (value is IDictionary dict) return NormalizeDictionary(dict, normalizePathSeparators);
        if (value is IEnumerable enumerable && value is not string)
        {
            return enumerable.Cast<object?>().Select(v => Normalize(v, normalizePathSeparators)).ToList();
        }

        return NormalizeJsonElement(JsonSerializer.SerializeToElement(value), normalizePathSeparators);
    }

    private static object? NormalizeJsonElement(JsonElement element, bool normalizePathSeparators)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToDictionary(p => p.Name, p => NormalizeJsonElement(p.Value, normalizePathSeparators)),
            JsonValueKind.Array => element.EnumerateArray().Select(i => NormalizeJsonElement(i, normalizePathSeparators)).ToList(),
            JsonValueKind.String => NormalizeString(element.GetString() ?? string.Empty, normalizePathSeparators),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private static object NormalizeString(string value, bool normalizePathSeparators)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
        {
            if (value.Length <= 10) return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        }

        if ((value == "1" || value == "0")) return value == "1";
        return normalizePathSeparators ? value.Replace('\\', '/').Trim() : value.Trim();
    }

    private static bool IsNumeric(object value)
        => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static double NormalizeNumber(object value)
    {
        var d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        return Math.Round(d, 6);
    }

    private static Dictionary<string, object?> NormalizeDictionary(IDictionary dict, bool normalizePathSeparators)
    {
        var output = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in dict)
        {
            output[Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty] = new DualRunPayloadNormalizer().Normalize(entry.Value, normalizePathSeparators);
        }

        return output.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
