using Godot;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public sealed class WorkshopHintTests(NativeSceneFixture godot)
{
    // This native fixture supplies transport results, never a local animation clock/evaluator.
    // Actual A execution and clock qualification are covered by the owned Chrome evidence.
    private HintReadClient _client = null!;
    private void Publish(Half opacity) => _client.Next = opacity;
    private static void Solve(Label solved) { var colour = solved.Modulate; colour.A = 1; solved.Modulate = colour; }
    private static readonly double QuarterDuration = (double)(Half).16 / 4;

    private Workshop Scene()
    {
        var scene = GD.Load<PackedScene>("res://scenes/workshop.tscn").Instantiate<Workshop>();
        godot.Tree.Root.AddChild(scene);
        scene.SetProcess(false);
        scene.SetPhysicsProcess(false);
        _client = new HintReadClient();
        // Native-only boundary injection; no production client setter or clock fallback.
        typeof(MachineWorld).GetField("_workshopClient", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic)!.SetValue(scene.World, _client);
        return scene;
    }
    private void Release(Workshop scene)
    {
        scene.Free();
        godot.Engine.Iteration();
    }
    private static T Control<T>(Workshop scene, WorkshopUiControl role) where T : Control =>
        Assert.IsAssignableFrom<T>(scene.FindChild(WorkshopUiControlBoundary.NodeName(role), true, false));
    private static void Press(Workshop scene, WorkshopUiControl role) =>
        Control<Button>(scene, role).EmitSignal(Button.SignalName.Pressed);
    private static Label Reveal(Workshop scene)
    {
        Press(scene, WorkshopUiControl.Goal);
        Press(scene, WorkshopUiControl.ShowHint);
        return Control<Label>(scene, WorkshopUiControl.Hint);
    }

    [Fact]
    public void ActualHintAppliesWorkerHalfSamplesAndRetiresConsumedValues()
    {
        var scene = Scene();
        try
        {
            var before = scene.World.WorkshopRead;
            var hint = Control<Label>(scene, WorkshopUiControl.Hint);
            var baseline = hint.Modulate;
            Reveal(scene);
            Assert.True(hint.IsVisibleInTree());
            Assert.Equal(0, hint.Modulate.A);
            Publish((Half).103515625);
            scene._Process(QuarterDuration);
            Assert.Equal(.103515625f, hint.Modulate.A);
            Publish((Half).896484375);
            scene._Process(QuarterDuration * 2);
            Assert.Equal(.896484375f, hint.Modulate.A);
            Publish((Half)1);
            scene._Process(QuarterDuration);
            Assert.Equal(baseline, hint.Modulate);
            // Inactive frames must not keep owning/writing opacity.
            var marker = hint.Modulate;
            marker.A = .375f;
            hint.Modulate = marker;
            scene._Process(1);
            Assert.Equal(marker, hint.Modulate);
            Assert.Equal(before, scene.World.WorkshopRead);
            Assert.Equal(0, scene.World.Ticks);
        }
        finally { Release(scene); }
    }

    [Fact]
    public void HiddenParentReportsVisibilityAndUsesNewSampleOnReentry()
    {
        var scene = Scene();
        try
        {
            var hint = Reveal(scene);
            Publish((Half).103515625);
            scene._Process(QuarterDuration);
            var quarter = hint.Modulate;
            Press(scene, WorkshopUiControl.Menu);
            Assert.False(hint.IsVisibleInTree());
            scene._Process(QuarterDuration);
            Assert.Equal(quarter, hint.Modulate);
            scene._Process(1);
            scene._Process(1);
            Assert.Equal(quarter, hint.Modulate);
            Press(scene, WorkshopUiControl.Goal);
            Assert.True(hint.IsVisibleInTree());
            Publish((Half)1);
            scene._Process(0);
            Assert.Equal(1, hint.Modulate.A);
            Assert.Equal(quarter.R, hint.Modulate.R);
            Assert.Equal(quarter.G, hint.Modulate.G);
            Assert.Equal(quarter.B, hint.Modulate.B);
            var marker = hint.Modulate;
            marker.A = .375f;
            hint.Modulate = marker;
            scene._Process(0);
            Assert.Equal(marker, hint.Modulate);
        }
        finally { Release(scene); }
    }

    [Fact]
    public void HideRestoresBaselineAndUnadmittedModeSelectionPreservesReveal()
    {
        var scene = Scene();
        try
        {
            var hint = Reveal(scene);
            scene._Process(QuarterDuration);
            Press(scene, WorkshopUiControl.ShowHint);
            Assert.False(hint.Visible);
            Assert.Equal(1, hint.Modulate.A);
            scene._Process(QuarterDuration);
            Assert.Equal(1, hint.Modulate.A);
            Press(scene, WorkshopUiControl.ShowHint);
            Publish((Half).103515625f);
            scene._Process(QuarterDuration);
            Assert.Equal(.103515625f, hint.Modulate.A);
            var picker = Control<OptionButton>(scene, WorkshopUiControl.LevelPicker);
            var construction = scene.World.Construction;
            var selected = picker.Selected;
            // This fixture owns hint samples only, with no admitted gameplay session.
            // A mode change now requires an atomic canonical construction command.
            picker.EmitSignal(OptionButton.SignalName.ItemSelected, 0);
            Assert.Equal(selected, picker.Selected);
            Assert.Equal(construction, scene.World.Construction);
            Assert.True(hint.IsVisibleInTree());
            Assert.Equal(.103515625f, hint.Modulate.A);
            scene._Process(QuarterDuration);
            Assert.Equal(.103515625f, hint.Modulate.A);
            Publish((Half).5);
            scene._Process(QuarterDuration * 2);
            Assert.Equal(.5f, hint.Modulate.A);
        }
        finally { Release(scene); }
    }

    [Fact]
    public void RemovalRestoresBaselineAndCosmeticReentryCannotReviveGpuSession()
    {
        var scene = Scene();
        try
        {
            var hint = Reveal(scene);
            Publish((Half).103515625);
            scene._Process(QuarterDuration);
            godot.Tree.Root.RemoveChild(scene);
            Assert.Equal(1, hint.Modulate.A);
            Assert.Equal(WorkshopSimulationPhase.Disposed, scene.World.WorkshopPhase);
            godot.Tree.Root.AddChild(scene);
            scene._Process(QuarterDuration);
            Assert.Equal(1, hint.Modulate.A);
            Press(scene, WorkshopUiControl.ShowHint);
            Assert.True(hint.Visible);
            Assert.Equal(0, hint.Modulate.A);
            Publish((Half).103515625);
            scene._Process(QuarterDuration);
            Assert.Equal(0, hint.Modulate.A);
            Assert.True(_client.Disposed);
            Press(scene, WorkshopUiControl.ShowHint);
            Assert.Equal(1, hint.Modulate.A);
            Assert.Equal(WorkshopSimulationPhase.Disposed, scene.World.WorkshopPhase);
            Assert.False(scene.World.Running);
        }
        finally { Release(scene); }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1)]
    public void InvalidFrameDeltaDoesNotConsumeCurrentReveal(double delta)
    {
        var scene = Scene();
        try
        {
            var hint = Reveal(scene);
            var before = hint.Modulate;
            Publish((Half).103515625);
            Assert.Throws<ArgumentOutOfRangeException>(() => scene._Process(delta));
            Assert.Equal(before, hint.Modulate);
            Assert.NotNull(_client.Next);
            scene._Process(QuarterDuration);
            Assert.Equal(.103515625f, hint.Modulate.A);
        }
        finally { Release(scene); }
    }

    [Fact]
    public void UiBindingWritesOnlyOpacityAndRestsOnDeclaredNeutral()
    {
        var label = new Label { Modulate = new Color(.2f, .4f, .6f, .9f) };
        try
        {
            Assert.Throws<ArgumentException>(() => new WorkshopUiBinding(WorkshopUiTarget.None, label));
            var goal = new WorkshopUiBinding(WorkshopUiTarget.Goal, label);
            Assert.Equal(WorkshopUiTarget.Goal, goal.Target);
            goal.ApplyNeutral();
            Assert.Equal(new Color(.2f, .4f, .6f, 0), label.Modulate);
            goal.Apply(new((Half).5));
            Assert.Equal(new Color(.2f, .4f, .6f, .5f), label.Modulate);
            new WorkshopUiBinding(WorkshopUiTarget.Hint, label).ApplyNeutral();
            Assert.Equal(new Color(.2f, .4f, .6f, 1), label.Modulate);
            Assert.Equal(new AnimationTargetId(1), UiCurves.Hint.AnimationTarget);
            Assert.Equal(new AnimationTargetId(ulong.MaxValue), UiCurves.Goal.AnimationTarget);
            Assert.Throws<ArgumentException>(() => (UiCurves.Hint with { Duration = (Half)0 }).Validate());
            Assert.Throws<ArgumentException>(() => (UiCurves.Goal with { Neutral = (Half)1.5 }).Validate());
            Assert.Throws<ArgumentException>(() => (UiCurves.Goal with { Target = WorkshopUiTarget.None }).Validate());
        }
        finally { label.Free(); }
    }

    [Fact]
    public void CanonicalHintUnitsRejectUndefinedValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDurationSeconds(Half.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDurationSeconds((Half)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationOpacity(Half.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationOpacity((Half)(-.01)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationOpacity((Half)1.01));
    }
    [Fact]
    public void FiniteMaximumDeltaCannotInventAWorkerSample()
    {
        var scene=Scene();
        try
        {
            var hint=Reveal(scene);
            scene._Process(double.MaxValue);
            Assert.Equal(0,hint.Modulate.A);
            Press(scene,WorkshopUiControl.ShowHint);
            Assert.False(hint.Visible);
            Assert.Equal(1,hint.Modulate.A);
            Press(scene,WorkshopUiControl.ShowHint);
            Assert.Equal(0,hint.Modulate.A);
            Publish((Half).103515625);
            scene._Process(QuarterDuration);
            Assert.Equal(.103515625f,hint.Modulate.A);
            Assert.Equal(0,scene.World.Ticks);
        }
        finally { Release(scene); }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CommittedBuildingClearsSolvedWithEitherAcknowledgementOrder(bool acknowledgementFirst)
    {
        var scene = Scene();
        try
        {
            var solved = Control<Label>(scene, WorkshopUiControl.Solved);
            Solve(solved);
            SetSceneField(scene, "_solvedPresentationEpoch", new SimulationEpoch(1));
            SetSceneField(scene, "_displayedWorkshopPhase", WorkshopSimulationPhase.Running);
            // Native projection boundary only: actual command/cursor ownership is tested separately.
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Building);
            if (acknowledgementFirst) InvokeScene(scene, "ResetUiAnimations");
            InvokeScene(scene, "ReconcileCommittedUi");
            Assert.Equal(0, solved.Modulate.A); // The goal label rests on its declared neutral opacity.
            Assert.Equal(default, SceneField<SimulationEpoch>(scene, "_solvedPresentationEpoch"));
            InvokeScene(scene, "ReconcileCommittedUi");
            Assert.Equal(0, solved.Modulate.A);
        }
        finally { Release(scene); }
    }

    [Fact]
    public void BuildingGoalCleanupPreservesHintsAndStaleResetCannotClearNewRunningGoal()
    {
        var scene = Scene();
        try
        {
            var hint = Reveal(scene); Publish((Half).5); scene._Process(0);
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Building);
            SetSceneField(scene, "_displayedWorkshopPhase", WorkshopSimulationPhase.Building);
            InvokeScene(scene, "ReconcileCommittedUi");
            InvokeScene(scene, "ReconcileCommittedUi");
            Assert.True(hint.Visible); Assert.Equal(.5f, hint.Modulate.A);
            var solved = Control<Label>(scene, WorkshopUiControl.Solved);
            Solve(solved); SetSceneField(scene, "_solvedPresentationEpoch", new SimulationEpoch(2));
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Running);
            // The valid older Reset ACK is inapplicable; its finally reconciles the newer committed phase.
            InvokeScene(scene, "ReconcileCommittedUi");
            Assert.Equal(1, solved.Modulate.A);
            Assert.Equal(new SimulationEpoch(2), SceneField<SimulationEpoch>(scene, "_solvedPresentationEpoch"));
        }
        finally { Release(scene); }
    }

    [Fact]
    public void GoalCleanupCompletesWhileTheHintControlIsQueuedBehindTheLease()
    {
        var scene = Scene();
        try
        {
            var hint = Reveal(scene);
            var solved = Control<Label>(scene, WorkshopUiControl.Solved); Solve(solved);
            SetSceneField(scene, "_solvedPresentationEpoch", new SimulationEpoch(1));
            _client.LeaseBusy = true;
            InvokeScene(scene, "ResetUiAnimations"); // queues the Hide; never throws while the lease is busy
            Assert.Equal(AnimationControlKind.Hide, _client.Queued);
            Assert.Equal(0, solved.Modulate.A);
            Assert.Equal(default, SceneField<SimulationEpoch>(scene, "_solvedPresentationEpoch"));
            Assert.False(hint.Visible); Assert.Equal(1, hint.Modulate.A);
        }
        finally { _client.LeaseBusy = false; Release(scene); }
    }

    [Fact]
    public void ClickingTheLevelPickerLeavesKeyboardFocusWithTheWorkshopShortcuts()
    {
        // A focused OptionButton consumes Space (ui_accept) before Workshop._UnhandledInput, so Run/Reset would never fire.
        var scene = Scene();
        try
        {
            var picker = Control<OptionButton>(scene, WorkshopUiControl.LevelPicker);
            var viewport = picker.GetViewport();
            // Pointer events arrive in window pixels; the headless window is stretched onto the 1440x900 viewport.
            var centre = viewport.GetFinalTransform() * picker.GetGlobalRect().GetCenter();
            foreach (var pressed in new[] { true, false })
                viewport.PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = pressed, Position = centre, GlobalPosition = centre });
            godot.Engine.Iteration();
            Assert.True(picker.GetPopup().Visible, "the click reached the picker and opened its list");
            picker.GetPopup().Hide();
            godot.Engine.Iteration();
            Assert.NotSame(picker, viewport.GuiGetFocusOwner());
        }
        finally { Release(scene); }
    }

    private static void InvokeScene(Workshop scene, string method) => typeof(Workshop).GetMethod(method,
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(scene, null);
    private static void SetSceneField<T>(Workshop scene, string field, T value) => typeof(Workshop).GetField(field,
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(scene, value);
    private static T SceneField<T>(Workshop scene, string field) => (T)typeof(Workshop).GetField(field,
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(scene)!;

    private sealed class HintReadClient : IWorkshopClient
    {
        public WorkshopCadenceSettings Settings => WorkshopCadenceSettings.Default();
        public SimulationEpoch Epoch => new(1);
        public AuthorityRevision Revision => default;
        public WorkshopCommandIdentity? Pending => null;
        public WorkshopTransportState TransportState => WorkshopTransportState.Ready;
        public Half? Next;
        public bool Disposed;
        public bool LeaseBusy;
        public AnimationControlKind? Queued;
        public void ControlUi(WorkshopUiTarget target, AnimationControlKind kind, bool visible)
        {
            Assert.Equal(WorkshopUiTarget.Hint, target); Assert.True(Enum.IsDefined(kind));
            if (LeaseBusy) { Queued = kind; return; } // The real client queues behind the single lease; it never throws here.
            if (kind is AnimationControlKind.Hide or AnimationControlKind.Reveal) Next = null;
        }
        public bool TryUiFrame(ulong frame, WorkshopPresentationSample physical, WorkshopUiTarget target, out AnimationOpacity opacity)
        {
            if (target != WorkshopUiTarget.Hint || Disposed || Next is not { } value) { opacity = default; return false; }
            Next = null; opacity = new(value); return true;
        }
        public bool TryCosmeticFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out WorkshopCosmeticSample sample) { sample = default; return false; }
        public bool TryRead(out WorkshopResponse response) { response = default; return false; }
        public bool TryPresent(ulong frame, out WorkshopPresentationSample sample) { sample = default; return false; }
        public void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene) { }
        public Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
            WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null) =>
            throw new InvalidOperationException("Gameplay is outside this hint read-consumer fixture.");
        public bool TryElectricalFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out ElectricalIndicatorSample sample) { sample = default; return false; }
        public Task<WorkshopDelivery> CancelPending() => throw new InvalidOperationException("No command is pending.");
        public ValueTask DisposeAsync() { Disposed = true; Next = null; return ValueTask.CompletedTask; }
    }
}
