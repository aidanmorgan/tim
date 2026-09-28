#if PLAYTEST
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralImpulseProbe { ElasticImpact, CoupledHinge }
internal sealed record GeneralImpulseReport(GeneralImpulseProbe Probe,double LinearA,double LinearB,
    double AngularB,int Iterations,double Residual);
internal static class GeneralImpulseQualification
{
    internal static GeneralImpulseReport Run(GeneralImpulseProbe probe)
    {
        var x=new CollisionVector(1,0,0); var y=new CollisionVector(0,1,0);
        var inertia=new InertiaTensor(1,1,1);
        if(probe==GeneralImpulseProbe.ElasticImpact)
        {
            var a=new ImpulseBody(new(0),PhysicsMotionType.Dynamic,-x,x*3,default,2,inertia);
            var b=new ImpulseBody(new(1),PhysicsMotionType.Dynamic,x,-x,default,3,inertia);
            var solved=ImpulseSolver.Solve([ImpulseConstraint.Contact(a,b,default,-x,1,0)]);
            return new(probe,a.LinearVelocity.X,b.LinearVelocity.X,b.AngularVelocity.Z,solved.Iterations,solved.MaximumResidual);
        }
        if(probe!=GeneralImpulseProbe.CoupledHinge) throw new System.ArgumentOutOfRangeException(nameof(probe));
        var beam=new ImpulseBody(new(0),PhysicsMotionType.Dynamic,x,default,default,2,inertia);
        var anchor=new ImpulseBody(new(1),PhysicsMotionType.Static,default,default,default);
        var ball=new ImpulseBody(new(2),PhysicsMotionType.Dynamic,x*2+y,-y*3,default,1,inertia);
        var pivot=new ImpulseConstraint(beam,anchor,ConstraintJacobian.AtPoint(beam,anchor,default,y),
            0,double.NegativeInfinity,double.PositiveInfinity);
        var result=ImpulseSolver.Solve([pivot,ImpulseConstraint.Contact(ball,beam,x*2,y,0,0)]);
        return new(probe,ball.LinearVelocity.Y,beam.LinearVelocity.Y,beam.AngularVelocity.Z,result.Iterations,result.MaximumResidual);
    }
}
#endif
