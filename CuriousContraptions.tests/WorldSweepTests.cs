using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class WorldSweepTests(HeadlessFixture godot)
{
    private const string FirstId = "first";
    private const string SecondId = "second";
    private const string WallKind = "wall";
    private const string BallKind = "ball";
    private MachineWorld World()
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        return world;
    }
    private static MachinePart Part(MachineWorld world,string id,Vector3 at,bool ball=false)
    {
        var part=world.AddPart(new(){Id=id,Kind=ball?BallKind:WallKind,Position=[at.X,at.Y,at.Z]});
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
            var wall = Part(world, FirstId, new(0,5,0));
            wall.Boxes.Add(new(Vector3.Zero, Vector3.One));
            var snapshot = WorldGeometry.CaptureSweep(world, null, null, SweepBodyMode.ExcludeBodies);
            var origin = new Vector3(-3,5,0);
            var travel = Vector3.Right * 6;
            var before = snapshot.Sweep(origin, .25f, travel);
            Assert.Same(wall, before.Part);
            switch (mutation)
            {
                case SceneMutation.Move: wall.Position += Vector3.Back * 10; break;
                case SceneMutation.Hide: wall.Visible = false; break;
                case SceneMutation.RemoveProxy: wall.Boxes.Clear(); break;
                default: throw new ArgumentOutOfRangeException(nameof(mutation));
            }
            Assert.Equal(before, snapshot.Sweep(origin, .25f, travel));
            Assert.Equal(SphereSweepStatus.Clear,
                WorldGeometry.CaptureSweep(world, null, null, SweepBodyMode.ExcludeBodies)
                    .Sweep(origin, .25f, travel).Status);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void StaticQueryExcludesBodyButRetainsItsSolidProxies()
    {
        var world=World();
        try
        {
            var body=Part(world,FirstId,new(0,3,0),true);
            var wall=Part(world,SecondId,new(2,3,0));
            wall.Boxes.Add(new(Vector3.Zero,Vector3.One));
            var origin=new Vector3(-3,3,0);
            var all=WorldGeometry.Sweep(world,origin,.25f,Vector3.Right*8);
            Assert.Same(body,all.Part);
            Assert.Equal(SweepSurfaceKind.Body,all.Surface);
            var solids=WorldGeometry.Sweep(world,origin,.25f,Vector3.Right*8,
                bodies:SweepBodyMode.ExcludeBodies);
            Assert.Same(wall,solids.Part);
            Assert.Equal(SweepSurfaceKind.Box,solids.Surface);
            body.Spheres.Add(new(Vector3.Zero,.1f));
            var proxy=WorldGeometry.Sweep(world,origin,.25f,Vector3.Right*8,
                bodies:SweepBodyMode.ExcludeBodies);
            Assert.Same(body,proxy.Part);
            Assert.Equal(SweepSurfaceKind.Sphere,proxy.Surface);
            Assert.Throws<ArgumentOutOfRangeException>(()=>WorldGeometry.Sweep(
                world,origin,.25f,Vector3.Right,bodies:(SweepBodyMode)int.MaxValue));
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
            var wall=Part(world,FirstId,new(0,5,0));
            wall.RotationDegrees=new(0,0,degrees);
            wall.Boxes.Add(new(Vector3.Zero,Vector3.One));
            var origin=wall.Transform*new Vector3(.9f,0,0);
            var hit=WorldGeometry.Sweep(world,origin,.25f,Vector3.Zero);
            Assert.Equal(SphereSweepStatus.Overlapping,hit.Status);
            Assert.InRange(hit.Penetration,.3499f,.3501f);
            Assert.InRange(hit.Normal.DistanceTo(wall.Basis*Vector3.Right),0,.0001f);
            var separated=origin+hit.Normal*(hit.Penetration+SphereSweep.ContactTolerance);
            Assert.Equal(SphereSweepStatus.Clear,WorldGeometry.Sweep(
                world,separated,.25f,hit.Normal).Status);
        }
        finally{world.Free();}
    }

    [Fact]
    public void EmptyWorldStillHitsDeckAndFiniteBase()
    {
        var world=World();
        try
        {
            var hit=WorldGeometry.Sweep(world,new(0,3,0),.25f,Vector3.Down*5);
            Assert.Equal(SphereSweepStatus.Contact,hit.Status);
            Assert.Equal(SweepObstacleKind.Workbench,hit.Kind);Assert.Null(hit.Part);
            Assert.InRange(hit.Distance,3.2098f,3.2102f);Assert.Equal(Vector3.Up,hit.Normal);
            var miss=WorldGeometry.Sweep(world,new(10,3,0),.25f,Vector3.Down*5);
            Assert.Equal(SphereSweepStatus.Clear,miss.Status);
            Assert.Equal(SweepObstacleKind.None,miss.Kind);Assert.Null(miss.Part);
            Assert.Equal(5,miss.Distance);
            var side=WorldGeometry.Sweep(world,new(10,-.7f,0),.05f,Vector3.Left*3);
            Assert.Equal(SweepObstacleKind.Workbench,side.Kind);
            Assert.InRange(side.Distance,1.4498f,1.4502f);
        }
        finally{world.Free();}
    }

    [Fact]
    public void NearestPartWinsRegardlessOfCollectionOrder()
    {
        var world=World();
        try
        {
            var far=Part(world,FirstId,new(3,3,0));
            var near=Part(world,SecondId,new(0,3,0),true);
            far.Boxes.Add(new(Vector3.Zero,Vector3.One,false));
            foreach(var reverse in new[]{false,true})
            {
                if(reverse)world.Parts.Reverse();
                var hit=WorldGeometry.Sweep(world,new(-3,3,0),.25f,Vector3.Right*10);
                Assert.Same(near,hit.Part);Assert.Equal(SweepObstacleKind.Part,hit.Kind);
                Assert.InRange(hit.Distance,3-near.Radius-.2502f,3-near.Radius-.2498f);
            }
            near.Visible=false;
            Assert.Same(far,WorldGeometry.Sweep(world,new(-3,3,0),.25f,Vector3.Right*10).Part);
            near.Visible=true;
            Assert.Same(far,WorldGeometry.Sweep(world,new(-3,3,0),.25f,Vector3.Right*10,near).Part);
        }
        finally{world.Free();}
    }

    [Fact]
    public void EqualDistanceUsesStableIdsAndOverlapBeatsTouch()
    {
        var world=World();
        try
        {
            var first=Part(world,FirstId,new(0,3,0));
            var second=Part(world,SecondId,new(0,3,0));
            first.Spheres.Add(new(Vector3.Zero,1));second.Spheres.Add(new(Vector3.Zero,1));
            world.Parts.Reverse();
            Assert.Same(first,WorldGeometry.Sweep(world,new(-3,3,0),.25f,Vector3.Right*5).Part);
            second.Position=new(-.1f,3,0);
            var hit=WorldGeometry.Sweep(world,new(-1.25f,3,0),.25f,Vector3.Right);
            Assert.Equal(SphereSweepStatus.Overlapping,hit.Status);Assert.Same(second,hit.Part);
        }
        finally{world.Free();}
    }

    [Fact]
    public void IgnoringOwnerRemovesEveryProxyAndQueryDoesNotMoveCargo()
    {
        var world=World();
        try
        {
            var owner=Part(world,FirstId,new(0,3,0));
            owner.Boxes.Add(new(Vector3.Zero,Vector3.One));
            owner.Spheres.Add(new(Vector3.Zero,1));
            owner.Tubes.Add(new(Transform3D.Identity,2,.2f,1.5f,false));
            var cargo=Part(world,SecondId,new(3,3,0),true);
            cargo.Velocity=new(1,2,3);
            var beforePosition=cargo.Position;var beforeVelocity=cargo.Velocity;
            var overlap=WorldGeometry.Sweep(world,owner.Position,.25f,Vector3.Zero);
            Assert.Equal(SphereSweepStatus.Overlapping,overlap.Status);Assert.Same(owner,overlap.Part);
            var hit=WorldGeometry.Sweep(world,owner.Position,.25f,Vector3.Right*5,owner);
            Assert.Same(cargo,hit.Part);Assert.Equal(SphereSweepStatus.Contact,hit.Status);
            Assert.Equal(beforePosition,cargo.Position);Assert.Equal(beforeVelocity,cargo.Velocity);
        }
        finally{world.Free();}
    }

    [Fact]
    public void SeparatingTouchDoesNotHideAnotherObstacle()
    {
        var world=World();
        try
        {
            var behind=Part(world,FirstId,new(-1.25f,3,0));
            behind.Boxes.Add(new(Vector3.Zero,Vector3.One));
            var ahead=Part(world,SecondId,new(2,3,0));
            ahead.Boxes.Add(new(Vector3.Zero,new(.01f,1,1)));
            var hit=WorldGeometry.Sweep(world,new(0,3,0),.25f,Vector3.Right*4);
            Assert.Same(ahead,hit.Part);Assert.InRange(hit.Distance,1.7398f,1.7402f);
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
            var part=Part(world,FirstId,new(0,6,0));
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
            var clear=WorldGeometry.Sweep(world,combined*localOrigin,.25f,combined.Basis*direction*.1f);
            Assert.Equal(SphereSweepStatus.Clear,clear.Status);
            var hit=WorldGeometry.Sweep(world,combined*localOrigin,.25f,combined.Basis*direction*2);
            Assert.Same(part,hit.Part);Assert.Equal(SphereSweepStatus.Contact,hit.Status);
            Assert.InRange(hit.Distance,.749f,.751f);
            Assert.InRange(hit.Normal.DistanceTo(combined.Basis*-direction),0,.001f);
        }
        finally{world.Free();}
    }

    [Fact]
    public void RotatedTransparentBoxStillStopsPhysicalHead()
    {
        var world=World();
        try
        {
            var part=Part(world,FirstId,new(0,5,0));
            part.Boxes.Add(new(Vector3.Right,new(.001f,1,1),false));
            part.RotationDegrees=new(30,45,15);
            var hit=WorldGeometry.Sweep(world,part.Transform*new Vector3(-3,0,0),.25f,part.Basis*Vector3.Right*6);
            Assert.Same(part,hit.Part);Assert.InRange(hit.Distance,3.7488f,3.7492f);
        }
        finally{world.Free();}
    }

    [Fact]
    public void RejectsScaledProxiesAndInvalidEmptyWorldQueries()
    {
        var world=World();
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>WorldGeometry.Sweep(world,Vector3.Up,0,Vector3.Right));
            Assert.Throws<ArgumentOutOfRangeException>(()=>WorldGeometry.Sweep(world,Vector3.Up,1,new(float.NaN,0,0)));
            var part=Part(world,FirstId,Vector3.Up*3);
            part.Spheres.Add(new(Vector3.Zero,1));part.Scale=new(2,1,1);
            Assert.Throws<InvalidOperationException>(()=>WorldGeometry.Sweep(world,Vector3.Up*3,.25f,Vector3.Right));
        }
        finally{world.Free();}
    }
}
