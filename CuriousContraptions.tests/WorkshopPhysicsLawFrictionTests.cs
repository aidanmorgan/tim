using System.Numerics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

public sealed class WorkshopPhysicsLawFrictionTests
{
    public enum Support { Point, Line, Face }

    // A 50 ms interval keeps the 5 m/s slide and 10 rad/s spin away from stopping.
    // Linear momentum measures the actual normal impulse, including initial settling;
    // no nominal mg*t approximation conceals speculative/soft normal response.
    [Theory]
    [InlineData(Support.Point, 0f, false)]
    [InlineData(Support.Point, .3f, false)]
    [InlineData(Support.Line, 0f, false)]
    [InlineData(Support.Line, .3f, false)]
    [InlineData(Support.Face, 0f, false)]
    [InlineData(Support.Face, .3f, false)]
    [InlineData(Support.Point, 0f, true)]
    [InlineData(Support.Point, .3f, true)]
    [InlineData(Support.Line, 0f, true)]
    [InlineData(Support.Line, .3f, true)]
    [InlineData(Support.Face, 0f, true)]
    [InlineData(Support.Face, .3f, true)]
    public void SlidingUsesTheSharedNormalLoadAndSpinSharesItsBudget(Support support, float friction, bool spin)
    {
        var scene = new LawScene();
        var material = new LawMaterial(0, .1f, friction, 0);
        var v = new Vector3(5, 0, 0);
        var w = spin ? new Vector3(0, 10, 0) : Vector3.Zero;
        var id = support switch
        {
            Support.Point => scene.Sphere(new(0, LawScene.BenchY + .3f, 0), .3f, 1, material, v, w, 0, LawScene.Gravity),
            Support.Line => scene.Cylinder(new(0, LawScene.BenchY + .3f, 0), .3f, .2f, 1, material,
                new Quaternion(.5f, .5f, .5f, .5f), v, w),
            Support.Face => scene.Box(new(0, LawScene.BenchY + .2f, 0), new(.2f), 1, material, velocity: v, angular: w),
            _ => throw new ArgumentOutOfRangeException(nameof(support))
        };
        var world = scene.Admit();
        var initial = world.Initial(id);
        var end = world.Step(6)[id];
        Assert.True(Energy(end, support) <= Energy(initial, support) - LawScene.Gravity * .0002,
            "contact energy must stay within the unchanged one-body slop budget");
        var normalImpulse = end.Velocity.Y - LawScene.Gravity * .05f;
        var mu = MathF.Sqrt(.3f * friction);
        var tangentImpulse = new Vector2(end.Velocity.X - 5, end.Velocity.Z).Length();
        // The unchanged slop envelope over this interval is 0.5 mm / .05s = .01 m/s.
        const float tolerance = .01f;
        Assert.InRange(tangentImpulse, 0, mu * normalImpulse + tolerance);
        if (friction == 0f) Assert.InRange(tangentImpulse, 0, 1e-5f);
        else if (!spin || support == Support.Point)
            Assert.InRange(tangentImpulse, mu * normalImpulse - tolerance, mu * normalImpulse + tolerance);
        else
        {
            Assert.True(tangentImpulse > .01f);
            var radius = support == Support.Line ? .2f : MathF.Sqrt(2) * .2f;
            var inertia = support == Support.Line ? .3f * .3f / 4 + .2f * .2f / 3 : 2 * .2f * .2f / 3;
            var scaledTorque = inertia * (10 - end.Angular.Y) / radius;
            var totalImpulse = MathF.Sqrt(tangentImpulse * tangentImpulse + scaledTorque * scaledTorque);
            Assert.InRange(totalImpulse, mu * normalImpulse - tolerance, mu * normalImpulse + tolerance);
        }

        if (support == Support.Point)
            Assert.InRange(end.Angular.Y, w.Y - tolerance, w.Y + tolerance); // zero geometric twist radius
        else if (spin && friction > 0f)
            Assert.True(end.Angular.Y < w.Y - .01f, "finite patch must dissipate twist");
        if (spin && friction == 0f)
            Assert.InRange(end.Angular.Y, w.Y - tolerance, w.Y + tolerance);
    }

