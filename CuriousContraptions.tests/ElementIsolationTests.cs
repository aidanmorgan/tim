using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CuriousContraptions.Tests;

/// <summary>Early package isolation: source knowledge is explicit owned debt, never a growing allowance.</summary>
public sealed class ElementIsolationTests
{
    public enum Rule { ElementLiteral, NamedPart, PackageIdentifier, SolverPin }
    public enum Owner
    {
        FinalIsolation, Lessons, Events, GateDrive, Ramp, Receiver, Basketball, Bowling,
        Bumper, Springboard, Switch, Lamp, Detector, Delay, Battery, Domino, Plate, Counter, Clock, Latch
    }

    public enum GenericMeaning { ClockProtocol, CaptureProtocol, SoftSpringLaw, Duration, DeclaredCapability }
    public sealed record SemanticContext(string File, string Symbol, string Token, int Count, GenericMeaning Meaning);
    public sealed record IsolationSnapshot(Entry[] Known, SemanticContext[] Generic);
    public sealed record Entry(string File, string Symbol, Rule Rule, string Token, int Count, Owner Owner);
    private sealed record Hit(string File, string Symbol, Rule Rule, string Token, int Line);
    private sealed record Lexeme(string Text, int Start, bool Literal);
    private const string ListPath = "CuriousContraptions.tests/Fixtures/element-isolation-known.json";
    private const string SolverPath = "CuriousContraptions.tests/WorkshopPhysicsLawTests.cs";
    private const string InitialCensusSha256 = "200e2b40662397bb76306734c3e17659583e64bfc69217ef344e25caef6d4cb7";
    private static readonly string Root = twodog.Engine.ResolveProjectDir();
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    private static readonly string[] Outputs = ["bin", "obj", ".godot", ".git"];
    private static readonly Dictionary<Owner, string> SprintKeys = new Dictionary<Owner, string>
    {
        [Owner.FinalIsolation] = "9a-45-compile-time-isolation-gate", [Owner.Lessons] = "9a-26-lessons-as-content-data",
        [Owner.Events] = "9a-22-event-ring-producer",
        [Owner.GateDrive] = "9a-c-gate-declared-drive-warmup", [Owner.Ramp] = "9a-c-ramp-receiver-conversion",
        [Owner.Receiver] = "9a-c-ramp-receiver-conversion", [Owner.Basketball] = "9a-c-balls-conversion",
        [Owner.Bowling] = "9a-c-balls-conversion", [Owner.Bumper] = "9a-c-bumper-spring-conversion",
        [Owner.Springboard] = "9a-c-bumper-spring-conversion", [Owner.Switch] = "9a-c-switch-impact-switch-conversion",
        [Owner.Lamp] = "9a-c-lamp-detector-conversion", [Owner.Detector] = "9a-c-lamp-detector-conversion",
        [Owner.Delay] = "9-5a-declared-timer-switched-contact-delay-conversion", [Owner.Battery] = "9a-39-generic-instance-battery",
        [Owner.Domino] = "9a-32-generic-instance-domino", [Owner.Plate] = "9a-c-plate-contact-load-conversion",
        [Owner.Counter] = "9a-c-memory-counter-latch-conversion", [Owner.Clock] = "9a-c-clock-conversion",
        [Owner.Latch] = "9a-c-memory-counter-latch-conversion"
    };

