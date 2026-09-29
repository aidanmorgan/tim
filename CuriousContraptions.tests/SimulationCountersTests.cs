namespace CuriousContraptions.Tests;

public class SimulationCountersTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(9)]
    public void EveryDeliveryCountsOnceAndOnlyOneReachesTheThreshold(int target)
    {
        var id=new SimulationCounterId(2);var world=new SimulationCounters([new(id,target)]);
        for(var count=1;count<=target;count++)
        {
            Assert.Equal(count==target?SimulationCounterResult.Reached:SimulationCounterResult.Accumulated,world.Increment(id));
            Assert.Equal(count,world.Read(id).Count);
        }
        for(var i=0;i<20;i++)Assert.Equal(SimulationCounterResult.Saturated,world.Increment(id));
        Assert.Equal(new SimulationCounterState(id,target,target),world.Read(id));
        Assert.Equal(SimulationCounterPhase.Reached,world.Read(id).Phase);
    }

    [Fact]
    public void RetainedSnapshotsAndOuterRollbackReplayThresholdResults()
    {
        var a=new SimulationCounterId(0);var b=new SimulationCounterId(1);
        var world=new SimulationCounters([new(a,2),new(b,1)]);
        var empty=world.Capture();world.Increment(a);var partial=world.Capture();
        world.BeginTransaction();
        Assert.Equal(SimulationCounterResult.Reached,world.Increment(a));
        Assert.Equal(SimulationCounterResult.Reached,world.Increment(b));
        Assert.Throws<InvalidOperationException>(()=>world.BeginTransaction());
        Assert.Throws<InvalidOperationException>(()=>world.Capture());
        Assert.Throws<InvalidOperationException>(()=>world.Restore(empty));
        world.RollbackTransaction();
        Assert.Equal(1,world.Read(a).Count);Assert.Equal(0,world.Read(b).Count);
        world.BeginTransaction();
        Assert.Equal(SimulationCounterResult.Reached,world.Increment(a));world.CommitTransaction();
        world.Restore(partial);Assert.Equal(SimulationCounterResult.Reached,world.Increment(a));
        world.Restore(empty);Assert.Equal(0,world.Read(a).Count);Assert.Equal(0,world.Read(b).Count);
        Assert.Equal(SimulationCounterPhase.Counting,world.Read(a).Phase);
    }

    [Fact]
    public void InvalidDeclarationsIdentityAndLifecycleRejectWithoutMutation()
    {
        var id=new SimulationCounterId(0);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new SimulationCounterId(-1));
        Assert.Throws<ArgumentException>(()=>new SimulationCounters([new(id,0)]));
        Assert.Throws<ArgumentException>(()=>new SimulationCounters([new(id,1),new(id,2)]));
        var world=new SimulationCounters([new(id,int.MaxValue)]);
        Assert.Throws<ArgumentException>(()=>world.Increment(new(1)));
        Assert.Throws<ArgumentException>(()=>world.Read(new(1)));
        Assert.Throws<InvalidOperationException>(()=>world.CommitTransaction());
        Assert.Throws<InvalidOperationException>(()=>world.RollbackTransaction());
        Assert.Throws<ArgumentException>(()=>world.Restore(new SimulationCounters([new(id,1)]).Capture()));
        Assert.Equal(SimulationCounterResult.Accumulated,world.Increment(id));
        Assert.Equal(new SimulationCounterState(id,1,int.MaxValue),world.Read(id));
    }

    [Fact]
    public void RoutineDeliveriesAndRollbackReuseStorage()
    {
        var id=new SimulationCounterId(0);var world=new SimulationCounters([new(id,2)]);
        for(var i=0;i<100;i++){world.BeginTransaction();world.Increment(id);world.Increment(id);world.RollbackTransaction();}
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<100;i++){world.BeginTransaction();world.Increment(id);world.Increment(id);world.RollbackTransaction();}
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
    }
}
