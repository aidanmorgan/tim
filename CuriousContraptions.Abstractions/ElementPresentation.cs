using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Presentation;

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
    CommittedSurfaceDeflection,
    CommittedTransmissionClosure,
    CommittedTransmissionPhase,
    CommittedRevoluteAngle,
    CommittedPrismaticPhase,
    CommittedRetentionReleased,
    CommittedStoredFraction,
    CommittedFiniteWorkState,
    CommittedGasRadius,
    CommittedFlowSpeed
}

public enum PresentationValueMapping : byte { Identity = 1, SineCyclesToUnit = 2, StateValues = 3, ClampToEndpoints = 4 }

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

public readonly record struct PresentationFourStateValues(float State0, float State1, float State2, float State3)
{
    public bool IsFinite() => float.IsFinite(State0) && float.IsFinite(State1) && float.IsFinite(State2) && float.IsFinite(State3);

    public float Map(Mechanical.TransmissionEngagementPhase phase) => phase switch
    {
        Mechanical.TransmissionEngagementPhase.Open => State0,
        Mechanical.TransmissionEngagementPhase.Opening => State3,
        Mechanical.TransmissionEngagementPhase.Closing => State1,
        Mechanical.TransmissionEngagementPhase.Engaged => State2,
        _ => throw new ArgumentOutOfRangeException(nameof(phase))
    };
}

