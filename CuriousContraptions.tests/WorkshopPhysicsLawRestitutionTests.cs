using System.Buffers.Binary;
using System.Numerics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>
/// Engine restitution slice (24 Oct 2026): the one restitution law on the shipped C# engine (WorkshopPhysicsEngine) through real
/// admission and tick progression. Box2D v3 semantics mapped onto the 480 Hz substep (each substep is a whole step): a point bounces
/// when it carried normal impulse in that substep and its substep-start approach exceeds the pair's bounce threshold; a manifold's
/// bouncing points are solved together to −e·v_n; friction and rolling are solved once more with the bounce impulse in their cones.
/// Every bound is derived from e, g, the 120 Hz commit period T and m·g·ContactSlop, never from observed output.
/// </summary>
public sealed class WorkshopPhysicsLawRestitutionTests
{
    private static readonly float BenchY = LawScene.BenchY;
    private static readonly float G = -(float)LawScene.Gravity;
    private const double T = 1.0 / 120;
    private static Vector3 At(float x, float y, float z = 0f) => new(x, y, z);
    private static double Squared(Vector3 v) => (double)v.X * v.X + (double)v.Y * v.Y + (double)v.Z * v.Z;
    /// <summary>The energy a tick may add: lifting 1 kg by the solver's contact slop.</summary>
    private static double Slop => G * WorkshopConstraintSolver.ContactSlop;

    public enum Body { Sphere, BoxOnBench, BoxOnBox, CylinderOnCap }

    private static readonly float SphereRadius = .1f;
    private static readonly Vector3 BoxHalf = At(.25f, .1f, .15f);
    private static readonly float CylinderRadius = .2f, CylinderHalf = .15f;

    /// <summary>Kinetic plus potential energy (J) of the 1 kg dropped body from its admitted f32 geometry.</summary>
    private static double Energy(Body body, LawBody b)
    {
        var w = Vector3.Transform(b.Angular, Quaternion.Inverse(b.Rotation));
        double rotation = body switch
        {
            Body.Sphere => .4 * SphereRadius * SphereRadius * Squared(w),
            Body.CylinderOnCap => (CylinderRadius * CylinderRadius / 4.0 + CylinderHalf * CylinderHalf / 3.0) * ((double)w.X * w.X + (double)w.Z * w.Z) +
                CylinderRadius * CylinderRadius / 2.0 * w.Y * w.Y,
            _ => ((double)BoxHalf.Y * BoxHalf.Y + BoxHalf.Z * BoxHalf.Z) / 3 * w.X * w.X + ((double)BoxHalf.X * BoxHalf.X + BoxHalf.Z * BoxHalf.Z) / 3 * w.Y * w.Y +
                ((double)BoxHalf.X * BoxHalf.X + BoxHalf.Y * BoxHalf.Y) / 3 * w.Z * w.Z,
        };
        return .5 * Squared(b.Velocity) + .5 * rotation + G * (b.Position.Y - BenchY);
    }

    /// <summary>The body's resting centre height and the scene with it placed <paramref name="drop"/> m above it.</summary>
    private static (LawWorld World, GpuBodyId Id, float RestY) Drop(Body body, LawMaterial material, float drop, Quaternion? rotation = null,
        Vector3 spin = default)
    {
        var scene = new LawScene();
        float rest;
        GpuBodyId id;
        switch (body)
        {
            case Body.Sphere:
                rest = BenchY + SphereRadius;
                id = scene.Sphere(At(0, rest + drop), .1f, 1f, material, default, spin, 0f, LawScene.Gravity, rotation: rotation);
                break;
            case Body.BoxOnBench:
                rest = BenchY + BoxHalf.Y;
                id = scene.Box(At(0, rest + drop), At(.25f, .1f, .15f), 1f, material, rotation, angular: spin);
                break;
            case Body.BoxOnBox:
                scene.StaticBox(At(0, BenchY + .3f), At(.3f, .3f, .5f), null, LawMaterial.Bench);
                rest = BenchY + .6f + BoxHalf.Y;
                id = scene.Box(At(0, rest + drop), At(.25f, .1f, .15f), 1f, material, rotation, angular: spin);
                break;
            case Body.CylinderOnCap:
                rest = BenchY + CylinderHalf;
                id = scene.Cylinder(At(0, rest + drop), .2f, .15f, 1f, material, rotation, angular: spin);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(body), body, "Unknown body.");
        }
        return (scene.Admit(), id, rest);
    }

    // ---- flat drops: e² bounce, no spin, no per-tick gain ----

    public static TheoryData<Body, float> FlatDrops()
    {
        var data = new TheoryData<Body, float>();
        foreach (var body in Enum.GetValues<Body>())
            foreach (var e in new[] { 0f, .3f, .6f, 1f })
                data.Add(body, e);
        return data;
    }

    /// <summary>A body dropped 0.5 m flat onto the bench (or a static box top). Sampled at the 120 Hz commit, the first rising tick's
    /// speed over the last falling tick's lies in [e − g·T·(1+e)/v_in, e·(1 + g·T/v_in)] (gravity acts for at most one period around
    /// the impact; ± 2⁻²⁰ for the f32 speed ratio); a flat landing leaves without spin; e = 0 never leaves the support; and no committed
    /// tick gains more than m·g·ContactSlop.</summary>
    [Theory]
    [MemberData(nameof(FlatDrops))]
    public void AFlatDropReboundsAtItsRestitutionWithoutSpinOrEnergyGain(Body body, float e)
    {
        var (world, id, rest) = Drop(body, new LawMaterial(e, .1f, .5f, 0f), .5f);
        double? previous = null; var worstRise = 0.0;
        double vIn = 0; double? vOut = null; var spinOut = 0f; var rising = false; var highestAfterImpact = double.NegativeInfinity;
        var landed = false;
        world.Step(3 * world.TicksPerSecond, (_, state) =>
        {
            var b = state[id];
            var energy = Energy(body, b);
            if (previous is { } p) worstRise = Math.Max(worstRise, energy - p);
            previous = energy;
            // The impact tick: near the support, no longer falling at more than half the last arrival speed.
            if (!landed && vIn > 1 && b.Position.Y - rest < .05f && b.Velocity.Y > -.5 * vIn) landed = true;
            if (!landed && b.Velocity.Y < 0) vIn = -b.Velocity.Y;
            if (landed) highestAfterImpact = Math.Max(highestAfterImpact, b.Position.Y - rest);
            if (landed && !rising && vOut is null && b.Velocity.Y > 0) { vOut = b.Velocity.Y; spinOut = b.Angular.Length(); rising = true; }
        });
        Assert.True(landed, "the body lands");
        Assert.True(worstRise <= Slop, $"a tick gained {worstRise * 1000:F3} mJ (slop {Slop * 1000:F3} mJ)");
        var eh = (float)e;
        if (eh == 0f)
        {
            Assert.True(highestAfterImpact <= .001, $"e = 0 left the support by {highestAfterImpact * 1000:F3} mm");
            return;
        }
        Assert.True(vOut is not null, "the body rebounds");
        var ratio = vOut!.Value / vIn;
        var g = G * T / vIn;
        // The declared e compiles exactly; the f32 speeds' rounding bounds the ratio within 2⁻²⁰.
        Assert.InRange(ratio, eh - g * (1 + eh) - 1.0 / (1 << 20), eh * (1 + g) + 1.0 / (1 << 20));
        Assert.True(spinOut <= 1e-3f, $"a flat landing left spinning at {spinOut} rad/s");
    }

    // ---- random poses: no per-tick gain at any e ----

    public static TheoryData<Body, float> RandomDrops()
    {
        var data = new TheoryData<Body, float>();
        foreach (var body in Enum.GetValues<Body>())
            foreach (var e in new[] { 0f, .3f, .55f, .6f, 1f })
                data.Add(body, e);
        return data;
    }

    /// <summary>Forty seeded poses per body and e (random orientation, spin ±3 rad/s per axis, drop 0.1–0.6 m above the resting
    /// height plus 0.35 m): no committed tick gains more than m·g·ContactSlop, including e = 1. Before this slice a deferred bounce on
    /// a stale approach added up to 8.8 J in one tick.</summary>
    [Theory]
    [MemberData(nameof(RandomDrops))]
    public void RandomPoseDropsNeverGainMoreThanTheSlopInATick(Body body, float e)
    {
        var rng = new Random(4100 + (int)body * 10 + (int)(e * 100));
        var worst = 0.0; var at = -1;
        for (var i = 0; i < 40; i++)
        {
            var q = Quaternion.Normalize(new((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1),
                (float)(rng.NextDouble() * 2 - 1)));
            var spin = new Vector3((float)(rng.NextDouble() * 6 - 3), (float)(rng.NextDouble() * 6 - 3), (float)(rng.NextDouble() * 6 - 3));
            var (world, id, _) = Drop(body, new LawMaterial(e, .1f, .5f, 0f), (float)(rng.NextDouble() * .5 + .1) + .35f, q, spin);
            double? previous = null;
            world.Step(3 * world.TicksPerSecond, (_, state) =>
            {
                var energy = Energy(body, state[id]);
                if (previous is { } p && energy - p > worst) { worst = energy - p; at = i; }
                previous = energy;
            });
        }
        Assert.True(worst <= Slop, $"case {at}: a tick gained {worst * 1000:F3} mJ (slop {Slop * 1000:F3} mJ)");
    }

    // ---- friction during the bounce (Coulomb over the whole normal impulse) ----

    /// <summary>A Basketball (r, m from its declaration; solid sphere I = 0.4 m r²) at 2 m/s with spin ω about z strikes an equal
    /// resting ball head-on (no gravity, drag or rolling resistance). Normal impulse Λ = (m/2)(1+e)v; tangential effective mass
    /// m_t = m/7; initial slip ω r. Sliding throughout when μΛ &lt; m_t·ω r: the struck ball's tangential speed is μ(1+e)v/2 and the
    /// slip keeps its sign; otherwise it sticks and takes ω r/7. Sliding holds to f32 arithmetic (2^-20); sticking within the solver's 1 %
