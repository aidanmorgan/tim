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
    // C-Clock (9A.42, D3/D6): a clock placed from the palette takes the declared 1.0 s and is configured through the generic editor,
    // which shows the instance's own value (the record panel showed a part-side copy that nothing set) and applies the Clamp policy:
    // 13 applies as 12 and 0.05 as 0.1, each one Undo and one command; NaN, Infinity and text are refused with no change, no command
    // and no undo; Cancel changes nothing; an unchanged Apply adds no Undo. Undo restores 1.0. Running, Paused and a locked clock are
    // refused. A moved clock keeps its interval (CaptureInstance reads the committed instance); a new one starts from 1.0 s.
    [Fact]
    public void TheClockIntervalEditorShowsTheInstanceValueClampsIntoRangeAndAppliesAsOneUndo()
    {
        var parameter = new ParameterId("interval_seconds");
        var (scene, client) = BenchScene(PartId.Clock, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var placed = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(BitConverter.SingleToUInt32Bits(1f), BitConverter.SingleToUInt32Bits(placed.Parameters[parameter]));
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
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
            var commands = client.Commands.Count;
            var undos = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            foreach (var invalid in new[] { "NaN", "Infinity", "-Infinity", "two", "" })
            {
                input.Text = invalid;
                Press("Apply parameters");
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(commands, client.Commands.Count);
                Assert.Equal(undos, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
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
                Assert.Equal(++undos, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
                Assert.Equal(++commands, client.Commands.Count);
            }
            // The editor shows the instance's value, and an unchanged Apply adds no Undo and sends no command.
            input = Configure();
            Assert.Equal("2.5", input.Text);
            Press("Apply parameters");
            Assert.Equal((undos, commands), (Field<List<WorkshopConstruction>>(scene, "_undo").Count, client.Commands.Count));

            // A move re-captures the committed instance and keeps its interval; a newly placed clock starts from the declared 1.0 s.
            var applied = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            var moved = Assert.IsType<WorkshopGenericInstance>(scene.World.CaptureInstance(PartId.Clock, applied.Id, new Vector3(1, 3, 0), Quaternion.Identity));
            Assert.Equal(applied.Parameters, moved.Parameters);
            var fresh = Assert.IsType<WorkshopGenericInstance>(scene.World.CaptureInstance(PartId.Clock, new GpuBodyId(99), new Vector3(1, 3, 0), Quaternion.Identity));
            Assert.Equal(BitConverter.SingleToUInt32Bits(1f), BitConverter.SingleToUInt32Bits(fresh.Parameters[parameter]));

            foreach (var _ in new[] { 1, 2, 3 }) Assert.Empty(InvokeHandler(scene, "Undo"));
            var restored = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(BitConverter.SingleToUInt32Bits(1f), BitConverter.SingleToUInt32Bits(restored.Parameters[parameter]));

            var accepted = scene.World.Construction;
            commands = client.Commands.Count;
            var proposed = new InstanceParameterCollection([new(parameter, 4f)]);
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, phase);
                Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", restored.Id, proposed));
                Assert.Equal(accepted, scene.World.Construction);
                Assert.Equal(commands, client.Commands.Count);
            }
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(restored with { Locked = true }) };
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(scene.World, locked);
            Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", restored.Id, proposed));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.Commands.Count);
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
        var (scene, client) = BenchScene(PartId.PressurePlate, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var placed = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(BitConverter.SingleToUInt32Bits(.5f), BitConverter.SingleToUInt32Bits(placed.Parameters[parameter]));
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
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
            var commands = client.Commands.Count;
            var undos = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            foreach (var invalid in new[] { "0.09", "16.01", "NaN", "Infinity", "two" })
            {
                input.Text = invalid;
                Press("Apply parameters");
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(commands, client.Commands.Count);
                Assert.Equal(undos, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
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
                Assert.Equal(++undos, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            }
            Assert.Empty(InvokeHandler(scene, "Undo"));
            Assert.Empty(InvokeHandler(scene, "Undo"));
            var restored = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(BitConverter.SingleToUInt32Bits(.5f), BitConverter.SingleToUInt32Bits(restored.Parameters[parameter]));

            var accepted = scene.World.Construction;
            commands = client.Commands.Count;
            var proposed = new InstanceParameterCollection([new(parameter, 2f)]);
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, phase);
                Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", restored.Id, proposed));
                Assert.Equal(accepted, scene.World.Construction);
                Assert.Equal(commands, client.Commands.Count);
            }
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(restored with { Locked = true }) };
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(scene.World, locked);
            Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", restored.Id, proposed));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.Commands.Count);
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
        var (scene, client) = BenchScene(PartId.Counter, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var placed = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(3f, placed.Parameters[parameter]);
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
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
            var commands = client.Commands.Count;
            var undos = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            foreach (var invalid in new[] { "0", "10", "2.5", "-1", "NaN", "Infinity", "three" })
            {
                input.Text = invalid;
                Press("Apply parameters");
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(commands, client.Commands.Count);
                Assert.Equal(undos, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
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
                Assert.Equal(++undos, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
                Assert.Equal(++commands, client.Commands.Count);
            }
            // The editor shows the instance's value, and an unchanged Apply adds no Undo and sends no command.
            input = Configure();
            Assert.Equal("1", input.Text);
            Press("Apply parameters");
            Assert.Equal((undos, commands), (Field<List<WorkshopConstruction>>(scene, "_undo").Count, client.Commands.Count));

            // A move re-captures the committed instance and keeps its target; a newly placed counter starts from the declared 3.
            var applied = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            var moved = Assert.IsType<WorkshopGenericInstance>(scene.World.CaptureInstance(PartId.Counter, applied.Id, new Vector3(1, 3, 0), Quaternion.Identity));
            Assert.Equal(applied.Parameters, moved.Parameters);
            var fresh = Assert.IsType<WorkshopGenericInstance>(scene.World.CaptureInstance(PartId.Counter, new GpuBodyId(99), new Vector3(1, 3, 0), Quaternion.Identity));
            Assert.Equal(3f, fresh.Parameters[parameter]);

            Assert.Empty(InvokeHandler(scene, "Undo"));
            Assert.Empty(InvokeHandler(scene, "Undo"));
            var restored = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(3f, restored.Parameters[parameter]);

            var accepted = scene.World.Construction;
            commands = client.Commands.Count;
            var proposed = new InstanceParameterCollection([new(parameter, 5f)]);
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, phase);
                Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", restored.Id, proposed));
                Assert.Equal(accepted, scene.World.Construction);
                Assert.Equal(commands, client.Commands.Count);
            }
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(restored with { Locked = true }) };
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(scene.World, locked);
            Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", restored.Id, proposed));
            Assert.Equal(locked, scene.World.Construction);
            Assert.Equal(commands, client.Commands.Count);
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
        var (scene, client) = BenchScene(PartId.Basketball, PlacementAnswer.Applied);
        try
        {
            var bench = (Half)height;
            SetField(scene, "_placementHeight", new Metres(bench));
            var plate = PlacedParts.Plate(new(1), 0, (double)bench, 0, .5f);
            var before = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(plate));
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(scene.World, before);
            SetField(scene, "_nextId", new GpuBodyId(2));
            Assert.Empty(InvokeHandler(scene, "CreatePreview", PartId.Basketball));
            var preview = Field<MachinePart>(scene, "_preview");
            Assert.Empty(InvokeHandler(scene, "ShowPreviewAt", (Vector3?)new Vector3(0, (float)bench, 0)));
            var commands = client.Commands.Count;
            var undos = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            var shown = (preview.Visible, preview.Position);
            Assert.Empty(ClickBench(scene));
            if (!admitted)
            {
                Assert.False(shown.Visible);
                Assert.Equal("Placement rejected: resting there would lift the part above the bench.", Field<Label>(scene, "_status").Text);
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal((commands, undos), (client.Commands.Count, Field<List<WorkshopConstruction>>(scene, "_undo").Count));
            }
            else
            {
                Assert.True(shown.Visible);
                Assert.True(MathF.Abs(shown.Position.Y - ((float)bench + .08f + .34f)) < 1e-4f, $"the preview rests at {shown.Position.Y}");
                var ball = Assert.Single(scene.World.Construction.Instances, instance => instance.PartId == PartId.Basketball);
                var placed = new Vector3((ball.Cell.X + ball.Local.X) / 16f, (ball.Cell.Y + ball.Local.Y) / 16f, (ball.Cell.Z + ball.Local.Z) / 16f);
                Assert.True(placed.DistanceTo(shown.Position) < 1e-4f, $"the click placed {placed}, the preview showed {shown.Position}");
                Assert.Equal(undos + 1, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            }
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
                PlacedParts.Lamp(new(3),-3,2,0,0,0,0,1)));
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
    [InlineData(ElectricalNandTests.FeedbackLoop.ThroughOr)]
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
            if(ids is null)
            {
                Assert.Contains(closing,scene.World.Construction.Connections);
                Assert.Equal(open.Connections.Count+1,scene.World.Construction.Connections.Count);
                Assert.Equal(undos+1,Field<List<WorkshopConstruction>>(scene,"_undo").Count);
                Assert.Empty(InvokeHandler(scene,"Undo"));
                Assert.Equal(open.Connections,scene.World.Construction.Connections);
            }
            else
            {
                Assert.Equal(open,scene.World.Construction);
                Assert.Equal(commands,client.Commands.Count);
                Assert.Equal(undos,Field<List<WorkshopConstruction>>(scene,"_undo").Count);
                var status=Field<Label>(scene,"_status").Text;
                Assert.Contains("parts "+ids+".",status,StringComparison.Ordinal);
                Assert.StartsWith("Break the wire loop",status,StringComparison.Ordinal);
            }
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
            if(ids is null)
            {
                Assert.Contains(closing,scene.World.Construction.Connections);
                Assert.Equal(open.Connections.Count+1,scene.World.Construction.Connections.Count);
                Assert.Equal(undos+1,Field<List<WorkshopConstruction>>(scene,"_undo").Count);
                Assert.Empty(InvokeHandler(scene,"Undo"));
                Assert.Equal(open.Connections,scene.World.Construction.Connections);
            }
            else
            {
                Assert.Equal(open,scene.World.Construction);
                Assert.Equal(commands,client.Commands.Count);
                Assert.Equal(undos,Field<List<WorkshopConstruction>>(scene,"_undo").Count);
                var status=Field<Label>(scene,"_status").Text;
                Assert.Contains("parts "+ids+".",status,StringComparison.Ordinal);
                Assert.StartsWith("Break the wire loop",status,StringComparison.Ordinal);
            }
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
        // A bench point outside the world is a caller defect: the canonical position boundary throws a plain argument exception,
        // not a declared refusal, so Propose must not turn it into a placement rejection (the handler boundary logs it as a fault).
        // C-Memory: the latch is a generic instance of a discovered part, so the defect is proposed on a discovered world.
        var discovered = DiscoveredWorld();
        try
        {
            var undo = 0;
            var defect = Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopBenchPlacement.Propose(discovered, PartId.Latch, new(7),
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
            var refused = WorkshopBenchPlacement.Propose(world, PartId.Basketball, new(7), BenchPoint, Quaternion.Identity, null, () => undo++);
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
            int Undos() => Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            var undos = Undos();
            Assert.Empty(ClickBench(scene));
            Assert.Equal(undos + 1, Undos());
            Assert.Equal((kind, 0), (Current().PartId, Current().Parameters.Count));
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            Assert.Empty(InvokeHandler(scene, "Select", Assert.Single(scene.World.Parts)));
            Assert.DoesNotContain(Nodes<Button>(choices), button => button.Text == "Configure");
            var placed = scene.World.Construction;
            var commands = client.Commands.Count;
            Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", Current().Id,
                new InstanceParameterCollection([new(new ParameterId("radius"), .5f)])));
            Assert.Equal((placed, commands, undos + 1), (scene.World.Construction, client.Commands.Count, Undos()));

            var camera = Field<Camera3D>(scene, "_camera");
            var gizmo = Field<RotationGizmo>(scene, "_rotationGizmo");
            foreach (var (move, axis) in new[] { (false, 2), (true, 0) })
            {
                var part = Assert.Single(scene.World.Parts);
                Assert.Empty(InvokeHandler(scene, "Select", part));
                gizmo.SetMoveMode(move);
                gizmo.Follow(part, true, camera);
                var before = scene.World.Construction;
                undos = Undos();
                var handle = camera.UnprojectPosition(gizmo.HandlePosition(axis));
                Assert.Empty(InvokeHandler(scene, "Click", handle));
                Assert.True(gizmo.Dragging, move ? "move" : "rotate");
                var along = camera.UnprojectPosition(part.GlobalPosition + Vector3.Right) - camera.UnprojectPosition(part.GlobalPosition);
                Assert.True(gizmo.Drag(camera, handle + (move ? along * .5f : new Vector2(90, 90)), move));
                Assert.Empty(InvokeHandler(scene, "EndGizmo", false));
                Assert.Equal(undos + 1, Undos());
                Assert.NotEqual(before.Instances, scene.World.Construction.Instances);
                Assert.Equal((kind, 0, before.Instances[0].Id), (Current().PartId, Current().Parameters.Count, Current().Id));
                Assert.Empty(InvokeHandler(scene, "Undo"));
                Assert.Equal((before.Instances, undos), (scene.World.Construction.Instances, Undos()));
            }

            var accepted = scene.World.Construction;
            commands = client.Commands.Count;
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, phase);
                Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", Current().Id, InstanceParameterCollection.Empty));
                Assert.Equal(accepted, scene.World.Construction);
            }
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Building);
            var locked = accepted with { Instances = new(Current() with { Locked = true }) };
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(scene.World, locked);
            Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", Current().Id, InstanceParameterCollection.Empty));
            Assert.Equal((locked, commands), (scene.World.Construction, client.Commands.Count));
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    // The detector placed beside a free pipe mouth snaps onto it (CAT-002 / CAT-048 typed mouths): its start mouth joins the pipe's
    // end mouth 1.89 m from the pipe's centre, so the 0.28 m collar centres 0.05 m short of the clicked point, as one placement with
    // one Undo.
    [Fact]
    public async Task ADetectorPlacedBesideAPipeMouthSnapsOntoItAsOneUndo()
    {
        var (scene, _) = BenchScene(PartId.BallDetector, PlacementAnswer.Applied);
        try
        {
            var height = Mathf.Snapped((float)Field<Metres>(scene, "_placementHeight").Value, .1f);
            var pipe = WorkshopInput.Pipe(new(1), -2.22, height, 0, 0, 0, 0, 1);
            await scene.World.ReplaceConstruction(new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(pipe)));
            SetField<PartId?>(scene, "_tool", PartId.BallDetector);
            SetField(scene, "_nextId", new GpuBodyId(2));
            var undos = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            Assert.Empty(ClickBench(scene));
            Assert.Equal(undos + 1, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            var detector = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances, instance => instance.PartId == PartId.BallDetector));
            var x = (detector.Cell.X + (double)detector.Local.X) / 16;
            var y = (detector.Cell.Y + (double)detector.Local.Y) / 16;
            Assert.True(Math.Abs(x + .05) < .001 && Math.Abs(y - height) < .001, $"snapped to ({x}, {y})");
            Assert.Empty(detector.Parameters);
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
        var (scene, client) = BenchScene(PartId.PinballBumper, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            var key = new ParameterId("strength");
            Assert.Equal((PartId.PinballBumper, 8f), (Current().PartId, Current().Parameters[key]));
            var initial = scene.World.Construction;
            var undo = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
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
                Assert.Equal(++undo, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
                // The store the shared compiler builds is the edited strength's (½·1 kg)·s·s.
                Assert.Equal(.5f * expected * expected,
                    Assert.Single(WorkshopPhysicsCompiler.Compile(scene.World.Construction, new(1, 2)).ContactWorks.ToArray()).InitialEnergy.Value);
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
            var values = new InstanceParameterCollection([new(key, 4f)]);
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

    // Story 10.2a (D4): the Weight placed through the drawer is a generic instance at its declared 4 kg, and the generic editor names
    // mass in kg with its declared range. A finite out-of-range entry clamps into 0.25–8 kg (the Clamp policy) and applies as one Undo,
    // and the compiled body takes the mass; NaN, ±Infinity and text are refused with no change, no command and no Undo. Behind the
    // field, the declared-parameter handler refuses an out-of-range, non-finite, missing or unknown mass atomically: nothing is clamped,
    // defaulted or migrated there.
    [Fact]
    public void TheWeightMassEditorClampsFiniteEntriesAndEverySeamBehindItRefusesAnInvalidMass()
    {
        var key = new ParameterId("mass");
        var (scene, client) = BenchScene(WeightTests.Weight, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            int Undos() => Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            Assert.Equal((WeightTests.Weight, 4f), (Current().PartId, Current().Parameters[key]));
            var undo = Undos();
            Assert.Empty(InvokeHandler(scene, "Select", Assert.Single(scene.World.Parts)));
            Press("Configure");
            Assert.Contains(Nodes<Label>(choices), label => label.Text == "mass (0.25–8 kg)");
            var field = Assert.Single(Nodes<LineEdit>(choices));
            Assert.Equal((key.Value, "4"), (field.Name.ToString(), field.Text));
            Press("Cancel");
            Assert.Equal(undo, Undos());
            foreach (var (text, expected) in new[] { ("0.249", .25f), ("8.01", 8f), ("0", .25f), ("6.5", 6.5f), ("-1", .25f), ("8", 8f), ("0.25", .25f) })
            {
                Press("Configure");
                Assert.Single(Nodes<LineEdit>(choices)).Text = text;
                Press("Apply parameters");
                Assert.Equal(BitConverter.SingleToUInt32Bits(expected), BitConverter.SingleToUInt32Bits(Current().Parameters[key]));
                Assert.Equal(++undo, Undos());
                Assert.Equal(new Kilograms((Half)expected), Assert.Single(WorkshopPhysicsCompiler.Compile(scene.World.Construction, new(1, 2))
                    .Bodies.ToArray(), body => body.Id == Current().Id).Mass);
            }
            var accepted = scene.World.Construction;
            var commands = client.Commands.Count;
            foreach (var text in new[] { "NaN", "Infinity", "-Infinity", "two" })
            {
                Press("Configure");
                Assert.Single(Nodes<LineEdit>(choices)).Text = text;
                Press("Apply parameters");
                Assert.Equal((accepted, commands, undo), (scene.World.Construction, client.Commands.Count, Undos()));
                Press("Cancel");
            }
            var unknown = new ParameterId("radius");
            foreach (var values in new InstanceParameterCollection[]
            {
                new([new(key, .249f)]), new([new(key, 8.01f)]), new([new(key, 0f)]), new([new(key, -1f)]), new([new(key, float.NaN)]),
                new([new(key, float.PositiveInfinity)]), new([new(key, float.NegativeInfinity)]), new([]), new([new(unknown, 4f)]),
                new([new(key, 4f), new(unknown, .3f)])
            })
            {
                Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", Current().Id, values));
                Assert.Equal((accepted, commands, undo), (scene.World.Construction, client.Commands.Count, Undos()));
            }
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    private static IEnumerable<T> Descendants<T>(Node parent) where T : Node =>
        parent.GetChildren().SelectMany(child => new[] { child }.Concat(Descendants<Node>(child))).OfType<T>();
    private static void Refresh(Workshop scene) => typeof(Workshop).GetMethod("RefreshConnectionChoices", Private)!.Invoke(scene, null);

    // Story 9A.39: the Battery placed through the drawer is a generic instance at its package defaults (3600 J, 120 W, full, Enabled;
    // owner decision 10 Oct 2026) and the generic editor replaces its panel. Capacity, power limit and initial charge are Clamp fields,
    // as the former SpinBoxes were; NaN refuses atomically; the enable state is an option list of its declared names; Cancel changes
    // nothing and one Apply records one Undo. The compiled store is the edited values.
    [Fact]
    public void GenericBatteryEditorClampsItsStoreAndPicksTheEnableStateFromItsOptions()
    {
        var (scene, client) = BenchScene(PartId.Battery, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
            void Press(string label) => Assert.Single(Descendants<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Dictionary<string, LineEdit> Fields() => Descendants<LineEdit>(choices).ToDictionary(field => field.Name.ToString());
            OptionButton Enable() => Assert.Single(Descendants<OptionButton>(choices));
            float Value(string key) => Current().Parameters[new(key)];
            Assert.Equal((PartId.Battery, 3600f, 120f, 1f, 1f), (Current().PartId, Value("capacity"), Value("power_limit"), Value("initial_charge"), Value("enabled")));
            var initial = scene.World.Construction;
            var undo = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
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
                Assert.Equal(++undo, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
                var source = Assert.Single(WorkshopPhysicsCompiler.Compile(scene.World.Construction, new(1, 2)).Electrical.Sources.ToArray());
                Assert.Equal((expected.Item1, expected.Item2, expected.Item3, expected.Item4 == 1f ? ElectricalEnable.Enabled : ElectricalEnable.Disabled),
                    (source.Capacity.Value, source.MaximumPower.Value, source.InitialFraction, source.Enabled));
            }
            var accepted = scene.World.Construction;
            var commands = client.Commands.Count;
            Press("Configure");
            Fields()["capacity"].Text = "NaN";
            Press("Apply parameters");
            Assert.Equal(accepted, scene.World.Construction);
            Assert.Equal(commands, client.Commands.Count);
            Assert.Equal(undo, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            Press("Cancel");
            // The build edit is refused while Running or Paused and on a locked source: no command, no Undo, no change.
            var values = new InstanceParameterCollection([new(new("capacity"), 900f), new(new("power_limit"), 90f),
                new(new("initial_charge"), .9f), new(new("enabled"), 1f)]);
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
            Assert.Equal(undo, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
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
        var (scene, client) = BenchScene(PartId.Battery, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
            Button Single(string label) => Assert.Single(Descendants<Button>(choices), button => button.Text == label);
            var owner = Assert.Single(scene.World.Construction.Instances).Id;
            var authored = scene.World.Construction;
            var undo = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            Assert.Contains(Descendants<Button>(choices), button => button.Text == "Configure");
            foreach (var phase in new[] { WorkshopSimulationPhase.Running, WorkshopSimulationPhase.Paused })
            {
                client.Phase = phase;
                typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, phase);
                Refresh(scene);
                Assert.DoesNotContain(Descendants<Button>(choices), button => button.Text == "Configure");
                Single(phase == WorkshopSimulationPhase.Running ? "Disable supply" : "Disable supply (queued)").EmitSignal(Button.SignalName.Pressed);
                Assert.False(Single("Enable supply (queued)").Disabled);
                Single("Enable supply (queued)").EmitSignal(Button.SignalName.Pressed);
                Single("Disable supply (queued)");
                Assert.Equal("Supply change accepted for the next physics step.", Field<Label>(scene, "_status").Text);
                Assert.Equal(authored, scene.World.Construction);
                Assert.Equal(undo, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            }
            Assert.Equal([new(owner, ElectricalEnable.Disabled), new(owner, ElectricalEnable.Enabled), new(owner, ElectricalEnable.Disabled),
                new(owner, ElectricalEnable.Enabled)], client.Controls);
            Assert.All(client.Commands, command => Assert.True(command.Kind != WorkshopCommandKind.ConfigureElectrical || command.Construction is null));
            // Reset retires the queued intent: the toggle again shows the committed (here authored) state.
            typeof(Workshop).GetMethod("ResetUiAnimations", Private)!.Invoke(scene, null);
            Refresh(scene);
            Single("Disable supply");
            // A locked source keeps its toggle, disabled.
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(scene.World,
                authored with { Instances = new(Assert.IsType<WorkshopGenericInstance>(Assert.Single(authored.Instances)) with { Locked = true }) });
            Refresh(scene);
            Assert.True(Single("Disable supply").Disabled);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
        // Control: a part whose package declares no run-time control (the Bumper) offers no supply toggle while Running, only its
        // disabled build editor.
        var (bumper, _) = BenchScene(PartId.PinballBumper, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(bumper));
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(bumper.World, WorkshopSimulationPhase.Running);
            Refresh(bumper);
            var buttons = Descendants<Button>(Field<VBoxContainer>(bumper, "_connectionChoices")).ToArray();
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
        var (scene, _) = BenchScene(PartId.Receiver, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
            void Press(string label) => Assert.Single(Descendants<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            var key = new ParameterId("capture_participation");
            Assert.Equal(1f, Current().Parameters[key]);
            var undo = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
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
            Assert.Equal(undo + 1, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
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
        var (scene, client) = BenchScene(PartId.Springboard, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            WorkshopGenericInstance Current() => Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            ParameterId k = new("stiffness"), c = new("damping"), q = new("initial_compression");
            Assert.Equal((PartId.Springboard, 400f, .2f, 0f), (Current().PartId, Current().Parameters[k], Current().Parameters[c], Current().Parameters[q]));
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
            var undo = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            foreach (var (key, text) in new[] { (k, "119"), (k, "1201"), (c, "-0.1"), (c, "8.1"), (q, "-0.01"), (q, "0.21"), (q, "NaN") })
            {
                var before = scene.World.Construction;
                var commands = client.Commands.Count;
                Edit(key, text);
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(commands, client.Commands.Count);
                Assert.Equal(undo, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
                Press("Cancel");
            }
            foreach (var (key, text, expected) in new[] { (k, "120", 120f), (k, "1200", 1200f), (c, "0", 0f), (c, "8", 8f), (q, "0.2", .2f), (q, "0", 0f) })
            {
                Edit(key, text);
                Assert.Equal(expected, Current().Parameters[key]);
                Assert.Equal(++undo, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            }
            var accepted = scene.World.Construction;
            var values = new InstanceParameterCollection([new(k, 500f), new(c, 1f), new(q, .1f)]);
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
        var (scene, _) = BenchScene(PartId.Springboard, PlacementAnswer.Applied);
        try
        {
            var zero = LaunchLessonSolveTests.Bench(LaunchLessonSolveTests.Route.SpringZero);
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(scene.World, zero);
            var board = (WorkshopGenericInstance)zero.Instances.Single(instance => instance.PartId == PartId.Springboard);
            var undo = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            var values = new InstanceParameterCollection([new(new("stiffness"), 400f), new(new("damping"), .2f), new(new("initial_compression"), .2f)]);
            Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", board.Id, values));
            var edited = scene.World.Construction;
            Assert.Equal(values, ((WorkshopGenericInstance)edited.Instances.Single(instance => instance.Id == board.Id)).Parameters);
            Assert.Equal(undo + 1, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            Assert.Equal(board with { Parameters = values }, edited.Instances.Single(instance => instance.Id == board.Id)); // only the preload
            Assert.Equal(zero.Instances.Where(instance => instance.Id != board.Id), edited.Instances.Where(instance => instance.Id != board.Id));
            // A fresh simulation admits its first construction at revision 2 (the Workshop advanced its own to 3); the content is as edited.
            var charged = edited with { Revision = zero.Revision };
            Assert.Empty(godot.Errors.Drain());
            var (_, run, _) = await NoFixedCapacityTests.RunAndReset(charged, 1200);
            Assert.True(LaunchLessonSolveTests.Solved(charged, run), "the editor-charged board solves Spring Forward");
            var (_, control, _) = await NoFixedCapacityTests.RunAndReset(zero, 1200);
            Assert.False(LaunchLessonSolveTests.Solved(zero, control), "the same board at zero preload does not");
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

    [Fact]
    public void GenericRampEditorKeepsExactNonHalfLengthAndRefusesOutOfRangeSubmissionsAtomically()
    {
        var (scene, client) = BenchScene(PartId.Ramp, PlacementAnswer.Applied);
        try
        {
            Assert.Empty(ClickBench(scene));
            var part = Assert.Single(scene.World.Parts);
            var choices = Field<VBoxContainer>(scene, "_connectionChoices");
            static IEnumerable<T> Nodes<T>(Node parent) where T : Node =>
                parent.GetChildren().SelectMany(child => new[] { child }.Concat(Nodes<Node>(child))).OfType<T>();
            void Press(string label) => Assert.Single(Nodes<Button>(choices), button => button.Text == label).EmitSignal(Button.SignalName.Pressed);
            Dictionary<ParameterId, LineEdit> Fields() => Nodes<LineEdit>(choices).ToDictionary(field => new ParameterId(field.Name.ToString()));
            var before = scene.World.Construction;
            var dimensions = part.Dimensions;
            var commands = client.Commands.Count;
            var undos = Field<List<WorkshopConstruction>>(scene, "_undo").Count;
            Press("Configure");
            Assert.Equal(new[] { new ParameterId("length"), new("width") }, Fields().Keys.Order());
            foreach (var text in new[] { "0.1", "4.5", "NaN" })
            {
                Fields()[new("length")].Text = text;
                Press("Apply parameters");
                Assert.Equal(before, scene.World.Construction);
                Assert.Equal(dimensions, part.Dimensions);
                Assert.Equal(commands, client.Commands.Count);
                Assert.Equal(undos, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            }
            Press("Cancel");
            Press("Configure");
            Fields()[new("length")].Text = "1.234567";
            Press("Apply parameters");
            Assert.NotEqual(1.234567f, (float)(Half)1.234567f);
            var current = Assert.IsType<WorkshopGenericInstance>(Assert.Single(scene.World.Construction.Instances));
            Assert.Equal(1.234567f, current.Parameters[new("length")]);
            Assert.Equal(1.234567f, part.Dimensions.X);
            Assert.Equal(undos + 1, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            var accepted = scene.World.Construction;
            commands = client.Commands.Count;
            Assert.Empty(InvokeHandler(scene, "ApplyDeclaredParameters", current.Id,
                new InstanceParameterCollection([new(new("length"), 4.5f), new(new("width"), 1.3f)])));
            Assert.Equal(accepted, scene.World.Construction);
            Assert.Equal(commands, client.Commands.Count);
            Assert.Equal(undos + 1, Field<List<WorkshopConstruction>>(scene, "_undo").Count);
            Assert.Equal(1.234567f, part.Dimensions.X);
            // A typed OutOfRange edit rejection at the editor boundary, not a logged unexpected fault.
            Assert.Equal("Invalid declared parameter value.", Field<Label>(scene, "_status").Text);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { Release(scene); }
    }

    private enum PlacementAnswer { Pending, Applied, Rejected }

    /// <summary>Records each command and answers it as configured: never (pending), Applied with an empty body read (a static
    /// part has no dynamic bodies), or Rejected.</summary>
    private sealed class PlacementClient(PlacementAnswer answer) : IWorkshopClient
    {
        public List<(WorkshopCommandKind Kind, WorkshopConstruction? Construction)> Commands { get; } = [];
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
            if (electrical is { } control) Controls.Add(control);
            if (answer == PlacementAnswer.Pending) return new TaskCompletionSource<WorkshopDelivery>().Task;
            var result = answer == PlacementAnswer.Applied
                ? new WorkshopCommandResult(WorkshopCommandOutcome.Applied, WorkshopRejection.None)
                : new WorkshopCommandResult(WorkshopCommandOutcome.Rejected, WorkshopRejection.InvalidConstruction);
            var bodies=construction is { } authored ? PhysicsGpuAbi.ReadDynamicBodies(PhysicsGpuAbi.Admission(
                WorkshopPhysicsCompiler.Compile(authored,new(1,1)),Epoch,
                new(authored.Settings.Simulation,authored.Settings.Physical,new(1)))) : default;
            var response = new WorkshopResponse(new((ulong)Commands.Count), WorkshopResponseKind.Acknowledgement, result,
                Phase, new WorkshopRead(Epoch, new(0), bodies, Revision,
                    new(WorkshopClockDomain.SimulationMonotonic, new(1), new(120_000_000), new(100_000))),
                Session: new(1, 1), Cadence: new(1), MasterGeneration: new(1), Projection: new(1));
            return Task.FromResult(new WorkshopDelivery(response, true));
        }
        public Task<WorkshopDelivery> CancelPending() => throw new InvalidOperationException("No command is pending in this fixture.");
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
