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
    private void Publish(Half opacity) => _client.Next = new(1, new(++_client.Pulse), new(1), opacity);
    private static readonly double QuarterDuration = (double)(Half).16 / 4;

    private Workshop Scene()
    {
        var scene = GD.Load<PackedScene>("res://scenes/workshop.tscn").Instantiate<Workshop>();
        godot.Tree.Root.AddChild(scene);
        scene.SetProcess(false);
        scene.SetPhysicsProcess(false);
        _client = new HintReadClient { ReadOpacity = () => Control<Label>(scene, WorkshopAnimationControl.Hint).Modulate.A };
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
    private static T Control<T>(Workshop scene, WorkshopAnimationControl role) where T : Control =>
        Assert.IsAssignableFrom<T>(scene.FindChild(WorkshopAnimationControlBoundary.NodeName(role), true, false));
    private static void Press(Workshop scene, WorkshopAnimationControl role) =>
        Control<Button>(scene, role).EmitSignal(Button.SignalName.Pressed);
    private static Label Reveal(Workshop scene)
    {
        Press(scene, WorkshopAnimationControl.Goal);
        Press(scene, WorkshopAnimationControl.ShowHint);
        return Control<Label>(scene, WorkshopAnimationControl.Hint);
    }

    [Fact]
    public void ActualHintAppliesWorkerHalfSamplesAndRetiresConsumedValues()
    {
        var scene = Scene();
        try
        {
            var before = scene.World.WorkshopRead;
            var hint = Control<Label>(scene, WorkshopAnimationControl.Hint);
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
            Press(scene, WorkshopAnimationControl.Menu);
            Assert.False(hint.IsVisibleInTree());
            scene._Process(QuarterDuration);
            Assert.Equal(quarter, hint.Modulate);
            scene._Process(1);
            scene._Process(1);
            Assert.Equal(quarter, hint.Modulate);
            Press(scene, WorkshopAnimationControl.Goal);
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
            Press(scene, WorkshopAnimationControl.ShowHint);
            Assert.False(hint.Visible);
            Assert.Equal(1, hint.Modulate.A);
            scene._Process(QuarterDuration);
            Assert.Equal(1, hint.Modulate.A);
            Press(scene, WorkshopAnimationControl.ShowHint);
            Publish((Half).103515625f);
            scene._Process(QuarterDuration);
            Assert.Equal(.103515625f, hint.Modulate.A);
            var picker = Control<OptionButton>(scene, WorkshopAnimationControl.LevelPicker);
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
            Press(scene, WorkshopAnimationControl.ShowHint);
            Assert.True(hint.Visible);
            Assert.Equal(0, hint.Modulate.A);
            Publish((Half).103515625);
            scene._Process(QuarterDuration);
            Assert.Equal(0, hint.Modulate.A);
            Assert.True(_client.Disposed);
            Press(scene, WorkshopAnimationControl.ShowHint);
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
            Press(scene,WorkshopAnimationControl.ShowHint);
            Assert.False(hint.Visible);
            Assert.Equal(1,hint.Modulate.A);
            Press(scene,WorkshopAnimationControl.ShowHint);
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
            var solved = Control<Label>(scene, WorkshopAnimationControl.Solved);
            solved.Visible = true;
            SetSceneField(scene, "_solvedPresentationEpoch", new SimulationEpoch(1));
            SetSceneField(scene, "_displayedWorkshopPhase", WorkshopSimulationPhase.Running);
            // Native projection boundary only: actual command/cursor ownership is tested separately.
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Building);
            if (acknowledgementFirst) InvokeScene(scene, "ResetUiAnimations");
            InvokeScene(scene, "ReconcileCommittedUi");
            Assert.False(solved.Visible);
            Assert.Equal(default, SceneField<SimulationEpoch>(scene, "_solvedPresentationEpoch"));
            InvokeScene(scene, "ReconcileCommittedUi");
            Assert.False(solved.Visible);
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
            var solved = Control<Label>(scene, WorkshopAnimationControl.Solved);
            solved.Visible = true; SetSceneField(scene, "_solvedPresentationEpoch", new SimulationEpoch(2));
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.WorkshopPhase))!.SetValue(scene.World, WorkshopSimulationPhase.Running);
            // The valid older Reset ACK is inapplicable; its finally reconciles the newer committed phase.
            InvokeScene(scene, "ReconcileCommittedUi");
            Assert.True(solved.Visible);
            Assert.Equal(new SimulationEpoch(2), SceneField<SimulationEpoch>(scene, "_solvedPresentationEpoch"));
        }
        finally { Release(scene); }
    }

    [Fact]
    public void GoalCleanupPrecedesAnUnavailableHintTransport()
    {
        var scene = Scene();
        try
        {
            var solved = Control<Label>(scene, WorkshopAnimationControl.Solved); solved.Visible = true;
            SetSceneField(scene, "_solvedPresentationEpoch", new SimulationEpoch(1));
            _client.RejectHint = true;
            Assert.Throws<System.Reflection.TargetInvocationException>(() => InvokeScene(scene, "ResetUiAnimations"));
            Assert.False(solved.Visible);
            Assert.Equal(default, SceneField<SimulationEpoch>(scene, "_solvedPresentationEpoch"));
            _client.RejectHint = false;
        }
        finally { _client.RejectHint = false; Release(scene); }
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
        public WorkshopHintSample? Next;
        public ulong Pulse;
        public bool Disposed;
        public bool RejectHint;
        public Func<float> ReadOpacity = null!;
        private Half _consumed;
        public void ControlHint(AnimationControlKind kind, bool visible)
        {
            if (RejectHint) throw new InvalidOperationException("An animation control is pending.");
            Assert.True(Enum.IsDefined(kind));
            if (kind is AnimationControlKind.Hide or AnimationControlKind.Reveal) Next = null;
        }
        public bool TryHint(ulong frame, out WorkshopHintSample sample)
        {
            if (Disposed || Next is not { } value) { sample = default; return false; }
            Next = null; sample = value; _consumed = value.Opacity; return true;
        }
        public bool TryCaptureOpacity(ulong frame, WorkshopPresentationSample physical, out Half opacity)
        { opacity = default; return false; }
        public bool TryActivationBlend(ulong frame, WorkshopPresentationSample physical, ActivationNodeId node, out Half blend) { blend = default; return false; }
        public bool TryTimerFrame(ulong frame, WorkshopPresentationSample physical, ActivationNodeId node, out CuriousContraptions.Presentation.AnimationTimerFrame result) { result = default; return false; }
        public void RecordCapturePresentation(ulong frame) => throw new InvalidOperationException("No capture opacity was supplied by this fixture.");
        public void RecordHintPresentation(ulong frame) => Assert.Equal((float)_consumed, ReadOpacity());
        public bool TryRead(out WorkshopResponse response) { response = default; return false; }
        public bool TryPresent(ulong frame, out WorkshopPresentationSample sample) { sample = default; return false; }
        public void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene) { }
        public Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
            WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null) =>
            throw new InvalidOperationException("Gameplay is outside this hint read-consumer fixture.");
        public Task<WorkshopDelivery> CancelPending() => throw new InvalidOperationException("No command is pending.");
        public ValueTask DisposeAsync() { Disposed = true; Next = null; return ValueTask.CompletedTask; }
    }
}
