using Godot;
using System;

namespace CuriousContraptions;

public readonly record struct SphereImpactResult(Vector3 FirstVelocity,Vector3 SecondVelocity,float Impulse);

/// <summary>Shared frictionless two-body normal impulse for discrete and time-of-impact contacts.
/// Normal points from the second body toward the first; separating contact does no work.</summary>
public static class SphereImpact
{
    public static SphereImpactResult Resolve(Vector3 firstVelocity,float firstMass,Vector3 secondVelocity,
        float secondMass,Vector3 normal,float restitution)
    {
        if(!firstVelocity.IsFinite()||!secondVelocity.IsFinite())throw new ArgumentException("Impact velocities must be finite.");
        if(!float.IsFinite(firstMass)||firstMass<=0)throw new ArgumentOutOfRangeException(nameof(firstMass));
        if(!float.IsFinite(secondMass)||secondMass<=0)throw new ArgumentOutOfRangeException(nameof(secondMass));
        if(!normal.IsFinite()||Mathf.Abs(normal.LengthSquared()-1)>.001f)throw new ArgumentException("Impact normal must be unit length.",nameof(normal));
        if(!float.IsFinite(restitution)||restitution<0||restitution>1)throw new ArgumentOutOfRangeException(nameof(restitution));
        var approach=(firstVelocity-secondVelocity).Dot(normal);
        if(approach>=0)return new(firstVelocity,secondVelocity,0);
        var impulse=-(1+restitution)*approach/(1/firstMass+1/secondMass);
        return new(firstVelocity+normal*impulse/firstMass,secondVelocity-normal*impulse/secondMass,impulse);
    }
}
