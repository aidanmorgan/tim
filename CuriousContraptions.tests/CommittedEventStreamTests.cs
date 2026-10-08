using CuriousContraptions.Bridge;
namespace CuriousContraptions.Tests;

public class CommittedEventStreamTests
{
    private static readonly EventStreamId Stream=new(0);
    private enum Signal { Impact, Trigger }
    private readonly record struct SourceId(int Value);
    private readonly record struct Payload(Signal Signal,SourceId Source);
    private static PoseReadStamp Stamp(long revision,long generation=1)=>new(new(generation),new(revision),revision*.01);

    [Fact]
    public void StagingRollbackAndAcknowledgementPreserveEveryOccurrence()
    {
        var stream=new CommittedEventStream<Payload>(Stream,Stamp(0),3);
        stream.Begin(Stamp(1));stream.Append(new(Signal.Impact,new(2)));stream.Seal();
        Assert.Throws<InvalidOperationException>(()=>stream.Peek());
        stream.Discard();
        Assert.Equal(Stamp(0),stream.CurrentStamp);
        stream.Begin(Stamp(1));
        stream.Append(new(Signal.Impact,new(2)));stream.Append(new(Signal.Impact,new(2)));stream.Append(new(Signal.Trigger,new(1)));
        stream.Seal();stream.Commit();
        Assert.Equal(3,stream.PendingCount);
        var first=stream.Peek();
        Assert.Equal(Stamp(1),first.Stamp);
        Assert.Equal(new EventSequence(1),first.Id.Sequence);
        Assert.Equal(new Payload(Signal.Impact,new(2)),first.Payload);
        stream.RequireWritable();
        Assert.Throws<InvalidOperationException>(stream.Discard);
        stream.Begin(Stamp(2));
        Assert.Throws<InvalidOperationException>(()=>stream.Append(new(Signal.Trigger,new(9))));
        stream.Discard();
        Assert.Equal(first,stream.Peek()); // Read alone cannot acknowledge delivery.
        Assert.Throws<ArgumentException>(()=>stream.Acknowledge(new(Stream,new(2),first.Id.Sequence)));
        stream.Acknowledge(first.Id);
        var second=stream.Peek();
        Assert.Equal(first.Payload,second.Payload);
        Assert.Equal(new EventSequence(2),second.Id.Sequence);
        Assert.Throws<ArgumentException>(()=>stream.Acknowledge(first.Id));
        Assert.Equal(second,stream.Peek());
        stream.Acknowledge(second.Id);
        var third=stream.Peek();Assert.Equal(Signal.Trigger,third.Payload.Signal);
        stream.Acknowledge(third.Id);
        Assert.Equal(0,stream.PendingCount);stream.RequireWritable();
        stream.Begin(Stamp(2));stream.Seal();stream.Commit();
        Assert.Equal(Stamp(2),stream.CurrentStamp);
        stream.Begin(Stamp(3));stream.Append(new(Signal.Trigger,new(3)));stream.Seal();stream.Commit();
        Assert.Equal(new EventSequence(4),stream.Peek().Id.Sequence);
        Assert.Equal(Stamp(1),first.Stamp);
    }

    [Fact]
    public void CapacityFailureCannotCommitOrOverwriteAndDiscardReleasesReservation()
    {
        var stream=new CommittedEventStream<Payload>(Stream,Stamp(0),1);
        stream.Begin(Stamp(1));stream.Append(new(Signal.Impact,new(1)));
        Assert.Throws<InvalidOperationException>(()=>stream.Append(new(Signal.Trigger,new(2))));
        Assert.Equal(Stamp(0),stream.CurrentStamp);
        Assert.Throws<InvalidOperationException>(()=>stream.Peek());
        Assert.Throws<InvalidOperationException>(stream.Commit);
        Assert.Throws<InvalidOperationException>(stream.Seal);
        stream.Discard();
        stream.Begin(Stamp(1));stream.Append(new(Signal.Trigger,new(3)));stream.Seal();stream.Commit();
        Assert.Equal(new Payload(Signal.Trigger,new(3)),stream.Peek().Payload);
        Assert.Equal(1,stream.HighWaterMark);
        Assert.Equal(new EventSequence(1),stream.Peek().Id.Sequence);
        stream.Remove();
        Assert.Equal(0,stream.PendingCount);
        Assert.Throws<InvalidOperationException>(()=>stream.Peek());
        Assert.Throws<InvalidOperationException>(stream.RequireWritable);
        Assert.Throws<InvalidOperationException>(stream.Remove);
    }

