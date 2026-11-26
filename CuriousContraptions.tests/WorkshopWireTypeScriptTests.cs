using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace CuriousContraptions.Tests;

/// <summary>
/// S5b: the TypeScript wire accessors the e2e suites import are build output of CuriousContraptions.FlatBuffers
/// (GenerateWireTypeScript), written into the ignored tools/e2e/.wire/ by the same pinned compiler as the C# accessors. These
/// facts read the real tree after the build: the generated module against the schemas (T1), the hand-written e2e modules (T5),
/// the pinned TypeScript runtime (T7) and the compiler-log evidence that one build graph generates once (T8). Each checker is
/// one function that the real-tree assertion and its synthetic controls both call.
/// </summary>
public sealed class WorkshopWireTypeScriptTests
{
    private const string E2eDirectory = "tools/e2e";
    private const string WireDirectory = E2eDirectory + "/.wire";
    private const string EntryModule = "curious-contraptions/wire.ts";
    private const string SchemaRecord = "schemas.txt";
    private const string HelperModule = "workshop-wire.ts";
    private const string VendoredProject = "CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj";
    private const string GenerationTarget = "GenerateWireTypeScript";

    private static readonly string Root = twodog.Engine.ResolveProjectDir();

    // ----- T1: the generated module exports every schema type, one per file, with no `any` ------------------------------

    [Fact]
    public void TheGeneratedModuleExportsEverySchemaTypeWithoutAny()
    {
        var files = ReadGeneratedTypeScript(Path.Combine(Root, WireDirectory));
        var schemas = Path.Combine(Root, "schemas");
        var schemaTypes = SchemaTypes(schemas);
        Assert.Contains("SavedConstruction", schemaTypes);
        Assert.Contains("Vec3f", schemaTypes);
        Assert.Empty(GeneratedTypeScriptViolations(files, schemas));
        // AC1: the real checkout ignores the generated folder and tracks nothing under it; control: the ignore rules do not cover a
        // hand-written module (--no-index applies the rules to a tracked path too).
        Assert.Equal(0, Git("check-ignore", "-q", "tools/e2e/.wire/curious-contraptions/wire.ts").ExitCode);
        Assert.Equal(1, Git("check-ignore", "-q", "--no-index", "tools/e2e/workshop-wire.ts").ExitCode);
        Assert.Equal("", Git("ls-files", "--", "tools/e2e/.wire").Output);

        // Control: a schema type missing from the module is named.
        var missing = new Dictionary<string, string>(files, StringComparer.Ordinal) { [EntryModule] = files[EntryModule].Replace("export { Vec3f } from './wire/vec3f.js';\n", "", StringComparison.Ordinal) };
        missing.Remove("curious-contraptions/wire/vec3f.ts");
        Assert.Contains("the entry module exports no Vec3f", GeneratedTypeScriptViolations(missing, schemas));
        // Boundary: a struct's file with a corrupted declaration, one with an unbalanced brace, and an `any` annotation.
        var corrupted = Replaced(files, "curious-contraptions/wire/vec3f.ts", "export class Vec3f {", "export class Vec3 {");
        Assert.Contains("curious-contraptions/wire/vec3f.ts declares [Vec3], not exactly Vec3f", GeneratedTypeScriptViolations(corrupted, schemas));
        var unbalanced = Replaced(files, "curious-contraptions/wire/vec3f.ts", "export class Vec3f {", "export class Vec3f {{");
        Assert.Contains("curious-contraptions/wire/vec3f.ts has unbalanced braces (+1)", GeneratedTypeScriptViolations(unbalanced, schemas));
        var annotated = Replaced(files, "curious-contraptions/wire/puzzle-record.ts", "optionalEncoding?:flatbuffers.Encoding", "optionalEncoding?:any");
        Assert.Contains("curious-contraptions/wire/puzzle-record.ts has an any annotation or its suppression", GeneratedTypeScriptViolations(annotated, schemas));
        var suppressed = Replaced(files, EntryModule, "@typescript-eslint/no-unused-vars", "@typescript-eslint/no-unused-vars, @typescript-eslint/no-explicit-any");
        Assert.Contains($"{EntryModule} has an any annotation or its suppression", GeneratedTypeScriptViolations(suppressed, schemas));
        var duplicated = Replaced(files, EntryModule, "export { Vec3f } from './wire/vec3f.js';\n", "export { Vec3f } from './wire/vec3f.js';\nexport { Vec3f } from './wire/vec3f.js';\n");
        Assert.Contains("the entry module exports Vec3f more than once", GeneratedTypeScriptViolations(duplicated, schemas));
        // Boundary: a schema record that lists a schema no longer in schemas/, and a missing record, are named.
        var renamed = Replaced(files, SchemaRecord, "workshop_wire.fbs", "workshop_renamed.fbs");
        var stale = Assert.Single(GeneratedTypeScriptViolations(renamed, schemas), violation => violation.StartsWith("schemas.txt records [", StringComparison.Ordinal));
        Assert.Contains("], but schemas/ holds [", stale, StringComparison.Ordinal);
        var sides = stale.Split("], but schemas/ holds [");
        Assert.Contains("workshop_renamed.fbs", sides[0], StringComparison.Ordinal);
        Assert.Contains("workshop_wire.fbs", sides[1], StringComparison.Ordinal);
        Assert.DoesNotContain("workshop_renamed.fbs", sides[1], StringComparison.Ordinal);
        var unrecorded = new Dictionary<string, string>(files, StringComparer.Ordinal);
        unrecorded.Remove(SchemaRecord);
        Assert.Contains("the schema record schemas.txt is missing", GeneratedTypeScriptViolations(unrecorded, schemas));
    }

