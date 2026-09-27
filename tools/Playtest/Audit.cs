using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

// Independent, read-only checks over browser observations; no game assembly or simulation calls.
// This checks sampled motion, not continuous rendering between samples or target-assignment optimality.
internal static class Audit
{
    private static Vector3 Position(JsonNode p) => new(
        p["position"]![0]!.GetValue<float>(), p["position"]![1]!.GetValue<float>(), p["position"]![2]!.GetValue<float>());
    private static Quaternion Rotation(JsonNode p) => Quaternion.Normalize(new(
        p["rotation"]![0]!.GetValue<float>(), p["rotation"]![1]!.GetValue<float>(),
        p["rotation"]![2]!.GetValue<float>(), p["rotation"]![3]!.GetValue<float>()));
    private static float Angle(Quaternion a, Quaternion b)
    {
        // Relative quaternion avoids acos(dot) precision loss near zero.
        var q = Quaternion.Normalize(Quaternion.Conjugate(a) * b);
        return 2 * MathF.Atan2(new Vector3(q.X, q.Y, q.Z).Length(), MathF.Abs(q.W)) * 180 / MathF.PI;
    }
    // Directed, typed edges are compared independently of serialization order.
    // Missing instrumentation is incomplete evidence, never an empty graph.
    internal static void CheckResetConnections(JsonNode run, JsonNode reset,
        List<string> errors, List<string> gaps)
    {
        HashSet<(string From, string To, string Type)>? Read(JsonNode state, string label)
        {
            if (state["connections"] == null)
            {
                gaps.Add(label + " connection evidence missing.");
                return null;
            }
            if (state["connections"] is not JsonArray connections)
            {
                errors.Add(label + " connections must be an array.");
                return null;
            }
            var edges = new HashSet<(string From, string To, string Type)>();
            foreach (var node in connections)
            {
                string? Text(string name) => node is JsonObject obj &&
                    obj[name] is JsonValue value && value.TryGetValue<string>(out var text)
                        ? text : null;
                var from = Text("from");
                var to = Text("to");
                var type = Text("type");
                if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) ||
                    string.IsNullOrWhiteSpace(type))
                {
                    errors.Add(label + " contains a malformed connection.");
                    continue;
                }
                if (!edges.Add((from, to, type)))
                    errors.Add(label + " contains a duplicate connection.");
            }
            return edges;
        }
        var initial = Read(run, "Run");
        var restored = Read(reset, "Reset");
        if (initial != null && restored != null && !initial.SetEquals(restored))
            errors.Add("Reset did not restore directed, typed connections.");
    }

    public static int Run(string root, string[] files)
    {
        var bytes = File.ReadAllBytes(Path.Combine(root, "content/puzzles.json"));
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var puzzles = JsonNode.Parse(bytes)!.AsArray();
        var reports = new List<object>();
        var failed = false;
        var incomplete = false;
        foreach (var file in files)
        {
            var record = JsonNode.Parse(file == "-" ? Console.In.ReadToEnd() : File.ReadAllText(file))!;
            var errors = new List<string>();
            var gaps = new List<string>();
            void Require(bool condition, string message) { if (!condition) errors.Add(message); }
            var recipe = record["recipe"];
            if (recipe?["puzzleDataSha256"]?.GetValue<string>() != hash)
            {
                reports.Add(new { file, status = "incomplete", gaps = new[] { "Missing recipe or historical campaign hash; not audited against changed author data." } });
                incomplete = true;
                continue;
            }
            var precision = record["precision"]!.GetValue<float>();
            var level = record["level"]!.GetValue<int>();
            var run = record["run"]!;
            var frames = record["frames"]!.AsArray();
            Require(record["errors"]!.AsArray().Count == 0, "Browser errors recorded.");
            Require(run["level"]!.GetValue<int>() == level, "Run level mismatch.");
            Require(MathF.Abs(run["precision"]!.GetValue<float>() - precision) < .0001f, "Run difficulty mismatch.");
            Require(frames.Count > 1 && frames[0]!["tick"]!.GetValue<int>() == 0, "Missing trajectory start.");
            var lastRequiredTick = Math.Min(120, record["outcome"]!["tick"]!.GetValue<int>());
            var expectedTicks = Enumerable.Range(0, lastRequiredTick / 4 + 1).Select(i => i * 4);
            Require(frames.Select(f => f!["tick"]!.GetValue<int>()).SequenceEqual(expectedTicks),
                "Missing or unexpected trajectory samples.");
            var targets = puzzles[level - 1]!["solution"]!.AsArray();
            var checkedParts = 0;
            var previousTick = -1;
            foreach (var frame in frames)
            {
                var tick = frame!["tick"]!.GetValue<int>();
                Require(tick > previousTick, "Unordered or duplicate sample ticks.");
                previousTick = tick;
            }
            foreach (var initial in run["parts"]!.AsArray().Select(p => p!))
            {
                if (initial["locked"]!.GetValue<bool>() || initial["dynamic"]!.GetValue<bool>()) continue;
                var id = initial["id"]!.GetValue<string>();
                var kind = initial["kind"]!.GetValue<string>();
                var settings = targets.Where(t => t!["kind"]!.GetValue<string>() == kind)
                    .Select(t => t!["difficulty"]!.AsArray().Single(k => MathF.Abs(k!["precision"]!.GetValue<float>() - precision) < .0001f)!).ToArray();
                Require(settings.Length > 0, id + ": missing authored kind.");
                if (settings.Length == 0) continue;
                var maxPosition = settings.Max(s => s["max_position_correction"]!.GetValue<float>());
                // Author caps use Euler-vector magnitude; sqrt(3) bounds its L1 path length.
                var maxAngle = MathF.Sqrt(3) * settings.Max(s => s["max_rotation_correction"]!.GetValue<float>());
                var durations = settings.Select(s => MathF.Max(.1f, s["blend_seconds"]!.GetValue<float>())).Distinct().ToArray();
                var samples = frames.Select(f => (Tick: f!["tick"]!.GetValue<int>(),
                    Part: f["parts"]!.AsArray().SingleOrDefault(p => p!["id"]!.GetValue<string>() == id))).ToArray();
                Require(samples.All(s => s.Part != null), id + ": missing part samples.");
                if (samples.Any(s => s.Part == null)) continue;
                var startPosition = Position(initial);
                var startRotation = Rotation(initial);
                var outsideEveryPositionWindow = targets.Where(t => t!["kind"]!.GetValue<string>() == kind).All(t =>
                {
                    var knot = t!["difficulty"]!.AsArray().Single(k => MathF.Abs(k!["precision"]!.GetValue<float>() - precision) < .0001f)!;
                    return Vector3.Distance(startPosition, Position(t!)) > knot["position_window"]!.GetValue<float>() + .0001f;
                });
                foreach (var sample in samples)
                {
                    var distance = Vector3.Distance(startPosition, Position(sample.Part!));
                    var angle = Angle(startRotation, Rotation(sample.Part!));
                    Require(distance <= maxPosition + .0001f, id + ": position cap exceeded.");
                    Require(angle <= maxAngle + .002f, id + ": conservative rotation cap exceeded.");
                    if (precision == 1 || outsideEveryPositionWindow)
                        Require(distance < .0001f && angle < .002f, id + ": ineligible/Precise part corrected.");
                }
                if (durations.Length != 1) gaps.Add(id + ": heterogeneous durations need assignment-aware audit.");
                else
                {
                    var duration = durations[0];
                    var settled = samples.FirstOrDefault(s => s.Tick / 120f >= duration + .0001f);
                    if (settled.Part == null) gaps.Add(id + ": run ended before a settled sample.");
                    else foreach (var sample in samples)
                    {
                        var t = Math.Clamp(sample.Tick / (120f * duration), 0, 1);
                        var weight = t * t * t * (10 + t * (-15 + 6 * t));
                        var expectedPosition = Vector3.Lerp(startPosition, Position(settled.Part), weight);
                        var expectedRotation = Quaternion.Slerp(startRotation, Rotation(settled.Part), weight);
                        Require(Vector3.Distance(expectedPosition, Position(sample.Part!)) < .0002f, id + ": sampled position easing mismatch.");
                        Require(Angle(expectedRotation, Rotation(sample.Part!)) < .01f, id + ": sampled rotation easing mismatch.");
                    }
                }
                checkedParts++;
            }
            var reset = record["reset"];
            if (reset == null) gaps.Add("Browser Reset evidence missing.");
            else
            {
                CheckResetConnections(run, reset, errors, gaps);
                Require(reset["level"]!.GetValue<int>() == level, "Reset level mismatch.");
                var restored = reset["parts"]!.AsArray();
                Require(restored.Count == run["parts"]!.AsArray().Count, "Reset changed part count.");
                foreach (var initial in run["parts"]!.AsArray().Select(p => p!))
                {
                    var part = restored.SingleOrDefault(p => p!["id"]!.GetValue<string>() == initial["id"]!.GetValue<string>());
                    Require(part != null, "Reset lost a part.");
                    if (part != null)
                        Require(Vector3.Distance(Position(initial), Position(part)) < .0001f &&
                            Angle(Rotation(initial), Rotation(part)) < .002f, initial["id"] + ": Reset did not restore transform.");
                }
                Require(record["resetUi"]?["running"]?.GetValue<bool>() == false, "Reset did not return to build UI.");
            }
            errors = errors.Distinct().ToList();
            failed |= errors.Count > 0;
            incomplete |= gaps.Count > 0;
            reports.Add(new { file, status = errors.Count > 0 ? "failed" : gaps.Count > 0 ? "incomplete" : "passed", checkedParts, errors, gaps });
        }
        Console.WriteLine(JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
        return failed ? 1 : incomplete ? 2 : 0;
    }
}
