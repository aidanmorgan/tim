using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public sealed class BasketballResourceTests(NativeSceneFixture godot)
{
    [Fact]
    public void ActualResourceAndSceneUseCanonicalMaterialBits()
    {
        var registry = new PartRegistry();
        registry.Discover();
        Assert.Equal(new[] { WorkshopPartKind.Basketball, WorkshopPartKind.Receiver },
            registry.Definitions.Keys.OrderBy(kind => kind));
        var definition = registry.Definitions[WorkshopPartKind.Basketball];
        Assert.Empty(definition.Parameters);
        var material = definition.Basketball!.Capture();
        Assert.Equal((ushort)13681, BitConverter.HalfToUInt16Bits(material.Radius.Value));
        Assert.Equal((ushort)14438, BitConverter.HalfToUInt16Bits(material.Bounce.Value));
        var ball = registry.Create(WorkshopPartKind.Basketball);
        try
        {
            godot.Tree.Root.AddChild(ball);
            var sphere = ball.Visual.GetChildren().OfType<MeshInstance3D>()
                .Select(n => n.Mesh).OfType<SphereMesh>().Single();
            Assert.Equal((float)material.Radius.Value, sphere.Radius);
        }
        finally { ball.GetParent()?.RemoveChild(ball); ball.Free(); }
    }

    [Fact]
    public void UnknownCatalogueAndOldFloatDeclarationRejectBeforeConstruction()
    {
        var registry = new PartRegistry();
        registry.Discover();
        Assert.Throws<ArgumentException>(() => registry.Create(WorkshopPartKind.Unsupported));
        Assert.Throws<ArgumentException>(() => registry.Create((WorkshopPartKind)999));
        using var obsolete = new PartDefinition { Id = "ball" };
        var ball = new BallPart();
        try { Assert.Throws<ArgumentException>(() => ball.Configure(obsolete)); }
        finally { ball.Free(); }
    }

    [Fact]
    public void CurrentGameAssemblyDoesNotContainCpuPhysicalSolverOrReferenceAssembly()
    {
        var game = typeof(MachineWorld).Assembly;
        Assert.DoesNotContain(game.GetTypes(), t => t.Namespace is { } ns &&
            (ns.StartsWith("CuriousContraptions.Physics", StringComparison.Ordinal) ||
             ns.StartsWith("CuriousContraptions.Geometry", StringComparison.Ordinal)));
        Assert.DoesNotContain(game.GetReferencedAssemblies(), a => a.Name == "CuriousContraptions.Geometry");
        Assert.Null(typeof(MachineWorld).GetMethod("Start"));
        Assert.Null(typeof(MachineWorld).GetMethod("Step"));
        Assert.Null(typeof(MachineWorld).GetMethod("Restore"));
    }

    [Theory]
    [InlineData(WorkshopSimulationPhase.Running, true)]
    [InlineData(WorkshopSimulationPhase.Completed, true)]
    [InlineData(WorkshopSimulationPhase.Faulted, true)]
    [InlineData(WorkshopSimulationPhase.Running, false)]
    [InlineData(WorkshopSimulationPhase.Completed, false)]
    [InlineData(WorkshopSimulationPhase.Faulted, false)]
    public async Task ActualWorldKeepsLaterCommittedPhaseWhenOriginalRunAckArrives(WorkshopSimulationPhase phase, bool sceneBeforeAck)
    {
        var client = new HeldWorkshopClient();
        var world = new MachineWorld();
        try
        {
            await world.InitializeWorkshop(() => Task.FromResult<IWorkshopClient>(client));
            var run = world.RunWorkshop();
            Assert.Equal(WorkshopSimulationPhase.Starting, world.WorkshopPhase);
            var tick = phase == WorkshopSimulationPhase.Completed ? 3600UL :
                phase == WorkshopSimulationPhase.Faulted ? 0UL : 1UL;
            client.NextRead = new(default, WorkshopResponseKind.Read,
                new(phase == WorkshopSimulationPhase.Faulted ? WorkshopCommandOutcome.Faulted : WorkshopCommandOutcome.Applied,
                    phase == WorkshopSimulationPhase.Faulted ? WorkshopRejection.DeviceLost : WorkshopRejection.None),
                phase, new(new(1), new(tick), null, new(tick + 1), new(WorkshopClockDomain.SimulationMonotonic, new(1), new(110_000_000), new(100_000))));
            client.AdmitPendingRead(); // Actual callback admission may precede the next scene frame.
            if (sceneBeforeAck) world._Process(0);
            Assert.Equal(sceneBeforeAck ? phase : WorkshopSimulationPhase.Starting, world.WorkshopPhase);
            var original = new WorkshopResponse(new(1), WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
                new(new(1), default, null, new(1), new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000))), new(11, 23), Cadence: new(1), MasterGeneration: new(1), Projection: new(1));
            client.RunAck.SetResult(original);
            var completion = await run;
            Assert.Equal(original, client.LastDelivery.Response);
            Assert.Equal(WorkshopCommandOutcome.Applied, completion.Result.Outcome);
            Assert.False(completion.Applicable);
            if (!sceneBeforeAck) world._Process(0);
            Assert.Equal(phase == WorkshopSimulationPhase.Running, world.Running);
            Assert.Equal(phase, world.WorkshopPhase);
            Assert.Equal(tick, world.WorkshopRead.Tick.Value);
            var reset = await world.ResetWorkshop();
            Assert.True(reset.Applicable);
            Assert.Equal(WorkshopCommandOutcome.Applied, reset.Result.Outcome);
            Assert.Equal(WorkshopSimulationPhase.Building, world.WorkshopPhase);
            Assert.False(world.Running);
            Assert.Equal(0UL, world.WorkshopRead.Tick.Value);
            Assert.Equal(2UL, world.WorkshopRead.Epoch.Value);
        }
        finally { world.Free(); }
    }

    [Fact]
    public async Task ActualWorldDoesNotReviveAfterRemovedSceneReceivesOriginalAppliedAck()
    {
        var client = new HeldWorkshopClient();
        var world = new MachineWorld();
        try
        {
            await world.InitializeWorkshop(() => Task.FromResult<IWorkshopClient>(client));
            var run = world.RunWorkshop();
            world._ExitTree();
            var original = new WorkshopResponse(new(1), WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
                new(new(1), default, null, new(1), new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000))), new(11, 23), Cadence: new(1), MasterGeneration: new(1), Projection: new(1));
            client.RunAck.SetResult(original);
            var completion = await run;
            Assert.False(completion.Applicable);
            Assert.Equal(WorkshopCommandOutcome.Applied, completion.Result.Outcome);
            Assert.Equal(original, client.LastDelivery.Response);
            Assert.Equal(WorkshopSimulationPhase.Disposed, world.WorkshopPhase);
            Assert.False(world.Running);
            Assert.Empty(world.Parts);
        }
        finally { world.Free(); }
    }

    private sealed class HeldWorkshopClient : IWorkshopClient
    {
        private static readonly RuntimeSessionId Session = new(11, 23);
        private readonly WorkshopClientCursor _cursor = new(Session);
        private bool _disposed;
        private ulong _sequence;
        public TaskCompletionSource<WorkshopResponse> RunAck { get; } = new();
        public WorkshopResponse? NextRead { get; set; }
        private bool _readAdmitted;
        public WorkshopDelivery LastDelivery { get; private set; }
        public SimulationEpoch Epoch => _cursor.Epoch;
        public AuthorityRevision Revision => _cursor.Revision;
        public WorkshopCommandIdentity? Pending => null;
        public WorkshopCadenceSettings Settings => WorkshopCadenceSettings.Default();
        public WorkshopTransportState TransportState => WorkshopTransportState.Ready;
        public async Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
            WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null)
        {
            var command = new WorkshopCommand(new(++_sequence), kind, Epoch, Revision, construction, target, Session: Session, Cadence: new(1), Projection: new(1));
            var dispatched = _cursor.ReadOrder;
            WorkshopResponse response;
            if (kind == WorkshopCommandKind.Run) response = await RunAck.Task;
            else if (kind == WorkshopCommandKind.Reset)
                response = new(command.Sequence, WorkshopResponseKind.Acknowledgement,
                    new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Building,
                    new(new(Epoch.Value + 1), default, null, new(Revision.Value + 1), new(WorkshopClockDomain.SimulationMonotonic, new(1), new(120_000_000), new(100_000))));
            else throw new ArgumentException("Unexpected test transport command.");
            response = response with { Session = Session, Cadence = new(1), MasterGeneration = new(1), Projection = new(response.Read.Epoch.Value), SourcePulse = new(response.Read.Tick.Value) };
            var prepared = _cursor.PrepareAcknowledgement(command, response, dispatched, _disposed);
            _cursor.Commit(prepared);
            LastDelivery = new(response, prepared.Applicable);
            return LastDelivery;
        }
        public void AdmitPendingRead()
        {
            var response = (NextRead ?? throw new InvalidOperationException("No pending read.")) with
                { Session = Session, Publication = new(_cursor.ReadOrder + 1), Cadence = new(1), MasterGeneration = new(1), Projection = new(1), SourcePulse = new(NextRead.Value.Read.Tick.Value) };
            var prepared = _cursor.PrepareRead(response);
            Assert.True(prepared.Applicable);
            _cursor.Commit(prepared); NextRead = response; _readAdmitted = true;
        }
        public bool TryRead(out WorkshopResponse response)
        {
            response = default;
            if (NextRead is null) return false;
            if (!_readAdmitted) AdmitPendingRead();
            response = NextRead!.Value; NextRead = null; _readAdmitted = false;
            return true;
        }
        public bool TryPresent(ulong frame, out WorkshopPresentationSample sample) { sample = default; return false; }
        public void ControlHint(HintControlKind kind, bool visible) => throw new InvalidOperationException("Hint control is outside this held read/ACK fixture.");
        public bool TryHint(ulong frame, out WorkshopHintSample sample) { sample = default; return false; }
        public bool TryCaptureOpacity(ulong frame, WorkshopPresentationSample physical, out Half opacity)
        { opacity = default; return false; }
        public void RecordCapturePresentation(ulong frame) => throw new InvalidOperationException("No capture opacity was supplied by this fixture.");
        public void RecordHintPresentation(ulong frame) => throw new InvalidOperationException("No hint was supplied by this fixture.");
        public void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene) { }
        public Task<WorkshopDelivery> CancelPending() => throw new InvalidOperationException("No cancellable test transport request.");
        public ValueTask DisposeAsync() { _disposed = true; return ValueTask.CompletedTask; }
    }
}
