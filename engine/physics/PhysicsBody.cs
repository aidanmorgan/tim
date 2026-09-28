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

/// <summary>Symmetric positive-definite tensor in a declared coordinate frame. Construction checks
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
    public InertiaTensor Rotated(RigidRotation rotation)
    {
        if(!IsPositiveDefinite||!rotation.IsValid) throw new ArgumentException("Rotation and inertia must be valid.");
        var inverse=rotation.Inverse();
        var cx=rotation.Apply(Apply(inverse.Apply(new(1,0,0))));
        var cy=rotation.Apply(Apply(inverse.Apply(new(0,1,0))));
        var cz=rotation.Apply(Apply(inverse.Apply(new(0,0,1))));
        return new(cx.X,cy.Y,cz.Z,(cx.Y+cy.X)*.5,(cx.Z+cz.X)*.5,(cy.Z+cz.Y)*.5);
    }
    public double FrobeniusNorm=>Math.Sqrt(XX*XX+YY*YY+ZZ*ZZ+2*(XY*XY+XZ*XZ+YZ*YZ));
    public InertiaTensor Inverse()
    {
        if(!IsPositiveDefinite) throw new InvalidOperationException("Uninitialised inertia tensor.");
        var d=Determinant;
        return new((YY*ZZ-YZ*YZ)/d,(XX*ZZ-XZ*XZ)/d,(XX*YY-XY*XY)/d,
            (XZ*YZ-XY*ZZ)/d,(XY*YZ-XZ*YY)/d,(XY*XZ-XX*YZ)/d);
    }
}

public readonly record struct PhysicsBodySnapshot(PhysicsBodyId Id,PhysicsMotionType MotionType,
    RigidPose Pose,CollisionVector LinearVelocity,CollisionVector AngularMomentum,CollisionVector KinematicAngularVelocity);
internal readonly record struct BodyVelocityUpdate(CollisionVector Linear,CollisionVector AngularMomentum);

/// <summary>Authoritative rigid pose, velocity and world angular momentum.
/// Constraints act directly on this state. Inertia is declared in body coordinates
/// and rotated with the pose, never replaced by a part-specific response.</summary>
public sealed class PhysicsBody
{
    public PhysicsBodyId Id { get; }
    public PhysicsMotionType MotionType { get; }
    public RigidPose Pose { get; private set; }
    public CollisionVector Center=>Pose.Center;
    public CollisionVector LinearVelocity { get; private set; }
    public CollisionVector AngularMomentum { get; private set; }
    public CollisionVector AngularVelocity=>MotionType==PhysicsMotionType.Dynamic?
        _inverseInertia.Apply(AngularMomentum):_kinematicAngularVelocity;
    public double InverseMass { get; }
    public InertiaTensor LocalInertia { get; }
    public ulong PoseRevision { get; private set; }
    private readonly InertiaTensor _localInverse;
    private InertiaTensor _inertia,_inverseInertia;
    private CollisionVector _kinematicAngularVelocity;

