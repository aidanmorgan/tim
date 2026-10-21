using System.Buffers.Binary;
using System.Text.RegularExpressions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.3: transport controls are typed commands and queries. The Simulation worker detects a reliable-read stall in
/// its own receipt accounting (<see cref="WorkshopReliableReadLedger"/>), memory observation is the typed
/// <see cref="WorkshopCommandKind.ObserveMemory"/> query answered by an acknowledgement on the command transport, and an unknown
/// command or worker envelope is refused with a typed reason. The raw stall flag and the raw memory channel are deleted.</summary>
public sealed class TypedTransportCommandTests
{
    private static readonly RuntimeSessionId Session = WireFaultTests.Session;
    private static MasterTimeNanoseconds Ms(long milliseconds) => new(milliseconds * 1_000_000);

    private static byte[] Receipt(ulong publication, RuntimeSessionId? session = null)
    {
        var bytes = new byte[WorkshopClockWire.ReceiptBytes];
        (session ?? Session).WriteTo(bytes.AsSpan(0, RuntimeSessionId.ByteLength));
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(RuntimeSessionId.ByteLength), publication);
        return bytes;
    }

    private static WorkshopReliableReadLedger Published(params (ulong Publication, long Milliseconds)[] reads)
    {
        var ledger = new WorkshopReliableReadLedger(Session);
        foreach (var (publication, milliseconds) in reads) ledger.Publish(new(publication), Ms(milliseconds));
        return ledger;
    }

    private static ReadReceiptRejection Rejection(WorkshopReliableReadLedger ledger, byte[] receipt) =>
        Assert.Throws<ReadReceiptRejectedException>(() => ledger.Receipt(receipt)).Reason;

    // ---- AC1: the worker detects the stall in its own reliable-read accounting ----

    [Fact]
    public void AnUnreceiptedReadStallsOnlyPastTheBound()
    {
        var ledger = Published((1, 100));
        Assert.False(ledger.Stalled(Ms(600)), "exactly the bound is not a stall");
        Assert.True(ledger.Stalled(new(Ms(600).Value + 1)), "one nanosecond past the bound is a stall");
        Assert.Equal(WorkshopRejection.ReliableStalled, ledger.DebtReason(new(Ms(600).Value + 1)));
    }

    [Fact]
    public void AReceiptRetiresItsReadAndClearsTheStall()
    {
        var ledger = Published((1, 0));
        Assert.True(ledger.Stalled(Ms(501)));
        Assert.Equal(new PublicationSequence(1), ledger.Receipt(Receipt(1)));
        Assert.True(ledger.Idle);
        Assert.False(ledger.Stalled(Ms(10_000)));
        Assert.Equal(WorkshopRejection.Capacity, ledger.DebtReason(Ms(10_000)));
    }

    [Fact]
    public void AReceiptRetiresEveryCoalescedEarlierPublication()
    {
        // The worker overwrites an unposted ordinary read with a newer one, so P1 and P2 are never posted or receipted.
        var ledger = Published((1, 0), (2, 8), (3, 16));
        ledger.Receipt(Receipt(3));
        Assert.True(ledger.Idle);
        Assert.False(ledger.Stalled(Ms(5_000)));
    }

    [Fact]
    public void ThePartlyReceiptedLedgerAgesFromItsOldestUnreceiptedRead()
    {
        var ledger = Published((1, 0), (2, 400));
        ledger.Receipt(Receipt(1));
        Assert.Equal(new UnreceiptedRead(new(2), Ms(400)), ledger.Oldest);
        Assert.False(ledger.Stalled(Ms(900)));
        Assert.True(ledger.Stalled(new(Ms(900).Value + 1)));
    }

    [Fact]
    public void AReceiptForAReadPublishedBeforeLaterOnesStillRetires()
    {
        // Reset never clears the ledger: a receipt in flight across a Reset retires its read, then the newer ones retire in turn.
        var ledger = Published((1, 0), (2, 1), (3, 2));
        ledger.Receipt(Receipt(1));
        ledger.Publish(new(4), Ms(3));
        ledger.Receipt(Receipt(2));
        ledger.Receipt(Receipt(4));
        Assert.True(ledger.Idle);
    }

    [Fact]
    public void AMismatchedReceiptIsATypedRejectionThatRetiresNothing()
    {
        var ledger = Published((1, 0), (2, 1));
        ledger.Receipt(Receipt(1));
        Assert.Equal(ReadReceiptRejection.Malformed, Rejection(ledger, new byte[WorkshopClockWire.ReceiptBytes - 1]));
        Assert.Equal(ReadReceiptRejection.Malformed, Rejection(ledger, new byte[WorkshopClockWire.ReceiptBytes + 1]));
        Assert.Equal(ReadReceiptRejection.WrongSession, Rejection(ledger, Receipt(2, new RuntimeSessionId(9, 9))));
        var zeroSession = Receipt(2); zeroSession.AsSpan(0, RuntimeSessionId.ByteLength).Clear();
        Assert.Equal(ReadReceiptRejection.WrongSession, Rejection(ledger, zeroSession));
        Assert.Equal(ReadReceiptRejection.Unpublished, Rejection(ledger, Receipt(3)));
        Assert.Equal(ReadReceiptRejection.Retired, Rejection(ledger, Receipt(1)));
        Assert.Equal(ReadReceiptRejection.Retired, Rejection(ledger, Receipt(0)));
        Assert.Equal(new UnreceiptedRead(new(2), Ms(1)), ledger.Oldest);
        Assert.True(ledger.Stalled(Ms(502)), "a refused receipt leaves its read unreceipted");
    }

    [Fact]
    public void AReceiptForAnUnpublishedGapRetiresNothing()
    {
        var ledger = Published((1, 0), (3, 100));
        Assert.Equal(ReadReceiptRejection.Unpublished, Rejection(ledger, Receipt(2)));
        Assert.Equal(new UnreceiptedRead(new(1), Ms(0)), ledger.Oldest);
        Assert.True(ledger.Stalled(Ms(501)));
        ledger.Receipt(Receipt(1));
        Assert.Equal(ReadReceiptRejection.Unpublished, Rejection(ledger, Receipt(2)));
        ledger.Receipt(Receipt(3));
        Assert.True(ledger.Idle);
    }

    [Fact]
    public void PublicationsAreStrictlyIncreasing()
    {
        var ledger = Published((5, 0));
        Assert.Throws<ArgumentException>(() => ledger.Publish(new(5), Ms(1)));
        Assert.Throws<ArgumentException>(() => ledger.Publish(new(4), Ms(1)));
        Assert.Throws<ArgumentException>(() => new WorkshopReliableReadLedger(default));
    }

    [Fact]
    public void TheLedgerHasNoFixedCapacity()
    {
        var ledger = new WorkshopReliableReadLedger(Session);
        for (ulong publication = 1; publication <= 20_000; publication++) ledger.Publish(new(publication), Ms(0));
        ledger.Receipt(Receipt(20_000));
        Assert.True(ledger.Idle);
    }

    [Fact]
    public async Task TheRunLoopStopsForAStalledLedger()
    {
        var ledger = Published((1, 0));
        var host = new LedgerHost(ledger, Ms(501));
        await WorkshopTickLoop.Run(host);
        Assert.Equal([WorkshopRejection.ReliableStalled], host.Stops);
        Assert.Equal(0, host.Advances);
    }

    [Fact]
    public async Task TheRunLoopKeepsRunningWhileReceiptsFlow()
    {
        var ledger = Published((1, 0));
        ledger.Receipt(Receipt(1));
        var host = new LedgerHost(ledger, Ms(501));
        await WorkshopTickLoop.Run(host);
        Assert.Empty(host.Stops);
        Assert.Equal(1, host.Advances);
    }

    /// <summary>A run-loop host whose debt is the ledger's stall, as the worker's RunLoopHost reads it; it completes after one tick.</summary>
    private sealed class LedgerHost(WorkshopReliableReadLedger ledger, MasterTimeNanoseconds now) : ITickLoopHost
    {
        public List<WorkshopRejection> Stops { get; } = [];
        public int Advances { get; private set; }
        private bool _completed;
        public bool Owns => Stops.Count == 0 && !_completed;
        public bool Blocked => false;
        public bool DebtExceeded => ledger.Stalled(now);
        public bool AtRunLimit => Advances == 1;
        public bool CaughtUp => false;
        public ValueTask<TickAttempt> Advance() { Advances++; return ValueTask.FromResult(TickAttempt.Committed); }
        public ValueTask StopForDebt() { Stops.Add(ledger.DebtReason(now)); return ValueTask.CompletedTask; }
        public ValueTask CompleteRun() { _completed = true; return ValueTask.CompletedTask; }
        public ValueTask YieldTurn() => ValueTask.CompletedTask;
        public ValueTask AwaitWork() => ValueTask.CompletedTask;
    }

    // ---- AC2: memory observation is a typed query with a receipt on the command/query transport ----

    private static readonly WorkshopMemoryMeasurement Measurement = new(1234, 1200, 19, 1_000_000_000, 5000, 5050, 6, 7);

    private static WorkshopCommand ObserveMemory(ulong sequence = 1) => new(new(sequence), WorkshopCommandKind.ObserveMemory, new(10), default,
        null, RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(2), Projection: new(3));

    private static WorkshopResponse Acknowledgement(WorkshopCommandResult result, WorkshopMemoryMeasurement? memory) =>
        WireFaultTests.Acknowledgement() with { Result = result, Memory = memory };

    private static readonly WorkshopCommandResult Applied = new(WorkshopCommandOutcome.Applied, WorkshopRejection.None);

    [Fact]
    public void TheMemoryQueryIsATypedCommandOnTheOneCodec()
    {
        var command = ObserveMemory();
        Assert.Equal(command, WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(command)));
        Assert.Equal(12u, (uint)WorkshopCommandKind.ObserveMemory);
        Assert.Equal((uint)WorkshopCommandKind.ObserveMemory, (uint)CuriousContraptions.Wire.CommandKind.ObserveMemory);
    }

    [Fact]
    public async Task TheMemoryQueryIsRoutedWithItsSequenceAndAnsweredByItsAcknowledgement()
    {
        var executed = new List<WorkshopCommand>();
        var acknowledgement = WorkshopWireFlatBuffers.EncodeResponse(Acknowledgement(Applied, Measurement));
        var router = new WorkshopCommandRouter(command => { executed.Add(command); return Task.FromResult(acknowledgement); });
        var routed = router.Dispatch(WorkshopWireFlatBuffers.EncodeCommand(ObserveMemory()));
        Assert.Equal(new CommandSequence(1), routed.Sequence);
        Assert.Equal(WorkshopCommandKind.ObserveMemory, Assert.Single(executed).Kind);
        var response = WorkshopWireFlatBuffers.DecodeResponse(await routed.Response);
        Assert.Equal(WorkshopResponseKind.Acknowledgement, response.Kind);
        Assert.Equal(Measurement, response.Memory);
    }

    [Fact]
    public void AnAcknowledgementCarriesItsMemoryMeasurementExactly()
    {
        var wide = new WorkshopMemoryMeasurement(long.MaxValue, 0, long.MaxValue, long.MaxValue, 0, long.MaxValue, 0, int.MaxValue);
        foreach (var memory in new[] { Measurement, wide })
        {
            var response = Acknowledgement(Applied, memory);
            Assert.Equal(response, WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response)));
        }
        var none = Acknowledgement(Applied, null);
        Assert.Null(WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(none)).Memory);
    }

    [Fact]
    public void OnlyAnAppliedAcknowledgementCarriesMemory()
    {
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeResponse(
            Acknowledgement(new(WorkshopCommandOutcome.Rejected, WorkshopRejection.WrongPhase), Measurement)));
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeResponse(WireFaultTests.PhysicalRead() with { Memory = Measurement }));
    }

    [Fact]
    public void AnUnboundedOrFabricatedMeasurementIsRefused()
    {
        foreach (var invalid in new[] {
            Measurement with { UsedBefore = -1 }, Measurement with { UsedAfter = -1 },
            Measurement with { DurationTicks = -1 }, Measurement with { Frequency = 0 },
            Measurement with { AllocatedBefore = -1 }, Measurement with { AllocatedAfter = 4999 },
            Measurement with { CollectionsBefore = -1 }, Measurement with { CollectionsAfter = 5 } })
        {
            Assert.Throws<ArgumentException>(invalid.Validate);
            Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeResponse(Acknowledgement(Applied, invalid)));
        }
    }

    [Theory]
    [InlineData(WorkshopSimulationPhase.Running)]
    [InlineData(WorkshopSimulationPhase.Paused)]
    [InlineData(WorkshopSimulationPhase.Starting)]
    [InlineData(WorkshopSimulationPhase.Resetting)]
    public void AQueryOutsideAStoppedPhaseIsWrongPhase(WorkshopSimulationPhase phase) =>
        Assert.Equal(new WorkshopCommandResult(WorkshopCommandOutcome.Rejected, WorkshopRejection.WrongPhase),
            WorkshopMemoryQuery.Admit(phase, gpuOperationPending: false, readsReceipted: true));

    [Theory]
    [InlineData(WorkshopSimulationPhase.Building)]
    [InlineData(WorkshopSimulationPhase.Completed)]
    [InlineData(WorkshopSimulationPhase.Faulted)]
    public void AStoppedIdleWorldAdmitsTheQuery(WorkshopSimulationPhase phase)
    {
        Assert.Equal(Applied, WorkshopMemoryQuery.Admit(phase, gpuOperationPending: false, readsReceipted: true));
        var busy = new WorkshopCommandResult(WorkshopCommandOutcome.Rejected, WorkshopRejection.Busy);
        Assert.Equal(busy, WorkshopMemoryQuery.Admit(phase, gpuOperationPending: true, readsReceipted: true));
        Assert.Equal(busy, WorkshopMemoryQuery.Admit(phase, gpuOperationPending: false, readsReceipted: false));
    }

    [Fact]
    public void AnAppliedQueryPairsTheSimulationMeasurementWithTheBrowserOne()
    {
        var browser = Measurement with { UsedBefore = 77 };
        var reply = WorkshopMemoryQuery.Complete(WorkshopCommandKind.ObserveMemory, Acknowledgement(Applied, Measurement), true, () => browser);
        Assert.Equal(Applied, reply.Result);
        Assert.Equal(new WorkshopMemoryObservation(browser, Measurement), reply.Observation);
    }

    [Fact]
    public void ARefusedQueryHasNoObservationAndCapturesNothing()
    {
        var refused = new WorkshopCommandResult(WorkshopCommandOutcome.Rejected, WorkshopRejection.WrongPhase);
        var reply = WorkshopMemoryQuery.Complete(WorkshopCommandKind.ObserveMemory, Acknowledgement(refused, null), true,
            () => throw new InvalidOperationException("A refused query captures nothing."));
        Assert.Equal(refused, reply.Result);
        Assert.Null(reply.Observation);
    }

    [Fact]
    public async Task MemoryQueryOwnsTheStoppedIntervalUntilCaptureCompletes()
    {
        var owner = new WorkshopMemoryQueryOwner();
        var source = new TaskCompletionSource<WorkshopDelivery>();
        var captures = 0;
        var pending = owner.Execute(0, () => source.Task, () => true, () =>
        {
            Assert.True(owner.Active);
            Assert.Throws<InvalidOperationException>(() => owner.AdmitCommand(WorkshopCommandKind.Run));
            captures++;
            return Measurement;
        });
        Assert.True(owner.Active);
        Assert.Throws<InvalidOperationException>(() => owner.AdmitCommand(WorkshopCommandKind.Run));
        Assert.Throws<InvalidOperationException>(() => owner.AdmitCommand(WorkshopCommandKind.Reset));
        owner.AdmitCommand(WorkshopCommandKind.Dispose);
        var busy = await owner.Execute(0, () => throw new InvalidOperationException("No second query."),
            () => true, () => throw new InvalidOperationException("No second capture."));
        Assert.Equal(WorkshopRejection.Busy, busy.Result.Reason);
        source.SetResult(new(Acknowledgement(Applied, Measurement), true));
        Assert.Equal(Applied, (await pending).Result);
        Assert.Equal(1, captures);
        Assert.False(owner.Active);
        owner.AdmitCommand(WorkshopCommandKind.Run);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task SupersededOrDisposedQueryReleasesOwnershipWithoutCapture(bool applicable, bool current)
    {
        var owner = new WorkshopMemoryQueryOwner();
        var reply = await owner.Execute(0, () => Task.FromResult(new WorkshopDelivery(Acknowledgement(Applied, Measurement), applicable)),
            () => current, () => throw new InvalidOperationException("No stale capture."));
        Assert.Equal(WorkshopRejection.StaleGeneration, reply.Result.Reason);
        Assert.Null(reply.Observation);
        Assert.False(owner.Active);
    }

    [Fact]
    public void ASupersededQueryCapturesNothing()
    {
        var reply = WorkshopMemoryQuery.Complete(WorkshopCommandKind.ObserveMemory, Acknowledgement(Applied, Measurement), false,
            () => throw new InvalidOperationException("A superseded query captures nothing."));
        Assert.Equal(new(WorkshopCommandOutcome.Rejected, WorkshopRejection.StaleGeneration), reply.Result);
        Assert.Null(reply.Observation);
    }

    [Fact]
    public void MemoryRidesOnlyOnTheAcknowledgementOfAnAppliedQuery()
    {
        Assert.Throws<ArgumentException>(() => WorkshopMemoryQuery.ValidateCarriage(WorkshopCommandKind.ObserveMemory, Acknowledgement(Applied, null)));
        Assert.Throws<ArgumentException>(() => WorkshopMemoryQuery.ValidateCarriage(WorkshopCommandKind.Save, Acknowledgement(Applied, Measurement)));
        WorkshopMemoryQuery.ValidateCarriage(WorkshopCommandKind.Save, Acknowledgement(Applied, null));
        WorkshopMemoryQuery.ValidateCarriage(WorkshopCommandKind.ObserveMemory, Acknowledgement(Applied, Measurement));
    }

    // ---- AC3: an unknown command or worker envelope is refused structurally with a typed reason ----

    [Fact]
    public void AnUnknownCommandKindIsATypedWireFault()
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(128);
        var session = new byte[RuntimeSessionId.ByteLength];
        Session.WriteTo(session);
        var sessionVector = CuriousContraptions.Wire.WorkshopCommand.CreateSessionVectorBlock(builder, session);
        CuriousContraptions.Wire.WorkshopCommand.StartWorkshopCommand(builder);
        CuriousContraptions.Wire.WorkshopCommand.AddSequence(builder, 1);
        CuriousContraptions.Wire.WorkshopCommand.AddKind(builder, (CuriousContraptions.Wire.CommandKind)99);
        CuriousContraptions.Wire.WorkshopCommand.AddEpoch(builder, 10);
        CuriousContraptions.Wire.WorkshopCommand.AddRevisionKind(builder, (uint)ExpectedRevisionKind.Any);
        CuriousContraptions.Wire.WorkshopCommand.AddSession(builder, sessionVector);
        CuriousContraptions.Wire.WorkshopCommand.AddCadence(builder, 2);
        CuriousContraptions.Wire.WorkshopCommand.AddProjection(builder, 3);
        builder.FinishSizePrefixed(CuriousContraptions.Wire.WorkshopCommand.EndWorkshopCommand(builder).Value, WorkshopWireFlatBuffers.CommandIdentifier);
        var executed = 0;
        var router = new WorkshopCommandRouter(_ => { executed++; return Task.FromResult(Array.Empty<byte>()); });
        var fault = Assert.Throws<WireFaultException>(() => router.Dispatch(builder.SizedByteArray()));
        Assert.Equal(WireFaultReason.UnknownCommand, fault.Reason);
        Assert.Contains("99", fault.Message, StringComparison.Ordinal);
        Assert.Equal(0, executed);
    }

    [Fact]
    public void AnUnknownWorkerEnvelopeIsRefusedByNameWithItsTypedReason()
    {
        var stall = new WorkerEnvelopeRejectedException(WorkerEnvelopeRejection.UnknownEnvelope, ["reliable" + "Stalled"]);
        Assert.Equal(WorkerEnvelopeRejection.UnknownEnvelope, stall.Reason);
        Assert.Equal("Unsupported worker envelope: reliable" + "Stalled", stall.Message);
        Assert.Equal("Unsupported worker envelope: memory" + "Request, extra",
            new WorkerEnvelopeRejectedException(WorkerEnvelopeRejection.UnknownEnvelope, ["memory" + "Request", "extra"]).Message);
        Assert.Equal("Unsupported worker envelope: (no keys)",
            new WorkerEnvelopeRejectedException(WorkerEnvelopeRejection.UnknownEnvelope, []).Message);
        Assert.Equal(WorkerEnvelopeRejection.CommandTransportSaturated,
            new WorkerEnvelopeRejectedException(WorkerEnvelopeRejection.CommandTransportSaturated, []).Reason);
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkerEnvelopeRejectedException((WorkerEnvelopeRejection)99, []));
    }

    // ---- Deletes: the raw stall flag and the raw memory channel are gone from production ----

    private static string Root => twodog.Engine.ResolveProjectDir();

    private static readonly string[] ProductionRoots = ["engine", "ui", "elements", "schemas", "CuriousContraptions.Abstractions",
        "CuriousContraptions.Abstractions.Electrical", "CuriousContraptions.Abstractions.Fluid", "CuriousContraptions.Abstractions.Mechanical",
        "CuriousContraptions.Abstractions.Signal", "CuriousContraptions.Animation", "CuriousContraptions.Animation.Worker",
        "CuriousContraptions.Bundle", "CuriousContraptions.Simulation", "CuriousContraptions.web"];

    private static readonly string[] ProductionExtensions = [".cs", ".js", ".mjs", ".fbs"];

    /// <summary>The typed command and query definitions: the closed command kind's declaration in the C# enum, the schema and the
    /// generated schema code. Everywhere else the query is named only through its enum (<c>CommandKind.ObserveMemory</c>).</summary>
    private static readonly string[] TypedDefinitions = ["engine/gpu/WorkshopMessages.cs", "schemas/workshop_wire.fbs", "engine/gpu/WorkshopWireGenerated.cs"];

    private static bool IsBuildOutput(string path) => path.Split(Path.DirectorySeparatorChar)
        .Any(segment => segment is "bin" or "obj" or "node_modules" or ".godot" or "AppBundle" or "_framework");

    /// <summary>The acceptance rg over production, plus the deleted channel's own symbols (split here so this file is no hit).</summary>
    [Fact]
    public void TheRawStallFlagAndMemoryChannelAreGoneFromProduction()
    {
        var retired = new Regex(string.Join('|', "reliable" + "Stalled", "memory" + "Request", "SetReliable" + "Stall", "Observe" + "Memory",
            "memory" + "Result", "candidate" + "TimedOut", "Workshop" + "MemoryWire", "Begin" + "Memory", "Receive" + "Memory\\b",
            "Release" + "Memory", "SendMemory" + "Request", "CompleteMemory" + "Observation", "settle" + "Memory"));
        var enumUse = new Regex(@"CommandKind\." + "Observe" + "Memory" + @"\b");
        var declaration = new Regex(@"\b" + "Observe" + "Memory" + @"\b(?=\s*(=|,|\}))");
        var hits = ProductionRoots.Select(directory => Path.Combine(Root, directory)).Where(Directory.Exists)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            .Select(path => Path.GetRelativePath(Root, path))
            .Where(path => ProductionExtensions.Contains(Path.GetExtension(path)) && !IsBuildOutput(path))
            .SelectMany(path =>
            {
                var text = enumUse.Replace(File.ReadAllText(Path.Combine(Root, path)), "");
                if (TypedDefinitions.Contains(path.Replace(Path.DirectorySeparatorChar, '/'))) text = declaration.Replace(text, "");
                return retired.Matches(text).Select(match => $"{path}: {match.Value}");
            }).ToArray();
        Assert.Empty(hits);
    }
}
