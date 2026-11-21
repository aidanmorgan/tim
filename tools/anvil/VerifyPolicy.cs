#:property TargetFramework=net10.0
#:property Nullable=enable
#:property TreatWarningsAsErrors=true
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Nodes;

if (args.Length > 0)
{
    if (args[0] == "--bmad-check") return CustomizationCheck.Run(args[1..]);
    if (args[0] == "--maintainability") return MaintainabilityGate.Run(args[1..]);
    if (args[0] == "--coupling") return CouplingGate.Run(args[1..]);
    Console.Error.WriteLine("Unknown operation; use no arguments, --bmad-check, --maintainability or --coupling.");
    return 1;
}

var sourceRoot = Directory.GetCurrentDirectory();
var temporary = Directory.CreateTempSubdirectory("anvil-policy-verification-");
try
{
    var root = temporary.FullName;
    Directory.CreateDirectory(Path.Combine(root, "tools/anvil"));
    foreach (var name in new[] { "CheckStagedTests.cs", "no-skipped-tests.json" })
        File.Copy(Path.Combine(sourceRoot, "tools/anvil", name), Path.Combine(root, "tools/anvil", name));
    Directory.CreateDirectory(Path.Combine(root, ".git-hooks"));
    // This isolated hook proves staged-test policy and Git blocking, not the full production build hook.
    File.WriteAllText(Path.Combine(root, ".git-hooks/pre-commit"),
        "#!/bin/sh\nset -eu\nexec dotnet run --file tools/anvil/CheckStagedTests.cs\n");
    Run(root, "chmod", ["+x", ".git-hooks/pre-commit"]);
    Run(root, "git", ["init", "-q"]);
    Run(root, "git", ["config", "user.name", "Policy verification"]);
    Run(root, "git", ["config", "user.email", "policy@example.invalid"]);
    Run(root, "git", ["config", "core.hooksPath", ".git-hooks"]);
    var options = new System.Text.Json.JsonSerializerOptions
    {
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };
    options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter<Source>(allowIntegerValues: false));
    options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter<Outcome>(allowIntegerValues: false));
    var fixtureContext = new FixtureContext(options);
    var cases = System.Text.Json.JsonSerializer.Deserialize<Case[]>(
        File.ReadAllText(Path.Combine(sourceRoot, "tools/anvil/fixtures.json")), fixtureContext.CaseArray)
        ?? throw new InvalidOperationException("Missing policy fixtures.");
    foreach (var invalid in new[]
    {
        """[{"Label":"invalid","Source":"Unsupported","Content":"","Expected":"Allow"}]""",
        """[{"Label":"invalid","Source":0,"Content":"","Expected":"Allow"}]""",
        """[{"Label":"invalid","Source":"CSharp","Content":"","Expected":"Unsupported"}]""",
        """[{"Label":"invalid","Source":"CSharp","Content":"","Expected":0}]"""
    })
    {
        try
        {
            System.Text.Json.JsonSerializer.Deserialize(invalid, fixtureContext.CaseArray);
            throw new InvalidOperationException("Unsupported fixture enum was accepted.");
        }
        catch (System.Text.Json.JsonException) { Console.WriteLine("PASS unsupported fixture enum rejected"); }
    }
    foreach (var test in cases)
    {
        Run(root, "git", ["read-tree", "--empty"]);
        var path = test.Source switch { Source.CSharp => "SampleTests.cs", Source.TypeScript => "sample.spec.ts", _ => throw new InvalidOperationException("Unsupported fixture source.") };
        File.WriteAllText(Path.Combine(root, path), test.Content);
        Run(root, "git", ["add", "--", path]);
        // Check staged bytes, even when the worktree looks clean.
        File.WriteAllText(Path.Combine(root, path), "// unstaged clean control");
        var result = Run(root, "sh", [".git-hooks/pre-commit"], requireSuccess: false);
        var actual = result.ExitCode == 0 ? Outcome.Allow : Outcome.Block;
        if (actual != test.Expected || actual == Outcome.Block && !result.Output.Contains("TEST-001", StringComparison.Ordinal))
            throw new InvalidOperationException($"FAIL {test.Label}: {result.ExitCode}\n{result.Output}");
        Console.WriteLine($"PASS {test.Label}");
    }
    Run(root, "git", ["read-tree", "--empty"]);
    var empty = Run(root, "sh", [".git-hooks/pre-commit"]);
    Console.WriteLine("PASS empty index");
    File.WriteAllText(Path.Combine(root, "SampleTests.cs"), cases.First(test => test.Expected == Outcome.Block).Content);
    Run(root, "git", ["add", "SampleTests.cs"]);
    var commit = Run(root, "git", ["commit", "-m", "Must be blocked"], requireSuccess: false);
    if (commit.ExitCode == 0 || !commit.Output.Contains("TEST-001", StringComparison.Ordinal))
        throw new InvalidOperationException("Git commit was not rejected by TEST-001.");
    Console.WriteLine("PASS real git commit rejected");
    Console.WriteLine($"Policy fixtures: Passed: {cases.Length + 6}, Failed: 0, Skipped: 0. Isolated staged-test hook only; full production hook unqualified.");
}
finally { temporary.Delete(true); }
return 0;

static Result Run(string root, string executable, string[] arguments, bool requireSuccess = true)
{
    var start = new ProcessStartInfo(executable)
    {
        WorkingDirectory = root, RedirectStandardOutput = true,
        RedirectStandardError = true, UseShellExecute = false
    };
    foreach (var argument in arguments) start.ArgumentList.Add(argument);
    using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start verification command.");
    var output = process.StandardOutput.ReadToEndAsync();
    var error = process.StandardError.ReadToEndAsync();
    process.WaitForExit();
    var result = new Result(process.ExitCode, output.Result + error.Result);
    if (requireSuccess && result.ExitCode != 0) throw new InvalidOperationException(result.Output);
    return result;
}

enum Source { CSharp, TypeScript }
enum Outcome { Allow, Block }
sealed record Case(string Label, Source Source, string Content, Outcome Expected);
sealed record Result(int ExitCode, string Output);

[System.Text.Json.Serialization.JsonSerializable(typeof(Case[]))]
sealed partial class FixtureContext : System.Text.Json.Serialization.JsonSerializerContext;

enum Route { Auto, BuildReview, BuildOneshot, CodeReviewNoSpec, CodeReviewFull, ReviewEngineering, ReviewPartial }
enum Problem { UnsupportedField, InvalidShape, MissingLayer, EmptyInstruction, DuplicateId, MissingPolicy, MissingHook, PolicyNeedsManualCheck, UntrackedPolicy }
sealed record Finding(Problem Kind, string Detail);
sealed record BmadResult(List<Finding> Findings, List<string> Conditions);

static class CustomizationCheck
{
    static readonly string[] Policies = ["bmad-loop-protocol.md", "project-engineering-contract.md",
        "element-free-engine-design.md", "element-free-engine-review.md"];
    // The role cards the prompts cite and the spec template the spec customization names: a worktree without them cannot run the loop.
    static readonly string[] Companions = ["role-cards.md", "bmad-spec-template.md"];
    static readonly string[] CommonLayers = ["blind-hunter", "edge-case-hunter", "verification-gap", "element-free-engine"];
    const string MaintainabilityHook = "--maintainability --project-root";
    // The same run's coupling check is mandatory on every approving route and must be stated as such.
    const string CouplingHook = "Balanced coupling is mandatory";

