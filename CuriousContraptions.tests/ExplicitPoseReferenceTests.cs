using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
using CuriousContraptions.Presentation;
using Godot;
using System.Text.Json;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ExplicitPoseReferenceTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery"),Motor=new("motor");
    private static readonly PhysicsBodyId Source=new(17),Reference=new(3);
    private Node3D Target()
    {
        var node=new Node3D();godot.Tree.Root.AddChild(node);return node;
    }
    private static SceneAnimationTargetHandle Bind(SceneAnimationAdapter adapter,Node3D node)
    {
        var handle=adapter.Register(node);adapter.ClaimPhysicalPose(handle);return handle;
    }

    [Fact]
    public void ExplicitReferenceUsesTheSameCapturedSetAndNeverTheSourcesDefaultFrame()
    {
        var node=Target();
        try
        {
            var adapter=new SceneAnimationAdapter(1);var handle=Bind(adapter,node);
            var offset=RigidPose.At(new(0,.4,0));
            var mapping=ScenePoseMap.Rigid(PoseReadSpace.Relative,RigidPose.Identity);
            var batch=new ScenePoseBatch(adapter,[Source,Reference],
                [new(handle,Source,PoseReferenceBinding.Attached(Reference,offset),mapping)]);
            var parent=new RigidPose(new(12,4,-2),RigidRotation.FromRotationVector(new(.3,-.4,.5)));
            var local=RigidPose.At(new(.2,.7,-.1));
            var pose=parent.Compose(offset).Compose(local);
            BodyPoseRead[] reads=[
                new(Reference,PhysicsMotionType.Kinematic,parent,RigidPose.At(new(500,0,0))),
                new(Source,PhysicsMotionType.Dynamic,pose,RigidPose.At(new(-500,0,0)))];
            batch.Queue(reads);
            adapter.Present(1,0,0);
            Assert.InRange((node.Position-new Vector3(.2f,.7f,-.1f)).Length(),0,1e-6f);
            var expected=mapping.Evaluate(new(Source,PhysicsMotionType.Dynamic,pose,parent.Compose(offset)));
            Assert.Equal(expected,node.Transform);
            // The next source pose is unchanged; only the explicitly named reference moves.
            reads[0]=new(Reference,PhysicsMotionType.Kinematic,
                new(parent.Center+new CollisionVector(1,0,0),parent.Rotation),RigidPose.Identity);
            batch.Queue(reads);adapter.Present(2,0,0);
            Assert.NotEqual(expected,node.Transform);
        }
        finally {node.Free();}
    }

    [Fact]
    public void MissingOrDefaultReferencesRejectBeforeAWriteAndAttachedQueuesAllocateNothing()
    {
        var node=Target();
        try
        {
            var adapter=new SceneAnimationAdapter(1);var handle=Bind(adapter,node);
            var map=ScenePoseMap.Rigid(PoseReadSpace.Relative,RigidPose.Identity);
            Assert.Throws<ArgumentException>(()=>new ScenePoseBatch(adapter,[Source],
                [new(handle,Source,PoseReferenceBinding.Attached(Reference,RigidPose.Identity),map)]));
            Assert.Throws<ArgumentException>(()=>new ScenePoseBatch(adapter,[Source],
                [new(handle,Source,default,map)]));
            Assert.Equal(0,adapter.Present(1,0,0).TransformWrites);
            var batch=new ScenePoseBatch(adapter,[Source,Reference],
                [new(handle,Source,PoseReferenceBinding.Attached(Reference,RigidPose.Identity),map)]);
            BodyPoseRead[] reads=[
                new(Source,PhysicsMotionType.Dynamic,RigidPose.At(new(0,.7,0)),RigidPose.Identity),
                new(Reference,PhysicsMotionType.Static,RigidPose.Identity,RigidPose.Identity)];
            for(var i=0;i<100;i++)batch.Queue(reads);
            var bytes=GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<1000;i++)batch.Queue(reads);
            Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-bytes);
            adapter.Present(2,0,0);
            Assert.Equal(new Vector3(0,.7f,0),node.Position);
        }
        finally {node.Free();}
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(35f)]
    [InlineData(90f)]
    public void WoundSpringDeformationWaitsForCommittedPresentationAndRestoresExactly(float angle)
    {
        var world=new MachineWorld {Gravity=0};godot.Tree.Root.AddChild(world);
        try
        {
            var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[-5,3,2]});
            var motor=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Motor.Value,Position=[-3,3,2]});
            var spring=(WoundSpringPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),
                Kind=WoundSpringPart.CatalogId,Position=[0,4,0],Orientation=PartOrientation.FromEulerDegrees(0,0,angle)});
            Assert.True(world.Connect(battery,SocketId.Supply,motor,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(motor,SocketId.Drive,spring,SocketId.DriveIn,ConnectionDomain.Mechanical));
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            var assets=spring.Plunger.RootPoseAssets.ToArray();
            Assert.Equal(2,assets.Length);
            var initial=assets.Select(a=>a.Target.Transform).ToArray();
            world.Start();
            for(var tick=0;tick<120;tick++)world.Step();
            Assert.True(spring.Compression>.01f);
            spring._Process(.05);
            Assert.Equal(initial,assets.Select(a=>a.Target.Transform).ToArray());
            var head=world.PhysicsAssembly.Body(new(spring.Plunger,MachinePart.RootBody)).Id;
            var reference=world.PhysicsAssembly.Body(new(spring,MachinePart.RootBody)).Id;
            Transform3D[] expected;
            using(var read=world.ReadCommittedPoses())
            {
                var source=read.Read(PoseSample.Current,head.Index);
                var parent=read.Read(PoseSample.Current,reference.Index);
                expected=assets.Select(a=>a.Map.Evaluate(new(source.Id,source.MotionType,source.Pose,
                    parent.Pose.Compose(a.Reference.Offset)))).ToArray();
            }
            var physical=world.Physics.Capture().BodyStates.ToArray();
            world.PresentFrame(0,1);
            Assert.Equal(expected,assets.Select(a=>a.Target.Transform).ToArray());
            for(var i=0;i<assets.Length;i++)Assert.NotEqual(initial[i],assets[i].Target.Transform);
            Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            var restored=(WoundSpringPart)world.FindPart(FixtureParts.Id(FixturePartId.Third))!;
            Assert.Equal(initial,restored.Plunger.RootPoseAssets.Select(a=>a.Target.Transform).ToArray());
        }
        finally {world.Free();}
    }
}

