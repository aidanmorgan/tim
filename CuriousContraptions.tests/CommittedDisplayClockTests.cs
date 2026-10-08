using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

public class CommittedDisplayClockTests
{
    private static readonly WorldGeneration Generation=new(1);
    private static PoseReadStamp Stamp(long revision)=>new(Generation,new(revision),revision);
    private static void Publish(CommittedPoseBuffer buffer,long revision,params BodyPublicationRead[] bodies)
    {
        buffer.BeginWrite(0);buffer.Stage(Stamp(revision),bodies,[],[],[],[],[]);buffer.Publish();
    }
    [Fact]
    public void PauseResumeSkippedFramesAndRepeatedPhasesNeverRewind()
    {
        var buffer=new CommittedPoseBuffer(Stamp(0),[],[],[],[],[],[]);var clock=new CommittedDisplayClock(Generation);
        using(var seed=buffer.Acquire())Assert.Equal(0,clock.Select(seed,DisplayPlayback.Running,.75));
        Publish(buffer,1);
        using(var read=buffer.Acquire())
        {
            Assert.Equal(.25,clock.Select(read,DisplayPlayback.Running,.25));
            Assert.Equal(.25,clock.Select(read,DisplayPlayback.Running,0));
            Assert.Equal(.75,clock.Select(read,DisplayPlayback.Running,.75));
            Assert.Equal(1,clock.Select(read,DisplayPlayback.Stopped,0));
            Assert.Equal(1,clock.Select(read,DisplayPlayback.Running,.5));
        }
        Publish(buffer,2);Publish(buffer,3);
        using(var read=buffer.Acquire())
        {
            Assert.Equal(2.5,clock.Select(read,DisplayPlayback.Running,.5));
            Assert.Equal(3,clock.Select(read,DisplayPlayback.Running,1));
        }
    }
    [Fact]
    public void ColliderRevisionChangesSnapTheWholeDisplayInterval()
    {
        var shape=new CompoundGeometry([new(new ConvexSphere(1),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
        BodyPublicationRead Body(ulong revision)=>new(
            new(new(0),PhysicsMotionType.Static,RigidPose.Identity,RigidPose.Identity),
            new(new(0),new(revision),CollisionParticipation.Enabled,shape,shape),OwnerActivity.Inactive,default);
        var buffer=new CommittedPoseBuffer(Stamp(0),[Body(0)],[],[],[],[],[]);
        var clock=new CommittedDisplayClock(Generation);
        Publish(buffer,1,Body(1));
        using(var read=buffer.Acquire())Assert.Equal(1,clock.Select(read,DisplayPlayback.Running,0));
        Publish(buffer,2,Body(1));
        using(var read=buffer.Acquire())Assert.Equal(1.25,clock.Select(read,DisplayPlayback.Running,.25));
    }
    [Fact]
    public void InvalidFractionsModesGenerationsAndOldPublicationsRejectWithoutAdvancing()
    {
        var buffer=new CommittedPoseBuffer(Stamp(0),[],[],[],[],[],[]);var clock=new CommittedDisplayClock(Generation);
        Publish(buffer,1);
        using(var read=buffer.Acquire())
        {
            foreach(var fraction in new[]{-1,1.01,double.NaN,double.PositiveInfinity})
                Assert.Throws<ArgumentOutOfRangeException>(()=>clock.Select(read,DisplayPlayback.Running,fraction));
            Assert.Throws<ArgumentOutOfRangeException>(()=>clock.Select(read,(DisplayPlayback)99,.5));
            Assert.Equal(.5,clock.Select(read,DisplayPlayback.Running,.5));
        }
        var old=new CommittedPoseBuffer(Stamp(0),[],[],[],[],[],[]);
        using(var read=old.Acquire())Assert.Throws<InvalidOperationException>(()=>clock.Select(read,DisplayPlayback.Running,.5));
        var next=new CommittedPoseBuffer(new(new(2),new(0),0),[],[],[],[],[],[]);
        using(var read=next.Acquire())
        {
            Assert.Throws<InvalidOperationException>(()=>clock.Select(read,DisplayPlayback.Running,.5));
            Assert.Equal(0,new CommittedDisplayClock(new(2)).Select(read,DisplayPlayback.Running,.5));
        }
        Assert.Throws<ArgumentException>(()=>new CommittedDisplayClock(default));
        Assert.Throws<InvalidOperationException>(()=>clock.Select(default,DisplayPlayback.Running,.5));
    }
    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    public void WarmClockSelectionAllocatesNoManagedMemory(int count)
    {
        var shape=new CompoundGeometry([new(new ConvexSphere(1),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
        var bodies=Enumerable.Range(0,count).Select(i=>new BodyPublicationRead(
            new(new(i),PhysicsMotionType.Static,RigidPose.Identity,RigidPose.Identity),
            new(new(i),new(0),CollisionParticipation.Enabled,shape,shape),OwnerActivity.Inactive,default)).ToArray();
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],[],[],[],[]);Publish(buffer,1,bodies);
        var clock=new CommittedDisplayClock(Generation);using var read=buffer.Acquire();
        for(var i=0;i<100;i++)clock.Select(read,DisplayPlayback.Running,.5);
        var before=GC.GetAllocatedBytesForCurrentThread();
        double time=0;
        for(var i=0;i<1000;i++)time=clock.Select(read,DisplayPlayback.Running,.5);
        var bytes=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.Equal(.5,time);Assert.Equal(0,bytes);
    }
}