    [Theory]
    [InlineData(Support.Line, 0f)]
    [InlineData(Support.Line, .3f)]
    [InlineData(Support.Face, 0f)]
    [InlineData(Support.Face, .3f)]
    public void PureTwistSpendsThePatchRadiusTimesCoulombImpulse(Support support, float friction)
    {
        var scene = new LawScene();
        var material = new LawMaterial(0, .1f, friction, 0);
        var id = support == Support.Line
            ? scene.Cylinder(new(0, LawScene.BenchY + .3f, 0), .3f, .2f, 1, material,
                new Quaternion(.5f, .5f, .5f, .5f), angular: new(0, 10, 0))
            : scene.Box(new(0, LawScene.BenchY + .2f, 0), new(.2f), 1, material, angular: new(0, 10, 0));
        var world = scene.Admit();
        var initial = world.Initial(id);
        var end = world.Step(6)[id];
        Assert.True(Energy(end, support) <= Energy(initial, support) - LawScene.Gravity * .0002,
            "pure twist must stay within the unchanged one-body slop energy budget");
        var normalImpulse = end.Velocity.Y - LawScene.Gravity * .05f;
        var radius = support == Support.Line ? .2f : MathF.Sqrt(2) * .2f;
        var inertia = support == Support.Line ? .3f * .3f / 4 + .2f * .2f / 3 : 2 * .2f * .2f / 3;
        var expected = MathF.Sqrt(.3f * friction) * normalImpulse * radius;
        var torqueImpulse = inertia * (10 - end.Angular.Y);
        // Allow one substep's normal load for moving contact geometry; independent
        // f32 error is far below this 480 Hz integration allowance.
        // Zero friction leaves yaw invariant by symmetry: a vertical normal has
        // no yaw moment. Bound accumulated f32 additions over 24 substeps,
        // four contact rows, eight sweeps and two rounded additions per row.
        var spinRounding = 24f * 4 * 8 * 2 * 10 / 16777216f;
        var tolerance = MathF.Sqrt(.3f * friction) * -LawScene.Gravity / 480 * radius + inertia * spinRounding;
        Assert.InRange(torqueImpulse, expected - tolerance, expected + tolerance);
        if (friction == 0f)
            Assert.InRange(end.Angular.Y, initial.Angular.Y - spinRounding, initial.Angular.Y + spinRounding);
        Assert.InRange(new Vector2(end.Velocity.X, end.Velocity.Z).Length(), 0, .01f);
    }


