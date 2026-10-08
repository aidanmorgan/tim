using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ImpulseTransformAdapterTests(NativeSceneFixture godot)
{
    private static AnimationImpulseDefinition Definition(AnimationImpulseCurve curve,AnimationClock clock=AnimationClock.Presentation)=>
        new(1,curve,AnimationImpulseOverlap.SaturatingSum,AnimationImpulseVisibility.DeferUntilVisible,clock,4,
            AnimationImpulseTiming.EventTimeRetainFirstFrame,curve==AnimationImpulseCurve.SineSquaredPulse?.5:.08/.53);
    private static Vector3 Direction(AnimationTranslationAxis axis)=>axis switch
    {
        AnimationTranslationAxis.X=>Vector3.Right,AnimationTranslationAxis.Y=>Vector3.Up,AnimationTranslationAxis.Z=>Vector3.Back,
        _=>throw new ArgumentOutOfRangeException(nameof(axis))
    };
    public static IEnumerable<object[]> Cases()
    {
        foreach(var axis in Enum.GetValues<AnimationTranslationAxis>())
        foreach(var clock in Enum.GetValues<AnimationClock>())
        foreach(var curve in new[]{AnimationImpulseCurve.SineSquaredPulse,AnimationImpulseCurve.SmoothRiseFall})
            yield return [axis,clock,curve];
    }
    private static void Close(Transform3D expected,Transform3D actual)
    {
        Assert.InRange(expected.Origin.DistanceTo(actual.Origin),0,2e-6);
        Assert.InRange(expected.Basis.X.DistanceTo(actual.Basis.X),0,2e-6);
        Assert.InRange(expected.Basis.Y.DistanceTo(actual.Basis.Y),0,2e-6);
        Assert.InRange(expected.Basis.Z.DistanceTo(actual.Basis.Z),0,2e-6);
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void ScaleAndTranslationComposeOnceWithExplicitClockAndRestore(AnimationTranslationAxis axis,AnimationClock clock,AnimationImpulseCurve curve)
    {
        var node=new Node3D{Transform=new(Basis.FromEuler(new(.3f,-.2f,.5f)).Scaled(new(1.5f,.8f,2)),new(3,4,5))};
        godot.Tree.Root.AddChild(node);
        try
        {
            var baseline=node.Transform;var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
            var d=Definition(curve,clock);var scale=adapter.BindImpulseScale(target,d,1.24);
            var translation=adapter.BindImpulseTranslation(target,d,-.16,axis);
            Assert.Equal(AnimationImpulseAdmission.Accepted,adapter.EnqueueImpulse(scale,new(1),1,0));
            Assert.Equal(AnimationImpulseAdmission.Accepted,adapter.EnqueueImpulse(translation,new(1),1,0));
            Assert.Equal(baseline,node.Transform);
            var expected=new Transform3D(new(baseline.Basis.X*1.24f,baseline.Basis.Y*1.24f,baseline.Basis.Z*1.24f),
                baseline.Origin-Direction(axis)*.16f);
            var first=adapter.Present(1,d.PeakPhase,0);
            Close(clock==AnimationClock.Presentation?expected:baseline,node.Transform);
            var second=adapter.Present(2,d.PeakPhase,d.PeakPhase);Close(expected,node.Transform);
            Assert.Equal(1,first.TransformWrites+second.TransformWrites);
            adapter.Present(3,d.PeakPhase+1,d.PeakPhase);
            Close(clock==AnimationClock.Presentation?baseline:expected,node.Transform);
            adapter.CancelImpulses(scale);adapter.CancelImpulses(translation);
            adapter.Present(4,d.PeakPhase+1,d.PeakPhase);Close(baseline,node.Transform);
            adapter.Reset();Assert.Equal(baseline,node.Transform);
            Assert.Throws<ArgumentException>(()=>adapter.EnqueueImpulse(scale,new(2),1,2));
        }
        finally{node.Free();}
    }
    [Theory]
    [InlineData(AnimationProperty.UniformScale)]
    [InlineData(AnimationProperty.LocalTranslation)]
    public void HiddenUnseenPeakSurvivesAndRemovalAllowsAReplacementWriter(AnimationProperty property)
    {
        var node=new Node3D{Position=new(3,4,5)};godot.Tree.Root.AddChild(node);
        try
        {
            var baseline=node.Transform;var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
            var d=Definition(AnimationImpulseCurve.SineSquaredPulse);
            var animation=property switch
            {
                AnimationProperty.UniformScale=>adapter.BindImpulseScale(target,d,1.24),
                AnimationProperty.LocalTranslation=>adapter.BindImpulseTranslation(target,d,-.16,AnimationTranslationAxis.X),
                _=>throw new ArgumentOutOfRangeException(nameof(property))
            };
            adapter.EnqueueImpulse(animation,new(1),1,0);adapter.Present(1,0,0);
            adapter.SetVisible(target,false);adapter.Present(2,2,0);Close(baseline,node.Transform);
            adapter.SetVisible(target,true);adapter.Present(3,2,0);Assert.NotEqual(baseline,node.Transform);
            adapter.RemoveAnimation(animation);adapter.Present(4,2,0);Close(baseline,node.Transform);
            var replacement=property switch
            {
                AnimationProperty.UniformScale=>adapter.BindScale(target,new(1,2,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation)),
                AnimationProperty.LocalTranslation=>adapter.BindTranslation(target,new(0,.5,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation),AnimationTranslationAxis.X),
                _=>throw new ArgumentOutOfRangeException()
            };
            adapter.Start(replacement);adapter.Present(5,2.5,0);Assert.NotEqual(baseline,node.Transform);
            adapter.Reset();Assert.Equal(baseline,node.Transform);
        }
        finally{node.Free();}
    }
    [Theory]
    [InlineData(AnimationProperty.UniformScale)]
    [InlineData(AnimationProperty.LocalTranslation)]
    public void PhysicalAndDuplicateWritersReject(AnimationProperty property)
    {
        using var node=new Node3D();var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
        var d=Definition(AnimationImpulseCurve.SineSquaredPulse);
        AnimationHandle Bind()=>property switch
        {
            AnimationProperty.UniformScale=>adapter.BindImpulseScale(target,d,1.24),
            AnimationProperty.LocalTranslation=>adapter.BindImpulseTranslation(target,d,-.16,AnimationTranslationAxis.X),
            _=>throw new ArgumentOutOfRangeException(nameof(property))
        };
        var animation=Bind();Assert.Throws<InvalidOperationException>(()=>Bind());
        Assert.Throws<InvalidOperationException>(()=>adapter.ClaimPhysicalPose(target));
        adapter.RemoveAnimation(animation);adapter.ClaimPhysicalPose(target);
        Assert.Throws<InvalidOperationException>(()=>Bind());
    }
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1e7)]
    public void UnsupportedScaleRejectsBeforeClaiming(double peak)
    {
        using var node=new Node3D();var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
        Assert.Throws<ArgumentOutOfRangeException>(()=>adapter.BindImpulseScale(target,Definition(AnimationImpulseCurve.SineSquaredPulse),peak));
        Assert.Equal(0,adapter.AnimationCount);adapter.ClaimPhysicalPose(target);
    }
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1e7)]
    [InlineData(1e7)]
    public void UnsupportedTranslationRejectsBeforeClaiming(double displacement)
    {
        using var node=new Node3D();var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
        Assert.Throws<ArgumentOutOfRangeException>(()=>adapter.BindImpulseTranslation(target,Definition(AnimationImpulseCurve.SineSquaredPulse),
            displacement,AnimationTranslationAxis.X));
        Assert.Equal(0,adapter.AnimationCount);adapter.ClaimPhysicalPose(target);
    }
    [Fact]
    public void InvalidAxisAndUnrepresentableScaledBasisRejectAtBinding()
    {
        using var node=new Node3D{Scale=Vector3.One*1e9f};var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
        var d=Definition(AnimationImpulseCurve.SineSquaredPulse);
        Assert.Throws<ArgumentOutOfRangeException>(()=>adapter.BindImpulseTranslation(target,d,.1,(AnimationTranslationAxis)99));
        Assert.Throws<ArgumentException>(()=>adapter.BindImpulseScale(target,d,1e6));
        Assert.Equal(0,adapter.AnimationCount);
    }
}
