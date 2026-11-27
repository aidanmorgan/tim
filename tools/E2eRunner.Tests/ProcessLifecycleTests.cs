using System.ComponentModel;
using Xunit;

namespace CuriousContraptions.E2eRunner.Tests;

/// <summary>The one stop (L3), the output sink (L7) and the preview's stop over a scripted handle (L9), without the clock.</summary>
public sealed class ProcessLifecycleTests
{
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(5);

    public enum KillFault { None, Aggregate, InvalidOperation, Win32, IO }

    private static Exception? Fault(KillFault kind) => kind switch
    {
        KillFault.None => null,
        KillFault.Aggregate => new AggregateException(new InvalidOperationException("a descendant could not be killed")),
        KillFault.InvalidOperation => new InvalidOperationException("no process is associated with this object"),
        KillFault.Win32 => new Win32Exception(1, "operation not permitted"),
        KillFault.IO => new IOException("the signal could not be sent"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown kill fault."),
    };

    // L3: whatever the kill does, the stop signals the tree exactly once and then waits exactly once, for the grace and with no
    // cancellation, for the root's exit; a kill that throws is recorded in the detail, never rethrown. Control: a clean kill whose
    // root ends. Boundaries: each of the four kill exceptions, and a root the kill does not end (StillRunning, still one wait).
    [Theory]
    [InlineData(KillFault.None, true)]
    [InlineData(KillFault.Aggregate, true)]
    [InlineData(KillFault.InvalidOperation, true)]
    [InlineData(KillFault.Win32, true)]
    [InlineData(KillFault.IO, true)]
    [InlineData(KillFault.None, false)]
    public async Task StopKillsOnceAndWaitsOneGrace(KillFault kind, bool rootEnds)
    {
        var fault = Fault(kind);
        using var child = new FakeChild([rootEnds ? RootAnswer.Exited : RootAnswer.AtLimit]) { KillFault = fault };
        var outcome = await ProcessLifecycle.StopAsync(child, Grace);
        var detail = fault is null ? "its process tree was killed" : $"the kill did not complete ({fault.Message})";
        Assert.Equal(new StopOutcome(rootEnds ? RootState.Ended : RootState.StillRunning, detail), outcome);
        Assert.Equal(["kill tree", "root wait"], child.Events);
        Assert.Equal([Grace], child.RootWaits);
        Assert.Equal([false], child.RootWaitsCancellable);
    }

    // L7: lines are rebuilt across chunks with one trailing \r dropped, a stream's partial line at its end of file is written as a
    // line, and each raw line is announced with its stream. Control: whole lines in one chunk.
    [Fact]
    public void TheSinkRebuildsLinesAcrossChunks()
    {
        using var writer = new StringWriter();
        var sink = new OutputSink(writer, "p ");
        var announced = new List<(OutputStream, string)>();
        sink.LineWritten += (stream, line) => announced.Add((stream, line));
        sink.Write(OutputStream.Standard, "one\ntwo\n");
        sink.Write(OutputStream.Standard, "thr");
        sink.Write(OutputStream.Standard, "ee\r\nfo");
        sink.Write(OutputStream.Error, "partial");
        sink.End(OutputStream.Error);
        Assert.Equal(["p one", "p two", "p three", "p partial"], Lines(writer));
        Assert.Equal([(OutputStream.Standard, "one"), (OutputStream.Standard, "two"), (OutputStream.Standard, "three"), (OutputStream.Error, "partial")],
            announced);
    }

    // L7: Close writes a partial line still pending first, as a line, then the runner's lines, which are the log's last lines, and
    // closes the writer; a chunk after Close is dropped and counted, never written to the closed writer.
    [Fact]
    public void TheSinkClosesWithThePendingPartialThenTheRunnerLinesLast()
    {
        using var writer = new StringWriter();
        var sink = new OutputSink(writer, "p ");
        sink.Write(OutputStream.Standard, "whole\npending");
        sink.Close(["E2E_RUNNER one", "E2E_RUNNER two"]);
        sink.Write(OutputStream.Standard, "late\n");
        sink.Close(["E2E_RUNNER again"]);
        Assert.Equal(["p whole", "p pending", "p E2E_RUNNER one", "p E2E_RUNNER two"], Lines(writer));
        Assert.Equal(1, sink.Dropped);
        Assert.Null(sink.Fault);
    }

    // L7: a writer fault (a full disk on the second line) is recorded once; later lines and the runner's lines are dropped and
    // counted; Close still disposes the writer and returns; a dropped line is still announced (the preview's readiness signal).
    [Fact]
    public void TheSinkRecordsAWriterFaultAndNeverThrowsIt()
    {
        using var writer = new FaultingWriter(faultingLine: 2);
        var sink = new OutputSink(writer, "");
        var announced = new List<string>();
        sink.LineWritten += (_, line) => announced.Add(line);
        sink.Write(OutputStream.Standard, "first\nsecond\nthird\n");
        sink.Close(["E2E_RUNNER one", "E2E_RUNNER two"]);
        Assert.Equal(["first"], writer.Lines);
        Assert.Equal(FaultingWriter.Cause, sink.Fault?.Message);
        Assert.Equal(4, sink.Dropped);
        Assert.True(writer.Disposed);
        Assert.Equal(["first", "second", "third"], announced);
    }

    // L9 (fake): a preview root the kill does not end is ProcessNotStopped after one kill and one grace wait, with no drain; its
    // log is closed with the stop line and the still-running line last, and a later line is dropped. A second Stop does nothing.
    [Fact]
    public void APreviewWhoseRootSurvivesTheKillIsNotStopped()
    {
        using var child = new FakeChild([RootAnswer.AtLimit]);
        using var writer = new StringWriter();
        var sink = new OutputSink(writer, "preview[1] ");
        var preview = new PreviewHost(child, sink, new Uri("http://127.0.0.1:1/"), Grace);
        var refusal = Assert.Throws<RunnerException>(preview.Stop);
        Assert.Equal(RunnerError.ProcessNotStopped, refusal.Error);
        Assert.Equal(["kill tree", "root wait"], child.Events);
        Assert.Equal([Grace], child.RootWaits);
        sink.Write(OutputStream.Standard, "late\n");
        Assert.Equal(1, sink.Dropped);
        Assert.Equal(["preview[1] E2E_RUNNER stop (stop requested): its process tree was killed", "preview[1] " + ProcessLifecycle.RootStillRunningLine],
            Lines(writer));
        preview.Stop();
        Assert.Equal(1, child.Kills);
    }

    // L9 control: a preview root that ends is drained for exactly the grace (the preview has no limit), and its log ends with the
    // stop line.
    [Fact]
    public void APreviewStopDrainsForTheGraceAndClosesItsLog()
    {
        using var child = new FakeChild([RootAnswer.Exited], [OutputAnswer.Ended]);
        using var writer = new StringWriter();
        var preview = new PreviewHost(child, new OutputSink(writer, "preview[1] "), new Uri("http://127.0.0.1:1/"), Grace);
        preview.Stop();
        Assert.Equal([Grace], child.OutputWaits);
        Assert.Equal(["preview[1] E2E_RUNNER stop (stop requested): its process tree was killed"], Lines(writer));
    }

    public enum InFlight { SuiteNotStopped, OtherFailure }

    // L9 precedence: stopping the preview while a failure is in flight, a preview root the kill does not end replaces that failure
    // (a process the run owns may still run); with a suite's ProcessNotStopped in flight, one refusal names both.
    [Theory]
    [InlineData(InFlight.SuiteNotStopped)]
    [InlineData(InFlight.OtherFailure)]
    public void APreviewLeftRunningOutranksTheFailureInFlight(InFlight kind)
    {
        using var child = new FakeChild([RootAnswer.AtLimit]);
        using var writer = new StringWriter();
        var preview = new PreviewHost(child, new OutputSink(writer, ""), new Uri("http://127.0.0.1:1/"), Grace);
        Exception inFlight = kind == InFlight.SuiteNotStopped
            ? new RunnerException(RunnerError.ProcessNotStopped, "Suite s could not be stopped")
            : new InvalidOperationException("the summary could not be written");
        var refusal = Assert.Throws<RunnerException>(() => preview.StopAfterFailure(inFlight));
        Assert.Equal(RunnerError.ProcessNotStopped, refusal.Error);
        Assert.Contains("The preview on port 1 was still running", refusal.Message, StringComparison.Ordinal);
        Assert.Equal(kind == InFlight.SuiteNotStopped, refusal.Message.Contains("Suite s could not be stopped", StringComparison.Ordinal));
    }

    // L9 precedence control: any other outcome of the stop (its output could not be read) gives way to the failure in flight, while
    // the same stop on its own is a ProcessOutputFailed refusal.
    [Fact]
    public void APreviewOutputFailureGivesWayToTheFailureInFlight()
    {
        using var alone = new FakeChild([RootAnswer.Exited], [OutputAnswer.Faults]);
        using var aloneLog = new StringWriter();
        var stopped = new PreviewHost(alone, new OutputSink(aloneLog, ""), new Uri("http://127.0.0.1:1/"), Grace);
        Assert.Equal(RunnerError.ProcessOutputFailed, Assert.Throws<RunnerException>(stopped.Stop).Error);

        using var child = new FakeChild([RootAnswer.Exited], [OutputAnswer.Faults]);
        using var writer = new StringWriter();
        var preview = new PreviewHost(child, new OutputSink(writer, ""), new Uri("http://127.0.0.1:1/"), Grace);
        preview.StopAfterFailure(new InvalidOperationException("the summary could not be written"));
        Assert.Equal(1, child.Kills);
    }

    // A writer fault (the log's disk full at the first line, the runner's stop line) reaches the caller after the stop as a typed
    // ProcessOutputFailed naming the cause; the log is still closed. Control: APreviewStopDrainsForTheGraceAndClosesItsLog.
    [Fact]
    public void ALogThatCannotBeRecordedIsAnOutputFailureAfterTheStop()
    {
        using var child = new FakeChild([RootAnswer.Exited], [OutputAnswer.Ended]);
        using var writer = new FaultingWriter(faultingLine: 1);
        var preview = new PreviewHost(child, new OutputSink(writer, ""), new Uri("http://127.0.0.1:1/"), Grace);
        var refusal = Assert.Throws<RunnerException>(preview.Stop);
        Assert.Equal(RunnerError.ProcessOutputFailed, refusal.Error);
        Assert.Contains(FaultingWriter.Cause, refusal.Message, StringComparison.Ordinal);
        Assert.Equal(1, child.Kills);
        Assert.True(writer.Disposed);
    }

    // A log that cannot be opened (its directory is missing) is a typed ProcessOutputFailed naming its path, before any process starts.
    [Fact]
    public void ALogThatCannotBeOpenedIsAnOutputFailure()
    {
        var path = Path.Combine(Path.GetTempPath(), "e2e-missing-" + Guid.NewGuid().ToString("N"), "suite.log");
        var refusal = Assert.Throws<RunnerException>(() => new OutputSink(path, append: false, ""));
        Assert.Equal(RunnerError.ProcessOutputFailed, refusal.Error);
        Assert.Contains(path, refusal.Message, StringComparison.Ordinal);
    }

    private static string[] Lines(StringWriter writer) => writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
}
