using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WallTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Wall=new("wall"),Ball=new("ball");
    private MachineWorld World()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        world.LoadMachine(new() { Gravity = 0, Pressure = 0, Parts = [
            new() { Id = FixtureParts.Id(FixturePartId.First), Kind = Wall.Value, Position = [0, 4, 0] }
        ] });
        return world;
    }

    [Fact]
    public void DimensionsDriveCollisionArtworkSerializationAndReset()
    {
        var world = World();
        try
        {
            var wall = (WallPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            wall.SetDimensions(new(5, 3, .6f));
            Assert.Equal(new Vector3(2.5f, 1.5f, .3f), Assert.Single(wall.Boxes).Half);
            var bounds = PlacementShadows.ArtworkBounds(wall);
            Assert.InRange(bounds.Size.X, 4.99f, 5.01f);
            Assert.InRange(bounds.Size.Y, 2.99f, 3.01f);
            wall.RotationDegrees = new(25, 40, 15);
            Assert.NotEqual(bounds, PlacementShadows.ArtworkBounds(wall));
            var authoredTransform = wall.Transform;
            var authoredScale = wall.Basis.Scale;
            var cachedScale = wall.Scale;
            var saved = System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            var before = wall.Serialize();
            Assert.Equal(5, before.Properties[PartParameterName.Of(WallParameter.Width)]);
            world.Start();
            world.Step();
            world.Restore();
            var restored = (WallPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(new Vector3(5, 3, .6f), restored.Dimensions);
            Assert.Equal(authoredTransform, restored.Transform);
            Assert.Equal(authoredScale, restored.Basis.Scale);
            Assert.Equal(saved,System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            Assert.Equal(before.Orientation, restored.Serialize().Orientation);
            System.Console.WriteLine($"Wall Reset cached_scale_before={cachedScale} cached_scale_after={restored.Scale} basis_scale_before={authoredScale} basis_scale_after={restored.Basis.Scale} exact_transform={authoredTransform == restored.Transform}");
            Assert.True(restored.RotationDegrees.DistanceTo(new(25, 40, 15)) < .001f);
            restored.SetDimensions(new(-2, 100, 0));
            Assert.Equal(new Vector3(.4f, 6, .12f), restored.Dimensions);
            restored.SetDimensions(new(float.NaN, 1, 1));
            Assert.Equal(new Vector3(.4f, 6, .12f), restored.Dimensions);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ResizeHandlesFollowRotatedLocalAxesAndCancel(int axis)
    {
        var world = World();
        var camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 14, Position = new(9, 9, 14) };
        var gizmo = new RotationGizmo();
        godot.Tree.Root.AddChild(camera);
        godot.Tree.Root.AddChild(gizmo);
        camera.LookAt(new(0, 4, 0));
        try
        {
            var wall = (WallPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            wall.RotationDegrees = new(20, 35, 10);
            var initial = wall.Dimensions;
            var position = wall.Position;
            var rotation = wall.Quaternion;
            gizmo.SetResizeMode();
            gizmo.Follow(wall, true, camera);
            var start = camera.UnprojectPosition(gizmo.HandlePosition(axis));
            Assert.True(gizmo.Begin(camera, start));
            Assert.Equal(axis, gizmo.ActiveAxis);
            var screenAxis = camera.UnprojectPosition(wall.GlobalPosition + wall.GlobalBasis[axis]) -
                             camera.UnprojectPosition(wall.GlobalPosition);
            Assert.True(gizmo.Drag(camera, start + screenAxis * .3f, true));
            Assert.Equal(Mathf.Snapped(initial[axis] + .6f, .1f), wall.Dimensions[axis], 3);
            Assert.Equal(position, wall.Position);
            Assert.Equal(rotation, wall.Quaternion);
            gizmo.End(true);
            Assert.Equal(initial, wall.Dimensions);
            Assert.Equal(initial * .5f, wall.Boxes[0].Half);
        }
        finally { gizmo.Free(); camera.Free(); world.Free(); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(45)]
    [InlineData(90)]
    public void RotatedResizedWallReflectsBalls(int yaw)
    {
        var world = World();
        try
        {
            var wall = (WallPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            wall.SetDimensions(new(4, 3, .4f));
            wall.RotationDegrees = new(0, yaw, 0);
            var normal = wall.Basis.Z;
            var center = wall.Position;
            var at = wall.Position + normal;
            var data = world.Snapshot();
            data.Parts.Add(new() { Id = FixtureParts.Id(FixturePartId.Second), Kind = Ball.Value, Position = [at.X, at.Y, at.Z] });
            world.LoadMachine(data);
            var ball = world.FindPart(FixtureParts.Id(FixturePartId.Second))!;
            ball.InitialVelocity = -normal * 4;
            world.Start();
            for (var i = 0; i < 40; i++) world.Step();
            Assert.True(CollisionVector.Dot(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity,SceneGeometryAdapter.CaptureVector(normal)) > 0);
            Assert.True((ball.Position - center).Dot(normal) >= .2f + ball.Radius);
        }
        finally { world.Free(); }
    }
}
