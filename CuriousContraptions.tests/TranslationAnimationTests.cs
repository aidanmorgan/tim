using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class TranslationAnimationTests(NativeSceneFixture godot)
{
    public static IEnumerable<object[]> Cases()
    {
        foreach(var axis in Enum.GetValues<AnimationTranslationAxis>())
        foreach(var clock in Enum.GetValues<AnimationClock>())yield return [axis,clock];
    }
    private static Vector3 Direction(AnimationTranslationAxis axis)=>axis switch
    {
        AnimationTranslationAxis.X=>Vector3.Right,AnimationTranslationAxis.Y=>Vector3.Up,AnimationTranslationAxis.Z=>Vector3.Back,
        _=>throw new ArgumentOutOfRangeException(nameof(axis))
    };
    [Theory]
    [MemberData(nameof(Cases))]
    public void TranslationUsesDeclaredClockComposesAndRestores(AnimationTranslationAxis axis,AnimationClock clock)
    {
        var node=new Node3D {Position=new(3,4,5)};godot.Tree.Root.AddChild(node);
        try
        {
            var original=node.Transform;var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
            var translation=adapter.BindTranslation(target,new(0,-.06,.06,AnimationCurve.Linear,AnimationRepeat.Once,clock),axis);
            var scale=adapter.BindScale(target,new(1,2,.06,AnimationCurve.Linear,AnimationRepeat.Once,clock));
            adapter.DriveTo(translation,AnimationEndpoint.To,0);adapter.Start(scale);
            var work=adapter.Present(1,.03,clock==AnimationClock.Simulation?.03:0);
            Assert.InRange(node.Position.DistanceTo(original.Origin-Direction(axis)*.03f),0,1e-6);
            Assert.InRange(node.Scale.DistanceTo(Vector3.One*1.5f),0,1e-6);Assert.Equal(1,work.TransformWrites);
            adapter.SetVisible(target,false);adapter.Present(2,.1,clock==AnimationClock.Simulation?.1:0);
            adapter.SetVisible(target,true);adapter.Present(3,.1,clock==AnimationClock.Simulation?.1:0);
            Assert.InRange(node.Position.DistanceTo(original.Origin-Direction(axis)*.06f),0,1e-6);
            adapter.RemoveAnimation(translation);adapter.Present(4,.1,clock==AnimationClock.Simulation?.1:0);
            Assert.Equal(original.Origin,node.Position);Assert.InRange(node.Scale.DistanceTo(Vector3.One*2),0,1e-6);
            adapter.Reset();Assert.Equal(original,node.Transform);
            Assert.Throws<ArgumentException>(()=>adapter.Start(translation));
        }
        finally{node.Free();}
    }
    [Fact]
    public void PhysicalWritersInvalidAxesAndUnsupportedRangeRejectWithoutClaiming()
    {
        using var node=new Node3D();var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
        var clip=new AnimationDefinition(0,1,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation);
        Assert.Throws<ArgumentOutOfRangeException>(()=>adapter.BindTranslation(target,clip,(AnimationTranslationAxis)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>adapter.BindTranslation(target,
            new(0,1e7,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation),AnimationTranslationAxis.Y));
        Assert.Equal(0,adapter.AnimationCount);adapter.ClaimPhysicalPose(target);
        Assert.Throws<InvalidOperationException>(()=>adapter.BindTranslation(target,clip,AnimationTranslationAxis.Y));
    }
}
