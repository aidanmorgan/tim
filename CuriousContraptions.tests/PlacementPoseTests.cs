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


    [Theory]
    [InlineData(1.4f, false)]
    [InlineData(1.2f, true)]
    public void BendGridPlacementUsesItsOccupiedArcBounds(float y, bool outside)
    {
        // The quarter bend's lowest geometry is its end stub at -1.787056,
        // not a reflected copy of the arc above its revolution origin. At y=1.4
        // its bottom is -0.387056, inside the -0.46 bench by less than one grid step.
        var pose = PlacementPose.Compose(Definitions(), [], new(new PartId("pipe_bend_90"), new(1), false),
            new Vector3(0, y, 0), Quaternion.Identity);
        Assert.Null(pose.Snap);
        Assert.Equal(outside ? PlacementRefusal.OutsideBench : PlacementRefusal.None, pose.Refusal);
    }

    [Theory]
    [InlineData(1.4f, false)]
    [InlineData(1.2f, true)]
    public void SnappedBendUsesItsOccupiedArcBounds(float y, bool outside)
    {
        var definitions = Definitions();
        var start = TubeMouthSnap.LocalMouth(definitions[new PartId("pipe_bend_90")], TubeMouthEnd.Start, null);
        var end = TubeMouthSnap.LocalMouth(definitions[new PartId("pipe")], TubeMouthEnd.End, InstanceParameterCollection.Defaults(new("pipe")));
        var target = new Vector3(0, y, 0);
        var pipeAt = target + start - end;
        IWorkshopInstance[] placed = [WorkshopInput.Generic(new PartId("pipe"),new(1), (float)(pipeAt.X), (float)(pipeAt.Y), (float)(pipeAt.Z), 0, 0, 0, 1)];
        var pose = PlacementPose.Compose(definitions, placed, new(new PartId("pipe_bend_90"), new(2), false), target, Quaternion.Identity);
        Assert.NotNull(pose.Snap);
        Assert.InRange((pose.Position - target).Length(), 0, 1e-5f);
        Assert.Equal(outside ? PlacementRefusal.SnapOutsideBench : PlacementRefusal.None, pose.Refusal);
    }

    public enum HollowPayload { Sphere, Box, Cylinder }

    [Theory]
    [InlineData(HollowPayload.Sphere, 0f, false)]
    [InlineData(HollowPayload.Sphere, .6f, true)]
    [InlineData(HollowPayload.Sphere, 1.5f, false)]
    [InlineData(HollowPayload.Box, 0f, false)]
    [InlineData(HollowPayload.Box, .6f, true)]
    [InlineData(HollowPayload.Box, 1.5f, false)]
    [InlineData(HollowPayload.Cylinder, 0f, false)]
    [InlineData(HollowPayload.Cylinder, .6f, true)]
    [InlineData(HollowPayload.Cylinder, 1.5f, false)]
    [InlineData(HollowPayload.Box, .6f, true, true)]
    [InlineData(HollowPayload.Box, 1.5f, false, true)]
    [InlineData(HollowPayload.Cylinder, .6f, true, true)]
    [InlineData(HollowPayload.Cylinder, 1.5f, false, true)]
    public async Task HollowWallAdmissionAgreesBetweenPlacementAndConstruct(HollowPayload payload, float offset, bool overlaps, bool tilted = false)
    {
        var definitions = Definitions();
        var part = payload switch
        {
            HollowPayload.Sphere => new PartId("ball"),
            HollowPayload.Box => new PartId("domino"),
            HollowPayload.Cylinder => WeightTests.Weight,
            _ => throw new ArgumentOutOfRangeException(nameof(payload))
        };
        var turn = payload == HollowPayload.Cylinder ? new Quaternion(Vector3.Back, -MathF.PI / 2) : Quaternion.Identity;
        if (tilted) turn = new Quaternion(Vector3.Forward, MathF.PI / 4) * turn;
        var pipe = WorkshopInput.Generic(new PartId("pipe"),new(1), 0, 3, 0, 0, 0, 0, 1);
        IWorkshopInstance[] placed = [pipe];
        var at = new Vector3(0, 3 + offset, 0);
        var pose = PlacementPose.Compose(definitions, placed, new(part, new(2), false), at, turn);
        Assert.Equal(overlaps ? PlacementRefusal.IntoPart : PlacementRefusal.None, pose.Refusal);
        Assert.Equal(at.Snapped(Vector3.One * .1f), pose.Position);
        Assert.Single(placed); // a rejected preview never changes the authored list
        var prior = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(pipe));
        await using var ring = await WorkshopIntegrationHarness.Building(prior);
        var before = ring.Committed;
        var candidate = prior with { Revision = new(3), Instances = new(pipe, At(part, 2, at, turn)) };
        if (overlaps)
        {
            EditRefusal.Is(WorkshopEditRejection.InitialOverlap, () => CompiledSceneFixture.Compile(candidate));
            Assert.Equal(prior, ring.Construction);
            Assert.Equal(before.Revision, ring.Committed.Revision);
            Assert.Equal(before.Bodies, ring.Committed.Bodies);
            await ring.Run();
            await ring.Advance(1);
            await ring.Reset();
        }
        else Assert.Equal(WorkshopCommandOutcome.Applied,
            (await ring.Execute(WorkshopCommandKind.Construct, candidate)).Result.Outcome);
    }

    private static readonly float PlateY = (float)((double)WorkshopConstruction.WorkbenchSurface.Value + .22);
    /// <summary>The plate face's top: its centre is 0.03 m above the plate origin and it is 0.1 m thick (the plate scene).</summary>
    private static readonly float FaceTop = PlateY + .03f + .05f;
    // Literal plate artwork: ring centre y=.084, outer tube radius=.018.
    private static readonly float RingTop = PlateY + .084f + .018f;

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
        return WorkshopInput.Generic(part, new(id), (float)(position.X), (float)(position.Y), (float)(position.Z), (float)(q.X), (float)(q.Y), (float)(q.Z), (float)(q.W), parameters);
    }

    // A Domino dropped into the plate (its centre at the plate origin height, straddling the face) rests with its foot on the face:
    // its centre rises to the face top + 0.55 m; x, z and the rotation are the grid's. So does a Basketball (0.34 m) and a Bowling
    // ball (0.28 m). The highest qualifying top wins: the base slab's top (0.1 m lower) is also overlapped.
    public static TheoryData<PartId, float> DynamicParts() => new() { { new PartId("domino"), .55f }, { new PartId("ball"), .34f }, { new PartId("bowling"), .28f } };

    [Theory]
    [MemberData(nameof(DynamicParts))]
    public void ADynamicPartDroppedIntoAPlateRestsOnItsFace(PartId part, float halfHeight)
    {
        var definitions = Definitions();
        IWorkshopInstance[] placed = [PlacedParts.Plate(new(1), -4, (float)(PlateY), 0, .5f)];
        var at = new Vector3(-3.6f, PlateY, .2f);
        var (position, rotation) = Composed(definitions, placed, new(part, new(2), false), at, Quaternion.Identity);
        var grid = at.Snapped(Vector3.One * .1f);
        Assert.Equal((grid.X, grid.Z, Quaternion.Identity), (position.X, position.Z, rotation));
        Assert.True(MathF.Abs(position.Y - ((part == new PartId("domino") ? RingTop : FaceTop) + halfHeight)) < 1e-5f, $"{part} rests at {position.Y}, face top {FaceTop}");
    }

    // Story 10.2a: the Weight is the first dynamic cylinder in the palette. Its origin is its base centre, so dropped upright into a
    // plate (raised to 1 m, inside the bench volume) it rests with its base on the face (the origin rises to the face top, 1.08 m);
    // lying on its side (axis along z) its lowest point is the curved side, r below the axis, so the origin rises to the face top + r.
    // x, z and the rotation are the grid's.
    [Fact]
    public void ADroppedWeightRestsOnThePlateFaceUprightOnItsBaseAndOnItsSide()
    {
        var definitions = Definitions();
        IWorkshopInstance[] placed = [PlacedParts.Plate(new(1), -4, 1, 0, .5f)];
        const float face = 1f + .03f + .05f;
        var weight = new TubeMouthMover(WeightTests.Weight, new(2), false);
        var at = new Vector3(-3.6f, 1f, .2f);
        var grid = at.Snapped(Vector3.One * .1f);
        var (upright, turn) = Composed(definitions, placed, weight, at, Quaternion.Identity);
        Assert.Equal((grid.X, grid.Z, Quaternion.Identity), (upright.X, upright.Z, turn));
        Assert.True(MathF.Abs(upright.Y - (1f + .084f + .018f)) < 1e-5f, $"upright Weight rests at {upright.Y}, face top {face}");
        var side = new Quaternion(Vector3.Right, MathF.PI / 2);
        // At x=.7 the nearest ring material is .382m away horizontally, beyond the
        // literal band reach (.3558+.0075)*scale. The band rests directly on the flat face.
        var scale = .136734f * 4f + .453070f;
        var radius = (.3558f + .0075f) * scale;
        var (lying, _) = Composed(definitions, placed, weight, new(-3.3f, 1f, .2f), side);
        Assert.True(MathF.Abs(lying.Y - (face + radius)) < 1e-5f, $"lying Weight rests at {lying.Y}, face top + r {face + radius}");
        // Control: held clear above the face it is not moved.
        var above = new Vector3(-3.6f, face + .5f, .2f);
        Assert.Equal(above.Snapped(Vector3.One * .1f), Composed(definitions, placed, weight, above, Quaternion.Identity).Position);
    }

    // F-1 through the real path: two Dominoes composed onto a 0.5 kg plate stand on its face and sum to 0.8 kg, so the plate loads and
    // its gate opens; one Domino is the Underweight control (0.4 kg).
    [Theory]
    [InlineData(2, true)]
    [InlineData(1, false)]
    public async Task DominoesPlacedOnAPlateFromThePaletteOpenTheGateOnlyWhenTheySum(int dominoes, bool opens)
    {
        var definitions = Definitions();
        var gate = WorkshopInput.Generic(new PartId("powered_gate"), new(1), 0, 3, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(2), 3, 3, 0);
        var plate = PlacedParts.Plate(new(3), -4, (float)(PlateY), 0, .5f);
        var placed = new List<IWorkshopInstance> { gate, battery, plate };
        // Centres±.6 keep the .125m half-width outside the ring's .318m outer reach;
        // the .325m half-depth is clear of studs at z±.62. The single centre control rests on the ring.
        foreach (var x in dominoes == 2 ? new[] { -4.6f, -3.4f } : [-4f])
        {
            var id = (ulong)placed.Count + 1;
            var (position, rotation) = Composed(definitions, placed, new(new PartId("domino"), new(id), false), new(x, PlateY, 0), Quaternion.Identity);
            Assert.True(MathF.Abs(position.Y - ((dominoes == 2 ? FaceTop : RingTop) + .55f)) < 1e-5f, $"domino {id} rests at {position.Y}");
            placed.Add(PlacedParts.Domino(new(id), (float)(position.X), (float)(position.Y), (float)(position.Z), (float)(rotation.X), (float)(rotation.Y), (float)(rotation.Z), (float)(rotation.W)));
        }
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new([.. placed]), Connections: new(
            new WorkshopConnection(battery.Id, WorkshopSocket.Supply, plate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical),
            new WorkshopConnection(plate.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)));
        var reads = (await WorkshopIntegrationHarness.ObservePhases(bench, ([], 2 * 120)))[0];
        var load = reads[^1].Presentation.Loads[0];
        Assert.True(reads[^1].Bodies.TryGet(bench.Instances.Identities[gate.Id].Sliders[0].Body, out var blade));
        var travel = (blade.Body.Cell.Y + blade.Body.Local.Y) / 16f - 3f;
        Assert.Equal(opens ? ContactLoadPhase.Loaded : ContactLoadPhase.Underweight, load.Phase);
        Assert.True(opens ? travel > 1.3f : travel < .2f, $"{dominoes} domino(es): gate travel {travel}");
        foreach (var domino in placed.Skip(3))
        {
            Assert.True(reads[^1].Bodies.TryGet(domino.Id, out var standing));
            Assert.True(MathF.Abs((standing.Body.Cell.Y + standing.Body.Local.Y) / 16f - ((dominoes == 2 ? FaceTop : RingTop) + .55f)) < .01f, "the domino stands on the face");
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
            ("ball above a funnel", [At(new PartId("funnel"), 1, new(0, 1, 0), Quaternion.Identity)], new(new PartId("ball"), new(2), false), new(0, 2.8f, 0), Quaternion.Identity),
            ("ball in a funnel", [At(new PartId("funnel"), 1, new(0, 1, 0), Quaternion.Identity)], new(new PartId("ball"), new(2), false), new(0, 1.2f, 0), Quaternion.Identity),
            ("domino beside a wall", [At(new PartId("wall"), 1, new(0, .6f, 0), Quaternion.Identity)], new(new PartId("domino"), new(2), false), new(0, .2f, .3f), Quaternion.Identity),
            ("ball in a tilted ramp", [At(new PartId("ramp"), 1, new(0, 0, 0), tilt)], new(new PartId("ball"), new(2), false), new(0, .2f, 0), Quaternion.Identity),
            // A 5° ramp's highest corner (0.22 m) lies within the ball's extent and the ball's centre is over the ramp, yet its top is not
            // horizontal, so the ball is not lifted.
            ("ball in a ramp tilted 5°", [At(new PartId("ramp"), 1, new(0, 0, 0), new Quaternion(Vector3.Back, Mathf.DegToRad(5)))],
                new(new PartId("ball"), new(2), false), new(0, .2f, 0), Quaternion.Identity),
            ("ball inside a tall wall", [At(new PartId("wall"), 1, new(0, 1, 0), Quaternion.Identity)], new(new PartId("ball"), new(2), false), new(0, .5f, 0), Quaternion.Identity),
            // The 2 m Wall's centre 1 m above the plate, so its body reaches into the plate face but not into the bench (10.2a F3: every
            // placement is bounded by the body; centred on the plate it would reach 0.78 m below the bench and be refused).
            ("wall into a plate", [PlacedParts.Plate(new(1), 0, (float)(PlateY), 0, .5f)], new(new PartId("wall"), new(2), false), new(0, PlateY + 1, 0), Quaternion.Identity),
            // The recipes' commonest pattern (cat-017, cat-020, cat-051, cat-052): a Basketball placed inside a Powered gate tilted −15°.
            ("ball inside a gate tilted -15°", [At(new PartId("powered_gate"), 1, new(0, 3, 0), new Quaternion(Vector3.Back, Mathf.DegToRad(-15)))],
                new(new PartId("ball"), new(2), false), new(-.08f, 3, 0), Quaternion.Identity),
            // A Basketball in the bore of a Ball detector tilted −90° (cat-002, cat-020): its ring slats have a vertical axis, but the
            // ball's centre is over none of their footprints.
            ("ball in a detector bore tilted -90°", [At(new PartId("ball_detector"), 1, new(0, 3, 0), new Quaternion(Vector3.Back, Mathf.DegToRad(-90)))],
                new(new PartId("ball"), new(2), false), new(0, 3, 0), Quaternion.Identity),
            // A Basketball in a 90° bend tilted −135° (capacity-9r's cradle): no box axis is vertical to within 1 − 10⁻⁶.
            ("ball in a bend tilted -135°", [At(new PartId("pipe_bend_90"), 1, new(0, 3, 0), new Quaternion(Vector3.Back, Mathf.DegToRad(-135)))],
                new(new PartId("ball"), new(2), false), new(0, 3, 0), Quaternion.Identity),
        };
        var pipe = WorkshopInput.Generic(new PartId("pipe"),new(1), 0, 1, 0, 0, 0, 0, 1);
        var mouth = TubeMouthSnap.LocalMouth(definitions[new PartId("pipe")], TubeMouthEnd.End, InstanceParameterCollection.Defaults(new("pipe")));
        var pipeMover = new TubeMouthMover(new PartId("pipe"), new(2), false, InstanceParameterCollection.Defaults(new("pipe")));
        var pipeAt = new Vector3(0, 1, 0) + mouth + new Vector3(1.9f, .05f, 0);
        Assert.NotNull(TubeMouthSnap.Find(definitions, [pipe], pipeMover, pipeAt.Snapped(Vector3.One * .1f), Quaternion.Identity)); // control: it snaps
        cases.Add(("pipe at a pipe mouth", [pipe], pipeMover, pipeAt, Quaternion.Identity));
        foreach (var (name, placed, mover, at, rotation) in cases)
            Assert.True(Base(definitions, placed, mover, at, rotation) == Composed(definitions, placed, mover, at, rotation),
                $"{name}: base {Base(definitions, placed, mover, at, rotation)}, composed {Composed(definitions, placed, mover, at, rotation)}");
    }

    // Coordinator decision B1: the lifted pose is validated. A Basketball, Domino or Bowling ball dropped into a plate at 8.9 m would
    // rest above the 9 m bench volume (OutsideBench); the same drop into a plate at 7.5 m keeps the whole body inside it and is admitted
    // (control). Since 10.2a F3 the volume bounds the body's extent, so the control height keeps the Domino's top (7.5 + 0.08 + 1.1 m)
    // under 9 m; the 8 m control used before admitted a Domino whose top was at 9.18 m.
    [Theory]
    [MemberData(nameof(DynamicParts))]
    public void ARestedPoseAboveTheBenchIsRefused(PartId part, float halfHeight)
    {
        var definitions = Definitions();
        foreach (var (height, refusal) in new[] { (8.9f, PlacementRefusal.RestOutsideBench), (7.5f, PlacementRefusal.None) })
        {
            IWorkshopInstance[] placed = [PlacedParts.Plate(new(1), 0, (float)(height), 0, .5f)];
            var composed = PlacementPose.Compose(definitions, placed, new(part, new(2), false), new(0, height, 0), Quaternion.Identity);
            Assert.Equal(refusal, composed.Refusal);
            Assert.True(MathF.Abs(composed.Position.Y - (height + (part == new PartId("domino") ? .084f + .018f : .08f) + halfHeight)) < 1e-4f, $"{part} at {height}: {composed.Position.Y}");
        }
    }

    // 10.2a F3 (coordinator decision, 24 Oct 2026): the bench volume bounds the rested body's world-space extent, not its origin. On the
    // plate standing on the bench, a Weight's origin is its base (the face, below y = 0, which the point volume refuses) while its
    // whole body is above the bench: admitted, as are a Basketball and a Domino (a box) on the same plate. On a plate whose face is at
    // 8.9 m the Weight's origin (8.9 m) lies in the point volume but its top (8.9 m + 2h) is above 9 m: refused, as are the ball and the box.
    [Fact]
    public void TheBenchVolumeBoundsTheRestedBodyNotItsOrigin()
    {
        var definitions = Definitions();
        var h = TestBundle.Entry(WeightTests.Weight).Colliders[0].HalfExtents.Y;
        foreach (var (plate, refusal) in new[] { (PlateY, PlacementRefusal.None), (8.9f - .08f, PlacementRefusal.RestOutsideBench) })
        {
            var face = plate + .08f;
            IWorkshopInstance[] placed = [PlacedParts.Plate(new(1), 0, (float)(plate), 0, .5f)];
            foreach (var (part, origin, top) in new[] { (WeightTests.Weight, plate + .084f + .018f, plate + .084f + .018f + 2 * h), (new PartId("ball"), face + .34f, face + .68f),
                         (new PartId("domino"), plate + .084f + .018f + .55f, plate + .084f + .018f + 1.1f) })
            {
                var composed = PlacementPose.Compose(definitions, placed, new(part, new(2), false), new(0, plate, 0), Quaternion.Identity);
                Assert.True(refusal == composed.Refusal, $"{part} on the plate at {plate} m (top {top} m): {composed.Refusal}");
                Assert.True(MathF.Abs(composed.Position.Y - origin) < 1e-4f, $"{part} on the plate at {plate} m: origin {composed.Position.Y}");
            }
        }
        // The point rule this replaces refused the low Weight and admitted the high one.
        Assert.False(PlacementPose.Inside(new(0, PlateY + .08f, 0)));
        Assert.True(PlacementPose.Inside(new(0, 8.9f, 0)));
    }

    // Review R1 (10.2a): a lifted cylinder is tested against a static sphere at its exact closest point, not as a point. A Weight
    // dropped upright onto the bench-standing plate beside a Bumper (a static sphere, r 0.65) whose centre is 0.9 m from the Weight's
    // axis at its mid-height reaches 0.35 m from the axis while the sphere reaches 0.25 m: refused IntoPart. With the Bumper 1.05 m away
    // (a 0.05 m gap) it is admitted. A Basketball (r 0.34) dropped on the same spot gets the same verdict each time.
    [Fact]
    public void ALiftedWeightBesideABumperIsRefusedAndAClearGapIsAdmittedLikeABall()
    {
        var definitions = Definitions();
        var face = PlateY + .08f;
        var h = TestBundle.Entry(WeightTests.Weight).Colliders[0].HalfExtents.Y;
        foreach (var (distance, refusal) in new[] { (.9, PlacementRefusal.IntoPart), (1.05, PlacementRefusal.None) })
        {
            IWorkshopInstance[] placed = [PlacedParts.Plate(new(1), 0, (float)(PlateY), 0, .5f),
                WorkshopInput.Generic(new PartId("bumper"), new(3), (float)(distance), (float)(face + h), 0, 0, 0, 0, 1)];
            foreach (var part in new[] { WeightTests.Weight, new PartId("ball") })
            {
                var composed = PlacementPose.Compose(definitions, placed, new(part, new(2), false), new(0, PlateY, 0), Quaternion.Identity);
                Assert.True(refusal == composed.Refusal, $"{part} with the Bumper {distance} m away: {composed.Refusal}");
            }
        }
    }

    // engine-hollow-shapes: a lifted part is tested against a hollow static's true wall (its exact signed distance), whose bore stays
    // open. A default Clear pipe lies along X over the bench-standing plate. With its axis at the plate face + 0.34, a Basketball lifted
    // onto the face sits centred in the 0.65 m bore and a Weight (r 0.35, top 0.56 above the face) reaches √(0.35² + 0.34²) = 0.49 m
    // from the axis: both admitted. With the axis 0.45 m higher, the ball (centre 0.45 below the axis) reaches 0.79 m from it, through
    // the 0.65–0.7 m wall, and the Weight stands across the wall (its base 0.79 below the axis, its top 0.23 below it): both refused
    // IntoPart.
    [Fact]
    public void ALiftedPartCuttingAPipeWallIsRefusedAndOneInsideItsBoreIsAdmitted()
    {
        var definitions = Definitions();
        var face = PlateY + .08f;
        foreach (var (rise, refusal) in new[] { (0f, PlacementRefusal.None), (.45f, PlacementRefusal.IntoPart) })
        {
            IWorkshopInstance[] placed = [PlacedParts.Plate(new(1), 0, (float)(PlateY), 0, .5f), WorkshopInput.Generic(new PartId("pipe"),new(3), 0, (float)(face + .34f + rise), 0, 0, 0, 0, 1)];
            foreach (var part in new[] { new PartId("ball"), WeightTests.Weight })
            {
                var composed = PlacementPose.Compose(definitions, placed, new(part, new(2), false), new(0, PlateY, 0), Quaternion.Identity);
                Assert.True(refusal == composed.Refusal, $"{part} with the pipe's axis {rise} m above the ball's rest: {composed.Refusal}");
            }
        }
    }

    // 10.2a F3 for every placement path: an unlifted (grid) pose is bounded by the part's world-space extent too, not its point. With no
    // part on the bench, at x = 0 a Weight (base origin, top 2h ≈ 0.56 m up), a Basketball (r 0.34) and a Domino (a box, centre origin,
    // half 0.125 × 0.55 × 0.325) are admitted at 1 m. At 8.5 m the Weight's top (9.06) and the Domino's (9.05) are above 9 m and refused,
    // while the ball's (8.84) is admitted; at 8.9 m all are refused, although the point 8.9 m lies in the point volume. At x = 6.9 m each
    // body crosses x = 7 (Weight 7.25, ball 7.24, Domino 7.025) and is refused; at 6.6 m each is inside and admitted.
    [Fact]
    public void AGridPlacementIsBoundedByTheBodyNotItsPoint()
    {
        var definitions = Definitions();
        foreach (var (at, weight, ball, box) in new[]
        {
            (new Vector3(0, 1, 0), PlacementRefusal.None, PlacementRefusal.None, PlacementRefusal.None),
            (new Vector3(0, 8.5f, 0), PlacementRefusal.OutsideBench, PlacementRefusal.None, PlacementRefusal.OutsideBench),
            (new Vector3(0, 8.9f, 0), PlacementRefusal.OutsideBench, PlacementRefusal.OutsideBench, PlacementRefusal.OutsideBench),
            (new Vector3(6.9f, 1, 0), PlacementRefusal.OutsideBench, PlacementRefusal.OutsideBench, PlacementRefusal.OutsideBench),
            (new Vector3(6.6f, 1, 0), PlacementRefusal.None, PlacementRefusal.None, PlacementRefusal.None)
        })
        {
            Assert.True(PlacementPose.Inside(at), $"{at} is inside the point volume");
            foreach (var (part, refusal) in new[] { (WeightTests.Weight, weight), (new PartId("ball"), ball), (new PartId("domino"), box) })
            {
                var composed = PlacementPose.Compose(definitions, [], new(part, new(2), false), at, Quaternion.Identity);
                Assert.True(refusal == composed.Refusal, $"{part} at {at}: {composed.Refusal}");
                Assert.Equal(at.Snapped(Vector3.One * .1f), composed.Position);
            }
        }
    }

    // elements-visible-solid-sweep: the proud parts that now collide count toward the bench edge (x ±7, z ±4).
    // - The Counter's power and supply sockets (r 0.075 at x ∓0.93) reach ±1.005, past the foot's ±0.9: at x ±6 the foot alone
    //   (±6.9) was inside, the sockets (±7.005) are not, so both sides refuse OutsideBench; at ±5.9 (±6.905) it is admitted.
    // - Turned a quarter about the vertical, the sockets reach ±1.005 along z: at z 3 (4.005) it is refused, at 2.9 (3.905) admitted.
    // - The Pressure plate's sockets stand out on +z only (the ActivationOut socket, r 0.075 at z 1.18, reaches 1.255), while −z ends
    //   at the base's −1.1: at z 2.8 it reaches 4.055 and is refused, at z −2.8 it reaches −3.9 and is admitted.
    [Fact]
    public void ProudSocketsCountTowardTheBenchEdge()
    {
        var definitions = Definitions();
        var quarter = new Quaternion(Vector3.Up, MathF.PI / 2);
        foreach (var (part, at, rotation, refusal) in new[]
        {
            (new PartId("counter"), new Vector3(6, 2, 0), Quaternion.Identity, PlacementRefusal.OutsideBench),
            (new PartId("counter"), new Vector3(5.9f, 2, 0), Quaternion.Identity, PlacementRefusal.None),
            (new PartId("counter"), new Vector3(-6, 2, 0), Quaternion.Identity, PlacementRefusal.OutsideBench),
            (new PartId("counter"), new Vector3(-5.9f, 2, 0), Quaternion.Identity, PlacementRefusal.None),
            (new PartId("counter"), new Vector3(0, 2, 3), quarter, PlacementRefusal.OutsideBench),
            (new PartId("counter"), new Vector3(0, 2, 2.9f), quarter, PlacementRefusal.None),
            (new PartId("pressure_plate"), new Vector3(0, 2, 2.8f), Quaternion.Identity, PlacementRefusal.OutsideBench),
            (new PartId("pressure_plate"), new Vector3(0, 2, -2.8f), Quaternion.Identity, PlacementRefusal.None)
        })
        {
            var composed = PlacementPose.Compose(definitions, [], new(part, new(2), false), at, rotation);
            Assert.True(refusal == composed.Refusal, $"the {part} at {at}: {composed.Refusal}");
        }
    }

    // Review R2 (10.2a F3 on the snap path): a new 3.6 m Pipe joined at a placed Pipe's end mouth continues it along +x, its start
    // mouth on the placed end mouth, so its centre is twice the mouth's offset (2 × 1.89 m) beyond the placed one's and its body reaches
    // 1.8 m further. Joined to a Pipe centred at x = 3 it would reach x ≈ 8.6, past the bench edge at 7: refused SnapOutsideBench.
    // Control: joined to a Pipe centred at x = −3 it reaches ≈ 2.6 and is admitted.
    // The drop point is inside the point volume in both cases, and the composed pose is the snap's own.
    [Fact]
    public void ATubeMouthSnapAcrossTheBenchEdgeIsRefusedAndOneInsideIsAdmitted()
    {
        var definitions = Definitions();
        var mouth = TubeMouthSnap.LocalMouth(definitions[new PartId("pipe")], TubeMouthEnd.End, InstanceParameterCollection.Defaults(new("pipe")));
        var mover = new TubeMouthMover(new PartId("pipe"), new(2), false, InstanceParameterCollection.Defaults(new("pipe")));
        foreach (var (x, refusal) in new[] { (3f, PlacementRefusal.SnapOutsideBench), (-3f, PlacementRefusal.None) })
        {
            IWorkshopInstance[] placed = [WorkshopInput.Generic(new PartId("pipe"),new(1), (float)(x), 1, 0, 0, 0, 0, 1)];
            var at = new Vector3(x, 1, 0) + mouth + new Vector3(1.9f, .05f, 0);
            Assert.True(PlacementPose.Inside(at), $"the drop point {at} is inside the point volume");
            var composed = PlacementPose.Compose(definitions, placed, mover, at, Quaternion.Identity);
            Assert.NotNull(composed.Snap);
            var joined = composed.Snap.Value;
            Assert.Equal((joined.Position, joined.Rotation), (composed.Position, composed.Rotation));
            Assert.True(MathF.Abs(composed.Position.X - (x + 2 * mouth.X)) < 1e-4f, $"joined at {composed.Position}, mouth offset {mouth.X}");
            Assert.True(refusal == composed.Refusal, $"a Pipe joined to one at x = {x}: {composed.Refusal}");
        }
    }

    // L5: each placement path's refusal is its own typed reason with its own status text; the construction is unchanged, and a pose
    // that is not refused has no refusal message.
    [Fact]
    public void EachPlacementRefusalHasItsOwnStatusText()
    {
        var world = new MachineWorld();
        try
        {
            var before = world.Construction;
            foreach (var (refusal, text) in new[]
            {
                (PlacementRefusal.OutsideBench, "Placement rejected: the part would reach outside the bench there."),
                (PlacementRefusal.SnapOutsideBench, "Placement rejected: joining that tube mouth would take the part outside the bench."),
                (PlacementRefusal.RestOutsideBench, "Placement rejected: resting there would lift the part outside the bench."),
                (PlacementRefusal.IntoPart, "Placement rejected: resting there would push the part into another part.")
            })
            {
                var proposal = WorkshopBenchPlacement.Refuse(world, refusal);
                Assert.Equal((BenchPlacementRejection.UnplaceablePose, before, text), (proposal.Rejection, proposal.Construction, proposal.Message));
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopBenchPlacement.Refuse(world, PlacementRefusal.None));
        }
        finally { world.Free(); }
    }

    // Same-owner upper features compose into the union; a distinct owner's overhead remains a refusal.
    [Fact]
    public void ALiftIntoAHigherBoxIsRefused()
    {
        var definitions = Definitions();
        var plate = PlacedParts.Plate(new(1), 0, 1, 0, .5f);
        var overhead = At(new PartId("wall"), 3, new(0, 1.8f, 0), Quaternion.Identity, WallFixture.Values(1f, .4f, 1f));
        var mover = new TubeMouthMover(new PartId("ball"), new(2), false);
        // The .7m initial centre overlaps only the plate base. Its owner's upper face is resolved,
        // raising the sphere to1.08+.34; the wall begins at1.6 and blocks that final pose.
        var refused = PlacementPose.Compose(definitions, [plate, overhead], mover, new(0, .7f, 0), Quaternion.Identity);
        Assert.Equal(PlacementRefusal.IntoPart, refused.Refusal);
        Assert.True(MathF.Abs(refused.Position.Y - (1.08f + .34f)) < 1e-4f);
        var admitted = PlacementPose.Compose(definitions, [plate], mover, new(0, .7f, 0), Quaternion.Identity);
        Assert.Equal(PlacementRefusal.None, admitted.Refusal);
        Assert.True(MathF.Abs(admitted.Position.Y - (1.08f + .34f)) < 1e-4f);
    }

    [Theory]
    [InlineData(0f, false)]
    [InlineData(37f, false)]
    [InlineData(37f, true)]
    public void CompoundOwnerRestPreservesShiftRotationAndOtherOwnerOrder(float degrees, bool reverse)
    {
        var q = new Quaternion(Vector3.Up, degrees * MathF.PI / 180);
        var plate = At(new PartId("pressure_plate"), 1, new(2, 1, -1), q);
        var distant = At(new PartId("wall"), 3, new(-4, 5, 0), Quaternion.Identity, WallFixture.Values(1, 2, 1));
        IWorkshopInstance[] placed = reverse ? [distant, plate] : [plate, distant];
        var result = PlacementPose.Compose(Definitions(), placed, new(new PartId("domino"), new(2), false), new(2, 1, -1), q);
        Assert.Equal(PlacementRefusal.None, result.Refusal);
        Assert.Equal((2f, -1f), (result.Position.X, result.Position.Z));
        Assert.InRange(MathF.Abs(result.Position.Y - (1f + .084f + .018f + .55f)), 0, 1e-5f);
        Assert.Equal(q, result.Rotation);
    }

    // The rest rule's own boundaries: a Basketball whose centre lies just outside the ramp's footprint is not lifted, and one held
    // above it (no overlap) is not moved. Control: the same ball 0.05 m inside the footprint, over the ramp's +Z rail (0.055 m square
    // at z 0.598, y 0.11, elements-visible-solid), rests on the rail's top at 0.1375 m.
    [Fact]
    public void RestNeedsTheCentreOverTheFootprintAndATrueOverlap()
    {
        var definitions = Definitions();
        IWorkshopInstance[] placed = [At(new PartId("ramp"), 1, new(0, 0, 0), Quaternion.Identity)]; // a 3 m x 1.3 m slab, its top at 0.09 m
        var mover = new TubeMouthMover(new PartId("ball"), new(2), false);
        var outside = new Vector3(0, .1f, .7f); // |z| = 0.7 > 0.65 (the box's half width)
        Assert.Equal(Base(definitions, placed, mover, outside, Quaternion.Identity), Composed(definitions, placed, mover, outside, Quaternion.Identity));
        var inside = new Vector3(0, .1f, .6f);
        var rested = Composed(definitions, placed, mover, inside, Quaternion.Identity).Position;
        Assert.True(MathF.Abs(rested.Y - (.11f + .0275f + .34f)) < 1e-5f, $"rests at {rested.Y}");
        var slab = new Vector3(0, .1f, .2f); // 0.37 m from the rail's inner face (z 0.5705), beyond the 0.34 m radius
        var onSlab = Composed(definitions, placed, mover, slab, Quaternion.Identity).Position;
        Assert.True(MathF.Abs(onSlab.Y - (.09f + .34f)) < 1e-5f, $"rests on the slab at {onSlab.Y}");
        var above = new Vector3(0, .5f, 0); // its lowest point 0.16 m is above the 0.09 m top
        Assert.Equal(Base(definitions, placed, mover, above, Quaternion.Identity), Composed(definitions, placed, mover, above, Quaternion.Identity));
    }

    // elements-visible-solid: an upright cylinder's flat cap is a rest surface under the box-top rule. A Basketball dropped into the
    // Impact switch (at 1 m) over its button (cylinder r 0.38, h 0.18 at y 0.06) rests on the button's top, 1 + 0.15 + 0.34; dropped
    // into the Battery (at 1 m) over its terminal (cylinder r 0.16, h 0.14 at y 0.59) it rests on the terminal's top, 1 + 0.66 + 0.34,
    // the highest top under its centre (the body's top, 0.525, is lower). Control: held clear above either part it is not moved.
    public static TheoryData<PartId, float, float> UprightCaps() => new()
    {
        { new PartId("switch"), 1.1f, 1f + .15f + .34f },
        { new PartId("battery"), 1.5f, 1f + .66f + .34f }
    };

    [Theory]
    [MemberData(nameof(UprightCaps))]
    public void ABallDroppedOverAnUprightCylinderRestsOnItsCap(PartId part, float dropY, float restY)
    {
        var definitions = Definitions();
        IWorkshopInstance[] placed = [At(part, 1, new(0, 1, 0), Quaternion.Identity)];
        var mover = new TubeMouthMover(new PartId("ball"), new(2), false);
        var rested = Composed(definitions, placed, mover, new(0, dropY, 0), Quaternion.Identity).Position;
        Assert.True(MathF.Abs(rested.Y - restY) < 1e-5f, $"a ball over the {part} rests at {rested.Y}, not {restY}");
        var clear = new Vector3(0, 3, 0);
        Assert.Equal(Base(definitions, placed, mover, clear, Quaternion.Identity), Composed(definitions, placed, mover, clear, Quaternion.Identity));
    }

    // The cap's disc is the footprint, not its bounding square. Placement points snap to the 0.1 m grid, so the boundary is shown on
    // the Impact switch's button (r 0.38, top 0.15, at 1 m), whose square holds grid points on both sides of the disc (the Battery's
    // r 0.16 terminal holds none outside it). A Basketball centred at (0.3, ·, 0.2), 0.361 m from the axis (inside the disc), rests on
    // the button's top (1 + 0.15 + 0.34) and is admitted. Centred at (0.3, ·, 0.3), 0.424 m from the axis (inside the square, outside
    // the disc), the eligible base selects its owner, whose button rim supplies the curved support. Its height is derived
    // from the sphere/rim right triangle; the bounding square is never used as the disc footprint.
    [Fact]
    public void AnUprightCapIsARestSurfaceOnlyOverItsDisc()
    {
        var definitions = Definitions();
        IWorkshopInstance[] placed = [At(new PartId("switch"), 1, new(0, 1, 0), Quaternion.Identity)];
        var mover = new TubeMouthMover(new PartId("ball"), new(2), false);
        var inside = PlacementPose.Compose(definitions, placed, mover, new(.3f, 1.1f, .2f), Quaternion.Identity);
        Assert.Equal(PlacementRefusal.None, inside.Refusal);
        Assert.True(MathF.Abs(inside.Position.Y - (1f + .15f + .34f)) < 1e-5f, $"inside the disc it rests at {inside.Position.Y}");
        var corner = PlacementPose.Compose(definitions, placed, mover, new(.3f, 1.1f, .3f), Quaternion.Identity);
        var radialGap = MathF.Sqrt(.3f * .3f + .3f * .3f) - .38f;
        var cornerHeight = 1f + .15f + MathF.Sqrt(.34f * .34f - radialGap * radialGap);
        Assert.True(MathF.Abs(corner.Position.Y - cornerHeight) < 1e-5f, $"outside the disc it rests at {corner.Position.Y}");
        Assert.Equal(PlacementRefusal.None, corner.Refusal); // the eligible base selects the same owner union
    }

    // A lying or tipped cylinder has no flat top, so it is no shelf: a Basketball over the default Delay's face disc (r 0.55, axis +Z,
    // at z 0.32; centred at z 0.5, beyond the housing and the base) and over the button of an Impact switch tipped 90° about Z (axis
    // −X; centred on it, clear of the base's footprint) keeps the base pose.
    [Fact]
    public void ALyingCylinderIsNotARestSurface()
    {
        var definitions = Definitions();
        var mover = new TubeMouthMover(new PartId("ball"), new(2), false);
        var cases = new (IWorkshopInstance[] Placed, Vector3 At)[]
        {
            ([At(new PartId("delay"), 1, new(0, 1, 0), Quaternion.Identity)], new(0, 1.4f, .5f)),
            ([At(new PartId("switch"), 1, new(0, 1, 0), new Quaternion(Vector3.Back, MathF.PI / 2))], new(-.06f, 1.3f, 0)),
        };
        foreach (var (placed, at) in cases)
            Assert.Equal(Base(definitions, placed, mover, at, Quaternion.Identity), Composed(definitions, placed, mover, at, Quaternion.Identity));
    }

    // A box diagonally beside an upright cylinder, inside its bounding square but outside its round side, does not overlap it: an
    // upright Weight (radius r) rested on the plate with a small Wall (0.4 × 0.4 × 0.12 m) whose nearest vertical edge is at (0.8 r, 0.8 r)
    // from the Weight's axis, 0.8·√2·r = 1.13 r away, is admitted. Control: with the edge at (0.6 r, 0.6 r), 0.85 r away, it is
    // refused IntoPart.
    [Theory]
    [InlineData(.8f, PlacementRefusal.None)]
    [InlineData(.6f, PlacementRefusal.IntoPart)]
    public void ABoxDiagonallyBesideAnUprightCylinderOverlapsOnlyItsTrueShape(float fraction, PlacementRefusal refusal)
    {
        var definitions = Definitions();
        var weight = CuriousContraptions.Bundle.BundleGeometry.Colliders(TestBundle.Entry(WeightTests.Weight))[0];
        var (r, h) = (weight.HalfExtents.X, weight.HalfExtents.Y);
        var face = PlateY + .08f;
        var edge = fraction * r;
        IWorkshopInstance[] placed = [PlacedParts.Plate(new(1), 0, (float)(PlateY), 0, .5f),
            At(new PartId("wall"), 3, new(edge + .2f, face + h, edge + .06f), Quaternion.Identity, WallFixture.Values(.4f, .4f, .12f))];
        var composed = PlacementPose.Compose(definitions, placed, new(WeightTests.Weight, new(2), false), new(0, PlateY, 0), Quaternion.Identity);
        Assert.True(MathF.Abs(composed.Position.Y - RingTop) < 1e-5f, $"the Weight rests at {composed.Position.Y}, not on the face {face}");
        Assert.Equal(refusal, composed.Refusal);
    }
}