    private static (int ExitCode, string Output) Git(params string[] arguments)
    {
        var start = new System.Diagnostics.ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add("-C");
        start.ArgumentList.Add(Root);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException("git did not start.");
        var errors = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Assert.True(errors.Result.Length == 0, $"git {string.Join(' ', arguments)} wrote to stderr: {errors.Result}");
        return (process.ExitCode, output);
    }

    private static Dictionary<string, string> Replaced(Dictionary<string, string> files, string path, string find, string replacement)
    {
        Assert.Contains(find, files[path], StringComparison.Ordinal);
        return new(files, StringComparer.Ordinal) { [path] = files[path].Replace(find, replacement, StringComparison.Ordinal) };
    }

    /// <summary>Every generated file under the wire directory, by its path relative to it (forward slashes).</summary>
    internal static Dictionary<string, string> ReadGeneratedTypeScript(string wireDirectory) =>
        Directory.GetFiles(wireDirectory, "*", SearchOption.AllDirectories)
            .ToDictionary(path => Path.GetRelativePath(wireDirectory, path).Replace('\\', '/'), File.ReadAllText, StringComparer.Ordinal);

    /// <summary>The enum, struct, table and union names the schema files in a directory declare.</summary>
    internal static HashSet<string> SchemaTypes(string schemaDirectory) => CSharpStaticGateTests
        .SchemaTypesIn(Directory.GetFiles(schemaDirectory, "*.fbs").Select(File.ReadAllText)).Keys.ToHashSet(StringComparer.Ordinal);

    /// <summary>The names the generated entry module re-exports, in the one export form T1 admits.</summary>
    internal static string[] EntryExports(string entry) => [.. EntryExport.Matches(entry).Select(match => match.Groups[1].Value)];

    // The compiler's entry module re-exports one name per type file. A union would add its unionTo helpers to that line, which
    // this form refuses, so a first union in the schemas fails here and its export form is decided then.
    private static readonly Regex EntryExport = new(@"^export \{ (\w+) \} from '\./(wire/[a-z0-9-]+)\.js';$", RegexOptions.Multiline);
    private static readonly Regex TypeDeclaration = new(@"^export (?:class|enum) (\w+) \{", RegexOptions.Multiline);
    private static readonly Regex AnyAnnotation = new(@":\s*any\b|\bas\s+any\b|<any>|\bany\s*\[\]|no-explicit-any");

    /// <summary>T1: the entry module exports exactly the schema types, each from its own file declaring exactly that class or
    /// enum with balanced braces; the schema record schemas.txt lists exactly the schema file names in the directory; no other
    /// file exists; no file has an `any` annotation or the no-explicit-any suppression.</summary>
    internal static List<string> GeneratedTypeScriptViolations(IReadOnlyDictionary<string, string> files, string schemaDirectory)
    {
        var schemaTypes = SchemaTypes(schemaDirectory);
        if (!files.TryGetValue(EntryModule, out var entry)) return [$"the entry module {EntryModule} is missing"];
        var violations = new List<string>();
        var lines = EntryExport.Matches(entry).Select(match => (Name: match.Groups[1].Value, Path: $"curious-contraptions/{match.Groups[2].Value}.ts")).ToArray();
        violations.AddRange(lines.GroupBy(line => line.Name, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key)
            .Order(StringComparer.Ordinal).Select(name => $"the entry module exports {name} more than once"));
        var exports = lines.DistinctBy(line => line.Name, StringComparer.Ordinal).ToDictionary(line => line.Name, line => line.Path, StringComparer.Ordinal);
        violations.AddRange(schemaTypes.Where(type => !exports.ContainsKey(type)).Order(StringComparer.Ordinal).Select(type => $"the entry module exports no {type}"));
        violations.AddRange(exports.Keys.Where(name => !schemaTypes.Contains(name)).Order(StringComparer.Ordinal).Select(name => $"the entry module exports {name}, which no schema declares"));
        foreach (var (name, path) in exports) violations.AddRange(TypeFileViolations(files, name, path));
        violations.AddRange(SchemaRecordViolations(files, schemaDirectory));
        violations.AddRange(files.Keys.Where(path => path != EntryModule && path != SchemaRecord && !exports.ContainsValue(path)).Order(StringComparer.Ordinal).Select(path => $"{path} is not exported by the entry module"));
        violations.AddRange(files.Where(file => AnyAnnotation.IsMatch(file.Value)).Select(file => file.Key).Order(StringComparer.Ordinal).Select(path => $"{path} has an any annotation or its suppression"));
        return violations;
    }

