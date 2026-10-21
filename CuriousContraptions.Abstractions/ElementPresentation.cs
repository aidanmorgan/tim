using System;
using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Abstractions;

[Flags]
public enum PresentationAxes : byte { None = 0, X = 1, Y = 2, Z = 4, XZ = X | Z, All = X | Y | Z }

public enum PresentationProperty : byte
{
    LocalX, LocalY, LocalZ, LocalRotationZ, UniformScale, ScaleX, ScaleY, ScaleZ,
    AlbedoRed, AlbedoGreen, AlbedoBlue, EmissionRed, EmissionGreen, EmissionBlue
}

public enum PresentationDriver : byte
{
    ActivationPulse, ContactImpulse, CapturePulse,
    CommittedActivation, CommittedCount, CommittedOscillatorPhase, CommittedOscillatorPulse,
    CommittedTimerProgress, CommittedTimerPhase, CommittedSupply, CommittedCharge, CommittedActuatorDrive,
    CommittedContactLoadPhase, CommittedActuatorState
}

public enum PresentationCurve : byte { Linear, SmoothStep, SineSquaredPulse, ExponentialFollow, LinearDecay, CubicSmoothStep }
public enum PresentationOverlap : byte { Replace, SaturatingSum, Maximum }
public enum PresentationTimerPhase : byte { None, Ready, Counting, Finished }
public enum PresentationBody : byte { Primary, Secondary }

/// <summary>A package-owned property writer. Threshold selects count/charge levels; phase selects a timer phase.
/// Values and time are f32. One-shot drivers retain their existing relay until the ring-driver cutover.</summary>
public readonly record struct PresentationBinding(
    string Node, PresentationProperty Property, PresentationDriver Driver,
    float From, float To, PresentationCurve Curve, float Duration,
    PresentationOverlap Overlap, float Threshold, PresentationTimerPhase Phase, float Response = 0f, float? Intermediate = null);

public readonly record struct PresentationBodyBinding(string Node, PresentationBody Body, Vector3 RestOffset);
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

