using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class CompositePowerPortTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    private static void Near(double expected,double actual)=>Assert.InRange(actual,expected-1e-9,expected+1e-9);

    [Fact]
    public void DeclarationCopiesFlattensAndComparesTypedComponents()
    {
        var point=new PointPowerPort(new(1),new(0),default,Z);
        var angular=new AngularPowerPort(new(1),new(0),Z,.4);
        MechanicalPowerPort[] input=[point,angular];
        var port=new CompositePowerPort(input);
        input[0]=angular;
        Assert.Equal(point,port.Components[0]);
        var same=new CompositePowerPort([new CompositePowerPort([point]),angular]);
        Assert.Equal(port,same);Assert.Equal(port.GetHashCode(),same.GetHashCode());
        Assert.Throws<ArgumentException>(()=>new CompositePowerPort([]));
        Assert.Throws<ArgumentNullException>(()=>new CompositePowerPort([null!]));
        var list=Assert.IsAssignableFrom<IList<MechanicalPowerPort>>(port.Components);
        Assert.Throws<NotSupportedException>(()=>list[0]=angular);
    }

    [Fact]
    public void ThreeBodyCaptureSumsWorkCoordinatesAndBounds()
    {
        PhysicsBody Body(int id,CollisionVector velocity,CollisionVector spin)=>new(new(id),PhysicsMotionType.Dynamic,
            RigidPose.At(new(id*2,0,0)),velocity,spin,1,new(1,1,1));
        var a=Body(1,new(1,2,3),new(1,2,3));
        var b=Body(0,new(-1,0,2),new(.2,.1,0));
        var c=Body(2,new(0,1,2),new(1,-1,.5));
        var bodies=new[]{a,b,c}.ToDictionary(body=>body.Id);
        var paths=bodies.Values.ToDictionary(body=>body.Id,body=>body.CreateTrajectory(.1,new(new(.1,0,.2),new(.2,-.1,.3))));
        var point=new PointPowerPort(a.Id,b.Id,new(.2,0,0),Z);
        var angular=new AngularPowerPort(a.Id,c.Id,Z,.4);
        var port=new CompositePowerPort([point,angular]);
        var path=MechanicalPortSpeedPath.Capture(port,bodies,[],paths);
        var p=MechanicalPortSpeedPath.Capture(point,bodies,[],paths);
        var q=MechanicalPortSpeedPath.Capture(angular,bodies,[],paths);
        Near(port.Bind(bodies,[]).Speed,path.At(0));
        Assert.Equal(3,port.Bind(bodies,[]).Bodies.Length);
        for(double start=0;start<path.Duration;)
        {
            var end=path.SegmentEndAfter(start);var interval=path.Evaluate(start,end);
            var lo=start+(end-start)*.25;var hi=start+(end-start)*.75;var mid=(lo+hi)*.5;
            Near(p.At(mid)+q.At(mid),path.At(mid));
            var delta=Math.Min(1e-5,(hi-lo)*.1);
            var finite=(path.At(mid+delta)-path.At(mid-delta))/(2*delta);
            Assert.InRange(path.Sample(mid).Rate,finite-1e-6,finite+1e-6);
            Assert.InRange(path.Sample(hi).Rate-path.Sample(lo).Rate,-interval.Curvature*(hi-lo)-1e-9,interval.Curvature*(hi-lo)+1e-9);
            start=end;
        }
        bodies.Remove(c.Id);
        Assert.Throws<ArgumentException>(()=>port.Bind(bodies,[]));
    }

    [Theory]
    [InlineData(.4)]
    [InlineData(-.4)]
    public void OneSupplyDrivesTranslationAndSpinWithBalancedReactions(double pitch)
    {
        const double h=.01;
        var rotor=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(2,2,2));
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,0,-4)),default,default,1,new(1,1,1));
        PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],[Object(frame),Object(rotor)],[],new(default,maximumStep:h));
        world.InstallEnergyStores([new(frame.Id,10,0)]);world.ChargeEnergyStore(frame.Id,10,1);
        var receiver=new CompositePowerPort([new PointPowerPort(rotor.Id,frame.Id,default,Z),
            new AngularPowerPort(rotor.Id,frame.Id,Z,pitch)]);
        world.ReplaceLoads(new(){Transfers=[new(new(0),new(new StoredFlowSource(new(0),frame.Id,4,new(100,1000))),receiver,new(1,100),1e-10)]});
        var initial=world.Capture();var result=world.Step([],[],h);
        var force=4/(1+.5*h*(2+pitch*pitch*1.5));
        Near(force*h,rotor.LinearVelocity.Z);Near(-force*h,frame.LinearVelocity.Z);
        Near(pitch*force*h/2,rotor.AngularVelocity.Z);Near(-pitch*force*h,frame.AngularVelocity.Z);
        Near(0,2*rotor.AngularVelocity.Z+frame.AngularVelocity.Z);
        var work=4*force*h;
        Near(work,world.EnergyStore(frame.Id).ReleasedEnergy);
        Near(work-rotor.KineticEnergy-frame.KineticEnergy,Assert.Single(world.TransferUse.ToArray()).PairedWork.Dissipated);
        Assert.Single(world.SourceUse.ToArray());
        var final=world.Capture();world.Restore(initial);
        Assert.Equal(result,world.Step([],[],h));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
    }
}
