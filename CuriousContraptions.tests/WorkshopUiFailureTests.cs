using System.ComponentModel;
using System.Globalization;
using CuriousContraptions.Gpu;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CuriousContraptions.Tests;

// Story 9R.0 F7: a UI handler's declared refusal is a stdout rejection showing its message; anything else is a defect,
// a whole-exception stderr fault, and the status line names it as an unexpected fault. Console controls run serialized
// (xunit.runner.json: parallelizeTestCollections=false).
public sealed class WorkshopUiFailureTests
{
    private static (string Status, string Out, string Error) Report(Exception error)
    {
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        using var errors = new StringWriter(CultureInfo.InvariantCulture);
        var (previousOut, previousError) = (Console.Out, Console.Error);
        Console.SetOut(output);
        Console.SetError(errors);
        try { return (WorkshopUiFailure.Report(error), output.ToString(), errors.ToString()); }
        finally { Console.SetOut(previousOut); Console.SetError(previousError); }
    }

    public static TheoryData<Exception> Refusals() => new()
    {
        new WorkshopEditRejectedException(WorkshopEditRejection.FixedPart, "Fixed puzzle instances cannot be edited."),
        new WorkbenchFullException(WorkbenchTable.Instances),
        new WorkshopTransportException(new(new(1), new(1), default, default, default), WorkshopTransportState.TimedOut, new TimeoutException("late")),
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public void ADeclaredRefusalIsAStdoutRejectionShowingItsMessage(Exception refusal)
    {
        var (status, output, errors) = Report(refusal);
        Assert.Equal(refusal.Message, status);
        Assert.StartsWith("CCGPU_UI_REJECTION " + refusal.GetType().FullName, output, StringComparison.Ordinal);
        Assert.Empty(errors);
    }

    // A real rule's refusal reaches the handler typed, with its reason and message.
    [Fact]
    public void AConnectionRuleRefusalIsAStdoutRejection()
    {
        var links = Enumerable.Range(0, WorkshopConnections.Capacity + 1)
            .Select(index => new WorkshopConnection(new(1), WorkshopSocket.ActivationOut, new((ulong)index + 2), WorkshopSocket.ActivationIn,
                WorkshopConnectionDomain.Activation)).ToArray();
        var error = Record.Exception(() => new WorkshopConnections(links).Validate(WorkshopInstances.Empty));
        var rejection = Assert.IsType<WorkshopEditRejectedException>(error);
        Assert.Equal(WorkshopEditRejection.ConnectionCapacity, rejection.Reason);
        var (status, output, errors) = Report(rejection);
        Assert.Equal("Connection capacity exceeded.", status);
        Assert.StartsWith("CCGPU_UI_REJECTION " + typeof(WorkshopEditRejectedException).FullName, output, StringComparison.Ordinal);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(WorkshopEditRejection.None)]
    [InlineData((WorkshopEditRejection)999)]
    public void ARejectionNeedsADeclaredReason(WorkshopEditRejection reason) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkshopEditRejectedException(reason, "never shown"));

    // ArgumentException itself, its other subtypes and every state failure: none of them is a refused edit.
    [Theory]
    [InlineData(typeof(ArgumentException))]
    [InlineData(typeof(NullReferenceException))]
    [InlineData(typeof(IndexOutOfRangeException))]
    [InlineData(typeof(KeyNotFoundException))]
    [InlineData(typeof(FormatException))]
    [InlineData(typeof(ArgumentNullException))]
    [InlineData(typeof(ArgumentOutOfRangeException))]
    [InlineData(typeof(InvalidEnumArgumentException))]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(ObjectDisposedException))]
    public void AnyOtherExceptionTypeIsADefect(Type defect)
    {
        var error = defect == typeof(ObjectDisposedException)
            ? new ObjectDisposedException("Workshop")
            : (Exception)Activator.CreateInstance(defect)!;
        Assert.False(WorkshopUiFailure.IsRejection(error));
    }

    private static readonly Dictionary<string, Action> RuntimeDefects = new(StringComparer.Ordinal)
    {
        // A framework ArgumentException is a defect, not a refused edit.
        ["duplicate key"] = () => new Dictionary<int, int> { [1] = 1 }.Add(1, 2),
        ["empty sequence"] = () => _ = Array.Empty<int>().First(),
        ["collection modified"] = () => { var items = new List<int> { 1, 2 }; foreach (var item in items) items.Add(item); },
        ["missing key"] = () => _ = new Dictionary<int, int>()[1],
        ["index"] = () => _ = new List<int>()[0],
        ["null argument"] = () => ArgumentNullException.ThrowIfNull((object?)null, "part"),
    };

    public static TheoryData<string> RuntimeDefectNames() => new(RuntimeDefects.Keys);

    [Theory]
    [MemberData(nameof(RuntimeDefectNames))]
    public void ARuntimeFailureIsAWholeStderrFaultAndAFaultStatus(string name)
    {
        // Thrown by the runtime itself, so the exception carries a stack trace for the log line to include.
        var error = Record.Exception(RuntimeDefects[name]);
        Assert.NotNull(error);
        var (status, output, errors) = Report(error);
        Assert.Equal($"Unexpected fault ({error.GetType().Name}): {error.Message} It has been logged.", status);
        Assert.StartsWith($"CCGPU_UI_HANDLER_EXCEPTION {error.GetType().FullName}: {error.Message}", errors, StringComparison.Ordinal);
        Assert.Contains(nameof(WorkshopUiFailureTests), errors, StringComparison.Ordinal);
        Assert.Empty(output);
    }

    // ----- the UI handlers route every caught exception through the classification -------------------------------

    private static string Root()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "CuriousContraptions.slnx"))) return directory.FullName;
        throw new InvalidOperationException("The repository root (CuriousContraptions.slnx) is not above the test output.");
    }

    private static bool Invokes(SyntaxNode node, string receiver, string method) =>
        node.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation =>
            invocation.Expression is MemberAccessExpressionSyntax access &&
            access.Expression.ToString() == receiver && access.Name.Identifier.ValueText == method);

    [Fact]
    public void EveryUiCatchReportsThroughTheClassificationOrIsAFaultOrRethrows()
    {
        var violations = new List<string>();
        var ui = Path.Combine(Root(), "ui");
        foreach (var path in Directory.EnumerateFiles(ui, "*.cs").Where(path => Path.GetFileName(path) != "WorkshopUiFailure.cs").Order(StringComparer.Ordinal))
        {
            var cancellation = TestContext.Current.CancellationToken;
            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(path), cancellationToken: cancellation).GetRoot(cancellation);
            if (Invokes(root, "WorkshopDiagnostics", "Rejection"))
                violations.Add($"{Path.GetFileName(path)}: logs a rejection without WorkshopUiFailure.Report");
            foreach (var clause in root.DescendantNodes().OfType<CatchClauseSyntax>())
            {
                var reported = Invokes(clause.Block, "WorkshopUiFailure", "Report") || Invokes(clause.Block, "WorkshopDiagnostics", "Fault");
                var rethrown = clause.Block.DescendantNodes().OfType<ThrowStatementSyntax>().Any(statement => statement.Expression is null);
                if (!reported && !rethrown)
                    violations.Add($"{Path.GetFileName(path)}:{clause.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {clause.CatchKeyword} neither reports nor rethrows");
            }
        }
        // Every violation in full (an assertion on the collection truncates each entry).
        if (violations.Count > 0) Assert.Fail($"{violations.Count} UI catch violation(s):\n" + string.Join('\n', violations));
    }
}