    private static IEnumerable<string> SchemaRecordViolations(IReadOnlyDictionary<string, string> files, string schemaDirectory)
    {
        if (!files.TryGetValue(SchemaRecord, out var record))
        {
            yield return $"the schema record {SchemaRecord} is missing";
            yield break;
        }
        var recorded = record.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Order(StringComparer.Ordinal).ToArray();
        var current = Directory.GetFiles(schemaDirectory, "*.fbs").Select(path => Path.GetFileName(path)).Order(StringComparer.Ordinal).ToArray();
        if (!recorded.SequenceEqual(current, StringComparer.Ordinal))
            yield return $"{SchemaRecord} records [{string.Join(", ", recorded)}], but schemas/ holds [{string.Join(", ", current)}]";
    }

    private static IEnumerable<string> TypeFileViolations(IReadOnlyDictionary<string, string> files, string name, string path)
    {
        if (!files.TryGetValue(path, out var text))
        {
            yield return $"{path}, which the entry module names for {name}, is missing";
            yield break;
        }
        var declared = TypeDeclaration.Matches(text).Select(match => match.Groups[1].Value).ToArray();
        if (declared is not [var only] || only != name) yield return $"{path} declares [{string.Join(", ", declared)}], not exactly {name}";
        var balance = text.Count(character => character == '{') - text.Count(character => character == '}');
        if (balance != 0) yield return $"{path} has unbalanced braces ({balance:+0;-0})";
    }

    // ----- T5: every schema-type name a hand-written e2e module uses is imported from the generated entry module ----------

