using System.Buffers.Binary;
using System.Numerics;
using System.Text.RegularExpressions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>Ball kinds the law tests declare; each maps to its catalogue identity and its package's declared material.</summary>
public enum LawBall { Basketball, Bowling }

/// <summary>Each kind's declared material as the compiler compiles it: the package's f32 declaration narrowed to the binary16 body,
/// collider and material lanes (WorkshopPhysicsCompiler), so a law test feeds the engine exactly the compiled values.</summary>
internal static class LawBalls
{
    public static PartId Part(LawBall ball) => ball switch
    {
        LawBall.Basketball => PartId.Basketball,
        LawBall.Bowling => PartId.BowlingBall,
        _ => throw new ArgumentOutOfRangeException(nameof(ball), ball, "Unknown law ball."),
    };
    private static CuriousContraptions.Abstractions.ColliderDescriptor Sphere(LawBall ball) => Assert.Single(DeclaredPhysics.Of(Part(ball)).Colliders);
    private static CuriousContraptions.Abstractions.Mechanical.RigidBodyMassProperties Body(LawBall ball) =>
        DeclaredPhysics.Of(Part(ball)).Mechanical!.MassProperties;
    public static float Radius(LawBall ball) => (float)(Half)Sphere(ball).HalfExtents.X;
    public static float Mass(LawBall ball) => (float)(Half)Body(ball).Mass;
    public static float Drag(LawBall ball) => (float)(Half)Body(ball).LinearDrag;
    public static float Rolling(LawBall ball) => (float)(Half)Sphere(ball).Material.RollingResistance;
    public static float Restitution(LawBall ball) => (float)(Half)Sphere(ball).Material.Restitution;
    public static float Friction(LawBall ball) => (float)(Half)Sphere(ball).Material.Friction;
    public static float BounceThreshold(LawBall ball) => (float)(Half)Sphere(ball).Material.BounceThreshold;
}

/// <summary>Declared contact material of one law-test body (the same lanes ContactMaterialDeclaration admits).</summary>
internal readonly record struct LawMaterial(float Restitution, float Threshold, float Friction, float Rolling)
{
    /// <summary>The compiler's workbench plane material (WorkshopPhysicsCompiler).</summary>
    public static LawMaterial Bench => new(1f, .1f, .3f, 0f);
    public static LawMaterial Domino => new((float)DominoMaterial.Default.Bounce.Value, (float)DominoMaterial.BounceThreshold.Value,
        (float)DominoMaterial.Default.Friction.Value, 0f);
    public static LawMaterial Ball(LawBall ball) =>
        new(LawBalls.Restitution(ball), LawBalls.BounceThreshold(ball), LawBalls.Friction(ball), LawBalls.Rolling(ball));
}

/// <summary>
/// A typed physics scene for the shipped C# engine (WorkshopPhysicsEngine): bodies, colliders, materials, orientation
/// sensors, contact work and prismatic joints are PhysicsSceneDeclaration records admitted by PhysicsGpuAbi.Admission, the
/// same path the compiler's scenes take. It replaces the deleted JS-kernel harness's hand-written admission bytes
/// (tools/workshop-rigid-body.test.mjs, Story 9.4b).
/// </summary>
internal sealed class LawScene
{
    public static readonly float BenchY = (float)WorkshopConstruction.WorkbenchSurface.Value;
    public static readonly float Gravity = -(float)WorkshopConstruction.Gravity.Value;
    private readonly List<RigidBodyDeclaration> _bodies = [];
    private readonly List<ColliderDeclaration> _colliders = [];
    private readonly List<ContactMaterialDeclaration> _materials = [];
    private readonly List<OrientationSensorDeclaration> _orientationSensors = [];
    private readonly List<ContactWorkDeclaration> _contactWorks = [];
    private readonly List<PrismaticConstraintDeclaration> _prismatics = [];
    private readonly Dictionary<GpuBodyId, GpuColliderId> _colliderOf = [];
    private ulong _next = 1;

    public LawScene(bool bench = true)
    {
        if (!bench) return;
        var height = WorkshopInput.Position(BenchY);
        var plane = new GpuBodyId(_next++);
        _bodies.Add(new(plane, RigidMotionKind.Static, new(0, height.Cell, 0), new(0f, height.Local, 0f), CanonicalRotation.Identity,
            default, default, new((Half)0), default, new((Half)0)));
        var material = Material(LawMaterial.Bench);
        _colliders.Add(new(new(_next++), plane, material, ColliderShapeKind.Plane, RigidLocalPose.Identity, new((Half)0), default));
    }

    private GpuMaterialId Material(LawMaterial value)
    {
        var id = new GpuMaterialId(_next++);
        _materials.Add(new(id, new((Half)value.Restitution), new((Half)value.Threshold), new((Half)value.Friction), new((Half)value.Rolling)));
        return id;
    }

    private GpuBodyId Body(RigidMotionKind motion, ColliderShapeKind shape, Vector3 position, Quaternion rotation, float radius,
        Vector3 half, float mass, LawMaterial material, Vector3 velocity, Vector3 angular, float drag, float gravity, Vector3 colliderOffset)
    {
        var id = new GpuBodyId(_next++);
        var x = WorkshopInput.Position(position.X); var y = WorkshopInput.Position(position.Y); var z = WorkshopInput.Position(position.Z);
        var dynamic = motion == RigidMotionKind.Dynamic;
        _bodies.Add(new(id, motion, new(x.Cell, y.Cell, z.Cell), new(x.Local, y.Local, z.Local),
            CanonicalRotation.Normalize(rotation.X, rotation.Y, rotation.Z, rotation.W),
            dynamic ? new(velocity.X, velocity.Y, velocity.Z) : default, dynamic ? new(angular.X, angular.Y, angular.Z) : default,
            new((Half)(dynamic ? mass : 0f)), dynamic ? new((Half)0, (Half)gravity, (Half)0) : default, new((Half)(dynamic ? drag : 0f))));
        var materialId = Material(material);
        var collider = new GpuColliderId(_next++);
        _colliderOf[id] = collider;
        _colliders.Add(new(collider, id, materialId, shape,
            new(new((Half)colliderOffset.X, (Half)colliderOffset.Y, (Half)colliderOffset.Z), CanonicalRotation.Identity),
            new((Half)(shape == ColliderShapeKind.Sphere ? radius : 0f)),
            shape == ColliderShapeKind.Box ? new((Half)half.X, (Half)half.Y, (Half)half.Z) : default));
        return id;
    }

    public GpuColliderId ColliderOf(GpuBodyId body) => _colliderOf[body];

    public GpuBodyId Sphere(Vector3 position, float radius, float mass, LawMaterial material, Vector3 velocity, Vector3 angular,
        float drag, float gravity, Vector3 colliderOffset = default, Quaternion? rotation = null) =>
        Body(RigidMotionKind.Dynamic, ColliderShapeKind.Sphere, position, rotation ?? Quaternion.Identity, radius, default, mass, material,
            velocity, angular, drag, gravity, colliderOffset);

    /// <summary>A declared ball of <paramref name="kind"/> with its own radius, mass, drag and material.</summary>
    public GpuBodyId Ball(LawBall kind, Vector3 position, Vector3 velocity = default, Vector3 angular = default, bool resistanceFree = false,
        float? gravity = null, float? mass = null) =>
        Sphere(position, LawBalls.Radius(kind), mass ?? LawBalls.Mass(kind),
            resistanceFree ? LawMaterial.Ball(kind) with { Rolling = 0f } : LawMaterial.Ball(kind), velocity, angular,
            resistanceFree ? 0f : LawBalls.Drag(kind), gravity ?? Gravity);

    public GpuBodyId Box(Vector3 position, Vector3 half, float mass, LawMaterial material, Quaternion? rotation = null,
        Vector3 velocity = default, Vector3 angular = default, float? gravity = null) =>
        Body(RigidMotionKind.Dynamic, ColliderShapeKind.Box, position, rotation ?? Quaternion.Identity, 0f, half, mass, material,
            velocity, angular, 0f, gravity ?? Gravity, default);

    /// <summary>The compiler's stock Domino tile: its declared extents, mass and material.</summary>
    public GpuBodyId Domino(Vector3 position, Quaternion? rotation = null, Vector3 angular = default, float? gravity = null)
    {
        var tile = DominoMaterial.Default;
        return Box(position, new((float)tile.HalfExtents.X, (float)tile.HalfExtents.Y, (float)tile.HalfExtents.Z), (float)tile.Mass.Value,
            LawMaterial.Domino, rotation, angular: angular, gravity: gravity);
    }

    public GpuBodyId StaticBox(Vector3 position, Vector3 half, Quaternion? rotation = null, LawMaterial? material = null,
        Vector3 colliderOffset = default) =>
        Body(RigidMotionKind.Static, ColliderShapeKind.Box, position, rotation ?? Quaternion.Identity, 0f, half, 0f,
            material ?? LawMaterial.Bench, default, default, 0f, 0f, colliderOffset);

    public GpuBodyId StaticSphere(Vector3 position, float radius, Quaternion rotation, LawMaterial material) =>
        Body(RigidMotionKind.Static, ColliderShapeKind.Sphere, position, rotation, radius, default, 0f, material, default, default, 0f, 0f,
            default);

    public void OrientationSensor(GpuBodyId body, Quaternion initial, double degrees) =>
        _orientationSensors.Add(new(new(_next++), body, CanonicalRotation.Normalize(initial.X, initial.Y, initial.Z, initial.W),
            OrientationThreshold.Degrees(degrees)));