/// onset-slip residual.</summary>
    [Theory]
    [InlineData(10f)]
    [InlineData(.3f)]
    public void ABallStrikeTransfersTheCoulombShareOfTangentialImpulse(float spin)
    {
        var material = LawMaterial.Ball(LawBall.Basketball) with { Rolling = 0f };
        float r = LawBalls.Radius(LawBall.Basketball), m = LawBalls.Mass(LawBall.Basketball);
        var scene = new LawScene();
        var a = scene.Sphere(At(-1, 1), r, m, material, At(2, 0, 0), At(0, 0, spin), 0f, 0f);
        var b = scene.Sphere(At(0, 1), r, m, material, default, default, 0f, 0f);
        var world = scene.Admit();
        var final = world.Step(world.TicksPerSecond);
        double e = material.Restitution * material.Restitution, mu = Math.Sqrt(material.Friction * material.Friction), v = 2;
        var normal = m / 2 * (1 + e) * v;
        var slip0 = spin * r;
        var sliding = mu * normal < m / 7 * slip0;
        var expected = sliding ? mu * normal / m : slip0 / 7;
        Assert.Equal(spin > 1, sliding);
        // Sliding: the transfer is the closed form computed from exact f32 μ, e, m and v, so it holds to f32 arithmetic (2^-20 relative,
        // sixteen roundings of 2^-24). Sticking: the onset of contact slips until the normal impulse can hold the spin, so the struck ball
        // takes slightly less than ω r/7; that is the solver's sticking residual (1 %), not a precision allowance.
        var tolerance = sliding ? 1.0 / (1 << 20) : .01;
        Assert.InRange(final[b].Velocity.Y, (1 - tolerance) * expected, (1 + tolerance) * expected);
        // Contact-point slip after the impact: (v_A + ω_A × r_A − v_B − ω_B × r_B)·ŷ with r_A = +r x̂, r_B = −r x̂.
        var slip = final[a].Velocity.Y + final[a].Angular.Z * r - final[b].Velocity.Y + final[b].Angular.Z * r;
        if (sliding) Assert.True(slip > 0, $"the slip reversed ({slip})");
        else Assert.True(MathF.Abs(slip) < .01f * slip0 + 1e-4f, $"a sticking strike still slips ({slip})");
    }

    // ---- the bounce threshold ----

    /// <summary>A ball (no gravity or drag) meets a static box face at exactly the pair's bounce threshold: no bounce (it stops);
    /// one f32 step faster: it rebounds at e·v.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheBounceThresholdIsStrict(bool above)
    {
        var threshold = .1f;
        var speed = above ? MathF.BitIncrement(threshold) : threshold;
        var scene = new LawScene(bench: false);
        scene.StaticBox(At(0, 0), At(1, .5f, 1), null, new LawMaterial(1f, .1f, 0f, 0f));
        var ball = scene.Sphere(At(0, .5f + .1f + .002f), .1f, 1f, new LawMaterial(.5f, .1f, 0f, 0f), At(0, -speed, 0), default, 0f, 0f);
        var world = scene.Admit();
        var final = world.Step(world.TicksPerSecond)[ball];
        if (above) Assert.InRange(final.Velocity.Y, .5f * speed * .99f, .5f * speed * 1.01f);
        else Assert.True(MathF.Abs(final.Velocity.Y) < 1e-4f, $"a threshold approach bounced ({final.Velocity.Y})");
    }

    // ---- speculative sphere contacts take the touch normal ----

    /// <summary>Frictionless elastic reflection off curved features (no gravity): a ball (r 0.1) off a static cylinder's rim
    /// (r 0.3, half-width 0.4, axis along Z; centre path x = r_cyl + 0.05, so n = (0.05, 0, √(0.1² − 0.05²))/0.1) and off a static
    /// sphere (r 0.3, impact parameter 0.2 = 0.4 sin 30°, n = (cos 30°, sin 30°, 0)). The outgoing velocity is v − 2(v·n)n within 2°
    /// and 1 % of its speed at every speed from 2 to 12 m/s: the bounce is resolved in the substep that stops it, so the contact
    /// normal is taken at the predicted touch, not across the speculative gap.</summary>
    [Theory]
    [InlineData(2f)]
    [InlineData(3f)]
    [InlineData(6f)]
    [InlineData(12f)]
    public void ABallReflectsOffCurvedFeaturesAboutTheTouchNormalAtEverySpeed(float speed)
    {
        var elastic = new LawMaterial(1f, .1f, 0f, 0f);
        var alongZ = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2);
        void Reflects(Action<LawScene> partner, Vector3 start, Vector3 velocity, Vector3 normal, string feature)
        {
            var scene = new LawScene(bench: false);
            partner(scene);
            var ball = scene.Sphere(start, .1f, 1f, elastic, velocity, default, 0f, 0f);
            var world = scene.Admit();
            var final = world.Step(2 * world.TicksPerSecond)[ball];
            var expected = velocity - 2 * Vector3.Dot(velocity, normal) * normal;
            var angle = Math.Acos(Math.Clamp(Vector3.Dot(Vector3.Normalize(final.Velocity), Vector3.Normalize(expected)), -1f, 1f)) * 180 / Math.PI;
            Assert.True(angle < 2, $"{feature} at {speed} m/s: outgoing {final.Velocity} vs {expected} ({angle:F2}°)");
            Assert.InRange(final.Velocity.Length(), .99f * expected.Length(), 1.01f * expected.Length());
        }
        Reflects(s => s.StaticCylinder(At(0, 1), .3f, .4f, alongZ, elastic), At(.3f + .05f, 1, 1.5f), At(0, 0, -speed),
            Vector3.Normalize(new Vector3(.05f, 0, MathF.Sqrt(.01f - .0025f))), "rim");
        Reflects(s => s.StaticSphere(At(0, 1), .3f, Quaternion.Identity, elastic), At(1.5f, 1.2f, 0), At(-speed, 0, 0),
            new Vector3(MathF.Cos(MathF.PI / 6), .5f, 0), "sphere");
    }

    /// <summary>The joined_pipe bend impact, isolated: a ball (r 0.1, 1 kg, Basketball restitution and friction, no rolling
    /// resistance, no gravity) falls at 11.6 m/s onto a static face inclined 60° (n = (−sin 60°, cos 60°, 0)) of the Receiver
    /// material (e = e_ball·e_face, μ = √(μ_ball μ_face)). The impact slides throughout when μ(1+e)|v_n| &lt; |v_t|/3.5 (solid sphere,
    /// m_t = m/3.5): v' = e|v_n| n + (v_t + μ(1+e)|v_n|) t̂ with t̂ along the slip, within 1 % of the speed and 1°. Frictionless
    /// control: the same face with μ = 0 reflects to v − (1+e)(v·n)n within 1°.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ABallOnAnInclinedFaceLeavesWithTheCoulombSlidingReflection(bool friction)
    {
        var ballMaterial = LawMaterial.Ball(LawBall.Basketball) with { Rolling = 0f, Friction = friction ? LawBalls.Friction(LawBall.Basketball) : 0f };
        var face = new LawMaterial(.12f, .1f, friction ? .3f : 0f, 0f);
        var n = new Vector3(-MathF.Sin(MathF.PI / 3), MathF.Cos(MathF.PI / 3), 0);
        var scene = new LawScene(bench: false);
        scene.StaticBox(At(0, 0), At(1, .5f, 1), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 3), face);
        var v = At(0, -11.6f, 0);
        var ball = scene.Sphere(n * (.5f + .1f) + At(0, .5f), .1f, 1f, ballMaterial, v, default, 0f, 0f);
        var world = scene.Admit();
        var final = world.Step(world.TicksPerSecond / 2)[ball];
        double e = (ballMaterial.Restitution) * (double)(face.Restitution), mu = Math.Sqrt((double)(ballMaterial.Friction) * (face.Friction));
        var vn = Vector3.Dot(v, n);
        var vt = v - vn * n;
        var tangent = Vector3.Normalize(vt);
        var normalImpulse = (1 + e) * -vn;
        Assert.True(mu * normalImpulse < vt.Length() / 3.5, "the impact slides throughout");
        var expected = (float)(e * -vn) * n + (float)(vt.Length() - mu * normalImpulse) * tangent;
        var angle = Math.Acos(Math.Clamp(Vector3.Dot(Vector3.Normalize(final.Velocity), Vector3.Normalize(expected)), -1f, 1f)) * 180 / Math.PI;
        Assert.True(angle < 1, $"outgoing {final.Velocity} vs {expected} ({angle:F2}°)");
        Assert.InRange(final.Velocity.Length(), .99f * expected.Length(), 1.01f * expected.Length());
    }

    // ---- a body landing on a resting dynamic body (reviewer F1) ----

    private static readonly Vector3 LowerHalf = At(.3f, .1f, .3f);

    /// <summary>Kinetic plus potential energy (J) of a box of mass m and admitted half-extents.</summary>
    private static double BoxEnergy(LawBody b, float m, Vector3 half)
    {
        var w = Vector3.Transform(b.Angular, Quaternion.Inverse(b.Rotation));
        double x = half.X, y = half.Y, z = half.Z;
        return .5 * m * Squared(b.Velocity) + .5 * m * ((y * y + z * z) / 3 * w.X * w.X + (x * x + z * z) / 3 * w.Y * w.Y + (x * x + y * y) / 3 * w.Z * w.Z) +
            m * G * (b.Position.Y - BenchY);
    }

    /// <summary>A 1 kg box (half 0.25 × 0.1 × 0.15) dropped onto a 2 kg box (half 0.3 × 0.1 × 0.3) resting on the bench, both of
    /// material (e, 0.1, 0.5): the worst per-tick rise of the pair's total energy over 3 s.</summary>
    private static double StackRise(float e, Quaternion rotation, float height, float x, Vector3 spin)
    {
        var material = new LawMaterial(e, .1f, .5f, 0f);
        var scene = new LawScene();
        var lower = scene.Box(At(0, BenchY + .1f), At(.3f, .1f, .3f), 2f, material);
        var upper = scene.Box(At(x, BenchY + .5f + height), At(.25f, .1f, .15f), 1f, material, rotation, angular: spin);
        var world = scene.Admit();
        double? previous = null; var worst = 0.0;
        world.Step(3 * world.TicksPerSecond, (_, state) =>
        {
            var energy = BoxEnergy(state[lower], 2f, LowerHalf) + BoxEnergy(state[upper], 1f, BoxHalf);
            if (previous is { } p) worst = Math.Max(worst, energy - p);
            previous = energy;
        });
        return worst;
    }

    /// <summary>The reviewer's two reproducers (review-engine-restitution.md F1): before the energy-bounded pass the e = 0.95 pose
    /// gained 26.5 mJ (31.5 mJ rise) and the e = 1 pose rose 455.8 mJ in a tick. Bound: Σ m·g·ContactSlop over the pair (3 kg).</summary>
    [Theory]
    [InlineData(.95f, .15044264f, -.12781543f, .7329633f, .6509954f, .7497563f, .06098506f, -3.4555395f, .9491693f, .3111048f)]
    [InlineData(1f, .5394204f, .28104627f, -.5952043f, .5251385f, .5262131f, .06089176f, 1.5234644f, -.7419849f, -3.30802f)]
    public void TheReviewerStackReproducersGainNoMoreThanTheSlop(float e, float qx, float qy, float qz, float qw, float h, float x, float sx,
        float sy, float sz)
    {
        var rise = StackRise(e, new Quaternion(qx, qy, qz, qw), h, x, new Vector3(sx, sy, sz));
        Assert.True(rise <= 3 * Slop, $"a tick gained {rise * 1000:F3} mJ (bound {3 * Slop * 1000:F3} mJ)");
    }

    /// <summary>200 seeded drops of the 1 kg box onto the resting 2 kg box per e (random orientation, spin ±4 rad/s per axis, drop
    /// 0.2–0.8 m, offset 0–0.2 m): no committed tick gains more than Σ m·g·ContactSlop, at every e up to 1.</summary>
    [Theory]
    [InlineData(.3f)]
    [InlineData(.6f)]
    [InlineData(.9f)]
    [InlineData(.95f)]
    [InlineData(1f)]
    public void BoxesDroppedOnARestingDynamicBoxNeverGainMoreThanTheSlopInATick(float e)
    {
        var rng = new Random(5300 + (int)(e * 100));
        var worst = 0.0; var at = -1;
        for (var i = 0; i < 200; i++)
        {
            var q = Quaternion.Normalize(new((float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1), (float)(rng.NextDouble() * 2 - 1),
                (float)(rng.NextDouble() * 2 - 1)));
            var height = (float)(rng.NextDouble() * .6 + .2);
            var x = (float)rng.NextDouble() * .2f;
            var spin = new Vector3((float)(rng.NextDouble() * 8 - 4), (float)(rng.NextDouble() * 8 - 4), (float)(rng.NextDouble() * 8 - 4));
            var rise = StackRise(e, q, height, x, spin);
            if (rise > worst) { worst = rise; at = i; }
        }
        Assert.True(worst <= 3 * Slop, $"case {at}: a tick gained {worst * 1000:F3} mJ (bound {3 * Slop * 1000:F3} mJ)");
    }

    /// <summary>A spinning face has one approaching edge and one separating edge. Net support
    /// cannot attract its centre downwards; the contacting edge responds, while energy remains
    /// bounded by the initial energy plus one slop of work. The lifted control sees gravity only.</summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(.5f)]
    public void APartiallyUnloadingFaceRespondsWithoutTensileAttraction(float restitution)
    {
        var material = new LawMaterial(restitution, .1f, 0f, 0f);
        foreach (var gap in new[] { 0f, .1f })
        {
            var scene = new LawScene();
            var id = scene.Box(At(0, BenchY + BoxHalf.Y + gap), BoxHalf, 1f, material,
                velocity: At(0, 3f), angular: At(0, 0, 20f));
            var world = scene.Admit(SimulationCadence.Hz240);
            var initial = world.Initial(id);
            var final = world.Step(1)[id];
            var gravityStep = G / world.TicksPerSecond;
            // Eight f32 ulps at 3 m/s, selected from the declared speed before observing output.
            const float rounding = 8f * (1f / 4194304f);
            Assert.True(final.Velocity.Y >= initial.Velocity.Y - gravityStep - rounding);
            Assert.True(Energy(Body.BoxOnBench, final) <= Energy(Body.BoxOnBench, initial) + Slop);
            if (gap == 0f)
                Assert.True(final.Velocity.Y > initial.Velocity.Y - gravityStep + .01f,
                    "The approaching edge must deliver real support.");
            else
            {
                Assert.InRange(final.Velocity.Y, initial.Velocity.Y - gravityStep - rounding,
                    initial.Velocity.Y - gravityStep + rounding);
                Assert.InRange(final.Angular.Z, 20f - rounding, 20f + rounding);
            }
        }
    }

    /// <summary>The lower sphere's initial approach is canceled by declared upward
    /// gravity before contact solving. A heavy, nonbouncing upper sphere transfers
    /// motion to it during the solve, so restitution must retain that relaxed energy.
    /// The first published motion segment records post-restitution velocity before
    /// the next substep; no private solver state or observer is read.</summary>
    [Theory]
    [InlineData(.25f, false)]
    [InlineData(1f, false)]
    [InlineData(.25f, true)]
    [InlineData(1f, true)]
    public void ABounceRetainsEnergyTransferredByANonBouncingContactWithoutExceedingIt(float restitution, bool incoming)
    {
        const float radius = .1f, lowerApproach = .02f, upperApproach = .01f, upperMass = 100f;
        var scene = new LawScene(bench: false);
        scene.StaticBox(new(0, 3f, 0), new(1f, .1f, 1f), material: new(1f, .001f, 0f, 0f));
        var lowerY = 3.1f + radius - .0005f;
        var lower = scene.Sphere(new(0, lowerY, 0), radius, 1f, new(restitution, .001f, 0f, 0f),
            new(0, -lowerApproach, 0), default, 0f, 9.6f);
        var upper = incoming
            ? scene.Sphere(new(0, lowerY + 2 * radius - .0005f, 0), radius, upperMass, new(0f, .001f, 0f, 0f),
                new(0, -upperApproach, 0), default, 0f, 0f)
            : default;
        var world = scene.Admit(SimulationCadence.Hz240);
        var end = world.Step(1);
        // PhysicsMotionRead's owner/start-ordinal/velocity lanes are the published
        // coefficient ABI. Advance writes each segment after restitution and
        // velocity settling; this scene has no later contact-work payment.
        var motion = end.World.AsSpan(PhysicsGpuAbi.Layout(end.World).MotionOffset);
        var count = (int)BinaryPrimitives.ReadUInt32LittleEndian(motion);
        var velocities = new Dictionary<GpuBodyId, float>();
        for (var i = 0; i < count; i++)
        {
            var piece = motion.Slice(PhysicsMotionRead.HeaderBytes + i * PhysicsMotionRead.PieceBytes, PhysicsMotionRead.PieceBytes);
            if (BinaryPrimitives.ReadUInt32LittleEndian(piece[PhysicsMotionRead.StartOrdinalOffset..]) != 0) continue;
            var owner = new GpuBodyId(BinaryPrimitives.ReadUInt64LittleEndian(piece[PhysicsMotionRead.OwnerOffset..]));
            var velocity = BinaryPrimitives.ReadSingleLittleEndian(piece[(PhysicsMotionRead.VelocityOffset + sizeof(float))..]);
            Assert.True(float.IsFinite(velocity));
            Assert.True(velocities.TryAdd(owner, velocity));
        }
        Assert.Equal(incoming ? 2 : 1, velocities.Count);
        // 128 f32 roundings at the declared approach and slop/substep scales,
        // independent of observed output (m/s).
        const float allowance = 128f / 16777216f * (.0002f * 480f + lowerApproach + upperApproach);
        var rebound = velocities[lower];
        if (!incoming)
        {
            Assert.InRange(rebound, -allowance, allowance);
            return;
        }

        // Central, frictionless contacts have no torque. The ground contact is
        // visited before the upper pair; the last zero-bias relax therefore gives
        // both spheres the same downward speed before the floor's restitution.
        // q is the inelastic momentum share. During four biased sweeps the lower
        // speed cannot exceed .012; the pair's recovery target adds at most .012.
        // Four relax sweeps multiply the upper speed by q four more times.
        // Both initial .5 mm penetrations remain deep: one substep can recover at
        // most .025 mm. Thus these independent bounds apply to the relaxed speed.
        var q4 = Math.Pow(upperMass / (upperMass + 1.0), 4);
        var minimumRelaxedSpeed = ((upperApproach + .024) * q4 - .024) * q4;
        var transferred = -velocities[upper];
        Assert.InRange(transferred, minimumRelaxedSpeed - allowance, upperApproach + allowance);
        var target = restitution * lowerApproach;
        var expected = restitution < 1f ? target : transferred;
        Assert.True(target < minimumRelaxedSpeed || target > upperApproach);
        Assert.InRange(rebound, expected - allowance, expected + allowance);
        Assert.True(rebound > .004f, "A genuine transferred-energy rebound must survive the limiter.");
    }

    /// <summary>A Basketball set down 2 mm above the bench comes to rest within the resting envelope (0.5 mm) and never rises above
    /// where it was set down: the 2 mm fall (≈ 0.2 m/s) bounces once at most and the bounces decay below the threshold.</summary>
    [Fact]
    public void ABallSetDownTwoMillimetresAboveTheBenchComesToRest()
    {
        var radius = LawBalls.Radius(LawBall.Basketball);
        var scene = new LawScene();
        var start = BenchY + radius + .002f;
        var ball = scene.Ball(LawBall.Basketball, At(0, start));
        var world = scene.Admit();
        var highest = float.NegativeInfinity;
        var final = world.Step(3 * world.TicksPerSecond, (_, state) => highest = MathF.Max(highest, state[ball].Position.Y))[ball];
        Assert.True(highest <= start, $"rose to {(highest - start) * 1000:F3} mm above where it was set down");
        Assert.True(final.Speed < 1e-3 && final.Spin < 1e-3, $"speed {final.Speed}, spin {final.Spin}");
        Assert.InRange(final.Position.Y, BenchY + radius - .0005f, BenchY + radius + .0005f);
    }

    // ---- rolling resistance during the bounce (reviewer F3) ----

    /// <summary>A frictionless ball (r 0.1, 1 kg, e = 0.6, C_rr 0.05) spinning at 20 rad/s about a horizontal axis drops 0.5 m onto
    /// the bench. With μ = 0 only the rolling-resistance row changes its spin, and the row is saturated (it would need I·ω = 0.08 N·m·s),
    /// so the spin falls by C_rr·r·Λ/I over the impact, with Λ the whole normal impulse including the bounce: Λ = m(Δv_y + g·T)
    /// across the impact tick. Without the rolling pass after the bounce the cone holds only the stop's impulse (≈ Λ/(1+e)).
    /// Within 3 %.</summary>
    [Fact]
    public void ABouncingSpinningBallLosesTheRollingResistanceOfTheWholeImpact()
    {
        var scene = new LawScene();
        var ball = scene.Sphere(At(0, BenchY + .1f + .5f), .1f, 1f, new LawMaterial(.6f, .1f, 0f, .05f), default, At(0, 0, 20), 0f,
            LawScene.Gravity);
        var world = scene.Admit();
        LawBody? before = null, after = null, last = null;
        world.Step(world.TicksPerSecond, (_, state) =>
        {
            var b = state[ball];
            if (after is null && last is { } l && l.Velocity.Y < 0 && b.Velocity.Y > 0) { before = l; after = b; }
            last = b;
        });
        Assert.True(before is not null && after is not null, "the ball bounces");
        var impulse = 1f * (after!.Value.Velocity.Y - before!.Value.Velocity.Y + G * (float)T);
        float r = .1f, inertia = .4f * r * r, rolling = .05f * r;
        var expected = rolling * impulse / inertia;
        Assert.InRange(before.Value.Angular.Z - after.Value.Angular.Z, .97f * expected, 1.03f * expected);
    }

    // ---- resting and stacks ----

    /// <summary>Three boxes stacked with small gaps (0.5, 1, 2 mm) and the middle box offset 0 or 10 mm, at e = 0, 0.3 and 0.6.
    /// After 1 s every point moves slower than the 0.1 m/s bounce threshold, so no restitution can act on a settled stack, and the
    /// per-tick motion over the last 2 s stays within 0.05 mm (a tenth of the 0.5 mm resting envelope). Some stacks keep a slow
    /// soft-contact oscillation below that bound on the pre-slice engine too: deferred to "Engine: resting stack settling".</summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(.3f)]
    [InlineData(.6f)]
    public void StacksStopBouncingAndStayWithinTheRestingEnvelope(float e)
    {
        foreach (var gap in new[] { .0005f, .001f, .002f })
            foreach (var offset in new[] { 0f, .01f })
            {
                var scene = new LawScene();
                var hy = .1f;
                var ids = new List<GpuBodyId>();
                for (var k = 0; k < 3; k++)
                    ids.Add(scene.Box(At(k == 1 ? offset : 0, BenchY + hy + k * 2 * hy + gap * (k + 1)), At(.25f, .1f, .15f), 1f,
                        new LawMaterial(e, .1f, .5f, 0f)));
                var world = scene.Admit();
                var last = new Dictionary<GpuBodyId, Vector3>();
                double pointSpeed = 0, lateMotion = 0;
                world.Step(10 * world.TicksPerSecond, (tick, state) =>
                {
                    foreach (var id in ids)
                    {
                        var b = state[id];
                        if (tick > world.TicksPerSecond) pointSpeed = Math.Max(pointSpeed, b.Speed + b.Spin * .3);
                        if (tick > 8 * world.TicksPerSecond && last.TryGetValue(id, out var p)) lateMotion = Math.Max(lateMotion, (b.Position - p).Length());
                        last[id] = b.Position;
                    }
                });
                Assert.True(pointSpeed < .1, $"gap {gap} offset {offset}: a point moves at {pointSpeed} m/s after 1 s");
                Assert.True(lateMotion <= 5e-5, $"gap {gap} offset {offset}: late per-tick motion {lateMotion * 1000:F4} mm");
            }
    }

    // ---- determinism and Reset ----

    /// <summary>A bouncing Basketball, a tumbling box and a cylinder (e = 0.3) on the bench: two runs, and a re-run on the same
    /// engine after reset and re-admission, commit the same identified body states on every tick for 600 ticks at 60, 120 and 240 Hz.</summary>
    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public void BouncingRunsAndAReusedEngineReplayPreserveBodyMotion(SimulationCadence cadence)
    {
        var lively = new LawMaterial(.3f, .1f, .5f, .05f);
        var scene = new LawScene();
        var ball = scene.Ball(LawBall.Basketball, At(-1, BenchY + 1.2f));
        var boxRotation = Quaternion.CreateFromYawPitchRoll(.3f, .5f, .2f);
        var boxAngular = At(1, -2, .5f);
        var box = scene.Box(At(0, BenchY + .8f), At(.25f, .1f, .15f), 1f, lively, boxRotation, angular: boxAngular);
        var cylinder = scene.Cylinder(At(1, BenchY + .9f), .2f, .15f, 1f, lively, Quaternion.CreateFromYawPitchRoll(.1f, .7f, -.4f));
        var world = scene.Admit(cadence);
        var bench = Assert.Single(world.Scene.Bodies.ToArray(), body => body.Motion == RigidMotionKind.Static);
        var authoredBench = new LawBody(At(0, BenchY), Quaternion.Identity, Vector3.Zero, Vector3.Zero, bench.Cell, bench.Local);
        GpuBodyId[] dynamicIds = [ball, box, cylinder];

        void SameState(LawBody expected, LawBody actual, string context)
        {
            Assert.True(expected.Cell == actual.Cell, $"{context}: cell {actual.Cell}, expected {expected.Cell}");
            Assert.True(expected.Local == actual.Local, $"{context}: local {actual.Local}, expected {expected.Local}");
            Assert.True(expected.Position == actual.Position, $"{context}: position {actual.Position}, expected {expected.Position}");
            Assert.True(expected.Rotation == actual.Rotation, $"{context}: rotation {actual.Rotation}, expected {expected.Rotation}");
            Assert.True(expected.Velocity == actual.Velocity, $"{context}: velocity {actual.Velocity}, expected {expected.Velocity}");
            Assert.True(expected.Angular == actual.Angular, $"{context}: angular velocity {actual.Angular}, expected {expected.Angular}");
        }

        List<(int Tick, Dictionary<GpuBodyId, LawBody> Bodies)> Run(WorkshopPhysicsEngine engine, int run)
        {
            var samples = new List<(int Tick, Dictionary<GpuBodyId, LawBody> Bodies)>();
            // Read the committed public ABI before its buffer is reused; retain only typed value snapshots.
            world.Step(600, (tick, state) =>
            {
                var committed = PhysicsGpuAbi.ReadDynamicBodies(state.World);
                Assert.True(committed.Count == dynamicIds.Length, $"run {run}, tick {tick}: dynamic count {committed.Count}, expected 3");
                var bodies = new Dictionary<GpuBodyId, LawBody>();
                foreach (var id in dynamicIds)
                {
                    var context = $"run {run}, tick {tick}, body {id.Value}";
                    Assert.True(committed.TryGet(id, out var read), $"{context}: missing committed body");
                    Assert.True(read.Body.Tick == (ulong)tick, $"{context}: committed tick {read.Body.Tick}");
                    bodies.Add(read.Body.Id, state[read.Body.Id]);
                }
                bodies.Add(bench.Id, state[bench.Id]);
                SameState(authoredBench, bodies[bench.Id], $"run {run}, tick {tick}, static body {bench.Id.Value}");
                samples.Add((tick, bodies));
            }, engine: engine);
            Assert.True(samples.Count == 600, $"run {run}: sample count {samples.Count}, expected 600");
            for (var index = 0; index < samples.Count; index++)
            {
                Assert.True(samples[index].Tick == index + 1, $"run {run}, sample {index}: tick {samples[index].Tick}");
                foreach (var (id, body) in samples[index].Bodies)
                {
                    var context = $"run {run}, tick {samples[index].Tick}, body {id.Value}";
                    Assert.True(body.Finite, $"{context}: non-finite state");
                    // Declared game-grade envelope; these bounds are independent of replay equality.
                    Assert.True(body.Speed <= 64d, $"{context}: speed {body.Speed} exceeds 64 m/s");
                    Assert.True(body.Spin <= 128d, $"{context}: spin {body.Spin} exceeds 128 rad/s");
                    Assert.True(MathF.Abs(body.Rotation.Length() - 1f) <= 1e-4f, $"{context}: non-unit quaternion {body.Rotation}");
                }
            }
            return samples;
        }
        var engine = new WorkshopPhysicsEngine();
        foreach (var first in new[] { Run(engine, 1), Run(new WorkshopPhysicsEngine(), 2), Run(engine, 3) })
        {
        // Gravity must lower both elevated bodies; nonzero ball restitution must reverse descent at the bench.
        var falling = first.FindIndex(sample => sample.Bodies[ball].Velocity.Y < 0f
            && sample.Bodies[ball].Position.Y < world.Initial(ball).Position.Y);
        Assert.InRange(falling, 0, 598);
        var rebound = first.FindIndex(falling + 1, sample => sample.Bodies[ball].Velocity.Y > 0f);
        Assert.InRange(rebound, falling + 1, 598);
        Assert.True(first[rebound + 1].Bodies[ball].Position.Y > first[rebound].Bodies[ball].Position.Y,
            $"run 1, tick {first[rebound].Tick}, body {ball.Value}: rebound must rise on the following tick");
        var interval = 1f / world.TicksPerSecond;
        var radius = LawBalls.Radius(LawBall.Basketball);
        var drop = 1.2f - radius;
        // Free-fall impact speed bounds this dissipative drop. One committed interval spans the contact
        // and upward travel; g*T² covers discrete gravity, with the declared 0.5 mm contact slop.
        var contactWindow = MathF.Sqrt(2f * G * drop) * interval + G * interval * interval + .0005f;
        var reboundHeight = first[rebound].Bodies[ball].Position.Y;
        Assert.True(MathF.Abs(reboundHeight - (BenchY + radius)) <= contactWindow,
            $"run 1, tick {first[rebound].Tick}, body {ball.Value}: rebound height {reboundHeight}, expected {BenchY + radius} ± {contactWindow}");
        Assert.Contains(first, sample => sample.Bodies[cylinder].Position.Y < world.Initial(cylinder).Position.Y
            && sample.Bodies[cylinder].Velocity.Y < 0f);

        // A visibly rotating box must change its orientation in each fresh or reused run.
        var initialAxis = Vector3.Normalize(Vector3.Transform(Vector3.UnitX, boxRotation));
        Assert.Contains(first, sample => Vector3.Distance(initialAxis,
            Vector3.Transform(Vector3.UnitX, sample.Bodies[box].Rotation)) > .05f);
        Assert.All(dynamicIds, id => Assert.True(first[^1].Bodies[id].Position.Y >= BenchY - .01f,
            $"body {id.Value} remains supported above the bench"));
        }
    }

    // ---- supported impacts (engine-supported-impact-restitution) ----

    /// <summary>A ball dropped 0.6 m onto a dynamic box resting on the bench rebounds at its supported limit without yielding into the support
    /// or gaining thermodynamic energy. With e = 0.55 (effective e² ≈ 0.0918), the rebound apex ratio recovers to >= 0.08 (2 kg box) and >= 0.07 (0.5 kg box).
    /// With e = 1.0, the rebound apex ratio recovers to >= 0.90 for both 0.3 kg and 2 kg boxes (within 5% of the static limit 0.9764).</summary>
    [Theory]
    [InlineData(2f, .55f, .08f)]
    [InlineData(.5f, .55f, .07f)]
    [InlineData(2f, 1f, .90f)]
    [InlineData(.3f, 1f, .90f)]
    public void ABallDroppedOntoARestingDynamicBoxReboundsAtItsSupportedLimit(float boxMass, float e, float minRatio)
    {
        const float ballMass = .6f, radius = .12f, drop = .6f;
        var boxHalf = At(.3f, .1f, .3f);
        var material = new LawMaterial(e, .1f, .5f, 0f);
        var scene = new LawScene();
        var box = scene.Box(At(0, BenchY + boxHalf.Y), boxHalf, boxMass, material);
        var restY = BenchY + 2 * boxHalf.Y + radius;
        var ball = scene.Sphere(At(0, restY + drop), radius, ballMass, material, default, default, 0f, LawScene.Gravity);
        var world = scene.Admit();

        var landed = false;
        var highestAfterImpact = float.NegativeInfinity;
        double? previousEnergy = null;
        var worstEnergyRise = 0.0;

        world.Step(3 * world.TicksPerSecond, (_, state) =>
        {
            var bState = state[ball];
            var boxState = state[box];

            var currentEnergy = .5 * ballMass * Squared(bState.Velocity) + ballMass * G * (bState.Position.Y - BenchY)
                + BoxEnergy(boxState, boxMass, boxHalf);
            if (previousEnergy is { } prev) worstEnergyRise = Math.Max(worstEnergyRise, currentEnergy - prev);
            previousEnergy = currentEnergy;

            if (!landed && bState.Position.Y - restY < .05f && bState.Velocity.Y > -1f)
                landed = true;

            if (landed)
            {
                var reboundHeight = bState.Position.Y - restY;
                if (reboundHeight > highestAfterImpact)
                    highestAfterImpact = reboundHeight;
            }
        });

        Assert.True(landed, "the ball lands on the resting box");
        var ratio = highestAfterImpact / drop;
        Assert.True(ratio >= minRatio, $"rebound apex ratio was {ratio:F4}, expected >= {minRatio:F4}");
        Assert.True(worstEnergyRise <= 3 * Slop, $"a tick gained {worstEnergyRise * 1000:F3} mJ (bound {3 * Slop * 1000:F3} mJ)");
    }

    /// <summary>Inelastic control: an e = 0 ball dropped 0.6 m onto a resting dynamic box dissipates its approach and never rebounds
    /// (stays within 1 mm of the resting contact height).</summary>
    [Theory]
    [InlineData(2f)]
    [InlineData(.5f)]
    public void AnInelasticBallOnARestingBoxDoesNotRebound(float boxMass)
    {
        const float ballMass = .6f, radius = .12f, drop = .6f;
        var boxHalf = At(.3f, .1f, .3f);
        var material = new LawMaterial(0f, .1f, .5f, 0f);
        var scene = new LawScene();
        var box = scene.Box(At(0, BenchY + boxHalf.Y), boxHalf, boxMass, material);
        var restY = BenchY + 2 * boxHalf.Y + radius;
        var ball = scene.Sphere(At(0, restY + drop), radius, ballMass, material, default, default, 0f, LawScene.Gravity);
        var world = scene.Admit();

        var landed = false;
        var highestAfterImpact = float.NegativeInfinity;

        world.Step(3 * world.TicksPerSecond, (_, state) =>
        {
            var bState = state[ball];
            if (!landed && bState.Position.Y - restY < .05f && bState.Velocity.Y > -1f)
                landed = true;

            if (landed)
            {
                var reboundHeight = bState.Position.Y - restY;
                if (reboundHeight > highestAfterImpact)
                    highestAfterImpact = reboundHeight;
            }
        });

        Assert.True(landed, "the ball lands on the resting box");
        Assert.True(highestAfterImpact <= .001f, $"e = 0 left the resting height by {highestAfterImpact * 1000:F3} mm (limit 1 mm)");
    }

    /// <summary>Unsupported control: in mid-air zero gravity with no support, a ball striking a dynamic box conserves linear momentum
    /// and separates at exactly e * |v_rel|.</summary>
    [Theory]
    [InlineData(.55f)]
    [InlineData(1f)]
    public void AFreeFallingMidAirCollisionConservesMomentumAndSeparatesAtRestitution(float e)
    {
        const float ballMass = .6f, boxMass = 2f, radius = .12f;
        var boxHalf = At(.3f, .1f, .3f);
        var material = new LawMaterial(MathF.Sqrt(e), .1f, 0f, 0f);
        var scene = new LawScene(bench: false);
        var box = scene.Box(At(0, 0, 0), boxHalf, boxMass, material, velocity: default, gravity: 0f);
        var ball = scene.Sphere(At(0, boxHalf.Y + radius + .01f, 0), radius, ballMass, material,
            velocity: At(0, -3f, 0), angular: default, drag: 0f, gravity: 0f);
        var world = scene.Admit();

        var initialP = ballMass * (-3f);
        var final = world.Step(world.TicksPerSecond / 4);
        var finalBall = final[ball];
        var finalBox = final[box];

        var finalP = ballMass * finalBall.Velocity.Y + boxMass * finalBox.Velocity.Y;
        Assert.InRange(finalP, initialP - 1e-4f, initialP + 1e-4f);

        var sep = finalBall.Velocity.Y - finalBox.Velocity.Y;
        var expectedSep = e * 3f;
        Assert.InRange(sep, expectedSep - 1e-3f, expectedSep + 1e-3f);
        Assert.InRange(MathF.Abs(finalBall.Velocity.X), 0f, 1e-4f);
        Assert.InRange(MathF.Abs(finalBall.Velocity.Z), 0f, 1e-4f);
        Assert.InRange(MathF.Abs(finalBox.Velocity.X), 0f, 1e-4f);
        Assert.InRange(MathF.Abs(finalBox.Velocity.Z), 0f, 1e-4f);
        Assert.InRange(finalBall.Angular.Length(), 0f, 1e-4f);
        Assert.InRange(finalBox.Angular.Length(), 0f, 1e-4f);
    }

    /// <summary>An off-center drop on a supported dynamic box couples normal impact to pitch/roll inertia,
    /// rebounding positively while inducing coupled angular spin without gaining thermodynamic energy.</summary>
    [Fact]
    public void AnOffCenterDropOnARestingBoxReboundsWithCoupledSpin()
    {
        const float upperMass = 1f, lowerMass = 2f, drop = .6f, offsetX = .15f;
        var lowerHalf = At(.3f, .1f, .3f);
        var upperHalf = At(.25f, .1f, .15f);
        var material = new LawMaterial(1f, .1f, .5f, 0f);
        var scene = new LawScene();
        var lower = scene.Box(At(0, BenchY + lowerHalf.Y), lowerHalf, lowerMass, material);
        var restY = BenchY + 2 * lowerHalf.Y + upperHalf.Y;
        var upper = scene.Box(At(offsetX, restY + drop), upperHalf, upperMass, material);
        var world = scene.Admit();

        var landed = false;
        var highestAfterImpact = float.NegativeInfinity;
        var maxUpperSpin = 0f;
        double? previousEnergy = null;
        var worstEnergyRise = 0.0;

        world.Step(3 * world.TicksPerSecond, (_, state) =>
        {
            var uState = state[upper];
            var lState = state[lower];

            var currentEnergy = BoxEnergy(lState, lowerMass, lowerHalf) + BoxEnergy(uState, upperMass, upperHalf);
            if (previousEnergy is { } prev) worstEnergyRise = Math.Max(worstEnergyRise, currentEnergy - prev);
            previousEnergy = currentEnergy;

            maxUpperSpin = MathF.Max(maxUpperSpin, uState.Angular.Length());

            if (!landed && uState.Position.Y - restY < .05f && uState.Velocity.Y > -1f)
                landed = true;

            if (landed)
            {
                var reboundHeight = uState.Position.Y - restY;
                if (reboundHeight > highestAfterImpact)
                    highestAfterImpact = reboundHeight;
            }
        });

        Assert.True(landed, "the upper box lands on the resting box");
        var ratio = highestAfterImpact / drop;
        Assert.True(ratio >= .20f, $"rebound apex ratio was {ratio:F4}, expected >= 0.20");
        Assert.True(maxUpperSpin > 1.0f, $"off-center impact induced upper spin {maxUpperSpin:F4} rad/s (expected > 1.0 rad/s)");
        Assert.True(worstEnergyRise <= 3 * Slop, $"a tick gained {worstEnergyRise * 1000:F3} mJ (bound {3 * Slop * 1000:F3} mJ)");
    }

    /// <summary>A ball dropped onto a dynamic two-box stack (box 1 on box 2 on bench) transmits impact through the transitive
    /// support island and rebounds off the stack without collapsing either box.</summary>
    [Fact]
    public void ABallDroppedOntoATwoBoxDynamicStackReboundsWithoutCollapsing()
    {
        const float ballMass = .6f, radius = .12f, drop = .6f;
        var boxHalf2 = At(.4f, .1f, .4f);
        var boxHalf1 = At(.3f, .1f, .3f);
        var material = new LawMaterial(1f, .1f, .5f, 0f);
        var scene = new LawScene();

        var box2 = scene.Box(At(0, BenchY + boxHalf2.Y), boxHalf2, 2f, material);
        var box1 = scene.Box(At(0, BenchY + 2 * boxHalf2.Y + boxHalf1.Y), boxHalf1, 1f, material);

        var restY = BenchY + 2 * boxHalf2.Y + 2 * boxHalf1.Y + radius;
        var ball = scene.Sphere(At(0, restY + drop), radius, ballMass, material, default, default, 0f, LawScene.Gravity);
        var world = scene.Admit();

        var landed = false;
        var highestAfterImpact = float.NegativeInfinity;
        double? previousEnergy = null;
        var worstEnergyRise = 0.0;
        var minBox2Y = float.PositiveInfinity;
        var minBox1Y = float.PositiveInfinity;

        world.Step(3 * world.TicksPerSecond, (_, state) =>
        {
            var bState = state[ball];
            var box1State = state[box1];
            var box2State = state[box2];

            minBox2Y = MathF.Min(minBox2Y, box2State.Position.Y);
            minBox1Y = MathF.Min(minBox1Y, box1State.Position.Y);

            var currentEnergy = .5 * ballMass * Squared(bState.Velocity) + ballMass * G * (bState.Position.Y - BenchY)
                + BoxEnergy(box1State, 1f, boxHalf1) + BoxEnergy(box2State, 2f, boxHalf2);
            if (previousEnergy is { } prev) worstEnergyRise = Math.Max(worstEnergyRise, currentEnergy - prev);
            previousEnergy = currentEnergy;

            if (!landed && bState.Position.Y - restY < .05f && bState.Velocity.Y > -1f)
                landed = true;

            if (landed)
            {
                var reboundHeight = bState.Position.Y - restY;
                if (reboundHeight > highestAfterImpact)
                    highestAfterImpact = reboundHeight;
            }
        });

        Assert.True(landed, "the ball lands on the two-box dynamic stack");
        var ratio = highestAfterImpact / drop;
        Assert.True(ratio >= .05f, $"rebound apex ratio was {ratio:F4}, expected >= 0.05");
        Assert.True(minBox2Y >= BenchY + boxHalf2.Y - .01f, $"bottom box settled to {minBox2Y:F4}, expected >= {BenchY + boxHalf2.Y - .01f:F4}");
        Assert.True(minBox1Y >= BenchY + 2 * boxHalf2.Y + boxHalf1.Y - .01f, $"top box settled to {minBox1Y:F4}, expected >= {BenchY + 2 * boxHalf2.Y + boxHalf1.Y - .01f:F4}");
        Assert.True(worstEnergyRise <= 3 * Slop, $"a tick gained {worstEnergyRise * 1000:F3} mJ (bound {3 * Slop * 1000:F3} mJ)");
    }
}
