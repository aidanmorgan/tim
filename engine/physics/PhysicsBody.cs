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
        var cx=rotation.Apply(new CollisionVector(1,0,0));
        var cy=rotation.Apply(new CollisionVector(0,1,0));
        var cz=rotation.Apply(new CollisionVector(0,0,1));
        var x=new CollisionVector(cx.X,cy.X,cz.X);
        var y=new CollisionVector(cx.Y,cy.Y,cz.Y);
        var z=new CollisionVector(cx.Z,cy.Z,cz.Z);
        return new(RotatedComponent(x,x,ZZ),RotatedComponent(y,y,ZZ),RotatedComponent(z,z,ZZ),
            RotatedComponent(x,y,0),RotatedComponent(x,z,0),RotatedComponent(y,z,0));
    }
    private double RotatedComponent(CollisionVector row,CollisionVector column,double identity)
    {
        // R*(I-ZZ*Identity)*R^T + ZZ*Identity. Retain the subtraction
        // inside the exact product sum: rounding I-ZZ first loses small
        // diagonal components of strongly anisotropic tensors.
        Span<ulong> storage=stackalloc ulong[BinaryProductSum.StorageLength];
        var sum=new BinaryProductSum(storage);
        // Omit exactly zero algebraic contributions, without rounding a
        // nonzero diagonal difference or choosing a different rotation map.
        if(XX!=ZZ){sum.Add(XX,row.X,column.X);sum.Add(-ZZ,row.X,column.X);}
        if(YY!=ZZ){sum.Add(YY,row.Y,column.Y);sum.Add(-ZZ,row.Y,column.Y);}
        if(XY!=0){sum.Add(XY,row.X,column.Y);sum.Add(XY,row.Y,column.X);}
        if(XZ!=0){sum.Add(XZ,row.X,column.Z);sum.Add(XZ,row.Z,column.X);}
        if(YZ!=0){sum.Add(YZ,row.Y,column.Z);sum.Add(YZ,row.Z,column.Y);}
        if(sum.IsZero)return identity;
        sum.Add(identity,1);
        return sum.Finish();
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
    RigidPose Pose,CollisionVector LinearVelocity,CollisionVector AngularMomentum,CollisionVector KinematicAngularVelocity)
{
    public PrescribedBodyMotion? PrescribedMotion { get; init; }
}
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
    public PrescribedBodyMotion? PrescribedMotion { get; private set; }
    public CollisionVector PrescribedLinearAcceleration=>PrescribedMotion?.LinearAccelerationAt(0)??default;
    public CollisionVector PrescribedAngularAcceleration=>PrescribedMotion?.AngularAccelerationAt(0)??default;

    private bool _worldOwned;
    internal void RequireUnowned()
    {
        if(_worldOwned)throw new InvalidOperationException("World-owned body motion must be changed through its world.");
    }
    internal void AttachToWorld()
    {
        RequireUnowned();
        _worldOwned=true;
    }

    public PhysicsBody(PhysicsBodyId id,PhysicsMotionType motionType,RigidPose pose,
        CollisionVector linearVelocity,CollisionVector angularVelocity,double mass=0,InertiaTensor inertia=default,PrescribedBodyMotion? prescribedMotion=null)
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
        if(prescribedMotion is not null&&(motionType!=PhysicsMotionType.Kinematic||pose!=prescribedMotion.At(0)||
            linearVelocity!=prescribedMotion.LinearVelocityAt(0)||angularVelocity!=prescribedMotion.AngularVelocityAt(0)))
            throw new ArgumentException("Prescribed body state must match its motion cursor.");
        PrescribedMotion=prescribedMotion;
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
        RequireUnowned();
        CommitImpulse(impulse,point);
    }
    internal void CommitImpulse(CollisionVector impulse,CollisionVector point)
    {
        if(!point.IsFinite) throw new ArgumentOutOfRangeException(nameof(point));
        CommitVelocity(AfterImpulse(impulse,CollisionVector.Cross(point-Center,impulse)));
    }
    public void ApplyWrench(CollisionVector force,CollisionVector torque,double duration)
    {
        RequireUnowned();
        Duration(duration);
        if(MotionType!=PhysicsMotionType.Dynamic) throw new InvalidOperationException("Only dynamic bodies integrate applied forces.");
        if(!force.IsFinite||!torque.IsFinite) throw new ArgumentException("Wrench must be finite.");
        CommitVelocity(AfterImpulse(force*duration,torque*duration));
    }
    public void SetKinematicVelocity(CollisionVector linear,CollisionVector angular)
    {
        RequireUnowned();
        if(MotionType!=PhysicsMotionType.Kinematic||PrescribedMotion is not null)
            throw new InvalidOperationException("Only unprofiled kinematic bodies accept independent velocity commands.");
        if(!linear.IsFinite||!angular.IsFinite) throw new ArgumentException("Velocity must be finite.");
        LinearVelocity=linear; _kinematicAngularVelocity=angular;
    }
    private static void Duration(double duration)
    {
        if(!double.IsFinite(duration)||duration<0) throw new ArgumentOutOfRangeException(nameof(duration));
    }
    public BodyTrajectory CreateTrajectory(double duration,BodyWrench wrench)=>new(this,duration,wrench);
    /// <summary>Commit a prefix of the exact path used by collision queries.
    /// Changing forces, velocities or pose invalidates the captured source.</summary>
    public void Advance(BodyTrajectory trajectory,double elapsed)
    {
        RequireUnowned();
        CommitTrajectory(trajectory,elapsed);
    }
    internal void CommitTrajectory(BodyTrajectory trajectory,double elapsed)
    {
        ArgumentNullException.ThrowIfNull(trajectory);
        trajectory.ValidateSource(this);
        var pose=trajectory.At(elapsed);
        if(elapsed==0||MotionType==PhysicsMotionType.Static) return;
        var linear=trajectory.LinearVelocityAt(elapsed);
        var momentum=trajectory.AngularMomentumAt(elapsed);
        SetPose(pose,momentum);
        LinearVelocity=linear; AngularMomentum=momentum;
        if(MotionType==PhysicsMotionType.Kinematic)_kinematicAngularVelocity=trajectory.PhysicalAngularVelocityAt(elapsed);
        PrescribedMotion=trajectory.PrescribedMotionAt(elapsed);
    }
    /// <summary>Constraint projection changes configuration, not physical linear
    /// velocity or world angular momentum. The rotated inertia is refreshed.</summary>
    internal void CorrectPose(CollisionVector translation,CollisionVector rotation)
    {
        if(MotionType!=PhysicsMotionType.Dynamic) throw new InvalidOperationException("Only dynamic poses can be projected.");
        if(!translation.IsFinite||!rotation.IsFinite) throw new ArgumentException("Pose correction must be finite.");
        SetPose(new(Pose.Center+translation,RigidRotation.FromRotationVector(rotation)*Pose.Rotation),AngularMomentum);
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
    public PhysicsBodySnapshot Snapshot()=>new(Id,MotionType,Pose,LinearVelocity,AngularMomentum,_kinematicAngularVelocity) {PrescribedMotion=PrescribedMotion};
    public void Restore(PhysicsBodySnapshot snapshot)
    {
        RequireUnowned();
        RestoreState(snapshot);
    }
    internal void RestoreState(PhysicsBodySnapshot snapshot)
    {
        if(snapshot.Id!=Id||snapshot.MotionType!=MotionType||!snapshot.Pose.Rotation.IsValid||
            !snapshot.LinearVelocity.IsFinite||!snapshot.AngularMomentum.IsFinite||!snapshot.KinematicAngularVelocity.IsFinite||
            (MotionType!=PhysicsMotionType.Dynamic&&snapshot.AngularMomentum!=default)||
            (MotionType!=PhysicsMotionType.Kinematic&&snapshot.KinematicAngularVelocity!=default)||
            (MotionType==PhysicsMotionType.Static&&snapshot.LinearVelocity!=default))
            throw new ArgumentException("Snapshot does not match this body's declared state.");
        if((snapshot.PrescribedMotion is null)!=(PrescribedMotion is null))
            throw new ArgumentException("Snapshot changes the prescribed motion declaration.");
        if(snapshot.PrescribedMotion is { } motion&&(motion.Path!=PrescribedMotion!.Path||motion.LocalPose!=PrescribedMotion.LocalPose||
            snapshot.Pose!=motion.At(0)||snapshot.LinearVelocity!=motion.LinearVelocityAt(0)||
            snapshot.KinematicAngularVelocity!=motion.AngularVelocityAt(0)))
            throw new ArgumentException("Snapshot does not match its prescribed motion cursor.");
        // Validate candidate angular state and pose before mutating any field.
        var inverse=MotionType==PhysicsMotionType.Dynamic?_localInverse.Rotated(snapshot.Pose.Rotation):default;
        if(!inverse.Apply(snapshot.AngularMomentum).IsFinite) throw new ArgumentException("Snapshot exceeds velocity range.");
        SetPose(snapshot.Pose,snapshot.AngularMomentum);
        LinearVelocity=snapshot.LinearVelocity; AngularMomentum=snapshot.AngularMomentum;
        _kinematicAngularVelocity=snapshot.KinematicAngularVelocity;
        PrescribedMotion=snapshot.PrescribedMotion;
    }
    public double KineticEnergy=>MotionType==PhysicsMotionType.Dynamic?
        .5*(LinearVelocity.LengthSquared/InverseMass+CollisionVector.Dot(AngularMomentum,AngularVelocity)):0;
}
