using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Abstractions;

[Flags]
public enum PresentationAxes : byte { None = 0, X = 1, Y = 2, Z = 4, XZ = X | Z, All = X | Y | Z }

public enum PresentationProperty : byte
{
    LocalX, LocalY, LocalZ, LocalRotationZ, UniformScale, ScaleX, ScaleY, ScaleZ,
    AlbedoRed, AlbedoGreen, AlbedoBlue, EmissionRed, EmissionGreen, EmissionBlue, Visible
}

public enum PresentationDriver : byte
{
    /// <summary>A one-shot cue started by each event ring record of <see cref="PresentationBinding.Event"/> from the owning instance.</summary>
    Event,
    CommittedActivation, CommittedCount, CommittedOscillatorPhase, CommittedOscillatorPulse,
    CommittedTimerProgress, CommittedTimerPhase, CommittedSupply, CommittedCharge, CommittedActuatorDrive,
    CommittedContactLoadPhase, CommittedActuatorState, CommittedTimerRemaining, CommittedTimerActive, CommittedElectricalSocket,
    CommittedWheelAngle,
    /// <summary>The owner's committed compliant-surface deflection in metres (0 at rest): the value is From + (To − From) · deflection.</summary>
    CommittedSurfaceDeflection
}

public enum PresentationValueMapping : byte { Identity = 1, SineCyclesToUnit = 2, StateValues = 3 }

public enum ActuatorObservationState : byte { Unpowered, Powered, Blocked }

public readonly record struct PresentationStateValues(float State0, float State1, float State2)
{
    public bool IsFinite() => float.IsFinite(State0) && float.IsFinite(State1) && float.IsFinite(State2);

    public float Map(ActuatorObservationState state) => state switch
    {
        ActuatorObservationState.Unpowered => State0,
        ActuatorObservationState.Powered => State1,
        ActuatorObservationState.Blocked => State2,
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };

    public float Map(ContactLoadPhase phase) => phase switch
    {
        ContactLoadPhase.Empty => State0,
        ContactLoadPhase.Loaded => State1,
        ContactLoadPhase.Underweight => State2,
        _ => throw new ArgumentOutOfRangeException(nameof(phase))
    };

    public float Map(ActivationTimerPhase phase) => phase switch
    {
        ActivationTimerPhase.Ready => State0,
        ActivationTimerPhase.Counting => State1,
        ActivationTimerPhase.Finished => State2,
        _ => throw new ArgumentOutOfRangeException(nameof(phase))
    };
}

public enum PresentationCurve : byte { Linear, SmoothStep, SineSquaredPulse, ExponentialFollow, LinearDecay, CubicSmoothStep }
public enum PresentationOverlap : byte { Replace, SaturatingSum, Maximum }

/// <summary>Package body ordinal: zero is the authored root; subsequent ordinals name declared child bodies.</summary>
public readonly record struct PackageBodyIndex(int Value);
/// <summary>Which records of its kind cue an event binding: any record, or only one whose Magnitude is positive (an Impact that paid
/// energy, so an unpaid or empty-store hit leaves the cue at rest).</summary>
public enum PresentationEventMagnitude : byte { Any, Positive }

/// <summary>A package-owned property writer. Threshold selects count/charge levels. An
/// <see cref="PresentationDriver.Event"/> binding names the ring record kind that cues it and which of its records do (Story 9A.25b);
/// every committed driver names <see cref="SimulationEventKind.None"/> and <see cref="PresentationEventMagnitude.Any"/>. Values and time
/// are f32.</summary>
public readonly record struct PresentationBinding(
    string Node, PresentationProperty Property, PresentationDriver Driver, PresentationValueMapping Mapping,
    float From, float To, PresentationCurve Curve, float Duration,
    PresentationOverlap Overlap, float Threshold, float Response = 0f, PortId? Socket = null,
    SimulationEventKind Event = SimulationEventKind.None, PresentationEventMagnitude EventMagnitude = PresentationEventMagnitude.Any, SignalNodeId? SignalNode = null,
    CoordinateReference? Actuator = null, PortId? GuidePort = null, PresentationStateValues? StateValues = null, Electrical.LocalSupplyNodeId? SupplyNode = null);

public readonly record struct PresentationBodyBinding(string Node, PackageBodyIndex Body, Vector3 RestOffset);
public readonly record struct PresentationSpanBinding(
    string MovingNode, Vector3 MovingAnchor, string SpanNode, Vector3 FixedAnchor, float RestLength);

/// <summary>An authored parameter changes a node component through multiplier * value + offset.</summary>
public readonly record struct PresentationParameterBinding(
    string Node, PresentationProperty Property, string Parameter, float Multiplier, float Offset);

public readonly record struct PresentationResizeParameter(string Parameter, PresentationAxes Axis);
public sealed record PresentationSizing(Vector3 Dimensions, Vector3 RadiusScale, Vector3 RadiusOffset,
    IReadOnlyList<PresentationResizeParameter> Parameters);

