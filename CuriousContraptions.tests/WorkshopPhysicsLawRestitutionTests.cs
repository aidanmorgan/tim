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

    // ---- the block solve's fallback (reviewer F2) ----

    /// <summary>A 1 kg box (half 0.25 × 0.1 × 0.15, identity pose) touching a static support with two bottom contact points, normal +Y.</summary>
    private static (SimBody Box, ContactPoint P, ContactPoint Q) Pair(Vector3 rp, Vector3 rq, Vector3 velocity, Vector3 angular,
        float relP, float relQ)
    {
        float ix = (.1f * .1f + .15f * .15f) / 3f, iy = (.25f * .25f + .15f * .15f) / 3f,
            iz = (.25f * .25f + .1f * .1f) / 3f;
        var box = new SimBody
        {
            Motion = RigidMotionKind.Dynamic, Mass = 1f, InvMass = 1f, InvI = new(1f / ix, 1f / iy, 1f / iz), Principal = Quaternion.Identity, Qw = 1f,
            Vx = velocity.X, Vy = velocity.Y, Vz = velocity.Z, Wx = angular.X, Wy = angular.Y, Wz = angular.Z,
        };
        box.InvIWorld[0] = 1f / ix; box.InvIWorld[4] = 1f / iy; box.InvIWorld[8] = 1f / iz;
        var support = new SimBody { Motion = RigidMotionKind.Static, Qw = 1f };
        ContactPoint Point(Vector3 r, float approach)
        {
            var c = new ContactPoint
            {
                A = box, B = support, Nx = 0f, Ny = 1f, Nz = 0f, RAx = r.X, RAy = r.Y, RAz = r.Z,
                Restitution = .5f, Threshold = .1f, MaxImpulse = 1f, RelVel = approach,
            };
            c.NormalMass = WorkshopConstraintSolver.EffectiveMass(c, Vector3.UnitY);
            return c;
        }
        return (box, Point(rp, relP), Point(rq, relQ));
    }

    private static float Vn(ContactPoint c) => Vector3.Dot(WorkshopConstraintSolver.RelativeVelocity(c), c.Normal);

    /// <summary>Two bouncing corners of a spinning box. When the simultaneous solve pushes both, each leaves at exactly −e·(its own
    /// approach). When it would pull one (that corner already separates far faster than its target), the manifold falls back to one
    /// point at a time, each accumulated impulse clamped at zero.
    /// The approaching corner leaves at exactly −e·(its approach), the separating corner takes no (and never a negative) impulse,
    /// and nothing is NaN. Two coincident points (a rank-deficient block, regularised) share the impulse equally and leave at the
    /// target. A block solve carries the normal block's regularisation (10⁻³ of the mean diagonal added to K), so its corners land
    /// within 1 % of the larger target; the one-at-a-time rows are exact.</summary>
    [Fact]
    public void APullingOrRankDeficientBlockFallsBackWithoutNaNAndEachPointBouncesToItsTarget()
    {
        // Simultaneous: spin 6 rad/s about z with vy = −0.5 (corners at −2 and +1 m/s): both impulses push, both corners at target.
        var (_, s, t) = Pair(At(-.25f, -.1f), At(.25f, -.1f), At(0, -.5f), At(0, 0, 6), -2f, -.2f);
        WorkshopConstraintSolver.ApplyRestitution([s, t]);
        Assert.True(s.LambdaN > 0f && t.LambdaN > 0f, $"block impulses {s.LambdaN}, {t.LambdaN}");
        Assert.InRange(Vn(s), .99f, 1.01f);
        Assert.InRange(Vn(t), .09f, .11f);

        // Pulling: spin 20 rad/s with vy = 3 (corners at −2 and +8 m/s); the block would pull the second corner (λ < 0).
        var (_, p, q) = Pair(At(-.25f, -.1f), At(.25f, -.1f), At(0, 3f), At(0, 0, 20), -2f, -.2f);
        WorkshopConstraintSolver.ApplyRestitution([p, q]);
        Assert.True(float.IsFinite(p.LambdaN) && float.IsFinite(q.LambdaN), $"impulses {p.LambdaN}, {q.LambdaN}");
        Assert.True(p.LambdaN > 0f && q.LambdaN == 0f, $"the separating corner was pulled ({q.LambdaN})");
        Assert.InRange(Vn(p), 1f - 1e-4f, 1f + 1e-4f);
        Assert.True(Vn(q) >= .1f, $"the separating corner fell below its own target ({Vn(q)})");

        var (_, a, b) = Pair(At(.25f, -.1f), At(.25f, -.1f), At(0, -2f), default, -2f, -2f);
        WorkshopConstraintSolver.ApplyRestitution([a, b]);
        // Equal shares, to the f32 elimination's rounding.
        Assert.True(float.IsFinite(a.LambdaN) && float.IsFinite(b.LambdaN) && MathF.Abs(a.LambdaN - b.LambdaN) <= 1e-4f * a.LambdaN && a.LambdaN > 0f,
            $"coincident impulses {a.LambdaN}, {b.LambdaN}");
        Assert.InRange(Vn(a), .99f, 1.01f);
    }

    /// <summary>The restitution pass's energy bound is max(kinetic energy before the contact solve, after the relax sweeps) per group
    /// (review R2-3; mutant MB2, pre-solve only, survived without this). A 1 kg box whose solve added kinetic energy this substep
    /// (pre-solve 0 J, relaxed 0.5 J at v_y = −1 m/s): a bounce that stays within the relaxed energy (e = 0.25 on a 2 m/s approach,
    /// v_y → +0.5 m/s, 0.125 J) is kept whole; one beyond it (e = 1, v_y → +2 m/s, 2 J) is scaled back along the line from −1 m/s to
    /// +2 m/s to the relaxed 0.5 J, which is v_y = +1 m/s.</summary>
    [Fact]
    public void TheEnergyBoundKeepsABounceWithinTheRelaxedEnergyAndScalesOneBeyondIt()
    {
        var (kept, p, _) = Pair(At(0, -.1f), At(0, -.1f), At(0, -1f), default, -2f, -2f);
        p.Restitution = .25f;
        kept.PreSolveKinetic = 0f;
        WorkshopConstraintSolver.RestitutionPass([new ContactManifold { Points = [p] }], [], 1);
        Assert.InRange(kept.Vy, .5f - 1e-5f, .5f + 1e-5f);

        var (scaled, q, _) = Pair(At(0, -.1f), At(0, -.1f), At(0, -1f), default, -2f, -2f);
        q.Restitution = 1f;
        scaled.PreSolveKinetic = 0f;
        WorkshopConstraintSolver.RestitutionPass([new ContactManifold { Points = [q] }], [], 1);
        Assert.InRange(scaled.Vy, 1f - 1e-4f, 1f + 1e-4f);
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
        GpuBodyId[] ids = [bench.Id, .. dynamicIds];

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
        var first = Run(engine, 1);
        var replays = new[] { Run(new WorkshopPhysicsEngine(), 2), Run(engine, 3) };
        for (var run = 0; run < replays.Length; run++)
            for (var index = 0; index < first.Count; index++)
            {
                Assert.True(first[index].Tick == replays[run][index].Tick, $"run {run + 2}, sample {index}: replay tick mismatch");
                foreach (var id in ids)
                    SameState(first[index].Bodies[id], replays[run][index].Bodies[id],
                        $"run {run + 2}, tick {first[index].Tick}, body {id.Value}");
            }

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

        // The first tick is free flight: the box begins 0.8 m above the bench and its half-diagonal
        // is under 0.31 m. A unit axis initially moves at |ω × axis|. This box's principal inertia
        // ratio is under 3, so 4|ω|²*T² conservatively bounds gyro/axis-curvature remainder over T≤1/60.
        // Subtract 32 f32 unit roundoffs for the normalized axis transforms; q and -q give the same axis.
        var initialAxis = Vector3.Normalize(Vector3.Transform(Vector3.UnitX, boxRotation));
        var firstAxis = Vector3.Normalize(Vector3.Transform(Vector3.UnitX, first[0].Bodies[box].Rotation));
        var minimumAxisMotion = Vector3.Cross(boxAngular, initialAxis).Length() * interval
            - 4f * boxAngular.LengthSquared() * interval * interval - 32f * MathF.Pow(2f, -24f);
        Assert.True(minimumAxisMotion > 0f, "authored first-tick angular motion must exceed its derived f32/remainder bound");
        Assert.True(Vector3.Distance(initialAxis, firstAxis) >= minimumAxisMotion,
            $"run 1, tick 1, body {box.Value}: axis motion {Vector3.Distance(initialAxis, firstAxis)}, minimum {minimumAxisMotion}");
    }
}
