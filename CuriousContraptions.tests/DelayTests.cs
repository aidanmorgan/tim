using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class DelayTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        return world;
    }
    [Theory]
    [InlineData(.1f)]
    [InlineData(1f)]
    [InlineData(1.13f)]
    public void DelayWaitsIgnoresRetriggersAndRestores(float seconds)
    {
        var world = World();
        try
        {
            var timer = (DelayPart)world.AddPart(new() { Id = "timer", Kind = "delay",
                Properties = new() { [DelayParameters.Seconds] = seconds } });
            var lamp = world.AddPart(new() { Id = "lamp", Kind = "lamp", Position = [3, 1, 0] });
            Assert.True(world.Connect(timer, lamp));
            var motor = world.AddPart(new() { Id = "motor", Kind = "motor", Position = [6, 1, 0] });
            Assert.False(world.Connect(timer, motor)); // Commands never replace supply.
            world.Start();
            world.Activate(timer);
            var due = timer.DueTick;
            Assert.Equal(DelayState.Counting, timer.State);
            while (world.Ticks < due)
            {
                world.Activate(timer);
                Assert.Equal(due, timer.DueTick);
                world.Step();
                Assert.False(lamp.Active);
            }
            world.Step();
            Assert.Equal(DelayState.Finished, timer.State);
            Assert.True(lamp.Active);
            Assert.Equal(due, world.Events[new(MachineEventKind.Activated, "lamp")]);
            world.Activate(timer);
            Assert.Equal(DelayState.Finished, timer.State);
            Assert.Equal(due, timer.DueTick);
            world.Restore();
            timer = (DelayPart)world.FindPart("timer")!;
            Assert.Equal(DelayState.Ready, timer.State);
            Assert.Equal(-1, timer.DueTick);
            Assert.Equal(0, timer.Progress);
            Assert.False(world.FindPart("lamp")!.Active);
        }
        finally { world.Free(); }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChainedCountdownsIgnoreEntityOrderAndInputRemoval(bool reverse)
    {
        var world = World();
        try
        {
            var specs = new List<PartSpec>
            {
                new() { Id = "a", Kind = "delay", Properties = new() { [DelayParameters.Seconds] = .1f } },
                new() { Id = "b", Kind = "delay", Properties = new() { [DelayParameters.Seconds] = .2f } },
                new() { Id = "lamp", Kind = "lamp" }
            };
            if (reverse) specs.Reverse();
            foreach (var spec in specs) world.AddPart(spec);
            Assert.True(world.Connect(world.FindPart("a")!, world.FindPart("b")!));
            Assert.True(world.Connect(world.FindPart("b")!, world.FindPart("lamp")!));
            world.Start();
            world.Activate(world.FindPart("a")!);
            for (var i = 0; i < 20; i++) world.Step();
            var b = (DelayPart)world.FindPart("b")!;
            Assert.Equal(DelayState.Counting, b.State);
            var expected = b.DueTick;
            world.Connections.RemoveAll(c => c.From == "a");
            for (var i = 0; i < 40; i++) world.Step();
            Assert.True(world.FindPart("lamp")!.Active);
            Assert.Equal(expected, world.Events[new(MachineEventKind.Activated, "lamp")]);
            Assert.Equal(36, expected);
        }
        finally { world.Free(); }
    }
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(13f)]
    [InlineData(float.NaN)]
    public void UnsupportedDurationIsRejected(float duration)
    {
        var world = World();
        try
        {
            Assert.Throws<ArgumentException>(() => world.AddPart(new() { Id = "timer", Kind = "delay",
                Properties = new() { [DelayParameters.Seconds] = duration } }));
            Assert.Empty(world.Parts);
        }
        finally { world.Free(); }
    }
    [Theory]
    [InlineData(-1f)]
    [InlineData(121f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidGoalDelayIsRejected(float seconds)
    {
        var world = World();
        try
        {
            Assert.Throws<ArgumentException>(() => world.ValidateMachine(new()
            {
                Goals = [new() { Type = GoalKind.ActivatedAfter, Target = "lamp", Body = "switch", MinimumDelaySeconds = seconds }]
            }));
            Assert.Empty(world.Parts);
        }
        finally { world.Free(); }
    }
    [Theory]
    [InlineData("delayed_signal", 0f)]
    [InlineData("delayed_signal", .45f)]
    [InlineData("delayed_signal", 1f)]
    [InlineData("delayed_solar", 0f)]
    [InlineData("delayed_solar", .45f)]
    [InlineData("delayed_solar", 1f)]
    public void LessonsNeedEveryLinkAndRejectEarlyBypass(string id, float precision)
    {
        var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json")).Single(p => p.Id == id);
        var data = MachineCodec.Clone(puzzle.CreateMachine());
        data.Parts.AddRange(puzzle.Solution);
        data.Connections = puzzle.SolutionConnections;
        // UI-created IDs depend on placement order, not authored solution-slot names.
        const string placedDelayId = "delay_27";
        data.Parts.Single(p => p.Id == "delay_1").Id = placedDelayId;
        foreach (var connection in data.Connections)
        {
            if (connection.From == "delay_1") connection.From = placedDelayId;
            if (connection.To == "delay_1") connection.To = placedDelayId;
        }
        var world = World();
        world.Precision = precision;
        void Run()
        {
            world.Start();
            for (var tick = 0; tick < 900 && world.Running; tick++) world.Step();
        }
        try
        {
            world.LoadMachine(data);
            Run();
            Assert.True(world.Won);
            var ticks = world.Ticks;
            world.Restore();
            Run();
            Assert.True(world.Won);
            Assert.Equal(ticks, world.Ticks);
            for (var i = 0; i < data.Connections.Count; i++)
            {
                var broken = MachineCodec.Clone(data);
                broken.Connections.RemoveAt(i);
                world.LoadMachine(broken);
                Run();
                Assert.False(world.Won);
            }
            var bypass = MachineCodec.Clone(data);
            bypass.Connections.Add(new() { From = "switch", To = id == "delayed_signal" ? "lamp" : "torch",
                Type = ConnectionDomain.Activation, FromPort = SocketId.ActivationOut, ToPort = SocketId.ActivationIn });
            world.LoadMachine(bypass);
            Run();
            Assert.False(world.Won);
Assert.Equal(DelayState.Finished, ((DelayPart)world.FindPart(placedDelayId)!).State);
        }
        finally { world.Free(); }
    }
}
