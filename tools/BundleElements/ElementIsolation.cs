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
    private readonly string Root;
    private readonly string[] Ids;
    private readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal);

    public ElementIsolation(string root, IEnumerable<BundleEntry> entries)
    {
        Root = root;
        var declarations = entries.ToArray();
        Ids = declarations.Select(entry => entry.Metadata.Id).ToArray();
        foreach (var entry in declarations)
        {
            Aliases[Pascal(entry.Metadata.Id)] = entry.Metadata.Id;
            Aliases[entry.Metadata.ScenePath.Split('/')[^2]] = entry.Metadata.Id;
        }
    }

    private static string Pascal(string value) => string.Concat(Regex.Matches(value, "[A-Za-z0-9]+")
        .Select(match => char.ToUpperInvariant(match.Value[0]) + match.Value[1..]));
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
    private string? PackageIdentifier(string identifier)
    {
        foreach (var (name, id) in Aliases)
        {
            // Generic laws may use capability vocabulary. Package records and package-owned configuration may not escape.
            if (identifier == "Workshop" + name || identifier == name + "Settings" ||
                identifier == name + "Dimensions" || identifier == name + "Lessons" || identifier == name + "Simulation" ||
                identifier == "Add" + name + "Configuration" || identifier == "Apply" + name + "Configuration") return id;
        }
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

    public List<Hit> ScanCSharp(string file, string source)
    {
        var hits = new List<Hit>();
        var syntax = Parse(source);
        void Add(SyntaxToken token, Rule rule, string value) =>
            hits.Add(new(file, Symbol(token.Parent!), rule, value, token.GetLocation().GetLineSpan().StartLinePosition.Line + 1));
        foreach (var token in syntax.DescendantTokens(descendIntoTrivia: true))
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

    public List<Hit> ScanJavaScript(string file, string source)
    {
        var hits = new List<Hit>();
        var tokens = LexJavaScript(source);
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var line = source.AsSpan(0, token.Start).Count('\n') + 1;
            Rule? rule = token.Literal && Ids.Contains(token.Text, StringComparer.Ordinal) ? Rule.ElementLiteral :
                !token.Literal && PackageIdentifier(token.Text) is not null ? Rule.PackageIdentifier : null;
            if (!token.Literal && i >= 2 && tokens[i - 2].Text == "PartId" && tokens[i - 1].Text == ".") rule = Rule.NamedPart;
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
