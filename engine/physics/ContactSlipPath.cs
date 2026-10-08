using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Material slip on the exact captured trajectories of every contact
/// participant, including driven shafts. Bounds never omit transmission motion.</summary>
public sealed class ContactSlipPath
{
    private readonly MaterialContact _contact;
    private readonly IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> _paths;
    private readonly BodyTrajectory _a,_b;
    public double Duration { get; }
    public ContactSlipPath(MaterialContact contact,IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths)
    {
        ArgumentNullException.ThrowIfNull(contact); ArgumentNullException.ThrowIfNull(paths);
        var owned=new Dictionary<PhysicsBodyId,BodyTrajectory>();
        foreach(var body in contact.Bodies)
        {
            if(!paths.TryGetValue(body.Id,out var path)||path is null)
                throw new ArgumentException("Material slip requires every participant trajectory.");
            path.ValidateSource(body); owned.Add(body.Id,path);
        }
        _contact=contact; _paths=owned; _a=owned[contact.Gap.A.Id]; _b=owned[contact.Gap.B.Id];
        Duration=owned.Values.Min(p=>p.Duration);
    }
    private void ValidateSources()
    {
        foreach(var body in _contact.Bodies) _paths[body.Id].ValidateSource(body);
    }
    public double SegmentEndAfter(double time)
    {
        ValidateSources();
        return _paths.Values.Min(p=>p.SegmentEndAfter(time));
    }
    public ContactSlipSample At(double time)
    {
        ValidateSources();
        var gap=_contact.Gap;
        var rigid=gap.SlipAt(_a,_b,time);
        if(_contact.Surfaces.Length==0) return rigid;
        PhysicsBody Geometric(PhysicsBody source,BodyTrajectory path)=>new(source.Id,PhysicsMotionType.Kinematic,
            path.At(time),path.LinearVelocityAt(time),path.AngularVelocityAt(time));
        var states=_contact.Bodies.ToArray().ToDictionary(body=>body.Id,body=>_paths[body.Id].SampleBody(time));
        var material=_contact.Rebind(states);
        var geometry=gap.Rebind(new Dictionary<PhysicsBodyId,PhysicsBody>
        {
            [gap.A.Id]=Geometric(gap.A,_a),[gap.B.Id]=Geometric(gap.B,_b)
        });
        var slip=material.Kinematics.SlipAlong(_paths,time); var derivative=rigid.Derivative;
        foreach(var surface in _contact.Surfaces)
        {
            var carrier=_paths[surface.Carrier.Id]; var shaft=_paths[surface.Shaft.Id];
            carrier.ValidateSource(surface.Carrier); shaft.ValidateSource(surface.Shaft);
            var basis=carrier.At(time).Rotation;
            var direction=basis.Apply(surface.LocalDirection);
            var axis=basis.Apply(surface.Drive.LocalB.Orientation.Apply(new(0,0,1)));
            var geometricSpin=carrier.AngularVelocityAt(time);
            var relative=shaft.PhysicalAngularVelocityAt(time)-carrier.PhysicalAngularVelocityAt(time);
            var acceleration=shaft.PhysicalAngularAccelerationAt(time)-carrier.PhysicalAngularAccelerationAt(time);
            var q=CollisionVector.Dot(axis,relative);
            var qRate=CollisionVector.Dot(CollisionVector.Cross(geometricSpin,axis),relative)+CollisionVector.Dot(axis,acceleration);
            var ratio=(surface.Carrier==gap.A?1:-1)*surface.TravelPerRadian;
            var motion=direction*(ratio*q);
            var rate=(CollisionVector.Cross(geometricSpin,direction)*q+direction*qRate)*ratio;
            var n=geometry.Normal; var dn=geometry.NormalVelocity;
            derivative+=rate-n*CollisionVector.Dot(n,rate)-dn*CollisionVector.Dot(n,motion)-n*CollisionVector.Dot(dn,motion);
        }
        if(!slip.IsFinite||!derivative.IsFinite) throw new InvalidOperationException("Driven material slip is not representable.");
        return new(slip,derivative);
    }
    public ContactSlipBounds? Bounds(double start,double end)
    {
        ValidateSources();
        if(end>SegmentEndAfter(start)) throw new ArgumentException("Material slip interval crosses a participant segment.");
        var rigid=_contact.Gap.SlipBounds(_a,_b,start,end);
        if(rigid is not { } bound) return null;
        var rate=bound.Rate; var curvature=bound.Curvature;
        foreach(var surface in _contact.Surfaces)
        {
            var carrier=_paths[surface.Carrier.Id]; var shaft=_paths[surface.Shaft.Id];
            var spin=carrier.AngularSpeedBound;
            var q=shaft.PhysicalAngularSpeedBound+carrier.PhysicalAngularSpeedBound;
            var acceleration=shaft.PhysicalAngularAccelerationBound+carrier.PhysicalAngularAccelerationBound;
            var qRate=spin*q+acceleration;
            var qCurvature=(carrier.AngularAccelerationBound+spin*spin)*q+2*spin*acceleration+
                shaft.PhysicalAngularCurvatureBound+carrier.PhysicalAngularCurvatureBound;
            var ratio=Math.Abs(surface.TravelPerRadian);
            var velocity=ratio*q;
            var first=ratio*(spin*q+qRate);
            var second=ratio*((carrier.AngularAccelerationBound+spin*spin)*q+2*spin*qRate+qCurvature);
            rate+=first+2*bound.NormalRate*velocity;
            curvature+=second+4*bound.NormalRate*first+
                (2*bound.NormalCurvature+2*bound.NormalRate*bound.NormalRate)*velocity;
        }
        if(!double.IsFinite(rate)||!double.IsFinite(curvature)) return null;
        return new(Math.BitIncrement(rate*(1+1e-12)),Math.BitIncrement(curvature*(1+1e-12)),bound.NormalRate,bound.NormalCurvature);
    }
}
