using System;

namespace CuriousContraptions;

public enum LogicGateKind { And, Or, Xor, Nor, Nand }

/// <summary>Two conditions only. Truth never implies a source of power or light.</summary>
public static class LogicGate
{
    public static bool Evaluate(LogicGateKind kind, bool first, bool second) => kind switch
    {
        LogicGateKind.And => first && second,
        LogicGateKind.Or => first || second,
        LogicGateKind.Xor => first != second,
        LogicGateKind.Nor => !first && !second,
        LogicGateKind.Nand => !(first && second),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported logic operation.")
    };
}

/// <summary>
/// Control snapshot for a carrier gate. Sample after tracing; advance once before the
/// next optical solve. Carrier energy is routed separately, never generated here.
/// </summary>
public sealed class OpticalLogicControl
{
    public LogicGateKind Kind { get; }
    public float OnThreshold { get; }
    public float OffThreshold { get; }
    public bool First { get; private set; }
    public bool Second { get; private set; }
    public bool IsOpen { get; private set; }

    public OpticalLogicControl(LogicGateKind kind, float onThreshold = .25f, float offThreshold = .225f)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (!float.IsFinite(onThreshold) || onThreshold <= 0 ||
            !float.IsFinite(offThreshold) || offThreshold < 0 || offThreshold >= onThreshold)
            throw new ArgumentException("Optical thresholds require finite 0 <= off < on.");
        Kind = kind;
        OnThreshold = onThreshold;
        OffThreshold = offThreshold;
        Reset();
    }

    public void Sample(float first, float second)
    {
        // Validate both before changing either: rejected input must not partially commit.
        if (!float.IsFinite(first) || first < 0 || !float.IsFinite(second) || second < 0)
            throw new ArgumentException("Optical control power must be finite and nonnegative.");
        First = Detect(First, first);
        Second = Detect(Second, second);
    }

    public void Advance() => IsOpen = LogicGate.Evaluate(Kind, First, Second);

    public void Reset()
    {
        First = Second = false;
        IsOpen = LogicGate.Evaluate(Kind, false, false);
    }

    private bool Detect(bool previous, float power) =>
        previous ? power > OffThreshold : power >= OnThreshold;
}
