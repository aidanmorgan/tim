using System.Runtime.CompilerServices;
using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

public class CommittedPublicationMemoryTests
{
    private static PoseReadStamp Stamp(long revision)=>new(new(1),new(revision),revision*.01);
    private static BodyPublicationRead Read(int id,ulong revision,CompoundGeometry shape)=>
        new(new(new(id),PhysicsMotionType.Static,RigidPose.At(new(id*3,0,0)),RigidPose.Identity),
            new(new(id),new(revision),CollisionParticipation.Enabled,shape,shape),OwnerActivity.Inactive,default);
    private static CompoundGeometry Shape()=>new([new(new ConvexSphere(1),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (CommittedPoseBuffer Buffer,WeakReference<CompoundGeometry> Shape) Seed()
    {
        var shape=Shape();
        return (new(Stamp(0),[Read(0,0,shape)],[],[],[],[],[]),new(shape));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<CompoundGeometry> Replace(CommittedPoseBuffer buffer,int revision,bool publish)
    {
        var shape=Shape();
        buffer.BeginWrite(0);buffer.Stage(Stamp(revision),[Read(0,(ulong)revision,shape)],[],[],[],[],[]);
        if(publish)buffer.Publish();else buffer.Discard();
        return new(shape);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Alive(WeakReference<CompoundGeometry> shape)=>shape.TryGetTarget(out _);
    private static void Collect()
    {
        GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
    }

    [Fact]
    public void BufferRotationRetainsAtMostThreeVersionsAndOwnedReadsKeepTheirVersion()
    {
        var (buffer,initial)=Seed();
        var first=Replace(buffer,1,true);
        var second=Replace(buffer,2,true);
        var third=Replace(buffer,3,true);
        Collect();
        Assert.False(Alive(initial));
        Assert.True(Alive(first));Assert.True(Alive(second));Assert.True(Alive(third));
        var fourth=Replace(buffer,4,true);
        Collect();
        Assert.False(Alive(first));
        Assert.True(Alive(second));Assert.True(Alive(third));Assert.True(Alive(fourth));
        BodyQueryRead owned;
        using(var lease=buffer.Acquire())owned=lease.ReadQuery(PoseSample.Current,0);
        for(var i=5;i<=8;i++)Replace(buffer,i,true);
        Collect();
        Assert.True(Alive(fourth));
        Assert.NotNull(owned.Solid);
        GC.KeepAlive(owned);GC.KeepAlive(buffer);
    }

    [Fact]
    public void DiscardReleasesAbortedGeometryWithoutAnotherTick()
    {
        var (buffer,initial)=Seed();
        var aborted=Replace(buffer,1,false);
        Collect();
        Assert.False(Alive(aborted));
        Assert.True(Alive(initial));
        using var read=buffer.Acquire();
        Assert.Equal(Stamp(0),read.Stamp(PoseSample.Current));
        GC.KeepAlive(buffer);
    }

    [Fact]
    public void RemovalReleasesStorageEvenWhenRetiredLeaseIsStillReferenced()
    {
        var (buffer,initial)=Seed();
        var lease=buffer.Acquire();
        buffer.Remove();
        Collect();
        Assert.False(Alive(initial));
        Assert.Throws<InvalidOperationException>(()=>lease.ReadQuery(PoseSample.Current,0));
        lease.Dispose();
        GC.KeepAlive(lease);GC.KeepAlive(buffer);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    public void StablePublicationAndPoseCopyAllocateNothing(int count)
    {
        var shape=Shape();
        var input=Enumerable.Range(0,count).Select(i=>Read(i,0,shape)).ToArray();
        var output=new BodyPoseRead[count];
        var buffer=new CommittedPoseBuffer(Stamp(0),input,[],[],[],[],[]);
        for(var revision=1;revision<=100;revision++)
        {
            buffer.BeginWrite(0);buffer.Stage(Stamp(revision),input,[],[],[],[],[]);buffer.Publish();
            using var lease=buffer.Acquire();lease.Copy(PoseSample.Current,output);
        }
        var allocated=GC.GetAllocatedBytesForCurrentThread();
        var started=System.Diagnostics.Stopwatch.GetTimestamp();
        for(var revision=101;revision<=1100;revision++)
        {
            buffer.BeginWrite(0);buffer.Stage(Stamp(revision),input,[],[],[],[],[]);buffer.Publish();
            using var lease=buffer.Acquire();lease.Copy(PoseSample.Current,output);
        }
        var elapsed=System.Diagnostics.Stopwatch.GetElapsedTime(started);
        allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
        Assert.Equal(0,allocated);
        for(var i=0;i<count;i++)Assert.Equal(input[i].Pose,output[i]);
        var publicationBytes=Unsafe.SizeOf<BodyPublicationRead>()*count;
        var poseBytes=Unsafe.SizeOf<BodyPoseRead>()*count;
        Console.WriteLine($"Publication payload: bodies={count}, buffer_payload_bytes={3*publicationBytes}, staged_bytes_per_tick={publicationBytes}, copied_pose_bytes_per_frame={poseBytes}, cycles=1000, allocated_bytes={allocated}, elapsed_ms={elapsed.TotalMilliseconds:R}");
    }
}
