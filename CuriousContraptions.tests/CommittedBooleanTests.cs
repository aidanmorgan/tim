using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
using System.Runtime.CompilerServices;

namespace CuriousContraptions.Tests;

public class CommittedBooleanTests
{
    private static readonly CompoundGeometry Shape=new([new(new ConvexSphere(1),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
    private static BodyPublicationRead Body(int id,int owner)=>new(
        new(new(id),PhysicsMotionType.Static,RigidPose.Identity,RigidPose.Identity),
        new(new(owner),new(0),CollisionParticipation.Enabled,Shape,Shape),OwnerActivity.Inactive,default);
    private static PoseReadStamp Stamp(long revision)=>new(new(1),new(revision),revision);
    private static readonly BooleanReadKey First=new(new(0),new(0)),Second=new(new(0),new(1));
    public enum Invalidity { Owner, NonRoot, Duplicate, Order, Count }
    [Theory]
    [InlineData(Invalidity.Owner)]
    [InlineData(Invalidity.NonRoot)]
    [InlineData(Invalidity.Duplicate)]
    [InlineData(Invalidity.Order)]
    [InlineData(Invalidity.Count)]
    public void InvalidBooleanTopologyCannotPublishAnyDomainAndRetrySucceeds(Invalidity fault)
    {
        BodyPublicationRead[] bodies=[Body(0,0),Body(1,0)];
        BooleanRead[] initial=[new(First,false),new(Second,true)];
        BooleanRead[] next=fault switch
        {
            Invalidity.Owner=>[new(First,true),new(new(new(2),new(1)),false)],
            Invalidity.NonRoot=>[new(First,true),new(new(new(1),new(1)),false)],
            Invalidity.Duplicate=>[new(First,true),new(First,false)],
            Invalidity.Order=>[new(Second,true),new(First,false)],
            Invalidity.Count=>[new(First,true)],
            _=>throw new ArgumentOutOfRangeException(nameof(fault))
        };
        if(fault is Invalidity.Owner or Invalidity.NonRoot or Invalidity.Duplicate)
            Assert.Throws<ArgumentException>(()=>new CommittedPoseBuffer(Stamp(0),bodies,[],[],[],[],next));
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],[],[],[],initial);
        buffer.BeginWrite(0);Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(1),bodies,[],[],[],[],next));
        buffer.Stage(Stamp(1),bodies,[],[],[],[],initial);buffer.Publish();
        using var read=buffer.Acquire();Assert.False(read.ReadBoolean(PoseSample.Current,First).Value);
        Assert.True(read.ReadBoolean(PoseSample.Current,Second).Value);
    }
    [Fact]
    public void OwnershipPinningDiscardCopyAndRemovalPreserveBooleanTypes()
    {
        BodyPublicationRead[] bodies=[Body(0,0)];BooleanRead[] values=[new(First,false)];
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],[],[],[],values);values[0]=new(First,true);
        var old=buffer.Acquire();Assert.False(old.ReadBoolean(PoseSample.Current,First).Value);
        Assert.Throws<InvalidOperationException>(()=>buffer.BeginWrite(0));old.Dispose();
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),bodies,[],[],[],[],values);values[0]=new(First,false);
        Assert.Throws<InvalidOperationException>(()=>buffer.Acquire());buffer.Publish();
        using(var read=buffer.Acquire())
        {
            Assert.Equal(1,read.BooleanCount);Assert.False(read.ReadBoolean(PoseSample.Previous,First).Value);
            Assert.True(read.ReadBoolean(PoseSample.Current,First).Value);
            var copy=new BooleanRead[1];read.CopyBooleans(PoseSample.Current,copy);Assert.True(copy[0].Value);
        }
        buffer.BeginWrite(0);buffer.Stage(Stamp(2),bodies,[],[],[],[],values);buffer.Discard();
        var final=buffer.Acquire();Assert.True(final.ReadBoolean(PoseSample.Current,First).Value);
        Assert.Throws<ArgumentException>(()=>final.ReadBoolean(PoseSample.Current,Second));
        Assert.Throws<ArgumentOutOfRangeException>(()=>final.ReadBoolean((PoseSample)999,First));
        Assert.Throws<ArgumentException>(()=>final.CopyBooleans(PoseSample.Current,[]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>final.CopyBooleans((PoseSample)999,new BooleanRead[1]));
        buffer.Remove();Assert.Throws<InvalidOperationException>(()=>final.ReadBoolean(PoseSample.Current,First));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BooleanObservationSlot(-1));
    }
    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    public void WarmBooleanPublicationAllocatesNothing(int count)
    {
        var bodies=Enumerable.Range(0,count).Select(i=>Body(i,i)).ToArray();
        var values=Enumerable.Range(0,count).Select(i=>new BooleanRead(new(new(i),new(0)),i%2==0)).ToArray();
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],[],[],[],values);var copy=new BooleanRead[count];
        for(var i=1;i<=100;i++)
        {
            buffer.BeginWrite(0);buffer.Stage(Stamp(i),bodies,[],[],[],[],values);buffer.Publish();
            using var read=buffer.Acquire();read.CopyBooleans(PoseSample.Current,copy);
        }
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=101;i<=1100;i++)
        {buffer.BeginWrite(0);buffer.Stage(Stamp(i),bodies,[],[],[],[],values);buffer.Publish();using var read=buffer.Acquire();read.CopyBooleans(PoseSample.Current,copy);}
        var bytes=GC.GetAllocatedBytesForCurrentThread()-before;
        Console.WriteLine($"Boolean publication: count={count}, bytes_per_read={Unsafe.SizeOf<BooleanRead>()}, cycles=1000, allocated_bytes={bytes}");
        Assert.Equal(0,bytes);Assert.Equal(values,copy);
    }
}
