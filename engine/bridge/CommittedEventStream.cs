using System;

namespace CuriousContraptions.Bridge;

/// <summary>World-scoped extensible stream identity. The host assigns distinct IDs to its streams.</summary>
public readonly record struct EventStreamId
{
    public int Index { get; }
    public EventStreamId(int index)
    {
        if(index<0)throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}
public readonly record struct EventSequence
{
    public ulong Value { get; }
    public EventSequence(ulong value)
    {
        if(value==0)throw new ArgumentOutOfRangeException(nameof(value));
        Value=value;
    }
}
public readonly record struct CommittedEventId(EventStreamId Stream,WorldGeneration Generation,EventSequence Sequence);
public readonly record struct CommittedEvent<T>(PoseReadStamp Stamp,CommittedEventId Id,T Payload) where T:unmanaged;
public enum EventStreamPhase { Idle, Writing, Rejected, Staged, Removed }

/// <summary>Bounded single-producer/single-consumer occurrence stream. Adjacent transactions
/// append atomically behind unacknowledged events; each event retains its own commit stamp.
/// Capacity is shared by retained and staged occurrences. Overflow rejects the whole pending
/// transaction without overwriting committed data. Consumers operate only between transactions.
/// Payload validation belongs to the typed producer; returned unmanaged values may be retained.</summary>
public sealed class CommittedEventStream<T> where T:unmanaged
{
    private readonly CommittedEvent<T>[] _events;
    private PoseReadStamp _current,_pending;
    private int _head,_count,_stagedCount;
    private ulong _lastSequence;
    public EventStreamId Id { get; }
    public EventStreamPhase Phase { get; private set; }
    public int Capacity=>_events.Length;
    public int PendingCount=>_count;
    public int StagedCount=>_stagedCount;
    public int AvailableCapacity=>Capacity-_count-_stagedCount;
    public int HighWaterMark { get; private set; }
    public PoseReadStamp CurrentStamp=>_current;

    public CommittedEventStream(EventStreamId id,PoseReadStamp initial,int capacity)
    {
        ValidateStamp(initial);
        if(initial.Revision.Value!=0||initial.SimulationTime!=0)
            throw new ArgumentException("Event publication must begin at the run seed.");
        if(capacity<=0)throw new ArgumentOutOfRangeException(nameof(capacity));
        Id=id;_current=initial;_events=new CommittedEvent<T>[capacity];
    }
    private static void ValidateStamp(PoseReadStamp stamp)
    {
        if(stamp.Generation.Value<=0||stamp.Revision.Value<0||
            !double.IsFinite(stamp.SimulationTime)||stamp.SimulationTime<0)
            throw new ArgumentException("Invalid event publication stamp.");
    }
    private int Index(int offset)=>(int)(((long)_head+offset)%Capacity);
    public void RequireWritable()
    {
        if(Phase!=EventStreamPhase.Idle)
            throw new InvalidOperationException("Event stream has a pending transaction or has been removed.");
    }
    public void Begin(PoseReadStamp stamp)
    {
        RequireWritable();ValidateStamp(stamp);
        if(stamp.Generation!=_current.Generation||stamp.Revision.Value!=checked(_current.Revision.Value+1)||
            stamp.SimulationTime<=_current.SimulationTime)
            throw new ArgumentException("Events must advance one complete revision within their run.");
        _pending=stamp;Phase=EventStreamPhase.Writing;
    }
    public CommittedEventId Append(T value)
    {
        if(Phase!=EventStreamPhase.Writing)throw new InvalidOperationException("No event write is reserved.");
        if(AvailableCapacity==0||_lastSequence>ulong.MaxValue-(ulong)_stagedCount-1)
        {
            Phase=EventStreamPhase.Rejected;
            throw new InvalidOperationException("Event capacity or sequence exhausted before commit.");
        }
        var sequence=new EventSequence(_lastSequence+(ulong)_stagedCount+1);
        _events[Index(_count+_stagedCount)]=new(_pending,new(Id,_pending.Generation,sequence),value);
        _stagedCount++;
        HighWaterMark=Math.Max(HighWaterMark,_count+_stagedCount);
        return new(Id,_pending.Generation,sequence);
    }
    public void Seal()
    {
        if(Phase!=EventStreamPhase.Writing)throw new InvalidOperationException("No event write to stage.");
        Phase=EventStreamPhase.Staged;
    }
    public void Discard()
    {
        if(Phase is not (EventStreamPhase.Writing or EventStreamPhase.Rejected or EventStreamPhase.Staged))
            throw new InvalidOperationException("Only uncommitted events may be discarded.");
        for(var i=0;i<_stagedCount;i++)_events[Index(_count+i)]=default;
        _stagedCount=0;_pending=default;Phase=EventStreamPhase.Idle;
    }
    public void Commit()
    {
        if(Phase!=EventStreamPhase.Staged)throw new InvalidOperationException("No staged event publication.");
        _lastSequence+=(ulong)_stagedCount;
        _count+=_stagedCount;_stagedCount=0;
        _current=_pending;_pending=default;Phase=EventStreamPhase.Idle;
    }
    public CommittedEvent<T> Peek()
    {
        RequireWritable();
        if(_count==0)throw new InvalidOperationException("No committed event to consume.");
        return _events[_head];
    }
    public void Acknowledge(CommittedEventId id)
    {
        var current=Peek();
        if(id!=current.Id)throw new ArgumentException("Acknowledgement does not identify the next committed occurrence.");
        _events[_head]=default;_head=Index(1);_count--;
    }
    public void Remove()
    {
        if(Phase==EventStreamPhase.Removed)throw new InvalidOperationException("Event stream already removed.");
        Array.Clear(_events);_head=_count=_stagedCount=0;_pending=default;Phase=EventStreamPhase.Removed;
    }
}
