using System;

namespace CuriousContraptions.Physics;

/// <summary>Immutable quadratic-translation, piecewise-exponential rigid path.
/// A held wrench evolves momentum; each rotational segment uses midpoint momentum
/// in the same Lie-midpoint integration queried by collision detection.
/// Queries and committed movement consume this exact captured path.</summary>
public sealed class BodyTrajectory : IRigidTrajectory
{
    private const double MaximumRotationStep=.125;
    private const int MaximumIntegrationSteps=4096;
    private const int MaximumMidpointIterations=64;
    private readonly PhysicsBody _owner;
    private readonly PhysicsBodySnapshot _source;
    private readonly ulong _revision;
    public RigidPoseTrajectory PosePath { get; }
    public double Duration { get; }
    public RigidPose StartPose=>_source.Pose;
    internal PhysicsBodyId SourceBody=>_source.Id;
    private CollisionVector HeldLinearAcceleration { get; }
    private PrescribedBodyMotion? Motion=>_source.PrescribedMotion;
    public double LinearAccelerationBound=>Motion?.LinearAccelerationBound??HeldLinearAcceleration.Length;
    public double LinearJerkBound=>Motion?.LinearJerkBound??0;
    public double AngularAccelerationBound=>Motion?.AngularAccelerationBound??0;
    public double AngularJerkBound=>Motion?.AngularJerkBound??0;
    private readonly BodyWrench _wrench;
    private readonly InertiaTensor _localInverse;
    public double AngularSpeedBound { get; }
    public double PhysicalAngularSpeedBound { get; }
    public double PhysicalAngularAccelerationBound { get; }
    /// <summary>Bound on the second derivative of physical angular velocity
    /// within a single constant-geometric-spin segment, not across spin jumps.</summary>
    public double PhysicalAngularCurvatureBound { get; }
    public int SegmentCount=>PosePath.SegmentCount;

