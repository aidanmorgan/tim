using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CuriousContraptions.Gpu;

/// <summary>An extensible content identity. Free Workshop has no authored identity.</summary>
public readonly record struct PuzzleId
{
    public string Value { get; }
    public PuzzleId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A puzzle identity is required.", nameof(value));
        Value = value;
    }
    public bool IsAuthored => Value is not null;
}

public enum PuzzleAvailability { Planned, Playable }

/// <summary>Content indexing does not admit future authored parts into the running game.</summary>
internal sealed record PuzzleContentEntry(PuzzleId Id, PuzzleAvailability Availability,
    string Title, string Subtitle, string Description, string Hint, JsonElement Declaration);

internal static class PuzzleContentReader
{
    internal static IReadOnlyList<PuzzleContentEntry> ReadIndex(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Puzzle content must be an array.");
        var entries = new List<PuzzleContentEntry>(16);
        var identities = new HashSet<PuzzleId>(16);
        foreach (var row in document.RootElement.EnumerateArray())
        {
            var fields = Fields(row, "puzzle");
            var id = new PuzzleId(Text(fields, "id", "puzzle"));
            if (!identities.Add(id)) throw Invalid(id.Value, "id", "duplicate identity");
            var availability = Text(fields, "availability", id.Value) switch
            {
                "Planned" => PuzzleAvailability.Planned,
                "Playable" => PuzzleAvailability.Playable,
                _ => throw Invalid(id.Value, "availability", "undefined admission state")
            };
            entries.Add(new(id, availability, Text(fields, "title", id.Value),
                Text(fields, "subtitle", id.Value), Text(fields, "description", id.Value),
                Text(fields, "hint", id.Value), row.Clone()));
        }
        if (entries.Count == 0) throw new InvalidDataException("Puzzle catalogue is empty.");
        return entries.AsReadOnly();
    }

    internal static Dictionary<string, JsonElement> Fields(JsonElement value, string where)
    {
        if (value.ValueKind != JsonValueKind.Object) throw Invalid(where, "", "expected object");
        var fields = new Dictionary<string, JsonElement>(4, StringComparer.Ordinal);
        foreach (var field in value.EnumerateObject())
            if (!fields.TryAdd(field.Name, field.Value)) throw Invalid(where, field.Name, "duplicate field");
        return fields;
    }

    internal static string Text(IReadOnlyDictionary<string, JsonElement> fields, string key, string where)
    {
        if (!fields.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
            throw Invalid(where, key, "required nonempty string");
        return value.GetString()!;
    }

    internal static InvalidDataException Invalid(string puzzle, string field, string reason) =>
        new($"Puzzle '{puzzle}', field '{field}': {reason}.");
}