    public PhysicsBody(PhysicsBodyId id,PhysicsMotionType motionType,RigidPose pose,
        CollisionVector linearVelocity,CollisionVector angularVelocity,double mass=0,InertiaTensor inertia=default)
    {
        if(!Enum.IsDefined(motionType)) throw new ArgumentOutOfRangeException(nameof(motionType));
        if(!pose.Rotation.IsValid||!pose.Center.IsFinite||!linearVelocity.IsFinite||!angularVelocity.IsFinite)
            throw new ArgumentException("Body state must be finite and pose explicit.");
        if(motionType==PhysicsMotionType.Dynamic)
        {
            if(!double.IsFinite(mass)||mass<=0||!double.IsFinite(1/mass)||!inertia.IsPositiveDefinite)
                throw new ArgumentException("Dynamic body requires finite positive mass and inertia.");
            InverseMass=1/mass; LocalInertia=inertia; _localInverse=inertia.Inverse();
            _inertia=inertia.Rotated(pose.Rotation); _inverseInertia=_localInverse.Rotated(pose.Rotation);
            AngularMomentum=_inertia.Apply(angularVelocity);
            if(!AngularMomentum.IsFinite) throw new ArgumentException("Angular momentum exceeds numeric range.");
        }
        else
        {
            if(mass!=0||!inertia.Equals(default(InertiaTensor)))
                throw new ArgumentException("Static and kinematic bodies do not declare dynamic mass.");
            if(motionType==PhysicsMotionType.Static&&(linearVelocity!=default||angularVelocity!=default))
                throw new ArgumentException("Static bodies cannot have velocity.");
            _kinematicAngularVelocity=angularVelocity;
        }
        Id=id; MotionType=motionType; Pose=pose; LinearVelocity=linearVelocity;
    }
    public CollisionVector InverseInertia(CollisionVector angularImpulse)
    {
        if(!angularImpulse.IsFinite) throw new ArgumentOutOfRangeException(nameof(angularImpulse));
        return MotionType==PhysicsMotionType.Dynamic?_inverseInertia.Apply(angularImpulse):default;
    }
    public CollisionVector PointVelocity(CollisionVector point)
    {
        if(!point.IsFinite) throw new ArgumentOutOfRangeException(nameof(point));
        return LinearVelocity+CollisionVector.Cross(AngularVelocity,point-Center);
    }
    internal BodyVelocityUpdate AfterImpulse(CollisionVector linear,CollisionVector angular)
    {
        if(!linear.IsFinite||!angular.IsFinite) throw new ArgumentException("Impulse must be finite.");
        var v=LinearVelocity+linear*InverseMass;
        var momentum=MotionType==PhysicsMotionType.Dynamic?AngularMomentum+angular:AngularMomentum;
        if(!v.IsFinite||!momentum.IsFinite||!InverseInertia(momentum).IsFinite)
            throw new InvalidOperationException("Impulse exceeds representable velocity.");
        return new(v,momentum);
    }
    internal void CommitVelocity(BodyVelocityUpdate velocity)
    {
        LinearVelocity=velocity.Linear; AngularMomentum=velocity.AngularMomentum;
    }
    public void ApplyImpulse(CollisionVector impulse,CollisionVector point)
    {
        if(!point.IsFinite) throw new ArgumentOutOfRangeException(nameof(point));
        CommitVelocity(AfterImpulse(impulse,CollisionVector.Cross(point-Center,impulse)));
    }
    public void ApplyWrench(CollisionVector force,CollisionVector torque,double duration)
    {
        Duration(duration);
        if(MotionType!=PhysicsMotionType.Dynamic) throw new InvalidOperationException("Only dynamic bodies integrate applied forces.");
        if(!force.IsFinite||!torque.IsFinite) throw new ArgumentException("Wrench must be finite.");
        CommitVelocity(AfterImpulse(force*duration,torque*duration));
    }
    public void SetKinematicVelocity(CollisionVector linear,CollisionVector angular)
    {
        if(MotionType!=PhysicsMotionType.Kinematic) throw new InvalidOperationException("Only kinematic bodies have prescribed velocity.");
        if(!linear.IsFinite||!angular.IsFinite) throw new ArgumentException("Velocity must be finite.");
        LinearVelocity=linear; _kinematicAngularVelocity=angular;
    }
    private static void Duration(double duration)
    {
        if(!double.IsFinite(duration)||duration<0) throw new ArgumentOutOfRangeException(nameof(duration));
    }
    public BodyTrajectory CreateTrajectory(double duration)=>new(this,duration);
    /// <summary>Commit a prefix of the exact path used by collision queries.
    /// Changing forces, velocities or pose invalidates the captured source.</summary>
    public void Advance(BodyTrajectory trajectory,double elapsed)
    {
        ArgumentNullException.ThrowIfNull(trajectory);
        trajectory.ValidateSource(this);
        var pose=trajectory.At(elapsed);
        if(elapsed==0||MotionType==PhysicsMotionType.Static) return;
        SetPose(pose,AngularMomentum);
    }
    private void SetPose(RigidPose pose,CollisionVector momentum)
    {
        var inertia=MotionType==PhysicsMotionType.Dynamic?LocalInertia.Rotated(pose.Rotation):default;
        var inverse=MotionType==PhysicsMotionType.Dynamic?_localInverse.Rotated(pose.Rotation):default;
        if(MotionType==PhysicsMotionType.Dynamic&&!inverse.Apply(momentum).IsFinite)
            throw new InvalidOperationException("Updated inertia exceeds velocity range.");
        var revision=checked(PoseRevision+1);
        Pose=pose; _inertia=inertia; _inverseInertia=inverse; PoseRevision=revision;
    }
    public PhysicsBodySnapshot Snapshot()=>new(Id,MotionType,Pose,LinearVelocity,AngularMomentum,_kinematicAngularVelocity);
    public void Restore(PhysicsBodySnapshot snapshot)
    {
        if(snapshot.Id!=Id||snapshot.MotionType!=MotionType||!snapshot.Pose.Rotation.IsValid||
            !snapshot.LinearVelocity.IsFinite||!snapshot.AngularMomentum.IsFinite||!snapshot.KinematicAngularVelocity.IsFinite||
            (MotionType!=PhysicsMotionType.Dynamic&&snapshot.AngularMomentum!=default)||
            (MotionType!=PhysicsMotionType.Kinematic&&snapshot.KinematicAngularVelocity!=default)||
            (MotionType==PhysicsMotionType.Static&&snapshot.LinearVelocity!=default))
            throw new ArgumentException("Snapshot does not match this body's declared state.");
        // Validate candidate angular state and pose before mutating any field.
        var inverse=MotionType==PhysicsMotionType.Dynamic?_localInverse.Rotated(snapshot.Pose.Rotation):default;
        if(!inverse.Apply(snapshot.AngularMomentum).IsFinite) throw new ArgumentException("Snapshot exceeds velocity range.");
        SetPose(snapshot.Pose,snapshot.AngularMomentum);
        LinearVelocity=snapshot.LinearVelocity; AngularMomentum=snapshot.AngularMomentum;
        _kinematicAngularVelocity=snapshot.KinematicAngularVelocity;
    }
    public double KineticEnergy=>MotionType==PhysicsMotionType.Dynamic?
        .5*(LinearVelocity.LengthSquared/InverseMass+CollisionVector.Dot(AngularMomentum,AngularVelocity)):0;
}
