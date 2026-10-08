using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ConstraintTrajectorySpeedTests(ITestOutputHelper output)
{
    private static readonly CollisionVector X=new(1,0,0),Z=new(0,0,1);
    public enum CapturedTrial { Candidate, Full }
    [Theory]
    [InlineData(CapturedTrial.Candidate)]
    [InlineData(CapturedTrial.Full)]
    public void CapturedComputedTensorMatchesIndependentRationalContraction(CapturedTrial trial)
    {
        const double horizon=3.5896331742143114e-10;
        var b3=new PhysicsBody(new(3),PhysicsMotionType.Dynamic,new(new(1.7725351040520066e-24,3.592058726294183,0.15864166517572206),new(4.328988105647369e-21,2.9634645513134936e-25,1.1342573376599233e-23,1)),
            new(2.1486017101577648e-64,-0.589173292971902,-0.15786851370117821),default,.5,new(0.020479999084472667,0.020479999084472667,0.020479999084472667));
        b3.Restore(b3.Snapshot() with {AngularMomentum=new(-4.591774807899561e-41,0,0)});
        var b6=new PhysicsBody(new(6),PhysicsMotionType.Dynamic,new(new(0.010635014709012638,4.210292895026268,0.3897375970611135),new(0.06840269135870182,-0.0026187519293389473,-0.017220016772327748,0.9975057318008652)),
            new(-0.01839357112387518,-0.5596503503672523,-0.2360026245579483),default,1,new(0.04624000097274781,0.04624000097274781,0.04624000097274781));
        b6.Restore(b6.Snapshot() with {AngularMomentum=new(-0.011388568948773766,-0.0025813810997119143,0.0017056158497484692)});
        CollisionVector a3Linear=trial switch
        {
            CapturedTrial.Candidate=>new(1.844816958329258e-15,48.75297577968597,13.063320956090283),
            CapturedTrial.Full=>new(3.470291002278711e-15,48.75297577962376,13.063320956073614),
            _=>throw new ArgumentOutOfRangeException(nameof(trial))
        };
        CollisionVector a3Angular=trial switch
        {
            CapturedTrial.Candidate=>new(2.8678581668430914e-15,1.2305233571306692e-15,5.20146651801672e-15),
            CapturedTrial.Full=>new(4.3752418821409805e-15,5.550147793493727e-15,-8.136730491393266e-15),
            _=>throw new ArgumentOutOfRangeException(nameof(trial))
        };
        CollisionVector a6Linear=trial switch
        {
            CapturedTrial.Candidate=>new(-16.46446020539785,47.39653757642466,17.418141685486567),
            CapturedTrial.Full=>new(-16.464460204696746,47.39653757747478,17.418141682461957),
            _=>throw new ArgumentOutOfRangeException(nameof(trial))
        };
        CollisionVector a6Angular=trial switch
        {
            CapturedTrial.Candidate=>new(27.321299105436704,44.453696642354906,-120.18107105500569),
            CapturedTrial.Full=>new(27.32129912897261,44.45369664019151,-120.18107105030123),
            _=>throw new ArgumentOutOfRangeException(nameof(trial))
        };
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>
        {
            [b3.Id]=b3.CreateTrajectory(horizon,new(a3Linear/b3.InverseMass,b3.LocalInertia.Rotated(b3.Pose.Rotation).Apply(a3Angular))),
            [b6.Id]=b6.CreateTrajectory(horizon,new(a6Linear/b6.InverseMass,b6.LocalInertia.Rotated(b6.Pose.Rotation).Apply(a6Angular))),
        };
        foreach(var body in new[]{b3,b6})
        {
            var inverse=body.LocalInertia.Inverse().Rotated(paths[body.Id].At(horizon*.5).Rotation);
            var torque=body.LocalInertia.Rotated(body.Pose.Rotation).Apply(body==b3?a3Angular:a6Angular);
            output.WriteLine($"Captured tensor {trial}, body={body.Id.Index}, inverse=({inverse.XX:R},{inverse.YY:R},{inverse.ZZ:R},{inverse.XY:R},{inverse.XZ:R},{inverse.YZ:R}), torque={torque}");
        }
        var u=new ConstraintGradient([
            new(b3,new(0,-0.3501377887464474,0.93669820587623),new(0.32000842019814657,-0.004829986014976738,-0.0018054487692524533)),
            new(b6,new(0,0.3501377887464474,-0.93669820587623),new(0.34000583504328363,-0.005131813179330386,-0.0019182717630912617)),
        ]).SpeedAlong(paths,horizon*.5);
        var v=new ConstraintGradient([
            new(b3,new(-0.999870205884638,0.01509135012205119,0.005641146665793483),new(-3.469446951953614e-18,-0.11206158556278736,0.29979022407181805)),
            new(b6,new(0.999870205884638,-0.01509135012205119,-0.005641146665793483),new(1.5178830414797062e-18,-0.11906434509428723,0.31852419823902495)),
        ]).SpeedAlong(paths,horizon*.5);
        var expected=trial switch
        {
            // Independent exact-rational canonical-map oracle:
            // docs/verification/P0-007/reviewer-rotation-contraction-r1.json
            CapturedTrial.Candidate=>(U:-3.3139234218915685e-9,V:1.55404558799674e-8),
            CapturedTrial.Full=>(U:-3.313923419879324e-9,V:1.5540455880408433e-8),
            _=>throw new ArgumentOutOfRangeException(nameof(trial))
        };
        output.WriteLine($"Captured contact {trial}: U={u:R}, V={v:R}, expectedU={expected.U:R}, expectedV={expected.V:R}");
        Assert.Equal(expected.U,u);
        Assert.Equal(expected.V,v);
    }
    [Fact]
    public void TinyHeldVelocityIncrementSurvivesParticipantCancellation()
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,X,default,1,new(1,1,1));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,X,default,1,new(1,1,1));
        const double time=1e-20;
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>
        {
            [a.Id]=a.CreateTrajectory(time,new(X,default)),
            [b.Id]=b.CreateTrajectory(time,default)
        };
        var gradient=new ConstraintGradient([new(a,X,default),new(b,-X,default)]);
        Assert.Equal(0,paths[a.Id].LinearVelocityAt(time).X-paths[b.Id].LinearVelocityAt(time).X);
        Assert.Equal(time,gradient.SpeedAlong(paths,time));
        a.Restore(a.Snapshot() with {LinearVelocity=X*99});
        Assert.Equal(time,gradient.SpeedAlong(paths,time));
    }
    [Fact]
    public void AnisotropicPhysicalTensorContractionRetainsTinyTorqueIncrement()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,X,Z,1,new(1,2,4));
        const double time=1e-20;
        var path=body.CreateTrajectory(time,new(X*2,Z*4));
        var gradient=new ConstraintGradient([new(body,X,-Z)]);
        Assert.Equal(time,gradient.SpeedAlong(new Dictionary<PhysicsBodyId,BodyTrajectory>{{body.Id,path}},time));
    }
    [Fact]
    public void PrescribedPhysicalDerivativeAndEndpointHoldRemainAuthoritative()
    {
        var profile=new QuinticRigidTrajectory(RigidPose.Identity,X,Z,1);
        var motion=new PrescribedBodyMotion(profile,RigidPose.Identity,0);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,motion.At(0),
            motion.LinearVelocityAt(0),motion.AngularVelocityAt(0),prescribedMotion:motion);
        var path=body.CreateTrajectory(2,default);
        var gradient=new ConstraintGradient([new(body,X,Z)]);
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>{{body.Id,path}};
        Assert.Equal(3.75,gradient.SpeedAlong(paths,.5));
        Assert.Equal(0,gradient.SpeedAlong(paths,2));
        Assert.Throws<ArgumentOutOfRangeException>(()=>gradient.SpeedAlong(paths,double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>gradient.SpeedAlong(paths,-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>gradient.SpeedAlong(paths,2.1));
        Assert.Throws<ArgumentException>(()=>gradient.SpeedAlong(new Dictionary<PhysicsBodyId,BodyTrajectory>(),0));
    }
    private static double SumProducts((double Velocity,double Gradient)[] inputs)
    {
        var bodies=inputs.Select((input,index)=>new PhysicsBody(new(index),PhysicsMotionType.Kinematic,
            RigidPose.Identity,new(input.Velocity,0,0),default)).ToArray();
        var gradient=new ConstraintGradient(bodies.Select((body,index)=>new ConstraintTerm(body,new(inputs[index].Gradient,0,0),default)).ToArray());
        return gradient.SpeedAlong(bodies.ToDictionary(body=>body.Id,body=>body.CreateTrajectory(0,default)),0);
    }
    [Fact]
    public void FormerlyUnderflowedProductResidualSurvivesCancellationAndScaling()
    {
        var value=Math.ScaleB(Math.BitIncrement(1),-511);
        var scale=Math.ScaleB(1,1000);
        Assert.Equal(Math.ScaleB(1,-126),SumProducts([(value*scale,value),(-(value*value)*scale,1)]));
    }
    [Fact]
    public void WarmedTrajectoryContractionDoesNotAllocate()
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,X,default,1,new(1,1,1));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,X,default,1,new(1,1,1));
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>
        {
            [a.Id]=a.CreateTrajectory(.01,new(X,default)),
            [b.Id]=b.CreateTrajectory(.01,default)
        };
        var gradient=new ConstraintGradient([new(a,X,default),new(b,-X,default)]);
        double total=0;
        for(var i=0;i<2048;i++)total+=gradient.SpeedAlong(paths,.005);
        var started=System.Diagnostics.Stopwatch.GetTimestamp();
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<4096;i++)total+=gradient.SpeedAlong(paths,.005);
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        var elapsed=System.Diagnostics.Stopwatch.GetElapsedTime(started);
        Assert.True(total>0);Assert.Equal(0,allocated);
        output.WriteLine($"Native contraction only: 4096 calls, {elapsed.TotalMilliseconds:R} ms, {allocated} bytes; not browser qualification.");
    }

    [Fact]
    public void SubnormalInputsFinalRoundingAndExtremeCancellationPreserveCallerDomain()
    {
        Assert.Equal(double.Epsilon,SumProducts([(double.Epsilon,1)]));
        Assert.Equal(double.Epsilon,SumProducts([(1,double.Epsilon)]));
        Assert.Equal(0,SumProducts([(double.Epsilon,.5)]));
        Assert.Equal(2*double.Epsilon,SumProducts([(double.Epsilon,1.5)]));
        Assert.Equal(double.Epsilon,SumProducts([(double.MaxValue,double.MaxValue),(-double.MaxValue,double.MaxValue),(double.Epsilon,1)]));
        Assert.Equal(1,SumProducts([(1,1),(Math.ScaleB(1,-53),1)]));
        Assert.Equal(Math.BitIncrement(Math.BitIncrement(1)),SumProducts([(Math.BitIncrement(1),1),(Math.ScaleB(1,-53),1)]));
        Assert.Equal(-1,SumProducts([(-1,1),(-Math.ScaleB(1,-53),1)]));
        Assert.Throws<InvalidOperationException>(()=>SumProducts([(double.MaxValue,2)]));
    }

    [Fact]
    public void UnsupportedProductRangeRejectsExplicitly()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.Identity,new(1e-200,0,0),default);
        var path=body.CreateTrajectory(0,default);
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>{{body.Id,path}};
        Assert.Equal(0,new ConstraintGradient([new(body,X*1e-200,default)]).SpeedAlong(paths,0));
        var other=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,X,default);
        Assert.Throws<ArgumentException>(()=>new ConstraintGradient([new(other,X,default)]).SpeedAlong(
            new Dictionary<PhysicsBodyId,BodyTrajectory>{{other.Id,path}},0));
    }
}
