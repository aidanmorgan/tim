namespace CuriousContraptions.Abstractions;

public enum SignalContactState : byte { Latched, TimerCounting }
public readonly record struct SignalContactControl(SignalNodeId Node, SignalContactState State);
