namespace CuriousContraptions.Tests;

public class SimulationTransactionTests
{
    private enum Failure { None, Capture, Restore }
    private enum Owner { First, Second, Third }
    private enum Operation { Capture, Restore }
    private readonly record struct Call(Owner Owner, Operation Operation);
    private sealed class Participant(Owner owner, List<Call>? calls = null) : SimulationTransactionParticipant
    {
        public int Value;
        public Failure Failure;
        private int _checkpoint;
        protected override void CaptureCheckpoint()
        {
            calls?.Add(new(owner, Operation.Capture));
            if (Failure == Failure.Capture) throw new InvalidOperationException();
            _checkpoint = Value;
        }
        protected override void RestoreCheckpoint()
        {
            calls?.Add(new(owner, Operation.Restore));
            if (Failure == Failure.Restore) throw new InvalidOperationException();
            Value = _checkpoint;
        }
    }

    [Fact]
    public void RollbackRestoresReverseOrderAndCommitPreservesState()
    {
        var calls = new List<Call>();
        var a = new Participant(Owner.First,calls) { Value = 1 };
        var b = new Participant(Owner.Second,calls) { Value = 2 };
        var transaction = new SimulationTransaction([a,b]);
        transaction.Begin(); a.Value = 10; b.Value = 20; transaction.Rollback();
        Assert.Equal(1,a.Value); Assert.Equal(2,b.Value);
        Assert.Equal(new Call[] { new(Owner.First,Operation.Capture),new(Owner.Second,Operation.Capture),
            new(Owner.Second,Operation.Restore),new(Owner.First,Operation.Restore) },calls);
        transaction.Begin(); a.Value = 11; b.Value = 21; transaction.Commit();
        Assert.Equal(11,a.Value); Assert.Equal(21,b.Value);
        Assert.Equal(SimulationTransactionPhase.Idle,transaction.Phase);
    }

    [Fact]
    public void FailedCaptureUnwindsOnlyAdmittedParticipantsAndCanRetry()
    {
        var calls = new List<Call>();
        var a = new Participant(Owner.First,calls);
        var b = new Participant(Owner.Second,calls) { Failure = Failure.Capture };
        var c = new Participant(Owner.Third,calls);
        var transaction = new SimulationTransaction([a,b,c]);
        Assert.Throws<InvalidOperationException>(()=>transaction.Begin());
        Assert.Equal(new Call[] {new(Owner.First,Operation.Capture),new(Owner.Second,Operation.Capture),
            new(Owner.First,Operation.Restore)},calls);
        Assert.Equal(SimulationTransactionPhase.Idle,transaction.Phase);
        b.Failure = Failure.None;
        transaction.Begin(); transaction.Commit();
    }

    [Fact]
    public void FailedRestoreStillRestoresOtherParticipantsAndFaultsCoordinator()
    {
        var a = new Participant(Owner.First) { Value = 1 };
        var b = new Participant(Owner.Second) { Value = 2, Failure = Failure.Restore };
        var c = new Participant(Owner.Third) { Value = 3, Failure = Failure.Restore };
        var transaction = new SimulationTransaction([a,b,c]);
        transaction.Begin(); a.Value = 10; b.Value = 20; c.Value = 30;
        Assert.Equal(2,Assert.Throws<AggregateException>(()=>transaction.Rollback()).InnerExceptions.Count);
        Assert.Equal(1,a.Value);
        Assert.Equal(SimulationTransactionPhase.Faulted,transaction.Phase);
        Assert.Equal(SimulationTransactionPhase.Faulted,b.TransactionPhase);
        Assert.Throws<InvalidOperationException>(()=>transaction.Begin());
        Assert.Throws<InvalidOperationException>(()=>b.BeginTransaction());
    }

    [Fact]
    public void CaptureAndRestoreFailuresAreBothRetained()
    {
        var a = new Participant(Owner.First) { Failure = Failure.Restore };
        var b = new Participant(Owner.Second) { Failure = Failure.Capture };
        var transaction = new SimulationTransaction([a,b]);
        Assert.Equal(2,Assert.Throws<AggregateException>(()=>transaction.Begin()).InnerExceptions.Count);
        Assert.Equal(SimulationTransactionPhase.Faulted,transaction.Phase);
    }

