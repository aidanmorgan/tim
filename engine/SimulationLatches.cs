using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public readonly record struct SimulationLatchId
{
    public int Index { get; }
    public SimulationLatchId(int index)
    {
        if(index<0)throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}
public enum SimulationLatchPhase { Off, On }
public enum SimulationLatchCommand { Set, Reset }
[Flags] public enum SimulationLatchRequests { None=0, Set=1, Reset=2 }
public enum LatchTransactionPhase { Idle, Active }
public readonly record struct SimulationLatchState(
    SimulationLatchId Id,SimulationLatchPhase Phase,
    SimulationLatchRequests CurrentRequests,SimulationLatchRequests NextRequests);
public sealed class SimulationLatchSnapshot
{
    internal SimulationLatches Owner { get; }
    internal long Tick { get; }
    internal SimulationLatchState[] States { get; }
    internal SimulationLatchSnapshot(SimulationLatches owner,long tick,SimulationLatchState[] states)
    {Owner=owner;Tick=tick;States=(SimulationLatchState[])states.Clone();}
}

/// <summary>Owned reset-dominant digital memory. Deliveries from tick t settle at
/// boundary t+1. The two request buckets admit deliveries on either side of the
/// current boundary; future or late deliveries reject. No electrical energy is supplied.</summary>
public sealed class SimulationLatches
{
    private readonly SimulationLatchState[] _states,_checkpoint;
    private readonly Dictionary<SimulationLatchId,int> _indices=new();
    private long _checkpointTick;
    public long Tick { get; private set; }
    public LatchTransactionPhase TransactionPhase { get; private set; }
    public SimulationLatches(IEnumerable<SimulationLatchId> declarations,long firstTick=0)
    {
        ArgumentNullException.ThrowIfNull(declarations);
        if(firstTick<0)throw new ArgumentOutOfRangeException(nameof(firstTick));
        Tick=firstTick-1;
        var ordered=declarations.OrderBy(id=>id.Index).ToArray();
        _states=new SimulationLatchState[ordered.Length];_checkpoint=new SimulationLatchState[ordered.Length];
        for(var i=0;i<ordered.Length;i++)
        {
            if(!_indices.TryAdd(ordered[i],i))throw new ArgumentException("Latch identities must be unique.");
            _states[i]=new(ordered[i],SimulationLatchPhase.Off,SimulationLatchRequests.None,SimulationLatchRequests.None);
        }
    }
    private int Index(SimulationLatchId id)=>_indices.TryGetValue(id,out var index)?index:
        throw new ArgumentException("Latch is not declared in this world.",nameof(id));
    public SimulationLatchState Read(SimulationLatchId id)=>_states[Index(id)];
    public void Submit(SimulationLatchId id,SimulationLatchCommand command,long deliveryTick)
    {
        var index=Index(id);
        var request=command switch
        {
            SimulationLatchCommand.Set=>SimulationLatchRequests.Set,
            SimulationLatchCommand.Reset=>SimulationLatchRequests.Reset,
            _=>throw new ArgumentOutOfRangeException(nameof(command))
        };
        if(deliveryTick<0||deliveryTick<Tick||
            (deliveryTick!=Tick&&(Tick==long.MaxValue||deliveryTick!=Tick+1)))
            throw new ArgumentOutOfRangeException(nameof(deliveryTick),"Delivery must belong to the current or next boundary.");
        var state=_states[index];
        _states[index]=deliveryTick==Tick
            ?state with {CurrentRequests=state.CurrentRequests|request}
            :state with {NextRequests=state.NextRequests|request};
    }
    public void Advance(long tick)
    {
        if(Tick==long.MaxValue||tick!=Tick+1)
            throw new ArgumentOutOfRangeException(nameof(tick),"Latch boundaries must advance consecutively.");
        for(var i=0;i<_states.Length;i++)
        {
            var state=_states[i];
            var phase=(state.CurrentRequests&SimulationLatchRequests.Reset)!=0?SimulationLatchPhase.Off:
                (state.CurrentRequests&SimulationLatchRequests.Set)!=0?SimulationLatchPhase.On:state.Phase;
            _states[i]=state with {Phase=phase,CurrentRequests=state.NextRequests,NextRequests=SimulationLatchRequests.None};
        }
        Tick=tick;
    }
    private void RequirePhase(LatchTransactionPhase phase)
    {
        if(TransactionPhase!=phase)throw new InvalidOperationException("Invalid latch transaction phase.");
    }
    public void BeginTransaction()
    {
        RequirePhase(LatchTransactionPhase.Idle);_states.CopyTo(_checkpoint,0);
        _checkpointTick=Tick;TransactionPhase=LatchTransactionPhase.Active;
    }
    public void CommitTransaction()
    {
        RequirePhase(LatchTransactionPhase.Active);TransactionPhase=LatchTransactionPhase.Idle;
    }
    public void RollbackTransaction()
    {
        RequirePhase(LatchTransactionPhase.Active);_checkpoint.CopyTo(_states,0);
        Tick=_checkpointTick;TransactionPhase=LatchTransactionPhase.Idle;
    }
    public SimulationLatchSnapshot Capture()
    {
        RequirePhase(LatchTransactionPhase.Idle);return new(this,Tick,_states);
    }
    public void Restore(SimulationLatchSnapshot snapshot)
    {
        RequirePhase(LatchTransactionPhase.Idle);ArgumentNullException.ThrowIfNull(snapshot);
        if(!ReferenceEquals(snapshot.Owner,this))throw new ArgumentException("Snapshot belongs to another latch world.",nameof(snapshot));
        snapshot.States.CopyTo(_states,0);Tick=snapshot.Tick;
    }
}
