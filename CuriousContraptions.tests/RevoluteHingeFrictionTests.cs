using System.Buffers.Binary;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 10.3b: the generic revolute hinge's declared Coulomb friction torque τ, on an unframed hinged bar driven straight through the
/// shipped engine (no element package). The bar is a 1 kg box, half extents 0.6 × 0.05 × 0.05 m, hinged to a static anchor at a point
/// d = 0.5 m from its centre of mass along its length, so gravity turns it about the hinge with torque m·g·d (4.905 N·m) while it is
/// level. Its moment about the hinge is I = m(hx² + hy²)/3 + m·d² (box law plus the parallel-axis term).
/// </summary>
public sealed class RevoluteHingeFrictionTests
{
    private const float Mass = 1f;
    private const float Arm = .5f;
    private static readonly Vector3 BarExtents = new(.6f, .05f, .05f);
    private static readonly Vector3 Hinge = new(0f, 2f, 0f);
    private static float GravityTorque => Mass * -LawScene.Gravity * Arm;
    private static float HingeInertia => Mass * (BarExtents.X * BarExtents.X + BarExtents.Y * BarExtents.Y) / 3f + Mass * Arm * Arm;

    /// <summary>The bar's principal moments about its centre of mass (box law); it turns only about Z, so they stay world-aligned.</summary>
    private static Vector3 BarMoments => Mass / 3f * new Vector3(BarExtents.Y * BarExtents.Y + BarExtents.Z * BarExtents.Z,
        BarExtents.X * BarExtents.X + BarExtents.Z * BarExtents.Z, BarExtents.X * BarExtents.X + BarExtents.Y * BarExtents.Y);

    /// <summary>The bar's mechanical energy above its initial value (it starts at rest with its centre at hinge height): translational
    /// plus rotational kinetic energy plus m·g·Δy.</summary>
    private static float EnergyGain(LawBody body) =>
        .5f * Mass * body.Velocity.LengthSquared() + .5f * Vector3.Dot(BarMoments, body.Angular * body.Angular) +
        Mass * -LawScene.Gravity * (body.Position.Y - Hinge.Y);

    /// <summary>The allowed energy gain (J). Friction only removes energy. A sign-reversed friction row at τ = 0.5·m·g·d would drive the
    /// bar with a net 1.5·m·g·d through about 0.099 rad in the 0.1 s window, which is about 0.24 J of work beyond the gravity it converts,
    /// about 24 times this slack. The slack covers the numerical energy error that is not a physical gain: the semi-implicit
    /// integrator, the soft joint rows' position correction, and the f32 rounding of m·g·y at y = 2 m (about 2e-6 J per read).</summary>
    private const float EnergySlack = .01f;

    /// <summary>The bar and its hinge with friction torque <paramref name="frictionTorque"/>. The anchor is a 5 cm static sphere at the
    /// hinge; connected collision is disabled, so the bar's only interaction is the joint.</summary>
    private static (LawWorld World, GpuBodyId Bar, RevoluteConstraintId Joint) HingedBar(float frictionTorque)
    {
        var scene = new LawScene(bench: false);
        var anchor = scene.StaticSphere(Hinge, .05f, Quaternion.Identity, LawMaterial.Bench);
        var bar = scene.Box(Hinge + new Vector3(Arm, 0f, 0f), BarExtents, Mass, LawMaterial.Bench);
        var joint = scene.Revolute(anchor, bar, ConstraintFrame.Identity, new(new(-Arm, 0f, 0f), Quaternion.Identity),
            -MathF.PI / 2f, MathF.PI / 2f, frictionTorque);
        return (scene.Admit(), bar, joint);
    }

    // Above τ the bar turns with the torque left after friction: angular acceleration (m·g·d − τ)/I, so after 0.1 s (12 ticks) it spins
    // at (m·g·d − τ)·t/I clockwise. Within that window the bar turns under 0.07 rad, so the gravity torque stays within 0.3 % of m·g·d.
    // τ = 0 is the frictionless control through the same clamp (the τ = 0 behaviour the lever, the only hinge package, no longer shows).
    // The 10 % allowance separates the plausible defects: no friction at τ = 0.5·m·g·d doubles the spin; a per-tick instead of
    // per-substep bound (4x) or a doubled application holds the bar. Friction only removes energy: at every tick the bar's mechanical
    // energy stays within EnergySlack of its initial value.
    [Theory]
    [InlineData(0f)]
    [InlineData(.5f)]
    public void AHingedBarTurnsWithTheTorqueLeftAfterItsFrictionTorque(float fraction)
    {
        var (world, bar, _) = HingedBar(fraction * GravityTorque);
        const float seconds = .1f;
        var maxGain = float.NegativeInfinity;

        var spin = world.Step(12, (_, state) => maxGain = MathF.Max(maxGain, EnergyGain(state[bar])))[bar].Angular.Z;

        var expected = -(1f - fraction) * GravityTorque * seconds / HingeInertia;
        Assert.True(MathF.Abs(spin - expected) <= .1f * MathF.Abs(expected),
            $"With τ = {fraction}·m·g·d the bar spins at {spin:F4} rad/s after {seconds} s; (m·g·d − τ)·t/I is {expected:F4} rad/s");
        Assert.True(maxGain <= EnergySlack, $"With τ = {fraction}·m·g·d the bar gained {maxGain:E2} J, allowed {EnergySlack} J");
    }

