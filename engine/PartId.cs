using System;

namespace CuriousContraptions;

/// <summary>An element's identity: the bundle's string element id, which saves and messages carry as it is (Story 9A.15). There is
/// no numeric form.</summary>
public readonly record struct PartId : IComparable<PartId>
{
    public string Value { get; }

    public PartId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PartId cannot be null or whitespace.", nameof(value));
        Value = value;
    }

    public int CompareTo(PartId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
    public static bool operator <(PartId left, PartId right) => left.CompareTo(right) < 0;
    public static bool operator <=(PartId left, PartId right) => left.CompareTo(right) <= 0;
    public static bool operator >(PartId left, PartId right) => left.CompareTo(right) > 0;
    public static bool operator >=(PartId left, PartId right) => left.CompareTo(right) >= 0;

    public override string ToString() => Value ?? string.Empty;

    public static readonly PartId Unsupported = new("unsupported");
    public static readonly PartId Basketball = new("ball");
    public static readonly PartId BowlingBall = new("bowling");
    public static readonly PartId Receiver = new("basket");
    public static readonly PartId Ramp = new("ramp");
    public static readonly PartId ImpactSwitch = new("switch");
    public static readonly PartId SignalLamp = new("lamp");
    public static readonly PartId Wall = new("wall");
    public static readonly PartId Delay = new("delay");
    public static readonly PartId PinballBumper = new("bumper");
    public static readonly PartId Domino = new("domino");
    public static readonly PartId Battery = new("battery");
    public static readonly PartId Springboard = new("spring");
    public static readonly PartId Pipe = new("pipe");
    public static readonly PartId BallDetector = new("ball_detector");
    public static readonly PartId PipeBend45 = new("pipe_bend_45");
    public static readonly PartId PipeBend90 = new("pipe_bend_90");
    public static readonly PartId Funnel = new("funnel");
    public static readonly PartId PoweredGate = new("powered_gate");
    public static readonly PartId PressurePlate = new("pressure_plate");
    public static readonly PartId Counter = new("counter");
    public static readonly PartId Clock = new("clock");
    public static readonly PartId Latch = new("latch");
}
