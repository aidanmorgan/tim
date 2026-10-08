using System.Runtime.CompilerServices;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

public class RigidPoseTrajectoryTests
{
    private static readonly CollisionVector X=new(1,0,0),Z=new(0,0,1);
    private static void Near(CollisionVector expected,CollisionVector actual)=>Assert.InRange((expected-actual).Length,0,1e-10);

    [Theory]
    [InlineData(PhysicsMotionType.Kinematic)]
    [InlineData(PhysicsMotionType.Dynamic)]
    public void MultiTurnPathRetainsInteriorMotionAfterBodyAdvances(PhysicsMotionType kind)
    {
        var speed=Math.Tau*6;
        var body=kind switch
        {
            PhysicsMotionType.Kinematic=>new PhysicsBody(new(0),kind,RigidPose.Identity,X,Z*speed),
            PhysicsMotionType.Dynamic=>new PhysicsBody(new(0),kind,RigidPose.Identity,X,Z*speed,2,new(2,2,2)),
            _=>throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var trajectory=body.CreateTrajectory(.5,default);
        var read=trajectory.PosePath;
        var middle=read.At(.137);
        Near(X*.137,middle.Center);
        Near(new(Math.Cos(speed*.137),Math.Sin(speed*.137),0),middle.Rotation.Apply(X));
        body.Advance(trajectory,.5);
        Assert.Equal(middle,read.At(.137));
        Assert.Equal(body.Pose,read.At(.5));
        Near(X,read.At(.5).Rotation.Apply(X));
        Assert.True((middle.Rotation.Apply(X)-X).Length>.1);
        Assert.Throws<InvalidOperationException>(()=>body.Advance(trajectory,.1));
    }

    [Fact]
    public void CapturedAccelerationAndPrescribedOffsetRemainAvailable()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(1,2,3)),X,default,2,new(2,2,2));
        var read=body.CreateTrajectory(1,new(new(0,-4,0),default)).PosePath;
        Near(new(1.25,1.9375,3),read.At(.25).Center);
        Near(new(1,-.5,0),read.LinearVelocityAt(.25));
        var profile=new QuinticRigidTrajectory(RigidPose.Identity,X,Z*(Math.Tau*3),1);
        var cursor=new PrescribedBodyMotion(profile,RigidPose.At(X),.2);
        var driver=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,cursor.At(0),cursor.LinearVelocityAt(0),
            cursor.AngularVelocityAt(0),prescribedMotion:cursor);
        var prescribed=driver.CreateTrajectory(1,default).PosePath;
        Assert.Equal(cursor.At(.137),prescribed.At(.137));
        Assert.Equal(cursor.At(1),prescribed.At(1));
        Assert.Equal(.8,prescribed.SegmentEndAfter(0));
        Assert.Equal(1,prescribed.SegmentEndAfter(.8));
        Assert.Equal(2,prescribed.SegmentCount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (RigidPoseTrajectory Read,WeakReference<PhysicsBody> Body,WeakReference<BodyTrajectory> Trajectory) Capture()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.Identity,X,Z*10);
        var trajectory=body.CreateTrajectory(1,default);
        return (trajectory.PosePath,new(body),new(trajectory));
    }
    [Fact]
    public void RetainedReadDoesNotKeepPhysicsBodyOrTrajectoryOwnerAlive()
    {
        var (read,body,trajectory)=Capture();
        GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
        Assert.False(body.TryGetTarget(out _));
        Assert.False(trajectory.TryGetTarget(out _));
        Near(X*.25,read.At(.25).Center);
        Near(new(Math.Cos(2.5),Math.Sin(2.5),0),read.At(.25).Rotation.Apply(X));
        GC.KeepAlive(read);
    }

    [Fact]
    public void InvalidReadsRejectAndRepeatedSamplingDoesNotAllocate()
    {
        Assert.Throws<InvalidOperationException>(()=>default(RigidPoseTrajectory).At(0));
        var (read,_,_)=Capture();
        foreach(var time in new[]{-1,1.1,double.NaN,double.PositiveInfinity})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>read.At(time));
            Assert.Throws<ArgumentOutOfRangeException>(()=>read.LinearVelocityAt(time));
            Assert.Throws<ArgumentOutOfRangeException>(()=>read.AngularVelocityAt(time));
            Assert.Throws<ArgumentOutOfRangeException>(()=>read.SegmentEndAfter(time));
        }
        for(var i=0;i<100;i++)read.At(.25);
        var before=GC.GetAllocatedBytesForCurrentThread();
        RigidPose sampled=default;
        for(var i=0;i<1000;i++)sampled=read.At(.25);
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.Equal(0,allocated);Assert.Equal(.25,sampled.Center.X);
    }
}
