using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class RgbAnimationTests(NativeSceneFixture godot)
{
    private static readonly Color Baseline=new(.2f,.3f,.4f,.8f);
    private static readonly Color First=new(.9f,.1f,.6f,.8f),Second=new(.1f,.8f,.2f,.8f);
    private static void Close(Color expected,Color actual)
    {
        Assert.InRange(Math.Abs(expected.R-actual.R),0,2e-6);
        Assert.InRange(Math.Abs(expected.G-actual.G),0,2e-6);
        Assert.InRange(Math.Abs(expected.B-actual.B),0,2e-6);
        Assert.Equal(expected.A,actual.A);
    }
    [Theory]
    [InlineData(AnimationClock.Presentation)]
    [InlineData(AnimationClock.Simulation)]
    public void IndependentChannelsRetargetAnalyticallyAcrossHiddenFramesAndRestore(AnimationClock clock)
    {
        _=godot.Tree;
        using var material=new StandardMaterial3D {AlbedoColor=Baseline};
        var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(material);
        adapter.BindFollowingRgb(target,12,clock);Assert.Equal(3,adapter.AnimationCount);
        adapter.FollowRgb(target,First,0);
        var work=adapter.Present(1,.1,clock==AnimationClock.Simulation?.1:0);
        var one=Baseline.Lerp(First,(float)(1-Math.Exp(-1.2)));
        Close(one,material.AlbedoColor);Assert.Equal(1,work.ColourWrites);
        adapter.SetVisible(target,false);
        adapter.FollowRgb(target,Second,.1);
        work=adapter.Present(2,.3,clock==AnimationClock.Simulation?.3:0);
        Assert.Equal(0,work.ColourWrites);Close(one,material.AlbedoColor);
        adapter.SetVisible(target,true);
        work=adapter.Present(3,.3,clock==AnimationClock.Simulation?.3:0);
        Close(one.Lerp(Second,(float)(1-Math.Exp(-2.4))),material.AlbedoColor);
        Assert.Equal(1,work.ColourWrites);
        adapter.Reset();Assert.Equal(Baseline,material.AlbedoColor);Assert.Equal(0,adapter.AnimationCount);
        Assert.Throws<ArgumentException>(()=>adapter.FollowRgb(target,First,.3));
    }
    public enum InvalidInput { Red, Green, Blue, Alpha, Time }
    [Theory]
    [InlineData(InvalidInput.Red)]
    [InlineData(InvalidInput.Green)]
    [InlineData(InvalidInput.Blue)]
    [InlineData(InvalidInput.Alpha)]
    [InlineData(InvalidInput.Time)]
    public void InvalidRetargetCannotPartiallyChangeChannels(InvalidInput input)
    {
        using var material=new StandardMaterial3D {AlbedoColor=Baseline};
        var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(material);
        adapter.BindFollowingRgb(target,12,AnimationClock.Presentation);
        adapter.FollowRgb(target,First,0);adapter.Present(1,.1,0);
        var bad=Second;
        switch(input)
        {
            case InvalidInput.Red:bad.R=float.NaN;break;
            case InvalidInput.Green:bad.G=1.1f;break;
            case InvalidInput.Blue:bad.B=-.1f;break;
            case InvalidInput.Alpha:bad.A=1;break;
            case InvalidInput.Time:break;
            default:throw new ArgumentOutOfRangeException(nameof(input));
        }
        Assert.ThrowsAny<ArgumentException>(()=>adapter.FollowRgb(target,bad,input==InvalidInput.Time?.09:.1));
        adapter.Present(2,.2,0);Close(Baseline.Lerp(First,(float)(1-Math.Exp(-2.4))),material.AlbedoColor);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RgbAndOpacityComposeAtFullCapacityAndRestore(bool opacityFirst)
    {
        using var material=new StandardMaterial3D {AlbedoColor=Baseline,Transparency=BaseMaterial3D.TransparencyEnum.Alpha};
        var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(material);
        var definition=new AnimationDefinition(.8,.2,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation);
        AnimationHandle opacity;
        if(opacityFirst){opacity=adapter.BindOpacity(target,definition);adapter.BindFollowingRgb(target,12,AnimationClock.Presentation);}
        else{adapter.BindFollowingRgb(target,12,AnimationClock.Presentation);opacity=adapter.BindOpacity(target,definition);}
        Assert.Equal(4,adapter.AnimationCount);
        Assert.Throws<InvalidOperationException>(()=>adapter.BindColour(target,definition,Baseline,First));
        Assert.Throws<InvalidOperationException>(()=>adapter.BindFollowingRgb(target,12,AnimationClock.Presentation));
        Assert.Throws<InvalidOperationException>(()=>adapter.ClaimCommittedColour(target));
        adapter.Start(opacity);adapter.FollowRgb(target,First,0);var work=adapter.Present(1,.5,0);
        var expected=Baseline.Lerp(First,(float)(1-Math.Exp(-6)));expected.A=.5f;
        Close(expected,material.AlbedoColor);Assert.Equal(1,work.ColourWrites);
        adapter.RemoveAnimation(opacity);adapter.Present(2,.5,0);
        expected.A=Baseline.A;Close(expected,material.AlbedoColor);
        adapter.RemoveTarget(target,AnimationTargetRemoval.RestoreBaseline);
        Assert.Equal(Baseline,material.AlbedoColor);Assert.Equal(0,adapter.AnimationCount);
    }
    [Fact]
    public void FailedBindingLeavesCapacityAvailableAndExistingWriterRejectsRgb()
    {
        using var material=new StandardMaterial3D {AlbedoColor=Baseline};
        var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(material);
        Assert.Throws<ArgumentOutOfRangeException>(()=>adapter.BindFollowingRgb(target,0,AnimationClock.Presentation));
        Assert.Equal(0,adapter.AnimationCount);
        var clip=adapter.BindFollowingColour(target,new(0,1,0,12,AnimationClock.Presentation),Baseline,First);
        Assert.Throws<InvalidOperationException>(()=>adapter.BindFollowingRgb(target,12,AnimationClock.Presentation));
        adapter.RemoveAnimation(clip);adapter.BindFollowingRgb(target,12,AnimationClock.Presentation);
        adapter.RemoveTarget(target,AnimationTargetRemoval.Detach);Assert.Equal(0,adapter.AnimationCount);
    }
}
