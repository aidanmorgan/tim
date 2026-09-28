using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public readonly record struct PhysicsJointId
{
    public int Index { get; }
    public PhysicsJointId(int index)
    {
        if(index<0) throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}
public enum ConnectedBodyCollision { Enabled, Disabled }

/// <summary>Immutable body-local declaration, persistent across world steps.
/// Fresh equations are rebuilt after pose changes.</summary>
public abstract class PhysicsJoint : IPositionConstraint
{
    public PhysicsJointId Id { get; }
    public PhysicsBody A { get; }
    public PhysicsBody B { get; }
    public ConnectedBodyCollision Collision { get; }
    protected PhysicsJoint(PhysicsJointId id,PhysicsBody a,PhysicsBody b,ConnectedBodyCollision collision)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if(a==b||a.Id==b.Id||!Enum.IsDefined(collision)||
            (a.MotionType!=PhysicsMotionType.Dynamic&&b.MotionType!=PhysicsMotionType.Dynamic))
            throw new ArgumentException("Joint requires distinct bodies, a dynamic participant and an explicit collision policy.");
        Id=id; A=a; B=b; Collision=collision;
    }
    public abstract IReadOnlyList<IImpulseConstraint> VelocityConstraints(double duration);
    public abstract double Error(double queryTolerance);
    public abstract void Project(double tolerance,PositionProjector projector);

    protected void ProjectEquations(JointEquation[] equations,PositionProjector projector)
    {
        if(equations.Length==0) return;
        var jacobians=equations.Select(e=>e.Jacobian).ToArray();
        var matrix=new ConstraintMassMatrix(A,B,jacobians,new double[equations.Length]);
        var rhs=equations.Select(e=>-e.Error).ToArray(); var result=new double[equations.Length];
        matrix.Solve(rhs,result);
        CollisionVector la=default,aa=default,lb=default,ab=default;
        for(var i=0;i<equations.Length;i++)
        {
            var j=equations[i].Jacobian;
            la+=j.LinearA*result[i]; aa+=j.AngularA*result[i];
            lb+=j.LinearB*result[i]; ab+=j.AngularB*result[i];
        }
        projector.Apply([new(A,la*A.InverseMass,A.InverseInertia(aa)),
            new(B,lb*B.InverseMass,B.InverseInertia(ab))]);
    }
}

public sealed class PhysicsFrameJoint : PhysicsJoint
{
    public FrameJointKind Kind { get; }
    public JointFrame LocalA { get; }
    public JointFrame LocalB { get; }
    public JointFrame FrameA=>World(A,LocalA);
    public JointFrame FrameB=>World(B,LocalB);
    public PhysicsFrameJoint(PhysicsJointId id,FrameJointKind kind,PhysicsBody a,JointFrame localA,
        PhysicsBody b,JointFrame localB,ConnectedBodyCollision collision):base(id,a,b,collision)
    {
        if(!Enum.IsDefined(kind)||!localA.Orientation.IsValid||!localB.Orientation.IsValid)
            throw new ArgumentException("Joint kind and local frames must be valid.");
        Kind=kind; LocalA=localA; LocalB=localB;
    }
    private static JointFrame World(PhysicsBody body,JointFrame frame)=>
        new(body.Pose.TransformPoint(frame.Anchor),body.Pose.Rotation*frame.Orientation);
    private JointEquation[] Equations()=>JointEquations.Frames(Kind,A,B,FrameA,FrameB);
    public override IReadOnlyList<IImpulseConstraint> VelocityConstraints(double duration)
    {
        if(!double.IsFinite(duration)||duration<=0) throw new ArgumentOutOfRangeException(nameof(duration));
        return Kind switch
        {
            FrameJointKind.BallSocket=>JointConstraints.BallSocket(A,B,FrameA,FrameB),
            FrameJointKind.Hinge=>JointConstraints.Hinge(A,B,FrameA,FrameB),
            FrameJointKind.Slider=>JointConstraints.Slider(A,B,FrameA,FrameB),
            _=>throw new InvalidOperationException("Undefined frame joint.")
        };
    }
    public override double Error(double queryTolerance)=>Equations().Max(e=>Math.Abs(e.Error));
    public override void Project(double tolerance,PositionProjector projector)
    {
        if(!double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        ProjectEquations(Equations(),projector);
    }
}

public sealed class PhysicsRopeJoint : PhysicsJoint
{
    public CollisionVector LocalA { get; }
    public CollisionVector LocalB { get; }
    public double MaximumLength { get; }
    public CollisionVector PointA=>A.Pose.TransformPoint(LocalA);
    public CollisionVector PointB=>B.Pose.TransformPoint(LocalB);
    public PhysicsRopeJoint(PhysicsJointId id,PhysicsBody a,CollisionVector localA,PhysicsBody b,CollisionVector localB,
        double maximumLength,ConnectedBodyCollision collision):base(id,a,b,collision)
    {
        if(!localA.IsFinite||!localB.IsFinite||!double.IsFinite(maximumLength)||maximumLength<=0)
            throw new ArgumentException("Rope requires finite anchors and a positive length.");
        LocalA=localA; LocalB=localB; MaximumLength=maximumLength;
    }
    public override IReadOnlyList<IImpulseConstraint> VelocityConstraints(double duration)=>
        [JointConstraints.Rope(A,B,PointA,PointB,MaximumLength,duration)];
    public override double Error(double queryTolerance)=>Math.Max(0,(PointA-PointB).Length-MaximumLength);
    public override void Project(double tolerance,PositionProjector projector)
    {
        if(!double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        if(Error(tolerance)<=tolerance) return; // A slack rope has no positional equality.
        ProjectEquations([JointEquations.Rope(A,B,PointA,PointB,MaximumLength)],projector);
    }
}