    public static int Run(string[] args)
    {
        try
        {
            var root = Directory.GetCurrentDirectory();
            Route? route = null;
            string[] selected = [];
            var selfTest = false;
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--project-root": root = Path.GetFullPath(args[++i]); break;
                    case "--route":
                        if (!Enum.TryParse<Route>(args[++i], out var parsed) || !Enum.IsDefined(parsed))
                            throw new ArgumentException("Unknown route.");
                        route = parsed; break;
                    case "--lenses": selected = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries); break;
                    case "--self-test": selfTest = true; break;
                    default: throw new ArgumentException("Unknown argument: " + args[i]);
                }
            }
            if (route == Route.ReviewPartial && selected.Length == 0)
                throw new ArgumentException("ReviewPartial requires --lenses with the exact selected IDs.");
            if (selected.Length != 0 && route != Route.ReviewPartial)
                throw new ArgumentException("--lenses is only valid for ReviewPartial.");
            if (selfTest) return SelfTest(root);
            var routes = route is { } one ? new[] { one } : Enum.GetValues<Route>().Where(r => r != Route.ReviewPartial);
            var untracked = UntrackedPolicies(path => IndexStatus(root, path)).ToArray();
            foreach (var finding in untracked) Console.Error.WriteLine($"Policy: {finding.Kind}: {finding.Detail}");
            var failures = untracked.Length;
            foreach (var current in routes)
            {
                var skill = Skill(current);
                var (schema, effective) = Load(root, skill);
                var result = Validate(schema, effective, current, selected, PolicyResolver(root, skill));
                foreach (var finding in result.Findings) Console.Error.WriteLine($"{current}: {finding.Kind}: {finding.Detail}");
                foreach (var condition in result.Conditions) Console.WriteLine($"{current}: semantic condition review required: {condition}");
                Console.WriteLine($"{current}: structural findings={result.Findings.Count}");
                failures += result.Findings.Count;
            }
            Console.WriteLine("File globs: literal, *, ?, bracket classes/ranges/!negation per segment, whole-segment **; at least one file and all matches readable, no recursive symlinks. Unresolved pattern syntax requires manual preflight verification. Structural checks only: fields, required IDs/instructions, policy references and hook presence. Prose conditions, routing, repair/completion order and agent adherence require independent semantic review; partial review grants no engineering approval.");
            return failures == 0 ? 0 : 1;
        }
        catch (Exception error) when (error is ArgumentException or IndexOutOfRangeException or InvalidOperationException
            or JsonException or IOException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("Customization check refused: " + error.Message);
            return 1;
        }
    }

    static string Skill(Route route) => route switch
    {
        Route.Auto => "bmad-build-auto",
        Route.BuildReview or Route.BuildOneshot => "bmad-build",
        Route.CodeReviewNoSpec or Route.CodeReviewFull => "bmad-code-review",
        Route.ReviewEngineering or Route.ReviewPartial => "bmad-review",
        _ => throw new ArgumentOutOfRangeException(nameof(route))
    };

    static (JsonObject Schema, JsonObject Effective) Load(string root, string skill)
    {
        var skillRoot = Path.Combine(root, ".agents", "skills", skill);
        foreach (var suffix in new[] { ".toml", ".user.toml" })
            if (File.Exists(Path.Combine(skillRoot, "_bmad", "custom", skill + suffix)))
                throw new InvalidOperationException("Base-schema root contains an override: " + skillRoot);
        // Explicit project root prevents the resolver's cwd-based override discovery.
        return (Resolve(root, skillRoot, skillRoot), Resolve(root, skillRoot, root));
    }

    static JsonObject Resolve(string root, string skillRoot, string projectRoot) =>
        ParseResult(Command("uv", ["run", Path.Combine(root, "_bmad/scripts/resolve_customization.py"),
            "--skill", skillRoot, "--project-root", projectRoot]));

    static (int ExitCode, string Output, string Error) Command(string executable, string[] arguments)
    {
        var info = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Cannot start resolver.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
    }

    static JsonObject ParseResult((int ExitCode, string Output, string Error) process)
    {
        if (process.ExitCode != 0) throw new InvalidOperationException($"Resolver exit {process.ExitCode}: {process.Error}");
        return JsonNode.Parse(process.Output) as JsonObject ?? throw new JsonException("Resolver output must be an object.");
    }

    static bool Readable(string path)
    {
        try { using var input = File.OpenRead(path); return true; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
    }

    // A policy file read from another checkout is not this candidate's policy: worktrees once ran rules that existed only,
    // uncommitted, in the primary checkout. Every referenced policy path must resolve inside the checked project root.
    internal static bool InCheckout(string root, string path)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(path));
        return !Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    static bool PolicyReadable(string path, Func<string, bool> readable)
    {
        var absolute = Path.GetFullPath(path);
        var prefix = Path.GetPathRoot(absolute)!;
        var parts = absolute[prefix.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var matches = MatchFiles(prefix, parts, 0).Distinct(StringComparer.Ordinal).ToArray();
        return matches.Length > 0 && matches.All(readable);
    }

    static bool MatchSegment(string pattern, string name)
    {
        var expression = new System.Text.StringBuilder("\\A");
        for (var index = 0; index < pattern.Length; index++)
        {
            var character = pattern[index];
            if (character == '*') expression.Append(".*");
            else if (character == '?') expression.Append('.');
            else if (character == '[')
            {
                var start = index + 1;
                var negative = start < pattern.Length && pattern[start] == '!';
                if (negative) start++;
                var end = start;
                if (end < pattern.Length && pattern[end] == ']') end++;
                while (end < pattern.Length && pattern[end] != ']') end++;
                if (end == pattern.Length) expression.Append("\\[");
                else
                {
                    expression.Append(negative ? "[^" : "[");
                    foreach (var item in pattern[start..end])
                    {
                        if (item is '\\' or '^' or ']' or '[') expression.Append('\\');
                        expression.Append(item);
                    }
                    expression.Append(']');
                    index = end;
                }
            }
            else expression.Append(System.Text.RegularExpressions.Regex.Escape(character.ToString()));
        }
        expression.Append("\\z");
        var options = System.Text.RegularExpressions.RegexOptions.CultureInvariant |
            System.Text.RegularExpressions.RegexOptions.NonBacktracking;
        if (OperatingSystem.IsWindows()) options |= System.Text.RegularExpressions.RegexOptions.IgnoreCase;
        return System.Text.RegularExpressions.Regex.IsMatch(name, expression.ToString(), options);
    }

    static IEnumerable<string> MatchFiles(string directory, string[] parts, int index)
    {
        if (index == parts.Length)
        {
            if (File.Exists(directory)) yield return directory;
            yield break;
        }
        if (!Directory.Exists(directory)) yield break;
        var part = parts[index];
        if (part == "**")
        {
            if (index == parts.Length - 1)
                foreach (var file in Directory.EnumerateFiles(directory)) yield return file;
            foreach (var file in MatchFiles(directory, parts, index + 1)) yield return file;
            foreach (var child in Directory.EnumerateDirectories(directory))
                if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0)
                    foreach (var file in MatchFiles(child, parts, index)) yield return file;
        }
        else
        {
            var entries = part.IndexOfAny(['*', '?', '[']) < 0
                ? new[] { Path.Combine(directory, part) }
                : Directory.EnumerateFileSystemEntries(directory).Where(entry =>
                    MatchSegment(part, Path.GetFileName(entry)));
            foreach (var entry in entries)
                foreach (var file in MatchFiles(entry, parts, index + 1)) yield return file;
        }
    }

    static string Expand(string path, string root, string skill)
    {
        var expanded = path.Replace("{project-root}", root, StringComparison.Ordinal)
            .Replace("{skill-root}", Path.Combine(root, ".agents", "skills", skill), StringComparison.Ordinal);
        return Path.IsPathRooted(expanded) ? expanded : Path.Combine(root, expanded);
    }

    static string Text(JsonNode? value) => value is JsonValue scalar && scalar.TryGetValue<string>(out var text) ? text : "";

    static BmadResult Validate(JsonObject schema, JsonObject effective, Route route, string[] selected, Func<string, bool> readable)
    {
        var errors = new List<Finding>();
        var conditions = new List<string>();
        void Add(Problem kind, string detail) => errors.Add(new(kind, detail));
        CheckShape(schema, effective, "", errors);
        if (effective["workflow"] is not JsonObject workflow)
        {
            Add(Problem.InvalidShape, "workflow must be an object");
            return new(errors, conditions);
        }
        var isLens = route is Route.ReviewEngineering or Route.ReviewPartial;
        var field = isLens ? "lenses" : route == Route.BuildOneshot ? "oneshot_review_layers" : "review_layers";
        var idKey = isLens ? "code" : "id";
        var required = route switch
        {
            Route.Auto => CommonLayers.Concat(["intent-alignment", "forward-only"]).ToArray(),
            Route.BuildOneshot => ["blind-hunter", "element-free-engine"],
            Route.CodeReviewFull => CommonLayers.Concat(["acceptance-auditor"]).ToArray(),
            Route.ReviewEngineering => ["element-free-engine"],
            Route.ReviewPartial => selected,
            _ => CommonLayers
        };
        var entries = workflow[field] as JsonArray;
        var byId = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        foreach (var node in entries ?? [])
        {
            if (node is not JsonObject entry || string.IsNullOrWhiteSpace(Text(entry[idKey])))
            { Add(Problem.InvalidShape, field + " entry needs " + idKey); continue; }
            var id = Text(entry[idKey]);
            if (!byId.TryAdd(id, entry)) Add(Problem.DuplicateId, id);
        }
        foreach (var id in required)
        {
            if (!byId.TryGetValue(id, out var entry)) { Add(Problem.MissingLayer, id); continue; }
            if (string.IsNullOrWhiteSpace(Text(entry["instruction"]))) Add(Problem.EmptyInstruction, id);
            if (!string.IsNullOrWhiteSpace(Text(entry["when"]))) conditions.Add(id + ": " + Text(entry["when"]));
        }
        CheckPolicies(workflow, readable, errors);
        CheckHooks(workflow, route, errors);
        return new(errors, conditions);
    }

    static void CheckPolicies(JsonObject workflow, Func<string, bool> readable, List<Finding> errors)
    {
        void Add(Problem kind, string detail) => errors.Add(new(kind, detail));
        var facts = (workflow["persistent_facts"] as JsonArray ?? []).Select(Text).ToArray();
        foreach (var file in Policies)
        {
            var expected = "file:{project-root}/_bmad/custom/" + file;
            if (!facts.Contains(expected, StringComparer.Ordinal)) Add(Problem.MissingPolicy, expected);
        }
        foreach (var reference in facts.Where(f => f.StartsWith("file:", StringComparison.Ordinal)))
            try
            {
                if (!readable(reference[5..])) Add(Problem.MissingPolicy, reference);
            }
            catch (ArgumentException) { Add(Problem.PolicyNeedsManualCheck, reference); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { Add(Problem.MissingPolicy, reference); }
    }

    static void CheckHooks(JsonObject workflow, Route route, List<Finding> errors)
    {
        void Add(Problem kind, string detail) => errors.Add(new(kind, detail));
        var hooks = string.Join("\n", (workflow["activation_steps_prepend"] as JsonArray ?? []).Select(Text));
        foreach (var marker in RequiredMarkers(route))
            if (!hooks.Contains(marker, StringComparison.Ordinal)) Add(Problem.MissingHook, marker);
        if (route == Route.Auto && !hooks.Contains("Bounded story entry", StringComparison.Ordinal))
            Add(Problem.MissingHook, "Bounded story entry");
        if (route is Route.CodeReviewNoSpec or Route.CodeReviewFull && !hooks.Contains("Owned review staging", StringComparison.Ordinal))
            Add(Problem.MissingHook, "Owned review staging");
        if (route is Route.Auto or Route.BuildReview or Route.BuildOneshot &&
            !Text(workflow["implementation_handoff"]).Contains("verification ownership", StringComparison.Ordinal))
            Add(Problem.MissingHook, "implementation_handoff verification ownership");
    }

    // Every route loads the protocol and runs VerifyPolicy. Every approving route also runs the maintainability gate (E9);
    // an exact partial lens review grants no approval, so it needs no gate.
    static string[] RequiredMarkers(Route route) => route == Route.ReviewPartial
        ? ["bmad-loop-protocol.md", "VerifyPolicy.cs"]
        : ["bmad-loop-protocol.md", "VerifyPolicy.cs", MaintainabilityHook, CouplingHook];

    // The maintainability gate and its mandatory coupling check are required on approving routes, not on an exact partial
    // lens review.
    static int GateHookFixtures(string root, JsonObject schema, Func<JsonObject> fixture)
    {
        MissingGateRefused(schema, fixture());
        MissingCouplingRefused(schema, fixture());
        PartialNeedsNoGate(root);
        return 3;
    }

    static void MissingGateRefused(JsonObject schema, JsonObject input)
    {
        var prepend = (JsonArray)input["workflow"]!["activation_steps_prepend"]!;
        foreach (var step in prepend.Where(step => Text(step).Contains(MaintainabilityHook, StringComparison.Ordinal)).ToArray())
            prepend.Remove(step);
        if (Validate(schema, input, Route.Auto, [], _ => true).Findings is not
            [{ Kind: Problem.MissingHook, Detail: MaintainabilityHook }, { Kind: Problem.MissingHook, Detail: CouplingHook }])
            throw new InvalidOperationException("Fixture failed: missing maintainability gate hook refused");
        Console.WriteLine("PASS missing maintainability gate hook refused");
    }

    // The gate command alone is not enough: a hook that no longer states coupling is mandatory is refused.
    static void MissingCouplingRefused(JsonObject schema, JsonObject input)
    {
        var prepend = (JsonArray)input["workflow"]!["activation_steps_prepend"]!;
        for (var i = 0; i < prepend.Count; i++)
            prepend[i] = JsonValue.Create(Text(prepend[i]).Replace(CouplingHook, "Balanced coupling", StringComparison.Ordinal));
        if (Validate(schema, input, Route.Auto, [], _ => true).Findings is not [{ Kind: Problem.MissingHook, Detail: CouplingHook }])
            throw new InvalidOperationException("Fixture failed: missing mandatory coupling hook refused");
        Console.WriteLine("PASS missing mandatory coupling hook refused");
    }

    static void PartialNeedsNoGate(string root)
    {
        var (partialSchema, partial) = Load(root, Skill(Route.ReviewPartial));
        ((JsonObject)partial["workflow"]!)["activation_steps_prepend"] = new JsonArray(
            ((JsonArray)partial["workflow"]!["activation_steps_prepend"]!).Select(Text)
                .Where(step => !step.Contains(MaintainabilityHook, StringComparison.Ordinal))
                .Select(step => (JsonNode?)JsonValue.Create(step)).ToArray());
        if (Validate(partialSchema, partial, Route.ReviewPartial, ["prose"], _ => true).Findings.Count != 0)
            throw new InvalidOperationException("Exact partial review must not require the maintainability gate.");
        Console.WriteLine("PASS partial review control needs no maintainability gate");
    }

    static void CheckShape(JsonObject schema, JsonObject value, string prefix, List<Finding> errors)
    {
        foreach (var (key, node) in value)
        {
            var path = prefix + key;
            if (!schema.TryGetPropertyValue(key, out var expected))
            { errors.Add(new(Problem.UnsupportedField, path)); continue; }
            if (expected is JsonObject objectSchema && node is JsonObject child)
                CheckShape(objectSchema, child, path + ".", errors);
            else if (expected is JsonArray arraySchema && node is JsonArray array)
            {
                var records = arraySchema.OfType<JsonObject>().ToArray();
                if (records.Length == 0)
                {
                    if (array.Any(item => item is not JsonValue scalar || !scalar.TryGetValue<string>(out _)))
                        errors.Add(new(Problem.InvalidShape, path + " requires string entries"));
                    continue;
                }
                var allowed = records.SelectMany(record => record.Select(p => p.Key)).ToHashSet(StringComparer.Ordinal);
                // Documented optional layer/lens keys, not guaranteed to occur in base values.
                allowed.Add("when");
                if (key == "lenses") allowed.Add("after");
                foreach (var entry in array)
                {
                    if (entry is not JsonObject record) { errors.Add(new(Problem.InvalidShape, path)); continue; }
                    foreach (var property in record)
                        if (!allowed.Contains(property.Key)) errors.Add(new(Problem.UnsupportedField, path + "." + property.Key));
                        else if (property.Value is not JsonValue scalar || !scalar.TryGetValue<string>(out _))
                            errors.Add(new(Problem.InvalidShape, path + "." + property.Key));
                }
            }
            else if (expected is JsonValue scalar && scalar.TryGetValue<string>(out _) &&
                node is JsonValue actual && actual.TryGetValue<string>(out _)) { }
            else errors.Add(new(Problem.InvalidShape, path));
        }
    }

    // The resolver Run uses: a referenced policy must be inside this checkout and readable.
    static Func<string, bool> PolicyResolver(string root, string skill) =>
        path => InCheckout(root, Expand(path, root, skill)) && PolicyReadable(Expand(path, root, skill), Readable);

    static int IndexStatus(string root, string path) =>
        Command("git", ["-C", root, "ls-files", "--error-unmatch", "--", path]).ExitCode;

    // Every required policy file must be in git's index for this checkout: an untracked file is absent from every other
    // worktree. Exit 0 is tracked and exit 1 is not; any other answer cannot be judged and refuses the check.
    static IEnumerable<Finding> UntrackedPolicies(Func<string, int> status) => Policies.Concat(Companions)
        .Select(file => "_bmad/custom/" + file)
        .Where(path => status(path) switch
        {
            0 => false,
            1 => true,
            var code => throw new InvalidOperationException($"Cannot determine whether {path} is tracked (git exit {code}).")
        })
        .Select(path => new Finding(Problem.UntrackedPolicy, path));

    static int CheckoutFixtures(string root, JsonObject schema, Func<JsonObject> fixture)
    {
        var resolver = PolicyResolver(root, Skill(Route.Auto));
        // A readable file that exists outside the checkout, so only the checkout rule can refuse it.
        var elsewhere = Path.GetTempFileName();
        try
        {
            var outside = "file:" + elsewhere;
            var input = fixture();
            ((JsonArray)input["workflow"]!["persistent_facts"]!).Add((JsonNode?)JsonValue.Create(outside));
            if (InCheckout(root, elsewhere) || !PolicyReadable(elsewhere, Readable) ||
                Validate(schema, fixture(), Route.Auto, [], resolver).Findings.Count != 0 ||
                Validate(schema, input, Route.Auto, [], resolver).Findings is not [{ Kind: Problem.MissingPolicy } only] || only.Detail != outside)
                throw new InvalidOperationException("Fixture failed: policy reference outside the checkout refused");
        }
        finally { File.Delete(elsewhere); }
        Console.WriteLine("PASS policy reference outside the checkout refused; inside control accepted");
        if (UntrackedPolicies(path => path.EndsWith("/role-cards.md", StringComparison.Ordinal) ? 1 : 0).ToArray() is not
            [{ Kind: Problem.UntrackedPolicy, Detail: "_bmad/custom/role-cards.md" }])
            throw new InvalidOperationException("Fixture failed: untracked policy reported");
        Console.WriteLine("PASS untracked policy reported; tracked controls accepted");
        var refused = false;
        try { _ = UntrackedPolicies(_ => 128).ToArray(); }
        catch (InvalidOperationException) { refused = true; }
        if (!refused) throw new InvalidOperationException("Fixture failed: undetermined tracking state refused");
        Console.WriteLine("PASS undetermined tracking state refused");
        return OutsideCheckoutFixture(root) + 3;
    }

    static int OutsideCheckoutFixture(string root)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(root))!;
        var inside = InCheckout(root, Path.Combine(root, "_bmad/custom/bmad-loop-protocol.md"));
        var outside = new[] { Path.Combine(parent, "elsewhere/policy.md"), Path.Combine(root, "../elsewhere/policy.md"), parent };
        if (!inside || outside.Any(path => InCheckout(root, path)))
            throw new InvalidOperationException("Policy path outside the checkout accepted, or an inside path refused.");
        Console.WriteLine("PASS policy path outside the checkout refused; inside control accepted");
        return 1;
    }

    static int SelfTest(string root)
    {
        var (schema, effective) = Load(root, Skill(Route.Auto));
        var count = 0;
        void Expect(string name, JsonObject input, Problem? problem, Func<string, bool>? readable = null)
        {
            var findings = Validate(schema, input, Route.Auto, [], readable ?? (_ => true)).Findings;
            if (problem is null ? findings.Count != 0 : !findings.Any(f => f.Kind == problem))
                throw new InvalidOperationException("Fixture failed: " + name);
            Console.WriteLine("PASS " + name); count++;
        }
        JsonObject Fixture() => (JsonObject)effective.DeepClone();
        JsonObject First(JsonObject fixture) => (JsonObject)fixture["workflow"]!["review_layers"]![0]!;
        Expect("effective Auto accepted structurally", Fixture(), null);
        var input = Fixture(); First(input)["instruction"] = " "; Expect("empty instruction refused", input, Problem.EmptyInstruction);
        input = Fixture(); ((JsonArray)input["workflow"]!["review_layers"]!).RemoveAt(0); Expect("missing required layer refused", input, Problem.MissingLayer);
        input = Fixture(); ((JsonArray)input["workflow"]!["review_layers"]!).Add(First(input).DeepClone()); Expect("duplicate ID refused", input, Problem.DuplicateId);
        input = Fixture(); input["workflow"]!["invented_field"] = ""; Expect("unsupported field refused", input, Problem.UnsupportedField);
        input = Fixture(); First(input)["invented_field"] = ""; Expect("unsupported layer field refused", input, Problem.UnsupportedField);
        input = Fixture(); First(input)["instruction"] = 1; Expect("non-string instruction refused", input, Problem.InvalidShape);
        input = Fixture(); input["workflow"]!["activation_steps_prepend"] = new JsonArray(); Expect("missing hook refused", input, Problem.MissingHook);
        count += GateHookFixtures(root, schema, Fixture);
        Expect("unreadable policy refused", Fixture(), Problem.MissingPolicy, _ => false);
        input = Fixture(); input["workflow"]!["persistent_facts"] = new JsonArray(); Expect("missing policy reference refused", input, Problem.MissingPolicy);
        input = Fixture(); First(input)["when"] = "Unresolved external condition";
        var conditional = Validate(schema, input, Route.Auto, [], _ => true);
        if (conditional.Findings.Count != 0 || !conditional.Conditions.Any(c => c.Contains("Unresolved external condition", StringComparison.Ordinal)))
            throw new InvalidOperationException("Condition must be exposed for semantic review.");
        Console.WriteLine("PASS condition exposed, never evaluated"); count++;
        var (lensSchema, lenses) = Load(root, Skill(Route.ReviewPartial));
        foreach (var item in (JsonArray)lenses["workflow"]!["lenses"]!)
            if (Text(item!["code"]) == "element-free-engine") item!["instruction"] = "";
        if (Validate(lensSchema, lenses, Route.ReviewPartial, ["prose"], _ => true).Findings.Count != 0 ||
            !Validate(lensSchema, lenses, Route.ReviewEngineering, [], _ => true).Findings.Any(f => f.Kind == Problem.EmptyInstruction))
            throw new InvalidOperationException("Partial review incorrectly grants or requires engineering approval.");
        Console.WriteLine("PASS partial lens set preserved; engineering approval checks its auditor"); count++;
        try { ParseResult((0, "{invalid", "")); throw new InvalidOperationException("Malformed JSON accepted."); }
        catch (JsonException) { Console.WriteLine("PASS malformed JSON refused"); count++; }
        var failed = Command("uv", ["run", Path.Combine(root, "_bmad/scripts/resolve_customization.py"), "--invalid-fixture-option"]);
        if (failed.ExitCode == 0) throw new InvalidOperationException("Resolver failure fixture unexpectedly succeeded.");
        var refused = false;
        try { ParseResult(failed); }
        catch (InvalidOperationException) { refused = true; }
        if (!refused) throw new InvalidOperationException("Failed resolver output accepted.");
        Console.WriteLine("PASS actual resolver failure refused"); count++;
        foreach (var pattern in new[] { "_bmad/custom/bmad-loop-protocol.md", "_bmad/custom/*.md",
            "_bmad/custom/bmad-loop-protoco?.md", "_bmad/custom/[bp]*.md",
            "_bmad/custom/[a-z]*.md", "_bmad/custom/[!z]*.md", "_bmad/custom/**/bmad-loop-protocol.md", "_bmad/**/bmad-loop-protocol.md", "_bmad/custom/**" })
        {
            if (!PolicyReadable(Path.Combine(root, pattern), Readable))
                throw new InvalidOperationException("Readable glob refused: " + pattern);
            Console.WriteLine("PASS readable glob " + pattern); count++;
        }
        foreach (var (pattern, expected) in new[]
        {
            ("_bmad/custom/no-policy-matches-*.md", Problem.MissingPolicy),
            ("_bmad/custom/[xyz]*.md", Problem.MissingPolicy)
        })
        {
            input = Fixture();
            ((JsonArray)input["workflow"]!["persistent_facts"]!).Add((JsonNode?)JsonValue.Create("file:" + Path.Combine(root, pattern)));
            Expect("glob refusal " + pattern, input, expected, path => PolicyReadable(Expand(path, root, Skill(Route.Auto)), Readable));
        }
        var readableCalls = 0;
        if (PolicyReadable(Path.Combine(root, "_bmad/custom/*.md"), _ => ++readableCalls != 2) || readableCalls != 2)
            throw new InvalidOperationException("Glob with an unreadable matched file accepted.");
        Console.WriteLine("PASS unreadable match refuses whole glob"); count++;
        count += CheckoutFixtures(root, schema, Fixture);

        void RouteFixture(Route requiredRoute, Route controlRoute, string collection, string id)
        {
            var (routeSchema, original) = Load(root, Skill(requiredRoute));
            foreach (var empty in new[] { false, true })
            {
                var fixture = (JsonObject)original.DeepClone();
                var layers = (JsonArray)fixture["workflow"]![collection]!;
                var layer = layers.OfType<JsonObject>().Single(item => Text(item["id"]) == id);
                if (empty) layer["instruction"] = " "; else layers.Remove(layer);
                var expected = empty ? Problem.EmptyInstruction : Problem.MissingLayer;
                var rejected = Validate(routeSchema, fixture, requiredRoute, [], _ => true);
                var control = Validate(routeSchema, fixture, controlRoute, [], _ => true);
                if (rejected.Findings.Count != 1 || rejected.Findings[0] != new Finding(expected, id) || control.Findings.Count != 0)
                    throw new InvalidOperationException("Route-specific required ID was not discriminated: " + id);
                Console.WriteLine($"PASS {requiredRoute} {expected} {id}; {controlRoute} control"); count++;
            }
        }
        RouteFixture(Route.CodeReviewFull, Route.CodeReviewNoSpec, "review_layers", "acceptance-auditor");
        RouteFixture(Route.BuildOneshot, Route.BuildReview, "oneshot_review_layers", "element-free-engine");
        Console.WriteLine($"Fixtures: Passed: {count}, Failed: 0, Skipped: 0. Configuration inputs only; no agent execution.");
        return 0;
    }
}

