using System;

namespace CuriousContraptions.Physics;

/// <summary>Immutable piecewise-exponential rigid path. Each dynamic segment's
/// spin comes from the same Lie-midpoint integration used for its endpoint.
/// Queries and committed movement consume this exact captured path.</summary>
public sealed class BodyTrajectory : IRigidTrajectory
{
    private const double MaximumRotationStep=.125;
    private const int MaximumIntegrationSteps=4096;
    private const int MaximumMidpointIterations=64;
    private readonly PhysicsBody _owner;
    private readonly PhysicsBodySnapshot _source;
    private readonly ulong _revision;
    private readonly RigidRotation[] _rotations;
    private readonly CollisionVector[] _spins;
    private readonly double _step;
    public double Duration { get; }
    public RigidPose StartPose=>_source.Pose;
    public CollisionVector LinearVelocity=>_source.LinearVelocity;
    public double AngularSpeedBound { get; }
    public int SegmentCount=>_spins.Length;

    internal BodyTrajectory(PhysicsBody owner,double duration)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if(!double.IsFinite(duration)||duration<0) throw new ArgumentOutOfRangeException(nameof(duration));
        _owner=owner; _source=owner.Snapshot(); _revision=owner.PoseRevision; Duration=duration;
        var inverse=owner.MotionType==PhysicsMotionType.Dynamic?owner.LocalInertia.Inverse():default;
        var bound=owner.MotionType==PhysicsMotionType.Dynamic?
            inverse.FrobeniusNorm*_source.AngularMomentum.Length:_source.KinematicAngularVelocity.Length;
        var count=owner.MotionType==PhysicsMotionType.Dynamic?
            Math.Max(1,Math.Ceiling(bound*duration/MaximumRotationStep)):1;
        if(!double.IsFinite(bound)||!double.IsFinite(count)||count>MaximumIntegrationSteps)
            throw new InvalidOperationException("Rigid trajectory exceeds its angular-step budget.");
        _step=duration/count;
        _rotations=new RigidRotation[(int)count+1]; _spins=new CollisionVector[(int)count];
        _rotations[0]=StartPose.Rotation;
        for(var i=0;i<_spins.Length;i++)
        {
            var spin=owner.MotionType==PhysicsMotionType.Dynamic?
                MidpointSpin(_rotations[i],inverse,_source.AngularMomentum,_step):_source.KinematicAngularVelocity;
            if(!spin.IsFinite||!double.IsFinite(spin.Length))
                throw new InvalidOperationException("Rigid trajectory spin is not finite.");
            _spins[i]=spin;
            AngularSpeedBound=Math.Max(AngularSpeedBound,spin.Length);
            _rotations[i+1]=_step==0?_rotations[i]:RigidRotation.FromRotationVector(spin*_step)*_rotations[i];
        }
        // Validate translation and rotation at the horizon before exposing the path.
        At(duration);
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
    public RigidPose At(double time)
    {
        if(!double.IsFinite(time)||time<0||time>Duration) throw new ArgumentOutOfRangeException(nameof(time));
        if(time==0) return StartPose;
        var center=StartPose.Center+LinearVelocity*time;
        if(time==Duration) return new(center,_rotations[^1]);
        var index=Math.Min(_spins.Length-1,(int)(time/_step));
        var local=time-index*_step;
        return new(center,RigidRotation.FromRotationVector(_spins[index]*local)*_rotations[index]);
    }
    /// <summary>End of the constant-spin segment containing the right-hand
    /// neighbourhood of time. Curvature certificates must not cross a spin jump.</summary>
    public double SegmentEndAfter(double time)
    {
        if(!double.IsFinite(time)||time<0||time>Duration) throw new ArgumentOutOfRangeException(nameof(time));
        if(time==Duration) return Duration;
        var index=Math.Min(_spins.Length-1,(int)(time/_step));
        while(index<_spins.Length-1&&(index+1)*_step<=time) index++;
        return index==_spins.Length-1?Duration:(index+1)*_step;
    }
    internal void ValidateSource(PhysicsBody body)
    {
        if(!ReferenceEquals(body,_owner)||body.PoseRevision!=_revision||body.Snapshot()!=_source)
            throw new InvalidOperationException("Trajectory source changed; capture a new path before advancing.");
    }
}