    [Fact]
    public void TheHelpersDeclareNoSchemaTypeAndNoImporterTakesOneFromThem()
    {
        var e2e = Path.Combine(Root, E2eDirectory);
        var sources = Directory.GetFiles(e2e, "*.ts").Concat(Directory.GetFiles(Path.Combine(e2e, "unit"), "*.ts"))
            .ToDictionary(path => Path.GetRelativePath(e2e, path).Replace('\\', '/'), File.ReadAllText, StringComparer.Ordinal);
        var schemaTypes = SchemaTypes(Path.Combine(Root, "schemas"));
        Assert.Contains(HelperModule, sources.Keys);
        Assert.Contains("workshop-driver.ts", sources.Keys);
        Assert.Contains("run-reset-snapshot.ts", sources.Keys);
        Assert.Contains("unit/run-reset-snapshot.test.ts", sources.Keys);
        Assert.Empty(HandWrittenModuleViolations(sources, schemaTypes));

        // Controls: a schema type declared in the helpers, one imported from them and a namespace import of them are refused.
        Assert.Equal([Uses(HelperModule, "Vec3f"), Declares(HelperModule, "Vec3f")], Violations(HelperModule, "export class Vec3f {\n}\n"));
        Assert.Equal([Uses("probe.test.ts", "CommandKind")],
            Violations("probe.test.ts", "import {\n    decodeWorkshopResponse, type CommandKind,\n} from './workshop-wire.ts';\n"));
        Assert.Contains("unit/probe.test.ts imports workshop-wire.ts as the namespace generated",
            Violations("unit/probe.test.ts", "import * as generated from '../workshop-wire.ts';\n"));
        // Boundary: the helper specifier forms module resolution also accepts (no extension, `.js`, double quotes) are uses too.
        Assert.Equal([Uses("probe.test.ts", "CommandKind"), Uses("probe.test.ts", "Vec3f")],
            Violations("probe.test.ts", "import { type Vec3f } from \"./workshop-wire\";\nimport { CommandKind } from './workshop-wire.js';\n"));
        // Boundary: an import of the generated module by any specifier but its exact one for the module's directory covers nothing.
        Assert.Equal([Uses("probe.test.ts", "CommandKind"), Uses("probe.test.ts", "Vec3f")],
            Violations("probe.test.ts", "import { type Vec3f } from \"./.wire/curious-contraptions/wire.js\";\nimport { CommandKind } from '../.wire/curious-contraptions/wire.ts';\n"));
        // Boundary: a hand-written copy of a schema type in any module, in each declaration form (a value table and its union
        // type, a const enum, a default class), is refused by name.
        var copies = Violations("driver.ts", "const CommandOutcome = { Applied: 0 } as const;\n    type SimulationPhase = 0 | 1;\n" +
            "export const enum ElectricalEnable { Disabled }\nexport default class Vec3i {}\n");
        Assert.Equal([Uses("driver.ts", "CommandOutcome"), Uses("driver.ts", "ElectricalEnable"), Uses("driver.ts", "SimulationPhase"), Uses("driver.ts", "Vec3i"),
            Declares("driver.ts", "CommandOutcome"), Declares("driver.ts", "ElectricalEnable"), Declares("driver.ts", "SimulationPhase"), Declares("driver.ts", "Vec3i")], copies);
        // Boundary: a declaration keyword followed by a schema-type name is refused anywhere in the file, even when the module
        // imports that name (a function-scope shadow, an interface).
        Assert.Equal([Declares("shadow.ts", "CommandKind"), Declares("shadow.ts", "Vec3f")], Violations("shadow.ts",
            "import { Vec3f, CommandKind } from './.wire/curious-contraptions/wire.ts';\nfunction f() {\n    const Vec3f = 1;\n    return Vec3f;\n}\ninterface CommandKind { x: number }\n"));
        // Control: a name imported from the generated entry module may be used anywhere (an alias covers its original name), and
        // a property key, a member access, a string, a regular expression, a template literal's text and a comment naming a
        // schema type are no use of it.
        Assert.Empty(Violations("snapshot.ts", "import {\n    type Vec3f, CommandKind as Kind,\n} from './.wire/curious-contraptions/wire.ts';\n" +
            "export const SCHEMA = { types: { Vec3f: { kind: 'struct' } }, enums: { SimulationPhase: 'uint32', CommandKind: 'uint32' } };\n" +
            "const v: Vec3f = read(wire.SimulationPhase, generated?.Vec3i, 'CommandOutcome', \"ElectricalEnable\", /Vec3i'/);\n" +
            "const t = `CommandOutcome ${Kind.Applied} Vec3i`; // SimulationPhase\n/* ElectricalEnable */\n"));
        // Boundary: import text inside a string literal is no import, so its names cover nothing.
        Assert.Equal([Uses("quoted.ts", "Vec3f")], Violations("quoted.ts",
            "const s = \"import { Vec3f } from './.wire/curious-contraptions/wire.ts'\";\nconst v: Vec3f = x;\n"));
        // Boundary: a '/' after an operand or a postfix '++' or '--' is division, not a regular expression hiding the rest of the line.
        Assert.Equal([Uses("divided.ts", "CommandKind")], Violations("divided.ts", "const r = total / count; const k: CommandKind = 0;\n"));
        Assert.Equal([Uses("postfix.ts", "CommandKind")], Violations("postfix.ts", "i++ / n; const k: CommandKind = 0;\n"));
        Assert.Equal([Uses("decrement.ts", "CommandKind")], Violations("decrement.ts", "i-- / n; const k: CommandKind = 0;\n"));
        // Boundary: from unit/ the generated module is ../.wire/..., and a template literal's ${ } expression is code.
        Assert.Equal([Uses("unit/snapshot.test.ts", "Vec3f")], Violations("unit/snapshot.test.ts",
            "import { CommandKind } from '../.wire/curious-contraptions/wire.ts';\nconst k: CommandKind = 0;\nconst t = `${Vec3f.name}`;\n"));
        // Boundary: an import commented out covers nothing.
        Assert.Equal([Uses("commented.ts", "SimulationPhase"), Declares("commented.ts", "SimulationPhase")], Violations("commented.ts",
            "// import { SimulationPhase } from './.wire/curious-contraptions/wire.ts';\nconst SimulationPhase = { Paused: 6 }\nimport { x } from './y.ts';\n"));
        // Boundary: a destructured binding, renamed, nested, array, defaulted, a rest element or behind a string-literal or
        // computed key, and an `import X =` alias, each bind a schema-type name the module did not import.
        Assert.Equal([Uses("destructured.ts", "CommandKind"), Uses("destructured.ts", "SimulationPhase")],
            Violations("destructured.ts", "const { CommandKind } = generated;\nconst { x: SimulationPhase, y = 1 } = other;\n"));
        Assert.Equal([Uses("aliased.ts", "Vec3f")], Violations("aliased.ts", "import Vec3f = Wire.Vec3f;\n"));
        Assert.Equal([Uses("nested.ts", "CommandKind"), Uses("nested.ts", "ElectricalEnable"), Uses("nested.ts", "Vec3f")],
            Violations("nested.ts", "const { a: { Vec3f } } = x;\nconst [CommandKind] = y;\nconst { b = {}, ElectricalEnable } = z;\n"));
        Assert.Equal([Uses("patterns.ts", "CommandKind"), Uses("patterns.ts", "ElectricalEnable"), Uses("patterns.ts", "SimulationPhase"), Uses("patterns.ts", "Vec3f")],
            Violations("patterns.ts", "const { a, ...CommandKind } = x;\nconst [b, ...Vec3f] = y;\nconst { 'k': SimulationPhase, [\"j\"]: ElectricalEnable } = z;\n"));
        // Boundary: a re-export through the helpers or of an imported name, the generated module re-exported whole, and the old
        // flatc-output marker.
        Assert.Contains("workshop-wire.ts re-exports the schema type Vec3f", Violations(HelperModule, "export { Vec3f } from './.wire/curious-contraptions/wire.ts';\n"));
        Assert.Equal(["exporter.ts re-exports the schema type Vec3f"],
            Violations("exporter.ts", "import { Vec3f } from './.wire/curious-contraptions/wire.ts';\nexport { Vec3f };\n"));
        Assert.Contains("alias.ts re-exports the generated module", Violations("alias.ts", "export * from './.wire/curious-contraptions/wire.ts';\n"));
        Assert.Contains("workshop-wire.ts carries the flatc-output marker",
            Violations(HelperModule, "// ---- hand-written helpers (everything above is flatc output; see schemas/README.md)\n"));

        List<string> Violations(string path, string text) => HandWrittenModuleViolations(new Dictionary<string, string> { [path] = text }, schemaTypes);
    }

