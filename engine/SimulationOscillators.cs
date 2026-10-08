using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public readonly record struct SimulationOscillatorId
{
    public int Index { get; }
    public SimulationOscillatorId(int index)
    {
        if(index<0)throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}
public enum SimulationOscillatorQuantity { ProgressFraction }
public enum SimulationOscillatorPhase { Stopped, Running }
public readonly record struct SimulationOscillatorDeclaration(SimulationOscillatorId Id,int IntervalTicks);
public readonly record struct SimulationOscillatorInput(SimulationOscillatorId Id,bool Enabled);
public readonly record struct SimulationOscillatorState(SimulationOscillatorId Id,
    SimulationOscillatorPhase Phase,int DueTick,int PulseCount,int LastPulseTick);
public readonly record struct SimulationOscillatorPulse(SimulationOscillatorId Id,int Tick,int Sequence);

public sealed class SimulationOscillatorSnapshot
{
    internal SimulationOscillators Owner { get; }
    internal SimulationOscillatorState[] States { get; }
    public int Tick { get; }
    internal SimulationOscillatorSnapshot(SimulationOscillators owner,int tick,SimulationOscillatorState[] states)
    {Owner=owner;Tick=tick;States=(SimulationOscillatorState[])states.Clone();}
}

/// <summary>Owned powered digital oscillators. Each consecutive integer tick samples
/// every input once; power restoration starts a full interval. No startup pulse or
/// catch-up for time spent disabled. Skipped or duplicate ticks reject.</summary>
public sealed class SimulationOscillators : SimulationTransactionParticipant
{
    private readonly SimulationOscillatorDeclaration[] _declarations;
    private readonly SimulationOscillatorState[] _states,_staging,_checkpoint;
    private readonly SimulationOscillatorPulse[] _pulses;
    private readonly Dictionary<SimulationOscillatorId,int> _indices=new();
    private int _checkpointTick;
    public int Tick { get; private set; }
    public SimulationOscillators(IEnumerable<SimulationOscillatorDeclaration> declarations,int firstTick=0)
    {
        ArgumentNullException.ThrowIfNull(declarations);
        if(firstTick<0)throw new ArgumentOutOfRangeException(nameof(firstTick));
        Tick=firstTick-1;
        _declarations=declarations.OrderBy(d=>d.Id.Index).ToArray();
        _states=new SimulationOscillatorState[_declarations.Length];
        _staging=new SimulationOscillatorState[_declarations.Length];
        _checkpoint=new SimulationOscillatorState[_declarations.Length];
        _pulses=new SimulationOscillatorPulse[_declarations.Length];
        for(var i=0;i<_declarations.Length;i++)
        {
            var declaration=_declarations[i];
            if(declaration.IntervalTicks<1||!_indices.TryAdd(declaration.Id,i))
                throw new ArgumentException("Oscillators require unique identities and positive tick intervals.");
            _states[i]=new(declaration.Id,SimulationOscillatorPhase.Stopped,-1,0,-1);
        }
    }
    private int Index(SimulationOscillatorId id)=>_indices.TryGetValue(id,out var index)?index:
        throw new ArgumentException("Oscillator is not declared in this world.",nameof(id));
    public SimulationOscillatorState Read(SimulationOscillatorId id)=>_states[Index(id)];
    public double Progress(SimulationOscillatorId id)
    {
        var index=Index(id);var state=_states[index];
        return state.Phase switch
        {
            SimulationOscillatorPhase.Stopped=>0,
            SimulationOscillatorPhase.Running=>1-(double)(state.DueTick-Tick)/_declarations[index].IntervalTicks,
            _=>throw new InvalidOperationException("Unsupported oscillator phase.")
        };
    }
    public double ReadQuantity(SimulationOscillatorId id,SimulationOscillatorQuantity quantity)=>quantity switch
    {
        SimulationOscillatorQuantity.ProgressFraction=>Progress(id),
        _=>throw new ArgumentOutOfRangeException(nameof(quantity))
    };
    /// <summary>Inputs must include each declared identity once in ascending order.
    /// The borrowed pulse span is valid until the next Advance attempt.
    /// Validation/overflow failures preserve every state and the committed clock.</summary>
    public ReadOnlySpan<SimulationOscillatorPulse> Advance(int tick,ReadOnlySpan<SimulationOscillatorInput> inputs)
    {
        if(tick<0||tick!=(long)Tick+1)throw new ArgumentOutOfRangeException(nameof(tick),"Oscillators require consecutive ticks.");
        if(inputs.Length!=_states.Length)throw new ArgumentException("Every oscillator input is required.",nameof(inputs));
        for(var i=0;i<_states.Length;i++)
        {
            var declaration=_declarations[i];var state=_states[i];
            if(inputs[i].Id!=declaration.Id)throw new ArgumentException("Oscillator inputs must follow declaration identity order.",nameof(inputs));
            if(!inputs[i].Enabled)
                state=state with {Phase=SimulationOscillatorPhase.Stopped,DueTick=-1};
            else if(state.Phase==SimulationOscillatorPhase.Stopped)
                state=state with {Phase=SimulationOscillatorPhase.Running,DueTick=checked(tick+declaration.IntervalTicks)};
            else if(tick==state.DueTick)
                state=state with {DueTick=checked(tick+declaration.IntervalTicks),
                    PulseCount=checked(state.PulseCount+1),LastPulseTick=tick};
            _staging[i]=state;
        }
        var count=0;
        for(var i=0;i<_states.Length;i++)
        {
            var next=_staging[i];
            if(next.PulseCount!=_states[i].PulseCount)_pulses[count++]=new(next.Id,tick,next.PulseCount);
            _states[i]=next;
        }
        Tick=tick;
        return _pulses.AsSpan(0,count);
    }
    protected override void CaptureCheckpoint()
    {
        _states.CopyTo(_checkpoint,0);_checkpointTick=Tick;
    }
    protected override void RestoreCheckpoint()
    {
        _checkpoint.CopyTo(_states,0);Tick=_checkpointTick;
    }
    public SimulationOscillatorSnapshot Capture()
    {
        RequireTransactionPhase(SimulationTransactionPhase.Idle);return new(this,Tick,_states);
    }
    public void Restore(SimulationOscillatorSnapshot snapshot)
    {
        RequireTransactionPhase(SimulationTransactionPhase.Idle);ArgumentNullException.ThrowIfNull(snapshot);
        if(!ReferenceEquals(snapshot.Owner,this))throw new ArgumentException("Snapshot belongs to another oscillator world.",nameof(snapshot));
        snapshot.States.CopyTo(_states,0);Tick=snapshot.Tick;
    }
}