public sealed record ElementPresentation(
    float PickRadius, PresentationAxes ResizeAxes,
    IReadOnlyList<PresentationBinding> Bindings,
    IReadOnlyList<PresentationBodyBinding> Bodies,
    IReadOnlyList<PresentationSpanBinding> Spans,
    IReadOnlyList<PresentationParameterBinding> Parameters, PresentationSizing? Sizing = null);


/// <summary>The single supported driver/curve contract shared by package and compiled admission.</summary>
public static class PresentationDriverRules
{
    public static bool SignalSource(PresentationDriver driver, SimulationEventKind kind) => driver is
        PresentationDriver.CommittedActivation or PresentationDriver.CommittedCount or PresentationDriver.CommittedOscillatorPhase or
        PresentationDriver.CommittedOscillatorPulse or PresentationDriver.CommittedTimerProgress or PresentationDriver.CommittedTimerPhase or
        PresentationDriver.CommittedTimerRemaining or PresentationDriver.CommittedTimerActive or PresentationDriver.CommittedContactLoadPhase ||
        driver == PresentationDriver.Event && SimulationEvents.SignalSource(kind);

    public static bool ContinuousNormalized(PresentationDriver driver) => driver is PresentationDriver.CommittedOscillatorPhase or
        PresentationDriver.CommittedTimerProgress or PresentationDriver.CommittedTimerRemaining;

    public static bool Discrete3State(PresentationDriver driver) => driver is
        PresentationDriver.CommittedActuatorState or
        PresentationDriver.CommittedContactLoadPhase or
        PresentationDriver.CommittedTimerPhase;

    public static bool ContinuousPhysical(PresentationDriver driver) => driver is
        PresentationDriver.CommittedWheelAngle or
        PresentationDriver.CommittedSurfaceDeflection or
        PresentationDriver.CommittedTimerProgress or
        PresentationDriver.CommittedTimerRemaining or
        PresentationDriver.CommittedOscillatorPhase;

    public static bool LevelDriver(PresentationDriver driver) => driver is
        PresentationDriver.CommittedActivation or
        PresentationDriver.CommittedCount or
        PresentationDriver.CommittedElectricalSocket or
        PresentationDriver.CommittedSupply or
        PresentationDriver.CommittedCharge or
        PresentationDriver.CommittedActuatorDrive or
        PresentationDriver.CommittedTimerActive;

    public static bool SupportsMapping(PresentationDriver driver, PresentationValueMapping mapping) => mapping switch
    {
        PresentationValueMapping.StateValues => Discrete3State(driver),
        PresentationValueMapping.SineCyclesToUnit => ContinuousNormalized(driver),
        PresentationValueMapping.Identity => !Discrete3State(driver),
        _ => false
    };

    /// <summary>Construction/Reset projection: zero scalar input or the declared State0 state.</summary>
    public static float InitialValue(PresentationBinding binding) =>
        InitialValue(binding.Mapping, binding.From, binding.To, binding.StateValues);

    public static float InitialValue(CompiledPresentationBinding binding) =>
        InitialValue(binding.Mapping, binding.From, binding.To, binding.StateValues);

    public static float InitialValue(PresentationValueMapping mapping, float from, float to, PresentationStateValues? stateValues)
    {
        var amount = mapping == PresentationValueMapping.StateValues
            ? stateValues!.Value.State0
            : Map(0, mapping);
        return float.Lerp(from, to, amount);
    }

    public static float Map(float input, PresentationValueMapping mapping)
    {
        if (!float.IsFinite(input)) throw new ArgumentException("Nonfinite presentation input.");
        return mapping switch
        {
            PresentationValueMapping.Identity => input,
            PresentationValueMapping.SineCyclesToUnit => .5f + .5f * MathF.Sin(MathF.Tau * input),
            _ => throw new ArgumentOutOfRangeException(nameof(mapping))
        };
    }

    public static bool Supports(PresentationDriver driver, PresentationCurve curve, float duration, PresentationOverlap overlap)
    {
        if (driver == PresentationDriver.Event)
        {
            return duration > 0 &&
                ((curve is PresentationCurve.SmoothStep or PresentationCurve.CubicSmoothStep && overlap == PresentationOverlap.Replace) ||
                 (curve == PresentationCurve.SineSquaredPulse && overlap == PresentationOverlap.SaturatingSum));
        }
        if (driver == PresentationDriver.CommittedOscillatorPulse)
        {
            return duration > 0 && curve == PresentationCurve.LinearDecay && overlap == PresentationOverlap.Maximum;
        }
        if (overlap != PresentationOverlap.Replace) return false;
        if (ContinuousPhysical(driver))
        {
            return (curve is PresentationCurve.Linear or PresentationCurve.ExponentialFollow) && duration == 0;
        }
        if (Discrete3State(driver))
        {
            return curve == PresentationCurve.Linear && duration == 0;
        }
        if (LevelDriver(driver))
        {
            return curve switch
            {
                PresentationCurve.Linear => duration == 0,
                PresentationCurve.SmoothStep or PresentationCurve.CubicSmoothStep => duration > 0,
                _ => false
            };
        }
        return false;
    }
}
