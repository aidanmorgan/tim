using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public sealed class WorkshopSpringboardTests(NativeSceneFixture godot)
{

    [Theory]
    [InlineData("strength", 8.5f)]
    [InlineData("initial_compression", .2f)]
    public void ObsoleteResourceParametersAndUnsupportedWireTailRejectBeforeMutation(string externalKey, float value)
    {
        using var resource = new SpringboardResource { Stiffness = 400, Damping = .2f };
        using var definition = new PartDefinition { Id = "spring", Springboard = resource };
        definition.Parameters[externalKey] = value; // Deliberately invalid legacy resource boundary, not a domain key.
        var part = new SpringboardPart();
        try
        {
            Assert.Throws<ArgumentException>(() => part.Configure(definition));
            Assert.Null(part.Definition);
            definition.Parameters.Clear();
            part.Configure(definition);
            Assert.Same(definition, part.Definition);
            var saved = new WorkshopSavedConstruction(Construction(), new(3));
            var accepted = WorkshopSaveCodec.Encode(saved);
            foreach (var tail in new[] { 112, 156 })
            {
                var malformed = accepted.ToArray();
                BinaryPrimitives.WriteSingleLittleEndian(malformed.AsSpan(24 + WorkshopWire.ConstructionHeaderBytes + WorkshopWire.InstanceBytes + tail), value);
                Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(malformed));
                Assert.Equal(saved, WorkshopSaveCodec.Decode(accepted));
            }
        }
        finally { part.Free(); }
    }
    private static WorkshopConstruction Construction(SpringboardSettings? settings = null) => new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Basketball(new(1), 0, 4, 0, 0, 0, 0, 1),
            WorkshopInput.Springboard(new(2), 0, 1, 0, 0, 0, 0, 1, settings ?? SpringboardSettings.Default)));

    [Fact]
    public void CompilesDistinctPlateIdentityAndFullDynamicPopulation()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction(), new(1, 2));
        var joint = Assert.Single(scene.Prismatics.ToArray());
        Assert.Equal(new GpuBodyId(2), joint.BodyA);
        Assert.Equal(WorkshopPhysicsCompiler.SpringboardPlate(new(2)), joint.BodyB);
        Assert.True(joint.BodyB.Value > uint.MaxValue);
        Assert.Equal(ConnectedCollision.Disabled, joint.Collision);
        Assert.Equal(-.25f, joint.Lower); Assert.Equal(0f, joint.Upper);
        var plate = scene.Bodies.ToArray().Single(body => body.Id == joint.BodyB);
        Assert.Equal((Half).25, plate.Mass.Value);
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var read = PhysicsGpuAbi.ReadDynamicBodies(bytes);
        Assert.Equal(2, read.Count);
        read.ValidateScene(scene, new(1), new(0));
        var missing = new PhysicsBodyReadSet([read[0]]);
        Assert.Throws<ArgumentException>(() => missing.ValidateScene(scene, new(1), new(0)));
        Assert.Throws<ArgumentException>(() => new PhysicsBodyReadSet([read[0], read[0]]));
        var foreign = read[1] with { Body = read[1].Body with { Id = new(999) } };
        Assert.Throws<ArgumentException>(() => new PhysicsBodyReadSet([read[0], foreign]).ValidateScene(scene, new(1), new(0)));
    }

    [Theory]
    [InlineData(120f, 0f)]
    [InlineData(400.00003f, .20000002f)]
    [InlineData(1200f, 8f)]
    public void CurrentSaveAndWirePreserveF32Settings(float stiffness, float damping)
    {
        var construction = Construction(new(stiffness, damping));
        var save = new WorkshopSavedConstruction(construction, new(3));
        var encoded = WorkshopSaveCodec.Encode(save);
        Assert.Equal(save, WorkshopSaveCodec.Decode(encoded));
        var source = encoded.ToArray();
        var springSlot = 24 + WorkshopWire.ConstructionHeaderBytes + WorkshopWire.InstanceBytes;
        BinaryPrimitives.WriteSingleLittleEndian(encoded.AsSpan(springSlot + 104), float.NaN);
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(encoded));
        Assert.Equal(save, WorkshopSaveCodec.Decode(source));
    }

    [Fact]
    public void CompiledCapacitiesCountBaseAndPlateAndRejectNextPart()
    {
        var parts = Enumerable.Range(1, 16).Select(i => (IWorkshopInstance)WorkshopInput.Springboard(new((ulong)i),
            i, 1, 0, 0, 0, 0, 1, SpringboardSettings.Default)).ToArray();
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(parts));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        Assert.Equal(33, scene.Bodies.Length); Assert.Equal(16, scene.Prismatics.Length);
        Assert.Throws<WorkbenchFullException>(() => construction.WithInstance(
            WorkshopInput.Springboard(new(17), 0, 1, 1, 0, 0, 0, 1, SpringboardSettings.Default)).Validate());
    }

    [Fact]
    public void PalettePreviewHasArtWithoutInventedIdentityAndOwnedPlateResets()
    {
        var registry = new PartRegistry(); registry.Discover();
        var part = Assert.IsType<SpringboardPart>(registry.Create(WorkshopPartKind.Springboard));
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.Empty(WorkshopPorts.For(WorkshopPartKind.Springboard).ToArray());
            part.EnsureConstructed(); // The real tool preview has no authored identity yet.
            Assert.Equal(0UL, part.AuthoredId.Value);
            Assert.Null(part.PhysicalVisual(default));
            part.AuthoredId = new(2);
            var plateId = WorkshopPhysicsCompiler.SpringboardPlate(part.AuthoredId);
            Assert.Throws<ArgumentException>(() => part.InstallPhysicalIdentity(PhysicalVisualSlot.Secondary, new(999)));
            part.InstallPhysicalIdentity(PhysicalVisualSlot.Secondary, plateId);
            var plate = Assert.IsAssignableFrom<Node3D>(part.PhysicalVisual(plateId));
            foreach (var offset in new[] { -2, -1, 1, 2, 3, 4, 61 })
            {
                var nonBody = new GpuBodyId(checked((ulong)((long)plateId.Value + offset)));
                Assert.Throws<ArgumentException>(() => part.InstallPhysicalIdentity(PhysicalVisualSlot.Secondary, nonBody));
                Assert.Null(part.PhysicalVisual(nonBody));
                Assert.Same(plate, part.PhysicalVisual(plateId));
            }
            var coil = part.Visual.GetNode<Node3D>("Coil");
            var initialPlate = plate.Transform; var initialCoil = coil.Transform;
            plate.Position += new Vector3(0, -.2f, 0); part.ApplyPhysicalSpans();
            Assert.True(coil.Scale.Y < initialCoil.Basis.Scale.Y);
            part.ResetPhysicalVisuals();
            Assert.Equal(initialPlate, plate.Transform); Assert.Equal(initialCoil, coil.Transform);
            Assert.Same(part, part.PhysicalVisual(part.AuthoredId));
        }
        finally { part.Free(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WholeWorldRejectsMissingOrDuplicateBindingBeforeAnyPresentationMutation(bool duplicate)
    {
        var world = new MachineWorld();
        var client = new PresentationClient();
        try
        {
            godot.Tree.Root.AddChild(world);
            await world.InitializeWorkshop(() => Task.FromResult<IWorkshopClient>(client));
            var parts = (List<MachinePart>)typeof(MachineWorld).GetField("_parts", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(world)!;
            var ball = world.Registry.Create(WorkshopPartKind.Basketball); ball.AuthoredId = new(1); world.AddChild(ball); ball.EnsureConstructed(); parts.Add(ball);
            var board = world.Registry.Create(WorkshopPartKind.Springboard); board.AuthoredId = new(2); world.AddChild(board); board.EnsureConstructed(); parts.Add(board);
            var plateId = WorkshopPhysicsCompiler.SpringboardPlate(new(2));
            board.InstallPhysicalIdentity(PhysicalVisualSlot.Secondary, plateId);
            var plate = board.PhysicalVisual(plateId)!;
            client.Sample = new(new([new(new(1), new(16,64,0), default, CanonicalRotation.Identity),
                new(plateId, new(0,16,0), default, CanonicalRotation.Identity)]), null, new(1), PresentationQuality.Interpolated);
            client.Available = true; world._Process(0);
            var oldBall = ball.Transform; var oldPlate = plate.Transform; var oldCoil = board.Visual.GetNode<Node3D>("Coil").Transform;
            var field = typeof(MachineWorld).GetField("_workshopPresentation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var oldPresentation = (WorkshopPresentationSample)field.GetValue(world)!;
            if (duplicate)
            {
                var other = world.Registry.Create(WorkshopPartKind.Springboard); other.AuthoredId = new(2); world.AddChild(other); other.EnsureConstructed();
                other.InstallPhysicalIdentity(PhysicalVisualSlot.Secondary, plateId); parts.Add(other);
            }
            client.Sample = new(new([new(new(1), new(80,80,0), default, CanonicalRotation.Identity),
                new(duplicate ? plateId : new GpuBodyId(999), new(0,80,0), default, CanonicalRotation.Identity)]), null, new(99), PresentationQuality.Interpolated);
            world._Process(0);
            Assert.Equal(WorkshopSimulationPhase.Faulted, world.WorkshopPhase);
            Assert.Equal(oldBall, ball.Transform); Assert.Equal(oldPlate, plate.Transform); Assert.Equal(oldCoil, board.Visual.GetNode<Node3D>("Coil").Transform);
            Assert.Equal(1, world.DisplaySimulationTime);
            var retained = (WorkshopPresentationSample)field.GetValue(world)!;
            Assert.Equal(oldPresentation.SimulationTime, retained.SimulationTime);
            Assert.Equal(oldPresentation.Evidence, retained.Evidence); Assert.Equal(oldPresentation.Quality, retained.Quality);
            Assert.Equal(oldPresentation.Bodies.Count, retained.Bodies.Count);
            for (var i = 0; i < retained.Bodies.Count; i++) Assert.Equal(oldPresentation.Bodies[i], retained.Bodies[i]);
        }
        finally { world.Free(); }
    }

    private sealed class PresentationClient : IWorkshopClient
    {
        public WorkshopPresentationSample Sample { get; set; }
        public bool Available { get; set; }
        public WorkshopCadenceSettings Settings => WorkshopCadenceSettings.Default();
        public SimulationEpoch Epoch => new(1);
        public AuthorityRevision Revision => new(1);
        public WorkshopCommandIdentity? Pending => null;
        public WorkshopTransportState TransportState => WorkshopTransportState.Ready;
        public bool TryPresent(ulong frame, out WorkshopPresentationSample sample) { sample = Sample; return Available; }
        public bool TryRead(out WorkshopResponse response) { response = default; return false; }
        public void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene) { }
        public void ControlUi(WorkshopUiTarget target, AnimationControlKind kind, bool visible) => throw new NotSupportedException();
        public bool TryUiFrame(ulong frame, WorkshopPresentationSample physical, WorkshopUiTarget target, out CuriousContraptions.Presentation.AnimationOpacity opacity) { opacity=default; return false; }
        public bool TryCosmeticFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out WorkshopCosmeticSample sample) { sample=default; return false; }
        public bool TryElectricalFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out ElectricalIndicatorSample sample) { sample=default; return false; }
        public Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction=null, WorkshopCommandIdentity? target=null, WorkshopCadenceSettings? settings=null, WorkshopElectricalControl? electrical=null) => throw new NotSupportedException();
        public Task<WorkshopDelivery> CancelPending() => throw new NotSupportedException();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

}
