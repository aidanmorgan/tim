#if PLAYTEST
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralConstraintProbe { SlidingFriction, MovingSlider, OffsetJoint }
internal sealed record GeneralConstraintReport(GeneralConstraintProbe Probe,double[] Linear,double[] Angular,
    double[] FrictionImpulse,int Iterations,double Residual);
internal static class GeneralConstraintQualification
{
    private static double[] Values(CollisionVector value)=>[value.X,value.Y,value.Z];
    internal static GeneralConstraintReport Run(GeneralConstraintProbe probe)
    {
        var x=new CollisionVector(1,0,0); var y=new CollisionVector(0,1,0); var z=new CollisionVector(0,0,1);
        var inertia=new InertiaTensor(1,1,1);
        var ground=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        PhysicsBody body;
        ImpulseSolveResult result;
        CollisionVector friction=default;
        switch(probe)
        {
            case GeneralConstraintProbe.SlidingFriction:
                body=new(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,new(2.4,-2,3.2),default,1,inertia);
                var contact=new ContactConstraint(ContactKinematics.AtPoint(body,ground,default,y),0,0,.5);
                result=ImpulseSolver.Solve([contact]); friction=contact.TangentImpulse;
                break;
            case GeneralConstraintProbe.MovingSlider:
                body=new(new(0),PhysicsMotionType.Dynamic,RigidPose.At(z*3),default,default,1,inertia);
                var rail=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,default,y);
                result=ImpulseSolver.Solve(JointConstraints.Slider(body,rail,
                    new(z*3,RigidRotation.Identity),new(default,RigidRotation.Identity)));
                break;
            case GeneralConstraintProbe.OffsetJoint:
                body=new(new(0),PhysicsMotionType.Dynamic,RigidPose.At(z*10),new(3,2,1),new(2,3,4),1,inertia);
                var frame=new JointFrame(default,RigidRotation.Identity);
                result=ImpulseSolver.Solve(JointConstraints.Slider(body,ground,frame,frame));
                break;
            default: throw new System.ArgumentOutOfRangeException(nameof(probe));
        }
        return new(probe,Values(body.LinearVelocity),Values(body.AngularVelocity),Values(friction),result.Iterations,result.MaximumResidual);
    }
}
#endif
