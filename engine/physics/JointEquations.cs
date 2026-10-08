using System;

namespace CuriousContraptions.Physics;

public enum FrameJointKind { BallSocket, Hinge, Slider }
public readonly record struct JointEquation(ConstraintJacobian Jacobian,double Error,double ConvectiveAcceleration);

/// <summary>Fresh geometric errors and their constraint directions. World Z
/// in each declared frame is the free hinge/slider axis.</summary>
public static class JointEquations
{
    private enum AxisReference { World, BodyB }
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static JointEquation Linear(PhysicsBody a,PhysicsBody b,JointFrame fa,JointFrame fb,
        CollisionVector axis,AxisReference reference=AxisReference.World)
    {
        var ra=fa.Anchor-a.Center; var rb=fb.Anchor-b.Center;
        var wa=a.AngularVelocity; var wb=b.AngularVelocity;
        var bias=CollisionVector.Dot(CollisionVector.Cross(wa,CollisionVector.Cross(wa,ra))-
            CollisionVector.Cross(wb,CollisionVector.Cross(wb,rb)),axis);
        if(reference==AxisReference.BodyB)
            bias+=2*CollisionVector.Dot(a.PointVelocity(fa.Anchor)-b.PointVelocity(fb.Anchor),CollisionVector.Cross(wb,axis))+
                CollisionVector.Dot(fa.Anchor-fb.Anchor,CollisionVector.Cross(wb,CollisionVector.Cross(wb,axis)));
        return new(new(axis,CollisionVector.Cross(ra,axis),
            -axis,-CollisionVector.Cross((reference==AxisReference.BodyB?fa.Anchor:fb.Anchor)-b.Center,axis)),
            CollisionVector.Dot(fa.Anchor-fb.Anchor,axis),bias);
    }
    private static JointEquation Angular(CollisionVector axis,double error,double bias)=>
        new(new(default,axis,default,-axis),error,bias);

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
                    Angular(u,CollisionVector.Dot(swing,u),CollisionVector.Dot(a.AngularVelocity-b.AngularVelocity,CollisionVector.Cross(a.AngularVelocity,u))),
                    Angular(v,CollisionVector.Dot(swing,v),CollisionVector.Dot(a.AngularVelocity-b.AngularVelocity,CollisionVector.Cross(a.AngularVelocity,v)))];
            case FrameJointKind.Slider:
                var error=(fa.Orientation*fb.Orientation.Inverse()).RotationVector();
                return [Linear(a,b,fa,fb,fb.Orientation.Apply(X),AxisReference.BodyB),
                    Linear(a,b,fa,fb,fb.Orientation.Apply(Y),AxisReference.BodyB),
                    Angular(X,error.X,0),Angular(Y,error.Y,0),Angular(Z,error.Z,0)];
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    /// <summary>Coordinate and exact differential of the frame's free axis.
    /// Hinge twist is the principal angle (-pi, pi]; an antiparallel swing has
    /// no defined twist and is rejected rather than assigning an arbitrary stop.</summary>
    public static JointEquation Travel(FrameJointKind kind,PhysicsBody a,PhysicsBody b,JointFrame fa,JointFrame fb)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if(!fa.Orientation.IsValid||!fb.Orientation.IsValid) throw new ArgumentException("Joint frames must be valid.");
        if(kind==FrameJointKind.Slider)
            return Linear(a,b,fa,fb,fb.Orientation.Apply(Z),AxisReference.BodyB);
        if(kind!=FrameJointKind.Hinge) throw new ArgumentOutOfRangeException(nameof(kind));
        var q=fb.Orientation.Inverse()*fa.Orientation;
        var denominator=q.W*q.W+q.Z*q.Z;
        if(denominator<=1e-24) throw new InvalidOperationException("Antiparallel hinge frames have undefined twist.");
        var gradient=fb.Orientation.Apply(new((q.W*q.Y+q.Z*q.X)/denominator,
            (-q.W*q.X+q.Z*q.Y)/denominator,1));
        var angle=Math.Atan2(2*q.Z*q.W,q.W*q.W-q.Z*q.Z);
        var relative=fb.Orientation.Inverse().Apply(a.AngularVelocity-b.AngularVelocity);
        var spinB=fb.Orientation.Inverse().Apply(b.AngularVelocity);
        var vector=new CollisionVector(q.X,q.Y,q.Z);
        var derivative=(CollisionVector.Cross(relative,vector)+relative*q.W)*.5;
        var dw=-.5*CollisionVector.Dot(relative,vector);
        var relativeDerivative=-CollisionVector.Cross(spinB,relative);
        var second=(CollisionVector.Cross(relativeDerivative,vector)+CollisionVector.Cross(relative,derivative)+
            relativeDerivative*q.W+relative*dw)*.5;
        var ddw=-.5*(CollisionVector.Dot(relativeDerivative,vector)+CollisionVector.Dot(relative,derivative));
        var numerator=q.W*derivative.Z-q.Z*dw;
        var bias=2*((q.W*second.Z-q.Z*ddw)/denominator-
            numerator*2*(q.W*dw+q.Z*derivative.Z)/(denominator*denominator));
        return Angular(gradient,angle,bias);
    }

}
