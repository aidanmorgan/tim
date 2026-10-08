using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AlignedPowerPortTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    private static void Near(double expected,double actual,double tolerance=1e-9)=>
        Assert.InRange(actual,expected-tolerance,expected+tolerance);

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(.5)]
    public void SignedAlignmentScalesBothWorkAndBalancedReaction(double alignment)
    {
        var body=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,default,Z*3,1,new(2,2,2));
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),default,Z,1,new(1,1,1));
        var direction=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(4,0,0)),default,default);
        var bodies=new[]{body,frame,direction}.ToDictionary(value=>value.Id);
        var axis=new CollisionVector(Math.Sqrt(1-alignment*alignment),0,alignment);
        var inner=new AngularPowerPort(body.Id,frame.Id,Z,.4);
        var port=new AlignedPowerPort(inner,body.Id,direction.Id,Z,axis);
        var gradient=port.Bind(bodies,[]);
        Near(.8*alignment,gradient.Speed);
        Near(.4*alignment,gradient.Terms.ToArray().Single(term=>term.Body==body).Angular.Z);
        Near(-.4*alignment,gradient.Terms.ToArray().Single(term=>term.Body==frame).Angular.Z);
        Assert.All(gradient.Terms.ToArray(),term=>Assert.Equal(default,term.Linear));
        Assert.Equal(2,gradient.Bodies.Length);
        Assert.Equal(port,new AlignedPowerPort(inner,body.Id,direction.Id,Z,axis));
        bodies.Remove(direction.Id);
        Assert.Throws<ArgumentException>(()=>port.Bind(bodies,[]));
    }

    [Fact]
    public void CapturedThreeBodyAlignmentUsesProductDerivativeAndCurvature()
    {
        PhysicsBody Body(int id,CollisionVector velocity,CollisionVector spin)=>new(new(id),PhysicsMotionType.Dynamic,
            RigidPose.At(new(id*2,0,0)),velocity,spin,1,new(1,2,3));
        var a=Body(0,new(1,2,3),new(1,2,3));
        var b=Body(1,new(0,-1,2),new(.2,.1,0));
        var c=Body(2,default,new(1,-1,.5));
        var bodies=new[]{a,b,c}.ToDictionary(body=>body.Id);
        var paths=bodies.Values.ToDictionary(body=>body.Id,body=>body.CreateTrajectory(.1,
            new(new(.1,0,.2),new(.2,-.1,.3))));
        var inner=new CompositePowerPort([new PointPowerPort(a.Id,b.Id,new(.2,0,0),Z),
            new AngularPowerPort(a.Id,b.Id,Z,.4)]);
        var port=new AlignedPowerPort(inner,a.Id,c.Id,Z,new(1,0,1));
        var path=MechanicalPortSpeedPath.Capture(port,bodies,[],paths);
        Near(port.Bind(bodies,[]).Speed,path.At(0));
        for(double start=0;start<path.Duration;)
        {
            var end=path.SegmentEndAfter(start);var interval=path.Evaluate(start,end);
            var lo=start+(end-start)*.25;var hi=start+(end-start)*.75;var mid=(lo+hi)*.5;
            var sample=bodies.Keys.ToDictionary(id=>id,id=>paths[id].SampleBody(mid));
            Near(port.Bind(sample,[]).Speed,path.At(mid));
            var delta=Math.Min(1e-5,(hi-lo)*.1);
            var finite=(path.At(mid+delta)-path.At(mid-delta))/(2*delta);
            Near(finite,path.Sample(mid).Rate,1e-6);
            Assert.InRange(path.Sample(hi).Rate-path.Sample(lo).Rate,
                -interval.Curvature*(hi-lo)-1e-9,interval.Curvature*(hi-lo)+1e-9);
            start=end;
        }
        paths.Remove(c.Id);
        Assert.Throws<ArgumentException>(()=>MechanicalPortSpeedPath.Capture(port,bodies,[],paths));
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.Evaluate(0,.2));
    }

    [Fact]
    public void InvalidAxesAndMissingPortRejectAtDeclaration()
    {
        var inner=new AngularPowerPort(new(1),new(0),Z,1);
        Assert.Throws<ArgumentNullException>(()=>new AlignedPowerPort(null!,new(1),new(0),Z,Z));
        foreach(var axis in new CollisionVector[]{default,new(double.NaN,0,0),new(double.PositiveInfinity,0,0)})
        {
            Assert.Throws<ArgumentException>(()=>new AlignedPowerPort(inner,new(1),new(0),axis,Z));
            Assert.Throws<ArgumentException>(()=>new AlignedPowerPort(inner,new(1),new(0),Z,axis));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(.5)]
    public void StoredSupplyDrivesSignedRotorWithExplicitReactionAndReplay(double alignment)
    {
        const double h=.01;
        var rotor=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(2,2,2));
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,0,-4)),default,default,1,new(1,1,1));
        var nozzle=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(4,0,0)),default,default);
        PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],new[]{frame,rotor,nozzle}.Select(Object).ToArray(),[],new(default,maximumStep:h));
        world.InstallEnergyStores([new(nozzle.Id,10,0)]);world.ChargeEnergyStore(nozzle.Id,10,1);
        var receiver=new AlignedPowerPort(new AngularPowerPort(rotor.Id,frame.Id,Z,.4),
            rotor.Id,nozzle.Id,Z,new(Math.Sqrt(1-alignment*alignment),0,alignment));
        world.ReplaceLoads(new(){Transfers=[new(new(0),
            new(new StoredFlowSource(new(0),nozzle.Id,4,new(100,1000))),receiver,new(1,100),1e-10)]});
        var initial=world.Capture();var result=world.Step([],[],h);
        var arm=.4*alignment;
        var force=4/(1+.5*h*arm*arm*1.5);
        Near(arm*force*h/2,rotor.AngularVelocity.Z);
        Near(-arm*force*h,frame.AngularVelocity.Z);
        Near(0,2*rotor.AngularVelocity.Z+frame.AngularVelocity.Z);
        Near(4*force*h,world.EnergyStore(nozzle.Id).ReleasedEnergy);
        Near(4*force*h-rotor.KineticEnergy-frame.KineticEnergy,
            Assert.Single(world.TransferUse.ToArray()).PairedWork.Dissipated);
        // Alignment converts work; zero conversion does not disable the declared
        // impedance. Its supplied work is dissipated, not stored in a fake rotor.
        if(alignment==0){Assert.Equal(default,rotor.AngularVelocity);Near(.16,world.EnergyStore(nozzle.Id).ReleasedEnergy);}
        var final=world.Capture();world.Restore(initial);
        Assert.Equal(result,world.Step([],[],h));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }

    [Fact]
    public void RotatingAlignmentHasAnalyticalSpeedAndFindsFlowReversal()
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var drive=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.At(new(2,0,0)),Z*2,default);
        var direction=new PhysicsBody(new(2),PhysicsMotionType.Kinematic,RigidPose.At(new(4,0,0)),default,new(0,1,0));
        var bodies=new[]{frame,drive,direction}.ToDictionary(body=>body.Id);
        var paths=bodies.Values.ToDictionary(body=>body.Id,body=>body.CreateTrajectory(2,default));
        var port=new AlignedPowerPort(new PointPowerPort(drive.Id,frame.Id,default,Z),direction.Id,frame.Id,Z,Z);
        var path=MechanicalPortSpeedPath.Capture(port,bodies,[],paths);
        foreach(var time in new[]{0.0,.2,.6,1.0,1.5,2.0})
        {
            Near(2*Math.Cos(time),path.At(time));
            Near(-2*Math.Sin(time),path.Sample(time).Rate);
        }
        var load=new MechanicalTransferLoad(new(0),new(new(0),port,new(100,1000)),
            new AngularPowerPort(drive.Id,frame.Id,Z,1),new(1,100),1e-10);
        var hit=load.Sweep(bodies,[],paths,2,1e-8,new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),1e-7,TransferSweepStage.Acceptance);
        Assert.Equal(ScalarSweepStatus.Boundary,hit.Status);
        Near(Math.PI/2,hit.Time,1e-7);
    }
}
