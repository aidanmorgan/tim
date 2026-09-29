using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public readonly record struct SimulationCounterId
{
    public int Index { get; }
    public SimulationCounterId(int index)
    {
        if(index<0)throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}
public enum SimulationCounterPhase { Counting, Reached }
public enum SimulationCounterResult { Accumulated, Reached, Saturated }
public enum CounterTransactionPhase { Idle, Active }
public readonly record struct SimulationCounterDeclaration(SimulationCounterId Id,int Target);
public readonly record struct SimulationCounterState(SimulationCounterId Id,int Count,int Target)
{
    public SimulationCounterPhase Phase=>Count==Target?SimulationCounterPhase.Reached:SimulationCounterPhase.Counting;
}
public sealed class SimulationCounterSnapshot
{
    internal SimulationCounters Owner { get; }
    internal SimulationCounterState[] States { get; }
    internal SimulationCounterSnapshot(SimulationCounters owner,SimulationCounterState[] states)
    {Owner=owner;States=(SimulationCounterState[])states.Clone();}
}

/// <summary>Owned saturating event counters. One accepted delivery increments once.
/// Reaching a target returns one occurrence; later deliveries preserve saturation.
/// The host owns ordering, event delivery and electrical supply.</summary>
public sealed class SimulationCounters
{
    private readonly SimulationCounterState[] _states,_checkpoint;
    private readonly Dictionary<SimulationCounterId,int> _indices=new();
    public CounterTransactionPhase TransactionPhase { get; private set; }
    public SimulationCounters(IEnumerable<SimulationCounterDeclaration> declarations)
    {
        ArgumentNullException.ThrowIfNull(declarations);
        var ordered=declarations.OrderBy(d=>d.Id.Index).ToArray();
        _states=new SimulationCounterState[ordered.Length];_checkpoint=new SimulationCounterState[ordered.Length];
        for(var i=0;i<ordered.Length;i++)
        {
            var declaration=ordered[i];
            if(declaration.Target<1||!_indices.TryAdd(declaration.Id,i))
                throw new ArgumentException("Counters require unique identities and positive targets.");
            _states[i]=new(declaration.Id,0,declaration.Target);
        }
    }
    private int Index(SimulationCounterId id)=>_indices.TryGetValue(id,out var index)?index:
        throw new ArgumentException("Counter is not declared in this world.",nameof(id));
    public SimulationCounterState Read(SimulationCounterId id)=>_states[Index(id)];
    public SimulationCounterResult Increment(SimulationCounterId id)
    {
        var index=Index(id);var state=_states[index];
        if(state.Count==state.Target)return SimulationCounterResult.Saturated;
        state=state with {Count=checked(state.Count+1)};_states[index]=state;
        return state.Count==state.Target?SimulationCounterResult.Reached:SimulationCounterResult.Accumulated;
    }
    private void RequirePhase(CounterTransactionPhase phase)
    {
        if(TransactionPhase!=phase)throw new InvalidOperationException("Invalid counter transaction phase.");
    }
    public void BeginTransaction()
    {
        RequirePhase(CounterTransactionPhase.Idle);
        _states.CopyTo(_checkpoint,0);TransactionPhase=CounterTransactionPhase.Active;
    }
    public void CommitTransaction()
    {
        RequirePhase(CounterTransactionPhase.Active);TransactionPhase=CounterTransactionPhase.Idle;
    }
    public void RollbackTransaction()
    {
        RequirePhase(CounterTransactionPhase.Active);
        _checkpoint.CopyTo(_states,0);TransactionPhase=CounterTransactionPhase.Idle;
    }
    public SimulationCounterSnapshot Capture()
    {
        RequirePhase(CounterTransactionPhase.Idle);return new(this,_states);
    }
    public void Restore(SimulationCounterSnapshot snapshot)
    {
        RequirePhase(CounterTransactionPhase.Idle);ArgumentNullException.ThrowIfNull(snapshot);
        if(!ReferenceEquals(snapshot.Owner,this))throw new ArgumentException("Snapshot belongs to another counter world.",nameof(snapshot));
        snapshot.States.CopyTo(_states,0);
    }
}
