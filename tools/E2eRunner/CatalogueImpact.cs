using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CuriousContraptions.E2eRunner;

/// <summary>Compare catalogue rows as syntax; any change outside the All initializer keeps Full.</summary>
internal static class CatalogueImpact
{
    public static IReadOnlySet<SuiteId>? Changed(string? before, string? after)
    {
        if (before is null || after is null) return null;
        var oldRoot = CSharpSyntaxTree.ParseText(before).GetRoot();
        var newRoot = CSharpSyntaxTree.ParseText(after).GetRoot();
        if (oldRoot.ContainsDiagnostics || newRoot.ContainsDiagnostics) return null;
        if (!PreprocessorText(oldRoot).SequenceEqual(PreprocessorText(newRoot), StringComparer.Ordinal)) return null;
        var oldAll = Initializer(oldRoot);
        var newAll = Initializer(newRoot);
        if (oldAll is null || newAll is null) return null;
        var empty = SyntaxFactory.CollectionExpression();
        if (!SyntaxFactory.AreEquivalent(oldRoot.ReplaceNode(oldAll, empty), newRoot.ReplaceNode(newAll, empty))) return null;
        var oldRows = Rows(oldAll);
        var newRows = Rows(newAll);
        if (oldRows is null || newRows is null) return null;
        var changed = new HashSet<SuiteId>();
        foreach (var id in oldRows.Keys.Union(newRows.Keys))
            if (!oldRows.TryGetValue(id, out var was) || !newRows.TryGetValue(id, out var now) || !SyntaxFactory.AreEquivalent(was, now))
                changed.Add(id);
        // A deleted ID cannot be executed by the current catalogue, so narrowing would omit its old coverage.
        return changed.Any(id => !SuiteCatalogue.All.Any(suite => suite.Id == id)) ? null : changed;
    }

    private static IEnumerable<string> PreprocessorText(SyntaxNode root) =>
        root.DescendantTrivia(descendIntoTrivia: true)
            .Where(trivia => trivia.IsDirective || trivia.IsKind(SyntaxKind.DisabledTextTrivia))
            .Select(trivia => trivia.ToFullString());

    private static CollectionExpressionSyntax? Initializer(SyntaxNode root)
    {
        var declarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(type => type.Identifier.ValueText == "SuiteCatalogue").ToArray();
        if (declarations.Length != 1) return null;
        var fields = declarations[0].Members.OfType<FieldDeclarationSyntax>()
            .SelectMany(field => field.Declaration.Variables)
            .Where(variable => variable.Identifier.ValueText == "All").ToArray();
        return fields.Length == 1 ? fields[0].Initializer?.Value as CollectionExpressionSyntax : null;
    }

    private static Dictionary<SuiteId, ExpressionSyntax>? Rows(CollectionExpressionSyntax all)
    {
        var result = new Dictionary<SuiteId, ExpressionSyntax>();
        foreach (var element in all.Elements)
        {
            if (element is not ExpressionElementSyntax { Expression: InvocationExpressionSyntax invocation } ||
                invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression is not MemberAccessExpressionSyntax member ||
                member.Expression is not IdentifierNameSyntax { Identifier.ValueText: "SuiteId" } ||
                !Enum.TryParse<SuiteId>(member.Name.Identifier.ValueText, out var id) || !Enum.IsDefined(id) ||
                !result.TryAdd(id, invocation)) return null;
        }
        return result;
    }
}
