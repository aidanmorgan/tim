using CuriousContraptions.Gpu;
using SimulationEventKind = CuriousContraptions.Abstractions.SimulationEventKind;
using NumQuaternion = System.Numerics.Quaternion;
using NumVector3 = System.Numerics.Vector3;

namespace CuriousContraptions.Tests;

/// <summary>
/// engine-hollow-shapes on the production simulation (<see cref="EventRingProducerTests.Ring"/>): the Basketball through the pipe
/// family's true hollow colliders. Bounds as in <see cref="WorkshopPhysicsLawHollowTests"/>: the surface-error bound 1e-5 m and the
/// envelope's 0.5 mm resting penetration. Actual Basketball checks use its sphere-plus-seam union;
/// equivalent primitive controls preserve the 0.34 m sphere's 0.31 m wall offset.
/// </summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class HollowElementTests
{

    /// <summary>The production snap joins the equal 0.65 m mouths. Within 2 cm
    /// either side both packages expose straight stubs. The compound surface stays within the
    /// 5 mm art bound; the sphere control retains the 0.31 m centre offset.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ABallCrossesASnappedStraightOrFunnelToBendJointWithoutAStep(bool funnel)
    {
        var registry = new PartRegistry();
        registry.Load();
        // The funnel wall rises atan(0.65 / 1.8) = 19.9 degrees toward its throat.
        // A 30-degree tilt makes that floor descend; a straight bore needs only 10.
        var turn = AboutZ(funnel ? -30 : -10);
        var origin = new NumVector3(-2, 5, 0);
        var axis = NumVector3.Transform(NumVector3.UnitX, turn);
        var down = NumVector3.Transform(-NumVector3.UnitY, turn);
        IWorkshopInstance first = funnel
            ? WorkshopInput.Generic(PartId.Funnel, First, origin.X, origin.Y, origin.Z, turn.X, turn.Y, turn.Z, turn.W)
            : WorkshopInput.Pipe(First, origin.X, origin.Y, origin.Z, turn.X, turn.Y, turn.Z, turn.W);
        var joint = origin + (funnel ? .99f : 1.89f) * axis;
        var near = joint - NumVector3.Transform(new NumVector3(-1.00844f, .182689f, 0), turn);
        var snap = TubeMouthSnap.Find(registry.Definitions, [first], new(PartId.PipeBend45, Second, false),
            new(near.X, near.Y, near.Z), new(turn.X, turn.Y, turn.Z, turn.W));
        Assert.NotNull(snap);
        var pose = snap.Value;
        var bend = WorkshopInput.Generic(PartId.PipeBend45, Second, pose.Position.X, pose.Position.Y, pose.Position.Z,
            pose.Rotation.X, pose.Rotation.Y, pose.Rotation.Z, pose.Rotation.W);
        var start = funnel ? origin + .4f * axis + .35f * down : joint - 1.5f * axis + .31f * down;
        var construction = Bench(first, bend, BallAt(start));
        await using var ring = await EventRingProducerTests.Ring.Running(construction);
        await ring.Advance(480);
        var before = false;
        var after = false;
        var crossed = false;
        foreach (var read in ring.Reads)
        {
            var (p, speed) = Read(read);
            var offset = p - joint;
            var along = NumVector3.Dot(offset, axis);
            crossed |= along > .3f;
            if (MathF.Abs(along) > .02f) continue;
            before |= along < 0;
            after |= along >= 0;
            var radial = (offset - along * axis).Length();
            double Distance(NumVector3 point)
            {
                var d = point - joint;
                return (d - NumVector3.Dot(d, axis) * axis).Length();
            }
            Assert.InRange(UnionBoreReach(Body(read), Distance), .65 - .005, .65 + .005);
            Assert.True(speed > .1, "the payload caught at the joined mouths");
        }
        Assert.True(before && after && crossed, "the payload did not traverse both sides of the snapped joint");
        before = after = crossed = false;
        foreach (var body in SphereHistory(construction, 480))
        {
            var offset = body.Position - joint; var along = NumVector3.Dot(offset, axis);
            crossed |= along > .3f;
            if (MathF.Abs(along) > .02f) continue;
            before |= along < 0; after |= along >= 0;
            Assert.InRange((offset - along * axis).Length(), .31f - .005f, .31f + .005f);
            Assert.True(body.Speed > .1);
        }
        Assert.True(before && after && crossed);
    }


    [Fact]
    public async Task AConnectedHollowPassageRestoresConstructionAndRepeatsAfterResetAndSaveLoad()
    {
        var turn = AboutZ(-90);
        var pipe = WorkshopInput.Pipe(First, 0, 6, 0, turn.X, turn.Y, turn.Z, turn.W, new(1f));
        var detector = WorkshopInput.Generic(PartId.BallDetector, Second, 0, 5.13, 0, turn.X, turn.Y, turn.Z, turn.W);
        var lamp = WorkshopInput.Generic(PartId.SignalLamp, new(3), 3, 2, 0, 0, 0, 0, 1);
        var construction = Bench(pipe, detector, lamp, BallAt(new(0, 7.2f, 0))) with
        {
            Connections = new([new(Second, WorkshopSocket.ActivationOut, lamp.Id, WorkshopSocket.ActivationIn,
                WorkshopConnectionDomain.Activation)])
        };
        await using var ring = await EventRingProducerTests.Ring.Building(construction);
        Assert.Equal(WorkshopCommandOutcome.Applied, ring.Simulation.Save().Outcome);
        var encoded = WorkshopSaveCodec.Encode(new(construction, new(10)));
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements, encoded);
        Assert.Equal(construction.Instances, decoded.Construction.Instances);
        Assert.Equal(construction.Connections, decoded.Construction.Connections);
        await ring.Run();
        await ring.Advance(20);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await ring.Simulation.Pause()).Outcome);
        var pausedTick = ring.Simulation.Committed.Tick;
        Assert.Equal(WorkshopCommandOutcome.Applied, (await ring.Simulation.Step()).Outcome);
        ring.Publish();
        Assert.Equal(pausedTick.Value + 1, ring.Simulation.Committed.Tick.Value);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await ring.Simulation.Resume()).Outcome);
        await ring.Advance(159);
        var firstPassage = Assert.Single(ring.Of(SimulationEventKind.Passage));
        Assert.Equal(Second.Value, firstPassage.Source);
        var finish = Read(ring.Simulation.Committed).Position;
        Assert.True(finish.Y < 4, "the ball did not pass through both hollow packages");
        await ring.Reset();
        Assert.Equal(construction.Instances, ring.Simulation.Construction.Instances);
        Assert.Equal(construction.Connections, ring.Simulation.Construction.Connections);
        Assert.InRange(Read(ring.Simulation.Committed).Position.Y, 7.2f - 1e-6f, 7.2f + 1e-6f);
        ring.Records.Clear();
        await ring.Run();
        await ring.Advance(180);
        var repeated = Assert.Single(ring.Of(SimulationEventKind.Passage));
        Assert.Equal(firstPassage.Source, repeated.Source);
        Assert.Equal(firstPassage.Tick, repeated.Tick);
        Assert.Equal(finish, Read(ring.Simulation.Committed).Position);
        await using var loaded = await EventRingProducerTests.Ring.Running(decoded.Construction);
        await loaded.Advance(180);
        var restored = Assert.Single(loaded.Of(SimulationEventKind.Passage));
        Assert.Equal(firstPassage.Source, restored.Source);
        Assert.Equal(firstPassage.Tick, restored.Tick);
        Assert.Equal(finish, Read(loaded.Simulation.Committed).Position);
    }


    [Theory]
    [InlineData(.999f)]
    [InlineData(8.001f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public async Task AnInvalidHollowLengthIsRefusedWithoutChangingThePriorConstruction(float length)
    {
        var pipe = WorkshopInput.Pipe(First, 0, 3, 0, 0, 0, 0, 1);
        var construction = Bench(pipe, BallAt(new(0, 4.5f, 0)));
        await using var ring = await EventRingProducerTests.Ring.Building(construction);
        var before = ring.Simulation.Committed;
        var malformed = construction with { Instances = new(pipe with { Length = new(length) }) };
        var refusal = await ring.Simulation.Construct(malformed);
        Assert.Equal(WorkshopCommandOutcome.Rejected, refusal.Outcome);
        Assert.Equal(WorkshopRejection.InvalidConstruction, refusal.Reason);
        Assert.Equal(construction, ring.Simulation.Construction);
        Assert.Equal(before.Revision, ring.Simulation.Committed.Revision);
        Assert.Equal(before.Tick, ring.Simulation.Committed.Tick);
        Assert.Equal(Read(before), Read(ring.Simulation.Committed));
        await ring.Run();
        await ring.Advance(60);
        Assert.True(Read(ring.Simulation.Committed).Position.Y < 4.5f);
    }


    [Theory]
    [InlineData(0f, true)]
    [InlineData(.6f, false)]
    [InlineData(1.2f, true)]
    [InlineData(.3099f, true)]
    [InlineData(.31005f, true)]
    [InlineData(.3102f, false)]
    public async Task AHollowWallOverlapIsRefusedAtomicallyWhileBoreAndOutsideRemainClear(float offset, bool admitted)
    {
        var pipe = WorkshopInput.Pipe(First, 0, 3, 0, 0, 0, 0, 1);
        var prior = Bench(pipe);
        await using var ring = await EventRingProducerTests.Ring.Building(prior);
        var before = ring.Simulation.Committed;
        var candidate = Bench(pipe, BallAt(new(0, 3 + offset, 0))) with { Revision = new(3) };
        var result = await ring.Simulation.Construct(candidate);
        Assert.Equal(admitted ? WorkshopCommandOutcome.Applied : WorkshopCommandOutcome.Rejected, result.Outcome);
        if (admitted)
            Assert.Equal(candidate.Instances, ring.Simulation.Construction.Instances);
        else
        {
            Assert.Equal(WorkshopRejection.InvalidConstruction, result.Reason);
            Assert.Equal(prior, ring.Simulation.Construction);
            Assert.Equal(before.Revision, ring.Simulation.Committed.Revision);
            Assert.Equal(before.Tick, ring.Simulation.Committed.Tick);
            Assert.Equal(before.Bodies, ring.Simulation.Committed.Bodies);
        }
    }

    private const double BallRadius = .34, Bore = .65, OnWall = Bore - BallRadius;
    private const double Low = OnWall - WorkshopPhysicsLawHollowTests.SurfaceError;
    private const double High = OnWall + WorkshopPhysicsLawHollowTests.Resting + WorkshopPhysicsLawHollowTests.SurfaceError;
    private const float Y = 3f;
    private static readonly GpuBodyId First = new(1), Second = new(2), Ball = new(9);

    private static WorkshopConstruction Bench(params IWorkshopInstance[] instances) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances));

    private static WorkshopGenericInstance BallAt(NumVector3 at) =>
        WorkshopInput.Generic(PartId.Basketball, Ball, at.X, at.Y, at.Z, 0, 0, 0, 1);

    private static NumQuaternion AboutZ(double degrees) => NumQuaternion.CreateFromAxisAngle(NumVector3.UnitZ, (float)(degrees * Math.PI / 180));

    private static (NumVector3 Position, double Speed) Read(WorkshopRead read)
    {
        Assert.True(read.Bodies.TryGet(Ball, out var row));
        var v = row.Body.Velocity;
        return (new((float)((row.Body.Cell.X + (double)row.Body.Local.X) / 16), (float)((row.Body.Cell.Y + (double)row.Body.Local.Y) / 16),
            (float)((row.Body.Cell.Z + (double)row.Body.Local.Z) / 16)), Math.Sqrt((double)v.X * v.X + (double)v.Y * v.Y + (double)v.Z * v.Z));
    }

    private static LawBody Body(WorkshopRead read)
    {
        Assert.True(read.Bodies.TryGet(Ball, out var row));
        var q = row.Rotation; var v = row.Body.Velocity; var w = row.AngularVelocity;
        return new(Read(read).Position, new(q.X, q.Y, q.Z, q.W), new(v.X, v.Y, v.Z), new(w.X, w.Y, w.Z), row.Body.Cell, row.Body.Local);
    }

    private static List<LawBody> SphereHistory(WorkshopConstruction construction, int ticks)
    {
        // Explicit primitive control: preserve the independently declared sphere mass frame/material,
        // with only its sphere collider. Static package unions remain unchanged.
        var compiled = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var colliders = compiled.Colliders.ToArray().Where(c => c.Body != Ball || c.Shape == ColliderShapeKind.Sphere).ToArray();
        Assert.Single(colliders, c => c.Body == Ball);
        var scene = new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity, compiled.Bodies, colliders, compiled.Materials, [], []);
        var world = new LawWorld(scene, PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz120);
        var history = new List<LawBody>();
        world.Step(ticks, (_, state) => history.Add(state[Ball]));
        return history;
    }

    private static readonly NumVector3[] RingOracleSamples = Enumerable.Range(0, 8192)
        .Select(i => new NumVector3((float)Math.Cos(2 * Math.PI * i / 8192), 0, (float)Math.Sin(2 * Math.PI * i / 8192))).ToArray();

    private static double UnionBoreReach(LawBody body, Func<NumVector3, double> distance)
    {
        var samples = RingOracleSamples.Length;
        const double radius = .336, tube = .014;
        var q = body.Rotation * new NumQuaternion(0, 0, -.3007058f, .95371693f);
        var x = NumVector3.Transform(NumVector3.UnitX, q);
        var z = NumVector3.Transform(NumVector3.UnitZ, q);
        var maximum = 0.0;
        for (var i = 0; i < samples; i++)
        {
            var direction = RingOracleSamples[i];
            var p = body.Position + (float)radius * direction.X * x + (float)radius * direction.Z * z;
            maximum = Math.Max(maximum, distance(p));
        }
        // Any centreline point is within radius*pi/N of its nearest sample.
        // Distance to the pipe/bend centreline is1-Lipschitz; the tube adds its own radius.
        return Math.Max(distance(body.Position) + .34, maximum + tube + radius * Math.PI / samples);
    }

    private static void AssertDissipative(LawBody body, ref double? previous)
    {
        var energy = .5 * body.Speed * body.Speed + .5 * (.4 * .34 * .34) * body.Spin * body.Spin + 9.81 * body.Position.Y;
        if (previous is { } prior) Assert.True(energy <= prior + 9.81 * .0002, $"specific energy rose {energy-prior}J/kg");
        previous = energy;
    }

    private static void AssertStraightTraversal(IEnumerable<LawBody> history, NumVector3 centre, NumVector3 axis,
        float stop, float requiredProgress, bool compound)
    {
        double nearest = double.PositiveInfinity, farthest = 0, previousSpeed = 0;
        double? previousEnergy = null;
        var progressed = false;
        double Distance(NumVector3 p)
        {
            var offset = p - centre;
            return (offset - NumVector3.Dot(offset, axis) * axis).Length();
        }
        foreach (var body in history)
        {
            var along = NumVector3.Dot(body.Position - centre, axis);
            progressed |= along > requiredProgress;
            if (along > stop) break;
            nearest = Math.Min(nearest, Distance(body.Position));
            farthest = Math.Max(farthest, Distance(body.Position));
            if (compound)
            {
                Assert.True(UnionBoreReach(body, Distance) <= Bore + WorkshopPhysicsLawHollowTests.Resting + WorkshopPhysicsLawHollowTests.SurfaceError);
                Assert.True(NumVector3.Dot(body.Velocity, axis) >= -1e-4f, "the compound reversed in the descending bore");
                AssertDissipative(body, ref previousEnergy);
            }
            else Assert.True(body.Speed >= previousSpeed - 1e-4, $"sphere slowed from {previousSpeed} to {body.Speed}");
            previousSpeed = body.Speed;
        }
        Assert.True(progressed, "the payload did not traverse the bore/joint by the original deadline");
        if (!compound) Assert.True(nearest >= Low && farthest <= High, $"sphere radial range {nearest}..{farthest}");
    }

    /// <summary>A pipe of each length (the minimum, the default and the maximum), tilted 10° downhill toward +X, with the Basketball
    /// released on its bore's floor 0.4 m inside its upper mouth. It rolls the whole bore and out of the lower mouth: while it is inside,
    /// the actual union remains contained, dissipative and moving forward. The equivalent sphere control retains
    /// its 0.31 m wall offset and monotonic acceleration (about (5/7)·g·(sin 10° − C_rr)).</summary>
    [Theory]
    [InlineData(1f)]
    [InlineData(3.6f)]
    [InlineData(8f)]
    public async Task ABallRollsThroughAStraightPipeOfEveryLengthOnItsWallWithoutCatching(float length)
    {
        var tilt = AboutZ(-10);
        var centre = new NumVector3(0, Y, 0);
        var axis = NumVector3.Transform(NumVector3.UnitX, tilt);
        var down = NumVector3.Transform(-NumVector3.UnitY, tilt);
        var reach = length / 2 + .09f;
        var start = centre + (.4f - reach) * axis + (float)OnWall * down;
        var construction = Bench(WorkshopInput.Pipe(First, centre.X, centre.Y, centre.Z, tilt.X, tilt.Y, tilt.Z, tilt.W, new(length)), BallAt(start));
        await using var ring = await EventRingProducerTests.Ring.Running(construction);
        await ring.Advance(600);
        AssertStraightTraversal(ring.Reads.Select(Body), centre, axis, reach, reach, true);
        AssertStraightTraversal(SphereHistory(construction, 600), centre, axis, reach, reach, false);
    }

    /// <summary>Two default pipes joined by the production tube-mouth snap (the second mover's start mouth onto the first's end mouth),
    /// the first tilted 10° downhill toward +X. The Basketball released in the first rolls across the joint into the second: its
    /// union remains contained and moves forward without energy gain. The sphere control retains
    /// its 0.31 m centre offset and monotonic speed across the joint.</summary>
    [Fact]
    public async Task ABallRollingThroughTwoSnappedPipesKeepsItsHeightAcrossTheJoint()
    {
        var registry = new PartRegistry();
        registry.Load();
        var tilt = AboutZ(-10);
        var centre = new NumVector3(-2f, Y + .5f, 0);
        var first = WorkshopInput.Pipe(First, centre.X, centre.Y, centre.Z, tilt.X, tilt.Y, tilt.Z, tilt.W);
        var axis = NumVector3.Transform(NumVector3.UnitX, tilt);
        var godotTilt = new Godot.Quaternion(tilt.X, tilt.Y, tilt.Z, tilt.W);
        var near = centre + 3.9f * axis;
        var snap = TubeMouthSnap.Find(registry.Definitions, [first], new(PartId.Pipe, Second, false, PipeLength.Default),
            new Godot.Vector3(near.X, near.Y, near.Z), godotTilt);
        Assert.NotNull(snap);
        var pose = snap.Value;
        var second = WorkshopInput.Pipe(Second, pose.Position.X, pose.Position.Y, pose.Position.Z, pose.Rotation.X, pose.Rotation.Y,
            pose.Rotation.Z, pose.Rotation.W);
        var joint = new NumVector3(pose.Joint.X, pose.Joint.Y, pose.Joint.Z);
        var down = NumVector3.Transform(-NumVector3.UnitY, tilt);
        var construction = Bench(first, second, BallAt(joint - 1.5f * axis + (float)OnWall * down));
        await using var ring = await EventRingProducerTests.Ring.Running(construction);
        await ring.Advance(240);
        AssertStraightTraversal(ring.Reads.Select(Body), joint, axis, 1.5f, .3f, true);
        AssertStraightTraversal(SphereHistory(construction, 240), joint, axis, 1.5f, .3f, false);
    }

    /// <summary>Each bend in the quarter_bend lesson's pose (turned −90° about Z: its start mouth up, its end mouth facing −X), the
    /// Basketball released 0.5 m above the start mouth's centre. The actual union remains contained and dissipative
    /// and exits within the same speed/time bounds. The equivalent sphere retains continuous wall attachment, never
    /// nearer the centreline than 0.31 m less the surface-error bound (it never lifts off) and never deeper than the envelope's temporary
    /// penetration under its impact and centripetal load: no slot or step catches it. It leaves at
    /// speed v with v ≤ √(2 g Δh) (no energy gained over the drop Δh from release to the exit's floor line) and v ≥ (5/7) √(2 g Δh)
    /// less what rolling resistance and drag can remove over the run: a ball entering the wall without spin and spun up to rolling by
    /// friction keeps at least 5/7 of its speed, and resistance removes at most (C_rr (g + v_max² / (R − 0.31)) + c v_max) t over the
    /// run's time t (C_rr 0.035, c 0.04, v_max = √(2 g Δh), R 2.4).</summary>
    [Theory]
    [InlineData(45)]
    [InlineData(90)]
    public async Task ABallDroppedIntoABendRidesItsOuterWallAndLeavesAtTheDerivedSpeed(int degrees)
    {
        var part = degrees == 45 ? PartId.PipeBend45 : PartId.PipeBend90;
        var turn = AboutZ(-90);
        var origin = new NumVector3(0, Y + 2, 0);
        NumVector3 World(float x, float y) => origin + NumVector3.Transform(new(x, y, 0), turn);
        var sweep = degrees * MathF.PI / 180;
        var startCollar = degrees == 45 ? new NumVector3(-.91844f, .182689f, 0) : new NumVector3(-1.697056f, .702944f, 0);
        var arcCentre = World(startCollar.X, startCollar.Y - 2.4f);
        var endTangent = NumVector3.Transform(new NumVector3(MathF.Cos(sweep), -MathF.Sin(sweep), 0), turn);
        var endCollar = World(startCollar.X + 2.4f * MathF.Sin(sweep), startCollar.Y - 2.4f + 2.4f * MathF.Cos(sweep));
        var endMouth = endCollar + .09f * endTangent;
        var startMouth = World(startCollar.X - .09f, startCollar.Y);
        var release = startMouth + new NumVector3(0, .5f, 0);
        var construction = Bench(WorkshopInput.Generic(part, First, origin.X, origin.Y, origin.Z, turn.X, turn.Y, turn.Z, turn.W), BallAt(release));
        await using var ring = await EventRingProducerTests.Ring.Running(construction);
        await ring.Advance(240);

        // The centreline: the arc's circle within its sweep, or a stub's axis.
        var planeNormal = NumVector3.UnitZ;
        var startDirection = NumVector3.Normalize(World(startCollar.X, startCollar.Y) - arcCentre);
        double FromCentreline(NumVector3 p, out bool outer)
        {
            var offset = p - arcCentre;
            var inPlane = offset - NumVector3.Dot(offset, planeNormal) * planeNormal;
            var radial = inPlane.Length();
            var angle = Math.Acos(Math.Clamp(NumVector3.Dot(inPlane / radial, startDirection), -1, 1));
            outer = radial > 2.4;
            return angle <= sweep ? Math.Sqrt(Math.Pow(radial - 2.4, 2) + Math.Pow(NumVector3.Dot(offset, planeNormal), 2)) : double.NaN;
        }
        Verify(ring.Reads.Select(Body), true);
        Verify(SphereHistory(construction, 240), false);
        void Verify(IEnumerable<LawBody> history, bool compound)
        {
            double nearest = double.PositiveInfinity, farthest = 0, exitSpeed = double.NaN, ticks = 0;
            var touched = false;
            double? previousEnergy = null;
            foreach (var body in history)
            {
                var p = body.Position; var speed = body.Speed;
                ticks++;
                if (NumVector3.Dot(p - endMouth, endTangent) > 0) { exitSpeed = speed; break; }
                var distance = FromCentreline(p, out var outer);
                if (double.IsNaN(distance)) continue;
                touched |= distance >= Low;
                if (!touched) continue;
                Assert.True(outer, $"{degrees}°: the ball left the outer wall at {p}");
                if (compound)
                {
                    double CentrelineDistance(NumVector3 point)
                    {
                        var offset = point - arcCentre;
                        var planar = offset - NumVector3.Dot(offset, planeNormal) * planeNormal;
                        return Math.Sqrt(Math.Pow(planar.Length() - 2.4, 2) + Math.Pow(NumVector3.Dot(offset, planeNormal), 2));
                    }
                    Assert.True(UnionBoreReach(body, CentrelineDistance) <= Bore + .05 * BallRadius);
                    AssertDissipative(body, ref previousEnergy);
                }
                nearest = Math.Min(nearest, distance);
                farthest = Math.Max(farthest, distance);
            }
            Assert.True(touched && !double.IsNaN(exitSpeed), $"{degrees}°: the ball never rode the arc out of the end mouth");
            // Pressed by its impact and the turn's centripetal load (several g), the ball may sink up to the envelope's temporary visual
            // penetration, 5 % of its radius; it never lifts off the wall.
            if (!compound) Assert.True(nearest >= Low && farthest <= OnWall + .05 * BallRadius,
                $"{degrees}°: the ball rode {nearest:F6}–{farthest:F6} m from the centreline, not {OnWall}");
            const double g = 9.81, rolling = .035, drag = .04;
            var drop = release.Y - (endMouth.Y - OnWall);
            var free = Math.Sqrt(2 * g * drop);
            var loss = (rolling * (g + free * free / (2.4 - OnWall)) + drag * free) * ticks / 120;
            Assert.True(exitSpeed <= free && exitSpeed >= 5.0 / 7 * free - loss,
                $"{degrees}°: the ball left at {exitSpeed:F3} m/s, outside [{5.0 / 7 * free - loss:F3}, {free:F3}]");
        }
        await AssertPhysicalReplay(construction, ring, 240);
    }

    /// <summary>The Funnel (CAT-030) stood on its outlet (turned −90° about Z: its inlet, local −X, up). The Basketball dropped 0.6 m
    /// off its axis into the 1.3 m inlet falls onto the conical wall, rolls down it and leaves through the 0.65 m throat: it reaches
    /// below the outlet mouth, and at no tick does its centre lie farther from the funnel's axis than the wall's inner radius at its
    /// height plus the resting penetration (a centre beyond the inner surface would have passed into or through the wall).</summary>
    [Fact]
    public async Task ABallDroppedIntoTheFunnelsInletLeavesThroughItsThroatInsideTheWall()
    {
        var turn = AboutZ(-90);
        var centre = new NumVector3(0, Y + 1, 0);
        var construction = Bench(WorkshopInput.Generic(PartId.Funnel, First, centre.X, centre.Y, centre.Z, turn.X, turn.Y, turn.Z, turn.W),
            BallAt(centre + new NumVector3(.6f, 1.2f, 0)));
        await using var ring = await EventRingProducerTests.Ring.Running(construction);
        await ring.Advance(360);
        var left = false;
        foreach (var read in ring.Reads)
        {
            var (p, _) = Read(read);
            var height = p.Y - centre.Y; // local −X: +0.9 at the inlet collar, −0.9 at the outlet collar
            if (height < -.99f) { left = true; break; }
            if (height > .9f) continue;
            // The wall's inner radius at this height: 1.3 at the inlet (+0.9) narrowing linearly to 0.65 at the outlet (−0.9).
            var inner = .65 + (1.3 - .65) * (height + .9) / 1.8;
            var off = Math.Sqrt((double)p.X * p.X + (double)p.Z * p.Z);
            Assert.True(off <= inner + WorkshopPhysicsLawHollowTests.Resting, $"the ball's centre is {off:F4} m off the axis at height {height:F3}, " +
                $"beyond the wall's inner radius {inner:F4}");
        }
        Assert.True(left, "the ball never left through the throat");
        await AssertPhysicalReplay(construction, ring, 360);
    }

    private static async Task AssertPhysicalReplay(WorkshopConstruction construction, EventRingProducerTests.Ring ring, int ticks)
    {
        var finish = Read(ring.Simulation.Committed);
        var save = new WorkshopSavedConstruction(construction, new(10));
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(save));
        Assert.Equal(save, decoded);
        await ring.Reset();
        Assert.Equal(construction.Instances, ring.Simulation.Construction.Instances);
        Assert.Equal(construction.Connections, ring.Simulation.Construction.Connections);
        var (authored, _) = TubeMouthSnap.Pose(construction.Instances.Single(instance => instance.Id == Ball));
        var restored = Read(ring.Simulation.Committed);
        Assert.InRange(NumVector3.Distance(restored.Position, new(authored.X, authored.Y, authored.Z)), 0, 1e-6f);
        Assert.Equal(0, restored.Speed);
        Assert.Equal(0UL, ring.Simulation.Committed.Tick.Value);
        await ring.Run();
        await ring.Advance(ticks);
        Assert.Equal(finish, Read(ring.Simulation.Committed));
        await using var fresh = await EventRingProducerTests.Ring.Running(decoded.Construction);
        await fresh.Advance(ticks);
        Assert.Equal(finish, Read(fresh.Simulation.Committed));
    }

    [Fact]
    public void ATangentialFunnelReleaseTravelsAroundTheWallAndExitsWithFullSphereClearance()
    {
        var scene = new LawScene();
        var centre = new NumVector3(0, 4, 0);
        var turn = AboutZ(-90);
        foreach (var collider in DeclaredPhysics.Colliders(PartId.Funnel))
        {
            var p = collider.LocalPose.Translation;
            var q = collider.LocalPose.Rotation;
            scene.StaticHollow(collider.ShapeKind, centre + NumVector3.Transform(new(p.X, p.Y, p.Z), turn),
                turn * new NumQuaternion(q.X, q.Y, q.Z, q.W), collider.Radius,
                new(collider.HalfExtents.X, collider.HalfExtents.Y, collider.HalfExtents.Z), new(.15f, .1f, .3f, 0));
        }
        var ball = scene.Sphere(centre + new NumVector3(.6f, 1.2f, 0), .34f, 1, new(.55f, .1f, .3f, .035f),
            new(0, 0, 1.5f), default, .04f, -9.81f);
        var world = scene.Admit();
        double angularTravel = 0, previous = 0;
        var exited = false;
        world.Step(3 * world.TicksPerSecond, (_, state) =>
        {
            if (exited) return;
            var p = state[ball].Position - centre;
            var angle = Math.Atan2(p.Z, p.X);
            var delta = Math.Atan2(Math.Sin(angle - previous), Math.Cos(angle - previous));
            angularTravel += delta;
            previous = angle;
            if (p.Y < -.99f) { exited = true; return; }
            // Independent inner-wall segment in (rho,y): (.65,-.9)..(1.3,.9).
            // Distance to that finite segment includes each rim, not just centre containment.
            var rho = Math.Sqrt((double)p.X * p.X + (double)p.Z * p.Z);
            var t = Math.Clamp(((rho - .65) * .65 + (p.Y + .9) * 1.8) / (.65 * .65 + 1.8 * 1.8), 0, 1);
            var gap = Math.Sqrt(Math.Pow(rho - (.65 + .65 * t), 2) + Math.Pow(p.Y - (-.9 + 1.8 * t), 2));
            Assert.True(gap >= .34 - .05 * .34 - WorkshopPhysicsLawHollowTests.SurfaceError,
                $"sphere/wall clearance {gap} at {p}");
            if (p.Y is >= -.9f and <= .9f)
                Assert.True(rho < .65 + .65 * (p.Y + .9) / 1.8, "the sphere escaped through the wall");
        });
        Assert.True(exited);
        Assert.True(angularTravel > Math.PI / 4, $"angular travel {angularTravel} rad");
    }

}
