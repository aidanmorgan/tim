using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

/// <summary>Source-flow switching under an opposing receiver requires a stall
/// balance: the active and inactive acceleration fields point toward zero flow.</summary>
public class TransferSourceStallTests
{
    private static readonly CollisionVector X=new(1,0,0);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));

    [Theory]
    [InlineData(0,1,false,0)]
    [InlineData(.25,1,false,0)]
    [InlineData(.25,2,false,0)]
    [InlineData(.25,2,true,0)]
    [InlineData(.25,1,false,.0001)]
    [InlineData(.25,2,true,.0001)]
    [InlineData(.25,1,false,-.0001)]
    [InlineData(.25,2,true,-.0001)]
    [InlineData(.25,1,false,1e-9)]
    [InlineData(.25,1,false,-1e-9)]
    public void OpposingReceiverCannotMakeACompressiveSourceChatter(double drive,int count,bool reverse,double initialFlow)
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(X*2),X*initialFlow,default,1,new(1,1,1));
        var receivers=Enumerable.Range(0,count).Select(i=>new PhysicsBody(new(2+i),PhysicsMotionType.Dynamic,
            RigidPose.At(X*(4+2*i)),-X*.5,default,1,new(1,1,1))).ToArray();
        var objects=new[]{frame,source}.Concat(receivers).Select(Object).ToArray();
        var supply=new MechanicalTransferSource(new(0),new PointPowerPort(source.Id,frame.Id,default,X),new(1,1));
        var transfers=receivers.Select((receiver,i)=>new MechanicalTransferLoad(new(i),supply,
            new PointPowerPort(receiver.Id,frame.Id,default,X),new(1,1),1e-10)).ToArray();
        if(reverse){Array.Reverse(objects);Array.Reverse(transfers);}
        var world=new PhysicsWorld([],objects,[],new(default,maximumStep:.01));
        world.ReplaceLoads(new(){Transfers=transfers});
        var before=world.Capture();
        PhysicsWrenchCommand[] commands=[new(source.Id,X*drive,default)];
        world.Step(commands,[],.01);
        // When drive is below the opposing receiver's demand, the source stalls:
        // reaction balances drive and the receiver loses energy dissipatively.
        Assert.InRange(Math.Abs(source.LinearVelocity.X),0,1e-10);
        if(initialFlow==0)Assert.InRange(Math.Abs(source.Center.X-2),0,1e-12);
        foreach(var receiver in receivers)
            Assert.InRange(Math.Abs(receiver.LinearVelocity.X-(-.5+(initialFlow+drive*.01)/count)),0,1e-10);
        var reports=world.TransferTotals.ToArray();
        Assert.Equal(count,reports.Length);
        var extracted=.5*initialFlow*initialFlow+drive*(source.Center.X-2)-source.KineticEnergy;
        Assert.InRange(Math.Abs(reports.Sum(report=>report.SourceExtraction.Supplied-report.SourceExtraction.Dissipated)-extracted),0,1e-10);
        var loss=count*.125-receivers.Sum(receiver=>receiver.KineticEnergy);
        Assert.InRange(Math.Abs(reports.Sum(report=>report.ReceiverDelivery.Dissipated)-loss),0,1e-10);
        if(initialFlow>0)
        {
            // Independent continuous solution before stall: slip obeys
            // s'=drive-(count+1)*s, and total momentum grows by drive*t.
            // Locate u=0, integrate u until that time, then hold the source.
            var k=count+1.0;var equilibrium=drive/k;
            var initialSlip=initialFlow+.5;
            (double Speed,double Distance) Solution(double time)
            {
                // Evaluate exp(-k*t)-1 and its integral without cancellation
                // at the near-zero entries. Here |k*t| <= .03; 24 terms put
                // truncation far below double precision.
                double term=-k*time,remainder=0,integral=0;
                for(var order=1;order<=24;order++)
                {
                    remainder+=term;integral+=term*time/(order+1);
                    term*=-k*time/(order+1);
                }
                return(initialFlow+drive*time/k+count*(initialSlip-equilibrium)*remainder/k,
                    initialFlow*time+drive*time*time/(2*k)+count*(initialSlip-equilibrium)*integral/k);
            }
            double Speed(double time)=>Solution(time).Speed;
            double lower=0,upper=.01;
            Assert.True(Speed(upper)<0);
            for(var i=0;i<80;i++)
            {
                var middle=(lower+upper)*.5;
                if(Speed(middle)>0)lower=middle;else upper=middle;
            }
            var stop=(lower+upper)*.5;
            var distance=Solution(stop).Distance;
            var error=Math.Abs(source.Center.X-2-distance);
            Console.WriteLine($"Stall trajectory: receivers={count}, stop={stop:R}, expected distance={distance:R}, actual distance={source.Center.X-2:R}, error={error:R}");
            Assert.InRange(error,0,1e-7);
        }
        var after=world.Capture();
        world.Restore(before);world.Step(commands,[],.01);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.TransferTotals.ToArray(),world.TransferTotals.ToArray());
        var velocities=receivers.Select(receiver=>receiver.LinearVelocity).ToArray();
        world.Step([new(source.Id,-X*.25,default)],[],.01);
        Assert.InRange(Math.Abs(source.LinearVelocity.X+.0025),0,1e-10);
        Assert.Equal(velocities,receivers.Select(receiver=>receiver.LinearVelocity).ToArray());
    }

    [Fact]
    public void SourceFlowIsCertifiedAfterTheCoupledIteration()
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(X*2),X,default,1,new(1,1,1));
        var receiver=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(X*4),-X*4,default,1,new(1,1,1));
        var bodies=new[]{frame,source,receiver}.ToDictionary(body=>body.Id);
        var paths=bodies.ToDictionary(pair=>pair.Key,pair=>pair.Value.CreateTrajectory(1,
            pair.Key==source.Id?new BodyWrench(-X*2,default):default));
        var load=new MechanicalTransferLoad(new(0),
            new(new(0),new PointPowerPort(source.Id,frame.Id,default,X),new(100,100)),
            new PointPowerPort(receiver.Id,frame.Id,default,X),new(1,100),1e-10);
        var colliders=new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>();
        var trial=load.Sweep(bodies,[],paths,1,1e-8,colliders,1e-7,TransferSweepStage.Iteration);
        var accepted=load.Sweep(bodies,[],paths,1,1e-8,colliders,1e-7,TransferSweepStage.Acceptance);
        Assert.Equal(ScalarSweepStatus.Clear,trial.Status);
        Assert.Equal(ScalarSweepStatus.Boundary,accepted.Status);
        Assert.InRange(accepted.Time,.5,.50000001);
        Assert.Throws<ArgumentOutOfRangeException>(()=>
            load.Sweep(bodies,[],paths,1,1e-8,colliders,1e-7,(TransferSweepStage)99));
    }

    [Theory]
    [InlineData(1.4)]
    [InlineData(1.5)]
    [InlineData(2)]
    public void HighRatioSourceTransitionsFromStallToFullEngagement(double drive)
    {
        const double mass=.25,ratio=15,conductance=.75,step=.01,opposingSpeed=.133;
        var z=new CollisionVector(0,0,1);
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(X*2),default,default,mass,new(1,1,1));
        var receiver=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(X*4),-z*opposingSpeed,default,1,new(1,1,1));
        var guide=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,source,new(default,RigidRotation.Identity),
            frame,new(X*2,RigidRotation.Identity),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var world=new PhysicsWorld([],[Object(frame),Object(source),Object(receiver)],[guide],new(default,maximumStep:step));
        world.ReplaceLoads(new(){Transfers=[new(new(0),
            new(new(0),new AxialPowerPort(guide.Id,FrameJointKind.Slider,ratio),new(9,108)),
            new PointPowerPort(receiver.Id,frame.Id,default,z),new(conductance,9),1e-10)]});
        var before=world.Capture();
        PhysicsWrenchCommand[] force=[new(source.Id,z*drive,default)];
        world.Step(force,[],step);
        var fullForce=conductance*(opposingSpeed+ratio*drive*step/(2*mass))/
            (1+conductance*(ratio*ratio/mass+1)*step/2);
        var expectedForce=Math.Min(drive/ratio,fullForce);
        Assert.InRange(Math.Abs(source.LinearVelocity.Z-(drive-ratio*expectedForce)*step/mass),0,1e-9);
        Assert.InRange(Math.Abs(receiver.LinearVelocity.Z-(-opposingSpeed+expectedForce*step)),0,1e-9);
        var work=Assert.Single(world.TransferTotals.ToArray());
        var sourceWork=drive*source.Center.Z-source.KineticEnergy;
        Assert.InRange(Math.Abs(work.SourceExtraction.Supplied-work.SourceExtraction.Dissipated-sourceWork),0,1e-10);
        var after=world.Capture();
        world.Restore(before);
        world.Step(force,[],step);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }
}
