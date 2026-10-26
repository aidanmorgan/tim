using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>
/// engine-hollow-shapes (owner decision "true hollow shapes", 25 Oct 2026): the Tube, Frustum and Torus collider kinds, proven on the
/// shipped C# engine (WorkshopPhysicsEngine) through real admission (PhysicsSceneDeclaration → PhysicsGpuAbi.Admission) and tick
/// progression. Every resting height is derived in closed form from the declared f32 geometry.
/// <para>Near-origin fixtures retain the 10 micrometre analytic target and 0.5 mm resting penetration.
/// These fixtures do not establish a universal rounding bound at the admitted dimension or world-coordinate extrema.</para>
/// </summary>
public sealed class WorkshopPhysicsLawHollowTests
{
    private static readonly float BenchY = LawScene.BenchY;
    /// <summary>The surface-error bound of every hollow kind (m).</summary>
    public const double SurfaceError = 1e-5;
    /// <summary>The envelope's resting penetration (m).</summary>
    public const double Resting = 5e-4;
    private static readonly float Rb = LawBalls.Radius(LawBall.Basketball);
    /// <summary>The pipe family's documented material: restitution 0.15, bounce threshold 0.1, friction 0.3.</summary>
    private static readonly LawMaterial Pipe = new(.15f, .1f, .3f, 0f);
    private static Quaternion About(Vector3 axis, double degrees) => Quaternion.CreateFromAxisAngle(axis, (float)(degrees * Math.PI / 180));
    /// <summary>A tube or frustum lying with its axis along world X.</summary>
    private static readonly Quaternion AlongX = About(Vector3.UnitZ, -90);


