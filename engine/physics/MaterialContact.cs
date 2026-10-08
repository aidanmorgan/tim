using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>One captured contact feature with its material transmission branch.
/// Rebinding retains that branch while the geometry is continuously predicted.</summary>
public sealed class MaterialContact
{
    public ContactGap Gap { get; }
    private readonly DrivenSurface[] _surfaces;
    private readonly PhysicsBody[] _bodies;
    public ReadOnlySpan<DrivenSurface> Surfaces=>_surfaces;
    public ReadOnlySpan<PhysicsBody> Bodies=>_bodies;
    public ContactKinematics Kinematics { get; }
    public CollisionVector Slip=>Kinematics.Slip;
    public CollisionVector TangentBias { get; }
    public MaterialContact(ContactGap gap,IEnumerable<DrivenSurface> surfaces):this(gap,Select(gap,surfaces)) { }
    private static DrivenSurface[] Select(ContactGap gap,IEnumerable<DrivenSurface> surfaces)
    {
        ArgumentNullException.ThrowIfNull(gap); ArgumentNullException.ThrowIfNull(surfaces);
        var supplied=surfaces.ToArray();
        if(supplied.Any(s=>s is null)) throw new ArgumentException("Material surfaces cannot be null.");
        return supplied.Where(s=>s.Matches(gap.A,gap.B,gap.Normal)).ToArray();
    }
    private MaterialContact(ContactGap gap,DrivenSurface[] surfaces)
    {
        Gap=gap; _surfaces=surfaces;
        var rigid=ContactKinematics.AtPoint(gap.A,gap.B,gap.Point,gap.Normal);
        Kinematics=Map(rigid,surfaces);
        // The analytic gap gradient, not a material point gradient, supplies
        // the normal force equation for curved/rounded geometric features.
        Kinematics=new(gap.Normal,gap.NormalJacobian.Bind(gap.A,gap.B),Kinematics.TangentU,Kinematics.TangentV);
        _bodies=Kinematics.Bodies.ToArray();
        var bias=gap.TangentialBias;
        foreach(var surface in surfaces)
        {
            var sign=surface.Carrier==gap.A?1:-1;
            var direction=surface.Direction; var axis=surface.Axis;
            var relative=surface.Shaft.AngularVelocity-surface.Carrier.AngularVelocity;
            var q=CollisionVector.Dot(axis,relative);
            var axisRate=CollisionVector.Cross(surface.Carrier.AngularVelocity,axis);
            var motion=direction*(sign*surface.TravelPerRadian*q);
            var rate=(CollisionVector.Cross(surface.Carrier.AngularVelocity,direction)*q+
                direction*CollisionVector.Dot(axisRate,relative))*(sign*surface.TravelPerRadian);
            bias+=rate-gap.Normal*CollisionVector.Dot(gap.Normal,rate)-
                gap.NormalVelocity*CollisionVector.Dot(gap.Normal,motion);
        }
        if(!bias.IsFinite) throw new InvalidOperationException("Driven contact acceleration is not representable.");
        TangentBias=bias;
    }
    internal static ContactKinematics Map(ContactKinematics rigid,IEnumerable<DrivenSurface> surfaces)
    {
        var us=rigid.TangentU.Terms.ToArray().ToList(); var vs=rigid.TangentV.Terms.ToArray().ToList();
        // The normal gradient's carrier coefficient distinguishes A from B.
        foreach(var surface in surfaces)
        {
            var term=rigid.NormalGradient.Terms.ToArray().Single(t=>t.Body==surface.Carrier);
            var sign=CollisionVector.Dot(term.Linear,rigid.Normal)>0?1:-1;
            void Add(List<ConstraintTerm> terms,CollisionVector tangent)
            {
                var angular=surface.Axis*(sign*surface.TravelPerRadian*CollisionVector.Dot(tangent,surface.Direction));
                terms.Add(new(surface.Shaft,default,angular));
                terms.Add(new(surface.Carrier,default,-angular));
            }
            Add(us,rigid.U); Add(vs,rigid.V);
        }
        return new(rigid.Normal,rigid.NormalGradient,new(us.ToArray()),new(vs.ToArray()));
    }
    public MaterialContact Rebind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> states)=>
        new(Gap.Rebind(states),_surfaces.Select(s=>s.Rebind(states)).ToArray());
    public ContactForce Force(double friction,FrictionRegime regime,
        IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double time)=>
        new(Kinematics,Gap.ConvectiveAcceleration,TangentBias,
            Kinematics.SlipAlong(paths,time),friction,regime);
}
