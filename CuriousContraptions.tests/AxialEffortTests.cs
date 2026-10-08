using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AxialEffortTests
{
    private static PhysicsObject Object(PhysicsBody body)=>
        new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
    public static TheoryData<FrameJointKind,int,bool> Cases()
    {
        var cases=new TheoryData<FrameJointKind,int,bool>();
        foreach(var kind in new[]{FrameJointKind.Slider,FrameJointKind.Hinge})
        foreach(var sign in new[]{-1,0,1})
        foreach(var rotated in new[]{false,true})cases.Add(kind,sign,rotated);
        return cases;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CoupledEffortBalancesMomentumWorkAndExactReplay(FrameJointKind kind,int sign,bool rotated)
    {
        var orientation=rotated?RigidRotation.FromRotationVector(new(.4,.7,-.3)):RigidRotation.Identity;
        var pose=new RigidPose(default,orientation);var frame=new JointFrame(default,RigidRotation.Identity);
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,pose,default,default,2,new(1,1,1));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,pose,default,default,3,new(2,2,2));
        var joint=new PhysicsFrameJoint(new(0),kind,a,frame,b,frame,ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var load=new ConstantAxialEffortLoad(joint.Id,kind,sign*3);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[joint],new(default,maximumStep:.01));
        world.ReplaceLoads(world.Loads with {Efforts=[load]});
        var initial=world.Capture();
        void Run()
        {
            world.Step([],[],.1);
            var inverse=kind==FrameJointKind.Slider?1.0/2+1.0/3:1.0+1.0/2;
            Assert.InRange(Math.Abs(joint.Travel.Jacobian.Bind(a,b).Speed-load.Effort*inverse*.1),0,1e-8);
            Assert.InRange(Math.Abs(joint.Travel.Error-.5*load.Effort*inverse*.01),0,1e-8);
            Assert.InRange(Math.Abs(a.KineticEnergy+b.KineticEnergy-load.Effort*joint.Travel.Error),0,1e-8);
            Assert.InRange((a.LinearVelocity*2+b.LinearVelocity*3).Length,0,1e-8);
            Assert.InRange((a.AngularMomentum+b.AngularMomentum).Length,0,1e-8);
        }
        Run();var final=world.Capture().BodyStates.ToArray();
        world.ReplaceLoads(world.Loads with {Efforts=[]});Assert.Empty(world.Loads.Efforts.ToArray());
        world.Restore(initial);Assert.Same(load,Assert.Single(world.Loads.Efforts.ToArray()));
        Run();Assert.Equal(final,world.Capture().BodyStates.ToArray());
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([]));
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(world.Loads with {Efforts=[new ConstantAxialEffortLoad(new(99),kind,1)]}));
        Assert.Throws<ArgumentNullException>(()=>world.ReplaceLoads(world.Loads with {Efforts=[null!]}));
        Assert.Same(load,Assert.Single(world.Loads.Efforts.ToArray()));
    }

    [Theory]
    [InlineData(FrameJointKind.Hinge)]
    [InlineData(FrameJointKind.Slider)]
    public void SameIdentityReplacementUsesCurrentJointAxis(FrameJointKind kind)
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(1,1,1));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        PhysicsFrameJoint Joint(FrameJointKind dimension,RigidRotation orientation)
        {
            var frame=new JointFrame(default,orientation);
            return new(new(0),dimension,a,frame,b,frame,ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        }
        var original=Joint(kind,RigidRotation.Identity);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[original],new(default));
        world.ReplaceLoads(world.Loads with {Efforts=[new ConstantAxialEffortLoad(original.Id,kind,2)]});
        var initial=world.Capture();
        var wrong=kind==FrameJointKind.Slider?FrameJointKind.Hinge:FrameJointKind.Slider;
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([Joint(wrong,RigidRotation.Identity)]));
        Assert.Same(original,Assert.Single(world.Joints.ToArray()));
        world.ReplaceJoints([Joint(kind,RigidRotation.FromRotationVector(new(0,Math.PI/2,0)))]);
        world.Step([],[],.1);
        var velocity=kind==FrameJointKind.Slider?a.LinearVelocity:a.AngularVelocity;
        Assert.InRange((velocity-new CollisionVector(.2,0,0)).Length,0,1e-8);
        world.Restore(initial);
        world.Step([],[],.1);
        velocity=kind==FrameJointKind.Slider?a.LinearVelocity:a.AngularVelocity;
        Assert.InRange((velocity-new CollisionVector(0,0,.2)).Length,0,1e-8);
    }

    [Fact]
    public void UnsupportedKindsAndNonfiniteEffortsReject()
    {
        foreach(var kind in new[]{FrameJointKind.BallSocket,(FrameJointKind)999})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new ConstantAxialEffortLoad(new(0),kind,1));
        foreach(var value in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new ConstantAxialEffortLoad(new(0),FrameJointKind.Hinge,value));
        var load=new ConstantAxialEffortLoad(new(0),FrameJointKind.Hinge,-2);
        Assert.Equal(-2,load.Effort);
        Assert.Throws<ArgumentException>(()=>load.Resolve([]));
        Assert.Throws<ArgumentNullException>(()=>load.Resolve(null!));
    }
}
