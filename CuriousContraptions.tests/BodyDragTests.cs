using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class BodyDragTests
{
    private static PhysicsObject Object(PhysicsBody body,ConvexGeometry geometry)=>
        new(body,new([new(geometry,AffineTransform.Identity)]),new(1,0,0));

    [Theory]
    [InlineData(0,0)]
    [InlineData(3,0)]
    [InlineData(0,7)]
    [InlineData(3,7)]
    [InlineData(400,500)]
    public void MidpointDragMatchesDiscreteDecayAndEnergyLoss(double linearRate,double angularRate)
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            new(2,-3,4),new(-1,2,3),2,new(1,1,1));
        var world=new PhysicsWorld([],[Object(body,new ConvexSphere(.1))],[],new(default,maximumStep:.01));
        var drag=new BodyDragLoad(body.Id,linearRate,angularRate);
        world.ReplaceLoads(world.Loads with {Drag=[drag]});var initial=world.Capture();
        void Run()
        {
            for(var i=0;i<30;i++)
            {
                var velocity=body.LinearVelocity;var momentum=body.AngularMomentum;var energy=body.KineticEnergy;
                world.Step([],[],.01);
                var linearFactor=(1-linearRate*.005)/(1+linearRate*.005);
                var angularFactor=(1-angularRate*.005)/(1+angularRate*.005);
                Assert.InRange((body.LinearVelocity-velocity*linearFactor).Length,0,1e-8);
                Assert.InRange((body.AngularMomentum-momentum*angularFactor).Length,0,1e-8);
                var meanVelocity=(velocity+body.LinearVelocity)*.5;
                var meanMomentum=(momentum+body.AngularMomentum)*.5;
                var loss=(linearRate/ body.InverseMass*CollisionVector.Dot(meanVelocity,meanVelocity)+
                    angularRate*CollisionVector.Dot(meanMomentum,meanMomentum))*.01;
                Assert.InRange(Math.Abs(energy-body.KineticEnergy-loss),0,1e-7);
                Assert.InRange(body.KineticEnergy,0,energy+1e-9);
            }
        }
        Run();var final=world.Capture().BodyStates.ToArray();
        world.ReplaceLoads(world.Loads with {Drag=[]});Assert.Empty(world.Loads.Drag.ToArray());
        world.Restore(initial);Assert.Same(drag,Assert.Single(world.Loads.Drag.ToArray()));
        Run();Assert.Equal(final,world.Capture().BodyStates.ToArray());
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(world.Loads with {Drag=[new(new(99),1,1)]}));
        Assert.Same(drag,Assert.Single(world.Loads.Drag.ToArray()));
    }

    [Fact]
    public void CollisionShortenedDragPathLosesEnergyAndReplays()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(-1,0,0)),new(10,0,0),default,1,new(1,1,1));
        var wall=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var world=new PhysicsWorld([],[Object(body,new ConvexSphere(.1)),Object(wall,new ConvexBox(new(.01,2,2)))],[],new(default,maximumStep:.2));
        world.ReplaceLoads(world.Loads with {Drag=[new(body.Id,1,0)]});var initial=world.Capture();
        world.Step([],[],.2);
        Assert.NotEmpty(world.Impacts.ToArray());Assert.True(body.LinearVelocity.X<0);
        Assert.InRange(body.KineticEnergy,0,49.9);
        var final=world.Capture().BodyStates.ToArray();var impacts=world.Impacts.ToArray();
        world.Restore(initial);world.Step([],[],.2);
        Assert.Equal(final,world.Capture().BodyStates.ToArray());Assert.Equal(impacts,world.Impacts.ToArray());
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(world.Loads with {Drag=[new(wall.Id,1,0)]}));
    }

    [Fact]
    public void InvalidRatesAndBodySamplesReject()
    {
        foreach(var value in new[]{-1d,double.NaN,double.PositiveInfinity,double.NegativeInfinity})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>new BodyDragLoad(new(0),value,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new BodyDragLoad(new(0),0,value));
        }
        var drag=new BodyDragLoad(new(0),1,1);
        var foreign=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(1,1,1));
        var fixedBody=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        Assert.Throws<ArgumentException>(()=>drag.Wrench(foreign));
        Assert.Throws<ArgumentException>(()=>drag.Wrench(fixedBody));
        Assert.Throws<ArgumentNullException>(()=>drag.Wrench(null!));
        Assert.Throws<ArgumentException>(()=>drag.Resolve(new Dictionary<PhysicsBodyId,PhysicsBody>()));
    }
}
