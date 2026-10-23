using System.Collections.Generic;
using System.Numerics;

namespace CuriousContraptions.Abstractions.Signal;

public enum SignalTriggerMode : byte
{
    Edge = 0,
    Level = 1,
    Pulse = 2
}

public readonly record struct SignalTriggerDescriptor(
    string TriggerId,
    SignalTriggerMode Mode,
    float Threshold,
    float PulseDurationSeconds);

public enum SignalLatchMode : byte
{
    None = 0,
    SetReset = 1,
    Toggle = 2
}

public readonly record struct SignalCounterDescriptor(
    int Threshold,
    bool ResetOnThreshold);

public interface ISignalElement : IPuzzleElementDeclaration
{
    IReadOnlyList<SignalTriggerDescriptor> Triggers { get; }
    SignalLatchMode LatchMode { get; }
    SignalCounterDescriptor? Counter { get; }
}

public enum TimerCompletionPolicy : byte { Latch, Rearm }
public enum TimerBoundary : byte { BeforeNetworks, BeforePhysics }
public enum TimerElapsedSignal : byte { None, Activation }

/// <summary>A timed window with declared duration, completion and delivery policies.</summary>
public readonly record struct SignalTimerDescriptor(string DurationParameter, TimerCompletionPolicy Completion,
    TimerBoundary Boundary, TimerElapsedSignal ElapsedSignal);

public interface ITimerElement : ISignalElement
{
    SignalTimerDescriptor Timer { get; }
}

/// <summary>A one-shot owner-body contact qualified by a declared f32 speed parameter.</summary>
public readonly record struct SignalContactDescriptor(string ThresholdParameter);

public interface IContactTriggerElement : ISignalElement
{
    SignalContactDescriptor Contact { get; }
}

/// <summary>An indicator on one declared Signal input that admits a level. Wired from a level output it follows that level; any other
/// wiring (or none) holds its first committed input until Reset.</summary>
public readonly record struct SignalIndicatorDescriptor(PortSocket Input);

public interface IIndicatorElement : ISignalElement
{
    SignalIndicatorDescriptor Indicator { get; }
}

/// <summary>A passage aperture on the owner's static frame, in f32 metres: a body centre crossing the local YZ plane along +X within
/// <see cref="ApertureRadius"/> of the axis fires one pulse, and the body rearms only once it is <see cref="RearmClearance"/> beyond
/// its own radius upstream.</summary>
public readonly record struct SignalPassageDescriptor(float ApertureRadius, float RearmClearance);

public interface IPassageSensorElement : ISignalElement
{
    SignalPassageDescriptor Passage { get; }
}

/// <summary>An orientation threshold on the owner's own dynamic body, in f32 degrees: while the owner's ActivationOut is wired, the body
/// fires one pulse once it has turned <see cref="ThresholdDegrees"/> from its admitted pose, and only a fresh admission rearms it.</summary>
public readonly record struct SignalOrientationDescriptor(float ThresholdDegrees);

public interface IOrientationSensorElement : ISignalElement
{
    SignalOrientationDescriptor Orientation { get; }
}

/// <summary>A contact-load sensor on one face of the owner's static frame, all f32. A dynamic body's resting contact counts when it
/// touches the baked Box collider <see cref="FaceCollider"/> (an index into the element's colliders), its normal toward the body aligns
/// with <see cref="LocalNormal"/> by at least <see cref="MinimumAlignment"/>, its gap is at most <see cref="ContactDistance"/> m, and its
/// point lies within the face's local x–z footprint widened by <see cref="ContactDistance"/> in x and <see cref="DepthMargin"/> in z.
/// Each body counts its mass once and bodies sum; the level ActivationOut is high from the first tick the sum reaches the Real kg
/// parameter <see cref="MinimumMassParameter"/> (inclusive) and is held <see cref="ReleaseHoldSeconds"/> after the last such tick.</summary>
public readonly record struct SignalContactLoadDescriptor(string MinimumMassParameter, int FaceCollider, Vector3 LocalNormal,
    float MinimumAlignment, float ContactDistance, float DepthMargin, float ReleaseHoldSeconds);

public interface IContactLoadSensorElement : ISignalElement
{
    SignalContactLoadDescriptor ContactLoad { get; }
}

/// <summary>A supplied periodic pulse source. While the owner's PowerIn route is supplied by exactly one source, its Pulse ActivationOut
/// pulses once every <see cref="IntervalParameter"/> (a Real second parameter, compiled to whole ticks of the run's cadence rounded up),
/// the first pulse one full interval after supply begins. Losing supply stops it with no pulse, and restored supply starts a fresh
/// interval.</summary>
public readonly record struct SignalOscillatorDescriptor(string IntervalParameter);

public interface IOscillatorElement : ISignalElement
{
    SignalOscillatorDescriptor Oscillator { get; }
}
