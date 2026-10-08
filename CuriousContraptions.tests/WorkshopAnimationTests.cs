using System.Text.Json;
using CuriousContraptions.Presentation;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WorkshopAnimationTests(NativeSceneFixture godot)
{
    private readonly record struct ResourcePath(string Value);
    private static readonly ResourcePath WorkshopScene=new("res://scenes/workshop.tscn");
    private Workshop Scene()
    {
        var scene=GD.Load<PackedScene>(WorkshopScene.Value).Instantiate<Workshop>();
        godot.Tree.Root.AddChild(scene);return scene;
    }
    private void Release(Workshop scene)
    {
        scene.Free();
        // Drain the scene tree's deferred deletion queue while its engine is still alive.
        godot.Engine.Iteration();
    }
    private static T Control<T>(Workshop scene,WorkshopAnimationControl role) where T:Godot.Control=>
        Assert.IsAssignableFrom<T>(scene.FindChild(WorkshopAnimationControlBoundary.NodeName(role),true,false));
    private static void Press(Workshop scene,WorkshopAnimationControl role)=>
        Control<Button>(scene,role).EmitSignal(Button.SignalName.Pressed);
    private static string Saved(Workshop scene)=>JsonSerializer.Serialize(scene.World.Snapshot(),MachineJson.Default.MachineData);
    private static Label Reveal(Workshop scene)
    {
        Press(scene,WorkshopAnimationControl.Goal);Press(scene,WorkshopAnimationControl.ShowHint);
        return Control<Label>(scene,WorkshopAnimationControl.Hint);
    }

    [Fact]
    public void HintUsesPresentationClockBeforeAnyPhysicsRunAndRetiresCompletedWork()
    {
        var scene=Scene();
        try
        {
            var saved=Saved(scene);Assert.False(scene.World.HasPhysicsState);
            var hint=Reveal(scene);var baseline=hint.Modulate;
            Assert.True(hint.IsVisibleInTree());Assert.NotEmpty(hint.Text);
            scene._Process(0);Assert.Equal(0,hint.Modulate.A);
            scene._Process(.08);Assert.Equal(.5f,hint.Modulate.A);
            Assert.Equal(baseline.R,hint.Modulate.R);Assert.Equal(baseline.G,hint.Modulate.G);Assert.Equal(baseline.B,hint.Modulate.B);
            scene._Process(.08);Assert.Equal(baseline,hint.Modulate);
            scene._Process(1);Assert.Equal(0,scene.UiAnimationWork.Animation.Evaluated);Assert.Equal(0,scene.UiAnimationWork.ColourWrites);
            Assert.Equal(saved,Saved(scene));Assert.Equal(0,scene.World.Ticks);Assert.False(scene.World.HasPhysicsState);
            Press(scene,WorkshopAnimationControl.Goal);Assert.False(hint.IsVisibleInTree());
            scene._Process(0);Assert.Equal(baseline,hint.Modulate);
            Reveal(scene);scene._Process(.08);Assert.Equal(.5f,hint.Modulate.A);
            Press(scene,WorkshopAnimationControl.ShowHint);scene._Process(0);
            Assert.False(hint.Visible);Assert.Equal(baseline,hint.Modulate);
        }
        finally{Release(scene);}
    }

    [Fact]
    public void HintFramesDoNotStepRunningOrPausedPhysicsAndResetCancelsItsGeneration()
    {
        var scene=Scene();
        try
        {
            var saved=Saved(scene);var hint=Reveal(scene);var baseline=hint.Modulate;
            Press(scene,WorkshopAnimationControl.Run);Assert.True(scene.World.Running);
            scene.World.Step();
            var bodies=scene.World.Physics.Capture().BodyStates.ToArray();var ticks=scene.World.Ticks;
            scene._Process(.04);Assert.Equal(.103515625f,hint.Modulate.A);
            Assert.Equal(ticks,scene.World.Ticks);Assert.Equal(bodies,scene.World.Physics.Capture().BodyStates.ToArray());
            scene.World.Running=false;scene._Process(.04);Assert.Equal(.5f,hint.Modulate.A);
            Assert.Equal(ticks,scene.World.Ticks);Assert.Equal(bodies,scene.World.Physics.Capture().BodyStates.ToArray());
            var generation=scene.UiAnimationGeneration;
            Press(scene,WorkshopAnimationControl.Run);
            Assert.NotEqual(generation,scene.UiAnimationGeneration);Assert.Equal(baseline,hint.Modulate);
            Assert.Equal(saved,Saved(scene));scene._Process(.04);Assert.Equal(baseline,hint.Modulate);
            Assert.Equal(0,scene.UiAnimationWork.Animation.Registered);
        }
        finally{Release(scene);}
    }

    [Fact]
    public void LevelLoadCancelsHalfFinishedRevealAndCannotLeakIntoNewHint()
    {
        var scene=Scene();
        try
        {
            var hint=Reveal(scene);var baseline=hint.Modulate;scene._Process(.08);
            Assert.Equal(.5f,hint.Modulate.A);var generation=scene.UiAnimationGeneration;
            var picker=Control<OptionButton>(scene,WorkshopAnimationControl.LevelPicker);
            picker.EmitSignal(OptionButton.SignalName.ItemSelected,picker.ItemCount-1);
            Assert.NotEqual(generation,scene.UiAnimationGeneration);Assert.False(hint.IsVisibleInTree());
            Assert.Equal(baseline,hint.Modulate);Assert.Empty(hint.Text);Assert.Empty(scene.World.Parts);
            var saved=Saved(scene);scene._Process(.08);Assert.Equal(baseline,hint.Modulate);
            Reveal(scene);scene._Process(.04);Assert.Equal(.103515625f,hint.Modulate.A);
            Assert.Equal(saved,Saved(scene));
        }
        finally{Release(scene);}
    }

    [Fact]
    public void HiddenParentSkipsWritesAndReentryUsesCurrentPresentationTime()
    {
        var scene=Scene();
        try
        {
            var hint=Reveal(scene);var baseline=hint.Modulate;scene._Process(.04);
            Press(scene,WorkshopAnimationControl.Menu);Assert.False(hint.IsVisibleInTree());
            scene._Process(.04);Assert.Equal(0,scene.UiAnimationWork.Animation.Evaluated);
            scene._Process(1);Assert.Equal(1,scene.UiAnimationWork.Animation.Evaluated);
            Assert.Equal(0,scene.UiAnimationWork.ColourWrites);
            scene._Process(.1);Assert.Equal(0,scene.UiAnimationWork.Animation.Evaluated);
            Assert.Equal(.103515625f,hint.Modulate.A);
            Press(scene,WorkshopAnimationControl.Goal);Assert.True(hint.IsVisibleInTree());
            scene._Process(0);Assert.Equal(baseline,hint.Modulate);
        }
        finally{Release(scene);}
    }

    [Fact]
    public void SceneRemovalRestoresBaselineAndReentryRegistersFreshTargets()
    {
        var scene=Scene();
        try
        {
            var hint=Reveal(scene);var baseline=hint.Modulate;scene._Process(.08);
            var generation=scene.UiAnimationGeneration;
            godot.Tree.Root.RemoveChild(scene);Assert.Equal(baseline,hint.Modulate);
            Assert.NotEqual(generation,scene.UiAnimationGeneration);
            godot.Tree.Root.AddChild(scene);scene._Process(.08);Assert.Equal(baseline,hint.Modulate);
            Press(scene,WorkshopAnimationControl.ShowHint);Press(scene,WorkshopAnimationControl.ShowHint);
            scene._Process(.08);Assert.Equal(.5f,hint.Modulate.A);
        }
        finally{Release(scene);}
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1)]
    public void InvalidFrameDeltaDoesNotConsumeReveal(double delta)
    {
        var scene=Scene();
        try
        {
            var hint=Reveal(scene);var initial=hint.Modulate;
            Assert.Throws<ArgumentOutOfRangeException>(()=>scene._Process(delta));Assert.Equal(initial,hint.Modulate);
            scene._Process(.08);Assert.Equal(.5f,hint.Modulate.A);
        }
        finally{Release(scene);}
    }

    [Fact]
    public void ControlNamesAreCanonicalAtTheExternalBoundaryAndUnknownRolesReject()
    {
        Assert.Equal("PuzzleHint",WorkshopAnimationControlBoundary.NodeName(WorkshopAnimationControl.Hint));
        Assert.Equal("ShowHint",WorkshopAnimationControlBoundary.NodeName(WorkshopAnimationControl.ShowHint));
        Assert.Equal("Goal",WorkshopAnimationControlBoundary.NodeName(WorkshopAnimationControl.Goal));
        Assert.Equal("RunMachine",WorkshopAnimationControlBoundary.NodeName(WorkshopAnimationControl.Run));
        Assert.Equal("LevelPicker",WorkshopAnimationControlBoundary.NodeName(WorkshopAnimationControl.LevelPicker));
        Assert.Equal("Menu",WorkshopAnimationControlBoundary.NodeName(WorkshopAnimationControl.Menu));
        var names=Enum.GetValues<WorkshopAnimationControl>().Select(WorkshopAnimationControlBoundary.NodeName).ToArray();
        Assert.Equal(names.Length,names.Distinct(StringComparer.Ordinal).Count());
        Assert.Throws<ArgumentOutOfRangeException>(()=>WorkshopAnimationControlBoundary.NodeName((WorkshopAnimationControl)99));
    }
}