/// <summary>
/// Maintainability gate (owner rule, 5 November 2026; rule family E9 of _bmad/custom/project-engineering-contract.md).
/// The .NET SDK's code-metrics analyzer scores every analysed type and member. Every one must reach a maintainability index
/// (CA1505, 0-100) of at least <see cref="MinimumIndex"/> and keep its cyclomatic complexity (CA1502) at most
/// <see cref="MaximumComplexity"/>, existing code included. Every production project must also pass
/// <see cref="CouplingGate"/>, read from the same builds. Scope is every C# compile the pre-commit static gate builds:
/// the solution, its PLAYTEST variant, the web host and every file-based app. The exclusions are all listed here: test
/// projects (IsTestProject=true; owner rule, 6 November 2026), the vendored FlatBuffers runtime (CSharpStaticGate=false
/// turns analyzers off) and FlatBuffers-compiler output marked &lt;auto-generated&gt;. The rules and thresholds are
/// injected for this run only, so normal builds keep the root .editorconfig as their only analyzer configuration. A
/// canary method that breaks both thresholds must be reported for both rules, and the same method in a test project for
/// neither, so a run whose injection or test exclusion silently failed cannot pass.
/// </summary>
static partial class MaintainabilityGate
{
    // Microsoft's code metrics rate an index of 20-100 as good maintainability, 10-19 moderate and 0-9 low.
    const int MinimumIndex = 20;
    // CA1502's default: more than 25 independent paths through one member is too many to test or change safely.
    const int MaximumComplexity = 25;

