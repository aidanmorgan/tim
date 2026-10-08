using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class CompoundWorldSweepTests(NativeSceneFixture godot)
{
    public enum ProbeShape { Box, Hull, Compound }

    private static CompoundGeometry Geometry(ProbeShape shape)=>shape switch
    {
        ProbeShape.Box=>new([new(new ConvexBox(new(.25,.5,.125)),AffineTransform.Identity)]),
        ProbeShape.Hull=>new([new(new ConvexHull([
            new(-.25,-.5,-.125),new(.25,-.5,-.125),new(0,.5,-.125),
            new(-.25,-.5,.125),new(.25,-.5,.125),new(0,.5,.125)]),AffineTransform.Identity)]),
        ProbeShape.Compound=>new([
            new(new ConvexBox(new(.25,.125,.125)),new(AffineBasis.Identity,new(.75f,0,0))),
            new(new ConvexSphere(.125),new(AffineBasis.Identity,new(-.5f,0,0)))]),
        _=>throw new ArgumentOutOfRangeException(nameof(shape))
    };

    [Theory]
    [InlineData(ProbeShape.Box,0)]
    [InlineData(ProbeShape.Box,90)]
    [InlineData(ProbeShape.Hull,0)]
    [InlineData(ProbeShape.Hull,90)]
    [InlineData(ProbeShape.Compound,0)]
    [InlineData(ProbeShape.Compound,90)]
    public void UsesEveryMovingChildAndItsRigidOrientation(ProbeShape shape,int degrees)
    {
        using var scene=new GeometryQueryScene();
        var wall=scene.Part;
        wall.Position=new(3,5,0);
        wall.Boxes.Add(new(Vector3.Zero,new(.125f,2,2),MachinePart.RootBody));
        var geometry=Geometry(shape);
        var pose=SceneGeometryAdapter.CaptureRigidPose(new(new Basis(Vector3.Up,Mathf.DegToRad(degrees)),new(0,5,0)));
        var front=double.NegativeInfinity;
        for(var i=0;i<geometry.Count;i++)
        {
            var child=geometry[new(i)];
            var local=SceneGeometryAdapter.CaptureRigidPose(child.Pose.ToScene());
            var rotation=pose.Rotation*local.Rotation;
            var support=child.Geometry.Support(rotation.Inverse().Apply(new(1,0,0)));
            front=Math.Max(front,pose.TransformPoint(local.TransformPoint(support)).X);
        }
        var hit=WorldGeometry.Sweep(scene.World,geometry,pose,new(6,0,0));
        Assert.Equal(WorldSweepStatus.Contact,hit.Status);
        QueryAssertions.Owner(scene.World,wall,hit.Body);
        Assert.InRange(Math.Abs(hit.Distance-(2.875-front)),0,1e-7);
        Assert.InRange((hit.Normal-new CollisionVector(-1,0,0)).Length,0,1e-7);
        Assert.Equal(WorldSweepStatus.Clear,WorldGeometry.Sweep(scene.World,geometry,pose,new(-6,0,0)).Status);
        Assert.Equal(WorldSweepStatus.Clear,WorldGeometry.Sweep(scene.World,geometry,
            new(pose.Center+new CollisionVector(0,4,0),pose.Rotation),new(6,0,0)).Status);
    }

    [Fact]
    public void InvalidQueryInputsRejectEvenWithoutObstacles()
    {
        using var scene=new GeometryQueryScene();
        var snapshot=WorldGeometry.CaptureSweep(scene.World,null,null,SweepBodyMode.IncludeBodies);
        var geometry=Geometry(ProbeShape.Box);
        Assert.Throws<ArgumentNullException>(()=>snapshot.Sweep(null!,RigidPose.Identity,default));
        Assert.Throws<ArgumentException>(()=>snapshot.Sweep(geometry,default,default));
        Assert.Throws<ArgumentOutOfRangeException>(()=>snapshot.Sweep(geometry,RigidPose.Identity,new(double.NaN,0,0)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>snapshot.Sweep(geometry,RigidPose.Identity,new(double.MaxValue,0,0)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Geometry((ProbeShape)int.MaxValue));
    }

    [Fact]
    public void OffsetChildOverlapAndDoublePrecisionTravelAreRetained()
    {
        using var scene=new GeometryQueryScene();
        scene.Part.Position=new(3,5,0);
        scene.Part.Boxes.Add(new(Vector3.Zero,new(.125f,2,2),MachinePart.RootBody));
        var shape=Geometry(ProbeShape.Compound);
        var hit=WorldGeometry.Sweep(scene.World,shape,RigidPose.At(new(2.25,5,0)),default);
        Assert.Equal(WorldSweepStatus.Overlapping,hit.Status);
        Assert.Equal(0,hit.Distance);
        Assert.True(hit.Penetration>0);
        var length=1.0000000001;
        var clear=WorldGeometry.Sweep(scene.World,shape,RigidPose.At(new(0,10,0)),new(length,0,0));
        Assert.Equal(WorldSweepStatus.Clear,clear.Status);
        Assert.Equal(length,clear.Distance);
        Assert.NotEqual((double)(float)length,clear.Distance);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CannonChecksOwnedPayloadChildrenAndResetRestoresConstruction(bool obstructed)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        try
        {
            // Catalogue strings are confined to this authored-content boundary.
            var cannon=(CannonPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),
                Kind=CannonPart.CatalogId,Position=[0,5,0]});
            var ball=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind="ball",Position=[0,5,0]});
            var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind="battery",Position=[-5,1,0]});
            var wall=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Fourth),Kind="wall",Position=[.8f,5.44f,0]});
            wall.Boxes.Clear();
            wall.Boxes.Add(new(Vector3.Zero,new(.01f,.03f,.05f),MachinePart.RootBody));
            ball.Spheres.Add(new(new(0,obstructed?.39f:-.39f,0),.04f,MachinePart.RootBody));
            Assert.True(world.Connect(battery,SocketId.Supply,cannon,SocketId.PowerIn,ConnectionDomain.Electrical));
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            for(var i=0;i<200;i++)world.Step();
            var payload=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            var geometry=world.Physics.Collider(payload.Id).Declaration.Geometry;
            Assert.True(geometry.Count>1);
            var before=payload.Pose;
            // Presentation/proxy corruption cannot remove a child from the owned collider.
            ball.Spheres.Clear(); ball.Position=new(20,20,20); ball.Visible=false;
            world.Activate(cannon); world.Step(); world.Step();
            Assert.Same(geometry,world.Physics.Collider(payload.Id).Declaration.Geometry);
            Assert.Equal(obstructed?CannonShotResult.Obstructed:CannonShotResult.Fired,cannon.LastShot);
            Assert.Equal(obstructed?0:1,cannon.ShotCount);
            if(obstructed)
            {
                Assert.Equal(before,payload.Pose);
                Assert.Equal(default,payload.LinearVelocity);
                Assert.Equal(45,cannon.StoredEnergy); Assert.Equal(0,cannon.ReleasedEnergy);
            }
            else
            {
                Assert.True(payload.LinearVelocity.X>9);
                Assert.InRange(cannon.ReleasedEnergy,44.99,45);
                Assert.Same(ball,cannon.LastPayload);
            }
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.False(world.HasPhysicsState);
            var restored=(CannonPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(0,restored.ShotCount); Assert.Equal(0,restored.StoredEnergy);
        }
        finally {world.Free();}
    }
}
