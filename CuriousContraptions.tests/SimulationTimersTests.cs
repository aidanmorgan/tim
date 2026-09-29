using CuriousContraptions;

namespace CuriousContraptions.Tests;

public class SimulationTimersTests
{
    [Fact]
    public void ExactDeadlineRetriggerAndRetainedSnapshotReplay()
    {
        var id=new SimulationTimerId(4);var world=new SimulationTimers([new(id,12)]);
        Assert.Equal(SimulationTimerPhase.Ready,world.Read(id).Phase);
        world.Trigger(id,3);var before=world.Capture();
        Assert.Empty(world.Advance(14).ToArray());Assert.Equal(11d/12,world.Progress(id));
        world.Trigger(id,14);Assert.Equal(15,world.Read(id).DueTick);
        Assert.Equal(new[]{new SimulationTimerElapsed(id,15)},world.Advance(15).ToArray());
        Assert.Equal(SimulationTimerPhase.Finished,world.Read(id).Phase);Assert.Equal(1,world.Progress(id));
        Assert.Empty(world.Advance(90).ToArray());world.Trigger(id,90);
        Assert.Equal(15,world.Read(id).DueTick);
        world.Restore(before);
        Assert.Equal(0,world.Tick);Assert.Equal(SimulationTimerPhase.Counting,world.Read(id).Phase);
        Assert.Equal(new[]{new SimulationTimerElapsed(id,15)},world.Advance(15).ToArray());
    }

    [Fact]
    public void SkippedTicksDeliverEveryDeadlineInIdentityOrder()
    {
        var a=new SimulationTimerId(9);var b=new SimulationTimerId(2);
        var world=new SimulationTimers([new(a,2),new(b,4)]);
        world.Trigger(a,0);world.Trigger(b,0);
        Assert.Equal(new[]{new SimulationTimerElapsed(b,4),new SimulationTimerElapsed(a,2)},world.Advance(20).ToArray());
        Assert.Empty(world.Advance(20).ToArray());
    }

    [Fact]
    public void InvalidDeclarationClockOwnershipAndOverflowRejectWithoutMutation()
    {
        var id=new SimulationTimerId(0);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new SimulationTimerId(-1));
        Assert.Throws<ArgumentException>(()=>new SimulationTimers([new(id,0)]));
        Assert.Throws<ArgumentException>(()=>new SimulationTimers([new(id,1),new(id,2)]));
        var world=new SimulationTimers([new(id,12)]);var other=new SimulationTimers([new(id,12)]);
        Assert.Throws<ArgumentException>(()=>world.Read(new(3)));
        Assert.Throws<ArgumentException>(()=>world.Restore(other.Capture()));
        Assert.Throws<OverflowException>(()=>world.Trigger(id,int.MaxValue));
        Assert.Equal(SimulationTimerPhase.Ready,world.Read(id).Phase);
        world.Trigger(id,3);
        Assert.Throws<ArgumentException>(()=>world.Advance(2));
        Assert.Equal(0,world.Tick);Assert.Equal(SimulationTimerPhase.Counting,world.Read(id).Phase);
        world.Advance(5);
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Advance(4));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Trigger(id,4));
        Assert.Equal(5,world.Tick);
    }

    [Fact]
    public void SteadyTimerAdvanceAllocatesNoRoutineScratch()
    {
        var world=new SimulationTimers([new(new(0),10000)]);
        world.Trigger(new(0),0);
        for(var tick=0;tick<100;tick++)world.Advance(tick);
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var tick=100;tick<200;tick++)world.Advance(tick);
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
    }

}
