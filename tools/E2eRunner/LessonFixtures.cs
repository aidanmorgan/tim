using System.Text.Json;

namespace CuriousContraptions.Content;

/// <summary>The admission states of content/puzzles.json (the engine's PuzzleAvailability). A Playable lesson carries typed
/// `placements`; a Planned lesson carries its planned `parts` and is indexed but never constructed.</summary>
public enum LessonAvailability { Planned, Playable }

/// <summary>One pre-placed fixture: its lesson, its identity within that lesson, its element id, its locked state and its exact
/// authored JSON (for change hashing).</summary>
public readonly record struct LessonFixture(string Lesson, string Fixture, string Part, bool Locked, string Authored);

/// <summary>Reads every lesson's pre-placed fixtures from the current content shape of its availability. Shared by the coverage
/// and e2e-selection tools; malformed content, an undefined availability or a shape that does not match it is refused.</summary>
public static class LessonFixtures
{
    public static IReadOnlyList<LessonFixture> Read(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Lesson content must be an array.");
        var fixtures = new List<LessonFixture>();
        foreach (var lesson in document.RootElement.EnumerateArray())
        {
            var id = Text(lesson, "id", "lesson");
            var availability = Text(lesson, "availability", id) switch
            {
                "Planned" => LessonAvailability.Planned,
                "Playable" => LessonAvailability.Playable,
                _ => throw new InvalidDataException($"Lesson {id} has an undefined availability.")
            };
            var placed = lesson.TryGetProperty("placements", out var placements);
            var planned = lesson.TryGetProperty("parts", out var parts);
            switch (availability)
            {
                case LessonAvailability.Playable when placed && !planned:
                    foreach (var placement in Array(placements, id, "placements"))
                    {
                        var instance = Object(placement, "instance", id);
                        fixtures.Add(new(id, Text(placement, "slot", id), Text(instance, "part", id), Boolean(instance, "locked", id),
                            placement.GetRawText()));
                    }
                    break;
                case LessonAvailability.Planned when planned && !placed:
                    foreach (var part in Array(parts, id, "parts"))
                        fixtures.Add(new(id, Text(part, "id", id), Text(part, "kind", id), Boolean(part, "locked", id), part.GetRawText()));
                    break;
                default:
                    throw new InvalidDataException($"Lesson {id} does not carry exactly the fixture shape of a {availability} lesson.");
            }
        }
        return fixtures;
    }

    private static JsonElement.ArrayEnumerator Array(JsonElement value, string lesson, string field) =>
        value.ValueKind == JsonValueKind.Array ? value.EnumerateArray() : throw new InvalidDataException($"Lesson {lesson} {field} must be an array.");

    private static JsonElement Object(JsonElement value, string field, string lesson) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(field, out var child) && child.ValueKind == JsonValueKind.Object
            ? child
            : throw new InvalidDataException($"Lesson {lesson} requires an object {field}.");

    private static string Text(JsonElement value, string field, string lesson) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(field, out var text) && text.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(text.GetString())
            ? text.GetString()!
            : throw new InvalidDataException($"Lesson {lesson} requires a nonempty string {field}.");

    private static bool Boolean(JsonElement value, string field, string lesson) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(field, out var flag) && flag.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? flag.GetBoolean()
            : throw new InvalidDataException($"Lesson {lesson} requires a boolean {field}.");
}