    [Theory]
    [InlineData(Support.Line, 0f, 1f)]
    [InlineData(Support.Line, .3f, 1f)]
    [InlineData(Support.Face, 0f, 1f)]
    [InlineData(Support.Face, .3f, 1f)]
    [InlineData(Support.Line, 0f, .02f)]
    [InlineData(Support.Line, .3f, .02f)]
    [InlineData(Support.Face, 0f, .02f)]
    [InlineData(Support.Face, .3f, .02f)]
    public void FiniteTwistStopsWithoutReversalAndStaysAtRestWhileZeroFrictionPreservesSlowSpin(
        Support support, float friction, float initialSpin)
    {
        var scene = new LawScene();
        var material = new LawMaterial(0, .1f, friction, 0);
        var id = support == Support.Line
            ? scene.Cylinder(new(0, LawScene.BenchY + .3f, 0), .3f, .2f, 1, material,
                new Quaternion(.5f, .5f, .5f, .5f), angular: new(0, initialSpin, 0))
            : scene.Box(new(0, LawScene.BenchY + .2f, 0), new(.2f), 1, material, angular: new(0, initialSpin, 0));
        var world = scene.Admit();
        var initial = world.Initial(id);
        var radius = support == Support.Line ? .2 : Math.Sqrt(2) * .2;
        var inertia = support == Support.Line ? .3 * .3 / 4 + .2 * .2 / 3 : 2 * .2 * .2 / 3;
        var deceleration = .3 * -LawScene.Gravity * radius / inertia;
        var stopTime = initialSpin / deceleration;
        // Starting tangent: free fall across contact slop bounds settling delay.
        // Add one committed interval to sample the independently predicted stop.
        var stopDeadline = stopTime + Math.Sqrt(2 * WorkshopConstraintSolver.ContactSlop / -LawScene.Gravity)
            + 1.0 / world.TicksPerSecond;
        var restTick = (int)Math.Ceiling(stopDeadline * world.TicksPerSecond);
        // The existing 50 micrometre post-stop envelope over a one-second window
        // bounds both material-point drift and its equivalent angular speed.
        const double restDistance = .00005;
        var spinError = restDistance / radius;
        LawBody? rest = null;
        LawBody? settled = null;
        var settledTick = Math.Max(restTick, world.TicksPerSecond);
        world.Step(settledTick + world.TicksPerSecond, (tick, state) =>
        {
            var body = state[id];
            Assert.True(Energy(body, support) <= Energy(initial, support) - LawScene.Gravity * .0002);
            Assert.InRange(Math.Abs(body.Position.Y - initial.Position.Y), 0, .0005);
            Assert.InRange(body.Angular.Y, -spinError, initialSpin + spinError);
            if (friction == 0)
            {
                Assert.InRange(body.Angular.Y, initialSpin - spinError, initialSpin + spinError);
                return;
            }
            var time = (double)tick / world.TicksPerSecond;
            var timingAllowance = stopDeadline - stopTime;
            var earliest = Math.Max(0, initialSpin - deceleration * (time + timingAllowance));
            var latest = Math.Max(0, initialSpin - deceleration * Math.Max(0, time - timingAllowance));
            Assert.InRange(body.Angular.Y, earliest - spinError, latest + spinError);
            if (tick < restTick) return;
            Assert.InRange(Math.Abs(body.Angular.Y), 0, spinError);
            rest ??= body;
            // Stopping twist does not stop normal settling. Tangential drift
            // is bounded immediately; full3D rest uses the final one-second
            // window after one second of ordinary contact settling.
            var translation = body.Position - rest.Value.Position;
            Assert.InRange(new Vector2(translation.X, translation.Z).Length(), 0, restDistance);
            if (tick >= settledTick)
            {
                settled ??= body;
                Assert.InRange(Vector3.Distance(body.Position, settled.Value.Position), 0, restDistance);
            }
            var localPoint = support == Support.Line ? new Vector3(0, .2f, 0) : new Vector3(.2f, 0, .2f);
            var displacement = Vector3.Transform(localPoint, body.Rotation)
                - Vector3.Transform(localPoint, rest.Value.Rotation);
            Assert.InRange(displacement.Length(), 0, restDistance);
        });
    }

