using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

// Produce input recipes only. This process cannot communicate with the game or set game state.
// The separate MCP adapter realizes each recipe through real mouse gestures and button clicks.
var root = new DirectoryInfo(Directory.GetCurrentDirectory());
while (root != null && !File.Exists(Path.Combine(root.FullName, "project.godot"))) root = root.Parent;
if (root == null) throw new InvalidOperationException("Run inside the game repository.");
if (args.Length > 0 && args[0] == "--audit")
{
    if (args.Length < 2) throw new ArgumentException("Supply one or more evidence JSON files.");
    Environment.ExitCode = Audit.Run(root.FullName, args[1..]);
    return;
}
var puzzleBytes = File.ReadAllBytes(Path.Combine(root.FullName, "content/puzzles.json"));
var puzzleHash = Convert.ToHexStringLower(SHA256.HashData(puzzleBytes));
var puzzles = JsonNode.Parse(puzzleBytes)!.AsArray();
var cases = new JsonArray();
foreach (var (puzzle, index) in puzzles.Select((p, i) => (p!, i)))
foreach (var (name, precision) in new[] { ("forgiving", 0f), ("balanced", .45f), ("precise", 1f) })
foreach (var variant in new[] { "reference", "near-positive", "near-negative", "outside-window" })
{
    var parts = new JsonArray();
    foreach (var solution in puzzle["solution"]!.AsArray())
        parts.Add(new JsonObject
        {
            ["slot"] = solution!["id"]!.GetValue<string>(),
            ["kind"] = solution["kind"]!.GetValue<string>(),
            ["position"] = solution["position"]!.DeepClone(),
            ["rotation"] = solution["rotation"]!.DeepClone(),
            ["dimensions"] = solution["kind"]!.GetValue<string>() == "wall"
                ? new JsonArray(JsonValue.Create(solution["properties"]?["width"]?.GetValue<float>() ?? 3),
                    JsonValue.Create(solution["properties"]?["height"]?.GetValue<float>() ?? 2),
                    JsonValue.Create(solution["properties"]?["thickness"]?.GetValue<float>() ?? .25f))
                : null
        });
    var chosen = parts[0]!.AsObject();
    if (variant is "near-positive" or "near-negative")
    {
        var sign = variant == "near-positive" ? 1f : -1f;
        chosen["offset"] = new JsonArray(JsonValue.Create(.08f * sign), JsonValue.Create(.005f * sign), JsonValue.Create(.04f * sign));
        chosen["rotationOffset"] = new JsonArray(JsonValue.Create(0), JsonValue.Create(0), JsonValue.Create(sign));
    }
    else if (variant == "outside-window")
    {
        var x = chosen["position"]![0]!.GetValue<float>();
        // Four units higher is outside all currently authored same-kind placement windows.
        // The sideways displacement keeps the error obvious while staying inside the workbench.
        chosen["offset"] = new JsonArray(JsonValue.Create(x > 0 ? -4 : 4), JsonValue.Create(4), JsonValue.Create(0));
    }
    cases.Add(new JsonObject
    {
        ["caseId"] = $"L{index + 1:00}-{name}-{variant}-direct",
        ["level"] = index + 1, ["precision"] = precision, ["variant"] = variant,
        ["puzzleDataSha256"] = puzzleHash,
        ["parts"] = parts, ["connections"] = puzzle["solution_connections"]!.DeepClone()
    });
}
var selected = cases.Where(node =>
    (args.Length < 1 || node!["level"]!.GetValue<int>() == int.Parse(args[0])) &&
    (args.Length < 2 || node!["caseId"]!.GetValue<string>().Contains("-" + args[1] + "-")) &&
    (args.Length < 3 || node!["variant"]!.GetValue<string>() == args[2]))
    .Select(node => node!.DeepClone()).ToArray();
Console.WriteLine(new JsonArray(selected).ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
