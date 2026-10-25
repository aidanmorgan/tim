using System;
using System.Runtime.CompilerServices;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Gpu;

public enum ActivationOccurrenceKind : uint { Contact = 1, TimerElapsed = 2, Orientation = 3, Passage = 4, ThresholdReached = 5, OscillatorTick = 6, ContactLoad = 7 }
public enum ActivationTimerPhase : uint { Ready, Counting, Finished }

/// <summary>Canonical physical boundary time, also used for integer-boundary controller outputs.</summary>
public readonly record struct ActivationTime(uint Ordinal, float Phase) : IComparable<ActivationTime>
{
    public void Validate()
    {
        if (!PhysicalPhase.IsAdmitted(Ordinal, Phase))
            throw new ArgumentException("Invalid activation occurrence time.");
    }
    public int CompareTo(ActivationTime other) =>
        Ordinal != other.Ordinal ? Ordinal.CompareTo(other.Ordinal) : Phase.CompareTo(other.Phase);
    public static bool operator <(ActivationTime left, ActivationTime right) => left.CompareTo(right) < 0;
    public static bool operator <=(ActivationTime left, ActivationTime right) => left.CompareTo(right) <= 0;
    public static bool operator >(ActivationTime left, ActivationTime right) => left.CompareTo(right) > 0;
    public static bool operator >=(ActivationTime left, ActivationTime right) => left.CompareTo(right) >= 0;
    public ulong CeilingTick(uint substeps)
    {
        Validate(); ValidateSubsteps(substeps);
        var tick = Ordinal / substeps;
        return checked((ulong)tick + (Ordinal % substeps != 0 || Phase > 0f ? 1UL : 0UL));
    }
    public static ActivationTime Boundary(SimulationTick tick, uint substeps)
    {
        ValidateSubsteps(substeps);
        return new(checked((uint)(tick.Value * substeps)), 0f);
    }
    private static void ValidateSubsteps(uint value)
    {
        if (value is not (2 or 4 or 8)) throw new ArgumentException("Unsupported activation clock.");
    }
}

/// <summary>Identity of a declared physical event source (contact trigger or orientation sensor) in the document identity space.</summary>
public readonly record struct GpuEventSourceId(ulong Value);

/// <summary>Physical provenance survives routing and delay without impersonating the delayed occurrence.</summary>
public readonly record struct ActivationCause(GpuEventSourceId Source, GpuBodyId Body,
    GpuColliderId Collider, ActivationTime Time, ContactSpeed ApproachSpeed)
{
    public void Validate()
    {
        Time.Validate();
        if (Source.Value == 0 || Body.Value == 0 || Collider.Value == 0 ||
            !float.IsFinite((float)ApproachSpeed.Value) || (float)ApproachSpeed.Value < 0f || (float)ApproachSpeed.Value > 4096f)
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
            (Kind is ActivationOccurrenceKind.Contact or ActivationOccurrenceKind.Orientation or ActivationOccurrenceKind.Passage or ActivationOccurrenceKind.ThresholdReached or ActivationOccurrenceKind.OscillatorTick or ActivationOccurrenceKind.ContactLoad && Time != Cause.Time) ||
            (Kind == ActivationOccurrenceKind.TimerElapsed && Time.Phase != 0f))
            throw new ArgumentException("Invalid activation occurrence.");
    }
}

public readonly record struct ActivationTimerDeclaration(ActivationNodeId Node, uint DurationTicks,
    TimerCompletionPolicy Completion = TimerCompletionPolicy.Latch,
    TimerBoundary Boundary = TimerBoundary.BeforePhysics,
    TimerElapsedSignal ElapsedSignal = TimerElapsedSignal.Activation)
{
    public static uint Duration(float seconds, SimulationCadence cadence)
    {
        if (!float.IsFinite(seconds) || seconds <= 0f) throw new ArgumentException("Invalid timer duration.");
        var rate = cadence switch
        {
            SimulationCadence.Hz60 => 60f,
            SimulationCadence.Hz120 => 120f,
            SimulationCadence.Hz240 => 240f,
            _ => throw new ArgumentException("Unsupported timer cadence.")
        };
        return checked((uint)MathF.Ceiling(seconds * rate));
    }

    public void Validate()
    {
        if (Node.Value == 0 || DurationTicks == 0 || !Enum.IsDefined(Completion) || !Enum.IsDefined(Boundary) || !Enum.IsDefined(ElapsedSignal) ||
            (Completion == TimerCompletionPolicy.Rearm && (ElapsedSignal != TimerElapsedSignal.None || Boundary != TimerBoundary.BeforeNetworks)) ||
            (Completion == TimerCompletionPolicy.Latch && (ElapsedSignal != TimerElapsedSignal.Activation || Boundary != TimerBoundary.BeforePhysics))) throw new ArgumentException("Invalid timer declaration.");
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
    public ActivationTimerState Complete(ActivationTimerDeclaration declaration, SimulationTick boundary, uint substeps, out ActivationOccurrence? occurrence)
    {
        Validate(); declaration.Validate();
        if (Node != declaration.Node) throw new ArgumentException("Timer declaration does not own this state.");
        _ = ActivationTime.Boundary(boundary, substeps); occurrence = null;
        if (Phase != ActivationTimerPhase.Counting || DueTick > boundary.Value) return this;
        if (declaration.ElapsedSignal == TimerElapsedSignal.Activation)
        {
            occurrence = new(ActivationOccurrenceKind.TimerElapsed, Node,
                ActivationTime.Boundary(new(DueTick), substeps), Input.Cause);
            occurrence.Value.Validate();
        }
        return declaration.Completion == TimerCompletionPolicy.Rearm ? Ready(Node) : this with { Phase = ActivationTimerPhase.Finished };
    }
}

/// <summary>One copied timer observation population in the committed world read, sized exactly from the compiled network.</summary>
public readonly struct PhysicsTimerRead
{
    private readonly ActivationTimerState[]? _values;
    public int Count => _values?.Length ?? 0;
    public PhysicsTimerRead(ReadOnlySpan<ActivationTimerState> values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i != 0 && values[i - 1].Node.Value >= values[i].Node.Value)
                throw new ArgumentException("Timer identities must be unique and ordered.");
        }
        _values = values.ToArray();
    }
    public ActivationTimerState this[int index] => index >= 0 && index < Count ?
        _values![index] : throw new ArgumentOutOfRangeException(nameof(index));
}
