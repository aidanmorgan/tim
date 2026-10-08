using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public enum PhysicsContactLoadPhase { Empty, Underweight, Loaded }

/// <summary>Direct-contact mass qualification in an owned frame-local region.
/// This is geometric contact sensing, not a force or stacked-load measurement.</summary>
public sealed record PhysicsContactLoadSensor
{
    public PhysicsBodyId Frame { get; }
    public CollisionBounds Region { get; }
    public CollisionVector LocalNormal { get; }
    public double MinimumAlignment { get; }
    public double MinimumMass { get; }
    public PhysicsContactLoadSensor(PhysicsBodyId frame,CollisionBounds region,CollisionVector localNormal,double minimumAlignment,double minimumMass)
    {
        if(!region.Minimum.IsFinite||!region.Maximum.IsFinite||
            region.Minimum.X>=region.Maximum.X||region.Minimum.Y>=region.Maximum.Y||region.Minimum.Z>=region.Maximum.Z)
            throw new ArgumentOutOfRangeException(nameof(region));
        if(!localNormal.IsFinite||!double.IsFinite(localNormal.Length)||localNormal.Length<=0)
            throw new ArgumentException("Contact normal must be finite and nonzero.",nameof(localNormal));
        if(!double.IsFinite(minimumAlignment)||minimumAlignment<=0||minimumAlignment>1)
            throw new ArgumentOutOfRangeException(nameof(minimumAlignment));
        if(!double.IsFinite(minimumMass)||minimumMass<=0)throw new ArgumentOutOfRangeException(nameof(minimumMass));
        Frame=frame;Region=region;LocalNormal=localNormal/localNormal.Length;MinimumAlignment=minimumAlignment;MinimumMass=minimumMass;
    }
    internal PhysicsContactLoadReading Measure(PhysicsBody frame,IEnumerable<ContactGap> contacts)
    {
        var normal=frame.Pose.Rotation.Apply(LocalNormal);
        var bodies=new Dictionary<PhysicsBodyId,PhysicsBody>();
        foreach(var contact in contacts)
        {
            var body=contact.B;
            if(body.MotionType!=PhysicsMotionType.Dynamic||CollisionVector.Dot(-contact.Normal,normal)<MinimumAlignment)continue;
            var point=frame.Pose.InverseTransformPoint(contact.Point);
            if(point.X<Region.Minimum.X||point.X>Region.Maximum.X||point.Y<Region.Minimum.Y||
                point.Y>Region.Maximum.Y||point.Z<Region.Minimum.Z||point.Z>Region.Maximum.Z)continue;
            bodies.TryAdd(body.Id,body);
        }
        var ordered=bodies.Values.OrderBy(body=>body.Id.Index).ToArray();
        var mass=ordered.Sum(body=>1/body.InverseMass);
        if(!double.IsFinite(mass))throw new InvalidOperationException("Contact mass exceeds numeric range.");
        return new(Frame,mass,mass==0?PhysicsContactLoadPhase.Empty:
            mass<MinimumMass?PhysicsContactLoadPhase.Underweight:PhysicsContactLoadPhase.Loaded,
            ordered.Select(body=>body.Id).ToArray());
    }
}

public sealed class PhysicsContactLoadReading
{
    public PhysicsBodyId Frame { get; }
    public double Mass { get; }
    public PhysicsContactLoadPhase Phase { get; }
    private readonly PhysicsBodyId[] _bodies;
    public ReadOnlySpan<PhysicsBodyId> Bodies=>_bodies;
    internal PhysicsContactLoadReading(PhysicsBodyId frame,double mass,PhysicsContactLoadPhase phase,PhysicsBodyId[] bodies)
    { Frame=frame;Mass=mass;Phase=phase;_bodies=(PhysicsBodyId[])bodies.Clone(); }
}
