using System;
using System.Runtime.CompilerServices;

namespace CuriousContraptions.Gpu;

public enum ActivationOccurrenceKind : uint { Contact = 1, TimerElapsed = 2 }
public enum ActivationTimerPhase : uint { Ready, Counting, Finished }

/// <summary>Canonical physical boundary time, also used for integer-boundary controller outputs.</summary>
public readonly record struct ActivationTime(uint Ordinal, Half Phase) : IComparable<ActivationTime>
{
    public void Validate()
    {
        if (!Half.IsFinite(Phase) || Phase < (Half)(-2048) || Phase >= (Half)2048 ||
            (Ordinal == 0 && Phase < (Half)0))
            throw new ArgumentException("Invalid activation occurrence time.");
    }
    public int CompareTo(ActivationTime other) =>
        Ordinal != other.Ordinal ? Ordinal.CompareTo(other.Ordinal) : Phase.CompareTo(other.Phase);
    public ulong CeilingTick(uint substeps)
    {
        Validate(); ValidateSubsteps(substeps);
        var tick = Ordinal / substeps;
        return checked((ulong)tick + (Ordinal % substeps != 0 || Phase > (Half)0 ? 1UL : 0UL));
    }
    public static ActivationTime Boundary(SimulationTick tick, uint substeps)
    {
        ValidateSubsteps(substeps);
        return new(checked((uint)(tick.Value * substeps)), (Half)0);
    }
    private static void ValidateSubsteps(uint value)
    {
        if (value is not (2 or 4 or 8)) throw new ArgumentException("Unsupported activation clock.");
    }
}

/// <summary>Physical provenance survives routing and delay without impersonating the delayed occurrence.</summary>
public readonly record struct ActivationCause(GpuContactTriggerId Trigger, GpuBodyId Body,
    GpuColliderId Collider, ActivationTime Time, LinearSpeed ApproachSpeed)
{
    public void Validate()
    {
        Time.Validate();
        if (Trigger.Value == 0 || Body.Value == 0 || Collider.Value == 0 ||
            !Half.IsFinite(ApproachSpeed.Value) || ApproachSpeed.Value < (Half)0 || ApproachSpeed.Value > (Half)4096)
            throw new ArgumentException("Invalid physical activation provenance.");
    }
}

public readonly record struct ActivationOccurrence(ActivationOccurrenceKind Kind, ActivationNodeId Emitter,
    ActivationTime Time, ActivationCause Cause)
{
    public void Validate()
    {
        Time.Validate(); Cause.Validate();
        if (!Enum.IsDefined(Kind) || Emitter.Value == 0 || Time.CompareTo(Cause.Time) < 0 ||
            (Kind == ActivationOccurrenceKind.Contact && Time != Cause.Time) ||
            (Kind == ActivationOccurrenceKind.TimerElapsed && !PhysicsDeclarationBounds.Zero(Time.Phase)))
            throw new ArgumentException("Invalid activation occurrence.");
    }
}

public readonly record struct ActivationTimerDeclaration(ActivationNodeId Node, uint DurationTicks)
{
    public void Validate()
    {
        if (Node.Value == 0 || DurationTicks == 0) throw new ArgumentException("Invalid timer declaration.");
    }
}

/// <summary>Immutable candidate state. No timer mutates or publishes itself.</summary>
public readonly record struct ActivationTimerState(ActivationNodeId Node, ActivationTimerPhase Phase,
    ulong StartedTick, ulong DueTick, ActivationOccurrence Input)
{
    public static ActivationTimerState Ready(ActivationNodeId node) => new(node, ActivationTimerPhase.Ready, 0, 0, default);
    public void Validate()
    {
        if (Node.Value == 0 || !Enum.IsDefined(Phase)) throw new ArgumentException("Invalid timer state.");
        if (Phase == ActivationTimerPhase.Ready)
        {
            if (StartedTick != 0 || DueTick != 0 || Input != default)
                throw new ArgumentException("Ready timer retained a countdown.");
        }
        else
        {
            Input.Validate();
            if (DueTick <= StartedTick) throw new ArgumentException("Timer deadline is not positive.");
        }
    }
    public ActivationTimerState Trigger(ActivationTimerDeclaration declaration, ActivationOccurrence occurrence, uint substeps)
    {
        Validate(); declaration.Validate(); occurrence.Validate();
        if (Node != declaration.Node) throw new ArgumentException("Timer declaration does not own this state.");
        if (Phase != ActivationTimerPhase.Ready) return this;
        var start = occurrence.Time.CeilingTick(substeps);
        var due = checked(start + declaration.DurationTicks);
        // Delayed events use the same finite physical timestamp domain as the committed stream.
        _ = ActivationTime.Boundary(new(due), substeps);
        return new(Node, ActivationTimerPhase.Counting, start, due, occurrence);
    }
    public ActivationTimerState Complete(SimulationTick boundary, uint substeps, out ActivationOccurrence? occurrence)
    {
        Validate(); _ = ActivationTime.Boundary(boundary, substeps); occurrence = null;
        if (Phase != ActivationTimerPhase.Counting || DueTick > boundary.Value) return this;
        occurrence = new(ActivationOccurrenceKind.TimerElapsed, Node,
            ActivationTime.Boundary(new(DueTick), substeps), Input.Cause);
        occurrence.Value.Validate();
        return this with { Phase = ActivationTimerPhase.Finished };
    }
}

[InlineArray(ActivationNetwork.Capacity)]
internal struct TimerReadStorage { private ActivationTimerState _first; }

/// <summary>One copied, bounded timer observation population in the committed world read.</summary>
public readonly struct PhysicsTimerRead
{
    private readonly TimerReadStorage _values;
    public byte Count { get; }
    public PhysicsTimerRead(ReadOnlySpan<ActivationTimerState> values)
    {
        if (values.Length > ActivationNetwork.Capacity) throw new ArgumentException("Timer capacity exceeded.");
        Count = checked((byte)values.Length); _values = default;
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i != 0 && values[i - 1].Node.Value >= values[i].Node.Value)
                throw new ArgumentException("Timer identities must be unique and ordered.");
            _values[i] = values[i];
        }
    }
    public ActivationTimerState this[int index] => index >= 0 && index < Count ?
        _values[index] : throw new ArgumentOutOfRangeException(nameof(index));
}
