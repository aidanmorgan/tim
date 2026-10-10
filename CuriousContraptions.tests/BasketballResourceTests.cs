using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public sealed class BasketballResourceTests(NativeSceneFixture godot)
{
    [Fact]
    public void RejectedContactCalibrationDoesNotConfigureOrConstructThePart()
    {
        using var resource = new ContactWorkResource { TargetSpeed=8f, ReferenceMass=2f, InitialEnergy=32f };
        using var definition = new PartDefinition { Id="bumper", ContactWork=resource };
        var part = new BumperPart();
        var originalName = part.Name;
        try
        {
            Assert.Throws<ArgumentException>(() => part.Configure(definition));
            Assert.Null(part.Definition);
            Assert.Equal(originalName, part.Name);
            Assert.Equal(0, part.GetChildCount());
            resource.ReferenceMass=1f;
            resource.InitialEnergy=31f;
            Assert.Throws<ArgumentException>(() => part.Configure(definition));
            Assert.Null(part.Definition);
            Assert.Equal(0, part.GetChildCount());
            resource.InitialEnergy=32f;
            part.Configure(definition);
            Assert.Same(definition, part.Definition);
            Assert.Equal(0, part.GetChildCount());
        }
        finally { part.Free(); }
    }

    [Fact]
    public void GenericContactWorkResourcePreservesF32AndRejectsMissingOrInvalidDeclarations()
    {
        using var resource = new ContactWorkResource { TargetSpeed=8.000001f, ReferenceMass=1f,
            InitialEnergy=.5f*8.000001f*8.000001f };
        var captured = resource.Capture();
        Assert.Equal(8.000001f, captured.TargetSpeed.Value);
        Assert.NotEqual((float)(Half)resource.TargetSpeed, captured.TargetSpeed.Value);
        var work = new BumperWork(captured.TargetSpeed,captured.ReferenceMass,captured.InitialEnergy);
        work.Validate();
        var declared = BumperWork.FromCanonicalStrength(8.000001f);
        Assert.Equal(declared, work);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Basketball(new(1), 0, 6, 0, 0, 0, 0, 1),
            WorkshopInput.Bumper(new(2), 0, 4, 0, 0, 0, 0, 1, BumperWork.FromCalibration(captured))));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var admitted = PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        Assert.Equal(8.000001f, System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(
            admitted.AsSpan(PhysicsGpuAbi.ContactWorksOffset + 16)));
        var saved = new WorkshopSavedConstruction(construction, new(3));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(saved)));
        resource.ReferenceMass=0;
        Assert.Throws<ArgumentException>(() => resource.Capture());
        resource.ReferenceMass=1f;
        resource.TargetSpeed=float.NaN;
        Assert.Throws<ArgumentException>(() => resource.Capture());
        using var missing = new ContactWorkResource();
        Assert.Throws<ArgumentException>(() => missing.Capture());
    }

    [Fact]
    public void ActualResourceAndSceneUseCanonicalMaterialBits()
    {
        var registry = new PartRegistry();
        registry.Discover();
        Assert.Equal(new[] { WorkshopPartKind.Basketball, WorkshopPartKind.Receiver, WorkshopPartKind.Ramp, WorkshopPartKind.ImpactSwitch, WorkshopPartKind.SignalLamp, WorkshopPartKind.Wall, WorkshopPartKind.Delay, WorkshopPartKind.PinballBumper, WorkshopPartKind.Domino, WorkshopPartKind.BowlingBall, WorkshopPartKind.Battery },
            registry.Definitions.Keys.OrderBy(kind => kind));
        var definition = registry.Definitions[WorkshopPartKind.Basketball];
        Assert.Empty(definition.Parameters);
        var material = definition.Ball!.Capture(WorkshopPartKind.Basketball);
        Assert.Equal((ushort)13681, BitConverter.HalfToUInt16Bits(material.Radius.Value));
        Assert.Equal((ushort)14438, BitConverter.HalfToUInt16Bits(material.Bounce.Value));
        Assert.Equal((ushort)10363, BitConverter.HalfToUInt16Bits(material.Rolling.Value));
        Assert.Equal(BallMaterial.For(WorkshopPartKind.Basketball), material);
        Assert.Throws<ArgumentException>(() => definition.Ball!.Capture(WorkshopPartKind.BowlingBall)); // Basketball bits are not a Bowling ball
        var bowlingDefinition = registry.Definitions[WorkshopPartKind.BowlingBall];
        Assert.Empty(bowlingDefinition.Parameters);
        var bowlingMaterial = bowlingDefinition.Ball!.Capture(WorkshopPartKind.BowlingBall);
        Assert.Equal(BallMaterial.For(WorkshopPartKind.BowlingBall), bowlingMaterial);
        Assert.Equal((ushort)13435, BitConverter.HalfToUInt16Bits(bowlingMaterial.Radius.Value));
        Assert.Equal((ushort)17408, BitConverter.HalfToUInt16Bits(bowlingMaterial.Mass.Value));
        Assert.Equal((ushort)12411, BitConverter.HalfToUInt16Bits(bowlingMaterial.Bounce.Value));
        Assert.Equal((ushort)10158, BitConverter.HalfToUInt16Bits(bowlingMaterial.Rolling.Value));
        var ball = registry.Create(WorkshopPartKind.Basketball);
        var bowling = registry.Create(WorkshopPartKind.BowlingBall);
        try
        {
            godot.Tree.Root.AddChild(ball); godot.Tree.Root.AddChild(bowling);
            static float Radius(MachinePart part) => part.Visual.GetChildren().OfType<MeshInstance3D>()
                .Select(n => n.Mesh).OfType<SphereMesh>().Single().Radius;
            Assert.Equal((float)material.Radius.Value, Radius(ball));
            Assert.Equal((float)bowlingMaterial.Radius.Value, Radius(bowling)); // visible art size equals the declared collision radius
            Assert.Equal(new Color(0.27f, 0.39f, 0.61f), bowling.Definition.Color);
        }
        finally { ball.GetParent()?.RemoveChild(ball); ball.Free(); bowling.GetParent()?.RemoveChild(bowling); bowling.Free(); }
    }

    [Fact]
    public void DelayArtworkUsesSharedFullTurnAndSourcePhasePalette()
    {
        var registry=new PartRegistry(); registry.Discover();
        var delay=(DelayPart)registry.Create(WorkshopPartKind.Delay);
        try
        {
            godot.Tree.Root.AddChild(delay);
            var hand=delay.Visual.GetNode<Node3D>("CountdownHand");
            var indicator=delay.Visual.GetChildren().OfType<MeshInstance3D>()
                .Single(mesh=>mesh.Position==new Vector3(0,-.35f,.37f));
            foreach(var (phase,progress,colour) in new[] {
                (CuriousContraptions.Presentation.AnimationTimerPhase.Ready,(Half)0,new Color("#556573")),
                (CuriousContraptions.Presentation.AnimationTimerPhase.Counting,(Half).25,new Color("#e8b764")),
                (CuriousContraptions.Presentation.AnimationTimerPhase.Finished,(Half)1,new Color("#f7cb52")) })
            {
                delay.ApplyCosmetic(new(progress,phase));
                var actual=((StandardMaterial3D)indicator.MaterialOverride).AlbedoColor;
                Assert.Equal((float)(Half)colour.R,actual.R); Assert.Equal((float)(Half)colour.G,actual.G); Assert.Equal((float)(Half)colour.B,actual.B);
                Assert.InRange(Math.Abs(hand.Rotation.Z-(float)(Half)((double)(Half)(-Math.Tau)*(double)progress)),0,.00001);
            }
            using var duration=new SpinBox {MinValue=(double)(Half).1,MaxValue=12,Step=0,Value=8.9375};
            Assert.Equal(new DelayDuration(new((Half)8.9375)),DelayDuration.FromInput(duration.Value));
            duration.Apply();
            Assert.Equal(8.9375,duration.Value);
            duration.GetLineEdit().Text="4";
            Assert.Equal(8.9375,duration.Value);
            duration.Apply();
            Assert.Equal(4,duration.Value);
        }
        finally {delay.GetParent()?.RemoveChild(delay);delay.Free();}
    }

    [Fact]
    public void ReceiverHaloFollowsTheDeclaredCaptureBlendAndRestsOnItsNeutralPalette()
    {
        var registry=new PartRegistry(); registry.Discover();
        var receiver=(BasketPart)registry.Create(WorkshopPartKind.Receiver);
        try
        {
            godot.Tree.Root.AddChild(receiver);
            var halo=receiver.Visual.GetChildren().OfType<MeshInstance3D>().Single(mesh=>mesh.Mesh is TorusMesh);
            var material=(StandardMaterial3D)halo.MaterialOverride;
            var neutral=new Color("#bdf4bd");
            Assert.True(receiver.HasCosmeticBindings); Assert.Equal(CosmeticCurves.Receiver,receiver.Cosmetic);
            Assert.Equal(CosmeticCurves.Receiver,WorkshopInput.Receiver(new(2),0,1,0,0,0,0,1).Cosmetic);
            receiver.ApplyCosmetic(WorkshopCosmeticSample.Neutral); // construction/Reset restore the declared neutral palette
            Assert.Equal((float)(Half)neutral.R,material.AlbedoColor.R); Assert.Equal((float)(Half)neutral.G,material.AlbedoColor.G);
            receiver.ApplyCosmetic(new((Half)1,CuriousContraptions.Presentation.AnimationTimerPhase.None));
            Assert.Equal(1,material.AlbedoColor.R); Assert.Equal(1,material.AlbedoColor.G); Assert.Equal(1,material.AlbedoColor.B);
            receiver.ApplyCosmetic(new((Half).5,CuriousContraptions.Presentation.AnimationTimerPhase.None));
            Assert.InRange(material.AlbedoColor.R,(float)(Half)neutral.R+.01f,.99f);
            receiver.ApplyCosmetic(new(Half.NaN,CuriousContraptions.Presentation.AnimationTimerPhase.None));
            Assert.InRange(material.AlbedoColor.R,(float)(Half)neutral.R+.01f,.99f); // game-grade: invalid blends keep the committed value
            receiver.ApplyCosmetic(WorkshopCosmeticSample.Neutral);
            Assert.Equal((float)(Half)neutral.R,material.AlbedoColor.R); Assert.Equal((float)(Half)neutral.B,material.AlbedoColor.B);
        }
        finally { receiver.GetParent()?.RemoveChild(receiver); receiver.Free(); }
    }

    [Fact]
    public void BumperAndSwitchArtworkFollowOneDeclaredBlendAndHoldOnInvalidSamples()
    {
        var registry=new PartRegistry(); registry.Discover();
        var bumper=(BumperPart)registry.Create(WorkshopPartKind.PinballBumper);
        var toggle=(SwitchPart)registry.Create(WorkshopPartKind.ImpactSwitch);
        try
        {
            godot.Tree.Root.AddChild(bumper); godot.Tree.Root.AddChild(toggle);
            var head=bumper.Visual.GetNode<MeshInstance3D>("BumperHead");
            var ring=bumper.Visual.GetNode<MeshInstance3D>("ImpactRing");
            var button=toggle.Visual.GetChildren().OfType<MeshInstance3D>().Single(mesh=>mesh.Mesh is CylinderMesh);
            Assert.Equal(CosmeticCurves.PinballBumper,bumper.Cosmetic); Assert.Equal(CosmeticCurves.ImpactSwitch,toggle.Cosmetic);
            var active=new WorkshopCosmeticSample((Half)1,CuriousContraptions.Presentation.AnimationTimerPhase.None);
            bumper.ApplyCosmetic(active); toggle.ApplyCosmetic(active);
            Assert.Equal(Vector3.One*(float)(Half).88,head.Scale); Assert.Equal(Vector3.One*(float)(Half)1.24,ring.Scale);
            Assert.Equal((float)(Half)(82d/255),((StandardMaterial3D)ring.MaterialOverride).AlbedoColor.B);
            var pressed=new Color("#bff5b0");
            Assert.Equal((float)(Half)(-.02),button.Position.Y);
            Assert.Equal((float)(Half)pressed.G,((StandardMaterial3D)button.MaterialOverride).AlbedoColor.G);
            bumper.ApplyCosmetic(new(Half.NaN,CuriousContraptions.Presentation.AnimationTimerPhase.None));
            bumper.ApplyCosmetic(new((Half)1.5,CuriousContraptions.Presentation.AnimationTimerPhase.None));
            Assert.Equal(Vector3.One*(float)(Half).88,head.Scale); // game-grade: invalid blends keep the committed value
            bumper.ApplyCosmetic(WorkshopCosmeticSample.Neutral); toggle.ApplyCosmetic(WorkshopCosmeticSample.Neutral);
            Assert.Equal(Vector3.One,head.Scale); Assert.Equal(Vector3.One,ring.Scale);
            Assert.Equal((float)(Half)(194d/255),((StandardMaterial3D)ring.MaterialOverride).AlbedoColor.B);
            Assert.Equal((float)(Half).06,button.Position.Y);
            Assert.Equal((float)(Half)toggle.Definition.Color.R,((StandardMaterial3D)button.MaterialOverride).AlbedoColor.R);
        }
        finally
        {
            bumper.GetParent()?.RemoveChild(bumper); bumper.Free();
            toggle.GetParent()?.RemoveChild(toggle); toggle.Free();
        }
    }

    private partial class ProbePart : MachinePart
    {
        public Action<ProbePart>? Bindings;
        protected override void Build() { PickRadius=.5f; Bindings?.Invoke(this); }
        public void Bind(CosmeticCurveDeclaration declaration,CuriousContraptions.Presentation.AnimationTimerPhase phase=CuriousContraptions.Presentation.AnimationTimerPhase.None)
        {
            var mesh=PartArt.Sphere(Visual,.1f,Colors.White);
            if (phase==CuriousContraptions.Presentation.AnimationTimerPhase.None) BindVisual(declaration,mesh,WorkshopVisualProperty.AlbedoRed,(Half)0,(Half)1);
            else BindPhaseVisual(declaration,mesh,WorkshopVisualProperty.AlbedoRed,phase,(Half)1);
        }
    }

    [Fact]
    public void ArtworkBindingsRequireOneDeclaredCurveAndTimerPhasesOnlyForTimers()
    {
        var registry=new PartRegistry(); registry.Discover();
        var definition=registry.Definitions[WorkshopPartKind.SignalLamp];
        foreach (var bindings in new Action<ProbePart>[] {
            probe=>probe.Bind(CosmeticCurveDeclaration.None),
            probe=>{ probe.Bind(CosmeticCurves.ImpactSwitch); probe.Bind(CosmeticCurves.PinballBumper); },
            probe=>probe.Bind(CosmeticCurves.ImpactSwitch,CuriousContraptions.Presentation.AnimationTimerPhase.Counting) })
        {
            var rejected=new ProbePart { Bindings=bindings }; rejected.Configure(definition);
            try { Assert.Throws<ArgumentException>(rejected.EnsureConstructed); } finally { rejected.Free(); }
        }
        var accepted=new ProbePart { Bindings=probe=>{ probe.Bind(CosmeticCurves.Delay); probe.Bind(CosmeticCurves.Delay,CuriousContraptions.Presentation.AnimationTimerPhase.Finished); } };
        accepted.Configure(definition);
        try { accepted.EnsureConstructed(); Assert.True(accepted.HasCosmeticBindings); Assert.Equal(CosmeticCurves.Delay,accepted.Cosmetic); }
        finally { accepted.Free(); }
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
        var client = new HeldWorkshopClient();
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
        public bool TryCosmeticFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out WorkshopCosmeticSample sample) { sample = default; return false; }
        public void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene) { }
        public bool TryElectricalFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out ElectricalIndicatorSample sample) { sample = default; return false; }
        public Task<WorkshopDelivery> CancelPending() => throw new InvalidOperationException("No cancellable test transport request.");
        public ValueTask DisposeAsync() { _disposed = true; return ValueTask.CompletedTask; }
    }
}