    enum Metric { MaintainabilityIndex, CyclomaticComplexity }
    sealed record Violation(Metric Metric, string File, int Line, string Symbol, int Score);
    internal sealed record Build(string Label, string[] Arguments, bool Coupling);

    public static int Run(string[] args) => Guard("Maintainability gate", "--maintainability", args, Gate);

    // Shared entry for both gates: resolve the root, give the gate a temporary folder and refuse on tool failure.
    internal static int Guard(string name, string operation, string[] args, Func<string, string, int> gate)
    {
        try
        {
            var root = args switch
            {
                [] => Directory.GetCurrentDirectory(),
                ["--project-root", var path] => Path.GetFullPath(path),
                _ => throw new ArgumentException($"Usage: {operation} [--project-root <path>]")
            };
            var work = Directory.CreateTempSubdirectory("maintainability-gate-");
            try { return gate(root, work.FullName); }
            finally { work.Delete(true); }
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or IOException
            or System.ComponentModel.Win32Exception or UnauthorizedAccessException or BadImageFormatException)
        {
            Console.Error.WriteLine(name + " refused: " + error.Message);
            return 1;
        }
    }

    static int Gate(string root, string work)
    {
        var inventory = Path.Combine(work, "inventory");
        var targets = WriteInjection(work, inventory);
        Canary(work, targets, inventory);
        CouplingGate.Canary(work);
        var metrics = new List<Violation>();
        var coupling = new List<CouplingGate.Report>();
        foreach (var build in Builds(root, work))
        {
            metrics.AddRange(Analyse(root, build, targets, inventory));
            // Builds share bin folders, so each build's IL is read before the next build replaces it.
            if (build.Coupling) coupling.Add(CouplingGate.Analyse(root, inventory, CouplingGate.Classify));
        }
        var violations = metrics.DistinctBy(violation => (violation.Metric, violation.File, violation.Line, violation.Symbol))
            .OrderBy(violation => violation.Metric)
            .ThenBy(violation => violation.Metric == Metric.MaintainabilityIndex ? violation.Score : -violation.Score)
            .ThenBy(violation => violation.File, StringComparer.Ordinal).ThenBy(violation => violation.Line).ToArray();
        foreach (var violation in violations)
            Console.WriteLine($"FAIL {Label(violation.Metric)} {violation.Score} {violation.File}:{violation.Line} {violation.Symbol}");
        var couplingViolations = CouplingGate.Print(coupling);
        var total = violations.Length + couplingViolations;
        var verdict = total == 0 ? "PASS" : "FAIL";
        Console.WriteLine($"Maintainability gate (CA1505 index >= {MinimumIndex}, CA1502 complexity <= {MaximumComplexity}, " +
            $"balanced coupling; test projects excluded): violations={total} (metrics={violations.Length}, " +
            $"coupling={couplingViolations}). {verdict}");
        return total == 0 ? 0 : 1;
    }

