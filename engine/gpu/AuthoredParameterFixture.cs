using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CuriousContraptions.Gpu;

/// <summary>A fixed content identity and its declared parameter; placement and assistance remain owned by the caller.</summary>
internal readonly record struct AuthoredParameterFixture(PartId Part, ParameterId Parameter)
{
    public static AuthoredParameterFixture Load(string resource)
    {
        using var stream = typeof(AuthoredParameterFixture).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidDataException($"Missing authored fixture resource '{resource}'.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    internal static AuthoredParameterFixture Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("An authored parameter fixture must be an object.");
        string? part = null, parameter = null;
        foreach (var field in root.EnumerateObject())
        {
            if (field.Value.ValueKind != JsonValueKind.String)
                throw new ArgumentException("Authored fixture identities must be strings.");
            if (field.NameEquals("part") && part is null) part = field.Value.GetString();
            else if (field.NameEquals("parameter") && parameter is null) parameter = field.Value.GetString();
            else throw new ArgumentException("Unknown or duplicate authored fixture field.");
        }
        if (string.IsNullOrWhiteSpace(part) || string.IsNullOrWhiteSpace(parameter))
            throw new ArgumentException("Authored fixture part and parameter are required.");
        var result = new AuthoredParameterFixture(new(part), new(parameter));
        if (!DeclaredPhysics.Contains(result.Part) ||
            !DeclaredPhysics.Of(result.Part).Metadata.Parameters.Any(value => value.Key == result.Parameter.Value &&
                value.Kind == Abstractions.ParameterKind.Real))
            throw new ArgumentException("Authored fixture names an unknown part or declared real parameter.");
        return result;
    }
}
