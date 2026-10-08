using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

/// <summary>Transactional impact commands and deferred scene notifications.
/// Effects never call the retired part collision/velocity response hooks.</summary>
public sealed class SceneImpactEffect(SceneBodyKey key,PhysicsBodyId body) : PhysicsImpactEffect(body)
{
    private MachinePart Owner=>key.Owner??throw new InvalidOperationException("A scene effect requires an owner.");
    private readonly record struct Notice(PhysicsBodyId Other,double Speed,CollisionVector Point,CollisionVector SelfLocalPoint,double OtherMass);
    private sealed record State(KeyValuePair<PhysicsBodyId,double>[] Last,Notice[] Notices) : PhysicsImpactEffectState;
    private Dictionary<PhysicsBodyId,double> _last=new();
    private List<Notice> _notices=new();
    public override PhysicsImpactEffectState Capture()=>new State(_last.ToArray(),_notices.ToArray());
    public override void Restore(PhysicsImpactEffectState state)
    {
        if(state is not State saved) throw new ArgumentException("Foreign scene effect snapshot.");
        _last=saved.Last.ToDictionary(entry=>entry.Key,entry=>entry.Value);
        _notices=new(saved.Notices);
    }
    public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
    {
        var self=context.A.After.Id==Body?context.A:context.B;
        var other=context.A.After.Id==Body?context.B:context.A;
        if(other.InverseMass<=0) return new([],[],[]);
        var time=context.Impact.Time;
        if(_last.TryGetValue(other.After.Id,out var previous)&&time-previous<Owner.PhysicsImpactCooldown)
            return new([],[],[]);
        if(context.ApproachSpeed<.05&&Owner.PhysicsImpactCooldown>0) return new([],[],[]);
        var impulses=Owner.PhysicsImpact(self,other,context.ApproachSpeed);
        _last[other.After.Id]=time;
        var point=(context.Impact.Separation.PointA+context.Impact.Separation.PointB)*.5;
        _notices.Add(new(other.After.Id,context.ApproachSpeed,point,
            self.Before.Pose.InverseTransformPoint(point),1/other.InverseMass));
        return new(impulses,[],[]);
    }
    public void Publish(MachineWorld world)
    {
        var notices=_notices.ToArray(); _notices.Clear();
        foreach(var notice in notices)
        {
            var other=world.PhysicsAssembly.Key(notice.Other);
            Owner.ObserveContact(new(key,other,notice.Point,notice.SelfLocalPoint,notice.Speed,notice.OtherMass),world);
        }
    }
}
