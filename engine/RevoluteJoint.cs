using Godot;
using System;

namespace CuriousContraptions;

public enum HingeLimit { None, Lower, Upper }
public readonly record struct HingeAdvance(HingeLimit Reached, double DissipatedEnergy);

/// <summary>
/// Finite-inertia, fixed-axis rotation for the impact lever. This primitive does
/// not detect collisions or attach cargo. A world solver must split advancement
/// at the earliest swept contact or TimeToLimit and solve simultaneous constraints.
/// Angles/velocities are radians; offsets, impulses and Axis share world space.
/// </summary>
public sealed class RevoluteJoint
{
    public Vector3 Axis { get; }
    public double Inertia { get; }
    public double LowerAngle { get; }
    public double UpperAngle { get; }
    public double InitialAngle { get; }
    public double Angle { get; private set; }
    public double AngularVelocity { get; private set; }
    public double Energy => .5 * Inertia * AngularVelocity * AngularVelocity;
    public HingeLimit Limit => Angle == LowerAngle ? HingeLimit.Lower :
        Angle == UpperAngle ? HingeLimit.Upper : HingeLimit.None;
    public double TimeToLimit => AngularVelocity > 0 ? (UpperAngle - Angle) / AngularVelocity :
        AngularVelocity < 0 ? (LowerAngle - Angle) / AngularVelocity : double.PositiveInfinity;

    public RevoluteJoint(Vector3 axis, double inertia, double lowerAngle, double upperAngle, double initialAngle = 0)
    {
        Unit(axis, nameof(axis));
        if (!double.IsFinite(inertia) || inertia <= 0) throw new ArgumentOutOfRangeException(nameof(inertia));
        if (!double.IsFinite(lowerAngle) || !double.IsFinite(upperAngle) ||
            lowerAngle >= upperAngle || !double.IsFinite(upperAngle - lowerAngle))
            throw new ArgumentException("Hinge limits must define a finite positive angular span.");
        if (!double.IsFinite(initialAngle) || initialAngle < lowerAngle || initialAngle > upperAngle)
            throw new ArgumentOutOfRangeException(nameof(initialAngle));
        Axis = axis; Inertia = inertia; LowerAngle = lowerAngle; UpperAngle = upperAngle;
        InitialAngle = initialAngle; Angle = initialAngle;
    }

    public Vector3 PointVelocity(Vector3 offset)
    {
        Finite(offset, nameof(offset));
        var tangent = Axis.Cross(offset);
        var result = new Vector3((float)(tangent.X * AngularVelocity),
            (float)(tangent.Y * AngularVelocity), (float)(tangent.Z * AngularVelocity));
        if (!result.IsFinite()) throw new InvalidOperationException("Hinge point velocity exceeds vector precision.");
        return result;
    }

    /// <summary>Axis dot (offset cross force/impulse), evaluated without float-product overflow.</summary>
    public double Moment(Vector3 offset, Vector3 force)
    {
        Finite(offset, nameof(offset)); Finite(force, nameof(force));
        return Axis.X * ((double)offset.Y * force.Z - (double)offset.Z * force.Y) +
            Axis.Y * ((double)offset.Z * force.X - (double)offset.X * force.Z) +
            Axis.Z * ((double)offset.X * force.Y - (double)offset.Y * force.X);
    }

    /// <summary>Unconstrained contact Jacobian; a contact solver must additionally solve active end stops.</summary>
    public double FreeInverseMassAlong(Vector3 offset, Vector3 direction)
    {
        Unit(direction, nameof(direction));
        var arm = Moment(offset, direction);
        var response = arm * arm / Inertia;
        if (!double.IsFinite(response)) throw new InvalidOperationException("Hinge response exceeds scalar precision.");
        return response;
    }

    public void ApplyImpulse(Vector3 offset, Vector3 impulse) => ApplyAngularImpulse(Moment(offset, impulse));

    /// <summary>Apply real angular momentum. A hard stop absorbs outward momentum, never reflects it.</summary>
    public void ApplyAngularImpulse(double impulse)
    {
        if (!double.IsFinite(impulse)) throw new ArgumentOutOfRangeException(nameof(impulse));
        var next = AngularVelocity + impulse / Inertia;
        if (!double.IsFinite(next) || !double.IsFinite(.5 * Inertia * next * next))
            throw new ArgumentOutOfRangeException(nameof(impulse), "Hinge impulse exceeds finite energy range.");
        if ((Limit == HingeLimit.Lower && next < 0) || (Limit == HingeLimit.Upper && next > 0)) next = 0;
        AngularVelocity = next;
    }

    /// <summary>
    /// Force-free angular flight. Stop impact is perfectly inelastic. Any remainder
    /// after the stop is stationary; forces must not be silently reapplied here.
    /// Callers needing intermediate collision events must split this interval.
    /// </summary>
    public HingeAdvance Advance(double duration)
    {
        if (!double.IsFinite(duration) || duration < 0) throw new ArgumentOutOfRangeException(nameof(duration));
        if (duration == 0 || AngularVelocity == 0) return new(HingeLimit.None, 0);
        if (duration >= TimeToLimit)
        {
            var reached = AngularVelocity > 0 ? HingeLimit.Upper : HingeLimit.Lower;
            var lost = Energy;
            Angle = reached == HingeLimit.Upper ? UpperAngle : LowerAngle;
            AngularVelocity = 0;
            return new(reached, lost);
        }
        Angle += AngularVelocity * duration;
        return new(HingeLimit.None, 0);
    }

    public void Reset() { Angle = InitialAngle; AngularVelocity = 0; }

    private static void Finite(Vector3 value, string name)
    {
        if (!value.IsFinite()) throw new ArgumentOutOfRangeException(name);
    }
    private static void Unit(Vector3 value, string name)
    {
        Finite(value, name);
        if (Math.Abs(value.LengthSquared() - 1) > .00001f) throw new ArgumentException("Expected unit vector.", name);
    }
}
