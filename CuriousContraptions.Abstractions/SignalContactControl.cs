using System.Text.Json.Serialization;

namespace CuriousContraptions.Abstractions;

public enum SignalContactState : byte { Latched, TimerCounting }
public enum CoordinateEndpoint : byte { Lower, Upper }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Observation")]
[JsonDerivedType(typeof(SignalStateContactControl), "SignalState")]
[JsonDerivedType(typeof(PrismaticContactControl), "PrismaticEndpoint")]
public abstract record SupplyContactControl;
public sealed record SignalStateContactControl(SignalNodeId Node, SignalContactState State) : SupplyContactControl;
public sealed record PrismaticContactControl(CoordinateReference Coordinate, CoordinateEndpoint Endpoint) : SupplyContactControl;
