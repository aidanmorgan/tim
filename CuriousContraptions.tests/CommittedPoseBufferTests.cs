using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

public class CommittedPoseBufferTests
{
    private static readonly CompoundGeometry Shape=new([new(new ConvexSphere(1),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
    private static BodyPublicationRead Read(double x)=>new(
        new(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(x,0,0)),RigidPose.Identity),
        new(new(0),new(0),CollisionParticipation.Enabled,Shape,Shape),OwnerActivity.Inactive,default);
    private static PoseReadStamp Stamp(long revision)=>new(new(1),new(revision),revision*.01);

    [Fact]
    public void PublicationIsAtomicLeasedAndInvalidatedAtRemoval()
    {
        var buffer=new CommittedPoseBuffer(Stamp(0),[Read(0)],[],[],[],[],[]);
        var initial=buffer.Acquire();
        Assert.Equal(initial.Read(PoseSample.Previous,0),initial.Read(PoseSample.Current,0));
        Assert.Throws<InvalidOperationException>(()=>buffer.BeginWrite(0));
        initial.Dispose();
        Assert.Throws<InvalidOperationException>(()=>initial.Read(PoseSample.Current,0));
        Assert.Throws<InvalidOperationException>(()=>initial.Dispose());
        buffer.BeginWrite(0);
        buffer.Stage(Stamp(1),[Read(1)],[],[],[],[],[]);
        Assert.Throws<InvalidOperationException>(()=>buffer.Acquire());
        buffer.Discard();
        using(var unchanged=buffer.Acquire())Assert.Equal(Stamp(0),unchanged.Stamp(PoseSample.Current));
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),[Read(1)],[],[],[],[],[]);buffer.Publish();
        var committed=buffer.Acquire();
        Assert.Equal(Stamp(0),committed.Stamp(PoseSample.Previous));
        Assert.Equal(Stamp(1),committed.Stamp(PoseSample.Current));
        Assert.Equal(Read(0).Pose,committed.Read(PoseSample.Previous,0));
        var retained=committed.Read(PoseSample.Current,0);
        buffer.Remove();
        Assert.Throws<InvalidOperationException>(()=>committed.Read(PoseSample.Current,0));
        committed.Dispose();
        Assert.Equal(Read(1).Pose,retained);
        Assert.Throws<InvalidOperationException>(()=>buffer.Acquire());
    }

