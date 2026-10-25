using System.Reflection;
using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class BasketballResourceTests(NativeSceneFixture godot)
{
    [Fact]
    public void GenericBumperWorkPreservesF32AndRejectsMissingOrInvalidDeclarations()
    {
        var declared = (WorkshopGenericInstance)PlacedParts.Bumper(new(2), 0, 4, 0, strength: 8.000001f);
        Assert.Equal(8.000001f, declared.Parameters[new("strength")]);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Generic(PartId.Basketball,new(1), 0, 6, 0, 0, 0, 0, 1), declared));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var admitted = PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        Assert.Equal(8.000001f, System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(
            admitted.AsSpan(scene.World.ContactWorkOffset(0) + 16)));
        var saved = new WorkshopSavedConstruction(construction, new(3));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved)));
        // A missing or nonfinite declared strength is no Bumper.
        Assert.ThrowsAny<ArgumentException>(() => (declared with { Parameters = InstanceParameterCollection.Empty }).Validate());
        Assert.ThrowsAny<ArgumentException>(() => PlacedParts.Bumper(new(2), 0, 4, 0, strength: float.NaN));
    }

    [Fact]
    public void TheBundleAndSceneUseCanonicalMaterialBits()
    {
        var registry = new PartRegistry();
        registry.Load();
        Assert.Equal(new[] { PartId.Basketball, PartId.Receiver, PartId.Ramp, PartId.ImpactSwitch, PartId.SignalLamp, PartId.Wall, PartId.Delay, PartId.PinballBumper, PartId.Domino, PartId.BowlingBall, PartId.Battery, PartId.Springboard, PartId.Pipe, PartId.BallDetector, PartId.PipeBend45, PartId.PipeBend90, PartId.Funnel, PartId.PoweredGate, PartId.PressurePlate, PartId.Counter, PartId.Clock, PartId.Latch, new PartId("hold_timer"), new PartId("both_gate"), new PartId("electrical_nand"), new PartId("electrical_nor"), new PartId("electrical_or"), new PartId("electrical_xor"), new PartId("weight") }.OrderBy(id => id),
            registry.Definitions.Keys.OrderBy(id => id));
        // Each package's declared f32 material compiles exactly (the generic dynamic-body compile): Basketball 0.34 m, 1 kg,
        // restitution 0.55, rolling 0.035; Bowling 0.28 m, 4 kg, restitution 0.14, rolling 0.03 (CAT-014, owner 9 Oct 2026).
        var scene = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Generic(PartId.Basketball, new(1), -1, 2, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.BowlingBall, new(2), 1, 2, 0, 0, 0, 0, 1))), new(1, 2));
        (uint Radius, uint Mass, uint Bounce, uint Rolling) Bits(ulong id)
        {
            var body = Assert.Single(scene.Bodies.ToArray(), row => row.Id == new GpuBodyId(id));
            var sphere = Assert.Single(scene.Colliders.ToArray(), row => row.Body == body.Id);
            var surface = Assert.Single(scene.Materials.ToArray(), row => row.Id == sphere.Material);
            return (BitConverter.SingleToUInt32Bits(sphere.Radius.Value), BitConverter.SingleToUInt32Bits(body.Mass.Value),
                BitConverter.SingleToUInt32Bits(surface.Restitution.Value), BitConverter.SingleToUInt32Bits(surface.RollingResistance.Value));
        }
        static (uint, uint, uint, uint) Declared(float radius, float mass, float bounce, float rolling) =>
            (BitConverter.SingleToUInt32Bits(radius), BitConverter.SingleToUInt32Bits(mass),
                BitConverter.SingleToUInt32Bits(bounce), BitConverter.SingleToUInt32Bits(rolling));
        Assert.Equal(Declared(.34f, 1f, .55f, .035f), Bits(1));
        Assert.Equal(Declared(.28f, 4f, .14f, .03f), Bits(2));
        var ball = registry.Create(PartId.Basketball);
        var bowling = registry.Create(PartId.BowlingBall);
        try
        {
            godot.Tree.Root.AddChild(ball); godot.Tree.Root.AddChild(bowling);
            static float Radius(MachinePart part) => part.Visual.GetChildren().OfType<MeshInstance3D>()
                .Select(n => n.Mesh).OfType<SphereMesh>().Single().Radius;
            // The committed scene art is the declared collider: its f32 mesh radius is the package's f32 sphere radius (no runtime resize).
            Assert.Equal(Assert.Single(DeclaredPhysics.Of(PartId.Basketball).Colliders).HalfExtents.X, Radius(ball));
            Assert.Equal(Assert.Single(DeclaredPhysics.Of(PartId.BowlingBall).Colliders).HalfExtents.X, Radius(bowling));
            Assert.Equal((.34f, .28f), (Radius(ball), Radius(bowling)));
            Assert.Equal(new Color(0.27f, 0.39f, 0.61f), bowling.Definition.Color);
        }
        finally { ball.GetParent()?.RemoveChild(ball); ball.Free(); bowling.GetParent()?.RemoveChild(bowling); bowling.Free(); }
    }

    [Fact]
    public void DelayArtworkUsesSharedFullTurnAndSourcePhasePalette()
    {
        var registry=new PartRegistry(); registry.Load();
        var delay=(MachinePart)registry.Create(PartId.Delay);
        try
        {
            godot.Tree.Root.AddChild(delay);
            var hand=delay.Visual.GetNode<Node3D>("CountdownHand");
            var indicator=delay.Visual.GetChildren().OfType<MeshInstance3D>()
                .Single(mesh=>mesh.Position==new Vector3(0,-.35f,.37f));
            foreach(var (phase,progress,colour) in new[] {
                (CuriousContraptions.Abstractions.PresentationTimerPhase.Ready,0f,new Color("#556573")),
                (CuriousContraptions.Abstractions.PresentationTimerPhase.Counting,.25f,new Color("#e8b764")),
                (CuriousContraptions.Abstractions.PresentationTimerPhase.Finished,1f,new Color("#f7cb52")) })
            {
                for (var i = 0; i < delay.Definition.Presentation.Bindings.Count; i++)
                {
                    var binding = delay.Definition.Presentation.Bindings[i];
                    if (binding.Driver == CuriousContraptions.Abstractions.PresentationDriver.CommittedTimerProgress)
                        delay.ApplyDeclaredProperty(i, WorkshopDeclaredPresentation.Value(binding.Property, float.Lerp(binding.From, binding.To, progress)));
                    else if (binding.Phase == phase)
                        delay.ApplyDeclaredProperty(i, WorkshopDeclaredPresentation.Value(binding.Property, binding.To));
                }
                var actual=((StandardMaterial3D)indicator.MaterialOverride).AlbedoColor;
                Assert.Equal((float)colour.R,actual.R); Assert.Equal((float)colour.G,actual.G); Assert.Equal((float)colour.B,actual.B);
                Assert.InRange(Math.Abs(hand.Rotation.Z-(float)((double)(float)(-Math.Tau)*(double)progress)),0,.00001);
            }
        }
        finally {delay.GetParent()?.RemoveChild(delay);delay.Free();}
    }

    /// <summary>Story 9A.25b: a one-shot cue's sample at <paramref name="amount"/> of its declared curve, applied to every binding of the
    /// part exactly as MachineWorld applies the Animation worker's declared samples.</summary>
    private static void ApplyCue(MachinePart part, float amount)
    {
        var bindings = part.Definition.Presentation.Bindings;
        for (var i = 0; i < bindings.Count; i++)
            part.ApplyDeclaredProperty(i, CuriousContraptions.Presentation.AnimationValue.Blend(
                WorkshopDeclaredPresentation.Value(bindings[i].Property, bindings[i].From),
                WorkshopDeclaredPresentation.Value(bindings[i].Property, bindings[i].To), new(amount)));
    }

    [Fact]
    public void ReceiverHaloFollowsTheDeclaredCaptureBlendAndRestsOnItsNeutralPalette()
    {
        var registry=new PartRegistry(); registry.Load();
        var receiver=(MachinePart)registry.Create(PartId.Receiver);
        try
        {
            godot.Tree.Root.AddChild(receiver);
            var halo=receiver.Visual.GetChildren().OfType<MeshInstance3D>().Single(mesh=>mesh.Mesh is TorusMesh);
            var material=(StandardMaterial3D)halo.MaterialOverride;
            var neutral=new Color("#bdf4bd");
            Assert.All(receiver.Definition.Presentation.Bindings, binding =>
                Assert.Equal((CuriousContraptions.Abstractions.PresentationDriver.Event, CuriousContraptions.Abstractions.SimulationEventKind.Captured), (binding.Driver, binding.Event)));
            receiver.ResetPhysicalVisuals(); // construction/Reset restore the declared neutral palette
            Assert.Equal((float)neutral.R,material.AlbedoColor.R); Assert.Equal((float)neutral.G,material.AlbedoColor.G);
            ApplyCue(receiver, 1);
            Assert.Equal(1,material.AlbedoColor.R); Assert.Equal(1,material.AlbedoColor.G); Assert.Equal(1,material.AlbedoColor.B);
            ApplyCue(receiver, .5f);
            Assert.InRange(material.AlbedoColor.R,(float)neutral.R+.01f,.99f);
            // A non-finite cue value is refused where it is made, so it never reaches the halo.
            Assert.ThrowsAny<ArgumentException>(() => ApplyCue(receiver, float.NaN));
            Assert.InRange(material.AlbedoColor.R,(float)neutral.R+.01f,.99f);
            receiver.ResetPhysicalVisuals();
            Assert.Equal((float)neutral.R,material.AlbedoColor.R); Assert.Equal((float)neutral.G,material.AlbedoColor.G);
        }
        finally { receiver.GetParent()?.RemoveChild(receiver); receiver.Free(); }
    }

    [Fact]
    public void BumperAndSwitchArtworkFollowOneDeclaredBlendAndHoldOnInvalidSamples()
    {
        var registry=new PartRegistry(); registry.Load();
        var bumper=(MachinePart)registry.Create(PartId.PinballBumper);
        var toggle=(MachinePart)registry.Create(PartId.ImpactSwitch);
        try
        {
            godot.Tree.Root.AddChild(bumper); godot.Tree.Root.AddChild(toggle);
            var head=bumper.Visual.GetNode<MeshInstance3D>("BumperHead");
            var ring=bumper.Visual.GetNode<MeshInstance3D>("ImpactRing");
            var button=toggle.Visual.GetChildren().OfType<MeshInstance3D>().Single(mesh=>mesh.Mesh is CylinderMesh);
            Assert.All(bumper.Definition.Presentation.Bindings, binding => Assert.Equal(CuriousContraptions.Abstractions.SimulationEventKind.Impact, binding.Event));
            Assert.All(toggle.Definition.Presentation.Bindings, binding => Assert.Equal(CuriousContraptions.Abstractions.SimulationEventKind.TriggerActivated, binding.Event));
            ApplyCue(bumper, 1); ApplyCue(toggle, 1);
            Assert.Equal(Vector3.One*.88f,head.Scale); Assert.Equal(Vector3.One*1.24f,ring.Scale);
            Assert.Equal((float)(82d/255),((StandardMaterial3D)ring.MaterialOverride).AlbedoColor.B);
            var pressed=new Color("#bff5b0");
            Assert.Equal(-.02f,button.Position.Y);
            Assert.Equal((float)pressed.G,((StandardMaterial3D)button.MaterialOverride).AlbedoColor.G);
            // An out-of-range or non-finite cue amount is refused where it is made, so the committed value holds.
            Assert.ThrowsAny<ArgumentException>(() => ApplyCue(bumper, float.NaN));
            Assert.ThrowsAny<ArgumentException>(() => ApplyCue(bumper, 1.5f));
            Assert.Equal(Vector3.One*.88f,head.Scale);
            bumper.ResetPhysicalVisuals(); toggle.ResetPhysicalVisuals();
            Assert.Equal(Vector3.One,head.Scale); Assert.Equal(Vector3.One,ring.Scale);
            Assert.Equal((float)(194d/255),((StandardMaterial3D)ring.MaterialOverride).AlbedoColor.B);
            Assert.Equal(.06f,button.Position.Y);
            Assert.Equal((float)toggle.Definition.Color.R,((StandardMaterial3D)button.MaterialOverride).AlbedoColor.R);
        }
        finally
        {
            bumper.GetParent()?.RemoveChild(bumper); bumper.Free();
            toggle.GetParent()?.RemoveChild(toggle); toggle.Free();
        }
    }

    [Fact]
    public void ADeclaredDelayPartBindsTheDelayCurve()
    {
        var registry = new PartRegistry(); registry.Load();
        var part = registry.Create(PartId.Delay);
        try
        {
            part.EnsureConstructed();
            Assert.DoesNotContain(part.Definition.Presentation.Bindings, b => b.Driver == CuriousContraptions.Abstractions.PresentationDriver.Event);
            Assert.Contains(part.Definition.Presentation.Bindings, b => b.Driver == CuriousContraptions.Abstractions.PresentationDriver.CommittedTimerProgress);
            Assert.Contains(part.Definition.Presentation.Bindings, b => b.Driver == CuriousContraptions.Abstractions.PresentationDriver.CommittedTimerPhase);
        }
        finally { part.Free(); }
    }

    [Fact]
    public void TheMachinePartIsSealedAndHasNoConstructionHook()
    {
        Assert.True(typeof(MachinePart).IsSealed);
        Assert.Null(typeof(MachinePart).GetMethod("Build", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
    }

    [Fact]
    public void UnknownCatalogueRejectsBeforeConstruction()
    {
        // C-Balls: a ball's material is its bundle entry alone (no separate material to omit), so only an unknown part is refused here.
        var registry = new PartRegistry();
        registry.Load();
        Assert.Throws<ArgumentException>(() => registry.Create(new PartId("unsupported")));
        Assert.Throws<ArgumentException>(() => registry.Create(new PartId("999")));
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
        await using var client = new HeldWorkshopClient();
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
                    phase == WorkshopSimulationPhase.Faulted ? WorkshopRejection.Capacity : WorkshopRejection.None),
                phase, new(new(1), new(tick), default, new(tick + 1), new(WorkshopClockDomain.SimulationMonotonic, new(1), new(110_000_000), new(100_000))));
            client.AdmitPendingRead(); // Actual callback admission may precede the next scene frame.
            if (sceneBeforeAck) world._Process(0);
            Assert.Equal(sceneBeforeAck ? phase : WorkshopSimulationPhase.Starting, world.WorkshopPhase);
            var original = new WorkshopResponse(new(1), WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
                new(new(1), default, default, new(1), new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000))), new(11, 23), Cadence: new(1), MasterGeneration: new(1), Projection: new(1));
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
        await using var client = new HeldWorkshopClient();
        var world = new MachineWorld();
        try
        {
            await world.InitializeWorkshop(() => Task.FromResult<IWorkshopClient>(client));
            var run = world.RunWorkshop();
            world._ExitTree();
            var original = new WorkshopResponse(new(1), WorkshopResponseKind.Acknowledgement,
                new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Running,
                new(new(1), default, default, new(1), new(WorkshopClockDomain.SimulationMonotonic, new(1), new(100_000_000), new(100_000))), new(11, 23), Cadence: new(1), MasterGeneration: new(1), Projection: new(1));
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

    // Murdoch-2 H1 (Story 9.4b): a retired worker session is an explicit fault the Workshop already shows; the per-frame
    // UI bindings stop with it instead of throwing "Worker session is unavailable" into the engine loop every frame.
    [Theory]
    [InlineData(WorkshopTransportState.Indeterminate)]
    [InlineData(WorkshopTransportState.RecoveryBlocked)]
    public async Task ARetiredTransportStopsUiBindingsWithoutThrowing(WorkshopTransportState retired)
    {
        await using var client = new RetiringUiClient();
        var world = new MachineWorld();
        try
        {
            await world.InitializeWorkshop(() => Task.FromResult<IWorkshopClient>(client));
            Assert.True(world.TryUiFrame(WorkshopUiTarget.Hint, out _), "control: a Ready transport samples UI frames");
            world.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Visibility, true);
            Assert.Equal((1, 1), (client.Frames, client.Controls));
            client.TransportState = retired;
            Assert.False(world.TryUiFrame(WorkshopUiTarget.Hint, out var opacity));
            Assert.Equal(default, opacity);
            world.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Visibility, false);
            Assert.Equal((1, 1), (client.Frames, client.Controls));
        }
        finally { world.Free(); }
    }

    // Story 9R.0 (Chrome, clock-master's liveness fault): a retired transport has already reported its fault once, on the
    // error stream (CCGPU_TRANSPORT_FAILURE). The presentation that then stops, and the disposal that then reports the
    // indeterminate operations, are that same fault: each is recorded on the standard stream, never as a second stderr fault.
    [Theory]
    [InlineData(WorkshopTransportState.Indeterminate)]
    [InlineData(WorkshopTransportState.RecoveryBlocked)]
    public async Task ARetiredTransportsStoppedPresentationAndDisposalAreNotSecondFaults(WorkshopTransportState retired)
    {
        await using var client = new RetiringUiClient();
        var (output, errors) = await PresentAndDispose(client, retired,
            new WorkshopTransportException(new(new(1), new(1), default, default, default), retired, new TimeoutException("unresponsive")));
        Assert.Equal("", errors);
        Assert.Contains("CCGPU_PRESENTATION_AFTER_RETIREMENT System.InvalidOperationException: Worker session is unavailable", output, StringComparison.Ordinal);
        Assert.Contains($"CCGPU_DISPOSAL_AFTER_RETIREMENT {typeof(WorkshopTransportException).FullName}", output, StringComparison.Ordinal);
    }

    // Control: on a transport that has not retired (every live state, a timed-out or backpressured one included), the same
    // failures are faults, written whole to the error stream.
    [Theory]
    [InlineData(WorkshopTransportState.Ready)]
    [InlineData(WorkshopTransportState.Backpressure)]
    [InlineData(WorkshopTransportState.TimedOut)]
    public async Task APresentationOrDisposalFailureOnALiveTransportIsAStderrFault(WorkshopTransportState live)
    {
        await using var client = new RetiringUiClient { ReadFailure = new InvalidOperationException("presentation defect") };
        var (output, errors) = await PresentAndDispose(client, live,
            new WorkshopTransportException(new(new(1), new(1), default, default, default), live, new TimeoutException("late")));
        Assert.Contains("CCGPU_PRESENTATION_EXCEPTION System.InvalidOperationException: presentation defect", errors, StringComparison.Ordinal);
        Assert.Contains($"CCGPU_DISPOSAL_EXCEPTION {typeof(WorkshopTransportException).FullName}", errors, StringComparison.Ordinal);
        Assert.DoesNotContain("AFTER_RETIREMENT", output, StringComparison.Ordinal);
    }

    private static async Task<(string Output, string Errors)> PresentAndDispose(RetiringUiClient client, WorkshopTransportState state,
        Exception disposalFailure)
    {
        using var output = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        using var errors = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        var world = new MachineWorld();
        var (previousOut, previousError) = (Console.Out, Console.Error);
        try
        {
            await world.InitializeWorkshop(() => Task.FromResult<IWorkshopClient>(client));
            client.TransportState = state;
            client.DisposalFailure = disposalFailure;
            Console.SetOut(output);
            Console.SetError(errors);
            world._Process(0); // the presentation read
            Assert.Equal(WorkshopSimulationPhase.Faulted, world.WorkshopPhase);
            await (Task)typeof(MachineWorld).GetMethod("DisposeWorkshop", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(world, null)!;
        }
        finally
        {
            Console.SetOut(previousOut);
            Console.SetError(previousError);
            client.DisposalFailure = null; // the test's own disposal of the fixture succeeds
            world.Free();
        }
        return (output.ToString(), errors.ToString());
    }

    /// <summary>Mirrors BrowserWorkshopClient: UI calls and reads on a retired transport throw.</summary>
    private sealed class RetiringUiClient : IWorkshopClient
    {
        public int Frames { get; private set; }
        public int Controls { get; private set; }
        public Exception? ReadFailure { get; init; }
        public Exception? DisposalFailure { get; set; }
        public WorkshopTransportState TransportState { get; set; } = WorkshopTransportState.Ready;
        private void Retire()
        {
            if (TransportState is WorkshopTransportState.Indeterminate or WorkshopTransportState.RecoveryBlocked)
                throw new InvalidOperationException("Worker session is unavailable; its unacknowledged operations remain indeterminate.");
        }
        public WorkshopCadenceSettings Settings => WorkshopCadenceSettings.Default();
        public SimulationEpoch Epoch => new(1);
        public AuthorityRevision Revision => new(1);
        public WorkshopCommandIdentity? Pending => null;
        public void ControlUi(WorkshopUiTarget target, AnimationControlKind kind, bool visible) { Retire(); Controls++; }
        public bool TryUiFrame(ulong frame, WorkshopPresentationSample physical, WorkshopUiTarget target, out CuriousContraptions.Presentation.AnimationOpacity opacity)
        { Retire(); Frames++; opacity = default; return true; }
        public void SetDeclaredVisible(GpuBodyId owner, bool visible) { }
        public bool TryDeclaredFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out System.Collections.Generic.IReadOnlyList<DeclaredPresentationSample> samples) { samples = System.Array.Empty<DeclaredPresentationSample>(); return false; }
        public Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
            WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null) =>
            throw new InvalidOperationException("Commands are outside this UI binding fixture.");
        public Task<WorkshopDelivery> CancelPending() => throw new InvalidOperationException("No cancellable test transport request.");
        public bool TryRead(out WorkshopResponse response)
        {
            Retire();
            if (ReadFailure is not null) throw ReadFailure;
            response = default; return false;
        }
        public bool TryPresent(ulong frame, out WorkshopPresentationSample sample) { sample = default; return false; }
        public void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene) { }
        public ValueTask DisposeAsync() => DisposalFailure is null ? ValueTask.CompletedTask : ValueTask.FromException(DisposalFailure);
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
            WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null)
        {
            var command = new WorkshopCommand(new(++_sequence), kind, Epoch, Revision, construction, target, Session: Session, Cadence: new(1), Projection: new(1));
            var dispatched = _cursor.ReadOrder;
            WorkshopResponse response;
            if (kind == WorkshopCommandKind.Run) response = await RunAck.Task;
            else if (kind == WorkshopCommandKind.Reset)
                response = new(command.Sequence, WorkshopResponseKind.Acknowledgement,
                    new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Building,
                    new(new(Epoch.Value + 1), default, default, new(Revision.Value + 1), new(WorkshopClockDomain.SimulationMonotonic, new(1), new(120_000_000), new(100_000))));
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
        public void ControlUi(WorkshopUiTarget target, AnimationControlKind kind, bool visible) => throw new InvalidOperationException("UI control is outside this held read/ACK fixture.");
        public bool TryUiFrame(ulong frame, WorkshopPresentationSample physical, WorkshopUiTarget target, out CuriousContraptions.Presentation.AnimationOpacity opacity) { opacity = default; return false; }
        public void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene) { }
        public void SetDeclaredVisible(GpuBodyId owner, bool visible) { }
        public bool TryDeclaredFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out System.Collections.Generic.IReadOnlyList<DeclaredPresentationSample> samples) { samples = System.Array.Empty<DeclaredPresentationSample>(); return false; }
        public Task<WorkshopDelivery> CancelPending() => throw new InvalidOperationException("No cancellable test transport request.");
        public ValueTask DisposeAsync() { _disposed = true; return ValueTask.CompletedTask; }
    }
}
