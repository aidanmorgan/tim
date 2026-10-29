using CuriousContraptions.Gpu;
using System.Collections.Immutable;
using CuriousContraptions.Simulation;
using W = CuriousContraptions.Wire;
using Google.FlatBuffers;

namespace CuriousContraptions.Tests;

public sealed class WorkshopObservationTests
{
    public enum StartupFailure { None, Bootstrap, Qualification, Initialize }
    [Fact]
    public async Task QueriesFollowCommittedRunPauseStepAndResetWithoutConsumingTheUi()
    {
        using var transport = new WorkshopActivationAnimationTests.NativeClient();
        var client = transport.Client; var port = client.Observation;
        var construction = PuzzleCatalogue.CreateSolution(new("first_principles"), new(2),
            WorkshopCadenceSettings.Default(), new(1), new(1f));
        await transport.Construct(construction);
        var built = port.Query(port.Session, WorkshopObservationQuery.Committed);
        Assert.Equal(WorkshopObservationRefusal.None, built.Refusal);
        Assert.Equal(WorkshopSimulationPhase.Building, built.Value.Phase);
        await client.Execute(WorkshopCommandKind.Run);
        await transport.Advance(3);
        var running = port.Query(port.Session, WorkshopObservationQuery.Committed).Value;
        Assert.Equal(WorkshopSimulationPhase.Running, running.Phase);
        Assert.True(running.Read.Tick.Value > 0);
        Assert.True(client.TryRead(out var delivered));
        Assert.Equal(running.Read.Tick, delivered.Read.Tick);
        await client.Execute(WorkshopCommandKind.Pause);
        Assert.Equal(WorkshopSimulationPhase.Paused, port.Query(port.Session, WorkshopObservationQuery.Committed).Value.Phase);
        var step = await client.Execute(WorkshopCommandKind.Step);
        Assert.Equal(step.Response.Read.Tick, port.Query(port.Session, WorkshopObservationQuery.Committed).Value.Read.Tick);
        await client.Execute(WorkshopCommandKind.Reset);
        var reset = port.Query(port.Session, WorkshopObservationQuery.Committed).Value;
        Assert.Equal(0ul, reset.Read.Tick.Value);
        Assert.Equal(built.Value.Read.Bodies.Count, reset.Read.Bodies.Count);
        for (var i = 0; i < reset.Read.Bodies.Count; i++)
        {
            Assert.Equal(built.Value.Read.Bodies[i].Body.Local, reset.Read.Bodies[i].Body.Local);
            Assert.Equal(built.Value.Read.Bodies[i].Rotation, reset.Read.Bodies[i].Rotation);
        }
        Assert.True(running.Read.Tick.Value > 0); // Retained observations do not turn into Reset state.
        var receipts = port.Receipts(port.Session, default).Value!;
        Assert.Contains(receipts, receipt => receipt.Kind == WorkshopCommandKind.Run && receipt.Result.Outcome == WorkshopCommandOutcome.Applied);
        Assert.Equal(WorkshopCommandKind.Reset, receipts[^1].Kind);
        Assert.Equal(port.Session, receipts[^1].Session);
        var saved = await client.Execute(WorkshopCommandKind.Save);
        Assert.Equal(WorkshopCommandOutcome.Applied, saved.Response.Result.Outcome);
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements,
            WorkshopSaveCodec.Encode(new(construction, new(1000))));
        await transport.Construct(loaded.Construction);
        var restored = port.Query(port.Session, WorkshopObservationQuery.Committed).Value.Read;
        Assert.Equal(reset.Read.Bodies.Count, restored.Bodies.Count);
        for (var i = 0; i < restored.Bodies.Count; i++)
        {
            Assert.Equal(reset.Read.Bodies[i].Body.Id, restored.Bodies[i].Body.Id);
            Assert.Equal(reset.Read.Bodies[i].Body.Local, restored.Bodies[i].Body.Local);
            Assert.Equal(reset.Read.Bodies[i].Rotation, restored.Bodies[i].Rotation);
        }
        Assert.Contains(port.Receipts(port.Session, receipts[^1].Ordinal).Value!, receipt =>
            receipt.Kind == WorkshopCommandKind.Save && receipt.Result.Outcome == WorkshopCommandOutcome.Applied);
    }

    [Fact]
    public async Task RejectedCommandsPreserveTheQueryAndReceiptGrowthPreservesEveryCompletion()
    {
        using var transport = new WorkshopActivationAnimationTests.NativeClient();
        var port = transport.Client.Observation;
        var before = port.Query(port.Session, WorkshopObservationQuery.Committed).Value;
        for (var i = 0; i < 70; i++)
        {
            var delivery = await transport.Client.Execute(WorkshopCommandKind.Resume);
            Assert.Equal(WorkshopCommandOutcome.Rejected, delivery.Response.Result.Outcome);
        }
        var receipts = port.Receipts(port.Session, default).Value!;
        Assert.Equal(71, receipts.Length); // Initialize, then seventy refused controls.
        Assert.Equal(WorkshopCommandKind.Initialize, receipts[0].Kind);
        Assert.All(receipts.Skip(1), receipt => Assert.Equal(WorkshopCommandKind.Resume, receipt.Kind));
        Assert.Equal(1ul, receipts[0].Ordinal.Value);
        Assert.Equal(71ul, receipts[^1].Ordinal.Value);
        Assert.Empty(port.Receipts(port.Session, receipts[^1].Ordinal).Value!);
        receipts[0] = default;
        Assert.Equal(WorkshopCommandKind.Initialize, port.Receipts(port.Session, default).Value![0].Kind);
        var after = port.Query(port.Session, WorkshopObservationQuery.Committed).Value;
        Assert.Equal(before.Read.Epoch, after.Read.Epoch);
        Assert.Equal(before.Read.Revision, after.Read.Revision);
        Assert.Equal(before.Read.Tick, after.Read.Tick);
    }

    [Fact]
    public async Task ADelayedLowerSequenceRemainsVisibleAfterTheLaterReceiptWasRead()
    {
        using var transport = new WorkshopActivationAnimationTests.NativeClient();
        var port = transport.Client.Observation;
        var initial = port.Receipts(port.Session, default).Value![^1].Ordinal;
        transport.HoldNextAcknowledgement = true;
        var earlier = transport.Client.Execute(WorkshopCommandKind.Save);
        Assert.False(earlier.IsCompleted);
        await transport.Client.Execute(WorkshopCommandKind.Resume);
        var first = Assert.Single(port.Receipts(port.Session, initial).Value!);
        Assert.Equal(WorkshopCommandKind.Resume, first.Kind);
        transport.ReleaseAcknowledgement();
        await earlier;
        var late = Assert.Single(port.Receipts(port.Session, first.Ordinal).Value!);
        Assert.Equal(WorkshopCommandKind.Save, late.Kind);
        Assert.True(late.Sequence.Value < first.Sequence.Value);
        Assert.True(late.Ordinal.Value > first.Ordinal.Value);
        Assert.Empty(port.Receipts(port.Session, late.Ordinal).Value!);
    }

    [Fact]
    public void ForeignUnknownClosedAndRetiredAccessHasTypedRefusals()
    {
        var transport = new WorkshopActivationAnimationTests.NativeClient();
        var port = transport.Client.Observation;
        Assert.Equal(WorkshopObservationRefusal.ForeignSession, port.Query(new(3, 4), WorkshopObservationQuery.Committed).Refusal);
        Assert.Equal(WorkshopObservationRefusal.UnknownQuery, port.Query(port.Session, (WorkshopObservationQuery)99).Refusal);
        var cursors = Enumerable.Range(0, 40).Select(_ => port.OpenEvents(port.Session).Value).ToArray();
        Assert.Equal(40, cursors.Distinct().Count());
        foreach (var cursor in cursors)
        {
            Assert.Equal(WorkshopObservationRefusal.None, port.ReadEvents(cursor).Refusal);
            Assert.Equal(WorkshopObservationRefusal.None, port.CloseEvents(cursor));
            Assert.Equal(WorkshopObservationRefusal.ClosedCursor, port.ReadEvents(cursor).Refusal);
        }
        transport.Dispose();
        Assert.Equal(WorkshopObservationRefusal.Retired, port.Query(port.Session, WorkshopObservationQuery.Committed).Refusal);
        Assert.Equal(WorkshopObservationRefusal.Retired, port.OpenEvents(port.Session).Refusal);
        Assert.NotNull(port.Terminal);
        Assert.Contains("disposed", port.Terminal.Value.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkerDiagnosticsAreDecodedBeforePublicationAndGrowWithoutLosingFaultDetail()
    {
        using var transport = new WorkshopActivationAnimationTests.NativeClient();
        var client = transport.Client; var port = client.Observation;
        var after = port.Diagnostics(port.Session, default).Value![^1].Ordinal;
        var source = new WorkshopDiagnostics();
        for (var i = 0; i < 70; i++) source.Fault(FaultSite.Advance, new InvalidOperationException("fault " + i));
        var bytes = WorkshopWireFlatBuffers.EncodeObservations(new(port.Session, WorkshopRuntimeRole.Simulation, source.Snapshot().ToImmutableArray()));
        transport.DeliverObservation(bytes);
        bytes.AsSpan().Clear();
        var records = port.Diagnostics(port.Session, after).Value!;
        Assert.Equal(70, records.Length);
        for (var i = 0; i < records.Length; i++)
        {
            Assert.Equal(after.Value + (ulong)i + 1, records[i].Ordinal.Value);
            var fault = Assert.IsType<WorkshopFaultDiagnostic>(records[i].Record);
            Assert.Equal(WorkshopFaultKind.Fault, fault.Kind);
            Assert.Equal(FaultSite.Advance, fault.Site);
            Assert.Equal(typeof(InvalidOperationException).FullName, fault.ExceptionType);
            Assert.Contains("fault " + i, fault.Detail, StringComparison.Ordinal);
        }
        records[0] = default;
        Assert.IsType<WorkshopFaultDiagnostic>(port.Diagnostics(port.Session, after).Value![0].Record);
        Assert.Empty(port.Diagnostics(port.Session, records[^1].Ordinal).Value!);
        Assert.Equal(WorkshopObservationRefusal.InvalidCursor, port.Diagnostics(port.Session, new(ulong.MaxValue)).Refusal);
        Assert.Equal(WorkshopObservationRefusal.ForeignSession, port.Diagnostics(new(7, 9), default).Refusal);
    }

    [Fact]
    public async Task RealTraceCaptureCrossesTheTypedTransportAsDecodedBodiesAndTerminalStatus()
    {
        using var transport = new WorkshopActivationAnimationTests.NativeClient();
        var construction = PuzzleCatalogue.CreateSolution(new("first_principles"), new(2), WorkshopCadenceSettings.Default(), new(1), new(1f));
        await transport.Construct(construction);
        var port = transport.Client.Observation;
        var before = port.Query(port.Session, WorkshopObservationQuery.Committed).Value;
        var trace = new WorkshopTrace();
        var profile = new WorkshopGpuProfile(construction.Settings.Simulation, construction.Settings.Physical, before.Cadence);
        trace.Begin(port.Session, construction, before.Read.Epoch, before.Projection, profile);
        trace.Append(WorkshopTraceRecord.Encode(before.Read, profile));
        trace.End(TraceEnd.Reset); trace.Flush(WorkshopSimulationPhase.Building);
        var bytes = WorkshopWireFlatBuffers.EncodeObservations(new(port.Session, WorkshopRuntimeRole.Simulation, trace.Diagnostics.Snapshot().ToImmutableArray()));
        transport.DeliverObservation(bytes);
        var records = port.Diagnostics(port.Session, default).Value!.Select(receipt => receipt.Record).OfType<WorkshopCaptureDiagnostic>().ToArray();
        var begin = Assert.Single(records, record => record.Kind == WorkshopCaptureKind.Pose && record.Stage == WorkshopCaptureStage.Begin);
        Assert.Equal(construction, begin.Construction!.Value.Construction);
        Assert.Equal(port.Session, begin.Construction.Value.Session);
        Assert.Equal(TraceEnd.Reset, begin.End); Assert.Equal(TraceTimingStatus.Incomplete, begin.Status);
        var chunk = Assert.Single(records, record => record.Kind == WorkshopCaptureKind.Pose && record.Stage == WorkshopCaptureStage.Chunk);
        var sample = Assert.Single(chunk.Samples);
        Assert.Equal(before.Read.Epoch, sample.Epoch); Assert.Equal(before.Read.Tick, sample.Tick);
        Assert.Equal(before.Read.Bodies.Count, sample.Bodies.Count);
        for (var i = 0; i < sample.Bodies.Count; i++)
        {
            Assert.Equal(before.Read.Bodies[i].Body, sample.Bodies[i].Body);
            Assert.Equal(before.Read.Bodies[i].Rotation, sample.Bodies[i].Rotation);
        }
        Assert.Contains(records, record => record.Kind == WorkshopCaptureKind.Timing && record.Stage == WorkshopCaptureStage.End &&
            record.Status == TraceTimingStatus.Incomplete && record.Count == 0);
    }

    [Fact]
    public async Task ForeignOrMalformedDiagnosticTransportRetiresOnlyObservationAndKeepsGameplayCommitted()
    {
        using var transport = new WorkshopActivationAnimationTests.NativeClient();
        var client = transport.Client; var port = client.Observation;
        var before = port.Query(port.Session, WorkshopObservationQuery.Committed).Value;
        var foreign = WorkshopWireFlatBuffers.EncodeObservations(new(new(9, 11), WorkshopRuntimeRole.Animation,
            [new AnimationDeferralDiagnostic(AnimationDeferralCause.Preparation, 1)]));
        transport.DeliverObservation(foreign);
        Assert.Equal(WorkshopObservationRefusal.Retired, port.Query(port.Session, WorkshopObservationQuery.Committed).Refusal);
        Assert.Contains("Foreign observation", port.Terminal!.Value.Detail, StringComparison.Ordinal);
        var reply = await client.Execute(WorkshopCommandKind.Save);
        Assert.Equal(WorkshopCommandOutcome.Applied, reply.Response.Result.Outcome);
        Assert.Equal(before.Read.Epoch, reply.Response.Read.Epoch);
        Assert.Equal(before.Read.Tick, reply.Response.Read.Tick);
        Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeObservations(foreign.AsSpan(0, foreign.Length - 1)));
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeObservations(new(port.Session, WorkshopRuntimeRole.Animation,
            [new AnimationDeferralDiagnostic((AnimationDeferralCause)99, 1)])));
    }

    [Fact]
    public void TransportRetirementImmediatelyRefusesObservationAndKeepsTheFullTerminalExplanation()
    {
        using var transport = new WorkshopActivationAnimationTests.NativeClient();
        var port = transport.Client.Observation;
        var cursor = port.OpenEvents(port.Session).Value;
        transport.RetireTransport("worker terminated: complete external reason");
        Assert.Equal(WorkshopObservationRefusal.Retired, port.ReadEvents(cursor).Refusal);
        Assert.Equal(WorkshopObservationRefusal.Retired, port.Receipts(port.Session, default).Refusal);
        Assert.Equal(WorkshopObservationRefusal.Retired, port.Diagnostics(port.Session, default).Refusal);
        Assert.Equal("worker terminated: complete external reason", port.Terminal!.Value.Detail);
    }

    public enum PayloadDefect { Absent, Mismatched, Multiple, Unknown }
    [Theory]
    [InlineData(PayloadDefect.Absent)]
    [InlineData(PayloadDefect.Mismatched)]
    [InlineData(PayloadDefect.Multiple)]
    [InlineData(PayloadDefect.Unknown)]
    public void DiagnosticKindsRequireExactlyTheirMatchingConcretePayload(PayloadDefect defect)
    {
        var builder = new FlatBufferBuilder(256);
        var fault = W.FaultObservation.CreateFaultObservation(builder, (uint)WorkshopFaultKind.Fault, (uint)FaultSite.Advance,
            builder.CreateString("System.InvalidOperationException"), builder.CreateString("cause"));
        var deferred = W.DeferralObservation.CreateDeferralObservation(builder, (uint)AnimationDeferralCause.Preparation, 1);
        var record = W.DiagnosticObservation.CreateDiagnosticObservation(builder,
            defect == PayloadDefect.Unknown ? (W.ObservationPayload)255 : W.ObservationPayload.Fault,
            faultOffset: defect is PayloadDefect.Multiple or PayloadDefect.Unknown ? fault : default,
            deferralOffset: defect is PayloadDefect.Multiple or PayloadDefect.Mismatched ? deferred : default);
        var records = W.WorkshopObservationBatch.CreateRecordsVector(builder, [record]);
        var identity = new byte[16]; new RuntimeSessionId(101, 206).WriteTo(identity);
        var session = W.WorkshopObservationBatch.CreateSessionVector(builder, identity);
        var batch = W.WorkshopObservationBatch.CreateWorkshopObservationBatch(builder, session, (uint)WorkshopRuntimeRole.Simulation, records);
        builder.FinishSizePrefixed(batch.Value, WorkshopWireFlatBuffers.ObservationIdentifier);
        Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeObservations(builder.SizedByteArray()));
    }

    [Fact]
    public async Task TransferDiagnosticsKeepEveryPaidTickAfterLaterCommittedReads()
    {
        using var transport = new WorkshopActivationAnimationTests.NativeClient();
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(WorkshopInput.Generic(PartId.BowlingBall, new(5), 0, 6, 0, 0, 0, 0, 1),
                PlacedParts.Bumper(new(6), 0, 3, 0, 8f), PlacedParts.Battery(new(3), 3, 1, 0)),
            Connections: new(new WorkshopConnection(new(3), WorkshopSocket.Supply, new(6), WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        await transport.Construct(construction);
        var client = transport.Client; var port = client.Observation;
        Assert.False(port.Query(port.Session, WorkshopObservationQuery.Committed).Value.Read.CarriesBalanceTransfer());
        await client.Execute(WorkshopCommandKind.Run);
        var expected = new List<(SimulationTick Tick, float Remaining, float Debit, float Supplied)>();
        for (var tick = 0; tick < 360; tick++)
        {
            await transport.Advance(1);
            var response = port.Query(port.Session, WorkshopObservationQuery.Committed).Value;
            if (!response.Read.CarriesBalanceTransfer()) continue;
            var source = response.Read.Electrical[0];
            expected.Add((response.Read.Tick, source.Remaining.Value, source.Debit.Value, response.Read.ContactWorks[0].SuppliedEnergy.Value));
        }
        Assert.NotEmpty(expected);
        await transport.Advance(2);
        var actual = port.Diagnostics(port.Session, default).Value!.Select(receipt => receipt.Record)
            .OfType<WorkshopTransferDiagnostic>().ToArray();
        Assert.Equal(expected.Count, actual.Length);
        for (var i = 0; i < actual.Length; i++)
        {
            var read = actual[i].Response.Read;
            Assert.Equal(expected[i], (read.Tick, read.Electrical[0].Remaining.Value, read.Electrical[0].Debit.Value, read.ContactWorks[0].SuppliedEnergy.Value));
        }
        Assert.True(port.Query(port.Session, WorkshopObservationQuery.Committed).Value.Read.Tick.Value > actual[^1].Response.Read.Tick.Value);
    }

    [Fact]
    public void EarlyDiscoveryHasNoTransportCursorUntilClientCreationCompletes()
    {
        var journal = new WorkshopDiagnostics();
        var port = new WorkshopObservation(new(101, 206), journal);
        using var transport = new WorkshopActivationAnimationTests.NativeClient(port, journal, beforeReady: () =>
            Assert.Equal(WorkshopObservationRefusal.NotCommitted, port.OpenEvents(port.Session).Refusal));
        Assert.Same(port, transport.Client.Observation);
        var opened = port.OpenEvents(port.Session);
        Assert.Equal(WorkshopObservationRefusal.None, opened.Refusal);
        Assert.Equal(WorkshopObservationRefusal.None, port.CloseEvents(opened.Value));
    }

    [Theory]
    [InlineData(StartupFailure.Bootstrap)]
    [InlineData(StartupFailure.Qualification)]
    [InlineData(StartupFailure.Initialize)]
    public void StartupFailureRetiresTheOriginalSessionWithReadableDetail(StartupFailure failure)
    {
        var diagnostics = new WorkshopDiagnostics();
        var port = new WorkshopObservation(new(101, 206), diagnostics);
        Assert.Equal(WorkshopObservationRefusal.NotCommitted, port.Query(port.Session, WorkshopObservationQuery.Committed).Refusal);
        var error = Record.Exception(() => new WorkshopActivationAnimationTests.NativeClient(port, diagnostics, failure));
        Assert.NotNull(error);
        Assert.Contains(error.Message, port.Terminal!.Value.Detail, StringComparison.Ordinal);
        Assert.Equal(WorkshopObservationRefusal.Retired, port.Query(port.Session, WorkshopObservationQuery.Committed).Refusal);
        Assert.Contains(diagnostics.Snapshot(), record => record is WorkshopFaultDiagnostic { Site: FaultSite.Startup });
    }

    [Fact]
    public async Task ARecoveredTransferPublicationReportsTheLostRecordAndKeepsTheSimulationRunning()
    {
        using var transport = new WorkshopActivationAnimationTests.NativeClient();
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(WorkshopInput.Generic(PartId.BowlingBall, new(5), 0, 6, 0, 0, 0, 0, 1),
                PlacedParts.Bumper(new(6), 0, 3, 0, 8f), PlacedParts.Battery(new(3), 3, 1, 0)),
            Connections: new(new WorkshopConnection(new(3), WorkshopSocket.Supply, new(6), WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        await transport.Construct(construction);
        var port = transport.Client.Observation;
        var first = true;
        transport.DiagnosticDelivery = bytes =>
        {
            if (first) { first = false; throw new System.IO.IOException("transfer output interrupted"); }
            transport.DeliverObservation(bytes);
        };
        await transport.Client.Execute(WorkshopCommandKind.Run);
        await transport.Advance(360);
        Assert.Contains("BalanceTransfer", port.Terminal!.Value.Detail, StringComparison.Ordinal);
        Assert.Contains("transfer output interrupted", port.Terminal.Value.Detail, StringComparison.Ordinal);
        var terminal = port.Terminal;
        await transport.Advance(2);
        Assert.Equal(terminal, port.Terminal);
        var paused = await transport.Client.Execute(WorkshopCommandKind.Pause);
        Assert.Equal(WorkshopCommandOutcome.Applied, paused.Response.Result.Outcome);
        Assert.Equal(WorkshopSimulationPhase.Paused, paused.Response.Phase);
        Assert.True(paused.Response.Read.Tick.Value > 0);
    }

    [Fact]
    public async Task ARecoveredWorkerPublicationKeepsDroppedCaptureDetailVisibleWithoutFaultingGameplay()
    {
        using var transport = new WorkshopActivationAnimationTests.NativeClient();
        var port = transport.Client.Observation;
        var construction = PuzzleCatalogue.CreateSolution(new("first_principles"), new(2), WorkshopCadenceSettings.Default(), new(1), new(1f));
        await transport.Construct(construction);
        var before = port.Query(port.Session, WorkshopObservationQuery.Committed).Value;
        var fail = true;
        WorkshopDiagnostics producer = null!;
        producer = new(record => producer.Publish(record, port.Session, WorkshopRuntimeRole.Simulation, bytes =>
        {
            if (fail) { fail = false; throw new System.IO.IOException("capture destination interrupted"); }
            transport.DeliverObservation(bytes);
        }));
        var trace = new WorkshopTrace(producer);
        var profile = new WorkshopGpuProfile(construction.Settings.Simulation, construction.Settings.Physical, before.Cadence);
        trace.Begin(port.Session, construction, before.Read.Epoch, before.Projection, profile);
        trace.Append(WorkshopTraceRecord.Encode(before.Read, profile)); trace.End(TraceEnd.Reset);
        trace.Flush(WorkshopSimulationPhase.Building);
        Assert.Contains("Capture", port.Terminal!.Value.Detail, StringComparison.Ordinal);
        Assert.Contains("System.IO.IOException: capture destination interrupted", port.Terminal.Value.Detail, StringComparison.Ordinal);
        var retained = port.Terminal;
        producer.Fault(FaultSite.TraceFlush, new InvalidOperationException("later recovered diagnostic"));
        Assert.Equal(retained, port.Terminal);
        var save = await transport.Client.Execute(WorkshopCommandKind.Save);
        Assert.Equal(WorkshopCommandOutcome.Applied, save.Response.Result.Outcome);
        Assert.Equal(before.Read.Epoch, save.Response.Read.Epoch);
    }

    [Theory]
    [InlineData(TraceEnd.Complete, 10, true)]
    [InlineData(TraceEnd.Reset, 10, false)]
    [InlineData(TraceEnd.Complete, 9, false)]
    public void CompleteCaptureTerminalMetadataMustAgreeAtBothCodecBoundaries(TraceEnd end, int count, bool admitted)
    {
        var profile = new WorkshopGpuProfile(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
        var diagnostic = new WorkshopCaptureDiagnostic(WorkshopCaptureKind.Timing, WorkshopCaptureStage.End, 1, new(1), profile,
            end, TraceTimingStatus.Complete, 10, count, 0, 1, 0, null, [], []);
        var batch = new WorkshopObservationBatch(new(101, 206), WorkshopRuntimeRole.Simulation, [diagnostic]);
        if (admitted)
        {
            var decoded = Assert.IsType<WorkshopCaptureDiagnostic>(Assert.Single(WorkshopWireFlatBuffers.DecodeObservations(WorkshopWireFlatBuffers.EncodeObservations(batch)).Records));
            Assert.Equal(TraceTimingStatus.Complete, decoded.Status); Assert.Equal(end, decoded.End);
            Assert.Equal(count, decoded.Count); Assert.Equal(10, decoded.ExpectedCount);
        }
        else Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeObservations(batch));
        var builder = new FlatBufferBuilder(256);
        var capture = W.CaptureObservation.CreateCaptureObservation(builder, (uint)WorkshopCaptureKind.Timing, (uint)WorkshopCaptureStage.End,
            1, 1, (uint)profile.Cadence, (uint)profile.Physical, 1, (uint)end, (uint)TraceTimingStatus.Complete, 10, count,
            samplesOffset: W.CaptureObservation.CreateSamplesVector(builder, []),
            millisecondsOffset: W.CaptureObservation.CreateMillisecondsVector(builder, []));
        var record = W.DiagnosticObservation.CreateDiagnosticObservation(builder, W.ObservationPayload.Capture, captureOffset: capture);
        var records = W.WorkshopObservationBatch.CreateRecordsVector(builder, [record]);
        var identity = new byte[16]; batch.Session.WriteTo(identity);
        var root = W.WorkshopObservationBatch.CreateWorkshopObservationBatch(builder,
            W.WorkshopObservationBatch.CreateSessionVector(builder, identity), (uint)batch.Role, records);
        builder.FinishSizePrefixed(root.Value, WorkshopWireFlatBuffers.ObservationIdentifier);
        if (admitted) Assert.Equal(count, Assert.IsType<WorkshopCaptureDiagnostic>(Assert.Single(WorkshopWireFlatBuffers.DecodeObservations(builder.SizedByteArray()).Records)).Count);
        else Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeObservations(builder.SizedByteArray()));
    }

    [Fact]
    public void CompleteCaptureChunksUseLocalCountsWithoutPretendingTheyAreTerminalTotals()
    {
        var diagnostic = new WorkshopCaptureDiagnostic(WorkshopCaptureKind.Timing, WorkshopCaptureStage.Chunk, 1, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)), TraceEnd.Complete, TraceTimingStatus.Complete,
            10, 1, 0, 10, 1, null, [], [0.5]);
        var value = Assert.IsType<WorkshopCaptureDiagnostic>(Assert.Single(WorkshopWireFlatBuffers.DecodeObservations(
            WorkshopWireFlatBuffers.EncodeObservations(new(new(101, 206), WorkshopRuntimeRole.Simulation, [diagnostic]))).Records));
        Assert.Equal(10, value.ExpectedCount); Assert.Equal(1, value.Count); Assert.Equal(0.5, Assert.Single(value.Milliseconds));
    }

    [Fact]
    public void ProductionConstructionProjectionIncludesPuzzleConfigurationAndConnectionDomains()
    {
        var construction = PuzzleCatalogue.CreateSolution(new("first_principles"), new(2), WorkshopCadenceSettings.Default(), new(1), new(0.45f));
        var command = new WorkshopCommand(new(7), WorkshopCommandKind.Construct, new(2), new(3), construction, Session: new(101, 206));
        using var dto = WorkshopObservationProjection.Construction<ExternalObject>(command);
        Assert.Equal("7", dto.Fields["sequence"]); Assert.Equal((int)WorkshopCommandKind.Construct, dto.Fields["kind"]);
        var puzzle = Assert.IsType<ExternalObject>(dto.Fields["puzzle"]);
        Assert.Equal("first_principles", puzzle.Fields["id"]); Assert.Equal(0.45f, puzzle.Fields["precision"]);
        var goal = Assert.IsType<ExternalObject>(puzzle.Fields["goal"]);
        Assert.Equal((int)construction.Puzzle.Goal.Kind, goal.Fields["kind"]);
        Assert.Equal(construction.Puzzle.Goal.Target.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), goal.Fields["target"]);
        var balanced = Assert.IsType<ExternalObject>(Assert.IsType<ExternalObject>(puzzle.Fields["primary"]).Fields["balanced"]);
        Assert.Equal(construction.Puzzle.PrimaryAssistance.Balanced.CaptureMargin.Value, balanced.Fields["captureMargin"]);
        Assert.Equal(11, balanced.Fields.Count);
        var connected = construction with { Connections = new(new WorkshopConnection(new(1), WorkshopSocket.ActivationOut,
            new(2), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)) };
        using var wired = WorkshopObservationProjection.Construction<ExternalObject>(command with { Construction = connected });
        var connection = Assert.Single(Assert.IsType<ExternalObject>(wired.Fields["connections"]).Items);
        Assert.Equal((int)WorkshopConnectionDomain.Activation, connection.Fields["domain"]);
        Assert.Equal((int)WorkshopSocket.ActivationOut, connection.Fields["output"]);
        Assert.Equal((int)WorkshopSocket.ActivationIn, connection.Fields["input"]);
        Assert.Null(WorkshopObservationProjection.OccurrenceKind(default));
        Assert.Throws<ArgumentException>(() => WorkshopObservationProjection.OccurrenceKind(new((ActivationOccurrenceKind)99, new(1), default, default)));
        Assert.Equal(7, WorkshopObservationProjection.EnumValue(WorkshopRejection.InvalidRead));
        Assert.Throws<ArgumentException>(() => WorkshopObservationProjection.EnumValue((WorkshopRejection)6));
        Assert.Throws<ArgumentException>(() => WorkshopObservationProjection.EnumValue((WorkshopConnectionDomain)99));
    }


    [Fact]
    public async Task LocalJournalFailureReleasesEveryCursorAndPreservesGameplay()
    {
        var diagnostics = new WorkshopDiagnostics();
        using var transport = new WorkshopActivationAnimationTests.NativeClient(diagnostics: diagnostics);
        var port = transport.Client.Observation;
        var first = port.OpenEvents(port.Session).Value;
        port.OpenEvents(port.Session);
        Assert.Equal(2, transport.OpenObserverCount);
        diagnostics.Capture(() => throw new InvalidOperationException("local capture unavailable"));
        Assert.Equal(0, transport.OpenObserverCount);
        Assert.Contains("local capture unavailable", port.Terminal!.Value.Detail, StringComparison.Ordinal);
        Assert.Equal(WorkshopObservationRefusal.Retired, port.CloseEvents(first));
        Assert.Equal(WorkshopObservationRefusal.Retired, port.Query(port.Session, WorkshopObservationQuery.Committed).Refusal);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await transport.Client.Execute(WorkshopCommandKind.Save)).Response.Result.Outcome);
    }

    [Fact]
    public async Task TwoManagedCursorsObserveCausalImpactInEachAdmittedRun()
    {
        using var transport = new WorkshopActivationAnimationTests.NativeClient();
        var client = transport.Client; var port = client.Observation;
        await transport.Construct(new(new(2), WorkshopCadenceSettings.Default(),
            new(WorkshopInput.Generic(PartId.BowlingBall, new(5), 0, 6, 0, 0, 0, 0, 1),
                PlacedParts.Bumper(new(6), 0, 3, 0, 8f), PlacedParts.Battery(new(3), 3, 1, 0)),
            Connections: new(new WorkshopConnection(new(3), WorkshopSocket.Supply, new(6), WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical))));
        var first = port.OpenEvents(port.Session).Value; var second = port.OpenEvents(port.Session).Value;
        SimulationEpoch previous = default;
        for (var run = 0; run < 2; run++)
        {
            await client.Execute(WorkshopCommandKind.Run);
            await transport.Advance(360); // Three game seconds allow the falling ball to reach the bumper.
            var committed = port.Query(port.Session, WorkshopObservationQuery.Committed).Value.Read;
            Assert.NotEqual(previous, committed.Epoch);
            foreach (var cursor in new[] { first, second })
            {
                var delivered = port.ReadEvents(cursor).Value!;
                Assert.Equal(committed.Epoch.Value, delivered.Epoch);
                Assert.Contains(delivered.Records, record => record.Kind == CuriousContraptions.Abstractions.SimulationEventKind.Impact &&
                    record.Source == 6 && record.Other == 5 && record.Tick > 0 && record.Tick <= committed.Tick.Value);
                Assert.Empty(port.ReadEvents(cursor).Value!.Records);
            }
            previous = committed.Epoch;
            await client.Execute(WorkshopCommandKind.Reset);
            foreach (var cursor in new[] { first, second })
            {
                var reset = port.ReadEvents(cursor).Value!;
                Assert.Equal(port.Query(port.Session, WorkshopObservationQuery.Committed).Value.Read.Epoch.Value, reset.Epoch);
                Assert.DoesNotContain(reset.Records, record => record.Kind == CuriousContraptions.Abstractions.SimulationEventKind.Impact);
            }
        }
    }

    [Fact]
    public async Task ProductionResponseAndDiagnosticProjectionPreserveObservedStateAndExactIdentities()
    {
        using var transport = new WorkshopActivationAnimationTests.NativeClient();
        await transport.Construct(PuzzleCatalogue.CreateSolution(new("first_principles"), new(2), WorkshopCadenceSettings.Default(), new(1), new(1f)));
        var response = transport.Client.Observation.Query(transport.Client.Observation.Session, WorkshopObservationQuery.Committed).Value;
        var read = response.Read with { Epoch = new(9007199254740993), Tick = new(9007199254740995),
            Captures = new([new(new(9007199254740997), new(9007199254740999), CaptureLatchPhase.Latched, 23, .5f)]) };
        using var projected = WorkshopObservationProjection.Response<ExternalObject>(response with { Read = read, Sequence = new(ulong.MaxValue) });
        Assert.Equal("18446744073709551615", projected.Fields["sequence"]);
        Assert.Equal("9007199254740993", projected.Fields["epoch"]);
        Assert.Equal((int)WorkshopSimulationPhase.Building, projected.Fields["phase"]);
        var capture = Assert.Single(Assert.IsType<ExternalObject>(projected.Fields["captures"]).Items);
        Assert.Equal("9007199254740997", capture.Fields["sensor"]);
        Assert.Equal((int)CaptureLatchPhase.Latched, capture.Fields["phase"]);
        Assert.Equal(23u, capture.Fields["ordinal"]);
        Assert.Equal(.5f, capture.Fields["eventPhase"]);
        var body = Assert.IsType<ExternalObject>(projected.Fields["bodies"]).Items[0];
        Assert.Equal(read.Bodies[0].Body.Local.X, Assert.IsType<ExternalObject>(body.Fields["local"]).Fields["x"]);
        var sample = new WorkshopTraceSample(read.Epoch, read.Tick, read.Bodies, read.Captures);
        var record = new WorkshopCaptureDiagnostic(WorkshopCaptureKind.Pose, WorkshopCaptureStage.Chunk, ulong.MaxValue, read.Epoch,
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)), TraceEnd.Reset, TraceTimingStatus.Incomplete,
            1, 1, 0, 1, 0, null, [sample], []);
        using var diagnostic = WorkshopObservationProjection.Diagnostic<ExternalObject>(record);
        Assert.Equal("18446744073709551615", diagnostic.Fields["identity"]);
        var projectedSample = Assert.Single(Assert.IsType<ExternalObject>(diagnostic.Fields["samples"]).Items);
        Assert.Equal(["bodies", "captures", "epoch", "tick"], projectedSample.Fields.Keys.Order().ToArray());
        Assert.Equal("9007199254740995", projectedSample.Fields["tick"]);
        var clock = new ClockLateRenewalDiagnostic(WorkshopRuntimeRole.Simulation, ulong.MaxValue, 7, 2, new(12),
            new(-9007199254740999, -9007199254740993), new(9007199254740993, 9007199254740999), null, ulong.MaxValue);
        using var clockDto = WorkshopObservationProjection.Diagnostic<ExternalObject>(clock);
        Assert.Equal("-9007199254740999", Assert.IsType<ExternalObject>(clockDto.Fields["retained"]).Fields["lower"]);
        Assert.Equal("9007199254740999", Assert.IsType<ExternalObject>(clockDto.Fields["fresh"]).Fields["upper"]);
        Assert.Equal("18446744073709551615", clockDto.Fields["count"]);
    }

    private sealed class ExternalObject : IWorkshopObservationObject<ExternalObject>
    {
        public Dictionary<string, object> Fields { get; } = new();
        public List<ExternalObject> Items { get; } = [];
        public static ExternalObject Object() => new();
        public static ExternalObject Array() => new();
        public void Push(ExternalObject value) => Items.Add(value);
        public void SetProperty(string name, ExternalObject value) => Fields.Add(name, value);
        public void SetProperty(string name, string value) => Fields.Add(name, value);
        public void SetProperty(string name, bool value) => Fields.Add(name, value);
        public void SetProperty(string name, int value) => Fields.Add(name, value);
        public void SetProperty(string name, uint value) => Fields.Add(name, value);
        public void SetProperty(string name, float value) => Fields.Add(name, value);
        public void SetMilliseconds(double value) => Fields.Add("milliseconds", value);
        public void Dispose() { }
    }
}
