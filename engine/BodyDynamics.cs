using System;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Immutable component-declared initial state and mass properties.
/// A latch or guide is a constraint, not a change of the body's mass/motion type.
/// This value does not track or overwrite an already running physics body.</summary>
public sealed record BodyDynamics
{
    public PhysicsMotionType Motion { get; }
    public double Mass { get; }
    public InertiaTensor Inertia { get; }
    public CollisionVector LinearVelocity { get; }
    public CollisionVector AngularVelocity { get; }

    public BodyDynamics(PhysicsMotionType motion,double mass,InertiaTensor inertia,
        CollisionVector linearVelocity,CollisionVector angularVelocity)
    {
        if(!Enum.IsDefined(motion)) throw new ArgumentOutOfRangeException(nameof(motion));
        if(!linearVelocity.IsFinite||!angularVelocity.IsFinite)
            throw new ArgumentException("Initial velocities must be finite.");
        if(motion==PhysicsMotionType.Dynamic)
        {
            if(!double.IsFinite(mass)||mass<=0||!double.IsFinite(1/mass)||!inertia.IsPositiveDefinite)
                throw new ArgumentException("Dynamic bodies require finite positive mass and inertia.");
        }
        else
        {
            if(mass!=0||!inertia.Equals(default(InertiaTensor)))
                throw new ArgumentException("Prescribed bodies cannot declare dynamic mass.");
            if(motion==PhysicsMotionType.Static&&(linearVelocity!=default||angularVelocity!=default))
                throw new ArgumentException("Static bodies cannot have initial velocity.");
        }
        Motion=motion; Mass=mass; Inertia=inertia;
        LinearVelocity=linearVelocity; AngularVelocity=angularVelocity;
    }

    public static BodyDynamics SolidSphere(double mass,double radius,CollisionVector linearVelocity,CollisionVector angularVelocity)
    {
        if(!double.IsFinite(radius)||radius<=0) throw new ArgumentOutOfRangeException(nameof(radius));
        var inertia=.4*mass*radius*radius;
        return new(PhysicsMotionType.Dynamic,mass,new(inertia,inertia,inertia),linearVelocity,angularVelocity);
    }

    public static BodyDynamics SolidBox(double mass,CollisionVector half,
        CollisionVector linearVelocity,CollisionVector angularVelocity)
    {
        if(!half.IsFinite||half.X<=0||half.Y<=0||half.Z<=0)
            throw new ArgumentOutOfRangeException(nameof(half));
        return new(PhysicsMotionType.Dynamic,mass,
            new(mass*(half.Y*half.Y+half.Z*half.Z)/3,
                mass*(half.X*half.X+half.Z*half.Z)/3,
                mass*(half.X*half.X+half.Y*half.Y)/3),linearVelocity,angularVelocity);
    }

    public PhysicsBody CreateBody(PhysicsBodyId id,RigidPose pose,PrescribedBodyMotion? prescribedMotion)=>
        new(id,Motion,pose,LinearVelocity,AngularVelocity,Mass,Inertia,prescribedMotion);
}
