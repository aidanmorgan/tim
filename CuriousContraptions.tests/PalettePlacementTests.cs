using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using Godot;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 9R.0c: the Free palette offers only parts with a constructible instance path, undelivered parts never reach it, and a
/// bench placement captures the part before it records undo, so an uncapturable part is one typed rejection and never a throw.
/// </summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class PalettePlacementTests(NativeSceneFixture godot)
{
    private static readonly Vector3 BenchPoint = new(0, 1, 0);

    private static MachineWorld DiscoveredWorld()
    {
        var world = new MachineWorld();
        world.Registry.Load();
        return world;
    }

    [Fact]
    public void EveryFreePalettePartCapturesSizesAndDecodesFromTheSaveAndTheWorkerWire()
    {
        Assert.NotNull(godot.Tree);
        var world = DiscoveredWorld();
        try
        {
            // The list comes from the palette the player sees, never from a hand-written copy.
            var palette = world.Registry.Free.Keys.ToArray();
            Assert.Equal(world.Registry.Definitions.Keys.OrderBy(id => id), palette.OrderBy(id => id));
            var body = 1u;
            foreach (var id in palette)
            {
                var instance = world.CaptureInstance(id, new(body++), BenchPoint, Quaternion.Identity);
                Assert.Equal(id, instance.PartId);
                if (instance is WorkshopBall ball) Assert.Equal(BallMaterial.For(id), ball.Material);
                WorkbenchCapacity.Validate([instance]);
                var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(instance));
                var saved = new WorkshopSavedConstruction(construction, new(body));
                var bytes = WorkshopSaveCodec.Encode(saved);
                var decoded = WorkshopSaveCodec.Decode(bytes);
                Assert.Equal(id, Assert.Single(decoded.Construction.Instances).PartId);
                Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));
                // The bench placement reaches the Simulation worker as a Construct command on the binary wire.
                var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), construction,
                    Session: new(1, 1), Cadence: new(1), Projection: new(1));
                var wire = WorkshopWireFlatBuffers.EncodeCommand(command);
                var received = WorkshopWireFlatBuffers.DecodeCommand(wire).Construction;
                Assert.Equal(id, Assert.Single(Assert.NotNull(received).Instances).PartId);
                Assert.Equal(wire, WorkshopWireFlatBuffers.EncodeCommand(command with { Construction = received }));
            }
        }
        finally { world.Free(); }
    }

    [Fact]
    public void UndeliveredPartsHaveNoPackageNoInstancePathAndNeverReachTheRegistryOrPalette()
    {
        Assert.Equal([UndeliveredPart.Tennis, UndeliveredPart.Balloon], UndeliveredParts.All);
        var world = DiscoveredWorld();
        try
        {
            foreach (var part in UndeliveredParts.All)
            {
                var id = UndeliveredParts.Id(part);
                Assert.True(UndeliveredParts.Contains(id));
                // Story 9A.14 deleted the catalogue resource; Story 12.1 ships the package. Admission alone keeps it out meanwhile
                // (PartRegistryTests proves a bundle entry for it is still refused).
                Assert.DoesNotContain(PartRegistry.Read(Godot.FileAccess.GetFileAsString(PartRegistry.BundlePath)),
                    entry => entry.Metadata.Id == id.Value);
                EditRefusal.Is(WorkshopEditRejection.UnsupportedPart, () => world.CaptureInstance(id, new(1), BenchPoint, Quaternion.Identity));
                Assert.False(world.Registry.Definitions.ContainsKey(id), $"{id} must not be registered.");
                Assert.False(world.Registry.Free.ContainsKey(id), $"{id} must not be offered by the Free palette.");
            }
            Assert.False(UndeliveredParts.Contains(PartId.Basketball));
            Assert.Throws<ArgumentOutOfRangeException>(() => UndeliveredParts.Id((UndeliveredPart)99));
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData("balloon")]
    [InlineData("tennis")]
    [InlineData("no-such-part")]
    public void BenchPlacementOfAnUncapturablePartIsOneTypedRejectionWithoutUndo(string external)
    {
        var world = DiscoveredWorld();
        try
        {
            var before = world.Construction;
            var undo = 0;
            var placement = WorkshopBenchPlacement.Propose(world, new PartId(external), new(7), BenchPoint, Quaternion.Identity,
                null, null, null, () => undo++);
            Assert.Equal(BenchPlacementRejection.UnsupportedPart, placement.Rejection);
            Assert.Contains(external, placement.Message);
            Assert.Empty(godot.Errors.Drain()); // a refusal is not an error: it is logged as a UI rejection on stdout
            Assert.Equal(0, undo);
            Assert.Equal(before, placement.Construction);
            Assert.Equal(before, world.Construction);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void ABenchPlacementDefectIsNotARejectionAndReachesTheHandlerBoundary()
    {
        // No discovered registry: CaptureBall's lookup guard throws a plain ArgumentException, a defect rather than a declared
        // refusal, so Propose must not turn it into a placement rejection (the handler boundary logs it as a fault).
        var world = new MachineWorld();
        try
        {
            var undo = 0;
            var defect = Assert.Throws<ArgumentException>(() => WorkshopBenchPlacement.Propose(world, PartId.Basketball, new(7),
                BenchPoint, Quaternion.Identity, null, null, null, () => undo++));
            Assert.Equal("Unsupported ball kind.", defect.Message);
            Assert.Equal(0, undo);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { world.Free(); }
    }

    [Fact]
    public void BenchPlacementOfAPalettePartProposesItAndRecordsOneUndo()
    {
        var world = DiscoveredWorld();
        try
        {
            var before = world.Construction;
            var undo = 0;
            var placement = WorkshopBenchPlacement.Propose(world, PartId.Ramp, new(7), BenchPoint, Quaternion.Identity,
                null, null, null, () => undo++);
            Assert.Equal(BenchPlacementRejection.None, placement.Rejection);
            Assert.Equal(1, undo);
            Assert.Empty(placement.Message);
            Assert.Equal(PartId.Ramp, Assert.Single(placement.Construction.Instances, i => i.Id == new GpuBodyId(7)).PartId);
            Assert.Equal(before, world.Construction); // proposing never commits; the worker admits the construction
        }
        finally { world.Free(); }
    }

    [Fact]
    public void AProposalWhoseLessonRefusesTheCapturedPartIsATypedRejectionWithoutUndo()
    {
        var world = DiscoveredWorld();
        try
        {
            var lesson = SpringboardLessons.Create(WorkshopPuzzleId.ReadyToRebound, new(1), WorkshopCadenceSettings.Default(),
                new(1), new(2), new(3), new((Half).5));
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(world, lesson);
            var undo = 0;
            // The Receiver captures, but the lesson's Receiver composition refuses an identity outside the authored domain.
            var refused = WorkshopBenchPlacement.Propose(world, PartId.Receiver, new(1UL << 32), BenchPoint, Quaternion.Identity,
                null, null, null, () => undo++);
            Assert.Equal(BenchPlacementRejection.UnsupportedPart, refused.Rejection);
            Assert.StartsWith("Placement rejected: ", refused.Message);
            Assert.Equal("Placement rejected: Authored body identity exceeds its domain.", refused.Message);
            Assert.Empty(godot.Errors.Drain());
            Assert.Equal(0, undo);
            Assert.Equal(lesson, refused.Construction);
            // Control: the same lesson composes its Receiver at an authored identity and records one undo.
            var composed = WorkshopBenchPlacement.Propose(world, PartId.Receiver, new(4), BenchPoint, Quaternion.Identity,
                null, null, null, () => undo++);
            Assert.Equal(BenchPlacementRejection.None, composed.Rejection);
            Assert.Equal(1, undo);
            Assert.Equal(new GpuBodyId(4), composed.Construction.Puzzle.Goal.Target);
        }
        finally { world.Free(); }
    }


    // The actual Workshop scene and its private bench Click (native-only reflection boundary, as in WorkshopHintTests): the defect's
    // precondition is restored by putting the part in the inventory and choosing it, then the bench is clicked at a point on the
    // placement plane. No production setter or test hook exists.
    private const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    private static T Field<T>(Workshop scene, string name) => (T)typeof(Workshop).GetField(name, Private)!.GetValue(scene)!;
    private static void SetField<T>(Workshop scene, string name, T value) => typeof(Workshop).GetField(name, Private)!.SetValue(scene, value);

    private (Workshop Scene, PlacementClient Client) BenchScene(PartId tool, PlacementAnswer answer = PlacementAnswer.Pending)
    {
        var scene = GD.Load<PackedScene>("res://scenes/workshop.tscn").Instantiate<Workshop>();
        godot.Tree.Root.AddChild(scene);
        scene.SetProcess(false);
        scene.SetPhysicsProcess(false);
        var client = new PlacementClient(answer);
        typeof(MachineWorld).GetField("_workshopClient", Private)!.SetValue(scene.World, client);
        typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Building);
        SetField(scene, "_gpuPending", false); // a ready Workshop: natively the browser client never initialises
        SetField(scene, "_buildView", false);
        SetField<IReadOnlyDictionary<PartId, PartAllowance>>(scene, "_inventory", new PartInventoryDictionary((tool, PartAllowance.Unlimited)));
        SetField<PartId?>(scene, "_tool", tool);
        return (scene, client);
    }

    /// <summary>Clicks the bench and returns every exception that escaped the async void handler. An escaping exception is posted
    /// to the captured synchronization context: natively Godot's logs it, but in the browser there is none and the thread pool
    /// terminates the .NET runtime (Story 9R.0c F1), so the click runs under a context that records instead.</summary>
    private static List<Exception> ClickBench(Workshop scene)
    {
        var height = (float)Field<Metres>(scene, "_placementHeight").Value;
        var screen = Field<Camera3D>(scene, "_camera").UnprojectPosition(new Vector3(0, height, 0));
        return InvokeHandler(scene, "Click", screen);
    }

    /// <summary>Invokes one private async void Workshop handler and returns every exception that escaped it.</summary>
    private static List<Exception> InvokeHandler(Workshop scene, string handler, params object[] arguments)
    {
        var recorder = new EscapeRecorder();
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(recorder);
        try { typeof(Workshop).GetMethod(handler, Private)!.Invoke(scene, arguments); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
        return recorder.Escaped;
    }

    private sealed class EscapeRecorder : SynchronizationContext
    {
        public List<Exception> Escaped { get; } = [];
        public override void Post(SendOrPostCallback callback, object? state)
        {
            try { callback(state); }
            catch (Exception escaped) { Escaped.Add(escaped); }
        }
    }

    private void Release(Workshop scene)
    {
        scene.Free();
        godot.Engine.Iteration();
    }

    [Fact]
    public void ClickingTheBenchWithAnUncapturablePartIsOneTypedRejectionWithAVisibleMessageAndNoUndo()
    {
        var (scene, client) = BenchScene(UndeliveredParts.Id(UndeliveredPart.Balloon));
        try
        {
            var before = scene.World.Construction;
            Assert.Empty(ClickBench(scene));
            Assert.Equal("Placement rejected: Unsupported instance part id 'balloon'.", Field<Label>(scene, "_status").Text);
            Assert.Empty(godot.Errors.Drain());
            Assert.Empty(Field<List<WorkshopConstruction>>(scene, "_undo"));
            Assert.Empty(client.Commands);
            Assert.Equal(before, scene.World.Construction);
            Assert.Equal(new GpuBodyId(1), Field<GpuBodyId>(scene, "_nextId"));
        }
        finally { Release(scene); }
    }

    [Fact]
    public void ClickingTheBenchWithAPalettePartSendsItsConstructionAfterRecordingOneUndo()
    {
        var (scene, client) = BenchScene(PartId.Ramp);
        try
        {
            var before = scene.World.Construction;
            Assert.Empty(ClickBench(scene));
            // Control: the same click reaches capture and the worker; the admission stays pending in this fixture.
            Assert.True(client.Commands.Count == 1, $"One Construct must reach the worker; status: {Field<Label>(scene, "_status").Text}");
            var (kind, construction) = client.Commands[0];
            Assert.Equal(WorkshopCommandKind.Construct, kind);
            Assert.Equal(PartId.Ramp, Assert.Single(Assert.NotNull(construction).Instances).PartId);
            Assert.Equal(before, Assert.Single(Field<List<WorkshopConstruction>>(scene, "_undo")));
        }
        finally { Release(scene); }
    }

    [Fact]
    public void AnAdmittedPlacementWhosePresentationFailsIsLoggedOnceNeverEscapesAndLeavesTheWorkshopUsable()
    {
        var (scene, client) = BenchScene(PartId.Ramp, PlacementAnswer.Applied);
        var detail = Field<Label>(scene, "_detail");
        try
        {
            // Fault injection after admission: only Select, which runs once the worker has applied the part, writes this label.
            SetField<Label?>(scene, "_detail", null);
            Assert.Empty(ClickBench(scene)); // nothing may escape the handler: in the browser that terminates the runtime
            var logged = godot.Errors.Drain();
            Assert.Contains("Bench placement of 'ramp' as body 1 was admitted, but presenting it failed", Assert.Single(logged).ToString());
            Assert.Equal("The part was placed, but it could not be shown. Choose a part to continue.", Field<Label>(scene, "_status").Text);
            Assert.Equal(new GpuBodyId(2), Field<GpuBodyId>(scene, "_nextId"));
            Assert.Single(Field<List<WorkshopConstruction>>(scene, "_undo")); // the admitted placement keeps its undo entry
            Assert.Null(Field<PartId?>(scene, "_tool")); // placement mode ended: no Escape is needed
            Assert.False(Field<bool>(scene, "_dragging"));
            // The next placement works: choose the part from the palette again and place it.
            SetField(scene, "_detail", detail);
            typeof(Workshop).GetMethod("ChooseTool", Private)!.Invoke(scene, [PartId.Ramp]);
            Assert.Empty(ClickBench(scene));
            Assert.Empty(godot.Errors.Drain());
            Assert.Equal("Drag to slide. Choose arrows to move or rings to rotate.", Field<Label>(scene, "_status").Text);
            Assert.Equal(2, client.Commands.Count);
            // It takes a fresh identity instead of silently replacing the admitted part: the construction holds both.
            Assert.Equal([new GpuBodyId(1), new GpuBodyId(2)], scene.World.Construction.Instances.Select(instance => instance.Id));
            Assert.Equal(new GpuBodyId(3), Field<GpuBodyId>(scene, "_nextId"));
        }
        finally { SetField(scene, "_detail", detail); Release(scene); }
    }

    [Fact]
    public void AFailingHandlerIsCaughtAtItsBoundaryLoggedOnceByNameAndShown()
    {
        // Undo is a real async void handler; its first step, Select(null), writes the detail label, so a missing label is a
        // genuine failure inside UndoAsync that only the handler boundary (HandlerFailed) can catch.
        var (scene, client) = BenchScene(PartId.Ramp);
        var detail = Field<Label>(scene, "_detail");
        try
        {
            Field<List<WorkshopConstruction>>(scene, "_undo").Add(scene.World.Construction);
            SetField<Label?>(scene, "_detail", null);
            Assert.Empty(InvokeHandler(scene, "Undo")); // nothing may escape: in the browser that terminates the runtime
            Assert.Contains("Workshop handler 'Undo' failed", Assert.Single(godot.Errors.Drain()).ToString());
            Assert.StartsWith("That action failed and was logged: ", Field<Label>(scene, "_status").Text);
            Assert.Empty(client.Commands);
        }
        finally { SetField(scene, "_detail", detail); Release(scene); }
    }

    [Fact]
    public void ARefusedPlacementLeavesNoUndoAndKeepsItsIdentity()
    {
        var (scene, client) = BenchScene(PartId.Ramp, PlacementAnswer.Rejected);
        try
        {
            Assert.Empty(ClickBench(scene));
            Assert.Single(client.Commands);
            Assert.StartsWith("Placement rejected: ", Field<Label>(scene, "_status").Text);
            Assert.Empty(Field<List<WorkshopConstruction>>(scene, "_undo"));
            Assert.Equal(new GpuBodyId(1), Field<GpuBodyId>(scene, "_nextId"));
        }
        finally { Release(scene); }
    }

    private enum PlacementAnswer { Pending, Applied, Rejected }

    /// <summary>Records each command and answers it as configured: never (pending), Applied with an empty body read (a static
    /// part has no dynamic bodies), or Rejected.</summary>
    private sealed class PlacementClient(PlacementAnswer answer) : IWorkshopClient
    {
        public List<(WorkshopCommandKind Kind, WorkshopConstruction? Construction)> Commands { get; } = [];
        public WorkshopCadenceSettings Settings => WorkshopCadenceSettings.Default();
        public SimulationEpoch Epoch => new(1);
        public AuthorityRevision Revision => default;
        public WorkshopCommandIdentity? Pending => null;
        public WorkshopTransportState TransportState => WorkshopTransportState.Ready;
        public void ControlUi(WorkshopUiTarget target, AnimationControlKind kind, bool visible) { }
        public bool TryUiFrame(ulong frame, WorkshopPresentationSample physical, WorkshopUiTarget target, out AnimationOpacity opacity) { opacity = default; return false; }
        public bool TryCosmeticFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out WorkshopCosmeticSample sample) { sample = default; return false; }
        public bool TryElectricalFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out ElectricalIndicatorSample sample) { sample = default; return false; }
        public bool TryRead(out WorkshopResponse response) { response = default; return false; }
        public bool TryPresent(ulong frame, out WorkshopPresentationSample sample) { sample = default; return false; }
        public void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene) { }
        public Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
            WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null)
        {
            Commands.Add((kind, construction));
            if (answer == PlacementAnswer.Pending) return new TaskCompletionSource<WorkshopDelivery>().Task;
            var result = answer == PlacementAnswer.Applied
                ? new WorkshopCommandResult(WorkshopCommandOutcome.Applied, WorkshopRejection.None)
                : new WorkshopCommandResult(WorkshopCommandOutcome.Rejected, WorkshopRejection.InvalidConstruction);
            var response = new WorkshopResponse(new((ulong)Commands.Count), WorkshopResponseKind.Acknowledgement, result,
                WorkshopSimulationPhase.Building, new WorkshopRead(Epoch, new(0), default, Revision,
                    new(WorkshopClockDomain.SimulationMonotonic, new(1), new(120_000_000), new(100_000))),
                Session: new(1, 1), Cadence: new(1), MasterGeneration: new(1), Projection: new(1));
            return Task.FromResult(new WorkshopDelivery(response, true));
        }
        public Task<WorkshopDelivery> CancelPending() => throw new InvalidOperationException("No command is pending in this fixture.");
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
