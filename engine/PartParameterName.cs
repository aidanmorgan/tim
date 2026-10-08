using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Explicit boundary between a part's closed parameter enum and the
/// extensible resource/serialization dictionary. Unknown enum values are rejected.</summary>
public static class PartParameterName
{
    public static string Of<TParameter>(TParameter parameter) where TParameter : struct, Enum
    {
        return Names<TParameter>.Values.TryGetValue(parameter, out var name)
            ? name : throw new ArgumentOutOfRangeException(nameof(parameter));
    }
    public static void RequireExact<TParameter>(IEnumerable<string> keys) where TParameter:struct,Enum
    {
        ArgumentNullException.ThrowIfNull(keys);
        var supplied=keys.ToArray();
        var expected=Names<TParameter>.Values.Values.ToHashSet(StringComparer.Ordinal);
        if(supplied.Length!=expected.Count||!expected.SetEquals(supplied))
            throw new ArgumentException("Parameter fields must exactly match the supported schema.",nameof(keys));
    }
    // Resource names are computed once, not allocated on every physics substep.
    private static class Names<TParameter> where TParameter : struct, Enum
    {
        internal static readonly IReadOnlyDictionary<TParameter, string> Values =
            Enum.GetValues<TParameter>().ToDictionary(value => value,
                value => JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString()));
    }
}