    private static string Uses(string path, string name) => $"{path} uses the schema type {name} without importing it from the generated entry module";
    private static string Declares(string path, string name) => $"{path} declares the schema type {name}";

    // A named import list and its specifier; the list may span lines.
    private static readonly Regex NamedImport = new(@"\bimport\s+(?:type\s+)?\{(?<names>[^}]*)\}\s*from\s*['""](?<specifier>[^'""]*)['""]");
    // `.X` and `?.X` member access (not a spread or rest element's `...X`), and an `X:` or `X?:` object key or annotated name.
    private static readonly Regex MemberAccess = new(@"(?<!\.)\.(?!\.)\s*[A-Za-z_$][\w$]*");
    private static readonly Regex PropertyKey = new(@"(?<=(?:^|[{,])\s*)[A-Za-z_$][\w$]*\s*\??\s*:(?!:)", RegexOptions.Multiline);
    private static readonly Regex Identifier = new(@"(?<![\w$])[A-Za-z_$][\w$]*(?![\w$])");
    // A declaration keyword immediately followed by the name it declares.
    private static readonly Regex DeclarationKeyword = new(@"(?<![\w$.])(?:const\s+enum|const|let|var|function|class|enum|type|interface|namespace)\s+([A-Za-z_$][\w$]*)(?![\w$])");
    private static readonly Regex ExportList = new(@"\bexport\s+(?:type\s+)?\{([^}]*)\}");
    private static readonly Regex HelperNamespaceImport = new(@"\bimport\s+(?:type\s+)?\*\s*as\s+(\w+)\s+from\s*['""](?:\.\.?/)+workshop-wire(?:\.ts|\.js)?['""]");
    private static readonly Regex GeneratedReExport = new(@"\bexport\s+(?:type\s+)?(?:\*|\{[^}]*\})(?:\s+as\s+\w+)?\s*from\s*['""][^'""]*\.wire/");

    /// <summary>T5, one rule: every whole-word use of a schema-type name in a hand-written module is covered by a named import
    /// of that name from the generated entry module by its exact specifier (./.wire/curious-contraptions/wire.ts, or
    /// ../.wire/curious-contraptions/wire.ts from unit/), after comments, the text of string, template and regular-expression
    /// literals, `.X` member access and `X:` keys are removed; so a declaration, a destructured binding of any shape, an alias
    /// and an import from workshop-wire.ts are each an uncovered use. Beside it: a declaration keyword followed by a schema-type
    /// name is refused anywhere, imported or not (a destructured binding that shadows an imported name is a defined limit); no module re-exports a schema type or the
    /// generated module or imports workshop-wire.ts as a namespace, and workshop-wire.ts carries no flatc-output marker.</summary>
    internal static List<string> HandWrittenModuleViolations(IReadOnlyDictionary<string, string> sources, IReadOnlyCollection<string> schemaTypes)
    {
        var violations = new List<string>();
        foreach (var (path, text) in sources.OrderBy(source => source.Key, StringComparer.Ordinal))
        {
            var (code, masked) = Lex(text);
            var entry = path.StartsWith("unit/", StringComparison.Ordinal) ? "../.wire/curious-contraptions/wire.ts" : "./.wire/curious-contraptions/wire.ts";
            var imported = new HashSet<string>(StringComparer.Ordinal);
            var uses = masked.ToCharArray();
            // An import counts only where its keyword is code, not text inside a string or template literal.
            var imports = NamedImport.Matches(code).Where(import => masked[import.Index] == 'i').ToArray();
            foreach (var import in imports)
            {
                if (import.Groups["specifier"].Value != entry) continue;
                imported.UnionWith(ListedNames(import.Groups["names"].Value));
                Blank(uses, import.Index, import.Index + import.Length);
            }
            var bare = PropertyKey.Replace(MemberAccess.Replace(new string(uses), " "), " ");
            violations.AddRange(Identifier.Matches(bare).Select(match => match.Value).Where(name => schemaTypes.Contains(name) && !imported.Contains(name))
                .Distinct().Order(StringComparer.Ordinal).Select(name => Uses(path, name)));
            var declarations = masked.ToCharArray();
            foreach (var list in imports.Concat(ExportList.Matches(masked))) Blank(declarations, list.Index, list.Index + list.Length);
            violations.AddRange(DeclarationKeyword.Matches(new string(declarations)).Select(match => match.Groups[1].Value).Where(schemaTypes.Contains)
                .Distinct().Order(StringComparer.Ordinal).Select(name => Declares(path, name)));
            violations.AddRange(ExportList.Matches(masked).SelectMany(match => ListedNames(match.Groups[1].Value)).Where(schemaTypes.Contains)
                .Distinct().Order(StringComparer.Ordinal).Select(name => $"{path} re-exports the schema type {name}"));
            violations.AddRange(HelperNamespaceImport.Matches(code).Select(match => $"{path} imports {HelperModule} as the namespace {match.Groups[1].Value}"));
            if (GeneratedReExport.IsMatch(code)) violations.Add($"{path} re-exports the generated module");
            if (path == HelperModule && text.Contains("everything above is flatc output", StringComparison.Ordinal)) violations.Add($"{HelperModule} carries the flatc-output marker");
        }
        return violations;
    }

