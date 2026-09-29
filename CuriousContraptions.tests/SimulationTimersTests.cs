using CuriousContraptions;

namespace CuriousContraptions.Tests;

public class SimulationTimersTests
{
    private static SimulationTimerDeclaration OneShot(SimulationTimerId id,int duration)=>
        new(id,duration,TimerCompletionPolicy.Latch,TimerBoundary.BeforePhysics);
    [Fact]
    public void FailedTransactionRestoresDeadlinesAndBoundaryForExactRetry()
    {
        var id=new SimulationTimerId(0);
        var world=new SimulationTimers([OneShot(id,1)]);
        world.Trigger(id,0);var snapshot=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.CommitTransaction());
        Assert.Throws<InvalidOperationException>(()=>world.RollbackTransaction());
        world.BeginTransaction();
        Assert.Throws<InvalidOperationException>(()=>world.BeginTransaction());
        Assert.Throws<InvalidOperationException>(()=>world.Capture());
        Assert.Throws<InvalidOperationException>(()=>world.Restore(snapshot));
        world.Advance(1,TimerBoundary.BeforeNetworks);
        var first=world.Advance(1,TimerBoundary.BeforePhysics).ToArray();
        world.RollbackTransaction();
        Assert.Equal(TimerTransactionPhase.Idle,world.TransactionPhase);
        Assert.Equal(0,world.Tick);Assert.Null(world.Boundary);
        Assert.Equal(new SimulationTimerState(id,SimulationTimerPhase.Counting,0,1),world.Read(id));
        world.BeginTransaction();
        world.Advance(1,TimerBoundary.BeforeNetworks);
        Assert.Equal(first,world.Advance(1,TimerBoundary.BeforePhysics).ToArray());
        world.CommitTransaction();
        Assert.Equal(SimulationTimerPhase.Finished,world.Read(id).Phase);
        Assert.Equal(TimerBoundary.BeforePhysics,world.Capture().Boundary);
    }

    [Fact]
    public void TransactionCheckpointReusesOwnedStorage()
    {
        var world=new SimulationTimers([OneShot(new(0),10000)]);
        world.Trigger(new(0),0);
        for(var i=0;i<100;i++){world.BeginTransaction();world.RollbackTransaction();}
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<100;i++)
        {
            world.BeginTransaction();world.Advance(1,TimerBoundary.BeforePhysics);
            world.RollbackTransaction();
        }
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
    }

    [Fact]
    public void RearmingWindowExpiresBeforeDelayedOccurrenceAndRestoresBoundary()
    {
        var hold=new SimulationTimerId(0);var delay=new SimulationTimerId(1);
        var world=new SimulationTimers([
            new(hold,3,TimerCompletionPolicy.Rearm,TimerBoundary.BeforeNetworks),OneShot(delay,3)]);
        Assert.True(world.Trigger(hold,0));Assert.True(world.Trigger(delay,0));
        Assert.False(world.Trigger(hold,1));
        Assert.Equal(new[]{new SimulationTimerElapsed(hold,3)},world.Advance(3,TimerBoundary.BeforeNetworks).ToArray());
        Assert.Equal(SimulationTimerPhase.Ready,world.Read(hold).Phase);
        Assert.Equal(SimulationTimerPhase.Counting,world.Read(delay).Phase);
        Assert.True(world.Trigger(hold,3));Assert.Equal(6,world.Read(hold).DueTick);
        var before=world.Capture();
        var elapsed=world.Advance(3,TimerBoundary.BeforePhysics).ToArray();
        Assert.Equal(new[]{new SimulationTimerElapsed(delay,3)},elapsed);
        Assert.False(world.Trigger(delay,3));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Advance(3,TimerBoundary.BeforeNetworks));
        world.Restore(before);
        Assert.Equal(TimerBoundary.BeforeNetworks,world.Boundary);
        Assert.Equal(elapsed,world.Advance(3,TimerBoundary.BeforePhysics).ToArray());
        Assert.Equal(new[]{new SimulationTimerElapsed(hold,6)},world.Advance(6,TimerBoundary.BeforeNetworks).ToArray());
    }

    [Fact]
    public void UnknownPoliciesAndBoundaryRejectExplicitly()
    {
        var id=new SimulationTimerId(0);
        Assert.Throws<ArgumentException>(()=>new SimulationTimers([new(id,1,(TimerCompletionPolicy)99,TimerBoundary.BeforePhysics)]));
        Assert.Throws<ArgumentException>(()=>new SimulationTimers([new(id,1,TimerCompletionPolicy.Latch,(TimerBoundary)99)]));
        var world=new SimulationTimers([OneShot(id,1)]);world.Trigger(id,0);
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Advance(1,(TimerBoundary)99));
        Assert.Equal(0,world.Tick);Assert.Null(world.Boundary);
        Assert.Equal(SimulationTimerPhase.Counting,world.Read(id).Phase);
    }

    [Fact]
    public void ExactDeadlineRetriggerAndRetainedSnapshotReplay()
    {
        var id=new SimulationTimerId(4);var world=new SimulationTimers([OneShot(id,12)]);
        Assert.Equal(SimulationTimerPhase.Ready,world.Read(id).Phase);
        world.Trigger(id,3);var before=world.Capture();
        Assert.Empty(world.Advance(14,TimerBoundary.BeforePhysics).ToArray());Assert.Equal(11d/12,world.Progress(id));
        world.Trigger(id,14);Assert.Equal(15,world.Read(id).DueTick);
        Assert.Equal(new[]{new SimulationTimerElapsed(id,15)},world.Advance(15,TimerBoundary.BeforePhysics).ToArray());
        Assert.Equal(SimulationTimerPhase.Finished,world.Read(id).Phase);Assert.Equal(1,world.Progress(id));
        Assert.Empty(world.Advance(90,TimerBoundary.BeforePhysics).ToArray());world.Trigger(id,90);
        Assert.Equal(15,world.Read(id).DueTick);
        world.Restore(before);
        Assert.Equal(0,world.Tick);Assert.Equal(SimulationTimerPhase.Counting,world.Read(id).Phase);
        Assert.Equal(new[]{new SimulationTimerElapsed(id,15)},world.Advance(15,TimerBoundary.BeforePhysics).ToArray());
    }

    [Fact]
    public void SkippedTicksDeliverEveryDeadlineInIdentityOrder()
    {
        var a=new SimulationTimerId(9);var b=new SimulationTimerId(2);
        var world=new SimulationTimers([OneShot(a,2),OneShot(b,4)]);
        world.Trigger(a,0);world.Trigger(b,0);
        Assert.Equal(new[]{new SimulationTimerElapsed(b,4),new SimulationTimerElapsed(a,2)},world.Advance(20,TimerBoundary.BeforePhysics).ToArray());
        Assert.Empty(world.Advance(20,TimerBoundary.BeforePhysics).ToArray());
    }

    [Fact]
    public void InvalidDeclarationClockOwnershipAndOverflowRejectWithoutMutation()
    {
        var id=new SimulationTimerId(0);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new SimulationTimerId(-1));
        Assert.Throws<ArgumentException>(()=>new SimulationTimers([OneShot(id,0)]));
        Assert.Throws<ArgumentException>(()=>new SimulationTimers([OneShot(id,1),OneShot(id,2)]));
        var world=new SimulationTimers([OneShot(id,12)]);var other=new SimulationTimers([OneShot(id,12)]);
        Assert.Throws<ArgumentException>(()=>world.Read(new(3)));
        Assert.Throws<ArgumentException>(()=>world.Restore(other.Capture()));
        Assert.Throws<OverflowException>(()=>world.Trigger(id,int.MaxValue));
        Assert.Equal(SimulationTimerPhase.Ready,world.Read(id).Phase);
        world.Trigger(id,3);
        Assert.Throws<ArgumentException>(()=>world.Advance(2,TimerBoundary.BeforePhysics));
        Assert.Equal(0,world.Tick);Assert.Equal(SimulationTimerPhase.Counting,world.Read(id).Phase);
        world.Advance(5,TimerBoundary.BeforePhysics);
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Advance(4,TimerBoundary.BeforePhysics));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Trigger(id,4));
        Assert.Equal(5,world.Tick);
    }

    [Fact]
    public void SteadyTimerAdvanceAllocatesNoRoutineScratch()
    {
        var world=new SimulationTimers([OneShot(new(0),10000)]);
        world.Trigger(new(0),0);
        for(var tick=0;tick<100;tick++)world.Advance(tick,TimerBoundary.BeforePhysics);
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var tick=100;tick<200;tick++)world.Advance(tick,TimerBoundary.BeforePhysics);
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
    }

}
