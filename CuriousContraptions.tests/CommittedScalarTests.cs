using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
using System.Runtime.CompilerServices;

namespace CuriousContraptions.Tests;

public class CommittedScalarTests
{
    private static readonly CompoundGeometry Shape=new([new(new ConvexSphere(1),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
    private static BodyPublicationRead Body(int id,int owner)=>new(
        new(new(id),PhysicsMotionType.Static,RigidPose.Identity,RigidPose.Identity),
        new(new(owner),new(0),CollisionParticipation.Enabled,Shape,Shape),OwnerActivity.Inactive,default);
    private static PoseReadStamp Stamp(long revision)=>new(new(1),new(revision),revision);
    private static readonly ScalarReadKey First=new(new(0),new(0)),Second=new(new(0),new(1));
    private static ScalarRead Reading(ScalarReadKey key,double value)=>new(key,ScalarUnit.GameIrradiance,value);
    public enum Invalidity { Nonfinite,UndefinedUnit,ChangedUnit,Owner,NonRoot,Duplicate,Order,Count }
    [Theory]
    [InlineData(Invalidity.Nonfinite)]
    [InlineData(Invalidity.UndefinedUnit)]
    [InlineData(Invalidity.ChangedUnit)]
    [InlineData(Invalidity.Owner)]
    [InlineData(Invalidity.NonRoot)]
    [InlineData(Invalidity.Duplicate)]
    [InlineData(Invalidity.Order)]
    [InlineData(Invalidity.Count)]
    public void InvalidScalarCannotPublishPartOfARevisionAndRetrySucceeds(Invalidity invalidity)
    {
        BodyPublicationRead[] bodies=[Body(0,0),Body(1,0)];
        ScalarRead[] initial=[Reading(First,0),Reading(Second,0)];
        ScalarRead[] next=[Reading(First,1),Reading(Second,2)];
        next=invalidity switch
        {
            Invalidity.Nonfinite=>[next[0],next[1] with {Value=double.NaN}],
            Invalidity.UndefinedUnit=>[next[0],next[1] with {Unit=(ScalarUnit)999}],
            Invalidity.ChangedUnit=>[next[0],next[1] with {Unit=ScalarUnit.Dimensionless}],
            Invalidity.Owner=>[next[0],Reading(new(new(2),new(1)),2)],
            Invalidity.NonRoot=>[next[0],Reading(new(new(1),new(1)),2)],
            Invalidity.Duplicate=>[next[0],next[0]],
            Invalidity.Order=>[next[1],next[0]],
            Invalidity.Count=>[next[0]],
            _=>throw new ArgumentOutOfRangeException(nameof(invalidity))
        };
        if(invalidity is not (Invalidity.ChangedUnit or Invalidity.Order or Invalidity.Count))
            Assert.Throws<ArgumentException>(()=>new CommittedPoseBuffer(Stamp(0),bodies,[],[],next,[],[]));
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],[],initial,[],[]);
        buffer.BeginWrite(0);Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(1),bodies,[],[],next,[],[]));
        buffer.Stage(Stamp(1),bodies,[],[],initial,[],[]);buffer.Publish();
        using var read=buffer.Acquire();Assert.Equal(0,read.ReadScalar(PoseSample.Current,First).Value);
        Assert.Equal(0,read.ReadScalar(PoseSample.Current,Second).Value);
    }
    [Fact]
    public void SnapshotOwnsValuesAndPinningDiscardRemovalApplyToScalars()
    {
        BodyPublicationRead[] bodies=[Body(0,0)];
        ScalarRead[] values=[Reading(First,.25)];
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],[],values,[],[]);
        values[0]=Reading(First,.5);var lease=buffer.Acquire();
        Assert.Equal(.25,lease.ReadScalar(PoseSample.Current,First).Value);
        Assert.Throws<InvalidOperationException>(()=>buffer.BeginWrite(0));lease.Dispose();
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),bodies,[],[],values,[],[]);values[0]=Reading(First,1);
        Assert.Throws<InvalidOperationException>(()=>buffer.Acquire());buffer.Publish();
        using(var read=buffer.Acquire())
        {
            Assert.Equal(.25,read.ReadScalar(PoseSample.Previous,First).Value);
            Assert.Equal(.5,read.ReadScalar(PoseSample.Current,First).Value);
            Assert.Equal(1,read.ScalarCount);
        }
        buffer.BeginWrite(0);buffer.Stage(Stamp(2),bodies,[],[],values,[],[]);buffer.Discard();
        var final=buffer.Acquire();Assert.Equal(.5,final.ReadScalar(PoseSample.Current,First).Value);
        Assert.Throws<ArgumentException>(()=>final.ReadScalar(PoseSample.Current,Second));
        Assert.Throws<ArgumentOutOfRangeException>(()=>final.ReadScalar((PoseSample)999,First));
        Assert.Throws<ArgumentException>(()=>final.CopyScalars(PoseSample.Current,[]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>final.CopyScalars((PoseSample)999,new ScalarRead[1]));
        buffer.Remove();Assert.Throws<InvalidOperationException>(()=>final.ReadScalar(PoseSample.Current,First));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ScalarObservationSlot(-1));
    }
    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    public void WarmScalarPublicationCopiesAllocateNothing(int count)
    {
        var bodies=Enumerable.Range(0,count).Select(i=>Body(i,i)).ToArray();
        var values=Enumerable.Range(0,count).Select(i=>Reading(new(new(i),new(0)),i*.25)).ToArray();
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],[],values,[],[]);var copy=new ScalarRead[count];
        for(var i=1;i<=100;i++)
        {buffer.BeginWrite(0);buffer.Stage(Stamp(i),bodies,[],[],values,[],[]);buffer.Publish();using var read=buffer.Acquire();read.CopyScalars(PoseSample.Current,copy);}
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=101;i<=1100;i++)
        {buffer.BeginWrite(0);buffer.Stage(Stamp(i),bodies,[],[],values,[],[]);buffer.Publish();using var read=buffer.Acquire();read.CopyScalars(PoseSample.Current,copy);}
        var bytes=GC.GetAllocatedBytesForCurrentThread()-before;
        Console.WriteLine($"Scalar publication: count={count}, bytes_per_read={Unsafe.SizeOf<ScalarRead>()}, cycles=1000, allocated_bytes={bytes}");
        Assert.Equal(0,bytes);Assert.Equal(values,copy);
    }
}
