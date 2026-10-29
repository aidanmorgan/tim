using System;
using System.Collections.Generic;
using System.Numerics;

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
    CommittedContactLoadPhase, CommittedActuatorState, CommittedTimerRemaining, CommittedTimerActive, CommittedElectricalSocket
}

public enum PresentationCurve : byte { Linear, SmoothStep, SineSquaredPulse, ExponentialFollow, LinearDecay, CubicSmoothStep }
public enum PresentationOverlap : byte { Replace, SaturatingSum, Maximum }
public enum PresentationTimerPhase : byte { None, Ready, Counting, Finished }
/// <summary>Package body ordinal: zero is the authored root; subsequent ordinals name declared child bodies.</summary>
public readonly record struct PackageBodyIndex(int Value);
/// <summary>Which records of its kind cue an event binding: any record, or only one whose Magnitude is positive (an Impact that paid
/// energy, so an unpaid or empty-store hit leaves the cue at rest).</summary>
public enum PresentationEventMagnitude : byte { Any, Positive }

/// <summary>A package-owned property writer. Threshold selects count/charge levels; phase selects a timer phase. An
/// <see cref="PresentationDriver.Event"/> binding names the ring record kind that cues it and which of its records do (Story 9A.25b);
/// every committed driver names <see cref="SimulationEventKind.None"/> and <see cref="PresentationEventMagnitude.Any"/>. Values and time
/// are f32.</summary>
public readonly record struct PresentationBinding(
    string Node, PresentationProperty Property, PresentationDriver Driver,
    float From, float To, PresentationCurve Curve, float Duration,
    PresentationOverlap Overlap, float Threshold, PresentationTimerPhase Phase, float Response = 0f, float? Intermediate = null, PortSocket? Socket = null,
    SimulationEventKind Event = SimulationEventKind.None, PresentationEventMagnitude EventMagnitude = PresentationEventMagnitude.Any);

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
    public static bool Supports(PresentationDriver driver, PresentationCurve curve, float duration, PresentationOverlap overlap) => driver switch
                {
                    // Story 9A.25b: a ring-kind cue replays a Replace endpoint clip or adds a saturating pulse per record.
                    PresentationDriver.Event => duration > 0 &&
                        ((curve == PresentationCurve.SmoothStep && overlap == PresentationOverlap.Replace) ||
                         (curve == PresentationCurve.SineSquaredPulse && overlap == PresentationOverlap.SaturatingSum)),
                    PresentationDriver.CommittedActivation or
                        PresentationDriver.CommittedCount or PresentationDriver.CommittedElectricalSocket => curve == PresentationCurve.SmoothStep && duration > 0 && overlap == PresentationOverlap.Replace,
                    PresentationDriver.CommittedOscillatorPhase => curve == PresentationCurve.ExponentialFollow && duration == 0 && overlap == PresentationOverlap.Replace,
                    PresentationDriver.CommittedOscillatorPulse => curve == PresentationCurve.LinearDecay && duration > 0 && overlap == PresentationOverlap.Maximum,
                    PresentationDriver.CommittedContactLoadPhase => curve == PresentationCurve.CubicSmoothStep && duration > 0 && overlap == PresentationOverlap.Replace,
                    PresentationDriver.CommittedTimerActive => overlap == PresentationOverlap.Replace &&
                        ((curve == PresentationCurve.Linear && duration == 0) || (curve == PresentationCurve.SmoothStep && duration > 0)),
                    PresentationDriver.CommittedTimerRemaining or PresentationDriver.CommittedTimerProgress or PresentationDriver.CommittedTimerPhase or PresentationDriver.CommittedSupply or
                        PresentationDriver.CommittedCharge or PresentationDriver.CommittedActuatorDrive or PresentationDriver.CommittedActuatorState =>
                            curve == PresentationCurve.Linear && duration == 0 && overlap == PresentationOverlap.Replace,
                    _ => false
                };

}