    private static double Energy(LawBody body, Support support)
    {
        var local = Vector3.Transform(body.Angular, Quaternion.Conjugate(body.Rotation));
        var angular = support switch
        {
            Support.Point => .4 * .3 * .3 * body.Spin * body.Spin,
            Support.Line => (.3 * .3 / 4 + .2 * .2 / 3) * ((double)local.X * local.X + (double)local.Z * local.Z) +
                .5 * .3 * .3 * local.Y * local.Y,
            Support.Face => (2 * .2 * .2 / 3) * body.Spin * body.Spin,
            _ => throw new ArgumentOutOfRangeException(nameof(support))
        };
        return .5 * body.Speed * body.Speed + .5 * angular - LawScene.Gravity * body.Position.Y;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ATiltedCylinderAtTheFiniteEdgeTipsAboutItsLowEnd(bool reverse)
    {
        var scene = new LawScene(false);
        scene.StaticBox(new(0, -.5f, 0), new(1, .5f, 1), material: new(0, .1f, 0, 0));
        var axis = reverse ? new Quaternion(.5f, -.5f, .5f, -.5f) : new Quaternion(.5f, .5f, .5f, .5f);
        var rotation = Quaternion.Multiply(Quaternion.CreateFromAxisAngle(Vector3.UnitX, .1f), axis);
        var height = .3f * MathF.Cos(.1f) + .2f * MathF.Sin(.1f);
        var id = scene.Cylinder(new(1.001f, height, 0), .3f, .2f, 1, new(0, .1f, 0, 0),
            rotation, new(0, -.5f, 0), gravity: 0);
        var world = scene.Admit();
        var end = world.Step(1)[id];
        // The +Z endpoint is lower by 2h sin(.1)=39.9 mm. Its upward
        // reaction has negative X torque; a spurious centred line cannot tip it.
        Assert.True(end.Angular.X < -.01f, $"low-end reaction {end}");
        Assert.True(end.Velocity.Y > -.5f);
        var direction = Vector3.Transform(Vector3.UnitY, end.Rotation);
        var extent = .2f * MathF.Abs(direction.Y) + .3f * MathF.Sqrt(1 - direction.Y * direction.Y);
        Assert.True(end.Position.Y - extent >= -.0005f);
    }

    [Theory]
    [InlineData(-.001f, false)]
    [InlineData(0f, false)]
    [InlineData(.001f, false)]
    [InlineData(-.001f, true)]
    [InlineData(0f, true)]
    [InlineData(.001f, true)]
    public void FiniteFaceAndEdgeImpactHasNoArtificialAxialKick(float beyond, bool reverse)
    {
        var scene = new LawScene(false);
        scene.StaticBox(new(0, -.5f, 0), new(1, .5f, 1));
        var rotation = reverse ? new Quaternion(.5f, -.5f, .5f, -.5f) : new Quaternion(.5f, .5f, .5f, .5f);
        var id = scene.Cylinder(new(1 + beyond, .3f, 0), .3f, .2f, 1, new(0, .1f, .3f, 0),
            rotation, new(0, -.5f, 0), gravity: 0);
        var world = scene.Admit();
        var end = world.Step(12)[id];
        Assert.InRange(MathF.Abs(end.Position.Z), 0, .00005f);
        Assert.InRange(MathF.Abs(end.Velocity.Z), 0, .001f);
        Assert.InRange(end.Position.Y, .2995f, .3005f);
        Assert.True(end.Speed < .05);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.3f)]
    public void CurvedWallSeparationAndRecontactPreserveMotionAndEnergy(float friction)
    {
        var scene = new LawScene(false);
        scene.StaticHollow(ColliderShapeKind.Tube, new(0, 2, 0), Quaternion.Identity, .5f, new(.4f, 2, 0),
            new(1, .1f, friction, 0));
        var id = scene.Sphere(new(0, 2, .15f), .1f, 1, new(1, .1f, friction, 0),
            new(2, 0, 0), default, 0, 0);
        var world = scene.Admit();
        var reversals = 0;
        var previous = Vector3.Dot(world.Initial(id).Position - new Vector3(0, 2, 0), world.Initial(id).Velocity);
        var firstTransfer = false;
        var final = world.Step(120, observe: (_, state) =>
        {
            var body = state[id];
            // Centre remains inside bore minus sphere radius, with 0.5 mm contact slop.
            Assert.InRange(new Vector2(body.Position.X, body.Position.Z).Length(), 0, .3005f);
            var energy = .5 * body.Speed * body.Speed + .5 * (.4 * .1 * .1) * body.Spin * body.Spin;
            Assert.InRange(energy, 0, 2 + .002);
            if (!firstTransfer && body.Velocity.X < 0)
            {
                // Offset .15 in a centre-accessible radius .3 gives sin(theta)=.5:
                // incoming tangent speed is 1 m/s. For a solid sphere, sticking
                // needs Jt=1/(1+mr²/I)=2/7 N s, creating spin Jt*r/I=50/7 rad/s.
                // mu=.3 and e=1 supply mu*Jn=.3*2*sqrt(3) > 2/7.
                // The normal can rotate by speed*h/radius within one substep;
                // (2/480/.3)*(50/7) < .1 rad/s bounds that sampling effect.
                if (friction > 0)
                    Assert.InRange(body.Spin, 50.0 / 7 - .1, 50.0 / 7 + .1);
                else
                    Assert.InRange(body.Spin, 0, 1e-5);
                firstTransfer = true;
            }
            var radial = Vector3.Dot(body.Position - new Vector3(0, 2, 0), body.Velocity);
            if (previous > 0 && radial < 0) reversals++;
            previous = radial;
        });
        Assert.True(firstTransfer);
        Assert.True(reversals >= 3, $"alternating wall contacts {reversals}, final {final[id]}");
    }

