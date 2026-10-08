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
        Assert.Single(registry.Definitions);
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
    [InlineData(WorkshopSimulationPhase.Running)]
    [InlineData(WorkshopSimulationPhase.Completed)]
    [InlineData(WorkshopSimulationPhase.Faulted)]
    public async Task ActualWorldKeepsLaterCommittedPhaseWhenOriginalRunAckArrives(WorkshopSimulationPhase phase)
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
                phase, new(new(1), new(tick), null, new(tick + 1)));
            world._Process(0);
            Assert.Equal(phase, world.WorkshopPhase);
            Assert.Equal(phase == WorkshopSimulationPhase.Running, world.Running);
            var original = new WorkshopResponse(new(1), WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
                new(new(1), default, null, new(1)));
            client.RunAck.SetResult(original);
            var completion = await run;
            Assert.Equal(original, client.LastDelivery.Response);
            Assert.Equal(WorkshopCommandOutcome.Applied, completion.Result.Outcome);
            Assert.False(completion.Applicable);
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
                new(new(1), default, null, new(1)));
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
        private readonly WorkshopClientCursor _cursor = new();
        private bool _disposed;
        private ulong _sequence;
        public TaskCompletionSource<WorkshopResponse> RunAck { get; } = new();
        public WorkshopResponse? NextRead { get; set; }
        public WorkshopDelivery LastDelivery { get; private set; }
        public SimulationEpoch Epoch => _cursor.Epoch;
        public AuthorityRevision Revision => _cursor.Revision;
        public WorkshopCommandIdentity? Pending => null;
        public WorkshopTransportState TransportState => WorkshopTransportState.Ready;
        public async Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
            WorkshopCommandIdentity? target = null)
        {
            var command = new WorkshopCommand(new(++_sequence), kind, Epoch, Revision, construction, target);
            var dispatched = _cursor.ReadOrder;
            WorkshopResponse response;
            if (kind == WorkshopCommandKind.Run) response = await RunAck.Task;
            else if (kind == WorkshopCommandKind.Reset)
                response = new(command.Sequence, WorkshopResponseKind.Acknowledgement,
                    new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Building,
                    new(new(Epoch.Value + 1), default, null, new(Revision.Value + 1)));
            else throw new ArgumentException("Unexpected test transport command.");
            LastDelivery = _cursor.Acknowledge(command, response, dispatched, _disposed);
            return LastDelivery;
        }
        public bool TryRead(out WorkshopResponse response)
        {
            response = NextRead ?? default;
            if (NextRead is null) return false;
            NextRead = null;
            return _cursor.AcceptRead(response);
        }
        public Task<WorkshopDelivery> CancelPending() => throw new InvalidOperationException("No cancellable test transport request.");
        public ValueTask DisposeAsync() { _disposed = true; return ValueTask.CompletedTask; }
    }
}
