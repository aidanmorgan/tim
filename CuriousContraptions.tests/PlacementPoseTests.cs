using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

/// <summary>F.6 F-1 (C-Plate D6): <see cref="PlacementPose.Compose"/>, the one placement composition the preview and the click share,
/// composes grid, tube-mouth snap and rest. A Dynamic part dropped from the palette into a Static part's collider whose top is
/// horizontal, within the part's height and under its x–z centre rests on the highest such top; every other pose is the base pose
/// (the snap, else the 0.1 m grid), which each control states independently as <c>TubeMouthSnap.Find(...) ?? grid</c>.</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class PlacementPoseTests
{
    private static readonly float PlateY = (float)((double)WorkshopConstruction.WorkbenchSurface.Value + .22);
    /// <summary>The plate face's top: its centre is 0.03 m above the plate origin and it is 0.1 m thick (the plate scene).</summary>
    private static readonly float FaceTop = PlateY + .03f + .05f;

    private static IReadOnlyDictionary<PartId, PartDefinition> Definitions()
    {
        var registry = new PartRegistry();
        registry.Load();
        return registry.Definitions;
    }

    private static (Vector3 Position, Quaternion Rotation) Base(IReadOnlyDictionary<PartId, PartDefinition> definitions,
        IReadOnlyList<IWorkshopInstance> placed, TubeMouthMover mover, Vector3 at, Quaternion rotation)
    {
        var grid = at.Snapped(Vector3.One * .1f);
        return TubeMouthSnap.Find(definitions, placed, mover, grid, rotation) is { } joined ? (joined.Position, joined.Rotation) : (grid, rotation);
    }

    /// <summary>The composed pose of an admitted placement (its refusal must be None).</summary>
    private static (Vector3 Position, Quaternion Rotation) Composed(IReadOnlyDictionary<PartId, PartDefinition> definitions,
        IReadOnlyList<IWorkshopInstance> placed, TubeMouthMover mover, Vector3 at, Quaternion rotation)
    {
        var composed = PlacementPose.Compose(definitions, placed, mover, at, rotation);
        Assert.Equal(PlacementRefusal.None, composed.Refusal);
        return (composed.Position, composed.Rotation);
    }

    private static WorkshopGenericInstance At(PartId part, ulong id, Vector3 position, Quaternion rotation, InstanceParameterCollection? parameters = null)
    {
        var q = rotation.Normalized();
        return WorkshopInput.Generic(part, new(id), position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W, parameters);
    }

    // A Domino dropped into the plate (its centre at the plate origin height, straddling the face) rests with its foot on the face:
    // its centre rises to the face top + 0.55 m; x, z and the rotation are the grid's. So does a Basketball (0.34 m) and a Bowling
    // ball (0.28 m). The highest qualifying top wins: the base slab's top (0.1 m lower) is also overlapped.
    public static TheoryData<PartId, float> DynamicParts() => new() { { PartId.Domino, .55f }, { PartId.Basketball, .34f }, { PartId.BowlingBall, .28f } };

    [Theory]
    [MemberData(nameof(DynamicParts))]
    public void ADynamicPartDroppedIntoAPlateRestsOnItsFace(PartId part, float halfHeight)
    {
        var definitions = Definitions();
        IWorkshopInstance[] placed = [PlacedParts.Plate(new(1), -4, PlateY, 0, .5f)];
        var at = new Vector3(-3.6f, PlateY, .2f);
        var (position, rotation) = Composed(definitions, placed, new(part, new(2), false), at, Quaternion.Identity);
        var grid = at.Snapped(Vector3.One * .1f);
        Assert.Equal((grid.X, grid.Z, Quaternion.Identity), (position.X, position.Z, rotation));
        Assert.True(MathF.Abs(position.Y - (FaceTop + halfHeight)) < 1e-5f, $"{part} rests at {position.Y}, face top {FaceTop}");
    }

    // F-1 through the real path: two Dominoes composed onto a 0.5 kg plate stand on its face and sum to 0.8 kg, so the plate loads and
    // its gate opens; one Domino is the Underweight control (0.4 kg).
    [Theory]
    [InlineData(2, true)]
    [InlineData(1, false)]
    public async Task DominoesPlacedOnAPlateFromThePaletteOpenTheGateOnlyWhenTheySum(int dominoes, bool opens)
    {
        var definitions = Definitions();
        var gate = WorkshopInput.Generic(PartId.PoweredGate, new(1), 0, 3, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(2), 3, 3, 0);
        var plate = PlacedParts.Plate(new(3), -4, PlateY, 0, .5f);
        var placed = new List<IWorkshopInstance> { gate, battery, plate };
        foreach (var x in dominoes == 2 ? new[] { -4.4f, -3.6f } : [-4f])
        {
            var id = (ulong)placed.Count + 1;
            var (position, rotation) = Composed(definitions, placed, new(PartId.Domino, new(id), false), new(x, PlateY, 0), Quaternion.Identity);
            Assert.True(MathF.Abs(position.Y - (FaceTop + .55f)) < 1e-5f, $"domino {id} rests at {position.Y}");
            placed.Add(PlacedParts.Domino(new(id), position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, rotation.W));
        }
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([.. placed]), Connections: new(
            new WorkshopConnection(battery.Id, WorkshopSocket.Supply, plate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical),
            new WorkshopConnection(plate.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        var reads = (await WorkshopPhysicsEngineTests.RunPhases(bench, ([], 2 * 120)))[0];
        var load = reads[^1].Presentation.Loads[0];
        Assert.True(reads[^1].Bodies.TryGet(WorkshopPhysicsCompiler.SliderBody(gate.Id, 0), out var blade));
        var travel = (blade.Body.Cell.Y + blade.Body.Local.Y) / 16f - 3f;
        Assert.Equal(opens ? ContactLoadPhase.Loaded : ContactLoadPhase.Underweight, load.Phase);
        Assert.True(opens ? travel > 1.3f : travel < .2f, $"{dominoes} domino(es): gate travel {travel}");
        foreach (var domino in placed.Skip(3))
        {
            Assert.True(reads[^1].Bodies.TryGet(domino.Id, out var standing));
            Assert.True(MathF.Abs((standing.Body.Cell.Y + standing.Body.Local.Y) / 16f - (FaceTop + .55f)) < .01f, "the domino stands on the face");
        }
    }

    // Unchanged-pose controls: each composes exactly the base pose (snap, else grid).
    // - a Basketball above a funnel (no overlap) and in its throat (overlapping only tilted walls: no horizontal top);
    // - a Domino overlapping the side of a Wall (the wall's top is above the domino, and its centre is beside the wall);
    // - a Basketball dropped into a ramp tilted 20° or 5° (no horizontal top);
    // - a Basketball overlapping a Wall whose top is above it;
    // - a static part (a Wall) dropped into a plate (only a Dynamic part rests);
    // - a Basketball inside a gate tilted −15°, in a detector's bore tilted −90° and in a bend tilted −135° (the recipes' patterns);
    // - a pipe at another pipe's mouth (the tube snap wins).
    [Fact]
    public void EveryOtherPlacementKeepsTheBasePose()
    {
        var definitions = Definitions();
        var tilt = new Quaternion(Vector3.Back, Mathf.DegToRad(20));
        var cases = new List<(string Name, IWorkshopInstance[] Placed, TubeMouthMover Mover, Vector3 At, Quaternion Rotation)>
        {
            ("ball above a funnel", [At(PartId.Funnel, 1, new(0, 1, 0), Quaternion.Identity)], new(PartId.Basketball, new(2), false), new(0, 2.8f, 0), Quaternion.Identity),
            ("ball in a funnel", [At(PartId.Funnel, 1, new(0, 1, 0), Quaternion.Identity)], new(PartId.Basketball, new(2), false), new(0, 1.2f, 0), Quaternion.Identity),
            ("domino beside a wall", [At(PartId.Wall, 1, new(0, .6f, 0), Quaternion.Identity)], new(PartId.Domino, new(2), false), new(0, .2f, .3f), Quaternion.Identity),
            ("ball in a tilted ramp", [At(PartId.Ramp, 1, new(0, 0, 0), tilt)], new(PartId.Basketball, new(2), false), new(0, .2f, 0), Quaternion.Identity),
            // A 5° ramp's highest corner (0.22 m) lies within the ball's extent and the ball's centre is over the ramp, yet its top is not
            // horizontal, so the ball is not lifted.
            ("ball in a ramp tilted 5°", [At(PartId.Ramp, 1, new(0, 0, 0), new Quaternion(Vector3.Back, Mathf.DegToRad(5)))],
                new(PartId.Basketball, new(2), false), new(0, .2f, 0), Quaternion.Identity),
            ("ball inside a tall wall", [At(PartId.Wall, 1, new(0, 1, 0), Quaternion.Identity)], new(PartId.Basketball, new(2), false), new(0, .5f, 0), Quaternion.Identity),
            ("wall into a plate", [PlacedParts.Plate(new(1), 0, PlateY, 0, .5f)], new(PartId.Wall, new(2), false), new(0, PlateY, 0), Quaternion.Identity),
            // The recipes' commonest pattern (cat-017, cat-020, cat-051, cat-052): a Basketball placed inside a Powered gate tilted −15°.
            ("ball inside a gate tilted -15°", [At(PartId.PoweredGate, 1, new(0, 3, 0), new Quaternion(Vector3.Back, Mathf.DegToRad(-15)))],
                new(PartId.Basketball, new(2), false), new(-.08f, 3, 0), Quaternion.Identity),
            // A Basketball in the bore of a Ball detector tilted −90° (cat-002, cat-020): its ring slats have a vertical axis, but the
            // ball's centre is over none of their footprints.
            ("ball in a detector bore tilted -90°", [At(PartId.BallDetector, 1, new(0, 3, 0), new Quaternion(Vector3.Back, Mathf.DegToRad(-90)))],
                new(PartId.Basketball, new(2), false), new(0, 3, 0), Quaternion.Identity),
            // A Basketball in a 90° bend tilted −135° (capacity-9r's cradle): no box axis is vertical to within 1 − 10⁻⁶.
            ("ball in a bend tilted -135°", [At(PartId.PipeBend90, 1, new(0, 3, 0), new Quaternion(Vector3.Back, Mathf.DegToRad(-135)))],
                new(PartId.Basketball, new(2), false), new(0, 3, 0), Quaternion.Identity),
        };
        var pipe = WorkshopInput.Pipe(new(1), 0, 1, 0, 0, 0, 0, 1);
        var mouth = TubeMouthSnap.LocalMouth(definitions[PartId.Pipe], TubeMouthEnd.End, PipeLength.Default);
        var pipeMover = new TubeMouthMover(PartId.Pipe, new(2), false, PipeLength.Default);
        var pipeAt = new Vector3(0, 1, 0) + mouth + new Vector3(1.9f, .05f, 0);
        Assert.NotNull(TubeMouthSnap.Find(definitions, [pipe], pipeMover, pipeAt.Snapped(Vector3.One * .1f), Quaternion.Identity)); // control: it snaps
        cases.Add(("pipe at a pipe mouth", [pipe], pipeMover, pipeAt, Quaternion.Identity));
        foreach (var (name, placed, mover, at, rotation) in cases)
            Assert.True(Base(definitions, placed, mover, at, rotation) == Composed(definitions, placed, mover, at, rotation),
                $"{name}: base {Base(definitions, placed, mover, at, rotation)}, composed {Composed(definitions, placed, mover, at, rotation)}");
    }

    // Coordinator decision B1: the lifted pose is validated. A Basketball or Domino dropped into a plate at 8.9 m would rest above the
    // 9 m bench volume (OutsideBench); the same drop into a plate at 8 m is admitted (control).
    [Theory]
    [MemberData(nameof(DynamicParts))]
    public void ARestedPoseAboveTheBenchIsRefused(PartId part, float halfHeight)
    {
        var definitions = Definitions();
        foreach (var (height, refusal) in new[] { (8.9f, PlacementRefusal.OutsideBench), (8f, PlacementRefusal.None) })
        {
            IWorkshopInstance[] placed = [PlacedParts.Plate(new(1), 0, height, 0, .5f)];
            var composed = PlacementPose.Compose(definitions, placed, new(part, new(2), false), new(0, height, 0), Quaternion.Identity);
            Assert.Equal(refusal, composed.Refusal);
            Assert.True(MathF.Abs(composed.Position.Y - (height + .08f + halfHeight)) < 1e-4f, $"{part} at {height}: {composed.Position.Y}");
        }
    }

    // B1: a part lifted onto a lower top into a higher box is refused (IntoPart). A Basketball at 0.7 m in a plate at 1 m overlaps only
    // the base slab's top (0.98 m; the face's 1.08 m top is above the ball), so rest lifts it to 1.32 m, inside the face (0.98–1.08 m).
    // Control: a ball dropped where both tops lie within its height rests on the face, touching it, and is admitted.
    [Fact]
    public void ALiftIntoAHigherBoxIsRefused()
    {
        var definitions = Definitions();
        IWorkshopInstance[] placed = [PlacedParts.Plate(new(1), 0, 1, 0, .5f)];
        var mover = new TubeMouthMover(PartId.Basketball, new(2), false);
        var refused = PlacementPose.Compose(definitions, placed, mover, new(0, .7f, 0), Quaternion.Identity);
        Assert.Equal(PlacementRefusal.IntoPart, refused.Refusal);
        Assert.True(MathF.Abs(refused.Position.Y - (.98f + .34f)) < 1e-4f, $"lifted to {refused.Position.Y}");
        var admitted = PlacementPose.Compose(definitions, placed, mover, new(0, 1f, 0), Quaternion.Identity);
        Assert.Equal(PlacementRefusal.None, admitted.Refusal);
        Assert.True(MathF.Abs(admitted.Position.Y - (1.08f + .34f)) < 1e-4f, $"rests at {admitted.Position.Y}");
    }

    // The rest rule's own boundaries: a Basketball whose centre lies just outside the box's footprint is not lifted, and one held above
    // it (no overlap) is not moved. Control: the same ball 0.05 m inside the footprint rests on the top.
    [Fact]
    public void RestNeedsTheCentreOverTheFootprintAndATrueOverlap()
    {
        var definitions = Definitions();
        IWorkshopInstance[] placed = [At(PartId.Ramp, 1, new(0, 0, 0), Quaternion.Identity)]; // a flat 3 m x 1.3 m box, its top at 0.09 m
        var mover = new TubeMouthMover(PartId.Basketball, new(2), false);
        var outside = new Vector3(0, .1f, .7f); // |z| = 0.7 > 0.65 (the box's half width)
        Assert.Equal(Base(definitions, placed, mover, outside, Quaternion.Identity), Composed(definitions, placed, mover, outside, Quaternion.Identity));
        var inside = new Vector3(0, .1f, .6f);
        var rested = Composed(definitions, placed, mover, inside, Quaternion.Identity).Position;
        Assert.True(MathF.Abs(rested.Y - (.09f + .34f)) < 1e-5f, $"rests at {rested.Y}");
        var above = new Vector3(0, .5f, 0); // its lowest point 0.16 m is above the 0.09 m top
        Assert.Equal(Base(definitions, placed, mover, above, Quaternion.Identity), Composed(definitions, placed, mover, above, Quaternion.Identity));
    }
}