    // A segment ends in a flat disc, not a hemispherical cap. Both partners must
    // settle on y = c.Y with their bottom face there, and fall freely beside it.
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(true, false, false, 17f)]
    [InlineData(true, true, false, 17f)]
    [InlineData(true, false, true, 17f)]
    [InlineData(true, true, true, 17f)]
    public void ASolidPartnerMeetsTheFlatTorusEndWithoutInventedCap(bool cylinder, bool beside, bool reverseOrder = false, float yawDegrees = 0f)
    {
        var scene = new LawScene();
        var c = new Vector3(0, BenchY + 3, 0);
        void Wall() => scene.StaticHollow(ColliderShapeKind.Torus, c, About(Vector3.UnitX, 90),
            1f, new(0f, .3f, MathF.PI / 2), Pipe);
        if (!reverseOrder) Wall();
        var start = c + new Vector3(beside ? 1.6f : 1f, .15f, 0);
        var material = new LawMaterial(0f, .1f, .3f, 0f);
        var body = cylinder
            ? scene.Cylinder(start, .1f, .1f, 1f, material, About(Vector3.UnitY, yawDegrees))
            : scene.Box(start, new(.1f), 1f, material);
        if (reverseOrder) Wall();
        var world = scene.Admit();
        var final = world.Step(world.TicksPerSecond)[body];
        if (beside)
        {
            Assert.True(final.Position.Y < c.Y - .5f, "the separated partner acquired phantom support");
            Assert.InRange(final.Position.X, start.X - 1e-5f, start.X + 1e-5f);
        }
        else
        {
            Assert.InRange(final.Position.Y, c.Y + .1f - Resting - SurfaceError, c.Y + .1f + SurfaceError);
            Assert.True(final.Speed < 1e-3, $"the partner did not settle on the flat end: {final.Position}, {final.Velocity}, spin {final.Angular}");
        }
    }


    [Theory]
    [InlineData(SimulationCadence.Hz60, false)]
    [InlineData(SimulationCadence.Hz120, false)]
    [InlineData(SimulationCadence.Hz240, false)]
    [InlineData(SimulationCadence.Hz60, true)]
    [InlineData(SimulationCadence.Hz120, true)]
    [InlineData(SimulationCadence.Hz240, true)]
    [InlineData(SimulationCadence.Hz60, false, 17f)]
    [InlineData(SimulationCadence.Hz240, false, 17f)]
    [InlineData(SimulationCadence.Hz60, true, 17f)]
    [InlineData(SimulationCadence.Hz240, true, 17f)]
    [InlineData(SimulationCadence.Hz120, false, 17f)]
    [InlineData(SimulationCadence.Hz120, true, 17f)]
    public void ACylinderRemainsAtRestOnTheFlatTorusEnd(SimulationCadence cadence, bool reverseOrder, float yawDegrees = 0f)
    {
        var scene = new LawScene();
        var centre = new Vector3(0, BenchY + 3, 0);
        void Wall() => scene.StaticHollow(ColliderShapeKind.Torus, centre, About(Vector3.UnitX, 90),
            1f, new(0f, .3f, MathF.PI / 2), Pipe);
        if (!reverseOrder) Wall();
        var body = scene.Cylinder(centre + new Vector3(1f, .15f, 0f), .1f, .1f, 1f, new(0f, .1f, .3f, 0f),
            About(Vector3.UnitY, yawDegrees));
        if (reverseOrder) Wall();
        var world = scene.Admit(cadence);
        var landing = new RestingObservation(.2f, world.Initial(body).Position);
        RestingObservation? resting = null;
        var settledRotation = Quaternion.Identity;
        // Eight actual cap-rim directions isolate rotation from centre translation.
        Vector3[] surfaceDirections =
        [
            new(.1f, .1f, 0), new(-.1f, .1f, 0), new(0, .1f, .1f), new(0, .1f, -.1f),
            new(.1f, -.1f, 0), new(-.1f, -.1f, 0), new(0, -.1f, .1f), new(0, -.1f, -.1f),
        ];
        var circumscribedRadius = MathF.Sqrt(.1f * .1f + .1f * .1f);
        world.Step(10 * world.TicksPerSecond, (tick, state) =>
        {
            PhysicsGpuAbi.ValidateCandidate(state.World, world.Admitted, new SimulationTick((ulong)tick));
            var observed = state[body];
            // A purely vertical symmetric drop must not acquire a hidden lateral landing offset.
            landing.Observe(observed.Position);
            Assert.True(landing.IsResting,
                $"{cadence}, yaw {yawDegrees}, tick {tick}: initial-placement offset {landing.PeakMetres} m");
            if (tick < world.TicksPerSecond) return;
            if (tick == world.TicksPerSecond)
            {
                // Diameter 0.2 m gives the preselected 20 micrometre allowance.
                resting = new RestingObservation(.2f, observed.Position);
                settledRotation = observed.Rotation;
            }
            Assert.NotNull(resting);
            Assert.True(observed.Speed < 1e-3,
                $"{cadence}, yaw {yawDegrees}, tick {tick}: linear speed {observed.Speed} m/s");
            Assert.True(observed.Spin * circumscribedRadius < 1e-3,
                $"{cadence}, yaw {yawDegrees}, tick {tick}: rotational surface speed {observed.Spin * circumscribedRadius} m/s");
            foreach (var direction in surfaceDirections)
            {
                var displacement = Vector3.Distance(Vector3.Transform(direction, observed.Rotation),
                    Vector3.Transform(direction, settledRotation));
                Assert.True(displacement <= resting.AllowanceMetres,
                    $"{cadence}, yaw {yawDegrees}, tick {tick}: rotational surface displacement {displacement} m");
            }
            resting.Observe(observed.Position);
            Assert.True(resting.IsResting,
                $"{cadence}, reverse {reverseOrder}, tick {tick}: excursion {resting.PeakMetres} m, allowance {resting.AllowanceMetres} m, resolved {resting.Resolved}");
            Assert.InRange(observed.Position.Y, centre.Y + .1f - Resting - SurfaceError, centre.Y + .1f + SurfaceError);
        });
    }

    [Fact]
    public void ARotatedCapRepeatsItsTypedTrajectoryAfterEngineResetAndOnAFreshEngine()
    {
        var scene = new LawScene();
        var centre = new Vector3(0, BenchY + 3, 0);
        scene.StaticHollow(ColliderShapeKind.Torus, centre, About(Vector3.UnitX, 90),
            1f, new(0f, .3f, MathF.PI / 2), Pipe);
        var body = scene.Cylinder(centre + new Vector3(1f, .15f, 0), .1f, .1f, 1f,
            new(0f, .1f, .3f, 0f), About(Vector3.UnitY, 17));
        var world = scene.Admit();
        var engine = new CuriousContraptions.Simulation.WorkshopPhysicsEngine();
        var original = Observe(engine);
        // Step resets the existing engine and starts from the admitted construction.
        var reset = Observe(engine);
        var fresh = Observe(new CuriousContraptions.Simulation.WorkshopPhysicsEngine());
        foreach (var replay in new[] { reset, fresh })
            for (var tick = 0; tick < original.Length; tick++)
            {
                var expected = original[tick];
                var actual = replay[tick];
                Assert.Equal(expected.Body.Id, actual.Body.Id);
                Assert.Equal(expected.Body.Epoch, actual.Body.Epoch);
                Assert.Equal(expected.Body.Tick, actual.Body.Tick);
                Assert.Equal(expected.Body.Cell, actual.Body.Cell);
                Assert.Equal(expected.Body.Local, actual.Body.Local);
                Assert.Equal(expected.Rotation, actual.Rotation);
                Assert.Equal(expected.Body.Velocity, actual.Body.Velocity);
                Assert.Equal(expected.AngularVelocity, actual.AngularVelocity);
            }

        PhysicsBodyRead[] Observe(CuriousContraptions.Simulation.WorkshopPhysicsEngine target)
        {
            var trajectory = new PhysicsBodyRead[10 * world.TicksPerSecond];
            world.Step(trajectory.Length, (tick, state) =>
            {
                var expectedTick = new SimulationTick((ulong)tick);
                var committed = PhysicsGpuAbi.ValidateCandidate(state.World, world.Admitted, expectedTick);
                Assert.True(committed.Bodies.TryGet(body, out var observed));
                Assert.Equal(body, observed.Body.Id);
                Assert.Equal(expectedTick, new SimulationTick(observed.Body.Tick));
                trajectory[tick - 1] = observed;
            }, engine: target);
            return trajectory;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TiltedTorusCrestCandidatesRemainOnTheMeridianSurface(bool contact)
    {
        var a = MathF.PI / 3;
        var radial = new Vector3(MathF.Cos(a), 0, MathF.Sin(a));
        var tangent = new Vector3(-MathF.Sin(a), 0, MathF.Cos(a));
        var n = .19f * tangent + MathF.Sqrt(1 - .19f * .19f) * Vector3.UnitY;
        var hollow = new HollowSolid(ColliderShapeKind.Torus, Vector3.Zero, Vector3.UnitX,
            Vector3.UnitY, Vector3.UnitZ, 2f, .699f, .7f, MathF.PI);
        var centre = 2 * radial - .7f * (contact ? Vector3.UnitY : n);
        var partner = new HollowPartner(ColliderShapeKind.Box, centre, radial, n,
            Vector3.Cross(radial, n), 0, .001f, .001f, .001f);
        // For the clear box every point lies within sqrt(3)*.001 of its centre.
        // Its centre's distance from the ring centreline is independently .68727,
        // leaving > .009 m to the .699 bore even after the box's bounding radius.
        var rho = Math.Sqrt((double)centre.X * centre.X + (double)centre.Z * centre.Z);
        var tubeDistance = Math.Sqrt((rho - 2) * (rho - 2) + (double)centre.Y * centre.Y);
        if (!contact) Assert.True(tubeDistance + Math.Sqrt(3) * .001 < .699);
        Assert.Equal(contact, HollowContactQuery.Overlaps(partner, hollow, 0f));
    }

    [Theory]
    [InlineData(.66f, 0f)]
    [InlineData(.75f, 0f)]
    [InlineData(.34f, 0f)]
    [InlineData(.34f, .4f)]
    [InlineData(.66f, 0f, .3f, false)]
    [InlineData(.66f, 0f, .3f, true)]
    [InlineData(.75f, 0f, .3f, false)]
    [InlineData(.75f, 0f, .3f, true)]
    [InlineData(.34f, 0f, .3f, false)]
    [InlineData(.34f, 0f, .3f, true)]
    [InlineData(.34f, .4f, .3f, false)]
    [InlineData(.34f, .4f, .3f, true)]
    [InlineData(.66f, 0f, 0f, false, true)]
    [InlineData(.75f, 0f, 0f, false, true)]
    [InlineData(.34f, 0f, 0f, false, true)]
    [InlineData(.34f, .4f, 0f, false, true)]
    [InlineData(.66f, 0f, .3f, false, true)]
    [InlineData(.66f, 0f, .3f, true, true)]
    [InlineData(.75f, 0f, .3f, false, true)]
    [InlineData(.75f, 0f, .3f, true, true)]
    [InlineData(.34f, 0f, .3f, false, true)]
    [InlineData(.34f, 0f, .3f, true, true)]
    [InlineData(.34f, .4f, .3f, false, true)]
    [InlineData(.34f, .4f, .3f, true, true)]
    public void BareSegmentMouthSupportsOversizePayloadsAndPassesFittingPayloads(float radius, float offset,
        float friction = 0f, bool reverseOrder = false, bool mirrored = false)
    {
        var c = new Vector3(0, 4, 0);
        var sign = mirrored ? -1f : 1f;
        // Reflect the mouth across Z=0. Its circular section is symmetric in Y,
        // so a half-turn about X represents the reflected quarter torus exactly.
        var turn = mirrored ? new Quaternion(1, 0, 0, 0) : Quaternion.Identity;
        LawBody Canonical(LawBody body) => body with
        {
            Position = new(body.Position.X, body.Position.Y, sign * body.Position.Z),
            Velocity = new(body.Velocity.X, body.Velocity.Y, sign * body.Velocity.Z),
            Angular = new(sign * body.Angular.X, sign * body.Angular.Y, body.Angular.Z)
        };
        var scene = new LawScene();
        void Mouth() => scene.StaticHollow(ColliderShapeKind.Torus, c, turn, 2.4f,
            new(.65f, .7f, MathF.PI / 2), new(0, .1f, friction, 0));
        if (!reverseOrder) Mouth();
        var start = c + new Vector3(2.4f, offset, -.5f);
        var ball = scene.Sphere(new(start.X, start.Y, sign * start.Z), radius, 1, new(0, .1f, friction, 0),
            sign * Vector3.UnitZ, default, 0, 0);
        if (reverseOrder) Mouth();
        var world = scene.Admit();
        var end = Canonical(world.Step(world.TicksPerSecond / 2, (_, state) =>
        {
            if (radius <= .65f) return;
            var body = Canonical(state[ball]);
            // Exact distance to the circular mouth, not axial displacement:
            // a grazing normal amplifies the permitted axial slop.
            var relative = body.Position - (c + new Vector3(2.4f, 0, 0));
            var radial = Math.Sqrt((double)relative.X * relative.X + (double)relative.Y * relative.Y);
            var rimDistance = Math.Sqrt((radial - .65) * (radial - .65) + (double)relative.Z * relative.Z);
            Assert.True(radius - rimDistance <= Resting + SurfaceError, $"rim penetration {radius - rimDistance}");
            Assert.True(relative.Z < 0, "oversize sphere crossed the mouth");
            var energy = .5 * body.Speed * body.Speed + .5 * (.4 * radius * radius) * body.Spin * body.Spin;
            Assert.InRange(energy, 0, .5 + 9.81 * .0002);
        })[ball]);
        if (radius > .65f)
        {
            var tangent = -Math.Sqrt((double)radius * radius - .65 * .65);
            if (friction > 0f)
            {
                // Same axial progress lower bound as the zero-friction control:
                // normal-space slop/energy/no-crossing were checked at every tick.
                Assert.InRange(end.Position.Z - c.Z, tangent - 1.0 / 120 - SurfaceError, 0);
                return;
            }
            // A grazing rim converts normal slop into a larger axial displacement.
            // Normal penetration and no-crossing are checked above at every tick;
            // retain the one-tick speculative progress lower bound independently.
            Assert.InRange(end.Position.Z, tangent - 1.0 / 120 - SurfaceError, 0);
            Assert.True(Vector3.Distance(new(end.Position.X, end.Position.Y, 0), new(start.X, start.Y, 0)) <= Resting + SurfaceError,
                $"transverse drift at {end.Position}, velocity {end.Velocity}");
            Assert.True(end.Speed < .003, $"jam speed {end.Speed}, at {end.Position}");
        }
        else if (offset == 0)
        {
            Assert.InRange(end.Position.Z, -SurfaceError, SurfaceError);
            Assert.InRange(end.Position.Y, start.Y - SurfaceError, start.Y + SurfaceError);
            Assert.InRange(end.Speed, 1 - SurfaceError, 1 + SurfaceError);
        }
        else
        {
            Assert.True(end.Position.Y < start.Y - .01f, "an offset payload did not meet the upper rim");
            Assert.True(end.Velocity.Y < 0);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void BroadFacesRestOnAFullTorusCrest(bool cylinder, bool clear)
    {
        var c = new Vector3(0, 4, 0);
        var scene = new LawScene();
        scene.StaticHollow(ColliderShapeKind.Torus, c, Quaternion.Identity, .7f,
            new(.15f, .2f, ColliderDeclaration.FullSweep), new(0, .1f, .3f, 0));
        var at = c + new Vector3(clear ? 3f : 0, .35f, 0);
        var body = cylinder ? scene.Cylinder(at, 1f, .1f, 1, new(0, .1f, .3f, 0))
            : scene.Box(at, new(1f, .1f, 1f), 1, new(0, .1f, .3f, 0));
        var world = scene.Admit();
        var end = world.Step(world.TicksPerSecond)[body];
        if (clear) Assert.True(end.Position.Y < c.Y - .5f);
        else
        {
            Assert.InRange(end.Position.Y, c.Y + .3 - Resting - SurfaceError, c.Y + .3 + SurfaceError);
            Assert.InRange((Vector3.Transform(Vector3.UnitY, end.Rotation) - Vector3.UnitY).Length(), 0, Resting);
            Assert.True(end.Speed < .001);
            Assert.True(end.Angular.Length() < .001);
        }
    }

    [Theory]
    [InlineData(ColliderShapeKind.Tube, false, false)]
    [InlineData(ColliderShapeKind.Tube, true, false)]
    [InlineData(ColliderShapeKind.Tube, false, true)]
    [InlineData(ColliderShapeKind.Tube, true, true)]
    [InlineData(ColliderShapeKind.Frustum, false, false)]
    [InlineData(ColliderShapeKind.Frustum, true, false)]
    [InlineData(ColliderShapeKind.Frustum, false, true)]
    [InlineData(ColliderShapeKind.Frustum, true, true)]
    public void ThinWallSupportsAdvancingTiltedPartnersBetweenTheirVertices(ColliderShapeKind kind, bool cylinder, bool clear)
    {
        // A long diagonal partner spans two narrow annulus patches. Its vertices
        // and axis midpoint miss the wall; contact occurs between those samples.
        var scene = new LawScene();
        var c = new Vector3(0, 3, 0);
        var thickness = ColliderDeclaration.MinimumThickness;
        scene.StaticHollow(kind, c, Quaternion.Identity, kind == ColliderShapeKind.Tube ? .7f : .5f,
            kind == ColliderShapeKind.Tube ? new(.7f - thickness, .5f, 0) : new(thickness, .5f, .7f), new(0, .1f, 0, 0));
        var yaw = About(Vector3.UnitY, 37);
        var turn = cylinder ? yaw * About(Vector3.UnitZ, 90) : yaw;
        var height = cylinder ? .0625f : .005f;
        var start = c + new Vector3(clear ? 3 : 0, .5f + height + .03f, 0);
        var id = cylinder ? scene.Cylinder(start, .0625f, 1f, 1, new(0, .1f, 0, 0), turn)
            : scene.Box(start, new(1f, .005f, .005f), 1, new(0, .1f, 0, 0), turn);
        var world = scene.Admit();
        var end = world.Step(world.TicksPerSecond)[id];
        if (clear) Assert.True(end.Position.Y < c.Y - .5f);
        else
        {
            Assert.InRange(end.Position.Y, c.Y + .5 + height - Resting - SurfaceError, c.Y + .5 + height + SurfaceError);
            Assert.True(end.Speed < .001);
            Assert.InRange(Vector3.Distance(end.Position, new(start.X, c.Y + .5f + height, start.Z)), 0, Resting + SurfaceError);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShortSweepTorusIntersectsBetweenBoxEdgeSamples(bool clear)
    {
        // The arc occupies z=0..0.0625 at R=2, while a 2 m box edge's
        // samples are 0.25 m apart. The box crosses at sweep/2.
        var halfSweep = 1f / 64;
        var radial = new Vector3(MathF.Cos(halfSweep), 0, MathF.Sin(halfSweep));
        var tangent = new Vector3(-radial.Z, 0, radial.X);
        var hollow = new HollowSolid(ColliderShapeKind.Torus, Vector3.Zero, Vector3.UnitX,
            Vector3.UnitY, Vector3.UnitZ, 2, .699f, .7f, 2 * halfSweep);
        var centre = 2 * radial + .11f * tangent + new Vector3(0, clear ? .702f : .7f, 0);
        var box = new HollowPartner(ColliderShapeKind.Box, centre, tangent, Vector3.UnitY, radial, 0, 1, .001f, .001f);
        Assert.Equal(!clear, HollowContactQuery.Overlaps(box, hollow, 0));
    }

    [Fact]
    public void MinimumThicknessRimKeepsDistinctInnerAndOuterNormals()
    {
        var thickness = ColliderDeclaration.MinimumThickness;
        var hollow = new HollowSolid(ColliderShapeKind.Tube, Vector3.Zero, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ,
            .7f, .7f - thickness, .5f, 0);
        var box = new HollowPartner(ColliderShapeKind.Box, new(.7f - thickness / 2, .5f, 0),
            Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, 0, .001f, .002f, .001f);
        Span<HollowContact> contacts = stackalloc HollowContact[HollowContactQuery.Capacity];
        var count = HollowContactQuery.Measure(box, hollow, .001f, contacts);
        var inner = false;
        var outer = false;
        foreach (var point in contacts[..count])
        {
            inner |= point.Normal.X < -.9f;
            outer |= point.Normal.X > .9f;
        }
        Assert.True(inner && outer, "nearby inner and outer rim features lost their distinct normals");
    }

    [Theory]
    [InlineData(.9f)]
    [InlineData(.75f)]
    [InlineData(1.05f)]
    [InlineData(1.2f)]
    public void SpheresMeetTheFrustumEndFaceAndBothRims(float x)
    {
        var scene = new LawScene();
        var c = new Vector3(0, 3, 0);
        scene.StaticHollow(ColliderShapeKind.Frustum, c, Quaternion.Identity, .6f, new(.2f, 1f, 1f), new(0, .1f, 0, 0));
        var ball = scene.Sphere(new(x, 4.2f, 0), .1f, 1, new(0, .1f, 0, 0), new(0, -.2f, 0), default, 0, 0);
        var world = scene.Admit();
        LawBody? touched = null;
        var end = world.Step(world.TicksPerSecond, (_, state) =>
        {
            if (state[ball].Velocity.Y > -.19f) touched ??= state[ball];
        })[ball];
        if (x == 1.2f)
        {
            Assert.Null(touched);
            // 480 f32 updates at |y| < 8; 2^-21 m per update bounds
            // both the small velocity-times-step product and position addition.
            const double accumulated = 480.0 / (1 << 21);
            Assert.InRange(end.Position.Y, 4 - accumulated, 4 + accumulated);
            Assert.Equal(new Vector3(0, -.2f, 0), end.Velocity);
        }
        else
        {
            Assert.NotNull(touched);
            var hit = touched.Value;
            if (x == .9f)
            {
                Assert.InRange(end.Position.Y, 4.1 - Resting - SurfaceError, 4.104 + .2 / 480 + SurfaceError);
                Assert.InRange(end.Velocity.X, -SurfaceError, SurfaceError);
            }
            else
            {
                var rim = x < .9f ? .8 : 1;
                var distance = Math.Sqrt(Math.Pow(hit.Position.X - rim, 2) + Math.Pow(hit.Position.Y - 4, 2));
                Assert.InRange(distance, .1 - Resting - SurfaceError, .104 + .2 / 480 + SurfaceError);
                Assert.True(x < .9f ? hit.Velocity.X < 0 : hit.Velocity.X > 0);
            }
        }
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TiltedCylinderSideDistinguishesAOneMillimetreClearance(bool clear)
    {
        var scene = new LawScene();
        var c = new Vector3(0, 3, 0);
        scene.StaticHollow(ColliderShapeKind.Tube, c, Quaternion.Identity, .7f, new(.65f, 2, 0), new(0, .1f, 0, 0));
        // Rotation about X leaves the cylinder's radial X extent exactly .1.
        // Thus x=.801 is separated by .001 for every point, regardless of Z travel.
        var start = c + new Vector3(clear ? .801f : .799f, 0, -.05f);
        var id = scene.Cylinder(start, .1f, .5f, 1, new(0, .1f, 0, 0), About(Vector3.UnitX, 20),
            velocity: new(0, 0, .1f), gravity: 0);
        var world = scene.Admit();
        var end = world.Step(world.TicksPerSecond)[id];
        if (clear)
        {
            Assert.Equal(new Vector3(0, 0, .1f), end.Velocity);
            Assert.InRange(end.Position.X, start.X - SurfaceError, start.X + SurfaceError);
            Assert.InRange(end.Position.Z, .05 - 480.0 / (1 << 24), .05 + 480.0 / (1 << 24));
        }
        else Assert.True(end.Position.X > start.X + .0001f, "the penetrating tilted side acquired no separating response");
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AdvancingBoxMeetsAShortTorusArcBetweenItsEdgeSamples(bool clear)
    {
        var halfSweep = 1f / 64;
        var radial = new Vector3(MathF.Cos(halfSweep), 0, MathF.Sin(halfSweep));
        var tangent = new Vector3(-radial.Z, 0, radial.X);
        var c = new Vector3(0, 3, 0);
        var scene = new LawScene();
        scene.StaticHollow(ColliderShapeKind.Torus, c, Quaternion.Identity, 2, new(.699f, .7f, 2 * halfSweep), new(0, .1f, 0, 0));
        var turn = About(Vector3.UnitY, -90 - halfSweep * 180 / MathF.PI);
        var start = c + (clear ? 3 : 2) * radial + .11f * tangent + new Vector3(0, .706f, 0);
        var box = scene.Box(start, new(1, .005f, .005f), 1, new(0, .1f, 0, 0), turn,
            velocity: new(0, -.1f, 0), gravity: 0);
        var world = scene.Admit();
        var end = world.Step(world.TicksPerSecond / 10)[box];
        if (clear) Assert.Equal(new Vector3(0, -.1f, 0), end.Velocity);
        else
        {
            Assert.True(end.Velocity.Y > -.09f, $"short arc did not stop the advancing face: {end.Velocity}");
            Assert.True(end.Angular.Length() > .001f, "off-centre support applied no moment");
        }
    }


    [Fact]
    public void FlatTorusEndKeepsItsPlaneNormalAtMicrometreSeparation()
    {
        var c = new Vector3(0, BenchY + 3, 0);
        var turn = About(Vector3.UnitX, 90);
        var hollow = new HollowSolid(ColliderShapeKind.Torus, c, Vector3.Transform(Vector3.UnitX, turn),
            Vector3.Transform(Vector3.UnitY, turn), Vector3.Transform(Vector3.UnitZ, turn), 1, 0, .3f, MathF.PI / 2);
        for (var i = 0; i < 16; i++)
        {
            var a = i * MathF.Tau / 16;
            var p = c + new Vector3(1 + .1f * MathF.Cos(a), 1e-6f, .1f * MathF.Sin(a));
            var distance = hollow.Distance(p, out _, out var normal, out _);
            Assert.InRange(distance, 0, 2e-6f);
            Assert.True((normal - Vector3.UnitY).Length() < 1e-5f, $"normal {normal} at {p}, gap {distance}");
        }
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TiltedCylinderSettlesFromPartialCapContactOnTheTorusEnd(bool clear)
    {
        var scene = new LawScene();
        var c = new Vector3(0, BenchY + 3, 0);
        scene.StaticHollow(ColliderShapeKind.Torus, c, About(Vector3.UnitX, 90), 1,
            new(0, .3f, MathF.PI / 2), new(0, .1f, .3f, 0));
        var body = scene.Cylinder(c + new Vector3(clear ? 1.6f : 1.2f, .17f, 0), .1f, .1f, 1,
            new(0, .1f, .3f, 0), About(Vector3.UnitX, 15));
        var world = scene.Admit();
        var end = world.Step(2 * world.TicksPerSecond)[body];
        if (clear) Assert.True(end.Position.Y < c.Y - .5f);
        else
        {
            Assert.InRange(end.Position.Y, c.Y + .1 - Resting - SurfaceError, c.Y + .1 + SurfaceError);
            Assert.True(end.Speed < .001, $"partial cap did not settle: {end.Velocity}");
            Assert.InRange((Vector3.Transform(Vector3.UnitY, end.Rotation) - Vector3.UnitY).Length(), 0, .005);
        }
    }

    public enum FrustumSphereRest { Face, InnerRim, OuterRim, Clear }

    [Theory]
    [InlineData(FrustumSphereRest.Face)]
    [InlineData(FrustumSphereRest.InnerRim)]
    [InlineData(FrustumSphereRest.OuterRim)]
    [InlineData(FrustumSphereRest.Clear)]
    public void GravityPressesSpheresOntoFrustumFacesAndRimsWithinTheRestingEnvelope(FrustumSphereRest kind)
    {
        // Top annulus: inner .8, outer 1 at y=4. For the inner rim,
        // r=1 and horizontal reach .8 give height .6 and n_y=.6.
        // Four equivalent outer rims at horizontal reach .06 hold r=.1:
        // height sqrt(.1²-.06²)=.08 and n_y=.8. Their symmetry holds
        // both horizontal directions; an isolated convex rim cannot do that.
        const double top = 4;
        var scene = new LawScene();
        var material = new LawMaterial(0, .1f, 0, 0);
        void Frustum(Vector3 centre) => scene.StaticHollow(ColliderShapeKind.Frustum, centre,
            Quaternion.Identity, .6f, new(.2f, 1, 1), material);
        Vector3[] rimCentres;
        double radius, rise, normalY, rimRadius;
        Vector3 rest;
        if (kind == FrustumSphereRest.OuterRim)
        {
            foreach (var axis in new[] { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitZ, -Vector3.UnitZ })
                Frustum(new Vector3(0, 3, 0) + 1.06f * axis);
            radius = .1;
            rimRadius = 1;
            rise = Math.Sqrt(radius * radius - .06 * .06);
            normalY = rise / radius;
            rest = new(0, (float)(top + rise), 0);
            rimCentres = [new(1.06f, 4, 0), new(-1.06f, 4, 0), new(0, 4, 1.06f), new(0, 4, -1.06f)];
        }
        else
        {
            Frustum(new(0, 3, 0));
            radius = kind == FrustumSphereRest.InnerRim ? 1 : .1;
            rimRadius = .8;
            rise = kind == FrustumSphereRest.InnerRim ? Math.Sqrt(radius * radius - .8 * .8) : radius;
            normalY = rise / radius;
            rest = new(kind == FrustumSphereRest.InnerRim ? 0 : kind == FrustumSphereRest.Clear ? 1.2f : .9f,
                (float)(top + rise), 0);
            rimCentres = kind == FrustumSphereRest.InnerRim
                ? [new(0, 4, 0)]
                : [];
        }
        var start = rest + new Vector3(0, .05f, 0);
        var id = scene.Sphere(start, (float)radius, 1, material, default, default, 0, -9.81f);
        var world = scene.Admit();
        if (kind == FrustumSphereRest.Clear)
        {
            var free = world.Step(world.TicksPerSecond / 2)[id];
            Assert.True(free.Position.Y < top - .5, "the separated sphere acquired support");
            Assert.InRange(free.Position.X, start.X - SurfaceError, start.X + SurfaceError);
            Assert.InRange(free.Position.Z, -SurfaceError, SurfaceError);
            // 240 velocity updates below 8 m/s, with product/addition roundoff
            // conservatively bounded by 2^-21 m/s per update.
            var velocityError = 240.0 / (1 << 21);
            Assert.InRange(free.Velocity.Y, -9.81 / 2 - velocityError, -9.81 / 2 + velocityError);
            return;
        }
        world.Step(5 * world.TicksPerSecond / 4, (tick, states) =>
        {
            if (tick <= world.TicksPerSecond) return;
            var body = states[id];
            // Height projected onto the independently derived contact normal.
            // The exact rim distances below separately check its curvature.
            var projected = (body.Position.Y - (top + rise)) * normalY;
            Assert.InRange(projected, -Resting - SurfaceError, SurfaceError);
            var sideways = new Vector3(body.Position.X - rest.X, 0, body.Position.Z - rest.Z).Length();
            Assert.InRange(sideways, 0, Resting + SurfaceError);
            Assert.True(body.Speed < .001, $"sphere did not rest: {body.Velocity}");
            if (rimCentres.Length == 0) return;
            var closest = double.PositiveInfinity;
            foreach (var point in rimCentres)
            {
                var dx = (double)body.Position.X - point.X;
                var dy = (double)body.Position.Y - point.Y;
                var dz = (double)body.Position.Z - point.Z;
                var radialGap = Math.Sqrt(dx * dx + dz * dz) - rimRadius;
                var gap = Math.Sqrt(radialGap * radialGap + dy * dy) - radius;
                Assert.True(gap >= -Resting - SurfaceError, $"rim penetration {gap}");
                closest = Math.Min(closest, gap);
            }
            Assert.True(closest <= SurfaceError, $"sphere stayed clear of its supporting rim: {closest}");
        });
    }

    public enum PassageShape { Straight, Bend45, Bend90, Funnel }

    [Theory]
    [InlineData(PassageShape.Straight, .34f, true)]
    [InlineData(PassageShape.Straight, .75f, false)]
    [InlineData(PassageShape.Bend45, .34f, true)]
    [InlineData(PassageShape.Bend45, .75f, false)]
    [InlineData(PassageShape.Bend90, .34f, true)]
    [InlineData(PassageShape.Bend90, .75f, false)]
    [InlineData(PassageShape.Funnel, .34f, true)]
    [InlineData(PassageShape.Funnel, .75f, false)]
    public void ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload(PassageShape passage, float radius, bool fits)
    {
        var part = passage switch
        {
            PassageShape.Straight => PartId.Pipe,
            PassageShape.Bend45 => PartId.PipeBend45,
            PassageShape.Bend90 => PartId.PipeBend90,
            PassageShape.Funnel => PartId.Funnel,
            _ => throw new ArgumentOutOfRangeException(nameof(passage))
        };
        var entry = TestBundle.Entry(part);
        var turn = About(Vector3.UnitZ, -90);
        var origin = new Vector3(0, 8, 0);
        Vector3 World(Vector3 local) => origin + Vector3.Transform(local, turn);
        var scene = new LawScene();
        foreach (var shape in DeclaredPhysics.Colliders(part))
        {
            var p = shape.LocalPose.Translation;
            var q = shape.LocalPose.Rotation;
            scene.StaticHollow(shape.ShapeKind, World(new(p.X, p.Y, p.Z)),
                turn * new Quaternion(q.X, q.Y, q.Z, q.W), shape.Radius,
                new(shape.HalfExtents.X, shape.HalfExtents.Y, shape.HalfExtents.Z), Pipe);
        }
        var start = entry.TubeMouths.Single(m => m.End == CuriousContraptions.Abstractions.TubeEnd.Start);
        var end = entry.TubeMouths.Single(m => m.End == CuriousContraptions.Abstractions.TubeEnd.End);
        var endAt = World(end.Position);
        var outward = Vector3.Transform(Vector3.UnitX, turn * end.Rotation);
        var startAt = World(start.Position);
        var ball = scene.Sphere(startAt + .5f * Vector3.UnitY, radius, 1f,
            new(.55f, .1f, .3f, .035f), default, default, .04f, -9.81f);
        var world = scene.Admit();
        var passed = false;
        Vector3? settledAt = null;
        var drift = 0f;
        var final = world.Step(3 * world.TicksPerSecond, (tick, state) =>
        {
            var displacement = state[ball].Position - endAt;
            var along = Vector3.Dot(displacement, outward);
            var radial = (displacement - along * outward).Length();
            passed |= along > 0f && radial < .65f;
            if (!fits && tick >= 11 * world.TicksPerSecond / 4)
            {
                settledAt ??= state[ball].Position;
                drift = MathF.Max(drift, Vector3.Distance(settledAt.Value, state[ball].Position));
            }
        });
        Assert.Equal(fits, passed);
        if (!fits)
        {
            // A jam retains the centred release line above the exit. A payload
            // escaping sideways or settling on the bench cannot satisfy these.
            var body = final[ball];
            Assert.True(body.Position.Y > endAt.Y);
            Assert.True(Vector3.Dot(body.Position - endAt, outward) < 0f);
            var lateral = new Vector3(body.Position.X - startAt.X, 0, body.Position.Z - startAt.Z).Length();
            Assert.InRange(lateral, 0, Resting + SurfaceError);
            // During the last quarter-second the jam stays inside the existing
            // resting envelope; the same displacement/window bounds its speed.
            Assert.NotNull(settledAt);
            Assert.InRange(drift, 0, Resting + SurfaceError);
            Assert.InRange(body.Speed, 0, (Resting + SurfaceError) / .25);
        }
    }

    [Theory]
    [InlineData(PassageShape.Straight)]
    [InlineData(PassageShape.Bend45)]
    [InlineData(PassageShape.Bend90)]
    [InlineData(PassageShape.Funnel)]
    public void ZeroRestitutionPackageControlDoesNotReturnImpactEnergy(PassageShape passage)
    {
        var (part, turn, x, z) = passage switch
        {
            PassageShape.Straight => (PartId.Pipe, Quaternion.Identity, 1.8f, 0f),
            PassageShape.Bend45 => (PartId.PipeBend45, About(Vector3.UnitX, -90), -.91844f, -.182689f),
            PassageShape.Bend90 => (PartId.PipeBend90, About(Vector3.UnitX, -90), -1.697056f, -.702944f),
            PassageShape.Funnel => (PartId.Funnel, Quaternion.Identity, .9f, 0f),
            _ => throw new ArgumentOutOfRangeException(nameof(passage))
        };
        var origin = new Vector3(0, 5, 0);
        var scene = new LawScene();
        foreach (var shape in DeclaredPhysics.Colliders(part))
        {
            var p = shape.LocalPose.Translation;
            var q = shape.LocalPose.Rotation;
            scene.StaticHollow(shape.ShapeKind, origin + Vector3.Transform(new(p.X, p.Y, p.Z), turn),
                turn * new Quaternion(q.X, q.Y, q.Z, q.W), shape.Radius,
                new(shape.HalfExtents.X, shape.HalfExtents.Y, shape.HalfExtents.Z), Pipe with { Restitution = 0f });
        }
        const float top = 5f + .78f + .34f;
        var ball = scene.Sphere(new(x, top + .5f, z), .34f, 1f,
            new(.55f, .1f, .3f, .035f), default, default, .04f, -9.81f);
        var world = scene.Admit();
        var landed = false;
        var incoming = 0f;
        var rebound = 0f;
        world.Step(world.TicksPerSecond, (_, state) =>
        {
            var body = state[ball];
            if (incoming > 1f && body.Position.Y < top + .05f && body.Velocity.Y > -.5f * incoming) landed = true;
            if (!landed) incoming = MathF.Max(incoming, -body.Velocity.Y);
            else rebound = MathF.Max(rebound, body.Velocity.Y);
        });
        Assert.True(landed);
        // Even a complete substep of gravity compensation cannot return more
        // than g / 480; the real .0825 restitution returns about .25 m/s.
        Assert.InRange(rebound, 0f, 9.81f / 480f);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void TiltedThinPartnersCrossTheMinimumWallBetweenVertices(bool cylinder, bool clear)
    {
        // A diagonal one-metre half-length crosses the radius .7 wall twice.
        // Every endpoint is outside and the centre is in the bore. Moving the
        // centre above the end by its half-thickness + .001 leaves a real gap.
        var axis = Vector3.Normalize(new Vector3(1, 0, 1));
        var perpendicular = Vector3.Normalize(new Vector3(-1, 0, 1));
        var hollow = new HollowSolid(ColliderShapeKind.Tube, Vector3.Zero, Vector3.UnitX,
            Vector3.UnitY, Vector3.UnitZ, .7f, .7f - ColliderDeclaration.MinimumThickness, 2f, 0f);
        var at = new Vector3(0, clear ? cylinder ? 2.0635f : 2.006f : 0f, 0);
        var partner = cylinder
            ? new HollowPartner(ColliderShapeKind.Cylinder, at, perpendicular, axis, Vector3.UnitY, .0625f, .0625f, 1f, .0625f)
            : new HollowPartner(ColliderShapeKind.Box, at, axis, Vector3.UnitY, perpendicular, 0f, 1f, .005f, .005f);
        Assert.Equal(!clear, HollowContactQuery.Overlaps(partner, hollow, HollowContactQuery.PlacementSlop));
    }

    [Theory]
    [InlineData(ColliderShapeKind.Tube, 0f)]
    [InlineData(ColliderShapeKind.Tube, 32f)]
    [InlineData(ColliderShapeKind.Frustum, 0f)]
    [InlineData(ColliderShapeKind.Frustum, 32f)]
    [InlineData(ColliderShapeKind.Torus, 0f)]
    [InlineData(ColliderShapeKind.Torus, 32f)]
    public void LiteralMaximumDimensionsAndTranslatedDistancesRetainTheAnalyticTarget(ColliderShapeKind shape, float translation)
    {
        // Coordinates and the quarter-metre probe offset are exactly representable
        // at these translations. This discriminates large dimensions/transforms,
        // but does not claim a universal bound for arbitrary f32 world coordinates.
        var (radius, lanes, probe, expected) = shape switch
        {
            ColliderShapeKind.Tube => (16f, new Vector3(15f, 16f, 0f), new Vector3(16.25f, 0, 0), .25),
            ColliderShapeKind.Frustum => (8f, new Vector3(1f, 16f, 16f), new Vector3(12.25f, 0, 0), .25 / Math.Sqrt(1 + .25 * .25)),
            ColliderShapeKind.Torus => (16f, new Vector3(3f, 4f, MathF.PI), new Vector3(20.25f, 0, 0), .25),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        Hollow(shape, radius, lanes.X, lanes.Y, lanes.Z).Validate();
        var centre = new Vector3(translation, -translation, translation);
        // An exact 90-degree axis permutation includes a nonidentity pose
        // without contaminating the independent expected distance.
        var solid = new HollowSolid(shape, centre, Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY,
            radius, lanes.X, lanes.Y, lanes.Z);
        var world = centre + probe.X * Vector3.UnitZ + probe.Y * Vector3.UnitX + probe.Z * Vector3.UnitY;
        var distance = solid.Distance(world, out _, out var normal, out _);
        Assert.InRange(distance, expected - SurfaceError, expected + SurfaceError);
        Assert.InRange(normal.Length(), 1f - 1e-6f, 1f + 1e-6f);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void BoxAndCylinderMeetTubeAndFrustumEndFaces(bool tapered, bool cylinder, bool clear)
    {
        var scene = new LawScene();
        var centre = new Vector3(0, 3, 0);
        scene.StaticHollow(tapered ? ColliderShapeKind.Frustum : ColliderShapeKind.Tube, centre,
            Quaternion.Identity, tapered ? .8f : 1f,
            tapered ? new(.2f, .5f, 1f) : new(.8f, .5f, 0f), Pipe);
        // End annulus at y3.5 spans radii .8..1. A .075-radius partner
        // centred at .9 is wholly on it; at1.2 it clears the outside by .125.
        var start = new Vector3(clear ? 1.2f : .9f, 3.65f, 0);
        var material = new LawMaterial(0f, .1f, .3f, 0f);
        var body = cylinder ? scene.Cylinder(start, .075f, .1f, 1f, material)
            : scene.Box(start, new(.075f, .1f, .075f), 1f, material);
        var world = scene.Admit();
        var final = world.Step(world.TicksPerSecond)[body];
        if (clear)
        {
            Assert.True(final.Position.Y < 3f);
            Assert.InRange(final.Position.X, start.X - 1e-5f, start.X + 1e-5f);
        }
        else
        {
            Assert.InRange(final.Position.Y, 3.6 - Resting - SurfaceError, 3.6 + SurfaceError);
            Assert.True(final.Speed < 1e-3);
        }
    }

    [Theory]
    [InlineData(false, 0f, .34f, true)]
    [InlineData(true, 0f, .34f, true)]
    [InlineData(false, 1.2f, .34f, true)]
    [InlineData(true, 1.2f, .34f, true)]
    [InlineData(false, 0f, .75f, false)]
    [InlineData(true, 0f, .75f, false)]
    public void FastAxialLaunchPassesOnlyTheBoreOrClearAir(bool rotated, float offset, float radius, bool passes)
    {
        var scene = new LawScene();
        var turn = rotated ? Quaternion.CreateFromYawPitchRoll(.4f, .7f, .2f) : Quaternion.Identity;
        var axis = Vector3.Transform(Vector3.UnitX, turn);
        var side = Vector3.Transform(Vector3.UnitZ, turn);
        var origin = new Vector3(0, 5, 0);
        scene.StaticHollow(ColliderShapeKind.Tube, origin, turn * AlongX, .7f, new(.65f, 1f, 0f), Pipe);
        var ball = scene.Sphere(origin - 3f * axis + offset * side, radius, 1f,
            new(0f, .1f, .3f, 0f), (64f * (1f - 1f / 1048576f)) * axis, default, 0f, 0f);
        var world = scene.Admit();
        var final = world.Step(world.TicksPerSecond / 8)[ball];
        var displacement = final.Position - origin;
        var along = Vector3.Dot(displacement, axis);
        if (passes)
        {
            // All coordinates stay below 8 m for these explicit poses: each
            // addition rounds by at most half an ulp = 2^-21 m. Sixty position
            // updates plus eight transform/projection roundings bound each
            // component; sqrt(3) converts that bound to Euclidean distance.
            var integrationBound = 68f * MathF.Sqrt(3f) / 2097152f;
            var expected = 8f * (1f - 1f / 1048576f) - 3f;
            Assert.InRange(along, expected - integrationBound, expected + integrationBound);
            Assert.InRange((displacement - along * axis - offset * side).Length(), 0f, integrationBound);
        }
        else Assert.True(along < -1f, "the oversized payload tunneled through the end rim");
    }

    // ---- admission ----

    private static ColliderDeclaration Hollow(ColliderShapeKind shape, float radius, float x, float y, float z) =>
        new(new(2), new(1), new(3), shape, RigidLocalPose.Identity, new(radius), new(x, y, z));

    public static TheoryData<string, ColliderShapeKind, float, float, float, float> AdmittedBoundaries() => new()
    {
        { "tube at the thinnest wall", ColliderShapeKind.Tube, .7f, .7f - ColliderDeclaration.MinimumThickness, 1.8f, 0f },
        { "tube at the smallest bore", ColliderShapeKind.Tube, .7f, 1f / 64, 1.8f, 0f },
        { "frustum at the thinnest wall", ColliderShapeKind.Frustum, 1.35f, ColliderDeclaration.MinimumThickness, .9f, .7f },
        { "solid torus", ColliderShapeKind.Torus, .715f, 0f, .065f, ColliderDeclaration.FullSweep },
        { "hollow torus over a half turn", ColliderShapeKind.Torus, 2.4f, .65f, .7f, MathF.PI },
    };

    [Theory]
    [MemberData(nameof(AdmittedBoundaries))]
    public void AHollowColliderAtItsDeclaredBoundaryIsAdmitted(string _, ColliderShapeKind shape, float radius, float x, float y, float z) =>
        Hollow(shape, radius, x, y, z).Validate();

    public static TheoryData<string, ColliderShapeKind, float, float, float, float> RefusedDeclarations() => new()
    {
        { "tube without a bore", ColliderShapeKind.Tube, .7f, 0f, 1.8f, 0f },
        { "tube whose bore reaches its wall", ColliderShapeKind.Tube, .7f, .7f, 1.8f, 0f },
        { "tube with a third lane", ColliderShapeKind.Tube, .7f, .65f, 1.8f, .1f },
        { "tube of zero length", ColliderShapeKind.Tube, .7f, .65f, 0f, 0f },
        { "frustum without a taper", ColliderShapeKind.Frustum, .7f, .05f, .9f, .7f },
        { "frustum whose wall closes its bore", ColliderShapeKind.Frustum, .7f, .7f, .9f, 1.35f },
        { "torus reaching its axis", ColliderShapeKind.Torus, .5f, 0f, .5f, ColliderDeclaration.FullSweep },
        { "torus whose bore reaches its wall", ColliderShapeKind.Torus, 2.4f, .7f, .7f, MathF.PI / 4 },
        { "torus sweep past a half turn", ColliderShapeKind.Torus, 2.4f, .65f, .7f, 3.2f },
        { "torus of zero sweep", ColliderShapeKind.Torus, 2.4f, .65f, .7f, 0f },
        { "non-finite tube radius", ColliderShapeKind.Tube, float.NaN, .65f, 1.8f, 0f },
    };

    [Theory]
    [MemberData(nameof(RefusedDeclarations))]
    public void AnUnrepresentableHollowColliderIsRefusedAtAdmission(string _, ColliderShapeKind shape, float radius, float x, float y, float z) =>
        Assert.Throws<ArgumentException>(() => Hollow(shape, radius, x, y, z).Validate());

    /// <summary>A hollow collider is static only: a dynamic body declaring one is refused before anything is stepped.</summary>
    [Fact]
    public void ADynamicHollowBodyIsRefusedAtAdmission()
    {
        var body = new RigidBodyDeclaration(new(1), RigidMotionKind.Dynamic, default, default, CanonicalRotation.Identity, default, default,
            new(1f), default, default);
        Assert.Throws<ArgumentException>(() => RigidMassProperties.Compile(body, Hollow(ColliderShapeKind.Tube, .7f, .65f, 1.8f, 0f)));
    }

    // ---- a ball at rest on every surface ----

    public enum Surface
    {
        TubeBore, TubeOuter, TubeEndFace, TubeRimEdge, FrustumBore, FrustumOuter, TorusBore, TorusOuter, TorusEndFace
    }

    /// <summary>The scene for each surface and the ball's exact resting centre, from the declared geometry (ball radius r_b 0.34):
    /// <list type="bullet">
    /// <item>tube bore: a level tube (r_i 0.65, r_o 0.7) about world X; the ball rests r_i − r_b below its axis;</item>
    /// <item>tube outer wall: two level tubes along X at z ±0.8; the ball rests in their groove, √((r_o + r_b)² − 0.8²) above their
    /// axes;</item>
    /// <item>tube end face: an upright tube (r_i 0.05, r_o 1, h 0.2); the ball, 0.5 off the axis, rests r_b above its top face;</item>
    /// <item>tube rim edge: an upright tube (r_i 0.2, r_o 0.3, h 0.2); the centred ball rests on the bore's rim circle,
    /// √(r_b² − r_i²) above the top;</item>
    /// <item>frustum bore: an upright frustum widening upward (outer 0.3 at −h, 0.9 at +h, wall 0.05, h 0.5); its bore's meridian line is
    /// ρ = a + k (y + h) with a 0.25, k 0.6, and the centred ball rests where its distance (a + k (y + h)) / √(1 + k²) to it is r_b;</item>
    /// <item>frustum outer wall: three upright frustums narrowing upward (outer 0.9 at −h, 0.3 at +h), their axes 1.1 m from the ball's
    /// vertical at 120°; the meridian line is ρ = 0.9 + k (y + h) with k −0.6, and the ball rests where (1.1 − 0.9 − k (y + h)) /
    /// √(1 + k²) is r_b;</item>
    /// <item>torus bore: a 90° torus segment (R 2.4, r_i 0.65, r_o 0.7) about world Z, its middle pointing down; the ball rests at the
    /// bore's lowest point, R + r_i − r_b below the centre;</item>
    /// <item>torus outer wall: a flat full torus (R 0.2, solid tube r_o 0.06) whose hole (0.14) is narrower than the ball; the centred
    /// ball rests √((r_b + r_o)² − R²) above its plane;</item>
    /// <item>torus end face: a solid 90° torus segment (R 1, r_o 0.3) turned so its start face is a level disc facing up; the ball
    /// centred over it rests r_b above it.</item>
    /// </list></summary>
    private static (LawScene Scene, Vector3 Ball, Vector3 Resting) Build(Surface surface)
    {
        var scene = new LawScene();
        // Clear of the bench: the torus bore's lowest point is 3.05 m under the centre.
        var c = new Vector3(0, BenchY + 4, 0);
        var tube = ColliderShapeKind.Tube;
        switch (surface)
        {
            case Surface.TubeBore:
                scene.StaticHollow(tube, c, AlongX, .7f, new(.65f, 1.8f, 0f), Pipe);
                return (scene, c, c - new Vector3(0, .65f - Rb, 0));
            case Surface.TubeOuter:
                scene.StaticHollow(tube, c + new Vector3(0, 0, .8f), AlongX, .7f, new(.65f, 1.8f, 0f), Pipe);
                scene.StaticHollow(tube, c - new Vector3(0, 0, .8f), AlongX, .7f, new(.65f, 1.8f, 0f), Pipe);
                return (scene, c + new Vector3(0, 1f, 0), c + new Vector3(0, MathF.Sqrt((.7f + Rb) * (.7f + Rb) - .64f), 0));
            case Surface.TubeEndFace:
                scene.StaticHollow(tube, c, Quaternion.Identity, 1f, new(.05f, .2f, 0f), Pipe);
                return (scene, c + new Vector3(.5f, .2f + Rb + .01f, 0), c + new Vector3(.5f, .2f + Rb, 0));
            case Surface.TubeRimEdge:
                scene.StaticHollow(tube, c, Quaternion.Identity, .3f, new(.2f, .2f, 0f), Pipe);
                return (scene, c + new Vector3(0, .2f + Rb, 0), c + new Vector3(0, .2f + MathF.Sqrt(Rb * Rb - .04f), 0));
            case Surface.FrustumBore:
            {
                scene.StaticHollow(ColliderShapeKind.Frustum, c, Quaternion.Identity, .3f, new(.05f, .5f, .9f), Pipe);
                const float a = .25f, k = .6f, h = .5f;
                var y = (Rb * MathF.Sqrt(1 + k * k) - a) / k - h;
                return (scene, c + new Vector3(0, .3f, 0), c + new Vector3(0, y, 0));
            }
            case Surface.FrustumOuter:
            {
                const float distance = 1.1f, k = -.6f, h = .5f;
                for (var i = 0; i < 3; i++)
                {
                    var angle = MathF.PI / 2 + i * 2 * MathF.PI / 3;
                    scene.StaticHollow(ColliderShapeKind.Frustum, c + distance * new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)),
                        Quaternion.Identity, .9f, new(.05f, .5f, .3f), Pipe);
                }
                var y = (distance - .9f - Rb * MathF.Sqrt(1 + k * k)) / k - h;
                return (scene, c + new Vector3(0, y + .05f, 0), c + new Vector3(0, y, 0));
            }
            case Surface.TorusBore:
            {
                var turn = About(Vector3.UnitZ, -45) * About(Vector3.UnitX, 90);
                scene.StaticHollow(ColliderShapeKind.Torus, c, turn, 2.4f, new(.65f, .7f, MathF.PI / 2), Pipe);
                return (scene, c - new Vector3(0, 2.4f, 0), c - new Vector3(0, 2.4f + .65f - Rb, 0));
            }
            case Surface.TorusOuter:
                scene.StaticHollow(ColliderShapeKind.Torus, c, Quaternion.Identity, .2f, new(0f, .06f, ColliderDeclaration.FullSweep), Pipe);
                return (scene, c + new Vector3(0, .4f, 0), c + new Vector3(0, MathF.Sqrt((Rb + .06f) * (Rb + .06f) - .04f), 0));
            case Surface.TorusEndFace:
                scene.StaticHollow(ColliderShapeKind.Torus, c, About(Vector3.UnitX, 90), 1f, new(0f, .3f, MathF.PI / 2), Pipe);
                return (scene, c + new Vector3(1f, Rb + .01f, 0), c + new Vector3(1f, Rb, 0));
            default:
                throw new ArgumentOutOfRangeException(nameof(surface), surface, "Unknown surface.");
        }
    }

    /// <summary>The vertical component of each resting contact's unit normal (from the surface toward the ball's centre): 1 on a
    /// level bottom or face; on the tube groove √((r_o + r_b)² − 0.8²) / (r_o + r_b); on the rim circle √(r_b² − r_i²) / r_b; on either
    /// frustum wall k / √(1 + k²) for its meridian slope |k| 0.6; on the flat ring √((r_b + r_o)² − R²) / (r_b + r_o).</summary>
    private static float Rise(Surface surface) => surface switch
    {
        Surface.TubeOuter => MathF.Sqrt((.7f + Rb) * (.7f + Rb) - .64f) / (.7f + Rb),
        Surface.TubeRimEdge => MathF.Sqrt(Rb * Rb - .04f) / Rb,
        Surface.FrustumBore or Surface.FrustumOuter => .6f / MathF.Sqrt(1 + .36f),
        Surface.TorusOuter => MathF.Sqrt((Rb + .06f) * (Rb + .06f) - .04f) / (Rb + .06f),
        _ => 1f
    };

    /// <summary>The Basketball set on (or just above, or dropped into) each surface comes to rest with its centre at the exact resting
    /// centre: its gap (centre distance along the contact less r_b) lies in [−(resting penetration + surface error), surface error],
    /// and it is still within 1 mm/s after 4 s.</summary>
    [Theory]
    [InlineData(Surface.TubeBore)]
    [InlineData(Surface.TubeOuter)]
    [InlineData(Surface.TubeEndFace)]
    [InlineData(Surface.TubeRimEdge)]
    [InlineData(Surface.FrustumBore)]
    [InlineData(Surface.FrustumOuter)]
    [InlineData(Surface.TorusBore)]
    [InlineData(Surface.TorusOuter)]
    [InlineData(Surface.TorusEndFace)]
    public void ABallPressedOnEachHollowSurfaceRestsWithinTheSurfaceErrorBound(Surface surface)
    {
        var (scene, start, rest) = Build(surface);
        var ball = scene.Ball(LawBall.Basketball, start);
        var world = scene.Admit();
        var final = world.Step(4 * world.TicksPerSecond)[ball];
        var offset = final.Position - rest;
        // Every rest above lies on a vertical line of the scene's symmetry, so a contact whose normal rises at cos θ from the
        // horizontal (θ from vertical) moves the centre vertically by gap / cos θ: the gap is the vertical offset times cos θ.
        Assert.True(MathF.Abs(offset.X) <= 1e-3f && MathF.Abs(offset.Z) <= 1e-3f, $"{surface}: the ball rests at {final.Position}, off {rest}");
        var gap = offset.Y * Rise(surface);
        Assert.True(gap >= -(Resting + SurfaceError) && gap <= SurfaceError,
            $"{surface}: the ball rests with a gap of {gap * 1e3:F4} mm from its exact resting centre {rest}");
        Assert.True(final.Speed < 1e-3, $"{surface}: the ball still moves at {final.Speed} m/s");
    }

    /// <summary>Control: the same ball set beside the tube's wall (outside its outer radius, clear of it) never touches it; it falls
    /// freely, so its horizontal velocity stays exactly zero.</summary>
    [Fact]
    public void ABallFallingBesideATubeTouchesNothing()
    {
        var scene = new LawScene();
        var c = new Vector3(0, BenchY + 3, 0);
        scene.StaticHollow(ColliderShapeKind.Tube, c, Quaternion.Identity, .7f, new(.65f, .5f, 0f), Pipe);
        var ball = scene.Ball(LawBall.Basketball, c + new Vector3(.7f + Rb + .01f, 1f, 0));
        var world = scene.Admit();
        var moved = false;
        world.Step(60, (_, state) => moved |= state[ball].Velocity.X != 0f || state[ball].Velocity.Z != 0f);
        Assert.False(moved, "the ball touched the tube it falls beside");
    }

    // ---- joints ----

    /// <summary>Two tubes (r_i 0.65, 2 m long) meet end to end at x 0 along a line tilted 5° downhill toward +X. The Basketball released
    /// 1.5 m before the joint rolls along the first bore at about (5/7) g sin 5° = 0.6 m/s² across the joint into the second. Throughout, its centre's distance from the common axis stays
    /// r_i − r_b within the resting penetration and the surface-error bound: no dip, step or hop at the joint.</summary>
    [Theory]
    [InlineData(0f, false, false)]
    [InlineData(.0005f, false, false)]
    [InlineData(.001f, false, false)]
    [InlineData(.0015f, false, false)]
    [InlineData(0f, true, false)]
    [InlineData(0f, true, true)]
    public void ABallRollingAcrossTwoTubesMeetingEndToEndKeepsItsHeightAcrossTheJoint(float gap, bool collar, bool reverse)
    {
        var scene = new LawScene();
        var tilt = About(Vector3.UnitZ, -5);
        var origin = new Vector3(0, BenchY + 3, 0);
        var axis = Vector3.Transform(Vector3.UnitX, tilt);
        void FirstTube() => scene.StaticHollow(ColliderShapeKind.Tube, origin - axis, tilt * AlongX, .7f, new(.65f, 1f, 0f), Pipe);
        void LastTube() => scene.StaticHollow(ColliderShapeKind.Tube, origin + (1 + gap) * axis, tilt * AlongX, .7f, new(.65f, 1f, 0f), Pipe);
        if (reverse) LastTube(); else FirstTube();
        if (collar) scene.StaticHollow(ColliderShapeKind.Torus, origin, tilt * AlongX, .715f, new(0, .065f, ColliderDeclaration.FullSweep), Pipe);
        if (reverse) FirstTube(); else LastTube();
        var down = Vector3.Transform(-Vector3.UnitY, tilt);
        var ball = scene.Ball(LawBall.Basketball, origin - 1.5f * axis + (.65f - Rb) * down);
        var world = scene.Admit();
        double nearest = double.PositiveInfinity, farthest = 0;
        var crossed = false;
        var trace = new List<string>();
        world.Step(4 * world.TicksPerSecond, (tick, state) =>
        {
            var p = state[ball].Position - origin;
            if (tick % 20 == 0) trace.Add($"{Vector3.Dot(p, axis):F3}/{(p - Vector3.Dot(p, axis) * axis).Length():F4}/{state[ball].Speed:F3}");
            var along = Vector3.Dot(p, axis);
            if (along < -1.5f || along > 1.5f) return;
            crossed |= along > .3f;
            var distance = (p - along * axis).Length();
            nearest = Math.Min(nearest, distance);
            farthest = Math.Max(farthest, distance);
        });
        Assert.True(crossed, "the ball never crossed the joint: " + string.Join(" ", trace));
        var expected = .65 - Rb;
        Assert.True(nearest >= expected - SurfaceError, $"the ball rose to {nearest:F6} m from the axis, above the bore's {expected:F6}");
        Assert.True(farthest <= expected + Resting + SurfaceError, $"the ball sank to {farthest:F6} m from the axis, into the bore's {expected:F6}");
    }

    /// <summary>Control for the seam rule: a lone tube (r_i 0.65, 2 m) tilted 5° downhill toward +X, with no collider beyond its end.
    /// The Basketball rolling out of it keeps touching the end's lip once its centre has passed the end plane: for some committed tick
    /// with its centre beyond the plane, its distance from the bore's rim circle is r_b within the resting penetration (it pivots over
    /// the free lip). Removing the lip's contact as a seam would drop it straight down instead, a full gap from the rim.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ABallRollingOutOfAFreeTubeEndPivotsOverItsLip(bool separatedMouth)
    {
        var scene = new LawScene();
        var tilt = About(Vector3.UnitZ, -5);
        var origin = new Vector3(0, BenchY + 3, 0);
        var axis = Vector3.Transform(Vector3.UnitX, tilt);
        scene.StaticHollow(ColliderShapeKind.Tube, origin - axis, tilt * AlongX, .7f, new(.65f, 1f, 0f), Pipe);
        if (separatedMouth)
            scene.StaticHollow(ColliderShapeKind.Tube, origin + 1.5f * axis, tilt * AlongX,
                .7f, new(.65f, 1f, 0f), Pipe); // a genuine half-metre air gap
        var down = Vector3.Transform(-Vector3.UnitY, tilt);
        var ball = scene.Ball(LawBall.Basketball, origin - 1.5f * axis + (.65f - Rb) * down);
        var world = scene.Admit();
        var pivoted = false;
        world.Step(4 * world.TicksPerSecond, (_, state) =>
        {
            var p = state[ball].Position - origin;
            var along = Vector3.Dot(p, axis);
            if (!(along > .01f)) return;
            var radial = p - along * axis;
            var length = radial.Length();
            if (!(length > 1e-6f)) return;
            // The rim circle's point nearest the centre lies in its meridian, at the bore radius in the end plane.
            var fromRim = (p - .65f * radial / length).Length();
            pivoted |= fromRim <= Rb + SurfaceError && fromRim >= Rb - Resting - SurfaceError;
        });
        Assert.True(pivoted, "the ball never pivoted on the free lip once past the end plane");
    }

    /// <summary>A bend as the pipe bends build it: a 1 m tube stub (r_i 0.65, r_o 0.7), a 90° torus segment (R 2.4, same tube) whose
    /// start face meets the stub's end, and a second 1 m stub from its end face. The bend lies flat (its axis vertical) and is tilted
    /// 15° about the in-plane line perpendicular to its chord, so both stubs and the whole arc descend (each stub at
    /// asin(sin 15° / √2) = 10.5°). The Basketball released in the first stub rolls along the bore's floor into the arc, rides up its
    /// outer wall and back down in the second stub, always on the wall. Until it leaves the second stub, its centre's distance from the
    /// tube's centreline (the stubs' axes and the arc's circle) stays r_i − r_b within the resting penetration and the surface-error
    /// bound across both seams: no dip, step or hop where a stub meets the arc.</summary>
    [Fact]
    public void ABallRollingFromAStubRoundATorusSegmentAndOutKeepsItsHeightAcrossBothSeams()
    {
        var scene = new LawScene();
        var centre = new Vector3(0, BenchY + 4, 0);
        var tilt = Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1, 0, 1)), 15f * MathF.PI / 180);
        const float bend = 2.4f, bore = .65f;
        // The segment about the tilted vertical: its centreline R (cos a, 0, sin a) from +X toward +Z.
        scene.StaticHollow(ColliderShapeKind.Torus, centre, tilt, bend, new(bore, .7f, MathF.PI / 2), Pipe);
        var start = centre + bend * Vector3.Transform(Vector3.UnitX, tilt);
        var startTangent = Vector3.Transform(Vector3.UnitZ, tilt);
        var end = centre + bend * Vector3.Transform(Vector3.UnitZ, tilt);
        var endTangent = Vector3.Transform(-Vector3.UnitX, tilt);
        Assert.True(startTangent.Y < -.18f && endTangent.Y < -.18f, "both stubs descend");
        // A stub's axis is its local Y: the shortest turn of Y onto each tangent.
        static Quaternion Onto(Vector3 tangent)
        {
            var axis = Vector3.Cross(Vector3.UnitY, tangent);
            return Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), MathF.Acos(Math.Clamp(tangent.Y, -1f, 1f)));
        }
        scene.StaticHollow(ColliderShapeKind.Tube, start - .5f * startTangent, Onto(startTangent), .7f, new(bore, .5f, 0f), Pipe);
        scene.StaticHollow(ColliderShapeKind.Tube, end + .5f * endTangent, Onto(endTangent), .7f, new(bore, .5f, 0f), Pipe);
        var floor = Vector3.Normalize(-Vector3.UnitY + startTangent.Y * startTangent);
        var ball = scene.Ball(LawBall.Basketball, start - .8f * startTangent + (bore - Rb) * floor);
        var world = scene.Admit();

        // The distance from the centreline: to a stub's axis segment, or to the arc's circle within its sweep.
        float FromCentreline(Vector3 p)
        {
            static float Segment(Vector3 p, Vector3 a, Vector3 b)
            {
                var ab = b - a;
                var t = Math.Clamp(Vector3.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
                return (p - (a + t * ab)).Length();
            }
            var local = Vector3.Transform(p - centre, Quaternion.Inverse(tilt));
            var angle = MathF.Atan2(local.Z, local.X);
            var arc = angle is >= 0f and <= MathF.PI / 2
                ? MathF.Sqrt(MathF.Pow(new Vector2(local.X, local.Z).Length() - bend, 2) + local.Y * local.Y) : float.PositiveInfinity;
            return MathF.Min(arc, MathF.Min(Segment(p, start - startTangent, start), Segment(p, end, end + endTangent)));
        }
        double nearest = double.PositiveInfinity, farthest = 0;
        var exited = false;
        var where = "";
        world.Step(4 * world.TicksPerSecond, (tick, state) =>
        {
            var p = state[ball].Position;
            exited |= Vector3.Dot(p - end, endTangent) > 1f;
            if (exited) return;
            var distance = FromCentreline(p);
            if (distance < nearest)
            {
                var local = Vector3.Transform(p - centre, Quaternion.Inverse(tilt));
                where = $"tick {tick}, arc angle {MathF.Atan2(local.Z, local.X) * 180 / MathF.PI:F1}°, speed {state[ball].Speed:F2} m/s";
            }
            nearest = Math.Min(nearest, distance);
            farthest = Math.Max(farthest, distance);
        });
        Assert.True(exited, "the ball never left the second stub");
        var expected = bore - Rb;
        Assert.True(nearest >= expected - SurfaceError,
            $"the ball rose to {nearest:F6} m from the centreline ({where}), off the bore's {expected:F6}");
        Assert.True(farthest <= expected + Resting + SurfaceError, $"the ball sank to {farthest:F6} m from the centreline, into the bore's {expected:F6}");
    }

    // ---- box and cylinder partners ----

    /// <summary>The Domino tile laid flat (its thinnest axis vertical, its longest along Z) across two level tubes (r_o 0.7, along X,
    /// their axes 0.6 of its half-length either side of its middle) rests on their tops: its centre r_o plus its half-thickness above
    /// their axes, within the resting penetration, level.</summary>
    [Fact]
    public void ADominoLaidAcrossTwoTubesRestsOnTheirTops()
    {
        var half = LawTile.HalfExtents;
        var axes = new[] { half.X, half.Y, half.Z };
        var thin = Array.IndexOf(axes, axes.Min());
        var longest = Array.IndexOf(axes, axes.Max());
        // A quarter-turn combination taking the thinnest local axis to world ±Y and the longest to world ±Z.
        Vector3 Unit(int i) => i == 0 ? Vector3.UnitX : i == 1 ? Vector3.UnitY : Vector3.UnitZ;
        var (u, w) = (Unit(thin), Unit(longest));
        var turns = from a in Enumerable.Range(0, 4) from b in Enumerable.Range(0, 4)
            select About(Vector3.UnitY, 90 * a) * About(Vector3.UnitX, 90 * b);
        var lay = turns.Concat(turns.Select(q => About(Vector3.UnitZ, 90) * q)).First(q =>
            MathF.Abs(Vector3.Transform(u, q).Y) > .99999f && MathF.Abs(Vector3.Transform(w, q).Z) > .99999f);

        var scene = new LawScene();
        var c = new Vector3(0, BenchY + 3, 0);
        var spacing = .6f * axes[longest];
        scene.StaticHollow(ColliderShapeKind.Tube, c + new Vector3(0, 0, spacing), AlongX, .7f, new(.65f, 1f, 0f), Pipe);
        scene.StaticHollow(ColliderShapeKind.Tube, c - new Vector3(0, 0, spacing), AlongX, .7f, new(.65f, 1f, 0f), Pipe);
        var tile = scene.Domino(c + new Vector3(0, .7f + axes[thin] + .005f, 0), lay);
        var world = scene.Admit();
        var final = world.Step(3 * world.TicksPerSecond)[tile];
        var expected = c.Y + .7 + axes[thin];
        Assert.True(final.Position.Y >= expected - Resting - SurfaceError && final.Position.Y <= expected + SurfaceError,
            $"the tile rests at {final.Position.Y:F5}, not on the tubes' tops at {expected:F5}");
        var turned = 2 * MathF.Acos(Math.Min(1f, MathF.Abs(Quaternion.Dot(Quaternion.Normalize(final.Rotation), lay))));
        Assert.True(turned < 2e-3f, $"the tile turned {turned} rad from flat");
        Assert.True(final.Speed < 1e-3, $"the tile still moves at {final.Speed} m/s");
    }

    /// <summary>A solid cylinder (r 0.3, half-width 0.4, the Weight's rolling resistance 0.03, which damps its rocking in the concave
    /// bore) lying along a level tube's bore (r_i 0.65) rests on the bore's bottom line: its axis r_i − r below the tube's, within the
    /// resting penetration, still.</summary>
    [Fact]
    public void ACylinderLyingInATubeBoreRestsOnItsBottomLine()
    {
        var scene = new LawScene();
        var c = new Vector3(0, BenchY + 3, 0);
        scene.StaticHollow(ColliderShapeKind.Tube, c, AlongX, .7f, new(.65f, 1.8f, 0f), Pipe);
        var log = scene.Cylinder(c - new Vector3(0, .65f - .3f - .005f, 0), .3f, .4f, 4f, new(0f, .1f, .5f, .03f), AlongX);
        var world = scene.Admit();
        var final = world.Step(3 * world.TicksPerSecond)[log];
        var expected = c.Y - (.65 - .3);
        Assert.True(final.Position.Y >= expected - Resting - SurfaceError && final.Position.Y <= expected + SurfaceError,
            $"the cylinder rests at {final.Position.Y:F5}, not on the bore's bottom at {expected:F5}");
        Assert.True(MathF.Abs(final.Position.Z - c.Z) <= 1e-3f, $"the cylinder rests off the bore's bottom line at z {final.Position.Z}");
        Assert.True(final.Speed < 1e-3, $"the cylinder still moves at {final.Velocity} m/s, spin {final.Angular}, at {final.Position}");
    }
}
