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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenericTimerEditorUsesSecondsAndCommitsOnlyValidStoppedValues(bool rearming)
    {
        var parameter = new ParameterId(rearming ? "hold_seconds" : "delay_seconds");
        var (scene, client) = BenchScene(rearming ? new PartId("hold_timer") : PartId.Delay, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            Press("Configure");
            var input = Assert.Single(Nodes<LineEdit>(choices));
            Assert.Equal(parameter.Value, input.Name.ToString());
            Assert.Contains(Nodes<Label>(choices), label => label.Text == parameter.Value + " (0.1–12 s)");
            var before = scene.World.Construction;
            var commands = client.Commands.Count;
            var undos = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            foreach (var invalid in new[] { "0", "13", "NaN", "Infinity" })
            {
                input.Text = invalid;
                Press("Apply parameters");
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(commands, client.Commands.Count);
                Assert.Equal(undos, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            }
            Press("Cancel");
            Assert.Equal(before, scene.World.Construction);
            Press("Configure");
            input = Assert.Single(Nodes<LineEdit>(choices));
            input.Text = "0.1234567";
            Press("Apply parameters");
            var current = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.True(F32Bits.Equal(.1234567f, current.Parameters[parameter]));
            Assert.Equal(undos + 1, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            var accepted = scene.World.Construction;
            commands = client.Commands.Count;
            var proposed = new InstanceParameterCollection([new(parameter, 12f)]);
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, phase);
                Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", current.Id, proposed));
                Assert.Equal(accepted, scene.World.Construction);
                Assert.Equal(commands, client.Commands.Count);
            }
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(current with { Locked = true }) };
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(scene.World, locked);
            Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", current.Id, proposed));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.Commands.Count);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }
    [Fact]
    public async Task ImpactContactConnectionChoicesPreserveDomainCancelAndOneUndo()
    {
        var (scene,client)=BenchScene(PartId.ImpactSwitch,PlacementAnswer.Applied);
        try
        {
            var construction=new WorkshopConstruction(new(2),WorkshopCadenceSettings.Default(),new(
                WorkshopInput.Generic(PartId.ImpactSwitch,new(1),0,1,0,0,0,0,1),
                WorkshopInput.Generic(PartId.PoweredGate,new(2),5,3,0,0,0,0,1),
                WorkshopInput.Lamp(new(3),-3,2,0,0,0,0,1)));
            await scene.World.ReplaceConstruction(construction);
            var source=scene.World.Parts.Single(part=>part.AuthoredId.Value==1);
            var gate=scene.World.Parts.Single(part=>part.AuthoredId.Value==2);
            var lamp=scene.World.Parts.Single(part=>part.AuthoredId.Value==3);
            var choices=Field<VBoxContainer>(scene,"_connectionChoices");
            IEnumerable<Button> Buttons()=>choices.GetChildren().OfType<Button>();
            void Press(string label)=>Assert.Single(Buttons(),button=>button.Text==label).EmitSignal(Button.SignalName.Pressed);
            void SelectSource()=>Assert.Empty(InvokeHandler(scene,"Select",source));
            SelectSource();Assert.Contains(Buttons(),button=>button.Text=="Connect ActivationOut");Assert.Contains(Buttons(),button=>button.Text=="Connect Supply");
            var undos=Field<List<WorkshopConstruction>>(scene,"_undo").Count;var commands=client.Commands.Count;
            Press("Connect Supply");Assert.Empty(InvokeHandler(scene,"ChooseConnectionTarget",gate));
            Assert.Single(Buttons());Assert.Empty(InvokeHandler(scene,"CancelTool"));
            Assert.Equal(commands,client.Commands.Count);Assert.Equal(undos,Field<List<WorkshopConstruction>>(scene,"_undo").Count);Assert.Empty(scene.World.Construction.Connections);
            SelectSource();Press("Connect ActivationOut");Assert.Empty(InvokeHandler(scene,"ChooseConnectionTarget",gate));Assert.Empty(Buttons());
            Assert.Empty(InvokeHandler(scene,"ChooseConnectionTarget",lamp));Press("ActivationOut to ActivationIn");
            Assert.Equal(WorkshopConnectionDomain.Activation,Assert.Single(scene.World.Construction.Connections).Domain);
            Assert.Equal(undos+1,Field<List<WorkshopConstruction>>(scene,"_undo").Count);Assert.Empty(InvokeHandler(scene,"Undo"));Assert.Empty(scene.World.Construction.Connections);
            SelectSource();Press("Connect Supply");Assert.Empty(InvokeHandler(scene,"ChooseConnectionTarget",gate));Press("Supply to PowerIn");
            var link=Assert.Single(scene.World.Construction.Connections);Assert.Equal(WorkshopConnectionDomain.Electrical,link.Domain);Assert.Equal(gate.AuthoredId,link.Target);
            Assert.Equal(undos+1,Field<List<WorkshopConstruction>>(scene,"_undo").Count);Assert.Empty(InvokeHandler(scene,"Undo"));Assert.Empty(scene.World.Construction.Connections);
            Assert.Empty(godot.Errors.Drain());
        }
        finally {Release(scene);}
    }

    [Fact]
    public async Task BothGateChooserRequiresAnExplicitSocketAndCancelDoesNotEdit()
    {
        var (scene,client)=BenchScene(BothGateTests.Kind,PlacementAnswer.Applied);
        try
        {
            var construction=BothGateTests.Machine() with {Connections=WorkshopConnections.Empty};
            await scene.World.ReplaceConstruction(construction);
            var source=scene.World.Parts.Single(part=>part.AuthoredId.Value==1);
            var gate=scene.World.Parts.Single(part=>part.AuthoredId.Value==4);
            var choices=Field<VBoxContainer>(scene,"_connectionChoices");
            IEnumerable<Button> Buttons()=>choices.GetChildren().OfType<Button>();
            void Choose()
            {
                Assert.Empty(InvokeHandler(scene,"Select",source));
                Assert.Single(Buttons(),button=>button.Text=="Connect Supply").EmitSignal(Button.SignalName.Pressed);
                Assert.Empty(InvokeHandler(scene,"ChooseConnectionTarget",gate));
            }
            var undos=Field<List<WorkshopConstruction>>(scene,"_undo").Count;
            var commands=client.Commands.Count;
            Choose();
            Assert.Equal(3,Buttons().Count());
            Assert.All(Buttons(),button=>Assert.NotNull(button.Icon));
            Assert.Empty(scene.World.Construction.Connections);
            Assert.Empty(InvokeHandler(scene,"CancelTool"));
            Assert.Equal(commands,client.Commands.Count);
            Assert.Equal(undos,Field<List<WorkshopConstruction>>(scene,"_undo").Count);
            foreach(var socket in new[]{WorkshopSocket.FirstIn,WorkshopSocket.SecondIn,WorkshopSocket.PowerIn})
            {
                Choose();
                Assert.Single(Buttons(),button=>(WorkshopSocket)button.GetMeta("socket").AsInt32()==socket).EmitSignal(Button.SignalName.Pressed);
                var link=Assert.Single(scene.World.Construction.Connections);
                Assert.Equal(socket,link.Input);
                Assert.Equal(WorkshopConnectionDomain.Electrical,link.Domain);
                Assert.Equal(undos+1,Field<List<WorkshopConstruction>>(scene,"_undo").Count);
                var saved=new WorkshopSavedConstruction(scene.World.Construction,new(6));
                Assert.Equal(saved,WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved)));
                Assert.Empty(InvokeHandler(scene,"Undo"));
                Assert.Empty(scene.World.Construction.Connections);
            }
            Assert.Empty(godot.Errors.Drain());
        }
        finally {Release(scene);}
    }

    /// <summary>CAT-024 through the real chooser: every Not both gate socket needs an explicit choice, and a wire that would close a
    /// loop through the gate, even through an open switch, is refused on the bottom status line with no command and no Undo.</summary>
    [Theory]
    [InlineData(ElectricalNandTests.FeedbackLoop.NandToNand)]
    [InlineData(ElectricalNandTests.FeedbackLoop.ThroughOpenSwitch)]
    public async Task NotBothGateChooserRefusesALoopClosingWireOnTheStatusLine(ElectricalNandTests.FeedbackLoop loop)
    {
        var (scene,client)=BenchScene(ElectricalNandTests.Kind,PlacementAnswer.Applied);
        try
        {
            var (open,closing,ids)=ElectricalNandTests.LoopCase(loop);
            await scene.World.ReplaceConstruction(open);
            var choices=Field<VBoxContainer>(scene,"_connectionChoices");
            IEnumerable<Button> Buttons()=>choices.GetChildren().OfType<Button>();
            MachinePart Part(GpuBodyId id)=>scene.World.Parts.Single(part=>part.AuthoredId==id);
            Assert.Empty(InvokeHandler(scene,"Select",Part(closing.Source)));
            Assert.Single(Buttons(),button=>button.Text=="Connect Supply").EmitSignal(Button.SignalName.Pressed);
            Assert.Empty(InvokeHandler(scene,"ChooseConnectionTarget",Part(closing.Target)));
            Assert.Equal(3,Buttons().Count()); // first, second and bottom supply: no default socket
            Assert.All(Buttons(),button=>Assert.NotNull(button.Icon));
            var undos=Field<List<WorkshopConstruction>>(scene,"_undo").Count;
            var commands=client.Commands.Count;
            Assert.Single(Buttons(),button=>(WorkshopSocket)button.GetMeta("socket").AsInt32()==closing.Input).EmitSignal(Button.SignalName.Pressed);
            Assert.Equal(open,scene.World.Construction);
            Assert.Equal(commands,client.Commands.Count);
            Assert.Equal(undos,Field<List<WorkshopConstruction>>(scene,"_undo").Count);
            var status=Field<Label>(scene,"_status").Text;
            Assert.Contains("parts "+ids+".",status,StringComparison.Ordinal);
            Assert.StartsWith("Break the wire loop",status,StringComparison.Ordinal);
            Assert.Empty(godot.Errors.Drain());
        }
        finally {Release(scene);}
    }

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
                var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
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
                null, () => undo++);
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
                BenchPoint, Quaternion.Identity, null, () => undo++));
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
                null, () => undo++);
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
            var lesson = PuzzleCatalogue.Create(new PuzzleId("ready_to_rebound"), new(1), WorkshopCadenceSettings.Default(), new(1), new((Half).5));
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(world, lesson);
            var undo = 0;
            // The Receiver captures, but the lesson's Receiver composition refuses an identity outside the authored domain.
            var refused = WorkshopBenchPlacement.Propose(world, PartId.Receiver, new(1UL << 32), BenchPoint, Quaternion.Identity,
                null, () => undo++);
            Assert.Equal(BenchPlacementRejection.UnsupportedPart, refused.Rejection);
            Assert.StartsWith("Placement rejected: ", refused.Message);
            Assert.Equal("Placement rejected: Authored body identity exceeds its domain.", refused.Message);
            Assert.Empty(godot.Errors.Drain());
            Assert.Equal(0, undo);
            Assert.Equal(lesson, refused.Construction);
            // Control: the same lesson composes its Receiver at an authored identity and records one undo.
            var composed = WorkshopBenchPlacement.Propose(world, PartId.Receiver, new(4), BenchPoint, Quaternion.Identity,
                null, () => undo++);
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
    public void PuzzleSelectionReservesIdentityBeforeSubmittingOrChangingUndo()
    {
        var (scene, client) = BenchScene(PartId.Ramp, PlacementAnswer.Applied);
        try
        {
            var id = new PuzzleId("first_principles");
            var count = (ulong)PuzzleCatalogue.Definition(id).Placements.Length;
            var exhausted = new GpuBodyId(ulong.MaxValue - count + 1);
            SetField(scene, "_nextId", exhausted);
            var before = scene.World.Construction;
            var undo = Field<List<WorkshopConstruction>>(scene, "_undo");
            undo.Add(before);
            Assert.Empty(InvokeHandler(scene, "SelectModeFromPicker", Array.IndexOf(Field<PuzzleId[]>(scene, "_puzzleChoices"), id)));
            Assert.Equal(before, scene.World.Construction);
            Assert.Equal(exhausted, Field<GpuBodyId>(scene, "_nextId"));
            Assert.Single(undo);
            Assert.Empty(client.Commands);
            Assert.Equal("Puzzle identities are exhausted.", Field<Label>(scene, "_status").Text);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
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


    [Fact]
    public void GenericWallEditorUsesDeclaredFieldsAndRejectsInvalidOrDisallowedEditsAtomically()
    {
        var (scene, client) = BenchScene(PartId.Wall, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var part = Assert.Single(scene.World.Parts);
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            Press("Configure");
            var fields = Nodes<LineEdit>(choices).ToDictionary(field => new ParameterId(field.Name.ToString()));
            Assert.Equal(new[] { new ParameterId("height"), new("thickness"), new("width") }, fields.Keys.Order());
            Assert.Contains(Nodes<Label>(choices), label => label.Text == "width (0.4–8 m)");
            Assert.Contains(Nodes<Label>(choices), label => label.Text == "height (0.4–6 m)");
            Assert.Contains(Nodes<Label>(choices), label => label.Text == "thickness (0.12–2 m)");
            var before = scene.World.Construction;
            var commands = client.Commands.Count;
            var undos = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            fields[new("width")].Text = "0";
            Press("Apply parameters");
            Assert.Equal(before, scene.World.Construction);
            Assert.Equal(commands, client.Commands.Count);
            Assert.Equal(undos, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            Assert.Equal(new Vector3(3, 2, .25f), part.Dimensions);
            fields[new("width")].Text = "NaN";
            Press("Apply parameters");
            Assert.Equal(before, scene.World.Construction);
            Press("Cancel");
            Assert.Equal(before, scene.World.Construction);
            Press("Configure");
            fields = Nodes<LineEdit>(choices).ToDictionary(field => new ParameterId(field.Name.ToString()));
            fields[new("width")].Text = "3.1234567";
            Press("Apply parameters");
            var current = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(3.1234567f, current.Parameters[new("width")]);
            Assert.Equal(3.1234567f, part.Dimensions.X);
            Assert.Equal(undos + 1, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            var accepted = scene.World.Construction;
            commands = client.Commands.Count;
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, phase);
                Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", current.Id, WallFixture.Maximum));
                Assert.Equal(accepted, scene.World.Construction);
                Assert.Equal(commands, client.Commands.Count);
            }
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(current with { Locked = true }) };
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(scene.World, locked);
            Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", current.Id, WallFixture.Maximum));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.Commands.Count);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    [Fact]
    public void GenericImpactEditorClampsOnlyDeclaredFiniteInputAndPreservesEditGuards()
    {
        var (scene, client) = BenchScene(PartId.ImpactSwitch, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            var key = new ParameterId("trigger_threshold");
            var initial = scene.World.Construction;
            var undo = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            Press("Configure");
            Assert.Contains(Nodes<Label>(choices), label => label.Text == "trigger_threshold (0–64 m/s)");
            var field = Assert.Single(Nodes<LineEdit>(choices));
            Assert.Equal(key, new ParameterId(field.Name.ToString()));
            field.Text = "1.234567";
            Press("Cancel");
            Assert.Equal(initial, scene.World.Construction);
            foreach (var (text, expected) in new[] { ("-1", 0f), ("65", 64f), ("1.234567", 1.234567f) })
            {
                Press("Configure");
                Assert.Single(Nodes<LineEdit>(choices)).Text = text;
                Press("Apply parameters");
                Assert.Equal(expected, Current().Parameters[key]);
                Assert.Equal(++undo, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            }
            var accepted = scene.World.Construction;
            var commands = client.Commands.Count;
            Press("Configure");
            Assert.Single(Nodes<LineEdit>(choices)).Text = "NaN";
            Press("Apply parameters");
            Assert.Equal(accepted, scene.World.Construction);
            Assert.Equal(commands, client.Commands.Count);
            Assert.Equal(undo, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            Press("Cancel");
            var values = new InstanceParameterCollection([new(key, 2f)]);
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, phase);
                Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", Current().Id, values));
                Assert.Equal(accepted, scene.World.Construction);
            }
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(Current() with { Locked = true }) };
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(scene.World, locked);
            Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", Current().Id, values));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.Commands.Count);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    [Fact]
    public void GenericWallResizeUsesLocalAxesClampsCancelsAndRecordsOneUndoPerGesture()
    {
        var (scene, _) = BenchScene(PartId.Wall, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var part = Assert.Single(scene.World.Parts);
            var original = scene.World.Construction;
            var originalDimensions = part.Dimensions;
            var camera = Field<Camera3D>(scene, "_camera");
            var gizmo = Field<RotationGizmo>(scene, "_rotationGizmo");
            gizmo.SetResizeMode();
            gizmo.Follow(part, true, camera);
            var undos = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            for (var axis = 0; axis < 3; axis++)
            {
                var handle = camera.UnprojectPosition(gizmo.HandlePosition(axis));
                Assert.Empty(InvokeHandler(scene, "Click", handle));
                Assert.True(gizmo.Dragging);
                Assert.Equal(undos + 1, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
                var direction = camera.UnprojectPosition(part.GlobalPosition + part.GlobalBasis[axis]) -
                    camera.UnprojectPosition(part.GlobalPosition);
                Assert.True(gizmo.Drag(camera, handle + direction * 100, true));
                var maximum = new Vector3(8, 6, 2);
                Assert.Equal(maximum[axis], part.Dimensions[axis]);
                Assert.Empty(InvokeHandler(scene, "EndGizmo", true));
                Assert.Equal(originalDimensions, part.Dimensions);
                Assert.Equal(original, scene.World.Construction);
                Assert.Equal(undos, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            }
            var at = camera.UnprojectPosition(gizmo.HandlePosition(0));
            Assert.Empty(InvokeHandler(scene, "Click", at));
            var projected = camera.UnprojectPosition(part.GlobalPosition + part.GlobalBasis.X) - camera.UnprojectPosition(part.GlobalPosition);
            Assert.True(gizmo.Drag(camera, at + projected * .5f, true));
            Assert.Empty(InvokeHandler(scene, "EndGizmo", false));
            Assert.Equal(4f, Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances)).Parameters[new("width")]);
            Assert.Equal(undos + 1, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            Assert.Empty(InvokeHandler(scene, "Undo"));
            Assert.Equal(original.Instances, scene.World.Construction.Instances);
            Assert.Equal(originalDimensions, Assert.Single(scene.World.Parts).Dimensions);
        }
        finally { Release(scene); }
    }


    [Fact]
    public void GenericRampResizeClampsBothHorizontalAxesWithoutEndingGestureAndSupportsCancelUndo()
    {
        var (scene, _) = BenchScene(PartId.Ramp, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var part = Assert.Single(scene.World.Parts);
            var original = scene.World.Construction;
            var dimensions = part.Dimensions;
            var camera = Field<Camera3D>(scene, "_camera");
            var gizmo = Field<RotationGizmo>(scene, "_rotationGizmo");
            gizmo.SetResizeMode(); gizmo.Follow(part, true, camera);
            var undos = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            foreach (var axis in new[] { 0, 2 })
            {
                var handle = camera.UnprojectPosition(gizmo.HandlePosition(axis));
                Assert.Empty(InvokeHandler(scene, "Click", handle));
                Assert.True(gizmo.Dragging);
                var direction = camera.UnprojectPosition(part.GlobalPosition + part.GlobalBasis[axis]) -
                    camera.UnprojectPosition(part.GlobalPosition);
                Assert.True(gizmo.Drag(camera, handle + direction * 100, true));
                Assert.Equal(4f, part.Dimensions[axis]);
                Assert.True(gizmo.Dragging);
                Assert.True(gizmo.Drag(camera, handle - direction * 100, true));
                Assert.Equal(.125f, part.Dimensions[axis]);
                Assert.True(gizmo.Dragging);
                Assert.Equal(undos + 1, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
                Assert.Empty(InvokeHandler(scene, "EndGizmo", true));
                Assert.Equal(original, scene.World.Construction);
                Assert.Equal(dimensions, part.Dimensions);
                Assert.Equal(undos, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            }
            var at = camera.UnprojectPosition(gizmo.HandlePosition(0));
            Assert.Empty(InvokeHandler(scene, "Click", at));
            var projected = camera.UnprojectPosition(part.GlobalPosition + part.GlobalBasis.X) - camera.UnprojectPosition(part.GlobalPosition);
            Assert.True(gizmo.Drag(camera, at + projected * .5f, true));
            Assert.Empty(InvokeHandler(scene, "EndGizmo", false));
            Assert.Equal(4f, Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances)).Parameters[new("length")]);
            Assert.Equal(undos + 1, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            Assert.Empty(InvokeHandler(scene, "Undo"));
            Assert.Equal(original.Instances, scene.World.Construction.Instances);
            Assert.Equal(dimensions, Assert.Single(scene.World.Parts).Dimensions);
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
        public void SetDeclaredVisible(GpuBodyId owner, bool visible) { }
        public bool TryDeclaredFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out System.Collections.Generic.IReadOnlyList<DeclaredPresentationSample> samples) { samples = System.Array.Empty<DeclaredPresentationSample>(); return false; }
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
            var bodies=construction is { } authored ? PhysicsGpuAbi.ReadDynamicBodies(PhysicsGpuAbi.Admission(
                WorkshopPhysicsCompiler.Compile(authored,new(1,1)),Epoch,
                new(authored.Settings.Simulation,authored.Settings.Physical,new(1)))) : default;
            var response = new WorkshopResponse(new((ulong)Commands.Count), WorkshopResponseKind.Acknowledgement, result,
                WorkshopSimulationPhase.Building, new WorkshopRead(Epoch, new(0), bodies, Revision,
                    new(WorkshopClockDomain.SimulationMonotonic, new(1), new(120_000_000), new(100_000))),
                Session: new(1, 1), Cadence: new(1), MasterGeneration: new(1), Projection: new(1));
            return Task.FromResult(new WorkshopDelivery(response, true));
        }
        public Task<WorkshopDelivery> CancelPending() => throw new InvalidOperationException("No command is pending in this fixture.");
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
