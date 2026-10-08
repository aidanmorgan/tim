using System;

namespace CuriousContraptions.Physics;

public readonly record struct PhysicsResidenceKey(PhysicsBodyId Frame,PhysicsBodyId Body);
public enum PhysicsResidencePhase { Outside, Dwelling, Captured }
public readonly record struct PhysicsResidenceState(PhysicsResidenceKey Key,PhysicsResidencePhase Phase,double Elapsed);

/// <summary>Sampled centre residence in a frame-local open box at bounded relative speed.
/// Consecutive eligible committed motion endpoints accrue physical time; capture latches.
/// This does not certify continuous membership between samples.</summary>
public sealed record PhysicsResidenceSensor
{
    public PhysicsResidenceKey Key { get; }
    public CollisionBounds Region { get; }
    public double MaximumSpeed { get; }
    public double Dwell { get; }
    public PhysicsResidenceSensor(PhysicsResidenceKey key,CollisionBounds region,double maximumSpeed,double dwell)
    {
        if(key.Body==key.Frame)throw new ArgumentException("Residence requires distinct target and frame bodies.",nameof(key));
        foreach(var value in new[]{region.Minimum.X,region.Minimum.Y,region.Minimum.Z,
            region.Maximum.X,region.Maximum.Y,region.Maximum.Z})
            if(!double.IsFinite(value))throw new ArgumentOutOfRangeException(nameof(region));
        if(region.Minimum.X>=region.Maximum.X||region.Minimum.Y>=region.Maximum.Y||region.Minimum.Z>=region.Maximum.Z)
            throw new ArgumentOutOfRangeException(nameof(region));
        if(!double.IsFinite(maximumSpeed)||maximumSpeed<=0)throw new ArgumentOutOfRangeException(nameof(maximumSpeed));
        if(!double.IsFinite(dwell)||dwell<=0)throw new ArgumentOutOfRangeException(nameof(dwell));
        Key=key;Region=region;MaximumSpeed=maximumSpeed;Dwell=dwell;
    }
    internal bool Contains(PhysicsBody body,PhysicsBody frame)
    {
        var local=frame.Pose.InverseTransformPoint(body.Center);
        return local.X>Region.Minimum.X&&local.X<Region.Maximum.X&&
            local.Y>Region.Minimum.Y&&local.Y<Region.Maximum.Y&&
            local.Z>Region.Minimum.Z&&local.Z<Region.Maximum.Z&&
            (body.LinearVelocity-frame.PointVelocity(body.Center)).Length<MaximumSpeed;
    }
}
