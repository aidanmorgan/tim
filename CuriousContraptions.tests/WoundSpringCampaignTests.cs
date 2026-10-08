using System.Text.Json;
using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;
using FileAccess = Godot.FileAccess;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WoundSpringCampaignTests(NativeSceneFixture godot, ITestOutputHelper output)
{
    private const string CampaignPath = "res://content/puzzles.json";
    public enum Circuit { Complete, NoSupply, NoBelt, NoRelease }
    [Theory]
    [InlineData(0f, Circuit.Complete)]
    [InlineData(.45f, Circuit.Complete)]
    [InlineData(1f, Circuit.Complete)]
    [InlineData(.45f, Circuit.NoSupply)]
    [InlineData(.45f, Circuit.NoBelt)]
    [InlineData(.45f, Circuit.NoRelease)]
    public void LessonRequiresWindingAndReleaseAndRestoresConstruction(float precision, Circuit circuit)
    {
        var puzzle = MachineCodec.ReadPuzzles(FileAccess.GetFileAsString(CampaignPath))
            .Single(p => p.Solution.Any(s => s.Kind == WoundSpringPart.CatalogId));
        var machine = MachineCodec.Clone(puzzle.CreateMachine());
        machine.Parts.AddRange(puzzle.Solution);
        machine.Connections = puzzle.SolutionConnections.Where(link => circuit switch
        {
            Circuit.Complete => true,
            Circuit.NoSupply => link.Type != ConnectionDomain.Electrical,
            Circuit.NoBelt => link.Type != ConnectionDomain.Mechanical,
            Circuit.NoRelease => link.Type != ConnectionDomain.Activation,
            _ => throw new ArgumentOutOfRangeException(nameof(circuit))
        }).ToList();
        var world = new MachineWorld { Precision = precision };
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(machine);
            Assert.All(machine.Connections, link => Assert.True(world.IsValidConnection(link)));
            var spring = Assert.Single(world.Parts.OfType<WoundSpringPart>());
            var payload = world.FindPart(Assert.Single(puzzle.Goals).Body)!;
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            world.Start();
            for (var tick = 0; tick < 1200 && world.Running; tick++) world.Step();
            output.WriteLine($"precision={precision} circuit={circuit} won={world.Won} tick={world.Ticks} payload={payload.Position} releases={spring.ReleaseCount} stored={spring.StoredEnergy} released={spring.ReleasedWork}");
            Assert.Equal(circuit == Circuit.Complete, world.Won);
            Assert.Equal(circuit == Circuit.Complete ? 1 : 0, spring.ReleaseCount);
            if (circuit == Circuit.Complete) Assert.InRange(spring.ReleasedWork, 51.19, 51.21);
            else if (circuit == Circuit.NoRelease) Assert.InRange(spring.StoredEnergy, 51.19, 51.21);
            else Assert.Equal(0, spring.AcceptedWork);
            world.Restore();
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
        }
        finally { world.Free(); }
    }
    public enum StoredCircuit { Complete, NoSupply, NoRelease }
    [Theory]
    [InlineData(0f, StoredCircuit.Complete)]
    [InlineData(.45f, StoredCircuit.Complete)]
    [InlineData(1f, StoredCircuit.Complete)]
    [InlineData(.45f, StoredCircuit.NoSupply)]
    [InlineData(.45f, StoredCircuit.NoRelease)]
    public void StoredEnergyLessonLaunchesAfterMotorStops(float precision, StoredCircuit circuit)
    {
        var puzzle = MachineCodec.ReadPuzzles(FileAccess.GetFileAsString(CampaignPath))
            .Single(p => p.Parts.Any(s => s.Kind == WoundSpringPart.CatalogId));
        var launcherId = puzzle.Parts.Single(p => p.Kind == WoundSpringPart.CatalogId).Id;
        var machine = MachineCodec.Clone(puzzle.CreateMachine());
        machine.Parts.AddRange(puzzle.Solution);
        machine.Connections = puzzle.SolutionConnections.Where(link => circuit switch
        {
            StoredCircuit.Complete => true,
            StoredCircuit.NoSupply => link.Type != ConnectionDomain.Electrical,
            StoredCircuit.NoRelease => link.Type != ConnectionDomain.Activation || link.To != launcherId,
            _ => throw new ArgumentOutOfRangeException(nameof(circuit))
        }).ToList();
        var world = new MachineWorld { Precision = precision };
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(machine);
            var spring = Assert.Single(world.Parts.OfType<WoundSpringPart>());
            var saved = JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            var heldTicks = 0;
            double? retainedWork = null;
            var releaseTick = -1;
            world.Start();
            for (var tick = 0; tick < 1200 && world.Running; tick++)
            {
                world.Step();
                if (spring.StoredEnergy > 51 && MechanicalNetwork.Speed(world,spring,SocketId.DriveIn) == 0 &&
                    world.Physics.MotorUse.ToArray().All(use=>use.SuppliedWork==0))
                {
                    heldTicks++;
                    retainedWork ??= spring.AcceptedWork;
                }
                if (retainedWork.HasValue) Assert.Equal(retainedWork.Value, spring.AcceptedWork);
                if (spring.ReleaseCount > 0 && releaseTick < 0)
                {
                    releaseTick = world.Ticks;
                    Assert.True(heldTicks >= 30, $"Only {heldTicks} ticks of unpowered retention.");
                    Assert.Equal(0, MechanicalNetwork.Speed(world,spring,SocketId.DriveIn));
                    Assert.All(world.Physics.MotorUse.ToArray(),use=>Assert.Equal(0,use.SuppliedWork));
                }
            }
            output.WriteLine($"precision={precision}, circuit={circuit}, won={world.Won}, tick={world.Ticks}, held={heldTicks}, release={releaseTick}, accepted={spring.AcceptedWork}, released={spring.ReleasedWork}");
            Assert.Equal(circuit == StoredCircuit.Complete, world.Won);
            Assert.Equal(circuit == StoredCircuit.Complete ? 1 : 0, spring.ReleaseCount);
            if (circuit == StoredCircuit.Complete)
            {
                Assert.InRange(spring.ReleasedWork, 51.19, 51.21);
                Assert.Equal(0, spring.StoredEnergy);
            }
            else if (circuit == StoredCircuit.NoRelease)
            {
                Assert.True(heldTicks >= 30);
                Assert.InRange(spring.StoredEnergy, 51.19, 51.21);
            }
            else Assert.Equal(0, spring.AcceptedWork);
            world.Restore();
            Assert.Equal(saved, JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
        }
        finally { world.Free(); }
    }
}
