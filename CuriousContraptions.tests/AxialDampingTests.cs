using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AxialDampingTests
{
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsObject Object(PhysicsBody body)=>
        new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(1,0,0));

    public static TheoryData<FrameJointKind,int,bool> Cases()
    {
        var cases=new TheoryData<FrameJointKind,int,bool>();
        foreach(var kind in new[]{FrameJointKind.Slider,FrameJointKind.Hinge})
        foreach(var sign in new[]{-1,1})
        foreach(var damped in new[]{false,true})cases.Add(kind,sign,damped);
        return cases;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CoupledDampingLosesOnlyItsMeasuredWorkAndReplays(FrameJointKind kind,int sign,bool damped)
    {
        var velocity=new CollisionVector(0,0,2*sign);
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            kind==FrameJointKind.Slider?velocity:default,kind==FrameJointKind.Hinge?velocity:default,2,new(1,1,1));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,
            kind==FrameJointKind.Slider?-velocity:default,kind==FrameJointKind.Hinge?-velocity:default,2,new(1,1,1));
        var joint=new PhysicsFrameJoint(new(0),kind,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var load=new AxialDampingLoad(joint.Id,kind,damped?3:0,damped?7:0);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default,maximumStep:.01));
        world.ReplaceLoads(world.Loads with {Damping=[load]});
        var initial=world.Capture();
        double Speed()=>joint.Travel.Jacobian.Bind(a,b).Speed;
        void Run()
        {
            for(var i=0;i<50;i++)
            {
                var before=Speed();var energy=a.KineticEnergy+b.KineticEnergy;
                world.Step([],[],.01);
                var after=Speed();var midpoint=(before+after)*.5;
                var coefficient=midpoint<0?load.NegativeCoefficient:load.PositiveCoefficient;
                var loss=coefficient*midpoint*midpoint*.01;
                Assert.InRange(Math.Abs(energy-a.KineticEnergy-b.KineticEnergy-loss),0,1e-8);
                Assert.InRange(Math.Abs(after),0,Math.Abs(before)+1e-10);
                if(damped)Assert.True(Math.Abs(after)<Math.Abs(before));
                else Assert.Equal(before,after);
                Assert.InRange((a.LinearVelocity+b.LinearVelocity).Length,0,1e-10);
                Assert.InRange((a.AngularMomentum+b.AngularMomentum).Length,0,1e-10);
            }
        }
        Run();var final=world.Capture().BodyStates.ToArray();
        world.ReplaceLoads(world.Loads with {Damping=[]});Assert.Empty(world.Loads.Damping.ToArray());
        world.Restore(initial);Assert.Same(load,Assert.Single(world.Loads.Damping.ToArray()));
        Run();Assert.Equal(final,world.Capture().BodyStates.ToArray());
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([]));
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(world.Loads with {Damping=[new(new(99),kind,1,1)]}));
        Assert.Same(load,Assert.Single(world.Loads.Damping.ToArray()));
    }

    [Fact]
    public void CoefficientsKindsAndSpeedsAreValidated()
    {
        foreach(var value in new[]{-1d,double.NaN,double.PositiveInfinity,double.NegativeInfinity})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>new AxialDampingLoad(new(0),FrameJointKind.Slider,value,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new AxialDampingLoad(new(0),FrameJointKind.Slider,0,value));
        }
        foreach(var kind in new[]{FrameJointKind.BallSocket,(FrameJointKind)999})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new AxialDampingLoad(new(0),kind,1,1));
        var load=new AxialDampingLoad(new(0),FrameJointKind.Slider,3,7);
        Assert.Equal(6,load.Effort(-2));Assert.Equal(-14,load.Effort(2));Assert.Equal(0,load.Effort(0));
        foreach(var speed in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity,double.MaxValue})
            Assert.Throws<ArgumentOutOfRangeException>(()=>load.Effort(speed));
    }
}
