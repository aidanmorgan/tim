using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class TrampolineRopeTests(NativeSceneFixture godot)
{
    public enum TetherMode { TautAboveBed, SlackRebound, Unconnected }
    private enum Role { Bed, Anchor, Load }
    // Construction/serialization boundary; closed choices remain typed internally.
    private static string Id(Role role) => role switch
    {
        Role.Bed => "bed", Role.Anchor => "anchor", Role.Load => "load",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static PartSpec Spec(Role role, Vector3 position) => new()
    {
        Id = Id(role),
        Kind = role switch
        {
            Role.Bed => TrampolinePart.CatalogId,
            Role.Anchor => "rope_anchor",
            Role.Load => "weight",
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        },
        Position = [position.X, position.Y, position.Z]
    };

    [Theory]
    [InlineData(TetherMode.TautAboveBed, false)]
    [InlineData(TetherMode.TautAboveBed, true)]
    [InlineData(TetherMode.SlackRebound, false)]
    [InlineData(TetherMode.SlackRebound, true)]
    [InlineData(TetherMode.Unconnected, false)]
    public void TetherControlsAccessWithoutAddingEnergyAndResets(TetherMode mode, bool reverse)
    {
        var world = new MachineWorld { Gravity = 9.81f, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        try
        {
            var bed = (TrampolinePart)world.AddPart(Spec(Role.Bed, new(0, 3, 0)));
            var anchor = world.AddPart(Spec(Role.Anchor, new(0, 8, -.18f)));
            var load = world.AddPart(Spec(Role.Load, new(0, 5.5f, 0)));
            if (mode != TetherMode.Unconnected)
            {
                Assert.True(world.Connect(reverse ? load : anchor, reverse ? anchor : load));
                var link = Assert.Single(world.Connections);
                Assert.Equal(ConnectionDomain.Rope, link.Type);
                Assert.Equal(SocketId.Tie, link.FromPort);
                Assert.Equal(SocketId.Tie, link.ToPort);
                if (mode == TetherMode.SlackRebound)
                {
                    var construction=world.Snapshot();
                    construction.Connections[0]=link with {RopeLength=link.RopeLength+2.3f};
                    world.LoadMachine(construction);
                    bed=(TrampolinePart)world.FindPart(Id(Role.Bed))!;
                    load=world.FindPart(Id(Role.Load))!;
                }
            }
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            world.Start();
            var initialEnergy = load.Mass * world.Gravity * load.Position.Y;
            var peakCompression = 0f;
            var rebounded = false;
            var touched = false;
            var taut = false;
            for (var tick = 0; tick < 1200; tick++)
            {
                world.Step();
                Assert.True(load.Visible && load.Position.IsFinite() && world.PhysicsAssembly.Body(new(load,MachinePart.RootBody)).LinearVelocity.IsFinite);
                peakCompression = Math.Max(peakCompression, bed.Compression);
                touched |= bed.ContactCount > 0;
                rebounded |= touched && world.PhysicsAssembly.Body(new(load,MachinePart.RootBody)).LinearVelocity.Y > .1f;
                var energy = load.Mass * world.Gravity * load.Position.Y
                    + .5f * load.Mass * world.PhysicsAssembly.Body(new(load,MachinePart.RootBody)).LinearVelocity.LengthSquared + bed.StoredElasticEnergy;
                Assert.InRange(energy, 0, initialEnergy * 1.01f);
                Assert.InRange(bed.Compression, 0, TrampolinePart.MaximumStroke);
                if (mode != TetherMode.Unconnected)
                {
                    var rope = Assert.Single(world.Ropes);
                    Assert.True(rope.CurrentLength(world) <= rope.Length + .002f);
                    taut |= rope.State(world) == RopeState.Taut;
                }
            }
            if (mode == TetherMode.TautAboveBed)
            {
                Assert.False(touched);
                Assert.False(rebounded);
                Assert.Equal(0, peakCompression);
                Assert.Equal(0, bed.ImpactCount);
                Assert.InRange(Math.Abs(load.Position.Y - 5.5f), 0, .002f);
                Assert.True(taut);
            }
            else
            {
                Assert.True(touched);
                Assert.True(rebounded);
                Assert.InRange(peakCompression, .05f, TrampolinePart.MaximumStroke);
                Assert.True(bed.ImpactCount > 0);
                if (mode == TetherMode.SlackRebound) Assert.True(taut);
            }
            var signature = world.StateSignature();
            world.Restore();
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
            bed = (TrampolinePart)world.FindPart(Id(Role.Bed))!;
            Assert.Equal(0, bed.ContactCount);
            Assert.Equal(0, bed.Compression);
            Assert.Equal(0, bed.StoredElasticEnergy);
            world.Start();
            for (var tick = 0; tick < 1200; tick++) world.Step();
            Assert.Equal(signature, world.StateSignature());
        }
        finally { world.Free(); }
    }
}
