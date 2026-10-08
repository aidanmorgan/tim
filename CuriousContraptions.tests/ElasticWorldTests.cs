using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ElasticWorldTests
{
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsObject Object(PhysicsBody body,ConvexGeometry geometry)=>
        new(body,new([new(geometry,AffineTransform.Identity)]),new(1,0,0));

    [Theory]
    [InlineData(-1,.01)]
    [InlineData(1,.01)]
    [InlineData(1,.002)]
    public void SharedOscillatorPreservesEnergyAndRestoresItsLoad(int sign,double step)
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,0,.2*sign)),default,default,2,new(1,1,1));
        var anchor=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,body,Origin,anchor,Origin,ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var law=new AxialElasticPotential(160,0);
        var load=new AxialElasticLoad(joint.Id,joint.Kind,law);
        var world=new PhysicsWorld([],[Object(body,new ConvexSphere(.01)),Object(anchor,new ConvexSphere(.01))],[joint],new(default,maximumStep:step));
        world.ReplaceLoads(world.Loads with {Elastic=[load]});
        var initial=world.Capture();var energy=law.Energy(joint.Travel.Error);
        void Run()
        {
            for(var i=0;i<100;i++)
            {
                world.Step([],[],step);
                Assert.InRange(Math.Abs(body.KineticEnergy+law.Energy(joint.Travel.Error)-energy),0,1e-8);
            }
        }
        Run();var final=world.Capture().BodyStates.ToArray();
        world.ReplaceLoads(world.Loads with {Elastic=[]});
        Assert.Empty(world.Loads.Elastic.ToArray());
        world.Restore(initial);
        Assert.Same(load,Assert.Single(world.Loads.Elastic.ToArray()));
        Run();Assert.Equal(final,world.Capture().BodyStates.ToArray());
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([]));
        Assert.Same(load,Assert.Single(world.Loads.Elastic.ToArray()));
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(world.Loads with {Elastic=[new(new(99),FrameJointKind.Slider,law)]}));
        Assert.Same(load,Assert.Single(world.Loads.Elastic.ToArray()));
    }

    [Fact]
    public void CollisionTruncationRecomputesElasticWorkAndReplaysExactly()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,0,.2)),new(0,0,-4),default,1,new(1,1,1));
        var anchor=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var wall=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,body,Origin,anchor,Origin,ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var law=new AxialElasticPotential(10,.2);
        var world=new PhysicsWorld([],[Object(body,new ConvexSphere(.01)),Object(anchor,new ConvexSphere(.001)),Object(wall,new ConvexBox(new(1,1,.01)))],
            [joint],new(default,maximumStep:.08));
        world.ReplaceLoads(world.Loads with {Elastic=[new(joint.Id,joint.Kind,law)]});
        var initial=world.Capture();
        world.Step([],[],.08);
        Assert.NotEmpty(world.Impacts.ToArray());Assert.True(body.LinearVelocity.Z>0);
        Assert.InRange(Math.Abs(body.KineticEnergy+law.Energy(joint.Travel.Error)-8),0,1e-7);
        var final=world.Capture().BodyStates.ToArray();var impacts=world.Impacts.ToArray();
        world.Restore(initial);world.Step([],[],.08);
        Assert.Equal(final,world.Capture().BodyStates.ToArray());Assert.Equal(impacts,world.Impacts.ToArray());
    }

    [Fact]
    public void UndefinedAndNonAxialBindingsReject()
    {
        var law=new AxialElasticPotential(1,0);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AxialElasticLoad(new(0),FrameJointKind.BallSocket,law));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new AxialElasticLoad(new(0),(FrameJointKind)999,law));
        Assert.Throws<ArgumentNullException>(()=>new AxialElasticLoad(new(0),FrameJointKind.Slider,null!));
    }
}
