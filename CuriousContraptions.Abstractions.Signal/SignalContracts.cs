using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Abstractions.Signal;

public interface ISignalElement : IPuzzleElementDeclaration
{
    IReadOnlyList<SignalNodeDescriptor> SignalNodes { get; }
    IReadOnlyList<SignalEdge> SignalEdges { get; }
    IReadOnlyList<SignalPortBinding> SignalPorts { get; }
}

public enum TimerCompletionPolicy : byte { Latch, Rearm }
public enum TimerBoundary : byte { BeforeNetworks, BeforePhysics }
public enum TimerElapsedSignal : byte { None, Activation }
public enum SignalTerminal : byte { Input, Set, Reset, Output }
public enum SignalNodeKind : byte { Contact, Indicator, Timer, Orientation, Passage, ContactLoad, Oscillator, PulseCount, Memory }

public readonly record struct SignalEndpoint(SignalNodeId Node, SignalTerminal Terminal);
public readonly record struct SignalEdge(SignalEndpoint Source, SignalEndpoint Target);
public readonly record struct SignalPortBinding(PortId Port, SignalEndpoint Endpoint);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Operation")]
[JsonDerivedType(typeof(SignalContactDescriptor), "Contact")]
[JsonDerivedType(typeof(SignalIndicatorDescriptor), "Indicator")]
[JsonDerivedType(typeof(SignalTimerDescriptor), "Timer")]
[JsonDerivedType(typeof(SignalOrientationDescriptor), "Orientation")]
[JsonDerivedType(typeof(SignalPassageDescriptor), "Passage")]
[JsonDerivedType(typeof(SignalContactLoadDescriptor), "ContactLoad")]
[JsonDerivedType(typeof(SignalOscillatorDescriptor), "Oscillator")]
[JsonDerivedType(typeof(SignalPulseCountDescriptor), "PulseCount")]
[JsonDerivedType(typeof(SignalMemoryDescriptor), "Memory")]
public abstract record SignalNodeDescriptor(SignalNodeId Id)
{
    [JsonIgnore]
    public SignalNodeKind Kind => this switch
    {
        SignalContactDescriptor => SignalNodeKind.Contact,
        SignalIndicatorDescriptor => SignalNodeKind.Indicator,
        SignalTimerDescriptor => SignalNodeKind.Timer,
        SignalOrientationDescriptor => SignalNodeKind.Orientation,
        SignalPassageDescriptor => SignalNodeKind.Passage,
        SignalContactLoadDescriptor => SignalNodeKind.ContactLoad,
        SignalOscillatorDescriptor => SignalNodeKind.Oscillator,
        SignalPulseCountDescriptor => SignalNodeKind.PulseCount,
        SignalMemoryDescriptor => SignalNodeKind.Memory,
        _ => throw new System.InvalidOperationException("Unsupported signal operation.")
    };
}

public sealed record SignalTimerDescriptor(SignalNodeId Id, ParameterId DurationParameter,
    TimerCompletionPolicy Completion, TimerBoundary Boundary, TimerElapsedSignal ElapsedSignal) : SignalNodeDescriptor(Id);
public sealed record SignalContactDescriptor(SignalNodeId Id, PackageBodyIndex Body, ParameterId ThresholdParameter,
    TargetSelector Target = default) : SignalNodeDescriptor(Id);
public sealed record SignalIndicatorDescriptor(SignalNodeId Id) : SignalNodeDescriptor(Id);
public sealed record SignalPassageDescriptor(SignalNodeId Id, PackageBodyIndex Frame, Vector3 LocalPosition,
    Quaternion LocalRotation, float ApertureRadius, float RearmClearance) : SignalNodeDescriptor(Id);
public sealed record SignalOrientationDescriptor(SignalNodeId Id, PackageBodyIndex Body,
    float ThresholdDegrees) : SignalNodeDescriptor(Id);
public sealed record SignalContactLoadDescriptor(SignalNodeId Id, PackageBodyIndex Frame, ParameterId MinimumMassParameter,
    int RegionCollider, Vector3 LocalNormal, float MinimumAlignment, float ContactDistance, float DepthMargin,
    float ReleaseHoldSeconds, IReadOnlyList<int> SupportingColliders) : SignalNodeDescriptor(Id);
public sealed record SignalOscillatorDescriptor(SignalNodeId Id, ParameterId IntervalParameter,
    PortId SupplyInput) : SignalNodeDescriptor(Id);
public sealed record SignalPulseCountDescriptor(SignalNodeId Id, ParameterId TargetParameter) : SignalNodeDescriptor(Id);
public sealed record SignalMemoryDescriptor(SignalNodeId Id) : SignalNodeDescriptor(Id);

/// <summary>The terminal contract of the existing signal operations, independent of public port roles.</summary>
public static class SignalTerminals
{
    public static PortSignalKind? Kind(SignalNodeDescriptor node, SignalTerminal terminal) => terminal switch
    {
        SignalTerminal.Set or SignalTerminal.Reset when node is SignalMemoryDescriptor => PortSignalKind.Pulse,
        SignalTerminal.Input when node is SignalTimerDescriptor or SignalPulseCountDescriptor => PortSignalKind.Pulse,
        SignalTerminal.Input when node is SignalIndicatorDescriptor => PortSignalKind.Level,
        SignalTerminal.Output when node is SignalMemoryDescriptor or SignalContactLoadDescriptor => PortSignalKind.Level,
        SignalTerminal.Output when node is SignalContactDescriptor or SignalOrientationDescriptor or
            SignalPassageDescriptor or SignalOscillatorDescriptor or SignalPulseCountDescriptor => PortSignalKind.Pulse,
        SignalTerminal.Output when node is SignalTimerDescriptor { ElapsedSignal: TimerElapsedSignal.Activation } => PortSignalKind.Pulse,
        _ => null
    };

    public static bool CanConnect(SignalNodeDescriptor source, SignalEndpoint output,
        SignalNodeDescriptor target, SignalEndpoint input)
    {
        var sourceKind = Kind(source, output.Terminal);
        var targetKind = Kind(target, input.Terminal);
        return output.Terminal == SignalTerminal.Output && input.Terminal != SignalTerminal.Output &&
            sourceKind is not null && targetKind is not null &&
            (sourceKind == targetKind || sourceKind == PortSignalKind.Pulse && target is SignalIndicatorDescriptor);
    }
}
