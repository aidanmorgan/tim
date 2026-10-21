using System.Collections.Generic;

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
