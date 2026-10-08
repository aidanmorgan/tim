using System.Text.Json;
using CuriousContraptions.Physics;
using CuriousContraptions.Geometry;
enum Check { SubnormalVelocity, SubnormalGradient, NegativeZeroTie, OddSubnormalTie, ExtremeCancellation, FinalOverflow, HeldIncrement, CapturedLifetime, AnisotropicIncrement, PrescribedDerivative, PrescribedHold, NegativeTime, BeyondTime, NonfiniteTime, MissingParticipant, WrongParticipant }
static class Program
{
    static readonly CollisionVector X=new(1,0,0),Z=new(0,0,1);
    static int failed;
    static void Equal(Check check,double expected,Func<double> action)
    {
        try {var actual=action();var pass=BitConverter.DoubleToUInt64Bits(actual)==BitConverter.DoubleToUInt64Bits(expected);if(!pass)failed++;Console.WriteLine(JsonSerializer.Serialize(new{Check=check,Pass=pass,ExpectedBits=BitConverter.DoubleToUInt64Bits(expected),ActualBits=BitConverter.DoubleToUInt64Bits(actual)}));}
        catch(Exception e){failed++;Console.WriteLine(JsonSerializer.Serialize(new{Check=check,Pass=false,Exception=e.GetType().FullName}));}
    }
    static void Reject<T>(Check check,Func<double> action) where T:Exception
    {
        try {action();failed++;Console.WriteLine(JsonSerializer.Serialize(new{Check=check,Pass=false}));}
        catch(T){Console.WriteLine(JsonSerializer.Serialize(new{Check=check,Pass=true}));}
    }
    static double Products(params (double Velocity,double Gradient)[] pairs)
    {
        var bodies=pairs.Select((p,i)=>new PhysicsBody(new(i),PhysicsMotionType.Kinematic,RigidPose.Identity,new(p.Velocity,0,0),default)).ToArray();
        var gradient=new ConstraintGradient(bodies.Select((b,i)=>new ConstraintTerm(b,new(pairs[i].Gradient,0,0),default)).ToArray());
        return gradient.SpeedAlong(bodies.ToDictionary(b=>b.Id,b=>b.CreateTrajectory(0,default)),0);
    }
    static int Main()
    {
        Equal(Check.SubnormalVelocity,double.Epsilon,()=>Products((double.Epsilon,1)));
        Equal(Check.SubnormalGradient,double.Epsilon,()=>Products((1,double.Epsilon)));
        Equal(Check.NegativeZeroTie,-0.0,()=>Products((-double.Epsilon,.5)));
        Equal(Check.OddSubnormalTie,4*double.Epsilon,()=>Products((double.Epsilon,3.5)));
        Equal(Check.ExtremeCancellation,double.Epsilon,()=>Products((double.MaxValue,double.MaxValue),(-double.MaxValue,double.MaxValue),(double.Epsilon,1)));
        Reject<InvalidOperationException>(Check.FinalOverflow,()=>Products((double.MaxValue,2)));
        var a=new PhysicsBody(new(10),PhysicsMotionType.Dynamic,RigidPose.Identity,X,default,1,new(1,1,1));
        var b=new PhysicsBody(new(11),PhysicsMotionType.Dynamic,RigidPose.Identity,X,default,1,new(1,1,1));
        const double time=1e-20;
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>{{a.Id,a.CreateTrajectory(time,new(X,default))},{b.Id,b.CreateTrajectory(time,default)}};
        var gradient=new ConstraintGradient([new(a,X,default),new(b,-X,default)]);
        Equal(Check.HeldIncrement,time,()=>gradient.SpeedAlong(paths,time));
        a.Restore(a.Snapshot() with {LinearVelocity=X*99});
        Equal(Check.CapturedLifetime,time,()=>gradient.SpeedAlong(paths,time));
        var an=new PhysicsBody(new(12),PhysicsMotionType.Dynamic,RigidPose.Identity,X,Z,1,new(1,2,4));
        Equal(Check.AnisotropicIncrement,time,()=>new ConstraintGradient([new(an,X,-Z)]).SpeedAlong(new Dictionary<PhysicsBodyId,BodyTrajectory>{{an.Id,an.CreateTrajectory(time,new(X*2,Z*4))}},time));
        var motion=new PrescribedBodyMotion(new QuinticRigidTrajectory(RigidPose.Identity,X,Z,1),RigidPose.Identity,0);
        var k=new PhysicsBody(new(13),PhysicsMotionType.Kinematic,motion.At(0),motion.LinearVelocityAt(0),motion.AngularVelocityAt(0),prescribedMotion:motion);
        var kp=new Dictionary<PhysicsBodyId,BodyTrajectory>{{k.Id,k.CreateTrajectory(2,default)}};
        var kg=new ConstraintGradient([new(k,X,Z)]);
        Equal(Check.PrescribedDerivative,3.75,()=>kg.SpeedAlong(kp,.5));
        Equal(Check.PrescribedHold,0,()=>kg.SpeedAlong(kp,2));
        Reject<ArgumentOutOfRangeException>(Check.NegativeTime,()=>kg.SpeedAlong(kp,-1));
        Reject<ArgumentOutOfRangeException>(Check.BeyondTime,()=>kg.SpeedAlong(kp,2.1));
        Reject<ArgumentOutOfRangeException>(Check.NonfiniteTime,()=>kg.SpeedAlong(kp,double.NaN));
        Reject<ArgumentException>(Check.MissingParticipant,()=>kg.SpeedAlong(new Dictionary<PhysicsBodyId,BodyTrajectory>(),0));
        Reject<ArgumentException>(Check.WrongParticipant,()=>kg.SpeedAlong(new Dictionary<PhysicsBodyId,BodyTrajectory>{{k.Id,paths[b.Id]}},0));
        return failed==0?0:1;
    }
}