    public GpuContactWorkId ContactWork(GpuBodyId owner, BodyTargetSet targets, float targetSpeed, float preload, float threshold,
        uint cooldown)
    {
        var id = new GpuContactWorkId(_next++);
        _contactWorks.Add(new(id, owner, targets, new(targetSpeed), new(preload), new(threshold), cooldown));
        return id;
    }

    public PrismaticConstraintId Prismatic(GpuBodyId a, GpuBodyId b, ConstraintFrame frameA, ConstraintFrame frameB, float lower,
        float upper, float stiffness, float damping, ConnectedCollision collision)
    {
        var id = new PrismaticConstraintId(_next++);
        _prismatics.Add(new(id, a, b, frameA, frameB, lower, upper, stiffness, damping, collision));
        return id;
    }

    /// <summary>Declares a supplied drive on <paramref name="joint"/> (C-Gate).</summary>
    public void Drive(PrismaticConstraintId joint, float target, float maxSpeed, float stoppingAcceleration, float maxEffort, float maxPower) =>
        _drives.Add(new(joint, target, maxSpeed, stoppingAcceleration, maxEffort, maxPower));
    private readonly List<PrismaticDriveDeclaration> _drives = [];

    public LawWorld Admit(SimulationCadence cadence = SimulationCadence.Hz120)
    {
        var scene = new PhysicsSceneDeclaration(new(1, 2), _next, [.. _bodies], [.. _colliders], [.. _materials], [], [],
            contactWorks: [.. _contactWorks], orientationSensors: [.. _orientationSensors], prismatics: [.. _prismatics],
            prismaticDrives: [.. _drives]);
        return new(scene, PhysicsGpuAbi.Admission(scene, new(1), new(cadence, PhysicalStepProfile.Canonical480Hz, new(1))), cadence);
    }
}

/// <summary>One committed body state read from the engine's world bytes at the named ABI offsets.</summary>
internal readonly record struct LawBody(Vector3 Position, Quaternion Rotation, Vector3 Velocity, Vector3 Angular, CellOrigin Cell,
    LocalPosition Local)
{
    /// <summary>Tilt of the body's local +Y axis from world up, in degrees.</summary>
    public float Tilt => MathF.Acos(Math.Clamp(1 - 2 * (Rotation.X * Rotation.X + Rotation.Z * Rotation.Z), -1f, 1f)) * 180 / MathF.PI;
    /// <summary>Double-precision lengths of the committed f32 lanes, as the harness measured them.</summary>
    public double Speed => Math.Sqrt((double)Velocity.X * Velocity.X + (double)Velocity.Y * Velocity.Y + (double)Velocity.Z * Velocity.Z);
    public double Spin => Math.Sqrt((double)Angular.X * Angular.X + (double)Angular.Y * Angular.Y + (double)Angular.Z * Angular.Z);
    public bool Finite => float.IsFinite(Position.X + Position.Y + Position.Z + Rotation.X + Rotation.Y + Rotation.Z + Rotation.W +
        Velocity.X + Velocity.Y + Velocity.Z + Angular.X + Angular.Y + Angular.Z);
}

internal sealed class LawState(PhysicsSceneDeclaration scene, byte[] world)
{
    public byte[] World => world;
    public LawBody this[GpuBodyId id] => Read(scene, world, id);

    public static LawBody Read(PhysicsSceneDeclaration scene, byte[] world, GpuBodyId id)
    {
        var o = scene.Layout.GetBodyOffset(id);
        int I(int at) => BinaryPrimitives.ReadInt32LittleEndian(world.AsSpan(o + at));
        float F(int at) => BinaryPrimitives.ReadSingleLittleEndian(world.AsSpan(o + at));
        var cell = new CellOrigin(I(PhysicsGpuAbi.BodyCellOffset), I(PhysicsGpuAbi.BodyCellOffset + 4), I(PhysicsGpuAbi.BodyCellOffset + 8));
        var local = new LocalPosition(F(PhysicsGpuAbi.BodyLocalOffset), F(PhysicsGpuAbi.BodyLocalOffset + 4), F(PhysicsGpuAbi.BodyLocalOffset + 8));
        return new(new((cell.X + local.X) / 16f, (cell.Y + local.Y) / 16f, (cell.Z + local.Z) / 16f),
            new(F(PhysicsGpuAbi.BodyRotationOffset), F(PhysicsGpuAbi.BodyRotationOffset + 4), F(PhysicsGpuAbi.BodyRotationOffset + 8),
                F(PhysicsGpuAbi.BodyRotationOffset + 12)),
            new(F(PhysicsGpuAbi.BodyVelocityOffset), F(PhysicsGpuAbi.BodyVelocityOffset + 4), F(PhysicsGpuAbi.BodyVelocityOffset + 8)),
            new(F(PhysicsGpuAbi.BodyAngularVelocityOffset), F(PhysicsGpuAbi.BodyAngularVelocityOffset + 4),
                F(PhysicsGpuAbi.BodyAngularVelocityOffset + 8)),
            cell, local);
    }
}

internal sealed class LawWorld(PhysicsSceneDeclaration scene, byte[] admitted, SimulationCadence cadence)
{
    public PhysicsSceneDeclaration Scene => scene;
    public byte[] Admitted => admitted;
    public int TicksPerSecond => cadence switch
    {
        SimulationCadence.Hz60 => 60, SimulationCadence.Hz120 => 120, SimulationCadence.Hz240 => 240,
        _ => throw new ArgumentOutOfRangeException(nameof(cadence), cadence, "Unknown cadence."),
    };
    public LawBody Initial(GpuBodyId id) => LawState.Read(scene, admitted, id);

    private int Body(GpuBodyId id) => scene.Layout.GetBodyOffset(id);

    /// <summary>Overrides a body's admitted principal moments (mantissa in [0.5, 1) times 2^exponent; zero stays zero).</summary>
    public void SetPrincipalMoments(GpuBodyId id, Vector3 moments)
    {
        for (var axis = 0; axis < 3; axis++)
        {
            var value = axis == 0 ? moments.X : axis == 1 ? moments.Y : moments.Z;
            var slot = admitted.AsSpan(Body(id) + PhysicsGpuAbi.BodyPrincipalInertiaOffset + axis * PhysicsGpuAbi.PrincipalInertiaBytes);
            var exponent = value > 0 ? MathF.ILogB(value) + 1 : 0;
            BinaryPrimitives.WriteHalfLittleEndian(slot, (Half)(value > 0 ? value / MathF.Pow(2, exponent) : 0f));
            BinaryPrimitives.WriteInt32LittleEndian(slot[PhysicsGpuAbi.PrincipalExponentOffset..], exponent);
        }
    }

    /// <summary>Sets an admitted velocity beyond the declaration bound (the envelope facts start outside it).</summary>
    public void SetVelocity(GpuBodyId id, Vector3 velocity, Vector3 angular)
    {
        var o = Body(id);
        for (var axis = 0; axis < 3; axis++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(admitted.AsSpan(o + PhysicsGpuAbi.BodyVelocityOffset + axis * 4),
                axis == 0 ? velocity.X : axis == 1 ? velocity.Y : velocity.Z);
            BinaryPrimitives.WriteSingleLittleEndian(admitted.AsSpan(o + PhysicsGpuAbi.BodyAngularVelocityOffset + axis * 4),
                axis == 0 ? angular.X : axis == 1 ? angular.Y : angular.Z);
        }
    }

    public void SetLocal(GpuBodyId id, CellOrigin cell, LocalPosition local)
    {
        var o = Body(id);
        BinaryPrimitives.WriteInt32LittleEndian(admitted.AsSpan(o + PhysicsGpuAbi.BodyCellOffset), cell.X);
        BinaryPrimitives.WriteInt32LittleEndian(admitted.AsSpan(o + PhysicsGpuAbi.BodyCellOffset + 4), cell.Y);
        BinaryPrimitives.WriteInt32LittleEndian(admitted.AsSpan(o + PhysicsGpuAbi.BodyCellOffset + 8), cell.Z);
        BinaryPrimitives.WriteSingleLittleEndian(admitted.AsSpan(o + PhysicsGpuAbi.BodyLocalOffset), local.X);
        BinaryPrimitives.WriteSingleLittleEndian(admitted.AsSpan(o + PhysicsGpuAbi.BodyLocalOffset + 4), local.Y);
        BinaryPrimitives.WriteSingleLittleEndian(admitted.AsSpan(o + PhysicsGpuAbi.BodyLocalOffset + 8), local.Z);
    }

    /// <summary>Steps the engine from the admitted world. Every committed cell remainder of every dynamic body is checked to be
    /// canonical, [-0.5, 0.5), on every tick (the host rejects a remainder that rounds to 0.5).</summary>
    public LawState Step(int ticks, Action<int, LawState>? observe = null, bool supplied = false, WorkshopPhysicsEngine? engine = null)
    {
        engine ??= new WorkshopPhysicsEngine();
        engine.Reset();
        var source = (byte[])admitted.Clone();
        var candidate = new byte[source.Length];
        // Every prismatic's route is closed when supplied (C-Gate), as the compiled supply graph would close it.
        var inputs = new PhysicsTickInputs([], Enumerable.Repeat(supplied, scene.Prismatics.Length).ToArray());
        for (var tick = 1; tick <= ticks; tick++)
        {
            engine.Advance(source, candidate, inputs);
            (source, candidate) = (candidate, source);
            foreach (var body in scene.Bodies)
            {
                if (body.Motion != RigidMotionKind.Dynamic) continue;
                var local = LawState.Read(scene, source, body.Id).Local;
                Assert.True(local.X is >= -.5f and < .5f && local.Y is >= -.5f and < .5f && local.Z is >= -.5f and < .5f,
                    $"tick {tick}: body {body.Id.Value} remainder {local} is not canonical");
            }
            observe?.Invoke(tick, new(scene, source));
        }
        return new(scene, (byte[])source.Clone());
    }
}

