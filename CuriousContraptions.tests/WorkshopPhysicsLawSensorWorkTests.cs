using System.Buffers.Binary;
using System.Numerics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 9.4b: the orientation-sensor and contact-work admission facts of the deleted JS-kernel harness
/// (tools/workshop-rigid-body.test.mjs), ported to step the shipped C# engine natively. The harness's contact-work facts spied on a
/// recording payment boundary; natively the real payment runs, so "payment called" is a recorded occurrence and "never called" is none.
/// </summary>
public sealed class WorkshopPhysicsLawSensorWorkTests
{
    private static readonly float BenchY = LawScene.BenchY;
    private static float DominoHalfX => (float)DominoMaterial.Default.HalfExtents.X;
    private static float DominoHalfY => (float)DominoMaterial.Default.HalfExtents.Y;
    private static Quaternion AboutZ(double degrees) => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)(degrees * Math.PI / 180));
    private static OrientationSensorRead Sensor(LawState state) => PhysicsGpuAbi.ReadOrientationSensor(state.World, 0);

    // ---- orientation sensors ----

    [Theory]
    [InlineData(-30, -1)]
    [InlineData(-30, 1)]
    [InlineData(30, -1)]
    [InlineData(30, 1)]
    public void ANonzeroAdmittedZAndEitherQuaternionSignPreserveTheRelativeThreshold(int degrees, int sign)
    {
        var half = degrees * Math.PI / 360;
        var q = new Quaternion(0, 0, (float)(sign * Math.Sin(half)), (float)(sign * Math.Cos(half)));
        LawWorld Admit(Vector3 angular, out GpuBodyId tile)
        {
            var scene = new LawScene();
            tile = scene.Box(new(0, 5, 0), new(DominoHalfX, DominoHalfY, (float)DominoMaterial.Default.HalfExtents.Z),
                (float)DominoMaterial.Default.Mass.Value, LawMaterial.Domino, q, angular: angular, gravity: 0);
            scene.OrientationSensor(tile, q, 10);
            return scene.Admit();
        }
        var stationary = Sensor(Admit(default, out _).Step(12));
        Assert.Equal((OrientationSensorPhase.Armed, 0u), (stationary.Phase, stationary.EventOrdinal));
        var threshold = (float)OrientationThreshold.Degrees(10).CosineHalfAngle;
        var world = Admit(new(0, 0, 1), out var tileId);
        (int Tick, uint Ordinal)? first = null; var previousAlignment = 1f;
        var final = world.Step(48, (tick, state) =>
        {
            var r = state[tileId].Rotation; var sensor = Sensor(state);
            var alignment = MathF.Abs(r.X * q.X + r.Y * q.Y + r.Z * q.Z + r.W * q.W);
            if (sensor.Phase == OrientationSensorPhase.Armed) Assert.True(alignment > threshold - 2e-7f, $"tick {tick}: armed beyond the threshold");
            if (sensor.Phase == OrientationSensorPhase.Fired && first is null)
            {
                Assert.True(previousAlignment > threshold && alignment <= threshold + 2e-7f, $"tick {tick}: the first event brackets the crossing");
                Assert.InRange(sensor.EventOrdinal, (uint)(tick - 1) * 4 + 1, (uint)tick * 4);
                first = (tick, sensor.EventOrdinal);
            }
            if (first is { } f) Assert.Equal(f.Ordinal, sensor.EventOrdinal);
            previousAlignment = alignment;
        });
        Assert.True(first is { Tick: > 1 }, "a later genuine relative rotation fires");
        Assert.Equal(OrientationSensorPhase.Fired, Sensor(final).Phase);
    }

    private static (LawScene Scene, GpuBodyId Tile) TiltedTile(double degrees, Quaternion? sensed = null)
    {
        var radians = degrees * Math.PI / 180;
        var scene = new LawScene();
        var rotation = AboutZ(degrees);
        var tile = scene.Domino(new(0, BenchY + DominoHalfY * (float)Math.Cos(radians) + DominoHalfX * (float)Math.Sin(radians) + .005f, 0),
            rotation);
        scene.OrientationSensor(tile, sensed ?? rotation, 45);
        return (scene, tile);
    }

    [Fact]
    public void ATilePlacedTenDegreesOffUprightRocksBackAndNeverFires()
    {
        var (scene, tile) = TiltedTile(10);
        var world = scene.Admit();
        var everFired = false;
        var final = world.Step(4 * world.TicksPerSecond, (_, state) => everFired |= Sensor(state).Phase == OrientationSensorPhase.Fired);
        Assert.False(everFired);
        var sensor = Sensor(final);
        Assert.Equal((OrientationSensorPhase.Armed, 0u, (Half)0), (sensor.Phase, sensor.EventOrdinal, sensor.EventPhase));
        const int DeclarationBytes = 34; // id, body slot, padding, initial rotation and threshold precede the event lanes
        Assert.Equal(world.Admitted.AsSpan(PhysicsGpuAbi.OrientationSensorsOffset, DeclarationBytes).ToArray(),
            final.World.AsSpan(PhysicsGpuAbi.OrientationSensorsOffset, DeclarationBytes).ToArray());
        Assert.True(final[tile].Tilt < 2, $"settles upright (tilt {final[tile].Tilt})");
        // Control: tipped to 30 deg, past its support edge, the same tile topples and the same sensor fires.
        var (tipped, _) = TiltedTile(30);
        var tippedWorld = tipped.Admit();
        Assert.Equal(OrientationSensorPhase.Fired, Sensor(tippedWorld.Step(4 * tippedWorld.TicksPerSecond)).Phase);
    }

    [Fact]
    public void ATileAdmittedRotatedNinetyDegreesAboutYStandsAndNeverFiresBecauseTheInitialPoseIsTheAdmittedPose()
    {
        var turn = new Quaternion(0, MathF.Sqrt(.5f), 0, MathF.Sqrt(.5f));
        LawScene Turned(Quaternion sensed, out GpuBodyId tile)
        {
            var scene = new LawScene();
            tile = scene.Domino(new(0, BenchY + DominoHalfY + .005f, 0), turn);
            scene.OrientationSensor(tile, sensed, 45);
            return scene;
        }
        var world = Turned(turn, out var tile).Admit();
        var everFired = false;
        var final = world.Step(2 * world.TicksPerSecond, (_, state) => everFired |= Sensor(state).Phase == OrientationSensorPhase.Fired);
        Assert.False(everFired);
        Assert.True(final[tile].Tilt < 2 && MathF.Abs(final[tile].Rotation.Y) > .7f, $"standing, still turned (qy {final[tile].Rotation.Y})");
        // Control: admission itself binds the initial pose to the admitted rotation, so an identity initial pose 90 deg from it is
        // rejected rather than firing.
        var mismatched = Assert.Throws<ArgumentException>(() => Turned(Quaternion.Identity, out _).Admit());
        Assert.Contains("initial pose", mismatched.Message);
    }

    [Fact]
    public void AToppplingTileFiresOnceInTheTickItPassesFortyFiveDegreesAndStaysFiredAtThatOrdinal()
    {
        var scene = new LawScene();
        var tile = scene.Domino(new(0, BenchY + DominoHalfY + .0005f, 0), angular: new(0, 0, -4));
        scene.OrientationSensor(tile, Quaternion.Identity, 45);
        var world = scene.Admit();
        int? crossed = null; (int Tick, uint Ordinal)? fired = null;
        var final = world.Step(4 * world.TicksPerSecond, (tick, state) =>
        {
            if (crossed is null && state[tile].Tilt >= 45) crossed = tick;
            var sensor = Sensor(state);
            if (fired is null && sensor.Phase == OrientationSensorPhase.Fired) fired = (tick, sensor.EventOrdinal);
            if (fired is null) Assert.Equal(0u, sensor.EventOrdinal);
        });
        Assert.True(fired is not null && crossed is not null, "the sensor fires");
        Assert.Equal(crossed, fired!.Value.Tick);
        Assert.InRange(fired.Value.Ordinal, (uint)(fired.Value.Tick - 1) * 4 + 1, (uint)fired.Value.Tick * 4);
        var sensorFinal = Sensor(final);
        Assert.Equal((OrientationSensorPhase.Fired, fired.Value.Ordinal, (Half)0), (sensorFinal.Phase, sensorFinal.EventOrdinal, sensorFinal.EventPhase));
        Assert.InRange(final[tile].Tilt, 87f, 93f);
    }

    [Fact]
    public void ASensedBodyThatTurnsBackWithinTheThresholdKeepsItsSingleEmission()
    {
        var radius = LawBalls.Radius(LawBall.Basketball);
        var scene = new LawScene();
        var ball = scene.Ball(LawBall.Basketball, new(0, BenchY + radius, 0), velocity: new(3, 0, 0));
        scene.OrientationSensor(ball, Quaternion.Identity, 45);
        var world = scene.Admit();
        var history = new List<(OrientationSensorRead Sensor, float Alignment)>();
        world.Step(2 * world.TicksPerSecond, (_, state) => history.Add((Sensor(state), MathF.Abs(state[ball].Rotation.W))));
        var first = history.FindIndex(h => h.Sensor.Phase == OrientationSensorPhase.Fired);
        Assert.InRange(first, 0, world.TicksPerSecond / 2 - 1);
        Assert.All(history[first..], h => Assert.Equal((OrientationSensorPhase.Fired, history[first].Sensor.EventOrdinal),
            (h.Sensor.Phase, h.Sensor.EventOrdinal)));
        Assert.Contains(history[(first + 1)..], h => h.Alignment > MathF.Cos(MathF.PI / 8));
    }

    [Fact]
    public void TheOrientationThresholdIsDeclaredDataNotAConstantOfTheEngine()
    {
        int FirstFire(double degrees)
        {
            var scene = new LawScene();
            var tile = scene.Domino(new(0, BenchY + DominoHalfY + .0005f, 0), angular: new(0, 0, -4));
            scene.OrientationSensor(tile, Quaternion.Identity, degrees);
            var world = scene.Admit();
            var fired = -1;
            world.Step(world.TicksPerSecond, (tick, state) => { if (fired < 0 && Sensor(state).Phase == OrientationSensorPhase.Fired) fired = tick; });
            return fired;
        }
        var narrow = FirstFire(20); var wide = FirstFire(80);
        Assert.True(narrow > 0 && wide > 0 && narrow < wide, $"20 deg fires at tick {narrow}, 80 deg at tick {wide}");
        var source = System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(),
            "CuriousContraptions.Simulation", "WorkshopPhysicsEngine.cs")), @"//[^\n]*|/\*[\s\S]*?\*/", "");
        Assert.DoesNotMatch(@"0\.92388|Cos\(MathF?\.PI / 8\)|45f? \*", source);
    }

    // ---- contact-work admission (bumper sphere 0.65 m, ball 0.34 m, zero gravity, target 8 m/s, threshold 0.05, cooldown 72) ----

    private const float BumperRadius = .65f;
    private static readonly LawMaterial Elastic = new(1, .1f, .3f, 0);
    private static readonly Vector3 Origin = new(0, 4, 0);

    private sealed record WorkScene(LawWorld World, GpuBodyId Ball, GpuBodyId Second, GpuColliderId Bumper);

    private static WorkScene ContactScene(Vector3 normal, Vector3 velocity, float gap = -.001f, float missZ = 0, bool second = false)
    {
        var radius = LawBalls.Radius(LawBall.Basketball);
        var scene = new LawScene();
        var ball = scene.Sphere(Origin + normal * (BumperRadius + radius + gap) + new Vector3(0, 0, missZ), radius, 1, Elastic, velocity,
            default, 0, 0);
        var other = second ? scene.Sphere(new(0, 4 - .989f, 0), radius, 1, Elastic, new(0, 4, 0), default, 0, 0) : default;
        var bumper = scene.StaticSphere(Origin, BumperRadius, new(.2f, .3f, .1f, MathF.Sqrt(.86f)), Elastic);
        scene.ContactWork(bumper, second ? new(BodyTargetKind.AllDynamic, default) : new(BodyTargetKind.NamedBody, ball), 8, 32, .05f, 72);
        return new(scene.Admit(), ball, other, scene.ColliderOf(bumper));
    }

    private static int OccurrenceSlot(byte[] world, GpuBodyId target)
    {
        var read = PhysicsGpuAbi.ReadContactWorks(world);
        for (var i = 0; i < read.OccurrenceCount; i++) if (read.Occurrence(i).Target == target) return i;
        throw new InvalidOperationException($"No occurrence slot for body {target.Value}.");
    }

    private static ContactWorkOccurrence Occurrence(byte[] world, GpuBodyId target) =>
        PhysicsGpuAbi.ReadContactWorks(world).Occurrence(OccurrenceSlot(world, target));

    private static uint Recorded(byte[] world) => PhysicsGpuAbi.ReadContactWorks(world)[0].OccurrenceCount;

    [Fact]
    public void AllSixRadialDirectionsQualifyAndArePaidIndependentOfTheOwnerRotation()
    {
        foreach (var normal in new[] { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ })
        {
            var s = ContactScene(normal, -4 * normal);
            var after = s.World.Step(1);
            Assert.Equal(1u, Recorded(after.World));
            var occurrence = Occurrence(after.World, s.Ball);
            Assert.True(occurrence.Effect == ContactWorkEffect.Paid && occurrence.ApproachSpeed.Value > 3.9f,
                $"{normal}: {occurrence.Effect} at {occurrence.ApproachSpeed.Value}");
            var v = after[s.Ball].Velocity; var outward = Vector3.Dot(v, normal);
            Assert.InRange(outward, 7.5f, 8.5f);
            Assert.True((v - outward * normal).Length() < .01f, $"{normal}: the paid response is radial ({v})");
        }
    }

    [Fact]
    public void AnObliqueContactKeepsItsOrdinarySpinAndTangentWhileADepthMissNeverQualifies()
    {
        var s = ContactScene(Vector3.UnitY, new(2, -4, 0));
        var after = s.World.Step(1);
        Assert.Equal(1u, Recorded(after.World));
        var body = after[s.Ball];
        Assert.True(body.Spin > 1 && body.Velocity.X is > 0 and < 2, $"spin {body.Spin}, tangent {body.Velocity.X}");
        // Solid-sphere tangential momentum about the contact is unchanged by friction and by the radial payment.
        Assert.True(MathF.Abs(body.Velocity.X - .4f * LawBalls.Radius(LawBall.Basketball) * body.Angular.Z - 2) < .03f);
        var missed = ContactScene(Vector3.UnitY, new(0, -4, 0), missZ: 2);
        Assert.Equal(0u, Recorded(missed.World.Step(1).World));
    }

    [Fact]
    public void RestingSeparatingBelowThresholdAndGrazingGapContactsRecordNoOccurrence()
    {
        foreach (var velocity in new Vector3[] { default, new(0, 2, 0), new(0, -.01f, 0) })
            Assert.Equal(0u, Recorded(ContactScene(Vector3.UnitY, velocity).World.Step(1).World));
        Assert.Equal(0u, Recorded(ContactScene(Vector3.UnitY, new(2, 0, 0), gap: .02f).World.Step(1).World));
    }

    /// <summary>Records one earlier qualified occurrence for the ball at ordinal 1 and starts the next tick at
    /// <paramref name="sourceOrdinal"/>.</summary>
    private static void PreviousOccurrence(WorkScene s, uint sourceOrdinal)
    {
        var bytes = s.World.Admitted;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(PhysicsGpuAbi.HeaderSourceOrdinalOffset), sourceOrdinal);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(PhysicsGpuAbi.ContactWorksOffset + PhysicsGpuAbi.ContactWorkOccurrenceCountOffset), 1);
        var at = PhysicsGpuAbi.WorkOccurrencesOffset + OccurrenceSlot(bytes, s.Ball) * PhysicsGpuAbi.WorkOccurrenceBytes;
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at + PhysicsGpuAbi.WorkOccurrenceColliderOffset),
            (ushort)s.World.Scene.Layout.GetColliderSlot(s.Bumper));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + PhysicsGpuAbi.WorkOccurrenceSequenceOffset), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + PhysicsGpuAbi.WorkOccurrenceOrdinalOffset), 1);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(at + PhysicsGpuAbi.WorkOccurrenceApproachOffset), 4);
    }

    [Fact]
    public void CooldownEligibilityIsPerBodyAndOpensAtTheExactSeventyTwoStepBoundary()
    {
        var before = ContactScene(Vector3.UnitY, new(0, -4, 0));
        PreviousOccurrence(before, 68);
        var held = before.World.Step(1).World;
        Assert.Equal(1u, Recorded(held));
        Assert.Equal((1u, 1u), (Occurrence(held, before.Ball).Sequence, Occurrence(held, before.Ball).EventOrdinal));
        var pair = ContactScene(Vector3.UnitY, new(0, -4, 0), second: true);
        PreviousOccurrence(pair, 68);
        var other = pair.World.Step(1).World;
        Assert.Equal(2u, Recorded(other)); // the second body has no earlier occurrence, so its own cooldown is open
        Assert.Equal(2u, Occurrence(other, pair.Second).Sequence);
        Assert.Equal(1u, Occurrence(other, pair.Ball).Sequence);
        var open = ContactScene(Vector3.UnitY, new(0, -4, 0));
        PreviousOccurrence(open, 72);
        var eligible = open.World.Step(1).World;
        Assert.Equal((2u, 73u), (Occurrence(eligible, open.Ball).Sequence, Occurrence(eligible, open.Ball).EventOrdinal));
    }
}
