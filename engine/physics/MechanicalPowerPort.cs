using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Immutable scalar mechanical port. Speed and effort share one
/// Jacobian so moving-frame work and reactions cannot be evaluated separately.</summary>
public abstract record MechanicalPowerPort
{
    public abstract ConstraintGradient Bind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyList<PhysicsJoint> joints);

    protected static PhysicsBody Resolve(PhysicsBodyId id,IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        if(!bodies.TryGetValue(id,out var body)||body is null||body.Id!=id)
            throw new ArgumentException("Mechanical port requires its current body identity.");
        return body;
    }
}

/// <summary>Current axial travel mapped to a scalar surface-speed coordinate.
/// Scale is dimensionless for a slider, metres for a hinge; sign selects direction.</summary>
public sealed record AxialPowerPort : MechanicalPowerPort
{
    public PhysicsJointId Joint { get; }
    public FrameJointKind Kind { get; }
    public double Scale { get; }

    public AxialPowerPort(PhysicsJointId joint,FrameJointKind kind,double scale)
    {
        if(kind is not (FrameJointKind.Slider or FrameJointKind.Hinge))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if(!double.IsFinite(scale)||scale==0)throw new ArgumentOutOfRangeException(nameof(scale));
        Joint=joint;Kind=kind;Scale=scale;
    }

    public override ConstraintGradient Bind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyList<PhysicsJoint> joints)
    {
        ArgumentNullException.ThrowIfNull(bodies);ArgumentNullException.ThrowIfNull(joints);
        PhysicsFrameJoint? found=null;
        foreach(var declaration in joints)
        {
            ArgumentNullException.ThrowIfNull(declaration);
            if(declaration.Id!=Joint)continue;
            if(found is not null||declaration is not PhysicsFrameJoint frame||frame.Kind!=Kind)
                throw new ArgumentException("Mechanical port requires one matching axial joint.");
            found=frame;
        }
        if(found is null)throw new ArgumentException("Mechanical port joint is missing.");
        var current=(PhysicsFrameJoint)found.Rebind(bodies);
        var gradient=current.Travel.Jacobian.Bind(current.A,current.B);
        return new(gradient.Terms.ToArray().Select(term=>
            new ConstraintTerm(term.Body,term.Linear*Scale,term.Angular*Scale)).ToArray());
    }
}

/// <summary>Work-conjugate relative point speed along a moving frame's axis.
/// The frame reaction acts at the same world point, including its moment arm.</summary>
public sealed record PointPowerPort : MechanicalPowerPort
{
    public PhysicsBodyId Body { get; }
    public PhysicsBodyId Frame { get; }
    public CollisionVector LocalPoint { get; }
    public CollisionVector FrameLocalAxis { get; }

    public PointPowerPort(PhysicsBodyId body,PhysicsBodyId frame,CollisionVector localPoint,
        CollisionVector frameLocalAxis)
    {
        var length=frameLocalAxis.Length;
        if(body==frame||!localPoint.IsFinite||!frameLocalAxis.IsFinite||!double.IsFinite(length)||length<=0)
            throw new ArgumentException("Point port requires distinct bodies and finite point/axis.");
        Body=body;Frame=frame;LocalPoint=localPoint;FrameLocalAxis=frameLocalAxis/length;
    }

    public override ConstraintGradient Bind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,
        IReadOnlyList<PhysicsJoint> joints)
    {
        ArgumentNullException.ThrowIfNull(joints);
        var body=Resolve(Body,bodies);var frame=Resolve(Frame,bodies);
        var point=body.Pose.TransformPoint(LocalPoint);
        var axis=frame.Pose.Rotation.Apply(FrameLocalAxis);
        return new([
            new(body,axis,CollisionVector.Cross(point-body.Center,axis)),
            new(frame,-axis,-CollisionVector.Cross(point-frame.Center,axis))
        ]);
    }
}


/// <summary>Physical relative spin projected on a body's local axis. Scale is
/// metres, mapping angular speed to a scalar surface speed and force to torque.
/// Equal opposite couples act on the body and explicit reaction frame.</summary>
public sealed record AngularPowerPort : MechanicalPowerPort
{
    public PhysicsBodyId Body { get; }
    public PhysicsBodyId Frame { get; }
    public CollisionVector BodyLocalAxis { get; }
    public double Scale { get; }
    public AngularPowerPort(PhysicsBodyId body,PhysicsBodyId frame,CollisionVector bodyLocalAxis,double scale)
    {
        var length=bodyLocalAxis.Length;
        if(body==frame||!bodyLocalAxis.IsFinite||!double.IsFinite(length)||length<=0||!double.IsFinite(scale)||scale==0)
            throw new ArgumentException("Angular port requires distinct bodies, finite axis and nonzero scale.");
        Body=body;Frame=frame;BodyLocalAxis=bodyLocalAxis/length;Scale=scale;
    }
    public override ConstraintGradient Bind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies,IReadOnlyList<PhysicsJoint> joints)
    {
        ArgumentNullException.ThrowIfNull(joints);
        var body=Resolve(Body,bodies);var frame=Resolve(Frame,bodies);
        var angular=body.Pose.Rotation.Apply(BodyLocalAxis)*Scale;
        return new([new(body,default,angular),new(frame,default,-angular)]);
    }
}
