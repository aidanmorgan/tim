using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

public enum CompliantContactInitialState { Unloaded, Preloaded }
public enum CompliantContactPhase { Unarmed, Ready, Engaged }
public readonly record struct CompliantContactKey(PhysicsBodyId Body,PhysicsBodyId Frame);
public readonly record struct PhysicsCompliantEntry(CompliantContactKey Contact,double Time,double ApproachSpeed);

/// <summary>Committed one-sided contact history owned and snapshotted by the
/// shared world. Geometry is sampled at committed event/substep boundaries;
/// continuous membrane-entry sweeping is a separate force-horizon obligation.</summary>
public readonly record struct CompliantContactState(CompliantContactKey Key,CompliantContactPhase Phase,
    SupportFootprint Footprint,double RelativeSpeed,ulong EpisodeCount)
{
    private static (SupportFootprint Footprint,double Speed,bool Enabled) Sample(CompliantContactLoad load,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        load.Validate(bodies);
        foreach(var id in new[]{load.Body,load.Frame})
            if(!colliders.TryGetValue(id,out var collider)||collider.Body!=id||collider.Geometry is null||
                !Enum.IsDefined(collider.Participation))
                throw new ArgumentException("Compliant contact requires current participant collider declarations.");
        var body=bodies[load.Body];var frame=bodies[load.Frame];
        var footprint=SupportFootprint.Sample(colliders[load.Body].Geometry,body.Pose,frame.Pose);
        var point=frame.Pose.TransformPoint(footprint.LowestPoint);
        var normal=frame.Pose.Rotation.Apply(new(0,1,0));
        var speed=CollisionVector.Dot(body.PointVelocity(point)-frame.PointVelocity(point),normal);
        if(!double.IsFinite(speed))throw new InvalidOperationException("Compliant contact speed exceeds numeric range.");
        return (footprint,speed,colliders[load.Body].Participation==CollisionParticipation.Enabled&&
            colliders[load.Frame].Participation==CollisionParticipation.Enabled);
    }

    internal static CompliantContactState Create(CompliantContactLoad load,IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders)
    {
        var sample=Sample(load,bodies,colliders);
        var phase=sample.Enabled&&sample.Footprint.LowestPoint.Y>=load.RestHeight
            ?CompliantContactPhase.Ready:CompliantContactPhase.Unarmed;
        if(load.InitialState==CompliantContactInitialState.Preloaded)
        {
            if(!sample.Enabled||!sample.Footprint.Fits(load.HalfX,load.HalfZ)||sample.Footprint.LowestPoint.Y>=load.RestHeight)
                throw new ArgumentException("A preloaded contact requires enabled, fitting, compressed initial geometry.");
            phase=CompliantContactPhase.Engaged;
        }
        return new(load.Key,phase,sample.Footprint,sample.Speed,phase==CompliantContactPhase.Engaged?1ul:0ul);
    }

    internal (CompliantContactState State,PhysicsCompliantEntry? Entry) Refresh(CompliantContactLoad load,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,IReadOnlyDictionary<PhysicsBodyId,PhysicsColliderUpdate> colliders,double time)
    {
        if(load.Key!=Key)throw new ArgumentException("Contact history belongs to another declaration.");
        var sample=Sample(load,bodies,colliders);
        var phase=Phase;var episodes=EpisodeCount;
        PhysicsCompliantEntry? entry=null;
        if(!sample.Enabled)phase=CompliantContactPhase.Unarmed;
        else if(sample.Footprint.LowestPoint.Y>=load.RestHeight)phase=CompliantContactPhase.Ready;
        else if(!sample.Footprint.Fits(load.HalfX,load.HalfZ))phase=CompliantContactPhase.Unarmed;
        else if(phase==CompliantContactPhase.Ready&&Footprint.LowestPoint.Y>=load.RestHeight&&sample.Speed<=0)
        {
            phase=CompliantContactPhase.Engaged;
            episodes=checked(episodes+1);
            entry=new(Key,time,Math.Max(0,-sample.Speed));
        }
        else if(phase!=CompliantContactPhase.Engaged)phase=CompliantContactPhase.Unarmed;
        return (new(Key,phase,sample.Footprint,sample.Speed,episodes),entry);
    }
}
