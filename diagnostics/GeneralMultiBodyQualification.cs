#if PLAYTEST
using System;
using System.Linq;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralMultiBodyProbe { MovingGuide, Shortening, OffsetGuide, ContactCoupling, BilateralChain }
internal sealed record GeneralMultiBodyReport(GeneralMultiBodyProbe Probe,double[] Speeds,double GuideSpin,
    double Energy,double[] Momentum,double AngularMomentum,double Residual,int Iterations,bool ExactRestore,bool ExactReplay);
internal static class GeneralMultiBodyQualification
{
    internal static GeneralMultiBodyReport Run(GeneralMultiBodyProbe probe)
    {
        if(!Enum.IsDefined(probe)) throw new ArgumentOutOfRangeException(nameof(probe));
        var y=new CollisionVector(0,1,0); var z=new CollisionVector(0,0,1);
        var sign=probe is GeneralMultiBodyProbe.Shortening or GeneralMultiBodyProbe.ContactCoupling?-1:1;
        PhysicsBody Body(int id,double speed,CollisionVector center)=>new(new(id),PhysicsMotionType.Dynamic,
            RigidPose.At(center),y*speed,default,1,new(1,1,1));
        var a=Body(0,3*sign,probe==GeneralMultiBodyProbe.BilateralChain?default:new(-1,0,0));
        var b=Body(1,sign,probe==GeneralMultiBodyProbe.BilateralChain?default:
            new(probe==GeneralMultiBodyProbe.OffsetGuide?2:1,0,0));
        var guide=Body(2,0,default);
        var ground=new PhysicsBody(new(3),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var bodies=new[]{a,b,guide,ground};
        IImpulseConstraint[] Rows()
        {
            if(probe==GeneralMultiBodyProbe.BilateralChain)
            {
                var first=new ImpulseConstraint(new([new(a,y,default),new(b,-y,default)]),0,double.NegativeInfinity,double.PositiveInfinity);
                var second=new ImpulseConstraint(new([new(b,y,default),new(guide,-y,default)]),0,double.NegativeInfinity,double.PositiveInfinity);
                return [new BilateralConstraintBlock([first,second])];
            }
            var direction=probe==GeneralMultiBodyProbe.ContactCoupling?-y:y;
            var gradient=new ConstraintGradient([new(a,direction,default),new(guide,-direction,z),
                new(b,direction,default),new(guide,-direction,probe==GeneralMultiBodyProbe.OffsetGuide?-z*2:-z)]);
            var rope=new ImpulseConstraint(gradient,0,double.NegativeInfinity,0);
            return probe==GeneralMultiBodyProbe.ContactCoupling?
                [rope,ImpulseConstraint.Contact(a,ground,a.Center,y,0,0)]:[rope];
        }
        var before=bodies.Select(b=>b.Snapshot()).ToArray();
        var result=ImpulseSolver.Solve(Rows());
        var after=bodies.Select(b=>b.Snapshot()).ToArray();
        var speed=new[]{a.LinearVelocity.Y,b.LinearVelocity.Y,guide.LinearVelocity.Y};
        var spin=guide.AngularVelocity.Z; var energy=bodies.Sum(b=>b.KineticEnergy);
        var momentum=a.LinearVelocity+b.LinearVelocity+guide.LinearVelocity;
        var angular=bodies.Sum(b=>(CollisionVector.Cross(b.Center,b.LinearVelocity)+b.AngularMomentum).Z);
        for(var i=0;i<bodies.Length;i++) bodies[i].Restore(before[i]);
        var restored=bodies.Select(b=>b.Snapshot()).SequenceEqual(before);
        var replay=ImpulseSolver.Solve(Rows())==result&&bodies.Select(b=>b.Snapshot()).SequenceEqual(after);
        return new(probe,speed,spin,energy,[momentum.X,momentum.Y,momentum.Z],angular,
            result.MaximumResidual,result.Iterations,restored,replay);
    }
}
#endif
