using System.Buffers.Binary;
using System.Text.Json;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>domino_effect is declaration data: locked Basketball, locked end Domino, locked Lamp, four Domino inventory, goal end → lamp.</summary>
public sealed class DominoEffectTests
{
    private const double Upright = -0.46 + .55;
    private static WorkshopConstruction Lesson(float precision = default) =>
        PuzzleCatalogue.Create(new("domino_effect"), new(1), WorkshopCadenceSettings.Default(), new(1), new(precision));
    private static readonly WorkshopConnection EndToLamp = new(new(2), WorkshopSocket.ActivationOut, new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
    private static WorkshopConstruction Solved(WorkshopConstruction lesson)
    {
        var result = lesson;
        for (var i = 0; i < 4; i++) result = result.WithInstance(PlacedParts.Domino(new((ulong)(4 + i)), (float)(-4 + i), (float)(Upright + .01), 0, 0, 0, 0, 1));
        return result with { Connections = new(EndToLamp) };
    }

    [Fact]
    public void FixturesInventoryGoalAndPickerIdentityAreTheAuthoredLevel()
    {
        var lesson = Lesson(.45f);
        Assert.Equal(new PuzzleId("domino_effect"), lesson.Puzzle.Id);
        Assert.Equal(new PartId("domino"), lesson.Puzzle.InventoryPart);
        Assert.Equal(4u, lesson.Puzzle.InventoryCount);
        Assert.Equal(new WorkshopGoal(WorkshopGoalKind.ActivatedAfter, default, default, new(2), new(3), new(0f)), lesson.Puzzle.Goal);
        Assert.All(lesson.Instances, instance => Assert.True(instance.Locked));
        var ball = Payloads.First(lesson); var end = lesson.Instances.Single(instance => instance.PartId == new PartId("domino")); var lamp = lesson.Instances.Single(instance => instance.PartId == new PartId("lamp"));
        At(-4.4, ball.Cell.X, ball.Local.X); At(3, ball.Cell.Y, ball.Local.Y);
        At(.1, end.Cell.X, end.Local.X); At(.09, end.Cell.Y, end.Local.Y);
        Assert.Equal(CanonicalRotation.Identity, end.Rotation);
        At(3, lamp.Cell.X, lamp.Local.X); At(1, lamp.Cell.Y, lamp.Local.Y);
        // Authored metre positions within 0.1 mm; canonical remainder rounding is not gameplay.
        static void At(double metres, int cell, float local) =>
            Assert.InRange((cell + (double)local) / 16, metres - .0001, metres + .0001);
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
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(save));
        Assert.Equal(save, decoded);
        Assert.Equal(solved.Puzzle, decoded.Construction.Puzzle);
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
        EditRefusal.Is(WorkshopEditRejection.InventoryExceeded, () => solved.WithInstance(PlacedParts.Domino(new(9), 1, (float)(Upright), 0, 0, 0, 0, 1)).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => lesson.WithInstance(WorkshopInput.Generic(new PartId("basket"), new(9), 0, 1, 0, 0, 0, 0, 1)).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => lesson.WithInstance(WorkshopInput.Generic(new PartId("switch"), new(9), 0, 1, 0, 0, 0, 0, 1)).Validate());
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => lesson.WithInstance(PlacedParts.Lamp(new(9), 0, 1, 0, 0, 0, 0, 1)).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(Payloads.First(lesson) with { Locked = false }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithInstance(PlacedParts.Domino(new(2), .5f, .09f, 0, 0, 0, 0, 1, locked: true)).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => lesson.WithoutInstance(new(2)).Validate());
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (lesson with { Puzzle = lesson.Puzzle with { InventoryCount = 3 } }).Validate());
        EditRefusal.Is(WorkshopEditRejection.PuzzleSettings, () => (lesson with { Puzzle = lesson.Puzzle with { Goal = lesson.Puzzle.Goal with { MinimumDuration = new(.5f) } } }).Validate());
        Assert.Equal(new PuzzleId("delayed_signal"), PuzzleCatalogue.WithPrecision(PuzzleCatalogue.Create(new("delayed_signal"), new(1), WorkshopCadenceSettings.Default(), new(1), new(0f)), new(.5f)).Puzzle.Id);
        Assert.Equal(.5f, PuzzleCatalogue.WithPrecision(lesson, new(.5f)).Puzzle.Precision.Value);
        // A Domino offers no activation input: wiring into one is rejected before any admission.
        var intoDomino = new WorkshopConnection(new(3), WorkshopSocket.ActivationOut, new(2), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => (lesson with { Connections = new(intoDomino) }).Validate());
    }

    [Fact]
    public void GoalSolvesOnlyWhenTheEndDominoAndTheLampAreBothLatched()
    {
        var goal = Lesson().Puzzle.Goal;
        var time = new ActivationTime(300,0f);
        var cause = new ActivationCause(new(7), new(2), new(9), time, new(0f));
        var end = new ActivationNodeDeclaration(new(2), new(2), ActivationNodeKind.OrientationSource, default, 0, new(7));
        var lamp = new ActivationNodeDeclaration(new(3), new(3), ActivationNodeKind.StickyFirstInput, default);
        var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Orientation, new(2), time, cause);
        var read = new WorkshopRead(new(2), new(75), default, Activations: new([ActivationLatch.From(end, occurrence), ActivationLatch.From(lamp, occurrence)]));
        Assert.Equal(WorkshopGoalPhase.Solved, WorkshopGoalEvaluator.Evaluate(goal, read, new(2), Lesson().Instances.Identities));
        Assert.Equal(time, WorkshopGoalEvaluator.Occurrence(goal, read, new(2), Lesson().Instances.Identities));
        var dark = read with { Activations = new([ActivationLatch.From(end, occurrence), ActivationLatch.Clear(lamp)]) };
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, dark, new(2), Lesson().Instances.Identities));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read, new(3), Lesson().Instances.Identities));
    }

    [Fact]
    public void AuthoredDataNamesTheTitleInventoryAndLockedEndDomino()
    {
        using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement.EnumerateArray().Single(p => p.GetProperty("id").GetString() == "domino_effect");
        Assert.Equal("The domino effect", level.GetProperty("title").GetString());
        Assert.Equal(4, level.GetProperty("inventory").EnumerateArray().Single(row => row.GetProperty("part").GetString() == "domino").GetProperty("count").GetInt32());
        var end = level.GetProperty("placements").EnumerateArray().Select(row => row.GetProperty("instance")).Single(row => row.GetProperty("part").GetString() == "domino");
        Assert.Equal("domino", end.GetProperty("part").GetString());
        Assert.True(end.GetProperty("locked").GetBoolean());
    }
}
