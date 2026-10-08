using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class OscillationAdapterTests(NativeSceneFixture godot)
{
    public enum Motion { Rotation, Translation }
    private static Vector3 Direction(AnimationRotationAxis axis)=>axis switch
    {
        AnimationRotationAxis.X=>Vector3.Right,AnimationRotationAxis.Y=>Vector3.Up,AnimationRotationAxis.Z=>Vector3.Back,
        _=>throw new ArgumentOutOfRangeException(nameof(axis))
    };
    private static AnimationTranslationAxis TranslationAxis(AnimationRotationAxis axis)=>axis switch
    {
        AnimationRotationAxis.X=>AnimationTranslationAxis.X,AnimationRotationAxis.Y=>AnimationTranslationAxis.Y,
        AnimationRotationAxis.Z=>AnimationTranslationAxis.Z,_=>throw new ArgumentOutOfRangeException(nameof(axis))
    };
    [Theory]
    [InlineData(Motion.Rotation,AnimationRotationAxis.X)]
    [InlineData(Motion.Rotation,AnimationRotationAxis.Y)]
    [InlineData(Motion.Rotation,AnimationRotationAxis.Z)]
    [InlineData(Motion.Translation,AnimationRotationAxis.X)]
    [InlineData(Motion.Translation,AnimationRotationAxis.Y)]
    [InlineData(Motion.Translation,AnimationRotationAxis.Z)]
    public void AnalyticMotionComposesFromConstructionAndHiddenResumeRestoresExactly(Motion motion,AnimationRotationAxis axis)
    {
        var node=new Node3D {Position=new(2,3,4),Rotation=new(.1f,.2f,.3f)};godot.Tree.Root.AddChild(node);
        try
        {
            var baseline=node.Transform;var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
            var definition=new AnimationOscillationDefinition(7,48,5,AnimationClock.Presentation);
            var handle=motion switch
            {
                Motion.Rotation=>adapter.BindOscillatingRotation(target,definition,axis),
                Motion.Translation=>adapter.BindOscillatingTranslation(target,definition,TranslationAxis(axis)),
                _=>throw new ArgumentOutOfRangeException(nameof(motion))
            };
            Assert.Throws<InvalidOperationException>(()=>adapter.ClaimPhysicalPose(target));
            adapter.ValidateOscillationKick(handle,new(1),1,0);Assert.Equal(0UL,adapter.ReadOscillation(handle).Accepted);
            adapter.KickOscillation(handle,new(1),1,0);var work=adapter.Present(1,.03,0);Assert.Equal(1,work.TransformWrites);
            var first=node.Transform;adapter.SetVisible(target,false);adapter.Present(2,.1,0);Assert.Equal(first,node.Transform);
            adapter.SetVisible(target,true);adapter.Present(3,.1,0);
            var angle=(float)(5.0/48*Math.Exp(-.7)*Math.Sin(4.8));
            var expected=motion==Motion.Rotation?new Transform3D(baseline.Basis*new Basis(Direction(axis),angle),baseline.Origin):
                new Transform3D(baseline.Basis,baseline.Origin+Direction(axis)*angle);
            Assert.InRange(expected.Origin.DistanceTo(node.Position),0,1e-6);
            Assert.InRange(expected.Basis.X.DistanceTo(node.Basis.X),0,1e-6);
            Assert.InRange(expected.Basis.Y.DistanceTo(node.Basis.Y),0,1e-6);
            Assert.InRange(expected.Basis.Z.DistanceTo(node.Basis.Z),0,1e-6);
            adapter.RemoveAnimation(handle);adapter.Present(4,.1,0);Assert.Equal(baseline,node.Transform);
            adapter.Reset();Assert.Equal(baseline,node.Transform);
        }
        finally{node.Free();}
    }
    [Fact]
    public void PhysicalTargetsAndUndefinedAxesRejectBeforeRegistration()
    {
        using var node=new Node3D();var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
        var definition=new AnimationOscillationDefinition(7,48,5,AnimationClock.Presentation);
        Assert.Throws<ArgumentOutOfRangeException>(()=>adapter.BindOscillatingRotation(target,definition,(AnimationRotationAxis)99));
        Assert.Throws<ArgumentOutOfRangeException>(()=>adapter.BindOscillatingTranslation(target,definition,(AnimationTranslationAxis)99));
        Assert.Equal(0,adapter.AnimationCount);adapter.ClaimPhysicalPose(target);
        Assert.Throws<InvalidOperationException>(()=>adapter.BindOscillatingRotation(target,definition,AnimationRotationAxis.Z));
        Assert.Throws<InvalidOperationException>(()=>adapter.BindOscillatingTranslation(target,definition,AnimationTranslationAxis.X));
    }
}