    // The source with its comments blanked (Code), and with the text of its string, template and regular-expression literals
    // blanked too (Masked); both keep the source's length, so a span found in one is the same span in the other. A template
    // literal's ${ } expressions stay code. A '/' starts a regular expression after an operator or an opening bracket, but is
    // division after an operand or a postfix '++' or '--'.
    private static (string Code, string Masked) Lex(string text)
    {
        var code = text.ToCharArray();
        var masked = text.ToCharArray();
        var templates = new Stack<int>();
        var depth = 0;
        var previous = '\0';
        var postfix = false;
        var index = 0;
        while (index < text.Length)
        {
            var character = text[index];
            var next = index + 1 < text.Length ? text[index + 1] : '\0';
            if (character == '/' && next is '/' or '*')
            {
                var close = next == '/' ? text.IndexOf('\n', index) : text.IndexOf("*/", index + 2, StringComparison.Ordinal);
                var end = close < 0 ? text.Length : next == '/' ? close : close + 2;
                Blank(code, index, end);
                Blank(masked, index, end);
                index = end;
            }
            else if (character is '\'' or '"' || (character == '/' && !postfix && previous is '\0' or '(' or ',' or '=' or ':' or '[' or '!' or '&' or '|' or '?' or '{' or ';' or '>' or '<' or '+' or '-' or '*' or '%' or '~' or '^'))
            {
                var end = LiteralEnd(text, index);
                Blank(masked, index + 1, end - 1);
                previous = 'a';
                postfix = false;
                index = end;
            }
            else if (character == '`' || (character == '}' && templates.TryPeek(out var open) && open == depth))
            {
                if (character == '}')
                {
                    templates.Pop();
                    depth--;
                }
                index = TemplateText(text, index + 1, masked, templates, ref depth);
                previous = text[index - 1] == '{' ? '{' : 'a';
                postfix = false;
            }
            else
            {
                if (character == '{') depth++;
                else if (character == '}') depth--;
                if (!char.IsWhiteSpace(character))
                {
                    postfix = character is '+' or '-' && previous == character && text[index - 1] == character;
                    previous = character;
                }
                index++;
            }
        }
        return (new string(code), new string(masked));
    }

    // The index after a string or regular-expression literal that opens at the start (an unterminated one ends at its line).
    private static int LiteralEnd(string text, int start)
    {
        var quote = text[start];
        var inClass = false;
        var index = start + 1;
        while (index < text.Length && text[index] != '\n')
        {
            if (text[index] == '\\')
            {
                index += 2;
                continue;
            }
            if (quote == '/' && text[index] == '[') inClass = true;
            else if (quote == '/' && text[index] == ']') inClass = false;
            else if (text[index] == quote && !inClass) break;
            index++;
        }
        return Math.Min(index + 1, text.Length);
    }

    // Blanks a template literal's text from the start to its closing '`' or its next "${", returning the index after it; an
    // opened ${ } expression is recorded at its brace depth so its closing '}' resumes the text.
    private static int TemplateText(string text, int start, char[] masked, Stack<int> templates, ref int depth)
    {
        var index = start;
        while (index < text.Length && text[index] != '`' && !(text[index] == '$' && index + 1 < text.Length && text[index + 1] == '{'))
            index += text[index] == '\\' ? 2 : 1;
        index = Math.Min(index, text.Length);
        Blank(masked, start, index);
        if (index >= text.Length) return text.Length;
        if (text[index] == '`') return index + 1;
        depth++;
        templates.Push(depth);
        return index + 2;
    }

    private static void Blank(char[] text, int start, int end)
    {
        for (var index = Math.Max(start, 0); index < Math.Min(end, text.Length); index++)
        {
            if (text[index] != '\n') text[index] = ' ';
        }
    }

    // The names of an import or export list: `type ` prefixes dropped, the original name of an `as` alias kept.
    private static IEnumerable<string> ListedNames(string list) => list.Split(',')
        .Select(entry => Regex.Replace(entry.Trim(), @"^type\s+", "").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "")
        .Where(name => name.Length > 0);

    // ----- T7: the e2e TypeScript FlatBuffers runtime is the compiler's version, pinned once, in tools/e2e only ------------