    static string Label(Metric metric) => metric switch
    {
        Metric.MaintainabilityIndex => "MI",
        Metric.CyclomaticComplexity => "CC",
        _ => throw new ArgumentOutOfRangeException(nameof(metric))
    };

    // The analyzer's diagnostic ID is the boundary string; anything else is refused.
    static Metric ParseRule(string rule) => rule switch
    {
        "CA1505" => Metric.MaintainabilityIndex,
        "CA1502" => Metric.CyclomaticComplexity,
        _ => throw new InvalidOperationException("Unexpected code-metrics rule: " + rule)
    };

    static string WriteInjection(string work, string inventory)
    {
        var config = Path.Combine(work, "maintainability.globalconfig");
        var thresholds = Path.Combine(work, "CodeMetricsConfig.txt");
        File.WriteAllText(config,
            "is_global = true\ndotnet_diagnostic.CA1505.severity = warning\ndotnet_diagnostic.CA1502.severity = warning\n");
        // CA1505 reports an index below its value; CA1502 reports a complexity above its value.
        File.WriteAllText(thresholds, $"CA1505: {MinimumIndex}\nCA1502: {MaximumComplexity}\n");
        var targets = Path.Combine(work, "maintainability.targets");
        // Both rules stay warnings so every project still builds and reports; all other warnings remain errors.
        // Test projects (IsTestProject=true) receive no metrics configuration, so neither rule runs on them.
        File.WriteAllText(targets, $"""
            <Project>
              <PropertyGroup><WarningsNotAsErrors>$(WarningsNotAsErrors);CA1505;CA1502</WarningsNotAsErrors></PropertyGroup>
              <ItemGroup Condition="'$(IsTestProject)' != 'true'">
                <EditorConfigFiles Include="{System.Security.SecurityElement.Escape(config)}" />
                <AdditionalFiles Include="{System.Security.SecurityElement.Escape(thresholds)}" />
              </ItemGroup>
            {CouplingGate.InventoryTarget(inventory)}
            </Project>
            """);
        return targets;
    }

    static void Canary(string work, string targets, string inventory)
    {
        var reported = Analyse(work, CanaryBuild(work, "Canary", ""), targets, inventory);
        foreach (var metric in Enum.GetValues<Metric>())
            if (!reported.Any(violation => violation.Metric == metric))
                throw new InvalidOperationException($"The canary was not reported for {metric}, so its rule did not run.");
        // The same member in a test project is the control: any report means the test exclusion failed.
        if (Analyse(work, CanaryBuild(work, "TestCanary", "#:property IsTestProject=true\n"), targets, inventory).Length != 0)
            throw new InvalidOperationException("The test-project canary was reported, so test projects are not excluded.");
        Console.WriteLine("PASS canary reported for every metric; test-project canary not reported");
    }

    static Build CanaryBuild(string work, string name, string header)
    {
        var directory = Directory.CreateDirectory(Path.Combine(work, name)).FullName;
        var source = new System.Text.StringBuilder(header +
            "System.Console.WriteLine(Canary.Score(args.Length));\nstatic class Canary\n{\n    public static int Score(int x)\n    {\n        var y = 0;\n");
        for (var i = 1; i <= 120; i++)
            source.Append(System.Globalization.CultureInfo.InvariantCulture, $"        if (x == {i}) y += {i} * x % 7;\n");
        source.Append("        return y;\n    }\n}\n");
        var file = Path.Combine(directory, name + ".cs");
        File.WriteAllText(file, source.ToString());
        return new(name, ["build", file, "--artifacts-path", Path.Combine(work, name + "-artifacts")], Coupling: false);
    }

    internal static IEnumerable<Build> Builds(string root, string work)
    {
        yield return new("solution", ["build", "CuriousContraptions.slnx", "-noAutoResponse"], Coupling: true);
        yield return new("playtest", ["build", "CuriousContraptions.slnx", "-noAutoResponse", "-p:PlaytestDiagnostics=true"],
            Coupling: true);
        yield return new("web", ["build", "CuriousContraptions.web/CuriousContraptions.web.csproj", "-noAutoResponse",
            "-p:WasmBuildOnlyAfterPublish=true", "-p:TwoDogAutoImport=false"], Coupling: true);
        // File-based apps reject MSBuild-only switches; a separate artifacts path never overwrites a running app.
        // They have no project references, so coupling reads only the three project builds above.
        foreach (var app in FileBasedApps(root))
            yield return new(app, ["build", app, "--artifacts-path", Path.Combine(work, "apps", app)], Coupling: false);
    }

    // A file-based app is a C# file whose first line is a #: directive; tracked or new, never ignored.
    static string[] FileBasedApps(string root)
    {
        var (exitCode, output) = Execute(root, "git", ["ls-files", "--cached", "--others", "--exclude-standard", "--", "*.cs"]);
        if (exitCode != 0) throw new InvalidOperationException("Cannot list repository C# files: " + output);
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(path => File.Exists(Path.Combine(root, path)) &&
                File.ReadLines(Path.Combine(root, path)).FirstOrDefault()?.StartsWith("#:", StringComparison.Ordinal) == true)
            .Order(StringComparer.Ordinal).ToArray();
    }

    static Violation[] Analyse(string root, Build build, string targets, string inventory)
    {
        var output = Compile(root, build, targets, inventory);
        return [.. DiagnosticLine().Matches(output).Select(match => new Violation(
            ParseRule(match.Groups["rule"].Value),
            Path.GetRelativePath(root, match.Groups["file"].Value),
            int.Parse(match.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture),
            match.Groups["symbol"].Value,
            int.Parse(match.Groups["score"].Value, System.Globalization.CultureInfo.InvariantCulture)))];
    }

    // Rebuilds from scratch with the injected targets; the emptied inventory folder then lists only this build's projects.
    internal static string Compile(string root, Build build, string targets, string inventory)
    {
        if (Directory.Exists(inventory)) Directory.Delete(inventory, true);
        Directory.CreateDirectory(inventory);
        string[] arguments = [.. build.Arguments, "-nologo", "-v", "q", "--no-incremental",
            "-p:CustomAfterMicrosoftCommonTargets=" + targets];
        var (exitCode, output) = Execute(root, "dotnet", arguments);
        return exitCode == 0 ? output : throw new InvalidOperationException(
            $"{build.Label} build failed (exit {exitCode}); the gate scores only a compiling candidate.\n{output}");
    }

    static (int ExitCode, string Output) Execute(string directory, string executable, string[] arguments)
    {
        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = directory, RedirectStandardOutput = true,
            RedirectStandardError = true, UseShellExecute = false
        };
        // Diagnostics are parsed from their English text.
        start.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start " + executable + ".");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }

    [System.Text.RegularExpressions.GeneratedRegex(
        @"^\s*(?<file>.+?)\((?<line>\d+),\d+\): warning (?<rule>CA1505|CA1502): '(?<symbol>[^']+)' has a (?:maintainability index|cyclomatic complexity) of '(?<score>\d+)'",
        System.Text.RegularExpressions.RegexOptions.Multiline)]
    private static partial System.Text.RegularExpressions.Regex DiagnosticLine();
}

/// <summary>
/// Coupling gate (owner rule, 6 November 2026; rule family E9 and docs/architecture/element-package-isolation.md §3, Vlad
/// Khononov's Balanced Coupling). Every dependency between two production projects is read from their compiled IL.
/// <b>Distance:</b> each measured dependency crosses a project boundary, so it is high distance; coupling inside one project
/// is left to the code metrics and review. <b>Strength:</b> a public type or member of a declared contract project (the
/// Abstractions family or the FlatBuffers wire contract) is Contract; a public type or member of any other project is Model;
/// a non-public type or member reached through InternalsVisibleTo, an UnsafeAccessor method, non-public reflection (the
/// BindingFlags NonPublic flag in a production source) or one source file compiled into two production assemblies is
/// Intrusive. Functional coupling (shared knowledge without a reference) is invisible to metadata and stays with review.
/// <b>Volatility</b> is declared: engine and element projects are volatile core; contracts and tools are stable.
/// A dependency fails when an element reaches another element or anything outside the Abstractions, when it is intrusive,
/// or when it is model coupling to a volatile project. Kinds are declared in <see cref="Declared"/>, with elements/* and
/// tools/* classified by path; an undeclared project is refused. Test projects (IsTestProject=true) are excluded and listed
/// in every run, and a production project that references one is refused. File-based apps have no project references and
/// are out of scope. Before the codebase is read, a fixture solution holding one case of every strength and rule, with
/// clean controls, must produce exactly its expected findings.
/// </summary>
static class CouplingGate
{
    internal enum Kind { Abstraction, WireContract, Element, Engine, Tool, Test }
    internal enum Strength { Contract, Model, Intrusive }
    internal enum Rule { SiblingElement, Intrusive, ElementBoundary, UnbalancedModel }
    internal sealed record Project(string Path, string Assembly, string Output, Kind Kind, string[] Sources);
    internal sealed record Dependency(string Consumer, Kind ConsumerKind, string Target, Kind? TargetKind, Strength Strength, string Symbol);
    internal sealed record Violation(Rule Rule, Dependency Dependency);
    internal sealed record Report(string[] Excluded, Dependency[] Dependencies, Violation[] Violations);
    sealed record Target(Project Project, Image Image);

