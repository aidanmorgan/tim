using System;

namespace CuriousContraptions.Physics;

public enum PhysicsMotionType { Static, Kinematic, Dynamic }
public readonly record struct PhysicsBodyId
{
    public int Index { get; }
    public PhysicsBodyId(int index)
    {
        if(index<0) throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}

/// <summary>Symmetric positive-definite tensor in world axes. Construction checks
/// Sylvester's criterion; no singular/negative dynamic inertia is accepted.</summary>
public readonly struct InertiaTensor
{
    public double XX { get; }
    public double YY { get; }
    public double ZZ { get; }
    public double XY { get; }
    public double XZ { get; }
    public double YZ { get; }
    public InertiaTensor(double xx,double yy,double zz,double xy=0,double xz=0,double yz=0)
    {
        XX=xx; YY=yy; ZZ=zz; XY=xy; XZ=xz; YZ=yz;
        if(!IsPositiveDefinite) throw new ArgumentException("Dynamic inertia must be finite and positive definite.");
    }
    public bool IsPositiveDefinite=>double.IsFinite(XX)&&double.IsFinite(YY)&&double.IsFinite(ZZ)&&
        double.IsFinite(XY)&&double.IsFinite(XZ)&&double.IsFinite(YZ)&&XX>0&&
        double.IsFinite(XX*YY-XY*XY)&&XX*YY-XY*XY>0&&double.IsFinite(Determinant)&&Determinant>0;
    private double Determinant=>XX*(YY*ZZ-YZ*YZ)-XY*(XY*ZZ-XZ*YZ)+XZ*(XY*YZ-XZ*YY);
    public CollisionVector Apply(CollisionVector v)=>new(XX*v.X+XY*v.Y+XZ*v.Z,
        XY*v.X+YY*v.Y+YZ*v.Z,XZ*v.X+YZ*v.Y+ZZ*v.Z);
    public InertiaTensor Inverse()
    {
        if(!IsPositiveDefinite) throw new InvalidOperationException("Uninitialised inertia tensor.");
        var d=Determinant;
        return new((YY*ZZ-YZ*YZ)/d,(XX*ZZ-XZ*XZ)/d,(XX*YY-XY*XY)/d,
            (XZ*YZ-XY*ZZ)/d,(XY*YZ-XZ*YY)/d,(XY*XZ-XX*YZ)/d);
    }
}

/// <summary>A body's velocity state during one constraint solve. All constraints
/// use the same unconstrained mass/inertia; joints are rows, not altered masses.</summary>
public sealed class ImpulseBody
{
    public PhysicsBodyId Id { get; }
    public PhysicsMotionType MotionType { get; }
    public CollisionVector Center { get; }
    public CollisionVector LinearVelocity { get; private set; }
    public CollisionVector AngularVelocity { get; private set; }
    public double InverseMass { get; }
    private readonly InertiaTensor _inverseInertia;

    public ImpulseBody(PhysicsBodyId id,PhysicsMotionType motionType,CollisionVector center,
        CollisionVector linearVelocity,CollisionVector angularVelocity,double mass=0,InertiaTensor inertia=default)
    {
        if(!Enum.IsDefined(motionType)) throw new ArgumentOutOfRangeException(nameof(motionType));
        if(!center.IsFinite||!linearVelocity.IsFinite||!angularVelocity.IsFinite)
            throw new ArgumentException("Body state must be finite.");
        if(motionType==PhysicsMotionType.Dynamic)
        {
            if(!double.IsFinite(mass)||mass<=0||!double.IsFinite(1/mass)||!inertia.IsPositiveDefinite)
                throw new ArgumentException("Dynamic body requires finite positive mass and inertia.");
            InverseMass=1/mass;
            _inverseInertia=inertia.Inverse();
        }
        else
        {
            if(mass!=0||!inertia.Equals(default(InertiaTensor)))
                throw new ArgumentException("Static and kinematic bodies do not declare dynamic mass.");
            if(motionType==PhysicsMotionType.Static&&(linearVelocity!=default||angularVelocity!=default))
                throw new ArgumentException("Static bodies cannot have velocity.");
        }
        Id=id; MotionType=motionType; Center=center;
        LinearVelocity=linearVelocity; AngularVelocity=angularVelocity;
    }
    public CollisionVector InverseInertia(CollisionVector angularImpulse)
    {
        if(!angularImpulse.IsFinite) throw new ArgumentOutOfRangeException(nameof(angularImpulse));
        return MotionType==PhysicsMotionType.Dynamic?_inverseInertia.Apply(angularImpulse):default;
    }
    public CollisionVector PointVelocity(CollisionVector point)=>
        LinearVelocity+CollisionVector.Cross(AngularVelocity,point-Center);
    internal (CollisionVector Linear,CollisionVector Angular) AfterImpulse(CollisionVector linear,CollisionVector angular)
    {
        if(!linear.IsFinite||!angular.IsFinite) throw new ArgumentException("Impulse must be finite.");
        var v=LinearVelocity+linear*InverseMass;
        var w=AngularVelocity+InverseInertia(angular);
        if(!v.IsFinite||!w.IsFinite) throw new InvalidOperationException("Impulse exceeds representable velocity.");
        return (v,w);
    }
    internal void CommitVelocity((CollisionVector Linear,CollisionVector Angular) velocity)
    {
        LinearVelocity=velocity.Linear; AngularVelocity=velocity.Angular;
    }
    public void ApplyImpulse(CollisionVector impulse,CollisionVector point)
    {
        if(!point.IsFinite) throw new ArgumentOutOfRangeException(nameof(point));
        CommitVelocity(AfterImpulse(impulse,CollisionVector.Cross(point-Center,impulse)));
    }
}