    [Fact]
    public void TheFlatBuffersRuntimeIsPinnedToTheCompilerVersion()
    {
        var version = Regex.Match(File.ReadAllText(Path.Combine(Root, VendoredProject)), "<FlatcVersion>([^<]+)</FlatcVersion>").Groups[1].Value;
        Assert.Matches(@"^\d+\.\d+\.\d+$", version);
        var pins = new RuntimeManifests(
            File.ReadAllText(Path.Combine(Root, E2eDirectory, "package.json")), File.ReadAllText(Path.Combine(Root, E2eDirectory, "package-lock.json")),
            File.ReadAllText(Path.Combine(Root, "package.json")), File.ReadAllText(Path.Combine(Root, "package-lock.json")),
            File.ReadAllText(Path.Combine(Root, E2eDirectory, "tsconfig.json")));
        Assert.Empty(RuntimePinViolations(pins, version));

        // Controls: a version range, a path mapping for the runtime and a root manifest that names it are each refused, naming
        // file and value.
        var pinned = $"\"flatbuffers\": \"{version}\"";
        Assert.Contains($"tools/e2e/package.json: devDependencies.flatbuffers is ^{version}, not exactly {version}",
            RuntimePinViolations(pins with { E2eManifest = Changed(pins.E2eManifest, pinned, $"\"flatbuffers\": \"^{version}\"") }, version));
        var mapped = """{ "compilerOptions": { "paths": { "flatbuffers": ["/opt/homebrew/lib/node_modules/flatbuffers"] } } }""";
        Assert.Contains("tools/e2e/tsconfig.json: compilerOptions.paths maps flatbuffers to [\"/opt/homebrew/lib/node_modules/flatbuffers\"]",
            RuntimePinViolations(pins with { E2eTsconfig = mapped }, version));
        var rootNamed = $$"""{ "name": "root", "devDependencies": { "flatbuffers": "{{version}}" } }""";
        Assert.Contains($"package.json: names flatbuffers in devDependencies (\"{version}\"); the runtime belongs to tools/e2e/package.json",
            RuntimePinViolations(pins with { RootManifest = rootNamed }, version));
        var rootDependency = $$"""{ "name": "root", "dependencies": { "flatbuffers": "{{version}}" } }""";
        Assert.Contains($"package.json: names flatbuffers in dependencies (\"{version}\"); the runtime belongs to tools/e2e/package.json",
            RuntimePinViolations(pins with { RootManifest = rootDependency }, version));
        var rootLocked = $$"""{ "packages": { "": { "name": "root" }, "node_modules/flatbuffers": { "version": "{{version}}" } } }""";
        Assert.Contains("package-lock.json: locks node_modules/flatbuffers; the runtime belongs to tools/e2e/package-lock.json",
            RuntimePinViolations(pins with { RootLock = rootLocked }, version));
        // Boundary: the e2e lock file resolving another version.
        Assert.Contains($"tools/e2e/package-lock.json: node_modules/flatbuffers is 0.0.1, not exactly {version}",
            RuntimePinViolations(pins with { E2eLock = Changed(pins.E2eLock, $"\"version\": \"{version}\"", "\"version\": \"0.0.1\"") }, version));
    }

    private static string Changed(string text, string find, string replacement)
    {
        Assert.Contains(find, text, StringComparison.Ordinal);
        return text.Replace(find, replacement, StringComparison.Ordinal);
    }

    /// <summary>The manifests and the e2e tsconfig T7 reads, as text.</summary>
    internal sealed record RuntimeManifests(string E2eManifest, string E2eLock, string RootManifest, string RootLock, string E2eTsconfig);

