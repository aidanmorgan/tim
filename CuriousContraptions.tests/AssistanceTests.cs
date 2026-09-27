using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class AssistanceTests(HeadlessFixture godot)
{
    private MachineWorld World(float precision = 0)
    {
        var world = new MachineWorld { Precision = precision };
        godot.Tree.Root.AddChild(world);
        return world;
    }

    private static MachineData Layout(float x = .2f, float angle = 4, float correction = .25f) => new()
    {
        Parts = [new() { Id = "placed", Kind = "ramp", Position = [x, 2, 0], Rotation = [0, 0, angle] }],
        PlacementTargets =
        [
            new()
            {
                Id = "authored", Kind = "ramp", Position = [0, 2, 0],
                Difficulty =
                [
                    new() { Precision = 0, PositionWindow = .5f, RotationWindow = 10,
                        MaxPositionCorrection = correction, MaxRotationCorrection = 5, BlendSeconds = .4f },
                    new() { Precision = 1 }
                ]
            }
        ]
    };

    [Fact]
    public void CorrectionIsAutomaticBoundedSmoothAndResettable()
    {
        var world = World();
        try
        {
            world.LoadMachine(Layout());
            var part = world.FindPart("placed")!;
            var original = part.Position;
            world.Start();
            Assert.Equal(original, part.Position); // No snap at run start.
            world.Step();
            Assert.InRange(original.X - part.Position.X, 0, .001f);
            var previous = part.Position;
            for (var tick = 1; tick < 60; tick++)
            {
                world.Step();
                Assert.InRange(previous.DistanceTo(part.Position), 0, .01f);
                Assert.InRange(part.Position.X, -.00001f, .20001f);
                Assert.True(part.Position.X <= previous.X + .00001f);
                previous = part.Position;
            }
            Assert.InRange(part.Position.Length() - 2, -.0001f, .0001f);
            Assert.InRange(Mathf.Abs(part.RotationDegrees.Z), 0, .001f);
            world.Restore();
            Assert.Equal(original, world.FindPart("placed")!.Position);
            world.Start();
            for (var tick = 0; tick < 60; tick++) world.Step();
            Assert.Equal(previous, world.FindPart("placed")!.Position);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(1f, .2f, 4f)] // Strict difficulty disables assistance.
    [InlineData(0f, .6f, 4f)] // Outside authored position tolerance.
    [InlineData(0f, .2f, 15f)] // Outside authored orientation tolerance.
    public void StrictOrOutOfRangePlacementsAreUntouched(float precision, float x, float angle)
    {
        var world = World(precision);
        try
        {
            world.LoadMachine(Layout(x, angle));
            var before = world.FindPart("placed")!.Transform;
            world.Start();
            for (var tick = 0; tick < 120; tick++) world.Step();
            Assert.Equal(before, world.FindPart("placed")!.Transform);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void AuthorCanLimitCorrectionWithoutChangingGlobalPhysics()
    {
        var world = World();
        try
        {
            world.LoadMachine(Layout(correction: .05f));
            world.Start();
            for (var tick = 0; tick < 120; tick++) world.Step();
            Assert.InRange(world.FindPart("placed")!.Position.X, .14999f, .15001f);
            Assert.Equal(9.81f, world.Gravity);
            Assert.False(world.Realistic);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void ExactPlacementReservesItsSlotAndDoesNotPullAnotherPartOntoIt()
    {
        var world = World();
        try
        {
            var data = Layout(x: 0, angle: 0);
            data.Parts.Add(new() { Id = "extra", Kind = "ramp", Position = [.2f, 2, 0] });
            world.LoadMachine(data);
            world.Start();
            for (var tick = 0; tick < 120; tick++) world.Step();
            Assert.Equal(.2f, world.FindPart("extra")!.Position.X);
        }
        finally { world.Free(); }
    }

    [Fact]
    public void RotationUsesTheShortArc()
    {
        var world = World();
        try
        {
            var data = Layout(x: 0, angle: 179);
            data.PlacementTargets[0].Rotation = [0, 0, -179];
            world.LoadMachine(data);
            var part = world.FindPart("placed")!;
            var before = part.Quaternion;
            world.Start();
            for (var tick = 0; tick < 24; tick++) world.Step();
            Assert.InRange(before.AngleTo(part.Quaternion), 0, Mathf.DegToRad(2.1f));
            for (var tick = 0; tick < 48; tick++) world.Step();
            Assert.InRange(part.Quaternion.AngleTo(Quaternion.FromEuler(new(0, 0, Mathf.DegToRad(-179)))), 0, .001f);
        }
        finally { world.Free(); }
    }


    [Fact]
    public void PlacementAssistanceExpandsSpringSolutionsWithoutRelaxingReceiverPhysics()
    {
        var world = World();
        try
        {
            var puzzle = MachineCodec.ReadPuzzles(Godot.FileAccess.GetFileAsString("res://content/puzzles.json"))
                .Single(p => p.Id == "spring_forward");
            var precise = new HashSet<int>();
            var forgiving = new HashSet<int>();
            for (var angle = -12; angle <= 12; angle++)
            foreach (var precision in new[] { 0f, 1f })
            {
                var data = MachineCodec.Clone(puzzle.CreateMachine());
                data.Parts.AddRange(MachineCodec.Clone(new() { Parts = puzzle.Solution }).Parts);
                // Identical strict capture physics in both runs isolates placement assistance.
                data.Parts.Single(p => p.Kind == "basket").Difficulty.Clear();
                data.Parts.Single(p => p.Kind == "spring").Rotation[2] += angle;
                world.Precision = precision;
                world.LoadMachine(data);
                world.Start();
                for (var tick = 0; tick < 1200 && world.Running; tick++) world.Step();
                if (world.Won) (precision == 0 ? forgiving : precise).Add(angle);
            }
            Assert.NotEmpty(precise);
            Assert.True(precise.IsSubsetOf(forgiving), "Automatic correction must preserve these working placements.");
            Assert.True(forgiving.Count > precise.Count, "Authored placement correction must widen this solution region.");
        }
        finally { world.Free(); }
    }

    [Fact]
    public void DifficultyCurveInterpolatesTheAuthorsValues()
    {
        var settings = PartAssistance.Evaluate(
        [
            new() { Precision = 0, MaxPositionCorrection = .8f, GuideAcceleration = 10 },
            new() { Precision = 1, MaxPositionCorrection = .2f, GuideAcceleration = 2 }
        ], .5f);
        Assert.Equal(.5f, settings.MaxPositionCorrection, 5);
        Assert.Equal(6, settings.GuideAcceleration);
        Assert.Equal(0, PartAssistance.Evaluate([], 0).MaxPositionCorrection);
    }
}