    internal BodyTrajectory(PhysicsBody owner,double duration,BodyWrench wrench)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if(!double.IsFinite(duration)||duration<0) throw new ArgumentOutOfRangeException(nameof(duration));
        if(owner.MotionType!=PhysicsMotionType.Dynamic&&wrench!=default)
            throw new ArgumentException("Prescribed bodies cannot receive a trajectory wrench.");
        _wrench=wrench; HeldLinearAcceleration=wrench.Force*owner.InverseMass;
        _owner=owner; _source=owner.Snapshot(); _revision=owner.PoseRevision; Duration=duration;
        _localInverse=owner.MotionType==PhysicsMotionType.Dynamic?owner.LocalInertia.Inverse():default;
        if(Motion is { } prescribed)
        {
            AngularSpeedBound=PhysicalAngularSpeedBound=prescribed.AngularSpeedBound;
            PhysicalAngularAccelerationBound=prescribed.AngularAccelerationBound;
            PhysicalAngularCurvatureBound=prescribed.AngularJerkBound;
            PosePath=new(StartPose,_source.LinearVelocity,HeldLinearAcceleration,duration,[],[],0,prescribed,AngularSpeedBound,PhysicalAngularSpeedBound);
            At(duration);LinearVelocityAt(duration);
            return;
        }
        var inverse=_localInverse;
        var bound=owner.MotionType==PhysicsMotionType.Dynamic?
            inverse.FrobeniusNorm*Math.Max(_source.AngularMomentum.Length,(_source.AngularMomentum+wrench.Torque*duration).Length):_source.KinematicAngularVelocity.Length;
        var count=owner.MotionType==PhysicsMotionType.Dynamic?
            Math.Max(1,Math.Ceiling(bound*duration/MaximumRotationStep)):1;
        if(!double.IsFinite(bound)||!double.IsFinite(count)||count>MaximumIntegrationSteps)
            throw new InvalidOperationException("Rigid trajectory exceeds its angular-step budget.");
        PhysicalAngularSpeedBound=bound;
        var step=duration/count;
        var rotations=new RigidRotation[(int)count+1]; var spins=new CollisionVector[(int)count];
        rotations[0]=StartPose.Rotation;
        for(var i=0;i<spins.Length;i++)
        {
            var spin=owner.MotionType==PhysicsMotionType.Dynamic?
                MidpointSpin(rotations[i],inverse,_source.AngularMomentum+wrench.Torque*((i+.5)*step),step):_source.KinematicAngularVelocity;
            if(!spin.IsFinite||!double.IsFinite(spin.Length))
                throw new InvalidOperationException("Rigid trajectory spin is not finite.");
            spins[i]=spin;
            AngularSpeedBound=Math.Max(AngularSpeedBound,spin.Length);
            rotations[i+1]=step==0?rotations[i]:RigidRotation.FromRotationVector(spin*step)*rotations[i];
        }
        PhysicalAngularAccelerationBound=owner.MotionType==PhysicsMotionType.Dynamic?
            2*AngularSpeedBound*PhysicalAngularSpeedBound+inverse.FrobeniusNorm*wrench.Torque.Length:0;
        // J(t)=R(t) I^-1 R(t)^T: ||J'|| <= 2|s| ||I^-1||,
        // ||J''|| <= 4|s|² ||I^-1|| and L(t)=L0+torque*t.
        // Spherical inertia is rotation invariant: physical spin is affine
        // under held torque even if the geometric midpoint spin is constant.
        var isotropic=inverse.XX==inverse.YY&&inverse.YY==inverse.ZZ&&inverse.XY==0&&inverse.XZ==0&&inverse.YZ==0;
        PhysicalAngularCurvatureBound=owner.MotionType==PhysicsMotionType.Dynamic&&!isotropic?
            4*AngularSpeedBound*(AngularSpeedBound*PhysicalAngularSpeedBound+inverse.FrobeniusNorm*wrench.Torque.Length):0;
        if(!double.IsFinite(PhysicalAngularAccelerationBound)||!double.IsFinite(PhysicalAngularCurvatureBound))
            throw new InvalidOperationException("Trajectory angular acceleration exceeds numeric range.");
        PosePath=new(StartPose,_source.LinearVelocity,HeldLinearAcceleration,duration,rotations,spins,step,null,AngularSpeedBound,PhysicalAngularSpeedBound);
        // Validate translation and rotation at the horizon before exposing the path.
        At(duration); LinearVelocityAt(duration); AngularMomentumAt(duration);
    }
    private static CollisionVector MidpointSpin(RigidRotation start,InertiaTensor inverse,
        CollisionVector momentum,double duration)
    {
        CollisionVector Omega(RigidRotation rotation)=>rotation.Apply(inverse.Apply(rotation.Inverse().Apply(momentum)));
        var spin=Omega(start);
        if(duration==0) return spin;
        for(var i=0;i<MaximumMidpointIterations;i++)
        {
            var midpoint=RigidRotation.FromRotationVector(spin*(duration*.5))*start;
            var next=Omega(midpoint);
            if((next-spin).Length*duration<=1e-13) return next;
            spin=next;
        }
        throw new InvalidOperationException("Rigid midpoint integration did not converge.");
    }
    public RigidPose At(double time)=>PosePath.At(time);
    public CollisionVector LinearVelocityAt(double time)=>PosePath.LinearVelocityAt(time);
    public CollisionVector AngularMomentumAt(double time)
    {
        if(!double.IsFinite(time)||time<0||time>Duration) throw new ArgumentOutOfRangeException(nameof(time));
        var momentum=_source.AngularMomentum+_wrench.Torque*time;
        if(!momentum.IsFinite) throw new InvalidOperationException("Trajectory momentum exceeds numeric range.");
        return momentum;
    }
    /// <summary>End of the constant-spin segment containing the right-hand
    /// neighbourhood of time. Curvature certificates must not cross a spin jump.</summary>
    public double SegmentEndAfter(double time)=>PosePath.SegmentEndAfter(time);
    /// <summary>Right-hand segment spin (left-hand at the final endpoint).
    /// This is the captured path derivative, not a newly integrated body velocity.</summary>
    public CollisionVector AngularVelocityAt(double time)=>PosePath.AngularVelocityAt(time);

    /// <summary>Angular velocity of the state committed at this time, distinct
    /// from the piecewise-constant spin used to parameterize the pose path.</summary>
    public CollisionVector PhysicalAngularVelocityAt(double time)
    {
        var pose=At(time);
        if(Motion is { } motion)return motion.AngularVelocityAt(time);
        return _source.MotionType==PhysicsMotionType.Dynamic?
            _localInverse.Rotated(pose.Rotation).Apply(AngularMomentumAt(time)):
            _source.KinematicAngularVelocity;
    }

    internal void AccumulatePhysicalVelocity(ref BinaryProductSum sum,CollisionVector linear,CollisionVector angular,double time)
    {
        var pose=At(time);
        if(!linear.IsFinite||!angular.IsFinite)throw new ArgumentException("Constraint directions must be finite.");
        if(Motion is { } motion)
        {
            AddDot(ref sum,linear,motion.LinearVelocityAt(time));
            AddDot(ref sum,angular,motion.AngularVelocityAt(time));
            return;
        }
        AddDot(ref sum,linear,_source.LinearVelocity);
        AddDot(ref sum,linear,HeldLinearAcceleration,time);
        if(_source.MotionType!=PhysicsMotionType.Dynamic)
        {
            AddDot(ref sum,angular,_source.KinematicAngularVelocity);
            return;
        }
        var inverse=_localInverse.Rotated(pose.Rotation);
        AddAngular(ref sum,angular,new(inverse.XX,inverse.XY,inverse.XZ),_source.AngularMomentum.X,_wrench.Torque.X,time);
        AddAngular(ref sum,angular,new(inverse.XY,inverse.YY,inverse.YZ),_source.AngularMomentum.Y,_wrench.Torque.Y,time);
        AddAngular(ref sum,angular,new(inverse.XZ,inverse.YZ,inverse.ZZ),_source.AngularMomentum.Z,_wrench.Torque.Z,time);
    }
    private static void AddDot(ref BinaryProductSum sum,CollisionVector a,CollisionVector b,double scale=1)
    {
        sum.Add(a.X,b.X,scale);sum.Add(a.Y,b.Y,scale);sum.Add(a.Z,b.Z,scale);
    }
    private static void AddAngular(ref BinaryProductSum sum,CollisionVector angular,CollisionVector column,double momentum,double torque,double time)
    {
        sum.Add(angular.X,column.X,momentum);sum.Add(angular.X,column.X,torque,time);
        sum.Add(angular.Y,column.Y,momentum);sum.Add(angular.Y,column.Y,torque,time);
        sum.Add(angular.Z,column.Z,momentum);sum.Add(angular.Z,column.Z,torque,time);
    }

    /// <summary>Terms whose velocity/time minus acceleration is the captured
    /// velocity constraint correction. Held force and torque cancel algebraically
    /// before rounding; forming v(t)/t-a(t) directly loses that cancellation
    /// when the velocity increment is smaller than an ulp of the initial speed.</summary>
    internal (double Velocity,double Acceleration) ConstraintVelocityTerms(
        CollisionVector linear,CollisionVector angular,double time)
    {
        var pose=At(time);
        if(!linear.IsFinite||!angular.IsFinite)throw new ArgumentException("Constraint directions must be finite.");
        if(Motion is { } motion)
            return (CollisionVector.Dot(linear,motion.LinearVelocityAt(time))+
                    CollisionVector.Dot(angular,motion.AngularVelocityAt(time)),
                CollisionVector.Dot(linear,motion.LinearAccelerationAt(time))+
                    CollisionVector.Dot(angular,motion.AngularAccelerationAt(time)));
        if(_source.MotionType!=PhysicsMotionType.Dynamic)
            return (CollisionVector.Dot(linear,_source.LinearVelocity)+
                CollisionVector.Dot(angular,_source.KinematicAngularVelocity),0);
        var inverse=_localInverse.Rotated(pose.Rotation);
        var momentum=AngularMomentumAt(time);
        var spin=AngularVelocityAt(time);
        var gyroscopic=CollisionVector.Cross(spin,inverse.Apply(momentum))-
            inverse.Apply(CollisionVector.Cross(spin,momentum));
        return (CollisionVector.Dot(linear,_source.LinearVelocity)+
                CollisionVector.Dot(angular,inverse.Apply(_source.AngularMomentum)),
            CollisionVector.Dot(angular,gyroscopic));
    }

    /// <summary>Derivative of the committed angular velocity field along this
    /// exact path, including changing world inertia and held applied torque.</summary>
    public CollisionVector PhysicalAngularAccelerationAt(double time)
    {
        var pose=At(time);
        if(Motion is { } motion)return motion.AngularAccelerationAt(time);
        if(_source.MotionType!=PhysicsMotionType.Dynamic) return default;
        var spin=AngularVelocityAt(time); var inverse=_localInverse.Rotated(pose.Rotation);
        var momentum=AngularMomentumAt(time); var velocity=inverse.Apply(momentum);
        return CollisionVector.Cross(spin,velocity)+inverse.Apply(_wrench.Torque-CollisionVector.Cross(spin,momentum));
    }

    /// <summary>Speed of a body-fixed direction within one geometric segment.
    /// Axial spin leaves its own axis unchanged. For varying prescribed spin,
    /// the angular acceleration bound covers departure from the initial rate.</summary>
    public double DirectionSpeedBound(CollisionVector localDirection,double start,double end)
    {
        if(!localDirection.IsFinite||!double.IsFinite(localDirection.Length))
            throw new ArgumentOutOfRangeException(nameof(localDirection));
        if(!double.IsFinite(start)||!double.IsFinite(end)||start<0||end<start||end>Duration||
            end>SegmentEndAfter(start))throw new ArgumentOutOfRangeException(nameof(end));
        var first=CollisionVector.Cross(AngularVelocityAt(start),At(start).Rotation.Apply(localDirection)).Length;
        var bound=AngularAccelerationBound==0?first:Math.Min(AngularSpeedBound*localDirection.Length,
            first+(AngularAccelerationBound+AngularSpeedBound*AngularSpeedBound)*localDirection.Length*(end-start));
        if(bound>0)bound=Math.BitIncrement(bound*(1+1e-12));
        if(!double.IsFinite(bound))throw new InvalidOperationException("Direction speed exceeds numeric range.");
        return bound;
    }

    public PhysicsBody SampleBody(double time)
    {
        var pose=At(time); var velocity=LinearVelocityAt(time);
        if(_source.MotionType==PhysicsMotionType.Dynamic)
        {
            var sample=new PhysicsBody(_source.Id,_source.MotionType,pose,velocity,default,
                1/_owner.InverseMass,_owner.LocalInertia);
            // Preserve the exact state that CommitTrajectory will install.
            // Reconstructing momentum from spin would introduce an I*inverse(I) round trip.
            sample.RestoreState(_source with {Pose=pose,LinearVelocity=velocity,AngularMomentum=AngularMomentumAt(time)});
            return sample;
        }
        return new(_source.Id,_source.MotionType,pose,velocity,PhysicalAngularVelocityAt(time),
            prescribedMotion:PrescribedMotionAt(time));
    }

    public CollisionVector LinearAccelerationAt(double time)
    {
        At(time);return Motion is { } motion?motion.LinearAccelerationAt(time):HeldLinearAcceleration;
    }
    public CollisionVector GeometricAngularAccelerationAt(double time)
    {
        At(time);return Motion is { } motion?motion.AngularAccelerationAt(time):default;
    }
    internal PrescribedBodyMotion? PrescribedMotionAt(double time)
    {
        At(time);return Motion?.Advance(time);
    }

    internal void ValidateSource(PhysicsBody body)
    {
        if(!ReferenceEquals(body,_owner)||body.PoseRevision!=_revision||body.Snapshot()!=_source)
            throw new InvalidOperationException("Trajectory source changed; capture a new path before advancing.");
    }
}
