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
