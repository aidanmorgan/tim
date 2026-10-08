using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class WorldPlacementTests
{
    private static CompoundGeometry Sphere(double radius=1)=>new([new(new ConvexSphere(radius),AffineTransform.Identity)]);
    private static PhysicsBody Body(int id,CollisionVector position,PhysicsMotionType motion)=>motion==PhysicsMotionType.Dynamic?
        new(new(id),motion,RigidPose.At(position),default,default,1,new(1,1,1)):
        new(new(id),motion,RigidPose.At(position),default,default);
    private static PhysicsObject Object(PhysicsBody body,CompoundGeometry? geometry=null)=>new(body,geometry??Sphere(),new(0,0,0));

    [Theory]
    [InlineData(PhysicsMotionType.Static,PhysicsMotionType.Static,false)]
    [InlineData(PhysicsMotionType.Static,PhysicsMotionType.Kinematic,true)]
    [InlineData(PhysicsMotionType.Static,PhysicsMotionType.Dynamic,true)]
    [InlineData(PhysicsMotionType.Kinematic,PhysicsMotionType.Static,true)]
    [InlineData(PhysicsMotionType.Kinematic,PhysicsMotionType.Kinematic,true)]
    [InlineData(PhysicsMotionType.Kinematic,PhysicsMotionType.Dynamic,true)]
    [InlineData(PhysicsMotionType.Dynamic,PhysicsMotionType.Static,true)]
    [InlineData(PhysicsMotionType.Dynamic,PhysicsMotionType.Kinematic,true)]
    [InlineData(PhysicsMotionType.Dynamic,PhysicsMotionType.Dynamic,true)]
    public void MovingBodiesUseDeterministicTypedPairsWithoutMutatingState(
        PhysicsMotionType first,PhysicsMotionType second,bool expected)
    {
        var a=Body(10,default,first);var b=Body(20,new(1,0,0),second);
        var world=new PhysicsWorld([],[Object(b),Object(a)],[],new(default));
        var before=world.Capture();var result=world.FindPlacementOverlap();
        Assert.Equal(expected,result.HasValue);
        if(result is { } overlap)
        {
            Assert.Equal(new PhysicsBodyId(10),overlap.Pair.A);
            Assert.Equal(new PhysicsBodyId(20),overlap.Pair.B);
            Assert.Equal(new ColliderChildId(0),overlap.Pair.ChildA);
            Assert.Equal(new ColliderChildId(0),overlap.Pair.ChildB);
            Assert.True(overlap.Separation.LowerBound<0);
        }
        Assert.Equal(result,world.FindPlacementOverlap());
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(before.Time,world.Time);
        Assert.Equal(0,world.RetainedContactPairs);
    }

    [Fact]
    public void CurrentGeometryParticipationAndRestoreControlPlacement()
    {
        var a=Body(0,default,PhysicsMotionType.Dynamic);var b=Body(1,new(1.5,0,0),PhysicsMotionType.Static);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[],new(default));
        var before=world.Capture();var overlap=world.FindPlacementOverlap();
        Assert.NotNull(overlap);
        world.ApplyColliderUpdates([new(a.Id,Sphere(.1),new(0,0,0),CollisionParticipation.Enabled)]);
        Assert.Null(world.FindPlacementOverlap());
        world.Restore(before);Assert.Equal(overlap,world.FindPlacementOverlap());
        world.ApplyColliderUpdates([new(a.Id,Sphere(),new(0,0,0),CollisionParticipation.Disabled)]);
        Assert.Null(world.FindPlacementOverlap());
        world.Restore(before);Assert.Equal(overlap,world.FindPlacementOverlap());
    }

    [Fact]
    public void LiveJointSuppressionAndRestoreReplaceConstructionFiltering()
    {
        var a=Body(0,default,PhysicsMotionType.Dynamic);var b=Body(1,default,PhysicsMotionType.Static);
        PhysicsFrameJoint Joint(ConnectedBodyCollision collision)=>new(new(0),FrameJointKind.Hinge,
            a,new(default,RigidRotation.Identity),b,new(default,RigidRotation.Identity),
            collision,null,JointTravelDirection.Both);
        var world=new PhysicsWorld([],[Object(a),Object(b)],[Joint(ConnectedBodyCollision.Enabled)],new(default));
        var before=world.Capture();Assert.NotNull(world.FindPlacementOverlap());
        world.ReplaceJoints([Joint(ConnectedBodyCollision.Disabled)]);
        Assert.Null(world.FindPlacementOverlap());
        world.Restore(before);Assert.NotNull(world.FindPlacementOverlap());
    }

    [Fact]
    public void SharedPrescribedFrameIsOneRigidAssembly()
    {
        var profile=new QuinticRigidTrajectory(RigidPose.Identity,new(1,0,0),default,1);
        PhysicsBody Member(int id,double x)
        {
            var motion=new PrescribedBodyMotion(profile,RigidPose.At(new(x,0,0)),0);
            return new(new(id),PhysicsMotionType.Kinematic,motion.At(0),motion.LinearVelocityAt(0),
                motion.AngularVelocityAt(0),prescribedMotion:motion);
        }
        var world=new PhysicsWorld([],[Object(Member(0,0)),Object(Member(1,.5))],[],new(default));
        Assert.Null(world.FindPlacementOverlap());
    }

    [Theory]
    [InlineData(0,false)]
    [InlineData(.7,true)]
    public void HollowInteriorStaysOpenAndShellOverlapIsRejected(double offset,bool expected)
    {
        var ball=Body(0,new(0,offset,0),PhysicsMotionType.Dynamic);
        var pipe=Body(1,default,PhysicsMotionType.Static);
        var geometry=HollowGeometry.Tube(2,.65,.7,new(.005)).Geometry;
        var world=new PhysicsWorld([],[Object(ball,Sphere(.1)),Object(pipe,geometry)],[],new(default));
        Assert.Equal(expected,world.FindPlacementOverlap().HasValue);
    }

    [Theory]
    [InlineData(2.1,false)]
    [InlineData(2,false)]
    [InlineData(1.9999995,false)]
    [InlineData(1.9999,true)]
    public void WorldPenetrationLimitDistinguishesTouchingFromOverlap(double separation,bool expected)
    {
        var world=new PhysicsWorld([],[Object(Body(0,default,PhysicsMotionType.Dynamic)),
            Object(Body(1,new(separation,0,0),PhysicsMotionType.Static))],[],new(default));
        Assert.Equal(expected,world.FindPlacementOverlap().HasValue);
    }
}
