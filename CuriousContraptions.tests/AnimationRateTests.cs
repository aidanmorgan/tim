using CuriousContraptions.Presentation;
using Godot;

namespace CuriousContraptions.Tests;

public class AnimationRateTests
{
    private static AnimationBinding Binding(int index)=>new(new(index),AnimationProperty.LocalRotationAngle);
    private static AnimationDefinition Loop(AnimationCurve curve=AnimationCurve.Linear,AnimationClock clock=AnimationClock.Presentation)=>
        new(0,1,1,curve,AnimationRepeat.Loop,clock);
    public static IEnumerable<object[]> Cases()
    {
        foreach(var curve in Enum.GetValues<AnimationCurve>())
        foreach(var clock in Enum.GetValues<AnimationClock>())
        foreach(var hidden in new[]{false,true})
        foreach(var boundaryFrame in new[]{false,true})yield return [curve,clock,hidden,boundaryFrame];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void SignedRateChangesPreservePhaseAcrossClocksVisibilityAndSkippedFrames(
        AnimationCurve curve,AnimationClock clock,bool hidden,bool boundaryFrame)
    {
        var batch=new AnimationBatch(1);var handle=batch.Register(Binding(0),Loop(curve,clock));
        ulong frame=0;
        void Advance(double time)=>batch.Advance(++frame,clock==AnimationClock.Presentation?time:100,clock==AnimationClock.Simulation?time:100);
        batch.SetLoopRate(handle,2,0);
        batch.SetVisible(handle,!hidden);
        if(boundaryFrame)Advance(.25);
        batch.SetLoopRate(handle,-1,.25); // Half a cycle reached even without a frame.
        Advance(.5);batch.SetVisible(handle,true);Advance(.5);
        var expected=curve switch
        {
            AnimationCurve.Linear=>.25,
            AnimationCurve.SmoothStep=>.103515625,
            AnimationCurve.SinePulse=>Math.Sqrt(.5),
            _=>throw new ArgumentOutOfRangeException(nameof(curve))
        };
        Assert.InRange(Math.Abs(batch.Read(handle).Value-expected),0,1e-14);
        batch.SetLoopRate(handle,0,.5);Advance(1);
        Assert.Equal(AnimationPlayback.Stopped,batch.Read(handle).Playback);Assert.Equal(0,batch.ScheduledCount);
        Assert.InRange(Math.Abs(batch.Read(handle).Value-expected),0,1e-14);
        batch.SetLoopRate(handle,-1,1);Advance(1.5); // Negative wrap gives phase .75.
        var reverseExpected=curve==AnimationCurve.SinePulse?expected:1-expected;
        Assert.InRange(Math.Abs(batch.Read(handle).Value-reverseExpected),0,1e-14);
        batch.Stop(handle,AnimationStop.Hold,1.625); // phase .625
        batch.SetLoopRate(handle,2,2);Advance(2.1875);Assert.Equal(0,batch.Read(handle).Value);
        batch.StartAt(handle,3);Advance(3.25); // Explicit restart restores unit rate and zero phase.
        Assert.InRange(Math.Abs(batch.Read(handle).Value-expected),0,1e-14);
        batch.Stop(handle,AnimationStop.RestoreInitial,3.25);Advance(4);
        Assert.Equal(0,batch.Read(handle).Value);
        batch.Reset();Assert.Throws<ArgumentException>(()=>batch.SetLoopRate(handle,1,0));
    }

    [Fact]
    public void InvalidRateAndNumericLimitsRejectWithoutPartialFrameOrControlMutation()
    {
        var batch=new AnimationBatch(2);
        var first=batch.Register(Binding(0),Loop());var second=batch.Register(Binding(1),Loop());
        batch.SetLoopRate(first,1,0);batch.SetLoopRate(second,double.MaxValue,0);
        batch.Advance(1,0,0);var samples=batch.Samples.ToArray();var before=batch.Read(first);
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Advance(2,2,0));
        Assert.Equal(before,batch.Read(first));Assert.Equal(samples,batch.Samples.ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.SetLoopRate(second,1,2));
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.Stop(second,AnimationStop.Hold,2));
        Assert.Equal(samples,batch.Samples.ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.SetLoopRate(first,double.NaN,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.SetLoopRate(first,double.PositiveInfinity,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.SetLoopRate(first,1,-1));
        batch.SetLoopRate(second,0,0);batch.Advance(2,.25,0);
        Assert.Equal(.25,batch.Read(first).Value); // Failed frame did not consume frame ID or clock.
        batch.SetLoopRate(first,1,.5);
        Assert.Throws<ArgumentOutOfRangeException>(()=>batch.SetLoopRate(first,1,.4));
        Assert.Throws<ArgumentException>(()=>batch.Advance(3,.4,0));
        batch.Advance(3,.5,0);
        var once=new AnimationBatch(1);
        var clip=once.Register(Binding(0),new(0,1,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation));
        Assert.Throws<InvalidOperationException>(()=>once.SetLoopRate(clip,1,0));
        once.Remove(clip);
        clip=once.Register(Binding(0),new(0,1,1,AnimationCurve.Linear,AnimationRepeat.PingPong,AnimationClock.Presentation));
        Assert.Throws<InvalidOperationException>(()=>once.SetLoopRate(clip,1,0));
    }

    [Fact]
    public void LargeDurationWrapDoesNotOverflowPhaseAddition()
    {
        var batch=new AnimationBatch(1);var duration=double.MaxValue;
        var handle=batch.Register(Binding(0),new(0,1,duration,AnimationCurve.Linear,AnimationRepeat.Loop,AnimationClock.Presentation));
        batch.SetLoopRate(handle,1,0);
        batch.SetLoopRate(handle,2,duration*.75);
        batch.Advance(1,duration,0);
        Assert.InRange(batch.Read(handle).Value,.24999999999999,.25000000000001);
    }

    [Fact]
    public void WarmedRateControlsAndFramesAllocateNothing()
    {
        var batch=new AnimationBatch(1);var handle=batch.Register(Binding(0),Loop());
        for(var i=0;i<100;i++){batch.SetLoopRate(handle,i%2==0?2:-1,i);batch.Advance((ulong)i+1,i+.5,0);}
        var bytes=GC.GetAllocatedBytesForCurrentThread();
        for(var i=100;i<1100;i++){batch.SetLoopRate(handle,i%2==0?2:-1,i);batch.Advance((ulong)i+1,i+.5,0);}
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-bytes);
    }
}

