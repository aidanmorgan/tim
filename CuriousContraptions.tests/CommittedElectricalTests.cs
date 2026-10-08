using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
using System.Runtime.CompilerServices;

namespace CuriousContraptions.Tests;

public class CommittedElectricalTests
{
    private static readonly CompoundGeometry Shape=new([new(new ConvexSphere(1),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
    private static BodyPublicationRead Body(int id,int owner)=>new(
        new(new(id),PhysicsMotionType.Static,RigidPose.Identity,RigidPose.Identity),
        new(new(owner),new(0),CollisionParticipation.Enabled,Shape,Shape),OwnerActivity.Inactive,default);
    private static PoseReadStamp Stamp(long revision)=>new(new(1),new(revision),revision);
    private static readonly ElectricalInputKey First=new(new(0),SocketId.FirstIn),Second=new(new(0),SocketId.SecondIn);
    private static ElectricalInputRead Reading(ElectricalInputKey key,bool available)=>
        new(key,available?ElectricalAvailability.Available:ElectricalAvailability.Unavailable);

    [Fact]
    public void ElectricalReadsAreOwnedAtomicAndPinnedWithTheWholeRevision()
    {
        BodyPublicationRead[] bodies=[Body(0,0)];
        ElectricalInputRead[] inputs=[Reading(First,false),Reading(Second,true)];
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],inputs,[],[],[]);
        inputs[0]=Reading(First,true);
        var lease=buffer.Acquire();
        Assert.Equal(2,lease.ElectricalInputCount);
        Assert.Equal(ElectricalAvailability.Unavailable,lease.ReadElectricalInput(PoseSample.Current,First).Availability);
        Assert.Throws<InvalidOperationException>(()=>buffer.BeginWrite(0));lease.Dispose();
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),bodies,[],inputs,[],[],[]);
        inputs[1]=Reading(Second,false);
        Assert.Throws<InvalidOperationException>(()=>buffer.Acquire());buffer.Publish();
        using(var read=buffer.Acquire())
        {
            Assert.Equal(Stamp(1),read.Stamp(PoseSample.Current));
            Assert.Equal(ElectricalAvailability.Unavailable,read.ReadElectricalInput(PoseSample.Previous,First).Availability);
            Assert.Equal(ElectricalAvailability.Available,read.ReadElectricalInput(PoseSample.Current,First).Availability);
            Assert.Equal(ElectricalAvailability.Available,read.ReadElectricalInput(PoseSample.Current,Second).Availability);
            var copy=new ElectricalInputRead[2];read.CopyElectricalInputs(PoseSample.Current,copy);
            Assert.Equal(Reading(First,true),copy[0]);Assert.Equal(Reading(Second,true),copy[1]);
        }
        buffer.BeginWrite(0);buffer.Stage(Stamp(2),bodies,[],inputs,[],[],[]);buffer.Discard();
        using(var read=buffer.Acquire())Assert.Equal(ElectricalAvailability.Available,read.ReadElectricalInput(PoseSample.Current,Second).Availability);
        buffer.Remove();
        Assert.Throws<InvalidOperationException>(()=>lease.ReadElectricalInput(PoseSample.Current,First));
    }

    public enum Invalidity { Socket,Availability,Owner,NonRoot,Duplicate,Order,Count }
    [Theory]
    [InlineData(Invalidity.Socket)]
    [InlineData(Invalidity.Availability)]
    [InlineData(Invalidity.Owner)]
    [InlineData(Invalidity.NonRoot)]
    [InlineData(Invalidity.Duplicate)]
    [InlineData(Invalidity.Order)]
    [InlineData(Invalidity.Count)]
    public void InvalidInputCannotPartiallyStageAndValidRetrySucceeds(Invalidity invalidity)
    {
        BodyPublicationRead[] bodies=[Body(0,0),Body(1,0)];
        ElectricalInputRead[] initial=[Reading(First,false),Reading(Second,false)];
        ElectricalInputRead[] next=[Reading(First,true),Reading(Second,true)];
        next=invalidity switch
        {
            Invalidity.Socket=>[next[0],new(new(new(0),(SocketId)999),ElectricalAvailability.Available)],
            Invalidity.Availability=>[next[0],new(Second,(ElectricalAvailability)999)],
            Invalidity.Owner=>[next[0],Reading(new(new(2),SocketId.SecondIn),true)],
            Invalidity.NonRoot=>[next[0],Reading(new(new(1),SocketId.SecondIn),true)],
            Invalidity.Duplicate=>[next[0],next[0]],
            Invalidity.Order=>[next[1],next[0]],
            Invalidity.Count=>[next[0]],
            _=>throw new ArgumentOutOfRangeException(nameof(invalidity))
        };
        if(invalidity is not (Invalidity.Order or Invalidity.Count))
            Assert.Throws<ArgumentException>(()=>new CommittedPoseBuffer(Stamp(0),bodies,[],next,[],[],[]));
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],initial,[],[],[]);
        buffer.BeginWrite(0);Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(1),bodies,[],next,[],[],[]));
        buffer.Stage(Stamp(1),bodies,[],initial,[],[],[]);buffer.Publish();
        using var read=buffer.Acquire();
        Assert.Equal(ElectricalAvailability.Unavailable,read.ReadElectricalInput(PoseSample.Current,First).Availability);
        Assert.Equal(ElectricalAvailability.Unavailable,read.ReadElectricalInput(PoseSample.Current,Second).Availability);
    }

    [Fact]
    public void ReadBoundariesAndRemovedLeaseRejectUnsupportedRequests()
    {
        var buffer=new CommittedPoseBuffer(Stamp(0),[Body(0,0)],[],[Reading(First,true)],[],[],[]);
        var read=buffer.Acquire();
        Assert.Throws<ArgumentException>(()=>read.ReadElectricalInput(PoseSample.Current,Second));
        Assert.Throws<ArgumentOutOfRangeException>(()=>read.ReadElectricalInput((PoseSample)999,First));
        Assert.Throws<ArgumentOutOfRangeException>(()=>read.CopyElectricalInputs((PoseSample)999,new ElectricalInputRead[1]));
        Assert.Throws<ArgumentException>(()=>read.CopyElectricalInputs(PoseSample.Current,[]));
        buffer.Remove();
        Assert.Throws<InvalidOperationException>(()=>read.ReadElectricalInput(PoseSample.Current,First));
        Assert.Throws<InvalidOperationException>(()=>read.CopyElectricalInputs(PoseSample.Current,new ElectricalInputRead[1]));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    public void WarmPublicationAndCopiesAllocateNothing(int count)
    {
        var bodies=Enumerable.Range(0,count).Select(i=>Body(i,i)).ToArray();
        var inputs=Enumerable.Range(0,count).Select(i=>Reading(new(new(i),SocketId.PowerIn),true)).ToArray();
        var copy=new ElectricalInputRead[count];
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],inputs,[],[],[]);
        for(var revision=1;revision<=100;revision++)
        {
            buffer.BeginWrite(0);buffer.Stage(Stamp(revision),bodies,[],inputs,[],[],[]);buffer.Publish();
            using var read=buffer.Acquire();read.CopyElectricalInputs(PoseSample.Current,copy);
        }
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var revision=101;revision<=1100;revision++)
        {
            buffer.BeginWrite(0);buffer.Stage(Stamp(revision),bodies,[],inputs,[],[],[]);buffer.Publish();
            using var read=buffer.Acquire();read.CopyElectricalInputs(PoseSample.Current,copy);
        }
        var bytes=GC.GetAllocatedBytesForCurrentThread()-before;
        Console.WriteLine($"Electrical publication: inputs={count}, bytes_per_input={Unsafe.SizeOf<ElectricalInputRead>()}, cycles=1000, allocated_bytes={bytes}");
        Assert.Equal(0,bytes);Assert.Equal(inputs,copy);
    }
}
