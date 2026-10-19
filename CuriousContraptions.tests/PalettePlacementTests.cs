using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;
using Godot;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 9R.0c: the Free palette offers only parts with a constructible instance path, undelivered parts never reach it, and a
/// bench placement captures the part before it records undo, so an uncapturable part is one typed rejection and never a throw.
/// </summary>
[Collection<NativeSceneCollection>]
public sealed class PalettePlacementTests(NativeSceneFixture godot)
{
    private static readonly Vector3 BenchPoint = new(0, 1, 0);

    private static MachineWorld DiscoveredWorld()
    {
        var world = new MachineWorld();
        world.Registry.Discover();
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
            var palette = WorkshopInventoryPolicy.Free.Keys.ToArray();
            Assert.Equal(world.Registry.Definitions.Count, palette.Length);
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
                var wire = WorkshopWire.Encode(command);
                var received = WorkshopWire.DecodeCommand(wire).Construction;
                Assert.Equal(id, Assert.Single(Assert.NotNull(received).Instances).PartId);
                Assert.Equal(wire, WorkshopWire.Encode(command with { Construction = received }));
            }
        }
        finally { world.Free(); }
    }

    [Fact]
    public void UndeliveredPartsShipResourcesButHaveNoInstancePathAndNeverReachTheRegistryOrPalette()
    {
        Assert.Equal([UndeliveredPart.Tennis, UndeliveredPart.Balloon], UndeliveredParts.All);
        var world = DiscoveredWorld();
        try
        {
            foreach (var part in UndeliveredParts.All)
            {
                var id = UndeliveredParts.Id(part);
                Assert.True(UndeliveredParts.Contains(id));
                // Control: the resource still ships (Story 9A.14 deletes it), so the exclusion is admission, not absence.
                Assert.True(ResourceLoader.Exists($"res://parts/catalog/{id.Value}.tres"), $"{id} resource ships until Story 9A.14.");
                Assert.Throws<ArgumentException>(() => world.CaptureInstance(id, new(1), BenchPoint, Quaternion.Identity));
                Assert.False(world.Registry.Definitions.ContainsKey(id), $"{id} must not be registered.");
                Assert.False(WorkshopInventoryPolicy.Free.ContainsKey(id), $"{id} must not be offered by the Free palette.");
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
                null, null, () => undo++);
            Assert.Equal(BenchPlacementRejection.UnsupportedPart, placement.Rejection);
            Assert.Contains(external, placement.Message);
            Assert.Equal(0, undo);
            Assert.Equal(before, placement.Construction);
            Assert.Equal(before, world.Construction);
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
                null, null, () => undo++);
            Assert.Equal(BenchPlacementRejection.None, placement.Rejection);
            Assert.Equal(1, undo);
            Assert.Empty(placement.Message);
            Assert.Equal(PartId.Ramp, Assert.Single(placement.Construction.Instances, i => i.Id == new GpuBodyId(7)).PartId);
            Assert.Equal(before, world.Construction); // proposing never commits; the worker admits the construction
        }
        finally { world.Free(); }
    }

    // The actual Workshop scene and its private bench Click (native-only reflection boundary, as in WorkshopHintTests): the defect's
    // precondition is restored by putting the part in the inventory and choosing it, then the bench is clicked at a point on the
    // placement plane. No production setter or test hook exists.
    private const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    private static T Field<T>(Workshop scene, string name) => (T)typeof(Workshop).GetField(name, Private)!.GetValue(scene)!;
    private static void SetField<T>(Workshop scene, string name, T value) => typeof(Workshop).GetField(name, Private)!.SetValue(scene, value);

    private (Workshop Scene, PlacementClient Client) BenchScene(PartId tool)
    {
        var scene = GD.Load<PackedScene>("res://scenes/workshop.tscn").Instantiate<Workshop>();
        godot.Tree.Root.AddChild(scene);
        scene.SetProcess(false);
        scene.SetPhysicsProcess(false);
        var client = new PlacementClient();
        typeof(MachineWorld).GetField("_workshopClient", Private)!.SetValue(scene.World, client);
        typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Building);
        SetField(scene, "_gpuPending", false); // a ready Workshop: natively the browser client never initialises
        SetField(scene, "_buildView", false);
        SetField<IReadOnlyDictionary<PartId, PartAllowance>>(scene, "_inventory", new PartInventory((tool, PartAllowance.Unlimited)));
        SetField<PartId?>(scene, "_tool", tool);
        return (scene, client);
    }

    private static void ClickBench(Workshop scene)
    {
        var height = (float)Field<Metres>(scene, "_placementHeight").Value;
        var screen = Field<Camera3D>(scene, "_camera").UnprojectPosition(new Vector3(0, height, 0));
        typeof(Workshop).GetMethod("Click", Private)!.Invoke(scene, [screen]);
    }

    private void Release(Workshop scene)
    {
        scene.Free();
        godot.Engine.Iteration();
    }

    [Fact]
    public void ClickingTheBenchWithAnUncapturablePartIsOneTypedRejectionWithAVisibleMessageAndNoUndo()
    {
        var (scene, client) = BenchScene(PartId.Balloon);
        try
        {
            var before = scene.World.Construction;
            ClickBench(scene);
            Assert.Equal("Placement rejected: Unsupported instance part id 'balloon'.", Field<Label>(scene, "_status").Text);
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
            ClickBench(scene);
            // Control: the same click reaches capture and the worker; the admission stays pending in this fixture.
            Assert.True(client.Commands.Count == 1, $"One Construct must reach the worker; status: {Field<Label>(scene, "_status").Text}");
            var (kind, construction) = client.Commands[0];
            Assert.Equal(WorkshopCommandKind.Construct, kind);
            Assert.Equal(PartId.Ramp, Assert.Single(Assert.NotNull(construction).Instances).PartId);
            Assert.Equal(before, Assert.Single(Field<List<WorkshopConstruction>>(scene, "_undo")));
        }
        finally { Release(scene); }
    }

    /// <summary>Records each command and never answers it: placement admission is outside this fixture.</summary>
    private sealed class PlacementClient : IWorkshopClient
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
            return new TaskCompletionSource<WorkshopDelivery>().Task;
        }
        public Task<WorkshopDelivery> CancelPending() => throw new InvalidOperationException("No command is pending in this fixture.");
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
