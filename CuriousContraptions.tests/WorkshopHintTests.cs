using Godot;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class WorkshopHintTests(NativeSceneFixture godot) : IAsyncDisposable
{
    // The scene's MachineWorld also retires the injected client; its disposal is idempotent.
    public ValueTask DisposeAsync() => _client?.DisposeAsync() ?? ValueTask.CompletedTask;
    // This native fixture supplies transport results, never a local animation clock/evaluator.
    // Actual A execution and clock qualification are covered by the owned Chrome evidence.
    private HintReadClient _client = null!;
    private void Publish(float opacity) => _client.Next = opacity;
    private void PublishState(Workshop scene, WorkshopSimulationPhase phase, ulong epoch)
    {
        _client.Epoch = new(epoch);
        _client.NextRead = _client.Response(phase);
        scene.World._Process(0);
        scene._PhysicsProcess(0);
        scene._Process(0);
    }
    private void Solve(Workshop scene)
    {
        _client.Goal = 1;
        scene._Process(0);
        Assert.Equal(1, Control<Label>(scene, WorkshopUiControl.Solved).Modulate.A);
    }
    private static readonly double QuarterDuration = (double)(float).16 / 4;

    private Workshop Scene()
    {
        var scene = GD.Load<PackedScene>("res://scenes/workshop.tscn").Instantiate<Workshop>();
        _client = new HintReadClient();
        scene.ClientFactory = () => Task.FromResult<IWorkshopClient>(_client);
        godot.Tree.Root.AddChild(scene);
        scene.Startup.GetAwaiter().GetResult();
        scene.SetProcess(false);
        scene.SetPhysicsProcess(false);
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
    public void ActualHintAppliesWorkerF32SamplesAndRetiresConsumedValues()
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
            Publish((float).103515625);
            scene._Process(QuarterDuration);
            Assert.Equal(.103515625f, hint.Modulate.A);
            Publish((float).896484375);
            scene._Process(QuarterDuration * 2);
            Assert.Equal(.896484375f, hint.Modulate.A);
            Publish((float)1);
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
            Publish((float).103515625);
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
            Publish((float)1);
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
            Publish((float).103515625f);
            scene._Process(QuarterDuration);
            Assert.Equal(.103515625f, hint.Modulate.A);
            var picker = Control<OptionButton>(scene, WorkshopUiControl.LevelPicker);
            var construction = scene.World.Construction;
            var selected = picker.Selected;
            // A running machine refuses mode selection before it can disturb the autonomous hint.
            PublishState(scene, WorkshopSimulationPhase.Running, 1);
            picker.EmitSignal(OptionButton.SignalName.ItemSelected, 0);
            Assert.Equal(selected, picker.Selected);
            Assert.Equal(construction, scene.World.Construction);
            Assert.True(hint.IsVisibleInTree());
            Assert.Equal(.103515625f, hint.Modulate.A);
            scene._Process(QuarterDuration);
            Assert.Equal(.103515625f, hint.Modulate.A);
            Publish((float).5);
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
            Publish((float).103515625);
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
            Publish((float).103515625);
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
            Publish((float).103515625);
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
            goal.Apply(new((float).5));
            Assert.Equal(new Color(.2f, .4f, .6f, .5f), label.Modulate);
            new WorkshopUiBinding(WorkshopUiTarget.Hint, label).ApplyNeutral();
            Assert.Equal(new Color(.2f, .4f, .6f, 1), label.Modulate);
            Assert.Equal(new AnimationTargetId(1), UiCurves.Hint.AnimationTarget);
            Assert.Equal(new AnimationTargetId(ulong.MaxValue), UiCurves.Goal.AnimationTarget);
            Assert.Throws<ArgumentException>(() => (UiCurves.Hint with { Duration = (float)0 }).Validate());
            Assert.Throws<ArgumentException>(() => (UiCurves.Goal with { Neutral = (float)1.5 }).Validate());
            Assert.Throws<ArgumentException>(() => (UiCurves.Goal with { Target = WorkshopUiTarget.None }).Validate());
        }
        finally { label.Free(); }
    }

    [Fact]
    public void CanonicalHintUnitsRejectUndefinedValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDurationSeconds(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationDurationSeconds((float)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationOpacity(float.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationOpacity((float)(-.01)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnimationOpacity((float)1.01));
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
            Publish((float).103515625);
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
            PublishState(scene, WorkshopSimulationPhase.Running, 1);
            Solve(scene);
            _client.HoldReset = !acknowledgementFirst;
            Press(scene, WorkshopUiControl.Run);
            if (!acknowledgementFirst)
            {
                PublishState(scene, WorkshopSimulationPhase.Building, 2);
                _client.CompleteReset(applicable: false);
            }
            scene._Process(0);
            Assert.Equal(0, solved.Modulate.A);
            scene._Process(0);
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
            var hint = Reveal(scene); Publish((float).5); scene._Process(0);
            PublishState(scene, WorkshopSimulationPhase.Building, 1);
            scene._Process(0);
            Assert.True(hint.Visible); Assert.Equal(.5f, hint.Modulate.A);
            PublishState(scene, WorkshopSimulationPhase.Running, 1);
            _client.HoldReset = true;
            Press(scene, WorkshopUiControl.Run);
            PublishState(scene, WorkshopSimulationPhase.Running, 2);
            Solve(scene);
            _client.CompleteReset(applicable: false);
            scene._Process(0);
            Assert.Equal(1, Control<Label>(scene, WorkshopUiControl.Solved).Modulate.A);
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
            PublishState(scene, WorkshopSimulationPhase.Running, 1);
            var solved = Control<Label>(scene, WorkshopUiControl.Solved); Solve(scene);
            _client.LeaseBusy = true;
            Press(scene, WorkshopUiControl.Run);
            Assert.Equal(AnimationControlKind.Hide, _client.Queued);
            Assert.Equal(0, solved.Modulate.A);
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
            {
                using var click = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = pressed, Position = centre, GlobalPosition = centre };
                viewport.PushInput(click);
            }
            godot.Engine.Iteration();
            Assert.True(picker.GetPopup().Visible, "the click reached the picker and opened its list");
            picker.GetPopup().Hide();
            godot.Engine.Iteration();
            Assert.NotSame(picker, viewport.GuiGetFocusOwner());
        }
        finally { Release(scene); }
    }

    private sealed class HintReadClient : IWorkshopClient
    {
        public WorkshopCadenceSettings Settings => WorkshopCadenceSettings.Default();
        public SimulationEpoch Epoch { get; set; } = new(1);
        public AuthorityRevision Revision => default;
        public WorkshopCommandIdentity? Pending => null;
        public WorkshopTransportState TransportState => WorkshopTransportState.Ready;
        public float? Next, Goal;
        public WorkshopResponse? NextRead;
        public bool HoldReset;
        private TaskCompletionSource<WorkshopDelivery>? _reset;
        public WorkshopResponse Response(WorkshopSimulationPhase phase) => new(new(1), WorkshopResponseKind.Acknowledgement,
            new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), phase,
            new(Epoch, default, default, Revision, new(WorkshopClockDomain.SimulationMonotonic, new(1), new(0), new(0))),
            new(1, 1), Cadence: new(1), MasterGeneration: new(1), Projection: new(1));
        public void CompleteReset(bool applicable)
        {
            _reset!.SetResult(new(Response(WorkshopSimulationPhase.Building), applicable));
            _reset = null;
        }
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
            var sample = target == WorkshopUiTarget.Goal ? Goal : Next;
            if (Disposed || sample is not { } value) { opacity = default; return false; }
            if (target == WorkshopUiTarget.Goal) Goal = null; else Next = null;
            opacity = new(value); return true;
        }
        public bool TryRead(out WorkshopResponse response) { response = NextRead ?? default; var available = NextRead is not null; NextRead = null; return available; }
        public bool TryPresent(ulong frame, out WorkshopPresentationSample sample) { sample = default; return false; }
        public void RecordPresentation(WorkshopPresentationSample sample, bool selected, PresentationScene scene) { }
        public Task<WorkshopDelivery> Execute(WorkshopCommandKind kind, WorkshopConstruction? construction = null,
            WorkshopCommandIdentity? target = null, WorkshopCadenceSettings? settings = null, WorkshopElectricalControl? electrical = null)
        {
            Assert.Equal(WorkshopCommandKind.Reset, kind);
            if (HoldReset) { _reset = new(); return _reset.Task; }
            Epoch = new(Epoch.Value + 1);
            return Task.FromResult(new WorkshopDelivery(Response(WorkshopSimulationPhase.Building), true));
        }
        public void SetDeclaredVisible(GpuBodyId owner, bool visible) { }
        public bool TryDeclaredFrame(ulong frame, WorkshopPresentationSample physical, GpuBodyId owner, out System.Collections.Generic.IReadOnlyList<DeclaredPresentationSample> samples) { samples = System.Array.Empty<DeclaredPresentationSample>(); return false; }
        public Task<WorkshopDelivery> CancelPending() => throw new InvalidOperationException("No command is pending.");
        public ValueTask DisposeAsync() { Disposed = true; Next = null; return ValueTask.CompletedTask; }
    }
}
