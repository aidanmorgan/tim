namespace CuriousContraptions.Tests;

public class SimulationOscillatorsTests
{
    [Fact]
    public void PowerRestorationStartsFullIntervalAndLossAtDeadlineSuppressesPulse()
    {
        var id=new SimulationOscillatorId(3);var world=new SimulationOscillators([new(id,2)]);
        SimulationOscillatorInput[] on=[new(id,true)],off=[new(id,false)];
        Assert.Empty(world.Advance(0,on).ToArray());Assert.Equal(2,world.Read(id).DueTick);
        Assert.Empty(world.Advance(1,on).ToArray());Assert.Equal(.5,world.Progress(id));
        Assert.Empty(world.Advance(2,off).ToArray());Assert.Equal(0,world.Progress(id));
        Assert.Empty(world.Advance(3,off).ToArray());
        Assert.Empty(world.Advance(4,on).ToArray());Assert.Equal(6,world.Read(id).DueTick);
        Assert.Empty(world.Advance(5,on).ToArray());
        Assert.Equal(new[]{new SimulationOscillatorPulse(id,6,1)},world.Advance(6,on).ToArray());
        Assert.Equal(0,world.Progress(id));Assert.Equal(8,world.Read(id).DueTick);
        Assert.Empty(world.Advance(7,on).ToArray());
        Assert.Equal(new[]{new SimulationOscillatorPulse(id,8,2)},world.Advance(8,on).ToArray());
        Assert.Equal(2,world.Read(id).PulseCount);Assert.Equal(8,world.Read(id).LastPulseTick);
    }

    [Fact]
    public void SimultaneousPulsesUseIdentityOrderAndSnapshotsReplayExactly()
    {
        var a=new SimulationOscillatorId(7);var b=new SimulationOscillatorId(2);
        var world=new SimulationOscillators([new(a,2),new(b,1)]);
        SimulationOscillatorInput[] inputs=[new(b,true),new(a,true)];
        world.Advance(0,inputs);world.Advance(1,inputs);var snapshot=world.Capture();
        var expected=world.Advance(2,inputs).ToArray();
        Assert.Equal(new[]{new SimulationOscillatorPulse(b,2,2),new SimulationOscillatorPulse(a,2,1)},expected);
        world.Advance(3,inputs);world.Restore(snapshot);
        Assert.Equal(1,world.Tick);Assert.Equal(expected,world.Advance(2,inputs).ToArray());
    }

    [Fact]
    public void InvalidInputsAndOverflowAreAtomicAcrossAllOscillators()
    {
        var a=new SimulationOscillatorId(0);var b=new SimulationOscillatorId(1);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new SimulationOscillatorId(-1));
        Assert.Throws<ArgumentException>(()=>new SimulationOscillators([new(a,0)]));
        Assert.Throws<ArgumentException>(()=>new SimulationOscillators([new(a,1),new(a,2)]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new SimulationOscillators([],-1));
        var world=new SimulationOscillators([new(a,1),new(b,2)],int.MaxValue-1);
        SimulationOscillatorInput[] inputs=[new(a,true),new(b,true)];
        Assert.Throws<OverflowException>(()=>world.Advance(int.MaxValue-1,inputs));
        Assert.Equal(int.MaxValue-2,world.Tick);
        Assert.Equal(SimulationOscillatorPhase.Stopped,world.Read(a).Phase);
        Assert.Equal(SimulationOscillatorPhase.Stopped,world.Read(b).Phase);
        Assert.Throws<ArgumentException>(()=>world.Advance(int.MaxValue-1,[new(a,true)]));
        Assert.Throws<ArgumentException>(()=>world.Advance(int.MaxValue-1,[new(b,true),new(a,true)]));
        Assert.Throws<ArgumentException>(()=>world.Read(new(99)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Advance(int.MaxValue,inputs));
        world.Advance(int.MaxValue-1,[new(a,true),new(b,false)]);
        Assert.Throws<ArgumentOutOfRangeException>(()=>world.Advance(int.MaxValue-1,inputs));
        Assert.Throws<OverflowException>(()=>world.Advance(int.MaxValue,[new(a,true),new(b,false)]));
        Assert.Equal(0,world.Read(a).PulseCount);Assert.Equal(int.MaxValue,world.Read(a).DueTick);
    }

    [Fact]
    public void FailedOuterTransactionRestoresPulseCountersAndCanRetry()
    {
        var id=new SimulationOscillatorId(0);var world=new SimulationOscillators([new(id,1)]);
        SimulationOscillatorInput[] inputs=[new(id,true)];
        world.Advance(0,inputs);var before=world.Read(id);var snapshot=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.CommitTransaction());
        Assert.Throws<InvalidOperationException>(()=>world.RollbackTransaction());
        world.BeginTransaction();
        Assert.Throws<InvalidOperationException>(()=>world.BeginTransaction());
        Assert.Throws<InvalidOperationException>(()=>world.Capture());
        Assert.Throws<InvalidOperationException>(()=>world.Restore(snapshot));
        var first=world.Advance(1,inputs).ToArray();world.RollbackTransaction();
        Assert.Equal(0,world.Tick);Assert.Equal(before,world.Read(id));
        world.BeginTransaction();Assert.Equal(first,world.Advance(1,inputs).ToArray());world.CommitTransaction();
        Assert.Equal(1,world.Read(id).PulseCount);
        Assert.Throws<ArgumentException>(()=>world.Restore(new SimulationOscillators([new(id,1)]).Capture()));
    }

    [Fact]
    public void StableAdvanceAndRollbackAllocateNoRoutineStorage()
    {
        var id=new SimulationOscillatorId(0);var world=new SimulationOscillators([new(id,1)]);
        SimulationOscillatorInput[] inputs=[new(id,true)];
        world.Advance(0,inputs);
        for(var i=0;i<100;i++){world.BeginTransaction();world.Advance(1,inputs);world.RollbackTransaction();}
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<100;i++){world.BeginTransaction();world.Advance(1,inputs);world.RollbackTransaction();}
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
    }
}
