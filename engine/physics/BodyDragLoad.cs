using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

/// <summary>Linear and angular momentum decay in still ambient fluid. Rates
/// are inverse seconds; shared prediction evaluates the current sampled state.</summary>
public sealed record BodyDragLoad
{
    public PhysicsBodyId Body { get; }
    public double LinearRate { get; }
    public double AngularRate { get; }
    public BodyDragLoad(PhysicsBodyId body,double linearRate,double angularRate)
    {
        if(!double.IsFinite(linearRate)||linearRate<0)throw new ArgumentOutOfRangeException(nameof(linearRate));
        if(!double.IsFinite(angularRate)||angularRate<0)throw new ArgumentOutOfRangeException(nameof(angularRate));
        Body=body;LinearRate=linearRate;AngularRate=angularRate;
    }
    public PhysicsBody Resolve(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        if(!bodies.TryGetValue(Body,out var body)||body.MotionType!=PhysicsMotionType.Dynamic)
            throw new ArgumentException("Drag requires its owned dynamic body.");
        return body;
    }
    public BodyWrench Wrench(PhysicsBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if(body.Id!=Body||body.MotionType!=PhysicsMotionType.Dynamic)
            throw new ArgumentException("Drag sample must match its declared dynamic body.");
        return new(-body.LinearVelocity*(LinearRate/body.InverseMass),-body.AngularMomentum*AngularRate);
    }
}
