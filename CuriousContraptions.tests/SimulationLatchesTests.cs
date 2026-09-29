namespace CuriousContraptions.Tests;

public class SimulationLatchesTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameTickResetWinsBothDeliveryOrdersAndBoundaries(bool reverse)
    {
        var id=new SimulationLatchId(4);var world=new SimulationLatches([id]);
        world.Submit(id,SimulationLatchCommand.Set,0);
        world.Advance(0);Assert.Equal(SimulationLatchPhase.Off,world.Read(id).Phase);
        world.Advance(1);Assert.Equal(SimulationLatchPhase.On,world.Read(id).Phase);
        var first=reverse?SimulationLatchCommand.Reset:SimulationLatchCommand.Set;
        var second=reverse?SimulationLatchCommand.Set:SimulationLatchCommand.Reset;
        world.Submit(id,first,2); // Before boundary two.
        world.Advance(2);Assert.Equal(SimulationLatchPhase.On,world.Read(id).Phase);
        world.Submit(id,second,2); // After boundary two, same delivery tick.
        Assert.Equal(SimulationLatchRequests.Set|SimulationLatchRequests.Reset,world.Read(id).CurrentRequests);
        world.Advance(3);Assert.Equal(SimulationLatchPhase.Off,world.Read(id).Phase);
        for(var tick=4;tick<10;tick++)world.Advance(tick);
        Assert.Equal(SimulationLatchPhase.Off,world.Read(id).Phase);
    }

    [Fact]
    public void AdjacentTicksStaySeparateAndMemoryPersistsWithoutNewCommands()
    {
        var a=new SimulationLatchId(0);var b=new SimulationLatchId(8);
        var world=new SimulationLatches([b,a]);
        world.Advance(0);
        world.Submit(a,SimulationLatchCommand.Set,0);
        world.Submit(a,SimulationLatchCommand.Set,0);
        world.Submit(a,SimulationLatchCommand.Reset,1);
        world.Submit(b,SimulationLatchCommand.Set,1);
        world.Advance(1);
        Assert.Equal(SimulationLatchPhase.On,world.Read(a).Phase);
        Assert.Equal(SimulationLatchPhase.Off,world.Read(b).Phase);
        world.Advance(2);
        Assert.Equal(SimulationLatchPhase.Off,world.Read(a).Phase);
        Assert.Equal(SimulationLatchPhase.On,world.Read(b).Phase);
        for(var tick=3;tick<100;tick++)world.Advance(tick);
        Assert.Equal(SimulationLatchPhase.On,world.Read(b).Phase);
    }

    [Fact]
    public void SnapshotAndTransactionRestoreBothBucketsClockAndSettledState()
    {
        var id=new SimulationLatchId(0);var world=new SimulationLatches([id]);
        world.Advance(0);world.Submit(id,SimulationLatchCommand.Set,0);
        world.Submit(id,SimulationLatchCommand.Reset,1);
        var saved=world.Capture();var state=world.Read(id);
        world.BeginTransaction();world.Advance(1);
        Assert.Equal(SimulationLatchPhase.On,world.Read(id).Phase);
        world.Submit(id,SimulationLatchCommand.Set,2);
        Assert.Throws<InvalidOperationException>(()=>world.BeginTransaction());
        Assert.Throws<InvalidOperationException>(()=>world.Capture());
        Assert.Throws<InvalidOperationException>(()=>world.Restore(saved));
        world.Advance(2);world.RollbackTransaction();
        Assert.Equal(0,world.Tick);Assert.Equal(state,world.Read(id));
        world.BeginTransaction();world.Advance(1);world.CommitTransaction();
        Assert.Equal(SimulationLatchPhase.On,world.Read(id).Phase);
        world.Advance(2);Assert.Equal(SimulationLatchPhase.Off,world.Read(id).Phase);
        world.Restore(saved);Assert.Equal(0,world.Tick);Assert.Equal(state,world.Read(id));
        world.Advance(1);Assert.Equal(SimulationLatchPhase.On,world.Read(id).Phase);
        world.Advance(2);Assert.Equal(SimulationLatchPhase.Off,world.Read(id).Phase);
    }

    [Fact]
    public void InvalidInputsRejectWithoutChangingAnyState()
    {
        var id=new SimulationLatchId(0);var world=new SimulationLatches([id]);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new SimulationLatchId(-1));
        Assert.Throws<ArgumentException>(()=>new SimulationLatches([id,id]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new SimulationLatches([id],-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Submit(id,(SimulationLatchCommand)99,0));
        Assert.Throws<ArgumentException>(()=>world.Submit(new(8),SimulationLatchCommand.Set,0));
        Assert.Throws<ArgumentException>(()=>world.Read(new(8)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Submit(id,SimulationLatchCommand.Set,-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Submit(id,SimulationLatchCommand.Set,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Advance(1));
        Assert.Equal(-1,world.Tick);
        world.Advance(0);world.Advance(1);
        var before=world.Read(id);
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Submit(id,SimulationLatchCommand.Set,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Advance(1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Advance(0));
        Assert.Throws<InvalidOperationException>(()=>world.CommitTransaction());
        Assert.Throws<InvalidOperationException>(()=>world.RollbackTransaction());
        Assert.Throws<ArgumentException>(()=>world.Restore(new SimulationLatches([id]).Capture()));
        Assert.Equal(before,world.Read(id));Assert.Equal(1,world.Tick);
    }

    [Fact]
    public void LastSupportedBoundaryRejectsOverflowAndReplays()
    {
        var id=new SimulationLatchId(0);var world=new SimulationLatches([id],long.MaxValue-1);
        world.Submit(id,SimulationLatchCommand.Set,long.MaxValue-1);
        world.Advance(long.MaxValue-1);world.Advance(long.MaxValue);
        Assert.Equal(SimulationLatchPhase.On,world.Read(id).Phase);
        var state=world.Read(id);
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Advance(long.MinValue));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Submit(id,SimulationLatchCommand.Reset,long.MinValue));
        Assert.Equal(state,world.Read(id));Assert.Equal(long.MaxValue,world.Tick);
    }

    [Fact]
    public void RoutineCommandsBoundariesAndRollbackAllocateNoStorage()
    {
        var id=new SimulationLatchId(0);var world=new SimulationLatches([id]);world.Advance(0);
        for(var i=0;i<100;i++)
        {world.BeginTransaction();world.Submit(id,SimulationLatchCommand.Set,0);world.Advance(1);world.RollbackTransaction();}
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<100;i++)
        {world.BeginTransaction();world.Submit(id,SimulationLatchCommand.Set,0);world.Advance(1);world.RollbackTransaction();}
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
    }
}
