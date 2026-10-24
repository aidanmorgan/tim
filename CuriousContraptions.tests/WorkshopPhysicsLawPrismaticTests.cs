using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.Intrinsics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 9.4b: the prismatic-joint facts of the deleted JS-kernel harness (tools/workshop-rigid-body.test.mjs, the "real WASM" rows),
/// ported to step the shipped C# engine natively or, where the harness isolated one kernel, to call that C# kernel directly. Joints are
/// declared as PrismaticConstraintDeclaration records; nothing here patches joint bytes.
/// </summary>
public sealed class WorkshopPhysicsLawPrismaticTests
{
    private const float Lower = -.25f, Stiffness = 400, Damping = .2f;
    private const float PlateMass = .25f;
    private static readonly Vector3 PlateHalf = new(.65f, .075f, .6f);
    private static readonly Vector3 TileHalf = LawTile.HalfExtents;
    private static readonly LawMaterial PlateMaterial = new(0, .05f, .1f, 0);
    private static ConstraintFrame Frame(float x = 0, float y = 0, float z = 0, Quaternion? rotation = null) =>
        new(new(x, y, z), rotation ?? Quaternion.Identity);
    private static Quaternion Axis(Vector3 axis, double angle) => Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), (float)angle);

    /// <summary>The admitted principal moments (one f32 each).</summary>
    private static Vector3 Moments(LawWorld world, GpuBodyId id)
    {
        var o = world.Scene.Layout.GetBodyOffset(id) + PhysicsGpuAbi.BodyPrincipalInertiaOffset;
        float M(int axis) => BinaryPrimitives.ReadSingleLittleEndian(world.Admitted.AsSpan(o + axis * PhysicsGpuAbi.PrincipalInertiaBytes));
        return new(M(0), M(1), M(2));
    }

    private static float Impulse(LawWorld world, PrismaticConstraintId joint, byte[] state, PrismaticRowKind row) =>
        BinaryPrimitives.ReadSingleLittleEndian(state.AsSpan(world.Scene.Layout.GetPrismaticOffset(joint.Value) +
            PhysicsGpuAbi.PrismaticImpulseOffset + (int)row * 4));

    private static double Rotational(Vector3 moments, LawBody body)
    {
        var local = Vector3.Transform(body.Angular, Quaternion.Conjugate(body.Rotation));
        return .5 * (moments.X * local.X * local.X + moments.Y * local.Y * local.Y + moments.Z * local.Z * local.Z);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(-.1f, 0f)]
    [InlineData(-.24f, 0f)]
    [InlineData(-.2f, -8f)]
    public void PrismaticRowsKeepZeroGravityRestAndDissipateAnUnchargedRelease(float compression, float initialSpeed)
    {
        var scene = new LawScene();
        var frame = scene.StaticBox(new(0, 5, 0), TileHalf);
        var plate = scene.Box(new(0, 5 + compression, 0), PlateHalf, PlateMass, LawMaterial.Domino, velocity: new(0, initialSpeed, 0), gravity: 0);
        var joint = scene.Prismatic(frame, plate, Frame(), Frame(), Lower, 0, Stiffness, Damping, ConnectedCollision.Disabled);
        var world = scene.Admit();
        var initial = world.Initial(plate);
        double Energy(LawBody b) => PlateMass / 2 * b.Speed * b.Speed + Stiffness / 2 * Math.Pow(b.Position.Y - 5, 2);
        var initialEnergy = Energy(initial); var lowerImpulse = 0f;
        world.Step(180, (tick, state) =>
        {
            var b = state[plate]; var energy = Energy(b); var travel = b.Position.Y - 5;
            lowerImpulse = MathF.Max(lowerImpulse, Impulse(world, joint, state.World, PrismaticRowKind.LowerStop));
            Assert.True(energy <= initialEnergy + .005, $"tick {tick}: passive energy {energy} <= {initialEnergy}");
            Assert.InRange(travel, -.251f, .001f);
            Assert.True(MathF.Abs(b.Position.X) < 1e-5f && MathF.Abs(b.Position.Z) < 1e-5f && b.Spin < 1e-5, $"tick {tick}: stays on its axis");
            if (compression == 0) Assert.Equal(0, energy); // an unloaded zero-gravity plate cannot start itself
        });
        if (initialSpeed < 0) Assert.True(lowerImpulse > 0, "incoming motion activates the lower unilateral stop");
    }

    private static Vector3 Rotate(Quaternion q, Vector3 v) => Vector3.Transform(v, q);

    [Fact]
    public void AMovingTiltedPairPreservesReactionMomentumAndBoundedTotalEnergy()
    {
        var rotation = new Quaternion(0, 0, MathF.Sin(.2f), MathF.Cos(.2f));
        var scene = new LawScene();
        var a = scene.Box(new(0, 5, 0), TileHalf, 1, LawMaterial.Domino, rotation, new(1, .3f, 0), new(.2f, .3f, .5f), 0);
        var b = scene.Box(new(0, 5, 0), TileHalf, PlateMass, LawMaterial.Domino, rotation, new(-.5f, -.4f, 0), new(-.2f, .1f, -.3f), 0);
        scene.Prismatic(a, b, Frame(.3f, 0, .2f), Frame(.3f, 0, .2f), Lower, 0, Stiffness, Damping, ConnectedCollision.Disabled);
        var world = scene.Admit();
        world.SetPrincipalMoments(a, Vector3.One); world.SetPrincipalMoments(b, Vector3.One);
        (double Energy, Vector3 Momentum, Vector3 Angular, float Travel) Quantities(LawBody p, LawBody q)
        {
            var axis = Rotate(p.Rotation, Vector3.UnitY);
            var displacement = q.Position + Rotate(q.Rotation, new(.3f, 0, .2f)) - p.Position - Rotate(p.Rotation, new(.3f, 0, .2f));
            var travel = Vector3.Dot(axis, displacement);
            var momentum = p.Velocity + PlateMass * q.Velocity;
            var angular = p.Angular + q.Angular + Vector3.Cross(p.Position, p.Velocity) + Vector3.Cross(q.Position, PlateMass * q.Velocity);
            return (.5 * p.Speed * p.Speed + PlateMass / 2 * q.Speed * q.Speed + .5 * (p.Spin * p.Spin + q.Spin * q.Spin) +
                Stiffness / 2 * travel * travel, momentum, angular, travel);
        }
        var initial = Quantities(world.Initial(a), world.Initial(b));
        var momentumError = 0f; var angularError = 0f;
        world.Step(120, (tick, state) =>
        {
            var current = Quantities(state[a], state[b]);
            Assert.True(current.Energy <= initial.Energy + .01, $"tick {tick}: energy {current.Energy} initial {initial.Energy}");
            Assert.InRange(current.Travel, -.251f, .001f);
            var dm = current.Momentum - initial.Momentum; var dl = current.Angular - initial.Angular;
            momentumError = MathF.Max(momentumError, MathF.Max(MathF.Abs(dm.X), MathF.Max(MathF.Abs(dm.Y), MathF.Abs(dm.Z))));
            angularError = MathF.Max(angularError, MathF.Max(MathF.Abs(dl.X), MathF.Max(MathF.Abs(dl.Y), MathF.Abs(dl.Z))));
        });
        Assert.True(momentumError < .001f, $"momentum {momentumError}");
        Assert.True(angularError < .01f, $"angular momentum {angularError}");
    }

    private static SimBody KernelBody(int slot, float angle, float mass, Vector3 moments, Vector3 position, Vector3 angular)
    {
        var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
        var body = new SimBody
        {
            BodySlot = slot, Motion = 1, Px = position.X, Py = position.Y, Pz = position.Z, Qx = q.X, Qy = q.Y, Qz = q.Z, Qw = q.W,
            Principal = Quaternion.Identity, ComLocal = new(.1f, 0, -.2f), InvMass = 1 / mass, Mass = mass,
            InvI = new(1 / moments.X, 1 / moments.Y, 1 / moments.Z), Vx = .3f, Vy = -.2f, Vz = .1f, Wx = angular.X, Wy = angular.Y, Wz = angular.Z,
        };
        WorkshopCollisionKernels.UpdateDynamicFrame(body);
        return body;
    }

    [Fact]
    public void AnIsolatedAnisotropicRowAppliesBothCurrentFrameReactions()
    {
        // All masses, moments and anchors are exactly representable; the expected inverse inertia is rebuilt from each body's
        // current rotation, independent of the solver's world-inertia cache.
        var a = KernelBody(1, .4f, 1, new(.5f, 1, 2), new(0, 4, 0), new(.2f, .3f, -.4f));
        var b = KernelBody(2, -.7f, .5f, new(2, .25f, 1), new(.2f, 4.1f, -.1f), new(-.3f, .5f, .1f));
        var joint = new SimJoint
        {
            A = a, B = b, Pa = new(.25f, .125f, -.25f), Qa = Quaternion.Identity, Pb = new(-.125f, .25f, .125f), Qb = Quaternion.Identity,
            Lower = Lower, Upper = 0, Soft = SoftConstraintCoefficients.FromSpring(Stiffness, Damping, WorkshopConstraintSolver.SubstepSeconds),
        };
        PrismaticConstraintSolver.PrepareJointRows(joint, WorkshopConstraintSolver.SubstepSeconds);
        var row = joint.Rows[(int)PrismaticRowKind.TransverseX];
        static Vector3 Inverse(SimBody body, Vector3 v)
        {
            var local = Vector3.Transform(v, Quaternion.Conjugate(body.Rotation)) * body.InvI;
            return Vector3.Transform(local, body.Rotation);
        }
        var ia = Inverse(a, row.Aa); var ib = Inverse(b, row.Ab);
        var expected = Vector3.Dot(row.La, row.La) * a.InvMass + Vector3.Dot(row.Lb, row.Lb) * b.InvMass + Vector3.Dot(row.Aa, ia) + Vector3.Dot(row.Ab, ib);
        var actual = PrismaticConstraintSolver.ConstraintMass(a, row.La, row.Aa) + PrismaticConstraintSolver.ConstraintMass(b, row.Lb, row.Ab);
        Assert.True(MathF.Abs(actual - expected) < 2e-6f, $"effective mass {actual} vs {expected}");
        var beforeA = (a.LinearVelocity, a.AngularVelocity); var beforeB = (b.LinearVelocity, b.AngularVelocity);
        const float impulse = .75f;
        PrismaticConstraintSolver.ApplyConstraintImpulse(joint, row, impulse);
        foreach (var (body, before, linear, angular) in new[] { (a, beforeA, row.La, ia), (b, beforeB, row.Lb, ib) })
        {
            Assert.True((body.LinearVelocity - before.LinearVelocity - linear * impulse * body.InvMass).Length() < 2e-5f, "linear reaction");
            Assert.True((body.AngularVelocity - before.AngularVelocity - angular * impulse).Length() < 2e-5f, "angular reaction");
        }
        Assert.True((b.AngularVelocity - beforeB.AngularVelocity).Length() > .01f, "the second body really reacts");
    }

    [Fact]
    public void AGravityFedBallAndPlateContactRespectsCompletePassiveEnergy()
    {
        var scene = new LawScene();
        var frame = scene.StaticBox(new(0, 4.5f, 0), new(.65f, .06f, .6f));
        var plate = scene.Box(new(0, 5, 0), PlateHalf, PlateMass, PlateMaterial);
        var ball = scene.Ball(LawBall.Basketball, new(0, 6.5f, 0));
        scene.Prismatic(frame, plate, Frame(y: .5f), Frame(), Lower, 0, Stiffness, Damping, ConnectedCollision.Disabled);
        var world = scene.Admit();
        var plateI = Moments(world, plate); var ballI = Moments(world, ball); var g = -LawScene.Gravity;
        double Energy(LawBody p, LawBody b)
        {
            Assert.True(p.Tilt < .001f && b.Tilt < .001f, "the centred drop keeps identity orientation");
            return PlateMass / 2 * p.Speed * p.Speed + .5 * LawBalls.Mass(LawBall.Basketball) * b.Speed * b.Speed + Rotational(plateI, p) +
                Rotational(ballI, b) + g * (PlateMass * p.Position.Y + LawBalls.Mass(LawBall.Basketball) * b.Position.Y) +
                Stiffness / 2 * Math.Pow(p.Position.Y - 5, 2);
        }
        var initial = Energy(world.Initial(plate), world.Initial(ball));
        var lowest = 5f; var upward = 0f;
        world.Step(360, (tick, state) =>
        {
            var p = state[plate]; var b = state[ball];
            Assert.True(Energy(p, b) <= initial + .01, $"tick {tick}: complete energy {Energy(p, b)} vs {initial}");
            Assert.InRange(p.Position.Y, 4.749f, 5.001f);
            lowest = MathF.Min(lowest, p.Position.Y); upward = MathF.Max(upward, b.Velocity.Y);
        });
        Assert.True(lowest < 4.85f && upward > 2, $"contact compresses the plate ({lowest}) and returns finite spring work ({upward})");
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(30, 50, 70)]
    [InlineData(90, 0, 0)]
    public void PreservedPassiveBallContactsAlongEachSourceOrientation(int x, int y, int z)
    {
        // Godot's default Euler order YXZ.
        static Quaternion About(Vector3 axis, int degrees) => Quaternion.CreateFromAxisAngle(axis, degrees * MathF.PI / 180);
        var rotation = About(Vector3.UnitY, y) * About(Vector3.UnitX, x) * About(Vector3.UnitZ, z);
        var n = Rotate(rotation, Vector3.UnitY); var origin = new Vector3(0, 4, 0);
        Vector3 At(float d) => origin + d * n;
        var scene = new LawScene();
        var frame = scene.StaticBox(At(-.36f), new(.65f, .06f, .6f), rotation);
        var plate = scene.Box(At(.14f), PlateHalf, PlateMass, PlateMaterial, rotation, gravity: 0);
        var ball = scene.Sphere(At(1), LawBalls.Radius(LawBall.Basketball), LawBalls.Mass(LawBall.Basketball), LawMaterial.Ball(LawBall.Basketball),
            -2 * n, default, 0, 0);
        scene.Prismatic(frame, plate, Frame(y: .5f), Frame(), Lower, 0, Stiffness, 0, ConnectedCollision.Disabled);
        var world = scene.Admit();
        world.SetPrincipalMoments(plate, new(PlateMass * (.15f * .15f + 1.2f * 1.2f) / 12, PlateMass * (1.3f * 1.3f + 1.2f * 1.2f) / 12,
            PlateMass * (1.3f * 1.3f + .15f * .15f) / 12));
        var baseBody = world.Initial(frame); var axis = Rotate(baseBody.Rotation, Vector3.UnitY);
        var inertia = (Plate: Moments(world, plate), Ball: Moments(world, ball));
        (double Energy, float Compression, float Returned) Measure(LawBody p, LawBody b)
        {
            var displacement = Vector3.Dot(p.Position - baseBody.Position, axis) - .5f;
            return (PlateMass / 2 * p.Speed * p.Speed + .5 * b.Speed * b.Speed + Rotational(inertia.Plate, p) + Rotational(inertia.Ball, b) +
                Stiffness / 2 * displacement * displacement, -displacement, Vector3.Dot(b.Velocity, axis));
        }
        var initial = Measure(world.Initial(plate), world.Initial(ball)).Energy;
        var compression = 0f; var returned = 0f;
        world.Step(120, (tick, state) =>
        {
            var m = Measure(state[plate], state[ball]);
            Assert.True(m.Energy >= 0 && m.Energy <= initial + 1e-5, $"({x},{y},{z}) tick {tick}: energy {m.Energy} initial {initial}");
            compression = MathF.Max(compression, m.Compression); returned = MathF.Max(returned, m.Returned);
        });
        Assert.InRange(compression, .01f, .250001f);
        Assert.True(returned > .2f, $"return speed {returned}");
    }

    private static SimBody SweepBody(int slot, int seed)
    {
        var body = new SimBody
        {
            BodySlot = slot, Motion = 1, Px = seed * .1f, Py = 4 + seed * .03f, Pz = -seed * .02f,
            Qy = MathF.Sin(seed * .03f), Qw = MathF.Cos(seed * .03f), Principal = Quaternion.Identity,
            InvMass = 1 / (1 + seed * .125f), InvI = new(1 + seed * .1f, .5f + seed * .05f, .8f + seed * .025f),
            Vx = seed * .07f, Vy = -seed * .03f, Vz = seed * .02f, Wx = seed * .01f, Wy = -seed * .015f, Wz = seed * .02f,
        };
        WorkshopCollisionKernels.UpdateDynamicFrame(body);
        return body;
    }

    private static SimJoint SweepJoint(SimBody a, SimBody b, int seed)
    {
        var joint = new SimJoint
        {
            A = a, B = b, Pa = new(.03f * seed, 0, .01f), Qa = Quaternion.Identity, Pb = new(-.01f, 0, .02f * seed), Qb = Quaternion.Identity,
            Lower = Lower, Upper = 0, Soft = SoftConstraintCoefficients.FromSpring(120 + seed * 50, Damping, WorkshopConstraintSolver.SubstepSeconds),
        };
        PrismaticConstraintSolver.PrepareJointRows(joint, WorkshopConstraintSolver.SubstepSeconds);
        return joint;
    }

    private static float[] State(SimJoint j) =>
        [j.A.Vx, j.A.Vy, j.A.Vz, j.A.Wx, j.A.Wy, j.A.Wz, j.B.Vx, j.B.Vy, j.B.Vz, j.B.Wx, j.B.Wy, j.B.Wz, .. j.Impulses];

    [Fact]
    public void FiveDisjointJointsScatterFullAndPartialSimdBatchesAndSharedBodiesSequence()
    {
        List<SimJoint> Make() => [.. Enumerable.Range(0, 5).Select(i => SweepJoint(SweepBody(i * 2 + 1, i + 1), SweepBody(i * 2 + 2, i + 2), i + 1))];
        var together = Make(); var separate = Make();
        for (var sweep = 0; sweep < 3; sweep++)
        {
            PrismaticConstraintSolver.SweepConstraints(together, true);
            foreach (var j in separate) PrismaticConstraintSolver.SweepConstraints([j], true);
        }
        for (var i = 0; i < 5; i++) Assert.Equal(State(separate[i]), State(together[i]));
        Assert.Equal(5, together.Select(j => string.Join(",", j.Impulses)).Distinct().Count()); // distinct lanes are not broadcast or dropped
        List<SimJoint> Shared() { var common = SweepBody(2, 2); return [SweepJoint(SweepBody(1, 1), common, 1), SweepJoint(common, SweepBody(3, 3), 2)]; }
        var actual = Shared(); var expected = Shared();
        // Independent scalar schedule: each later joint consumes the earlier shared-body reaction, row by row.
        for (var sweep = 0; sweep < 3; sweep++)
        {
            PrismaticConstraintSolver.SweepConstraints(actual, true);
            for (var row = 0; row < PhysicsGpuAbi.PrismaticRowCount; row++)
                foreach (var j in expected)
                {
                    var r = j.Rows[row];
                    var result = SoftConstraint.Solve4(
                        Vector128.Create(PrismaticConstraintSolver.ConstraintMass(j.A, r.La, r.Aa) + PrismaticConstraintSolver.ConstraintMass(j.B, r.Lb, r.Ab), 0, 0, 0),
                        Vector128.Create(PrismaticConstraintSolver.ConstraintVelocity(j.A, r.La, r.Aa) + PrismaticConstraintSolver.ConstraintVelocity(j.B, r.Lb, r.Ab), 0, 0, 0),
                        Vector128.Create(r.Error, 0, 0, 0), Vector128.Create(r.Gamma, 0, 0, 0), Vector128.Create(r.Bias, 0, 0, 0),
                        Vector128.Create(j.Impulses[row], 0, 0, 0), Vector128.Create(r.Minimum, 0, 0, 0), Vector128.Create(float.MaxValue, 0, 0, 0));
                    j.Impulses[row] = result.Accumulated.GetElement(0);
                    PrismaticConstraintSolver.ApplyConstraintImpulse(j, r, result.Delta.GetElement(0));
                }
        }
        for (var i = 0; i < 2; i++) Assert.Equal(State(expected[i]), State(actual[i]));
    }

    [Fact]
    public void TheConnectedCollisionPolicyFiltersOnlyItsDeclaredPair()
    {
        (LawBody Plate, LawBody Other) Run(ConnectedCollision policy)
        {
            var scene = new LawScene();
            var frame = scene.StaticBox(new(0, 5, 0), new(.65f, .05f, .6f));
            var plate = scene.Box(new(0, 5, 0), PlateHalf, PlateMass, LawMaterial.Domino, gravity: 0);
            scene.StaticBox(new(3, 5, 0), new(.5f, .1f, .5f));
            var ball = scene.Sphere(new(3, 5.3f, 0), LawBalls.Radius(LawBall.Basketball), LawBalls.Mass(LawBall.Basketball),
                LawMaterial.Ball(LawBall.Basketball), new(0, -2, 0), default, 0, 0);
            scene.Prismatic(frame, plate, Frame(), Frame(), Lower, 0, Stiffness, 0, policy);
            var after = scene.Admit().Step(1);
            return (after[plate], after[ball]);
        }
        var disabled = Run(ConnectedCollision.Disabled); var enabled = Run(ConnectedCollision.Enabled);
        Assert.Equal(0, disabled.Plate.Speed);
        Assert.True(enabled.Plate.Speed > .001 || MathF.Abs(enabled.Plate.Position.Y - 5) > 1e-5f, "an enabled connected overlap has a physical response");
        Assert.Equal(disabled.Other, enabled.Other); // the unrelated collision is retained under both policies
        Assert.True(disabled.Other.Velocity.Y > 0, "the unrelated ball really collides");
    }

    [Theory]
    [InlineData(179.5)]
    [InlineData(180.5)]
    public void CompoundLocalFramesRestoreAcrossTheShortestArcBoundary(double degrees)
    {
        var la = Axis(new(1, 2, 0), .4); var lb = Axis(new(0, 1, 2), -.3); var qa = Axis(new(2, -1, 1), .5);
        var results = new List<(float Angle, double MaximumSpin)>();
        foreach (var sign in new[] { 1f, -1f })
        {
            var qb = Axis(new(1, 2, -3), degrees * Math.PI / 180) * qa * la * Quaternion.Conjugate(lb);
            qb = new(qb.X * sign, qb.Y * sign, qb.Z * sign, qb.W * sign);
            var scene = new LawScene();
            var frame = scene.StaticBox(new(0, 5, 0), TileHalf, qa);
            var body = scene.Box(new(0, 5, 0), TileHalf, PlateMass, LawMaterial.Domino, qb, gravity: 0);
            scene.Prismatic(frame, body, Frame(rotation: la), Frame(rotation: lb), Lower, 0, Stiffness, 0, ConnectedCollision.Disabled);
            var world = scene.Admit();
            world.SetPrincipalMoments(body, new(.5f, .75f, 1));
            var maximumSpin = 0.0;
            var final = world.Step(120, (tick, state) =>
            {
                var b = state[body];
                Assert.True(b.Finite && b.Spin < 128, $"tick {tick}: spin {b.Spin}");
                maximumSpin = Math.Max(maximumSpin, b.Spin);
            })[body];
            var relative = final.Rotation * lb * Quaternion.Conjugate(world.Initial(frame).Rotation * la);
            var angle = 2 * MathF.Atan2(new Vector3(relative.X, relative.Y, relative.Z).Length(), MathF.Abs(relative.W));
            Assert.True(angle < .01f, $"restoration {degrees}/{sign}: {angle}");
            results.Add((angle, maximumSpin));
        }
        Assert.True(MathF.Abs(results[0].Angle - results[1].Angle) < 1e-5f && Math.Abs(results[0].MaximumSpin - results[1].MaximumSpin) < 1e-4,
            "both quaternion signs restore identically");
    }

    [Theory]
    [InlineData(0, .2f, true, 0f)]
    [InlineData(0, 0f, true, 0f)]
    [InlineData(0, .2f, true, 2f)]
    [InlineData(0, .2f, false, 0f)]
    [InlineData(-30, .2f, true, 0f)]
    [InlineData(-30, 0f, true, 0f)]
    [InlineData(-30, .2f, true, 2f)]
    [InlineData(-30, .2f, false, 0f)]
    public void PreloadTransfersOnlyFiniteStoredWorkToContactingPayloadsInUprightAndTiltedFrames(int degrees, float compression, bool payload,
        float miss)
    {
        var rotation = new Quaternion(0, 0, MathF.Sin(degrees * MathF.PI / 360), MathF.Cos(degrees * MathF.PI / 360));
        var origin = new Vector3(0, 2.5f, 0); var axis = Rotate(rotation, Vector3.UnitY);
        Vector3 At(float d) => origin + d * axis;
        var scene = new LawScene();
        // The declared base collider sits 0.36 m below its authored origin.
        var frame = scene.StaticBox(origin, new(.65f, .06f, .6f), rotation, colliderOffset: new(0, -.36f, 0));
        var plate = scene.Box(At(.14f - compression), PlateHalf, PlateMass, PlateMaterial, rotation, gravity: 0);
        GpuBodyId? ball = payload ? scene.Sphere(At(.557f - compression) + new Vector3(miss, 0, 0), LawBalls.Radius(LawBall.Basketball),
            LawBalls.Mass(LawBall.Basketball), LawMaterial.Ball(LawBall.Basketball), default, default, 0, 0) : null;
        scene.Prismatic(frame, plate, Frame(y: .14f), Frame(), Lower, 0, Stiffness, Damping, ConnectedCollision.Disabled);
        var world = scene.Admit();
        var inertia = (Plate: Moments(world, plate), Ball: ball is { } id ? Moments(world, id) : default);
        (double Energy, float Travel, double Speed, float Normal) Measure(LawState? state)
        {
            LawBody Read(GpuBodyId body) => state is null ? world.Initial(body) : state[body];
            var b = Read(frame); var p = Read(plate);
            // The shipped f32 geometric spring row, not a nominal authored-energy ledger.
            Span<PrismaticRow> rows = stackalloc PrismaticRow[PrismaticRows.Count];
            PrismaticRows.Prepare(new(b.Position, b.Rotation, b.Position), new(p.Position, p.Rotation, p.Position), Frame(y: .14f), Frame(),
                Lower, 0, rows);
            var q = rows[(int)PrismaticRowKind.Spring].Error;
            var energy = Stiffness / 2 * q * q + PlateMass / 2 * p.Speed * p.Speed + Rotational(inertia.Plate, p);
            if (ball is not { } payloadId) return (energy, q, 0, 0);
            var load = Read(payloadId);
            return (energy + .5 * load.Speed * load.Speed + Rotational(inertia.Ball, load), q, load.Speed, Vector3.Dot(load.Velocity, axis));
        }
        var initial = Measure(null);
        Assert.InRange(initial.Energy, (compression == 0 ? 0 : 8) - 1e-5, (compression == 0 ? 0 : 8) + 1e-5);
        byte[]? previous = null;
        for (var replay = 0; replay < 2; replay++)
        {
            var speed = 0.0; var normal = 0f;
            var final = world.Step(120, (tick, state) =>
            {
                var value = Measure(state);
                Assert.True(value.Energy <= initial.Energy + 1e-5, $"tick {tick}: energy {value.Energy} initial {initial.Energy}");
                Assert.InRange(value.Travel, -.251f, .001f);
                speed = Math.Max(speed, value.Speed); normal = MathF.Max(normal, value.Normal);
            });
            if (payload && compression > 0 && miss == 0) Assert.True(normal > 1, $"the charged plate launches its payload ({normal})");
            else Assert.Equal(0, speed); // uncharged or missed payloads receive no work; an empty plate has no payload
            if (previous is not null) Assert.Equal(previous, final.World); // the replay is identical
            previous = final.World;
        }
    }
}
