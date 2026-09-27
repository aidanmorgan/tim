using System;

namespace CuriousContraptions;

public enum LaunchReleaseStatus { Released, Empty, SpeedLimited, InsufficientPrecision }
public readonly record struct LaunchRelease(LaunchReleaseStatus Status, float ForwardSpeed, double EnergyUsed);

/// <summary>Finite ideal launch-energy store. Charging consumes supplied work; a trigger adds none.
/// The caller must preserve transverse velocity when applying a scalar release. An anchored launcher absorbs momentum.
/// Chamber occupancy, muzzle clearance and physical flight are separate mandatory interlocks.</summary>
public sealed class LaunchEnergyStore
{
    public double Capacity { get; }
    public double Energy { get; private set; }
    public double AcceptedEnergy { get; private set; }
    public double ReleasedEnergy { get; private set; }

    public LaunchEnergyStore(float capacity)
    {
        if (!float.IsFinite(capacity) || capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity=capacity;
    }

    /// <returns>Work accepted, excluding excess offered after reaching capacity.</returns>
    public double Charge(float suppliedPower,float elapsedSeconds)
    {
        if (!float.IsFinite(suppliedPower) || suppliedPower < 0) throw new ArgumentOutOfRangeException(nameof(suppliedPower));
        if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        // Inputs are floats; double multiplication cannot overflow for two finite float operands.
        var accepted=Math.Min(Capacity-Energy,(double)suppliedPower*elapsedSeconds);
        Energy+=accepted;AcceptedEnergy+=accepted;
        return accepted;
    }

    public LaunchRelease Release(float mass,float incomingForwardSpeed,float maximumSpeed)
    {
        if (!float.IsFinite(mass) || mass <= 0) throw new ArgumentOutOfRangeException(nameof(mass));
        if (!float.IsFinite(incomingForwardSpeed)) throw new ArgumentOutOfRangeException(nameof(incomingForwardSpeed));
        if (!float.IsFinite(maximumSpeed) || maximumSpeed <= 0) throw new ArgumentOutOfRangeException(nameof(maximumSpeed));
        if (Energy==0) return new(LaunchReleaseStatus.Empty,incomingForwardSpeed,0);
        var incoming=Math.Abs((double)incomingForwardSpeed);
        if (incoming>=maximumSpeed) return new(LaunchReleaseStatus.SpeedLimited,incomingForwardSpeed,0);
        var speedLimitEnergy=.5*mass*(maximumSpeed-incoming)*(maximumSpeed+incoming);
        var available=Math.Min(Energy,speedLimitEnergy);
        var speed=Math.Min(maximumSpeed,(float)Math.Sqrt(incoming*incoming+2*available/mass));
        double Cost(float outgoing)=>.5*mass*(outgoing-incoming)*(outgoing+incoming);
        // Float velocity rounding must never create more kinetic energy than was stored.
        if (Cost(speed)>available) speed=MathF.BitDecrement(speed);
        var cost=Cost(speed);
        if (cost<=0) return new(LaunchReleaseStatus.InsufficientPrecision,incomingForwardSpeed,0);
        Energy-=cost;ReleasedEnergy+=cost;
        return new(LaunchReleaseStatus.Released,speed,cost);
    }

    public void Reset()
    {
        Energy=0;AcceptedEnergy=0;ReleasedEnergy=0;
    }
}
