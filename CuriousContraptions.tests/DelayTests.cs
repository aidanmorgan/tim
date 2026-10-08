using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class DelayTests(NativeSceneFixture godot)
{
    private MachineWorld World()
    {
        var world = new MachineWorld();
        godot.Tree.Root.AddChild(world);
        return world;
    }
    [Fact]
    public void ParameterBoundaryIsCanonicalAndRejectsUnsupportedValues()
    {
        Assert.Equal("delay_seconds",PartParameterName.Of(DelayParameter.DelaySeconds));
        Assert.Throws<ArgumentOutOfRangeException>(()=>PartParameterName.Of((DelayParameter)99));
        Assert.Throws<ArgumentException>(()=>PartParameterName.RequireExact<DelayParameter>([]));
        Assert.Throws<ArgumentException>(()=>PartParameterName.RequireExact<DelayParameter>(["unsupported"]));
    }
    [Fact]
    public void CountdownReadingsFollowOwnedSnapshotRestoration()
    {
        const string catalogue="delay";
        const string identity="owned_timer";
        var world=World();
        try
        {
            var timer=(DelayPart)world.AddPart(new() {Id=identity,Kind=catalogue,
                Properties=new() {[PartParameterName.Of(DelayParameter.DelaySeconds)]=.1f}});
            world.Start();
            var ready=world.Timers.Capture();
            world.Activate(timer);world.Step();
            var counting=world.Timers.Capture();
            var due=timer.DueTick;
            while(world.Ticks<=due)world.Step();
            Assert.Equal(SimulationTimerPhase.Finished,timer.State);
            world.Timers.Restore(counting);
            Assert.Equal(SimulationTimerPhase.Counting,timer.State);
            Assert.Equal(due,timer.DueTick);Assert.Equal(0,timer.Progress);
            world.Timers.Restore(ready);
            Assert.Equal(SimulationTimerPhase.Ready,timer.State);
            Assert.Equal(-1,timer.StartedTick);Assert.Equal(-1,timer.DueTick);
        }
        finally {world.Free();}
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
                Properties = new() { [PartParameterName.Of(DelayParameter.DelaySeconds)] = seconds } });
            var lamp = world.AddPart(new() { Id = "lamp", Kind = "lamp", Position = [3, 1, 0] });
            Assert.True(world.Connect(timer, lamp));
            var motor = world.AddPart(new() { Id = "motor", Kind = "motor", Position = [6, 1, 0] });
            Assert.False(world.Connect(timer, motor)); // Commands never replace supply.
            world.Start();
            world.Activate(timer);
            var due = timer.DueTick;
            Assert.Equal(SimulationTimerPhase.Counting, timer.State);
            while (world.Ticks < due)
            {
                world.Activate(timer);
                Assert.Equal(due, timer.DueTick);
                world.Step();
                Assert.False(lamp.Active);
            }
            world.Step();
            Assert.Equal(SimulationTimerPhase.Finished, timer.State);
            Assert.True(lamp.Active);
            Assert.Equal(due, world.Events[new(MachineEventKind.Activated, "lamp")]);
            world.Activate(timer);
            Assert.Equal(SimulationTimerPhase.Finished, timer.State);
            Assert.Equal(due, timer.DueTick);
            world.Restore();
            timer = (DelayPart)world.FindPart("timer")!;
            Assert.Equal(SimulationTimerPhase.Ready, timer.State);
            Assert.Equal(-1, timer.DueTick);
            Assert.Equal(0, timer.Progress);
            Assert.False(world.FindPart("lamp")!.Active);
        }
        finally { world.Free(); }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChainedCountdownsIgnoreEntityOrderAndRejectLiveInputRemoval(bool reverse)
    {
        var world = World();
        try
        {
            var specs = new List<PartSpec>
            {
                new() { Id = "a", Kind = "delay", Properties = new() { [PartParameterName.Of(DelayParameter.DelaySeconds)] = .1f } },
                new() { Id = "b", Kind = "delay", Properties = new() { [PartParameterName.Of(DelayParameter.DelaySeconds)] = .2f } },
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
            Assert.Equal(SimulationTimerPhase.Counting, b.State);
            var expected = b.DueTick;
            var input=world.Connections.Single(c=>c.From==world.FindPart("a")!.Uid);
            var physics=world.Physics;
            Assert.Throws<InvalidOperationException>(()=>world.Disconnect(input));
            Assert.Same(physics,world.Physics);
            Assert.Contains(input,world.Connections);
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
                Properties = new() { [PartParameterName.Of(DelayParameter.DelaySeconds)] = duration } }));
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
    public enum Lesson { Signal, Solar }
    [Theory]
    [InlineData(Lesson.Signal, 0f)]
    [InlineData(Lesson.Signal, .45f)]
    [InlineData(Lesson.Signal, 1f)]
    [InlineData(Lesson.Solar, 0f)]
    [InlineData(Lesson.Solar, .45f)]
    [InlineData(Lesson.Solar, 1f)]
    public void LessonsNeedEveryLinkAndRejectEarlyBypass(Lesson lesson, float precision)
    {
        var id=lesson switch
        {
            Lesson.Signal=>"delayed_signal",Lesson.Solar=>"delayed_solar",
            _=>throw new ArgumentOutOfRangeException(nameof(lesson))
        };
        var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json")).Single(p => p.Id == id);
        var data = MachineCodec.Clone(puzzle.CreateMachine());
        data.Parts.AddRange(puzzle.Solution);
        data.Connections = puzzle.SolutionConnections;
        // UI-created IDs depend on placement order, not authored solution-slot names.
        const string placedDelayId = "delay_27";
        const string authoredDelayId = "delay_1";
        data.Parts.Single(p => p.Id == authoredDelayId).Id = placedDelayId;
        data.Connections = data.Connections.Select(connection => connection with
        {
            From = connection.From == authoredDelayId ? placedDelayId : connection.From,
            To = connection.To == authoredDelayId ? placedDelayId : connection.To
        }).ToList();
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
            bypass.Connections.Add(new() { From = "switch", To = lesson == Lesson.Signal ? "lamp" : "torch",
                Type = ConnectionDomain.Activation, FromPort = SocketId.ActivationOut, ToPort = SocketId.ActivationIn });
            world.LoadMachine(bypass);
            Run();
            Assert.False(world.Won);
Assert.Equal(SimulationTimerPhase.Finished, ((DelayPart)world.FindPart(placedDelayId)!).State);
        }
        finally { world.Free(); }
    }
}
