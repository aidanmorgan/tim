using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsJointTests
{
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,spin,1,new(.1,.1,.1));
    private static PhysicsBody Fixed(int id)=>new(new(id),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new CompoundGeometry([new(new ConvexSphere(.1),Transform3D.Identity)]),new(0,0,0));
    private static PhysicsFrameJoint Joint(FrameJointKind kind,PhysicsBody a,PhysicsBody b,JointFrame? localA=null)=>
        new(new(0),kind,a,localA??Origin,b,Origin,ConnectedBodyCollision.Disabled,null);
    private static void Near(CollisionVector expected,CollisionVector actual,double tolerance=1e-7)=>
        Assert.InRange((expected-actual).Length,0,tolerance);

    [Theory]
    [InlineData(FrameJointKind.BallSocket)]
    [InlineData(FrameJointKind.Hinge)]
    [InlineData(FrameJointKind.Slider)]
    public void WorldProjectsDeclaredFrameErrorsWithoutInjectingVelocity(FrameJointKind kind)
    {
        var body=Body(0,new(.2,.3,0)); var anchor=Fixed(1);
        body.Restore(body.Snapshot() with {Pose=new(body.Center,RigidRotation.FromRotationVector(new(.1,.2,0)))});
        var joint=Joint(kind,body,anchor);
        var world=new PhysicsWorld([Object(body),Object(anchor)],[joint],new(default));
        world.Step(.01);
        Assert.InRange(joint.Error(1e-8),0,1e-7);
        Near(default,body.LinearVelocity); Near(default,body.AngularMomentum);
        Assert.Equal(RigidPose.Identity,anchor.Pose);
    }

    [Fact]
    public void HingePendulumMaintainsItsPivotAndReplaysExactly()
    {
        var body=Body(0,new(1,0,0)); var anchor=Fixed(1);
        var joint=Joint(FrameJointKind.Hinge,body,anchor,new(new(-1,0,0),RigidRotation.Identity));
        var world=new PhysicsWorld([Object(body),Object(anchor)],[joint],new(new(0,-9.8,0)));
        var before=world.Capture();
        for(var i=0;i<240;i++)
        {
            world.Step(1.0/120);
            Assert.InRange(joint.Error(1e-8),0,1e-7);
            Near(default,body.PointVelocity(joint.FrameA.Anchor));
            Assert.InRange(Math.Abs(body.Center.Z),0,1e-8);
        }
        Assert.True(body.Center.Y<-.1);
        var after=world.Capture();
        var energy=.5*body.LinearVelocity.LengthSquared+.05*body.AngularVelocity.LengthSquared+9.8*body.Center.Y;
        Assert.True(energy<=1e-4);
        world.Restore(before);
        for(var i=0;i<240;i++) world.Step(1.0/120);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.Time,world.Time);
    }

    [Fact]
    public void SliderAllowsOnlyItsDeclaredTravelAxis()
    {
        var body=Body(0,default,new(2,3,4),new(1,2,3)); var anchor=Fixed(1);
        var joint=Joint(FrameJointKind.Slider,body,anchor);
        var world=new PhysicsWorld([Object(body),Object(anchor)],[joint],new(new(1,2,3)));
        for(var i=0;i<120;i++) world.Step(1.0/120);
        Near(new(0,0,7),body.LinearVelocity);
        Near(default,body.AngularVelocity);
        Assert.InRange(joint.Error(1e-8),0,1e-7);
        Assert.True(body.Center.Z>5);
    }

    [Fact]
    public void BallSocketLocksTranslationButLeavesThreeRotationalDegrees()
    {
        var body=Body(0,default,new(1,2,3),new(2,3,4)); var anchor=Fixed(1);
        var joint=Joint(FrameJointKind.BallSocket,body,anchor);
        var world=new PhysicsWorld([Object(body),Object(anchor)],[joint],new(new(0,-9.8,0)));
        world.Step(.1);
        Near(default,body.Center); Near(default,body.LinearVelocity); Near(new(2,3,4),body.AngularVelocity);
    }

    [Theory]
    [InlineData(.5,.1,.1)]
    [InlineData(1,2,0)]
    public void RopeLeavesSlackFreeButStopsOutwardMotionAtFullExtension(double length,double speed,double expected)
    {
        var body=Body(0,new(length,0,0),new(speed,0,0)); var anchor=Fixed(1);
        var rope=new PhysicsRopeJoint(new(0),body,default,anchor,default,1,ConnectedBodyCollision.Disabled);
        var world=new PhysicsWorld([Object(body),Object(anchor)],[rope],new(default));
        world.Step(.01);
        Near(new(expected,0,0),body.LinearVelocity);
        Assert.InRange(rope.Error(1e-8),0,1e-7);
    }

    [Fact]
    public void OverextendedRopeUsesMassWeightedProjectionAndNeverPushes()
    {
        var a=Body(0,new(2,0,0)); var b=Body(1,default);
        var rope=new PhysicsRopeJoint(new(0),a,default,b,default,1,ConnectedBodyCollision.Enabled);
        PositionSolver.Solve([rope],new([a,b],[],1e-6),1e-7);
        Near(new(1.5,0,0),a.Center); Near(new(.5,0,0),b.Center);
        Near(default,a.LinearVelocity); Near(default,b.LinearVelocity);
        a.ApplyImpulse(new(-1,0,0),a.Center);
        ImpulseSolver.Solve(rope.VelocityConstraints(1e-7));
        Near(new(-1,0,0),a.LinearVelocity);
    }

    [Fact]
    public void CollisionPolicyIsExplicitAndCanExposeAnImpossibleConstruction()
    {
        var a=Body(0,default); var b=Fixed(1);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.BallSocket,a,Origin,b,Origin,ConnectedBodyCollision.Enabled,null);
        var world=new PhysicsWorld([Object(a),Object(b)],[joint],new(default));
        var before=a.Snapshot();
        Assert.Throws<InvalidOperationException>(()=>world.Step(.01));
        Assert.Equal(before,a.Snapshot()); Assert.Equal(0,world.Time);
    }

    [Fact]
    public void ImpactAndHingeReactInOneWorldSolve()
    {
        var beam=Body(0,default); var anchor=Fixed(1); var ball=Body(2,new(1,1,0),new(0,-10,0));
        var joint=Joint(FrameJointKind.Hinge,beam,anchor);
        var shape=new CompoundGeometry([new(new ConvexBox(new(2,.1,.1)),Transform3D.Identity)]);
        var world=new PhysicsWorld([new(beam,shape,new(0,0,0)),Object(anchor),Object(ball)],[joint],new(default));
        var result=world.Step(.1);
        Assert.True(result.Events>0);
        Assert.True(beam.AngularVelocity.Z<-.1);
        Assert.True(ball.LinearVelocity.Y>-10);
        Near(default,beam.PointVelocity(joint.FrameA.Anchor));
        Assert.InRange(joint.Error(1e-8),0,1e-7);
        Assert.All(world.Impacts.ToArray(),impact=>Assert.NotEqual(new PhysicsBodyId(1),impact.Pair.B));
    }

    [Fact]
    public void WorldRejectsForeignBodyReferencesAndDuplicateJointIdentities()
    {
        var a=Body(0,default); var b=Fixed(1); var foreign=Body(0,default);
        var joint=Joint(FrameJointKind.Hinge,a,b);
        Assert.Throws<ArgumentException>(()=>new PhysicsWorld([Object(a),Object(b)],[Joint(FrameJointKind.Hinge,foreign,b)],new(default)));
        Assert.Throws<ArgumentException>(()=>new PhysicsWorld([Object(a),Object(b)],[joint,joint],new(default)));
        Assert.Throws<ArgumentException>(()=>new PhysicsFrameJoint(new(0),(FrameJointKind)999,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,null));
        Assert.Throws<ArgumentException>(()=>new PhysicsRopeJoint(new(0),a,default,b,default,0,ConnectedBodyCollision.Disabled));
    }
}
