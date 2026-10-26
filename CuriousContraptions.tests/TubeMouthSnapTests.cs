using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

/// <summary>Story 9R.6: typed tube-mouth snapping. Mouths are declared on each element's own scene (a `Loc_Port_mouth_*` locator
/// whose local +X is the outward axis, with `metadata/bore_radius`); the pure main-thread query <see cref="TubeMouthSnap.Find"/>
/// proposes a snapped pose. Every applied pose is checked after the canonical f32 cell/local/rotation capture.</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class TubeMouthSnapTests
{
    private const float StandardBore = .65f;
    private const float FunnelInletBore = 1.3f;
    private static readonly PartId[] TubeParts =
        [PartId.Pipe, PartId.PipeBend45, PartId.PipeBend90, PartId.Funnel, PartId.PoweredGate, PartId.BallDetector];

    private static IReadOnlyDictionary<PartId, PartDefinition> Definitions()
    {
        var registry = new PartRegistry();
        registry.Load();
        return registry.Definitions;
    }

    private static Quaternion Rz(float degrees) => new(Vector3.Back, Mathf.DegToRad(degrees));
    private static Quaternion Rx(float degrees) => new(Vector3.Right, Mathf.DegToRad(degrees));

    private static ExtractedPortLocator Mouth(PartDefinition definition, TubeMouthEnd end) =>
        definition.PortLocators.Values.Single(locator => locator.Mouth == end);

    /// <summary>The committed pose of an instance, computed independently of the query: (cell + local) / 16 and its f32 rotation.</summary>
    private static (Vector3 Position, Quaternion Rotation) Pose(IWorkshopInstance instance) =>
        (new((float)((instance.Cell.X + (double)instance.Local.X) / 16), (float)((instance.Cell.Y + (double)instance.Local.Y) / 16),
            (float)((instance.Cell.Z + (double)instance.Local.Z) / 16)),
         new Quaternion(instance.Rotation.X, instance.Rotation.Y, instance.Rotation.Z, instance.Rotation.W).Normalized());

    private static (Vector3 Position, Vector3 Outward) World(IReadOnlyDictionary<PartId, PartDefinition> definitions,
        IWorkshopInstance instance, TubeMouthEnd end)
    {
        var (position, rotation) = Pose(instance);
        var mouth = Mouth(definitions[instance.PartId], end);
        return (position + rotation * mouth.Position, (rotation * (mouth.Rotation * Vector3.Right)).Normalized());
    }

    private static IWorkshopInstance Tube(IReadOnlyDictionary<PartId, PartDefinition> definitions, PartId part, ulong id,
        Vector3 position, Quaternion rotation, bool locked = false)
    {
        _ = definitions[part];
        var q = rotation.Normalized();
        if (part == PartId.Pipe)
        {
            IWorkshopInstance pipe = WorkshopInput.Pipe(new(id), position.X, position.Y, position.Z, q.X, q.Y, q.Z, q.W) with { Locked = locked };
            return pipe;
        }
        var x = WorkshopInput.Position(position.X); var y = WorkshopInput.Position(position.Y); var z = WorkshopInput.Position(position.Z);
        return new WorkshopGenericInstance(part, new(id), new(x.Cell, y.Cell, z.Cell), new(x.Local, y.Local, z.Local),
            CanonicalRotation.Normalize(q.X, q.Y, q.Z, q.W), locked);
    }

    /// <summary>The mover pose that puts its `end` mouth at `mouthAt` with rotation `rotation`.</summary>
    private static Vector3 PlaceMouth(IReadOnlyDictionary<PartId, PartDefinition> definitions, PartId part, TubeMouthEnd end,
        Vector3 mouthAt, Quaternion rotation) => mouthAt - rotation * Mouth(definitions[part], end).Position;

    private static TubeMouthSnapResult? Find(IReadOnlyDictionary<PartId, PartDefinition> definitions, IReadOnlyList<IWorkshopInstance> placed,
        PartId part, ulong id, Vector3 position, Quaternion rotation, bool locked = false) =>
        TubeMouthSnap.Find(definitions, placed, new TubeMouthMover(part, new(id), locked, part == PartId.Pipe ? PipeLength.Default : null),
            position, rotation);

    /// <summary>The snapped pose after the canonical capture: both mouths coincide within 1e-4 m and face opposite.</summary>
    private static IWorkshopInstance AssertJoined(IReadOnlyDictionary<PartId, PartDefinition> definitions, IReadOnlyList<IWorkshopInstance> placed,
        TubeMouthSnapResult snap, PartId part, ulong id)
    {
        var applied = Tube(definitions, part, id, snap.Position, snap.Rotation);
        var target = placed.Single(instance => instance.Id == snap.TargetBody);
        var (at, outward) = World(definitions, target, snap.TargetMouth);
        var (moverAt, moverOutward) = World(definitions, applied, snap.MoverMouth);
        Assert.True(at.DistanceTo(moverAt) <= 1e-4f, $"mouths {at} and {moverAt} are {at.DistanceTo(moverAt)} m apart");
        Assert.True(outward.Dot(moverOutward) < -.9999f, $"mouths face {outward} and {moverOutward}");
        return applied;
    }

    [Fact]
    public void EveryTubePartDeclaresAStartAndEndMouthWithABoreFacingOutward()
    {
        var definitions = Definitions();
        var declared = definitions.Values.Where(definition => definition.PortLocators.Values.Any(locator => locator.Mouth is not null))
            .Select(definition => definition.PartId).OrderBy(id => id).ToArray();
        Assert.Equal(TubeParts.OrderBy(id => id).ToArray(), declared);
        foreach (var part in TubeParts)
        {
            var mouths = definitions[part].PortLocators.Values.Where(locator => locator.Mouth is not null).ToArray();
            Assert.Equal([TubeMouthEnd.Start, TubeMouthEnd.End], mouths.Select(m => m.Mouth!.Value).OrderBy(m => m).ToArray());
            var start = Mouth(definitions[part], TubeMouthEnd.Start);
            var end = Mouth(definitions[part], TubeMouthEnd.End);
            var startOutward = start.Rotation * Vector3.Right;
            var endOutward = end.Rotation * Vector3.Right;
            // Local +X of each mouth locator points away from the part: out of the tube, never into its bore.
            Assert.True(startOutward.Dot(start.Position - end.Position) > 0, $"{part} mouth_start faces {startOutward}, into the part");
            Assert.True(endOutward.Dot(end.Position - start.Position) > 0, $"{part} mouth_end faces {endOutward}, into the part");
            Assert.Equal(part == PartId.Funnel ? FunnelInletBore : StandardBore, start.BoreRadius);
            Assert.Equal(StandardBore, end.BoreRadius);
        }
    }

    [Fact]
    public void PipeMouthsSitAtTheOuterCollarFaces()
    {
        var pipe = Definitions()[PartId.Pipe];
        Assert.True(Mouth(pipe, TubeMouthEnd.Start).Position.IsEqualApprox(new(-1.89f, 0, 0)));
        Assert.True(Mouth(pipe, TubeMouthEnd.End).Position.IsEqualApprox(new(1.89f, 0, 0)));
    }

    [Fact]
    public void ANewPipeOffsetAndTurnedSnapsOntoAFreeMouthByTheShortestArcAndKeepsItsRoll()
    {
        var definitions = Definitions();
        var target = Tube(definitions, PartId.Pipe, 1, new(0, 3, 0), Quaternion.Identity);
        IReadOnlyList<IWorkshopInstance> placed = [target];
        // 0.1 m off the free end mouth, turned 0.08 rad about Z, with a 30° roll about the tube's own axis.
        var rotation = (new Quaternion(Vector3.Back, .08f) * Rx(30)).Normalized();
        var position = PlaceMouth(definitions, PartId.Pipe, TubeMouthEnd.Start, new(1.99f, 3, 0), rotation);

        var snap = Find(definitions, placed, PartId.Pipe, 2, position, rotation);

        Assert.NotNull(snap);
        Assert.Equal((new GpuBodyId(1), TubeMouthEnd.End, TubeMouthEnd.Start), (snap.Value.TargetBody, snap.Value.TargetMouth, snap.Value.MoverMouth));
        // The shortest arc only removes the 0.08 rad misalignment: the roll about the axis survives.
        Assert.True(Mathf.Abs(snap.Value.Rotation.Dot(Rx(30))) > 1 - 1e-6f, $"snapped rotation {snap.Value.Rotation}");
        var applied = AssertJoined(definitions, placed, snap.Value, PartId.Pipe, 2);
        Assert.True(Pose(applied).Position.IsEqualApprox(new(3.78f, 3, 0)), $"joined pipe at {Pose(applied).Position}");
    }

    [Theory]
    [InlineData(.44f, true)]
    [InlineData(.46f, false)]
    [InlineData(.72f, false)]
    public void OnlyAMouthWithinTheCaptureDistanceSnaps(float offset, bool snaps)
    {
        var definitions = Definitions();
        IReadOnlyList<IWorkshopInstance> placed = [Tube(definitions, PartId.Pipe, 1, new(0, 3, 0), Quaternion.Identity)];
        var position = PlaceMouth(definitions, PartId.Pipe, TubeMouthEnd.Start, new(1.89f + offset, 3, 0), Quaternion.Identity);
        var snap = Find(definitions, placed, PartId.Pipe, 2, position, Quaternion.Identity);
        Assert.Equal(snaps, snap is not null);
        if (snap is { } joined) AssertJoined(definitions, placed, joined, PartId.Pipe, 2);
    }

    [Theory]
    [InlineData(19f, true)]
    [InlineData(21f, false)]
    [InlineData(40f, false)]
    public void OnlyAMouthWithinTheCaptureAngleSnaps(float degrees, bool snaps)
    {
        var definitions = Definitions();
        IReadOnlyList<IWorkshopInstance> placed = [Tube(definitions, PartId.Pipe, 1, new(0, 3, 0), Quaternion.Identity)];
        var rotation = Rz(degrees);
        var position = PlaceMouth(definitions, PartId.Pipe, TubeMouthEnd.Start, new(1.89f, 3, 0), rotation);
        var snap = Find(definitions, placed, PartId.Pipe, 2, position, rotation);
        Assert.Equal(snaps, snap is not null);
        if (snap is { } joined) AssertJoined(definitions, placed, joined, PartId.Pipe, 2);
    }

    [Fact]
    public void AnOccupiedMouthIsNotACandidate()
    {
        var definitions = Definitions();
        var first = Tube(definitions, PartId.Pipe, 1, new(0, 3, 0), Quaternion.Identity);
        var joined = Tube(definitions, PartId.Pipe, 2, new(3.78f, 3, 0), Quaternion.Identity);
        var position = PlaceMouth(definitions, PartId.Pipe, TubeMouthEnd.Start, new(1.99f, 3, 0), Quaternion.Identity);

        Assert.NotNull(Find(definitions, [first], PartId.Pipe, 3, position, Quaternion.Identity)); // control: the mouth is free
        Assert.Null(Find(definitions, [first, joined], PartId.Pipe, 3, position, Quaternion.Identity));
    }

    public static TheoryData<string> EveryTubePart => new() { "pipe", "pipe_bend_45", "pipe_bend_90", "funnel", "powered_gate", "ball_detector" };

    /// <summary>AC1: every declared mouth owner joins. A pipe offered 0.1 m off and turned 5° from each tube part's free end mouth
    /// snaps onto it exactly.</summary>
    [Theory]
    [MemberData(nameof(EveryTubePart))]
    public void APipeSnapsOntoTheEndMouthOfEveryTubePart(string id)
    {
        var definitions = Definitions();
        var part = new PartId(id);
        IReadOnlyList<IWorkshopInstance> placed = [Tube(definitions, part, 1, new(0, 3, 0), Quaternion.Identity)];
        var (at, outward) = World(definitions, placed[0], TubeMouthEnd.End);
        // The pipe's start mouth faces -X: turn its +X onto the target's outward axis, then 5° off it.
        var rotation = (new Quaternion(Vector3.Back, Mathf.DegToRad(5)) * new Quaternion(Vector3.Right, outward)).Normalized();
        var offset = outward.Cross(Vector3.Back).Normalized() * .1f;
        var position = PlaceMouth(definitions, PartId.Pipe, TubeMouthEnd.Start, at + offset, rotation);

        var snap = Find(definitions, placed, PartId.Pipe, 2, position, rotation);

        Assert.NotNull(snap);
        Assert.Equal((new GpuBodyId(1), TubeMouthEnd.End, TubeMouthEnd.Start), (snap.Value.TargetBody, snap.Value.TargetMouth, snap.Value.MoverMouth));
        AssertJoined(definitions, placed, snap.Value, PartId.Pipe, 2);
    }

    [Fact]
    public void APoweredGateSnapsOntoAPipeMouth()
    {
        var definitions = Definitions();
        IReadOnlyList<IWorkshopInstance> placed = [Tube(definitions, PartId.Pipe, 1, new(0, 3, 0), Quaternion.Identity)];
        var position = PlaceMouth(definitions, PartId.PoweredGate, TubeMouthEnd.Start, new(2, 3.05f, 0), Quaternion.Identity);
        var snap = Find(definitions, placed, PartId.PoweredGate, 2, position, Quaternion.Identity);
        Assert.NotNull(snap);
        Assert.Equal((TubeMouthEnd.End, TubeMouthEnd.Start), (snap.Value.TargetMouth, snap.Value.MoverMouth));
        AssertJoined(definitions, placed, snap.Value, PartId.PoweredGate, 2);
    }

    [Fact]
    public void ALockedMoverNeverSnapsAndALockedTargetsFreeMouthDoes()
    {
        var definitions = Definitions();
        var position = PlaceMouth(definitions, PartId.Pipe, TubeMouthEnd.Start, new(1.99f, 3, 0), Quaternion.Identity);
        IReadOnlyList<IWorkshopInstance> free = [Tube(definitions, PartId.Pipe, 1, new(0, 3, 0), Quaternion.Identity)];
        Assert.Null(Find(definitions, free, PartId.Pipe, 2, position, Quaternion.Identity, locked: true));

        // CAT-048 fact 21: a new pipe beside a locked fixed pipe at (-2, 3, 0) snaps to x 1.78 (mouths touching, 3.78 m apart).
        IReadOnlyList<IWorkshopInstance> fixture = [Tube(definitions, PartId.Pipe, 1, new(-2, 3, 0), Quaternion.Identity, locked: true)];
        var snap = Find(definitions, fixture, PartId.Pipe, 2, new(1.9f, 3, 0), Quaternion.Identity);
        Assert.NotNull(snap);
        var applied = AssertJoined(definitions, fixture, snap.Value, PartId.Pipe, 2);
        Assert.True(Mathf.Abs(Pose(applied).Position.X - 1.78f) <= .001f && Mathf.Abs(Pose(applied).Position.Y - 3) <= .001f);
    }

    [Fact]
    public void ThePipeSnapsToTheFunnelOutletButNotItsWideInlet()
    {
        var definitions = Definitions();
        IReadOnlyList<IWorkshopInstance> placed = [Tube(definitions, PartId.Funnel, 1, new(0, 5, 0), Quaternion.Identity)];
        // CAT-030 fact 5: the same pipe at (2.95, 5, 0) snaps to the outlet; at (-2.95, 5, 0) it does not snap to the 1.3 m inlet.
        Assert.Null(Find(definitions, placed, PartId.Pipe, 2, new(-2.95f, 5, 0), Quaternion.Identity));
        var snap = Find(definitions, placed, PartId.Pipe, 2, new(2.95f, 5, 0), Quaternion.Identity);
        Assert.NotNull(snap);
        Assert.Equal(TubeMouthEnd.End, snap.Value.TargetMouth);
        AssertJoined(definitions, placed, snap.Value, PartId.Pipe, 2);
    }

    /// <summary>A two-mouth test tube along local X with half length 1 and the given bore at both ends.</summary>
    private static PartDefinition TestTube(string id, float bore, Vector3? endPosition = null, Quaternion? endRotation = null)
    {
        var template = TestBundle.Entry(PartId.Pipe);
        var endAt = endPosition ?? new Vector3(1, 0, 0);
        var endTurn = endRotation ?? Quaternion.Identity;
        var physics = template with
        {
            Metadata = template.Metadata with { Id = id },
            ColliderBindings = [],
            TubeMouthBindings = [],
            TubeMouths =
            [
                new(CuriousContraptions.Abstractions.TubeEnd.Start, new(-1, 0, 0), new(0, 1, 0, 0), bore),
                new(CuriousContraptions.Abstractions.TubeEnd.End, new(endAt.X, endAt.Y, endAt.Z),
                    new(endTurn.X, endTurn.Y, endTurn.Z, endTurn.W), bore)
            ]
        };
        var definition = new PartDefinition(new PartId(id), id, id, CuriousContraptions.Abstractions.ElementCategory.Motion, Colors.White,
            "res://elements/Pipe/icon.svg", null, physics);
        return definition;
    }

    [Theory]
    [InlineData(5e-7f, true)]
    [InlineData(2e-6f, false)]
    public void BoresMustBeEqualWithin1EMinus6Metres(float difference, bool snaps)
    {
        var target = TestTube("bore_target", StandardBore);
        var mover = TestTube("bore_mover", StandardBore + difference);
        var definitions = new Dictionary<PartId, PartDefinition> { [target.PartId] = target, [mover.PartId] = mover };
        IReadOnlyList<IWorkshopInstance> placed = [Tube(definitions, target.PartId, 1, new(0, 3, 0), Quaternion.Identity)];
        var snap = Find(definitions, placed, mover.PartId, 2, new(2.1f, 3, 0), Quaternion.Identity);
        Assert.Equal(snaps, snap is not null);
    }

    [Theory]
    [InlineData(16, true)]
    [InlineData(17, false)]
    [InlineData(-16, true)]
    [InlineData(-17, false)]
    public void FindUsesTheNearestRepresentableBoreBoundary(float steps, bool snaps)
    {
        // At .65 m one f32 step is 2^-24 m: 16 steps = .953674 µm, 17 = 1.013279 µm.
        // Exact 1 µm separation is not representable here.
        var target = TestTube("boundary_target", StandardBore);
        var mover = TestTube("boundary_mover", StandardBore + steps * MathF.Pow(2f, -24));
        var definitions = new Dictionary<PartId, PartDefinition> { [target.PartId] = target, [mover.PartId] = mover };
        IReadOnlyList<IWorkshopInstance> placed = [Tube(definitions, target.PartId, 1, new(0, 3, 0), Quaternion.Identity)];
        Assert.Equal(snaps, Find(definitions, placed, mover.PartId, 2, new(2.1f, 3, 0), Quaternion.Identity) is not null);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void NonFiniteBoresAreRefusedBeforeTheyCanReachFind(float bore)
    {
        // PartDefinition admission is the public boundary; do not bypass it to construct an impossible query input.
        var refusal = Assert.Throws<ArgumentException>(() => TestTube("invalid_bore", bore));
        Assert.Contains("BUNDLE002", refusal.Message);
        Assert.Contains("TubeMouths.BoreRadius: not finite", refusal.Message);
    }

    [Fact]
    public void EqualCandidatesBreakTiesByLowerPartIdThenStartBeforeEnd()
    {
        var definitions = Definitions();
        var higher = Tube(definitions, PartId.Pipe, 5, new(0, 3, 0), Quaternion.Identity);
        var lower = Tube(definitions, PartId.Pipe, 4, new(0, 3, 0), Quaternion.Identity);
        var position = PlaceMouth(definitions, PartId.Pipe, TubeMouthEnd.Start, new(1.99f, 3, 0), Quaternion.Identity);
        Assert.Equal(new GpuBodyId(4), Find(definitions, [higher, lower], PartId.Pipe, 6, position, Quaternion.Identity)!.Value.TargetBody);
        Assert.Equal(new GpuBodyId(4), Find(definitions, [lower, higher], PartId.Pipe, 6, position, Quaternion.Identity)!.Value.TargetBody);

        // A mover whose two mouths coincide and face the same way: its start mouth is chosen before its end mouth.
        var target = TestTube("tie_target", StandardBore);
        var twin = TestTube("tie_twin", StandardBore, new(-1, 0, 0), new Quaternion(Vector3.Up, Mathf.Pi));
        var custom = new Dictionary<PartId, PartDefinition> { [target.PartId] = target, [twin.PartId] = twin };
        IReadOnlyList<IWorkshopInstance> one = [Tube(custom, target.PartId, 1, new(0, 3, 0), Quaternion.Identity)];
        Assert.Equal(TubeMouthEnd.Start, Find(custom, one, twin.PartId, 2, new(2.1f, 3, 0), Quaternion.Identity)!.Value.MoverMouth);
    }

    [Fact]
    public void AMovedTubeIgnoresItsOwnCommittedPose()
    {
        var definitions = Definitions();
        var first = Tube(definitions, PartId.Pipe, 1, new(0, 3, 0), Quaternion.Identity);
        // The moving pipe's committed pose already occupies the end mouth; while it is dragged, that pose neither occupies nor competes.
        var moving = Tube(definitions, PartId.Pipe, 2, new(3.78f, 3, 0), Quaternion.Identity);
        var position = PlaceMouth(definitions, PartId.Pipe, TubeMouthEnd.Start, new(2.1f, 3.1f, 0), Quaternion.Identity);
        var snap = Find(definitions, [first, moving], PartId.Pipe, 2, position, Quaternion.Identity);
        Assert.NotNull(snap);
        Assert.Equal(new GpuBodyId(1), snap.Value.TargetBody);
        AssertJoined(definitions, [first, moving], snap.Value, PartId.Pipe, 2);
    }

    [Fact]
    public void TheQueryNeverMutatesTheConstruction()
    {
        var definitions = Definitions();
        var instances = new WorkshopInstances(Tube(definitions, PartId.Pipe, 1, new(0, 3, 0), Quaternion.Identity));
        var construction = new WorkshopConstruction(new(7), WorkshopCadenceSettings.Default(), instances);
        var copy = new WorkshopInstances([.. instances]);

        Assert.NotNull(Find(definitions, construction.Instances, PartId.Pipe, 2, new(3.9f, 3, 0), Quaternion.Identity));

        Assert.Same(instances, construction.Instances);
        Assert.Equal(new ConstructionRevision(7), construction.Revision);
        Assert.Equal(copy, construction.Instances);
    }

    // Authored mouth rejection is covered by SceneGeometryExtractorTests at the build boundary.

    public enum Joint { StraightToBend45, Bend45ToBend45, StraightToBend90, FunnelOutletToPipe }

    /// <summary>A route of two tubes, the second snapped to the first through the query, and a ball dropped 0.6 m above the entry.</summary>
    private static (WorkshopConstruction Bench, Vector3 Entry, Vector3 Exit, Vector3 ExitOutward) Route(Joint joint)
    {
        var definitions = Definitions();
        var (first, second, secondRotation, height) = joint switch
        {
            Joint.StraightToBend45 => (PartId.Pipe, PartId.PipeBend45, Rz(-90), 5.5f),
            Joint.Bend45ToBend45 => (PartId.PipeBend45, PartId.PipeBend45, Rz(-135), 6f),
            Joint.StraightToBend90 => (PartId.Pipe, PartId.PipeBend90, Rz(-90), 6f),
            Joint.FunnelOutletToPipe => (PartId.Funnel, PartId.Pipe, Rz(-90), 7.5f),
            _ => throw new ArgumentOutOfRangeException(nameof(joint)),
        };
        // The first tube's start mouth points up; the second is offered near its end mouth, a little off and turned.
        var upstream = Tube(definitions, first, 2, new(0, height, 0), Rz(-90));
        var (joint0, _) = World(definitions, upstream, TubeMouthEnd.End);
        var offered = (Rz(4) * secondRotation).Normalized();
        var near = PlaceMouth(definitions, second, TubeMouthEnd.Start, joint0 + new Vector3(.12f, -.08f, .05f), offered);
        var snap = Find(definitions, [upstream], second, 3, near, offered);
        Assert.NotNull(snap);
        Assert.Equal((new GpuBodyId(2), TubeMouthEnd.End, TubeMouthEnd.Start), (snap.Value.TargetBody, snap.Value.TargetMouth, snap.Value.MoverMouth));
        var downstream = AssertJoined(definitions, [upstream], snap.Value, second, 3);
        var (entry, _) = World(definitions, upstream, TubeMouthEnd.Start);
        var (exit, exitOutward) = World(definitions, downstream, TubeMouthEnd.End);
        var drop = entry + new Vector3(joint == Joint.FunnelOutletToPipe ? .3f : 0, .6f, 0);
        var ball = WorkshopInput.Generic(PartId.Basketball,new(1), drop.X, drop.Y, drop.Z, 0, 0, 0, 1);
        return (new(new(2), WorkshopCadenceSettings.Default(), new(ball, upstream, downstream)), entry, exit, exitOutward);
    }

    private static Vector3 BallPosition(WorkshopRead read)
    {
        var body = Enumerable.Range(0, read.Bodies.Count).Select(i => read.Bodies[i]).Single(b => b.Body.Id == new GpuBodyId(1)).Body;
        return new((body.Cell.X + body.Local.X) / 16, (body.Cell.Y + body.Local.Y) / 16, (body.Cell.Z + body.Local.Z) / 16);
    }

    private static float BallSpeed(WorkshopRead read)
    {
        var body = Enumerable.Range(0, read.Bodies.Count).Select(i => read.Bodies[i]).Single(b => b.Body.Id == new GpuBodyId(1)).Body;
        return new Vector3(body.Velocity.X, body.Velocity.Y, body.Velocity.Z).Length();
    }

    /// <summary>CAT-048 fact 9: a ball crosses each snapped joint continuously. From entering the first mouth until it leaves the far
    /// mouth, every tick keeps at least 0.05 m/s and progress along the route's chord never falls back; Reset restores the bench.</summary>
    [Theory]
    [InlineData(Joint.StraightToBend45)]
    [InlineData(Joint.Bend45ToBend45)]
    [InlineData(Joint.StraightToBend90)]
    [InlineData(Joint.FunnelOutletToPipe)]
    public async Task ABallCrossesASnappedJointWithoutSnagging(Joint joint)
    {
        var (bench, entry, exit, exitOutward) = Route(joint);
        bench.Validate();
        var (built, run, reset) = await NoFixedCapacityTests.RunAndReset(bench, 3 * 120);
        NoFixedCapacityTests.AssertSameState(built, reset, $"{joint} Reset");

        var chord = (exit - entry).Normalized();
        var entered = run.FindIndex(read => (BallPosition(read) - entry).Dot(chord) > 0);
        var left = run.FindIndex(read => (BallPosition(read) - exit).Dot(exitOutward) > .4f);
        Assert.True(entered >= 0, $"{joint}: the ball never entered the first mouth");
        Assert.True(left > entered, $"{joint}: the ball never left the far mouth (last at {BallPosition(run[^1])})");
        var progress = (BallPosition(run[entered]) - entry).Dot(chord);
        for (var tick = entered; tick <= left; tick++)
        {
            var speed = BallSpeed(run[tick]);
            Assert.True(speed >= .05f, $"{joint}: tick {tick} speed {speed} m/s at {BallPosition(run[tick])}");
            var next = (BallPosition(run[tick]) - entry).Dot(chord);
            Assert.True(next >= progress - 1e-3f, $"{joint}: tick {tick} progress fell back from {progress} to {next}");
            progress = Math.Max(progress, next);
        }    }
}
