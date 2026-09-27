using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class PipeResizeTests(HeadlessFixture godot)
{
    [Theory]
    [InlineData(1f)]
    [InlineData(4.2f)]
    [InlineData(8f)]
    public void LengthUpdatesGeometryPropertiesOpticalCollarsAndReset(float length)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var pipe = (PipePart)world.AddPart(new() { Id = "pipe", Kind = "pipe", Position = [0, 4, 0] });
            pipe.SetDimensions(new(length, 1.3f, 1.3f));
            Assert.Equal(length * .5f, pipe.Tubes[0].HalfLength);
            Assert.All(pipe.Tubes, tube => Assert.Equal(PipePart.BoreRadius, tube.InnerRadius));
            Assert.Equal(-length * .5f, pipe.Tubes[1].Pose.Origin.X);
            Assert.Equal(length * .5f, pipe.Tubes[2].Pose.Origin.X);
            Assert.Equal(length, pipe.Serialize().Properties[PipeParameters.Length]);
            Assert.InRange(PlacementShadows.ArtworkBounds(pipe).Size.X, length + .179f, length + .181f);
            var emitter = world.AddPart(new() { Id = "light", Kind = "flashlight", Position = [-6, 4, 0] });
            Assert.InRange(WorldGeometry.Trace(TraceMedium.Light,world, new(-6, 4.7f, 0), Vector3.Right, 12, emitter),
                6 - length * .5f - .091f, 6 - length * .5f - .089f);
            world.Start();
            world.Restore();
            pipe = (PipePart)world.FindPart("pipe")!;
            Assert.Equal(length, pipe.Length);
            Assert.Equal(Vector3.One, pipe.Scale);
            Assert.Throws<ArgumentException>(() => pipe.SetDimensions(new(length, 2, 1.3f)));
            Assert.Equal(length, pipe.Length);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(9f)]
    [InlineData(float.NaN)]
    public void InvalidAuthoredLengthIsRejected(float length)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            Assert.Throws<ArgumentException>(() => world.AddPart(new() { Id = "pipe", Kind = "pipe",
                Properties = new() { [PipeParameters.Length] = length } }));
            Assert.Empty(world.Parts);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void OnlyTheLocalLengthHandleIsInteractiveAndCancelRestoresGeometry()
    {
        var world = new MachineWorld();
        var camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 14, Position = new(9, 9, 14) };
        var gizmo = new RotationGizmo();
        godot.Tree.Root.AddChild(world);
        godot.Tree.Root.AddChild(camera);
        godot.Tree.Root.AddChild(gizmo);
        camera.LookAt(new(0, 4, 0));
        try
        {
            var pipe = (PipePart)world.AddPart(new() { Id = "pipe", Kind = "pipe", Position = [0, 4, 0], Rotation = [20, 35, 10] });
            gizmo.SetResizeMode();
            gizmo.Follow(pipe, true, camera);
            Assert.True(gizmo.AxisEnabled(0));
            Assert.False(gizmo.AxisEnabled(1));
            Assert.False(gizmo.AxisEnabled(2));
            Assert.False(gizmo.Begin(camera, camera.UnprojectPosition(gizmo.HandlePosition(1))));
            var start = camera.UnprojectPosition(gizmo.HandlePosition(0));
            Assert.True(gizmo.Begin(camera, start));
            var direction = camera.UnprojectPosition(pipe.GlobalPosition + pipe.GlobalBasis.X) - camera.UnprojectPosition(pipe.GlobalPosition);
            gizmo.Drag(camera, start + direction * .3f, true);
            Assert.Equal(4.2f, pipe.Length, 3);
            Assert.Equal(1.3f, pipe.Dimensions.Y);
            gizmo.End(true);
            Assert.Equal(3.6f, pipe.Length, 3);
            Assert.Equal(1.8f, pipe.Tubes[0].HalfLength, 3);
        }
        finally { gizmo.Free(); camera.Free(); world.Free(); }
    }
}
