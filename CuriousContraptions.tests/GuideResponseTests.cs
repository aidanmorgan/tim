using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

public sealed class GuideResponseTests
{
    [Theory]
    [InlineData(1f, false)]
    [InlineData(-1f, true)]
    [InlineData(8f, true)]
    [InlineData(-.125f, false)]
    public void ExactSignedCopiesHaveZeroResponseAndReconstructTheirReaction(float scale, bool reverse)
    {
        var locked = Vector(3f * scale, -2f * scale, scale);
        var original = new List<GuideVector> { locked, Vector(0f, 0f, 0f) };
        if (reverse) original.Reverse();
        var normalized = Vector(locked[0], locked[1], locked[2]); normalized.Scale(1f / normalized.Norm());
        var factor = new GuideQr(); factor.Factor([normalized]);
        foreach (var sign in new[] { 1f, -1f })
        {
            var query = Vector(sign * locked[0], sign * locked[1], sign * locked[2]);
            var response = new GuideVector(); var removed = new GuideVector();
            factor.ProjectResponse(query, response, removed, original);
            Assert.Equal(0f, query.Dot(query)); Assert.Equal(0f, response.Norm());
            // Three-component local f32 projection/reconstruction: 32 roundoff units
            // allow its two projection passes, without a scene-level precision requirement.
            var tolerance = 32f * 1.1920929e-7f * locked.Norm();
            for (var axis = 0; axis < 3; axis++)
                Assert.InRange(MathF.Abs(factor.Basis(0)[axis] * removed[0] - sign * locked[axis]), 0f, tolerance);
        }
    }

