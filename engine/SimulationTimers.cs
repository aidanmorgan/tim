using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public readonly record struct SimulationTimerId
{
    public int Index { get; }
    public SimulationTimerId(int index)
    {
        if(index<0)throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}
public enum SimulationTimerPhase { Ready, Counting, Finished }
public enum TimerTransactionPhase { Idle, Active }
public enum TimerCompletionPolicy { Latch, Rearm }
public enum TimerBoundary { BeforeNetworks, BeforePhysics }
public readonly record struct SimulationTimerDeclaration(SimulationTimerId Id,int DurationTicks,
    TimerCompletionPolicy Completion,TimerBoundary Boundary);
public readonly record struct SimulationTimerState(SimulationTimerId Id,SimulationTimerPhase Phase,int StartedTick,int DueTick);
public readonly record struct SimulationTimerElapsed(SimulationTimerId Id,int Tick);

/// <summary>Owned retained state, valid only for the world which captured it.</summary>
public sealed class SimulationTimerSnapshot
{
    internal SimulationTimers Owner { get; }
    internal SimulationTimerState[] Values { get; }
    public int Tick { get; }
    public TimerBoundary? Boundary { get; }
    internal SimulationTimerSnapshot(SimulationTimers owner,int tick,TimerBoundary? boundary,SimulationTimerState[] states)
    {Owner=owner;Tick=tick;Boundary=boundary;Values=(SimulationTimerState[])states.Clone();}
}

/// <summary>Renderer-independent digital timers with explicit completion and
/// scheduling policies. Busy retriggers never restart a countdown. Reset creates a new
/// declared world; Restore is an exact transaction rollback on the same owner.</summary>
public sealed class SimulationTimers
{
    private readonly SimulationTimerDeclaration[] _declarations;
    private readonly SimulationTimerState[] _states;
    private readonly SimulationTimerElapsed[] _elapsed;
    private readonly SimulationTimerState[] _checkpoint;
    private int _checkpointTick;
    private TimerBoundary? _checkpointBoundary;
    public TimerTransactionPhase TransactionPhase { get; private set; }
    private readonly Dictionary<SimulationTimerId,int> _indices=new();
    public int Tick { get; private set; }
    public TimerBoundary? Boundary { get; private set; }
    public SimulationTimers(IEnumerable<SimulationTimerDeclaration> declarations)
    {
        ArgumentNullException.ThrowIfNull(declarations);
        _declarations=declarations.OrderBy(d=>d.Id.Index).ToArray();
        _states=new SimulationTimerState[_declarations.Length];
        _elapsed=new SimulationTimerElapsed[_declarations.Length];
        _checkpoint=new SimulationTimerState[_declarations.Length];
        for(var i=0;i<_declarations.Length;i++)
        {
            var declaration=_declarations[i];
            if(declaration.DurationTicks<1||!Enum.IsDefined(declaration.Completion)||!Enum.IsDefined(declaration.Boundary)||
                !_indices.TryAdd(declaration.Id,i))
                throw new ArgumentException("Timers require unique identities and positive tick durations.");
            _states[i]=new(declaration.Id,SimulationTimerPhase.Ready,-1,-1);
        }
    }
    private int Index(SimulationTimerId id)=>_indices.TryGetValue(id,out var index)?index:
        throw new ArgumentException("Timer is not declared in this world.",nameof(id));
    public SimulationTimerState Read(SimulationTimerId id)=>_states[Index(id)];
    public double Progress(SimulationTimerId id)
    {
        var state=Read(id);
        return state.Phase switch
        {
            SimulationTimerPhase.Ready=>0,
            SimulationTimerPhase.Counting=>Math.Clamp((double)(Tick-state.StartedTick)/(state.DueTick-state.StartedTick),0,1),
            SimulationTimerPhase.Finished=>1,
            _=>throw new InvalidOperationException("Unsupported timer phase.")
        };
    }
    public bool Trigger(SimulationTimerId id,int tick)
    {
        var index=Index(id);
        if(tick<Tick)throw new ArgumentOutOfRangeException(nameof(tick),"Timer commands cannot precede the observed clock.");
        if(_states[index].Phase!=SimulationTimerPhase.Ready)return false;
        var due=checked(tick+_declarations[index].DurationTicks);
        _states[index]=new(id,SimulationTimerPhase.Counting,tick,due);
        return true;
    }
    /// <summary>Borrowed events remain valid only until the next Advance call.
    /// Every due timer emits once, in identity order, with its actual deadline.
    /// No event is dropped when advancing over more than one tick.</summary>
    public ReadOnlySpan<SimulationTimerElapsed> Advance(int tick,TimerBoundary boundary)
    {
        if(!Enum.IsDefined(boundary)||tick<Tick||tick==Tick&&Boundary is { } prior&&boundary<prior)
            throw new ArgumentOutOfRangeException(nameof(boundary),"Timer boundaries must advance monotonically.");
        foreach(var state in _states)
            if(state.StartedTick>tick)throw new ArgumentException("Clock precedes an admitted timer trigger.",nameof(tick));
        var count=0;
        for(var i=0;i<_states.Length;i++)
        {
            var state=_states[i];
            var declaration=_declarations[i];
            if(declaration.Boundary!=boundary||state.Phase!=SimulationTimerPhase.Counting||state.DueTick>tick)continue;
            _states[i]=state with {Phase=declaration.Completion switch
            {
                TimerCompletionPolicy.Latch=>SimulationTimerPhase.Finished,
                TimerCompletionPolicy.Rearm=>SimulationTimerPhase.Ready,
                _=>throw new InvalidOperationException("Unsupported timer completion policy.")
            }};
            _elapsed[count++]=new(state.Id,state.DueTick);
        }
        Tick=tick;Boundary=boundary;
        return _elapsed.AsSpan(0,count);
    }
    private void RequireTransactionPhase(TimerTransactionPhase expected)
    {
        if(TransactionPhase!=expected)throw new InvalidOperationException("Invalid timer transaction phase.");
    }
    public void BeginTransaction()
    {
        RequireTransactionPhase(TimerTransactionPhase.Idle);
        _states.CopyTo(_checkpoint,0);_checkpointTick=Tick;_checkpointBoundary=Boundary;
        TransactionPhase=TimerTransactionPhase.Active;
    }
    public void CommitTransaction()
    {
        RequireTransactionPhase(TimerTransactionPhase.Active);
        TransactionPhase=TimerTransactionPhase.Idle;
    }
    public void RollbackTransaction()
    {
        RequireTransactionPhase(TimerTransactionPhase.Active);
        _checkpoint.CopyTo(_states,0);Tick=_checkpointTick;Boundary=_checkpointBoundary;
        TransactionPhase=TimerTransactionPhase.Idle;
    }
    public SimulationTimerSnapshot Capture()
    {
        RequireTransactionPhase(TimerTransactionPhase.Idle);
        return new(this,Tick,Boundary,_states);
    }
    public void Restore(SimulationTimerSnapshot snapshot)
    {
        RequireTransactionPhase(TimerTransactionPhase.Idle);
        ArgumentNullException.ThrowIfNull(snapshot);
        if(!ReferenceEquals(snapshot.Owner,this))throw new ArgumentException("Snapshot belongs to another timer world.",nameof(snapshot));
        snapshot.Values.CopyTo(_states,0);Tick=snapshot.Tick;Boundary=snapshot.Boundary;
    }
}