public enum PresentationCurve : byte { Linear, SmoothStep, SineSquaredPulse, ExponentialFollow, LinearDecay, CubicSmoothStep, SmoothRiseFall, IntegratedRate }
public enum PresentationOverlap : byte { Replace, SaturatingSum, Maximum, HyperbolicTangentSum }

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
    CoordinateReference? Actuator = null, PortId? GuidePort = null, PresentationStateValues? StateValues = null,
    Electrical.LocalSupplyNodeId? SupplyNode = null, Mechanical.LocalMechanicalTransmissionId? Transmission = null,
    PresentationFourStateValues? FourStateValues = null, PrismaticPhaseValues? PrismaticValues = null, Mechanical.LocalPrismaticRetentionId? Retention = null, LocalFiniteWorkActuatorId? FiniteWork = null,
    LocalEnergyStoreId? Store = null, PresentationFiniteWorkValues? FiniteWorkValues = null, AnimationPeakPhase? ImpulsePeak = null,
    AnimationImpulseVisibility? ImpulseVisibility = null, AnimationImpulseTiming? ImpulseTiming = null,
    LocalColliderReference? GasShape = null, LocalIntegrityObservationId? Integrity = null,
    [property: System.Text.Json.Serialization.JsonRequired] LocalAirflowSourceId? Airflow = null,
    [property: System.Text.Json.Serialization.JsonRequired] ChoicePredicate? Selection = null);

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
        PresentationDriver.CommittedTimerProgress or PresentationDriver.CommittedTimerRemaining or
        PresentationDriver.CommittedTransmissionClosure or PresentationDriver.CommittedStoredFraction;

    public static bool Discrete3State(PresentationDriver driver) => driver is
        PresentationDriver.CommittedActuatorState or
        PresentationDriver.CommittedContactLoadPhase or
        PresentationDriver.CommittedTimerPhase;

    public static bool Discrete4State(PresentationDriver driver) => driver == PresentationDriver.CommittedTransmissionPhase;

    public static bool ContinuousPhysical(PresentationDriver driver) => driver is
        PresentationDriver.CommittedGasRadius or PresentationDriver.CommittedFlowSpeed or
        PresentationDriver.CommittedWheelAngle or
        PresentationDriver.CommittedRevoluteAngle or
        PresentationDriver.CommittedSurfaceDeflection or
        PresentationDriver.CommittedTransmissionClosure or
        PresentationDriver.CommittedTimerProgress or
        PresentationDriver.CommittedTimerRemaining or
        PresentationDriver.CommittedOscillatorPhase or PresentationDriver.CommittedStoredFraction;

    public static bool LevelDriver(PresentationDriver driver) => driver is
        PresentationDriver.CommittedActivation or
        PresentationDriver.CommittedCount or
        PresentationDriver.CommittedElectricalSocket or
        PresentationDriver.CommittedSupply or
        PresentationDriver.CommittedCharge or
        PresentationDriver.CommittedActuatorDrive or
        PresentationDriver.CommittedTimerActive or
        PresentationDriver.CommittedRetentionReleased;

    public static bool SupportsMapping(PresentationDriver driver, PresentationValueMapping mapping) => mapping switch
    {
        PresentationValueMapping.StateValues => Discrete3State(driver) || Discrete4State(driver) || driver is PresentationDriver.CommittedPrismaticPhase or PresentationDriver.CommittedFiniteWorkState,
        PresentationValueMapping.SineCyclesToUnit => ContinuousNormalized(driver),
        PresentationValueMapping.ClampToEndpoints => ContinuousNormalized(driver) || ContinuousPhysical(driver),
        PresentationValueMapping.Identity => !Discrete3State(driver) && !Discrete4State(driver) && driver is not (PresentationDriver.CommittedPrismaticPhase or PresentationDriver.CommittedFiniteWorkState),
        _ => false
    };

    /// <summary>Construction/Reset projection: zero scalar input or the declared State0 state.</summary>
    public static float InitialValue(PresentationBinding binding) =>
        binding.Curve == PresentationCurve.IntegratedRate ? 0f :
            InitialValue(binding.Mapping, binding.From, binding.To, binding.StateValues, binding.FourStateValues, binding.PrismaticValues, binding.FiniteWorkValues);

    public static float InitialValue(CompiledPresentationBinding binding) =>
        binding.Curve == PresentationCurve.IntegratedRate ? 0f :
            InitialValue(binding.Mapping, binding.From, binding.To, binding.StateValues, binding.FourStateValues, binding.PrismaticValues, binding.FiniteWorkValues);

    public static float InitialValue(PresentationValueMapping mapping, float from, float to, PresentationStateValues? stateValues,
        PresentationFourStateValues? fourStateValues, PrismaticPhaseValues? prismaticValues = null, PresentationFiniteWorkValues? finiteWorkValues = null)
    {
        var amount = mapping == PresentationValueMapping.StateValues
            ? (finiteWorkValues?.Empty ?? prismaticValues?.Unpowered ?? fourStateValues?.State0 ?? stateValues!.Value.State0)
            : Map(0, mapping);
        return Output(amount, mapping, from, to);
    }

    public static float Map(float input, PresentationValueMapping mapping)
    {
        if (!float.IsFinite(input)) throw new ArgumentException("Nonfinite presentation input.");
        return mapping switch
        {
            PresentationValueMapping.Identity or PresentationValueMapping.ClampToEndpoints => input,
            PresentationValueMapping.SineCyclesToUnit => .5f + .5f * MathF.Sin(MathF.Tau * input),
            _ => throw new ArgumentOutOfRangeException(nameof(mapping))
        };
    }

    public static bool AdmitsPropertyValue(PresentationProperty property, float value) => float.IsFinite(value) && (property switch
    {
        PresentationProperty.Visible => value is 0f or 1f,
        PresentationProperty.UniformScale or PresentationProperty.ScaleX or PresentationProperty.ScaleY or PresentationProperty.ScaleZ => value > 0f,
        PresentationProperty.AlbedoRed or PresentationProperty.AlbedoGreen or PresentationProperty.AlbedoBlue or
            PresentationProperty.EmissionRed or PresentationProperty.EmissionGreen or PresentationProperty.EmissionBlue => value is >= 0f and <= 1f,
        _ => true
    });

    public static bool ValidPropertyValues(CompiledPresentationBinding d) =>
        (d.Curve != PresentationCurve.IntegratedRate || d.Property == PresentationProperty.LocalRotationZ && d.Mapping == PresentationValueMapping.Identity) &&
        AdmitsPropertyValue(d.Property, d.From) && AdmitsPropertyValue(d.Property, d.To) &&
        AdmitsStateValues(d.Property, d.From, d.To, d.StateValues) && AdmitsStateValues(d.Property, d.From, d.To, d.FourStateValues) &&
        AdmitsStateValues(d.Property, d.From, d.To, d.PrismaticValues) && AdmitsStateValues(d.Property, d.From, d.To, d.FiniteWorkValues);

    public static bool AdmitsStateValues(PresentationProperty property, float from, float to, PresentationStateValues? values) =>
        values is not { } s || AdmitsPropertyValue(property, Output(s.State0, PresentationValueMapping.StateValues, from, to)) &&
        AdmitsPropertyValue(property, Output(s.State1, PresentationValueMapping.StateValues, from, to)) && AdmitsPropertyValue(property, Output(s.State2, PresentationValueMapping.StateValues, from, to));

    public static bool AdmitsStateValues(PresentationProperty property, float from, float to, PresentationFourStateValues? values) =>
        values is not { } s || AdmitsPropertyValue(property, Output(s.State0, PresentationValueMapping.StateValues, from, to)) &&
        AdmitsPropertyValue(property, Output(s.State1, PresentationValueMapping.StateValues, from, to)) && AdmitsPropertyValue(property, Output(s.State2, PresentationValueMapping.StateValues, from, to)) &&
        AdmitsPropertyValue(property, Output(s.State3, PresentationValueMapping.StateValues, from, to));

    public static bool AdmitsStateValues(PresentationProperty property, float from, float to, PrismaticPhaseValues? values) =>
        values is not { } s || AdmitsPropertyValue(property, Output(s.Holding, PresentationValueMapping.StateValues, from, to)) &&
        AdmitsPropertyValue(property, Output(s.Positive, PresentationValueMapping.StateValues, from, to)) && AdmitsPropertyValue(property, Output(s.Negative, PresentationValueMapping.StateValues, from, to)) &&
        AdmitsPropertyValue(property, Output(s.Blocked, PresentationValueMapping.StateValues, from, to)) && AdmitsPropertyValue(property, Output(s.Conflict, PresentationValueMapping.StateValues, from, to)) &&
        AdmitsPropertyValue(property, Output(s.Unpowered, PresentationValueMapping.StateValues, from, to));

    public static bool AdmitsStateValues(PresentationProperty property, float from, float to, PresentationFiniteWorkValues? values) =>
        values is not { } s || AdmitsPropertyValue(property, Output(s.Empty, PresentationValueMapping.StateValues, from, to)) &&
        AdmitsPropertyValue(property, Output(s.Loading, PresentationValueMapping.StateValues, from, to)) && AdmitsPropertyValue(property, Output(s.SeatedUncharged, PresentationValueMapping.StateValues, from, to)) &&
        AdmitsPropertyValue(property, Output(s.SeatedCharged, PresentationValueMapping.StateValues, from, to)) && AdmitsPropertyValue(property, Output(s.Departing, PresentationValueMapping.StateValues, from, to)) &&
        AdmitsPropertyValue(property, Output(s.Unavailable, PresentationValueMapping.StateValues, from, to));

    public static float Output(float input, PresentationValueMapping mapping, float from, float to) =>
        mapping == PresentationValueMapping.ClampToEndpoints ? Math.Clamp(input, from, to) : float.Lerp(from, to, input);

    public static bool IsImpulseCurve(PresentationCurve curve) => curve is
        PresentationCurve.LinearDecay or PresentationCurve.SineSquaredPulse or PresentationCurve.SmoothRiseFall;

    public static AnimationImpulseCurve ImpulseCurve(PresentationCurve curve) => curve switch
    {
        PresentationCurve.LinearDecay => AnimationImpulseCurve.LinearDecay,
        PresentationCurve.SineSquaredPulse => AnimationImpulseCurve.SineSquaredPulse,
        PresentationCurve.SmoothRiseFall => AnimationImpulseCurve.SmoothRiseFall,
        _ => throw new ArgumentOutOfRangeException(nameof(curve))
    };

    public static AnimationImpulseOverlap ImpulseOverlap(PresentationOverlap overlap) => overlap switch
    {
        PresentationOverlap.Maximum => AnimationImpulseOverlap.Maximum,
        PresentationOverlap.SaturatingSum => AnimationImpulseOverlap.SaturatingSum,
        PresentationOverlap.HyperbolicTangentSum => AnimationImpulseOverlap.HyperbolicTangentSum,
        _ => throw new ArgumentOutOfRangeException(nameof(overlap))
    };

    public static bool ValidImpulsePeak(PresentationCurve curve, AnimationPeakPhase? peak) =>
        IsImpulseCurve(curve) ? peak is { } value && AnimationImpulseDefinition.SupportsPeak(ImpulseCurve(curve), value) : peak is null;

    public static bool ValidImpulseChoices(PresentationCurve curve, AnimationImpulseVisibility? visibility, AnimationImpulseTiming? timing) =>
        IsImpulseCurve(curve) ? visibility is { } v && Enum.IsDefined(v) && timing is { } t && Enum.IsDefined(t)
            : visibility is null && timing is null;

    public static bool Supports(PresentationDriver driver, PresentationCurve curve, float duration, PresentationOverlap overlap)
    {
        if (driver == PresentationDriver.Event)
        {
            return duration > 0 &&
                ((curve is PresentationCurve.SmoothStep or PresentationCurve.CubicSmoothStep && overlap == PresentationOverlap.Replace) ||
                 (IsImpulseCurve(curve) && overlap is PresentationOverlap.Maximum or PresentationOverlap.SaturatingSum or PresentationOverlap.HyperbolicTangentSum));
        }
        if (driver == PresentationDriver.CommittedOscillatorPulse)
        {
            return duration > 0 && curve == PresentationCurve.LinearDecay && overlap is PresentationOverlap.Maximum or PresentationOverlap.SaturatingSum or PresentationOverlap.HyperbolicTangentSum;
        }
        if (overlap != PresentationOverlap.Replace) return false;
        if (ContinuousPhysical(driver))
        {
            return (curve is PresentationCurve.Linear or PresentationCurve.ExponentialFollow or PresentationCurve.IntegratedRate) && duration == 0;
        }
        if (Discrete3State(driver) || Discrete4State(driver) || driver is PresentationDriver.CommittedPrismaticPhase or PresentationDriver.CommittedFiniteWorkState)
        {
            return curve == PresentationCurve.Linear && duration == 0;
        }
        if (LevelDriver(driver))
        {
            return curve switch
            {
                PresentationCurve.Linear => duration >= 0,
                PresentationCurve.SmoothStep or PresentationCurve.CubicSmoothStep => duration > 0,
                _ => false
            };
        }
        return false;
    }
}
