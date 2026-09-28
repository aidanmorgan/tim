using System;

namespace CuriousContraptions.Physics;

public enum FrameJointKind { BallSocket, Hinge, Slider }
public readonly record struct JointEquation(ConstraintJacobian Jacobian,double Error);

/// <summary>Fresh geometric errors and their constraint directions. World Z
/// in each declared frame is the free hinge/slider axis.</summary>
public static class JointEquations
{
    private enum AxisReference { World, BodyB }
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static JointEquation Linear(PhysicsBody a,PhysicsBody b,JointFrame fa,JointFrame fb,
        CollisionVector axis,AxisReference reference=AxisReference.World)=>new(
            new(axis,CollisionVector.Cross(fa.Anchor-a.Center,axis),
                -axis,-CollisionVector.Cross((reference==AxisReference.BodyB?fa.Anchor:fb.Anchor)-b.Center,axis)),
            CollisionVector.Dot(fa.Anchor-fb.Anchor,axis));
    private static JointEquation Angular(CollisionVector axis,double error)=>new(new(default,axis,default,-axis),error);

    public static JointEquation[] Frames(FrameJointKind kind,PhysicsBody a,PhysicsBody b,JointFrame fa,JointFrame fb)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if(!Enum.IsDefined(kind)||!fa.Orientation.IsValid||!fb.Orientation.IsValid)
            throw new ArgumentException("Joint kind and frames must be valid.");
        switch(kind)
        {
            case FrameJointKind.BallSocket:
                return [Linear(a,b,fa,fb,X),Linear(a,b,fa,fb,Y),Linear(a,b,fa,fb,Z)];
            case FrameJointKind.Hinge:
                var axisA=fa.Orientation.Apply(Z); var axisB=fb.Orientation.Apply(Z);
                var cross=CollisionVector.Cross(axisB,axisA); var sine=cross.Length;
                var cosine=Math.Clamp(CollisionVector.Dot(axisA,axisB),-1,1);
                // The declared X axis resolves the exactly antiparallel swing.
                var swing=sine>1e-14?cross*(Math.Atan2(sine,cosine)/sine):
                    cosine<0?fa.Orientation.Apply(X)*Math.PI:default;
                var u=fa.Orientation.Apply(X); var v=fa.Orientation.Apply(Y);
                return [Linear(a,b,fa,fb,X),Linear(a,b,fa,fb,Y),Linear(a,b,fa,fb,Z),
                    Angular(u,CollisionVector.Dot(swing,u)),Angular(v,CollisionVector.Dot(swing,v))];
            case FrameJointKind.Slider:
                var error=(fa.Orientation*fb.Orientation.Inverse()).RotationVector();
                return [Linear(a,b,fa,fb,fb.Orientation.Apply(X),AxisReference.BodyB),
                    Linear(a,b,fa,fb,fb.Orientation.Apply(Y),AxisReference.BodyB),
                    Angular(X,error.X),Angular(Y,error.Y),Angular(Z,error.Z)];
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public static JointEquation Rope(PhysicsBody a,PhysicsBody b,CollisionVector pointA,CollisionVector pointB,double length)
    {
        if(!pointA.IsFinite||!pointB.IsFinite||!double.IsFinite(length)||length<=0)
            throw new ArgumentException("Rope geometry must be finite with positive length.");
        var delta=pointA-pointB; var distance=delta.Length;
        if(!double.IsFinite(distance)||distance==0) throw new ArgumentException("Coincident rope endpoints have no defined gradient.");
        var axis=delta/distance;
        return new(new(axis,CollisionVector.Cross(pointA-a.Center,axis),
            -axis,-CollisionVector.Cross(pointB-b.Center,axis)),distance-length);
    }
}