    private static readonly JsonDocumentOptions JsonOptions = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };
    private static readonly string[] LockedSourceFields = ["resolved", "integrity"];
    private static readonly string[] DependencySections = ["dependencies", "devDependencies", "optionalDependencies", "peerDependencies"];

    /// <summary>T7: flatbuffers is an exact devDependency of tools/e2e at the compiler version, locked there at that version with
    /// its tarball and integrity; the root manifest and lock do not name it; no tsconfig path mapping supplies it.</summary>
    internal static List<string> RuntimePinViolations(RuntimeManifests manifests, string version)
    {
        var violations = new List<string>();
        using var manifest = JsonDocument.Parse(manifests.E2eManifest, JsonOptions);
        var declared = Text(manifest.RootElement, "devDependencies", "flatbuffers");
        if (declared != version) violations.Add($"tools/e2e/package.json: devDependencies.flatbuffers is {declared ?? "absent"}, not exactly {version}");
        using var lockFile = JsonDocument.Parse(manifests.E2eLock, JsonOptions);
        var lockedRoot = Text(lockFile.RootElement, "packages", "", "devDependencies", "flatbuffers");
        if (lockedRoot != version) violations.Add($"tools/e2e/package-lock.json: the root devDependencies.flatbuffers is {lockedRoot ?? "absent"}, not exactly {version}");
        var locked = Text(lockFile.RootElement, "packages", "node_modules/flatbuffers", "version");
        if (locked != version) violations.Add($"tools/e2e/package-lock.json: node_modules/flatbuffers is {locked ?? "absent"}, not exactly {version}");
        foreach (var field in LockedSourceFields.Where(field => string.IsNullOrEmpty(Text(lockFile.RootElement, "packages", "node_modules/flatbuffers", field))))
            violations.Add($"tools/e2e/package-lock.json: node_modules/flatbuffers has no {field}");
        using var rootManifest = JsonDocument.Parse(manifests.RootManifest, JsonOptions);
        foreach (var section in DependencySections)
            if (Find(rootManifest.RootElement, section, "flatbuffers") is { } named)
                violations.Add($"package.json: names flatbuffers in {section} ({named.GetRawText()}); the runtime belongs to tools/e2e/package.json");
        using var rootLock = JsonDocument.Parse(manifests.RootLock, JsonOptions);
        if (Find(rootLock.RootElement, "packages") is { ValueKind: JsonValueKind.Object } packages)
            foreach (var package in packages.EnumerateObject().Where(package => package.Name.EndsWith("node_modules/flatbuffers", StringComparison.Ordinal)))
                violations.Add($"package-lock.json: locks {package.Name}; the runtime belongs to tools/e2e/package-lock.json");
        using var tsconfig = JsonDocument.Parse(manifests.E2eTsconfig, JsonOptions);
        if (Find(tsconfig.RootElement, "compilerOptions", "paths", "flatbuffers") is { } mapping)
            violations.Add($"tools/e2e/tsconfig.json: compilerOptions.paths maps flatbuffers to {mapping.GetRawText()}");
        return violations;
    }

    private static JsonElement? Find(JsonElement element, params string[] path)
    {
        foreach (var name in path)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out element)) return null;
        }
        return element;
    }

    private static string? Text(JsonElement element, params string[] path) =>
        Find(element, path) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    // ----- T8: one build graph runs the TypeScript compiler step once ----------------------------------------------

    private static readonly string[] GraphLogs = ["playtest.binlog", "solution.binlog", "web.binlog"];

    [Fact]
    public void TheTypeScriptStepRunsExactlyOncePerBuildGraph()
    {
        var directory = Environment.GetEnvironmentVariable("CSHARP_STATIC_GATE_BINLOGS") is { Length: > 0 } logs
            ? logs
            : throw new InvalidOperationException("CSHARP_STATIC_GATE_BINLOGS is required. Capture the five matching compiler-proof logs described in docs/delivery-workflow.md#local-test-authority.");
        foreach (var log in GraphLogs)
        {
            var path = Path.Combine(directory, log);
            Assert.True(File.Exists(path), $"{log}: missing (every gate build's binary log is required)");
            Assert.Null(OnceViolation(log, TypeScriptCompilerRuns(Replay(path))));
        }

        // Controls: a graph that never ran the step and one that ran it twice are each refused, naming the log and the count.
        Assert.Equal("web.binlog: the compiler Exec of GenerateWireTypeScript started 0 times, not exactly once",
            OnceViolation("web.binlog", TypeScriptCompilerRuns([Target(1, 1, GenerationTarget, VendoredProject), Task(1, 2, "Exec")])));
        Assert.Equal("solution.binlog: the compiler Exec of GenerateWireTypeScript started 2 times, not exactly once",
            OnceViolation("solution.binlog", TypeScriptCompilerRuns([
                Target(1, 1, GenerationTarget, VendoredProject), Task(1, 1, "Exec"),
                Target(2, 3, GenerationTarget, VendoredProject), Task(2, 3, "Exec"), Task(2, 3, "Touch"),
                Target(1, 4, "GenerateWireAccessors", VendoredProject), Task(1, 4, "Exec"),
                Target(1, 5, GenerationTarget, "CuriousContraptions.csproj"), Task(1, 5, "Exec")])));
    }

    /// <summary>T8: the Exec tasks that start inside the vendored project's GenerateWireTypeScript target, matched by the
    /// node, project context and target of their build event context.</summary>
    internal static int TypeScriptCompilerRuns(IEnumerable<BuildEventArgs> events)
    {
        var targets = new HashSet<(int, int, int)>();
        var runs = 0;
        foreach (var raised in events)
        {
            if (raised.BuildEventContext is not { } context) continue;
            var key = (context.NodeId, context.ProjectContextId, context.TargetId);
            if (raised is TargetStartedEventArgs { TargetName: GenerationTarget } target && IsVendoredProject(target.ProjectFile)) targets.Add(key);
            else if (raised is TaskStartedEventArgs { TaskName: "Exec" } && targets.Contains(key)) runs++;
        }
        return runs;
    }

    internal static string? OnceViolation(string log, int runs) =>
        runs == 1 ? null : $"{log}: the compiler Exec of {GenerationTarget} started {runs} times, not exactly once";

    private static bool IsVendoredProject(string? projectFile) =>
        projectFile is not null && projectFile.Replace('\\', '/').EndsWith("/" + VendoredProject, StringComparison.Ordinal);

    // Only target starts and Exec starts are kept: the count needs nothing else from a log.
    private static List<BuildEventArgs> Replay(string binlog)
    {
        var events = new List<BuildEventArgs>();
        var replay = new Microsoft.Build.Logging.BinaryLogReplayEventSource();
        replay.AnyEventRaised += (_, raised) =>
        {
            if (raised is TargetStartedEventArgs or TaskStartedEventArgs { TaskName: "Exec" }) events.Add(raised);
        };
        replay.Replay(binlog);
        return events;
    }

    private static TargetStartedEventArgs Target(int node, int target, string name, string project) =>
        new("", "", name, "/repository/" + project, "/repository/" + project) { BuildEventContext = Context(node, target) };

    private static TaskStartedEventArgs Task(int node, int target, string name) =>
        new("", "", "/repository/" + VendoredProject, "/repository/" + VendoredProject, name) { BuildEventContext = Context(node, target) };

    private static BuildEventContext Context(int node, int target) => new(1, node, 1, 1, 1, target, 1);
}
