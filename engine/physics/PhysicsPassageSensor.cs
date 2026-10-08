using System;

namespace CuriousContraptions.Physics;

public readonly record struct PhysicsPassageKey(PhysicsBodyId Frame,PhysicsBodyId Body);
public enum PhysicsPassagePhase { Unarmed, Armed }
public enum PhysicsPassageEventKind { Passed, OutsideAperture }
public readonly record struct PhysicsPassageState(PhysicsPassageKey Key,PhysicsPassagePhase Phase,
    ulong PassedCount,PhysicsColliderRevision BodyRevision,PhysicsColliderRevision FrameRevision);
public readonly record struct PhysicsPassageEvent(PhysicsPassageKey Key,PhysicsPassageEventKind Kind,
    double Time,RigidPose BodyPose,RigidPose FramePose,CylindricalExtent Extent);

/// <summary>Forward body-centre passage through a frame-local YZ aperture.
/// Arming requires the entire owned collider to clear the upstream side.</summary>
public readonly record struct PhysicsPassageSensor
{
    public PhysicsBodyId Frame { get; }
    public double Radius { get; }
    public double RearmClearance { get; }
    public PhysicsPassageSensor(PhysicsBodyId frame,double radius,double rearmClearance)
    {
        if(!double.IsFinite(radius)||radius<=0)throw new ArgumentOutOfRangeException(nameof(radius));
        if(!double.IsFinite(rearmClearance)||rearmClearance<=0)throw new ArgumentOutOfRangeException(nameof(rearmClearance));
        Frame=frame;Radius=radius;RearmClearance=rearmClearance;
    }
}

public readonly record struct PassageSweepResult(bool Crossed, double Time, int Iterations);

/// <summary>One captured centre-plane path.</summary>
internal sealed class PassageBoundaryPath(BodyTrajectory body,BodyTrajectory frame)
{
    public double Duration=>Math.Min(body.Duration,frame.Duration);
    public double Gap(double time)
    {
        var pose=frame.At(time);var offset=body.At(time).Center-pose.Center;
        var normal=pose.Rotation.Apply(new(1,0,0));
        return -CollisionVector.Dot(offset,normal);
    }
    public PassageSweepResult Sweep(double duration,double tolerance,double allowance)
    {
        const int Steps = 100;
        var dt = duration / Steps;
        for (var i = 0; i < Steps; i++)
        {
            var t0 = i * dt;
            var t1 = (i == Steps - 1) ? duration : (i + 1) * dt;
            var g0 = Gap(t0) + allowance;
            var g1 = Gap(t1) + allowance;
            if (g0 <= tolerance)
            {
                return new(true, t0, i + 1);
            }
            if (g1 <= tolerance)
            {
                var low = t0; var high = t1;
                for (var b = 0; b < 28; b++)
                {
                    var mid = (low + high) * 0.5;
                    var gm = Gap(mid) + allowance;
                    if (gm <= tolerance) high = mid;
                    else low = mid;
                }
                return new(true, high, i + 29);
            }
        }
        var endGap = Gap(duration) + allowance;
        if (endGap <= tolerance) return new(true, duration, Steps);
        return new(false, duration, Steps);
    }
}
