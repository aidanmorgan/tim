using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class RuntimeQueryOwnershipTests(NativeSceneFixture godot)
{
    private enum Fixture { Ball, Wall }
    private static string Wire(Fixture fixture)=>fixture switch
    {
        Fixture.Ball=>"ball", Fixture.Wall=>"wall",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        return world;
    }
    private static MachinePart Add(MachineWorld world,Fixture fixture)
    {
        var wire=Wire(fixture);
        return world.AddPart(new(){Id=wire,Kind=wire,Position=[0,5,0]});
    }
    private static readonly Vector3 Origin=new(-4,5,0);
    private static WorldSweepResult Sweep(MachineWorld world)=>
        WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(Origin)),SceneGeometryAdapter.CaptureVector(Vector3.Right*8));
    private static float Trace(MachineWorld world,TraceMedium medium)=>
        WorldGeometry.Trace(medium,world,Origin,Vector3.Right,8,null);

    [Fact]
    public void QueriesReadSolvedBodiesBeforePresentationAndWhilePaused()
    {
        var world=World();
        try
        {
            var ball=Add(world,Fixture.Ball);
            ball.InitialVelocity=Vector3.Right;
            world.Start();
            var snapshot=WorldGeometry.CaptureSweep(world,null,null,SweepBodyMode.IncludeBodies);
            var first=Sweep(world);
            world.Physics.Step([],[],.5); // Advance owned state without scene presentation.
            var body=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            Assert.InRange(Math.Abs(body.Center.X-.5),0,1e-12);
            Assert.Equal(0,ball.Position.X);
            ball.Position=new(12,9,2);
            ball.Scale=new(2,1,1); // Even invalid presentation cannot become a query pose.
            ball.Visible=false; // Participation, not presentation visibility, owns live collision.
            world.Running=false;
            Assert.Equal(first,snapshot.Sweep(new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(Origin)),SceneGeometryAdapter.CaptureVector(Vector3.Right*8)));
            QueryAssertions.Owner(world,ball,Sweep(world).Body);
            Assert.InRange(Sweep(world).Distance-first.Distance,.4999f,.5001f);
            foreach(var medium in new[]{TraceMedium.Light,TraceMedium.Sound,TraceMedium.Air})
                Assert.InRange(Trace(world,medium),4.5f-ball.Radius-.0001f,4.5f-ball.Radius+.0001f);
            var captured=Assert.Single(WorldGeometry.CaptureBodies(world),b=>b.Owner==ball);
            Assert.Equal(body.Pose,captured.Pose);
            Assert.Equal(WorldSweepStatus.Clear,WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(Origin)),SceneGeometryAdapter.CaptureVector(Vector3.Right*8),ignoredBody:ball).Status);
            world.Restore();
            var restored=world.Parts.OfType<BallPart>().Single();
            Assert.Equal(new Vector3(0,5,0),restored.Position);
            Assert.Equal(Vector3.One,restored.Scale);
            Assert.True(restored.Visible);
            Assert.InRange(Sweep(world).Distance-first.Distance,-.0001f,.0001f);
        }
        finally {world.Free();}
    }

    [Fact]
    public void DisabledColliderAndItsRestoredSnapshotControlAllQueryMedia()
    {
        var world=World();
        try
        {
            var ball=Add(world,Fixture.Ball);
            world.Start();
            var body=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            var saved=world.Physics.Capture();
            var first=Sweep(world);
            var collider=world.Physics.Collider(body.Id).Declaration;
            world.Physics.ApplyColliderUpdates([new(body.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
            Assert.True(ball.Visible);
            Assert.Equal(WorldSweepStatus.Clear,Sweep(world).Status);
            foreach(var medium in new[]{TraceMedium.Light,TraceMedium.Sound,TraceMedium.Air})
                Assert.Equal(8,Trace(world,medium));
            Assert.DoesNotContain(WorldGeometry.CaptureBodies(world),b=>b.Owner==ball);
            world.Physics.Restore(saved);
            Assert.Equal(first,Sweep(world));
            Assert.Single(WorldGeometry.CaptureBodies(world),b=>b.Owner==ball);
        }
        finally {world.Free();}
    }

    [Fact]
    public void ExplicitReplacementCommitsShapeAndMediaTogetherAndRollbackRestoresBoth()
    {
        var world=World();
        try
        {
            var wall=Add(world,Fixture.Wall);
            wall.Boxes.Clear(); wall.Boxes.Add(new(default,Vector3.One,MachinePart.RootBody));
            world.Start();
            var saved=world.Physics.Capture();
            var old=WorldGeometry.CaptureSweep(world,null,null,SweepBodyMode.IncludeBodies);
            var first=Sweep(world);
            var key=new SceneBodyKey(wall,MachinePart.RootBody);
            var body=world.PhysicsAssembly.Body(key);
            var material=world.Physics.Collider(body.Id).Declaration.Material;
            wall.Boxes[0]=new(default,new(2,1,1),MachinePart.RootBody,false);
            Assert.Equal(first,Sweep(world)); // Merely editing the declaration has no runtime effect.
            var geometry=world.CollisionGeometry(key);
            var child=geometry[new(0)];
            var replacement=geometry.WithChild(new(0),child with
            {
                Shape=new(new ConvexBox(new(2,1,1)),AffineTransform.Identity),Opaque=false
            });
            // Neither invalid presentation nor mutable construction proxies are runtime inputs.
            wall.Scale=new(2,1,1);
            world.ReplaceCollisionGeometry(key,replacement,material,CollisionParticipation.Enabled);
            Assert.InRange(Sweep(world).Distance,1.8999f,1.9001f);
            Assert.InRange(Trace(world,TraceMedium.Air),1.9999f,2.0001f);
            Assert.Equal(8,Trace(world,TraceMedium.Light));
            Assert.Equal(8,Trace(world,TraceMedium.Sound));
            Assert.Equal(first,old.Sweep(new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(Origin)),SceneGeometryAdapter.CaptureVector(Vector3.Right*8)));
            var replaced=world.Physics.Capture();
            world.Physics.Restore(saved);
            Assert.Equal(first,Sweep(world));
            Assert.InRange(Trace(world,TraceMedium.Light),2.9999f,3.0001f);
            world.Physics.Restore(replaced);
            Assert.InRange(Sweep(world).Distance,1.8999f,1.9001f);
            Assert.Equal(8,Trace(world,TraceMedium.Light));
        }
        finally {world.Free();}
    }

    [Fact]
    public void InvalidReplacementLeavesCommittedQueriesUnchanged()
    {
        var world=World();
        try
        {
            var wall=Add(world,Fixture.Wall);
            world.Start();
            var key=new SceneBodyKey(wall,MachinePart.RootBody);
            var body=world.PhysicsAssembly.Body(key);
            var before=world.Physics.Collider(body.Id);
            var hit=Sweep(world);
            Assert.Throws<ArgumentOutOfRangeException>(()=>world.ReplaceCollisionGeometry(
                key,world.CollisionGeometry(key),before.Declaration.Material,(CollisionParticipation)999));
            Assert.Equal(before,world.Physics.Collider(body.Id));
            var geometry=world.CollisionGeometry(key);
            Assert.Throws<ArgumentOutOfRangeException>(()=>world.ReplaceCollisionGeometry(
                key,geometry.WithChild(new(0),geometry[new(0)] with {Surface=(SweepSurfaceKind)999}),
                before.Declaration.Material,CollisionParticipation.Enabled));
            var wrongSlot=new BodyColliderGeometry(new(body.Id.Index+1),geometry.Value);
            Assert.Throws<ArgumentException>(()=>world.ReplaceCollisionGeometry(
                key,wrongSlot,before.Declaration.Material,CollisionParticipation.Enabled));
            Assert.Throws<ArgumentNullException>(()=>world.ReplaceCollisionGeometry(
                key,null!,before.Declaration.Material,CollisionParticipation.Enabled));
            Assert.Equal(before,world.Physics.Collider(body.Id));
            Assert.Equal(hit,Sweep(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void UnregisteredLowLevelGeometryRejectsRatherThanRebuildingFromScene()
    {
        var world=World();
        try
        {
            var wall=Add(world,Fixture.Wall);
            world.Start();
            var body=world.PhysicsAssembly.Body(new(wall,MachinePart.RootBody));
            var collider=world.Physics.Collider(body.Id).Declaration;
            var saved=world.Physics.Capture();
            var unsupported=new CompoundGeometry([new(new ConvexSphere(1),AffineTransform.Identity)]);
            world.Physics.ApplyColliderUpdates([new(body.Id,unsupported,collider.Material,CollisionParticipation.Enabled)]);
            Assert.Throws<InvalidOperationException>(()=>Sweep(world));
            Assert.Throws<InvalidOperationException>(()=>Trace(world,TraceMedium.Light));
            world.Physics.Restore(saved);
            QueryAssertions.Owner(world,wall,Sweep(world).Body);
        }
        finally {world.Free();}
    }


    [Fact]
    public void CapturedNumericSweepRemainsUsableAfterSceneOwnerIsFreed()
    {
        WorldSweepSnapshot snapshot;
        WorldSweepResult expected;
        var world=World();
        var moving=new CompoundGeometry([new(new ConvexSphere(.1),AffineTransform.Identity)]);
        var pose=RigidPose.At(new(-3,6,0));
        var travel=new CollisionVector(6,0,0);
        try
        {
            var wall=Add(world,Fixture.Wall);
            wall.Position=new(0,6,0);
            snapshot=WorldGeometry.CaptureSweep(world,null,null,SweepBodyMode.IncludeBodies);
            expected=snapshot.Sweep(moving,pose,travel);
            Assert.Equal(WorldSweepStatus.Contact,expected.Status);
            Assert.NotNull(expected.Body);
            wall.Boxes.Clear();wall.Position=new(100,100,100);
        }
        finally {world.Free();}
        Assert.Equal(expected,snapshot.Sweep(moving,pose,travel));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiveOrPausedWorldCannotRecaptureConstructionBodies(bool paused)
    {
        var world=World();
        try
        {
            Add(world,Fixture.Wall);
            Assert.True(WorldGeometry.CapturePhysicsBodies(world).Length>1);
            world.Start();world.Running=!paused;
            var physics=world.Physics;
            var before=physics.Capture().BodyStates.ToArray();
            Assert.Throws<InvalidOperationException>(()=>WorldGeometry.CapturePhysicsBodies(world));
            Assert.Throws<InvalidOperationException>(()=>WorldGeometry.CapturePhysicsAssembly(world,world.Ropes));
            Assert.Same(physics,world.Physics);
            Assert.Equal(before,physics.Capture().BodyStates.ToArray());
            Assert.NotEmpty(WorldGeometry.CaptureBodies(world));
            world.Restore();
            Assert.True(WorldGeometry.CapturePhysicsBodies(world).Length>1);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(CollisionParticipation.Enabled)]
    [InlineData(CollisionParticipation.Disabled)]
    public void PusherReplacementReadsOnlyOwnedGeometryAndPreservesMaterialParticipationAndSnapshots(CollisionParticipation participation)
    {
        var world=World();
        try
        {
            var id=FixtureParts.Id(FixturePartId.First);
            var pusher=(LinearPusherPart)world.AddPart(new(){Id=id,Kind=LinearPusherPart.CatalogId,Position=[0,6,0]});
            FixtureParts.ConfigureParameter(pusher,PusherParameter.Speed,1);
            FixtureParts.ConfigureParameter(pusher,PusherParameter.Acceleration,2);
            var construction=pusher.Transform;var boxes=pusher.Boxes.ToArray();
            world.Start();
            var key=new SceneBodyKey(pusher,MachinePart.RootBody);
            var root=world.PhysicsAssembly.Body(key);
            var head=world.PhysicsAssembly.Body(new(pusher,LinearPusherPart.HeadBody));
            var original=world.CollisionGeometry(key);
            var material=new ContactMaterial(.2,.25,.4);
            world.Physics.ApplyColliderUpdates([new(root.Id,original.Geometry,material,participation)]);
            var before=world.Physics.Capture();
            world.Physics.SetServoMode(world.PhysicsAssembly.JointId(new(pusher,LinearPusherPart.HeadGuide)),PhysicsServoMode.Upper);
            world.Physics.ApplyImpulse(head.Id,new(LinearPusherPart.HeadMass,0,0),head.Center);
            world.Physics.Step([],[],.8);
            Assert.InRange(Math.Abs(head.Center.X-(LinearPusherPart.RestHeadX+.8)),0,1e-10);
            var from=new Vector3(1.2f,6,-2);
            float Ray(TraceMedium medium)=>WorldGeometry.Trace(medium,world,from,Vector3.Back,4,null);
            foreach(var medium in Enum.GetValues<TraceMedium>())Assert.Equal(4,Ray(medium));
            var frozen=WorldGeometry.CaptureSweep(world,null,null,SweepBodyMode.IncludeBodies);
            var oldHit=frozen.Sweep(new CompoundGeometry([new(new ConvexSphere(.01f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(from)),SceneGeometryAdapter.CaptureVector(Vector3.Back*4));
            // Deliberately corrupt presentation and construction lists after capture.
            pusher.Position=new(12,9,3);pusher.Scale=new(2,1,1);pusher.Boxes.Clear();
            pusher.ObservePhysics(world,MachineWorld.Tick);
            var replaced=world.CollisionGeometry(key);
            Assert.NotSame(original,replaced);
            Assert.Empty(pusher.Boxes);
            var committed=world.Physics.Collider(root.Id);
            Assert.Equal(material,committed.Declaration.Material);
            Assert.Equal(participation,committed.Declaration.Participation);
            Assert.Same(replaced.Geometry,committed.Declaration.Geometry);
            Assert.Equal(original[new(0)],replaced[new(0)]);
            Assert.Equal(original[new(1)],replaced[new(1)]);
            foreach(var medium in Enum.GetValues<TraceMedium>())
                if(participation==CollisionParticipation.Enabled)Assert.InRange(Ray(medium),1.9399f,1.9401f);
                else Assert.Equal(4,Ray(medium));
            Assert.Equal(oldHit,frozen.Sweep(new CompoundGeometry([new(new ConvexSphere(.01f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(from)),SceneGeometryAdapter.CaptureVector(Vector3.Back*4)));
            var after=world.Physics.Capture();
            pusher.ObservePhysics(world,MachineWorld.Tick);
            Assert.Equal(committed,world.Physics.Collider(root.Id)); // No gratuitous revision or contact invalidation.
            world.Physics.Restore(before);
            Assert.Same(original,world.CollisionGeometry(key));
            var restoredCollider=world.Physics.Collider(root.Id);
            pusher.ObservePhysics(world,MachineWorld.Tick);
            Assert.Equal(restoredCollider,world.Physics.Collider(root.Id));
            world.Physics.Restore(after);
            Assert.Same(replaced,world.CollisionGeometry(key));
            Assert.Equal(committed,world.Physics.Collider(root.Id));
            world.Restore();
            pusher=(LinearPusherPart)world.FindPart(id)!;
            Assert.Equal(construction,pusher.Transform);Assert.Equal(boxes,pusher.Boxes.ToArray());
        }
        finally {world.Free();}
    }

    [Fact]
    public void OverlappingReplacementCannotPublishNewCollisionOrQueryMetadata()
    {
        var world=World();
        try
        {
            var wall=Add(world,Fixture.Wall);
            var ball=world.AddPart(new(){Id=Wire(Fixture.Ball),Kind=Wire(Fixture.Ball),Position=[3,5,0]});
            world.Start();
            var key=new SceneBodyKey(wall,MachinePart.RootBody);
            var root=world.PhysicsAssembly.Body(key);
            var original=world.CollisionGeometry(key);
            var collider=world.Physics.Collider(root.Id);
            var before=world.Physics.Capture().BodyStates.ToArray();
            var hit=Sweep(world);
            var replacement=original.WithChild(new(0),original[new(0)] with
            {
                Shape=new(new ConvexBox(new(4,2,1)),AffineTransform.Identity),Opaque=false
            });
            Assert.Throws<InvalidOperationException>(()=>world.ReplaceCollisionGeometry(
                key,replacement,collider.Declaration.Material,CollisionParticipation.Enabled));
            Assert.Same(original,world.CollisionGeometry(key));
            Assert.Equal(collider,world.Physics.Collider(root.Id));
            Assert.Equal(before,world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(hit,Sweep(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void FixtureMappingRejectsUnknownValues()
    {
        Assert.Equal("ball",Wire(Fixture.Ball));
        Assert.Equal("wall",Wire(Fixture.Wall));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Wire((Fixture)999));
    }
}
