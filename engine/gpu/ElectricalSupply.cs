using System;
using System.Runtime.Intrinsics;

namespace CuriousContraptions.Gpu;

public enum ElectricalEnable : byte { Disabled = 0, Enabled = 1 }
public readonly record struct ElectricalTransfer(float Remaining, float Debit);

/// <summary>One finite source feeding distinct declared storage loads at 120 Hz.</summary>
public static class ElectricalSupply
{
    public const float PhaseSeconds = 1f / 120f;
    public const float MaximumCapacity = 14400f;
    public const int LoadCapacity = WorkshopConnections.Capacity;

    /// <summary>
    /// Admission validates every input before writing output. Runtime arithmetic clamps ordinary
    /// float residuals; unavailable or unrepresentable work never creates a recipient credit.
    /// </summary>
    public static ElectricalTransfer Allocate(ElectricalEnable enabled, float sourceBalance, float maximumPower,
        ReadOnlySpan<float> balances, ReadOnlySpan<float> capacities, Span<float> nextBalances)
    {
        if (!Enum.IsDefined(enabled) || !float.IsFinite(sourceBalance) || sourceBalance < 0 ||
            sourceBalance > MaximumCapacity || !float.IsFinite(maximumPower) || maximumPower is < 10f or > 480f ||
            balances.Length != capacities.Length || balances.Length > LoadCapacity || nextBalances.Length != balances.Length ||
            balances.Overlaps(nextBalances) || capacities.Overlaps(nextBalances))
            throw new ArgumentException("Invalid finite electrical allocation.");
        for (var i = 0; i < balances.Length; i++)
            if (!float.IsFinite(capacities[i]) || capacities[i] is < 0f or > MaximumCapacity ||
                !float.IsFinite(balances[i]) || balances[i] < 0f || balances[i] > capacities[i])
                throw new ArgumentException("Invalid electrical storage balance.");

        // All inputs have been admitted; demand and grants share one finite reservation.
        Span<float> demand = stackalloc float[LoadCapacity];
        var totalDemand = 0f;
        for (var i = 0; i < balances.Length; i++)
        {
            demand[i] = capacities[i] - balances[i];
            totalDemand += demand[i];
        }
        balances.CopyTo(nextBalances);
        if (enabled == ElectricalEnable.Disabled || sourceBalance == 0 || totalDemand <= 0)
            return new(sourceBalance, 0f);

        var requested = MathF.Min(sourceBalance, MathF.Min(maximumPower * PhaseSeconds, totalDemand));
        var reservedRemaining = sourceBalance - requested;
        var reserved = sourceBalance - reservedRemaining;
        if (reserved > requested)
        {
            reservedRemaining = MathF.Min(sourceBalance, MathF.BitIncrement(reservedRemaining));
            reserved = sourceBalance - reservedRemaining;
        }
        if (!(reserved > 0f)) return new(sourceBalance, 0f);

        var scale = MathF.Min(1f, reserved / totalDemand);
        Span<float> grants = stackalloc float[LoadCapacity];
        var slot = 0;
        for (; slot + 4 <= balances.Length; slot += 4)
        {
            var vector = Vector128.Create(demand[slot], demand[slot + 1], demand[slot + 2], demand[slot + 3]) *
                Vector128.Create(scale);
            for (var lane = 0; lane < 4; lane++) grants[slot + lane] = vector.GetElement(lane);
        }
        for (; slot < balances.Length; slot++) grants[slot] = demand[slot] * scale;

        var spent = 0f;
        for (var i = 0; i < balances.Length; i++)
        {
            var before = nextBalances[i];
            var grant = MathF.Min(grants[i], MathF.Max(0f, reserved - spent));
            if (!(grant > 0f)) continue;
            var after = MathF.Min(capacities[i], before + grant);
            if (after - before > grant)
                after = MathF.Max(before, MathF.BitDecrement(after));
            var accepted = MathF.Max(0f, after - before);
            nextBalances[i] = after;
            spent += accepted;
        }
        // Refund unused representable work. Keep the debit at least the realized float credits;
        // a refund that rounds too high retreats one float, never beyond the original reservation.
        var remaining = MathF.Min(sourceBalance, reservedRemaining + MathF.Max(0f, reserved - spent));
        if (sourceBalance - remaining < spent)
            remaining = MathF.Max(reservedRemaining, MathF.BitDecrement(remaining));
        return new(remaining, sourceBalance - remaining);
    }
}