    const string SharedSource = "shared-source";
    const string Reflection = "reflection";
    const string UnsafeAccessor = "unsafe-accessor";
    const string NonPublicReflection = "BindingFlags.NonPublic";
    const string UnsafeAccessorAttribute = "System.Runtime.CompilerServices.UnsafeAccessorAttribute";

    // Declared kinds, and with them volatility. Changing a kind to make a dependency pass weakens the gate (E9: Fail).
    static readonly Dictionary<string, Kind> Declared = new(StringComparer.Ordinal)
    {
        ["CuriousContraptions.Abstractions/CuriousContraptions.Abstractions.csproj"] = Kind.Abstraction,
        ["CuriousContraptions.Abstractions.Electrical/CuriousContraptions.Abstractions.Electrical.csproj"] = Kind.Abstraction,
        ["CuriousContraptions.Abstractions.Fluid/CuriousContraptions.Abstractions.Fluid.csproj"] = Kind.Abstraction,
        ["CuriousContraptions.Abstractions.Mechanical/CuriousContraptions.Abstractions.Mechanical.csproj"] = Kind.Abstraction,
        ["CuriousContraptions.Abstractions.Signal/CuriousContraptions.Abstractions.Signal.csproj"] = Kind.Abstraction,
        ["CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj"] = Kind.WireContract,
        ["CuriousContraptions.csproj"] = Kind.Engine,
        ["CuriousContraptions.2dog/CuriousContraptions.2dog.csproj"] = Kind.Engine,
        ["CuriousContraptions.web/CuriousContraptions.web.csproj"] = Kind.Engine,
        ["CuriousContraptions.Core/CuriousContraptions.Core.csproj"] = Kind.Engine,
        ["CuriousContraptions.Simulation/CuriousContraptions.Simulation.csproj"] = Kind.Engine,
        ["CuriousContraptions.Animation/CuriousContraptions.Animation.csproj"] = Kind.Engine,
        ["CuriousContraptions.Animation.Worker/CuriousContraptions.Animation.Worker.csproj"] = Kind.Engine,
        ["CuriousContraptions.Bundle/CuriousContraptions.Bundle.csproj"] = Kind.Engine,
    };

    public static int Run(string[] args) => MaintainabilityGate.Guard("Coupling gate", "--coupling", args, Gate);

    static int Gate(string root, string work)
    {
        var inventory = Path.Combine(work, "inventory");
        var targets = Path.Combine(work, "coupling.targets");
        File.WriteAllText(targets, "<Project>\n" + InventoryTarget(inventory) + "\n</Project>\n");
        Canary(work);
        var reports = new List<Report>();
        foreach (var build in MaintainabilityGate.Builds(root, work).Where(build => build.Coupling))
        {
            MaintainabilityGate.Compile(root, build, targets, inventory);
            reports.Add(Analyse(root, inventory, Classify));
        }
        var violations = Print(reports);
        var verdict = violations == 0 ? "PASS" : "FAIL";
        Console.WriteLine($"Coupling gate (balanced coupling; test projects excluded): violations={violations}. {verdict}");
        return violations == 0 ? 0 : 1;
    }

    internal static Kind Classify(string path)
    {
        if (Declared.TryGetValue(path, out var kind)) return kind;
        return path.Split('/') switch
        {
            ["elements", _, _] => Kind.Element,
            ["tools", _, _] => Kind.Tool,
            _ => throw new InvalidOperationException($"Undeclared project {path}: its kind needs an owner decision in CouplingGate.Declared.")
        };
    }

    static bool Volatile(Kind? kind) => kind is Kind.Engine or Kind.Element;

    // Each built project records its path, output assembly, test marker and compiled sources; a unique name per
    // project instance keeps parallel builds of one project from writing the same file.
    internal static string InventoryTarget(string directory)
    {
        var file = System.Security.SecurityElement.Escape(
            Path.Combine(directory, "$(MSBuildProjectName).$([System.Guid]::NewGuid()).inventory"));
        return $"""
              <Target Name="CouplingInventory" AfterTargets="Build" Condition="'$(TargetPath)' != ''">
                <ItemGroup>
                  <CouplingInventoryLine Include="project=$(MSBuildProjectFullPath);assembly=$(TargetPath);test=$(IsTestProject)" />
                  <CouplingInventoryLine Include="@(Compile->'source=%(FullPath)')" />
                </ItemGroup>
                <WriteLinesToFile File="{file}" Lines="@(CouplingInventoryLine)" Overwrite="true" />
              </Target>
            """;
    }

    internal static Report Analyse(string root, string inventory, Func<string, Kind> classify)
    {
        var real = RealPath(root);
        var projects = Inventory(real, inventory, classify);
        var images = new Dictionary<string, Image>(StringComparer.Ordinal);
        try
        {
            foreach (var project in projects) images.Add(project.Path, new Image(project.Output));
            var targets = Targets(projects, images);
            var production = projects.Where(project => project.Kind != Kind.Test).ToArray();
            Dependency[] dependencies = [.. production.SelectMany(project => References(project, images[project.Path].Reader, targets))
                .Concat(SharedSources(real, production)).Concat(ReflectionSites(real, production)).Distinct()];
            Violation[] violations = [.. dependencies.Select(dependency => Judge(dependency) is { } rule ? new Violation(rule, dependency) : null)
                .OfType<Violation>()];
            return new([.. projects.Where(project => project.Kind == Kind.Test).Select(project => project.Path)], dependencies, violations);
        }
        finally
        {
            foreach (var image in images.Values) image.Dispose();
        }
    }

    static Rule? Judge(Dependency dependency) => (dependency.ConsumerKind, dependency.TargetKind, dependency.Strength) switch
    {
        (Kind.Element, Kind.Element, _) => Rule.SiblingElement,
        (_, _, Strength.Intrusive) => Rule.Intrusive,
        (Kind.Element, not Kind.Abstraction, _) => Rule.ElementBoundary,
        (_, _, Strength.Model) when Volatile(dependency.TargetKind) => Rule.UnbalancedModel,
        _ => null
    };

    internal static int Print(IReadOnlyCollection<Report> reports)
    {
        foreach (var path in reports.SelectMany(report => report.Excluded).Distinct().Order(StringComparer.Ordinal))
            Console.WriteLine("EXCLUDED test project " + path);
        Violation[] violations = [.. reports.SelectMany(report => report.Violations).Distinct()
            .OrderBy(violation => violation.Rule).ThenBy(violation => violation.Dependency.Consumer, StringComparer.Ordinal)
            .ThenBy(violation => violation.Dependency.Target, StringComparer.Ordinal)
            .ThenBy(violation => violation.Dependency.Symbol, StringComparer.Ordinal)];
        foreach (var violation in violations) Console.WriteLine("FAIL COUPLING " + Describe(violation));
        foreach (var edge in violations.GroupBy(violation => (violation.Rule, violation.Dependency.Consumer, violation.Dependency.Target)))
            Console.WriteLine($"EDGE {edge.Key.Rule} {edge.Key.Consumer} -> {edge.Key.Target}: {edge.Count()} symbol(s)");
        var dependencies = reports.SelectMany(report => report.Dependencies).Distinct().ToArray();
        Console.WriteLine($"Coupling dependencies: contract={Count(dependencies, Strength.Contract)}, " +
            $"model={Count(dependencies, Strength.Model)}, intrusive={Count(dependencies, Strength.Intrusive)}; violations={violations.Length}");
        return violations.Length;
    }

    static int Count(Dependency[] dependencies, Strength strength) => dependencies.Count(dependency => dependency.Strength == strength);

    static string Describe(Violation violation) =>
        $"{violation.Rule} {violation.Dependency.Strength} {violation.Dependency.Consumer} -> {violation.Dependency.Target} {violation.Dependency.Symbol}";

    static Project[] Inventory(string root, string directory, Func<string, Kind> classify)
    {
        Project[] projects = [.. Directory.GetFiles(directory, "*.inventory").Order(StringComparer.Ordinal)
            .Select(file => ReadProject(root, File.ReadAllLines(file), classify)).DistinctBy(project => project.Path)];
        return projects.Length > 0 ? projects
            : throw new InvalidOperationException("The build recorded no projects, so the coupling inventory target did not run.");
    }

    static Project ReadProject(string root, string[] lines, Func<string, Kind> classify)
    {
        string Value(string key) => lines.Single(line => line.StartsWith(key + "=", StringComparison.Ordinal))[(key.Length + 1)..];
        var path = Relative(root, Value("project"));
        var output = Value("assembly");
        var test = string.Equals(Value("test"), "true", StringComparison.OrdinalIgnoreCase);
        string[] sources = [.. lines.Where(line => line.StartsWith("source=", StringComparison.Ordinal)).Select(line => line["source=".Length..])];
        return new(path, Path.GetFileNameWithoutExtension(output), output, test ? Kind.Test : classify(path), sources);
    }

