using System.Text.RegularExpressions;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 9R.0c F1: in the browser an exception escaping an async void handler is posted to the thread pool and terminates the
/// .NET runtime. Every async void under ui/ is therefore exactly a boundary over its Task method that logs and shows a failure
/// (Workshop.HandlerFailed); engine/ keeps only MachineWorld._ExitTree, whose disposal catches its own failure.
/// </summary>
public sealed class WorkshopHandlerBoundaryTests
{
    private static readonly Regex AsyncVoid = new(@"async void (?<name>\w+)\(", RegexOptions.CultureInvariant);

    private static DirectoryInfo Root()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "project.godot"))) root = root.Parent;
        return Assert.IsType<DirectoryInfo>(root);
    }

    private static IEnumerable<(string File, string Name, string Body)> Handlers(string directory)
    {
        foreach (var file in Directory.GetFiles(Path.Combine(Root().FullName, directory), "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in AsyncVoid.Matches(text))
                yield return (Path.GetFileName(file), match.Groups["name"].Value, Body(text, match.Index));
        }
    }

    // The method body from its opening brace to the matching closing brace, or an expression body up to its semicolon.
    private static string Body(string text, int start)
    {
        var open = text.IndexOf('{', start);
        var arrow = text.IndexOf("=>", start, StringComparison.Ordinal);
        if (arrow >= 0 && (open < 0 || arrow < open)) return text[arrow..(text.IndexOf(';', arrow) + 1)];
        var depth = 0;
        for (var index = open; index < text.Length; index++)
        {
            if (text[index] == '{') depth++;
            else if (text[index] == '}' && --depth == 0) return text[open..(index + 1)];
        }
        throw new InvalidDataException("Unbalanced method body.");
    }

    // A Godot override such as _Ready awaits ReadyAsync.
    private static bool IsBoundary(string name, string body) => Regex.IsMatch(Regex.Replace(body, @"\s+", " "),
        $@"^\{{ try \{{ await {name.TrimStart('_')}Async\([^)]*\); \}} catch \(Exception error\) \{{ HandlerFailed\(nameof\({name}\), error\); \}} \}}$",
        RegexOptions.CultureInvariant);

    [Fact]
    public void EveryAsyncVoidUiHandlerIsACatchingBoundaryOverItsTaskMethod()
    {
        var handlers = Handlers("ui").ToArray();
        Assert.True(handlers.Length >= 16,$"Expected the Workshop's async void handlers; found {handlers.Length}.");
        Assert.All(handlers, handler => Assert.True(IsBoundary(handler.Name, handler.Body),
            $"{handler.File}: async void {handler.Name} must only await {handler.Name.TrimStart('_')}Async inside a catch that calls HandlerFailed."));
        // Control: the shape check rejects a handler that awaits work without the boundary.
        Assert.False(IsBoundary("Click", "{ if (_tool is null) return; await SubmitConstruction(proposed); }"));
        Assert.False(IsBoundary("Click", "{ try { await ClickAsync(screen); } catch (ArgumentException error) { HandlerFailed(nameof(Click), error); } }"));
    }

    [Fact]
    public void TheEngineKeepsOnlyItsAuditedAsyncVoidOverride()
    {
        var handler = Assert.Single(Handlers("engine"));
        Assert.Equal(("MachineWorld.cs", "_ExitTree"), (handler.File, handler.Name));
        Assert.Equal("=> await DisposeWorkshop();", handler.Body);
    }
}
