using Godot;
using System;

namespace CuriousContraptions;

public readonly record struct SurfaceFrictionResult(Vector3 Velocity, float SpeedLoss);

/// <summary>Passive tangential response bounded by both Coulomb normal impulse
/// per unit mass and the caller's remaining substep damping budget.</summary>
public static class SurfaceFriction
{
    public static SurfaceFrictionResult Apply(Vector3 velocity, Vector3 normal,
        float normalVelocityChange, float budget, float coefficient)
    {
        if (!velocity.IsFinite()) throw new ArgumentOutOfRangeException(nameof(velocity));
        if (!normal.IsFinite() || Mathf.Abs(normal.LengthSquared()-1)>.001f)
            throw new ArgumentException("Friction normal must be unit length.",nameof(normal));
        if (!float.IsFinite(normalVelocityChange) || normalVelocityChange<0)
            throw new ArgumentOutOfRangeException(nameof(normalVelocityChange));
        if (!float.IsFinite(budget) || budget<0) throw new ArgumentOutOfRangeException(nameof(budget));
        if (!float.IsFinite(coefficient) || coefficient<0) throw new ArgumentOutOfRangeException(nameof(coefficient));
        var tangent=velocity-normal*velocity.Dot(normal);
        var speed=tangent.Length();
        var loss=(float)Math.Min(speed,Math.Min(budget,(double)coefficient*normalVelocityChange));
        return new(loss>0?velocity-tangent*(loss/speed):velocity,loss);
    }
}
