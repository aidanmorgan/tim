using CuriousContraptions.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Gpu;

public readonly record struct ActivationNodeId(ulong Value);
/// <summary>StickyFirstInput is a declared indicator's first-input-wins hold (never reset within a run); StateLatch is the bistable
/// Set/Reset memory (CAT-037); LevelIndicator is a declared indicator whose input is wired from a level ActivationOut (the latch or
/// a declared contact-load sensor), mirroring it. PassageSource is a declared passage aperture's crossing pulse.
/// ContactLoadSource is a declared contact-load row's committed level (Story 9R.4 D3): Latched from its first Loaded tick until its
/// committed release hold (Story 9R.5, ReleaseHoldTicks after the last Loaded tick) expires.
/// The kinds are distinct and share no name.</summary>
public enum ActivationNodeKind : uint { ContactSource = 1, StickyFirstInput = 2, Timer = 3, OrientationSource = 4, PassageSource = 5, Counter = 6, Oscillator = 7, StateLatch = 8, LevelIndicator = 9, ContactLoadSource = 10 }
public enum ActivationPhase : uint { Clear, Latched }
/// <summary>Committed bistable memory of one State Latch.</summary>
public enum SimulationLatchPhase : uint { Off = 0, On = 1 }
/// <summary>Requests a State Latch accumulates within one tick; Reset dominates Set at the boundary.</summary>
[Flags]
public enum StateLatchRequest : uint { None = 0, Set = 1, Reset = 2 }
public readonly record struct ActivationNodeDeclaration(ActivationNodeId Id, GpuBodyId Owner,
    ActivationNodeKind Kind, GpuContactTriggerId Trigger, uint DurationTicks = 0, GpuOrientationSensorId Sensor = default, GpuPassageSensorId Passage = default, uint TargetCount = 0, uint IntervalTicks = 0, SupplyNodeId PowerProvider = default,
    GpuContactLoadSensorId ContactLoad = default, uint ReleaseHoldTicks = 0,
    TimerCompletionPolicy Completion = TimerCompletionPolicy.Latch, TimerBoundary Boundary = TimerBoundary.BeforePhysics,
    TimerElapsedSignal ElapsedSignal = TimerElapsedSignal.Activation);
/// <summary>One routed signal; Input names the target socket (ActivationIn, or SetIn/ResetIn on a State Latch).</summary>
public readonly record struct CompiledSignalContact(SupplyNodeId Contact, GpuBodyId Owner, ActivationNodeId Node, SignalContactState State);
public readonly record struct ActivationEdge(ActivationNodeId Source, ActivationNodeId Target, PortRole Input);

public readonly record struct ActivationLatch(ActivationNodeId Node, GpuBodyId Owner, ActivationPhase Phase,
    GpuEventSourceId Source, GpuBodyId Body, GpuColliderId Collider, uint EventOrdinal,
    float EventPhase, ContactSpeed ApproachSpeed, ActivationOccurrenceKind Kind = default,
    ActivationNodeId Emitter = default, uint CauseOrdinal = 0, float CausePhase = default, ActivationCauseKind CauseKind = ActivationCauseKind.Unknown)
{
    public ActivationOccurrence Occurrence => new(Kind, Emitter, new(EventOrdinal, EventPhase),
        new(Source, Body, Collider, new(CauseOrdinal, CausePhase), ApproachSpeed, CauseKind));
    public static ActivationLatch From(ActivationNodeDeclaration node, ActivationOccurrence occurrence) =>
        new(node.Id, node.Owner, ActivationPhase.Latched, occurrence.Cause.Source, occurrence.Cause.Body,
            occurrence.Cause.Collider, occurrence.Time.Ordinal, occurrence.Time.Phase, occurrence.Cause.ApproachSpeed,
            occurrence.Kind, occurrence.Emitter, occurrence.Cause.Time.Ordinal, occurrence.Cause.Time.Phase, occurrence.Cause.Kind);
    public static ActivationLatch Clear(ActivationNodeDeclaration node) => new(node.Id, node.Owner,
        ActivationPhase.Clear, default, default, default, 0, 0f, new(0f));
    public void Validate()
    {
        if (Node.Value == 0 || Owner.Value == 0 || !Enum.IsDefined(Phase) ||
            !PhysicalPhase.IsAdmitted(EventOrdinal, EventPhase) ||
            !float.IsFinite(ApproachSpeed.Value) || ApproachSpeed.Value < 0f || ApproachSpeed.Value > 4096f)
            throw new ArgumentException("Invalid activation latch.");
        if (Phase == ActivationPhase.Clear)
        {
            if (Source.Value != 0 || Body.Value != 0 || Collider.Value != 0 || EventOrdinal != 0 ||
                EventPhase != 0f || ApproachSpeed.Value != 0f ||
                CauseKind != ActivationCauseKind.Unknown || Kind != 0 || Emitter.Value != 0 || CauseOrdinal != 0 || CausePhase != 0f)
                throw new ArgumentException("Clear activation retained an event.");
        }
        else Occurrence.Validate();
    }
}

/// <summary>The committed activation population, sized exactly from the compiled network (no fixed activation table; owner
/// decision, 20 Oct 2026; Story 9R.5b: no instance bound either).</summary>
public readonly struct PhysicsActivationRead
{
    private readonly ActivationLatch[]? _values;
    public int Count => _values?.Length ?? 0;
    public PhysicsActivationRead(ReadOnlySpan<ActivationLatch> values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i != 0 && values[i - 1].Node.Value >= values[i].Node.Value)
                throw new ArgumentException("Activation nodes must be uniquely ordered.");
        }
        _values = values.ToArray();
    }
    public ActivationLatch this[int index] => index >= 0 && index < Count ? _values![index] : throw new ArgumentOutOfRangeException(nameof(index));
}

/// <summary>Emitted: a node emitted an occurrence at this boundary. LevelChanged: a level node's committed level rose or released.</summary>
public enum ActivationTransitionKind : uint { Emitted = 1, LevelChanged = 2 }

/// <summary>One discrete transition a Physical-stage candidate made (Story 9A.22): never committed state. Count is the emitting
/// node's memory-row count after the emission (0 without a row); Level is the new level of a LevelChanged node; Occurrence is the
/// emitted occurrence, or the occurrence a rising level holds (default when a level releases).</summary>
public readonly record struct ActivationTransition(ActivationTransitionKind Kind, ActivationNodeId Node,
    ActivationOccurrence Occurrence, uint Count, ActivationPhase Level);

/// <summary>The transitions of one candidate, in the order the network made them.</summary>
public readonly struct ActivationTransitionRead
{
    private readonly ActivationTransition[]? _values;
    public int Count => _values?.Length ?? 0;
    public ActivationTransitionRead(ReadOnlySpan<ActivationTransition> values) => _values = values.ToArray();
    public ActivationTransition this[int index] => index >= 0 && index < Count ? _values![index] : throw new ArgumentOutOfRangeException(nameof(index));
}

/// <summary>The committed discrete checkpoint: activations, timers and the Counter/oscillator memory rows (Story 9R.1 D3), with the
/// transitions the Physical stage made to reach it (empty at admission and in the scheduled stage; never committed).</summary>
public readonly record struct ActivationCheckpoint(PhysicsActivationRead Activations, PhysicsTimerRead Timers,
    PhysicsMemoryRead Memory = default, ActivationTransitionRead Transitions = default);
