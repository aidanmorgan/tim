using System;

namespace CuriousContraptions;

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
    public static readonly PartId Tennis = new("tennis");
    public static readonly PartId Balloon = new("balloon");
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

    public uint ToNumericId() => Value switch
    {
        "ball" => 1,
        "basket" => 2,
        "ramp" => 3,
        "switch" => 4,
        "lamp" => 5,
        "wall" => 6,
        "delay" => 7,
        "bumper" => 8,
        "domino" => 9,
        "bowling" => 10,
        "battery" => 11,
        "spring" => 12,
        "tennis" => 13,
        "balloon" => 14,
        "pipe" => 15,
        "ball_detector" => 16,
        "pipe_bend_45" => 17,
        "pipe_bend_90" => 18,
        "funnel" => 19,
        "powered_gate" => 20,
        "pressure_plate" => 21,
        "counter" => 22,
        "clock" => 23,
        "latch" => 24,
        _ => 0
    };

    public static PartId FromNumericId(uint id) => id switch
    {
        0 => default,
        1 => Basketball,
        2 => Receiver,
        3 => Ramp,
        4 => ImpactSwitch,
        5 => SignalLamp,
        6 => Wall,
        7 => Delay,
        8 => PinballBumper,
        9 => Domino,
        10 => BowlingBall,
        11 => Battery,
        12 => Springboard,
        13 => Tennis,
        14 => Balloon,
        15 => Pipe,
        16 => BallDetector,
        17 => PipeBend45,
        18 => PipeBend90,
        19 => Funnel,
        20 => PoweredGate,
        21 => PressurePlate,
        22 => Counter,
        23 => Clock,
        24 => Latch,
        _ => throw new ArgumentException($"Unsupported numeric part id '{id}'.")
    };
}
