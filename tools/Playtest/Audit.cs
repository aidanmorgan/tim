using CuriousContraptions;
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
        HashSet<(string From, string To, ConnectionDomain Type, string FromPort, string ToPort, float? RopeLength)>? Read(JsonNode state, string label)
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
            var edges = new HashSet<(string From, string To, ConnectionDomain Type, string FromPort, string ToPort, float? RopeLength)>();
            var identities = new HashSet<(string From, string To, ConnectionDomain Type, string FromPort, string ToPort)>();
            foreach (var node in connections)
            {
                string? Text(string name) => node is JsonObject obj &&
                    obj[name] is JsonValue value && value.TryGetValue<string>(out var text)
                        ? text : null;
                var from = Text("from");
                var to = Text("to");
                var typeText = Text("type");
                if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) ||
                    string.IsNullOrWhiteSpace(typeText))
                {
                    errors.Add(label + " contains a malformed connection.");
                    continue;
                }
                ConnectionDomain type;
                try
                {
                    type = JsonSerializer.Deserialize(node!["type"]!.ToJsonString(), MachineJson.Default.ConnectionDomain);
                    if (type == ConnectionDomain.Unknown) throw new JsonException("Unknown connection domain.");
                }
                catch (JsonException)
                {
                    errors.Add(label + " contains a malformed connection type.");
                    continue;
                }
                var fromPort = Text("fromPort");
                var toPort = Text("toPort");
                // Browser diagnostics use camelCase; every edge requires explicit socket identities.
                if (string.IsNullOrWhiteSpace(fromPort) || string.IsNullOrWhiteSpace(toPort))
                {
                    errors.Add(label + " contains a malformed connection socket.");
                    continue;
                }
                float? ropeLength = null;
                if (type == ConnectionDomain.Rope)
                {
                    if (node!["ropeLength"] is not JsonValue lengthValue ||
                        !lengthValue.TryGetValue<float>(out var length) || !float.IsFinite(length) ||
                        length < .05f || length > 200)
                    {
                        errors.Add(label + " contains a malformed rope length.");
                        continue;
                    }
                    ropeLength = length;
                    // Rope ends are undirected; canonicalize without changing the recorded evidence.
                    if (string.CompareOrdinal(from, to) > 0)
                    {
                        (from, to) = (to, from);
                        (fromPort, toPort) = (toPort, fromPort);
                    }
                }
                else if (node!["ropeLength"] != null)
                    errors.Add(label + " non-rope connection contains a rope length.");
                if (!identities.Add((from, to, type, fromPort, toPort)))
                    errors.Add(label + " contains a duplicate connection.");
                edges.Add((from, to, type, fromPort, toPort, ropeLength));
            }
            return edges;
        }
        var initial = Read(run, "Run");
        var restored = Read(reset, "Reset");
        if (initial != null && restored != null && !initial.SetEquals(restored))
            errors.Add("Reset did not restore typed connections and rope lengths.");
    }

    // Properties include wall dimensions and other authored physical parameters.
    // Missing fields cannot be interpreted as empty dictionaries in old captures.
    internal static void CheckProperties(JsonNode initial, JsonNode observed, string label,
        List<string> errors, List<string> gaps)
    {
        Dictionary<string, float>? Read(JsonNode node, string phase)
        {
            if (node["properties"] == null)
            {
                gaps.Add(phase + " property evidence missing.");
                return null;
            }
            if (node["properties"] is not JsonObject properties)
            {
                errors.Add(phase + " properties must be an object.");
                return null;
            }
            var result = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var (key, value) in properties)
            {
                if (value is not JsonValue scalar || !scalar.TryGetValue<float>(out var number) ||
                    !float.IsFinite(number))
                    errors.Add(phase + " contains a malformed property: " + key);
                else result.Add(key, number);
            }
            return result;
        }
        var before = Read(initial, "Run");
        var after = Read(observed, label);
        if (before != null && after != null &&
            (before.Count != after.Count || before.Any(p =>
                !after.TryGetValue(p.Key, out var value) || MathF.Abs(value - p.Value) > .0001f)))
            errors.Add(label + " did not preserve part properties.");
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
            if (record["failure"] != null)
            {
                reports.Add(new { file, status = "failed", errors = new[] {
                    "UI attempt failed: " + record["failure"]!.ToJsonString() } });
                failed = true;
                continue; // Construction/lifecycle failures need not contain a Run or outcome.
            }
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
                    CheckProperties(initial, sample.Part!, id + ": sample", errors, gaps);
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
                    {
                        CheckProperties(initial, part, initial["id"] + ": Reset", errors, gaps);
                        Require(JsonNode.DeepEquals(initial["kind"], part["kind"]) &&
                            JsonNode.DeepEquals(initial["locked"], part["locked"]) &&
                            JsonNode.DeepEquals(initial["dynamic"], part["dynamic"]),
                            initial["id"] + ": Reset changed part identity or flags.");
                        Require(Vector3.Distance(Position(initial), Position(part)) < .0001f &&
                            Angle(Rotation(initial), Rotation(part)) < .002f, initial["id"] + ": Reset did not restore transform.");
                    }
                }
                Require(record["resetUi"]?["running"]?.GetValue<bool>() == false, "Reset did not return to build UI.");
            }
            errors = errors.Distinct().ToList();
            gaps = gaps.Distinct().ToList();
            failed |= errors.Count > 0;
            incomplete |= gaps.Count > 0;
            reports.Add(new { file, status = errors.Count > 0 ? "failed" : gaps.Count > 0 ? "incomplete" : "passed", checkedParts, errors, gaps });
        }
        Console.WriteLine(JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
        return failed ? 1 : incomplete ? 2 : 0;
    }
}
