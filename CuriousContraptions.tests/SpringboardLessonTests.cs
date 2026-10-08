using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SpringboardLessonTests(NativeSceneFixture godot, ITestOutputHelper output)
{
    private enum Role { Ball, Receiver, Spring, Lamp }
    public enum Lesson { Basket, Signal, Precharged }
    private const string CampaignPath = "res://content/puzzles.json";
    private static string LessonId(Lesson lesson) => lesson switch
    {
        Lesson.Basket => "spring_forward",
        Lesson.Signal => "spring_signal",
        Lesson.Precharged => "ready_to_rebound",
        _ => throw new ArgumentOutOfRangeException(nameof(lesson))
    };

    // Explicit boundary for the authored identities in these lessons.
    private static Role ReadRole(string value) => value switch
    {
        "ball" => Role.Ball,
        "receiver" => Role.Receiver,
        "spring_1" => Role.Spring,
        "lamp" => Role.Lamp,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown lesson identity.")
    };

    private enum Placement { Included, Omitted }

    [Theory]
    [InlineData(Lesson.Basket, 1f, true)]
    [InlineData(Lesson.Basket, .45f, true)]
    [InlineData(Lesson.Basket, 1f, false)]
    [InlineData(Lesson.Signal, 1f, true)]
    [InlineData(Lesson.Signal, .45f, true)]
    [InlineData(Lesson.Signal, 1f, false)]
    public void ReachableReceiverNeedsThePassiveSpring(Lesson scenario, float precision, bool includeSpring)
    {
        var placement = includeSpring ? Placement.Included : Placement.Omitted;
        var lesson = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString(CampaignPath))
            .Single(p => p.Id == LessonId(scenario));
        var data = MachineCodec.Clone(lesson.CreateMachine());
        if (placement == Placement.Included) data.Parts.AddRange(lesson.Solution);
        data.Connections = lesson.SolutionConnections;
        var specs = data.Parts.ToDictionary(p => ReadRole(p.Id));
        Assert.Equal(new float[] { -.7f, .55f, 0 }, specs[Role.Receiver].Position);
        var world = new MachineWorld { Precision = precision };
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(data);
            world.Start();
            for (var tick = 0; tick < 600 && world.Running; tick++) world.Step();
            Assert.Equal(placement == Placement.Included, world.Won);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void AuthoredTiltLoadsPlateAndReturnsBallWithFiniteHeight()
    {
        const Lesson scenario = Lesson.Basket;
        var lesson = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString(CampaignPath))
            .Single(p => p.Id == LessonId(scenario));
        var data = MachineCodec.Clone(lesson.CreateMachine());
        data.Parts.AddRange(lesson.Solution);
        var specs = data.Parts.ToDictionary(p => ReadRole(p.Id));
        var world = new MachineWorld { Precision = 1 };
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(data);
            var parts = world.Parts.ToDictionary(p => ReadRole(p.Uid));
            var spring = Assert.IsType<SpringPart>(parts[Role.Spring]);
            var ball = parts[Role.Ball];
            world.Start();
            double compression = 0, rebound = 0, peak = 0;
            for (var tick = 0; tick < 220; tick++)
            {
                world.Step();
                var body = world.PhysicsAssembly.Body(new(ball, MachinePart.RootBody));
                compression = Math.Max(compression, -spring.PlateOffset);
                if (spring.HitCount > 0)
                {
                    rebound = Math.Max(rebound, body.LinearVelocity.Y);
                    peak = Math.Max(peak, body.Center.Y);
                    if (tick % 12 == 0)
                        output.WriteLine($"tick={tick} position={body.Center} velocity={body.LinearVelocity}");
                }
            }
            Assert.True(spring.HitCount > 0);
            Assert.InRange(compression, .05, SpringPart.MaximumStroke + 1e-6);
            Assert.True(rebound > 1);
            Assert.InRange(peak, 0, specs[Role.Ball].Position[1]);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.2f)]
    public void PrecompressedPlateCanSupplyAnAuthoredGravityLaunch(float compression)
    {
        var lesson = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString(CampaignPath))
            .Single(p => p.Id == LessonId(Lesson.Basket));
        var data = MachineCodec.Clone(lesson.CreateMachine());
        data.Parts.AddRange(lesson.Solution);
        data.Parts.RemoveAll(p => ReadRole(p.Id) == Role.Receiver);
        data.Goals.Clear();
        data.PlacementTargets.Clear();
        var specs = data.Parts.ToDictionary(p => ReadRole(p.Id));
        var plate = specs[Role.Spring];
        plate.Position = [0, 2.5f, 0];
        plate.Orientation = PartOrientation.FromEulerDegrees(0, 0, -30);
        plate.Properties[PartParameterName.Of(SpringParameter.InitialCompression)] = compression;
        var offset = plate.Orientation.Transform(new(0, SpringPart.RestHeight - compression + .075f + .34f + .002f, 0));
        specs[Role.Ball].Position = [offset.X, 2.5f + offset.Y, offset.Z];
        var world = new MachineWorld { Precision = 1 };
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(data);
            var parts = world.Parts.ToDictionary(p => ReadRole(p.Uid));
            var spring = Assert.IsType<SpringPart>(parts[Role.Spring]);
            var ball = parts[Role.Ball];
            var initialHeight = ball.Position.Y;
            world.Start();
            Assert.InRange(Math.Abs(spring.StoredElasticEnergy - new AxialElasticPotential(400, 0).Energy(compression)), 0, 1e-5);
            double rise = 0;
            for (var tick = 0; tick < 180; tick++)
            {
                world.Step();
                var state = world.PhysicsAssembly.Body(new(ball, MachinePart.RootBody));
                rise = Math.Max(rise, state.Center.Y - initialHeight);
                if (compression > 0 && tick % 12 == 0)
                    output.WriteLine($"preload={compression} tick={tick} position={state.Center} velocity={state.LinearVelocity}");
            }
            if (compression > 0) Assert.True(rise > .2, $"Charged plate must lift its payload; measured rise {rise}.");
            else Assert.InRange(rise, 0, .01);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(1f, true)]
    [InlineData(.45f, true)]
    [InlineData(1f, false)]
    public void PrechargedLessonNeedsAReceiverAndRestoresItsStoredEnergy(float precision, bool includeReceiver)
    {
        var lesson = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString(CampaignPath))
            .Single(p => p.Id == LessonId(Lesson.Precharged));
        var data = MachineCodec.Clone(lesson.CreateMachine());
        if (includeReceiver) data.Parts.AddRange(lesson.Solution);
        var world = new MachineWorld { Precision = precision };
        godot.Tree.Root.AddChild(world);
        try
        {
            world.LoadMachine(data);
            var before = System.Text.Json.JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData);
            for (var replay = 0; replay < 2; replay++)
            {
                var spring = world.Parts.OfType<SpringPart>().Single();
                world.Start();
                Assert.InRange(spring.StoredElasticEnergy, 7.99999, 8.00001);
                for (var tick = 0; tick < 600 && world.Running; tick++) world.Step();
                Assert.Equal(includeReceiver, world.Won);
                world.Restore();
                Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(world.Snapshot(), MachineJson.Default.MachineData));
            }
        }
        finally { world.Free(); }
    }
}