    [Fact]
    public void OwnershipAndAllParticipantPreflightRejectInterference()
    {
        var a = new Participant(Owner.First);
        var b = new Participant(Owner.Second);
        var transaction = new SimulationTransaction([a,b]);
        b.BeginTransaction();
        Assert.Throws<InvalidOperationException>(()=>transaction.Begin());
        Assert.Equal(SimulationTransactionPhase.Idle,a.TransactionPhase);
        b.RollbackTransaction();
        transaction.Begin();
        Assert.Throws<InvalidOperationException>(()=>a.CommitTransaction());
        Assert.Throws<InvalidOperationException>(()=>a.RollbackTransaction());
        Assert.Throws<InvalidOperationException>(()=>a.BeginTransaction());
        Assert.Throws<InvalidOperationException>(()=>transaction.Begin());
        Assert.Throws<InvalidOperationException>(()=>new SimulationTransaction([a]).Begin());
        transaction.Rollback();
        Assert.Throws<InvalidOperationException>(()=>transaction.Commit());
        Assert.Throws<InvalidOperationException>(()=>transaction.Rollback());
        Assert.Throws<ArgumentException>(()=>new SimulationTransaction([a,a]));
        Assert.Throws<ArgumentNullException>(()=>new SimulationTransaction([null!]));
    }

    [Fact]
    public void RealControllersRestoreClocksRequestsCountsAndReplayTogether()
    {
        var timers = new SimulationTimers([new(new(0),1,TimerCompletionPolicy.Latch,TimerBoundary.BeforePhysics)]);
        var oscillators = new SimulationOscillators([new(new(0),1)]);
        var counters = new SimulationCounters([new(new(0),2)]);
        var latches = new SimulationLatches([new(0)]);
        var transaction = new SimulationTransaction([timers,oscillators,counters,latches]);
        var timer = timers.Read(new(0)); var oscillator = oscillators.Read(new(0));
        var counter = counters.Read(new(0)); var latch = latches.Read(new(0));
        void Advance()
        {
            timers.Trigger(new(0),0); timers.Advance(1,TimerBoundary.BeforePhysics);
            oscillators.Advance(0,[new(new(0),true)]); oscillators.Advance(1,[new(new(0),true)]);
            counters.Increment(new(0)); latches.Submit(new(0),SimulationLatchCommand.Set,0);
            latches.Advance(0); latches.Advance(1);
        }
        transaction.Begin(); Advance(); transaction.Rollback();
        Assert.Equal(timer,timers.Read(new(0))); Assert.Equal(oscillator,oscillators.Read(new(0)));
        Assert.Equal(counter,counters.Read(new(0))); Assert.Equal(latch,latches.Read(new(0)));
        transaction.Begin(); Advance(); transaction.Commit();
        Assert.Equal(SimulationTimerPhase.Finished,timers.Read(new(0)).Phase);
        Assert.Equal(1,oscillators.Read(new(0)).PulseCount);
        Assert.Equal(1,counters.Read(new(0)).Count);
        Assert.Equal(SimulationLatchPhase.On,latches.Read(new(0)).Phase);
    }

    private readonly record struct Memory(Owner Owner,int Count);

    [Fact]
    public void TypedValueCellsRestoreAndCommitWithoutAliasedStateOrAllocation()
    {
        var initial=new Memory(Owner.First,3);
        var state=new SimulationState<Memory>(initial);
        var transaction=new SimulationTransaction([state]);
        transaction.Begin();
        state.Value=new(Owner.Second,7);
        transaction.Rollback();
        Assert.Equal(initial,state.Value);
        transaction.Begin();
        state.Value=new(Owner.Third,9);
        transaction.Commit();
        Assert.Equal(new Memory(Owner.Third,9),state.Value);
        for(var i=0;i<1000;i++){transaction.Begin();state.Value=initial;transaction.Rollback();}
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<2000;i++){transaction.Begin();state.Value=initial;transaction.Rollback();}
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.Equal(0,allocated);
        Assert.Equal(new Memory(Owner.Third,9),state.Value);
    }

    [Fact]
    public void WarmedCompositionAllocatesNoManagedMemory()
    {
        var a = new Participant(Owner.First);
        var b = new Participant(Owner.Second);
        var transaction = new SimulationTransaction([a,b]);
        for(var i=0;i<1000;i++) {transaction.Begin();a.Value++;transaction.Rollback();transaction.Begin();transaction.Commit();}
        var before = GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<2000;i++) {transaction.Begin();a.Value++;transaction.Rollback();transaction.Begin();transaction.Commit();}
        var allocated = GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.Equal(0,allocated); Assert.Equal(0,a.Value);
    }
}
