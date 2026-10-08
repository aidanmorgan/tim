using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SceneBodyGeometryTests(NativeSceneFixture godot)
{
    private enum TestPart { Pipe, Lever, Ball, Wall }
    private MachineWorld World()
    {
        var world=new MachineWorld(); godot.Tree.Root.AddChild(world); return world;
    }
    private static MachinePart Add(MachineWorld world,TestPart kind,Vector3 at)
    {
        // Explicit current catalogue/instance serialization boundary.
        var wire=kind switch
        {
            TestPart.Pipe=>"pipe",
            TestPart.Lever=>ImpactLeverPart.CatalogId,
            TestPart.Ball=>"ball",
            TestPart.Wall=>"wall",
            _=>throw new ArgumentOutOfRangeException(nameof(kind))
        };
        return world.AddPart(new(){Id=wire,Kind=wire,Position=[at.X,at.Y,at.Z]});
    }
    private static SceneWorldBodyCapture Declared(MachineWorld world,MachinePart owner,BodySlot slot)=>
        WorldGeometry.CaptureBodies(world).Single(b=>b.Owner==owner&&b.Slot==slot);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RigidPartMotionReusesHollowGeometryAndKeepsOldQuerySnapshots(bool rotate)
    {
        var world=World();
        try
        {
            var pipe=Add(world,TestPart.Pipe,new(0,5,0));
            var before=Declared(world,pipe,MachinePart.RootBody);
            var snapshot=WorldGeometry.CaptureSweep(world,null,null,SweepBodyMode.IncludeBodies);
            var origin=new Vector3(0,8,0); var travel=Vector3.Down*6;
            var oldHit=snapshot.Sweep(new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(travel));
            QueryAssertions.Owner(world,pipe,oldHit.Body);
            var light=WorldGeometry.Trace(TraceMedium.Air,world,origin,Vector3.Down,6,null);
            pipe.Transform=new(rotate?new Basis(new Vector3(.3f,.6f,.7f).Normalized(),.7f):Basis.Identity,new(10,5,0));
            var after=Declared(world,pipe,MachinePart.RootBody);
            Assert.Same(before.Geometry,after.Geometry);
            // Scene capture composed with an identity body slot normalizes again.
            // Compare independent transform geometry here; captured snapshot equality below stays exact.
            Assert.Equal(SceneGeometryAdapter.CaptureVector(pipe.Position),after.Pose.Center);
            foreach(var point in new[]{Vector3.Zero,Vector3.Right,Vector3.Up,Vector3.Back})
                Assert.InRange((after.Pose.TransformPoint(SceneGeometryAdapter.CaptureVector(point))-
                    SceneGeometryAdapter.CaptureVector(pipe.Transform*point)).Length,0,2e-6);
            Assert.NotEqual(before.Pose,after.Pose);
            Assert.Equal(oldHit,snapshot.Sweep(new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(travel)));
            Assert.Equal(WorldSweepStatus.Clear,WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(travel)).Status);
            var transformedOrigin=pipe.Transform*new Vector3(0,3,0);
            var direction=pipe.Basis*Vector3.Down;
            var moved=WorldGeometry.Trace(TraceMedium.Air,world,transformedOrigin,direction,6,null);
            Assert.InRange(Math.Abs(moved-light),0,.0001f);
        }
        finally {world.Free();}
    }

    [Fact]
    public void HingeOwnsIndependentLocalGeometryAndPoseWithoutRebuildingFixedFixture()
    {
        var world=World();
        try
        {
            var lever=(ImpactLeverPart)Add(world,TestPart.Lever,new(0,5,0));
            var fixture=Declared(world,lever,MachinePart.RootBody);
            var beam=Declared(world,lever,ImpactLeverPart.BeamBody);
            Assert.Equal(AffineTransform.Identity,beam.Geometry[new(0)].Pose);
            Assert.Single(WorldGeometry.CaptureBodies(world),b=>b.Owner==lever&&b.Slot==ImpactLeverPart.BeamBody);
            var captured=WorldGeometry.CaptureSweep(world,null,null,SweepBodyMode.ExcludeDynamicBodies);
            var origin=new Vector3(1,6,0); var travel=Vector3.Down*2;
            var first=captured.Sweep(new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(travel));
            QueryAssertions.Owner(world,lever,first.Body);
            world.Gravity=0;world.Start();
            var body=world.PhysicsAssembly.Body(new(lever,ImpactLeverPart.BeamBody));
            // One radian/second about Z: angular impulse equals the actual inertia.
            world.Physics.ApplyImpulse(body.Id,new(0,body.LocalInertia.ZZ,0),body.Center+new CollisionVector(1,0,0));
            for(var i=0;i<24;i++)world.Step();
            var turned=Declared(world,lever,ImpactLeverPart.BeamBody);
            var fixedAfter=Declared(world,lever,MachinePart.RootBody);
            Assert.Same(beam.Geometry,turned.Geometry); Assert.Same(fixture.Geometry,fixedAfter.Geometry);
            Assert.Equal(fixture.Pose,fixedAfter.Pose); Assert.NotEqual(beam.Pose,turned.Pose);
            Assert.InRange((body.Pose.Center-turned.Pose.Center).Length,0,1e-6);
            Assert.InRange((body.Pose.Rotation.Inverse()*turned.Pose.Rotation).RotationVector().Length,0,1e-6);
            Assert.Equal(first,captured.Sweep(new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(travel)));
            var next=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(travel),bodies:SweepBodyMode.ExcludeDynamicBodies);
            QueryAssertions.Owner(world,lever,next.Body); Assert.True(next.Distance<first.Distance-.1f);
        }
        finally {world.Free();}
    }

    [Fact]
    public void DynamicEnvelopeAndAttachedProxiesShareOneRigidBody()
    {
        var world=World();
        try
        {
            var ball=Add(world,TestPart.Ball,new(0,5,0));
            ball.Boxes.Add(new(new(2,0,0),new(.1f,.5f,.5f), MachinePart.RootBody));
            var declarations=WorldGeometry.CaptureBodies(world).Where(b=>b.Owner==ball).ToArray();
            var declaration=Assert.Single(declarations);
            Assert.Equal(MachinePart.RootBody,declaration.Slot);
            Assert.Equal(2,declaration.Geometry.Count);
            var before=Declared(world,ball,MachinePart.RootBody);
            ball.Position+=Vector3.Back*2;
            var after=Declared(world,ball,MachinePart.RootBody);
            Assert.Same(before.Geometry,after.Geometry); Assert.NotEqual(before.Pose,after.Pose);
            var hit=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(-3,5,2))),SceneGeometryAdapter.CaptureVector(Vector3.Right*7),bodies:SweepBodyMode.ExcludeBodies);
            QueryAssertions.Owner(world,ball,hit.Body); Assert.Equal(SweepSurfaceKind.Box,hit.Surface);
            Assert.InRange(hit.Distance,4.799f,4.801f);
        }
        finally {world.Free();}
    }

    [Fact]
    public void GeometryEditsInvalidateDeclarationsButNeverMutateCapturedShapes()
    {
        var world=World();
        try
        {
            var wall=Add(world,TestPart.Wall,new(0,5,0));
            wall.Boxes.Clear(); wall.Boxes.Add(new(default,Vector3.One, MachinePart.RootBody));
            var before=Declared(world,wall,MachinePart.RootBody);
            var captured=WorldGeometry.CaptureSweep(world,null,null,SweepBodyMode.IncludeBodies);
            var first=captured.Sweep(new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(-4,5,0))),SceneGeometryAdapter.CaptureVector(Vector3.Right*8));
            wall.Boxes[0]=new(default,new(2,1,1), MachinePart.RootBody);
            var after=Declared(world,wall,MachinePart.RootBody);
            Assert.NotSame(before.Geometry,after.Geometry); Assert.Equal(before.Pose,after.Pose);
            Assert.Equal(first,captured.Sweep(new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(-4,5,0))),SceneGeometryAdapter.CaptureVector(Vector3.Right*8)));
            var next=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(-4,5,0))),SceneGeometryAdapter.CaptureVector(Vector3.Right*8));
            Assert.InRange(first.Distance-next.Distance,.9999f,1.0001f);
        }
        finally {world.Free();}
    }

    [Fact]
    public void CachedGeometryCannotHideAnInvalidNewOwnerTransform()
    {
        var world=World();
        try
        {
            var part=Add(world,TestPart.Wall,new(0,5,0));
            Declared(world,part,MachinePart.RootBody);
            part.Scale=new(2,1,1);
            Assert.Throws<ArgumentException>(()=>WorldGeometry.CaptureBodies(world));
            Assert.Throws<ArgumentException>(()=>WorldGeometry.Trace(TraceMedium.Air,world,new(-3,5,0),Vector3.Right,6,null));
        }
        finally {world.Free();}
    }

    [Fact]
    public void TransparentFixtureStillBlocksAirAndBodiesButNotLightOrSound()
    {
        var world=World();
        try
        {
            var part=Add(world,TestPart.Wall,new(0,5,0));
            part.Boxes.Clear(); part.Boxes.Add(new(default,Vector3.One, MachinePart.RootBody,false));
            foreach(var medium in new[]{TraceMedium.Light,TraceMedium.Sound})
                Assert.Equal(6,WorldGeometry.Trace(medium,world,new(-3,5,0),Vector3.Right,6,null));
            Assert.InRange(WorldGeometry.Trace(TraceMedium.Air,world,new(-3,5,0),Vector3.Right,6,null),1.999f,2.001f);
            QueryAssertions.Owner(world,part,WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(-3,5,0))),SceneGeometryAdapter.CaptureVector(Vector3.Right*6)).Body);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenericConvexProxyPreservesOwnershipMediaAndImmutableQuerySnapshots(bool opaque)
    {
        var world=World();
        try
        {
            var part=Add(world,TestPart.Wall,new(0,5,0));
            part.Boxes.Clear();
            part.ConvexShapes.Add(new(new(new ConvexBox(new(1,1,1)),AffineTransform.Identity),MachinePart.RootBody,opaque));
            var before=Declared(world,part,MachinePart.RootBody);
            var snapshot=WorldGeometry.CaptureSweep(world,null,null,SweepBodyMode.IncludeBodies);
            var origin=new Vector3(-4,5,0);var travel=Vector3.Right*8;
            var hit=snapshot.Sweep(new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(travel));
            QueryAssertions.Owner(world,part,hit.Body);Assert.Equal(SweepSurfaceKind.Convex,hit.Surface);
            Assert.InRange(hit.Distance,2.8999f,2.9001f);
            foreach(var medium in new[]{TraceMedium.Light,TraceMedium.Sound})
                Assert.InRange(WorldGeometry.Trace(medium,world,origin,Vector3.Right,8,null),
                    opaque?2.9999f:8,opaque?3.0001f:8);
            Assert.InRange(WorldGeometry.Trace(TraceMedium.Air,world,origin,Vector3.Right,8,null),2.9999f,3.0001f);
            part.ConvexShapes[0]=new(new(new ConvexBox(new(2,1,1)),AffineTransform.Identity),MachinePart.RootBody,opaque);
            Assert.NotSame(before.Geometry,Declared(world,part,MachinePart.RootBody).Geometry);
            Assert.Equal(hit,snapshot.Sweep(new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(travel)));
            Assert.InRange(WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.1f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(travel)).Distance,1.8999f,1.9001f);
            part.ConvexShapes[0]=new(default,MachinePart.RootBody);
            Assert.Throws<ArgumentNullException>(()=>WorldGeometry.CaptureBodies(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void BodyCaptureHasStableWorkbenchOwnershipAndIndependentReturnedArrays()
    {
        var world=World();
        try
        {
            var first=WorldGeometry.CaptureBodies(world);
            var bench=Assert.Single(first);
            Assert.Equal(Workbench.Body,bench.Slot); Assert.Null(bench.Owner);
            Assert.Equal(RigidPose.Identity,bench.Pose);
            first[0]=default;
            Assert.Equal(bench,Assert.Single(WorldGeometry.CaptureBodies(world)));
        }
        finally {world.Free();}
    }
}