    [Fact]
    public void InvalidStampsAndLifecycleRejectBeforeMutation()
    {
        Assert.Throws<ArgumentException>(()=>new CommittedEventStream<Payload>(Stream,default,1));
        Assert.Throws<ArgumentException>(()=>new CommittedEventStream<Payload>(Stream,Stamp(1),1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new CommittedEventStream<Payload>(Stream,Stamp(0),0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new EventSequence(0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new EventStreamId(-1));
        var stream=new CommittedEventStream<Payload>(Stream,Stamp(0),1);
        foreach(var invalid in new[]{Stamp(2),Stamp(1,2),new PoseReadStamp(new(1),new(1),double.NaN),Stamp(0)})
            Assert.Throws<ArgumentException>(()=>stream.Begin(invalid));
        Assert.Equal(EventStreamPhase.Idle,stream.Phase);
        Assert.Throws<InvalidOperationException>(stream.Seal);
        Assert.Throws<InvalidOperationException>(stream.Commit);
        Assert.Throws<InvalidOperationException>(()=>stream.Append(default));
        stream.Begin(Stamp(1));stream.Seal();stream.Commit();
        Assert.Equal(Stamp(1),stream.CurrentStamp);
        Assert.Throws<ArgumentException>(()=>stream.Begin(Stamp(1)));
    }

    [Fact]
    public void WarmPublicationAndDeliveryAllocateNothing()
    {
        var stream=new CommittedEventStream<Payload>(Stream,Stamp(0),4);
        void Cycle(int revision)
        {
            stream.Begin(Stamp(revision));
            for(var i=0;i<4;i++)stream.Append(new(Signal.Impact,new(i)));
            stream.Seal();stream.Commit();
            while(stream.PendingCount>0)stream.Acknowledge(stream.Peek().Id);
        }
        for(var i=1;i<=100;i++)Cycle(i);
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=101;i<=1100;i++)Cycle(i);
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
        Assert.Equal(4,stream.HighWaterMark);
        Assert.Equal(Stamp(1100),stream.CurrentStamp);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(64)]
    public void RetainedTransactionsSurviveWrapEmptyCommitsAndPartialDiscard(int capacity)
    {
        var stream=new CommittedEventStream<Payload>(Stream,Stamp(0),capacity);
        var expected=new Queue<CommittedEvent<Payload>>();
        ulong sequence=0;
        long revision=0;
        for(var cycle=0;cycle<100;cycle++)
        {
            while(stream.AvailableCapacity>0)
            {
                var stamp=Stamp(++revision);
                var payload=new Payload(Signal.Trigger,new(cycle));
                stream.Begin(stamp);stream.Append(payload);stream.Seal();stream.Commit();
                expected.Enqueue(new(stamp,new(Stream,stamp.Generation,new(++sequence)),payload));
            }
            var first=stream.Peek();
            stream.Begin(Stamp(revision+1));stream.Seal();stream.Commit();revision++;
            Assert.Equal(first,stream.Peek()); // Empty commits cannot relabel old occurrences.
            stream.Begin(Stamp(revision+1));
            Assert.Throws<InvalidOperationException>(()=>stream.Append(new(Signal.Impact,new(99))));
            Assert.Equal(capacity,stream.PendingCount);Assert.Equal(0,stream.StagedCount);
            Assert.Throws<InvalidOperationException>(stream.Seal);
            stream.Discard();Assert.Equal(first,stream.Peek());
            var drain=Math.Max(1,capacity/2);
            for(var i=0;i<drain;i++)
            {
                Assert.Equal(expected.Dequeue(),stream.Peek());
                stream.Acknowledge(stream.Peek().Id);
            }
            stream.Begin(Stamp(revision+1));
            for(var i=0;i<drain;i++)stream.Append(new(Signal.Impact,new(99)));
            Assert.Equal(drain,stream.StagedCount);
            stream.Seal();stream.Discard(); // Roll back only the tail, including wrapped storage.
            Assert.Equal(capacity-drain,stream.PendingCount);
            if(expected.Count>0)Assert.Equal(expected.Peek(),stream.Peek());
        }
        while(expected.Count>0)
        {
            Assert.Equal(expected.Dequeue(),stream.Peek());stream.Acknowledge(stream.Peek().Id);
        }
        Assert.Equal(0,stream.PendingCount);Assert.Equal(capacity,stream.AvailableCapacity);
        Assert.Equal(capacity,stream.HighWaterMark);
    }

    [Theory]
    [InlineData(EventStreamPhase.Idle)]
    [InlineData(EventStreamPhase.Writing)]
    [InlineData(EventStreamPhase.Staged)]
    [InlineData(EventStreamPhase.Rejected)]
    public void RemovalInvalidatesCommittedAndStagedOccurrencesWithoutBlockingReset(EventStreamPhase phase)
    {
        var stream=new CommittedEventStream<Payload>(Stream,Stamp(0),2);
        stream.Begin(Stamp(1));stream.Append(new(Signal.Trigger,new(1)));stream.Seal();stream.Commit();
        var old=stream.Peek();
        switch(phase)
        {
            case EventStreamPhase.Idle:break;
            case EventStreamPhase.Writing:
            case EventStreamPhase.Staged:
            case EventStreamPhase.Rejected:
                stream.Begin(Stamp(2));stream.Append(new(Signal.Impact,new(2)));
                if(phase==EventStreamPhase.Staged)stream.Seal();
                if(phase==EventStreamPhase.Rejected)
                    Assert.Throws<InvalidOperationException>(()=>stream.Append(new(Signal.Impact,new(3))));
                break;
            default:throw new ArgumentOutOfRangeException(nameof(phase));
        }
        stream.Remove();Assert.Equal(EventStreamPhase.Removed,stream.Phase);
        Assert.Equal(0,stream.PendingCount);Assert.Equal(0,stream.StagedCount);
        Assert.Throws<InvalidOperationException>(()=>stream.Acknowledge(old.Id));
        Assert.Throws<InvalidOperationException>(()=>stream.Begin(Stamp(2)));
        var replacement=new CommittedEventStream<Payload>(Stream,Stamp(0,2),2);
        replacement.Begin(Stamp(1,2));replacement.Append(old.Payload);replacement.Seal();replacement.Commit();
        Assert.Throws<ArgumentException>(()=>replacement.Acknowledge(old.Id));
        Assert.Equal(new EventSequence(1),replacement.Peek().Id.Sequence);
        Assert.Equal(old.Payload,replacement.Peek().Payload);
    }

    [Fact]
    public void ConsumersCannotInterleaveWithPendingTransactionsOrAcknowledgeOutOfOrder()
    {
        var stream=new CommittedEventStream<Payload>(Stream,Stamp(0),3);
        stream.Begin(Stamp(1));stream.Append(new(Signal.Trigger,new(1)));stream.Seal();stream.Commit();
        var first=stream.Peek();
        stream.Begin(Stamp(2));stream.Append(new(Signal.Trigger,new(2)));
        Assert.Equal(1,stream.PendingCount);Assert.Equal(1,stream.StagedCount);
        Assert.Throws<InvalidOperationException>(()=>stream.Peek());
        Assert.Throws<InvalidOperationException>(()=>stream.Acknowledge(first.Id));
        stream.Seal();Assert.Throws<InvalidOperationException>(()=>stream.Acknowledge(first.Id));
        stream.Commit();
        Assert.Throws<ArgumentException>(()=>stream.Acknowledge(new(Stream,first.Id.Generation,new(2))));
        Assert.Equal(first,stream.Peek());stream.Acknowledge(first.Id);
        var second=stream.Peek();Assert.Equal(Stamp(2),second.Stamp);
        Assert.Throws<ArgumentException>(()=>stream.Acknowledge(first.Id));
        Assert.Equal(second,stream.Peek());
    }

    [Fact]
    public void OscillatorBackpressureRollsBackScheduleAndRetainsEveryOlderPulse()
    {
        var id=new SimulationOscillatorId(0);
        var oscillators=new SimulationOscillators([new(id,1)]);
        var transaction=new SimulationTransaction([oscillators]);
        var stream=new CommittedEventStream<SimulationOscillatorPulse>(Stream,Stamp(0),3);
        SimulationOscillatorInput[] inputs=[new(id,true)];
        void Tick(int tick)
        {
            stream.Begin(Stamp(tick+1));transaction.Begin();
            try
            {
                foreach(var pulse in oscillators.Advance(tick,inputs))stream.Append(pulse);
                stream.Seal();
            }
            catch
            {
                stream.Discard();transaction.Rollback();throw;
            }
            transaction.Commit();stream.Commit();
        }
        for(var tick=0;tick<=3;tick++)Tick(tick);
        Assert.Equal(3,stream.PendingCount);var before=oscillators.Read(id);var first=stream.Peek();
        Assert.Throws<InvalidOperationException>(()=>Tick(4));
        Assert.Equal(before,oscillators.Read(id));Assert.Equal(3,oscillators.Tick);
        Assert.Equal(Stamp(4),stream.CurrentStamp);Assert.Equal(first,stream.Peek());
        stream.Acknowledge(first.Id);Tick(4);
        Assert.Equal(4,oscillators.Read(id).PulseCount);
        for(var tick=2;tick<=4;tick++)
        {
            var occurrence=stream.Peek();
            Assert.Equal(new SimulationOscillatorPulse(id,tick,tick),occurrence.Payload);
            Assert.Equal(Stamp(tick+1),occurrence.Stamp);
            Assert.Equal(new EventSequence((ulong)tick),occurrence.Id.Sequence);
            stream.Acknowledge(occurrence.Id);
        }
        Assert.Equal(0,stream.PendingCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(1024)]
    public void WarmDelayedDeliveryAndWrappedRollbackAllocateNothing(int capacity)
    {
        var stream=new CommittedEventStream<Payload>(Stream,Stamp(0),capacity);
        long revision=0;
        void Cycle()
        {
            while(stream.AvailableCapacity>0)
            {
                stream.Begin(Stamp(++revision));stream.Append(new(Signal.Impact,new(1)));stream.Seal();stream.Commit();
            }
            stream.Acknowledge(stream.Peek().Id);
            stream.Begin(Stamp(revision+1));stream.Append(new(Signal.Trigger,new(2)));stream.Seal();stream.Discard();
        }
        for(var i=0;i<100;i++)Cycle();
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<1000;i++)Cycle();
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.Equal(0,allocated);
        Console.WriteLine($"Committed event stream: capacity={capacity}, cycles=1000, allocated_bytes={allocated}");
    }

    [Fact]
    public void MatchingGenerationAndSequenceCannotAcknowledgeAnotherStream()
    {
        var first=new CommittedEventStream<Payload>(Stream,Stamp(0),1);
        var second=new CommittedEventStream<Payload>(new(7),Stamp(0),1);
        foreach(var stream in new[]{first,second})
        {
            stream.Begin(Stamp(1));stream.Append(new(Signal.Trigger,new(1)));stream.Seal();stream.Commit();
        }
        var a=first.Peek();var b=second.Peek();
        Assert.Equal(a.Stamp,b.Stamp);Assert.Equal(a.Id.Sequence,b.Id.Sequence);
        Assert.NotEqual(a.Id.Stream,b.Id.Stream);
        Assert.Throws<ArgumentException>(()=>first.Acknowledge(b.Id));
        Assert.Throws<ArgumentException>(()=>second.Acknowledge(a.Id));
        Assert.Equal(a,first.Peek());Assert.Equal(b,second.Peek());
        first.Acknowledge(a.Id);second.Acknowledge(b.Id);
        Assert.Equal(0,first.PendingCount);Assert.Equal(0,second.PendingCount);
    }
}
