using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class TrampolineSkinTests(HeadlessFixture godot)
{
    private enum Role { Bed, Payload }
    private static PartSpec Spec(Role role, Vector3 at) => new()
    {
        Id = role switch { Role.Bed => "bed", Role.Payload => "payload", _ => throw new ArgumentOutOfRangeException(nameof(role)) },
        Kind = role switch { Role.Bed => TrampolinePart.CatalogId, Role.Payload => "ball", _ => throw new ArgumentOutOfRangeException(nameof(role)) },
        Position = [at.X, at.Y, at.Z]
    };
    [Theory]
    [InlineData(.8f, 0f)]
    [InlineData(-.8f, 0f)]
    [InlineData(.8f, .5f)]
    [InlineData(-.8f, -.5f)]
    [InlineData(0f, .5f)]
    public void OffCentreSagSpreadsAcrossAvailableFabricWithFixedEdges(float x, float z)
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var bed = (TrampolinePart)world.AddPart(Spec(Role.Bed, new(0, 4, 0)));
            var ball = world.AddPart(Spec(Role.Payload, new(x, 4.56f, z)));
            world.Start();
            ball.Velocity = Vector3.Down * 5;
            var checkedDeepContact = false;
            for (var tick = 0; tick < 40; tick++)
            {
                world.Step();
                if (bed.Compression < .15f) continue;
                checkedDeepContact = true;
                Assert.True(TrampolinePart.RestHeight - bed.MembraneHeight(Vector2.Zero) > bed.Compression * .15f);
                for (var i = 0; i <= 20; i++)
                {
                    var u = Mathf.Lerp(-TrampolinePart.BedHalf.X, TrampolinePart.BedHalf.X, i / 20f);
                    var v = Mathf.Lerp(-TrampolinePart.BedHalf.Y, TrampolinePart.BedHalf.Y, i / 20f);
                    Assert.Equal(TrampolinePart.RestHeight, bed.MembraneHeight(new(u, -TrampolinePart.BedHalf.Y)));
                    Assert.Equal(TrampolinePart.RestHeight, bed.MembraneHeight(new(u, TrampolinePart.BedHalf.Y)));
                    Assert.Equal(TrampolinePart.RestHeight, bed.MembraneHeight(new(-TrampolinePart.BedHalf.X, v)));
                    Assert.Equal(TrampolinePart.RestHeight, bed.MembraneHeight(new(TrampolinePart.BedHalf.X, v)));
                    for (var j = 0; j <= 20; j++)
                    {
                        var sampleZ = Mathf.Lerp(-TrampolinePart.BedHalf.Y, TrampolinePart.BedHalf.Y, j / 20f);
                        Assert.InRange(bed.MembraneHeight(new(u, sampleZ)),
                            TrampolinePart.RestHeight - bed.Compression - .00001f, TrampolinePart.RestHeight);
                    }
                }
                var position = ball.Position;
                var velocity = ball.Velocity;
                var energy = bed.StoredElasticEnergy;
                bed._Process(.016);
                bed._Process(.5);
                Assert.Equal(position, ball.Position);
                Assert.Equal(velocity, ball.Velocity);
                Assert.Equal(energy, bed.StoredElasticEnergy);
            }
            Assert.True(checkedDeepContact);
            world.Restore();
            var restored = Assert.Single(world.Parts.OfType<TrampolinePart>());
            Assert.Equal(TrampolinePart.RestHeight, restored.MembraneHeight(Vector2.Zero));
        }
        finally { world.Free(); }
    }
}
