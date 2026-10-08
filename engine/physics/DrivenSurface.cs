using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

/// <summary>A planar material surface driven by the relative coordinate of a
/// finite-mass hinge. Geometry belongs to the carrier; material motion draws
/// energy from the shaft and applies the opposite transmission torque.</summary>
public sealed class DrivenSurface
{
    public PhysicsFrameJoint Drive { get; }
    public PhysicsBody Carrier=>Drive.B;
    public PhysicsBody Shaft=>Drive.A;
    public CollisionVector LocalNormal { get; }
    public CollisionVector LocalDirection { get; }
    public double TravelPerRadian { get; }
    public CollisionVector Direction=>Carrier.Pose.Rotation.Apply(LocalDirection);
    public CollisionVector Axis=>Drive.FrameB.Orientation.Apply(new(0,0,1));
    public double Speed=>TravelPerRadian*CollisionVector.Dot(Axis,Shaft.AngularVelocity-Carrier.AngularVelocity);

    public DrivenSurface(PhysicsFrameJoint drive,CollisionVector localNormal,CollisionVector localDirection,double travelPerRadian)
    {
        ArgumentNullException.ThrowIfNull(drive);
        if(drive.Kind!=FrameJointKind.Hinge||drive.A.MotionType!=PhysicsMotionType.Dynamic||
            !localNormal.IsFinite||!localDirection.IsFinite||
            Math.Abs(localNormal.LengthSquared-1)>1e-10||Math.Abs(localDirection.LengthSquared-1)>1e-10||
            Math.Abs(CollisionVector.Dot(localNormal,localDirection))>1e-10||
            !double.IsFinite(travelPerRadian)||travelPerRadian==0)
            throw new ArgumentException("Driven material requires a finite-mass hinge shaft, orthogonal unit face axes and finite nonzero ratio.");
        Drive=drive; LocalNormal=localNormal; LocalDirection=localDirection; TravelPerRadian=travelPerRadian;
    }
    public bool Matches(PhysicsBody a,PhysicsBody b,CollisionVector normal)
    {
        var outward=Carrier==a?-normal:Carrier==b?normal:default;
        return CollisionVector.Dot(Carrier.Pose.Rotation.Apply(LocalNormal),outward)>=1-1e-8;
    }
    public DrivenSurface Rebind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> states)=>
        new((PhysicsFrameJoint)Drive.Rebind(states),LocalNormal,LocalDirection,TravelPerRadian);
}

public readonly record struct PhysicsSurfaceContact(PhysicsJointId Drive,PhysicsBodyId Receiver,
    CollisionVector Point,CollisionVector Direction,double SurfaceSpeed);
