using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsBodyOwnershipTests
{
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new([new(new ConvexSphere(.1),AffineTransform.Identity)]),new(0,0,0));
    private static PhysicsBody Body(int id,PhysicsMotionType motion)=>new(new(id),motion,
        RigidPose.At(new(id*2,0,0)),default,default,motion==PhysicsMotionType.Dynamic?1:0,
        motion==PhysicsMotionType.Dynamic?new(1,1,1):default);

    [Theory]
    [InlineData(PhysicsMotionType.Dynamic)]
    [InlineData(PhysicsMotionType.Kinematic)]
    [InlineData(PhysicsMotionType.Static)]
    public void LiveBodiesRejectDirectMutationWithoutChangingWorldState(PhysicsMotionType motion)
    {
        var body=Body(0,motion);
        var world=new PhysicsWorld([],[Object(body)],[],new(default));
        var initial=world.Capture();
        var state=body.Snapshot();var path=body.CreateTrajectory(.01,default);
        Assert.Throws<InvalidOperationException>(()=>body.ApplyImpulse(new(1,0,0),body.Center));
        Assert.Throws<InvalidOperationException>(()=>body.ApplyWrench(new(1,0,0),default,.01));
        Assert.Throws<InvalidOperationException>(()=>body.SetKinematicVelocity(new(1,0,0),default));
        Assert.Throws<InvalidOperationException>(()=>body.Advance(path,.01));
        Assert.Throws<InvalidOperationException>(()=>body.Restore(state));
        Assert.Equal(initial.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        if(motion==PhysicsMotionType.Dynamic)world.ApplyImpulse(body.Id,new(1,0,0),body.Center);
        world.Step([],[],.01);
        var final=world.Capture().BodyStates.ToArray();
        world.Restore(initial);
        if(motion==PhysicsMotionType.Dynamic)world.ApplyImpulse(body.Id,new(1,0,0),body.Center);
        world.Step([],[],.01);
        Assert.Equal(final,world.Capture().BodyStates.ToArray());
    }

    [Theory]
    [InlineData(PhysicsMotionType.Dynamic)]
    [InlineData(PhysicsMotionType.Kinematic)]
    [InlineData(PhysicsMotionType.Static)]
    public void AngularImpulseValidatesOwnershipAndPreservesLinearMotion(PhysicsMotionType motion)
    {
        var body=Body(0,motion);
        var world=new PhysicsWorld([],[Object(body)],[],new(default));
        var before=world.Capture();
        Assert.Throws<ArgumentException>(()=>world.ApplyAngularImpulse(new(99),new(0,0,1)));
        if(motion!=PhysicsMotionType.Dynamic)
        {
            Assert.Throws<ArgumentException>(()=>world.ApplyAngularImpulse(body.Id,new(0,0,1)));
            Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
            return;
        }
        Assert.Throws<ArgumentException>(()=>world.ApplyAngularImpulse(body.Id,new(double.NaN,0,0)));
        Assert.Throws<ArgumentException>(()=>world.ApplyAngularImpulse(body.Id,new(0,double.PositiveInfinity,0)));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        var impulse=new CollisionVector(.2,-.3,.4);
        world.ApplyAngularImpulse(body.Id,impulse);
        Assert.Equal(impulse,body.AngularMomentum);
        Assert.Equal(default,body.LinearVelocity);
        Assert.Equal(before.BodyStates[0].Pose,body.Pose);
        world.Step([],[],.01);
        var after=world.Capture();
        world.Restore(before);
        world.ApplyAngularImpulse(body.Id,impulse);
        world.Step([],[],.01);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void FailedWorldClaimDoesNotCaptureOtherParticipants()
    {
        var first=Body(0,PhysicsMotionType.Dynamic);var owned=Body(1,PhysicsMotionType.Dynamic);
        var original=new PhysicsWorld([],[Object(owned)],[],new(default));
        Assert.Throws<InvalidOperationException>(()=>new PhysicsWorld([],[Object(first),Object(owned)],[],new(default)));
        first.ApplyImpulse(new(1,0,0),first.Center);
        Assert.Equal(new CollisionVector(1,0,0),first.LinearVelocity);
        var independent=new PhysicsWorld([],[Object(first)],[],new(default));
        independent.Step([],[],.01);original.Step([],[],.01);
        Assert.Equal(0,owned.LinearVelocity.Length);
        Assert.True(first.Center.X>0);
    }
}
