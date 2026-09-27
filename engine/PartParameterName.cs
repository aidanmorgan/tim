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
    // Resource names are computed once, not allocated on every physics substep.
    private static class Names<TParameter> where TParameter : struct, Enum
    {
        internal static readonly IReadOnlyDictionary<TParameter, string> Values =
            Enum.GetValues<TParameter>().ToDictionary(value => value,
                value => JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString()));
    }
}