    [Fact]
    public void UnsupportedStampTopologyAndSampleReject()
    {
        Assert.Throws<ArgumentException>(()=>new CommittedPoseBuffer(default,[Read(0)],[],[],[],[],[]));
        var buffer=new CommittedPoseBuffer(Stamp(0),[Read(0)],[],[],[],[],[]);
        buffer.BeginWrite(0);
        Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(2),[Read(1)],[],[],[],[],[]));
        Assert.Throws<ArgumentException>(()=>buffer.Stage(new(new(2),new(1),.01),[Read(1)],[],[],[],[],[]));
        Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(1),[],[],[],[],[],[]));
        Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(1),[new(new(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,RigidPose.Identity),Read(0).Query,OwnerActivity.Inactive,default)],[],[],[],[],[]));
        buffer.Discard();
        using var lease=buffer.Acquire();
        Assert.Throws<ArgumentOutOfRangeException>(()=>lease.Read((PoseSample)999,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>lease.Stamp((PoseSample)999));
        Assert.Throws<ArgumentException>(()=>lease.Copy(PoseSample.Current,new BodyPoseRead[2]));
    }

    [Fact]
    public void StableTopologyPublicationAndLeasesAllocateNothingAfterWarmup()
    {
        var buffer=new CommittedPoseBuffer(Stamp(0),[Read(0)],[],[],[],[],[]);
        var pending=new[]{Read(1)};
        for(var i=1;i<=100;i++)
        {
            buffer.BeginWrite(0);buffer.Stage(Stamp(i),pending,[],[],[],[],[]);buffer.Publish();
            using var lease=buffer.Acquire();_=lease.Read(PoseSample.Current,0);
        }
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=101;i<=2100;i++)
        {
            buffer.BeginWrite(0);buffer.Stage(Stamp(i),pending,[],[],[],[],[]);buffer.Publish();
            using var lease=buffer.Acquire();_=lease.Read(PoseSample.Current,0);
        }
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
        using var final=buffer.Acquire();
        Assert.Equal(Stamp(2099),final.Stamp(PoseSample.Previous));
        Assert.Equal(Stamp(2100),final.Stamp(PoseSample.Current));
    }

    [Fact]
    public void QueryVersionsPublishWithPosesAndSurviveRetirement()
    {
        var initial=Read(0);
        var replacement=new CompoundGeometry([new(new ConvexSphere(2),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
        var changed=new BodyPublicationRead(Read(1).Pose,new(new(0),new(1),CollisionParticipation.Disabled,replacement,null),OwnerActivity.Inactive,default);
        var buffer=new CommittedPoseBuffer(Stamp(0),[initial],[],[],[],[],[]);
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),[changed],[],[],[],[],[]);buffer.Discard();
        using(var unchanged=buffer.Acquire())
        {
            Assert.Equal(initial.Pose,unchanged.Read(PoseSample.Current,0));
            Assert.Equal(initial.Query,unchanged.ReadQuery(PoseSample.Current,0));
        }
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),[changed],[],[],[],[],[]);buffer.Publish();
        var lease=buffer.Acquire();
        Assert.Equal(initial.Query,lease.ReadQuery(PoseSample.Previous,0));
        Assert.Equal(changed.Pose,lease.Read(PoseSample.Current,0));
        var retained=lease.ReadQuery(PoseSample.Current,0);
        Assert.Equal(changed.Query,retained);
        Assert.Throws<ArgumentOutOfRangeException>(()=>lease.ReadQuery((PoseSample)999,0));
        buffer.Remove();
        Assert.Throws<InvalidOperationException>(()=>lease.ReadQuery(PoseSample.Current,0));
        lease.Dispose();
        Assert.Same(replacement,retained.Solid);
        Assert.Null(retained.Opaque);
        Assert.Equal(CollisionParticipation.Disabled,retained.Participation);
    }

    [Fact]
    public void InvalidQueryOwnershipAndUnversionedChangesRejectBeforePublication()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BodyQueryRead(new(0),new(0),(CollisionParticipation)999,Shape,Shape));
        Assert.Throws<ArgumentNullException>(()=>new BodyQueryRead(new(0),new(0),CollisionParticipation.Enabled,null!,null));
        Assert.Throws<ArgumentException>(()=>new CommittedPoseBuffer(Stamp(0),[new(Read(0).Pose,default,OwnerActivity.Inactive,default)],[],[],[],[],[]));
        Assert.Throws<ArgumentException>(()=>new CommittedPoseBuffer(Stamp(0),
            [new(Read(0).Pose,new(new(1),new(0),CollisionParticipation.Enabled,Shape,Shape),OwnerActivity.Inactive,default)],[],[],[],[],[]));
        var buffer=new CommittedPoseBuffer(Stamp(0),[Read(0)],[],[],[],[],[]);
        buffer.BeginWrite(0);
        Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(1),
            [new(Read(1).Pose,new(new(0),new(0),CollisionParticipation.Disabled,Shape,null),OwnerActivity.Inactive,default)],[],[],[],[],[]));
        buffer.Discard();
        buffer.BeginWrite(0);
        buffer.Stage(Stamp(1),[new(Read(1).Pose,new(new(0),new(2),CollisionParticipation.Disabled,Shape,null),OwnerActivity.Inactive,default)],[],[],[],[],[]);
        buffer.Publish();
        buffer.BeginWrite(0);
        Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(2),
            [new(Read(2).Pose,new(new(0),new(1),CollisionParticipation.Disabled,Shape,null),OwnerActivity.Inactive,default)],[],[],[],[],[]));
        buffer.Discard();
        using var lease=buffer.Acquire();
        Assert.Equal(Stamp(1),lease.Stamp(PoseSample.Current));
        Assert.Equal(new PhysicsColliderRevision(2),lease.ReadQuery(PoseSample.Current,0).Revision);
    }

    [Fact]
    public void MotionReservationRequiresCompleteOrderedMatchingHistory()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.Identity,new(1,0,0),default);
        var physics=new PhysicsWorld([],[new(body,Shape,new(1,0,0))],[],new(default,maximumStep:1));
        physics.Step([],[],.01);var first=physics.LastMotion!;
        physics.Step([],[],.01);var second=physics.LastMotion!;
        var buffer=new CommittedPoseBuffer(Stamp(0),[Read(0)],[],[],[],[],[]);
        Assert.Throws<ArgumentOutOfRangeException>(()=>buffer.BeginWrite(-1));
        buffer.BeginWrite(1);
        Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(1),[Read(.01)],[],[],[],[],[]));
        Assert.Throws<ArgumentException>(()=>buffer.AppendMotion(second));
        buffer.AppendMotion(first);
        Assert.Throws<InvalidOperationException>(()=>buffer.AppendMotion(first));
        Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(1),[Read(1)],[],[],[],[],[]));
        buffer.Stage(Stamp(1),[Read(.01)],[],[],[],[],[]);buffer.Publish();
        using(var read=buffer.Acquire())
        {
            Assert.Equal(1,read.MotionStepCount);
            Assert.Same(first,read.ReadMotionStep(0));
            Assert.Equal(.005,read.SampleAcceptedPose(new(0),.005).Center.X);
        }
        buffer.BeginWrite(2);buffer.AppendMotion(second);
        Assert.Throws<ArgumentException>(()=>buffer.AppendMotion(second));
        buffer.Discard();
        using var unchanged=buffer.Acquire();
        Assert.Same(first,unchanged.ReadMotionStep(0));
    }
}
