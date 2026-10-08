using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class TrampolineStackTests(NativeSceneFixture godot)
{
    private enum Role { Bed, Lower, Upper }
    // Test construction serialization boundary; internal decisions retain Role.
    private static string Id(Role role) => role switch
    {
        Role.Bed => "bed", Role.Lower => "lower", Role.Upper => "upper",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static PartSpec Part(Role role, Vector3 at) => new()
    {
        Id = Id(role),
        Kind = role switch
        {
            Role.Bed => TrampolinePart.CatalogId,
            Role.Lower or Role.Upper => "ball",
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        },
        Position = [at.X, at.Y, at.Z]
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CentredStackTransmitsCombinedWeightAndResetsExactly(bool reverseInsertion)
    {
        var world = new MachineWorld { Gravity = 9.81f, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var bed = (TrampolinePart)world.AddPart(Part(Role.Bed, new(0, 4, 0)));
            MachinePart lower, upper;
            if (reverseInsertion)
            {
                upper = world.AddPart(Part(Role.Upper, new(0, 5.2201f, 0)));
                lower = world.AddPart(Part(Role.Lower, new(0, 4.5401f, 0)));
            }
            else
            {
                lower = world.AddPart(Part(Role.Lower, new(0, 4.5401f, 0)));
                upper = world.AddPart(Part(Role.Upper, new(0, 5.2201f, 0)));
            }
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            world.Start();
            var samples = new List<string>();
            var initialEnergy = world.Gravity * (lower.Mass * lower.Position.Y + upper.Mass * upper.Position.Y);
            for (var tick = 0; tick < 2400; tick++)
            {
                world.Step();
                if (tick >= 2388) samples.Add($"{tick}: lower={lower.Position.Y:R}/{world.PhysicsAssembly.Body(new(lower,MachinePart.RootBody)).LinearVelocity.Y:R}, upper={upper.Position.Y:R}/{world.PhysicsAssembly.Body(new(upper,MachinePart.RootBody)).LinearVelocity.Y:R}, compression={bed.Compression:R}");
                if (tick >= 2280)
                {
                    Assert.InRange(world.PhysicsAssembly.Body(new(lower,MachinePart.RootBody)).LinearVelocity.Length, 0, .03f);
                    Assert.InRange(world.PhysicsAssembly.Body(new(upper,MachinePart.RootBody)).LinearVelocity.Length, 0, .03f);
                }
                Assert.True(lower.Position.IsFinite() && upper.Position.IsFinite());
                Assert.True(world.PhysicsAssembly.Body(new(lower,MachinePart.RootBody)).LinearVelocity.IsFinite && world.PhysicsAssembly.Body(new(upper,MachinePart.RootBody)).LinearVelocity.IsFinite);
                Assert.InRange(bed.Compression, 0, TrampolinePart.MaximumStroke);
                Assert.InRange(lower.Position.DistanceTo(upper.Position),
                    lower.Radius + upper.Radius - .002f, 2);
                var energy = world.Gravity * (lower.Mass * lower.Position.Y + upper.Mass * upper.Position.Y)
                    + .5f * lower.Mass * world.PhysicsAssembly.Body(new(lower,MachinePart.RootBody)).LinearVelocity.LengthSquared
                    + .5f * upper.Mass * world.PhysicsAssembly.Body(new(upper,MachinePart.RootBody)).LinearVelocity.LengthSquared + bed.StoredElasticEnergy;
                Assert.InRange(energy, 0, initialEnergy * 1.01f);
            }
            Assert.Equal(1, bed.ContactCount);
            Assert.InRange(Math.Abs(bed.Compression - (lower.Mass + upper.Mass) * world.Gravity / 180), 0, .015f);
            Assert.True(world.PhysicsAssembly.Body(new(lower,MachinePart.RootBody)).LinearVelocity.Length <= .03f, string.Join(System.Environment.NewLine, samples));
            Assert.InRange(world.PhysicsAssembly.Body(new(upper,MachinePart.RootBody)).LinearVelocity.Length, 0, .03f);
            Assert.InRange(Math.Abs(lower.Position.X) + Math.Abs(lower.Position.Z), 0, .001f);
            Assert.InRange(Math.Abs(upper.Position.X) + Math.Abs(upper.Position.Z), 0, .001f);
            world.Restore();
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
            bed = (TrampolinePart)world.FindPart(Id(Role.Bed))!;
            Assert.Equal(0, bed.ContactCount);
            Assert.Equal(0, bed.Compression);
            Assert.Equal(0, bed.StoredElasticEnergy);
        }
        finally { world.Free(); }
    }
}
