using Godot;
using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class CommittedActivityTests(NativeSceneFixture godot)
{
    private static readonly CompoundGeometry Shape=new([new(new ConvexSphere(1),AffineTransform.Identity)]);
    private static BodyPublicationRead Body(int id,int owner,OwnerActivity activity)=>new(
        new(new(id),PhysicsMotionType.Static,RigidPose.Identity,RigidPose.Identity),
        new(new(owner),new(0),CollisionParticipation.Enabled,Shape,Shape),activity,default);
    private static PoseReadStamp Stamp(long revision)=>new(new(1),new(revision),revision);

    [Fact]
    public void ActivityPublishesAtomicallyWithoutChangingColliderRevision()
    {
        var initial=new[]{Body(0,0,OwnerActivity.Inactive),Body(1,0,OwnerActivity.Inactive)};
        var next=new[]{Body(0,0,OwnerActivity.Active),Body(1,0,OwnerActivity.Active)};
        var buffer=new CommittedPoseBuffer(Stamp(0),initial,[],[],[],[],[]);
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),next,[],[],[],[],[]);buffer.Discard();
        using(var read=buffer.Acquire())Assert.Equal(OwnerActivity.Inactive,read.ReadActivity(PoseSample.Current,1));
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),next,[],[],[],[],[]);buffer.Publish();
        var lease=buffer.Acquire();
        for(var i=0;i<2;i++)
        {
            Assert.Equal(OwnerActivity.Inactive,lease.ReadActivity(PoseSample.Previous,i));
            Assert.Equal(OwnerActivity.Active,lease.ReadActivity(PoseSample.Current,i));
            Assert.Equal(lease.ReadQuery(PoseSample.Previous,i),lease.ReadQuery(PoseSample.Current,i));
        }
        Assert.Throws<ArgumentOutOfRangeException>(()=>lease.ReadActivity((PoseSample)999,0));
        buffer.Remove();
        Assert.Throws<InvalidOperationException>(()=>lease.ReadActivity(PoseSample.Current,0));
        lease.Dispose();
        Assert.Throws<InvalidOperationException>(()=>default(PoseReadLease).ReadActivity(PoseSample.Current,0));
    }

    [Fact]
    public void InvalidActivityAndInconsistentOwnerStateReject()
    {
        Assert.Throws<ArgumentException>(()=>new CommittedPoseBuffer(Stamp(0),[Body(0,0,(OwnerActivity)999)],[],[],[],[],[]));
        Assert.Throws<ArgumentException>(()=>new CommittedPoseBuffer(Stamp(0),
            [Body(0,0,OwnerActivity.Active),Body(1,0,OwnerActivity.Inactive)],[],[],[],[],[]));
        var buffer=new CommittedPoseBuffer(Stamp(0),[Body(0,0,OwnerActivity.Inactive)],[],[],[],[],[]);
        buffer.BeginWrite(0);
        Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(1),[Body(0,0,(OwnerActivity)999)],[],[],[],[],[]));
        buffer.Discard();
        using var read=buffer.Acquire();
        Assert.Equal(Stamp(0),read.Stamp(PoseSample.Current));
        Assert.Equal(OwnerActivity.Inactive,read.ReadActivity(PoseSample.Current,0));
    }

    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery");
    private partial class Probe : BatteryPart
    {
        public Action? Observe;
        public override void ObservePhysics(MachineWorld world,float delta)=>Observe?.Invoke();
    }
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void FailedTickRestoresActivityAndResetRetiresPublication(int failingSubstep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var probe=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            probe.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[0,6,0]});
            world.AttachPart(probe);
            var construction=System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            var id=world.PhysicsAssembly.Body(new(probe,MachinePart.RootBody)).Id;
            OwnerActivity initial;
            using(var read=world.ReadCommittedPoses())initial=read.ReadActivity(PoseSample.Current,id.Index);
            var active=initial!=OwnerActivity.Active;
            var calls=0;
            probe.Observe=()=>
            {
                probe.Active=active;
                if(++calls==failingSubstep)throw new InvalidOperationException("Injected activity failure.");
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(initial==OwnerActivity.Active,probe.Active);
            using(var read=world.ReadCommittedPoses())
            {
                Assert.Equal(Stamp(0).Revision,read.Stamp(PoseSample.Current).Revision);
                Assert.Equal(initial,read.ReadActivity(PoseSample.Current,id.Index));
            }
            probe.Observe=()=>probe.Active=active;
            world.Step();
            using(var read=world.ReadCommittedPoses())
            {
                Assert.Equal(initial,read.ReadActivity(PoseSample.Previous,id.Index));
                Assert.Equal(active?OwnerActivity.Active:OwnerActivity.Inactive,read.ReadActivity(PoseSample.Current,id.Index));
                probe.Active=!active;
                Assert.Equal(active?OwnerActivity.Active:OwnerActivity.Inactive,read.ReadActivity(PoseSample.Current,id.Index));
            }
            Assert.Equal(construction,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            var retired=world.ReadCommittedPoses();
            world.Restore();
            Assert.Throws<InvalidOperationException>(()=>retired.ReadActivity(PoseSample.Current,id.Index));
            retired.Dispose();
            Assert.Equal(construction,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.Start();
            using var reset=world.ReadCommittedPoses();
            Assert.Equal(0,reset.Stamp(PoseSample.Current).Revision.Value);
            Assert.Equal(initial,reset.ReadActivity(PoseSample.Current,id.Index));
        }
        finally {world.Free();}
    }
}
