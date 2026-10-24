using System.Buffers.Binary;
using System.Numerics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 10.1 (ENGINE-CYLINDER): the solid cylinder collider and its compiled inertia, proven on the shipped C# engine
/// (WorkshopPhysicsEngine) through real admission (PhysicsSceneDeclaration → PhysicsGpuAbi.Admission) and tick progression.
/// Every expectation is derived analytically from the declared f32 geometry, never from observed output.
/// </summary>
public sealed class WorkshopPhysicsLawCylinderTests
{
    private static readonly float BenchY = LawScene.BenchY;
    /// <summary>The gravity a law body is admitted with (its f32 lane).</summary>
    private static readonly float G = -(float)LawScene.Gravity;
    private static Vector3 At(float x, float y, float z = 0f) => new(x, y, z);
    private static Quaternion AboutX(double degrees) => Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)(degrees * Math.PI / 180));
    private static Quaternion AboutZ(double degrees) => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)(degrees * Math.PI / 180));
    /// <summary>A cylinder lying with its axis along world Z.</summary>
    private static readonly Quaternion AlongZ = AboutX(90);
    private static readonly LawMaterial Dead = new(0f, .1f, .5f, 0f);

    // ---- inertia ----

    [Theory]
    [InlineData(.4, .17, 1.5)]   // the Pulley wheel's declared size (CAT-053)
    [InlineData(.22, 1.02, 3)]   // the Lever fulcrum post (CAT-034)
    [InlineData(.0625, 8, .5)]   // a rod, w >> r
    public void CompiledMomentsAreTheSolidCylinderMomentsOfTheDeclaredGeometry(double radius, double width, double mass)
    {
        var body = new RigidBodyDeclaration(new(1), RigidMotionKind.Dynamic, default, default, CanonicalRotation.Identity, default, default,
            new((float)mass), default, default);
        var collider = new ColliderDeclaration(new(2), new(1), new(3), ColliderShapeKind.Cylinder, RigidLocalPose.Identity,
            new((float)radius), new(0f, (float)(width / 2), 0f));
        var properties = RigidMassProperties.Compile(body, collider);
        double m = (double)(float)mass, r = (double)(float)radius, h = (double)(float)(width / 2);
        // Six f32 roundings at most (r², h², the division by 3, the sum, the two products): 6 x 2^-24 relative to the exact
        // moment of the declared f32 inputs.
        const double bound = 6.0 / (1 << 24);
        Near(properties.Y, m * r * r / 2, bound);
        Near(properties.X, m * (r * r / 4 + h * h / 3), bound);
        Near(properties.Z, m * (r * r / 4 + h * h / 3), bound);
        Assert.Equal(properties.X, properties.Z);
        Assert.Equal(collider.Pose.Rotation, properties.PrincipalFrame);

        static void Near(PrincipalInertia value, double expected, double bound)
        {
            value.Validate();
            var actual = (double)value.Moment;
            Assert.True(Math.Abs(actual - expected) / expected <= bound, $"moment {actual} vs analytic {expected}");
        }
    }

    // ---- declaration and admission ----

    public static TheoryData<string, float, float, float> InvalidCylinders() => new()
    {
        { "zero radius", 0f, .1f, 1f }, { "negative radius", -.2f, .1f, 1f }, { "NaN radius", float.NaN, .1f, 1f },
        { "infinite radius", float.PositiveInfinity, .1f, 1f }, { "negative infinite radius", float.NegativeInfinity, .1f, 1f },
        { "zero width", .2f, 0f, 1f }, { "negative width", .2f, -.1f, 1f }, { "NaN width", .2f, float.NaN, 1f },
        { "infinite width", .2f, float.PositiveInfinity, 1f },
        { "zero mass", .2f, .1f, 0f }, { "negative mass", .2f, .1f, -1f }, { "NaN mass", .2f, .1f, float.NaN },
        { "infinite mass", .2f, .1f, float.PositiveInfinity },
    };

    [Theory]
    [MemberData(nameof(InvalidCylinders))]
    public void ANonPositiveOrNonFiniteRadiusWidthOrMassIsRejectedAtomically(string _, float radius, float halfWidth, float mass)
    {
        // A running world, stepped by one engine; the refused admission must leave that engine's next tick exactly the reference's.
        var valid = new LawScene();
        var drum = valid.Cylinder(At(0, BenchY + .3f), .3f, .1f, 1f, Dead, AlongZ, velocity: At(1, 0, 0));
        var world = valid.Admit();
        var engine = new WorkshopPhysicsEngine();
        var (source, candidate, inputs) = Start(engine, world);
        for (var tick = 0; tick < 30; tick++) { engine.Advance(source, candidate, inputs); (source, candidate) = (candidate, source); }
        var before = (byte[])source.Clone();

        var invalid = new LawScene();
        invalid.Cylinder(At(0, BenchY + 1), radius, halfWidth, mass, Dead);
        Assert.Throws<ArgumentException>(() => invalid.Admit());
        if (mass == 1f)
        {
            // The declaration itself refuses the geometry, so a static cylinder (no mass compilation) is refused too.
            var fixedScene = new LawScene();
            fixedScene.StaticCylinder(At(0, BenchY + 1), radius, halfWidth);
            Assert.Throws<ArgumentException>(() => fixedScene.Admit());
        }

        Assert.Equal(before, source);
        engine.Advance(source, candidate, inputs);
        Assert.Equal(world.Step(31).World, candidate);
        Assert.True(LawState.Read(world.Scene, candidate, drum).Finite);
    }

    [Fact]
    public void UnusedCylinderLanesAndUnequalBakedRadiiAreRejected()
    {
        ColliderDeclaration Cylinder(float x, float z) => new(new(2), new(1), new(3), ColliderShapeKind.Cylinder, RigidLocalPose.Identity,
            new(.3f), new(x, .1f, z));
        Cylinder(0f, 0f).Validate();
        Assert.Throws<ArgumentException>(() => Cylinder(.3f, 0f).Validate());
        Assert.Throws<ArgumentException>(() => Cylinder(0f, .3f).Validate());
        Assert.Throws<ArgumentException>(() => Cylinder((float)(-0.0), 0f).Validate());
        // Range: radius in [1/16, 16] (the sphere range), half-width in [1/1024, 16] (the box range).
        Assert.Throws<ArgumentException>(() => (Cylinder(0f, 0f) with { Radius = new((float)(1.0 / 32)) }).Validate());
        Assert.Throws<ArgumentException>(() => (Cylinder(0f, 0f) with { Radius = new(17f) }).Validate());
        Assert.Throws<ArgumentException>(() => (Cylinder(0f, 0f) with { HalfExtents = new(0f, 17f, 0f) }).Validate());

        var baked = DeclaredPhysics.Admit(new(CuriousContraptions.Abstractions.ColliderShapeKind.Cylinder, new(.4f, .085f, .4f),
            new(0, .5f, 0), Quaternion.Identity, default));
        Assert.Equal(ColliderShapeKind.Cylinder, baked.ShapeKind);
        Assert.Equal((float).4f, baked.Radius);
        Assert.Equal(new MetreVector(0f, (float).085f, 0f), baked.HalfExtents);
        Assert.Throws<ArgumentException>(() => DeclaredPhysics.Admit(new(CuriousContraptions.Abstractions.ColliderShapeKind.Cylinder,
            new(.4f, .085f, .41f), default, Quaternion.Identity, default)));
        Assert.Throws<ArgumentException>(() => DeclaredPhysics.Admit(new(CuriousContraptions.Abstractions.ColliderShapeKind.Capsule,
            new(.4f, .085f, .4f), default, Quaternion.Identity, default)));
    }

    [Fact]
    public void AnUndefinedShapeAtIngestIsATypedInvalidDeclarationThatChangesNothingElse()
    {
        var scene = new LawScene();
        var drum = scene.Cylinder(At(0, BenchY + .3f), .3f, .1f, 1f, Dead, AlongZ);
        var world = scene.Admit();
        var shapeOffset = world.Scene.Layout.GetColliderOffset(scene.ColliderOf(drum)) + 16;
        Assert.Equal((uint)ColliderShapeKind.Cylinder, BinaryPrimitives.ReadUInt32LittleEndian(world.Admitted.AsSpan(shapeOffset)));

        var engine = new WorkshopPhysicsEngine();
        var (source, candidate, inputs) = Start(engine, world);
        BinaryPrimitives.WriteUInt32LittleEndian(source.AsSpan(shapeOffset), 4);
        var original = (byte[])source.Clone();
        engine.Advance(source, candidate, inputs);
        Assert.Equal(PhysicsFailure.InvalidDeclaration, PhysicsGpuAbi.ReadFailure(candidate));
        Assert.Equal(original, source);
        var expected = (byte[])original.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(PhysicsGpuAbi.HeaderStatusOffset), (uint)PhysicsCandidateStatus.Invalid);
        BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(PhysicsGpuAbi.HeaderFailureOffset), (uint)PhysicsFailure.InvalidDeclaration);
        Assert.Equal(expected, candidate);

        // Control: the same world with its admitted Cylinder shape commits.
        var (good, next, goodInputs) = Start(engine, world);
        engine.Advance(good, next, goodInputs);
        Assert.Equal(PhysicsFailure.None, PhysicsGpuAbi.ReadFailure(next));
    }

    // ---- rolling ----

    /// <summary>A cylinder on a static box ramp (rotated θ about Z), its axis along Z, released from rest touching the surface.
    /// Returns the mean downhill acceleration between 0.25 s and 1.25 s and the final speed and spin.</summary>
    private static (double Acceleration, double Speed, double Spin) RollDownRamp(double degrees, float friction, float radius = .25f)
    {
        var theta = degrees * Math.PI / 180;
        var normal = new Vector3((float)-Math.Sin(theta), (float)Math.Cos(theta), 0);
        var uphill = new Vector3((float)Math.Cos(theta), (float)Math.Sin(theta), 0);
        var rampCentre = At(0, BenchY + 2);
        var scene = new LawScene();
        scene.StaticBox(rampCentre, At(4, .25f, 1), AboutZ(degrees), new LawMaterial(0f, .1f, friction, 0f));
        var start = rampCentre + .25f * normal + 3f * uphill + (radius) * normal;
        var drum = scene.Cylinder(start, radius, .2f, 1f, new LawMaterial(0f, .1f, friction, 0f), AlongZ);
        var world = scene.Admit();
        var downhill = -uphill;
        double v1 = 0, v2 = 0;
        var tps = world.TicksPerSecond;
        var final = world.Step(5 * tps / 4, (tick, state) =>
        {
            if (tick == tps / 4) v1 = Vector3.Dot(state[drum].Velocity, downhill);
            if (tick == 5 * tps / 4) v2 = Vector3.Dot(state[drum].Velocity, downhill);
        })[drum];
        return (v2 - v1, Vector3.Dot(final.Velocity, downhill), final.Spin);
    }

    [Theory]
    [InlineData(10.0)]
    [InlineData(25.0)]
    public void ACylinderRollsDownARampAtTwoThirdsGSinThetaWithoutSlipping(double degrees)
    {
        var sin = Math.Sin(degrees * Math.PI / 180);
        var tan = Math.Tan(degrees * Math.PI / 180);
        const float friction = .6f;
        Assert.True(friction >= tan / 3, "no-slip needs μ ≥ tanθ/3");
        var expected = 2.0 / 3 * G * sin;
        var (acceleration, speed, spin) = RollDownRamp(degrees, friction);
        Assert.True(Math.Abs(acceleration - expected) <= .03 * expected, $"a {acceleration} vs (2/3) g sinθ {expected}");
        var sphere = 5.0 / 7 * G * sin;
        Assert.True(Math.Abs(acceleration - sphere) > .03 * sphere, $"a {acceleration} is distinguishable from a sphere's {sphere}");
        var r = (.25f);
        Assert.True(Math.Abs(spin - speed / r) <= .02 * speed / r, $"ω {spin} vs v/r {speed / r}");
    }

    [Fact]
    public void BelowTheNoSlipFrictionTheCylinderSlipsFasterThanItRolls()
    {
        const double degrees = 25.0;
        var sin = Math.Sin(degrees * Math.PI / 180);
        const float friction = .05f;
        Assert.True(friction < Math.Tan(degrees * Math.PI / 180) / 3);
        var (acceleration, speed, spin) = RollDownRamp(degrees, friction);
        Assert.True(acceleration > 2.0 / 3 * G * sin * 1.05, $"a {acceleration} exceeds the rolling (2/3) g sinθ");
        // Sliding with kinetic friction: a = g (sinθ − μ cosθ).
        var sliding = G * (sin - friction * Math.Cos(degrees * Math.PI / 180));
        Assert.True(Math.Abs(acceleration - sliding) <= .05 * sliding, $"a {acceleration} vs sliding {sliding}");
        Assert.True(spin < .9 * speed / (.25f), $"ω {spin} < v/r {speed / (.25f)}");
    }

    [Fact]
    public void RollingResistanceOnTheCurvedSideBringsABenchRollToRestAndWithoutItTheRollContinues()
    {
        // Rolling with Crr on the side: a = −Crr g m r² / (I + m r²) = −(2/3) Crr g, so v0 = 1 m/s stops at 3 v0 / (2 Crr g).
        const float crr = .05f;
        var r = (.3f);
        (double Speed, double Spin, double Moving) Roll(float rolling)
        {
            var scene = new LawScene();
            var drum = scene.Cylinder(At(0, BenchY + r), .3f, .15f, 1f, new LawMaterial(0f, .1f, .6f, rolling), AlongZ,
                velocity: At(1, 0, 0), angular: At(0, 0, -1 / r));
            var world = scene.Admit();
            var stop = 3.0 / (2 * (crr) * G);
            double moving = 0;
            var final = world.Step((int)(1.5 * stop * world.TicksPerSecond), (tick, state) =>
            {
                if (tick == (int)(.5 * stop * world.TicksPerSecond)) moving = state[drum].Speed;
            })[drum];
            return (final.Speed, final.Spin, moving);
        }
        var resisted = Roll(crr);
        Assert.True(resisted.Moving > .3, $"still rolling at half the analytic stop time ({resisted.Moving})");
        Assert.True(resisted.Speed < .01 && resisted.Spin < .05, $"at rest by 1.5x the stop time (v {resisted.Speed}, ω {resisted.Spin})");
        var free = Roll(0f);
        Assert.True(free.Speed > .9, $"without rolling resistance the roll continues ({free.Speed})");
    }

    [Theory]
    [InlineData(4f)]
    [InlineData(12f)]
    public void AFastRollStaysOnItsCurvedSideAtConstantSpeed(float speed)
    {
        // The side support is the axis point, not the material point: a rolling contact never lifts off, so with Crr 0 and no drag
        // the roll keeps v and ω = v / r and the axis stays r above the bench on every tick.
        var r = (.25f);
        var scene = new LawScene();
        var drum = scene.Cylinder(At(0, BenchY + r), .25f, .3f, 1f, new LawMaterial(0f, .1f, .6f, 0f), AlongZ,
            velocity: At(speed, 0, 0), angular: At(0, 0, -speed / r));
        var world = scene.Admit();
        double lowest = double.MaxValue, highest = double.MinValue, slowest = double.MaxValue, fastest = double.MinValue;
        world.Step(world.TicksPerSecond, (_, state) =>
        {
            var body = state[drum];
            lowest = Math.Min(lowest, body.Position.Y); highest = Math.Max(highest, body.Position.Y);
            slowest = Math.Min(slowest, body.Speed); fastest = Math.Max(fastest, body.Speed);
            Assert.True(Math.Abs(body.Spin - body.Speed / r) <= .01 * speed / r, $"ω {body.Spin} vs v/r {body.Speed / r}");
        });
        Assert.True(lowest >= BenchY + r - .0005f && highest <= BenchY + r + .0005f, $"axis height {lowest - BenchY - r}..{highest - BenchY - r}");
        Assert.True(slowest >= .99 * speed && fastest <= 1.01 * speed, $"speed {slowest}..{fastest}");
    }

    // ---- rest ----

    [Fact]
    public void DroppedEndDownACylinderSettlesOnItsCapWithoutJitterOrDrift()
    {
        var h = (.085f);
        var scene = new LawScene();
        var wheel = scene.Cylinder(At(.2f, BenchY + h + .05f, -.1f), .4f, .085f, 1f, Dead);
        var world = scene.Admit();
        LawBody? settled = null;
        var final = world.Step(10 * world.TicksPerSecond, (tick, state) => { if (tick == 2 * world.TicksPerSecond) settled = state[wheel]; })[wheel];
        Assert.True(final.Position.Y >= BenchY + h - .0005f, $"penetration {BenchY + h - final.Position.Y}");
        Assert.True(final.Position.Y <= BenchY + h + .0005f, $"floats {final.Position.Y - BenchY - h}");
        Assert.True(final.Speed < .01 && final.Spin < .05, $"rest v {final.Speed} ω {final.Spin}");
        Assert.True(final.Tilt < .5f, $"tilt {final.Tilt}");
        Assert.True(Vector3.Distance(final.Position, settled!.Value.Position) < .0005f, $"drift {final.Position - settled.Value.Position}");
        Assert.True((final.Rotation - settled.Value.Rotation).Length() < 1e-3f, "no rotational drift");
    }

    [Fact]
    public void DroppedOnItsSideACylinderRestsAlongALineWithoutCreepingAlongItsAxis()
    {
        var r = (.25f);
        var scene = new LawScene();
        var drum = scene.Cylinder(At(0, BenchY + r + .03f, .3f), .25f, .4f, 1f, Dead, AlongZ);
        var world = scene.Admit();
        LawBody? settled = null;
        var final = world.Step(10 * world.TicksPerSecond, (tick, state) => { if (tick == 2 * world.TicksPerSecond) settled = state[drum]; })[drum];
        Assert.InRange(final.Position.Y, BenchY + r - .0005f, BenchY + r + .0005f);
        Assert.InRange(final.Tilt, 89.5f, 90.5f);
        Assert.True(MathF.Abs(final.Position.Z - settled!.Value.Position.Z) < .0005f, $"axial creep {final.Position.Z - settled.Value.Position.Z}");
        Assert.True(MathF.Abs(final.Position.X - settled.Value.Position.X) < .0005f, $"rolling creep {final.Position.X - settled.Value.Position.X}");
        Assert.True(final.Speed < .01 && final.Spin < .05, $"rest v {final.Speed} ω {final.Spin}");
    }

    [Theory]
    [InlineData(20.0, true)]   // past atan(r/h) = 14.04°: the centre is outside the rim, it topples
    [InlineData(8.0, false)]   // inside it: it rights itself onto its cap
    public void ATallCylinderTippedPastItsRimTopplesOntoItsSideAndOtherwiseRightsItself(double degrees, bool topples)
    {
        float r = (.1f), h = (.4f);
        var theta = degrees * Math.PI / 180;
        var centreY = BenchY + (float)(h * Math.Cos(theta) + r * Math.Sin(theta)) + .0005f;
        var scene = new LawScene();
        var rod = scene.Cylinder(At(0, centreY), .1f, .4f, 1f, Dead, AboutZ(degrees));
        var world = scene.Admit();
        var final = world.Step(4 * world.TicksPerSecond)[rod];
        if (topples)
        {
            Assert.InRange(final.Tilt, 88f, 92f);
            Assert.InRange(final.Position.Y, BenchY + r - .001f, BenchY + r + .001f);
        }
        else
        {
            Assert.True(final.Tilt < 1f, $"tilt {final.Tilt}");
            Assert.InRange(final.Position.Y, BenchY + h - .001f, BenchY + h + .001f);
        }
    }

    // ---- stacking ----

    private static void Holds(LawWorld world, GpuBodyId body, float restY, float tolerance = .0015f, float seconds = 3f)
    {
        var maximumY = float.MinValue;
        var final = world.Step((int)(seconds * world.TicksPerSecond), (_, state) => maximumY = MathF.Max(maximumY, state[body].Position.Y))[body];
        Assert.InRange(final.Position.Y, restY - tolerance, restY + tolerance);
        Assert.True(maximumY < restY + .02f, $"never launched (max {maximumY - restY} above rest)");
        Assert.True(final.Speed < .02 && final.Spin < .05, $"rest v {final.Speed} ω {final.Spin}");
    }

    [Fact]
    public void ABoxRestsOnACylinderCap()
    {
        var h = (.085f);
        var scene = new LawScene();
        scene.Cylinder(At(0, BenchY + h), .4f, .085f, 1f, Dead);
        var box = scene.Box(At(.05f, BenchY + 2 * h + (.1f) + .005f), At(.15f, .1f, .15f), .5f, LawMaterial.Domino);
        Holds(scene.Admit(), box, BenchY + 2 * h + (.1f));
    }

    [Fact]
    public void ACylinderStandsOnItsEndAtopABox()
    {
        var h = (.085f);
        var scene = new LawScene();
        scene.Box(At(0, BenchY + (.2f)), At(.3f, .2f, .3f), 2f, LawMaterial.Domino);
        var wheel = scene.Cylinder(At(0, BenchY + 2 * (.2f) + h + .005f), .25f, .085f, 1f, Dead);
        Holds(scene.Admit(), wheel, BenchY + 2 * (.2f) + h);
    }

    [Fact]
    public void AHorizontalCylinderIsCradledByTwoBoxEdges()
    {
        // Inner top edges at x = ±0.15, top 0.4 above the bench: the r = 0.25 drum's axis rests √(r² − 0.15²) above them.
        var top = BenchY + 2 * (.2f);
        var r = (.25f);
        var rest = top + MathF.Sqrt(r * r - .15f * .15f);
        var scene = new LawScene();
        scene.StaticBox(At(-.35f, BenchY + (.2f)), At(.2f, .2f, .5f));
        scene.StaticBox(At(.35f, BenchY + (.2f)), At(.2f, .2f, .5f));
        var drum = scene.Cylinder(At(0, rest + .005f), .25f, .4f, 1f, Dead, AlongZ);
        var world = scene.Admit();
        Holds(world, drum, rest);
        Assert.True(MathF.Abs(world.Step(3 * world.TicksPerSecond)[drum].Position.X) < .001f, "centred between the edges");
    }

    [Fact]
    public void ALogRestsInTheGrooveOfTwoParallelLogs()
    {
        // Static logs (r 0.2) at x = ±0.25: the top log's axis rests √(0.4² − 0.25²) above theirs, held along its whole length.
        var r = (.2f);
        var below = BenchY + 1;
        var rest = below + MathF.Sqrt(4 * r * r - .25f * .25f);
        var scene = new LawScene();
        scene.StaticCylinder(At(-.25f, below), .2f, .5f, AlongZ);
        scene.StaticCylinder(At(.25f, below), .2f, .5f, AlongZ);
        var log = scene.Cylinder(At(0, rest + .005f, .2f), .2f, .5f, 1f, Dead, AlongZ);
        var world = scene.Admit();
        Holds(world, log, rest);
        var final = world.Step(3 * world.TicksPerSecond)[log];
        Assert.InRange(final.Tilt, 89.5f, 90.5f);
        Assert.True(MathF.Abs(final.Position.X) < .001f && MathF.Abs(final.Position.Z - .2f) < .001f, $"drift {final.Position}");
    }

    [Fact]
    public void ALogLiesAcrossAStandingCylindersCap()
    {
        // The log's lowest generator crosses the cap disc: a chord of two points (z = ±0.3), the axis r above the cap. Its centre at
        // z = 0.15 is inside the chord, so it holds level.
        var top = BenchY + 1 + (.2f);
        var rest = top + (.1f);
        var scene = new LawScene();
        scene.StaticCylinder(At(0, BenchY + 1), .3f, .2f);
        var log = scene.Cylinder(At(0, rest + .005f, .15f), .1f, .5f, 1f, Dead, AlongZ);
        var world = scene.Admit();
        Holds(world, log, rest);
        var final = world.Step(3 * world.TicksPerSecond)[log];
        Assert.InRange(final.Tilt, 89.5f, 90.5f);
        Assert.True(MathF.Abs(final.Position.Z - .15f) < .001f, $"drift {final.Position}");
    }

    [Fact]
    public void ThreeCylindersStackEndToEnd()
    {
        var h = (.1f);
        var scene = new LawScene();
        scene.Cylinder(At(0, BenchY + h + .002f), .2f, .1f, .5f, Dead);
        scene.Cylinder(At(0, BenchY + 3 * h + .004f), .2f, .1f, .5f, Dead);
        var top = scene.Cylinder(At(0, BenchY + 5 * h + .006f), .2f, .1f, .5f, Dead);
        var world = scene.Admit();
        Holds(world, top, BenchY + 5 * h, .002f);
        Assert.True(world.Step(3 * world.TicksPerSecond)[top].Tilt < 1f);
    }

    // ---- pairs: a ball reflects off the true surface (frictionless, elastic) ----

    /// <summary>A ball (r 0.1, no gravity) against a static cylinder (r 0.3, half-width 0.4, axis along Z, centre at y 1), both
    /// elastic and frictionless: the outgoing velocity is v − 2 (v·n) n for the analytic surface normal n.</summary>
    private static void Reflects(Vector3 start, Vector3 velocity, Vector3 normal)
    {
        var elastic = new LawMaterial(1f, .1f, 0f, 0f);
        var scene = new LawScene(bench: false);
        scene.StaticCylinder(At(0, 1), .3f, .4f, AlongZ, elastic);
        var ball = scene.Sphere(start, .1f, 1f, elastic, velocity, default, 0f, 0f);
        var world = scene.Admit();
        var final = world.Step(world.TicksPerSecond)[ball];
        var expected = velocity - 2 * Vector3.Dot(velocity, normal) * normal;
        var angle = MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.Normalize(final.Velocity), Vector3.Normalize(expected)), -1f, 1f)) * 180 / MathF.PI;
        Assert.True(angle < 2f, $"outgoing {final.Velocity} vs analytic {expected} ({angle}°)");
        Assert.InRange(final.Velocity.Length(), .95f * expected.Length(), 1.02f * expected.Length());
    }

    [Fact]
    public void ABallOffTheCurvedSideReflectsRadially()
    {
        // Impact parameter 0.2 = (R + r) sin 30°: the normal is radial, (cos 30°, sin 30°, 0); the axial component is untouched.
        var normal = new Vector3(MathF.Cos(MathF.PI / 6), .5f, 0);
        Reflects(At(1.5f, 1.2f, 0), At(-3, 0, .5f), normal);
    }

    [Fact]
    public void ABallOffTheFlatEndReflectsAxially() => Reflects(At(.1f, 1.05f, 1.5f), At(.5f, 0, -3), Vector3.UnitZ);

    [Fact]
    public void ABallOffTheRimReflectsAlongTheRimNormal()
    {
        // Centre path x = r + 0.05 meets the rim circle (r, ·, h) at distance R: n = (0.05, 0, √(R² − 0.05²)) / R.
        var normal = Vector3.Normalize(new Vector3(.05f, 0, MathF.Sqrt(.01f - .0025f)));
        Reflects(At((.3f) + .05f, 1, 1.5f), At(0, 0, -3), normal);
    }

    [Fact]
    public void ABallInTheBoundingBoxCornerOutsideTheTrueCylinderIsNeverTouched()
    {
        // Radial distance 0.9 (r + R) √2 > r + R: inside the box hull of the cylinder grown by R, outside the cylinder.
        float r = (.3f), d = .9f * ((.3f) + .1f);
        var scene = new LawScene(bench: false);
        scene.StaticCylinder(At(0, 1), .3f, .4f);
        var ball = scene.Sphere(At(d, 1, d), .1f, 1f, Dead, default, default, 0f, 0f);
        var world = scene.Admit();
        var final = world.Step(world.TicksPerSecond)[ball];
        Assert.Equal(At(d, 1, d), final.Position, new Vector3Comparer(1e-6f));
        Assert.Equal(0.0, final.Speed);
        Assert.Empty(Contacts(Collider(ShapeKind.Cylinder, At(0, 0), Quaternion.Identity, r, (.4f)),
            Collider(ShapeKind.Sphere, At(d, 0, d), Quaternion.Identity, (.1f), index: 1)));
        // Control: a box of the cylinder's bounds would touch it.
        Assert.NotEmpty(Contacts(Collider(ShapeKind.Box, At(0, 0), Quaternion.Identity, 0, At(r, (.4f), r)),
            Collider(ShapeKind.Sphere, At(d, 0, d), Quaternion.Identity, (.1f), index: 1)));
    }

    // ---- pairs: contact counts and gaps, through the shipped narrowphase ----

    private static SimCollider Collider(ShapeKind shape, Vector3 position, Quaternion rotation, float radius = 0f, float halfY = 0f,
        Vector3? half = null, int index = 0)
    {
        var body = new SimBody
        {
            Px = position.X, Py = position.Y, Pz = position.Z, Qx = rotation.X, Qy = rotation.Y, Qz = rotation.Z, Qw = rotation.W,
            Motion = 1, Mass = 1, InvMass = 1, Principal = Quaternion.Identity, InvI = Vector3.One, Q0 = Quaternion.Identity
        };
        var extents = half ?? new Vector3(0, halfY, 0);
        var collider = new SimCollider
        {
            Index = index, Body = body, Shape = shape, Radius = radius, Hx = extents.X, Hy = shape == ShapeKind.Cylinder ? halfY : extents.Y,
            Hz = extents.Z, Rq = Quaternion.Identity
        };
        WorkshopCollisionKernels.RefreshCollider(collider);
        return collider;
    }

    private static SimCollider Collider(ShapeKind shape, Vector3 position, Quaternion rotation, float radius, Vector3 half, int index = 0) =>
        Collider(shape, position, rotation, radius, half.Y, half, index);

    private static List<ContactPoint> Contacts(SimCollider a, SimCollider b, float margin = .005f)
    {
        var contacts = new List<ContactPoint>();
        WorkshopCollisionKernels.GenerateManifold(a, b, margin, contacts);
        return contacts;
    }

    private static void Gaps(List<ContactPoint> contacts, int count, float gap)
    {
        Assert.Equal(count, contacts.Count);
        foreach (var contact in contacts)
            Assert.True(MathF.Abs(contact.Gap - gap) <= 1e-4f, $"gap {contact.Gap} vs analytic {gap}");
    }

    [Fact]
    public void CylinderPlaneManifoldsHaveFourCapPointsTwoSidePointsOrOneRimPoint()
    {
        var plane = Collider(ShapeKind.Plane, Vector3.Zero, Quaternion.Identity, index: 1);
        Gaps(Contacts(Collider(ShapeKind.Cylinder, At(0, .2f + .002f), Quaternion.Identity, .3f, .2f), plane), 4, .002f);
        var side = Contacts(Collider(ShapeKind.Cylinder, At(0, .3f + .002f), AlongZ, .3f, .2f), plane);
        Gaps(side, 2, .002f);
        Assert.All(side, contact => Assert.Equal(ContactSupport.Axis, contact.SupportA));
        // Tilted 30°: the lowest rim point h sin30 + r cos30 below the centre.
        var tilted = MathF.Sin(MathF.PI / 6) * .2f + MathF.Cos(MathF.PI / 6) * .3f;
        var rim = Contacts(Collider(ShapeKind.Cylinder, At(0, tilted + .002f), AboutZ(60), .3f, .2f), plane);
        Gaps(rim, 1, .002f);
        Assert.Equal(ContactSupport.Material, rim[0].SupportA);
    }

    [Fact]
    public void SphereGapsFollowTheSideTheCapAndTheRim()
    {
        var cylinder = Collider(ShapeKind.Cylinder, Vector3.Zero, Quaternion.Identity, .3f, .2f);
        Gaps(Contacts(cylinder, Collider(ShapeKind.Sphere, At(.3f + .1f + .002f, .1f), Quaternion.Identity, .1f, index: 1)), 1, .002f);
        Gaps(Contacts(cylinder, Collider(ShapeKind.Sphere, At(.1f, .2f + .1f + .002f), Quaternion.Identity, .1f, index: 1)), 1, .002f);
        // Rim: centre at (r + 0.03, h + 0.04): distance 0.05 from the rim circle.
        Gaps(Contacts(cylinder, Collider(ShapeKind.Sphere, At(.33f, .24f), Quaternion.Identity, .046f, index: 1)), 1, .004f);
        // Inside past the side: exit by the side (depth r − ρ = 0.05 < h − |y| = 0.2).
        Gaps(Contacts(cylinder, Collider(ShapeKind.Sphere, At(.25f, 0), Quaternion.Identity, .1f, index: 1)), 1, -.15f);
    }

    [Fact]
    public void BoxCylinderGapsMatchTheAnalyticDistances()
    {
        var standing = Collider(ShapeKind.Cylinder, Vector3.Zero, Quaternion.Identity, .3f, .2f);
        var half = At(.1f, .1f, .1f);
        // Box face on the curved side: the generator clipped to the face, two points.
        Gaps(Contacts(standing, Collider(ShapeKind.Box, At(.3f + .1f + .002f, 0), Quaternion.Identity, 0, half, 1)), 2, .002f);
        // Box above the cap, inside the disc: its four vertices.
        Gaps(Contacts(standing, Collider(ShapeKind.Box, At(.05f, .2f + .1f + .003f), Quaternion.Identity, 0, half, 1)), 4, .003f);
        // A vertical box edge (box turned 45° about Y) along the side: the common stretch, two points.
        var turned = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4);
        Gaps(Contacts(standing, Collider(ShapeKind.Box, At(.3f + .1f * MathF.Sqrt(2) + .002f, 0), turned, 0, half, 1)), 2, .002f);
        // A box edge across a lying cylinder's side (box turned 45° about X, edge along X over an axis along Z): one point.
        var lying = Collider(ShapeKind.Cylinder, Vector3.Zero, AlongZ, .3f, .5f);
        Gaps(Contacts(lying, Collider(ShapeKind.Box, At(0, .3f + .1f * MathF.Sqrt(2) + .002f), AboutX(45), 0, half, 1)), 1, .002f);
    }

    [Fact]
    public void ABoxEdgePastTheRimIsSeparatedByItsTrueRimDistance()
    {
        // A box turned 45° about Z: its bottom edge runs along Z at (r + dx, h + dy). Its true distance to the rim is √(dx² + dy²).
        var standing = Collider(ShapeKind.Cylinder, Vector3.Zero, Quaternion.Identity, .3f, .2f);
        var edgeBelowCentre = .1f * MathF.Sqrt(2);
        SimCollider Box(float dx, float dy) =>
            Collider(ShapeKind.Box, At(.3f + dx, .2f + dy + edgeBelowCentre), AboutZ(45), 0, At(.1f, .1f, .1f), 1);
        Assert.Empty(Contacts(standing, Box(.01f, .02f)));
        Gaps(Contacts(standing, Box(.001f, .002f)), 1, MathF.Sqrt(.000005f));
    }

    [Fact]
    public void CylinderPairsGiveTwoPointsWhenParallelOneWhenCrossedAndFourCapOnCap()
    {
        var a = Collider(ShapeKind.Cylinder, Vector3.Zero, AlongZ, .3f, .5f);
        var parallel = Contacts(a, Collider(ShapeKind.Cylinder, At(.6f + .002f, 0), AlongZ, .3f, .4f, index: 1));
        Gaps(parallel, 2, .002f);
        Assert.All(parallel, contact => Assert.True(contact.SupportA == ContactSupport.Axis && contact.SupportB == ContactSupport.Axis));
        var crossed = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2);
        Gaps(Contacts(a, Collider(ShapeKind.Cylinder, At(0, .5f + .002f), crossed, .2f, .5f, index: 1)), 1, .002f);
        var lower = Collider(ShapeKind.Cylinder, Vector3.Zero, Quaternion.Identity, .2f, .1f);
        var capOnCap = Contacts(lower, Collider(ShapeKind.Cylinder, At(0, .2f + .002f), Quaternion.Identity, .2f, .1f, index: 1));
        Gaps(capOnCap, 4, .002f);
        Assert.All(capOnCap, contact => Assert.Equal(ContactSupport.Material, contact.SupportA));
        // A cap on a side: the generator's chord across the disc, two points.
        Gaps(Contacts(lower, Collider(ShapeKind.Cylinder, At(0, .1f + .15f + .002f), AlongZ, .15f, .5f, index: 1)), 2, .002f);
        // Separated beyond the margin: nothing.
        Assert.Empty(Contacts(a, Collider(ShapeKind.Cylinder, At(.6f + .02f, 0), AlongZ, .3f, .4f, index: 1)));
    }

    // ---- review F1: the cap on the plane at any azimuth (the true deepest rim point) ----

    private static Quaternion About(Vector3 axis, double degrees) =>
        Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), (float)(degrees * Math.PI / 180));

    /// <summary>A tilt of <paramref name="degrees"/> that moves the axis toward the horizontal azimuth (0 = toward U0, a cap point of
    /// the square; 45 = between two of them).</summary>
    private static Quaternion Tipped(double degrees, double azimuth) =>
        About(Vector3.Cross(Vector3.UnitY, new((float)Math.Cos(azimuth * Math.PI / 180), 0, (float)Math.Sin(azimuth * Math.PI / 180))), degrees);

    /// <summary>The true lowest point of a cylinder below its centre: h |a_y| + r √(1 − a_y²).</summary>
    private static float Lowest(Quaternion rotation, float r, float h)
    {
        var t = MathF.Abs(Vector3.Transform(Vector3.UnitY, rotation).Y);
        return h * t + r * MathF.Sqrt(MathF.Max(0f, 1f - t * t));
    }

    /// <summary>Kinetic plus potential energy (J) of a mass-m cylinder from its admitted principal moments.</summary>
    private static double Energy(LawBody b, float m, float r, float h)
    {
        double ix = m * (r * r / 4 + h * h / 3), iy = m * r * r / 2;
        var w = Vector3.Transform(b.Angular, Quaternion.Inverse(b.Rotation));
        return .5 * m * Squared(b.Velocity) + .5 * (ix * w.X * w.X + iy * w.Y * w.Y + ix * w.Z * w.Z) + m * G * (b.Position.Y - BenchY);
    }

    private static double Squared(Vector3 v) => (double)v.X * v.X + (double)v.Y * v.Y + (double)v.Z * v.Z;

    /// <summary>The energy a tick may add: lifting the body by the solver's contact slop (m g × 0.2 mm). A box's own position
    /// correction stays far below it.</summary>
    private static double SlopEnergy(float m) => m * G * WorkshopConstraintSolver.ContactSlop;

    private static (float Tilt, double Transient, double Resting, double Rise) Release(float radius, float halfWidth, double degrees,
        double azimuth, int seconds = 4)
    {
        var q = Tipped(degrees, azimuth);
        float r = (radius), h = (halfWidth);
        var scene = new LawScene();
        var body = scene.Cylinder(At(0, BenchY + Lowest(q, r, h) + .0005f), radius, halfWidth, 1f, Dead, q);
        var world = scene.Admit();
        double transient = 0, rise = 0;
        double? previous = null;
        var final = world.Step(seconds * world.TicksPerSecond, (_, state) =>
        {
            var b = state[body];
            transient = Math.Max(transient, BenchY - (b.Position.Y - Lowest(b.Rotation, r, h)));
            var e = Energy(b, (1f), r, h);
            if (previous is { } p) rise = Math.Max(rise, e - p);
            previous = e;
        })[body];
        return (final.Tilt, transient, BenchY - (final.Position.Y - Lowest(final.Rotation, r, h)), rise);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(22.5)]
    [InlineData(45.0)]
    public void ARodRightsItselfBelowAtanROverHAndTopplesAboveItAtEveryAzimuth(double azimuth)
    {
        // r 0.1, h 0.4: the centre passes over the rim at atan(r/h) = 14.04°, whatever the tilt's direction.
        Assert.True(Release(.1f, .4f, 10.5, azimuth).Tilt < 1f, "10.5° rights itself");
        Assert.True(Release(.1f, .4f, 12.5, azimuth).Tilt < 1f, "12.5° rights itself");
        Assert.InRange(Release(.1f, .4f, 15.5, azimuth).Tilt, 88f, 92f);
    }

    [Theory]
    [InlineData(.4f, .085f)]   // the Pulley wheel
    [InlineData(.22f, .51f)]   // the Lever fulcrum post
    [InlineData(.25f, .25f)]
    public void ATiltedCapLandsOnItsTrueLowestRimPointWithoutSinkingOrGainingEnergy(float radius, float halfWidth)
    {
        // Every tilt rights itself onto the cap (atan(r/h) > 20° for each). The envelope: transient penetration ≤ 5 % of the smaller
        // extent, resting ≤ 0.5 mm, and no tick gains more than the slop energy.
        var extent = 2 * MathF.Min((radius), (halfWidth));
        foreach (var azimuth in new[] { 0.0, 45.0 })
            foreach (var degrees in new[] { 5.0, 11.0, 20.0 })
            {
                var (tilt, transient, resting, rise) = Release(radius, halfWidth, degrees, azimuth);
                var at = $"az {azimuth} tilt {degrees}";
                Assert.True(tilt < 1f, $"{at}: rests on its cap (tilt {tilt})");
                Assert.True(transient <= .05 * extent, $"{at}: transient penetration {transient * 1000} mm");
                Assert.True(resting <= .0005, $"{at}: resting penetration {resting * 1000} mm");
                Assert.True(rise <= SlopEnergy((1f)), $"{at}: a tick gained {rise * 1000} mJ");
            }
    }

    [Fact]
    public void ANearUprightCapWithinTheMarginAlwaysHasItsDeepestContact()
    {
        var plane = Collider(ShapeKind.Plane, Vector3.Zero, Quaternion.Identity, index: 1);
        foreach (var (radius, halfWidth) in new[] { (.4f, .085f), (.22f, .51f) })
            foreach (var azimuth in new[] { 0.0, 30.0, 45.0 })
                foreach (var degrees in new[] { 2.0, 5.0, 8.0, 11.4 })
                {
                    var q = Tipped(degrees, azimuth);
                    var contacts = Contacts(Collider(ShapeKind.Cylinder, At(0, Lowest(q, radius, halfWidth) + .001f), q, radius, halfWidth), plane);
                    Assert.NotEmpty(contacts);
                    var nearest = contacts.Min(contact => contact.Gap);
                    Assert.True(MathF.Abs(nearest - .001f) <= 1e-4f, $"r {radius} az {azimuth} tilt {degrees}: nearest gap {nearest}");
                }
    }

    [Fact]
    public void ANearUprightCapOverABoxOrACapHasItsDeepestContact()
    {
        // The cap regime against a box face and against another cap: with the true lowest rim point 1 mm above the partner's top, the
        // nearest contact is that 1 mm at every azimuth (eight fixed samples alone miss it between samples).
        var box = Collider(ShapeKind.Box, At(0, -.5f), Quaternion.Identity, 0f, At(2, .5f, 2), 1);
        var drum = Collider(ShapeKind.Cylinder, At(0, -.5f), Quaternion.Identity, 1.5f, .5f, index: 1);
        foreach (var partner in new[] { box, drum })
            foreach (var azimuth in new[] { 0.0, 22.5, 30.0, 45.0 })
                foreach (var degrees in new[] { 2.0, 6.0, 11.0 })
                {
                    var q = Tipped(degrees, azimuth);
                    var wheel = Collider(ShapeKind.Cylinder, At(0, Lowest(q, .4f, .085f) + .001f), q, .4f, .085f);
                    var contacts = Contacts(wheel, partner);
                    Assert.NotEmpty(contacts);
                    var nearest = contacts.Min(contact => contact.Gap);
                    Assert.True(MathF.Abs(nearest - .001f) <= 1e-4f, $"{partner.Shape} az {azimuth} tilt {degrees}: nearest gap {nearest}");
                }
    }

    [Fact]
    public void RandomDropsGainNoMoreEnergyThanABoxAndTheSlop()
    {
        var rng = new Random(11);
        double cylinder = 0, box = 0;
        for (var drop = 0; drop < 30; drop++)
        {
            var q = Quaternion.Normalize(new((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1),
                (float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1)));
            {
                var scene = new LawScene();
                var body = scene.Cylinder(At(0, BenchY + .6f), .2f, .15f, 1f, Dead, q);
                var world = scene.Admit();
                double? previous = null;
                world.Step(3 * world.TicksPerSecond, (_, state) =>
                {
                    var e = Energy(state[body], 1f, (.2f), (.15f));
                    if (previous is { } p) cylinder = Math.Max(cylinder, e - p);
                    previous = e;
                });
            }
            {
                var scene = new LawScene();
                var body = scene.Box(At(0, BenchY + .6f), At(.2f, .15f, .2f), 1f, Dead, q);
                var world = scene.Admit();
                double hx = (.2f), hy = (.15f), hz = (.2f);
                double? previous = null;
                world.Step(3 * world.TicksPerSecond, (_, state) =>
                {
                    var b = state[body];
                    var w = Vector3.Transform(b.Angular, Quaternion.Inverse(b.Rotation));
                    var e = .5 * Squared(b.Velocity) + .5 * ((hy * hy + hz * hz) / 3 * w.X * w.X + (hx * hx + hz * hz) / 3 * w.Y * w.Y +
                        (hx * hx + hy * hy) / 3 * w.Z * w.Z) + G * (b.Position.Y - BenchY);
                    if (previous is { } p) box = Math.Max(box, e - p);
                    previous = e;
                });
            }
        }
        Assert.True(cylinder <= box + SlopEnergy(1f), $"worst tick gain: cylinder {cylinder * 1000} mJ, box {box * 1000} mJ");
    }

    // ---- review F2: gaps against an exact separation oracle ----

    private readonly record struct D3(double X, double Y, double Z)
    {
        public static D3 Of(Vector3 v) => new(v.X, v.Y, v.Z);
        public static D3 operator +(D3 a, D3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static D3 operator -(D3 a, D3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static D3 operator *(double s, D3 a) => new(s * a.X, s * a.Y, s * a.Z);
        public double Dot(D3 b) => X * b.X + Y * b.Y + Z * b.Z;
        public double Length => Math.Sqrt(Dot(this));
    }

    /// <summary>The nearest point of a shipped collider's true solid to p (double precision, in its own f32 frame): a cylinder clamps
    /// its axial and radial coordinates, a box its three local coordinates, a sphere its radius.</summary>
    private static D3 Project(SimCollider c, D3 p)
    {
        var centre = D3.Of(c.Position);
        var d = p - centre;
        switch (c.Shape)
        {
            case ShapeKind.Cylinder:
            {
                var axis = D3.Of(c.U1);
                var y = d.Dot(axis);
                var radial = d - y * axis;
                var rho = radial.Length;
                return centre + Math.Clamp(y, -c.Hy, c.Hy) * axis + (rho > c.Radius ? c.Radius / rho : 1) * radial;
            }
            case ShapeKind.Box:
                return centre + Math.Clamp(d.Dot(D3.Of(c.U0)), -c.Hx, c.Hx) * D3.Of(c.U0) +
                    Math.Clamp(d.Dot(D3.Of(c.U1)), -c.Hy, c.Hy) * D3.Of(c.U1) + Math.Clamp(d.Dot(D3.Of(c.U2)), -c.Hz, c.Hz) * D3.Of(c.U2);
            case ShapeKind.Sphere:
                return d.Length > c.Radius ? centre + (c.Radius / d.Length) * d : p;
            default:
                throw new ArgumentOutOfRangeException(nameof(c), c.Shape, "No projection.");
        }
    }

    /// <summary>The exact distance between two separated convex solids by alternating closed-form projections (von Neumann): the
    /// iterates converge to a closest pair and their distance only falls, run to a 1e-11 m step in double precision. It shares no code
    /// or method with the narrowphase.</summary>
    private static double Distance(SimCollider a, SimCollider b, double below = 0)
    {
        var x = D3.Of(a.Position);
        var y = Project(b, x);
        for (var iteration = 0; iteration < 200000; iteration++)
        {
            var nextX = Project(a, y);
            var nextY = Project(b, nextX);
            var moved = (nextX - x).Length + (nextY - y).Length;
            x = nextX; y = nextY;
            // The distance only falls, so once it is under `below` the caller's question (is it above?) is answered.
            if (moved < 1e-11 || (x - y).Length < below) break;
        }
        return (x - y).Length;
    }

    [Fact]
    public void TheDistanceOracleIsExactOnAnalyticPairs()
    {
        // Controls with closed forms: a sphere to the solid cylinder (max(|y| − h, 0) with max(ρ − r, 0), less R), and two parallel
        // coaxial cylinders end to end and side by side.
        var rng = new Random(5);
        for (var i = 0; i < 40; i++)
        {
            var q = RandomRotation(rng);
            var cylinder = Collider(ShapeKind.Cylinder, Vector3.Zero, q, .3f, .2f);
            var centre = new Vector3((float)rng.NextDouble() - .5f, (float)rng.NextDouble() - .5f, (float)rng.NextDouble() - .5f) * 1.6f;
            var d = D3.Of(centre);
            var y = d.Dot(D3.Of(cylinder.U1));
            var rho = Math.Sqrt(Math.Max(0, d.Dot(d) - y * y));
            double dy = Math.Abs(y) - .2f, dr = rho - .3f;
            var analytic = Math.Sqrt(Math.Pow(Math.Max(dy, 0), 2) + Math.Pow(Math.Max(dr, 0), 2)) - .1;
            if (analytic <= .001) continue;
            var oracle = Distance(cylinder, Collider(ShapeKind.Sphere, centre, Quaternion.Identity, .1f, index: 1));
            Assert.True(Math.Abs(oracle - analytic) < 1e-7, $"oracle {oracle} vs analytic {analytic}");
        }
        var a = Collider(ShapeKind.Cylinder, Vector3.Zero, AlongZ, .3f, .2f);
        Assert.True(Math.Abs(Distance(a, Collider(ShapeKind.Cylinder, At(0, 0, .45f), AlongZ, .2f, .2f, index: 1)) - .05) < 1e-7);
        Assert.True(Math.Abs(Distance(a, Collider(ShapeKind.Cylinder, At(.6f, 0, .1f), AlongZ, .2f, .2f, index: 1)) - .1) < 1e-6);
    }

    private static string Millimetres(List<ContactPoint> contacts) =>
        string.Join(", ", contacts.Select(c => (c.Gap * 1000).ToString("F3", System.Globalization.CultureInfo.InvariantCulture)));

    private static Quaternion RandomRotation(Random rng) => Quaternion.Normalize(new((float)(rng.NextDouble() * 2 - 1),
        (float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1)));

    /// <summary>The partner's centre along <paramref name="direction"/> at which the true separation is <paramref name="target"/>
    /// (bisection on the oracle), and the separation actually reached at that f32 pose.</summary>
    private static (SimCollider Partner, double Exact) Place(SimCollider fixedCylinder, Func<Vector3, SimCollider> partner, Vector3 direction,
        double target)
    {
        float lo = 0f, hi = 1.5f;
        for (var step = 0; step < 20; step++)
        {
            var mid = .5f * (lo + hi);
            var partnerAt = partner(direction * mid);
            // Overlapping solids converge to a common point (distance 0), never above the target.
            if (Distance(fixedCylinder, partnerAt, target) > target) hi = mid; else lo = mid;
        }
        var placed = partner(direction * hi);
        return (placed, Distance(fixedCylinder, placed));
    }

    /// <summary>Seeded random poses at true separations of 1 mm and 3 mm (inside the 5 mm margin), half of them aimed at a rim: every
    /// contact's gap is within 0.5 mm of the exact separation and never negative; at 20 mm there is no contact.</summary>
    private static void GapsMatchTheOracle(bool cylinderPartner, int seed)
    {
        var rng = new Random(seed);
        var failures = new List<string>();
        for (var pose = 0; pose < 200; pose++)
        {
            var a = Collider(ShapeKind.Cylinder, Vector3.Zero, RandomRotation(rng), (.3f), (.2f));
            var qb = RandomRotation(rng);
            SimCollider Partner(Vector3 at) => cylinderPartner
                ? Collider(ShapeKind.Cylinder, at, qb, (.2f), (.35f), index: 1)
                : Collider(ShapeKind.Box, at, qb, 0f, At((.25f), (.1f), (.15f)), 1);
            Vector3 direction;
            if (pose % 2 == 0)
                direction = Vector3.Normalize(new((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1)));
            else
            {
                // Aimed past a rim: from the centre through a random rim point of a random cap.
                var angle = rng.NextDouble() * 2 * Math.PI;
                var rim = (rng.Next(2) == 0 ? 1 : -1) * a.Hy * a.U1 + a.Radius * ((float)Math.Cos(angle) * a.U0 + (float)Math.Sin(angle) * a.U2);
                direction = Vector3.Normalize(rim);
            }
            foreach (var target in new[] { .001, .003 })
            {
                var (partner, exact) = Place(a, Partner, direction, target);
                var contacts = Contacts(a, partner);
                if (contacts.Count == 0) { failures.Add($"pose {pose} at {exact * 1000:F3} mm: no contact"); continue; }
                var nearest = contacts.Min(contact => contact.Gap);
                if (Math.Abs(nearest - exact) > .0005 || contacts.Any(contact => contact.Gap < 0f || contact.Gap < exact - .0005))
                    failures.Add($"pose {pose} at {exact * 1000:F3} mm: gaps {Millimetres(contacts)} mm");
            }
            var (far, farExact) = Place(a, Partner, direction, .02);
            if (Contacts(a, far).Count != 0) failures.Add($"pose {pose} at {farExact * 1000:F3} mm: contact beyond the margin");
        }
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void CylinderBoxGapsMatchTheExactSeparation() => GapsMatchTheOracle(false, 37);

    [Fact]
    public void CylinderCylinderGapsMatchTheExactSeparation() => GapsMatchTheOracle(true, 31);

    [Fact]
    public void ThinDiscsCrossingRimToRimGapsMatchTheExactSeparation()
    {
        // Two coins (r 0.3, half-width 0.01) at random orientations, aimed rim at rim: the case no closed-form axis reaches.
        var rng = new Random(43);
        var failures = new List<string>();
        for (var pose = 0; pose < 100; pose++)
        {
            var a = Collider(ShapeKind.Cylinder, Vector3.Zero, RandomRotation(rng), (.3f), (.01f));
            var qb = RandomRotation(rng);
            SimCollider Partner(Vector3 at) => Collider(ShapeKind.Cylinder, at, qb, (.3f), (.01f), index: 1);
            var angle = rng.NextDouble() * 2 * Math.PI;
            var direction = Vector3.Normalize((float)Math.Cos(angle) * a.U0 + (float)Math.Sin(angle) * a.U2 + .3f * a.U1);
            var (partner, exact) = Place(a, Partner, direction, .002);
            var contacts = Contacts(a, partner);
            if (contacts.Count == 0 || Math.Abs(contacts.Min(c => c.Gap) - exact) > .0005 || contacts.Any(c => c.Gap < 0f))
                failures.Add($"pose {pose} at {exact * 1000:F3} mm: gaps {Millimetres(contacts)} mm");
        }
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MotionlessPartnersApartStayApartInTheEngine(bool cylinderPartner)
    {
        // A static cylinder and a weightless motionless dynamic partner ≥ 2 mm away (true separation at the admitted pose): no contact
        // that does not exist may push it.
        var rng = new Random(cylinderPartner ? 31 : 37);
        var centre = At(0, 1);
        var pushed = new List<string>();
        for (var pose = 0; pose < 100; pose++)
        {
            var qa = RandomRotation(rng);
            var qb = RandomRotation(rng);
            var direction = Vector3.Normalize(new((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1)));
            var a = Collider(ShapeKind.Cylinder, centre, qa, (.3f), (.2f));
            SimCollider Partner(Vector3 offset) => cylinderPartner
                ? Collider(ShapeKind.Cylinder, centre + offset, qb, (.2f), (.35f), index: 1)
                : Collider(ShapeKind.Box, centre + offset, qb, 0f, At((.25f), (.1f), (.15f)), 1);
            var (placed, _) = Place(a, Partner, direction, .003);
            var scene = new LawScene(bench: false);
            scene.StaticCylinder(centre, .3f, .2f, qa, Dead);
            var body = cylinderPartner
                ? scene.Cylinder(placed.Position, .2f, .35f, 1f, Dead, qb, gravity: 0f)
                : scene.Box(placed.Position, At(.25f, .1f, .15f), 1f, Dead, qb, gravity: 0f);
            var world = scene.Admit();
            var initial = world.Initial(body);
            var admitted = Distance(a, cylinderPartner
                ? Collider(ShapeKind.Cylinder, initial.Position, initial.Rotation, (.2f), (.35f), index: 1)
                : Collider(ShapeKind.Box, initial.Position, initial.Rotation, 0f, At((.25f), (.1f), (.15f)), 1));
            Assert.True(admitted >= .002, $"pose {pose}: admitted separation {admitted * 1000} mm");
            var final = world.Step(60)[body];
            var moved = (final.Position - initial.Position).Length();
            if (final.Speed > 1e-3 || moved > 1e-4) pushed.Add($"pose {pose} ({admitted * 1000:F2} mm apart): speed {final.Speed}, moved {moved * 1000} mm");
        }
        Assert.True(pushed.Count == 0, string.Join(Environment.NewLine, pushed));
    }

    // ---- review F3: the end faces add no rolling resistance ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ACylinderOnItsCapIsUntouchedByRollingResistanceWhileOnItsSideItIsResisted(bool rocked)
    {
        // Coordinator decision: caps and rims do not roll, so their manifolds add no rolling row. A cylinder sliding or rocking on its
        // cap follows bit for bit the same path whatever its Crr; on its side the same Crr changes the roll.
        static (Vector3, Quaternion, Vector3, Vector3) Pose(LawBody b) => (b.Position, b.Rotation, b.Velocity, b.Angular);
        (Vector3, Quaternion, Vector3, Vector3) Upright(float rolling)
        {
            var scene = new LawScene();
            var h = (.15f);
            var body = scene.Cylinder(At(0, BenchY + h + .0005f), .2f, .15f, 1f, new LawMaterial(0f, .1f, .5f, rolling),
                velocity: rocked ? default : At(1, 0, 0), angular: rocked ? At(0, 0, 1.5f) : default);
            var world = scene.Admit();
            return Pose(world.Step(2 * world.TicksPerSecond)[body]);
        }
        var still = Upright(0f);
        Assert.Equal(still, Upright(.1f));
        Assert.NotEqual(Quaternion.Identity, still.Item2);

        (Vector3, Quaternion, Vector3, Vector3) Lying(float rolling)
        {
            var scene = new LawScene();
            var r = (.2f);
            var body = scene.Cylinder(At(0, BenchY + r), .2f, .15f, 1f, new LawMaterial(0f, .1f, .5f, rolling), AlongZ,
                velocity: At(1, 0, 0), angular: At(0, 0, -1 / r));
            var world = scene.Admit();
            return Pose(world.Step(2 * world.TicksPerSecond)[body]);
        }
        Assert.True(Lying(.1f).Item3.X < Lying(0f).Item3.X - .1f, "on its side the same Crr slows the roll");
    }

    // ---- tunnelling ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ACylinderAtSixtyFourMetresPerSecondNeverPassesAOneCentimetreWall(bool endOn)
    {
        var scene = new LawScene(bench: false);
        scene.StaticBox(At(0, 1), At(.005f, 1, 1));
        var rotation = endOn ? AboutZ(-90) : AlongZ;
        var drum = scene.Cylinder(At(-3, 1), .2f, .2f, 1f, new LawMaterial(.5f, .1f, .5f, 0f), rotation, velocity: At(64, 0, 0), gravity: 0f);
        var world = scene.Admit();
        var nearFace = -.005f - (.2f);
        var maximumX = float.MinValue;
        world.Step(world.TicksPerSecond, (_, state) => maximumX = MathF.Max(maximumX, state[drum].Position.X));
        Assert.True(maximumX < nearFace + .01f, $"reached x {maximumX}, the wall's near face is at {nearFace}");
    }

    [Fact]
    public void ABallAtSixtyFourMetresPerSecondNeverPassesAStaticCylinder()
    {
        var scene = new LawScene(bench: false);
        scene.StaticCylinder(At(0, 1), .1f, .5f, AlongZ);
        var ball = scene.Sphere(At(-3, 1), .1f, 1f, new LawMaterial(.5f, .1f, .5f, 0f), At(64, 0, 0), default, 0f, 0f);
        var world = scene.Admit();
        var maximumX = float.MinValue;
        world.Step(world.TicksPerSecond, (_, state) => maximumX = MathF.Max(maximumX, state[ball].Position.X));
        Assert.True(maximumX < -.2f + .01f, $"reached x {maximumX}");
    }

    // ---- determinism, Reset and the native save/load equivalent ----

    private static (byte[] Source, byte[] Candidate, PhysicsTickInputs Inputs) Start(WorkshopPhysicsEngine engine, LawWorld world)
    {
        engine.Reset();
        var source = (byte[])world.Admitted.Clone();
        return (source, new byte[source.Length], new PhysicsTickInputs([], new bool[world.Scene.Prismatics.Length]));
    }

    private static List<byte[]> Run(WorkshopPhysicsEngine engine, LawWorld world, int ticks)
    {
        var (source, candidate, inputs) = Start(engine, world);
        var frames = new List<byte[]>(ticks);
        for (var tick = 0; tick < ticks; tick++)
        {
            engine.Advance(source, candidate, inputs);
            (source, candidate) = (candidate, source);
            frames.Add((byte[])source.Clone());
        }
        return frames;
    }

    private static LawScene Mixed()
    {
        var scene = new LawScene();
        scene.Cylinder(At(-1, BenchY + .25f), .25f, .3f, 1f, new LawMaterial(0f, .1f, .6f, .05f), AlongZ, velocity: At(1, 0, 0));
        var h = (.085f);
        scene.Cylinder(At(1, BenchY + h + .05f), .4f, .085f, 1f, Dead);
        scene.Box(At(1, BenchY + 2 * h + .2f), At(.15f, .1f, .15f), .5f, LawMaterial.Domino);
        scene.StaticCylinder(At(0, BenchY + .5f, 2), .3f, .4f, AlongZ);
        scene.Ball(LawBall.Basketball, At(0, BenchY + 1.5f, 2));
        scene.Cylinder(At(-2, BenchY + .6f, -1), .2f, .4f, 1f, Dead, AboutZ(30));
        return scene;
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public void TwoRunsAndAResetReadmissionAreBitIdenticalForSixHundredTicks(SimulationCadence cadence)
    {
        var world = Mixed().Admit(cadence);
        var again = Mixed().Admit(cadence);
        Assert.Equal(world.Admitted, again.Admitted);
        var engine = new WorkshopPhysicsEngine();
        var first = Run(engine, world, 600);
        var reset = Run(engine, world, 600);
        var fresh = Run(new WorkshopPhysicsEngine(), again, 600);
        for (var tick = 0; tick < 600; tick++)
        {
            Assert.True(first[tick].AsSpan().SequenceEqual(reset[tick]), $"tick {tick + 1}: Reset re-admission differs");
            Assert.True(first[tick].AsSpan().SequenceEqual(fresh[tick]), $"tick {tick + 1}: second run differs");
        }
        Assert.All(world.Scene.Bodies.ToArray().Where(body => body.Motion == RigidMotionKind.Dynamic),
            body => Assert.True(LawState.Read(world.Scene, first[^1], body.Id).Finite));
    }

    [Fact]
    public void TheAdmittedCylinderRecordCarriesItsShapeAndExactF32Lanes()
    {
        var scene = new LawScene();
        var drum = scene.Cylinder(At(0, BenchY + .3f), .3f, .1f, 1f, Dead, AlongZ);
        var world = scene.Admit();
        var record = world.Admitted.AsSpan(world.Scene.Layout.GetColliderOffset(scene.ColliderOf(drum)));
        Assert.Equal((uint)ColliderShapeKind.Cylinder, BinaryPrimitives.ReadUInt32LittleEndian(record[PhysicsGpuAbi.ColliderShapeOffset..]));
        // The declared radius 0.3 and half-width 0.1 need a full f32 significand: the record holds their exact f32 bits.
        uint Lane(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(world.Admitted.AsSpan(
            world.Scene.Layout.GetColliderOffset(scene.ColliderOf(drum)) + offset));
        Assert.Equal(BitConverter.SingleToUInt32Bits(.3f), Lane(PhysicsGpuAbi.ColliderRadiusOffset));
        Assert.Equal(0u, Lane(PhysicsGpuAbi.ColliderHalfExtentsOffset));
        Assert.Equal(BitConverter.SingleToUInt32Bits(.1f), Lane(PhysicsGpuAbi.ColliderHalfExtentsOffset + 4));
        Assert.Equal(0u, Lane(PhysicsGpuAbi.ColliderHalfExtentsOffset + 8));
    }

    private sealed class Vector3Comparer(float tolerance) : IEqualityComparer<Vector3>
    {
        public bool Equals(Vector3 a, Vector3 b) => Vector3.Distance(a, b) <= tolerance;
        public int GetHashCode(Vector3 value) => 0;
    }

    // ---- review F4 and N5: dynamic contact energy and overlap depth, on the reviewer's own poses ----
    // The certified oracle is the reviewer's (round 2, evidence-10-1-review/round2/probes): an upper bound from alternating
    // projections and a lower bound from the exact support-function separation along the primal direction, refined locally.

    private static double SupportOf(SimCollider c, D3 u)
    {
        var p = D3.Of(c.Position);
        if (c.Shape == ShapeKind.Cylinder)
        {
            var t = Math.Abs(u.Dot(D3.Of(c.U1)));
            return p.Dot(u) + c.Hy * t + c.Radius * Math.Sqrt(Math.Max(0, 1 - t * t));
        }
        return p.Dot(u) + c.Hx * Math.Abs(u.Dot(D3.Of(c.U0))) + c.Hy * Math.Abs(u.Dot(D3.Of(c.U1))) + c.Hz * Math.Abs(u.Dot(D3.Of(c.U2)));
    }

    /// <summary>A certified lower bound on the separation of two solids (positive only when they are certainly apart).</summary>
    private static double CertifiedLower(SimCollider a, SimCollider b)
    {
        double Sep(D3 u) => -SupportOf(b, (-1) * u) - SupportOf(a, u);
        D3 pa = Project(a, D3.Of(b.Position)), pb = Project(b, pa);
        double upper = (pa - pb).Length, lower = double.NegativeInfinity;
        for (var round = 0; round < 40; round++)
        {
            for (var i = 0; i < 5000; i++) { pa = Project(a, pb); pb = Project(b, pa); }
            upper = (pa - pb).Length;
            if (upper < 1e-9) return double.NegativeInfinity;
            lower = Math.Max(lower, Sep((1 / upper) * (pb - pa)));
            if (upper - lower <= 1e-5) return lower;
        }
        var u = (1 / upper) * (pb - pa);
        var rng = new Random(3);
        for (var step = .01; step > 1e-10;)
        {
            var improved = false;
            for (var k = 0; k < 30; k++)
            {
                var v = u + step * new D3(rng.NextDouble() * 2 - 1, rng.NextDouble() * 2 - 1, rng.NextDouble() * 2 - 1);
                v = (1 / v.Length) * v;
                var s = Sep(v);
                if (s > lower) { lower = s; u = v; improved = true; }
            }
            if (!improved) step *= .5;
        }
        return lower;
    }

    /// <summary>The smallest distance along <paramref name="dir"/> at which B is certified apart from A (bisection over [0, 3]).</summary>
    private static float CertifiedTouch(SimCollider a, Func<Vector3, SimCollider> b, Vector3 centre, Vector3 dir)
    {
        float lo = 0f, hi = 3f;
        for (var step = 0; step < 30; step++)
        {
            var mid = .5f * (lo + hi);
            if (CertifiedLower(a, b(centre + dir * mid)) > 0) hi = mid; else lo = mid;
        }
        return hi;
    }

    private static double BoxEnergy(LawBody b, float m, Vector3 half)
    {
        var w = Vector3.Transform(b.Angular, Quaternion.Inverse(b.Rotation));
        double x = half.X, y = half.Y, z = half.Z;
        return .5 * m * Squared(b.Velocity) + .5 * m * ((y * y + z * z) / 3 * w.X * w.X + (x * x + z * z) / 3 * w.Y * w.Y +
            (x * x + y * y) / 3 * w.Z * w.Z) + m * G * b.Position.Y;
    }

    /// <summary>The reviewer's C6: a 1 kg box resting 0.3 mm into a static partner (A, cylinder or box) on top of it, the pose turned so
    /// the A→B direction is up, under normal gravity for 2 s. Returns the worst per-tick rise of the box's energy.</summary>
    private static double TumbleRise(bool cylinderBelow, int seed, int pose)
    {
        var centre = At(0, 1);
        var rng = new Random(seed);
        Quaternion qa = default, qb = default;
        var dir = Vector3.Zero;
        for (var i = 0; i <= pose; i++)
        {
            qa = RandomRotation(rng); qb = RandomRotation(rng);
            dir = Vector3.Normalize(new((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1)));
        }
        var axis = Vector3.Cross(dir, Vector3.UnitY);
        var angle = MathF.Acos(Math.Clamp(Vector3.Dot(dir, Vector3.UnitY), -1f, 1f));
        var turn = axis.Length() < 1e-6f ? Quaternion.Identity : Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), angle);
        qa = Quaternion.Normalize(turn * qa); qb = Quaternion.Normalize(turn * qb);
        var half = At((.25f), (.1f), (.15f));
        var a = cylinderBelow ? Collider(ShapeKind.Cylinder, centre, qa, (.3f), (.2f))
            : Collider(ShapeKind.Box, centre, qa, 0f, At((.3f), (.2f), (.3f)), 0);
        var touch = CertifiedTouch(a, at => Collider(ShapeKind.Box, at, qb, 0f, half, 1), centre, Vector3.UnitY);
        var scene = new LawScene(bench: false);
        if (cylinderBelow) scene.StaticCylinder(centre, .3f, .2f, qa, Dead); else scene.StaticBox(centre, At(.3f, .2f, .3f), qa, Dead);
        var body = scene.Box(centre + Vector3.UnitY * (touch - 2e-4f - .0003f), At(.25f, .1f, .15f), 1f, Dead, qb);
        var world = scene.Admit();
        double rise = 0;
        double? previous = null;
        world.Step(2 * world.TicksPerSecond, (_, state) =>
        {
            var e = BoxEnergy(state[body], 1f, half);
            if (previous is { } p) rise = Math.Max(rise, e - p);
            previous = e;
        });
        return rise;
    }

    [Fact]
    public void ACylinderTumblingOverACapRimGainsNoMoreThanTheSlopEnergy()
    {
        // The C6 construction with a dynamic cylinder (r 0.2, half-width 0.15) on the static cylinder, seed 402 poses 0–20.
        var results = new List<string>();
        for (var pose = 0; pose < 21; pose++)
        {
            var centre = At(0, 1);
            var rng = new Random(402);
            Quaternion qa = default, qb = default;
            var dir = Vector3.Zero;
            for (var i = 0; i <= pose; i++)
            {
                qa = RandomRotation(rng); qb = RandomRotation(rng);
                dir = Vector3.Normalize(new((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1)));
            }
            var axis = Vector3.Cross(dir, Vector3.UnitY);
            var turn = axis.Length() < 1e-6f ? Quaternion.Identity : Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), MathF.Acos(Math.Clamp(Vector3.Dot(dir, Vector3.UnitY), -1f, 1f)));
            qa = Quaternion.Normalize(turn * qa); qb = Quaternion.Normalize(turn * qb);
            var a = Collider(ShapeKind.Cylinder, centre, qa, (.3f), (.2f));
            var touch = CertifiedTouch(a, at => Collider(ShapeKind.Cylinder, at, qb, (.2f), (.15f), index: 1), centre, Vector3.UnitY);
            var scene = new LawScene(bench: false);
            scene.StaticCylinder(centre, .3f, .2f, qa, Dead);
            var body = scene.Cylinder(centre + Vector3.UnitY * (touch - 2e-4f - .0003f), .2f, .15f, 1f, Dead, qb);
            var world = scene.Admit();
            double rise = 0; double? previous = null;
            world.Step(2 * world.TicksPerSecond, (_, state) =>
            {
                var e = Energy(state[body], 1f, (.2f), (.15f));
                if (previous is { } p) rise = Math.Max(rise, e - p);
                previous = e;
            });
            if (rise > SlopEnergy(1f)) results.Add($"pose {pose}: worst tick gain {rise * 1000:F3} mJ");
        }
        Assert.True(results.Count == 0, string.Join(Environment.NewLine, results));
    }

    [Fact]
    public void ABoxTumblingOverACapRimGainsNoMoreEnergyThanOverABox()
    {
        // The reviewer's three spiking poses (seed 402: 2, 4, 135) against the box–box control poses (seed 401: 0–19).
        var control = Enumerable.Range(0, 20).Max(pose => TumbleRise(false, 401, pose));
        var bound = control + SlopEnergy(1f);
        foreach (var pose in new[] { 2, 4, 135 })
        {
            var rise = TumbleRise(true, 402, pose);
            Assert.True(rise <= bound, $"pose {pose}: worst tick gain {rise * 1000:F3} mJ, bound {bound * 1000:F3} mJ (control {control * 1000:F3})");
        }
    }

    /// <summary>The reviewer's C8: 40 drops (seed 77) of a 1 kg body onto a static partner on the bench; the worst per-tick energy rise.</summary>
    private static double DropRise(bool cylinderDropped, bool cylinderBelow)
    {
        var rng = new Random(77);
        double worst = 0;
        var half = At((.25f), (.1f), (.15f));
        float r = (.2f), h = (.15f);
        for (var drop = 0; drop < 40; drop++)
        {
            var q = RandomRotation(rng);
            var offset = (float)(rng.NextDouble() * .4 - .2);
            var scene = new LawScene();
            if (cylinderBelow) scene.StaticCylinder(At(0, BenchY + .3f), .3f, .5f, AlongZ, Dead);
            else scene.StaticBox(At(0, BenchY + .3f), At(.3f, .3f, .5f), null, Dead);
            var body = cylinderDropped ? scene.Cylinder(At(offset, BenchY + .95f), .2f, .15f, 1f, Dead, q)
                : scene.Box(At(offset, BenchY + .95f), At(.25f, .1f, .15f), 1f, Dead, q);
            var world = scene.Admit();
            double? previous = null;
            world.Step(3 * world.TicksPerSecond, (_, state) =>
            {
                var b = state[body];
                var e = cylinderDropped ? Energy(b, 1f, r, h) : BoxEnergy(b, 1f, half) - G * BenchY;
                if (previous is { } p) worst = Math.Max(worst, e - p);
                previous = e;
            });
        }
        return worst;
    }

    [Fact]
    public void DropsOntoALyingCylinderGainNoMoreEnergyThanBoxesOntoABox()
    {
        var bound = DropRise(false, false) + SlopEnergy(1f);
        foreach (var (dropped, below) in new[] { (true, true), (true, false), (false, true) })
        {
            var rise = DropRise(dropped, below);
            Assert.True(rise <= bound, $"{(dropped ? "cylinder" : "box")} onto {(below ? "lying cylinder" : "box")}: worst tick gain {rise * 1000:F3} mJ, bound {bound * 1000:F3} mJ");
        }
    }

    /// <summary>The reviewer's C4: B placed δ back from a certified touch t* (certified apart at t*), so its true minimum-translation depth
    /// is at most δ; no kernel point may claim more than that by over 0.5 mm.</summary>
    private static List<string> OverlapOverestimates(bool cylinderPartner, int seed, int poses)
    {
        var rng = new Random(seed);
        var centre = At(0, 1);
        var failures = new List<string>();
        for (var pose = 0; pose < poses; pose++)
        {
            var qa = RandomRotation(rng); var qb = RandomRotation(rng);
            var dir = Vector3.Normalize(new((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1)));
            var a = Collider(ShapeKind.Cylinder, centre, qa, (.3f), (.2f));
            SimCollider B(Vector3 at) => cylinderPartner ? Collider(ShapeKind.Cylinder, at, qb, (.2f), (.35f), index: 1)
                : Collider(ShapeKind.Box, at, qb, 0f, At((.25f), (.1f), (.15f)), 1);
            // Place by a rough projection distance (2000 alternating projections, an upper bound), then advance until the touch is
            // certified apart: any certified t* bounds the depth.
            static double Rough(SimCollider x, SimCollider y)
            {
                D3 px = Project(x, D3.Of(y.Position)), py = Project(y, px);
                for (var i = 0; i < 2000; i++) { px = Project(x, py); py = Project(y, px); }
                return (px - py).Length;
            }
            float lo = 0f, hi = 3f;
            for (var step = 0; step < 24; step++)
            {
                var mid = .5f * (lo + hi);
                if (Rough(a, B(centre + dir * mid)) > 1e-5) hi = mid; else lo = mid;
            }
            // 0.2 mm out, where the dual bounds converge quickly (the reviewer's C4 keeps the same 0.2 mm slack).
            hi += 2e-4f;
            while (CertifiedLower(a, B(centre + dir * hi)) <= 0) hi += 1e-4f;
            var touch = hi;
            foreach (var depth in new[] { .0003f, .002f, .005f })
            {
                var contacts = Contacts(a, B(centre + dir * (touch - depth)));
                if (contacts.Count == 0) { failures.Add($"pose {pose} at {depth * 1000} mm: no contact"); continue; }
                var excess = -contacts.Min(c => c.Gap) - depth;
                if (excess > .0005) failures.Add($"pose {pose} at {depth * 1000} mm: kernel {excess * 1000:F2} mm deeper than the certified bound");
            }
        }
        return failures;
    }

    [Theory]
    [InlineData(false, 7002)]
    [InlineData(true, 7001)]
    public void ARimAimedWalkThroughTouchKeepsTheGapContinuousAndNeverPositiveWhileOverlapping(bool cylinderPartner, int seed)
    {
        // Review N9: B walks along a rim-aimed direction from 1 mm apart (certified touch t*) to 1.6 mm overlapping in 0.05 mm steps. The
        // reported gap may not jump by more than 0.5 mm between steps, nor read positive by more than 0.5 mm while overlapping.
        var rng = new Random(seed);
        var centre = At(0, 1);
        var failures = new List<string>();
        for (var pose = 0; pose < 60; pose++)
        {
            var qa = RandomRotation(rng); var qb = RandomRotation(rng);
            var a = Collider(ShapeKind.Cylinder, centre, qa, (.3f), (.2f));
            var angle = rng.NextDouble() * 2 * Math.PI;
            var rim = (rng.NextDouble() < .5 ? a.Hy : -a.Hy) * a.U1 + a.Radius * ((float)Math.Cos(angle) * a.U0 + (float)Math.Sin(angle) * a.U2);
            var dir = Vector3.Normalize(rim);
            SimCollider B(Vector3 at) => cylinderPartner ? Collider(ShapeKind.Cylinder, at, qb, (.2f), (.35f), index: 1)
                : Collider(ShapeKind.Box, at, qb, 0f, At((.25f), (.1f), (.15f)), 1);
            var touch = CertifiedTouch(a, B, centre, dir);
            float? previous = null;
            for (var k = 20; k >= -32; k--)
            {
                var s = k * .00005f;
                var contacts = Contacts(a, B(centre + dir * (touch + s)));
                if (contacts.Count == 0) { previous = null; continue; }
                var gap = contacts.Min(c => c.Gap);
                if (previous is { } p && MathF.Abs(gap - p) > .0005f)
                    failures.Add($"pose {pose} at {s * 1000:F2} mm: gap jumps {p * 1000:F3} → {gap * 1000:F3} mm");
                if (k < 0 && gap > .0005f) failures.Add($"pose {pose} at {s * 1000:F2} mm overlapping: gap {gap * 1000:F3} mm");
                previous = gap;
            }
        }
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures.Take(20)));
    }

    [Fact]
    public void SideManifoldsKeepTheDeepestPointAndTheExtremeEndsWithinFour()
    {
        // Requirement (a): six points on a line, the deepest in the interior; the reduction keeps four: the deepest, both extreme ends
        // and the next deepest, in emitted order, deterministically.
        var a = Collider(ShapeKind.Cylinder, Vector3.Zero, Quaternion.Identity, .3f, .2f);
        var b = Collider(ShapeKind.Box, At(0, 1), Quaternion.Identity, 0f, At(.1f, .1f, .1f), 1);
        List<ContactPoint> Points() =>
        [
            .. new[] { (0f, .002f), (1f, .001f), (2.5f, -.003f), (3f, .0005f), (4f, .004f), (5f, .003f) }
                .Select((p, i) => WorkshopCollisionKernels.MakeContact(a.Body, b.Body, a, b, Vector3.UnitY, At(p.Item1, 0), p.Item2, i)),
        ];
        var points = Points();
        WorkshopCollisionKernels.ReduceSideManifold(points, 0);
        Assert.Equal([0, 2, 3, 5], points.Select(p => p.Feature));
        var again = Points();
        WorkshopCollisionKernels.ReduceSideManifold(again, 0);
        Assert.Equal(points.Select(p => p.Feature), again.Select(p => p.Feature));

        // Skewed crossed sides emit the closest point and the generator ends within the margin, never more than four, and always the
        // pair's closest point at the exact gap.
        var rng = new Random(8101);
        for (var pose = 0; pose < 40; pose++)
        {
            var skew = (float)(rng.NextDouble() * 10 + 2.5);
            var log = Collider(ShapeKind.Cylinder, Vector3.Zero, AlongZ, .3f, .5f);
            var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, skew * MathF.PI / 180) * AlongZ;
            var drum = Collider(ShapeKind.Cylinder, At(0, .5f + .001f), q, .2f, .15f, index: 1);
            var contacts = Contacts(log, drum);
            Assert.InRange(contacts.Count, 1, 4);
            Assert.True(MathF.Abs(contacts.Min(c => c.Gap) - .001f) < 1e-4f, $"skew {skew}: nearest {contacts.Min(c => c.Gap)}");
        }
    }

    [Fact]
    public void CrossedSkewedSidesGiveEachGeneratorEndItsOwnRadialGapAndNormal()
    {
        // Side emission rule (E2): two equal lying cylinders crossed at 5°, 1 mm apart over their crossing. A facing generator's end at
        // axial h lies √((r + g)² + (h sin φ)²) from the other axis, so each end reads that radial gap and a normal turned
        // atan(h sin φ / (r + g)) from the closest point's; the reduction keeps the closest point and three of the four ends.
        float r = (.2f), h = (.15f), g = .001f, phi = 5f * MathF.PI / 180;
        var lower = Collider(ShapeKind.Cylinder, Vector3.Zero, AlongZ, r, h);
        var upper = Collider(ShapeKind.Cylinder, At(0, 2 * r + g), Quaternion.CreateFromAxisAngle(Vector3.UnitY, phi) * AlongZ, r, h, index: 1);
        var contacts = Contacts(upper, lower);
        Assert.Equal(4, contacts.Count);
        var closest = contacts.MinBy(c => c.Gap)!;
        Assert.InRange(closest.Gap, g - 1e-4f, g + 1e-4f);
        var endGap = Math.Sqrt((r + g) * (r + g) + Math.Pow(h * Math.Sin(phi), 2)) - r;
        var turn = Math.Atan(h * Math.Sin(phi) / (r + g));
        foreach (var end in contacts.Where(c => c != closest))
        {
            Assert.InRange(end.Gap, endGap - 5e-5, endGap + 5e-5);
            var angle = Math.Acos(Math.Min(1, Math.Abs(Vector3.Dot(end.Normal, closest.Normal))));
            Assert.InRange(angle, turn - 1e-3, turn + 1e-3);
        }
    }

    [Fact]
    public void ASkewedBoxRidgeUnderTheSideGivesEachPointItsOwnRadialGapAndNormal()
    {
        // Side emission rule (E4): a box ridge 1 mm under a lying cylinder, skewed 5° from its axis. The ridge point at axial z lies
        // √(d² + (z tan φ)²) from the axis (d = r + 1 mm), so each emitted point reads that radial gap at its own z and a normal turned
        // atan(|z| tan φ / d) from vertical; the closest point is the crossing at 1 mm and the stretch ends reach the cylinder's ends.
        float r = (.2f), h = (.15f), g = .001f, phi = 5f * MathF.PI / 180, e = .1f;
        var d = r + g;
        var log = Collider(ShapeKind.Cylinder, Vector3.Zero, AlongZ, r, h);
        var ridge = Collider(ShapeKind.Box, At(0, -d - e * MathF.Sqrt(2)), Quaternion.CreateFromAxisAngle(Vector3.UnitY, phi) * AboutZ(45), 0f,
            At(e, e, .5f), 1);
        var contacts = Contacts(log, ridge);
        Assert.Equal(3, contacts.Count);
        Assert.InRange(contacts.Min(c => c.Gap), g - 1e-4f, g + 1e-4f);
        Assert.True(contacts.Max(c => Math.Abs(c.Point.Z)) > .9 * h, "the stretch ends reach the cylinder's ends");
        foreach (var c in contacts)
        {
            var lateral = Math.Abs(c.Point.Z) * Math.Tan(phi);
            var gap = Math.Sqrt(d * d + lateral * lateral) - r;
            Assert.InRange(c.Gap, gap - 5e-5, gap + 5e-5);
            var turn = Math.Atan(lateral / d);
            Assert.InRange(Math.Acos(Math.Min(1, Math.Abs(c.Normal.Y))), turn - 1e-3, turn + 1e-3);
        }
    }

    [Fact]
    public void ABoxVertexInsideTheRimNeverReadsAPositiveGap()
    {
        // A box vertex pushed δ along the rim's 45° diagonal into an upright cylinder overlaps by δ/√2, yet the 1 mm shrunk core reads
        // √2·s − δ − s > 0 from it (its rim is rounded). The exact shapes overlap, so no contact may report them apart, and none deeper
        // than the true overlap.
        float r = (.2f), h = (.15f), e = .05f;
        var cylinder = Collider(ShapeKind.Cylinder, Vector3.Zero, Quaternion.Identity, r, h);
        var outward = Vector3.Normalize(At(1, 1));
        var corner = Vector3.Normalize(new Vector3(-1, -1, -1));
        var turn = Quaternion.CreateFromAxisAngle(Vector3.Normalize(Vector3.Cross(corner, -outward)), MathF.Acos(Vector3.Dot(corner, -outward)));
        foreach (var depth in new[] { .00005f, .0001f, .0002f, .0003f })
        {
            var vertex = At(r, h) - depth * outward;
            var box = Collider(ShapeKind.Box, vertex + e * MathF.Sqrt(3) * outward, turn, 0f, At(e, e, e), 1);
            var contacts = Contacts(cylinder, box);
            Assert.NotEmpty(contacts);
            Assert.InRange(contacts.Min(c => c.Gap), -depth / MathF.Sqrt(2) - 5e-5f, 0f);
        }
    }

    [Fact]
    public void ManifoldPointsNameDistinctCylinderFeatures()
    {
        // Warm starts match points by feature id: within one manifold every point names a different feature, and the cap points of a
        // standing cylinder name another feature kind than the side points of a lying one (review F3; mutation n11).
        float r = (.2f), h = (.15f);
        var plane = Collider(ShapeKind.Plane, Vector3.Zero, Quaternion.Identity, index: 1);
        var standing = Contacts(Collider(ShapeKind.Cylinder, At(0, h + .001f), Quaternion.Identity, r, h), plane);
        var lying = Contacts(Collider(ShapeKind.Cylinder, At(0, r + .001f), AlongZ, r, h), plane);
        var crossed = Contacts(
            Collider(ShapeKind.Cylinder, At(0, 2 * r + .001f), Quaternion.CreateFromAxisAngle(Vector3.UnitY, 5f * MathF.PI / 180) * AlongZ, r, h, index: 1),
            Collider(ShapeKind.Cylinder, Vector3.Zero, AlongZ, r, h));
        Assert.Equal(4, standing.Count);
        Assert.Equal(2, lying.Count);
        Assert.Equal(4, crossed.Count);
        foreach (var manifold in new[] { standing, lying, crossed })
            Assert.Equal(manifold.Count, manifold.Select(c => c.Feature).Distinct().Count());
        Assert.Empty(standing.Select(c => c.Feature >> 12).Intersect(lying.Select(c => c.Feature >> 12)));
    }

    [Fact]
    public void ANearlyUprightCapReadsItsTrueGapAtEveryTinyTilt()
    {
        // Requirement (c): an f32 cap tilted by micro-radians once read a rim direction with a 12% axial part (a −24.8 mm bench gap).
        // The engine's integrated orientation is not exactly unit: an upside-down body (|x|,|z| ≈ 1) turns a norm error ε into an
        // axis of length ≈ 1 − 2ε, whose axial residue dwarfs a micro-radian radial part. Upright and flipped, at unit and integrated
        // norms, placed analytically with its lowest rim point on the plane, every cap point's gap lies in [0, 2 r sin(tilt)] + slack.
        float r = (.2f), h = (.15f);
        var plane = Collider(ShapeKind.Plane, Vector3.Zero, Quaternion.Identity, index: 1);
        foreach (var flip in new[] { Quaternion.Identity, AboutX(180) })
        foreach (var norm in new[] { 1f, 1f - 6e-8f, 1f - 1.2e-7f, 1f + 1.2e-7f })
        foreach (var tilt in new[] { 0.0, 1e-7, 1e-6, 2e-6, 3e-6, 1e-5, 3e-5, 1e-4, 1e-3 })
            for (var azimuth = 0; azimuth < 64; azimuth++)
            {
                var tilted = Quaternion.CreateFromAxisAngle(new Vector3(MathF.Cos(azimuth * .1f), 0, MathF.Sin(azimuth * .1f)), (float)tilt);
                var q = Quaternion.Concatenate(flip, tilted);
                q = new Quaternion(q.X * norm, q.Y * norm, q.Z * norm, q.W * norm);
                var centre = At(.123f, (float)(h * Math.Cos(tilt) + r * Math.Sin(tilt)), -.37f);
                var contacts = Contacts(Collider(ShapeKind.Cylinder, centre, q, r, h), plane);
                Assert.Equal(4, contacts.Count);
                var top = 2 * r * Math.Sin(tilt) + 3e-5;
                Assert.All(contacts, c => Assert.InRange(c.Gap, -3e-5, top));
            }
    }

    // ---- fresh-seed energy gate (coordinator direction, 23 Oct 2026) ----
    // Fixed seeds chosen once and never tuned against; the reviewer keeps a separate held-back seed. The bound is exactly the box
    // control's worst per-tick rise plus m·g·ContactSlop (≈ 1.96 mJ at 1 kg), per tick, unrelaxed.

    public enum GateClass { CylinderOntoPlane, CylinderOntoBox, BoxOntoCylinder, CylinderOntoLyingCylinder, CylinderOntoStandingCylinder, SkewedRollOffEdge }

    private const int GateSeed = 91000;

    /// <summary>The gate's material sets. Rolling: the dropped body's rolling resistance 0.05 (restitution 0), so the curved-side
    /// rolling row with per-point normals is active. Lively: also restitution 0.3 against the bench material (product 0.3).</summary>
    public enum GateMaterials { Dead, Rolling, Lively }

    /// <summary>One random case of a class: random orientation, spin (±3 rad/s per axis), drop height (0.1–0.6 m) and lateral offset;
    /// the roll-off case rolls a lying cylinder at 0.3–2 m/s off a box edge skewed 0–20°. Returns the worst per-tick energy rise.</summary>
    private static double GateCase(GateClass kind, Random rng, GateMaterials materials, bool boxControl = false)
    {
        var mover = materials switch
        {
            GateMaterials.Dead => Dead, GateMaterials.Rolling => new LawMaterial(0f, .1f, .5f, .05f), GateMaterials.Lively => Lively,
            _ => throw new ArgumentOutOfRangeException(nameof(materials), materials, "Unknown gate materials."),
        };
        var partner = materials == GateMaterials.Lively ? LawMaterial.Bench : Dead;
        var q = RandomRotation(rng);
        var spin = new Vector3((float)(rng.NextDouble() * 6 - 3), (float)(rng.NextDouble() * 6 - 3), (float)(rng.NextDouble() * 6 - 3));
        var height = (float)(rng.NextDouble() * .5 + .1);
        var offset = new Vector3((float)(rng.NextDouble() * .5 - .25), 0, (float)(rng.NextDouble() * .4 - .2));
        var skew = (float)(rng.NextDouble() * 20);
        var speed = (float)(rng.NextDouble() * 1.7 + .3);
        float r = (.2f), h = (.15f);
        var half = At((.25f), (.1f), (.15f));
        var scene = new LawScene();
        GpuBodyId body;
        var dynamicBox = boxControl || kind == GateClass.BoxOntoCylinder;
        var top = BenchY + .6f;
        switch (kind)
        {
            case GateClass.CylinderOntoPlane: top = BenchY; break;
            case GateClass.CylinderOntoBox: scene.StaticBox(At(0, BenchY + .3f), At(.3f, .3f, .5f), null, partner); break;
            case GateClass.BoxOntoCylinder: case GateClass.CylinderOntoLyingCylinder: scene.StaticCylinder(At(0, BenchY + .3f), .3f, .5f, AlongZ, partner); break;
            case GateClass.CylinderOntoStandingCylinder: scene.StaticCylinder(At(0, BenchY + .3f), .3f, .3f, null, partner); break;
            case GateClass.SkewedRollOffEdge: scene.StaticBox(At(0, BenchY + (.3f)), At(.5f, .3f, .6f), null, partner); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown gate class.");
        }
        if (boxControl && kind != GateClass.CylinderOntoPlane && kind != GateClass.SkewedRollOffEdge)
        {
            // The control replaces every static partner by the same box (box onto box), and the dropped body by the box.
            scene = new LawScene();
            scene.StaticBox(At(0, BenchY + .3f), At(.3f, .3f, .5f), null, partner);
        }
        if (kind == GateClass.SkewedRollOffEdge && !boxControl)
        {
            var rotation = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(Vector3.UnitY, skew * MathF.PI / 180) * AlongZ);
            body = scene.Cylinder(At(.1f, BenchY + 2 * (.3f) + r, offset.Z), .2f, .15f, 1f, mover, rotation, velocity: At(speed, 0, 0),
                angular: At(0, 0, -speed / r));
        }
        else
        {
            var at = At(offset.X, top + .35f + height, offset.Z);
            body = dynamicBox ? scene.Box(at, At(.25f, .1f, .15f), 1f, mover, q, angular: spin)
                : scene.Cylinder(at, .2f, .15f, 1f, mover, q, angular: spin);
        }
        var world = scene.Admit();
        double worst = 0;
        double? previous = null;
        world.Step(3 * world.TicksPerSecond, (_, state) =>
        {
            var b = state[body];
            var e = dynamicBox ? BoxEnergy(b, 1f, half) - G * BenchY : Energy(b, 1f, r, h);
            if (previous is { } p) worst = Math.Max(worst, e - p);
            previous = e;
        });
        return worst;
    }

    private static readonly LawMaterial Lively = new(.3f, .1f, .5f, .05f);

    /// <summary>Runs <paramref name="cases"/> per class on fixed seeds for each material set, and returns each class's failure count
    /// against that material's box control + m·g·ContactSlop.</summary>
    private static List<(string Class, double Bound, int Failed, double Worst)> Gate(int cases, params GateMaterials[] sets)
    {
        var results = new List<(string, double, int, double)>();
        foreach (var materials in sets)
        {
            var offset = 100 * (int)materials;
            var controlRng = new Random(GateSeed + offset);
            var control = 0.0;
            for (var i = 0; i < cases; i++)
                control = Math.Max(control, GateCase(GateClass.CylinderOntoBox, controlRng, materials, boxControl: true));
            var bound = control + SlopEnergy(1f);
            foreach (var kind in Enum.GetValues<GateClass>())
            {
                var rng = new Random(GateSeed + offset + 1 + (int)kind);
                int failed = 0; double worst = 0;
                for (var i = 0; i < cases; i++)
                {
                    var rise = GateCase(kind, rng, materials);
                    worst = Math.Max(worst, rise);
                    if (rise > bound) failed++;
                }
                results.Add(($"{kind} ({materials})", bound, failed, worst));
            }
        }
        return results;
    }

    private static void AssertGate(int cases, params GateMaterials[] sets)
    {
        var results = Gate(cases, sets);
        var report = string.Join(Environment.NewLine, results.Select(c =>
            $"{c.Class}: {c.Failed}/{cases} over {c.Bound * 1000:F3} mJ (worst {c.Worst * 1000:F3} mJ)"));
        TestContext.Current.TestOutputHelper?.WriteLine(report);
        Assert.True(results.All(c => c.Failed == 0), report);
    }

    [Fact]
    public void FreshSeedDropsGainNoMoreEnergyThanTheBoxControlAndTheSlop() => AssertGate(200, GateMaterials.Dead, GateMaterials.Rolling);

    /// <summary>The lively set (restitution 0.3), restored from Story 10.1 (evidence-10-1/lively-gate-source.txt) as the restitution
    /// slice's entry check: red before that slice (a deferred bounce returned on a stale approach gains energy), green after.</summary>
    [Fact]
    public void FreshSeedDropsWithRestitutionGainNoMoreEnergyThanTheBoxControlAndTheSlop() => AssertGate(200, GateMaterials.Lively);

    [Theory]
    [InlineData(false, 402)]
    [InlineData(true, 403)]
    public void OverlapDepthNeverExceedsTheCertifiedBound(bool cylinderPartner, int seed)
    {
        var failures = OverlapOverestimates(cylinderPartner, seed, 200);
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }
}
