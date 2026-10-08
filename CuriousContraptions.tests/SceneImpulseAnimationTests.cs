using Godot;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SceneImpulseAnimationTests(NativeSceneFixture godot)
{
    public enum TargetKind { Canvas, Material }
    private static AnimationImpulseDefinition Definition()=>new(.25,AnimationImpulseCurve.LinearDecay,
        AnimationImpulseOverlap.Maximum,AnimationImpulseVisibility.DeferUntilVisible,AnimationClock.Presentation,4,AnimationImpulseTiming.FirstPresentation,0);

    [Theory]
    [InlineData(TargetKind.Canvas)]
    [InlineData(TargetKind.Material)]
    public void FrameOnlyPulseColourRetainsHiddenOccurrencesAndRestoresExactBaseline(TargetKind kind)
    {
        var from=new Color("#556573");var to=new Color("#f7cb52");
        var canvas=new Control {Modulate=from};godot.Tree.Root.AddChild(canvas);
        using var material=new StandardMaterial3D {AlbedoColor=from};
        try
        {
            var adapter=new SceneAnimationAdapter(1);
            var target=kind switch
            {
                TargetKind.Canvas=>adapter.Register(canvas),TargetKind.Material=>adapter.Register(material),
                _=>throw new ArgumentOutOfRangeException(nameof(kind))
            };
            Color Read()=>kind switch
            {
                TargetKind.Canvas=>canvas.Modulate,TargetKind.Material=>material.AlbedoColor,
                _=>throw new ArgumentOutOfRangeException(nameof(kind))
            };
            var pulse=adapter.BindImpulseColour(target,Definition(),from,to);
            Assert.Throws<InvalidOperationException>(()=>adapter.ClaimCommittedColour(target));
            Assert.Equal(AnimationImpulseAdmission.Accepted,adapter.EnqueueImpulse(pulse,new(1),1,0));
            Assert.Equal(from,Read());adapter.SetVisible(target,false);
            Assert.Equal(0,adapter.Present(1,10,0).ColourWrites);
            Assert.Equal(1,adapter.ReadImpulses(pulse).Pending);
            adapter.SetVisible(target,true);
            Assert.Equal(1,adapter.Present(2,10,0).ColourWrites);Assert.Equal(to,Read());
            adapter.Present(3,10.125,0);Assert.Equal(from.Lerp(to,.5f),Read());
            adapter.EnqueueImpulse(pulse,new(2),1,0);
            Assert.Equal(1,adapter.Present(4,10.125,0).ColourWrites);Assert.Equal(to,Read());
            Assert.Equal(2,adapter.ReadImpulses(pulse).Playing);
            adapter.Present(5,11,0);Assert.Equal(from,Read());Assert.Equal(2UL,adapter.ReadImpulses(pulse).Completed);
            Assert.Equal(0,adapter.Present(6,12,0).ColourWrites);
            adapter.EnqueueImpulse(pulse,new(3),1,0);adapter.Present(7,12,0);Assert.Equal(to,Read());
            adapter.CancelImpulses(pulse);Assert.Equal(to,Read());adapter.Present(8,12,0);Assert.Equal(from,Read());
            adapter.EnqueueImpulse(pulse,new(4),1,0);adapter.Present(9,12,0);adapter.Reset();
            Assert.Equal(from,Read());Assert.Throws<ArgumentException>(()=>adapter.EnqueueImpulse(pulse,new(5),1,0));
        }
        finally{canvas.Free();}
    }

    [Fact]
    public void BindingRejectsInvalidPaletteAndCompetingColourWritersBeforeMutation()
    {
        using var material=new StandardMaterial3D {AlbedoColor=Colors.Black};
        var node=new Node3D();godot.Tree.Root.AddChild(node);
        try
        {
            var adapter=new SceneAnimationAdapter(2);var target=adapter.Register(material);var spatial=adapter.Register(node);
            Assert.Throws<ArgumentException>(()=>adapter.BindImpulseColour(spatial,Definition(),Colors.Black,Colors.White));
            Assert.Throws<ArgumentException>(()=>adapter.BindImpulseColour(target,Definition(),Colors.Black,new(float.NaN,0,0)));
            Assert.Throws<ArgumentException>(()=>adapter.BindImpulseColour(target,Definition(),Colors.Black,new(1,1,1,.5f)));
            var pulse=adapter.BindImpulseColour(target,Definition(),Colors.Black,Colors.White);
            Assert.Throws<InvalidOperationException>(()=>adapter.BindImpulseColour(target,Definition(),Colors.Black,Colors.White));
            Assert.Throws<InvalidOperationException>(()=>adapter.BindColour(target,new(0,1,1,AnimationCurve.Linear,
                AnimationRepeat.Once,AnimationClock.Presentation),Colors.Black,Colors.White));
            adapter.EnqueueImpulse(pulse,new(1),1,0);adapter.RemoveAnimation(pulse);adapter.Present(1,0,0);
            Assert.Equal(Colors.Black,material.AlbedoColor);
            adapter.ClaimCommittedColour(target);
            Assert.Throws<InvalidOperationException>(()=>adapter.BindImpulseColour(target,Definition(),Colors.Black,Colors.White));
        }
        finally{node.Free();}
    }

    [Fact]
    public void WarmedImpulseColourSubmissionAllocatesNothing()
    {
        using var material=new StandardMaterial3D {AlbedoColor=Colors.Black};
        var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(material);
        var pulse=adapter.BindImpulseColour(target,Definition(),Colors.Black,Colors.White);
        ulong frame=0,id=0;double time=0;
        void Cycle()
        {
            adapter.EnqueueImpulse(pulse,new(++id),1,0);adapter.Present(++frame,time,0);
            time+=.25;adapter.Present(++frame,time,0);
        }
        for(var i=0;i<100;i++)Cycle();
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<1000;i++)Cycle();
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
        Assert.Equal(1100UL,adapter.ReadImpulses(pulse).Completed);
        adapter.Reset();Assert.Equal(Colors.Black,material.AlbedoColor);
    }
}