    // Below τ the hinge holds: with τ = 2·m·g·d the bar stays within 0.01 rad of level (the lever's held-tilt bound) at every tick of 2 s,
    // and ends at rest within 0.02 rad/s. The holding friction adds no energy: the bar's mechanical energy stays within EnergySlack of
    // its initial value at every tick.
    [Fact]
    public void AHingedBarIsHeldWhileItsGravityTorqueIsBelowTheFrictionTorque()
    {
        var (world, bar, _) = HingedBar(2f * GravityTorque);
        var maxTilt = 0f;
        var maxGain = float.NegativeInfinity;

        var end = world.Step(240, (_, state) =>
        {
            var rotation = state[bar].Rotation;
            maxTilt = MathF.Max(maxTilt, MathF.Abs(2f * MathF.Atan2(rotation.Z, rotation.W)));
            maxGain = MathF.Max(maxGain, EnergyGain(state[bar]));
        });

        Assert.True(maxTilt <= .01f, $"The held bar tilted {maxTilt:F4} rad within 2 s, expected <= 0.01 rad");
        Assert.True(MathF.Abs(end[bar].Angular.Z) <= .02f, $"The held bar ends spinning at {end[bar].Angular.Z:F4} rad/s");
        Assert.True(maxGain <= EnergySlack, $"The held bar gained {maxGain:E2} J, allowed {EnergySlack} J");
    }

    // A friction torque must be finite and at least 0. Each invalid value is refused by the declaration, and the scene that carries it is
    // not admitted; 0 (frictionless) and the lever's 2 N·m are admitted.
    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void AnInvalidFrictionTorqueIsRefusedByTheDeclarationAndTheScene(float frictionTorque)
    {
        Assert.Throws<ArgumentException>(() => HingedBar(frictionTorque));
        _ = HingedBar(0f);
        _ = HingedBar(2f);
    }

    // The ABI read refuses a revolute record whose friction torque is negative or non-finite, or whose trailing padding is not zero.
    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void TheAbiReadRefusesAnInvalidFrictionTorqueOrNonzeroPadding(float frictionTorque)
    {
        var (world, _, joint) = HingedBar(2f);
        var offset = world.Scene.Layout.GetRevoluteOffset(joint.Value);
        Assert.Equal(2f, PhysicsGpuAbi.ReadRevolute(world.Admitted, 0).FrictionTorque);

        var poisoned = (byte[])world.Admitted.Clone();
        BinaryPrimitives.WriteSingleLittleEndian(poisoned.AsSpan(offset + PhysicsGpuAbi.RevoluteFrictionTorqueOffset), frictionTorque);
        var padded = (byte[])world.Admitted.Clone();
        padded[offset + PhysicsGpuAbi.RevoluteBytes - 1] = 1;

        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadRevolute(poisoned, 0));
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadRevolute(padded, 0));
    }

    // A committed friction impulse is admitted exactly up to ±τ·h (RevoluteRows.FrictionImpulseLimit, the bound the solver clamps to)
    // and refused one f32 step beyond it, on both sides; at τ = 0 the same bound admits only zero.
    [Theory]
    [InlineData(2f)]
    [InlineData(0f)]
    public void ACommittedFrictionImpulseIsAdmittedUpToTheFrictionBoundAndRefusedBeyondIt(float frictionTorque)
    {
        var (world, _, joint) = HingedBar(frictionTorque);
        var friction = world.Scene.Layout.GetRevoluteOffset(joint.Value) + PhysicsGpuAbi.RevoluteImpulseOffset + 4 * (int)RevoluteRowKind.Friction;
        var limit = RevoluteRows.FrictionImpulseLimit(frictionTorque);

        bool Admitted(float impulse)
        {
            var candidate = (byte[])world.Admitted.Clone();
            BinaryPrimitives.WriteSingleLittleEndian(candidate.AsSpan(friction), impulse);
            try
            {
                PhysicsGpuAbi.ValidateCandidate(candidate, world.Admitted, new(0), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        Assert.Equal((true, true, false, false), (Admitted(limit), Admitted(-limit), Admitted(MathF.BitIncrement(limit)), Admitted(MathF.BitDecrement(-limit))));
    }
}