/// <summary>
/// Story 9.4b: the rigid-body, drag, rolling-resistance and f32-velocity facts of the deleted JS-kernel harness
/// (tools/workshop-rigid-body.test.mjs), ported to step the shipped C# engine natively with the harness's own bounds.
/// </summary>
public sealed class WorkshopPhysicsLawTests
{
    private static readonly float BenchY = LawScene.BenchY;
    private static Vector3 At(float x, float y, float z = 0f) => new(x, y, z);
    private static Quaternion AboutZ(double degrees) => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)(degrees * Math.PI / 180));
    private static Quaternion AboutX(double degrees) => Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)(degrees * Math.PI / 180));
    private static float DominoHalfY => (float)DominoMaterial.Default.HalfExtents.Y;
    private static float DominoHalfX => (float)DominoMaterial.Default.HalfExtents.X;

    // ---- rigid body ----

    [Fact]
    public void AnUprightBoxSetAboveTheBenchSettlesOnItsFaceWithinTheRestTolerances()
    {
        var scene = new LawScene();
        var tile = scene.Domino(At(0, BenchY + DominoHalfY + .01f));
        var world = scene.Admit();
        var final = world.Step(3 * world.TicksPerSecond)[tile];
        Assert.True(final.Tilt < 2, $"tilt {final.Tilt}");
        Assert.True(MathF.Sqrt(final.Position.X * final.Position.X + final.Position.Z * final.Position.Z) < .005f, $"drift {final.Position}");
        Assert.True(final.Speed < .05f && final.Spin < .05f, $"speed {final.Speed} spin {final.Spin}");
        Assert.InRange(final.Position.Y, BenchY + DominoHalfY - .0005f, BenchY + DominoHalfY + .0005f);
    }

    [Fact]
    public void ABoxTippedPastItsSupportTopplesThroughSixtyDegreesAndSettlesFlatWithoutJitter()
    {
        var scene = new LawScene();
        var tile = scene.Domino(At(0, BenchY + DominoHalfY + .0005f), angular: At(0, 0, -4));
        var world = scene.Admit();
        var passed = -1; var maximumChange = 0f; Quaternion? previous = null;
        var final = world.Step(4 * world.TicksPerSecond, (tick, state) =>
        {
            var body = state[tile];
            if (passed < 0 && body.Tilt > 60) passed = tick;
            if (tick > 3 * world.TicksPerSecond && previous is { } last)
                maximumChange = MathF.Max(maximumChange, (body.Rotation - last).Length());
            previous = body.Rotation;
        })[tile];
        Assert.InRange(passed, 1, 2 * world.TicksPerSecond);
        Assert.InRange(final.Tilt, 87f, 93f);
        Assert.True(final.Speed < .05f && final.Spin < .1f, $"rest speed {final.Speed} spin {final.Spin}");
        Assert.InRange(final.Position.Y, BenchY + DominoHalfX - .002f, BenchY + DominoHalfX + .002f);
        Assert.True(maximumChange < .002f, $"frame-to-frame quaternion change {maximumChange} over the final second");
    }

    [Fact]
    public void FrictionTurnsASlidingSphereIntoARollingOneThroughTheDeclaredInertia()
    {
        var scene = new LawScene();
        var radius = LawBalls.Radius(LawBall.Basketball);
        var ball = scene.Ball(LawBall.Basketball, At(0, BenchY + radius), velocity: At(3, 0, 0), resistanceFree: true);
        var world = scene.Admit();
        var final = world.Step(world.TicksPerSecond)[ball];
        Assert.InRange(final.Velocity.X, 1.5f, 3f);
        Assert.True(MathF.Abs(final.Angular.Z + final.Velocity.X / radius) < .3f, $"omega {final.Angular.Z} vs {-final.Velocity.X / radius}");
    }

    [Fact]
    public void ABoxOnAStaticBoxFaceHoldsThroughTheBoxBoxManifoldWithoutSinkingOrLaunching()
    {
        var top = BenchY + 2;
        var scene = new LawScene();
        scene.StaticBox(At(0, BenchY + 1), At(1.5f, 1, .125f));
        var tile = scene.Domino(At(0, top + DominoHalfY + .005f));
        var world = scene.Admit();
        var maximumY = float.MinValue;
        var final = world.Step(2 * world.TicksPerSecond, (_, state) => maximumY = MathF.Max(maximumY, state[tile].Position.Y))[tile];
        Assert.True(final.Tilt < 2, $"tilt {final.Tilt}");
        Assert.InRange(final.Position.Y, top + DominoHalfY - .001f, top + DominoHalfY + .001f);
        Assert.True(maximumY < top + DominoHalfY + .02f, $"never launched (max {maximumY})");
        Assert.True(final.Speed < .05f);
        // Control: without the static box the same tile falls to the bench.
        var control = new LawScene();
        var falling = control.Domino(At(0, top + DominoHalfY + .005f));
        var controlWorld = control.Admit();
        Assert.True(controlWorld.Step(2 * controlWorld.TicksPerSecond)[falling].Position.Y < top - 1);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.7f)]
    public void ABoxAcrossATiltedBeamEdgeBalancesOnTheGenuineEdgeEdgeCrossing(float along)
    {
        // The dynamic box's bottom edge (along Z) crosses the top edge (along X) of a static beam tilted 45 deg about X; off-centre at
        // 0.7 m an endpoint-midpoint support point would sit 0.7 m from the real support.
        const float hs = .2f, hb = .1f;
        var topEdge = BenchY + 1 + hs * MathF.Sqrt(2); var rest = topEdge + hb * MathF.Sqrt(2);
        var scene = new LawScene();
        scene.StaticBox(At(0, BenchY + 1), At(1.5f, hs, hs), AboutX(45));
        var upper = scene.Box(At(along, rest + .002f), At(hb, hb, .8f), .4f, LawMaterial.Domino, AboutZ(45));
        var world = scene.Admit();
        var maximumSpin = 0.0;
        var final = world.Step(world.TicksPerSecond / 2, (_, state) => maximumSpin = Math.Max(maximumSpin, state[upper].Spin))[upper];
        Assert.True(maximumSpin < .05f, $"the balanced box never spins up (max {maximumSpin})");
        Assert.True(MathF.Sqrt(MathF.Pow(final.Position.X - along, 2) + final.Position.Z * final.Position.Z) < .002f, $"drift {final.Position}");
        Assert.InRange(final.Position.Y, rest - .003f, rest + .003f);
    }

    [Fact]
    public void ASphereDroppedOntoAStandingBoxTopplesItPastSixtyDegrees()
    {
        // The deleted harness also bounded each tick by the 120 Hz budget; native timing is not that claim (P0-034 owns it).
        var scene = new LawScene();
        var tile = scene.Domino(At(0, BenchY + DominoHalfY + .0005f));
        scene.Ball(LawBall.Basketball, At(.4f, 3));
        var world = scene.Admit();
        var passed = -1;
        var final = world.Step(3 * world.TicksPerSecond, (tick, state) => { if (passed < 0 && state[tile].Tilt > 60) passed = tick; })[tile];
        Assert.InRange(passed, 1, 2 * world.TicksPerSecond);
        Assert.InRange(final.Tilt, 85f, 95f);
    }

    private static Vector3[] SphereDirections(int count) => [.. Enumerable.Range(0, count).Select(i =>
    {
        var z = 1 - (2.0 * i + 1) / count; var r = Math.Sqrt(1 - z * z); var a = i * 2.399963229728653;
        return new Vector3((float)(r * Math.Cos(a)), (float)z, (float)(r * Math.Sin(a)));
    })];
    private static double Squared(Vector3 v) => (double)v.X * v.X + (double)v.Y * v.Y + (double)v.Z * v.Z;

    [Fact]
    public void SpeedsBeyondTheEnvelopeClampInsideTheExactHostBoundInEveryDirectionAndTheTickContinues()
    {
        var directions = SphereDirections(16);
        for (var k = 0; k < directions.Length; k++)
        {
            var scene = new LawScene();
            var tile = scene.Domino(At(0, 20), gravity: 0);
            var world = scene.Admit();
            world.SetVelocity(tile, 72 * directions[k], 144 * directions[(k + 5) % directions.Length]);
            var body = world.Step(1)[tile];
            Assert.True(Squared(body.Velocity) <= 64 * 64 && body.Speed > 63.99f, $"direction {k}: |v|² {Squared(body.Velocity)}");
            Assert.True(Squared(body.Angular) <= 128 * 128 && body.Spin > 127.99f, $"direction {k}: |omega|² {Squared(body.Angular)}");
            Assert.True(body.Finite, $"direction {k}: finite state");
        }
    }

    [Fact]
    public void ZeroRestitutionNeverReboundsAndZeroFrictionNeverSpinsUp()
    {
        var scene = new LawScene();
        var dead = scene.Sphere(At(0, 2), LawBalls.Radius(LawBall.Basketball), 1, LawMaterial.Ball(LawBall.Basketball) with { Restitution = 0 },
            default, default, LawBalls.Drag(LawBall.Basketball), LawScene.Gravity);
        var world = scene.Admit();
        var landed = false; var rebound = 0f;
        world.Step(2 * world.TicksPerSecond, (_, state) =>
        {
            var b = state[dead];
            if (b.Velocity.Y < -1) landed = true;
            if (landed) rebound = MathF.Max(rebound, b.Velocity.Y);
        });
        Assert.True(landed && rebound < .05f, $"restitution 0 gives no rebound (max upward {rebound})");
        var radius = LawBalls.Radius(LawBall.Basketball);
        var slick = new LawScene();
        var ball = slick.Sphere(At(0, BenchY + radius), radius, 1, LawMaterial.Ball(LawBall.Basketball) with { Friction = 0, Rolling = 0 },
            At(3, 0, 0), default, 0, LawScene.Gravity);
        var slickWorld = slick.Admit();
        var final = slickWorld.Step(slickWorld.TicksPerSecond)[ball];
        Assert.True(final.Spin < .05f && final.Velocity.X > 2.9f, $"friction 0 never spins ({final.Spin}) or slows ({final.Velocity.X}) the ball");
    }

    [Fact]
    public void RollingSpeedFollowsTheDeclaredPrincipalMomentsSolidVersusHollow()
    {
        // Sliding at 3 m/s into pure rolling: v = v0 / (1 + I / (m r²)) -> 2.143 m/s solid (2/5), 1.80 m/s hollow (2/3).
        var radius = LawBalls.Radius(LawBall.Basketball); var mass = LawBalls.Mass(LawBall.Basketball);
        float Roll(float? moment)
        {
            var scene = new LawScene();
            var ball = scene.Ball(LawBall.Basketball, At(0, BenchY + radius), velocity: At(3, 0, 0), resistanceFree: true);
            var world = scene.Admit();
            if (moment is { } i) world.SetPrincipalMoments(ball, new(i, i, i));
            return world.Step(world.TicksPerSecond)[ball].Velocity.X;
        }
        Assert.InRange(Roll(null), 2.143f - .08f, 2.143f + .08f);
        Assert.InRange(Roll(2f / 3 * mass * radius * radius), 1.8f - .08f, 1.8f + .08f);
    }

    [Fact]
    public void ARemainderCrossingHalfACellCarriesIntoTheNextCell()
    {
        // The engine commits a canonical remainder in [-0.5, 0.5) (checked on every tick of every law test): a body whose remainder
        // is the largest f32 below 0.5 and that moves forward carries into the next cell.
        var scene = new LawScene(bench: false);
        var ball = scene.Ball(LawBall.Basketball, At(0, 5), velocity: At(.01f, 0, 0), resistanceFree: true, gravity: 0);
        var world = scene.Admit();
        var start = world.Initial(ball);
        world.SetLocal(ball, start.Cell with { X = 0 }, start.Local with { X = MathF.BitDecrement(.5f) });
        var after = world.Step(1)[ball];
        Assert.Equal(1, after.Cell.X);
        Assert.InRange(after.Local.X, -.4988f, -.4985f); // 0.5 - 2^-25 + 0.01 m/s * (1/120) s * 16 - 1 = -0.49867
        // Control: the same remainder moving back keeps its cell (at rest the engine may canonicalise the largest f32 below 0.5 into
        // the next cell at -0.5, the same position, so rest is not a control).
        var still = new LawScene(bench: false);
        var resting = still.Ball(LawBall.Basketball, At(0, 5), velocity: At(-.01f, 0, 0), resistanceFree: true, gravity: 0);
        var stillWorld = still.Admit();
        stillWorld.SetLocal(resting, start.Cell with { X = 0 }, start.Local with { X = MathF.BitDecrement(.5f) });
        var back = stillWorld.Step(1)[resting];
        Assert.True(back.Cell.X == 0 && back.Local.X is > .49f and < .5f, $"moving back stays in cell 0 ({back.Cell.X}, {back.Local.X})");
    }

    [Fact]
    public void AZeroDeclaredMomentReadsAsInfiniteInertiaAndTheBodyStaysFinite()
    {
        var radius = LawBalls.Radius(LawBall.Basketball);
        var scene = new LawScene();
        var ball = scene.Ball(LawBall.Basketball, At(0, 1), velocity: At(2, 0, 0));
        var world = scene.Admit();
        world.SetPrincipalMoments(ball, Vector3.Zero);
        var final = world.Step(2 * world.TicksPerSecond)[ball];
        Assert.True(final.Finite);
        Assert.Equal(0f, final.Spin);
        Assert.InRange(final.Position.Y, BenchY + radius - .002f, BenchY + radius + .002f);
        // Control: the declared solid moment lets the same contact spin the ball.
        var control = new LawScene();
        var spun = control.Ball(LawBall.Basketball, At(0, 1), velocity: At(2, 0, 0));
        var controlWorld = control.Admit();
        Assert.True(controlWorld.Step(2 * controlWorld.TicksPerSecond)[spun].Spin > 1);
    }

    // ---- element-free solver ratchet (Story 9.4b coordinator decision, 18 Oct 2026; hardened for Murdoch-2 F2) ----

    /// <summary>The scheduled stories that own the element-specific solver code 8f682f8 introduced.</summary>
    public enum ViolationOwner { Story9A19 }

    /// <summary>Whether the general scan can see a pinned violation (so deleting its entry fails as unowned code), or only
    /// its pin can (the frozen list hash below then guards the entry).</summary>
    public enum ScanVisibility { General, PinnedOnly }

    private static string SprintKey(ViolationOwner owner) => owner switch
    {
        ViolationOwner.Story9A19 => "9a-19-declared-prismatic-drive",
        _ => throw new ArgumentOutOfRangeException(nameof(owner), owner, "Undefined violation owner."),
    };

    /// <summary>One pinned known violation: the exact number of matches of its pattern in its file, and the story that deletes it.</summary>
    private sealed record KnownViolation(string File, string Pattern, int Count, ViolationOwner Owner, ScanVisibility Visibility, string What);

    // The ratchet only shrinks. Each owning story deletes its code and its entry together. Any change to this list changes
    // its pinned SHA-256 (KnownViolationsSha256) and may never raise the pinned entry or match totals, so an addition is
    // visible in the diff and fails; a stale entry, a changed count, or element-specific code outside these entries fails.
    // When 9A.19 is done this list must be empty (asserted below against sprint-status.yaml).
    // Empty since C-Gate (9A.19): the gate drive's literals and its spring-family force went with ActuatorSliderDrive, replaced
    // by the declared drive law. It stays empty permanently (coordinator decision, 18 Oct 2026).
    private static readonly KnownViolation[] KnownViolations = [];

    // Frozen: these change only when an owning story deletes entries (both totals may only fall). Raised once, by the
    // coordinator's R2-F5 instruction, for the catalogue-derived scan's one new finding (fSpring, 9A.19). Lowered by Story 9R.3,
    // which deleted the direct drag encoding and band, the global battery flag and the travel-limit gate recognition. Lowered by
    // Story 9R.4, which deleted the size-recognised plate, its load window, 0.5 kg threshold and global flag, the plate drag
    // encoding and band, and the plate-named drive parameter (its gatePowered|plateLoaded entry fell to zero and was deleted).
    // Lowered to zero by C-Gate, which deleted the last two entries with ActuatorSliderDrive.
    private const int MaximumKnownViolations = 0;
    private const int MaximumPinnedMatches = 0;
    private const string KnownViolationsSha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    // Reviewed identifiers that contain an element name but name no element: "clamp" contains "lamp", "conjugate" contains
    // "gate". The shared electrical-domain tables contain "electrical" (the domain word of the electrical_* gate ids) but name no gate.
    private static readonly string[] GenericIdentifiers = ["Clamp", "ClampVelocity", "SpeedClamp", "SpinClamp", "Conjugate",
        "ElectricalSupply", "ElectricalEnable", "ElectricalSources", "ElectricalBindings", "ElectricalSourceOffset", "ElectricalBindingOffset"];

    // Reviewed qualified names: the prismatic constraint's soft spring row is the generic declared stiffness/damping law of
    // every prismatic, not the Springboard part. Only these exact qualified forms are set aside, so a bare "Spring" token
    // (a future Springboard-specific name) is still found.
    private const string GenericQualifiedNames = @"\b(?:PrismaticRowKind\.Spring|SoftConstraintCoefficients\.FromSpring)\b";

    /// <summary>Murdoch-2 R2-F5: the scanned element names are the catalogue's, never a hand list. Every word of every element
    /// package Id in <c>elements/</c> and of every undelivered part (letters only, three or more), so a new element is scanned the
    /// day its package is added (spring, pipe, funnel, detector and clock included). Display titles are not identifiers: their
    /// English words ("set", "clear", "signal") name no element.</summary>
    private static readonly string[] ElementNames = CatalogueElementWords();

    private static string[] CatalogueElementWords()
    {
        var root = twodog.Engine.ResolveProjectDir();
        var catalogue = UndeliveredParts.All.Select(part => UndeliveredParts.Id(part).Value);
        var packages = PartRegistry.Read(File.ReadAllText(Path.Combine(root, "elements", "elements.bundle.json")))
            .Select(entry => entry.Metadata.Id);
        return catalogue.Concat(packages)
            .SelectMany(name => Regex.Matches(name, "[A-Za-z]{3,}").Select(word => word.Value.ToLowerInvariant()))
            .Distinct().Order(StringComparer.Ordinal).ToArray();
    }

    private static string CodeOnly(string path) =>
        // Comments (their newlines kept, so line numbers hold) and the C# switch keyword are not code that names an element.
        Regex.Replace(Regex.Replace(File.ReadAllText(path), @"//[^\n]*|/\*[\s\S]*?\*/",
            comment => new string('\n', comment.Value.Count(c => c == '\n'))), @"\bswitch(?=\s*[({\r\n])", "");

    private static string Canonical(KnownViolation known) => string.Join('\u001f', known.File, known.Pattern,
        known.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), ((int)known.Owner).ToString(System.Globalization.CultureInfo.InvariantCulture),
        ((int)known.Visibility).ToString(System.Globalization.CultureInfo.InvariantCulture), known.What);

    private static string KnownViolationsHash() => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(string.Join('\u001e', KnownViolations.Select(Canonical)))));

    /// <summary>Every element-specific match in one solver source: identifiers containing an element name (any case,
    /// inside camelCase), the harness's ball literals, and recognition by declared geometry, limits or drag.</summary>
    private static List<(int Start, int End, string Text)> GeneralHits(string source)
    {
        // The harness's inertia, fallback and ball-literal patterns, with C# f suffixes and ?? fallbacks.
        const string Literals = @"3\.5f? \* |2\.5f? / |(\|\||\?\?) 0?\.(34|75|1|3)f?\b|(\|\||\?\?) (1\.0|8\.0)f?\b|" +
            @"(?<![\d.])0?\.(28|38|14|035|03)f?(?!\d)";
        const string Recognition = @"MathF\.Abs\([^()]*\.(Hx|Hy|Hz|Tx|Ty|Tz|Upper|Lower|Radius|Drag|Mass) - [\d.]+f?\)|\.Drag\s*[<>]=?\s*[\d.]+f?";
        var hits = new List<(int, int, string)>();
        // The reviewed qualified names are blanked, keeping every other index.
        var tokens = Regex.Replace(source, GenericQualifiedNames, name => new string(' ', name.Length));
        foreach (Match token in Regex.Matches(tokens, @"\b[A-Za-z_][A-Za-z0-9_]*\b"))
            if (!GenericIdentifiers.Contains(token.Value) &&
                Regex.Matches(token.Value, @"[A-Z]+(?=[A-Z][a-z]|[^A-Za-z]|$)|[A-Z]?[a-z]+|[A-Z]+").Any(word => ElementNames.Contains(word.Value.ToLowerInvariant(), StringComparer.Ordinal)))
                hits.Add((token.Index, token.Index + token.Length, token.Value));
        foreach (var pattern in new[] { Literals, Recognition })
            foreach (Match match in Regex.Matches(source, pattern)) hits.Add((match.Index, match.Index + match.Length, match.Value));
        return hits;
    }

    private static readonly string[] ShippedSolverFiles = ["WorkshopPhysicsEngine.cs", "WorkshopCollisionKernels.cs",
        "WorkshopConstraintSolver.cs", "PrismaticConstraintSolver.cs", "WorkshopDynamicBvh.cs"];

    [Fact]
    public void TheShippedSolverNamesNoElementBeyondThePinnedOwnedViolations()
    {
        var root = twodog.Engine.ResolveProjectDir();
        var problems = new List<string>();
        var solverFiles = ShippedSolverFiles.Select(file => "CuriousContraptions.Simulation/" + file).ToArray();
        var general = solverFiles.ToDictionary(file => file, file => GeneralHits(CodeOnly(Path.Combine(root, file))));
        var excused = new Dictionary<string, List<(int Start, int End)>>();
        foreach (var known in KnownViolations)
        {
            var matches = Regex.Matches(CodeOnly(Path.Combine(root, known.File)), known.Pattern);
            if (matches.Count != known.Count)
                problems.Add(known.Count == 0 || matches.Count > 0
                    ? $"Known violation '{known.What}' ({SprintKey(known.Owner)}) in {known.File} now matches {matches.Count}, pinned {known.Count}."
                    : $"Stale: '{known.What}' is gone from {known.File}; {SprintKey(known.Owner)} must delete its entry.");
            if (!excused.TryGetValue(known.File, out var spans)) excused[known.File] = spans = [];
            foreach (Match match in matches)
            {
                spans.Add((match.Index, match.Index + match.Length));
                // A pin the general scan can see protects itself: deleting it leaves the code unowned.
                var seen = general.TryGetValue(known.File, out var hits) &&
                    hits.Any(hit => match.Index < hit.End && match.Index + match.Length > hit.Start);
                if (seen != (known.Visibility == ScanVisibility.General))
                    problems.Add($"'{known.What}' is declared {known.Visibility} but the general scan {(seen ? "sees" : "does not see")} '{match.Value}'.");
            }
        }
        foreach (var file in solverFiles)
        {
            var source = CodeOnly(Path.Combine(root, file));
            var spans = excused.GetValueOrDefault(file) ?? [];
            foreach (var hit in general[file])
                if (!spans.Any(span => hit.Start < span.End && hit.End > span.Start))
                    problems.Add($"{file}:{source[..hit.Start].Count(c => c == '\n') + 1}: unowned element-specific code '{hit.Text}'");
        }
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    // Murdoch-2 F2(c): the list is frozen; an added entry changes the hash and raises a total, so it cannot pass unseen.
    [Fact]
    public void TheKnownViolationListIsFrozenAndCanOnlyShrink()
    {
        Assert.True(KnownViolations.Length <= MaximumKnownViolations, $"{KnownViolations.Length} entries exceed the frozen {MaximumKnownViolations}");
        var pinned = KnownViolations.Sum(known => known.Count);
        Assert.True(pinned <= MaximumPinnedMatches, $"{pinned} pinned matches exceed the frozen {MaximumPinnedMatches}");
        var hash = KnownViolationsHash();
        Assert.True(hash == KnownViolationsSha256, $"The known-violation list changed (SHA-256 {hash}); only an owning story may delete entries.");
        Assert.Equal(KnownViolations.Length, KnownViolations.Select(known => (known.File, known.Pattern)).Distinct().Count());
    }

    [Theory]
    [InlineData("bumperBoost", true)] // Murdoch-2 F2(a): camelCase escaped the word-boundary scan
    [InlineData("_plateLoaded", true)]
    [InlineData("isGateOpen", true)]
    [InlineData("SpeedClamp", false)]
    [InlineData("Conjugate", false)]
    [InlineData("contactNormal", false)]
    [InlineData("PrismaticRowKind.Spring", false)] // the reviewed generic soft row, qualified
    [InlineData("Spring", true)]                  // a bare Spring token is still the spring family
    [InlineData("softRowError", false)]
    [InlineData("holdWindow", true)]
    [InlineData("threshold", false)]
    [InlineData("ElectricalSupply", false)] // the reviewed shared electrical-domain table
    [InlineData("electricalNandRow", true)]  // an element-specific electrical name is still found
    [InlineData("nandRow", true)]
    [InlineData("norRow", true)]             // Neither gate (electrical_nor); "contactNormal" above stays generic
    [InlineData("electricalOrRow", true)]    // Either gate: found through its "electrical" id word
    [InlineData("orRow", false)]             // "or" is under the three-letter id-word floor (like "color", "order")
    public void TheGeneralScanFindsElementNamesInsideIdentifiersButNotTheReviewedGenericWords(string identifier, bool element) =>
        Assert.Equal(element, GeneralHits($"var x = {identifier};").Count > 0);

    // Murdoch-2 R2-F5: the families the hand list missed are scanned, and every catalogue part's Id words are in the set.
    [Theory]
    [InlineData("springStiffness")] [InlineData("pipeBore")] [InlineData("funnelMouth")] [InlineData("detectorAperture")]
    [InlineData("clockPhase")] [InlineData("delayTicks")] [InlineData("tennisSpin")] [InlineData("balloonLift")]
    public void TheGeneralScanCoversEveryCatalogueFamily(string identifier) => Assert.NotEmpty(GeneralHits($"var x = {identifier};"));

    [Fact]
    public void TheScannedElementNamesAreDerivedFromTheCatalogue() =>
        Assert.All(PartRegistry.Read(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "elements", "elements.bundle.json"))), entry =>
            Assert.Contains(Regex.Match(entry.Metadata.Id, "^[a-z]+").Value, ElementNames));

    [Fact]
    public void EveryPinnedViolationsOwnerIsOpenAndTheListIsEmptyOnce9A19IsDone()
    {
        var status = File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(),
            "_bmad-output/implementation-artifacts/sprint-status.yaml"));
        string State(ViolationOwner owner)
        {
            var match = Regex.Match(status, $@"(?m)^\s*{Regex.Escape(SprintKey(owner))}:\s*(\S+)");
            Assert.True(match.Success, $"sprint-status.yaml has no story {SprintKey(owner)}");
            return match.Groups[1].Value;
        }
        foreach (var known in KnownViolations)
            Assert.True(State(known.Owner) != "done", $"{SprintKey(known.Owner)} is done but still owns '{known.What}'.");
        if (State(ViolationOwner.Story9A19) == "done") Assert.Empty(KnownViolations);
        // Control: the known list is non-empty today and every entry names a defined owner.
        Assert.All(KnownViolations, known => Assert.True(Enum.IsDefined(known.Owner)));
    }

    [Fact]
    public void TheMotionDescriptionIsWrittenAtTheHostDeclaredOffsetAfterTheOrientationSensorTable()
    {
        var scene = new LawScene();
        scene.Ball(LawBall.Basketball, At(0, 3));
        var world = scene.Admit();
        var stepped = world.Step(1).World;
        var layout = PhysicsGpuAbi.Layout(stepped);
        var bytes = stepped.AsSpan(layout.MotionOffset);
        // Story 9R.5b: the motion payload directly follows the exactly sized orientation-sensor table.
        Assert.Equal(layout.OrientationSensorsOffset + layout.OrientationSensors * PhysicsGpuAbi.OrientationSensorBytes, layout.MotionOffset);
        Assert.Equal(4u, BinaryPrimitives.ReadUInt32LittleEndian(bytes)); // one dynamic body contributes four motion pieces
        Assert.Equal(4u, BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..])); // four substeps recorded
        Assert.Equal(4u, BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..])); // last ordinal recorded
    }

    // ---- drag ----

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public void AFreeFlyingSphereSlowsAsDeclaredDragAndItsFreeFlightSpinIsNeverDamped(SimulationCadence cadence)
    {
        const float drag = .125f;
        var expected = 2 * MathF.Exp(-drag * 2);
        LawBody Fly(float declared)
        {
            var scene = new LawScene(bench: false);
            var ball = scene.Sphere(At(0, 1), LawBalls.Radius(LawBall.Basketball), 1, LawMaterial.Ball(LawBall.Basketball), At(2, 0, 0),
                At(0, 5, 0), declared, 0);
            var world = scene.Admit(cadence);
            return world.Step(2 * world.TicksPerSecond)[ball];
        }
        var flying = Fly(drag);
        Assert.InRange(flying.Velocity.X, expected * .99f, expected * 1.01f);
        Assert.Equal(5f, flying.Angular.Y);
        Assert.Equal(2f, Fly(0).Velocity.X); // control: zero declared drag keeps the speed exactly
    }

    [Fact]
    public void AFreeFlyingSphereMovingDiagonallySlowsAlongBothXAndZ()
    {
        var scene = new LawScene(bench: false);
        var ball = scene.Sphere(At(0, 1), LawBalls.Radius(LawBall.Basketball), 1, LawMaterial.Ball(LawBall.Basketball), At(2, 0, 2), default,
            .125f, 0);
        var world = scene.Admit();
        var final = world.Step(2 * world.TicksPerSecond)[ball];
        var expected = 2 * MathF.Exp(-.125f * 2);
        Assert.InRange(final.Velocity.X, expected * .97f, expected * 1.03f);
        Assert.InRange(final.Velocity.Z, expected * .97f, expected * 1.03f);
    }

    [Fact]
    public void AtTheEnvelopeBoundaryDeclaredDragStillSlowsABallFlyingAt63Point9OnEveryTick()
    {
        var scene = new LawScene(bench: false);
        var ball = scene.Ball(LawBall.Basketball, At(-40, 1), velocity: At(63.9f, 0, 0), gravity: 0);
        var world = scene.Admit(SimulationCadence.Hz240);
        var drag = LawBalls.Drag(LawBall.Basketball); var previous = 63.9f;
        world.Step(world.TicksPerSecond / 2, (tick, state) =>
        {
            var v = state[ball].Velocity.X; var expected = 63.9f * MathF.Exp(-drag * tick / world.TicksPerSecond);
            Assert.True(v < previous && MathF.Abs(v - expected) < 1e-3f * expected, $"tick {tick}: {v} vs {expected} (previous {previous})");
            previous = v;
        });
    }

    [Fact]
    public void EveryMotionPieceCarriesItsBodysDeclaredDragRate()
    {
        const int MotionPieceBodyIdOffset = 24; // PhysicsMotionRead piece layout: body identity (u64) at +24
        var scene = new LawScene(bench: false);
        var ball = scene.Ball(LawBall.Basketball, At(0, 1), velocity: At(2, 0, 0), gravity: 0);
        var bowling = scene.Sphere(At(0, 1, 2), LawBalls.Radius(LawBall.Bowling), LawBalls.Mass(LawBall.Bowling),
            LawMaterial.Ball(LawBall.Bowling), At(2, 0, 0), default, .125f, 0);
        var free = scene.Ball(LawBall.Basketball, At(0, 1, -2), resistanceFree: true, gravity: 0);
        var world = scene.Admit();
        var stepped = world.Step(1).World;
        var motion = stepped.AsSpan(PhysicsGpuAbi.Layout(stepped).MotionOffset);
        var declared = new Dictionary<ulong, Half> { [ball.Value] = (Half)LawBalls.Drag(LawBall.Basketball), [bowling.Value] = (Half).125f,
            [free.Value] = (Half)0 };
        var count = (int)BinaryPrimitives.ReadUInt32LittleEndian(motion);
        Assert.Equal(12, count);
        for (var i = 0; i < count; i++)
        {
            var piece = motion[(PhysicsMotionRead.HeaderBytes + i * PhysicsMotionRead.PieceBytes)..];
            var id = BinaryPrimitives.ReadUInt64LittleEndian(piece[MotionPieceBodyIdOffset..]);
            Assert.Equal(BitConverter.HalfToUInt16Bits(declared[id]),
                BinaryPrimitives.ReadUInt16LittleEndian(piece[PhysicsMotionRead.DragRateOffset..]));
        }
    }

    // ---- rolling resistance ----

    private static LawBody[] RollOnBench(LawBall kind, float speed, int seconds, SimulationCadence cadence = SimulationCadence.Hz120,
        bool resistanceFree = false)
    {
        var radius = LawBalls.Radius(kind);
        var scene = new LawScene();
        var ball = scene.Ball(kind, At(0, BenchY + radius), velocity: At(speed, 0, 0), angular: At(0, 0, -speed / radius),
            resistanceFree: resistanceFree);
        var world = scene.Admit(cadence);
        var states = new List<LawBody>();
        world.Step(seconds * world.TicksPerSecond, (_, state) => states.Add(state[ball]));
        return [.. states];
    }

    private static float Predicted(LawBall kind, float v) =>
        5f / 7 * (LawBalls.Rolling(kind) * -LawScene.Gravity + LawBalls.Drag(kind) * v);

    [Theory]
    [InlineData(LawBall.Basketball)]
    [InlineData(LawBall.Bowling)]
    public void EachBallRollingAt0Point7DeceleratesAtTheRollingLawRestsAtItsRadiusAndNeverReverses(LawBall kind)
    {
        const int rate = 120;
        var states = RollOnBench(kind, .7f, 6);
        var early = states[rate / 4 - 1]; var later = states[5 * rate / 4 - 1];
        var measured = early.Velocity.X - later.Velocity.X;
        var predicted = Predicted(kind, (early.Velocity.X + later.Velocity.X) / 2);
        Assert.True(MathF.Abs(measured - predicted) < .1f * predicted, $"{kind} decelerates {measured} m/s² vs {predicted}");
        var rest = Array.FindIndex(states, b => b.Speed < .02f);
        Assert.InRange(rest, 0, 5 * rate - 1);
        Assert.True(states.Min(b => b.Velocity.X) >= -1e-6f && states.Max(b => b.Angular.Z) <= 1e-6f, $"{kind} never reverses");
        var tail = states[^rate..];
        Assert.True(tail.Max(b => (b.Position - tail[0].Position).Length()) < .001f && tail.Max(b => b.Speed) < .02f);
        Assert.InRange(tail[^1].Position.Y, BenchY + LawBalls.Radius(kind) - .001f, BenchY + LawBalls.Radius(kind) + .001f);
    }

    [Fact]
    public void WithZeroDeclaredDragAndCoefficientARollingBallKeepsItsSpeed()
    {
        var final = RollOnBench(LawBall.Basketball, .7f, 3, resistanceFree: true)[^1];
        Assert.InRange(final.Velocity.X, .695f, .705f);
        Assert.True(MathF.Abs(final.Angular.Z + final.Velocity.X / LawBalls.Radius(LawBall.Basketball)) < .02f);
    }

    [Theory]
    [InlineData(LawBall.Basketball)]
    [InlineData(LawBall.Bowling)]
    public void AtEveryCadenceABallStruckTo1MetrePerSecondStopsAtTheSameTimeAndNoTickErasesItsDecrement(LawBall kind)
    {
        var stops = new Dictionary<SimulationCadence, float>();
        foreach (var (cadence, rate) in new[] { (SimulationCadence.Hz60, 60), (SimulationCadence.Hz120, 120), (SimulationCadence.Hz240, 240) })
        {
            var forward = RollOnBench(kind, 1, 6, cadence).Select(b => b.Velocity.X).ToArray();
            var rest = Array.FindIndex(forward, v => v < .02f);
            Assert.True(rest > 0, $"{kind} {rate} Hz comes to rest");
            for (var i = 1; i < rest; i++) Assert.True(forward[i] < forward[i - 1], $"{kind} {rate} Hz tick {i + 1}: {forward[i]} !< {forward[i - 1]}");
            stops[cadence] = (rest + 1f) / rate;
        }
        Assert.True(stops[SimulationCadence.Hz120] < 5);
        foreach (var cadence in new[] { SimulationCadence.Hz60, SimulationCadence.Hz240 })
            Assert.True(MathF.Abs(stops[cadence] - stops[SimulationCadence.Hz120]) < .1f * stops[SimulationCadence.Hz120],
                $"{kind} {cadence} stops at {stops[cadence]} s vs {stops[SimulationCadence.Hz120]} s");
    }

    private static (LawBody[] States, int RestTick) Settle(LawScene scene, GpuBodyId body, int seconds = 6)
    {
        var world = scene.Admit();
        var states = new List<LawBody>();
        world.Step(seconds * world.TicksPerSecond, (_, state) => states.Add(state[body]));
        return ([.. states], states.FindIndex(b => b.Speed < .02f));
    }
    private static float Spread(LawBody[] tail) => tail.Max(b => (b.Position - tail[0].Position).Length());

    [Fact]
    public void ABallRollingAlongPlusZAboutXAlsoComesToRest()
    {
        var radius = LawBalls.Radius(LawBall.Basketball);
        var scene = new LawScene();
        var ball = scene.Ball(LawBall.Basketball, At(0, BenchY + radius), velocity: At(0, 0, .7f), angular: At(.7f / radius, 0, 0));
        var (states, rest) = Settle(scene, ball);
        Assert.InRange(rest, 0, 5 * 120 - 1);
        Assert.True(states.Min(b => b.Velocity.Z) >= 0 && Spread(states[^120..]) < .001f);
        // Control: resistance-free, the same roll keeps its speed.
        var control = new LawScene();
        var free = control.Ball(LawBall.Basketball, At(0, BenchY + radius), velocity: At(0, 0, .7f), angular: At(.7f / radius, 0, 0),
            resistanceFree: true);
        Assert.True(Settle(control, free, 3).States[^1].Velocity.Z > .69f);
    }

    [Fact]
    public void ABallRollingOnAStaticBoxSlabComesToRestOnTheSlab()
    {
        var radius = LawBalls.Radius(LawBall.Basketball); var top = BenchY + 1.1f;
        var scene = new LawScene();
        scene.StaticBox(At(0, BenchY + 1), At(2.5f, .1f, 1));
        var ball = scene.Ball(LawBall.Basketball, At(-1, top + radius), velocity: At(.7f, 0, 0), angular: At(0, 0, -.7f / radius));
        var (states, rest) = Settle(scene, ball);
        Assert.InRange(rest, 0, 5 * 120 - 1);
        var last = states[^1];
        Assert.True(MathF.Abs(last.Position.Y - (top + radius)) < .002f && MathF.Abs(last.Position.X) < 2.5f && Spread(states[^120..]) < .001f,
            $"still on the slab at {last.Position}");
    }

    [Fact]
    public void ABallRollingOnADynamicBoxSlabAlsoComesToRest()
    {
        var radius = LawBalls.Radius(LawBall.Basketball); var top = BenchY + .2f;
        var scene = new LawScene();
        scene.Box(At(0, BenchY + .1f + .0005f), At(2.5f, .1f, 1), 20, LawMaterial.Domino);
        var ball = scene.Ball(LawBall.Basketball, At(-1, top + radius), velocity: At(.7f, 0, 0), angular: At(0, 0, -.7f / radius));
        var (states, rest) = Settle(scene, ball);
        Assert.InRange(rest, 0, 5 * 120 - 1);
        Assert.True(MathF.Abs(states[^1].Position.Y - (top + radius)) < .003f && Spread(states[^120..]) < .001f);
    }

    [Fact]
    public void ABallRestingOnTheBenchSpinningAboutTheContactNormalKeepsItsSpin()
    {
        var radius = LawBalls.Radius(LawBall.Basketball);
        var scene = new LawScene();
        var ball = scene.Ball(LawBall.Basketball, At(0, BenchY + radius), angular: At(0, 5, 0));
        var world = scene.Admit();
        Assert.Equal(5f, world.Step(2 * world.TicksPerSecond)[ball].Angular.Y);
    }

    [Fact]
    public void AFreePartnerBallReceivesTheEqualAndOppositeRollingImpulse()
    {
        (LawBody A, LawBody B, LawBody A0, LawBody B0) Strike(float spin)
        {
            LawBody[] Run(bool resistanceFree)
            {
                var scene = new LawScene();
                var a = scene.Ball(LawBall.Basketball, At(-1, 1), velocity: At(2, 0, 0), angular: At(0, 0, spin), resistanceFree: resistanceFree, gravity: 0);
                var b = scene.Ball(LawBall.Basketball, At(0, 1), resistanceFree: resistanceFree, gravity: 0);
                var world = scene.Admit();
                var final = world.Step(world.TicksPerSecond);
                return [final[a], final[b]];
            }
            var with = Run(false); var without = Run(true);
            return (with[0], with[1], without[0], without[1]);
        }
        var small = Strike(.3f);
        Assert.True(MathF.Abs(small.A.Angular.Z - small.B.Angular.Z) < .005f, $"relative rolling zeroed ({small.A.Angular.Z} vs {small.B.Angular.Z})");
        Assert.True(small.B.Angular.Z - small.B0.Angular.Z > .1f, $"the partner gains spin ({small.B.Angular.Z - small.B0.Angular.Z})");
        var large = Strike(10);
        var dA = large.A.Angular.Z - large.A0.Angular.Z; var dB = large.B.Angular.Z - large.B0.Angular.Z;
        Assert.True(dB > .15f && MathF.Abs(dA + dB) < .05f, $"equal and opposite (striker {dA}, partner {dB})");
    }

    [Fact]
    public void ARollingBowlingBallPushingARestingBasketballBringsBothToRestTogether()
    {
        var rw = LawBalls.Radius(LawBall.Bowling); var rb = LawBalls.Radius(LawBall.Basketball);
        var scene = new LawScene();
        var bowling = scene.Ball(LawBall.Bowling, At(-1, BenchY + rw), velocity: At(.7f, 0, 0), angular: At(0, 0, -.7f / rw));
        var ball = scene.Ball(LawBall.Basketball, At(0, BenchY + rb));
        var world = scene.Admit();
        var states = new List<(LawBody W, LawBody B)>();
        world.Step(6 * world.TicksPerSecond, (_, state) => states.Add((state[bowling], state[ball])));
        Assert.True(states.Max(s => s.B.Velocity.X) > .3f, "the Basketball is driven forward");
        var rest = Math.Max(states.FindLastIndex(s => s.W.Speed >= .02f), states.FindLastIndex(s => s.B.Speed >= .02f)) + 1;
        Assert.True(rest < 5 * world.TicksPerSecond, $"both rest by tick {rest}");
        Assert.All(states, s => Assert.True(s.W.Velocity.X >= -.005f && s.B.Velocity.X >= -.005f &&
            MathF.Abs(s.W.Position.Z) < .005f && MathF.Abs(s.B.Position.Z) < .005f));
        var (w, b) = states[^1];
        Assert.InRange(b.Position.X - w.Position.X, rw + rb - .005f, rw + rb + .005f);
    }

    // ---- f32 velocity ----

    [Fact]
    public void ABasketballRollingAt5MetresPerSecondAt240HzDeceleratesOnEveryCommittedTick()
    {
        var states = RollOnBench(LawBall.Basketball, 5, 18, SimulationCadence.Hz240);
        const int rate = 240; var radius = LawBalls.Radius(LawBall.Basketball);
        var rest = Array.FindIndex(states, b => b.Speed < .02f);
        Assert.InRange(rest, 1, (int)(17.5 * rate));
        var previous = 5f; var previousSpin = (double)(5 / radius);
        for (var i = 0; i < rest; i++)
        {
            Assert.True(states[i].Velocity.X < previous && states[i].Spin < previousSpin,
                $"tick {i + 1}: {states[i].Velocity.X} m/s (previous {previous}), spin {states[i].Spin} rad/s (previous {previousSpin})");
            previous = states[i].Velocity.X; previousSpin = states[i].Spin;
        }
    }

    [Fact]
    public void ABasketballRollingAt5MetresPerSecondAt240HzDeceleratesAtTheRollingLawAndRestsOnTheBench()
    {
        var states = RollOnBench(LawBall.Basketball, 5, 18, SimulationCadence.Hz240);
        const int rate = 240; var radius = LawBalls.Radius(LawBall.Basketball);
        var rest = Array.FindIndex(states, b => b.Speed < .02f);
        Assert.InRange(rest, 1, (int)(17.5 * rate));
        var early = states[rate - 1]; var later = states[3 * rate - 1];
        var measured = (early.Velocity.X - later.Velocity.X) / 2;
        var predicted = Predicted(LawBall.Basketball, (early.Velocity.X + later.Velocity.X) / 2);
        Assert.True(MathF.Abs(measured - predicted) < .1f * predicted, $"decelerates {measured} vs {predicted}");
        Assert.True(states.Min(b => b.Velocity.X) >= -1e-6f && states.Max(b => b.Angular.Z) <= 1e-6f);
        Assert.InRange(states[^1].Position.Y, BenchY + radius - .001f, BenchY + radius + .001f);
    }

    private static List<LawBody> RollAt20MetresPerSecond()
    {
        var radius = LawBalls.Radius(LawBall.Basketball);
        var scene = new LawScene();
        var ball = scene.Ball(LawBall.Basketball, At(-60, BenchY + radius), velocity: At(20, 0, 0), angular: At(0, 0, -20 / radius));
        var world = scene.Admit(SimulationCadence.Hz240);
        var states = new List<LawBody>();
        world.Step(world.TicksPerSecond, (_, state) => states.Add(state[ball]));
        return states;
    }

    [Fact]
    public void ABasketballRollingAt20MetresPerSecondAt240HzNeverRaisesItsSpin()
    {
        var states = RollAt20MetresPerSecond();
        var previousSpin = (double)(20 / LawBalls.Radius(LawBall.Basketball));
        for (var i = 0; i < states.Count; i++)
        {
            Assert.True(states[i].Spin <= previousSpin, $"tick {i + 1}: spin rises to {states[i].Spin} from {previousSpin}");
            previousSpin = states[i].Spin;
        }
    }

    [Fact]
    public void ABasketballRollingAt20MetresPerSecondAt240HzDeceleratesAtTheRollingLawOverASecondAndNeverReverses()
    {
        var states = RollAt20MetresPerSecond(); var radius = LawBalls.Radius(LawBall.Basketball);
        Assert.True(states[^1].Spin < states[0].Spin,
            $"the spin falls over the second (max {states.Max(b => b.Spin)}, first {states[0].Spin}, last {states[^1].Spin}, declared {20 / radius})");
        Assert.True(states.Min(b => b.Velocity.X) > 0 && states.Max(b => b.Angular.Z) < 0);
        var last = states[^1]; var predicted = Predicted(LawBall.Basketball, (20 + last.Velocity.X) / 2);
        Assert.True(MathF.Abs(20 - last.Velocity.X - predicted) < .1f * predicted, $"decelerates {20 - last.Velocity.X} vs {predicted}");
        Assert.True(MathF.Abs(last.Angular.Z + last.Velocity.X / radius) < .01f * last.Velocity.X / radius &&
            MathF.Abs(last.Position.Y - (BenchY + radius)) < .002f);
    }

    [Fact]
    public void ABodyWhoseCentreOfMassSitsOffItsOriginSpinsAboutThatCentreInFreeFlight()
    {
        var scene = new LawScene(bench: false);
        var body = scene.Sphere(At(0, 1), .2f, 1, LawMaterial.Ball(LawBall.Basketball) with { Rolling = 0 }, default, At(0, 0, 2), 0, 0,
            colliderOffset: At(.1f, 0, 0));
        var world = scene.Admit();
        var worst = 0f; var reach = 0f;
        world.Step(2 * world.TicksPerSecond, (_, state) =>
        {
            var p = state[body].Position;
            worst = MathF.Max(worst, MathF.Abs((p - At(.1f, 1)).Length() - .1f));
            reach = MathF.Max(reach, (p - At(0, 1)).Length());
        });
        Assert.True(worst < 3e-3f, $"the origin stays 0.1 m from the declared centre of mass (worst {worst})");
        Assert.True(reach > .19f, $"the origin really circles the centre (farthest {reach})");
    }

    [Fact]
    public void AHeavyBodyStrikingALightOneAtTheEnvelopeCannotCommitAStruckSpeedBeyond64()
    {
        var material = new LawMaterial(1, .1f, 0, 0);
        var scene = new LawScene(bench: false);
        var light = scene.Sphere(At(0, 5), .3f, 1, material, default, default, 0, 0);
        var heavy = scene.Sphere(At(-1.07f, 5), .3f, 100, material, At(64, 0, 0), default, 0, 0);
        var world = scene.Admit();
        var first = world.Step(1);
        Assert.True(Squared(first[light].Velocity) <= 64 * 64 && first[light].Velocity.X > 63.99f, $"struck {first[light].Velocity.X}");
        Assert.True(Squared(first[heavy].Velocity) <= 64 * 64 && first[heavy].Velocity.X < 63.9f, $"striker {first[heavy].Velocity.X}");
    }

    private static SimBody GuardBody(float vx, float vy, float vz, float wx, float wy, float wz) =>
        new() { Motion = 1, Vx = vx, Vy = vy, Vz = vz, Wx = wx, Wy = wy, Wz = wz };

    [Fact]
    public void TheEndOfSubstepGuardClampsAFiniteVelocityOfAnySize()
    {
        // Components near the format maximum: their squared length overflows, yet the vectors are finite and must clamp, not zero.
        var huge = GuardBody(3e38f, 3e38f, -3e38f, 3e38f, -3e38f, 2e38f);
        WorkshopConstraintSolver.SettleVelocity(huge);
        Assert.InRange(Squared(huge.LinearVelocity), 63.99 * 63.99, 64 * 64);
        Assert.InRange(Squared(huge.AngularVelocity), 127.99 * 127.99, 128 * 128);
        Assert.True(huge.Vx == huge.Vy && huge.Vx == -huge.Vz && huge.Wx == -huge.Wy && huge.Wz > 0, "and keeps its direction");
    }

    [Fact]
    public void TheEndOfSubstepGuardClampsAnOrdinaryOvershootKeepsAnInEnvelopeVelocityAndDropsOnlyANonFiniteOne()
    {
        var fast = GuardBody(0, -100, 0, 0, 0, 200);
        WorkshopConstraintSolver.SettleVelocity(fast);
        Assert.True(fast.Vy < -63.99f && fast.Wz > 127.99f && Squared(fast.LinearVelocity) <= 64 * 64);
        var inside = GuardBody(3, -4, 12, 1, 2, -3);
        WorkshopConstraintSolver.SettleVelocity(inside);
        Assert.Equal((3f, -4f, 12f, 1f, 2f, -3f), (inside.Vx, inside.Vy, inside.Vz, inside.Wx, inside.Wy, inside.Wz));
        foreach (var lane in new[] { 0, 5 })
        {
            var broken = GuardBody(1, 2, 3, 4, 5, 6);
            if (lane == 0) broken.Vx = float.NaN; else broken.Wz = float.PositiveInfinity;
            WorkshopConstraintSolver.SettleVelocity(broken);
            Assert.Equal((0f, 0f, 0f, 0f, 0f, 0f), (broken.Vx, broken.Vy, broken.Vz, broken.Wx, broken.Wy, broken.Wz));
        }
    }

    // Blind hunter 11 (Story 9.4b final round): a NaN lane has no length to clamp to, so the envelope scale is 0, and the
    // mid-substep clamp zeroes such a vector instead of multiplying NaN (or infinity) by 0, which stays NaN.
    [Theory]
    [InlineData(float.NaN, 0f, 0f, 0f)]
    [InlineData(1f, float.NaN, 2f, 0f)]
    [InlineData(float.PositiveInfinity, 0f, 0f, 0f)]
    [InlineData(1f, float.NegativeInfinity, float.NaN, 0f)]
    [InlineData(3f, 4f, 0f, 1f)] // control: inside the envelope
    public void TheEnvelopeScaleOfANonFiniteVectorIsZero(float x, float y, float z, float expected) =>
        Assert.Equal(expected, WorkshopConstraintSolver.EnvelopeScale(x, y, z, WorkshopConstraintSolver.SpeedClamp));

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void TheMidSubstepClampZeroesANonFiniteVelocityWithoutTouchingTheFiniteOne(float bad)
    {
        var body = GuardBody(bad, 2, 3, 4, -5, 6);
        WorkshopConstraintSolver.ClampVelocity(body);
        Assert.Equal((0f, 0f, 0f), (body.Vx, body.Vy, body.Vz));
        Assert.Equal((4f, -5f, 6f), (body.Wx, body.Wy, body.Wz));
    }

    [Fact]
    public void ThePaidContactFrameUsesTheBodysCurrentAnisotropicInertia()
    {
        // A body rotated 0.2 rad about X with principal inverse moments (1, 4, 1): its current world inverse inertia couples Y and Z.
        // The deleted kernel also recomputed the contact arm after pose integration; the shipped engine pays with the arm prepared
        // before integration (Story 9.4b finding), so only the current-inertia half of that fact is asserted here.
        const float angle = .2f;
        var s = MathF.Sin(angle); var c = MathF.Cos(angle);
        var izz = 4 * s * s + c * c; var iyz = 3 * s * c;
        var dynamic = new SimBody { Motion = 1, InvMass = 1, Vy = 4, Wx = 96 };
        float[] current = [1, 0, 0, 0, 4 * c * c + s * s, iyz, 0, iyz, izz];
        current.CopyTo(dynamic.InvIWorld, 0);
        var contact = new ContactPoint { A = dynamic, B = new SimBody { Motion = 0 }, Px = 1, Ny = 1, RAx = 1, RBx = 1 };
        var inverseMass = 1 / WorkshopConstraintSolver.EffectiveMass(contact, contact.Normal);
        Assert.InRange(inverseMass, 1 + izz - 1e-5f, 1 + izz + 1e-5f);
        Assert.InRange(inverseMass, 2.118f, 2.119f);
        var speed = Vector3.Dot(WorkshopConstraintSolver.RelativeVelocity(contact), contact.Normal);
        Assert.Equal(4f, speed);
        var impulse = (8 - speed) / inverseMass;
        Assert.True(speed * impulse + .5f * inverseMass * impulse * impulse < 12, "current inertia lowers the work needed for the target");
        Assert.True(speed * 2 + .5f * inverseMass * 2 * 2 > 12.23f, "the stale 2-unit impulse would spend undeclared work");
        // Control: a stale isotropic inertia (identity) gives a different, larger effective mass.
        float[] stale = [1, 0, 0, 0, 1, 0, 0, 0, 1];
        stale.CopyTo(dynamic.InvIWorld, 0);
        Assert.InRange(1 / WorkshopConstraintSolver.EffectiveMass(contact, contact.Normal), 2 - 1e-6f, 2 + 1e-6f);
    }
}
