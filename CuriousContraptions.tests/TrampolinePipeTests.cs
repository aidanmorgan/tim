using Godot;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class TrampolinePipeTests(HeadlessFixture godot, ITestOutputHelper output)
{
    public enum Route { Aimed, FlatBed, MissedPipe }
    private enum Role { Bed, Pipe, Payload }
    private static string Id(Role role) => role switch
    {
        Role.Bed => "bed", Role.Pipe => "pipe", Role.Payload => "payload",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    // Catalog/instance serialization boundary only.
    private static PartSpec Spec(Role role, Vector3 position, Vector3 rotation = default) => new()
    {
        Id = Id(role), Kind = role switch
        {
            Role.Bed => TrampolinePart.CatalogId, Role.Pipe => "pipe", Role.Payload => "ball",
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        },
        Position = [position.X, position.Y, position.Z],
        Rotation = [rotation.X, rotation.Y, rotation.Z]
    };

    [Theory]
    [InlineData(Route.Aimed)]
    [InlineData(Route.FlatBed)]
    [InlineData(Route.MissedPipe)]
    public void ReboundTraversesOpenBoreOnlyWhenBothElementsAlign(Route route)
    {
        var world = new MachineWorld { Gravity = 9.81f, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var bed = (TrampolinePart)world.AddPart(Spec(Role.Bed, new(0, 3, 0),
                new(0, 0, route == Route.FlatBed ? 0 : -30)));
            var pipe = (PipePart)world.AddPart(Spec(Role.Pipe, new(3, 3.1f, route == Route.MissedPipe ? 2 : 0)));
            var payload = world.AddPart(Spec(Role.Payload, new(0, 7, 0)));
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            world.Start();
            var initialEnergy = payload.Mass * world.Gravity * payload.Position.Y;
            var entered = false;
            var crossedCentre = false;
            var exited = false;
            var boreTicks = 0;
            Vector3? exitPosition = null;
            var previous = pipe.ToLocal(payload.Position);
            for (var tick = 0; tick < 600; tick++)
            {
                world.Step();
                Assert.True(payload.Position.IsFinite() && payload.Velocity.IsFinite());
                var at = pipe.ToLocal(payload.Position);
                var radial = new Vector2(at.Y, at.Z).Length();
                if (previous.X < -pipe.Length * .5f && at.X >= -pipe.Length * .5f &&
                    radial + payload.Radius <= PipePart.BoreRadius + .002f) entered = true;
                if (entered && Math.Abs(at.X) < pipe.Length * .5f - payload.Radius)
                {
                    Assert.InRange(radial + payload.Radius, 0, PipePart.BoreRadius + .002f);
                    boreTicks++;
                    crossedCentre |= previous.X < 0 && at.X >= 0;
                }
                if (crossedCentre && at.X > pipe.Length * .5f + .09f + payload.Radius)
                {
                    exited = true;
                    exitPosition ??= payload.Position;
                }
                var energy = payload.Mass * world.Gravity * payload.Position.Y
                    + .5f * payload.Mass * payload.Velocity.LengthSquared() + bed.StoredElasticEnergy;
                Assert.True(energy <= initialEnergy * 1.01f);
                previous = at;
            }
            output.WriteLine($"route={route}, entered={entered}, centre={crossedCentre}, exited={exited}, boreTicks={boreTicks}, exit={exitPosition}, final={payload.Position}");
            Assert.True(bed.ImpactCount > 0);
            Assert.Equal(route == Route.Aimed, entered);
            Assert.Equal(route == Route.Aimed, crossedCentre);
            Assert.Equal(route == Route.Aimed, exited);
            if (route == Route.Aimed) Assert.True(boreTicks >= 20);
            var signature = world.StateSignature();
            world.Restore();
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
            world.Start();
            for (var tick = 0; tick < 600; tick++) world.Step();
            Assert.Equal(signature, world.StateSignature());
        }
        finally { world.Free(); }
    }
}
