using CuriousContraptions.Abstractions;
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
        var (scene, client) = BenchScene(rearming ? new PartId("hold_timer") : new PartId("delay"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Choices(scene);
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            Press("Configure");
            var input = Assert.Single(Nodes<LineEdit>(choices));
            Assert.Equal(parameter.Value, input.Name.ToString());
            Assert.Contains(Nodes<Label>(choices), label => label.Text == parameter.Value + " (0.1–12 s)");
            var before = scene.World.Construction;
            var commands = client.EditCount;
            var undos = scene.World.Construction;
            foreach (var invalid in new[] { "0", "13", "NaN", "Infinity" })
            {
                input.Text = invalid;
                Press("Apply parameters");
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(commands, client.EditCount);
            }
            Press("Cancel");
            Assert.Equal(before, scene.World.Construction);
            Press("Configure");
            input = Assert.Single(Nodes<LineEdit>(choices));
            input.Text = "0.1234567";
            Press("Apply parameters");
            var current = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.True(F32Bits.Equal(.1234567f, current.Parameters[parameter]));
            AssertParameterUndo(scene, ref undos);
            var accepted = scene.World.Construction;
            commands = client.EditCount;
            var proposed = new InstanceParameterCollection([new(parameter, 12f)]);
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                Phase(scene, phase);
                Assert.Empty(Apply(scene, current.Id, proposed));
                Assert.Equal(accepted, scene.World.Construction);
                Assert.Equal(commands, client.EditCount);
            }
            Phase(scene, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(current with { Locked = true }) };
            locked = Install(scene, locked);
            commands = client.EditCount;
            Assert.Empty(Apply(scene, current.Id, proposed));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }
    // C-Clock (9A.42, D3/D6): a clock placed from the palette takes the declared 1.0 s and is configured through the generic editor,
    // which shows the instance's own value (the record panel showed a part-side copy that nothing set) and applies the Clamp policy:
    // 13 applies as 12 and 0.05 as 0.1, each one Undo and one command; NaN, Infinity and text are refused with no change, no command
    // and no undo; Cancel changes nothing; an unchanged Apply adds no Undo. Undo restores 1.0. Running, Paused and a locked clock are
    // refused. A moved clock keeps its interval (CaptureInstance reads the committed instance); a new one starts from 1.0 s.
    [Fact]
    public void TheClockIntervalEditorShowsTheInstanceValueClampsIntoRangeAndAppliesAsOneUndo()
    {
        var parameter = new ParameterId("interval_seconds");
        var (scene, client) = BenchScene(new PartId("clock"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var placed = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(BitConverter.SingleToUInt32Bits(1f), BitConverter.SingleToUInt32Bits(placed.Parameters[parameter]));
            var choices = Choices(scene);
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            LineEdit Configure()
            {
                Press("Configure");
                var field = Assert.Single(Nodes<LineEdit>(choices));
                Assert.Equal(parameter.Value, field.Name.ToString());
                Assert.Contains(Nodes<Label>(choices), label => label.Text == "interval_seconds (0.1–12 s)");
                return field;
            }
            var input = Configure();
            Assert.Equal("1", input.Text);
            var before = scene.World.Construction;
            var commands = client.EditCount;
            var undos = scene.World.Construction;
            foreach (var invalid in new[] { "NaN", "Infinity", "-Infinity", "two", "" })
            {
                input.Text = invalid;
                Press("Apply parameters");
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(commands, client.EditCount);
            }
            Press("Cancel");
            Assert.Equal(before, scene.World.Construction);
            foreach (var (text, value) in new[] { ("13", 12f), ("0.05", .1f), ("2.5", 2.5f) })
            {
                input = Configure();
                input.Text = text;
                Press("Apply parameters");
                var current = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
                Assert.Equal(BitConverter.SingleToUInt32Bits(value), BitConverter.SingleToUInt32Bits(current.Parameters[parameter]));
                Assert.Equal(++commands, client.EditCount);
                AssertParameterUndo(scene, ref undos);
                commands = client.EditCount;
            }
            // The editor shows the instance's value, and an unchanged Apply adds no Undo and sends no command.
            input = Configure();
            Assert.Equal("2.5", input.Text);
            Press("Apply parameters");
            Assert.Equal(commands, client.EditCount);

            // A move re-captures the committed instance and keeps its interval; a newly placed clock starts from the declared 1.0 s.
            var applied = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            var moved = Assert.IsType<WorkshopGenericInstance>(scene.World.CaptureInstance(new PartId("clock"), applied.Id, new Vector3(1, 3, 0), Quaternion.Identity));
            Assert.Equal(applied.Parameters, moved.Parameters);
            var fresh = Assert.IsType<WorkshopGenericInstance>(scene.World.CaptureInstance(new PartId("clock"), new GpuBodyId(99), new Vector3(1, 3, 0), Quaternion.Identity));
            Assert.Equal(BitConverter.SingleToUInt32Bits(1f), BitConverter.SingleToUInt32Bits(fresh.Parameters[parameter]));

            foreach (var _ in new[] { 1, 2, 3 }) Assert.Empty(Undo(scene));
            var restored = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(BitConverter.SingleToUInt32Bits(1f), BitConverter.SingleToUInt32Bits(restored.Parameters[parameter]));

            var accepted = scene.World.Construction;
            commands = client.EditCount;
            var proposed = new InstanceParameterCollection([new(parameter, 4f)]);
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                Phase(scene, phase);
                Assert.Empty(Apply(scene, restored.Id, proposed));
                Assert.Equal(accepted, scene.World.Construction);
                Assert.Equal(commands, client.EditCount);
            }
            Phase(scene, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(restored with { Locked = true }) };
            locked = Install(scene, locked);
            commands = client.EditCount;
            Assert.Empty(Apply(scene, restored.Id, proposed));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }
    // C-Plate (9A.40, D4): a plate placed from the palette takes the declared 0.5 kg. The generic editor names minimum_mass in kg with
    // its declared range and applies the Reject policy: 0.09, 16.01, NaN, Infinity and text are refused with no change, no command and
    // no undo (never clamped); 0.1 and 16 are accepted bit for bit; each application is one Undo, and Undo restores 0.5. Running,
    // Paused and a locked plate are refused.
    [Fact]
    public void ThePlateMinimumMassEditorRefusesOutOfRangeWithoutClampAndAppliesAsOneUndo()
    {
        var parameter = new ParameterId("minimum_mass");
        var (scene, client) = BenchScene(new PartId("pressure_plate"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var placed = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(BitConverter.SingleToUInt32Bits(.5f), BitConverter.SingleToUInt32Bits(placed.Parameters[parameter]));
            var choices = Choices(scene);
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            LineEdit Configure()
            {
                Press("Configure");
                var field = Assert.Single(Nodes<LineEdit>(choices));
                Assert.Equal(parameter.Value, field.Name.ToString());
                Assert.Contains(Nodes<Label>(choices), label => label.Text == "minimum_mass (0.1–16 kg)");
                return field;
            }
            var input = Configure();
            Assert.Equal("0.5", input.Text);
            var before = scene.World.Construction;
            var commands = client.EditCount;
            var undos = scene.World.Construction;
            foreach (var invalid in new[] { "0.09", "16.01", "NaN", "Infinity", "two" })
            {
                input.Text = invalid;
                Press("Apply parameters");
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(commands, client.EditCount);
            }
            Press("Cancel");
            Assert.Equal(before, scene.World.Construction);
            foreach (var (text, value) in new[] { ("16", 16f), ("0.1", .1f) })
            {
                input = Configure();
                input.Text = text;
                Press("Apply parameters");
                var current = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
                Assert.Equal(BitConverter.SingleToUInt32Bits(value), BitConverter.SingleToUInt32Bits(current.Parameters[parameter]));
                AssertParameterUndo(scene, ref undos);
            }
            Assert.Empty(Undo(scene));
            Assert.Empty(Undo(scene));
            var restored = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(BitConverter.SingleToUInt32Bits(.5f), BitConverter.SingleToUInt32Bits(restored.Parameters[parameter]));

            var accepted = scene.World.Construction;
            commands = client.EditCount;
            var proposed = new InstanceParameterCollection([new(parameter, 2f)]);
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                Phase(scene, phase);
                Assert.Empty(Apply(scene, restored.Id, proposed));
                Assert.Equal(accepted, scene.World.Construction);
                Assert.Equal(commands, client.EditCount);
            }
            Phase(scene, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(restored with { Locked = true }) };
            locked = Install(scene, locked);
            commands = client.EditCount;
            Assert.Empty(Apply(scene, restored.Id, proposed));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    // C-Memory (9A.41, D3): a counter placed from the palette takes the declared target 3 and is configured through the generic editor
    // (retro-verification #13: there was no editor). The editor names target_count with its declared whole range and applies the Reject
    // policy: 0, 10, 2.5, -1, NaN, Infinity and text are refused with no change, no command and no undo (never clamped or rounded); 1
    // and 9 are accepted, each one Undo and one command. Cancel changes nothing; an unchanged Apply adds no Undo; two Undos restore 3.
    // A move keeps the target and a newly placed counter starts from 3. Running, Paused and a locked counter are refused.
    [Fact]
    public void TheCounterTargetEditorAdmitsOneToNineRefusesTheRestAndAppliesAsOneUndo()
    {
        var parameter = new ParameterId("target_count");
        var (scene, client) = BenchScene(new PartId("counter"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var placed = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(3f, placed.Parameters[parameter]);
            var choices = Choices(scene);
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            LineEdit Configure()
            {
                Press("Configure");
                var field = Assert.Single(Nodes<LineEdit>(choices));
                Assert.Equal(parameter.Value, field.Name.ToString());
                Assert.Contains(Nodes<Label>(choices), label => label.Text == "target_count (1–9 )");
                return field;
            }
            var input = Configure();
            Assert.Equal("3", input.Text);
            var before = scene.World.Construction;
            var commands = client.EditCount;
            var undos = scene.World.Construction;
            foreach (var invalid in new[] { "0", "10", "2.5", "-1", "NaN", "Infinity", "three" })
            {
                input.Text = invalid;
                Press("Apply parameters");
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(commands, client.EditCount);
            }
            Press("Cancel");
            Assert.Equal(before, scene.World.Construction);
            foreach (var (text, value) in new[] { ("9", 9f), ("1", 1f) })
            {
                input = Configure();
                input.Text = text;
                Press("Apply parameters");
                var current = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
                Assert.Equal(value, current.Parameters[parameter]);
                Assert.Equal(++commands, client.EditCount);
                AssertParameterUndo(scene, ref undos);
                commands = client.EditCount;
            }
            // The editor shows the instance's value, and an unchanged Apply adds no Undo and sends no command.
            input = Configure();
            Assert.Equal("1", input.Text);
            Press("Apply parameters");
            Assert.Equal(commands, client.EditCount);

            // A move re-captures the committed instance and keeps its target; a newly placed counter starts from the declared 3.
            var applied = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            var moved = Assert.IsType<WorkshopGenericInstance>(scene.World.CaptureInstance(new PartId("counter"), applied.Id, new Vector3(1, 3, 0), Quaternion.Identity));
            Assert.Equal(applied.Parameters, moved.Parameters);
            var fresh = Assert.IsType<WorkshopGenericInstance>(scene.World.CaptureInstance(new PartId("counter"), new GpuBodyId(99), new Vector3(1, 3, 0), Quaternion.Identity));
            Assert.Equal(3f, fresh.Parameters[parameter]);

            Assert.Empty(Undo(scene));
            Assert.Empty(Undo(scene));
            var restored = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(3f, restored.Parameters[parameter]);

            var accepted = scene.World.Construction;
            commands = client.EditCount;
            var proposed = new InstanceParameterCollection([new(parameter, 5f)]);
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                Phase(scene, phase);
                Assert.Empty(Apply(scene, restored.Id, proposed));
                Assert.Equal(accepted, scene.World.Construction);
                Assert.Equal(commands, client.EditCount);
            }
            Phase(scene, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(restored with { Locked = true }) };
            locked = Install(scene, locked);
            commands = client.EditCount;
            Assert.Empty(Apply(scene, restored.Id, proposed));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    // F.6 F-1, coordinator decision B1: the preview and the click share one composed pose. A Basketball dropped into a plate whose face
    // lies at the top of the bench would rest above the 9 m volume: the preview hides, and the click is refused with the typed
    // UnplaceablePose message, no command, no undo and an unchanged construction. Control: the same drop into a plate at 8 m rests at
    // 8.42 m; the preview shows exactly the pose the click then places.
    [Theory]
    [InlineData(8.9f, false)]
    [InlineData(8f, true)]
    public void ThePreviewAndTheClickShareTheComposedPoseAndItsRefusal(float height, bool admitted)
    {
        var viewport = new SubViewport { Size = new(1440, 900), HandleInputLocally = true };
        godot.Tree.Root.AddChild(viewport);
        var store = new MemoryStore();
        var (scene, client) = BenchScene(new PartId("ball"), PlacementAnswer.Applied, store, viewport);
        try
        {
            var plate = PlacedParts.Plate(new(1), 0, height, 0, .5f);
            store.Bytes = WorkshopSaveCodec.Encode(new(new(new(2), WorkshopCadenceSettings.Default(), new(plate)), new(2)));
            Action(scene, "Load");
            Choose(scene, new PartId("ball"));
            var before = scene.World.Construction;
            var preview = Assert.Single(Descendants<MachinePart>(scene).Except(scene.World.Parts));
            var camera = Assert.Single(Descendants<Camera3D>(scene));
            camera.Position += Vector3.Up * (height - 3);
            camera.LookAt(new Vector3(0, height, 0));
            var start = camera.UnprojectPosition(new Vector3(0, 3, 0));
            using var initialPointer = new InputEventMouseMotion { Position = start, GlobalPosition = start };
            viewport.PushInput(initialPointer, true);
            scene._Process(0);
            var lift = Assert.Single(Descendants<Button>(scene), button => button.HasMeta("action_label") &&
                button.GetMeta("action_label").AsString() == "↕ Lift");
            using var press = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true };
            lift.EmitSignal(Control.SignalName.GuiInput, press);
            var liftStart = scene.GetViewport().GetMousePosition();
            var axis = camera.UnprojectPosition(preview.GlobalPosition + Vector3.Up) - camera.UnprojectPosition(preview.GlobalPosition);
            using var motion = new InputEventMouseMotion { Position = liftStart + axis * (height - preview.Position.Y) };
            scene._Input(motion);
            using var release = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false };
            scene._Input(release);
            Assert.InRange(preview.Position.Y, height - .001f, height + .001f);
            var screen = camera.UnprojectPosition(new Vector3(0, height, 0));
            using var pointer = new InputEventMouseMotion { Position = screen, GlobalPosition = screen };
            scene.GetViewport().PushInput(pointer, true);
            scene._Process(0);
            var shown = (preview.Visible, preview.Position);
            var commands = client.EditCount;
            Assert.Empty(Click(scene, screen));
            if (!admitted)
            {
                Assert.False(shown.Visible);
                Assert.Equal("Placement rejected: resting there would lift the part outside the bench.", Status(scene));
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(commands, client.EditCount);
                Assert.Empty(Undo(scene));
                Assert.Equal(commands, client.EditCount);
            }
            else
            {
                Assert.True(shown.Visible, $"screen={screen}; mouse={scene.GetViewport().GetMousePosition()}; hovered={scene.GetViewport().GuiGetHoveredControl()?.Name}; preview={shown.Position}; status={Status(scene)}");
                Assert.InRange(shown.Position.Y, height + .42f - .001f, height + .42f + .001f);
                var ball = Assert.Single(scene.World.Construction.Instances, instance => instance.PartId == new PartId("ball"));
                var placed = new Vector3((ball.Cell.X + ball.Local.X) / 16f, (ball.Cell.Y + ball.Local.Y) / 16f, (ball.Cell.Z + ball.Local.Z) / 16f);
                Assert.InRange(placed.DistanceTo(shown.Position), 0, .001f);
                Assert.Empty(Undo(scene));
                Assert.Equal(before.Instances, scene.World.Construction.Instances);
            }
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); viewport.Free(); }
    }

    [Fact]
    public async Task ImpactContactConnectionChoicesPreserveDomainCancelAndOneUndo()
    {
        var (scene,client)=BenchScene(new PartId("switch"),PlacementAnswer.Applied);
        try
        {
            var construction=new WorkshopConstruction(new(2),WorkshopCadenceSettings.Default(),new(
                WorkshopInput.Generic(new PartId("switch"),new(1),0,1,0,0,0,0,1),
                WorkshopInput.Generic(new PartId("powered_gate"),new(2),5,3,0,0,0,0,1),
                PlacedParts.Lamp(new(3),-3,2,0,0,0,0,1)));
            await scene.World.ReplaceConstruction(construction);
            var source=scene.World.Parts.Single(part=>part.AuthoredId.Value==1);
            var gate=scene.World.Parts.Single(part=>part.AuthoredId.Value==2);
            var lamp=scene.World.Parts.Single(part=>part.AuthoredId.Value==3);
            var choices=Choices(scene);
            IEnumerable<Button> Buttons()=>choices.GetChildren().OfType<Button>();
            void Press(string label)=>Assert.Single(Buttons(),button=>button.Text==label).EmitSignal(Button.SignalName.Pressed);
            void SelectSource()=>Assert.Empty(Select(scene,source));
            SelectSource();Assert.Contains(Buttons(),button=>button.Text=="Connect ActivationOut");Assert.Contains(Buttons(),button=>button.Text=="Connect Supply");var commands=client.EditCount;
            Press("Connect Supply");Assert.Empty(Target(scene,gate));
            Assert.Single(Buttons());Assert.Empty(Cancel(scene));
            Assert.Equal(commands,client.EditCount);Assert.Empty(scene.World.Construction.Connections);
            SelectSource();Press("Connect ActivationOut");Assert.Empty(Target(scene,gate));Assert.Empty(Buttons());
            Assert.Empty(Target(scene,lamp));Press("ActivationOut to ActivationIn");
            Assert.Equal(WorkshopConnectionDomain.Activation,Assert.Single(scene.World.Construction.Connections).Domain);
            Assert.Empty(Undo(scene));Assert.Empty(scene.World.Construction.Connections);
            SelectSource();Press("Connect Supply");Assert.Empty(Target(scene,gate));Press("Supply to PowerIn");
            var link=Assert.Single(scene.World.Construction.Connections);Assert.Equal(WorkshopConnectionDomain.Electrical,link.Domain);Assert.Equal(gate.AuthoredId,link.Target);
            Assert.Empty(Undo(scene));Assert.Empty(scene.World.Construction.Connections);
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
            var choices=Choices(scene);
            IEnumerable<Button> Buttons()=>choices.GetChildren().OfType<Button>();
            void Choose()
            {
                Assert.Empty(Select(scene,source));
                Assert.Single(Buttons(),button=>button.Text=="Connect Supply").EmitSignal(Button.SignalName.Pressed);
                Assert.Empty(Target(scene,gate));
            }
            var commands=client.EditCount;
            Choose();
            Assert.Equal(3,Buttons().Count());
            Assert.All(Buttons(),button=>Assert.NotNull(button.Icon));
            Assert.Empty(scene.World.Construction.Connections);
            Assert.Empty(Cancel(scene));
            Assert.Equal(commands,client.EditCount);

            foreach(var socket in new[]{WorkshopSocket.FirstIn,WorkshopSocket.SecondIn,WorkshopSocket.PowerIn})
            {
                Choose();
                Assert.Single(Buttons(),button=>(WorkshopSocket)button.GetMeta("socket").AsInt32()==socket).EmitSignal(Button.SignalName.Pressed);
                var link=Assert.Single(scene.World.Construction.Connections);
                Assert.Equal(socket,link.Input);
                Assert.Equal(WorkshopConnectionDomain.Electrical,link.Domain);

                var saved=new WorkshopSavedConstruction(scene.World.Construction,new(6));
                Assert.Equal(saved,WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(saved)));
                Assert.Empty(Undo(scene));
                Assert.Empty(scene.World.Construction.Connections);
            }
            var editsAfterChoice = client.EditCount;
            Assert.Empty(Undo(scene));
            Assert.Equal(editsAfterChoice, client.EditCount);
            Assert.Empty(godot.Errors.Drain());
        }
        finally {Release(scene);}
    }

    /// <summary>CAT-024 through the real chooser: every Not both gate socket needs an explicit choice, and a wire that would close a
    /// loop through the gate, even through an open switch, is refused on the bottom status line with no command and no Undo.</summary>
    [Theory]
    [InlineData(ElectricalNandTests.FeedbackLoop.NandToNand)]
    [InlineData(ElectricalNandTests.FeedbackLoop.ThroughOr)]
    [InlineData(ElectricalNandTests.FeedbackLoop.ThroughOpenSwitch)]
    public async Task NotBothGateChooserRefusesALoopClosingWireOnTheStatusLine(ElectricalNandTests.FeedbackLoop loop)
    {
        var (scene,client)=BenchScene(ElectricalNandTests.Kind,PlacementAnswer.Applied);
        try
        {
            var (open,closing,ids)=ElectricalNandTests.LoopCase(loop);
            await scene.World.ReplaceConstruction(open);
            var choices=Choices(scene);
            IEnumerable<Button> Buttons()=>choices.GetChildren().OfType<Button>();
            MachinePart Part(GpuBodyId id)=>scene.World.Parts.Single(part=>part.AuthoredId==id);
            Assert.Empty(Select(scene,Part(closing.Source)));
            Assert.Single(Buttons(),button=>button.Text=="Connect Supply").EmitSignal(Button.SignalName.Pressed);
            Assert.Empty(Target(scene,Part(closing.Target)));
            Assert.Equal(3,Buttons().Count()); // first, second and bottom supply: no default socket
            Assert.All(Buttons(),button=>Assert.NotNull(button.Icon));
            var commands=client.EditCount;
            Assert.Single(Buttons(),button=>(WorkshopSocket)button.GetMeta("socket").AsInt32()==closing.Input).EmitSignal(Button.SignalName.Pressed);
            Assert.Equal(open,scene.World.Construction);
            Assert.Equal(commands,client.EditCount);

            var status=Assert.Single(Descendants<Label>(scene), label => label.Name == "WorkshopStatus").Text;
            Assert.Contains("parts "+ids+".",status,StringComparison.Ordinal);
            Assert.StartsWith("Break the wire loop",status,StringComparison.Ordinal);
            var editsAfterChoice = client.EditCount;
            Assert.Empty(Undo(scene));
            Assert.Equal(editsAfterChoice, client.EditCount);
            Assert.Empty(godot.Errors.Drain());
        }
        finally {Release(scene);}
    }

    /// <summary>CAT-025 through the real chooser: every Neither gate socket needs an explicit choice, and a wire that would close a
    /// loop through the gate (every CAT-025 loop case, including through an open switch and into the bottom supply) is refused on
    /// the bottom status line with no command and no Undo.</summary>
    [Theory]
    [InlineData(ElectricalNorTests.FeedbackLoop.NorToNor)]
    [InlineData(ElectricalNorTests.FeedbackLoop.ThroughBothGate)]
    [InlineData(ElectricalNorTests.FeedbackLoop.ThroughOr)]
    [InlineData(ElectricalNorTests.FeedbackLoop.MixedWithNand)]
    [InlineData(ElectricalNorTests.FeedbackLoop.ThroughOpenSwitch)]
    [InlineData(ElectricalNorTests.FeedbackLoop.EnteringPowerIn)]
    public async Task NeitherGateChooserRefusesALoopClosingWireOnTheStatusLine(ElectricalNorTests.FeedbackLoop loop)
    {
        var (scene,client)=BenchScene(ElectricalNorTests.Kind,PlacementAnswer.Applied);
        try
        {
            var (open,closing,ids)=ElectricalNorTests.LoopCase(loop);
            await scene.World.ReplaceConstruction(open);
            var choices=Choices(scene);
            IEnumerable<Button> Buttons()=>choices.GetChildren().OfType<Button>();
            MachinePart Part(GpuBodyId id)=>scene.World.Parts.Single(part=>part.AuthoredId==id);
            Assert.Empty(Select(scene,Part(closing.Source)));
            Assert.Single(Buttons(),button=>button.Text=="Connect Supply").EmitSignal(Button.SignalName.Pressed);
            Assert.Empty(Target(scene,Part(closing.Target)));
            Assert.Equal(3,Buttons().Count()); // first, second and bottom supply: no default socket
            Assert.All(Buttons(),button=>Assert.NotNull(button.Icon));
            var commands=client.EditCount;
            Assert.Single(Buttons(),button=>(WorkshopSocket)button.GetMeta("socket").AsInt32()==closing.Input).EmitSignal(Button.SignalName.Pressed);
            Assert.Equal(open,scene.World.Construction);
            Assert.Equal(commands,client.EditCount);

            var status=Assert.Single(Descendants<Label>(scene), label => label.Name == "WorkshopStatus").Text;
            Assert.Contains("parts "+ids+".",status,StringComparison.Ordinal);
            Assert.StartsWith("Break the wire loop",status,StringComparison.Ordinal);
            var editsAfterChoice = client.EditCount;
            Assert.Empty(Undo(scene));
            Assert.Equal(editsAfterChoice, client.EditCount);
            Assert.Empty(godot.Errors.Drain());
        }
        finally {Release(scene);}
    }

    /// <summary>CAT-026 through the real chooser: a wire closing a pure Either-gate loop is admitted with one Undo entry, and a wire
    /// closing a loop with a Not both, Neither or Exactly one gate in it is refused on the bottom status line with no command and no
    /// Undo.</summary>
    [Theory]
    [InlineData(ElectricalOrTests.FeedbackLoop.OrToOr)]
    [InlineData(ElectricalOrTests.FeedbackLoop.WithNand)]
    [InlineData(ElectricalOrTests.FeedbackLoop.WithNor)]
    [InlineData(ElectricalOrTests.FeedbackLoop.WithXor)]
    [InlineData(ElectricalOrTests.FeedbackLoop.ThroughOpenSwitch)]
    [InlineData(ElectricalOrTests.FeedbackLoop.EnteringPowerIn)]
    public async Task EitherGateChooserAdmitsAnOrLoopAndRefusesAMixedOne(ElectricalOrTests.FeedbackLoop loop)
    {
        var (scene,client)=BenchScene(ElectricalOrTests.Kind,PlacementAnswer.Applied);
        try
        {
            var (open,closing,ids)=ElectricalOrTests.LoopCase(loop);
            await scene.World.ReplaceConstruction(open);
            var choices=Choices(scene);
            IEnumerable<Button> Buttons()=>choices.GetChildren().OfType<Button>();
            MachinePart Part(GpuBodyId id)=>scene.World.Parts.Single(part=>part.AuthoredId==id);
            Assert.Empty(Select(scene,Part(closing.Source)));
            Assert.Single(Buttons(),button=>button.Text=="Connect Supply").EmitSignal(Button.SignalName.Pressed);
            Assert.Empty(Target(scene,Part(closing.Target)));
            Assert.Equal(3,Buttons().Count()); // first, second and bottom supply: no default socket
            Assert.All(Buttons(),button=>Assert.NotNull(button.Icon));
            var commands=client.EditCount;
            Assert.Single(Buttons(),button=>(WorkshopSocket)button.GetMeta("socket").AsInt32()==closing.Input).EmitSignal(Button.SignalName.Pressed);
            if(ids is null)
            {
                Assert.Contains(closing,scene.World.Construction.Connections);
                Assert.Equal(open.Connections.Count+1,scene.World.Construction.Connections.Count);

                Assert.Empty(Undo(scene));
                Assert.Equal(open.Connections,scene.World.Construction.Connections);
            }
            else
            {
                Assert.Equal(open,scene.World.Construction);
                Assert.Equal(commands,client.EditCount);

                var status=Assert.Single(Descendants<Label>(scene), label => label.Name == "WorkshopStatus").Text;
                Assert.Contains("parts "+ids+".",status,StringComparison.Ordinal);
                Assert.StartsWith("Break the wire loop",status,StringComparison.Ordinal);
            }
            var editsAfterChoice = client.EditCount;
            Assert.Empty(Undo(scene));
            Assert.Equal(editsAfterChoice, client.EditCount);
            Assert.Empty(godot.Errors.Drain());
        }
        finally {Release(scene);}
    }

    /// <summary>CAT-027 through the real chooser: every wire closing a loop through an Exactly one gate is refused on the bottom
    /// status line with no command and no Undo; an Or loop feeding the gate off the cycle is admitted with one Undo entry.</summary>
    [Theory]
    [InlineData(ElectricalXorTests.FeedbackLoop.XorToXor)]
    [InlineData(ElectricalXorTests.FeedbackLoop.WithOr)]
    [InlineData(ElectricalXorTests.FeedbackLoop.WithNand)]
    [InlineData(ElectricalXorTests.FeedbackLoop.ThroughOpenSwitch)]
    [InlineData(ElectricalXorTests.FeedbackLoop.EnteringPowerIn)]
    [InlineData(ElectricalXorTests.FeedbackLoop.OrLoopFeedingXor)]
    public async Task ExactlyOneGateChooserRefusesALoopThroughTheGateAndAdmitsAnOrLoopBeside(ElectricalXorTests.FeedbackLoop loop)
    {
        var (scene,client)=BenchScene(ElectricalXorTests.Kind,PlacementAnswer.Applied);
        try
        {
            var (open,closing,ids)=ElectricalXorTests.LoopCase(loop);
            await scene.World.ReplaceConstruction(open);
            var choices=Choices(scene);
            IEnumerable<Button> Buttons()=>choices.GetChildren().OfType<Button>();
            MachinePart Part(GpuBodyId id)=>scene.World.Parts.Single(part=>part.AuthoredId==id);
            Assert.Empty(Select(scene,Part(closing.Source)));
            Assert.Single(Buttons(),button=>button.Text=="Connect Supply").EmitSignal(Button.SignalName.Pressed);
            Assert.Empty(Target(scene,Part(closing.Target)));
            Assert.Equal(3,Buttons().Count()); // first, second and bottom supply: no default socket
            Assert.All(Buttons(),button=>Assert.NotNull(button.Icon));
            var commands=client.EditCount;
            Assert.Single(Buttons(),button=>(WorkshopSocket)button.GetMeta("socket").AsInt32()==closing.Input).EmitSignal(Button.SignalName.Pressed);
            if(ids is null)
            {
                Assert.Contains(closing,scene.World.Construction.Connections);
                Assert.Equal(open.Connections.Count+1,scene.World.Construction.Connections.Count);

                Assert.Empty(Undo(scene));
                Assert.Equal(open.Connections,scene.World.Construction.Connections);
            }
            else
            {
                Assert.Equal(open,scene.World.Construction);
                Assert.Equal(commands,client.EditCount);

                var status=Assert.Single(Descendants<Label>(scene), label => label.Name == "WorkshopStatus").Text;
                Assert.Contains("parts "+ids+".",status,StringComparison.Ordinal);
                Assert.StartsWith("Break the wire loop",status,StringComparison.Ordinal);
            }
            var editsAfterChoice = client.EditCount;
            Assert.Empty(Undo(scene));
            Assert.Equal(editsAfterChoice, client.EditCount);
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
                // A payload is placed as a zero-parameter generic instance; its material is its package's alone.
                if (DeclaredPhysics.IsPayload(id)) Assert.Empty(Assert.IsType<WorkshopGenericInstance>(instance).Parameters);
                new WorkshopInstances(instance).Validate();
                var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(instance));
                var saved = new WorkshopSavedConstruction(construction, new(body));
                var bytes = WorkshopSaveCodec.Encode(saved);
                var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
                Assert.Equal(id, Assert.Single(decoded.Construction.Instances).PartId);
                Assert.Equal(saved, decoded);
                // The bench placement reaches the Simulation worker as a Construct command on the binary wire.
                var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), CompiledSceneFixture.Compile(construction),
                    Session: new(1, 1), Cadence: new(1), Projection: new(1));
                var wire = WorkshopWireFlatBuffers.EncodeCommand(command);
                var received = WorkshopWireFlatBuffers.DecodeCommand(wire).Scene;
                Assert.Equal(instance.Id, Assert.Single(received!.Owners));
                Assert.Equal(command.Sequence, WorkshopWireFlatBuffers.DecodeCommand(wire).Sequence);
            }
        }
        finally { world.Free(); }
    }

    [Fact]
    public void UndeliveredPartsHaveNoPackageNoInstancePathAndNeverReachTheRegistryOrPalette()
    {
        var world = DiscoveredWorld();
        try
        {
            foreach (var part in new PartId[] { new("tennis"), new("balloon") })
            {
                var id = part;
                // No declaration is shipped for this identity; unknown input is refused before placement.
                Assert.DoesNotContain(PartRegistry.Read(Godot.FileAccess.GetFileAsString(PartRegistry.BundlePath)),
                    entry => entry.Metadata.Id == id.Value);
                EditRefusal.Is(WorkshopEditRejection.UnsupportedPart, () => world.CaptureInstance(id, new(1), BenchPoint, Quaternion.Identity));
                Assert.False(world.Registry.Definitions.ContainsKey(id), $"{id} must not be registered.");
                Assert.False(world.Registry.Free.ContainsKey(id), $"{id} must not be offered by the Free palette.");
            }
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
        // A bench point outside the world is a caller defect: the canonical position boundary throws a plain argument exception,
        // not a declared refusal, so Propose must not turn it into a placement rejection (the handler boundary logs it as a fault).
        // C-Memory: the latch is a generic instance of a discovered part, so the defect is proposed on a discovered world.
        var discovered = DiscoveredWorld();
        try
        {
            var undo = 0;
            var defect = Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopBenchPlacement.Propose(discovered, new PartId("latch"), new(7),
                new(100, 0, 0), Quaternion.Identity, null, () => undo++));
            Assert.Equal("metres", defect.ParamName);
            Assert.Equal(0, undo);
        }
        finally { discovered.Free(); }
        var world = new MachineWorld();
        try
        {
            var undo = 0;
            // Control: with no discovered registry a payload is a generic instance the declared rule refuses (UnsupportedPart).
            var refused = WorkshopBenchPlacement.Propose(world, new PartId("ball"), new(7), BenchPoint, Quaternion.Identity, null, () => undo++);
            Assert.Equal(BenchPlacementRejection.UnsupportedPart, refused.Rejection);
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
            var placement = WorkshopBenchPlacement.Propose(world, new PartId("ramp"), new(7), BenchPoint, Quaternion.Identity,
                null, () => undo++);
            Assert.Equal(BenchPlacementRejection.None, placement.Rejection);
            Assert.Equal(1, undo);
            Assert.Empty(placement.Message);
            Assert.Equal(new PartId("ramp"), Assert.Single(placement.Construction.Instances, i => i.Id == new GpuBodyId(7)).PartId);
            Assert.Equal(before, world.Construction); // proposing never commits; the worker admits the construction
        }
        finally { world.Free(); }
    }

    [Fact]
    public async Task IdentityExhaustionRefusesTheProposalWithoutUndoAndWideIdentitySucceeds()
    {
        var world = DiscoveredWorld();
        try
        {
            var lesson = PuzzleCatalogue.Create(new PuzzleId("ready_to_rebound"), new(1), WorkshopCadenceSettings.Default(), new(1), new(.5f));
            await world.InitializeWorkshop(() => Task.FromResult<IWorkshopClient>(new PlacementClient(PlacementAnswer.Applied)));
            await world.ReplaceConstruction(lesson);
            lesson = world.Construction;
            var undo = 0;
            // A representationally exhausted identity refuses the complete proposed scene atomically.
            var refused = WorkshopBenchPlacement.Propose(world, new PartId("basket"), new(ulong.MaxValue), BenchPoint, Quaternion.Identity,
                null, () => undo++);
            Assert.Equal(BenchPlacementRejection.UnsupportedPart, refused.Rejection);
            Assert.StartsWith("Placement rejected: ", refused.Message);
            Assert.Equal("Placement rejected: The authored identities leave no space for the compiled scene.", refused.Message);
            Assert.Empty(godot.Errors.Drain());
            Assert.Equal(0, undo);
            Assert.Equal(lesson, refused.Construction);
            // Control: a wide representable identity is admitted, with one undo and the precise authored goal.
            var composed = WorkshopBenchPlacement.Propose(world, new PartId("basket"), new(1UL << 32), BenchPoint, Quaternion.Identity,
                null, () => undo++);
            Assert.Equal(BenchPlacementRejection.None, composed.Rejection);
            Assert.Equal(1, undo);
            Assert.Equal(new GpuBodyId(1UL << 32), composed.Construction.Puzzle.Goal.Target);
        }
        finally { world.Free(); }
    }


    private (Workshop Scene, PlacementClient Client) BenchScene(PartId tool, PlacementAnswer answer = PlacementAnswer.Pending, IWorkshopSaveStore? store = null, SubViewport? viewport = null)
    {
        var client = new PlacementClient(answer);
        var scene = GD.Load<PackedScene>("res://scenes/workshop.tscn").Instantiate<Workshop>();
        scene.ClientFactory = () => Task.FromResult<IWorkshopClient>(client);
        if (store is not null) scene.SaveStore = store;
        if (viewport is null) godot.Tree.Root.AddChild(scene);
        else viewport.AddChild(scene);
        scene.Startup.GetAwaiter().GetResult();
        scene.SetProcess(false);
        scene.SetPhysicsProcess(false);
        // Orbit mode places on the horizontal bench, through the same mouse input as a player.
        using var orbit = new InputEventMouseButton { ButtonIndex = MouseButton.Right, Pressed = true };
        scene._UnhandledInput(orbit);
        orbit.Pressed = false;
        scene._UnhandledInput(orbit);
        Choose(scene, tool);
        return (scene, client);
    }

    /// <summary>Clicks the bench and returns every exception that escaped the async void handler. An escaping exception is posted
    /// to the captured synchronization context: natively Godot's logs it, but in the browser there is none and the thread pool
    /// terminates the .NET runtime (Story 9R.0c F1), so the click runs under a context that records instead.</summary>
    private static List<Exception> ClickBench(Workshop scene)
    {
        var height = 3f; // the authored initial placement plane, set when choosing a palette part
        var screen = Assert.Single(Descendants<Camera3D>(scene)).UnprojectPosition(new Vector3(0, height, 0));
        return Click(scene, screen);
    }

    private static VBoxContainer Choices(Workshop scene) =>
        Assert.Single(Descendants<VBoxContainer>(scene), panel => panel.Name == "ConnectionChoices");
    private static void Choose(Workshop scene, PartId part) =>
        Assert.Single(Descendants<Button>(scene), button =>
            button.HasMeta("part_kind") && button.GetMeta("part_kind").AsString() == part.Value)
            .EmitSignal(Button.SignalName.Pressed);
    private static List<Exception> Input(Workshop scene, InputEvent input)
    {
        var recorder = new EscapeRecorder();
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(recorder);
        try { scene._UnhandledInput(input); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
        return recorder.Escaped;
    }
    private static List<Exception> Click(Workshop scene, Vector2 screen)
    {
        using var input = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = screen };
        return Input(scene, input);
    }
    private static List<Exception> Undo(Workshop scene)
    {
        using var input = new InputEventKey { Keycode = Key.Z, CtrlPressed = true, Pressed = true };
        return Input(scene, input);
    }
    private static List<Exception> Cancel(Workshop scene)
    {
        using var input = new InputEventKey { Keycode = Key.Escape, Pressed = true };
        return Input(scene, input);
    }
    private static List<Exception> Select(Workshop scene, MachinePart part)
    {
        Assert.Empty(Cancel(scene));
        return Click(scene, Assert.Single(Descendants<Camera3D>(scene)).UnprojectPosition(part.GlobalPosition));
    }
    private static List<Exception> Target(Workshop scene, MachinePart part) =>
        Click(scene, Assert.Single(Descendants<Camera3D>(scene)).UnprojectPosition(part.GlobalPosition));
    private static List<Exception> Apply(Workshop scene, GpuBodyId owner, InstanceParameterCollection parameters)
    {
        scene.ApplyDeclaredParametersAsync(owner, parameters).GetAwaiter().GetResult();
        return [];
    }
    private static List<Exception> EndGesture(Workshop scene, bool cancel)
    {
        using InputEvent input = cancel
            ? new InputEventKey { Keycode = Key.Escape, Pressed = true }
            : new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false };
        scene._Input(input);
        return [];
    }
    private static WorkshopConstruction Install(Workshop scene, WorkshopConstruction construction)
    {
        var completion = scene.World.ReplaceConstruction(construction).GetAwaiter().GetResult();
        Assert.Equal(WorkshopCommandOutcome.Applied, completion.Result.Outcome);
        scene._PhysicsProcess(0);
        return scene.World.Construction;
    }
    private static void Phase(Workshop scene, WorkshopSimulationPhase phase)
    {
        var command = phase switch
        {
            WorkshopSimulationPhase.Running => WorkshopCommandKind.Run,
            WorkshopSimulationPhase.Paused => WorkshopCommandKind.Pause,
            WorkshopSimulationPhase.Building => WorkshopCommandKind.Reset,
            _ => throw new ArgumentOutOfRangeException(nameof(phase))
        };
        var operation = command switch
        {
            WorkshopCommandKind.Run => scene.World.RunWorkshop(),
            WorkshopCommandKind.Reset => scene.World.ResetWorkshop(),
            _ => scene.World.PlaybackWorkshop(command)
        };
        operation.GetAwaiter().GetResult();
        scene._PhysicsProcess(0);
    }

    public static TheoryData<PartId, ParameterId, string, string[]> ParameterUndoCases() => new()
    {
        { new PartId("delay"), new("delay_seconds"), "2.5", ["0", "13", "NaN", "Infinity"] },
        { new("hold_timer"), new("hold_seconds"), "2.5", ["0", "13", "NaN", "Infinity"] },
        { new PartId("clock"), new("interval_seconds"), "2.5", ["NaN", "Infinity", "-Infinity", "two", ""] },
        { new PartId("pressure_plate"), new("minimum_mass"), "2.5", ["0.09", "16.01", "NaN", "Infinity", "two"] },
        { new PartId("counter"), new("target_count"), "4", ["0", "10", "2.5", "-1", "NaN", "Infinity", "three"] },
        { new PartId("wall"), new("width"), "4", ["0", "NaN"] },
        { new PartId("switch"), new("trigger_threshold"), "2", ["NaN"] },
        { new PartId("bumper"), new("strength"), "12", ["NaN"] },
        { WeightTests.Weight, new("mass"), "6.5", ["NaN", "Infinity", "-Infinity", "heavy", ""] },
        { new PartId("battery"), new("capacity"), "900", ["NaN"] },
        { new PartId("battery"), new("power_limit"), "90", ["NaN"] },
        { new PartId("battery"), new("initial_charge"), "0.5", ["NaN"] },
        { new PartId("spring"), new("stiffness"), "500", ["119", "1201", "NaN"] },
        { new PartId("spring"), new("damping"), "1", ["-0.1", "8.1", "NaN"] },
        { new PartId("spring"), new("initial_compression"), "0.1", ["-0.01", "0.21", "NaN"] },
        { new PartId("ramp"), new("length"), "1.234567", ["0.1", "4.5", "NaN"] }
    };

    [Theory]
    [MemberData(nameof(ParameterUndoCases))]
    public void RejectedAndUnchangedParameterSubmissionsPreserveThePreviousRealUndo(
        PartId part, ParameterId parameter, string acceptedText, string[] rejectedTexts)
    {
        var (scene, client) = BenchScene(part, PlacementAnswer.Applied);
        try
        {
            var empty = scene.World.Construction;
            Assert.Empty(ClickBench(scene));
            var placed = scene.World.Construction;
            void Press(string label) => Assert.Single(Descendants<Button>(Choices(scene)),
                button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            void SubmitWithoutEdit()
            {
                var before = scene.World.Construction;
                var commands = client.EditCount;
                foreach (var text in rejectedTexts)
                {
                    Press("Configure");
                    Assert.Single(Descendants<LineEdit>(Choices(scene)), field => field.Name == parameter.Value).Text = text;
                    Press("Apply parameters");
                    Assert.Equal(before, scene.World.Construction);
                    Assert.Equal(commands, client.EditCount);
                    Press("Cancel");
                }
                Press("Configure");
                Press("Apply parameters"); // Unchanged values must not add an Undo entry either.
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(commands, client.EditCount);
            }
            SubmitWithoutEdit();
            Press("Configure");
            Assert.Single(Descendants<LineEdit>(Choices(scene)), field => field.Name == parameter.Value).Text = acceptedText;
            Press("Apply parameters");
            Assert.NotEqual(placed.Instances, scene.World.Construction.Instances);
            SubmitWithoutEdit();
            Assert.Empty(Undo(scene));
            Assert.Equal(placed.Instances, scene.World.Construction.Instances);
            Assert.Equal(placed.Connections, scene.World.Construction.Connections);
            Assert.Empty(Undo(scene));
            Assert.Equal(empty.Instances, scene.World.Construction.Instances);
            Assert.Equal(empty.Connections, scene.World.Construction.Connections);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    // An edit's observable Undo result, then reapply the admitted parameters for the next scenario.
    private static void AssertParameterUndo(Workshop scene, ref WorkshopConstruction prior)
    {
        var edited = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
        Assert.Empty(Undo(scene));
        Assert.Equal(prior.Instances, scene.World.Construction.Instances);
        Assert.Equal(prior.Connections, scene.World.Construction.Connections);
        Assert.Empty(Select(scene, Assert.Single(scene.World.Parts)));
        Assert.Empty(Apply(scene, edited.Id, edited.Parameters));
        Assert.Equal(edited, Assert.Single(scene.World.Construction.Instances));
        prior = scene.World.Construction;
    }

    private static string Status(Workshop scene) =>
        Assert.Single(Descendants<Label>(scene), label => label.Name == "WorkshopStatus").Text;
    private static void Action(Workshop scene, string label) =>
        Assert.Single(Descendants<Button>(scene), button => button.HasMeta("action_label") &&
            button.GetMeta("action_label").AsString() == label).EmitSignal(Button.SignalName.Pressed);
    private sealed class MemoryStore : IWorkshopSaveStore
    {
        public byte[] Bytes { get; set; } = [];
        public Task Save(byte[] bytes) { Bytes = bytes; return Task.CompletedTask; }
        public Task<string> Load(int expectedBytes) => Task.FromResult(Convert.ToBase64String(Bytes));
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
        var id = new PuzzleId("first_principles");
        var count = (ulong)PuzzleCatalogue.Definition(id).Placements.Length;
        var exhausted = new GpuBodyId(ulong.MaxValue - count + 1);
        var store = new MemoryStore();
        var (scene, client) = BenchScene(new PartId("ramp"), PlacementAnswer.Applied, store);
        try
        {
            store.Bytes = WorkshopSaveCodec.Encode(new(scene.World.Construction, exhausted));
            Action(scene, "Load");
            var before = scene.World.Construction;
            var commands = client.EditCount;
            var picker = Assert.Single(Descendants<OptionButton>(scene),
                node => node.Name == WorkshopUiControlBoundary.NodeName(WorkshopUiControl.LevelPicker));
            var index = Enumerable.Range(0, picker.ItemCount).Single(i => picker.GetItemText(i) == PuzzleCatalogue.Presentation(id).Title);
            picker.EmitSignal(OptionButton.SignalName.ItemSelected, index);
            Assert.Equal(before, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
            Assert.Equal("Puzzle identities are exhausted.", Status(scene));
            Assert.Empty(Undo(scene));
            Assert.Equal(commands, client.EditCount);
            Action(scene, "Save");
            Assert.Equal(exhausted, WorkshopSaveCodec.Decode(SaveFixture.Elements, store.Bytes).NextBodyId);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    [Fact]
    public void UndeliveredPartsCannotBeSelectedFromTheActualPalette()
    {
        var (scene, client) = BenchScene(new PartId("ramp"), PlacementAnswer.Applied);
        try
        {
            var buttons = Descendants<Button>(scene).Where(button => button.HasMeta("part_kind")).ToArray();
            foreach (var part in new PartId[] { new("tennis"), new("balloon") })
                Assert.DoesNotContain(buttons, button => button.GetMeta("part_kind").AsString() == part.Value);
            Assert.Empty(client.Commands);
            Assert.Empty(scene.World.Construction.Instances);
            Assert.Empty(ClickBench(scene));
            Assert.Equal(new PartId("ramp"), Assert.Single(scene.World.Construction.Instances).PartId);
            Assert.Empty(Undo(scene));
            Assert.Empty(scene.World.Construction.Instances);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    [Fact]
    public void ClickingTheBenchWithAPalettePartWaitsForAdmissionAndUndoRestoresItsConstruction()
    {
        var (scene, client) = BenchScene(new PartId("ramp"));
        try
        {
            var before = scene.World.Construction;
            Assert.Empty(ClickBench(scene));
            var command = Assert.Single(client.Commands);
            Assert.Equal(WorkshopCommandKind.Construct, command.Kind);
            Assert.Equal(new PartId("ramp"), Assert.Single(Assert.NotNull(command.Construction).Instances).PartId);
            Assert.Equal(before, scene.World.Construction);
            Assert.Empty(Undo(scene));
            Assert.Single(client.Commands);
            client.Complete();
            Assert.Equal(new PartId("ramp"), Assert.Single(scene.World.Construction.Instances).PartId);
            client.Answer = PlacementAnswer.Applied;
            Assert.Empty(Undo(scene));
            Assert.Equal(before.Instances, scene.World.Construction.Instances);
            var commands = client.EditCount;
            Assert.Empty(Undo(scene));
            Assert.Equal(commands, client.EditCount);
        }
        finally { Release(scene); }
    }

    [Fact]
    public void RemovingTheSceneDuringAdmissionRetiresItsPresentationWithoutEscaping()
    {
        var (scene, client) = BenchScene(new PartId("ramp"));
        try
        {
            Assert.Empty(ClickBench(scene));
            godot.Tree.Root.RemoveChild(scene);
            client.Complete();
            Assert.Empty(godot.Errors.Drain());
            Assert.Single(client.Commands);
        }
        finally { Release(scene); }
        var (fresh, _) = BenchScene(new PartId("ramp"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(fresh));
            Assert.Equal(new PartId("ramp"), Assert.Single(fresh.World.Construction.Instances).PartId);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(fresh); }
    }

    [Fact]
    public void AnUndoTransportFaultIsCaughtLoggedOnceAndShown()
    {
        var (scene, client) = BenchScene(new PartId("ramp"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var accepted = scene.World.Construction;
            client.NextFailure = new InvalidOperationException("worker disconnected");
            Assert.Empty(Undo(scene));
            var diagnostic = Assert.IsType<WorkshopFaultDiagnostic>(Assert.Single(scene.Diagnostics.Snapshot()));
            Assert.Equal(FaultSite.UiHandler, diagnostic.Site);
            Assert.Equal(WorkshopFaultKind.Fault, diagnostic.Kind);
            Assert.Contains("worker disconnected", diagnostic.Detail, StringComparison.Ordinal);
            Assert.Contains("worker disconnected", Status(scene), StringComparison.Ordinal);
            Assert.Empty(godot.Errors.Drain());
            Assert.Equal(accepted, scene.World.Construction);
        }
        finally { Release(scene); }
    }

    [Fact]
    public void ARefusedPlacementLeavesNoUndoAndKeepsItsIdentity()
    {
        var (scene, client) = BenchScene(new PartId("ramp"), PlacementAnswer.Rejected);
        try
        {
            Assert.Empty(ClickBench(scene));
            Assert.Single(client.Commands);
            Assert.StartsWith("Placement rejected: ", Status(scene), StringComparison.Ordinal);
            Assert.Empty(Undo(scene));
            Assert.Single(client.Commands);
            client.Answer = PlacementAnswer.Applied;
            Assert.Empty(ClickBench(scene));
            Assert.Equal(new GpuBodyId(1), Assert.Single(scene.World.Construction.Instances).Id);
            Assert.Empty(Undo(scene));
            Assert.Empty(scene.World.Construction.Instances);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    [Fact]
    public void GenericWallEditorUsesDeclaredFieldsAndRejectsInvalidOrDisallowedEditsAtomically()
    {
        var (scene, client) = BenchScene(new PartId("wall"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var part = Assert.Single(scene.World.Parts);
            var choices = Choices(scene);
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
            var commands = client.EditCount;
            var undos = scene.World.Construction;
            fields[new("width")].Text = "0";
            Press("Apply parameters");
            Assert.Equal(before, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
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
            AssertParameterUndo(scene, ref undos);
            var accepted = scene.World.Construction;
            commands = client.EditCount;
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                Phase(scene, phase);
                Assert.Empty(Apply(scene, current.Id, WallFixture.Maximum));
                Assert.Equal(accepted, scene.World.Construction);
                Assert.Equal(commands, client.EditCount);
            }
            Phase(scene, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(current with { Locked = true }) };
            locked = Install(scene, locked);
            commands = client.EditCount;
            Assert.Empty(Apply(scene, current.Id, WallFixture.Maximum));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    [Fact]
    public void GenericImpactEditorClampsOnlyDeclaredFiniteInputAndPreservesEditGuards()
    {
        var (scene, client) = BenchScene(new PartId("switch"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Choices(scene);
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            var key = new ParameterId("trigger_threshold");
            var initial = scene.World.Construction;
            var undo = scene.World.Construction;
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
                AssertParameterUndo(scene, ref undo);
            }
            var accepted = scene.World.Construction;
            var commands = client.EditCount;
            Press("Configure");
            Assert.Single(Nodes<LineEdit>(choices)).Text = "NaN";
            Press("Apply parameters");
            Assert.Equal(accepted, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
            Press("Cancel");
            var values = new InstanceParameterCollection([new(key, 2f)]);
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                Phase(scene, phase);
                Assert.Empty(Apply(scene, Current().Id, values));
                Assert.Equal(accepted, scene.World.Construction);
            }
            Phase(scene, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(Current() with { Locked = true }) };
            locked = Install(scene, locked);
            commands = client.EditCount;
            Assert.Empty(Apply(scene, Current().Id, values));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    // C-Lamp+Detector (9A.36/9A.37) and 9A.32: the Signal lamp, the Ball detector and the Domino placed through the drawer are their
    // packages' generic instances with no parameters, so selecting one offers no Configure panel (control: the Impact switch editor above offers one)
    // and the declared-parameter path refuses any value. A rotate and a move gesture each record one Undo, keep the generic
    // instance and are undone by one Undo. Running, Paused and a locked part refuse an edit and send nothing.
    [Theory]
    [InlineData("lamp")]
    [InlineData("ball_detector")]
    [InlineData("domino")]
    public void ZeroParameterSignalPartsArePlacedGenericWithoutAPanelAndEditAsOneUndoPerGesture(string id)
    {
        var kind = new PartId(id);
        var (scene, client) = BenchScene(kind, PlacementAnswer.Applied);
        try
        {
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Empty(ClickBench(scene));
            Assert.Equal((kind, 0), (Current().PartId, Current().Parameters.Count));
            var choices = Choices(scene);
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            Assert.Empty(Select(scene, Assert.Single(scene.World.Parts)));
            Assert.DoesNotContain(Nodes<Button>(choices), button => button.Text == "Configure");
            var placed = scene.World.Construction;
            var commands = client.EditCount;
            Assert.Empty(Apply(scene, Current().Id,
                new InstanceParameterCollection([new(new ParameterId("radius"), .5f)])));
            Assert.Equal((placed, commands), (scene.World.Construction, client.EditCount));

            var camera = Assert.Single(Descendants<Camera3D>(scene));
            var gizmo = Assert.Single(Descendants<RotationGizmo>(scene));
            foreach (var (move, axis) in new[] { (false, 2), (true, 0) })
            {
                var part = Assert.Single(scene.World.Parts);
                Assert.Empty(Select(scene, part));
                gizmo.SetMoveMode(move);
                gizmo.Follow(part, true, camera);
                var before = scene.World.Construction;
                var handle = camera.UnprojectPosition(gizmo.HandlePosition(axis));
                Assert.Empty(Click(scene, handle));
                Assert.True(gizmo.Dragging, move ? "move" : "rotate");
                var along = camera.UnprojectPosition(part.GlobalPosition + Vector3.Right) - camera.UnprojectPosition(part.GlobalPosition);
                Assert.True(gizmo.Drag(camera, handle + (move ? along * .5f : new Vector2(90, 90)), move));
                Assert.Empty(EndGesture(scene, false));
                Assert.NotEqual(before.Instances, scene.World.Construction.Instances);
                Assert.Equal((kind, 0, before.Instances[0].Id), (Current().PartId, Current().Parameters.Count, Current().Id));
                Assert.Empty(Undo(scene));
                Assert.Equal(before.Instances, scene.World.Construction.Instances);
            }

            var accepted = scene.World.Construction;
            commands = client.EditCount;
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                Phase(scene, phase);
                Assert.Empty(Apply(scene, Current().Id, InstanceParameterCollection.Empty));
                Assert.Equal(accepted, scene.World.Construction);
            }
            Phase(scene, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(Current() with { Locked = true }) };
            locked = Install(scene, locked);
            commands = client.EditCount;
            Assert.Empty(Apply(scene, Current().Id, InstanceParameterCollection.Empty));
            Assert.Equal((locked, commands), (scene.World.Construction, client.EditCount));
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    // The detector placed beside a free pipe mouth snaps onto it (CAT-002 / CAT-048 typed mouths): its start mouth joins the pipe's
    // end mouth 1.89 m from the pipe's centre, so the 0.28 m collar centres 0.05 m short of the clicked point, as one placement with
    // one Undo.
    [Fact]
    public void ADetectorPlacedBesideAPipeMouthSnapsOntoItAsOneUndo()
    {
        var store = new MemoryStore();
        var (scene, _) = BenchScene(new PartId("ball_detector"), PlacementAnswer.Applied, store);
        try
        {
            const float height = 3f;
            var pipe = WorkshopInput.Generic(new PartId("pipe"),new(1), -2.22f, height, 0, 0, 0, 0, 1);
            store.Bytes = WorkshopSaveCodec.Encode(new(new(new(2), WorkshopCadenceSettings.Default(), new(pipe)), new(2)));
            Action(scene, "Load");
            Choose(scene, new PartId("ball_detector"));
            Assert.Empty(ClickBench(scene));
            var detector = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances, instance => instance.PartId == new PartId("ball_detector")));
            var x = (detector.Cell.X + (double)detector.Local.X) / 16;
            var y = (detector.Cell.Y + (double)detector.Local.Y) / 16;
            Assert.InRange(x, -.051, -.049);
            Assert.InRange(y, height - .001, height + .001);
            Assert.Empty(detector.Parameters);
            Assert.Empty(Undo(scene));
            Assert.Equal(pipe, Assert.Single(scene.World.Construction.Instances));
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    // C-Bumper+Spring (9A.33): the Bumper placed through the drawer is a generic instance at its declared 8 m/s, and the generic editor
    // replaces its panel. Its strength is the declared Clamp policy: a finite out-of-range value clamps into 0–20 m/s (as the former
    // SpinBox did), NaN refuses atomically, Cancel changes nothing, one Apply records one Undo, and Running/Paused/locked guard it.
    [Fact]
    public void GenericBumperEditorClampsAFiniteStrengthAndRefusesNaNAtomically()
    {
        var (scene, client) = BenchScene(new PartId("bumper"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Choices(scene);
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            var key = new ParameterId("strength");
            Assert.Equal((new PartId("bumper"), 8f), (Current().PartId, Current().Parameters[key]));
            var initial = scene.World.Construction;
            var undo = scene.World.Construction;
            Press("Configure");
            Assert.Contains(Nodes<Label>(choices), label => label.Text == "strength (0–20 m/s)");
            Assert.Equal(key, new ParameterId(Assert.Single(Nodes<LineEdit>(choices)).Name.ToString()));
            Assert.Single(Nodes<LineEdit>(choices)).Text = "12";
            Press("Cancel");
            Assert.Equal(initial, scene.World.Construction);
            foreach (var (text, expected) in new[] { ("-1", 0f), ("25", 20f), ("12.345678", 12.345678f), ("0", 0f), ("20", 20f) })
            {
                Press("Configure");
                Assert.Single(Nodes<LineEdit>(choices)).Text = text;
                Press("Apply parameters");
                Assert.Equal(expected, Current().Parameters[key]);
                AssertParameterUndo(scene, ref undo);
                // The store the shared compiler builds is the edited strength's (½·1 kg)·s·s.
                Assert.Equal(.5f * expected * expected,
                    Assert.Single(WorkshopPhysicsCompiler.Compile(scene.World.Construction, new(1, 2)).ContactWorks.ToArray()).InitialEnergy.Value);
            }
            var accepted = scene.World.Construction;
            var commands = client.EditCount;
            Press("Configure");
            Assert.Single(Nodes<LineEdit>(choices)).Text = "NaN";
            Press("Apply parameters");
            Assert.Equal(accepted, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
            Press("Cancel");
            var values = new InstanceParameterCollection([new(key, 4f)]);
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                Phase(scene, phase);
                Assert.Empty(Apply(scene, Current().Id, values));
                Assert.Equal(accepted, scene.World.Construction);
            }
            Phase(scene, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(Current() with { Locked = true }) };
            locked = Install(scene, locked);
            commands = client.EditCount;
            Assert.Empty(Apply(scene, Current().Id, values));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    // Story 10.2a (D4): the Weight placed through the drawer is a generic instance at its declared 4 kg, and the generic editor names
    // mass in kg with its declared range. A finite out-of-range entry clamps into 0.25–8 kg (the Clamp policy) and applies as one Undo,
    // and the compiled body takes the mass; NaN, ±Infinity and text are refused with no change, no command and no Undo. Behind the
    // field, the same editor operation applies the declared Clamp policy; non-finite, missing and unknown mass values refuse
    // atomically. Serialization and domain admission remain strict (WeightTests covers those separate boundaries).
    [Fact]
    public async Task TheWeightMassEditorClampsFiniteEntriesAndRefusesMalformedParameters()
    {
        var key = new ParameterId("mass");
        var (scene, client) = BenchScene(WeightTests.Weight, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Choices(scene);
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal((WeightTests.Weight, 4f), (Current().PartId, Current().Parameters[key]));
            var undo = scene.World.Construction;
            Assert.Empty(Select(scene, Assert.Single(scene.World.Parts)));
            Press("Configure");
            Assert.Contains(Nodes<Label>(choices), label => label.Text == "mass (0.25–8 kg)");
            var field = Assert.Single(Nodes<LineEdit>(choices));
            Assert.Equal((key.Value, "4"), (field.Name.ToString(), field.Text));
            Press("Cancel");
            foreach (var (text, expected) in new[] { ("0.249", .25f), ("8.01", 8f), ("0", .25f), ("6.5", 6.5f), ("-1", .25f), ("8", 8f), ("0.25", .25f) })
            {
                Press("Configure");
                Assert.Single(Nodes<LineEdit>(choices)).Text = text;
                Press("Apply parameters");
                Assert.Equal(BitConverter.SingleToUInt32Bits(expected), BitConverter.SingleToUInt32Bits(Current().Parameters[key]));
                AssertParameterUndo(scene, ref undo);
                Assert.Equal(new Kilograms(expected),Assert.Single(WorkshopPhysicsCompiler.Compile(scene.World.Construction, new(1, 2))
                    .Bodies.ToArray(), body => body.Id == Current().Id).Mass);
            }
            foreach (var (value, expected) in new[] { (.249f, .25f), (8.01f, 8f), (0f, .25f), (-1f, .25f) })
            {
                var previous = Current().Parameters[key];
                await scene.ApplyDeclaredParametersAsync(Current().Id, new([new(key, value)]));
                Assert.Equal(expected, Current().Parameters[key]);
                if (previous != expected) AssertParameterUndo(scene, ref undo);
            }
            var accepted = scene.World.Construction;
            var commands = client.EditCount;
            foreach (var text in new[] { "NaN", "Infinity", "-Infinity", "two" })
            {
                Press("Configure");
                Assert.Single(Nodes<LineEdit>(choices)).Text = text;
                Press("Apply parameters");
                Assert.Equal((accepted, commands), (scene.World.Construction, client.EditCount));
                Press("Cancel");
            }
            var unknown = new ParameterId("radius");
            foreach (var values in new InstanceParameterCollection[]
            {
                new([new(key, float.NaN)]),
                new([new(key, float.PositiveInfinity)]), new([new(key, float.NegativeInfinity)]), new([]), new([new(unknown, 4f)]),
                new([new(key, 4f), new(unknown, .3f)])
            })
            {
                Assert.Empty(Apply(scene, Current().Id, values));
                Assert.Equal((accepted, commands), (scene.World.Construction, client.EditCount));
            }
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    private static IEnumerable<T> Descendants<T>(Node parent) where T : Node =>
        parent.GetChildren().SelectMany(child => new[] { child }.Concat(Descendants<Node>(child))).OfType<T>();
    private static void Refresh(Workshop scene) => Assert.Empty(Select(scene, Assert.Single(scene.World.Parts)));

    // Story 9A.39: the Battery placed through the drawer is a generic instance at its package defaults (3600 J, 120 W, full, Enabled;
    // owner decision 10 Oct 2026) and the generic editor replaces its panel. Capacity, power limit and initial charge are Clamp fields,
    // as the former SpinBoxes were; NaN refuses atomically; the enable state is an option list of its declared names; Cancel changes
    // nothing and one Apply records one Undo. The compiled store is the edited values.
    [Fact]
    public void GenericBatteryEditorClampsItsStoreAndPicksTheEnableStateFromItsOptions()
    {
        var (scene, client) = BenchScene(new PartId("battery"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Choices(scene);
            void Press(string label) => Assert.Single(Descendants<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Dictionary<string, LineEdit> Fields() => Descendants<LineEdit>(choices).ToDictionary(field => field.Name.ToString());
            OptionButton Enable() => Assert.Single(Descendants<OptionButton>(choices));
            float Value(string key) => Current().Parameters[new(key)];
            Assert.Equal((new PartId("battery"), 3600f, 120f, 1f, 1f), (Current().PartId, Value("capacity"), Value("power_limit"), Value("initial_charge"), Value("enabled")));
            var initial = scene.World.Construction;
            var undo = scene.World.Construction;
            Press("Configure");
            Assert.Equal(["capacity", "initial_charge", "power_limit"], Fields().Keys.Order(StringComparer.Ordinal));
            Assert.Contains(Descendants<Label>(choices), label => label.Text == "capacity (60–14400 J)");
            Assert.Contains(Descendants<Label>(choices), label => label.Text == "power_limit (10–480 W)");
            Assert.Equal(("enabled", 2, "Disabled", "Enabled", 1), (Enable().Name.ToString(), Enable().ItemCount, Enable().GetItemText(0), Enable().GetItemText(1), Enable().Selected));
            Fields()["capacity"].Text = "100"; Enable().Select(0);
            Press("Cancel");
            Assert.Equal(initial, scene.World.Construction);
            foreach (var (capacity, power, charge, enabled, expected) in new[]
            {
                ("50", "500", "0.5", 0, (60f, 480f, .5f, 0f)), ("20000", "5", "-1", 1, (14400f, 10f, 0f, 1f)),
                ("1234.5678", "77.25", "0.625", 0, (1234.5678f, 77.25f, .625f, 0f))
            })
            {
                Press("Configure");
                Fields()["capacity"].Text = capacity; Fields()["power_limit"].Text = power; Fields()["initial_charge"].Text = charge;
                Enable().Select(enabled);
                Press("Apply parameters");
                Assert.Equal(expected, (Value("capacity"), Value("power_limit"), Value("initial_charge"), Value("enabled")));
                AssertParameterUndo(scene, ref undo);
                var source = Assert.Single(WorkshopPhysicsCompiler.Compile(scene.World.Construction, new(1, 2)).Electrical.Sources.ToArray());
                Assert.Equal((expected.Item1, expected.Item2, expected.Item3, expected.Item4 == 1f ? ElectricalEnable.Enabled : ElectricalEnable.Disabled),
                    (source.Capacity.Value, source.MaximumPower.Value, source.InitialFraction, source.Enabled));
            }
            var accepted = scene.World.Construction;
            var commands = client.EditCount;
            Press("Configure");
            Fields()["capacity"].Text = "NaN";
            Press("Apply parameters");
            Assert.Equal(accepted, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
            Press("Cancel");
            // The build edit is refused while Running or Paused and on a locked source: no command, no Undo, no change.
            var values = new InstanceParameterCollection([new(new("capacity"), 900f), new(new("power_limit"), 90f),
                new(new("initial_charge"), .9f), new(new("enabled"), 1f)]);
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                Phase(scene, phase);
                Assert.Empty(Apply(scene, Current().Id, values));
                Assert.Equal(accepted, scene.World.Construction);
            }
            Phase(scene, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(Current() with { Locked = true }) };
            locked = Install(scene, locked);
            commands = client.EditCount;
            Assert.Empty(Apply(scene, Current().Id, values));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    // Story 9A.39: while Running or Paused the panel offers exactly what the package declares run-editable: one supply toggle for the
    // store's enable state (not the build editor). A click is a typed ConfigureElectrical command for the next physics step; it shows as
    // queued, a second click reverses it, and it never edits the construction or records an Undo. A locked source's toggle is disabled.
    [Fact]
    public void TheRunControlToggleSendsTypedControlsWithoutEditingAndAppearsOnlyForADeclaredRunControl()
    {
        var (scene, client) = BenchScene(new PartId("battery"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Choices(scene);
            Button Single(string label) => Assert.Single(Descendants<Button>(choices), button => button.Text == label);
            var owner = Assert.Single(scene.World.Construction.Instances).Id;
            var authored = scene.World.Construction;
            Assert.Contains(Descendants<Button>(choices), button => button.Text == "Configure");
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                client.Phase = phase;
                Phase(scene, phase);
                Refresh(scene);
                Assert.DoesNotContain(Descendants<Button>(choices), button => button.Text == "Configure");
                Single(phase == WorkshopSimulationPhase.Running ? "Disable supply" : "Disable supply (queued)").EmitSignal(Button.SignalName.Pressed);
                Assert.False(Single("Enable supply (queued)").Disabled);
                Single("Enable supply (queued)").EmitSignal(Button.SignalName.Pressed);
                Single("Disable supply (queued)");
                Assert.Equal("Supply change accepted for the next physics step.", Assert.Single(Descendants<Label>(scene), label => label.Name == "WorkshopStatus").Text);
                Assert.Equal(authored, scene.World.Construction);
            }
            Assert.Equal([new(owner, ElectricalEnable.Disabled), new(owner, ElectricalEnable.Enabled), new(owner, ElectricalEnable.Disabled),
                new(owner, ElectricalEnable.Enabled)], client.Controls);
            Assert.All(client.Commands, command => Assert.True(command.Kind != WorkshopCommandKind.ConfigureElectrical || command.Construction is null));
            // Reset retires the queued intent: the toggle again shows the committed (here authored) state.
            Action(scene, "↶ Reset");
            Phase(scene, WorkshopSimulationPhase.Running);
            Refresh(scene);
            Single("Disable supply");
            // A locked source keeps its toggle, disabled.
            Phase(scene, WorkshopSimulationPhase.Building);
            Install(scene, authored with { Instances = new(Assert.IsType<WorkshopGenericInstance>(Assert.Single(authored.Instances)) with { Locked = true }) });
            Phase(scene, WorkshopSimulationPhase.Running);
            Refresh(scene);
            Assert.True(Single("Disable supply").Disabled);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
        // Control: a part whose package declares no run-time control (the Bumper) offers no supply toggle while Running, only its
        // disabled build editor.
        var (bumper, _) = BenchScene(new PartId("bumper"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(bumper));
            Phase(bumper, WorkshopSimulationPhase.Running);
            Refresh(bumper);
            var buttons = Descendants<Button>(Choices(bumper)).ToArray();
            Assert.True(Assert.Single(buttons, button => button.Text == "Configure").Disabled);
            Assert.DoesNotContain(buttons, button => button.Text.Contains("supply", StringComparison.Ordinal));
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(bumper); }
    }

    // Story 9A.39 D3: every Choice parameter renders as an option list of its declared names, the Receiver's capture participation too;
    // picking an option applies its index as one Undo, and Cancel changes nothing.
    [Fact]
    public void AChoiceParameterIsPickedFromItsDeclaredOptions()
    {
        var (scene, _) = BenchScene(new PartId("basket"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Choices(scene);
            void Press(string label) => Assert.Single(Descendants<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            var key = new ParameterId("capture_participation");
            Assert.Equal(1f, Current().Parameters[key]);
            var undo = scene.World.Construction;
            Press("Configure");
            var option = Assert.Single(Descendants<OptionButton>(choices));
            Assert.Equal((key.Value, Enum.GetNames<SensorParticipation>().Length), (option.Name.ToString(), option.ItemCount));
            Assert.Equal(Enum.GetNames<SensorParticipation>(), Enumerable.Range(0, option.ItemCount).Select(option.GetItemText));
            Assert.DoesNotContain(Descendants<LineEdit>(choices), field => field.Name.ToString() == key.Value);
            option.Select(0);
            Press("Cancel");
            Assert.Equal(1f, Current().Parameters[key]);
            Press("Configure");
            Assert.Single(Descendants<OptionButton>(choices)).Select(0);
            Press("Apply parameters");
            Assert.Equal(0f, Current().Parameters[key]);
            AssertParameterUndo(scene, ref undo);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    // C-Bumper+Spring (9A.34): the Springboard placed through the drawer is a generic instance at its declared 400 N/m, 0.2 N·s/m and
    // zero preload, edited through the generic editor. Its parameters keep the Reject policy (CAT-062 fact 7): every value just outside
    // its range, and NaN, is refused atomically (no command, no Undo, no change); the endpoints are accepted, one Undo each.
    [Fact]
    public void GenericSpringboardEditorRefusesOutOfRangeValuesAtomicallyAndAcceptsEndpoints()
    {
        var (scene, client) = BenchScene(new PartId("spring"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Choices(scene);
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            ParameterId k = new("stiffness"), c = new("damping"), q = new("initial_compression");
            Assert.Equal((new PartId("spring"), 400f, .2f, 0f), (Current().PartId, Current().Parameters[k], Current().Parameters[c], Current().Parameters[q]));
            void Edit(ParameterId key, string text)
            {
                Press("Configure");
                Assert.Equal(3, Nodes<LineEdit>(choices).Count());
                Assert.Single(Nodes<LineEdit>(choices), field => new ParameterId(field.Name.ToString()) == key).Text = text;
                Press("Apply parameters");
            }
            Press("Configure");
            foreach (var label in new[] { "stiffness (120–1200 N/m)", "damping (0–8 N·s/m)", "initial_compression (0–0.2 m)" })
                Assert.Contains(Nodes<Label>(choices), row => row.Text == label);
            Press("Cancel");
            var undo = scene.World.Construction;
            foreach (var (key, text) in new[] { (k, "119"), (k, "1201"), (c, "-0.1"), (c, "8.1"), (q, "-0.01"), (q, "0.21"), (q, "NaN") })
            {
                var before = scene.World.Construction;
                var commands = client.EditCount;
                Edit(key, text);
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(commands, client.EditCount);
                Press("Cancel");
            }
            foreach (var (key, text, expected) in new[] { (k, "120", 120f), (k, "1200", 1200f), (c, "0", 0f), (c, "8", 8f), (q, "0.2", .2f), (q, "0", 0f) })
            {
                Edit(key, text);
                Assert.Equal(expected, Current().Parameters[key]);
                AssertParameterUndo(scene, ref undo);
            }
            var accepted = scene.World.Construction;
            var values = new InstanceParameterCollection([new(k, 500f), new(c, 1f), new(q, .1f)]);
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                Phase(scene, phase);
                Assert.Empty(Apply(scene, Current().Id, values));
                Assert.Equal(accepted, scene.World.Construction);
            }
            Phase(scene, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(Current() with { Locked = true }) };
            locked = Install(scene, locked);
            Assert.Empty(Apply(scene, Current().Id, values));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    // Spec 6.5 / 9R.8 decision: the distinct charged Spring Forward construction solves with its preload set through the generic editor.
    // The matched zero-preload board at the charged pose (Precise) is edited to 0.2 m by the Workshop's own handler, through the lesson's
    // placement configuration; the edited construction solves in the real simulation and the unedited one does not.
    [Fact]
    public async Task TheChargedSpringForwardRouteSolvesWithItsPreloadSetThroughTheGenericEditor()
    {
        var (scene, _) = BenchScene(new PartId("spring"), PlacementAnswer.Applied);
        try
        {
            var zero = LaunchLessonSolveTests.Bench(LaunchLessonSolveTests.Route.SpringZero);
            zero = Install(scene, zero);
            var board = (WorkshopGenericInstance)zero.Instances.Single(instance => instance.PartId == new PartId("spring"));
            var values = new InstanceParameterCollection([new(new("stiffness"), 400f), new(new("damping"), .2f), new(new("initial_compression"), .2f)]);
            Assert.Empty(Apply(scene, board.Id, values));
            var edited = scene.World.Construction;
            Assert.Equal(values, ((WorkshopGenericInstance)edited.Instances.Single(instance => instance.Id == board.Id)).Parameters);

            Assert.Equal(board with { Parameters = values }, edited.Instances.Single(instance => instance.Id == board.Id)); // only the preload
            Assert.Equal(zero.Instances.Where(instance => instance.Id != board.Id), edited.Instances.Where(instance => instance.Id != board.Id));
            // A fresh simulation admits its first construction at revision 2 (the Workshop advanced its own to 3); the content is as edited.
            var charged = edited with { Revision = zero.Revision };
            Assert.Empty(godot.Errors.Drain());
            var (_, run, _) = await WorkshopIntegrationHarness.ObserveReset(charged, 1200);
            Assert.True(LaunchLessonSolveTests.Solved(charged, run), "the editor-charged board solves Spring Forward");
            var (_, control, _) = await WorkshopIntegrationHarness.ObserveReset(zero, 1200);
            Assert.False(LaunchLessonSolveTests.Solved(zero, control), "the same board at zero preload does not");
        }
        finally { Release(scene); }
    }

    [Fact]
    public void GenericWallResizeUsesLocalAxesClampsCancelsAndRecordsOneUndoPerGesture()
    {
        var (scene, _) = BenchScene(new PartId("wall"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var part = Assert.Single(scene.World.Parts);
            var original = scene.World.Construction;
            var originalDimensions = part.Dimensions;
            var camera = Assert.Single(Descendants<Camera3D>(scene));
            var gizmo = Assert.Single(Descendants<RotationGizmo>(scene));
            gizmo.SetResizeMode();
            gizmo.Follow(part, true, camera);
            for (var axis = 0; axis < 3; axis++)
            {
                var handle = camera.UnprojectPosition(gizmo.HandlePosition(axis));
                Assert.Empty(Click(scene, handle));
                Assert.True(gizmo.Dragging);

                var direction = camera.UnprojectPosition(part.GlobalPosition + part.GlobalBasis[axis]) -
                    camera.UnprojectPosition(part.GlobalPosition);
                Assert.True(gizmo.Drag(camera, handle + direction * 100, true));
                var maximum = new Vector3(8, 6, 2);
                Assert.Equal(maximum[axis], part.Dimensions[axis]);
                Assert.Empty(EndGesture(scene, true));
                Assert.Equal(originalDimensions, part.Dimensions);
                Assert.Equal(original, scene.World.Construction);

            }
            var at = camera.UnprojectPosition(gizmo.HandlePosition(0));
            Assert.Empty(Click(scene, at));
            var projected = camera.UnprojectPosition(part.GlobalPosition + part.GlobalBasis.X) - camera.UnprojectPosition(part.GlobalPosition);
            Assert.True(gizmo.Drag(camera, at + projected * .5f, true));
            Assert.Empty(EndGesture(scene, false));
            Assert.Equal(4f, Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances)).Parameters[new("width")]);

            Assert.Empty(Undo(scene));
            Assert.Equal(original.Instances, scene.World.Construction.Instances);
            Assert.Equal(originalDimensions, Assert.Single(scene.World.Parts).Dimensions);
        }
        finally { Release(scene); }
    }


    [Fact]
    public void GenericRampResizeClampsBothHorizontalAxesWithoutEndingGestureAndSupportsCancelUndo()
    {
        var (scene, _) = BenchScene(new PartId("ramp"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var part = Assert.Single(scene.World.Parts);
            var original = scene.World.Construction;
            var dimensions = part.Dimensions;
            var camera = Assert.Single(Descendants<Camera3D>(scene));
            var gizmo = Assert.Single(Descendants<RotationGizmo>(scene));
            gizmo.SetResizeMode(); gizmo.Follow(part, true, camera);
            foreach (var axis in new[] { 0, 2 })
            {
                var handle = camera.UnprojectPosition(gizmo.HandlePosition(axis));
                Assert.Empty(Click(scene, handle));
                Assert.True(gizmo.Dragging);
                var direction = camera.UnprojectPosition(part.GlobalPosition + part.GlobalBasis[axis]) -
                    camera.UnprojectPosition(part.GlobalPosition);
                Assert.True(gizmo.Drag(camera, handle + direction * 100, true));
                Assert.Equal(4f, part.Dimensions[axis]);
                Assert.True(gizmo.Dragging);
                Assert.True(gizmo.Drag(camera, handle - direction * 100, true));
                Assert.Equal(.125f, part.Dimensions[axis]);
                Assert.True(gizmo.Dragging);

                Assert.Empty(EndGesture(scene, true));
                Assert.Equal(original, scene.World.Construction);
                Assert.Equal(dimensions, part.Dimensions);

            }
            var at = camera.UnprojectPosition(gizmo.HandlePosition(0));
            Assert.Empty(Click(scene, at));
            var projected = camera.UnprojectPosition(part.GlobalPosition + part.GlobalBasis.X) - camera.UnprojectPosition(part.GlobalPosition);
            Assert.True(gizmo.Drag(camera, at + projected * .5f, true));
            Assert.Empty(EndGesture(scene, false));
            Assert.Equal(4f, Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances)).Parameters[new("length")]);

            Assert.Empty(Undo(scene));
            Assert.Equal(original.Instances, scene.World.Construction.Instances);
            Assert.Equal(dimensions, Assert.Single(scene.World.Parts).Dimensions);
        }
        finally { Release(scene); }
    }

    [Fact]
    public void GenericRampEditorKeepsExactNonHalfLengthAndRefusesOutOfRangeSubmissionsAtomically()
    {
        var (scene, client) = BenchScene(new PartId("ramp"), PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var part = Assert.Single(scene.World.Parts);
            var choices = Choices(scene);
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            Dictionary<ParameterId, LineEdit> Fields() => Nodes<LineEdit>(choices).ToDictionary(field => new ParameterId(field.Name.ToString()));
            var before = scene.World.Construction;
            var dimensions = part.Dimensions;
            var commands = client.EditCount;
            var undos = scene.World.Construction;
            Press("Configure");
            Assert.Equal(new[] { new ParameterId("length"), new("width") }, Fields().Keys.Order());
            foreach (var text in new[] { "0.1", "4.5", "NaN" })
            {
                Fields()[new("length")].Text = text;
                Press("Apply parameters");
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(dimensions, part.Dimensions);
                Assert.Equal(commands, client.EditCount);
            }
            Press("Cancel");
            Press("Configure");
            Fields()[new("length")].Text = "1.234567";
            Press("Apply parameters");
            Assert.True(TestPrecision.NeedsMoreThanElevenSignificantBits(1.234567f));
            var current = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(1.234567f, current.Parameters[new("length")]);
            Assert.Equal(1.234567f, part.Dimensions.X);
            AssertParameterUndo(scene, ref undos);
            var accepted = scene.World.Construction;
            commands = client.EditCount;
            Assert.Empty(Apply(scene, current.Id,
                new InstanceParameterCollection([new(new("length"), 4.5f), new(new("width"), 1.3f)])));
            Assert.Equal(accepted, scene.World.Construction);
            Assert.Equal(commands, client.EditCount);
            Assert.Equal(1.234567f, part.Dimensions.X);
            // A typed OutOfRange edit rejection at the editor boundary, not a logged unexpected fault.
            Assert.Equal("Invalid declared parameter value.", Assert.Single(Descendants<Label>(scene), label => label.Name == "WorkshopStatus").Text);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    private enum PlacementAnswer { Pending, Applied, Rejected }

    /// <summary>Records each command and answers it as configured: never (pending), Applied with an empty body read (a static
    /// part has no dynamic bodies), or Rejected.</summary>
    private sealed class PlacementClient(PlacementAnswer answer) : IWorkshopClient
    {
        public PlacementAnswer Answer { get; set; } = answer;
        public Exception? NextFailure { get; set; }
        private TaskCompletionSource<WorkshopDelivery>? _held;
        private WorkshopDelivery _heldDelivery;
        private WorkshopConstruction? _construction;
        public void Complete()
        {
            var held = _held ?? throw new InvalidOperationException("No held delivery.");
            _held = null;
            held.SetResult(_heldDelivery);
        }
        public List<(WorkshopCommandKind Kind, WorkshopConstruction? Construction)> Commands { get; } = [];
        public int EditCount => Commands.Count(command => command.Kind == WorkshopCommandKind.Construct);
        /// <summary>Each run-time electrical control the scene sent, in order.</summary>
        public List<WorkshopElectricalControl> Controls { get; } = [];
        /// <summary>The phase every answer reports (a Building workshop unless a test runs one).</summary>
        public WorkshopSimulationPhase Phase { get; set; } = WorkshopSimulationPhase.Building;
        public WorkshopCadenceSettings Settings => WorkshopCadenceSettings.Default();
        public SimulationEpoch Epoch => new(1);
        public AuthorityRevision Revision => default;
        public WorkshopCommandIdentity? Pending => null;
        public WorkshopTransportState TransportState => WorkshopTransportState.Ready;
        public void ControlUi(WorkshopUiTarget target, AnimationControlKind kind, bool visible) { }
        public bool TryUiFrame(ulong frame, WorkshopPresentationSample physical, WorkshopUiTarget target, out AnimationOpacity opacity) { opacity = default; return false; }
        public void SetDeclaredVisible(GpuBodyId owner, bool visible) { }
        public bool TryDeclaredFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out System.Collections.Generic.IReadOnlyList<DeclaredPresentationSample> samples) { samples = System.Array.Empty<DeclaredPresentationSample>(); return false; }
        public bool TryRead(out WorkshopResponse response) { response = default; return false; }
        public bool TryPresent(ulong frame, out WorkshopPresentationSample sample) { sample = default; return false; }
        public void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene) { }
        public Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
            WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null, RopeSpanId? cut = null)
        {
            Phase = kind switch
            {
                WorkshopCommandKind.Run or WorkshopCommandKind.Resume => WorkshopSimulationPhase.Running,
                WorkshopCommandKind.Pause => WorkshopSimulationPhase.Paused,
                WorkshopCommandKind.Reset => WorkshopSimulationPhase.Building,
                _ => Phase
            };
            Commands.Add((kind, construction));
            if (electrical is { } control) Controls.Add(control);
            if (NextFailure is { } failure) { NextFailure = null; throw failure; }
            var result = Answer != PlacementAnswer.Rejected
                ? new WorkshopCommandResult(WorkshopCommandOutcome.Applied, WorkshopRejection.None)
                : new WorkshopCommandResult(WorkshopCommandOutcome.Rejected, WorkshopRejection.InvalidConstruction);
            if (construction is { } candidate && Answer != PlacementAnswer.Rejected) _construction = candidate;
            var bodies=_construction is { } authored ? PhysicsGpuAbi.ReadDynamicBodies(PhysicsGpuAbi.Admission(
                WorkshopPhysicsCompiler.Compile(authored,new(1,1)),Epoch,
                new(authored.Settings.Simulation,authored.Settings.Physical,new(1)))) : default;
            var response = new WorkshopResponse(new((ulong)Commands.Count), WorkshopResponseKind.Acknowledgement, result,
                Phase, new WorkshopRead(Epoch, new(0), bodies, Revision,
                    new(WorkshopClockDomain.SimulationMonotonic, new(1), new(120_000_000), new(100_000))),
                Session: new(1, 1), Cadence: new(1), MasterGeneration: new(1), Projection: new(1));
            var delivery = new WorkshopDelivery(response, true);
            if (Answer == PlacementAnswer.Pending)
            {
                _heldDelivery = delivery;
                _held = new();
                return _held.Task;
            }
            return Task.FromResult(delivery);
        }
        public Task<WorkshopDelivery> CancelPending() => throw new InvalidOperationException("No command is pending in this fixture.");
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
