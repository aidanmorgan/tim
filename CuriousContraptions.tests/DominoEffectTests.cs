using System.Buffers.Binary;
using System.Text.Json;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>domino_effect is declaration data: locked Basketball, locked end Domino, locked Lamp, four Domino inventory, goal end → lamp.</summary>
public sealed class DominoEffectTests
{
    private const double Upright = -0.46 + .55;
    private static WorkshopConstruction Lesson(Half precision = default) =>
        DominoEffect.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new(3), new(precision));
    private static readonly WorkshopConnection EndToLamp = new(new(2), WorkshopSocket.ActivationOut, new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
    private static WorkshopConstruction Solved(WorkshopConstruction lesson)
    {
        var result = lesson;
        for (var i = 0; i < 4; i++) result = result.WithInstance(WorkshopInput.Domino(new((ulong)(4 + i)), -4 + i, Upright + .01, 0, 0, 0, 0, 1));
        return result with { Connections = new(EndToLamp) };
    }

    [Fact]
    public void FixturesInventoryGoalAndPickerIdentityAreTheAuthoredLevel()
    {
        var lesson = Lesson((Half).45);
        Assert.Equal(WorkshopPuzzleId.DominoEffect, lesson.Puzzle.Id);
        Assert.Equal(WorkshopPartKind.Domino, lesson.Puzzle.InventoryKind);
        Assert.Equal(4u, lesson.Puzzle.InventoryCount);
        Assert.Equal(new WorkshopGoal(WorkshopGoalKind.ActivatedAfter, default, default, default, new(2), new(3), new((Half)0)), lesson.Puzzle.Goal);
        Assert.All(lesson.Instances, instance => Assert.True(instance.Locked));
        var ball = lesson.Ball!.Value; var end = lesson.Instances.Find<WorkshopDomino>()!.Value; var lamp = lesson.Instances.Find<WorkshopLamp>()!.Value;
        Assert.Equal(WorkshopInput.Position(-4.4), (ball.Cell.X, ball.Local.X)); Assert.Equal(WorkshopInput.Position(3), (ball.Cell.Y, ball.Local.Y));
        Assert.Equal(WorkshopInput.Position(.1), (end.Cell.X, end.Local.X)); Assert.Equal(WorkshopInput.Position(.09), (end.Cell.Y, end.Local.Y));
        Assert.Equal(CanonicalRotation.Identity, end.Rotation);
        Assert.Equal(WorkshopInput.Position(3), (lamp.Cell.X, lamp.Local.X)); Assert.Equal(WorkshopInput.Position(1), (lamp.Cell.Y, lamp.Local.Y));
        Assert.Equal(PartAllowance.Counted(4), Assert.Single(WorkshopInventoryPolicy.Authored(lesson.Puzzle)).Value);
        // The end Domino is only a signal source once wired; the fixtures alone compile no sensor and one lamp node.
        var scene = WorkshopPhysicsCompiler.Compile(lesson, new(1, 2));
        Assert.Empty(scene.OrientationSensors.ToArray()); Assert.Empty(scene.Triggers.ToArray());
        Assert.Equal(2, scene.Bodies.ToArray().Count(b => b.Motion == RigidMotionKind.Dynamic));
        Assert.Equal(1, (int)WorkshopActivationCompiler.Compile(lesson).Clear().Count);
    }

    [Fact]
    public void FourPlacedDominoesAndTheEndToLampWireRoundTripThroughSaveAndCompileOneSensor()
    {
        var solved = Solved(Lesson());
        solved.Validate();
        var save = new WorkshopSavedConstruction(solved, new(8));
        var decoded = WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(save));
        Assert.Equal(save, decoded);
        var puzzle = new byte[WorkshopPuzzleWire.ByteLength]; WorkshopPuzzleWire.Write(puzzle, solved.Puzzle);
        Assert.Equal(solved.Puzzle, WorkshopPuzzleWire.Read(puzzle));
        var scene = WorkshopPhysicsCompiler.Compile(decoded.Construction, new(1, 2));
        var sensor = Assert.Single(scene.OrientationSensors.ToArray());
        Assert.Equal(new GpuBodyId(2), sensor.Body);
        Assert.Equal(6, scene.Bodies.ToArray().Count(b => b.Motion == RigidMotionKind.Dynamic));
        Assert.Equal(2, (int)WorkshopActivationCompiler.Compile(decoded.Construction).Clear().Count);
    }

    [Fact]
    public void ForeignInventoryExhaustedTilesChangedFixturesAndWiringIntoADominoReject()
    {
        var lesson = Lesson(); var solved = Solved(lesson);
        Assert.Throws<ArgumentException>(() => solved.WithInstance(WorkshopInput.Domino(new(9), 1, Upright, 0, 0, 0, 0, 1)).Validate());
        Assert.Throws<ArgumentException>(() => lesson.WithInstance(WorkshopInput.Receiver(new(9), 0, 1, 0, 0, 0, 0, 1)).Validate());
        Assert.Throws<ArgumentException>(() => lesson.WithInstance(WorkshopInput.Switch(new(9), 0, 1, 0, 0, 0, 0, 1, ContactTriggerSettings.Default)).Validate());
        Assert.Throws<ArgumentException>(() => lesson.WithInstance(WorkshopInput.Lamp(new(9), 0, 1, 0, 0, 0, 0, 1)).Validate());
        Assert.Throws<ArgumentException>(() => lesson.WithInstance(lesson.Ball!.Value with { Locked = false }).Validate());
        Assert.Throws<ArgumentException>(() => lesson.WithInstance(WorkshopInput.Domino(new(2), .5, .09, 0, 0, 0, 0, 1) with { Locked = true }).Validate());
        Assert.Throws<ArgumentException>(() => lesson.WithoutInstance(new(2)).Validate());
        Assert.Throws<ArgumentException>(() => (lesson with { Puzzle = lesson.Puzzle with { InventoryCount = 3 } }).Validate());
        Assert.Throws<ArgumentException>(() => (lesson with { Puzzle = lesson.Puzzle with { Goal = lesson.Puzzle.Goal with { MinimumDelay = new((Half).5) } } }).Validate());
        Assert.Throws<ArgumentException>(() => DominoEffect.WithPrecision(DelayedSignal.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new(3), new((Half)0)), new((Half).5)));
        Assert.Equal((Half).5, DominoEffect.WithPrecision(lesson, new((Half).5)).Puzzle.Precision.Value);
        // A Domino offers no activation input: wiring into one is rejected before any admission.
        var intoDomino = new WorkshopConnection(new(3), WorkshopSocket.ActivationOut, new(2), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
        Assert.Throws<ArgumentException>(() => (lesson with { Connections = new(intoDomino) }).Validate());
    }

    [Fact]
    public void GoalSolvesOnlyWhenTheEndDominoAndTheLampAreBothLatched()
    {
        var goal = Lesson().Puzzle.Goal;
        var time = new ActivationTime(300, (Half)0);
        var cause = new ActivationCause(new(7), new(2), new(9), time, new((Half)0));
        var end = new ActivationNodeDeclaration(new(2), new(2), ActivationNodeKind.OrientationSource, default, 0, new(7));
        var lamp = new ActivationNodeDeclaration(new(3), new(3), ActivationNodeKind.Latch, default);
        var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Orientation, new(2), time, cause);
        var read = new WorkshopRead(new(2), new(75), default, Activations: new([ActivationLatch.From(end, occurrence), ActivationLatch.From(lamp, occurrence)]));
        Assert.Equal(WorkshopGoalPhase.Solved, WorkshopGoalEvaluator.Evaluate(goal, read, new(2)));
        Assert.Equal(time, WorkshopGoalEvaluator.Occurrence(goal, read, new(2)));
        var dark = read with { Activations = new([ActivationLatch.From(end, occurrence), ActivationLatch.Clear(lamp)]) };
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, dark, new(2)));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read, new(3)));
    }

    [Fact]
    public void LegacyAuthoredSourceNamesTheSameTitleInventoryAndLockedEndDomino()
    {
        using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement.EnumerateArray().Single(p => p.GetProperty("id").GetString() == "domino_effect");
        Assert.Equal("The domino effect", level.GetProperty("title").GetString());
        Assert.Equal(4, level.GetProperty("inventory").GetProperty("domino").GetInt32());
        var end = level.GetProperty("parts").EnumerateArray().Single(p => p.GetProperty("id").GetString() == "end");
        Assert.Equal("domino", end.GetProperty("kind").GetString());
        Assert.True(end.GetProperty("locked").GetBoolean());
    }
}