[Collection<NativeSceneCollection>]
public class SceneAnimationRateTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(AnimationRotationAxis.X)]
    [InlineData(AnimationRotationAxis.Y)]
    [InlineData(AnimationRotationAxis.Z)]
    public void AdapterAppliesRateChangesOnlyOnFramesAndResetRestoresAuthoredPose(AnimationRotationAxis axis)
    {
        var node=new Node3D();godot.Tree.Root.AddChild(node);
        try
        {
            node.Transform=new(new Basis(Vector3.Up,.3f),new(2,3,4));var baseline=node.Transform;
            var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
            var handle=adapter.BindRotation(target,new(0,Math.Tau,1,AnimationCurve.Linear,AnimationRepeat.Loop,AnimationClock.Presentation),axis);
            adapter.SetLoopRate(handle,2,0);adapter.SetLoopRate(handle,-1,.25);
            Assert.Equal(baseline,node.Transform);
            adapter.Present(1,.5,0);
            var direction=axis switch
            {
                AnimationRotationAxis.X=>Vector3.Right,AnimationRotationAxis.Y=>Vector3.Up,AnimationRotationAxis.Z=>Vector3.Back,
                _=>throw new ArgumentOutOfRangeException(nameof(axis))
            };
            var expected=baseline.Basis*new Basis(direction,(float)(Math.Tau*.25));
            Assert.InRange((expected.X-node.Basis.X).Length(),0,1e-6f);
            Assert.InRange((expected.Y-node.Basis.Y).Length(),0,1e-6f);
            Assert.Equal(baseline.Origin,node.Position);
            adapter.SetLoopRate(handle,0,.5);Assert.Equal(0,adapter.Present(2,1,0).TransformWrites);
            adapter.Reset();Assert.Equal(baseline,node.Transform);
            Assert.Throws<ArgumentException>(()=>adapter.SetLoopRate(handle,1,0));
        }
        finally{node.Free();}
    }
}
