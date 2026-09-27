using System;

namespace CuriousContraptions;

public enum SpringLatchState { Latched, Releasing, Spent }
public enum SpringWindStatus { Wound, Stationary, Reverse, Full, TorqueLimited, WorkLimited, Blocked, Unlatched, PrecisionLimited }
public enum SpringTriggerResult { Released, Empty, AlreadyReleased }
public enum SpringLatchResult { Latched, AlreadyLatched, StillCompressed }
public readonly record struct SpringWinding(SpringWindStatus Status, double CompressionTravel, double ShaftTravel, double Work);
public readonly record struct SpringExtension(double Travel, double Work);

/// <summary>
/// Ideal ratcheted screw winds a linear spring. This is an energy/kinematic primitive,
/// not a collision solver or a mechanical-network power allocator. The caller supplies
/// real shaft travel, available torque and swept clearance. Released work must be
/// accounted into plunger/load motion or dissipation; an activation contributes none.
/// </summary>
public sealed class LatchedSpringStore
{
    public double Stiffness { get; }
    public double MaximumCompression { get; }
    public double Lead { get; }
    public double Compression { get; private set; }
    public double Energy => Potential(Compression);
    public double Capacity => Potential(MaximumCompression);
    public double Force => Stiffness * Compression;
    public double RequiredTorque => Force * Lead;
    public double AcceptedWork { get; private set; }
    public double ReleasedWork { get; private set; }
    public SpringLatchState State { get; private set; } = SpringLatchState.Latched;

    /// <param name="lead">Compression per positive shaft radian, not per revolution.</param>
    public LatchedSpringStore(float stiffness, float maximumCompression, float lead)
    {
        Positive(stiffness, nameof(stiffness));
        Positive(maximumCompression, nameof(maximumCompression));
        Positive(lead, nameof(lead));
        Stiffness = stiffness;
        MaximumCompression = maximumCompression;
        Lead = lead;
    }

    /// <summary>
    /// Consume only accepted positive shaft travel. Opposite travel freewheels and
    /// cannot unwind the latch. Torque is an ideal quasistatic input bound; it is
    /// never inferred from nonzero shaft speed. Clearance limits actual compression.
    /// </summary>
    public SpringWinding Wind(float shaftTravel, float availableTorque, float clearance, double availableWork)
    {
        if (!float.IsFinite(shaftTravel)) throw new ArgumentOutOfRangeException(nameof(shaftTravel));
        Nonnegative(availableTorque, nameof(availableTorque));
        Nonnegative(clearance, nameof(clearance));
        if (!double.IsFinite(availableWork) || availableWork < 0) throw new ArgumentOutOfRangeException(nameof(availableWork));
        SpringWinding Stop(SpringWindStatus status) => new(status, 0, 0, 0);
        if (State != SpringLatchState.Latched) return Stop(SpringWindStatus.Unlatched);
        if (shaftTravel == 0) return Stop(SpringWindStatus.Stationary);
        if (shaftTravel < 0) return Stop(SpringWindStatus.Reverse);
        if (Compression == MaximumCompression) return Stop(SpringWindStatus.Full);
        if (clearance == 0) return Stop(SpringWindStatus.Blocked);

        var torqueLimit = Math.Min(MaximumCompression, availableTorque / (Stiffness * Lead));
        if (torqueLimit <= Compression) return Stop(SpringWindStatus.TorqueLimited);
        if (availableWork == 0) return Stop(SpringWindStatus.WorkLimited);
        var energyLimit = Math.Sqrt(Compression * Compression + 2 * (availableWork / Stiffness));
        var requested = Math.Min((double)shaftTravel * Lead, clearance);
        var next = Math.Min(energyLimit, Math.Min(torqueLimit, Compression + requested));
        var work = Potential(next) - Energy;
        if (work > availableWork)
        {
            next = Math.BitDecrement(next);
            work = Potential(next) - Energy;
        }
        if (work > availableWork) throw new InvalidOperationException("Spring rounding exceeded the work allowance.");
        if (next <= Compression || work <= 0) return Stop(SpringWindStatus.PrecisionLimited);
        var travel = next - Compression;
        Compression = next;
        AcceptedWork += work;
        return new(SpringWindStatus.Wound, travel, travel / Lead, work);
    }

    /// <summary>No deferred trigger: an empty release leaves the latch armed, without storing a command.</summary>
    public SpringTriggerResult Release()
    {
        if (State != SpringLatchState.Latched) return SpringTriggerResult.AlreadyReleased;
        if (Compression == 0) return SpringTriggerResult.Empty;
        State = SpringLatchState.Releasing;
        return SpringTriggerResult.Released;
    }

    /// <summary>
    /// Spend energy only for actual outward plunger travel after collision clearance
    /// and dynamics have been solved by the caller. A blocked stroke spends nothing.
    /// No rewinding or extra stroke is accepted during release.
    /// </summary>
    public SpringExtension Extend(float permittedTravel)
    {
        Nonnegative(permittedTravel, nameof(permittedTravel));
        if (State != SpringLatchState.Releasing || permittedTravel == 0) return new(0, 0);
        var next = Math.Max(0, Compression - permittedTravel);
        var work = Energy - Potential(next);
        if (next >= Compression || work <= 0) return new(0, 0);
        var travel = Compression - next;
        Compression = next;
        ReleasedWork += work;
        if (Compression == 0) State = SpringLatchState.Spent;
        return new(travel, work);
    }

    /// <summary>The caller rearms only at the physical rest stop. Relatching cannot recharge.</summary>
    public SpringLatchResult Latch()
    {
        if (State == SpringLatchState.Latched) return SpringLatchResult.AlreadyLatched;
        if (Compression != 0) return SpringLatchResult.StillCompressed;
        State = SpringLatchState.Latched;
        return SpringLatchResult.Latched;
    }

    public void Reset()
    {
        Compression = 0;
        AcceptedWork = 0;
        ReleasedWork = 0;
        State = SpringLatchState.Latched;
    }

    private double Potential(double compression) => .5 * Stiffness * compression * compression;
    private static void Positive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(name);
    }
    private static void Nonnegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name);
    }
}
