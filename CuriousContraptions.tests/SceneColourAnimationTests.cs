using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SceneColourAnimationTests(NativeSceneFixture godot)
{
    public enum TargetKind { Canvas, Material }

    [Theory]
    [InlineData(TargetKind.Canvas,false)]
    [InlineData(TargetKind.Canvas,true)]
    [InlineData(TargetKind.Material,false)]
    [InlineData(TargetKind.Material,true)]
    public void ColourAndOpacityComposeIndependentlyOfBindingOrder(TargetKind kind,bool reverse)
    {
        var baseline=new Color(.1f,.2f,.3f,.8f);
        var endpoint=new Color(.9f,.8f,.7f,.8f);
        var canvas=new Control {Modulate=baseline};godot.Tree.Root.AddChild(canvas);
        using var material=new StandardMaterial3D {AlbedoColor=baseline,Transparency=BaseMaterial3D.TransparencyEnum.Alpha};
        try
        {
            var adapter=new SceneAnimationAdapter(1);
            var target=kind switch
            {
                TargetKind.Canvas=>adapter.Register(canvas),
                TargetKind.Material=>adapter.Register(material),
                _=>throw new ArgumentOutOfRangeException(nameof(kind))
            };
            Color Read()=>kind==TargetKind.Canvas?canvas.Modulate:material.AlbedoColor;
            AnimationHandle Colour()=>adapter.BindColour(target,
                new(0,1,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation),baseline,endpoint);
            AnimationHandle Opacity()=>adapter.BindOpacity(target,
                new(.8,.2,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation));
            AnimationHandle colour,opacity;
            if(reverse){opacity=Opacity();colour=Colour();}else{colour=Colour();opacity=Opacity();}
            adapter.Start(colour);adapter.Start(opacity);
            Assert.Equal(baseline,Read());
            Assert.Equal(1,adapter.Present(1,.5,0).ColourWrites);
            var expected=baseline.Lerp(endpoint,.5f);expected.A=.5f;
            Assert.Equal(expected,Read());
            Assert.Equal(0,adapter.Present(2,.5,0).ColourWrites);
            adapter.RemoveAnimation(colour);adapter.Present(3,.5,0);
            expected=baseline;expected.A=.5f;Assert.Equal(expected,Read());
            adapter.RemoveAnimation(opacity);adapter.Present(4,.5,0);Assert.Equal(baseline,Read());
            adapter.Reset();Assert.Equal(baseline,Read());
        }
        finally{canvas.Free();}
    }

    [Fact]
    public void InvalidColoursRangesAndDuplicateWritersRejectBeforeRegistration()
    {
        using var material=new StandardMaterial3D {AlbedoColor=new(.2f,.3f,.4f,1)};
        var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(material);
        var definition=new AnimationDefinition(0,1,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation);
        var baseline=material.AlbedoColor;
        Assert.Throws<ArgumentException>(()=>adapter.BindColour(target,definition,new(float.NaN,0,0,1),baseline));
        Assert.Throws<ArgumentException>(()=>adapter.BindColour(target,definition,baseline,new(1,1,1,.5f)));
        Assert.Throws<ArgumentException>(()=>adapter.BindColour(target,definition,new(-float.MaxValue,0,0,1),new(float.MaxValue,0,0,1)));
        Assert.Throws<ArgumentException>(()=>adapter.BindColour(target,
            new(-.1,1,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation),baseline,baseline));
        Assert.Equal(0,adapter.AnimationCount);
        adapter.BindColour(target,definition,baseline,baseline);
        Assert.Throws<InvalidOperationException>(()=>adapter.BindColour(target,definition,baseline,baseline));
        Assert.Equal(1,adapter.AnimationCount);Assert.Equal(baseline,material.AlbedoColor);
        adapter.Reset();
    }

    [Fact]
    public void HiddenColourResumesCurrentPhaseAndResetInvalidatesItsHandle()
    {
        using var material=new StandardMaterial3D {AlbedoColor=new(.2f,.3f,.4f,1)};
        var baseline=material.AlbedoColor;var endpoint=new Color(.9f,.8f,.7f,1);
        var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(material);
        var animation=adapter.BindColour(target,
            new(0,1,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation),baseline,endpoint);
        adapter.DriveTo(animation,AnimationEndpoint.To,0);adapter.SetVisible(target,false);
        Assert.Equal(0,adapter.Present(1,.5,0).ColourWrites);Assert.Equal(baseline,material.AlbedoColor);
        adapter.SetVisible(target,true);adapter.Present(2,.5,0);
        Assert.Equal(baseline.Lerp(endpoint,.5f),material.AlbedoColor);
        adapter.Reset();Assert.Equal(baseline,material.AlbedoColor);
        Assert.Throws<ArgumentException>(()=>adapter.DriveTo(animation,AnimationEndpoint.To,0));
    }

    [Fact]
    public void WarmedColourSubmissionAllocatesNoManagedMemory()
    {
        using var material=new StandardMaterial3D {AlbedoColor=new(.2f,.3f,.4f,1)};
        var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(material);
        var animation=adapter.BindColour(target,
            new(0,1,1,AnimationCurve.Linear,AnimationRepeat.PingPong,AnimationClock.Presentation),
            material.AlbedoColor,new(.9f,.8f,.7f,1));
        adapter.Start(animation);
        for(ulong frame=1;frame<=100;frame++)adapter.Present(frame,frame/60d,0);
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(ulong frame=101;frame<=1100;frame++)adapter.Present(frame,frame/60d,0);
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
        adapter.Reset();
    }
}
