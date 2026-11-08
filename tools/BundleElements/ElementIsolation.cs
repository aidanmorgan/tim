using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CuriousContraptions.Bundle;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CuriousContraptions.BundleElements;

/// <summary>Build-time package coupling guard over shipping source ownership.</summary>
public sealed class ElementIsolation
{
    public enum Rule { ElementLiteral, NamedPart, PackageIdentifier }
    public sealed record Hit(string File, string Symbol, Rule Rule, string Token, int Line);
    private sealed record Lexeme(string Text, int Start, bool Literal);
    private static readonly string[] Outputs = ["bin", "obj", ".godot", ".git"];
    private static readonly string[] Suffixes =
    [
        "Solver", "ConstraintSolver", "TransmissionSolver", "FrictionSolver",
        "Read", "WheelRead", "Observation", "Record", "TargetRead", "StateRead",
        "Reached", "Set", "Triggered", "Fired", "StateChanged", "Changed",
        "Settings", "Dimensions", "Lessons", "Simulation", "Payload",
        "Logic", "Handler", "Controller", "Dispatch"
    ];
    private static readonly string[] Prefixes =
    [
        "Solve", "Step", "Compile", "Simulate", "Execute", "Dispatch",
        "Query", "Read", "On", "Workshop"
    ];
    private readonly string Root;
    private readonly string[] Ids;
    private readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal);

    public ElementIsolation(string root, IEnumerable<BundleEntry> entries)
    {
        Root = root;
        var declarations = entries.ToArray();
        Ids = declarations.Select(entry => entry.Metadata.Id).ToArray();
        foreach (var entry in declarations)
            Aliases[entry.Metadata.ScenePath.Split('/')[^2]] = entry.Metadata.Id;
    }

    private static SyntaxNode Parse(string source)
    {
        // Scan every conditional branch, independently of this tool's build symbols.
        // Blanking only directive trivia preserves original source positions and all branch text.
        var characters = source.ToCharArray();
        var parsed = CSharpSyntaxTree.ParseText(source).GetRoot();
        foreach (var directive in parsed.DescendantTrivia(descendIntoTrivia: true).Where(trivia => trivia.IsDirective))
            for (var i = directive.FullSpan.Start; i < directive.FullSpan.End; i++)
                if (characters[i] is not ('\r' or '\n')) characters[i] = ' ';
        return CSharpSyntaxTree.ParseText(new string(characters)).GetRoot();
    }

    private static string ToCamel(string value) =>
        char.ToLowerInvariant(value[0]) + value[1..];

    private static bool MatchesSuffix(string identifier, string name)
    {
        var camel = ToCamel(name);
        var iface = "I" + name;
        foreach (var suffix in Suffixes)
        {
            if (identifier == name + suffix || identifier == camel + suffix || identifier == iface + suffix)
                return true;
        }
        return false;
    }

    private static bool MatchesPrefix(string identifier, string name)
    {
        foreach (var prefix in Prefixes)
            if (identifier == prefix + name || identifier == ToCamel(prefix) + name) return true;
        return false;
    }

    private static bool MatchesCompound(string identifier, string name)
    {
        if (identifier == "Workshop" + name + "Solver" || identifier == "workshop" + name + "Solver") return true;
        if (identifier == "Add" + name + "Configuration" || identifier == "add" + name + "Configuration") return true;
        if (identifier == "Apply" + name + "Configuration" || identifier == "apply" + name + "Configuration") return true;
        return false;
    }

    private static bool MatchesPlural(string identifier, string name)
    {
        var camel = ToCamel(name);
        if (identifier == name + "s" || identifier == camel + "s") return true;
        if (name.EndsWith('y') && (identifier == name[..^1] + "ies" || identifier == camel[..^1] + "ies")) return true;
        if ((name.EndsWith('o') || name.EndsWith('x') || name.EndsWith('s') ||
             name.EndsWith("ch", StringComparison.Ordinal) || name.EndsWith("sh", StringComparison.Ordinal)) &&
            (identifier == name + "es" || identifier == camel + "es")) return true;
        return false;
    }

    private string? PackageIdentifier(string identifier)
    {
        foreach (var (name, id) in Aliases)
        {
            if (MatchesSuffix(identifier, name) || MatchesPrefix(identifier, name) ||
                MatchesCompound(identifier, name) || MatchesPlural(identifier, name))
                return id;
        }
        return null;
    }

    private bool HasElementNamespace(string text, out string nameToken)
    {
        var segments = text.Split(['.', ':'], StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i] == "Elements" && Aliases.ContainsKey(segments[i + 1]))
            {
                nameToken = segments[i + 1];
                return true;
            }
        }
        if (segments.Length > 0 && segments[^1] == "Elements" &&
            (text.StartsWith("CuriousContraptions", StringComparison.Ordinal) || text.StartsWith("global::CuriousContraptions", StringComparison.Ordinal)))
        {
            nameToken = "Elements";
            return true;
        }
        nameToken = "";
        return false;
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

    private static bool IsStringLiteral(SyntaxToken token) =>
        token.IsKind(SyntaxKind.StringLiteralToken) ||
        token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken) ||
        token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken) ||
        token.IsKind(SyntaxKind.InterpolatedStringTextToken);

    private void ScanTokens(SyntaxNode syntax, Action<SyntaxToken, Rule, string> add)
    {
        foreach (var token in syntax.DescendantTokens(descendIntoTrivia: true))
        {
            if (IsStringLiteral(token) && Ids.Contains(token.ValueText, StringComparer.Ordinal))
                add(token, Rule.ElementLiteral, token.ValueText);
            else if (token.IsKind(SyntaxKind.IdentifierToken) && PackageIdentifier(token.ValueText) is not null)
                add(token, Rule.PackageIdentifier, token.ValueText);
        }
    }

    private void ScanNamespaceReferences(SyntaxNode syntax, Action<SyntaxToken, Rule, string> add)
    {
        foreach (var use in syntax.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            var text = use.Name?.ToString();
            if (text is not null && HasElementNamespace(text, out var nameToken))
            {
                var token = use.Name!.DescendantTokens().LastOrDefault(t => t.ValueText == nameToken);
                add(token.Kind() != SyntaxKind.None ? token : use.GetFirstToken(), Rule.PackageIdentifier, text);
            }
        }
        foreach (var qualified in syntax.DescendantNodes().OfType<QualifiedNameSyntax>())
        {
            if (qualified.Parent is not UsingDirectiveSyntax and not QualifiedNameSyntax)
                CheckNamespaceNode(qualified, qualified.ToString(), add);
        }
        foreach (var member in syntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (member.Parent is not MemberAccessExpressionSyntax)
                CheckNamespaceNode(member, member.ToString(), add);
        }
    }

    private void CheckNamespaceNode(SyntaxNode node, string text, Action<SyntaxToken, Rule, string> add)
    {
        if (HasElementNamespace(text, out var nameToken))
        {
            var token = node.DescendantTokens().LastOrDefault(t => t.ValueText == nameToken);
            add(token.Kind() != SyntaxKind.None ? token : node.GetFirstToken(), Rule.PackageIdentifier, text);
        }
    }

    private static HashSet<string> ResolvePartAliases(SyntaxNode syntax) =>
        syntax.DescendantNodes().OfType<UsingDirectiveSyntax>()
            .Where(use => use.Alias is not null && use.Name?.ToString().Split('.').Last() == "PartId")
            .Select(use => use.Alias!.Name.Identifier.ValueText).Append("PartId").ToHashSet(StringComparer.Ordinal);

    private void ScanMembers(SyntaxNode syntax, HashSet<string> partAliases, Action<SyntaxToken, Rule, string> add)
    {
        foreach (var member in syntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (Aliases.ContainsKey(member.Name.Identifier.ValueText) && (partAliases.Contains(member.Expression.ToString()) || member.Expression.ToString().EndsWith(".PartId", StringComparison.Ordinal)))
                add(member.Name.Identifier, Rule.NamedPart, member.Name.Identifier.ValueText);
            else if (member.Expression.ToString() == "CosmeticCurves" && Aliases.ContainsKey(member.Name.Identifier.ValueText))
                add(member.Name.Identifier, Rule.PackageIdentifier, "CosmeticCurves." + member.Name.Identifier.ValueText);
        }
    }

    private static void ScanPatterns(SyntaxNode syntax, HashSet<string> partAliases, Action<SyntaxToken, Rule, string> add)
    {
        foreach (var name in syntax.DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => partAliases.Contains(name.Identifier.ValueText)))
            if (name.Ancestors().TakeWhile(parent => parent is not StatementSyntax).Any(parent =>
                parent is DeclarationPatternSyntax or TypePatternSyntax || parent is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.IsExpression) || parent is TypeArgumentListSyntax arguments &&
                arguments.Parent is GenericNameSyntax generic && generic.Identifier.ValueText == "OfType"))
                add(name.Identifier, Rule.NamedPart, "<type-pattern>");
    }

    public List<Hit> ScanCSharp(string file, string source)
    {
        var hits = new List<Hit>();
        var syntax = Parse(source);
        void Add(SyntaxToken token, Rule rule, string value) =>
            hits.Add(new(file, Symbol(token.Parent!), rule, value, token.GetLocation().GetLineSpan().StartLinePosition.Line + 1));
        ScanTokens(syntax, Add);
        ScanNamespaceReferences(syntax, Add);
        var partAliases = ResolvePartAliases(syntax);
        ScanMembers(syntax, partAliases, Add);
        ScanPatterns(syntax, partAliases, Add);
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
        LexJavaScriptCode(source, result, ref i, false);
        return result;
    }

    private static void LexJavaScriptCode(string source, List<Lexeme> result, ref int i, bool interpolation)
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
                LexJavaScriptString(source, result, ref i, start, c);
                continue;
            }
            if (char.IsLetter(c) || c is '_' or '$')
            {
                while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] is '_' or '$')) i++;
                result.Add(new(source[start..i], start, false));
            }
            else if (c == '.') result.Add(new(".", start, false));
        }
    }

    private static void LexJavaScriptString(string source, List<Lexeme> result, ref int i, int start, char c)
    {
        var text = new StringBuilder();
        while (i < source.Length)
        {
            var next = source[i++];
            if (next == '\\' && i < source.Length) { text.Append(next).Append(source[i++]); continue; }
            if (next == c) break;
            if (c == '`' && next == '$' && i < source.Length && source[i] == '{')
            {
                result.Add(new(Unescape(text.ToString()), start, true)); text.Clear(); i++; LexJavaScriptCode(source, result, ref i, true); start = i; continue;
            }
            text.Append(next);
        }
        result.Add(new(Unescape(text.ToString()), start, true));
    }

    private bool HasElementPath(string text)
    {
        var parts = text.Replace('\\', '/').Split('/');
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (parts[i] == "elements" && (Aliases.ContainsKey(parts[i + 1]) || Ids.Contains(parts[i + 1], StringComparer.Ordinal)))
                return true;
        }
        return false;
    }

    private bool IsElementImportPath(List<Lexeme> tokens, int index)
    {
        var token = tokens[index];
        if (!token.Literal) return false;
        if (HasElementPath(token.Text)) return true;
        if (index > 0 && tokens[index - 1].Text is "from" or "import")
        {
            var parts = token.Text.Replace('\\', '/').Split('/');
            if (parts.Any(p => Aliases.ContainsKey(p) || Ids.Contains(p, StringComparer.Ordinal)))
                return true;
        }
        return false;
    }

    private bool IsProhibitedImportToken(List<Lexeme> tokens, int index)
    {
        var token = tokens[index];
        if (token.Literal || !Aliases.ContainsKey(token.Text)) return false;
        var hasImport = false;
        for (var j = index - 1; j >= 0 && j >= index - 6; j--)
        {
            if (tokens[j].Text is "import" or "export") { hasImport = true; break; }
        }
        if (!hasImport) return false;
        for (var j = index + 1; j < tokens.Count && j <= index + 6; j++)
        {
            if (tokens[j].Text == "from") return true;
        }
        return false;
    }

    public List<Hit> ScanJavaScript(string file, string source)
    {
        var hits = new List<Hit>();
        var tokens = LexJavaScript(source);
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var line = source.AsSpan(0, token.Start).Count('\n') + 1;
            Rule? rule = null;
            if (token.Literal)
            {
                if (Ids.Contains(token.Text, StringComparer.Ordinal))
                    rule = Rule.ElementLiteral;
                else if (IsElementImportPath(tokens, i))
                    rule = Rule.PackageIdentifier;
            }
            else
            {
                if (PackageIdentifier(token.Text) is not null || IsProhibitedImportToken(tokens, i))
                    rule = Rule.PackageIdentifier;
                else if (i >= 2 && tokens[i - 2].Text == "PartId" && tokens[i - 1].Text == ".")
                    rule = Rule.NamedPart;
            }
            if (rule is not null) hits.Add(new(file, "<module>", rule.Value, token.Text, line));
        }
        return hits;
    }

    public static bool Included(string file)
    {
        // Exact outputs of the pinned RuntimeTypeScript target; their .ts sources remain scanned.
        if (file is "CuriousContraptions.web/wwwroot/pose-ring.js" or "CuriousContraptions.web/wwwroot/workshop-isolation.js" or
            "CuriousContraptions.web/wwwroot/workshop-isolation-worker.js" or "CuriousContraptions.web/wwwroot/workshop-bootstrap.js" or
            "CuriousContraptions.Animation.Worker/wwwroot/worker.js" or "CuriousContraptions.Simulation/wwwroot/clock-master.js" or
            "CuriousContraptions.web/wwwroot/clock-ring.js" or "CuriousContraptions.web/wwwroot/native-clock.js" or
            "CuriousContraptions.Simulation/wwwroot/worker.js" or
            "CuriousContraptions.web/wwwroot/event-ring-cursor.js" or "CuriousContraptions.web/wwwroot/workshop-client.js") return false;
        var parts = file.Replace('\\', '/').Split('/');
        if (parts.Any(part => Outputs.Contains(part, StringComparer.Ordinal))) return false;
        if (!(file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".js", StringComparison.Ordinal) ||
              file.EndsWith(".ts", StringComparison.Ordinal))) return false;
        return parts[0] is "engine" or "ui" || parts[0].StartsWith("CuriousContraptions.", StringComparison.Ordinal) &&
            parts[0] != "CuriousContraptions.tests";
    }

    private IEnumerable<string> Sources(string directory)
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
    public List<Hit> ScanTree() => new[] { Path.Combine(Root, "engine"), Path.Combine(Root, "ui") }
        .Concat(Directory.EnumerateDirectories(Root, "CuriousContraptions.*")
            .Where(path => Path.GetFileName(path) != "CuriousContraptions.tests"))
        .SelectMany(Sources).Order(StringComparer.Ordinal)
        .SelectMany(file => file.EndsWith(".cs", StringComparison.Ordinal) ? ScanCSharp(file, File.ReadAllText(Path.Combine(Root, file))) :
            ScanJavaScript(file, File.ReadAllText(Path.Combine(Root, file)))).ToList();
}
