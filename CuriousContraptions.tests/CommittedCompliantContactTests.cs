using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

public class CommittedCompliantContactTests
{
    public enum InvalidContact { SameParticipant, MissingBody, StaticBody, UndefinedPhase, NonFiniteSpeed, ReversedBounds, NegativeRadius }
    private static readonly CompoundGeometry Shape=new([new(new ConvexSphere(1),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
    private static PoseReadStamp Stamp(long revision)=>new(new(1),new(revision),revision*.01);
    private static BodyPublicationRead[] Bodies(int contacts)=>Enumerable.Range(0,contacts+1).Select(i=>
        new BodyPublicationRead(new(new(i),i==0?PhysicsMotionType.Static:PhysicsMotionType.Dynamic,RigidPose.Identity,RigidPose.Identity),
            new(new(i),new(0),CollisionParticipation.Enabled,Shape,Shape),OwnerActivity.Inactive,default)).ToArray();
    private static CompliantContactState Value(int body=1)=>new(new(new(body),new(0)),CompliantContactPhase.Ready,
        new(new(0,.3,0),-.2,.2,-.2,.2,.2),0,0);
    private static CommittedPoseBuffer Buffer(BodyPublicationRead[] bodies)=>new(Stamp(0),bodies,[],[],[],[],[]);

    [Fact]
    public void ContactAndPosePublicationShareAtomicityAndLeaseLifetime()
    {
        var bodies=Bodies(1);var buffer=Buffer(bodies);var initial=Value();
        buffer.RegisterCompliantContacts([initial]);
        using(var seed=buffer.Acquire())
        {
            Assert.Equal(1,seed.CompliantContactCount);
            Assert.Equal(initial,seed.ReadCompliantContact(PoseSample.Previous,initial.Key));
            Assert.Throws<InvalidOperationException>(()=>buffer.BeginWrite(0));
        }
        var engaged=initial with {Phase=CompliantContactPhase.Engaged,EpisodeCount=1,RelativeSpeed=-2};
        buffer.BeginWrite(0);
        Assert.Throws<InvalidOperationException>(()=>buffer.Stage(Stamp(1),bodies,[],[],[],[],[]));
        buffer.StageCompliantContacts([engaged]);
        Assert.Throws<InvalidOperationException>(()=>buffer.StageCompliantContacts([engaged]));
        buffer.Stage(Stamp(1),bodies,[],[],[],[],[]);
        Assert.Throws<InvalidOperationException>(()=>buffer.Acquire());
        buffer.Discard();
        using(var unchanged=buffer.Acquire())Assert.Equal(initial,unchanged.ReadCompliantContact(PoseSample.Current,initial.Key));
        buffer.BeginWrite(0);buffer.StageCompliantContacts([engaged]);
        buffer.Stage(Stamp(1),bodies,[],[],[],[],[]);buffer.Publish();
        var read=buffer.Acquire();
        Assert.Equal(initial,read.ReadCompliantContact(PoseSample.Previous,initial.Key));
        Assert.Equal(engaged,read.ReadCompliantContact(PoseSample.Current,initial.Key));
        Assert.Throws<ArgumentException>(()=>read.CopyCompliantContacts(PoseSample.Current,new CompliantContactState[2]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>read.ReadCompliantContact((PoseSample)999,initial.Key));
        buffer.Remove();
        Assert.Throws<InvalidOperationException>(()=>read.ReadCompliantContact(PoseSample.Current,initial.Key));
        read.Dispose();
    }

    [Theory]
    [InlineData(InvalidContact.SameParticipant)]
    [InlineData(InvalidContact.MissingBody)]
    [InlineData(InvalidContact.StaticBody)]
    [InlineData(InvalidContact.UndefinedPhase)]
    [InlineData(InvalidContact.NonFiniteSpeed)]
    [InlineData(InvalidContact.ReversedBounds)]
    [InlineData(InvalidContact.NegativeRadius)]
    public void InvalidContactsRejectBeforeRegistrationAndStaging(InvalidContact invalid)
    {
        var value=Value();var malformed=invalid switch
        {
            InvalidContact.SameParticipant=>value with {Key=new(new(1),new(1))},
            InvalidContact.MissingBody=>value with {Key=new(new(99),new(0))},
            InvalidContact.StaticBody=>value with {Key=new(new(0),new(1))},
            InvalidContact.UndefinedPhase=>value with {Phase=(CompliantContactPhase)999},
            InvalidContact.NonFiniteSpeed=>value with {RelativeSpeed=double.NaN},
            InvalidContact.ReversedBounds=>value with {Footprint=value.Footprint with {MinimumX=1}},
            InvalidContact.NegativeRadius=>value with {Footprint=value.Footprint with {RoundingRadius=-1}},
            _=>throw new ArgumentOutOfRangeException(nameof(invalid))
        };
        var bodies=Bodies(1);var buffer=Buffer(bodies);
        Assert.Throws<ArgumentException>(()=>buffer.RegisterCompliantContacts([malformed]));
        buffer.RegisterCompliantContacts([value]);
        buffer.BeginWrite(0);Assert.Throws<ArgumentException>(()=>buffer.StageCompliantContacts([malformed]));
        buffer.Discard();using var read=buffer.Acquire();
        Assert.Equal(value,read.ReadCompliantContact(PoseSample.Current,value.Key));
    }

    [Fact]
    public void FrozenTopologyRejectsMissingDuplicateReorderedAndRegressingContacts()
    {
        var bodies=Bodies(2);var buffer=Buffer(bodies);var a=Value(1) with {EpisodeCount=2};var b=Value(2);
        Assert.Throws<ArgumentException>(()=>buffer.RegisterCompliantContacts([a,a]));
        buffer.RegisterCompliantContacts([a,b]);
        Assert.Throws<InvalidOperationException>(()=>buffer.RegisterCompliantContacts([a,b]));
        buffer.BeginWrite(0);
        Assert.Throws<ArgumentException>(()=>buffer.StageCompliantContacts([a]));
        Assert.Throws<ArgumentException>(()=>buffer.StageCompliantContacts([b,a]));
        Assert.Throws<ArgumentException>(()=>buffer.StageCompliantContacts([a with {EpisodeCount=1},b]));
        buffer.Discard();
        var absent=Buffer(bodies);using var lease=absent.Acquire();
        Assert.Throws<ArgumentException>(()=>lease.ReadCompliantContact(PoseSample.Current,a.Key));
        Assert.Throws<InvalidOperationException>(()=>absent.RegisterCompliantContacts([a]));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    public void WarmedCompletePublicationAndCopyAllocateNothing(int count)
    {
        var bodies=Bodies(count);var values=Enumerable.Range(1,count).Select(i=>Value(i)).ToArray();
        var target=new CompliantContactState[count];var buffer=Buffer(bodies);buffer.RegisterCompliantContacts(values);
        void Publish(long revision)
        {
            buffer.BeginWrite(0);buffer.StageCompliantContacts(values);
            buffer.Stage(Stamp(revision),bodies,[],[],[],[],[]);buffer.Publish();
            using var read=buffer.Acquire();read.CopyCompliantContacts(PoseSample.Current,target);
        }
        for(var i=1;i<=100;i++)Publish(i);
        var bytes=GC.GetAllocatedBytesForCurrentThread();
        for(var i=101;i<=1100;i++)Publish(i);
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-bytes);
        Assert.Equal(values,target);
    }

    [Fact]
    public void ContactValidationUsesTheSamePendingBodySnapshot()
    {
        var bodies=Bodies(1);var buffer=Buffer(bodies);var contact=Value();
        buffer.RegisterCompliantContacts([contact]);buffer.BeginWrite(0);
        buffer.StageCompliantContacts([contact]);
        var previous=bodies[1].Pose;
        bodies[1]=bodies[1] with {Pose=new(previous.Id,PhysicsMotionType.Static,previous.Pose,previous.ReferencePose)};
        Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(1),bodies,[],[],[],[],[]));
        buffer.Discard();using var read=buffer.Acquire();
        Assert.Equal(PhysicsMotionType.Dynamic,read.Read(PoseSample.Current,1).MotionType);
        Assert.Equal(contact,read.ReadCompliantContact(PoseSample.Current,contact.Key));
    }
}
