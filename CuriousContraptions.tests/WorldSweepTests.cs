using CuriousContraptions.Physics;
using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WorldSweepTests(NativeSceneFixture godot)
{
    private enum TestPart { Wall, Ball }
    private MachineWorld World()
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        return world;
    }
    private static MachinePart Part(MachineWorld world,FixturePartId id,Vector3 at,TestPart kind=TestPart.Wall)
    {
        var part=world.AddPart(new(){Id=FixtureParts.Id(id),Kind=kind switch { TestPart.Wall=>"wall",TestPart.Ball=>"ball",_=>throw new ArgumentOutOfRangeException(nameof(kind)) },Position=[at.X,at.Y,at.Z]});
        part.Boxes.Clear();part.Spheres.Clear();part.Tubes.Clear();part.Bends.Clear();part.Frustums.Clear();
        return part;
    }

    public enum SceneMutation { Move, Hide, RemoveProxy }

    [Theory]
    [InlineData(SceneMutation.Move)]
    [InlineData(SceneMutation.Hide)]
    [InlineData(SceneMutation.RemoveProxy)]
    public void CapturedGeometryIsStableAndRecaptureReflectsSceneChanges(SceneMutation mutation)
    {
        var world = World();
        try
        {
            var wall = Part(world, FixturePartId.First, new(0,5,0));
            wall.Boxes.Add(new(Vector3.Zero, Vector3.One, MachinePart.RootBody));
            var snapshot = WorldGeometry.CaptureSweep(world, null, null, SweepBodyMode.ExcludeBodies);
            var origin = new Vector3(-3,5,0);
            var travel = Vector3.Right * 6;
            var before = snapshot.Sweep(new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(travel));
            QueryAssertions.Owner(world,wall, before.Body);
            switch (mutation)
            {
                case SceneMutation.Move: wall.Position += Vector3.Back * 10; break;
                case SceneMutation.Hide: wall.Visible = false; break;
                case SceneMutation.RemoveProxy: wall.Boxes.Clear(); break;
                default: throw new ArgumentOutOfRangeException(nameof(mutation));
            }
            Assert.Equal(before, snapshot.Sweep(new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(travel)));
            Assert.Equal(WorldSweepStatus.Clear,
                WorldGeometry.CaptureSweep(world, null, null, SweepBodyMode.ExcludeBodies)
                    .Sweep(new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(travel)).Status);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void StaticQueryExcludesBodyButRetainsItsSolidProxies()
    {
        var world=World();
        try
        {
            var body=Part(world,FixturePartId.First,new(0,3,0),TestPart.Ball);
            var wall=Part(world,FixturePartId.Second,new(2,3,0));
            wall.Boxes.Add(new(Vector3.Zero,Vector3.One, MachinePart.RootBody));
            var origin=new Vector3(-3,3,0);
            var all=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(Vector3.Right*8));
            QueryAssertions.Owner(world,body,all.Body);
            Assert.Equal(SweepSurfaceKind.Body,all.Surface);
            var solids=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(Vector3.Right*8),bodies:SweepBodyMode.ExcludeBodies);
            QueryAssertions.Owner(world,wall,solids.Body);
            Assert.Equal(SweepSurfaceKind.Box,solids.Surface);
            body.Spheres.Add(new(Vector3.Zero,.1f,MachinePart.RootBody));
            var proxy=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(Vector3.Right*8),bodies:SweepBodyMode.ExcludeBodies);
            QueryAssertions.Owner(world,body,proxy.Body);
            Assert.Equal(SweepSurfaceKind.Sphere,proxy.Surface);
            Assert.Throws<ArgumentOutOfRangeException>(()=>WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(Vector3.Right),bodies:(SweepBodyMode)int.MaxValue));
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(63f)]
    public void OverlapDepthSupportsSeparationInRotatedWorldSpace(float degrees)
    {
        var world=World();
        try
        {
            var wall=Part(world,FixturePartId.First,new(0,5,0));
            wall.RotationDegrees=new(0,0,degrees);
            wall.Boxes.Add(new(Vector3.Zero,Vector3.One, MachinePart.RootBody));
            var origin=wall.Transform*new Vector3(.9f,0,0);
            var hit=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(origin)),SceneGeometryAdapter.CaptureVector(Vector3.Zero));
            Assert.Equal(WorldSweepStatus.Overlapping,hit.Status);
            Assert.InRange(hit.Penetration,.3499f,.3501f);
            Assert.InRange((hit.Normal-SceneGeometryAdapter.CaptureVector(wall.Basis*Vector3.Right)).Length,0,.0001f);
            var separated=SceneGeometryAdapter.CaptureVector(origin)+hit.Normal*(hit.Penetration+ConvexSweep.ContactDistance);
            Assert.Equal(WorldSweepStatus.Clear,WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(separated),hit.Normal).Status);
        }
        finally{world.Free();}
    }

    [Fact]
    public void EmptyWorldStillHitsDeckAndFiniteBase()
    {
        var world=World();
        try
        {
            var hit=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(0,3,0))),SceneGeometryAdapter.CaptureVector(Vector3.Down*5));
            Assert.Equal(WorldSweepStatus.Contact,hit.Status);
            Assert.Equal(SweepObstacleKind.Workbench,hit.Kind);Assert.Equal(new PhysicsBodyId(0),hit.Body);
            Assert.InRange(hit.Distance,3.2098f,3.2102f);Assert.Equal(SceneGeometryAdapter.CaptureVector(Vector3.Up),hit.Normal);
            var miss=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(10,3,0))),SceneGeometryAdapter.CaptureVector(Vector3.Down*5));
            Assert.Equal(WorldSweepStatus.Clear,miss.Status);
            Assert.Equal(SweepObstacleKind.None,miss.Kind);Assert.Null(miss.Body);
            Assert.Equal(5,miss.Distance);
            var side=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.05f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(10,-.7f,0))),SceneGeometryAdapter.CaptureVector(Vector3.Left*3));
            Assert.Equal(SweepObstacleKind.Workbench,side.Kind);
            Assert.InRange(side.Distance,1.4498f,1.4502f);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NearestPartWinsRegardlessOfCollectionOrder(bool reverse)
    {
        var world=World();
        try
        {
            var far=Part(world,reverse?FixturePartId.Second:FixturePartId.First,new(3,3,0));
            var near=Part(world,reverse?FixturePartId.First:FixturePartId.Second,new(0,3,0),TestPart.Ball);
            far.Boxes.Add(new(Vector3.Zero,Vector3.One, MachinePart.RootBody,false));
            var hit=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(-3,3,0))),SceneGeometryAdapter.CaptureVector(Vector3.Right*10));
            QueryAssertions.Owner(world,near,hit.Body);Assert.Equal(SweepObstacleKind.Part,hit.Kind);
            Assert.InRange(hit.Distance,3-near.Radius-.2502f,3-near.Radius-.2498f);
            near.Visible=false;
            QueryAssertions.Owner(world,far,WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(-3,3,0))),SceneGeometryAdapter.CaptureVector(Vector3.Right*10)).Body);
            near.Visible=true;
            QueryAssertions.Owner(world,far,WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(-3,3,0))),SceneGeometryAdapter.CaptureVector(Vector3.Right*10),near).Body);
        }
        finally{world.Free();}
    }

    [Fact]
    public void EqualDistanceUsesStableIdsAndOverlapBeatsTouch()
    {
        var world=World();
        try
        {
            var second=Part(world,FixturePartId.Second,new(0,3,0));
            var first=Part(world,FixturePartId.First,new(0,3,0));
            first.Spheres.Add(new(Vector3.Zero,1,MachinePart.RootBody));second.Spheres.Add(new(Vector3.Zero,1,MachinePart.RootBody));
            QueryAssertions.Owner(world,first,WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(-3,3,0))),SceneGeometryAdapter.CaptureVector(Vector3.Right*5)).Body);
            second.Position=new(-.1f,3,0);
            var hit=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(-1.25f,3,0))),SceneGeometryAdapter.CaptureVector(Vector3.Right));
            Assert.Equal(WorldSweepStatus.Overlapping,hit.Status);QueryAssertions.Owner(world,second,hit.Body);
        }
        finally{world.Free();}
    }

    [Fact]
    public void IgnoringOwnerRemovesEveryProxyAndQueryDoesNotMoveCargo()
    {
        var world=World();
        try
        {
            var owner=Part(world,FixturePartId.First,new(0,3,0));
            owner.Boxes.Add(new(Vector3.Zero,Vector3.One, MachinePart.RootBody));
            owner.Spheres.Add(new(Vector3.Zero,1,MachinePart.RootBody));
            owner.Tubes.Add(new(Transform3D.Identity,2,.2f,1.5f,false));
            var cargo=Part(world,FixturePartId.Second,new(3,3,0),TestPart.Ball);
            cargo.InitialVelocity=new(1,2,3);
            var beforePosition=cargo.Position;var beforeVelocity=cargo.InitialVelocity;
            var overlap=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(owner.Position)),SceneGeometryAdapter.CaptureVector(Vector3.Zero));
            Assert.Equal(WorldSweepStatus.Overlapping,overlap.Status);QueryAssertions.Owner(world,owner,overlap.Body);
            var hit=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(owner.Position)),SceneGeometryAdapter.CaptureVector(Vector3.Right*5),owner);
            QueryAssertions.Owner(world,cargo,hit.Body);Assert.Equal(WorldSweepStatus.Contact,hit.Status);
            Assert.Equal(beforePosition,cargo.Position);Assert.Equal(beforeVelocity,cargo.InitialVelocity);
        }
        finally{world.Free();}
    }

    [Fact]
    public void SeparatingTouchDoesNotHideAnotherObstacle()
    {
        var world=World();
        try
        {
            var behind=Part(world,FixturePartId.First,new(-1.25f,3,0));
            behind.Boxes.Add(new(Vector3.Zero,Vector3.One, MachinePart.RootBody));
            var ahead=Part(world,FixturePartId.Second,new(2,3,0));
            ahead.Boxes.Add(new(Vector3.Zero,new(.01f,1,1), MachinePart.RootBody));
            var hit=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(new Vector3(0,3,0))),SceneGeometryAdapter.CaptureVector(Vector3.Right*4));
            QueryAssertions.Owner(world,ahead,hit.Body);Assert.InRange(hit.Distance,1.7398f,1.7402f);
        }
        finally{world.Free();}
    }

    public enum HollowShape { Tube, Bend, Frustum }
    [Theory]
    [InlineData(HollowShape.Tube)]
    [InlineData(HollowShape.Bend)]
    [InlineData(HollowShape.Frustum)]
    public void HollowProxiesComposePartAndProxyPoses(HollowShape shape)
    {
        var world=World();
        try
        {
            var part=Part(world,FixturePartId.First,new(0,6,0));
            part.RotationDegrees=new(20,40,10);
            var pose=new Transform3D(new Basis(Vector3.Right,.7f),new(0,1,0));
            var localOrigin=Vector3.Zero;
            var direction=Vector3.Back;
            switch(shape)
            {
                case HollowShape.Tube:
                    part.Tubes.Add(new(pose,2,1,1.1f,false));break;
                case HollowShape.Bend:
                    var bend=new BendProxy(pose,2,Mathf.Pi/2,1,1.1f);
                    part.Bends.Add(bend);localOrigin=bend.Centre(Mathf.Pi/4);break;
                case HollowShape.Frustum:
                    part.Frustums.Add(new(pose,2,1,1,.1f));break;
                default:throw new ArgumentOutOfRangeException(nameof(shape));
            }
            var combined=part.Transform*pose;
            var clear=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(combined*localOrigin)),SceneGeometryAdapter.CaptureVector(combined.Basis*direction*.1f));
            Assert.Equal(WorldSweepStatus.Clear,clear.Status);
            var hit=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(combined*localOrigin)),SceneGeometryAdapter.CaptureVector(combined.Basis*direction*2));
            QueryAssertions.Owner(world,part,hit.Body);Assert.Equal(WorldSweepStatus.Contact,hit.Status);
            // The compiled shell's declared Hausdorff/bore error is 0.005;
            // this radial ray reaches the unit bore normally. A faceted normal
            // is not the analytic cylinder normal, but must rotate covariantly.
            const float shellError=.005f;
            Assert.InRange(hit.Distance,.75f-shellError-.0001f,.75f+.0001f);
            var rotation=part.Basis;
            part.RotationDegrees=Vector3.Zero;
            var referencePose=part.Transform*pose;
            var reference=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(referencePose*localOrigin)),SceneGeometryAdapter.CaptureVector(referencePose.Basis*direction*2));
            Assert.InRange(Math.Abs(hit.Distance-reference.Distance),0,.0001f);
            Assert.InRange((hit.Normal-SceneGeometryAdapter.CaptureRigidPose(new(rotation,Vector3.Zero)).Rotation.Apply(reference.Normal)).Length,0,.001f);
        }
        finally{world.Free();}
    }

    [Fact]
    public void RotatedTransparentBoxStillStopsPhysicalHead()
    {
        var world=World();
        try
        {
            var part=Part(world,FixturePartId.First,new(0,5,0));
            part.Boxes.Add(new(Vector3.Right,new(.001f,1,1), MachinePart.RootBody,false));
            part.RotationDegrees=new(30,45,15);
            var hit=WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(part.Transform*new Vector3(-3,0,0))),SceneGeometryAdapter.CaptureVector(part.Basis*Vector3.Right*6));
            QueryAssertions.Owner(world,part,hit.Body);Assert.InRange(hit.Distance,3.7488f,3.7492f);
        }
        finally{world.Free();}
    }

    [Fact]
    public void RejectsScaledProxiesAndInvalidEmptyWorldQueries()
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(0),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(Vector3.Up)),SceneGeometryAdapter.CaptureVector(Vector3.Right)));
            Assert.Throws<ArgumentOutOfRangeException>(()=>WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(1),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(Vector3.Up)),SceneGeometryAdapter.CaptureVector(new Vector3(float.NaN,0,0))));
            var part=Part(world,FixturePartId.First,Vector3.Up*3);
            part.Spheres.Add(new(Vector3.Zero,1,MachinePart.RootBody));part.Scale=new(2,1,1);
            Assert.Throws<ArgumentException>(()=>WorldGeometry.Sweep(world,new CompoundGeometry([new(new ConvexSphere(.25f),AffineTransform.Identity)]),RigidPose.At(SceneGeometryAdapter.CaptureVector(Vector3.Up*3)),SceneGeometryAdapter.CaptureVector(Vector3.Right)));
        }
        finally{world.Free();}
    }
}
