using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class CommittedEnumTests
{
    public enum Mode:byte { Idle, Ready }
    public enum OtherMode:long { Stopped=-3, Moving=long.MaxValue }
    private static readonly CompoundGeometry Shape=new([new(new ConvexSphere(1),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
    private static BodyPublicationRead Body(int id,int owner)=>new(
        new(new(id),PhysicsMotionType.Static,RigidPose.Identity,RigidPose.Identity),
        new(new(owner),new(0),CollisionParticipation.Enabled,Shape,Shape),OwnerActivity.Inactive,default);
    private static PoseReadStamp Stamp(long revision)=>new(new(1),new(revision),revision);
    private static readonly EnumReadKey<Mode> First=new(new(0),new(0)),Second=new(new(0),new(1));
    private static readonly EnumReadKey<OtherMode> Other=new(new(0),new(0));
    public enum Invalidity { Owner, NonRoot, Duplicate, Order, Count, Undefined }
    [Theory]
    [InlineData(Invalidity.Owner)]
    [InlineData(Invalidity.NonRoot)]
    [InlineData(Invalidity.Duplicate)]
    [InlineData(Invalidity.Order)]
    [InlineData(Invalidity.Count)]
    [InlineData(Invalidity.Undefined)]
    public void InvalidStagingCannotChangeCommittedValuesAndCorrectedRetrySucceeds(Invalidity fault)
    {
        BodyPublicationRead[] bodies=[Body(0,0),Body(1,0)];
        EnumRead<Mode>[] initial=[new(First,Mode.Idle),new(Second,Mode.Ready)];
        EnumRead<Mode>[] invalid=fault switch
        {
            Invalidity.Owner=>[new(First,Mode.Ready),new(new(new(2),new(1)),Mode.Idle)],
            Invalidity.NonRoot=>[new(First,Mode.Ready),new(new(new(1),new(1)),Mode.Idle)],
            Invalidity.Duplicate=>[new(First,Mode.Ready),new(First,Mode.Idle)],
            Invalidity.Order=>[new(Second,Mode.Ready),new(First,Mode.Idle)],
            Invalidity.Count=>[new(First,Mode.Ready)],
            Invalidity.Undefined=>[new(First,Mode.Ready),new(Second,(Mode)255)],
            _=>throw new ArgumentOutOfRangeException(nameof(fault))
        };
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],[],[],[],[]);
        if(fault is Invalidity.Owner or Invalidity.NonRoot or Invalidity.Duplicate or Invalidity.Undefined)
            Assert.Throws<ArgumentException>(()=>buffer.RegisterEnums<Mode>(invalid));
        buffer.RegisterEnums<Mode>(initial);buffer.BeginWrite(0);
        Assert.Throws<ArgumentException>(()=>buffer.StageEnums<Mode>(invalid));
        Assert.Throws<InvalidOperationException>(()=>buffer.Stage(Stamp(1),bodies,[],[],[],[],[]));
        buffer.StageEnums<Mode>(initial);buffer.Stage(Stamp(1),bodies,[],[],[],[],[]);buffer.Publish();
        using var read=buffer.Acquire();Assert.Equal(Mode.Idle,read.ReadEnum(PoseSample.Current,First).Value);
        Assert.Equal(Mode.Ready,read.ReadEnum(PoseSample.Current,Second).Value);
    }
    [Fact]
    public void DistinctEnumTypesShareAtomicLeaseWithoutNumericOrStringConversion()
    {
        BodyPublicationRead[] bodies=[Body(0,0)];
        EnumRead<Mode>[] values=[new(First,Mode.Idle)];
        EnumRead<OtherMode>[] other=[new(Other,OtherMode.Stopped)];
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],[],[],[],[]);
        buffer.RegisterEnums<Mode>(values);buffer.RegisterEnums<OtherMode>(other);
        Assert.Throws<ArgumentException>(()=>buffer.RegisterEnums<Mode>(values));
        values[0]=new(First,Mode.Ready);other[0]=new(Other,OtherMode.Moving);
        var seed=buffer.Acquire();
        Assert.Equal(Mode.Idle,seed.ReadEnum(PoseSample.Current,First).Value);
        Assert.Equal(OtherMode.Stopped,seed.ReadEnum(PoseSample.Current,Other).Value);
        Assert.Throws<InvalidOperationException>(()=>buffer.BeginWrite(0));seed.Dispose();
        Assert.Throws<InvalidOperationException>(()=>buffer.RegisterEnums<DayOfWeek>([]));
        buffer.BeginWrite(0);buffer.StageEnums<Mode>(values);
        Assert.Throws<InvalidOperationException>(()=>buffer.Stage(Stamp(1),bodies,[],[],[],[],[]));
        Assert.Throws<InvalidOperationException>(()=>buffer.StageEnums<Mode>(values));
        buffer.StageEnums<OtherMode>(other);buffer.Stage(Stamp(1),bodies,[],[],[],[],[]);
        values[0]=new(First,Mode.Idle);other[0]=new(Other,OtherMode.Stopped);
        Assert.Throws<InvalidOperationException>(()=>buffer.Acquire());buffer.Publish();
        using(var read=buffer.Acquire())
        {
            Assert.Equal(1,read.EnumCount<Mode>());Assert.Equal(1,read.EnumCount<OtherMode>());
            Assert.Equal(Mode.Ready,read.ReadEnum(PoseSample.Current,First).Value);
            Assert.Equal(Mode.Idle,read.ReadEnum(PoseSample.Previous,First).Value);
            Assert.Equal(OtherMode.Moving,read.ReadEnum(PoseSample.Current,Other).Value);
            Assert.Equal(OtherMode.Stopped,read.ReadEnum(PoseSample.Previous,Other).Value);
        }
        buffer.BeginWrite(0);buffer.StageEnums<Mode>(values);buffer.Discard();
        using(var read=buffer.Acquire())Assert.Equal(Mode.Ready,read.ReadEnum(PoseSample.Current,First).Value);
        buffer.BeginWrite(0);buffer.StageEnums<Mode>(values);buffer.StageEnums<OtherMode>(other);
        buffer.Stage(Stamp(2),bodies,[],[],[],[],[]);buffer.Discard();
        var final=buffer.Acquire();Assert.Equal(Mode.Ready,final.ReadEnum(PoseSample.Current,First).Value);
        Assert.Throws<ArgumentException>(()=>final.ReadEnum(PoseSample.Current,Second));
        Assert.Throws<ArgumentOutOfRangeException>(()=>final.ReadEnum((PoseSample)999,First));
        Assert.Throws<ArgumentException>(()=>final.CopyEnums<Mode>(PoseSample.Current,[]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>final.CopyEnums<Mode>((PoseSample)999,new EnumRead<Mode>[1]));
        Assert.Throws<ArgumentException>(()=>final.EnumCount<DayOfWeek>());
        buffer.Remove();Assert.Throws<InvalidOperationException>(()=>final.EnumCount<Mode>());
        Assert.Throws<InvalidOperationException>(()=>final.ReadEnum(PoseSample.Current,Other));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new EnumObservationSlot<Mode>(-1));
    }
    [Fact]
    public void FirstWriteFreezesRegistrationAndFailedOtherDomainDiscardsEnums()
    {
        BodyPublicationRead[] bodies=[Body(0,0)];
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],[],[],[],[]);
        buffer.RegisterEnums<Mode>([new(First,Mode.Idle)]);buffer.BeginWrite(0);
        Assert.Throws<ArgumentException>(()=>buffer.StageEnums<OtherMode>([]));
        buffer.StageEnums<Mode>([new(First,Mode.Ready)]);
        Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(2),bodies,[],[],[],[],[]));
        buffer.Discard();Assert.Throws<InvalidOperationException>(()=>buffer.RegisterEnums<OtherMode>([]));
        using var read=buffer.Acquire();Assert.Equal(Stamp(0),read.Stamp(PoseSample.Current));
        Assert.Equal(Mode.Idle,read.ReadEnum(PoseSample.Current,First).Value);
    }
    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    public void WarmTypedPublicationAllocatesNothing(int count)
    {
        var bodies=Enumerable.Range(0,count).Select(i=>Body(i,i)).ToArray();
        var values=Enumerable.Range(0,count).Select(i=>new EnumRead<Mode>(new(new(i),new(0)),Mode.Ready)).ToArray();
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],[],[],[],[]);
        buffer.RegisterEnums<Mode>(values);var copy=new EnumRead<Mode>[count];
        void Cycle(int i)
        {
            buffer.BeginWrite(0);buffer.StageEnums<Mode>(values);buffer.Stage(Stamp(i),bodies,[],[],[],[],[]);buffer.Publish();
            using var read=buffer.Acquire();read.CopyEnums<Mode>(PoseSample.Current,copy);
        }
        for(var i=1;i<=100;i++)Cycle(i);
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=101;i<=1100;i++)Cycle(i);
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);Assert.Equal(values,copy);
    }
}
