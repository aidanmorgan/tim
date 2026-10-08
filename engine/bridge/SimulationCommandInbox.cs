using System;

namespace CuriousContraptions.Bridge;

public readonly record struct WorldGeneration
{
    public long Value { get; }
    public WorldGeneration(long value)
    { if(value<=0)throw new ArgumentOutOfRangeException(nameof(value));Value=value; }
}
public readonly record struct CommandSequence
{
    public long Value { get; }
    public CommandSequence(long value)
    { if(value<=0)throw new ArgumentOutOfRangeException(nameof(value));Value=value; }
}
public readonly record struct SimulationRevision
{
    public long Value { get; }
    public SimulationRevision(long value)
    { if(value<0)throw new ArgumentOutOfRangeException(nameof(value));Value=value; }
}
public readonly record struct CommandId
{
    public WorldGeneration Generation { get; }
    public CommandSequence Sequence { get; }
    public CommandId(WorldGeneration generation,CommandSequence sequence)
    {
        if(generation.Value<=0||sequence.Value<=0)
            throw new ArgumentException("Command identity must be initialized.");
        Generation=generation;Sequence=sequence;
    }
}
public enum CommandAdmission { Accepted, WrongGeneration, OutOfOrder, Full }
public enum CommandOutcome { Applied, UnsupportedTarget, StaleRevision, InvalidCapability, InvalidMode, OutOfRange, InvalidatedByBarrier }
public readonly record struct CommandEnvelope<T>(WorldGeneration Generation,CommandSequence Sequence,
    SimulationRevision? ExpectedRevision,T Payload) where T:unmanaged;
public readonly record struct CommandResult(WorldGeneration Generation,CommandSequence Sequence,
    SimulationRevision Revision,CommandOutcome Outcome)
{
    public CommandId Id=>new(Generation,Sequence);
}

/// <summary>Single-thread bounded inbox. Admission reserves one result slot per command.
/// Application occurs inside the owning transaction; results are observable only at Idle.
/// Payloads contain no executable callbacks, scene objects or mutable reference graphs.</summary>
public sealed class SimulationCommandInbox<T> : SimulationTransactionParticipant where T:unmanaged
{
    private readonly CommandEnvelope<T>[] _pending;
    private readonly CommandResult[] _results;
    private int _head,_count,_resultHead,_resultCount;
    private int _savedHead,_savedCount,_savedResultCount;
    private long _lastSequence;
    public WorldGeneration Generation { get; private set; }
    public CommandSequence NextSequence=>new(checked(_lastSequence+1));
    public int PendingCount=>_count;
    public int ResultCount
    {
        get { RequireTransactionPhase(SimulationTransactionPhase.Idle);return _resultCount; }
    }
    public int Capacity=>_pending.Length;
    public int HighWater { get; private set; }
    public SimulationCommandInbox(WorldGeneration generation,int capacity)
    {
        if(generation.Value<=0)throw new ArgumentException("Invalid world generation.",nameof(generation));
        if(capacity<=0)throw new ArgumentOutOfRangeException(nameof(capacity));
        Generation=generation;_pending=new CommandEnvelope<T>[capacity];_results=new CommandResult[capacity];
    }
    public CommandAdmission Admit(CommandEnvelope<T> command)
    {
        RequireTransactionPhase(SimulationTransactionPhase.Idle);
        if(command.Generation.Value<=0||command.Sequence.Value<=0)
            throw new ArgumentException("Command identities must be initialized.",nameof(command));
        if(command.Generation!=Generation)return CommandAdmission.WrongGeneration;
        if(command.Sequence.Value<=_lastSequence)return CommandAdmission.OutOfOrder;
        if(_count+_resultCount==Capacity)return CommandAdmission.Full;
        _pending[(_head+_count)%Capacity]=command;
        _count++;_lastSequence=command.Sequence.Value;
        HighWater=Math.Max(HighWater,_count+_resultCount);
        return CommandAdmission.Accepted;
    }
    public bool TryPeek(out CommandEnvelope<T> command)
    {
        RequireTransactionPhase(SimulationTransactionPhase.Active);
        command=_count==0?default:_pending[_head];
        return _count!=0;
    }
    public void Complete(CommandOutcome outcome,SimulationRevision revision)
    {
        RequireTransactionPhase(SimulationTransactionPhase.Active);
        if(!Enum.IsDefined(outcome)||outcome==CommandOutcome.InvalidatedByBarrier)
            throw new ArgumentOutOfRangeException(nameof(outcome));
        if(_count==0)throw new InvalidOperationException("No pending command.");
        var command=_pending[_head];
        AppendResult(command,outcome,revision);
        _head=(_head+1)%Capacity;_count--;
    }
    private void AppendResult(CommandEnvelope<T> command,CommandOutcome outcome,SimulationRevision revision)
    {
        if(_resultCount==Capacity)throw new InvalidOperationException("Reserved result capacity was violated.");
        _results[(_resultHead+_resultCount)%Capacity]=new(command.Generation,command.Sequence,revision,outcome);
        _resultCount++;
    }
    /// <summary>Returns an owned value without releasing its reserved slot. Repeated
    /// peeks identify the same committed command until its consumer acknowledges it.</summary>
    public bool TryPeekResult(out CommandResult result)
    {
        RequireTransactionPhase(SimulationTransactionPhase.Idle);
        result=_resultCount==0?default:_results[_resultHead];
        return _resultCount!=0;
    }
    public void AcknowledgeResult(CommandId id)
    {
        RequireTransactionPhase(SimulationTransactionPhase.Idle);
        if(id.Generation.Value<=0||id.Sequence.Value<=0)
            throw new ArgumentException("Command identity must be initialized.",nameof(id));
        if(_resultCount==0||_results[_resultHead].Id!=id)
            throw new InvalidOperationException("Only the next committed result can be acknowledged.");
        _results[_resultHead]=default;
        _resultHead=(_resultHead+1)%Capacity;_resultCount--;
    }
    /// <summary>Idle lifecycle barrier. Old results remain readable and every pending command
    /// receives an explicit invalidation result; reserved slots make this independent of reader lag.</summary>
    public void AdvanceGeneration(WorldGeneration next,SimulationRevision revision)
    {
        RequireTransactionPhase(SimulationTransactionPhase.Idle);
        if(next.Value<=Generation.Value)throw new ArgumentOutOfRangeException(nameof(next));
        while(_count>0)
        {
            AppendResult(_pending[_head],CommandOutcome.InvalidatedByBarrier,revision);
            _head=(_head+1)%Capacity;_count--;
        }
        Generation=next;_lastSequence=0;
    }
    protected override void CaptureCheckpoint()
    { _savedHead=_head;_savedCount=_count;_savedResultCount=_resultCount; }
    protected override void RestoreCheckpoint()
    { _head=_savedHead;_count=_savedCount;_resultCount=_savedResultCount; }
}
