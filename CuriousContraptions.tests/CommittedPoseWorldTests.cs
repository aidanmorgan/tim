using Godot;
using CuriousContraptions.Bridge;
namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class CommittedPoseWorldTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Ball=new("ball"),Battery=new("battery");
    private partial class Probe : BatteryPart
    {
        public Action? Observe;
        public override void ObservePhysics(MachineWorld world,float delta)=>Observe?.Invoke();
    }
    [Fact]
    public void LeavingSceneTreeRetiresOutstandingPoseLeases()
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Ball.Value,Position=[0,6,0]});
        world.Start();
        var lease=world.ReadCommittedPoses();
        world.Free();
        Assert.Throws<InvalidOperationException>(()=>lease.Read(PoseSample.Current,0));
        lease.Dispose();
    }

    [Fact]
    public void FailedTickCannotPublishAndSuccessfulTicksRetainAdjacentRevisions()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var ball=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Ball.Value,Position=[0,6,0],InitialVelocity=[1,0,0]});
            var probe=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            probe.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[4,6,0]});
            world.AttachPart(probe);
            world.Start();
            var id=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).Id;
            BodyPoseRead initial;
            using(var lease=world.ReadCommittedPoses())
            {
                initial=lease.Read(PoseSample.Current,id.Index);
                Assert.Equal(initial,lease.Read(PoseSample.Previous,id.Index));
                Assert.Throws<InvalidOperationException>(()=>world.Step());
                Assert.Equal(0,world.Ticks);
            }
            probe.Observe=()=>
            {
                Assert.Throws<InvalidOperationException>(()=>world.ReadCommittedPoses());
                throw new InvalidOperationException("Injected observer failure.");
            };
            Assert.Throws<InvalidOperationException>(()=>world.Step());
            using(var unchanged=world.ReadCommittedPoses())
            {
                Assert.Equal(0,unchanged.Stamp(PoseSample.Current).Revision.Value);
                Assert.Equal(initial,unchanged.Read(PoseSample.Current,id.Index));
            }
            var held=ball.Transform;
            var observations=0;
            probe.Observe=()=>
            {
                observations++;
                Assert.Equal(held,ball.Transform);
                Assert.True(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).Center.X>ball.Position.X);
            };
            world.Step();
            Assert.Equal(held,ball.Transform);
            world.PresentFrame(0,1);
            Assert.NotEqual(held,ball.Transform);
            held=ball.Transform;
            world.Step();
            Assert.Equal(MachineWorld.Substeps*2,observations);
            probe.Observe=null;
            using(var current=world.ReadCommittedPoses())
            {
                Assert.Equal(1,current.Stamp(PoseSample.Previous).Revision.Value);
                Assert.Equal(2,current.Stamp(PoseSample.Current).Revision.Value);
                Assert.Equal(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).Pose,current.Read(PoseSample.Current,id.Index).Pose);
            }
            // Render-frame consumer uses the committed pair; no new simulation revision.
            world._Process(0);
            var retired=world.ReadCommittedPoses();
            var generation=retired.Stamp(PoseSample.Current).Generation;
            world.Restore();
            Assert.Throws<InvalidOperationException>(()=>retired.Read(PoseSample.Current,id.Index));
            retired.Dispose();
            Assert.Throws<InvalidOperationException>(()=>world.ReadCommittedPoses());
            world.Start();
            using var reset=world.ReadCommittedPoses();
            Assert.Equal(generation.Value+1,reset.Stamp(PoseSample.Current).Generation.Value);
            Assert.Equal(0,reset.Stamp(PoseSample.Current).Revision.Value);
            Assert.Equal(reset.Read(PoseSample.Previous,id.Index),reset.Read(PoseSample.Current,id.Index));
        }
        finally {world.Free();}
    }

    [Fact]
    public void ColliderReplacementAndParticipationPublishAtomicallyAndResetReseeds()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var ball=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Ball.Value,Position=[0,6,0],InitialVelocity=[1,0,0]});
            var probe=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            probe.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[4,6,0]});
            world.AttachPart(probe);world.Start();
            var key=new SceneBodyKey(ball,MachinePart.RootBody);
            var body=world.PhysicsAssembly.Body(key);
            var original=world.Physics.Collider(body.Id);
            var originalGeometry=world.CollisionGeometry(key);
            var replaced=originalGeometry.WithChild(new(0),originalGeometry[new(0)] with
                {Shape=new(new Physics.ConvexSphere(.1),global::CuriousContraptions.Geometry.AffineTransform.Identity),Opaque=false});
            BodyQueryRead initial;
            BodyPoseRead initialPose;
            using(var read=world.ReadCommittedPoses())
            {
                initial=read.ReadQuery(PoseSample.Current,body.Id.Index);
                initialPose=read.Read(PoseSample.Current,body.Id.Index);
                Assert.Equal(body.Id,initial.Owner);
                Assert.NotNull(initial.Opaque);
            }
            var fail=true;
            probe.Observe=()=>
            {
                world.ReplaceCollisionGeometry(key,replaced,original.Declaration.Material,Physics.CollisionParticipation.Disabled);
                Assert.Throws<InvalidOperationException>(()=>world.ReadCommittedPoses());
                if(fail)throw new InvalidOperationException("Injected failure after collider replacement.");
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(original,world.Physics.Collider(body.Id));
            using(var unchanged=world.ReadCommittedPoses())
            {
                Assert.Equal(initial,unchanged.ReadQuery(PoseSample.Current,body.Id.Index));
                Assert.Equal(initialPose,unchanged.Read(PoseSample.Current,body.Id.Index));
            }
            fail=false;world.Step();
            using(var committed=world.ReadCommittedPoses())
            {
                Assert.Equal(initial,committed.ReadQuery(PoseSample.Previous,body.Id.Index));
                var current=committed.ReadQuery(PoseSample.Current,body.Id.Index);
                Assert.Same(replaced.Geometry,current.Solid);
                Assert.Null(current.Opaque);
                Assert.Equal(Physics.CollisionParticipation.Disabled,current.Participation);
                Assert.Equal(world.Physics.Collider(body.Id).Revision,current.Revision);
                Assert.True(current.Revision.Value>initial.Revision.Value);
                Assert.NotEqual(initialPose.Pose,committed.Read(PoseSample.Current,body.Id.Index).Pose);
            }
            var retired=world.ReadCommittedPoses();
            world.Restore();
            Assert.Throws<InvalidOperationException>(()=>retired.ReadQuery(PoseSample.Current,body.Id.Index));
            retired.Dispose();world.Start();
            var resetId=world.PhysicsAssembly.Body(new(world.FindPart(FixtureParts.Id(FixturePartId.First))!,MachinePart.RootBody)).Id;
            using var reset=world.ReadCommittedPoses();
            Assert.Equal(reset.ReadQuery(PoseSample.Previous,resetId.Index),reset.ReadQuery(PoseSample.Current,resetId.Index));
            Assert.Equal(Physics.CollisionParticipation.Enabled,reset.ReadQuery(PoseSample.Current,resetId.Index).Participation);
            Assert.NotNull(reset.ReadQuery(PoseSample.Current,resetId.Index).Opaque);
            Assert.Equal(0,reset.Stamp(PoseSample.Current).Revision.Value);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void MotionPublishesAllSubstepsAndFailedTickPreservesPriorHistory(int failingSubstep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            var ball=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Ball.Value,Position=[0,6,0],InitialVelocity=[1,0,0]});
            var probe=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            probe.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[4,6,0]});
            world.AttachPart(probe);world.Start();
            var id=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).Id;
            using(var seed=world.ReadCommittedPoses())
            {
                Assert.Equal(0,seed.MotionStepCount);
                Assert.Equal(seed.Read(PoseSample.Current,id.Index).Pose,seed.SampleAcceptedPose(id,0));
            }
            world.Step();
            Physics.PhysicsMotionHistory[] retained;
            using(var read=world.ReadCommittedPoses())
            {
                Assert.Equal(MachineWorld.Substeps,read.MotionStepCount);
                retained=Enumerable.Range(0,read.MotionStepCount).Select(read.ReadMotionStep).ToArray();
                for(var i=0;i<read.MotionStepCount;i++)
                {
                    var motion=read.ReadMotionStep(i);
                    var time=(motion.StartTime+motion.EndTime)*.5;
                    Assert.Equal(motion.Sample(id,time),read.SampleAcceptedPose(id,time));
                    Assert.InRange(Math.Abs(read.SampleAcceptedPose(id,time).Center.X-time),0,1e-9);
                    Assert.Equal((ulong)(i+1),motion.StepIndex);
                }
                Assert.Equal(read.Read(PoseSample.Current,id.Index).Pose,read.SampleAcceptedPose(id,MachineWorld.Tick));
            }
            var observed=0;
            probe.Observe=()=>
            {
                Assert.Throws<InvalidOperationException>(()=>world.ReadCommittedPoses());
                if(++observed==failingSubstep)throw new InvalidOperationException("Injected motion publication failure.");
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(1,world.Ticks);
            using(var unchanged=world.ReadCommittedPoses())
                for(var i=0;i<retained.Length;i++)Assert.Same(retained[i],unchanged.ReadMotionStep(i));
            probe.Observe=null;world.Step();
            var retired=world.ReadCommittedPoses();
            Assert.Equal(2,retired.Stamp(PoseSample.Current).Revision.Value);
            Assert.Equal((ulong)(MachineWorld.Substeps+1),retired.ReadMotionStep(0).StepIndex);
            Assert.Equal((double)MachineWorld.Tick,retired.ReadMotionStep(0).StartTime);
            world.Restore();
            Assert.Throws<InvalidOperationException>(()=>retired.ReadMotionStep(0));
            retired.Dispose();world.Start();
            using var reset=world.ReadCommittedPoses();Assert.Equal(0,reset.MotionStepCount);
        }
        finally {world.Free();}
    }
}
