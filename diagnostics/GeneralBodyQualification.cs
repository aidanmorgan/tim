#if PLAYTEST
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal sealed record GeneralBodyReport(int Steps,double Duration,double[] Center,bool MomentumExact,
    double MaximumRelativeEnergyError,bool Restored,bool Replayed);
internal static class GeneralBodyQualification
{
    internal static GeneralBodyReport Run()
    {
        const int steps=4800;
        const double delta=1.0/480;
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            new(.2,.3,.4),new(.7,1.1,1.6),2,new InertiaTensor(1,2,3));
        var initial=body.Snapshot(); var energy=body.KineticEnergy;
        var exact=true; double error=0;
        for(var i=0;i<steps;i++)
        {
            body.Advance(body.CreateTrajectory(delta),delta);
            exact&=body.AngularMomentum==initial.AngularMomentum;
            error=System.Math.Max(error,System.Math.Abs(body.KineticEnergy-energy)/energy);
        }
        var end=body.Snapshot();
        body.Restore(initial);
        var restored=body.Snapshot()==initial;
        for(var i=0;i<steps;i++) body.Advance(body.CreateTrajectory(delta),delta);
        return new(steps,steps*delta,[body.Center.X,body.Center.Y,body.Center.Z],exact,error,
            restored,body.Snapshot()==end);
    }
}
#endif
