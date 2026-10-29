using System.ComponentModel;
using CuriousContraptions.Gpu;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CuriousContraptions.Tests;

// Story 9R.0 F7: a UI handler's declared refusal is a typed rejection showing its message; anything else is a defect,
// a whole-exception typed fault, and the status line names it as an unexpected fault. Diagnostic controls run serialized
// (xunit.runner.json: parallelizeTestCollections=false).
public sealed class WorkshopUiFailureTests
{
    private static (string Status, WorkshopFaultDiagnostic Diagnostic) Report(Exception error)
    {
        var diagnostics = new WorkshopDiagnostics();
        var status = WorkshopUiFailure.Report(error, diagnostics);
        return (status, Assert.IsType<WorkshopFaultDiagnostic>(Assert.Single(diagnostics.Snapshot())));
    }

    public static TheoryData<Exception> Refusals() => new()
    {
        new WorkshopEditRejectedException(WorkshopEditRejection.FixedPart, "Fixed puzzle instances cannot be edited."),
        new WorkshopEditRejectedException(WorkshopEditRejection.IdentityExhausted, "Compiled identity storage is exhausted."),
        new WorkshopTransportException(new(new(1), new(1), default, default, default), WorkshopTransportState.TimedOut, new TimeoutException("late")),
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public void ADeclaredRefusalIsATypedRejectionShowingItsMessage(Exception refusal)
    {
        var (status, diagnostic) = Report(refusal);
        Assert.Equal(refusal.Message, status);
        Assert.Equal(WorkshopFaultKind.Rejection, diagnostic.Kind);
        Assert.Equal(refusal.GetType().FullName, diagnostic.ExceptionType);
    }

    // A real rule's refusal reaches the handler typed, with its reason and message.
    [Fact]
    public void AConnectionRuleRefusalIsATypedRejection()
    {
        // Story 9R.5b: there is no connection limit, so the rule exercised is the self-connection refusal.
        var link = new WorkshopConnection(new(1), WorkshopSocket.ActivationOut, new(1), WorkshopSocket.ActivationIn,
            WorkshopConnectionDomain.Activation);
        var error = Record.Exception(() => new WorkshopConnections(link).Validate(WorkshopInstances.Empty));
        var rejection = Assert.IsType<WorkshopEditRejectedException>(error);
        Assert.Equal(WorkshopEditRejection.InvalidConnection, rejection.Reason);
        var (status, diagnostic) = Report(rejection);
        Assert.Equal("Unsupported or duplicate connection.", status);
        Assert.Equal(WorkshopFaultKind.Rejection, diagnostic.Kind);
        Assert.Equal(typeof(WorkshopEditRejectedException).FullName, diagnostic.ExceptionType);
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
    public void ARuntimeFailureIsAWholeTypedFaultAndAFaultStatus(string name)
    {
        // Thrown by the runtime itself, so the exception carries a stack trace for the log line to include.
        var error = Record.Exception(RuntimeDefects[name]);
        Assert.NotNull(error);
        var (status, diagnostic) = Report(error);
        Assert.Equal($"Unexpected fault ({error.GetType().Name}): {error.Message} It has been logged.", status);
        Assert.Equal(WorkshopFaultKind.Fault, diagnostic.Kind);
        Assert.Equal(FaultSite.UiHandler, diagnostic.Site);
        Assert.Equal(error.GetType().FullName, diagnostic.ExceptionType);
        Assert.Equal(error.ToString(), diagnostic.Detail);
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
            if (Invokes(root, "diagnostics", "Rejection"))
                violations.Add($"{Path.GetFileName(path)}: logs a rejection without WorkshopUiFailure.Report");
            foreach (var clause in root.DescendantNodes().OfType<CatchClauseSyntax>().Where(clause => !CatchReports(clause)))
                violations.Add($"{Path.GetFileName(path)}:{clause.GetLocation().GetLineSpan().StartLinePosition.Line + 1}: {clause.CatchKeyword} neither reports nor rethrows");
        }
        // Every violation in full (an assertion on the collection truncates each entry).
        if (violations.Count > 0) Assert.Fail($"{violations.Count} UI catch violation(s):\n" + string.Join('\n', violations));
    }

    // A catch reports when it classifies (WorkshopUiFailure.Report), records a fault (Diagnostics.Fault) or rethrows.
    // Story 9R.0c's handler boundary also reports: it passes the caught exception to the same-type HandlerFailed, or logs
    // it whole with GD.PushError. Either one counts only when the caught exception itself is among its arguments.
    private static bool CatchReports(CatchClauseSyntax clause)
    {
        var caught = clause.Declaration?.Identifier.ValueText;
        bool PassesCaught(InvocationExpressionSyntax invocation) => !string.IsNullOrEmpty(caught) && invocation.ArgumentList.Arguments
            .Any(argument => argument.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(name => name.Identifier.ValueText == caught));
        var invocations = clause.Block.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        return Invokes(clause.Block, "WorkshopUiFailure", "Report") || Invokes(clause.Block, "Diagnostics", "Fault")
            || clause.Block.DescendantNodes().OfType<ThrowStatementSyntax>().Any(statement => statement.Expression is null)
            || invocations.Any(invocation => invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "HandlerFailed" } && PassesCaught(invocation))
            || invocations.Any(invocation => invocation.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "GD" }, Name.Identifier.ValueText: "PushError" }
                && PassesCaught(invocation));
    }

    [Theory]
    [InlineData("try { } catch (Exception error) { HandlerFailed(nameof(M), error); }", true)]
    [InlineData("try { } catch (Exception error) { GD.PushError($\"failed: {error}\"); }", true)]
    [InlineData("try { } catch (Exception error) { Diagnostics.Fault(FaultSite.UiHandler, error); }", true)]
    [InlineData("try { } catch (Exception error) { _status.Text = WorkshopUiFailure.Report(error); }", true)]
    [InlineData("try { } catch (Exception) { throw; }", true)]
    [InlineData("try { } catch (Exception error) { HandlerFailed(nameof(M), other); }", false)]
    [InlineData("try { } catch (Exception error) { GD.PushError(\"failed\"); }", false)]
    [InlineData("try { } catch (Exception) { GD.PushError(\"failed\"); }", false)]
    [InlineData("try { } catch (Exception error) { GD.PushWarning($\"{error}\"); }", false)]
    [InlineData("try { } catch (Exception error) { Other.HandlerFailed(nameof(M), error); }", false)]
    [InlineData("try { } catch (Exception error) { _status.Text = error.Message; }", false)]
    public void TheCatchRuleAcceptsOnlyAReportOrTheHandlerBoundaryPassingTheCaughtException(string body, bool reports)
    {
        var cancellation = TestContext.Current.CancellationToken;
        var root = CSharpSyntaxTree.ParseText($"class Workshop {{ void M() {{ {body} }} }}", cancellationToken: cancellation).GetRoot(cancellation);
        Assert.Equal(reports, CatchReports(Assert.Single(root.DescendantNodes().OfType<CatchClauseSyntax>())));
    }
}