    private static readonly string[] Ids = TestBundle.Entries.Select(entry => entry.Metadata.Id).Order(StringComparer.Ordinal).ToArray();
    private static readonly Dictionary<string, string> Aliases = PackageAliases();
    private static string Pascal(string value) => string.Concat(Regex.Matches(value, "[A-Za-z0-9]+")
        .Select(match => char.ToUpperInvariant(match.Value[0]) + match.Value[1..]));
    private static Dictionary<string, string> PackageAliases()
    {
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in TestBundle.Entries)
        {
            aliases[Pascal(entry.Metadata.Id)] = entry.Metadata.Id;
            aliases[entry.Metadata.ScenePath.Split('/')[^2]] = entry.Metadata.Id;
        }
        // Named members are existing canonical aliases, not an independently maintained catalogue.
        var syntax = Parse(File.ReadAllText(Path.Combine(Root, "engine/PartId.cs")));
        foreach (var variable in syntax.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            var literal = variable.Initializer?.DescendantNodes().OfType<LiteralExpressionSyntax>()
                .FirstOrDefault(node => node.IsKind(SyntaxKind.StringLiteralExpression))?.Token.ValueText;
            if (literal is not null && Ids.Contains(literal, StringComparer.Ordinal)) aliases[variable.Identifier.ValueText] = literal;
        }
        return aliases;
    }

    private static SyntaxNode Parse(string source) =>
        CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken).GetRoot(TestContext.Current.CancellationToken);
    private static readonly Regex IdentifierWords = new("[A-Z]?[a-z]+|[A-Z]+(?=[A-Z][a-z]|$)|[0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly (string[] Words, string Id)[] PackageWords = Aliases.Select(pair => (Words(pair.Key), pair.Value))
        .OrderByDescending(pair => pair.Item1.Length).ThenByDescending(pair => pair.Item1.Sum(word => word.Length)).ToArray();
    private static string[] Words(string identifier) => IdentifierWords.Matches(identifier).Select(match => match.Value.ToLowerInvariant()).ToArray();
    private static string? PackageIdentifier(string identifier)
    {
        var words = Words(identifier);
        foreach (var package in PackageWords)
            for (var start = 0; start + package.Words.Length <= words.Length; start++)
                if (words.AsSpan(start, package.Words.Length).SequenceEqual(package.Words)) return package.Id;
        return null;
    }

    private static string Symbol(SyntaxNode node)
    {
        var members = node.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().Reverse().Select(member => member switch
        {
            BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            FieldDeclarationSyntax field => string.Join(",", field.Declaration.Variables.Select(variable => variable.Identifier.ValueText)),
            _ => ""
        }).Where(value => value.Length > 0);
        return string.Join(".", members);
    }

    private static List<Hit> ScanCSharp(string file, string source)
    {
        var hits = new List<Hit>();
        var syntax = Parse(source);
        void Add(SyntaxToken token, Rule rule, string value) =>
            hits.Add(new(file, Symbol(token.Parent!), rule, value, token.GetLocation().GetLineSpan().StartLinePosition.Line + 1));
        foreach (var token in syntax.DescendantTokens())
        {
            if (token.IsKind(SyntaxKind.StringLiteralToken) || token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken) ||
                token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken) || token.IsKind(SyntaxKind.InterpolatedStringTextToken))
            {
                if (Ids.Contains(token.ValueText, StringComparer.Ordinal)) Add(token, Rule.ElementLiteral, token.ValueText);
            }
            if (token.IsKind(SyntaxKind.IdentifierToken) && PackageIdentifier(token.ValueText) is not null)
                Add(token, Rule.PackageIdentifier, token.ValueText);
        }
        var partAliases = syntax.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(use => use.Alias is not null && use.Name?.ToString().Split('.').Last() == "PartId")
            .Select(use => use.Alias!.Name.Identifier.ValueText).Append("PartId").ToHashSet(StringComparer.Ordinal);
        foreach (var member in syntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (Aliases.ContainsKey(member.Name.Identifier.ValueText) && (partAliases.Contains(member.Expression.ToString()) || member.Expression.ToString().EndsWith(".PartId", StringComparison.Ordinal)))
                Add(member.Name.Identifier, Rule.NamedPart, member.Name.Identifier.ValueText);
            else if (member.Expression.ToString() == "CosmeticCurves" && Aliases.ContainsKey(member.Name.Identifier.ValueText))
                Add(member.Name.Identifier, Rule.PackageIdentifier, "CosmeticCurves." + member.Name.Identifier.ValueText);
        }
        // Declaration/type patterns, including 'is PartId', switch type arms and OfType<PartId>.
        foreach (var name in syntax.DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => partAliases.Contains(name.Identifier.ValueText)))
            if (name.Ancestors().TakeWhile(parent => parent is not StatementSyntax).Any(parent =>
                parent is DeclarationPatternSyntax or TypePatternSyntax || parent is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.IsExpression) || parent is TypeArgumentListSyntax arguments &&
                arguments.Parent is GenericNameSyntax generic && generic.Identifier.ValueText == "OfType"))
                Add(name.Identifier, Rule.NamedPart, "<type-pattern>");
        return hits;
    }

    private static string Unescape(string value) => Regex.Replace(value, @"\\(?:u\{([0-9a-fA-F]+)\}|u([0-9a-fA-F]{4})|x([0-9a-fA-F]{2})|(.))", match =>
    {
        var hex = match.Groups.Cast<Group>().Skip(1).Take(3).FirstOrDefault(group => group.Success)?.Value;
        return hex is not null ? char.ConvertFromUtf32(int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture)) :
            match.Groups[4].Value switch { "n" => "\n", "r" => "\r", "t" => "\t", var other => other };
    });

    private static List<Lexeme> LexJavaScript(string source)
    {
        var result = new List<Lexeme>();
        var i = 0;
        void Code(bool interpolation)
        {
            var depth = 0;
            while (i < source.Length)
            {
                var start = i;
                var c = source[i++];
                if (interpolation && c == '}' && depth-- == 0) return;
                if (c == '{') depth++;
                if (c == '/' && i < source.Length && source[i] == '/')
                { while (i < source.Length && source[i] != '\n') i++; continue; }
                if (c == '/' && i < source.Length && source[i] == '*')
                { i++; while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/')) i++; i = Math.Min(source.Length, i + 2); continue; }
                if (c is '\'' or '"' or '`')
                {
                    var text = new StringBuilder();
                    while (i < source.Length)
                    {
                        var next = source[i++];
                        if (next == '\\' && i < source.Length) { text.Append(next).Append(source[i++]); continue; }
                        if (next == c) break;
                        if (c == '`' && next == '$' && i < source.Length && source[i] == '{')
                        {
                            result.Add(new(Unescape(text.ToString()), start, true)); text.Clear(); i++; Code(true); start = i; continue;
                        }
                        text.Append(next);
                    }
                    result.Add(new(Unescape(text.ToString()), start, true)); continue;
                }
                if (char.IsLetter(c) || c is '_' or '$')
                {
                    while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] is '_' or '$')) i++;
                    result.Add(new(source[start..i], start, false));
                }
                else if (c == '.') result.Add(new(".", start, false));
            }
        }
        Code(false);
        return result;
    }

    private static List<Hit> ScanJavaScript(string file, string source)
    {
        var hits = new List<Hit>();
        var tokens = LexJavaScript(source);
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var line = source.AsSpan(0, token.Start).Count('\n') + 1;
            Rule? rule = token.Literal && Ids.Contains(token.Text, StringComparer.Ordinal) ? Rule.ElementLiteral :
                !token.Literal && token.Text != "switch" && PackageIdentifier(token.Text) is not null ? Rule.PackageIdentifier : null;
            if (!token.Literal && i >= 2 && tokens[i - 2].Text == "PartId" && tokens[i - 1].Text == ".") rule = Rule.NamedPart;
            if (rule is not null) hits.Add(new(file, "<module>", rule.Value, token.Text, line));
        }
        return hits;
    }

    private static bool Included(string file)
    {
        var parts = file.Replace('\\', '/').Split('/');
        if (parts.Any(part => Outputs.Contains(part, StringComparer.Ordinal))) return false;
        if (!(file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".js", StringComparison.Ordinal) ||
              file.EndsWith(".ts", StringComparison.Ordinal))) return false;
        return parts[0] is "engine" or "ui" || parts[0].StartsWith("CuriousContraptions.", StringComparison.Ordinal) &&
            parts[0] != "CuriousContraptions.tests";
    }

    private static IEnumerable<string> Sources(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var relative = Path.GetRelativePath(Root, file).Replace('\\', '/');
            if (Included(relative)) yield return relative;
        }
        foreach (var child in Directory.EnumerateDirectories(directory))
            if (!Outputs.Contains(Path.GetFileName(child), StringComparer.Ordinal) && Path.GetFileName(child) != "node_modules")
                foreach (var file in Sources(child)) yield return file;
    }
    private static List<Hit> ScanTree() => new[] { Path.Combine(Root, "engine"), Path.Combine(Root, "ui") }
        .Concat(Directory.EnumerateDirectories(Root, "CuriousContraptions.*")
            .Where(path => Path.GetFileName(path) != "CuriousContraptions.tests"))
        .SelectMany(Sources).Order(StringComparer.Ordinal)
        .SelectMany(file => file.EndsWith(".cs", StringComparison.Ordinal) ? ScanCSharp(file, File.ReadAllText(Path.Combine(Root, file))) :
            ScanJavaScript(file, File.ReadAllText(Path.Combine(Root, file)))).ToList();
    private static string Key(Entry entry) => string.Join('|', entry.File, entry.Symbol, entry.Rule, entry.Token);
    private static string Key(Hit hit) => string.Join('|', hit.File, hit.Symbol, hit.Rule, hit.Token);
    private static string Canonical(IEnumerable<Entry> entries) => JsonSerializer.Serialize(entries.OrderBy(Key, StringComparer.Ordinal), Json);
    private static string SemanticKey(SemanticContext entry) => string.Join('|', entry.File, entry.Symbol, entry.Token);
    private static IsolationSnapshot ReadSnapshot(string source)
    {
        var snapshot = JsonSerializer.Deserialize<IsolationSnapshot>(source, Json);
        if (snapshot?.Known is null || snapshot.Generic is null) throw new InvalidDataException("Missing isolation list or semantic contexts.");
        return snapshot;
    }
    private static string CanonicalSnapshot(IsolationSnapshot snapshot) => JsonSerializer.Serialize(new IsolationSnapshot(
        snapshot.Known.OrderBy(Key, StringComparer.Ordinal).ToArray(), snapshot.Generic.OrderBy(SemanticKey, StringComparer.Ordinal).ToArray()), Json);
    private static List<string> ClassificationErrors(IReadOnlyList<SemanticContext> generic, IReadOnlyList<Entry> known)
    {
        var contexts = generic.Select(SemanticKey).ToHashSet(StringComparer.Ordinal);
        return known.Where(entry => entry.Rule == Rule.PackageIdentifier && contexts.Contains(string.Join('|', entry.File, entry.Symbol, entry.Token)))
            .Select(entry => "Known violation cannot be reclassified as generic: " + Key(entry)).ToList();
    }
    private static List<Hit> RemoveSemanticContexts(IReadOnlyList<Hit> hits, IReadOnlyList<SemanticContext> exceptions, List<string> errors)
    {
        foreach (var duplicate in exceptions.GroupBy(SemanticKey).Where(group => group.Count() != 1)) errors.Add("Duplicate semantic context: " + duplicate.Key);
        foreach (var entry in exceptions)
        {
            var count = hits.Count(hit => hit.Rule == Rule.PackageIdentifier && hit.File == entry.File && hit.Symbol == entry.Symbol && hit.Token == entry.Token);
            if (count != entry.Count || count <= 0 || !Enum.IsDefined(entry.Meaning))
                errors.Add($"{entry.File}:1: stale/count/meaning mismatch for generic {entry.Symbol}/{entry.Token}: actual {count}, pinned {entry.Count}");
        }
        var keys = exceptions.Select(SemanticKey).ToHashSet(StringComparer.Ordinal);
        return hits.Where(hit => hit.Rule != Rule.PackageIdentifier || !keys.Contains(string.Join('|', hit.File, hit.Symbol, hit.Token))).ToList();
    }
    private static string Hash(string source) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source)));

    private static List<string> ShrinkErrors(IReadOnlyList<Entry> current, IReadOnlyList<Entry> prior)
    {
        var errors = new List<string>();
        var previous = prior.ToDictionary(Key, StringComparer.Ordinal);
        foreach (var entry in current)
            if (!previous.TryGetValue(Key(entry), out var before)) errors.Add("Added entry: " + Key(entry));
            else if (entry.Count > before.Count || entry.Owner != before.Owner) errors.Add("Widened/reassigned entry: " + Key(entry));
        return errors;
    }

    private static List<string> AdmissionErrors(IReadOnlyList<Hit> hits, IReadOnlyList<Entry> entries, string status)
    {
        var errors = new List<string>();
        var groups = hits.GroupBy(Key).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        foreach (var duplicate in entries.GroupBy(Key).Where(group => group.Count() != 1)) errors.Add("Duplicate entry: " + duplicate.Key);
        foreach (var entry in entries)
        {
            var actual = groups.GetValueOrDefault(Key(entry)) ?? [];
            var where = actual.Length > 0 ? $"{entry.File}:{actual[0].Line}" : entry.File + ":1";
            if (actual.Length != entry.Count || entry.Count <= 0)
                errors.Add($"{where}: stale/count mismatch {entry.Symbol}/{entry.Token}: actual {actual.Length}, pinned {entry.Count}");
            if (!Enum.IsDefined(entry.Rule) || !SprintKeys.TryGetValue(entry.Owner, out var key))
            { errors.Add(where + ": undefined rule/owner"); continue; }
            var state = Regex.Match(status, @"(?m)^\s*" + Regex.Escape(key) + @":\s*(\S+)");
            if (!state.Success || state.Groups[1].Value == "done") errors.Add($"{where}: missing/done owner {key}");
        }
        var admitted = entries.Select(Key).ToHashSet(StringComparer.Ordinal);
        foreach (var hit in hits.Where(hit => !admitted.Contains(Key(hit))))
            errors.Add($"{hit.File}:{hit.Line}: unowned {hit.Rule} {hit.Token} in {hit.Symbol}");
        return errors;
    }

    private static (int Code, string Text) Git(params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = Root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot read isolation baseline from git.");
        var output = process.StandardOutput.ReadToEnd(); var error = process.StandardError.ReadToEnd(); process.WaitForExit();
        return (process.ExitCode, process.ExitCode == 0 ? output : error);
    }
    private static string? SelectPrior(string current, string? head, string? firstParent) =>
        head is not null && head != current ? head : firstParent;
    private static string? Prior(string path, string current, Func<string, string> canonical)
    {
        var head = Git("show", "HEAD:" + path);
        if (head.Code == 0 && SelectPrior(canonical(current), canonical(head.Text), null) is not null) return head.Text;
        var parent = Git("rev-parse", "--verify", "HEAD^");
        Assert.True(parent.Code == 0, "Isolation ratchet requires HEAD and its first parent; use checkout fetch-depth: 2. " + parent.Text);
        var previous = Git("show", "HEAD^:" + path);
        if (previous.Code == 0) return previous.Text;
        var tree = Git("ls-tree", "--name-only", "HEAD^", "--", path);
        Assert.True(tree.Code == 0 && tree.Text.Length == 0, "Cannot read prior isolation list: " + previous.Text);
        return null;
    }

    [Fact]
    public void ProductionKnowledgeIsExactlyTheOwnedShrinkingList()
    {
        var hits = ScanTree();
        Assert.True(File.Exists(Path.Combine(Root, ListPath)), "Missing reviewed isolation list: " + ListPath);
        var text = File.ReadAllText(Path.Combine(Root, ListPath));
        var snapshot = ReadSnapshot(text);
        var errors = new List<string>();
        var specific = RemoveSemanticContexts(hits, snapshot.Generic, errors);
        var status = File.ReadAllText(Path.Combine(Root, "_bmad-output/implementation-artifacts/sprint-status.yaml"));
        errors.AddRange(AdmissionErrors(specific, snapshot.Known, status));
        errors.AddRange(ClassificationErrors(snapshot.Generic, snapshot.Known));
        var previous = Prior(ListPath, text, value => CanonicalSnapshot(ReadSnapshot(value)));
        if (previous is null) Assert.Equal(InitialCensusSha256, Hash(CanonicalSnapshot(snapshot)));
        else
        {
            var before = ReadSnapshot(previous);
            errors.AddRange(ShrinkErrors(snapshot.Known, before.Known));
            errors.AddRange(ClassificationErrors(snapshot.Generic, before.Known));
        }
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public void SolverPinsCannotBeAddedReassignedOrWidened()
    {
        static Entry[] Pins(string source)
        {
            var field = Parse(source).DescendantNodes().OfType<FieldDeclarationSyntax>()
                .Single(node => node.Declaration.Variables.Any(variable => variable.Identifier.ValueText == "KnownViolations"));
            return field.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Select(node =>
            {
                var args = node.ArgumentList.Arguments;
                string Literal(int index) => ((LiteralExpressionSyntax)args[index].Expression).Token.ValueText;
                Assert.Equal("ViolationOwner.Story9A19", args[3].Expression.ToString());
                return new Entry(Literal(0), "PrismaticConstraintSolver", Rule.SolverPin, Literal(1),
                    int.Parse(Literal(2), CultureInfo.InvariantCulture), Owner.GateDrive);
            }).ToArray();
        }
        var text = File.ReadAllText(Path.Combine(Root, SolverPath));
        var prior = Prior(SolverPath, text, value => Canonical(Pins(value)));
        Assert.NotNull(prior);
        Assert.Empty(ShrinkErrors(Pins(text), Pins(prior)));
        var status = File.ReadAllText(Path.Combine(Root, "_bmad-output/implementation-artifacts/sprint-status.yaml"));
        var family = Regex.Match(status, @"(?m)^\s*" + Regex.Escape(SprintKeys[Owner.GateDrive]) + @":\s*(\S+)");
        Assert.True(family.Success, "Missing solver family owner " + SprintKeys[Owner.GateDrive]);
        if (family.Groups[1].Value == "done") Assert.Empty(Pins(text));
    }

    [Theory]
    [InlineData("engine/planted.cs", true)] [InlineData("ui/planted.cs", true)]
    [InlineData("CuriousContraptions.web/wwwroot/planted.js", true)]
    [InlineData("CuriousContraptions.Simulation/wwwroot/planted.js", true)]
    [InlineData("CuriousContraptions.Animation.Worker/wwwroot/planted.js", true)]
    [InlineData("elements/Wall/planted.cs", false)] [InlineData("content/planted.cs", false)]
    [InlineData("CuriousContraptions.tests/planted.cs", false)] [InlineData("tools/e2e/planted.ts", false)]
    [InlineData("tools/Coverage/planted.cs", false)] [InlineData("docs/planted.cs", false)]
    [InlineData("_bmad-output/planted.cs", false)] [InlineData("archives/planted.cs", false)]
    [InlineData("CuriousContraptions.web/obj/planted.cs", false)]
    public void ScopeIsExplicit(string path, bool included) => Assert.Equal(included, Included(path));

    [Fact]
    public void PlantedLiteralsRecordsAndRealPanelHelpersAreUnowned()
    {
        var source = "class Probe { void Test(object x) { var id = \"wall\"; if (x is WorkshopBattery) {} x.OfType<WorkshopClock>(); } }";
        var hits = ScanCSharp("engine/planted.cs", source);
        Assert.Contains(hits, hit => hit.Rule == Rule.ElementLiteral && hit.Token == "wall");
        Assert.Contains(hits, hit => hit.Rule == Rule.PackageIdentifier && hit.Token == "WorkshopBattery");
        Assert.Contains(hits, hit => hit.Token == "WorkshopClock");
        Assert.All(AdmissionErrors(hits, [], ""), error => Assert.Contains("engine/planted.cs:1:", error, StringComparison.Ordinal));
        foreach (var name in new[] { "AddBatteryConfiguration", "ApplyClockInterval", "CaptureBall", "AdvanceBattery", "BatterySimulation", "ResolveWallMaterial", "TickClock" }) Assert.NotNull(PackageIdentifier(name));
        var qualified = ScanCSharp("engine/qualified.cs", "using P = CuriousContraptions.PartId; class Probe { void Test() { var a = CuriousContraptions.PartId.Battery; var b = global::CuriousContraptions.PartId.Battery; var c = P.Battery; } }");
        Assert.Equal(3, qualified.Count(hit => hit.Rule == Rule.NamedPart && hit.Token == "Battery"));
        Assert.Empty(ScanCSharp("engine/generic.cs", "class Probe { void Test(dynamic instance) { var x = instance.PartId.Value; instance.PartId.Equals(x); instance.PartId.ToString(); } }"));
        Assert.Equal(2, ScanCSharp("engine/alias.cs", "using P = CuriousContraptions.PartId; class Probe { void Test(object x) { if (x is P) {} x.OfType<P>(); } }").Count(hit => hit.Token == "<type-pattern>"));

        Assert.Empty(ScanCSharp("engine/control.cs", "// \"wall\" WorkshopBattery\nclass Control { string s = \"not-an-element\"; }"));
    }

    [Fact]
    public void JavaScriptEscapesAndTemplateExpressionsAreScanned()
    {
        var source = "// 'wall'\nconst a = '\\x77all'; const b = `\\u0077all`; const c = `${PartId.Battery} ${AddBatteryConfiguration()}`;";
        var hits = ScanJavaScript("CuriousContraptions.web/wwwroot/planted.js", source);
        Assert.Equal(2, hits.Count(hit => hit.Token == "wall"));
        Assert.Contains(hits, hit => hit.Rule == Rule.NamedPart && hit.Token == "Battery");
        Assert.Contains(hits, hit => hit.Token == "AddBatteryConfiguration");
        Assert.All(hits, hit => Assert.Equal(2, hit.Line));
    }

    [Fact]
    public void SemanticContextsCannotHideNewNamesOrOtherRules()
    {
        const string file = "engine/presentation/AnimationBatch.cs";
        var source = "class AnimationBatch { void Evaluate() { var clockTime = 1; TickClock(); AdvanceBattery(); var id = \"clock\"; var part = PartId.Clock; } }";
        var hits = ScanCSharp(file, source);
        var generic = new SemanticContext(file, "AnimationBatch.Evaluate", "clockTime", 1, GenericMeaning.ClockProtocol);
        var errors = new List<string>();
        var remaining = RemoveSemanticContexts(hits, [generic], errors);
        Assert.Empty(errors);
        Assert.DoesNotContain(remaining, hit => hit.Token == "clockTime");
        Assert.Contains(remaining, hit => hit.Token == "TickClock");
        Assert.Contains(remaining, hit => hit.Token == "AdvanceBattery");
        Assert.Contains(remaining, hit => hit.Rule == Rule.ElementLiteral && hit.Token == "clock");
        Assert.Contains(remaining, hit => hit.Rule == Rule.NamedPart && hit.Token == "Clock");
        Assert.NotEmpty(AdmissionErrors(remaining, [], ""));
        var oldDebt = new Entry(file, generic.Symbol, Rule.PackageIdentifier, "TickClock", 1, Owner.Clock);
        Assert.NotEmpty(ClassificationErrors([generic with { Token = "TickClock" }], [oldDebt]));
        Assert.Empty(ClassificationErrors([generic], [oldDebt])); // a reviewed new generic context is permitted
        Assert.Empty(ClassificationErrors([generic with { Count = 2 }], [])); // exact live count still enforced below
        errors.Clear(); RemoveSemanticContexts(hits, [generic with { Count = 2 }], errors); Assert.NotEmpty(errors);
        errors.Clear(); RemoveSemanticContexts([], [generic], errors); Assert.NotEmpty(errors);
    }

    [Fact]
    public void PriorSelectionDistinguishesDirtyCommittedMergeAndBootstrap()
    {
        Assert.Equal("head", SelectPrior("dirty", "head", "parent"));
        Assert.Equal("parent", SelectPrior("head", "head", "parent")); // clean commit or no-ff merge
        Assert.Null(SelectPrior("initial", null, null)); // bootstrap still requires pinned census hash
        Assert.Equal("initial", SelectPrior("initial", "initial", "initial"));
        Assert.Equal("removed", SelectPrior("readded", "removed", "older-with-entry"));
    }

    [Fact]
    public void ListControlsRejectAddReplaceWidenStaleDoneAndReaddition()
    {
        var entry = new Entry("engine/test.cs", "Probe.Test", Rule.ElementLiteral, "wall", 2, Owner.FinalIsolation);
        var hit = new Hit(entry.File, entry.Symbol, entry.Rule, entry.Token, 7);
        var open = "  " + SprintKeys[entry.Owner] + ": backlog";
        Assert.Empty(AdmissionErrors([hit, hit], [entry], open));
        Assert.Empty(ShrinkErrors([entry with { Count = 1 }], [entry]));
        Assert.NotEmpty(ShrinkErrors([entry, entry with { Token = "battery" }], [entry]));
        Assert.NotEmpty(ShrinkErrors([entry with { Token = "battery" }], [entry]));
        Assert.NotEmpty(ShrinkErrors([entry with { Count = 3 }], [entry]));
        Assert.NotEmpty(ShrinkErrors([entry with { Owner = Owner.Battery }], [entry]));
        Assert.Empty(ShrinkErrors([], [entry]));
        Assert.NotEmpty(ShrinkErrors([entry], [])); // deleted then re-added cannot spend old headroom
        Assert.NotEmpty(AdmissionErrors([], [entry], open));
        Assert.NotEmpty(AdmissionErrors([hit, hit], [entry], open.Replace("backlog", "done", StringComparison.Ordinal)));
        Assert.NotEmpty(AdmissionErrors([hit, hit], [entry], ""));
        var family = entry with { Owner = Owner.Battery };
        Assert.NotEmpty(AdmissionErrors([hit, hit], [family], "  " + SprintKeys[Owner.Battery] + ": done"));
        var clockFamily = entry with { Owner = Owner.Clock };
        Assert.NotEmpty(AdmissionErrors([hit, hit], [clockFamily], "  9a-c-clock-conversion: done\n  9a-42-generic-instance-system-clock: backlog"));
        Assert.NotEmpty(AdmissionErrors([hit, hit], [entry with { Owner = (Owner)999 }], open));
        Assert.NotEmpty(AdmissionErrors([hit, hit], [entry, entry], open));
    }
}