    [Theory]
    [InlineData(1e-12f, false)]
    [InlineData(1e-6f, true)]
    public void ResolvedWeakFreedomKeepsCrossMassAndAppliedVelocity(float free, bool nearSpan)
    {
        var original = new List<GuideVector> { Vector(1f, 0f, 0f) };
        var factor = new GuideQr(); factor.Factor(original);
        var query = Vector(nearSpan ? 1f : 0f, free, 0f);
        var response = new GuideVector(); var removed = new GuideVector();
        factor.ProjectResponse(query, response, removed, original);
        Assert.Equal(free * free, query.Dot(query));
        Assert.Equal(free, response[1]); Assert.Equal(0f, response[0]);
        var other = Vector(0f, 2f, 0f); var otherResponse = new GuideVector(); var otherRemoved = new GuideVector();
        factor.ProjectResponse(other, otherResponse, otherRemoved, original);
        // Axis-aligned operations are exact here: cross mass equals measured dv
        // for a unit impulse, and the removed x reaction reconstructs the input.
        Assert.Equal(query.Dot(other), 2f * response[1]);
        Assert.Equal(query.Dot(other), free * otherResponse[1]);
        Assert.Equal(nearSpan ? 1f : 0f, removed[0]);
        var zero = new GuideVector();
        factor.ProjectResponse(zero, response, removed, original);
        Assert.Equal(0f, response.Norm()); Assert.Equal(0f, removed.Norm());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReorderedRescaledAndRedundantRowsPreserveTheIndependentResponse(bool reverse)
    {
        // A local mathematical contract: projecting onto x+y=0 leaves (1,-1,3).
        // This catches loss of a physical free direction, rather than pinning a chosen QR basis.
        var rows = new List<GuideVector> { Vector(1f, 1f, 0f), Vector(-3f, -3f, 0f), Vector(2f, 2f, 0f) };
        if (reverse) rows.Reverse();
        var factor = new GuideQr();
        factor.Factor(rows);
        var impulse = Vector(2f, 0f, 3f);
        factor.Project(impulse);
        Assert.InRange(impulse[0], .99999f, 1.00001f);
        Assert.InRange(impulse[1], -1.00001f, -.99999f);
        Assert.InRange(impulse[2], 2.99999f, 3.00001f);
        Assert.InRange(impulse.Dot(impulse), 10.9999f, 11.0001f);
        factor.ProjectTranspose(impulse);
        Assert.InRange(impulse[0], .99999f, 1.00001f);
        Assert.InRange(impulse[1], -1.00001f, -.99999f);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1e-7f)]
    public void ZeroAndNumericallyDependentRowsKeepFiniteFreeResponse(float residual)
    {
        var factor = new GuideQr();
        factor.Factor([Vector(0f, 0f, 0f), Vector(1f, 1f, 0f), Vector(1f, 1f, residual)]);
        var impulse = Vector(0f, 0f, 2f);
        factor.Project(impulse);
        factor.ProjectTranspose(impulse);
        Assert.InRange(impulse[2], 1.99999f, 2.00001f);
        Assert.InRange(MathF.Abs(impulse[0]) + MathF.Abs(impulse[1]), 0f, 1e-5f);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void CompatibleCyclicGuidesShareMomentumWithoutLockingAxialMotion(bool redundant, bool reverse)
    {
        var scene = new LawScene(false);
        var a = scene.Box(new(-1, 5, 0), new(.1f), 1f, LawMaterial.Domino, velocity: new(3f, 1f, 0f), gravity: 0f);
        var b = scene.Box(new(0, 5, 0), new(.1f), 1f, LawMaterial.Domino, gravity: 0f);
        var c = scene.Box(new(1, 5, 0), new(.1f), 1f, LawMaterial.Domino, gravity: 0f);
        var pairs = new List<(GpuBodyId A, GpuBodyId B, float Arm)> { (a, b, .5f), (b, c, .5f) };
        if (redundant) pairs.Add((a, c, 1f));
        if (reverse) pairs.Reverse();
        foreach (var pair in pairs)
            scene.Prismatic(pair.A, pair.B, new(new(pair.Arm, 0, 0), Quaternion.Identity),
                new(new(-pair.Arm, 0, 0), Quaternion.Identity), -2f, 2f, 0f, 0f, ConnectedCollision.Disabled);
        var final = scene.Admit().Step(120);
        // Over one second, 2 cm excludes visible guide separation or tilt of these 20 cm boxes.
        const float clearance = .02f;
        var centre = (final[a].Position + final[b].Position + final[c].Position) / 3f;
        Assert.InRange(centre.X, 1f - clearance, 1f + clearance); // Initial X momentum is 3 kg m/s over 3 kg.
        foreach (var body in new[] { a, b, c })
        {
            var initialX = body == a ? -1f : body == b ? 0f : 1f;
            Assert.InRange(final[body].Position.X - initialX, 1f - clearance, 1f + clearance);
            Assert.InRange(final[body].Position.Z, -clearance, clearance);
            foreach (var x in new[] { -.1f, .1f })
            foreach (var y in new[] { -.1f, .1f })
            foreach (var z in new[] { -.1f, .1f })
            {
                var corner = new Vector3(x, y, z);
                Assert.True(Vector3.Distance(Vector3.Transform(corner, final[body].Rotation), corner) <= clearance,
                    "The locked orientations must not visibly tilt a guide corner over one second.");
            }
        }
        Assert.InRange(final[a].Position.Y, 6f - clearance, 6f + clearance);
        Assert.InRange(final[b].Position.Y, 5f - clearance, 5f + clearance);
        Assert.InRange(final[c].Position.Y, 5f - clearance, 5f + clearance);
    }

    [Fact]
    public void PositionalRecoveryNearTheSpeedCapPreservesSharedMomentum()
    {
        var scene = new LawScene(false);
        var a = scene.Box(new(0, 5, 0), new(.1f), 1f, LawMaterial.Domino, velocity: new(63f, 0, 0), gravity: 0f);
        var b = scene.Box(new(1, 5, 0), new(.1f), 1f, LawMaterial.Domino, velocity: new(63f, 0, 0), gravity: 0f);
        scene.Prismatic(a, b, ConstraintFrame.Identity, ConstraintFrame.Identity, -1f, 1f, 0f, 0f,
            ConnectedCollision.Disabled);
        var final = scene.Admit().Step(1);
        // Internal positional recovery may redistribute momentum, but must scale before the per-body cap clips it.
        // Equal masses retain 126 kg m/s and advance their centre by 63/120 m in this isolated mathematical fixture.
        Assert.InRange(final[a].Velocity.X + final[b].Velocity.X, 125.999f, 126.001f);
        Assert.InRange((final[a].Position.X + final[b].Position.X) * .5f, 1.0249f, 1.0251f);
        Assert.True(final[b].Position.X - final[a].Position.X < 1f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HingesAndTransmissionKeepTheirLawOnAMovingGuidedCarriage(bool connected)
    {
        var scene = new LawScene(false);
        var rail = scene.StaticBox(new(-2, 5, 0), new(.1f));
        var carriage = scene.Box(new(0, 5, 0), new(.2f), 2f, LawMaterial.Domino,
            velocity: new(0, .3f, 0), gravity: 0f);
        scene.Prismatic(rail, carriage, new(new(2, 0, 0), Quaternion.Identity), ConstraintFrame.Identity,
            -2f, 2f, 0f, 0f, ConnectedCollision.Disabled);
        var a = scene.Sphere(new(.6f, 5, 0), .15f, .5f, LawMaterial.Domino,
            new(0, .3f, 0), Vector3.Zero, 0f, 0f);
        var b = scene.Sphere(new(-.6f, 5, 0), .15f, .5f, LawMaterial.Domino,
            new(0, .3f, 0), Vector3.Zero, 0f, 0f);
        var first = scene.Revolute(carriage, a, new(new(.6f, 0, 0), Quaternion.Identity), ConstraintFrame.Identity,
            -MathF.PI, MathF.PI, 0f, travel: RevoluteTravel.Continuous);
        var second = scene.Revolute(carriage, b, new(new(-.6f, 0, 0), Quaternion.Identity), ConstraintFrame.Identity,
            -MathF.PI, MathF.PI, 0f, travel: RevoluteTravel.Continuous);
        scene.RevoluteDrive(first, 4f, 2f);
        if (connected) scene.Transmission(first, second, -1f);
        var final = scene.Admit().Step(240, supplied: true);
        // Existing 2% shaft-speed allowance distinguishes a transmitted reverse drive from the disconnected rotor.
        Assert.InRange(final[a].Angular.Z, 3.92f, 4.08f);
        Assert.InRange(final[b].Angular.Z, connected ? -4.08f : -.08f, connected ? -3.92f : .08f);
        Assert.InRange(final[carriage].Position.Y, 5.59f, 5.61f); // Preserve the carriage's free 0.6 m travel.
        Assert.InRange(final[carriage].Angular.Length(), 0f, .01f);
        Assert.InRange(Vector3.Distance(final[a].Position, final[carriage].Position + new Vector3(.6f, 0, 0)), 0f, .001f);
        Assert.InRange(Vector3.Distance(final[b].Position, final[carriage].Position + new Vector3(-.6f, 0, 0)), 0f, .001f);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.1f)]
    public void RollingFrictionActsOnTheWholeGuidedMass(float rolling)
    {
        var (world, carrier, ball) = GuidedBall(.3f, new(1, 0, 0), new(0, 0, -5), 0f, rolling);
        var final = world.Step(240);
        if (rolling > 0f) Assert.InRange(MathF.Abs(final[ball].Velocity.X), 0f, .05f);
        else Assert.True(final[ball].Velocity.X > .8f, "Without rolling resistance the freely rolling guided mass must continue.");
        Assert.InRange(Vector3.Distance(final[carrier].Position, final[ball].Position), 0f, .001f);
        Assert.InRange(final[ball].Position.Y, .2995f, .3005f); // Actual passive support, unlike powered indentation.
    }

    [Fact]
    public void RestitutionReturnsTheGuidedMassWithoutCreatingEnergy()
    {
        var (world, carrier, ball) = GuidedBall(2f, Vector3.Zero, Vector3.Zero, .8f, 0f);
        var bounced = false;
        var apex = .3f;
        var final = world.Step(180, (_, state) =>
        {
            if (state[ball].Velocity.Y > 1f) bounced = true;
            if (bounced) apex = MathF.Max(apex, state[ball].Position.Y);
            var energy = .5f * 9.81f * (state[carrier].Position.Y + state[ball].Position.Y) +
                .25f * (state[carrier].Velocity.LengthSquared() + state[ball].Velocity.LengthSquared()) +
                .001f * state[carrier].Angular.LengthSquared() + .004f * state[ball].Angular.LengthSquared();
            Assert.True(energy <= 19.62f + .01f, "Contact must not create energy in either response-affected body.");
        });
        Assert.True(bounced);
        // e² times the1.7m drop gives a1.388m apex; this gameplay interval rejects absent or fully elastic bounce.
        Assert.InRange(apex, 1.2f, 1.55f);
        Assert.InRange(Vector3.Distance(final[carrier].Position, final[ball].Position), 0f, .001f);
    }

    private static (LawWorld World, GpuBodyId Carrier, GpuBodyId Ball) GuidedBall(float y, Vector3 velocity,
        Vector3 angular, float restitution, float rolling)
    {
        var source = new LawScene(false);
        source.StaticBox(Vector3.Zero, new(5f, .1f, 2f));
        var material = new LawMaterial(restitution, .1f, .5f, rolling);
        var carrier = source.Sphere(new(0, y, 0), .1f, .5f, material, velocity, angular, 0f, -9.81f);
        var ball = source.Sphere(new(0, y, 0), .2f, .5f, material, velocity, angular, 0f, -9.81f);
        var frame = new ConstraintFrame(Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f));
        source.Prismatic(carrier, ball, frame, frame, -1f, 1f, 0f, 0f, ConnectedCollision.Disabled);
        // The smaller concentric collider is enclosed; only the outer sphere touches the floor.
        var world = source.Admit();
        return (world, carrier, ball);
    }

    private static GuideVector Vector(float x, float y, float z)
    {
        var result = new GuideVector();
        result.Set(0, x); result.Set(1, y); result.Set(2, z);
        return result;
    }
}
