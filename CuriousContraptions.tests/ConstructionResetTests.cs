using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class ConstructionResetTests(HeadlessFixture godot)
{
    private const string PartId = "aimed_part";

    [Fact]
    public void LoadingAnotherMachineDiscardsPreviousRunConstruction()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            world.AddPart(new() { Id = PartId, Kind = CannonPart.CatalogId });
            world.Start();
            world.LoadMachine(new());
            world.Restore();
            Assert.Empty(world.Parts);
            Assert.Null(world.Initial);
            Assert.False(world.Running);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0f, 30f, 75f)]
    [InlineData(30f, 45f, 60f)]
    public void RepeatedRunResetPreservesExactCompoundTransform(float x, float y, float z)
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var part = world.AddPart(new() { Id = PartId, Kind = CannonPart.CatalogId,
                Position = [0, 3, 0], Rotation = [x, y, z] });
            // Match gizmo editing: successive world-axis rotations rather than Euler assignment.
            part.Basis = Basis.Identity;
            part.Rotate(Vector3.Back, Mathf.DegToRad(z));
            part.Rotate(Vector3.Right, Mathf.DegToRad(x));
            part.Rotate(Vector3.Up, Mathf.DegToRad(y));
            var expected = part.Transform;
            var expectedScale = part.Scale;
            for (var cycle = 0; cycle < 20; cycle++)
            {
                world.Start();
                part.Position += Vector3.One;
                part.Rotate(Vector3.Up, .17f);
                world.Step();
                world.Restore();
                part = world.FindPart(PartId)!;
                Assert.Equal(expected, part.Transform);
                Assert.Equal(expectedScale, part.Scale);
                Assert.False(world.Running);
                Assert.Equal(0, world.Ticks);
                Assert.Null(world.Initial);
            }
        }
        finally { world.Free(); }
    }
}
