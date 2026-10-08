using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public sealed class WorkshopHintTests(NativeSceneFixture godot)
{
    private static readonly double QuarterDuration = (double)(Half).16 / 4;

    private Workshop Scene()
    {
        var scene = GD.Load<PackedScene>("res://scenes/workshop.tscn").Instantiate<Workshop>();
        godot.Tree.Root.AddChild(scene);
        scene.SetProcess(false);
        scene.SetPhysicsProcess(false);
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
    public void ActualHintPreservesQuinticQuarterPhasesAndRetiresCompletedWork()
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
            scene._Process(QuarterDuration);
            Assert.Equal(.103515625f, hint.Modulate.A);
            scene._Process(QuarterDuration * 2);
            Assert.Equal(.896484375f, hint.Modulate.A);
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
    public void HiddenParentAdvancesClockWithoutColourWritesAndPublishesOnceOnReentry()
    {
        var scene = Scene();
        try
        {
            var hint = Reveal(scene);
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
    public void HideAndFreeModeSelectionCancelRevealAndRestoreBaseline()
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
            scene._Process(QuarterDuration);
            Assert.Equal(.103515625f, hint.Modulate.A);
            var picker = Control<OptionButton>(scene, WorkshopAnimationControl.LevelPicker);
            picker.EmitSignal(OptionButton.SignalName.ItemSelected, 1);
            Assert.False(hint.IsVisibleInTree());
            Assert.Equal(1, hint.Modulate.A);
            scene._Process(QuarterDuration);
            Assert.Equal(1, hint.Modulate.A);
            Reveal(scene);
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
            scene._Process(QuarterDuration);
            godot.Tree.Root.RemoveChild(scene);
            Assert.Equal(1, hint.Modulate.A);
            Assert.Equal(WorkshopSimulationPhase.Disposed, scene.World.WorkshopPhase);
            godot.Tree.Root.AddChild(scene);
            scene._Process(QuarterDuration);
            Assert.Equal(1, hint.Modulate.A);
            Press(scene, WorkshopAnimationControl.ShowHint);
            Press(scene, WorkshopAnimationControl.ShowHint);
            scene._Process(QuarterDuration);
            Assert.Equal(.103515625f, hint.Modulate.A);
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
            Assert.Throws<ArgumentOutOfRangeException>(() => scene._Process(delta));
            Assert.Equal(before, hint.Modulate);
            scene._Process(QuarterDuration);
            Assert.Equal(.103515625f, hint.Modulate.A);
        }
        finally { Release(scene); }
    }

    [Fact]
    public void CanonicalHintUnitsRejectUndefinedValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HintDurationSeconds(Half.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HintDurationSeconds((Half)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HintOpacity(Half.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HintOpacity((Half)(-.01)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HintOpacity((Half)1.01));
    }
}