    [Theory]
    [InlineData(0f, false)]
    [InlineData(.3f, false)]
    [InlineData(0f, true)]
    [InlineData(.3f, true)]
    public void DynamicFinitePatchesExchangeMomentumWithinOneSharedBudget(float friction, bool unequal)
    {
        var scene = new LawScene(false);
        var half = new Vector3(.4f, .02f, .4f);
        var lowerHalf = unequal ? new Vector3(.6f, .03f, .5f) : half;
        var lowerMass = unequal ? 2f : 1f;
        var material = new LawMaterial(0, .1f, friction, 0);
        var upper = scene.Box(new(0, .02f, 0), half, 1, material,
            velocity: new(.2f, -.5f, 0), angular: new(0, .2f, 0), gravity: 0);
        var lower = scene.Box(new(0, -lowerHalf.Y, 0), lowerHalf, lowerMass, material,
            velocity: new(-.2f, .5f, 0), angular: new(0, -.2f, 0), gravity: 0);
        var world = scene.Admit(SimulationCadence.Hz240);
        var a0 = world.Initial(upper); var b0 = world.Initial(lower);
        var state = world.Step(1);
        var a = state[upper]; var b = state[lower];
        static Vector3 Inertia(Vector3 h, float mass) => mass / 3 * new Vector3(
            h.Y * h.Y + h.Z * h.Z, h.X * h.X + h.Z * h.Z, h.X * h.X + h.Y * h.Y);
        var inertiaA = Inertia(half, 1); var inertiaB = Inertia(lowerHalf, lowerMass);
        static Vector3 AngularMomentum(LawBody body, Vector3 inertia, float mass) =>
            Vector3.Transform(Vector3.Transform(body.Angular, Quaternion.Conjugate(body.Rotation)) * inertia, body.Rotation)
            + Vector3.Cross(body.Position, mass * body.Velocity);
        // Two substeps, eight sweeps, four normal and three friction rows:
        // 32 rounded scalar operations per row, scaled by the larger mass.
        var error = lowerMass * 2 * 8 * 7 * 32 / 16777216f;
        Assert.InRange((a.Velocity + lowerMass * b.Velocity - a0.Velocity - lowerMass * b0.Velocity).Length(), 0, error);
        Assert.InRange((AngularMomentum(a, inertiaA, 1) + AngularMomentum(b, inertiaB, lowerMass)
            - AngularMomentum(a0, inertiaA, 1) - AngularMomentum(b0, inertiaB, lowerMass)).Length(), 0, error);
        var impulse = a.Velocity - a0.Velocity;
        var partner = lowerMass * (b.Velocity - b0.Velocity);
        Assert.InRange(new Vector2(impulse.X + partner.X, impulse.Z + partner.Z).Length(), 0, error);
        var torque = inertiaA.Y * (a.Angular.Y - a0.Angular.Y);
        var partnerTorque = inertiaB.Y * (b.Angular.Y - b0.Angular.Y);
        Assert.InRange(MathF.Abs(torque + partnerTorque), 0, error);
        // The partner's measured torque predicts this body's angular response
        // through its own homogeneous inertia, even when masses/extents differ.
        Assert.InRange(a.Angular.Y, a0.Angular.Y - partnerTorque / inertiaA.Y - error / inertiaA.Y,
            a0.Angular.Y - partnerTorque / inertiaA.Y + error / inertiaA.Y);
        var radius = MathF.Sqrt(half.X * half.X + half.Z * half.Z);
        var shared = MathF.Sqrt(impulse.X * impulse.X + impulse.Z * impulse.Z + torque * torque / (radius * radius));
        Assert.InRange(shared, 0, friction * impulse.Y + error);
        if (friction == 0)
        {
            Assert.InRange(MathF.Abs(a.Velocity.X - a0.Velocity.X), 0, error);
            Assert.InRange(MathF.Abs(b.Velocity.X - b0.Velocity.X), 0, error);
            Assert.InRange(MathF.Abs(a.Angular.Y - a0.Angular.Y), 0, error);
            Assert.InRange(MathF.Abs(b.Angular.Y - b0.Angular.Y), 0, error);
        }
        else
        {
            Assert.True(impulse.X < -.01f && partner.X > .01f);
            Assert.True(a.Angular.Y < a0.Angular.Y - .01f && b.Angular.Y > b0.Angular.Y + .01f);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnOffCentreFinitePatchMinimizesTheCoupledTranslationalAndRotationalEnergy(bool rotated)
    {
        // The support covers the left half of a homogeneous thin plate. Its
        // footprint centroid is x=-.8, radius sqrt(.8²+.8²), Iy=(1.6²+.8²)/3.
        // A Z impulse therefore changes both vz and yaw, unlike a centred patch.
        const float height = 1f / 1024f;
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, rotated ? MathF.PI / 4 : 0);
        var inverse = Quaternion.Conjugate(rotation);
        Vector3 World(Vector3 value) => Vector3.Transform(value, rotation);
        var scene = new LawScene(false);
        var material = new LawMaterial(0, .1f, .3f, 0);
        scene.StaticBox(World(new(-.8f, -.1f, 0)), new(.8f, .1f, .8f), rotation, material);
        var id = scene.Box(World(new(0, height, 0)), new(1.6f, height, .8f), 1, material, rotation,
            velocity: World(new(.05f, -.1f, .03f)), angular: World(new(0, .005f, 0)), gravity: 0);
        var world = scene.Admit(SimulationCadence.Hz240);
        var initial = world.Initial(id);
        var end = world.Step(1)[id];
        var initialVelocity = Vector3.Transform(initial.Velocity, inverse);
        var endVelocity = Vector3.Transform(end.Velocity, inverse);
        var initialAngular = Vector3.Transform(initial.Angular, inverse);
        var endAngular = Vector3.Transform(end.Angular, inverse);
        const double lever = .8, inertia = (1.6 * 1.6 + .8 * .8) / 3;
        var radius = Math.Sqrt(.8 * .8 + .8 * .8);
        var budget = .3 * (endVelocity.Y - initialVelocity.Y);
        // Independently solve the physical KKT equations in double: a scalar X
        // equation and the analytic inverse of the coupled Z/yaw 2x2 block.
        (double X, double Z, double Twist) Impulse(double alpha)
        {
            var zz = 1 + lever * lever / inertia + alpha;
            var tt = radius * radius / inertia + alpha;
            var zt = lever * radius / inertia;
            var slip = initialVelocity.Z + lever * initialAngular.Y;
            var spin = radius * initialAngular.Y;
            var determinant = zz * tt - zt * zt;
            return (-initialVelocity.X / (1 + alpha),
                (-tt * slip + zt * spin) / determinant,
                (zt * slip - zz * spin) / determinant);
        }
        var lo = 0.0; var hi = 1024.0;
        for (var i = 0; i < 80; i++)
        {
            var mid = (lo + hi) / 2; var value = Impulse(mid);
            if (value.X * value.X + value.Z * value.Z + value.Twist * value.Twist > budget * budget) lo = mid;
            else hi = mid;
        }
        var expected = Impulse(hi);
        // Initial total kinetic energy bounds speed and spin throughout this
        // inelastic impact. During one tick, every footprint point moves at
        // most (vMax + wMax*bodyRadius)/240. Four times its fractional lever
        // change bounds the two lever factors and the two coupled coordinates.
        // Normal pitch feeds Z slip only through the plate's tiny height.
        var normal = endVelocity.Y - initialVelocity.Y;
        var ix = (height * height + .8 * .8) / 3;
        var energy = .5 * (.05 * .05 + .1 * .1 + .03 * .03) + .5 * inertia * .005 * .005;
        var movement = (Math.Sqrt(2 * energy) + Math.Sqrt(2 * energy / ix) * Math.Sqrt(1.6 * 1.6 + .8 * .8)) / 240;
        var error = 4 * budget * movement / lever + normal * height * lever / ix;
        Assert.True(budget > .001);
        Assert.InRange(endVelocity.X - initialVelocity.X, expected.X - error, expected.X + error);
        Assert.InRange(endVelocity.Z - initialVelocity.Z, expected.Z - error, expected.Z + error);
        var expectedYaw = initialAngular.Y + (lever * expected.Z + radius * expected.Twist) / inertia;
        Assert.InRange(endAngular.Y, expectedYaw - error / lever, expectedYaw + error / lever);
    }
}