    static Dictionary<string, Target> Targets(Project[] projects, Dictionary<string, Image> images)
    {
        var targets = new Dictionary<string, Target>(StringComparer.Ordinal);
        foreach (var project in projects)
        {
            var image = images[project.Path];
            if (image.Name != project.Assembly)
                throw new InvalidOperationException($"{project.Output} defines assembly {image.Name}, not {project.Assembly}.");
            if (!targets.TryAdd(project.Assembly, new(project, image)))
                throw new InvalidOperationException($"Two projects build assembly {project.Assembly}.");
        }
        return targets;
    }

    static IEnumerable<Dependency> References(Project consumer, MetadataReader reader, Dictionary<string, Target> targets)
    {
        foreach (var handle in reader.TypeReferences)
            if (Locate(reader, handle, consumer, targets) is { } target)
            {
                var name = ReferenceName(reader, handle);
                var exposed = TypeExposed(target.Image.Reader, target.Image.Find(name));
                yield return Edge(consumer, target, exposed ? ExposedStrength(target.Project.Kind) : Strength.Intrusive, name);
            }
        foreach (var handle in reader.MemberReferences)
            if (HiddenMember(reader, handle, consumer, targets) is { } dependency) yield return dependency;
        foreach (var method in UnsafeAccessors(reader))
            yield return new(consumer.Assembly, consumer.Kind, UnsafeAccessor, null, Strength.Intrusive, method);
    }

    static Strength ExposedStrength(Kind kind) => kind is Kind.Abstraction or Kind.WireContract ? Strength.Contract : Strength.Model;

    static Dependency Edge(Project consumer, Target target, Strength strength, string symbol) =>
        new(consumer.Assembly, consumer.Kind, target.Project.Assembly, target.Project.Kind, strength, symbol);

    // The inventoried project that defines a referenced type, or null for the consumer itself and outside assemblies.
    static Target? Locate(MetadataReader reader, TypeReferenceHandle handle, Project consumer, Dictionary<string, Target> targets)
    {
        if (ReferenceAssembly(reader, handle) is not { } assembly || assembly == consumer.Assembly
            || !targets.TryGetValue(assembly, out var target)) return null;
        return target.Project.Kind != Kind.Test ? target
            : throw new InvalidOperationException($"Production project {consumer.Path} references test project {target.Project.Path}.");
    }

    // A member reached through InternalsVisibleTo on an otherwise public type is intrusive.
    static Dependency? HiddenMember(MetadataReader reader, MemberReferenceHandle handle, Project consumer, Dictionary<string, Target> targets)
    {
        var member = reader.GetMemberReference(handle);
        if (ParentType(reader, member.Parent) is not { } parent || Locate(reader, parent, consumer, targets) is not { } target) return null;
        var type = ReferenceName(reader, parent);
        var name = reader.GetString(member.Name);
        var field = member.GetKind() == MemberReferenceKind.Field;
        var signature = field ? member.DecodeFieldSignature(SignatureText.Instance, null)
            : SignatureText.Render(member.DecodeMethodSignature(SignatureText.Instance, null));
        return target.Image.MemberExposed(type, name, field, signature) ? null : Edge(consumer, target, Strength.Intrusive, type + "::" + name);
    }

    static TypeReferenceHandle? ParentType(MetadataReader reader, EntityHandle parent) => parent.Kind switch
    {
        HandleKind.TypeReference => (TypeReferenceHandle)parent,
        HandleKind.TypeSpecification => GenericDefinition(reader, (TypeSpecificationHandle)parent),
        _ => null
    };

    // A member of a generic instantiation names its type through a specification: GENERICINST (CLASS|VALUETYPE) type.
    static TypeReferenceHandle? GenericDefinition(MetadataReader reader, TypeSpecificationHandle handle)
    {
        var blob = reader.GetBlobReader(reader.GetTypeSpecification(handle).Signature);
        if (blob.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance) return null;
        blob.ReadSignatureTypeCode();
        var type = blob.ReadTypeHandle();
        return type.Kind == HandleKind.TypeReference ? (TypeReferenceHandle)type : null;
    }

