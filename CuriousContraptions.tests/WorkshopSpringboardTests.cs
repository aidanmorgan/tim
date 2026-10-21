using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class WorkshopSpringboardTests(NativeSceneFixture godot)
{


    [Theory]
    [InlineData(.3)]
    [InlineData(-.5)]
    public void ForgivingSpringForwardReceiverBoundaryUsesCompiledSourceScene(double offset)
    {
        var lesson = SpringboardLessons.Create(WorkshopPuzzleId.SpringForward, new(1), WorkshopCadenceSettings.Default(),
            new(1), new(2), new(3), new((Half)0));
        var construction = lesson.WithInstance(WorkshopInput.Springboard(new(3), -3, .8, 0,
            0, 0, -Math.Sin(Math.PI / 18), Math.Cos(Math.PI / 18), SpringboardSettings.Default));
        var original = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var bodies = original.Bodies.ToArray();
        var receiverIndex = Array.FindIndex(bodies, b => b.Id == construction.Receiver!.Value.Id);
        var shifted = WorkshopInput.Receiver(new(2), -.7 + offset, .55, 0, 0, 0, 0, 1);
        bodies[receiverIndex] = bodies[receiverIndex] with { Cell = shifted.Cell, Local = shifted.Local };
        var scene = new PhysicsSceneDeclaration(original.Document, original.NextIdentity, bodies,
            original.Colliders, original.Materials, original.Sensors, original.Guides,
            original.Triggers, original.ContactWorks, original.OrientationSensors,
            prismatics: original.Prismatics);
        Assert.Equal((Half).3, construction.Receiver!.Value.Capture.Margin.Value);
        Assert.Equal((Half)12, construction.Receiver!.Value.ForceRegion.MaximumAcceleration.Value);
        var bytes = PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        // Supplemental immutable-fixture boundary: the player cannot move this locked Receiver, but the compiled scene
        // admits it at the shifted pose. (The former SPRING_COMPILED_FIXTURES export fed the deleted JS-kernel harness.)
        Assert.Equal(scene.World.ByteLength, bytes.Length);
    }


    [Theory]
    [InlineData(WorkshopPuzzleId.SpringForward, "spring_forward")]
    [InlineData(WorkshopPuzzleId.ReadyToRebound, "ready_to_rebound")]
    public void LaunchLessonsPreserveSourceFixturesAssistanceAndNamedCapture(WorkshopPuzzleId lesson, string sourceId)
    {
        using var source = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetString() == sourceId);
        var initial = SpringboardLessons.Create(lesson, new(1), WorkshopCadenceSettings.Default(),
            new(1), new(2), new(3), new((Half)1));
        foreach (var fixture in level.GetProperty("parts").EnumerateArray())
        {
            IWorkshopInstance actual = fixture.GetProperty("kind").GetString() switch
            {
                "ball" => initial.Ball!.Value,
                "basket" => initial.Receiver!.Value,
                "spring" => initial.Instances.OfType<WorkshopSpringboard>().Single(),
                _ => throw new InvalidDataException("Unexpected source fixture.")
            };
            var position = fixture.GetProperty("position");
            Assert.Equal(WorkshopInput.Position(position[0].GetDouble()).Local, actual.Local.X);
            Assert.Equal(WorkshopInput.Position(position[1].GetDouble()).Local, actual.Local.Y);
            Assert.Equal(WorkshopInput.Position(position[2].GetDouble()).Local, actual.Local.Z);
            Assert.Equal(new CellOrigin(WorkshopInput.Position(position[0].GetDouble()).Cell,
                WorkshopInput.Position(position[1].GetDouble()).Cell, WorkshopInput.Position(position[2].GetDouble()).Cell), actual.Cell);
            Assert.True(actual.Locked);
            var basis = new Basis(new Quaternion(actual.Rotation.X, actual.Rotation.Y, actual.Rotation.Z, actual.Rotation.W));
            for (var column = 0; column < 3; column++)
                for (var row = 0; row < 3; row++)
                    Assert.InRange(Math.Abs(basis[column][row] - fixture.GetProperty("orientation")[column * 3 + row].GetDouble()), 0, 2e-7);
            Profile(fixture.GetProperty("difficulty"), actual.PartId switch
            {
                var id when id == PartId.Basketball => initial.Puzzle.BallAssistance,
                var id when id == PartId.Receiver => initial.Puzzle.ReceiverAssistance,
                var id when id == PartId.Springboard => initial.Puzzle.RampAssistance,
                _ => throw new InvalidDataException()
            });
        }
        var ready = lesson == WorkshopPuzzleId.ReadyToRebound;
        Profile(level.GetProperty("solution")[0].GetProperty("difficulty"),
            ready ? initial.Puzzle.ReceiverAssistance : initial.Puzzle.RampAssistance);
        var inventory = WorkshopInventoryPolicy.Authored(initial.Puzzle);
        Assert.Single(inventory);
        Assert.Equal(PartAllowance.Counted(1), inventory[ready ? PartId.Receiver : PartId.Springboard]);
        IWorkshopInstance placed = ready ? WorkshopInput.Receiver(new(3), 1.8, 1.3, 0, 0, 0, 0, 1)
            : WorkshopInput.Springboard(new(3), -3, .8, 0, 0, 0, -Math.Sin(Math.PI / 18), Math.Cos(Math.PI / 18), SpringboardSettings.Default);
        var construction = initial.WithInstance(placed); construction.Validate();
        Assert.Equal(WorkshopGoalKind.Captured, construction.Puzzle.Goal.Kind);
        Assert.Equal(WorkshopPhysicsCompiler.CaptureSensor(construction.Receiver!.Value), construction.Puzzle.Goal.EventSource);
        foreach (var precision in new Half[] { (Half)0, (Half).45, (Half)1 })
        {
            var next = SpringboardLessons.WithPrecision(construction, new(precision));
            var receiver = next.Receiver!.Value;
            Assert.Equal((Half)(precision == (Half)0 ? .3 : precision == (Half)1 ? .02 : .174), receiver.Capture.Margin.Value);
            Assert.Equal((Half)(ready ? 0 : precision == (Half)0 ? 12 : precision == (Half)1 ? 0 : 6.6), receiver.ForceRegion.MaximumAcceleration.Value);
            var saved = new WorkshopSavedConstruction(next, new(4));
            Assert.Equal(saved, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(saved)));
            Assert.Single(WorkshopPhysicsCompiler.Compile(next, new(1,2)).Prismatics.ToArray());
        }
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => (construction with { Puzzle = construction.Puzzle with { InventoryCount = 2 } }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => (construction with { Puzzle = construction.Puzzle with { RampAssistance = default } }).Validate());
        if (ready)
        {
            var removed = construction.WithoutInstance(new(3)); removed.Validate();
            var replaced = removed.WithInstance(WorkshopInput.Receiver(new(4), 1.8, 1.3, 0, 0, 0, 0, 1)); replaced.Validate();
            Assert.Equal(new GpuBodyId(4), replaced.Puzzle.Goal.Target);
            Assert.Throws<WorkbenchFullException>(() => replaced.WithInstance(WorkshopInput.Receiver(new(5), 2, 1, 0, 0, 0, 0, 1)).Validate());
        }
        static void Profile(System.Text.Json.JsonElement data, AssistanceProfile expected)
        {
            var knots = new[] { expected.Forgiving, expected.Balanced, expected.Precise };
            string[] names = ["precision", "position_window", "rotation_window", "max_position_correction",
                "max_rotation_correction", "blend_seconds", "capture_margin", "capture_speed", "capture_dwell",
                "guide_acceleration", "trigger_threshold"];
            for (var i = 0; i < 3; i++)
            {
                var k = knots[i];
                Half[] values = [k.Precision,k.PositionWindow.Value,k.RotationWindowDegrees,k.MaximumPositionCorrection.Value,
                    k.MaximumRotationCorrectionDegrees,k.Blend.Value,k.CaptureMargin.Value,k.CaptureSpeed.Value,k.CaptureDwell.Value,
                    k.GuideAcceleration.Value,k.TriggerThreshold];
                for (var j = 0; j < names.Length; j++) Assert.Equal((Half)data[i].GetProperty(names[j]).GetDouble(), values[j]);
            }
        }
    }


    // Story 9A.14: the catalogue resource and its untyped Parameters are gone; a definition carries only its bundle declaration.
    [Fact]
    public void UnsupportedWireTailRejectsBeforeMutation()
    {
        var definition = new PartDefinition(PartId.Springboard, "Springboard", "A springboard.", CuriousContraptions.Abstractions.ElementCategory.Motion,
            Colors.White, "res://elements/Springboard/icon.svg", null, null);
        var part = new MachinePart();
        try
        {
            part.Configure(definition);
            Assert.Same(definition, part.Definition);
            var saved = new WorkshopSavedConstruction(Construction(), new(3));
            var accepted = WorkshopSaveCodec.Encode(saved);
            foreach (var invalidSettings in new[] {
                new SpringboardSettings(50f, .2f, 0f),
                new SpringboardSettings(1500f, .2f, 0f),
                new SpringboardSettings(400f, -1f, 0f),
                new SpringboardSettings(400f, 10f, 0f),
                new SpringboardSettings(400f, .2f, -.1f),
                new SpringboardSettings(400f, .2f, .5f),
                new SpringboardSettings(float.NaN, .2f, 0f) })
            {
                var malformedConstruction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
                    new(WorkshopInput.Basketball(new(1), 0, 4, 0, 0, 0, 0, 1), Board(invalidSettings)));
                var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
                var crOffset = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, malformedConstruction, validate: false);
                var savedBytes = SaveFixture.Finish(builder, crOffset, 3);
                SaveFixture.Refused(float.IsFinite(invalidSettings.Stiffness) ? WorkshopSaveRejection.OutOfRange : WorkshopSaveRejection.NonFinite, () => WorkshopSaveCodec.Decode(savedBytes));
                Assert.Equal(saved, WorkshopSaveCodec.Decode(accepted));
            }
        }
        finally { part.Free(); }
    }
    private static WorkshopSpringboard Board(SpringboardSettings settings)
    {
        var px = WorkshopInput.Position(0); var py = WorkshopInput.Position(1); var pz = WorkshopInput.Position(0);
        return new(new(2), new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local), CanonicalRotation.Identity, settings);
    }
    private static WorkshopConstruction Construction(SpringboardSettings? settings = null) => new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Basketball(new(1), 0, 4, 0, 0, 0, 0, 1),
            WorkshopInput.Springboard(new(2), 0, 1, 0, 0, 0, 0, 1, settings ?? SpringboardSettings.Default)));

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(2.5f, -30f)]
    public void CompiledPreloadUsesActualF32PoseAndEightJoules(float height, float angle)
    {
        var q = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitZ, angle * MathF.PI / 180);
        var board = WorkshopInput.Springboard(new(2), 0, height, 0, q.X, q.Y, q.Z, q.W, new(400, .2f, .2f));
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(board));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1,2));
        var joint = Assert.Single(scene.Prismatics.ToArray());
        var a = scene.Bodies.ToArray().Single(value => value.Id == joint.BodyA);
        var b = scene.Bodies.ToArray().Single(value => value.Id == joint.BodyB);
        static System.Numerics.Vector3 Position(RigidBodyDeclaration body) =>
            new((body.Cell.X + body.Local.X) / 16f, (body.Cell.Y + body.Local.Y) / 16f, (body.Cell.Z + body.Local.Z) / 16f);
        static ConstraintBodyFrame Frame(RigidBodyDeclaration body) => new(Position(body),
            new(body.Rotation.X, body.Rotation.Y, body.Rotation.Z, body.Rotation.W), Position(body));
        Span<PrismaticRow> rows = stackalloc PrismaticRow[PrismaticRows.Count];
        PrismaticRows.Prepare(Frame(a), Frame(b), joint.FrameA, joint.FrameB, joint.Lower, joint.Upper, rows);
        var displacement = rows[(int)PrismaticRowKind.Spring].Error;
        Assert.InRange(.5f * joint.Stiffness * displacement * displacement, 7.99999f, 8.00001f);
        var state = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        PhysicsGpuAbi.ValidateCandidate(state, state, new(0));
        var bodies = PhysicsGpuAbi.ReadDynamicBodies(state); Assert.Equal(1, bodies.Count);
        var actual = bodies[0];
        Assert.Equal(b.Local, actual.Body.Local); Assert.Equal(b.Rotation, actual.Rotation);
        var saved = new WorkshopSavedConstruction(construction, new(3));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(saved)));
    }

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
        var badConstruction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(WorkshopInput.Basketball(new(1), 0, 4, 0, 0, 0, 0, 1), Board(new(float.NaN, damping))));
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var crOffset = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, badConstruction, validate: false);
        var savedBytes = SaveFixture.Finish(builder, crOffset, 3);
        SaveFixture.Refused(WorkshopSaveRejection.NonFinite, () => WorkshopSaveCodec.Decode(savedBytes));
        Assert.Equal(save, WorkshopSaveCodec.Decode(encoded));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.1234567f)]
    [InlineData(.2f)]
    public void PreloadResourceSaveAndPhysicalRestPreserveExactF32(float compression)
    {
        var settings = new SpringboardSettings(400, .2f, compression);
        settings.Validate();
        Assert.Equal(compression, settings.InitialCompression);
        var saved = new WorkshopSavedConstruction(Construction(settings), new(3));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(saved)));
        var scene = WorkshopPhysicsCompiler.Compile(saved.Construction, new(1, 2));
        var plate = scene.Bodies.ToArray().Single(body => body.Id == WorkshopPhysicsCompiler.SpringboardPlate(new(2)));
        Assert.InRange(MathF.Abs((plate.Cell.Y + plate.Local.Y) / 16f - (1.14f - compression)), 0, 1e-7f);
    }

    [Theory]
    [InlineData(-.000001f)]
    [InlineData(.200001f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void InvalidPreloadRejectsResourceAndSaveBeforeAcceptedStateChanges(float compression)
    {
        var part = new MachinePart();
        try
        {
            EditRefusal.Is(WorkshopEditRejection.OutOfRange, () => part.ApplySpring(new SpringboardSettings(400, .2f, compression)));
            part.ApplySpring(new SpringboardSettings(400, .2f, .2f));
            Assert.Equal(.2f, part.CanonicalSpring.InitialCompression);
            var saved = new WorkshopSavedConstruction(Construction(new(400, .2f, .2f)), new(3));
            var accepted = WorkshopSaveCodec.Encode(saved);
            var badConstruction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
                new(WorkshopInput.Basketball(new(1), 0, 4, 0, 0, 0, 0, 1), Board(new(400, .2f, compression))));
            var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
            var crOffset = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, badConstruction, validate: false);
            var savedBytes = SaveFixture.Finish(builder, crOffset, 3);
            SaveFixture.Refused(float.IsFinite(compression) ? WorkshopSaveRejection.OutOfRange : WorkshopSaveRejection.NonFinite, () => WorkshopSaveCodec.Decode(savedBytes));
            Assert.Equal(saved, WorkshopSaveCodec.Decode(accepted));
        }
        finally { part.Free(); }
    }

    [Fact]
    public void CompiledPopulationsCountBaseAndPlateAndAdmitTheNextPart()
    {
        var parts = Enumerable.Range(1, 16).Select(i => (IWorkshopInstance)WorkshopInput.Springboard(new((ulong)i),
            i, 1, 0, 0, 0, 0, 1, SpringboardSettings.Default)).ToArray();
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(parts));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        Assert.Equal(33, scene.Bodies.Length); Assert.Equal(16, scene.Prismatics.Length);
        // Story 9R.5b: the retired 16-row prismatic and 33-row body tables no longer refuse the seventeenth Springboard.
        var seventeen = construction.WithInstance(WorkshopInput.Springboard(new(17), 0, 1, 1, 0, 0, 0, 1, SpringboardSettings.Default));
        seventeen.Validate();
        var grown = WorkshopPhysicsCompiler.Compile(seventeen, new(1, 2));
        Assert.Equal(35, grown.Bodies.Length); Assert.Equal(17, grown.Prismatics.Length);
    }

    [Fact]
    public void PalettePreviewHasArtWithoutInventedIdentityAndOwnedPlateResets()
    {
        var registry = new PartRegistry(); registry.Load();
        var part = Assert.IsType<MachinePart>(registry.Create(PartId.Springboard));
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.Empty(WorkshopPorts.For(PartId.Springboard).ToArray());
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
        await using var client = new PresentationClient();
        try
        {
            godot.Tree.Root.AddChild(world);
            world.Registry.Load(); // the Workshop loads the bundle at start-up (Story 9A.14)
            await world.InitializeWorkshop(() => Task.FromResult<IWorkshopClient>(client));
            var parts = (List<MachinePart>)typeof(MachineWorld).GetField("_parts", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(world)!;
            var ball = world.Registry.Create(PartId.Basketball); ball.AuthoredId = new(1); world.AddChild(ball); ball.EnsureConstructed(); parts.Add(ball);
            var board = world.Registry.Create(PartId.Springboard); board.AuthoredId = new(2); world.AddChild(board); board.EnsureConstructed(); parts.Add(board);
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
                var other = world.Registry.Create(PartId.Springboard); other.AuthoredId = new(2); world.AddChild(other); other.EnsureConstructed();
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
