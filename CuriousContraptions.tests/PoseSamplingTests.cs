using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

public class PoseSamplingTests
{
    private static readonly CompoundGeometry Shape=new([new(new ConvexSphere(.1),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
    private static readonly PoseReferenceBinding World=PoseReferenceBinding.Fixed(RigidPose.Identity);
    private static readonly PoseReferenceBinding Parent=PoseReferenceBinding.Attached(new(0),RigidPose.At(new(.5,0,0)));
    private static CommittedPoseBuffer Buffer()
    {
        var root=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.Identity,default,new(0,0,Math.Tau*2));
        var child=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.At(new(3,0,0)),new(1,0,0),default);
        var world=new PhysicsWorld([],[new(root,Shape,new(1,0,0)),new(child,Shape,new(1,0,0))],[],new(default,maximumStep:1));
        BodyPublicationRead Read(PhysicsBody body,PoseReferenceBinding reference)=>new(
            new(body.Id,body.MotionType,body.Pose,reference.Resolve(root.Pose)),
            new(body.Id,new(0),CollisionParticipation.Enabled,Shape,Shape),OwnerActivity.Inactive,new(body.LinearVelocity,body.AngularVelocity));
        var buffer=new CommittedPoseBuffer(new(new(1),new(0),0),[Read(root,World),Read(child,Parent)],[],[],[],[],[]);
        world.Step([],[],1);
        buffer.BeginWrite(1);buffer.AppendMotion(world.LastMotion!);
        buffer.Stage(new(new(1),new(1),1),[Read(root,World),Read(child,Parent)],[],[],[],[],[]);buffer.Publish();
        return buffer;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.125)]
    [InlineData(.25)]
    [InlineData(.5)]
    [InlineData(1)]
    public void AttachedReferenceFollowsFullSpinAtTheSameTime(double time)
    {
        var buffer=Buffer();using var lease=buffer.Acquire();
        var read=lease.SamplePose(time,1,Parent);
        var rotation=RigidRotation.FromRotationVector(new(0,0,Math.Tau*2*time));
        var reference=new RigidPose(rotation.Apply(new(.5,0,0)),rotation);
        Assert.InRange((read.Pose.Center-new CollisionVector(3+time,0,0)).Length,0,1e-12);
        Assert.InRange((read.ReferencePose.Center-reference.Center).Length,0,1e-12);
        Assert.InRange((read.ReferencePose.Rotation.Apply(new(1,0,0))-rotation.Apply(new(1,0,0))).Length,0,1e-12);
        Assert.InRange((read.RelativePose.Center-reference.InverseTransformPoint(read.Pose.Center)).Length,0,1e-12);
        if(time==1)Assert.Equal(lease.Read(PoseSample.Current,1),read);
        Assert.Equal(RigidPose.Identity,lease.SamplePose(time,0,World).ReferencePose);
    }

    [Fact]
    public void InvalidBindingsTimesAndRetiredLeasesReject()
    {
        var buffer=Buffer();var read=buffer.Acquire();
        Assert.Throws<ArgumentException>(()=>read.SamplePose(.5,1,default));
        Assert.Throws<ArgumentException>(()=>read.SamplePose(.5,1,World));
        Assert.Throws<IndexOutOfRangeException>(()=>read.SamplePose(.5,1,PoseReferenceBinding.Attached(new(99),RigidPose.Identity)));
        foreach(var time in new[]{-1,1.01,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>read.SamplePose(time,1,Parent));
        Assert.Throws<ArgumentException>(()=>PoseReferenceBinding.Fixed(default));
        buffer.Remove();Assert.Throws<InvalidOperationException>(()=>read.SamplePose(.5,1,Parent));read.Dispose();
        Assert.Throws<InvalidOperationException>(()=>default(PoseReadLease).SamplePose(0,0,World));
    }

    [Fact]
    public void AcceptedReferenceSamplingAllocatesNothingAfterWarmup()
    {
        var buffer=Buffer();using var lease=buffer.Acquire();
        for(var i=0;i<100;i++)lease.SamplePose(.125,1,Parent);
        var before=GC.GetAllocatedBytesForCurrentThread();
        BodyPoseRead result=default;
        for(var i=0;i<1000;i++)result=lease.SamplePose(.125,1,Parent);
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.Equal(0,allocated);Assert.Equal(3.125,result.Pose.Center.X);
    }
}
