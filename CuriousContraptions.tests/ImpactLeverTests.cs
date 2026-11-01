using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class ImpactLeverTests
{
    private static WorkshopGenericInstance Lever(ulong id = 1, float x = 0, float y = 3, float z = 0, float mass = 2f, float angleDeg = 0f) =>
        WorkshopInput.Generic(new PartId("impact_lever"), new(id), x, y, z, 0, 0, 0, 1,
            new(new Dictionary<ParameterId, float>
            {
                [new("beam_mass")] = mass,
                [new("initial_angle")] = angleDeg
            }));

    /// <summary>The top face of a lever placed at y = 3 m: the pivot plus the beam's 0.12 m half-thickness (its 0.24 m box). A Weight's
    /// origin is its base, so a Weight placed here rests on the level beam without overlapping it.</summary>
    private const float BeamTop = 3f + 0.12f;

    private static WorkshopGenericInstance Basketball(ulong id = 2, float x = 1.2f, float y = 3.46f, float z = 0) =>
        WorkshopInput.Generic(new PartId("ball"), new(id), x, y, z, 0, 0, 0, 1);

    private static WorkshopGenericInstance BowlingBall(ulong id = 3, float x = -1.2f, float y = 6f, float z = 0) =>
        WorkshopInput.Generic(new PartId("bowling"), new(id), x, y, z, 0, 0, 0, 1);

    private static float BodyY(WorkshopRead read, GpuBodyId id)
    {
        Assert.True(read.Bodies.TryGet(id, out var value));
        return (value.Body.Cell.Y + value.Body.Local.Y) / 16f;
    }

    private static Vector3 BodyPos(WorkshopRead read, GpuBodyId id)
    {
        Assert.True(read.Bodies.TryGet(id, out var value));
        return new Vector3(
            (value.Body.Cell.X + value.Body.Local.X) / 16f,
            (value.Body.Cell.Y + value.Body.Local.Y) / 16f,
            (value.Body.Cell.Z + value.Body.Local.Z) / 16f);
    }

    private static Vector3 BodyVel(WorkshopRead read, GpuBodyId id)
    {
        Assert.True(read.Bodies.TryGet(id, out var value));
        return new Vector3(value.Body.Velocity.X, value.Body.Velocity.Y, value.Body.Velocity.Z);
    }

    private static float BeamAngle(WorkshopRead read, GpuBodyId beamId)
    {
        Assert.True(read.Bodies.TryGet(beamId, out var value));
        var qz = value.Rotation.Z;
        var qw = value.Rotation.W;
        return 2f * MathF.Atan2(qz, qw);
    }

    private static float BeamAngularVelocity(WorkshopRead read, GpuBodyId beamId)
    {
        Assert.True(read.Bodies.TryGet(beamId, out var value));
        return value.AngularVelocity.Z;
    }

    [Fact]
    public async Task AlignedBowlingBallImpact_FlingsPayloadUpward()
    {
        var lever = Lever(1, 0, 3, 0);
        var basketball = Basketball(2, 1.2f, 3.46f, 0);
        var bowling = BowlingBall(3, -1.2f, 6f, 0);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(lever, basketball, bowling));
        var beamId = construction.Instances.Identities[lever.Id].Hinges[0].Body;

        await using var world = await WorkshopIntegrationHarness.Running(construction);

        // Calculate initial total mechanical energy (potential energy relative to y = 0)
        // Bowling (4 kg) at y = 6, Basketball (1 kg) at y = 3.46, Beam (2 kg) at y = 3
        const float g = 9.81f;
        const float mBowling = 4.0f;
        const float mBasket = 1.0f;
        const float mBeam = 2.0f;
        var initialEnergy = mBowling * g * 6.0f + mBasket * g * 3.46f + mBeam * g * 3.0f;

        var maxPayloadHeight = 3.46f;
        var maxPayloadVy = 0f;
        var maxEnergyRatio = 0f;

        var minAngleSeen = 999f;
        var maxAngleSeen = -999f;
        // Advance 240 ticks (2 s at 120 Hz)
        for (var t = 0; t < 240; t++)
        {
            await world.Advance(1);
            var read = world.Committed;

            var curAngle = BeamAngle(read, beamId);
            minAngleSeen = MathF.Min(minAngleSeen, curAngle);
            maxAngleSeen = MathF.Max(maxAngleSeen, curAngle);

            var posBasket = BodyPos(read, basketball.Id);
            var velBasket = BodyVel(read, basketball.Id);
            var posBowling = BodyPos(read, bowling.Id);
            var velBowling = BodyVel(read, bowling.Id);
            var posBeam = BodyPos(read, beamId);
            var velBeam = BodyVel(read, beamId);
            var omegaBeam = BeamAngularVelocity(read, beamId);

            maxPayloadHeight = MathF.Max(maxPayloadHeight, posBasket.Y);
            maxPayloadVy = MathF.Max(maxPayloadVy, velBasket.Y);

            // Compute total energy: Kinetic + Potential
            var keBasket = 0.5f * mBasket * velBasket.LengthSquared();
            var peBasket = mBasket * g * MathF.Max(0f, posBasket.Y);

            var keBowling = 0.5f * mBowling * velBowling.LengthSquared();
            var peBowling = mBowling * g * MathF.Max(0f, posBowling.Y);

            // Beam inertia about Z: I_z = m/3 * (hx^2 + hy^2) = 2/3 * (1.8^2 + 0.12^2) = 2.1696
            var iBeam = (mBeam / 3f) * (1.8f * 1.8f + 0.12f * 0.12f);
            var keBeam = 0.5f * mBeam * velBeam.LengthSquared() + 0.5f * iBeam * omegaBeam * omegaBeam;
            var peBeam = mBeam * g * MathF.Max(0f, posBeam.Y);

            var totalEnergy = keBasket + peBasket + keBowling + peBowling + keBeam + peBeam;
            maxEnergyRatio = MathF.Max(maxEnergyRatio, totalEnergy / initialEnergy);

            // Beam angle should not penetrate beyond limit stops (-pi/6 to +pi/6)
            var angle = BeamAngle(read, beamId);
            Assert.True(angle >= -MathF.PI / 6f - 0.05f && angle <= MathF.PI / 6f + 0.05f,
                $"Beam angle {angle} should stay within limit stops [-pi/6, +pi/6]");
        }

        // Acceptance criteria: payload peaks past y = 4.0 m with vertical velocity > 2.0 m/s
        Assert.True(maxPayloadHeight > 4.0f, $"Payload reached height {maxPayloadHeight:F3} m, expected > 4.0 m");
        Assert.True(maxPayloadVy > 2.0f, $"Payload max vertical velocity {maxPayloadVy:F3} m/s, expected > 2.0 m/s");

        // Energy strictly conserved within 1.01 * initial energy
        Assert.True(maxEnergyRatio <= 1.01f, $"Max energy ratio {maxEnergyRatio:F4} must be <= 1.01");
    }

    /// <summary>The beam's rotation axis (local Z) tipped away from world Z: the out-of-plane tilt the hinge's two locked transverse
    /// rotations exclude.</summary>
    private static float OffAxisTilt(WorkshopRead read, GpuBodyId beamId)
    {
        Assert.True(read.Bodies.TryGet(beamId, out var value));
        var axis = Vector3.Transform(Vector3.UnitZ, new Quaternion(value.Rotation.X, value.Rotation.Y, value.Rotation.Z, value.Rotation.W));
        return MathF.Acos(Math.Clamp(MathF.Abs(axis.Z), 0f, 1f));
    }

    // Story 10.3a: the compiled revolute row reaches the running world through the production wire, so an asymmetric load turns the
    // beam about its authored pivot instead of tipping a loose plank off the fulcrum. A 4 kg weight resting on the left arm
    // (x = -1.2 m, its base on the beam top) drives the beam (left end down, positive Z) to its +pi/6 stop within the 2 s window (about 0.4 s from the load's 47 N m torque).
    // Throughout that window:
    // - The beam origin stays on the pivot within 0.03 m, a quarter of the beam's 0.12 m half-thickness. Beyond that the beam visibly
    //   leaves its fulcrum pin and the lever arms the puzzle relies on change; a loose plank slides and drops tenths of a metre.
    // - The rotation axis stays within 0.05 rad of world Z. A larger tilt carries the 1.8 m arm ends about 9 cm out of the play plane,
    //   enough for payloads and ropes to leave the beam sideways.
    [Fact]
    public async Task AsymmetricLoad_TurnsTheBeamToItsStopAboutTheAuthoredPivot()
    {
        const float pivotDrift = 0.03f;
        const float offAxisTilt = 0.05f;
        const float limitTolerance = 0.05f;
        var lever = Lever(1, 0, 3, 0);
        var load = WeightTests.Placed(2, -1.2f, BeamTop, 0, 4f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(lever, load));
        var beamId = construction.Instances.Identities[lever.Id].Hinges[0].Body;
        var pivot = new Vector3(0f, 3f, 0f);

        await using var world = await WorkshopIntegrationHarness.Running(construction);

        var maxDrift = 0f;
        var maxTilt = 0f;
        var maxAngle = 0f;
        for (var t = 0; t < 240; t++)
        {
            await world.Advance(1);
            var read = world.Committed;
            maxDrift = MathF.Max(maxDrift, Vector3.Distance(BodyPos(read, beamId), pivot));
            maxTilt = MathF.Max(maxTilt, OffAxisTilt(read, beamId));
            maxAngle = MathF.Max(maxAngle, BeamAngle(read, beamId));
        }

        Assert.True(maxDrift <= pivotDrift, $"The beam origin left its pivot by {maxDrift:F4} m (allowed {pivotDrift} m).");
        Assert.True(maxTilt <= offAxisTilt, $"The beam's hinge axis tilted {maxTilt:F4} rad out of the play plane (allowed {offAxisTilt} rad).");
        Assert.True(maxAngle >= MathF.PI / 6f - limitTolerance && maxAngle <= MathF.PI / 6f + limitTolerance,
            $"The loaded beam reached {maxAngle:F3} rad; its stop is {MathF.PI / 6f:F3} rad.");
    }

    // Equal 4 kg loads at x = +/-1.2 m put no net torque on the beam, so it stays level through the 3 s play window: within 0.01 rad
    // and 0.02 rad/s at every tick. That window is about seven times the ~0.4 s an unequal load takes to reach its stop
    // (AsymmetricLoad_TurnsTheBeamToItsStopAboutTheAuthoredPivot is the control).
    // Story 10.3a: this heavy pair's former 10 s hold is not asserted. Loads resting on top of a frictionless pivot are an unstable
    // (inverted pendulum) equilibrium: with I of about 15 kg m^2 and M g h of about 31 N m, any seed grows about e-fold every 0.7 s,
    // so a 10 s hold measures the size of the solver-noise seed, not whether equal loads balance. The requirement's ten-second
    // balance is EqualBallLoads_StayBalancedForTenSeconds.
    [Fact]
    public async Task SymmetricalLoadBalance_RemainsBalanced()
    {
        var lever = Lever(1, 0, 3, 0);
        var loadLeft = WeightTests.Placed(2, -1.2f, BeamTop, 0, 4f);
        var loadRight = WeightTests.Placed(3, 1.2f, BeamTop, 0, 4f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(lever, loadLeft, loadRight));
        var beamId = construction.Instances.Identities[lever.Id].Hinges[0].Body;

        await using var world = await WorkshopIntegrationHarness.Running(construction);

        var maxTilt = 0f;
        var maxOmega = 0f;
        for (var t = 0; t < 360; t++)
        {
            await world.Advance(1);
            maxTilt = MathF.Max(maxTilt, MathF.Abs(BeamAngle(world.Committed, beamId)));
            maxOmega = MathF.Max(maxOmega, MathF.Abs(BeamAngularVelocity(world.Committed, beamId)));
        }

        Assert.True(maxTilt <= 0.01f, $"Equally loaded beam tilted {maxTilt:F4} rad within 3 s, expected <= 0.01 rad");
        Assert.True(maxOmega <= 0.02f, $"Equally loaded beam turned at {maxOmega:F4} rad/s within 3 s, expected <= 0.02 rad/s");
    }

    // CAT-034 fact 17, the requirement row's equal-load ten-second balance: Basketballs resting on both arms at x = +/-1 m (centres at
    // the beam top plus their 0.34 m radius) keep the beam within 0.01 rad, and their heights within 0.01 m of each other, at every
    // tick of 1,200 ticks (10 s at 120 Hz); the final hinge speed is at most 0.02 rad/s. A height split beyond 0.01 m is the beam
    // visibly tilting under the balls, after which they roll off its low end.
    [Fact]
    public async Task EqualBallLoads_StayBalancedForTenSeconds()
    {
        var lever = Lever(1, 0, 3, 0);
        var left = Basketball(2, -1f, BeamTop + 0.34f, 0);
        var right = Basketball(3, 1f, BeamTop + 0.34f, 0);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(lever, left, right));
        var beamId = construction.Instances.Identities[lever.Id].Hinges[0].Body;

        await using var world = await WorkshopIntegrationHarness.Running(construction);

        var maxTilt = 0f;
        var maxSplit = 0f;
        for (var t = 0; t < 1200; t++)
        {
            await world.Advance(1);
            var read = world.Committed;
            maxTilt = MathF.Max(maxTilt, MathF.Abs(BeamAngle(read, beamId)));
            maxSplit = MathF.Max(maxSplit, MathF.Abs(BodyY(read, left.Id) - BodyY(read, right.Id)));
        }
        var omega = MathF.Abs(BeamAngularVelocity(world.Committed, beamId));

        Assert.True(maxTilt <= 0.01f, $"Equally ball-loaded beam tilted {maxTilt:F4} rad within 10 s, expected <= 0.01 rad");
        Assert.True(maxSplit <= 0.01f, $"The balls' heights split by {maxSplit:F4} m within 10 s, expected <= 0.01 m");
        Assert.True(omega <= 0.02f, $"Final hinge speed {omega:F4} rad/s, expected <= 0.02 rad/s");
    }

    [Fact]
    public async Task DepthMissControl_PayloadRemainsUndisturbed()
    {
        var lever = Lever(1, 0, 3, 0);
        var basketball = Basketball(2, 1.2f, 3.46f, 0);
        // Depth miss: z = 2.0 m
        var bowlingMiss = BowlingBall(3, -1.2f, 6f, 2.0f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(lever, basketball, bowlingMiss));

        await using var world = await WorkshopIntegrationHarness.Running(construction);

        var maxPayloadHeight = 3.46f;
        for (var t = 0; t < 240; t++)
        {
            await world.Advance(1);
            var height = BodyY(world.Committed, basketball.Id);
            maxPayloadHeight = MathF.Max(maxPayloadHeight, height);
        }

        Assert.True(maxPayloadHeight < 3.6f, $"Payload height was {maxPayloadHeight:F3} m, expected < 3.6 m on depth miss");
    }

    [Fact]
    public async Task PivotHitControl_PayloadRemainsUndisturbed()
    {
        var lever = Lever(1, 0, 3, 0);
        var basketball = Basketball(2, 1.2f, 3.46f, 0);
        // Strike directly above fulcrum pivot: x = 0
        var bowlingPivot = BowlingBall(3, 0f, 6f, 0f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(lever, basketball, bowlingPivot));

        await using var world = await WorkshopIntegrationHarness.Running(construction);

        var maxPayloadHeight = 3.46f;
        for (var t = 0; t < 240; t++)
        {
            await world.Advance(1);
            var height = BodyY(world.Committed, basketball.Id);
            maxPayloadHeight = MathF.Max(maxPayloadHeight, height);
        }

        Assert.True(maxPayloadHeight < 3.6f, $"Payload height was {maxPayloadHeight:F3} m, expected < 3.6 m on pivot strike");
    }

    [Fact]
    public async Task AngularLimitsBoundary_StopsArrestTravelWithoutPenetration()
    {
        // High-energy impact (4 kg bowling ball dropped from y = 6 m) drives the lever through its dynamic travel arc
        var lever = Lever(1, 0, 3, 0);
        var basketball = Basketball(2, 1.2f, 3.46f, 0);
        var bowling = BowlingBall(3, -1.2f, 6f, 0);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(lever, basketball, bowling));
        var beamId = construction.Instances.Identities[lever.Id].Hinges[0].Body;

        await using var world = await WorkshopIntegrationHarness.Running(construction);

        var minAngle = 0f;
        var maxAngle = 0f;
        for (var t = 0; t < 240; t++)
        {
            await world.Advance(1);
            var angle = BeamAngle(world.Committed, beamId);
            minAngle = MathF.Min(minAngle, angle);
            maxAngle = MathF.Max(maxAngle, angle);
        }

        // Beam exercises a wide angular stroke (> 0.5 rad peak-to-peak)
        var totalStroke = maxAngle - minAngle;
        Assert.True(totalStroke >= 0.50f, $"Beam angular stroke was {totalStroke:F3} rad, expected >= 0.50 rad");

        // Travel stops at ±pi/6 (±0.5236 rad) strictly arrest motion without penetration
        const float limitTolerance = 0.05f;
        Assert.True(minAngle >= -MathF.PI / 6f - limitTolerance,
            $"Beam penetrated lower stop: min angle {minAngle:F3} rad (limit {-MathF.PI / 6f:F3})");
        Assert.True(maxAngle <= MathF.PI / 6f + limitTolerance,
            $"Beam penetrated upper stop: max angle {maxAngle:F3} rad (limit {MathF.PI / 6f:F3})");
    }

    [Fact]
    public async Task RopeLiftingIntegration_FallingWeightLiftsArm()
    {
        // Falling counterweight (4 kg) routed through pulleys lifts lever TieA
        // Lever at (0, 3, 0). TieA is at (-1.8, 0, 0) relative to beam, so world position is (-1.8, 3, 0).
        var lever = Lever(1, 0, 3, 0);
        var p1 = WorkshopInput.Generic(new("pulley"), new(2), -1.8f, 6f, -.12f, 0, 0, 0, 1);
        var p2 = WorkshopInput.Generic(new("pulley"), new(3), -4.0f, 6f, -.12f, 0, 0, 0, 1);
        var weight = WeightTests.Placed(4, -4.0f, 4.5f, 0f, 4f);

        var tieLever = new Vector3(-1.8f, 3f, 0f);
        var tieWeight = new Vector3(-4.0f, 4.5f, 0f) + DeclaredPorts.Current.LocalPosition(weight, WorkshopSocket.Tie);

        var guides = new (Vector3 Position, Quaternion Rotation)[]
        {
            (new(-1.8f, 6f, -.12f), Quaternion.Identity),
            (new(-4.0f, 6f, -.12f), Quaternion.Identity)
        };
        var evaluated = RopeGeometry.EvaluateRoute(tieLever, tieWeight, guides);
        var len = evaluated.TotalLength;

        var span0 = Vector3.Distance(tieLever, new Vector3(-1.8f, 6f, 0f));
        var span1 = 2.2f; // distance between pulleys
        var span2 = len - span0 - span1;

        var link0 = new WorkshopConnection(lever.Id, WorkshopSocket.TieA, p1.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span0);
        var link1 = new WorkshopConnection(p1.Id, WorkshopSocket.Tie, p2.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span1);
        var link2 = new WorkshopConnection(p2.Id, WorkshopSocket.Tie, weight.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span2);

        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(lever, p1, p2, weight),
            Connections: new(link0, link1, link2));

        var beamId = construction.Instances.Identities[lever.Id].Hinges[0].Body;

        await using var world = await WorkshopIntegrationHarness.Running(construction);

        var initialAngle = BeamAngle(world.Committed, beamId);
        await world.Advance(50);

        var finalAngle = BeamAngle(world.Committed, beamId);
        // Tension pulling upward on TieA (at x = -1.8) lifts the left arm (negative rotation around Z)
        Assert.True(finalAngle < initialAngle - 0.02f, $"Falling weight should lift TieA: initial {initialAngle:F3}, final {finalAngle:F3}");
        Assert.True(finalAngle >= -MathF.PI / 6f - 0.05f, $"Beam penetrated lower stop: angle {finalAngle:F3}");
    }

    [Theory]
    [InlineData(0.2f)] // Below 0.5 kg
    [InlineData(25f)]  // Above 20 kg
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void OutOfRangeBeamMass_IsRefused(float mass)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            WorkshopInput.Generic(new PartId("impact_lever"), new(1), 0, 3, 0, 0, 0, 0, 1,
                new(new Dictionary<ParameterId, float> { [new("beam_mass")] = mass })).Validate());
    }

    [Theory]
    [InlineData(-35f)] // Below -30 deg
    [InlineData(35f)]  // Above +30 deg
    [InlineData(float.NaN)]
    [InlineData(float.NegativeInfinity)]
    public void OutOfRangeInitialAngle_IsRefused(float angle)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            WorkshopInput.Generic(new PartId("impact_lever"), new(1), 0, 3, 0, 0, 0, 0, 1,
                new(new Dictionary<ParameterId, float> { [new("initial_angle")] = angle })).Validate());
    }

    [Fact]
    public async Task RunResetRestoration_RestoresAuthoredStateExactly()
    {
        var lever = Lever(1, 0, 3, 0, angleDeg: 15f);
        var basketball = Basketball(2, 1.2f, 3.46f, 0);
        var bowling = BowlingBall(3, -1.2f, 6f, 0);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(lever, basketball, bowling));
        var beamId = construction.Instances.Identities[lever.Id].Hinges[0].Body;

        await using var harness = await WorkshopIntegrationHarness.Building(construction);
        await harness.Run();

        // Advance while running to displace state
        await harness.Advance(120);
        var displacedAngle = BeamAngle(harness.Committed, beamId);
        Assert.NotEqual(15f * MathF.PI / 180f, displacedAngle);

        // Reset
        await harness.Reset();
        var resetRead = harness.Committed;

        var restoredAngle = BeamAngle(resetRead, beamId);
        var restoredOmega = BeamAngularVelocity(resetRead, beamId);
        var restoredVel = BodyVel(resetRead, beamId);

        // Angle restored within 1 urad
        var initialAngleRad = 15f * MathF.PI / 180f;
        Assert.True(MathF.Abs(restoredAngle - initialAngleRad) <= 1e-5f, $"Restored angle {restoredAngle} vs initial {initialAngleRad}");
        Assert.True(MathF.Abs(restoredOmega) <= 1e-5f, $"Restored angular velocity {restoredOmega} should be zero");
        Assert.True(restoredVel.Length() <= 1e-5f, $"Restored linear velocity {restoredVel} should be zero");
    }

    [Fact]
    public async Task RopeLiftingIntegration_TieBLiftsArm()
    {
        // Falling counterweight (4 kg) routed through pulleys lifts lever TieB
        // Lever at (0, 3, 0). TieB is at (1.8, 0, 0) relative to beam, so world position is (1.8, 3, 0).
        var lever = Lever(1, 0, 3, 0);
        var p1 = WorkshopInput.Generic(new("pulley"), new(2), 1.8f, 6f, -.12f, 0, 0, 0, 1);
        var p2 = WorkshopInput.Generic(new("pulley"), new(3), 4.0f, 6f, -.12f, 0, 0, 0, 1);
        var weight = WeightTests.Placed(4, 4.0f, 4.5f, 0f, 4f);

        var tieLever = new Vector3(1.8f, 3f, 0f);
        var tieWeight = new Vector3(4.0f, 4.5f, 0f) + DeclaredPorts.Current.LocalPosition(weight, WorkshopSocket.Tie);

        var guides = new (Vector3 Position, Quaternion Rotation)[]
        {
            (new(1.8f, 6f, -.12f), Quaternion.Identity),
            (new(4.0f, 6f, -.12f), Quaternion.Identity)
        };
        var evaluated = RopeGeometry.EvaluateRoute(tieLever, tieWeight, guides);
        var len = evaluated.TotalLength;

        var span0 = Vector3.Distance(tieLever, new Vector3(1.8f, 6f, 0f));
        var span1 = 2.2f; // distance between pulleys
        var span2 = len - span0 - span1;

        var link0 = new WorkshopConnection(lever.Id, WorkshopSocket.TieB, p1.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span0);
        var link1 = new WorkshopConnection(p1.Id, WorkshopSocket.Tie, p2.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span1);
        var link2 = new WorkshopConnection(p2.Id, WorkshopSocket.Tie, weight.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span2);

        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(lever, p1, p2, weight),
            Connections: new(link0, link1, link2));

        var beamId = construction.Instances.Identities[lever.Id].Hinges[0].Body;

        await using var world = await WorkshopIntegrationHarness.Running(construction);

        var initialAngle = BeamAngle(world.Committed, beamId);
        await world.Advance(50);

        var finalAngle = BeamAngle(world.Committed, beamId);
        // Tension pulling upward on TieB (at x = 1.8) lifts the right arm (positive rotation around Z)
        Assert.True(finalAngle > initialAngle + 0.02f, $"Falling weight should lift TieB: initial {initialAngle:F3}, final {finalAngle:F3}");
        Assert.True(finalAngle <= MathF.PI / 6f + 0.05f, $"Beam penetrated upper stop: angle {finalAngle:F3}");
    }

    [Fact]
    public async Task MultiJointBatching_SolvesConcurrentlyAcrossSimd()
    {
        // 2 Levers at x = 0 and x = 6, both hit by falling bowling balls flinging basketball payloads
        var lever1 = Lever(1, 0, 3, 0);
        var basket1 = Basketball(2, 1.2f, 3.46f, 0);
        var bowl1 = BowlingBall(3, -1.2f, 6f, 0);

        var lever2 = Lever(4, 6, 3, 0);
        var basket2 = Basketball(5, 7.2f, 3.46f, 0);
        var bowl2 = BowlingBall(6, 4.8f, 6f, 0);

        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(lever1, basket1, bowl1, lever2, basket2, bowl2));

        var beamId1 = construction.Instances.Identities[lever1.Id].Hinges[0].Body;
        var beamId2 = construction.Instances.Identities[lever2.Id].Hinges[0].Body;

        await using var world = await WorkshopIntegrationHarness.Running(construction);

        var maxPayloadHeight1 = 3.46f;
        var maxPayloadHeight2 = 3.46f;

        for (var t = 0; t < 240; t++)
        {
            await world.Advance(1);
            var read = world.Committed;

            maxPayloadHeight1 = MathF.Max(maxPayloadHeight1, BodyPos(read, basket1.Id).Y);
            maxPayloadHeight2 = MathF.Max(maxPayloadHeight2, BodyPos(read, basket2.Id).Y);

            var angle1 = BeamAngle(read, beamId1);
            var angle2 = BeamAngle(read, beamId2);

            Assert.True(angle1 >= -MathF.PI / 6f - 0.05f && angle1 <= MathF.PI / 6f + 0.05f);
            Assert.True(angle2 >= -MathF.PI / 6f - 0.05f && angle2 <= MathF.PI / 6f + 0.05f);
        }

        Assert.True(maxPayloadHeight1 > 4.0f, $"Payload 1 reached height {maxPayloadHeight1:F3} m, expected > 4.0 m");
        Assert.True(maxPayloadHeight2 > 4.0f, $"Payload 2 reached height {maxPayloadHeight2:F3} m, expected > 4.0 m");
    }

    [Fact]
    public void SaveLoadPersistenceRoundtrip_RestoresAuthoredState()
    {
        var lever = Lever(1, 0, 3, 0, mass: 4.5f, angleDeg: 12f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(lever));
        var saved = new WorkshopSavedConstruction(construction, new(10));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(saved.Construction.Instances, loaded.Construction.Instances);
    }

    [Theory]
    [InlineData("beam_mass", 0.1f)]   // Below minimum (0.5 kg)
    [InlineData("beam_mass", 50f)]    // Above maximum (20 kg)
    [InlineData("initial_angle", -45f)] // Below minimum (-30 deg)
    [InlineData("initial_angle", 45f)]  // Above maximum (+30 deg)
    public void SaveDecoding_RefusesOutOfRangeValues(string paramKey, float value)
    {
        var parameters = new InstanceParameterCollection(new Dictionary<ParameterId, float>
        {
            [new("beam_mass")] = paramKey == "beam_mass" ? value : 2f,
            [new("initial_angle")] = paramKey == "initial_angle" ? value : 0f
        });
        var forgedInstance = new WorkshopGenericInstance(new("impact_lever"), new(1), default, default, CanonicalRotation.Identity)
        {
            Parameters = parameters
        };
        var malformed = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(forgedInstance));
        var savedBytes = SaveFixture.Forged(malformed, 10);
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, savedBytes));
    }

    [Fact]
    public void PhysicsSceneDeclaration_RefusesInvalidRevoluteDeclarations()
    {
        var lever = Lever(1, 0, 3, 0);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(lever));
        var valid = WorkshopPhysicsCompiler.Compile(construction, new(101, 206));
        var revolute = valid.Revolutes[0];

        // Duplicate pair: same body pair reversed or identical
        Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(valid.Document, valid.NextIdentity + 1,
            valid.Bodies.ToArray(), valid.Colliders.ToArray(), valid.Materials.ToArray(), [], [],
            revolutes: [revolute, revolute with { Id = new(valid.NextIdentity) }]));

        // Self-connection BodyA == BodyB
        Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(valid.Document, valid.NextIdentity + 1,
            valid.Bodies.ToArray(), valid.Colliders.ToArray(), valid.Materials.ToArray(), [], [],
            revolutes: [revolute with { BodyB = revolute.BodyA }]));

        // Absent body
        Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(valid.Document, valid.NextIdentity + 1,
            valid.Bodies.ToArray(), valid.Colliders.ToArray(), valid.Materials.ToArray(), [], [],
            revolutes: [revolute with { BodyB = new(9999) }]));

        // Both static bodies
        var staticBodies = valid.Bodies.ToArray();
        for (var i = 0; i < staticBodies.Length; i++)
            staticBodies[i] = staticBodies[i] with { Motion = RigidMotionKind.Static };
        Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(valid.Document, valid.NextIdentity + 1,
            staticBodies, valid.Colliders.ToArray(), valid.Materials.ToArray(), [], [],
            revolutes: [revolute]));

        // ID not below nextIdentity
        Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(valid.Document, revolute.Id.Value,
            valid.Bodies.ToArray(), valid.Colliders.ToArray(), valid.Materials.ToArray(), [], [],
            revolutes: [revolute]));
    }
}
