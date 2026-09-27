using System;

namespace CuriousContraptions;

public readonly record struct TractionResult(float Speed, double Work, double Impulse);

/// <summary>One-dimensional supplied friction drive. Braking dissipates energy;
/// crossing zero cannot recover that dissipated energy to fund reverse acceleration.</summary>
public static class MechanicalTraction
{
    public static TractionResult Apply(float incoming, float target, float mass, double impulseLimit, double workLimit)
    {
        if (!float.IsFinite(incoming)) throw new ArgumentOutOfRangeException(nameof(incoming));
        if (!float.IsFinite(target)) throw new ArgumentOutOfRangeException(nameof(target));
        if (!float.IsFinite(mass) || mass <= 0) throw new ArgumentOutOfRangeException(nameof(mass));
        if (!double.IsFinite(impulseLimit) || impulseLimit < 0) throw new ArgumentOutOfRangeException(nameof(impulseLimit));
        if (!double.IsFinite(workLimit) || workLimit < 0) throw new ArgumentOutOfRangeException(nameof(workLimit));
        if (incoming == target || impulseLimit == 0) return new(incoming, 0, 0);
        var sign = Math.Sign((double)target - incoming);
        var old = (double)incoming * sign;
        var desired = Math.Min((double)target * sign, old + impulseLimit / mass);
        var start = Math.Max(0, old);
        var energyBound = Math.Sqrt(start * start + 2 * (workLimit / mass));
        if (desired > 0) desired = Math.Min(desired, energyBound);
        var directed = (float)desired;
        if (directed > desired) directed = MathF.BitDecrement(directed);
        if (directed <= old) return new(incoming, 0, 0);
        double Cost(float speed) => .5 * mass * (Math.Max(0, (double)speed) - start) * (Math.Max(0, (double)speed) + start);
        var cost = Math.Max(0, Cost(directed));
        if (cost > workLimit)
        {
            directed = MathF.BitDecrement(directed);
            if (directed <= old) return new(incoming, 0, 0);
            cost = Math.Max(0, Cost(directed));
        }
        if (cost > workLimit) throw new InvalidOperationException("Traction rounding exceeded the work allowance.");
        return new(directed * sign, cost, (directed - old) * mass);
    }
}