    static IEnumerable<string> UnsafeAccessors(MetadataReader reader)
    {
        foreach (var handle in reader.CustomAttributes)
        {
            var attribute = reader.GetCustomAttribute(handle);
            if (attribute.Parent.Kind != HandleKind.MethodDefinition || AttributeType(reader, attribute.Constructor) != UnsafeAccessorAttribute)
                continue;
            var method = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Parent);
            yield return DefinitionName(reader, method.GetDeclaringType()) + "::" + reader.GetString(method.Name);
        }
    }

    static string? AttributeType(MetadataReader reader, EntityHandle constructor) =>
        constructor.Kind == HandleKind.MemberReference
        && reader.GetMemberReference((MemberReferenceHandle)constructor).Parent is { Kind: HandleKind.TypeReference } parent
            ? ReferenceName(reader, (TypeReferenceHandle)parent) : null;

    // One source file compiled into two production assemblies couples them through shared implementation.
    static IEnumerable<Dependency> SharedSources(string root, Project[] production)
    {
        var compiled = production.SelectMany(project => project.Sources.Where(source => InRoot(root, source))
            .Select(source => (Source: source, Project: project))).GroupBy(pair => pair.Source, StringComparer.Ordinal);
        foreach (var group in compiled)
        {
            Project[] owners = [.. group.Select(pair => pair.Project).DistinctBy(project => project.Assembly)
                .OrderBy(project => project.Assembly, StringComparer.Ordinal)];
            if (owners.Length > 1)
                yield return new(owners[0].Assembly, owners[0].Kind, string.Join(" + ", owners.Skip(1).Select(project => project.Assembly)),
                    null, Strength.Intrusive, SharedSource + " " + Relative(root, group.Key));
        }
    }

    // Reflection over non-public members bypasses every declared surface; the reflected type is not visible to this scan.
    static IEnumerable<Dependency> ReflectionSites(string root, Project[] production) =>
        production.SelectMany(project => project.Sources.Where(source => InRoot(root, source) && File.Exists(source))
            .SelectMany(source => File.ReadLines(source).Select((line, index) => (Line: line, Number: index + 1))
                .Where(entry => entry.Line.Contains(NonPublicReflection, StringComparison.Ordinal))
                .Select(entry => new Dependency(project.Assembly, project.Kind, Reflection, null, Strength.Intrusive,
                    $"{Relative(root, source)}:{entry.Number}"))));

    static bool InRoot(string root, string path) => !Relative(root, path).StartsWith("../", StringComparison.Ordinal);

    static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

    // MSBuild reports physical paths, so symbolic links in the root (such as macOS /var and /tmp) are resolved first.
    static string RealPath(string path)
    {
        var full = Path.GetFullPath(path);
        var current = Path.GetPathRoot(full) ?? throw new InvalidOperationException("No filesystem root for " + full);
        foreach (var part in full[current.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            if (new DirectoryInfo(current).ResolveLinkTarget(returnFinalTarget: true) is { } target) current = target.FullName;
        }
        return current;
    }

    static string ReferenceName(MetadataReader reader, TypeReferenceHandle handle)
    {
        var type = reader.GetTypeReference(handle);
        var name = reader.GetString(type.Name);
        if (type.ResolutionScope.Kind == HandleKind.TypeReference)
            return ReferenceName(reader, (TypeReferenceHandle)type.ResolutionScope) + "/" + name;
        var space = reader.GetString(type.Namespace);
        return space.Length == 0 ? name : space + "." + name;
    }

    static string? ReferenceAssembly(MetadataReader reader, TypeReferenceHandle handle)
    {
        var scope = reader.GetTypeReference(handle).ResolutionScope;
        return scope.Kind switch
        {
            HandleKind.TypeReference => ReferenceAssembly(reader, (TypeReferenceHandle)scope),
            HandleKind.AssemblyReference => reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)scope).Name),
            _ => null
        };
    }

    static string DefinitionName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        var name = reader.GetString(type.Name);
        var outer = type.GetDeclaringType();
        if (!outer.IsNil) return DefinitionName(reader, outer) + "/" + name;
        var space = reader.GetString(type.Namespace);
        return space.Length == 0 ? name : space + "." + name;
    }

    // Public and protected surface is exposed; internal and private surface is reachable only intrusively.
    static bool TypeExposed(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        var outer = type.GetDeclaringType();
        return (type.Attributes & TypeAttributes.VisibilityMask) is TypeAttributes.Public or TypeAttributes.NestedPublic
                or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem
            && (outer.IsNil || TypeExposed(reader, outer));
    }

    static bool Exposed(MethodAttributes access) => access is MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem;

    static bool Exposed(FieldAttributes access) => access is FieldAttributes.Public or FieldAttributes.Family or FieldAttributes.FamORAssem;

    sealed class Image : IDisposable
    {
        readonly PEReader file;
        readonly Dictionary<string, TypeDefinitionHandle> types = new(StringComparer.Ordinal);

        public Image(string path)
        {
            file = new PEReader(ImmutableArray.Create(File.ReadAllBytes(path)));
            Reader = file.GetMetadataReader();
            Name = Reader.GetString(Reader.GetAssemblyDefinition().Name);
            foreach (var handle in Reader.TypeDefinitions) types[DefinitionName(Reader, handle)] = handle;
        }

        public MetadataReader Reader { get; }
        public string Name { get; }

        public TypeDefinitionHandle Find(string name) =>
            types.TryGetValue(name, out var handle) ? handle : throw new InvalidOperationException($"{Name} does not define {name}.");

        // Searches the referenced type, then its base types in the same assembly, for the exact member signature.
        public bool MemberExposed(string type, string name, bool field, string signature)
        {
            for (var handle = Find(type); !handle.IsNil; handle = BaseDefinition(handle))
                if ((field ? FieldExposed(handle, name, signature) : MethodExposed(handle, name, signature)) is { } exposed) return exposed;
            throw new InvalidOperationException($"{Name} does not define {type}::{name} {signature}.");
        }

        bool? FieldExposed(TypeDefinitionHandle handle, string name, string signature) => Reader.GetTypeDefinition(handle).GetFields()
            .Select(Reader.GetFieldDefinition)
            .Where(field => Reader.StringComparer.Equals(field.Name, name) && field.DecodeSignature(SignatureText.Instance, null) == signature)
            .Select(field => (bool?)Exposed(field.Attributes & FieldAttributes.FieldAccessMask)).FirstOrDefault();

        bool? MethodExposed(TypeDefinitionHandle handle, string name, string signature) => Reader.GetTypeDefinition(handle).GetMethods()
            .Select(Reader.GetMethodDefinition)
            .Where(method => Reader.StringComparer.Equals(method.Name, name)
                && SignatureText.Render(method.DecodeSignature(SignatureText.Instance, null)) == signature)
            .Select(method => (bool?)Exposed(method.Attributes & MethodAttributes.MemberAccessMask)).FirstOrDefault();

        TypeDefinitionHandle BaseDefinition(TypeDefinitionHandle handle)
        {
            var baseType = Reader.GetTypeDefinition(handle).BaseType;
            return !baseType.IsNil && baseType.Kind == HandleKind.TypeDefinition ? (TypeDefinitionHandle)baseType : default;
        }

        public void Dispose() => file.Dispose();
    }

    // Renders signatures to text so a reference's signature compares equal to its definition's across assemblies.
    sealed class SignatureText : ISignatureTypeProvider<string, object?>
    {
        public static readonly SignatureText Instance = new();

        public static string Render(MethodSignature<string> signature) =>
            $"{signature.GenericParameterCount}:{signature.ReturnType}({string.Join(",", signature.ParameterTypes)})";

        public string GetArrayType(string elementType, ArrayShape shape) => elementType + "[" + new string(',', shape.Rank - 1) + "]";
        public string GetByReferenceType(string elementType) => elementType + "&";
        public string GetFunctionPointerType(MethodSignature<string> signature) => "method " + Render(signature);
        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) =>
            genericType + "<" + string.Join(",", typeArguments) + ">";
        public string GetGenericMethodParameter(object? genericContext, int index) =>
            string.Create(System.Globalization.CultureInfo.InvariantCulture, $"!!{index}");
        public string GetGenericTypeParameter(object? genericContext, int index) =>
            string.Create(System.Globalization.CultureInfo.InvariantCulture, $"!{index}");
        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired) =>
            $"{unmodifiedType} {(isRequired ? "modreq" : "modopt")}({modifier})";
        public string GetPinnedType(string elementType) => elementType;
        public string GetPointerType(string elementType) => elementType + "*";
        public string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode.ToString();
        public string GetSZArrayType(string elementType) => elementType + "[]";
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => DefinitionName(reader, handle);
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => ReferenceName(reader, handle);
        public string GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    }

    // The fixture: one case of every strength and rule, plus a contract control, a stable-model control and a test project.
    const string MachineSource = """
        public static class Machine
        {
            public static int Run(Port port, Frame frame) =>
                new Solver().Step() + new Solver().Poke() + new Secret().Value() + Helper.One() + SharedRule.Apply() + (port is null || frame is null ? 0 : 1);

            public static int Hidden() => typeof(Solver).GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Length;

            [System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "Poke")]
            public static extern int Poke(Solver solver);
        }
        """;

    internal static void Canary(string work)
    {
        var root = Directory.CreateDirectory(Path.Combine(work, "coupling-canary")).FullName;
        var inventory = Path.Combine(work, "coupling-canary-inventory");
        var targets = Path.Combine(work, "coupling-canary.targets");
        File.WriteAllText(targets, "<Project>\n" + InventoryTarget(inventory) + "\n</Project>\n");
        WriteFixture(root);
        MaintainabilityGate.Compile(root, new("coupling canary", ["build", "Fixture.slnx"], Coupling: true), targets, inventory);
        var report = Analyse(root, inventory, FixtureKind);
        string[] actual = [.. report.Violations.Select(Describe).Order(StringComparer.Ordinal)];
        var controls = report.Dependencies.Any(dependency => dependency is { Strength: Strength.Contract, Symbol: "Fixture.Port" })
            && report.Dependencies.Any(dependency => dependency is { Strength: Strength.Model, Symbol: "Fixture.Helper" });
        if (!actual.SequenceEqual(ExpectedFixture().Order(StringComparer.Ordinal)) || !controls
            || report.Excluded is not ["Fixture.Tests/Fixture.Tests.csproj"])
            throw new InvalidOperationException("The coupling canary did not produce exactly its expected findings:\n" + string.Join('\n', actual));
        Console.WriteLine("PASS coupling canary reported every strength and rule; controls clean; test project excluded");
    }

    static IEnumerable<string> ExpectedFixture()
    {
        var reflectionLine = Array.FindIndex(("namespace Fixture;\n" + MachineSource).Split('\n'),
            line => line.Contains(NonPublicReflection, StringComparison.Ordinal)) + 1;
        yield return "SiblingElement Model Fixture.ElementA -> Fixture.ElementB Fixture.B";
        yield return "Intrusive Intrusive Fixture.Host -> Fixture.Engine Fixture.Secret";
        yield return "Intrusive Intrusive Fixture.Host -> Fixture.Engine Fixture.Solver::Poke";
        yield return "Intrusive Intrusive Fixture.Engine -> Fixture.Host " + SharedSource + " Fixture.Engine/Shared.cs";
        yield return "Intrusive Intrusive Fixture.Host -> " + Reflection + " Fixture.Host/Host.cs:" +
            reflectionLine.ToString(System.Globalization.CultureInfo.InvariantCulture);
        yield return "Intrusive Intrusive Fixture.Host -> " + UnsafeAccessor + " Fixture.Machine::Poke";
        yield return "ElementBoundary Contract Fixture.ElementA -> Fixture.Wire Fixture.Frame";
        yield return "UnbalancedModel Model Fixture.Host -> Fixture.Engine Fixture.Solver";
    }

    static Kind FixtureKind(string path) => Path.GetFileNameWithoutExtension(path) switch
    {
        "Fixture.Abstractions" => Kind.Abstraction,
        "Fixture.Wire" => Kind.WireContract,
        "Fixture.Library" => Kind.Tool,
        "Fixture.Engine" or "Fixture.Host" => Kind.Engine,
        "Fixture.ElementA" or "Fixture.ElementB" => Kind.Element,
        _ => throw new InvalidOperationException("Undeclared fixture project " + path)
    };

    static void WriteFixture(string root)
    {
        FixtureProject(root, "Fixture.Abstractions", [], "", "public sealed class Port { }");
        FixtureProject(root, "Fixture.Wire", [], "", "public sealed class Frame { }");
        FixtureProject(root, "Fixture.Library", [], "", "public static class Helper { public static int One() => 1; }");
        FixtureProject(root, "Fixture.Engine", [],
            """<ItemGroup><InternalsVisibleTo Include="Fixture.Host" /><InternalsVisibleTo Include="Fixture.Tests" /></ItemGroup>""",
            "public sealed class Solver { public int Step() => 1; internal int Poke() => 2; }\ninternal sealed class Secret { public int Value() => 3; }");
        File.WriteAllText(Path.Combine(root, "Fixture.Engine", "Shared.cs"),
            "namespace Fixture;\ninternal static class SharedRule { public static int Apply() => 4; }\n");
        FixtureProject(root, "Fixture.Host", ["Fixture.Abstractions", "Fixture.Wire", "Fixture.Library", "Fixture.Engine"],
            """<ItemGroup><Compile Include="../Fixture.Engine/Shared.cs" /></ItemGroup>""", MachineSource);
        FixtureProject(root, "Fixture.ElementB", ["Fixture.Abstractions"], "",
            "public static class B { public static int Value() => 1; public static Port? Make() => null; }");
        FixtureProject(root, "Fixture.ElementA", ["Fixture.Abstractions", "Fixture.Wire", "Fixture.ElementB"], "",
            "public static class A { public static int Use(Port port, Frame frame) => B.Value() + (port is null || frame is null ? 0 : 1); }");
        FixtureProject(root, "Fixture.Tests", ["Fixture.Engine"], "<PropertyGroup><IsTestProject>true</IsTestProject></PropertyGroup>",
            "public static class Probe { public static int Run() => new Secret().Value() + new Solver().Poke(); }");
        var projects = Directory.GetDirectories(root).Select(Path.GetFileName).Order(StringComparer.Ordinal)
            .Select(name => $"  <Project Path=\"{name}/{name}.csproj\" />\n");
        File.WriteAllText(Path.Combine(root, "Fixture.slnx"), "<Solution>\n" + string.Concat(projects) + "</Solution>\n");
    }

    static void FixtureProject(string root, string name, string[] references, string extra, string source)
    {
        var directory = Directory.CreateDirectory(Path.Combine(root, name)).FullName;
        var items = string.Concat(references.Select(reference => $"<ProjectReference Include=\"../{reference}/{reference}.csproj\" />"));
        File.WriteAllText(Path.Combine(directory, name + ".csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable></PropertyGroup>
              <ItemGroup>{items}</ItemGroup>
              {extra}
            </Project>
            """);
        File.WriteAllText(Path.Combine(directory, name.Split('.')[1] + ".cs"), "namespace Fixture;\n" + source + "\n");
    }
}
