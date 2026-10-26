using System.Numerics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

public enum RecoveryShape { Sphere, Box, Cylinder }

public sealed class WorkshopPhysicsLawRecoveryTests
{
    private const float Radius = .2f;
    private const float Depth = .007f;
    private const float G = 9.81f;
    private const float Slop = .0002f;

    public static TheoryData<SimulationCadence, RecoveryShape, float> LoadedCases()
    {
        var cases = new TheoryData<SimulationCadence, RecoveryShape, float>();
        foreach (var cadence in Enum.GetValues<SimulationCadence>())
        foreach (var shape in Enum.GetValues<RecoveryShape>())
        foreach (var friction in new[] { 0f, .5f })
            cases.Add(cadence, shape, friction);
        return cases;
    }

    [Theory]
    [MemberData(nameof(LoadedCases))]
    public void DeepContactsRecoverWithoutSpendingMoreThanOneSlopOfPotentialPerTick(
        SimulationCadence cadence, RecoveryShape shape, float friction)
    {
        var scene = new LawScene(bench: false);
        var material = new LawMaterial(0f, .1f, friction, 0f);
        scene.StaticBox(new(0f, -.1f, 0f), new(1f, .1f, 1f), material: material);
        var position = new Vector3(0f, Radius - Depth, 0f);
        var body = shape switch
        {
            RecoveryShape.Sphere => scene.Sphere(position, Radius, 1f, material, default, default, 0f, -G),
            RecoveryShape.Box => scene.Box(position, new(Radius), 1f, material),
            RecoveryShape.Cylinder => scene.Cylinder(position, Radius, Radius, 1f, material),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
        var world = scene.Admit(cadence);
        var previousEnergy = Energy(world.Initial(body), shape);
        var previousHeight = world.Initial(body).Position.Y;
        var quarterSecond = world.Initial(body);
        // With omega=2*pi*60, h=1/480, zeta=10, a2=h*omega*(2*zeta+h*omega), the loaded
        // row advances at massScale*(cap-g/[omega*(2*zeta+h*omega)]). Here massScale=.94228
        // and load bias=.00125193 m/s, giving .0101277 m/s: over 1 mm in .25 s.
        // The capped phase from 7 mm to .862 mm takes .606 s. The soft tail decays at
        // massScale*biasRate=17.09/s toward .269 mm static sag, reaching .5 mm in .056 s.
        // Two seconds allows ample settling. These are isolated loaded-row estimates.
        var final = world.Step(2 * world.TicksPerSecond, (tick, state) =>
        {
            var current = state[body];
            Assert.True(current.Finite);
            var energy = Energy(current, shape);
            Assert.True(energy - previousEnergy <= (double)G * Slop,
                $"tick {tick}: energy rise {energy - previousEnergy} J exceeds one slop");
            previousEnergy = energy;
            // At .2 m, each coordinate ulp is 2^-26 m. Eight substeps at 60 Hz plus
            // commit/subtraction rounding fit within 1 um; this does not enlarge the energy bound.
            Assert.True(current.Position.Y - previousHeight <= .012f / world.TicksPerSecond + 1e-6f,
                $"tick {tick}: upward recovery exceeds the physical rate cap");
            previousHeight = current.Position.Y;
            // No upward launch outside the unchanged resting envelope.
            Assert.InRange(current.Position.Y, Radius - Depth - .0005f, Radius + .0005f);
            if (tick == world.TicksPerSecond / 4) quarterSecond = current;
            // The independently estimated .606 + .056 = .662 s settling precedes this
            // final full second; rest is an interval, not just the endpoint sample.
            if (tick >= world.TicksPerSecond)
            {
                Assert.InRange(current.Position.Y, Radius - .0005f, Radius + .0002f);
                Assert.True(current.Speed < .002f && current.Spin < .01f, "Recovery must remain at rest.");
            }
        })[body];
        Assert.True(quarterSecond.Position.Y >= position.Y + .001f, "Recovery must progress within .25 s.");
        Assert.InRange(final.Position.Y, Radius - .0005f, Radius + .0002f);
        Assert.True(final.Speed < .002f && final.Spin < .01f, "Recovery must end at rest.");
    }

    private static double Energy(LawBody body, RecoveryShape shape)
    {
        // Host-side oracle only: sum the committed f32 states in double so rounding a ~2 J
        // total cannot hide a small breach of the 1.962 mJ energy-rise limit.
        const double r = Radius;
        var transverse = shape switch
        {
            RecoveryShape.Sphere => 2 * r * r / 5,
            RecoveryShape.Box => 2 * r * r / 3,
            RecoveryShape.Cylinder => 7 * r * r / 12,
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
        var axial = shape == RecoveryShape.Cylinder ? r * r / 2 : transverse;
        var angular = Vector3.Transform(body.Angular, Quaternion.Conjugate(body.Rotation));
        static double Square(float value) => (double)value * value;
        return (double)G * body.Position.Y +
            .5 * (Square(body.Velocity.X) + Square(body.Velocity.Y) + Square(body.Velocity.Z)) +
            .5 * (transverse * (Square(angular.X) + Square(angular.Z)) + axial * Square(angular.Y));
    }

    public static TheoryData<SimulationCadence, bool, float> PairCases()
    {
        var cases = new TheoryData<SimulationCadence, bool, float>();
        foreach (var cadence in Enum.GetValues<SimulationCadence>())
        foreach (var dynamicSupport in new[] { false, true })
        foreach (var friction in new[] { 0f, .5f })
            cases.Add(cadence, dynamicSupport, friction);
        return cases;
    }

    [Theory]
    [MemberData(nameof(PairCases))]
    public void ObliqueSinglePointRecoverySeparatesStaticAndDynamicSupportsAtTheSamePhysicalRate(
        SimulationCadence cadence, bool dynamicSupport, float friction)
    {
        var scene = new LawScene(bench: false);
        var material = new LawMaterial(0f, .1f, friction, 0f);
        // A 3-4-5 direction gives an oblique central normal on both spheres (no torque arm).
        // The existing tilted-cap/rim gates and exact friction case 179 cover angular leverage.
        var normal = new Vector3(.6f, .8f, 0f);
        var support = dynamicSupport
            ? scene.Sphere(default, Radius, 1f, material, default, default, 0f, 0f)
            : scene.StaticSphere(default, Radius, Quaternion.Identity, material);
        var body = scene.Sphere(normal * (2f * Radius - Depth), Radius, 1f, material, default, default, 0f, 0f);
        var world = scene.Admit(cadence);
        // Up to 960 additions of coordinates below .4 m: N * .4 * 2^-24 bounds accumulated
        // positional rounding. Velocity directions are checked separately below.
        const float accumulatedPositionError = 960f * .4f / (1 << 24);
        var initialCentre = (world.Initial(body).Position + world.Initial(support).Position) / 2f;
        var previousDistance = 2f * Radius - Depth;
        var quarterDistance = previousDistance;
        var final = world.Step(2 * world.TicksPerSecond, (tick, state) =>
        {
            var a = state[body]; var b = state[support];
            var separation = a.Position - b.Position;
            var distance = separation.Length();
            // 1 um covers f32 rounding of near-origin positions/distance; it is a geometric
            // measurement tolerance, not an energy allowance. Requested correction is 12 mm/s.
            Assert.InRange(distance - previousDistance, -1e-6f, .012f / world.TicksPerSecond + 1e-6f);
            Assert.True(Vector3.Cross(separation, normal).Length() < accumulatedPositionError, "No lateral displacement.");
            Assert.True(Vector3.Cross(a.Velocity - b.Velocity, normal).Length() < 1e-6f, "No lateral impulse.");
            Assert.True(a.Speed < .002f && b.Speed < .002f, "Unbiased relaxation removes recovery velocity.");
            // Central normal/zero tangential launch gives zero torque. Conservatively accumulate
            // 8*u*r*cap impulse-moment error per substep, times inverse inertia 5/(2*r^2):
            // N*20*u*cap/r rad/s. This permits rounding, not a physical torque response.
            const double angularBound = 960 * 20 * .012 / (Radius * 16777216.0);
            Assert.True(a.Spin <= angularBound && b.Spin <= angularBound, "Central recovery must not invent spin.");
            // Both unit-mass solid spheres: I=2*m*r^2/5. Gravity is zero, so energy is all kinetic.
            var kinetic = .5 * (a.Speed * a.Speed + b.Speed * b.Speed) +
                Radius * (double)Radius / 5 * (a.Spin * a.Spin + b.Spin * b.Spin);
            var massInContact = dynamicSupport ? 2 : 1;
            Assert.True(kinetic <= massInContact * (double)G * Slop, "Combined kinetic energy stays in the existing contact envelope.");
            if (dynamicSupport)
                Assert.True(Vector3.Distance((a.Position + b.Position) / 2f, initialCentre) < accumulatedPositionError,
                    "Equal and opposite recovery preserves the centre of mass.");
            else Assert.Equal(Vector3.Zero, b.Position);
            previousDistance = distance;
            if (tick == world.TicksPerSecond / 4) quarterDistance = distance;
        });
        Assert.True(quarterDistance > 2f * Radius - Depth + .001f);
        Assert.InRange(Vector3.Distance(final[body].Position, final[support].Position),
            2f * Radius - .0005f, 2f * Radius + 1e-6f);
    }


    [Theory]
    [InlineData(-.00002)]
    [InlineData(0.0)]
    [InlineData(.00002)]
    public void RecoveryCrossesThePhysicalRateCapContinuouslyAtItsDerivedDepth(double offset)
    {
        const double h = 1.0 / 480, cap = .0002 * 60;
        var omega = 2 * Math.PI * 60;
        var biasRate = omega / (20 + h * omega);
        var a2 = h * omega * (20 + h * omega);
        var massScale = a2 / (1 + a2);
        var transition = .0002 + cap / biasRate;
        var scene = new LawScene(bench: false);
        var material = new LawMaterial(0f, .1f, 0f, 0f);
        scene.StaticBox(new(0f, -.1f, 0f), new(1f, .1f, 1f), material: material);
        var body = scene.Sphere(new(0f, Radius - (float)(transition + offset), 0f),
            Radius, 1f, material, default, default, 0f, 0f);
        var world = scene.Admit(SimulationCadence.Hz240);
        var initial = world.Initial(body);
        var depth = (double)Radius - initial.Position.Y;
        // Solve the isolated compliant row from rest: delta_y=h*mScale*min(b*(d-slop),cap).
        // Relax removes its velocity, so the next of the two substeps starts at rest again.
        double Predict(bool capped)
        {
            var displacement = 0.0;
            for (var substep = 0; substep < 2; substep++)
            {
                var requested = biasRate * (depth - displacement - .0002);
                displacement += h * massScale * (capped ? Math.Min(requested, cap) : requested);
            }
            return displacement;
        }
        // Six ulps at .2 m are 6*2^-26=8.94e-8 m: two position additions, authoring and
        // observation arithmetic. 1e-7 covers these without masking the ~.7 um excess
        // the uncapped law would add for the +20 um input's first substep.
        const double tolerance = 1e-7;
        var expected = Predict(capped: true);
        if (offset > 0) Assert.True(Predict(capped: false) - expected > 4 * tolerance);
        var final = world.Step(1)[body];
        Assert.InRange((double)final.Position.Y - initial.Position.Y, expected - tolerance, expected + tolerance);
    }

    [Theory]
    [InlineData(false, 0f)]
    [InlineData(false, .5f)]
    [InlineData(true, 0f)]
    [InlineData(true, .5f)]
    public void TheSameObliquePairHasTheSameIntermediateSeparationAtEveryCadence(bool dynamicSupport, float friction)
    {
        float[]? reference = null;
        foreach (var cadence in Enum.GetValues<SimulationCadence>())
        {
            var scene = new LawScene(bench: false);
            var material = new LawMaterial(0f, .1f, friction, 0f);
            var support = dynamicSupport
                ? scene.Sphere(default, Radius, 1f, material, default, default, 0f, 0f)
                : scene.StaticSphere(default, Radius, Quaternion.Identity, material);
            var body = scene.Sphere(new Vector3(.6f, .8f, 0f) * (2f * Radius - Depth),
                Radius, 1f, material, default, default, 0f, 0f);
            var world = scene.Admit(cadence);
            var samples = new float[8];
            world.Step(2 * world.TicksPerSecond, (tick, state) =>
            {
                if (tick % (world.TicksPerSecond / 4) == 0)
                    samples[tick / (world.TicksPerSecond / 4) - 1] =
                        Vector3.Distance(state[body].Position, state[support].Position);
            });
            if (reference is null) reference = samples;
            else
                for (var sample = 0; sample < samples.Length; sample++)
                {
                    // Two trajectories, two bodies each, at most 960 near-origin additions
                    // below .4 m: 4*N*.4*2^-24, plus 1 um for the distance observations.
                    const float tolerance = 4 * 960f * .4f / (1 << 24) + 1e-6f;
                    Assert.InRange(samples[sample], reference[sample] - tolerance, reference[sample] + tolerance);
                }
        }
    }

    public static TheoryData<SimulationCadence, float> QuietCases()
    {
        var cases = new TheoryData<SimulationCadence, float>();
        foreach (var cadence in Enum.GetValues<SimulationCadence>())
        foreach (var gap in new[] { -.0002f, -.0001f, 0f, .0001f, .005f })
            cases.Add(cadence, gap);
        return cases;
    }

    [Theory]
    [MemberData(nameof(QuietCases))]
    public void ClearAndSlopBandContactsDoNotInventMotion(SimulationCadence cadence, float gap)
    {
        var scene = new LawScene(bench: false);
        var material = new LawMaterial(0f, .1f, .5f, 0f);
        scene.StaticBox(new(0f, -.1f, 0f), new(1f, .1f, 1f), material: material);
        var body = scene.Sphere(new(.4f, Radius + gap, 0f), Radius, 1f, material, default, default, 0f, 0f);
        var world = scene.Admit(cadence);
        var initial = world.Initial(body);
        world.Step(world.TicksPerSecond, (_, state) =>
        {
            var current = state[body];
            Assert.True(Vector3.Distance(initial.Position, current.Position) < 1e-6f);
            Assert.True(current.Speed < 1e-6f && current.Spin < 1e-6f);
        });
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public void AClearContactDoesNotSupportABodyBeforeItsFreeFallReachesTheSurface(SimulationCadence cadence)
    {
        var scene = new LawScene(bench: false);
        var material = new LawMaterial(0f, .1f, .5f, 0f);
        scene.StaticBox(new(0f, -.1f, 0f), new(1f, .1f, 1f), material: material);
        var body = scene.Sphere(new(.4f, Radius + .005f, 0f), Radius, 1f, material, default, default, 0f, -G);
        var world = scene.Admit(cadence);
        var initial = world.Initial(body);
        var final = world.Step(1)[body];
        var substeps = 480 / world.TicksPerSecond;
        // Symplectic Euler: v_n = -g*n*h, dy = -g*h^2*n*(n+1)/2.
        // At 60 Hz the fall is only 1.53 mm, less than the 5 mm clear gap.
        // 1 um and 1 um/s covers <=8 near-origin f32 integration roundings.
        var expectedVelocity = -G / world.TicksPerSecond;
        var expectedDrop = G / (480f * 480f) * substeps * (substeps + 1) / 2f;
        Assert.InRange(final.Velocity.Y, expectedVelocity - 1e-6f, expectedVelocity + 1e-6f);
        Assert.InRange(initial.Position.Y - final.Position.Y, expectedDrop - 1e-6f, expectedDrop + 1e-6f);
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public void ResetReadmissionReplaysDeepRecoveryThroughCopiedSemanticStates(SimulationCadence cadence)
    {
        var scene = new LawScene(bench: false);
        var material = new LawMaterial(0f, .1f, .5f, 0f);
        scene.StaticBox(new(0f, -.1f, 0f), new(1f, .1f, 1f), material: material);
        var body = scene.Sphere(new(.4f, Radius - Depth, 0f), Radius, 1f, material, default, default, 0f, -G);
        var world = scene.Admit(cadence);
        var engine = new WorkshopPhysicsEngine();
        LawBody[] Replay(WorkshopPhysicsEngine target)
        {
            var states = new LawBody[2 * world.TicksPerSecond];
            world.Step(states.Length, (tick, state) => states[tick - 1] = state[body], engine: target);
            var final = states[^1];
            Assert.InRange(final.Position.Y, Radius - .0005f, Radius + .0002f);
            Assert.True(final.Speed < .002f && final.Spin < .01f, "Each replay must physically settle.");
            // 960 additions at coordinates below .4 m: N*.4*2^-24 per axis.
            const float positionError = 960f * .4f / (1 << 24);
            Assert.InRange(final.Position.X, .4f - positionError, .4f + positionError);
            Assert.InRange(final.Position.Z, -positionError, positionError);
            return states;
        }
        var first = Replay(engine);
        var reset = Replay(engine);
        var fresh = Replay(new WorkshopPhysicsEngine());
        for (var tick = 0; tick < first.Length; tick++)
        {
            Assert.Equal(first[tick], reset[tick]);
            Assert.Equal(first[tick], fresh[tick]);
        }
        Assert.InRange(first[^1].Position.Y, Radius - .0005f, Radius + .0002f);
    }
}

