using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 11.1a (CAT-042): the shipped Motor package, a dynamic rotor on a <see cref="RevoluteTravel.Continuous"/> hinge with a supplied
/// velocity drive (the <c>speed</c> and <c>torque</c> parameters) and 0.05 N·m of bearing friction, supplied through its PowerIn
/// consumer. Every case runs the real compiled package through the shared native harness (the built bundle, the production compiler,
/// the Workshop simulation over the production device), wired from a Battery as a player wires it.
///
/// Every expectation derives from the declared rotor: 0.5 kg, radius 0.3 m, so its axial inertia is I = m·r²/2 = 0.0225 kg·m². At the
/// default speed 6 rad/s and torque 20 N·m it reaches ω_t after I·ω_t/(τ_d − τ_f) = 6.8 ms and holds ½·I·ω_t² = 0.405 J; unpowered,
/// the friction spins it down at τ_f/I = 2.22 rad/s².
/// </summary>
public sealed class MotorTests
{
    private static readonly PartId Motor = new("motor");
    private const float RotorMass = .5f;
    private const float RotorRadius = .3f;
    private const float RotorInertia = RotorMass * RotorRadius * RotorRadius / 2f;
    private const float FrictionTorque = .05f;
    private const float DefaultSpeed = 6f;
    private const float DefaultTorque = 20f;
    private const int TicksPerSecond = 120;
    private const float TickSeconds = 1f / TicksPerSecond;
    private const float Substep = 1f / 480f;
    private const float Gravity = 9.81f;
    /// <summary>The rotor centre in the Motor's frame (CAT-042 §2).</summary>
    private static readonly Vector3 RotorCentre = new(0f, 0f, .54f);
    private static readonly Vector3 MotorAt = new(0f, 4f, 0f);

    /// <summary>The angular rest envelope (rad/s): slower shaft motion reads as still.</summary>
    private const float RestSpin = .02f;
    /// <summary>A shaft's speed is read by eye: 2 % off its target is not visibly wrong, while every plausible defect (a dropped drive,
    /// an unbounded drive, a lost target) misses by far more.</summary>
    private static float SpeedAllowance(float target) => .02f * MathF.Abs(target);
    /// <summary>The energy slack (J): 3 % of the rotor's kinetic energy at its target, covering the soft joint rows' position correction
    /// and f32 rounding when kinetic energy is compared with committed work. A drive that created energy would gain at least one
    /// substep's τ_d·h·ω_t (0.25 J at the defaults, against a 0.012 J slack).</summary>
    private static float EnergySlack(float target) => MathF.Max(.01f, .03f * .5f * RotorInertia * target * target);
    /// <summary>The load slack (J) for the held and back-driven cases: 0.01 J, a hundredth of the 1 kg rim load's 2.9 J per radian.</summary>
    private const float LoadSlack = .01f;
    /// <summary>The ticks the drive needs from rest to ω_t at τ_d against τ_f, plus one.</summary>
    private static int RampTicks(float speed, float torque) =>
        (int)MathF.Ceiling(RotorInertia * speed / (torque - FrictionTorque) * TicksPerSecond) + 1;

    private static readonly Vector3 Gold = new(247f / 255f, 203f / 255f, 82f / 255f);
    private static readonly Vector3 Slate = new(85f / 255f, 101f / 255f, 115f / 255f);
    /// <summary>A lamp channel within half an 8-bit colour step reads as its endpoint.</summary>
    private const float ColourStep = .5f / 255f;

    private static WorkshopGenericInstance MotorPart(ulong id = 1, float speed = DefaultSpeed, float torque = DefaultTorque,
        Quaternion? rotation = null)
    {
        var q = rotation ?? Quaternion.Identity;
        return WorkshopInput.Generic(RuntimeTestServices.Bundle, Motor, new(id), MotorAt.X, MotorAt.Y, MotorAt.Z, q.X, q.Y, q.Z, q.W,
            new([new(new("speed"), speed), new(new("torque"), torque)]));
    }

    private static WorkshopConnection Power(GpuBodyId battery, GpuBodyId motor) =>
        new(battery, PortId.Supply, motor, PortId.PowerIn, WorkshopConnectionDomain.Electrical);

    /// <summary>A Motor and a Battery (id 2), wired Supply → PowerIn when <paramref name="wired"/>.</summary>
    private static WorkshopConstruction Supplied(WorkshopGenericInstance motor, bool wired = true,
        ElectricalEnable enabled = ElectricalEnable.Enabled, IWorkshopInstance? other = null)
    {
        var battery = PlacedParts.Battery(new(2), 3, 4, 0, enabled: enabled);
        IWorkshopInstance[] instances = other is null ? [motor, battery] : [motor, battery, other];
        return new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(instances))
            { Connections = wired ? new(Power(battery.Id, motor.Id)) : new() };
    }

    private static GpuBodyId Rotor(WorkshopConstruction construction, GpuBodyId motor) =>
        construction.Instances.Identities[motor].Hinges[0].Body;

    /// <summary>One committed tick: the rotor's linear and angular velocity, its committed orientation, its axial spin about the mount's
    /// hinge axis, the committed drive state of the scene's one hinge, the supply lamp's colour and the Motor's committed target speed.</summary>
    private readonly record struct Tick(int Index, Vector3 Position, Vector3 Velocity, Vector3 Angular, Quaternion Rotation, float Spin,
        RevoluteStateRead State, Vector3 Lamp, float Target)
    {
        public float Kinetic => .5f * RotorMass * Velocity.LengthSquared() + .5f * RotorInertia * Angular.LengthSquared();
    }

    /// <summary>The signed angle (rad, within ±π) the committed rotor pose turned about the world <paramref name="axis"/> from one tick to
    /// another: the swing of the relative rotation to·from⁻¹ about that axis (q and −q are the same rotation).</summary>
    private static float PoseTurn(Tick from, Tick to, Vector3 axis)
    {
        var relative = to.Rotation * Quaternion.Conjugate(from.Rotation);
        if (relative.W < 0f) relative = Quaternion.Negate(relative);
        return 2f * MathF.Atan2(Vector3.Dot(new Vector3(relative.X, relative.Y, relative.Z), axis), relative.W);
    }

    private sealed class Probe(WorkshopIntegrationHarness harness, GpuBodyId owner, GpuBodyId rotor, Vector3 axis)
    {
        /// <summary>The observation at Run, before any advance.</summary>
        public Tick Initial { get; set; }
        public List<Tick> Ticks { get; } = [];
        private double _clock;

        public Tick Observe()
        {
            var read = harness.Committed;
            Assert.True(read.Bodies.TryGet(rotor, out var body));
            var angular = new Vector3(body.AngularVelocity.X, body.AngularVelocity.Y, body.AngularVelocity.Z);
            var position = new Vector3((body.Body.Cell.X + body.Body.Local.X) / 16f, (body.Body.Cell.Y + body.Body.Local.Y) / 16f,
                (body.Body.Cell.Z + body.Body.Local.Z) / 16f);
            var state = PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.ToArray(), 0);
            _clock = Math.Max(_clock, read.Tick.Value * TickSeconds);
            var samples = harness.Presentation!.Advance(AnimationTestTime.Seconds(_clock));
            float Channel(int binding) => BitConverter.UInt32BitsToSingle(
                Assert.Single(samples, sample => sample.Owner == owner && sample.Binding == binding).Value.CanonicalBits);
            return new((int)read.Tick.Value, position, new(body.Body.Velocity.X, body.Body.Velocity.Y, body.Body.Velocity.Z), angular,
                new(body.Rotation.X, body.Rotation.Y, body.Rotation.Z, body.Rotation.W), Vector3.Dot(angular, axis), state,
                new(Channel(0), Channel(1), Channel(2)), read.RunControl(owner, RunControlLane.RevoluteDriveTarget, joint: read.DriveTargets[0].Joint).Scalar);
        }

        /// <summary>Advances the presentation clock past the committed tick (a later frame of the animation worker).</summary>
        public Vector3 LampAfter(double seconds)
        {
            _clock += seconds;
            var samples = harness.Presentation!.Advance(AnimationTestTime.Seconds(_clock));
            float Channel(int binding) => BitConverter.UInt32BitsToSingle(
                Assert.Single(samples, sample => sample.Owner == owner && sample.Binding == binding).Value.CanonicalBits);
            return new(Channel(0), Channel(1), Channel(2));
        }

        /// <summary>Another owner's declared binding value at the current presentation time.</summary>
        public float ChannelOf(GpuBodyId other, int binding) => BitConverter.UInt32BitsToSingle(Assert.Single(
            harness.Presentation!.Advance(AnimationTestTime.Seconds(_clock)), sample => sample.Owner == other && sample.Binding == binding).Value.CanonicalBits);

        public async Task<List<Tick>> Advance(int ticks)
        {
            var result = new List<Tick>(ticks);
            for (var i = 0; i < ticks; i++)
            {
                Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.AdvanceOne()).Outcome);
                result.Add(Observe());
            }
            Ticks.AddRange(result);
            return result;
        }
    }

    private static async Task<(WorkshopIntegrationHarness Harness, Probe Probe)> Start(WorkshopConstruction construction, GpuBodyId motor,
        Quaternion? rotation = null, Func<ICompiledScene, WorkshopCompiledScene>? fixture = null)
    {
        WorkshopIntegrationHarness harness;
        if (fixture is null) harness = await WorkshopIntegrationHarness.Running(construction);
        else
        {
            harness = await WorkshopIntegrationHarness.Initialized(construction.Settings);
            var command = harness.Command(WorkshopCommandKind.Construct, construction);
            var response = await harness.Dispatch(command with { Scene = fixture(command.Scene!) });
            Assert.Equal(WorkshopCommandOutcome.Applied, response.Result.Outcome);
            await harness.Run();
        }
        var axis = Vector3.Transform(Vector3.UnitZ, rotation ?? Quaternion.Identity);
        var probe = new Probe(harness, motor, Rotor(construction, motor), axis);
        probe.Initial = probe.Observe();
        return (harness, probe);
    }

    /// <summary>The compiled scene with load spans, joints or an undriven slider appended: a test fixture for a load or slot the Motor has
    /// no socket for (its Drive socket is 11.1b). Each appended row takes fresh identities above the scene's own (a span two: its own and
    /// its route's).</summary>
    private static WorkshopCompiledScene Rebuilt(ICompiledScene compiled, IReadOnlyList<TensileSpanDeclaration> spans,
        IReadOnlyList<RevoluteConstraintDeclaration> revolutes, IReadOnlyList<PrismaticConstraintDeclaration> prismatics)
    {
        var physics = compiled.Physics;
        return new WorkshopCompiledScene(compiled.Identity, compiled.Revision, compiled.Settings,
            new PhysicsSceneDeclaration(physics.Document,
                physics.NextIdentity + 2UL * (ulong)spans.Count + (ulong)revolutes.Count + (ulong)prismatics.Count, physics.Bodies,
                physics.Colliders, physics.Materials, physics.Sensors, physics.Guides, physics.Triggers, physics.ContactWorks,
                physics.OrientationSensors, physics.Electrical.Sources.ToArray(), physics.Electrical.Bindings.ToArray(),
                [.. physics.Prismatics.ToArray(), .. prismatics],
                physics.SupplyGraph.Nodes.ToArray(), physics.SupplyGraph.Edges.ToArray(), physics.ContactLoadSensors,
                physics.PrismaticDrives, [.. physics.TensileSpans.ToArray(), .. spans], [.. physics.Revolutes.ToArray(), .. revolutes],
                physics.CompliantSurfaces, physics.RevoluteDrives),
            compiled.Activation, compiled.Presentation, compiled.Owners, compiled.RunControls);
    }

    private static Func<ICompiledScene, WorkshopCompiledScene> WithSpans(
        Func<IPhysicsSceneDeclaration, IReadOnlyList<TensileSpanDeclaration>> spans) => compiled => Rebuilt(compiled, spans(compiled.Physics), [], []);

    /// <summary>One complete single-span route with fresh span and route identities.</summary>
    private static TensileSpanDeclaration Span(IPhysicsSceneDeclaration physics, GpuBodyId a, Vector3 localA, GpuBodyId b, Vector3 localB,
        float length) =>
        new(new(physics.NextIdentity), new(physics.NextIdentity + 1), 0, a, localA, b, localB, length, true);

    private static void AssertEnergy(IEnumerable<Tick> ticks, float speed, float torque) => Assert.All(ticks, tick =>
    {
        Assert.True(tick.Kinetic <= tick.State.Work + EnergySlack(speed),
            $"tick {tick.Index}: rotor kinetic energy {tick.Kinetic:F4} J exceeds the drive's work {tick.State.Work:F4} J");
        var bound = torque * MathF.Abs(speed) * tick.Index * TickSeconds;
        Assert.True(tick.State.Work <= bound + EnergySlack(speed),
            $"tick {tick.Index}: work {tick.State.Work:F4} J exceeds τ_d·|ω_t|·t = {bound:F4} J");
    });

    private static void AssertColour(Vector3 expected, Vector3 actual, string where) =>
        Assert.True(MathF.Abs(actual.X - expected.X) <= ColourStep && MathF.Abs(actual.Y - expected.Y) <= ColourStep &&
            MathF.Abs(actual.Z - expected.Z) <= ColourStep, $"{where}: the lamp is {actual}, expected {expected}");

    // ---- supplied ---------------------------------------------------------------------------------------------------------------

    // Spin-up at the defaults, and the top of the speed range. The rotor reaches ω_t within its ramp I·ω_t/(τ_d − τ_f) plus a tick and
    // holds it within 2 % for a second. It then pays only the bearing loss: from 0.5 s to 1 s the work grows by τ_f·ω_t·0.5 s (within
    // 10 %; a ledger counting τ_d·ω_t would show 60 J at the defaults, a missing friction row 0). Its travel after 1 s is
    // ω_t·(1 s − T/2) within 2 %. The energy oracle holds at every tick.
    [Theory]
    [InlineData(6f)]
    [InlineData(20f)]
    public async Task ASuppliedMotorSpinsUpToItsSpeedHoldsItAndPaysOnlyItsBearingLoss(float speed)
    {
        var motor = MotorPart(speed: speed);
        var (harness, probe) = await Start(Supplied(motor), motor.Id);
        await using var _ = harness;
        var ticks = await probe.Advance(TicksPerSecond);

        Assert.All(ticks.Skip(RampTicks(speed, DefaultTorque) - 1), tick => Assert.True(MathF.Abs(tick.Spin - speed) <= SpeedAllowance(speed),
            $"tick {tick.Index}: the rotor spins at {tick.Spin:F3} rad/s, target {speed} rad/s"));
        Assert.All(ticks, tick => Assert.True(tick.State.Supplied, $"tick {tick.Index}: the committed drive is unsupplied"));
        var paid = ticks[^1].State.Work - ticks[TicksPerSecond / 2 - 1].State.Work;
        var loss = FrictionTorque * speed * .5f;
        Assert.True(MathF.Abs(paid - loss) <= .1f * loss, $"From 0.5 s to 1 s the motor did {paid:F4} J; τ_f·ω_t·0.5 s is {loss:F4} J");
        var ramp = RotorInertia * speed / (DefaultTorque - FrictionTorque);
        var travel = speed * (1f - ramp / 2f);
        Assert.True(MathF.Abs(ticks[^1].State.Travel - travel) <= .02f * travel,
            $"After 1 s the travel is {ticks[^1].State.Travel:F3} rad; ω_t·(t − T/2) is {travel:F3} rad");
        AssertEnergy(ticks, speed, DefaultTorque);
    }

    // The weakest Motor (torque 0.1 N·m, twice the bearing friction) still visibly accelerates: after 10 ticks and after 1 s it spins at
    // (τ_d − τ_f)·n·Δt/I, 0.0185 rad/s per tick, within one substep of friction (τ_f·h/I = 0.0046 rad/s, which the first substep from
    // rest does not yet resist) plus 2 %. A drive whose bound ignored the torque would be at ω_t after one tick, and one missing its
    // friction row would gain twice as fast. The first tick alone is not sampled: when Coulomb friction engages within it is incidental.
    [Fact]
    public async Task TheLowestTorqueAcceleratesTheRotorAtItsNetTorqueOverItsInertia()
    {
        const float torque = .1f;
        var motor = MotorPart(torque: torque);
        var (harness, probe) = await Start(Supplied(motor), motor.Id);
        await using var _ = harness;
        var ticks = await probe.Advance(TicksPerSecond);

        foreach (var n in new[] { 10, TicksPerSecond })
        {
            var expected = (torque - FrictionTorque) * n * TickSeconds / RotorInertia;
            var allowance = FrictionTorque * Substep / RotorInertia + .02f * expected;
            Assert.True(MathF.Abs(ticks[n - 1].Spin - expected) <= allowance,
                $"After {n} ticks at 0.1 N·m the rotor spins at {ticks[n - 1].Spin:F5} rad/s; (τ_d − τ_f)·t/I is {expected:F5} rad/s");
        }
        AssertEnergy(ticks, DefaultSpeed, torque);
    }

    // The supply lamp follows the drive's committed supplied state: slate at Run, a SmoothStep blend mid-clip (neither endpoint 5 ticks,
    // 0.042 s, after the first supplied tick), gold 0.1 s after supply begins. The clip starts at the presentation frame that accepts the
    // first supplied read, at or one tick before that tick, so the mid sample is 5 or 6 ticks into the 12-tick clip and the clip has
    // ended by tick 13 either way.
    [Fact]
    public async Task TheSupplyLampBlendsFromSlateToGoldOverATenthOfASecondWhenSupplied()
    {
        var motor = MotorPart();
        var (harness, probe) = await Start(Supplied(motor), motor.Id);
        await using var _ = harness;
        AssertColour(Slate, probe.Initial.Lamp, "at Run");
        var ticks = await probe.Advance(TicksPerSecond / 4);

        AssertMidBlend(Slate, Gold, ticks[5].Lamp, "5 ticks into the blend");
        Assert.All(ticks.Skip(12), tick => AssertColour(Gold, tick.Lamp, $"tick {tick.Index}"));
    }

    /// <summary>A lamp strictly between its endpoints (each channel at least 5 % of the way from either), as a SmoothStep blend is 4 to
    /// 6 ticks into its 12-tick clip (the samples at index 4 are 4 or 5 ticks in, the one at index 5 is 5 or 6).</summary>
    private static void AssertMidBlend(Vector3 from, Vector3 to, Vector3 actual, string where) =>
        Assert.True(MathF.Abs(actual.X - from.X) > .05f * MathF.Abs(to.X - from.X) && MathF.Abs(actual.X - to.X) > .05f * MathF.Abs(to.X - from.X),
            $"{where} the lamp is {actual}, at an endpoint");

    // ---- unpowered ------------------------------------------------------------------------------------------------------------------

    public enum Unpowered { NoBattery, DisabledBattery, UnwiredBattery }

    // An unpowered Motor does nothing: with no battery, a disabled battery or an unwired one, the committed drive is unsupplied with
    // work exactly 0 at every tick of a second, the rotor stays slower than one bounded drive substep would leave it (τ_d·h/I = 1.85
    // rad/s; it does not move at all), and the lamp stays slate.
    [Theory]
    [InlineData(Unpowered.NoBattery)]
    [InlineData(Unpowered.DisabledBattery)]
    [InlineData(Unpowered.UnwiredBattery)]
    public async Task AnUnpoweredMotorStaysStillDoesNoWorkAndKeepsItsLampSlate(Unpowered control)
    {
        var motor = MotorPart();
        var construction = control switch
        {
            Unpowered.NoBattery => new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(motor)),
            Unpowered.DisabledBattery => Supplied(motor, enabled: ElectricalEnable.Disabled),
            Unpowered.UnwiredBattery => Supplied(motor, wired: false),
            _ => throw new ArgumentOutOfRangeException(nameof(control))
        };
        var (harness, probe) = await Start(construction, motor.Id);
        await using var _ = harness;
        var ticks = await probe.Advance(TicksPerSecond);

        Assert.All(ticks, tick => Assert.True((tick.State.Supplied, tick.State.Work) == (false, 0f) && MathF.Abs(tick.Spin) < RestSpin,
            $"tick {tick.Index}: supplied {tick.State.Supplied}, work {tick.State.Work}, spin {tick.Spin}"));
        AssertColour(Slate, ticks[^1].Lamp, "after 1 s unpowered");
    }

    // An activation wire never creates supply: the Motor has no activation socket, so a Switch's ActivationOut wired into its PowerIn
    // is refused at the connection seam with a typed reason.
    [Fact]
    public void AnActivationWireIntoThePowerInputIsRefused()
    {
        var motor = MotorPart();
        var button = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("switch"), new(3), -3, 4, 0, 0, 0, 0, 1);
        var instances = new WorkshopInstances(motor, button);
        var wire = new WorkshopConnection(button.Id, PortId.ActivationOut, motor.Id, PortId.PowerIn, WorkshopConnectionDomain.Activation);
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => new WorkshopConnections(wire).Validate(RuntimeTestServices.Core.Supply, instances));
    }

    // Supply cut mid-run (owner, 3 November 2026: the rotor spins down under bearing friction). After 0.5 s at ω_t the battery is
    // disabled. From then the drive is unsupplied with its work frozen at the cut value, the rotor decelerates at τ_f/I (1 s after the
    // cut it spins at ω_t − τ_f·1 s/I = 3.78 rad/s within 5 %), it comes to rest after ω_t·I/τ_f = 2.7 s within 10 % and stays at rest,
    // and the lamp returns to slate. An engine that ignored the principal frame (axial inertia 0.01195) would stop after 1.4 s.
    [Fact]
    public async Task CuttingTheSupplySpinsTheRotorDownUnderBearingFrictionWithNoFurtherWork()
    {
        var motor = MotorPart();
        var construction = Supplied(motor);
        var battery = construction.Instances[1].Id;
        var (harness, probe) = await Start(construction, motor.Id);
        await using var _ = harness;
        var before = (await probe.Advance(TicksPerSecond / 2))[^1];
        Assert.True(MathF.Abs(before.Spin - DefaultSpeed) <= SpeedAllowance(DefaultSpeed), $"Before the cut: {before}");
        await harness.Require(WorkshopCommandKind.ConfigureRunControl, runControl: harness.RunControl(battery, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        var after = await probe.Advance(4 * TicksPerSecond);

        Assert.All(after, tick => Assert.True((tick.State.Supplied, tick.State.Work) == (false, before.State.Work),
            $"tick {tick.Index}: supplied {tick.State.Supplied}, work {tick.State.Work} (at the cut {before.State.Work})"));
        var deceleration = FrictionTorque / RotorInertia;
        var oneSecond = before.Spin - deceleration;
        Assert.True(MathF.Abs(after[TicksPerSecond - 1].Spin - oneSecond) <= .05f * oneSecond,
            $"1 s after the cut the rotor spins at {after[TicksPerSecond - 1].Spin:F3} rad/s; expected {oneSecond:F3} rad/s");
        var stop = after.FindIndex(tick => MathF.Abs(tick.Spin) <= RestSpin);
        var expectedStop = before.Spin / deceleration;
        Assert.True(stop >= 0 && MathF.Abs((stop + 1) * TickSeconds - expectedStop) <= .1f * expectedStop,
            $"The rotor came to rest {(stop + 1) * TickSeconds:F3} s after the cut; ω_t·I/τ_f is {expectedStop:F3} s");
        Assert.All(after.Skip(stop), tick => Assert.True(MathF.Abs(tick.Spin) <= RestSpin, $"tick {tick.Index}: the stopped rotor spins at {tick.Spin}"));
        AssertMidBlend(Gold, Slate, after[4].Lamp, "5 ticks into the fade");
        AssertColour(Slate, after[^1].Lamp, "after the cut");
        AssertEnergy(probe.Ticks, DefaultSpeed, DefaultTorque);
    }

    // Supply enabled during Run: a Motor wired to an authored-Disabled Battery stays at rest with its lamp slate for 0.25 s; once the
    // Battery is enabled it spins up from rest to ω_t within its ramp plus a tick and holds it, and its lamp blends from slate to gold.
    [Fact]
    public async Task EnablingTheBatteryDuringRunSpinsTheMotorUpFromRestAndLightsItsLamp()
    {
        var motor = MotorPart();
        var construction = Supplied(motor, enabled: ElectricalEnable.Disabled);
        var battery = construction.Instances[1].Id;
        var (harness, probe) = await Start(construction, motor.Id);
        await using var _ = harness;
        var idle = await probe.Advance(TicksPerSecond / 4);
        Assert.All(idle, tick => Assert.True(!tick.State.Supplied && MathF.Abs(tick.Spin) < RestSpin && tick.State.Work == 0f,
            $"tick {tick.Index} before the enable: {tick}"));
        AssertColour(Slate, idle[^1].Lamp, "before the enable");
        await harness.Require(WorkshopCommandKind.ConfigureRunControl, runControl: harness.RunControl(battery, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Enabled)));
        var driven = await probe.Advance(TicksPerSecond / 2);

        Assert.All(driven.Skip(RampTicks(DefaultSpeed, DefaultTorque) - 1), tick => Assert.True(MathF.Abs(tick.Spin - DefaultSpeed) <= SpeedAllowance(DefaultSpeed),
            $"tick {tick.Index}: the rotor spins at {tick.Spin:F3} rad/s after the enable"));
        AssertMidBlend(Slate, Gold, driven[4].Lamp, "5 ticks after the enable");
        AssertColour(Gold, driven[^1].Lamp, "after the enable");
        // The power bound τ_d·|ω_t|·t counts from Run, not from the enable, so only the no-creation side is checked here.
        Assert.All(driven, tick => Assert.True(tick.Kinetic <= tick.State.Work + EnergySlack(DefaultSpeed),
            $"tick {tick.Index}: rotor kinetic energy {tick.Kinetic:F4} J exceeds the drive's work {tick.State.Work:F4} J"));
    }

    // ---- speed during Run (engine-revolute-drive-target) -----------------------------------------------------------------------------

    /// <summary>Sends the Motor's speed run control: one ConfigureRunControl on its RevoluteDriveTarget row.</summary>
    private static Task<WorkshopCommandResult> SetSpeed(WorkshopIntegrationHarness harness, GpuBodyId motor, float speed) =>
        harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(motor, RunControlLane.RevoluteDriveTarget, RunControlValue.Of(speed)));

    // Speed up during Run (owner, 3 November 2026; CAT-042 open question 5). A supplied Motor at its authored 6 rad/s is set to 12 rad/s.
    // The committed target stays 6 until the next committed step, which reads 12, as does every later tick. The shaft is within 2 % of
    // 12 rad/s once the ramp for Δω = 6 rad/s (I·Δω/(τ_d − τ_f) plus a tick) and one more tick of latency have passed, and holds it for a
    // second. The authored construction keeps speed 6. The drive's work stays within τ_d·max|ω_t|·t from Run (12 rad/s), and the rotor
    // never holds more kinetic energy than the drive did work.
    [Fact]
    public async Task ASpeedChangeDuringRunDrivesTheShaftToTheNewSpeedFromTheNextCommittedStep()
    {
        const float faster = 12f;
        var motor = MotorPart();
        var construction = Supplied(motor);
        var (harness, probe) = await Start(construction, motor.Id);
        await using var _ = harness;
        var before = (await probe.Advance(TicksPerSecond / 2))[^1];
        Assert.True(MathF.Abs(before.Spin - DefaultSpeed) <= SpeedAllowance(DefaultSpeed), $"Before the change: {before}");
        Assert.Equal(DefaultSpeed, before.Target);

        await SetSpeed(harness, motor.Id, faster);
        Assert.Equal(DefaultSpeed, probe.Observe().Target);
        var after = await probe.Advance(TicksPerSecond);

        Assert.All(after, tick => Assert.Equal(faster, tick.Target));
        Assert.All(after.Skip(RampTicks(faster - DefaultSpeed, DefaultTorque)), tick => Assert.True(
            MathF.Abs(tick.Spin - faster) <= SpeedAllowance(faster), $"tick {tick.Index}: the rotor spins at {tick.Spin:F3} rad/s, target {faster} rad/s"));
        Assert.Equal(construction, harness.Construction);
        AssertEnergy(probe.Ticks, faster, DefaultTorque);
    }

    // Speed down to 0 during Run. The committed target reads 0 from the next committed step. Braking is helped by the bearing friction,
    // so the 6 rad/s ramp plus one tick of latency is a conservative bound; from then the supplied shaft stays within the rest envelope
    // (speed 0 has no 2 % allowance) for a second. Control: the same shaft without the change still spins at 6 rad/s.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ASpeedOfZeroDuringRunBringsTheShaftToRestAndHoldsIt(bool changed)
    {
        var motor = MotorPart();
        var (harness, probe) = await Start(Supplied(motor), motor.Id);
        await using var _ = harness;
        await probe.Advance(TicksPerSecond / 2);

        if (changed) await SetSpeed(harness, motor.Id, 0f);
        var after = await probe.Advance(TicksPerSecond);

        var target = changed ? 0f : DefaultSpeed;
        Assert.All(after, tick => Assert.True(tick.State.Supplied && tick.Target == target, $"tick {tick.Index}: {tick}"));
        Assert.All(after.Skip(RampTicks(DefaultSpeed, DefaultTorque)), tick => Assert.True(changed
                ? MathF.Abs(tick.Spin) <= RestSpin : MathF.Abs(tick.Spin - DefaultSpeed) <= SpeedAllowance(DefaultSpeed),
            $"tick {tick.Index}: the rotor spins at {tick.Spin:F3} rad/s, target {target} rad/s"));
    }

    // Speed set while unsupplied: with the Battery disabled the speed control 12 is committed on the next step while the shaft stays at
    // rest and unsupplied. Once the Battery is enabled the shaft spins up from rest to the committed 12 rad/s within its ramp plus a tick.
    [Fact]
    public async Task ASpeedSetWhileUnsuppliedIsCommittedAndDrivesTheShaftOnceSupplied()
    {
        const float faster = 12f;
        var motor = MotorPart();
        var construction = Supplied(motor, enabled: ElectricalEnable.Disabled);
        var battery = construction.Instances[1].Id;
        var (harness, probe) = await Start(construction, motor.Id);
        await using var _ = harness;

        await SetSpeed(harness, motor.Id, faster);
        var idle = await probe.Advance(TicksPerSecond / 4);
        Assert.All(idle, tick => Assert.True(tick.Target == faster && !tick.State.Supplied && MathF.Abs(tick.Spin) < RestSpin,
            $"tick {tick.Index} before the enable: {tick}"));
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(battery, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Enabled)));
        var driven = await probe.Advance(TicksPerSecond / 2);

        Assert.All(driven.Skip(RampTicks(faster, DefaultTorque) - 1), tick => Assert.True(MathF.Abs(tick.Spin - faster) <= SpeedAllowance(faster),
            $"tick {tick.Index}: the rotor spins at {tick.Spin:F3} rad/s after the enable, target {faster} rad/s"));
    }

    // A speed change never reaches the authored construction: a save made during Run after the change holds the authored 6 rad/s, and a
    // committed Reset clears the held control and re-admits the authored target, which the next run keeps.
    [Fact]
    public async Task ASaveDuringRunAndResetKeepTheAuthoredSpeedAfterASpeedChange()
    {
        var motor = MotorPart();
        var construction = Supplied(motor);
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        await harness.Advance(30);
        await SetSpeed(harness, motor.Id, 12f);
        var changed = await harness.Advance(30);
        Assert.All(changed, read => Assert.Equal(12f, read.RunControl(motor.Id, RunControlLane.RevoluteDriveTarget, joint: read.DriveTargets[0].Joint).Scalar));

        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(harness.Construction, new(9)))).Construction;
        Assert.Equal(construction, loaded);
        Assert.Equal(DefaultSpeed, Assert.IsType<WorkshopGenericInstance>(loaded.Instances[0]).Parameters[new("speed")]);

        var row = harness.RunControl(motor.Id, RunControlLane.RevoluteDriveTarget, RunControlValue.Of(12f)).Control;
        await harness.Reset();
        Assert.Null(harness.Simulation.HeldRunControl(row));
        Assert.Equal(DefaultSpeed, harness.Committed.RunControl(motor.Id, RunControlLane.RevoluteDriveTarget, joint: harness.Committed.DriveTargets[0].Joint).Scalar);
        await harness.Run();
        Assert.All(await harness.Advance(2), read => Assert.Equal(DefaultSpeed, read.RunControl(motor.Id, RunControlLane.RevoluteDriveTarget, joint: read.DriveTargets[0].Joint).Scalar));
    }

    // Supply routed through a switched element: a Battery powers an Either gate whose output wires the Motor's PowerIn; a second Battery
    // on the gate's first input is its switch. With that input enabled the Motor is supplied and reaches ω_t; with it disabled (one cause
    // changed) the gate's output stays open, the drive unsupplied and the rotor at rest.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AMotorSuppliedThroughAnEitherGateRunsOnlyWhileTheGateIsOn(bool inputEnabled)
    {
        var motor = MotorPart();
        var supply = PlacedParts.Battery(new(2), 3, 4, 0);
        var input = PlacedParts.Battery(new(3), 6, 4, 0, enabled: inputEnabled ? ElectricalEnable.Enabled : ElectricalEnable.Disabled);
        var gate = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("electrical_or"), new(4), 9, 4, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(motor, supply, input, gate))
        {
            Connections = new(
                new WorkshopConnection(supply.Id, PortId.Supply, gate.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                new WorkshopConnection(input.Id, PortId.Supply, gate.Id, PortId.FirstIn, WorkshopConnectionDomain.Electrical),
                new WorkshopConnection(gate.Id, PortId.Supply, motor.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical))
        };
        var (harness, probe) = await Start(construction, motor.Id);
        await using var _ = harness;
        var ticks = await probe.Advance(TicksPerSecond / 2);

        if (!inputEnabled)
        {
            Assert.All(ticks, tick => Assert.True((tick.State.Supplied, tick.State.Work) == (false, 0f) && MathF.Abs(tick.Spin) < RestSpin,
                $"tick {tick.Index}: the gate is off, yet supplied {tick.State.Supplied}, work {tick.State.Work}, spin {tick.Spin}"));
            return;
        }
        Assert.All(ticks.Skip(RampTicks(DefaultSpeed, DefaultTorque) - 1), tick => Assert.True(tick.State.Supplied &&
            MathF.Abs(tick.Spin - DefaultSpeed) <= SpeedAllowance(DefaultSpeed), $"tick {tick.Index}: through the gate the rotor spins at {tick.Spin:F3} rad/s"));
        AssertEnergy(ticks, DefaultSpeed, DefaultTorque);
    }

    // ---- loads ----------------------------------------------------------------------------------------------------------------------

    /// <summary>The rotor's rim point on its local +X, where the load spans attach.</summary>
    private static readonly Vector3 Rim = new(RotorRadius, 0f, 0f);
    private const float WeightMass = 1f;
    /// <summary>The Weight's tie in its own frame at 1 kg (its declared tie binding).</summary>
    private static readonly Vector3 WeightTie = new(0f, .5771f * (.136734f * WeightMass + .453070f), 0f);
    private const float HangLength = 1.5f;
    /// <summary>The 1 kg Weight's torque about the shaft while its rope leaves the rim horizontally level with the hinge: m·g·r.</summary>
    private static float LoadTorque => WeightMass * Gravity * RotorRadius;

    /// <summary>A 1 kg Weight hanging 1.5 m below the rim point, its tie spanned to the rim. It pulls the rim down, turning the shaft
    /// clockwise (negative travel) with 2.94 N·m while the rim is level with the hinge.</summary>
    private static (WorkshopConstruction Construction, WorkshopGenericInstance Weight) Hung(WorkshopGenericInstance motor, bool supplied)
    {
        var rim = MotorAt + RotorCentre + Rim;
        var origin = rim - new Vector3(0f, HangLength, 0f) - WeightTie;
        var weight = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("weight"), new(3), origin.X, origin.Y, origin.Z, 0, 0, 0, 1,
            new([new(new("mass"), WeightMass)]));
        return (Supplied(motor, wired: supplied, other: weight), weight);
    }

    private static Func<ICompiledScene, WorkshopCompiledScene> HangSpan(WorkshopConstruction construction, GpuBodyId motor,
        GpuBodyId weight) => WithSpans(physics => [Span(physics, Rotor(construction, motor), Rim, weight, WeightTie, HangLength)]);

    // Speed 0 on an unloaded shaft (the rope-loaded brake is ASuppliedMotorAtSpeedZeroHoldsItsLoadedShaftStill): a supplied Motor at
    // speed 0 is supplied at every tick of a second, yet its rotor stays at rest and the drive does no work beyond the slack. Nothing torques the shaft (gravity acts through the hinge axis), so any spin would come from a speed
    // parameter that was not evaluated to 0 (the default 6 rad/s is reached within two ticks, with 0.4 J of work). The slack is the
    // energy slack at speed 0 (its 0.01 J floor): the drive row still solves residual joint velocities each substep.
    [Fact]
    public async Task ASuppliedMotorAtSpeedZeroStaysAtRestWithNoWork()
    {
        var motor = MotorPart(speed: 0f);
        var (harness, probe) = await Start(Supplied(motor), motor.Id);
        await using var _ = harness;
        var ticks = await probe.Advance(TicksPerSecond);

        Assert.All(ticks, tick => Assert.True(tick.State.Supplied && MathF.Abs(tick.Spin) < RestSpin && tick.State.Work <= EnergySlack(0f),
            $"tick {tick.Index}: supplied {tick.State.Supplied}, spin {tick.Spin}, work {tick.State.Work}"));
    }

    // Speed 0 holding a rim load (engine-tensile-drive-coupling): a supplied Motor at speed 0 brakes the 1 kg Weight hung on its rim rope.
    // The load torque m·g·r = 2.94 N·m lies between the bearing friction and τ_d, so the drive holds it: over 2 s the committed travel
    // stays within 0.01 rad and the work within LoadSlack at every tick, and the pose agrees with that ledger (the Weight stays within
    // r·0.01 rad = 3 mm of its height at Run). Control, the Battery unwired (one cause): the load turns the shaft past −0.5 rad within 2 s.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ASuppliedMotorAtSpeedZeroHoldsItsLoadedShaftStill(bool supplied)
    {
        Assert.True(LoadTorque > FrictionTorque && LoadTorque < DefaultTorque);
        var motor = MotorPart(speed: 0f);
        var (construction, weight) = Hung(motor, supplied);
        var (harness, probe) = await Start(construction, motor.Id, fixture: HangSpan(construction, motor.Id, weight.Id));
        await using var _ = harness;
        var start = WeightState(harness.Committed, weight.Id).Position.Y;
        var drop = 0f;
        for (var i = 0; i < 2 * TicksPerSecond; i++)
        {
            await probe.Advance(1);
            drop = MathF.Max(drop, MathF.Abs(WeightState(harness.Committed, weight.Id).Position.Y - start));
        }
        var ticks = probe.Ticks;

        if (!supplied)
        {
            Assert.True(ticks.Any(tick => tick.State.Travel < -.5f),
                $"Unbraked, the load turned the shaft only to {ticks.Min(tick => tick.State.Travel):F3} rad");
            return;
        }
        Assert.All(ticks, tick => Assert.True(MathF.Abs(tick.State.Travel) <= .01f && tick.State.Work <= LoadSlack,
            $"tick {tick.Index}: the braked shaft is at {tick.State.Travel:F4} rad with work {tick.State.Work:E2} J"));
        Assert.True(drop <= RotorRadius * .01f, $"The braked load moved {drop * 1000f:F3} mm from its height at Run");
    }

    // Lift (engine-tensile-drive-coupling, review pass 1): a supplied Motor that wins raises its rope-hung 1 kg Weight. Speed 1 rad/s,
    // default torque 20 N·m against the 2.94 N·m load. At this speed the rim stays near level for the half second observed (it turns
    // about 0.5 rad), so the rope stays near vertical and the load follows the rim's height. Every bound is derived from the fixture:
    // - Ramp: the coupled inertia I + m·r² = 0.1125 kg·m² reaches ω_t under τ_d − τ_f − m·g·r = 17 N·m within one tick. The rope is
    //   the shared soft law (60 Hz, ζ = 10) on the row's inverse mass. That mass sums both ends and does not see the hinge: the
    //   Weight's 1/m, the rotor's 1/m_rotor and its r²/I, 1 + 2 + 4 = 7 kg⁻¹. So k = ω²/7 and c = 20·ω/7, and the stretch transient
    //   decays with c/k = 20/ω = 53 ms, whatever the mass. The spin and the rise are checked from the ramp plus three of those time
    //   constants.
    // - Spin: the target within the existing 2 % allowance.
    // - Rise, at every settled tick: the Weight rises by the rim's rise r·sin θ, θ that tick's committed pose turn, within the steady
    //   rope stretch m·g·7/ω² = 0.48 mm, plus the swing the rim's sideways shift Δx = r·(1 − cos θ) can give the 1.5 m pendulum (at most 2Δx/L, lowering the load by
    //   (L + tie)·φ²/2 with the tie 0.34 m above the Weight's origin; 2.2 mm at θ = 0.5).
    // - Energy at every tick: rotor kinetic energy plus the load's kinetic energy and potential energy gain is at most the work plus
    //   LoadSlack, as in the overload case.
    // - The ledger does not over-charge once settled: the work over the settled window is at most that window's energy gain plus the
    //   bearing loss τ_f·|Δtravel| plus LoadSlack. The spin-up's one-off ledger over-count (½·τ_d·h·ω_t) and the rope's dissipation
    //   while it yanks the load to rim speed are both before the window. The rope's elastic take-up is left out of the ledger and of
    //   the gain alike.
    [Fact]
    public async Task ASuppliedMotorThatWinsLiftsItsRopeHungLoadAndChargesOnlyWhatItLifts()
    {
        const float speed = 1f;
        const float ropeHz = 60f;
        var ropeOmega = 2f * MathF.PI * ropeHz;
        var rowInverseMass = 1f / WeightMass + 1f / RotorMass + RotorRadius * RotorRadius / RotorInertia;
        var coupled = RotorInertia + WeightMass * RotorRadius * RotorRadius;
        var ramp = coupled * speed / (DefaultTorque - FrictionTorque - LoadTorque);
        var settled = (int)MathF.Ceiling((ramp + 3f * 20f / ropeOmega) * TicksPerSecond);
        var motor = MotorPart(speed: speed);
        var (construction, weight) = Hung(motor, supplied: true);
        var (harness, probe) = await Start(construction, motor.Id, fixture: HangSpan(construction, motor.Id, weight.Id));
        await using var _ = harness;
        var start = WeightState(harness.Committed, weight.Id).Position.Y;
        var gains = new List<float>();
        var heights = new List<float>();
        for (var i = 0; i < TicksPerSecond / 2; i++)
        {
            var tick = (await probe.Advance(1))[0];
            var load = WeightState(harness.Committed, weight.Id);
            heights.Add(load.Position.Y - start);
            gains.Add(tick.Kinetic + .5f * WeightMass * load.Velocity.LengthSquared() + WeightMass * Gravity * (load.Position.Y - start));
        }
        var ticks = probe.Ticks;

        Assert.All(ticks, tick => Assert.True(tick.State.Supplied, $"tick {tick.Index}: the lifting drive is unsupplied"));
        Assert.All(ticks.Skip(settled - 1), tick => Assert.True(MathF.Abs(tick.Spin - speed) <= SpeedAllowance(speed),
            $"tick {tick.Index}: lifting, the rotor spins at {tick.Spin:F4} rad/s, target {speed} rad/s"));

        var stretch = WeightMass * Gravity * rowInverseMass / (ropeOmega * ropeOmega);
        Assert.All(ticks.Zip(heights).Skip(settled - 1), pair =>
        {
            var turned = PoseTurn(probe.Initial, pair.First, Vector3.UnitZ);
            var rimRise = RotorRadius * MathF.Sin(turned);
            var swing = 2f * RotorRadius * (1f - MathF.Cos(turned)) / HangLength;
            var allowance = stretch + (HangLength + WeightTie.Y) * swing * swing / 2f;
            Assert.True(MathF.Abs(pair.Second - rimRise) <= allowance,
                $"tick {pair.First.Index}: after {turned:F4} rad the load rose {pair.Second * 1000f:F3} mm; the rim rose {rimRise * 1000f:F3} mm (allowance {allowance * 1000f:F3} mm)");
        });

        Assert.All(ticks.Zip(gains), pair => Assert.True(pair.Second <= pair.First.State.Work + LoadSlack,
            $"tick {pair.First.Index}: rotor and load gained {pair.Second:F4} J against {pair.First.State.Work:F4} J of work"));
        var from = settled - 1;
        var charged = ticks[^1].State.Work - ticks[from].State.Work;
        var lifted = gains[^1] - gains[from];
        var bearing = FrictionTorque * MathF.Abs(ticks[^1].State.Travel - ticks[from].State.Travel);
        Assert.True(charged <= lifted + bearing + LoadSlack,
            $"Settled, the drive charged {charged:F4} J for {lifted:F4} J of lift and {bearing:F4} J of bearing loss");
    }

    // Overload: a Motor with torque 1 N·m against the 2.94 N·m rim load is back-driven clockwise past −0.5 rad. The drive is supplied
    // at every tick and resists: the same load on the Motor unwired turns the shaft past −0.5 rad sooner. Through that descent the drive
    // only resists, so its work stays within LoadSlack; it never decreases at any tick; and the rotor's and Weight's mechanical energy
    // above their start never exceeds the work plus LoadSlack. The load hangs on the same rim rope as the held cases; here the load
    // wins and the rope is never asked to hold the drive at rest.
    [Fact]
    public async Task AnOverloadedMotorIsBackDrivenAndAddsNoWorkWhileItIs()
    {
        const float torque = 1f;
        Assert.True(LoadTorque > torque + FrictionTorque); // the load overcomes the drive and the bearing
        var free = MotorPart(torque: torque);
        var (unwired, freeWeight) = Hung(free, supplied: false);
        var (freeHarness, freeProbe) = await Start(unwired, free.Id, fixture: HangSpan(unwired, free.Id, freeWeight.Id));
        await using (freeHarness)
            await freeProbe.Advance(2 * TicksPerSecond);
        var freeDescent = freeProbe.Ticks.FindIndex(tick => tick.State.Travel < -.5f);
        var motor = MotorPart(torque: torque);
        var (construction, weight) = Hung(motor, supplied: true);
        var (harness, probe) = await Start(construction, motor.Id, fixture: HangSpan(construction, motor.Id, weight.Id));
        await using var _ = harness;
        var start = WeightState(harness.Committed, weight.Id);
        var gains = new List<float>();
        for (var i = 0; i < 2 * TicksPerSecond; i++)
        {
            var tick = (await probe.Advance(1))[0];
            var load = WeightState(harness.Committed, weight.Id);
            gains.Add(tick.Kinetic + .5f * WeightMass * load.Velocity.LengthSquared() + WeightMass * Gravity * (load.Position.Y - start.Position.Y));
        }
        var ticks = probe.Ticks;

        var descended = ticks.FindIndex(tick => tick.State.Travel < -.5f);
        Assert.True(descended >= 0, $"The overloaded shaft reached only {ticks.Min(tick => tick.State.Travel):F3} rad");
        Assert.All(ticks, tick => Assert.True(tick.State.Supplied, $"tick {tick.Index}: the overloaded drive is unsupplied"));
        Assert.True(freeDescent >= 0 && freeDescent < descended,
            $"The unwired shaft passed −0.5 rad at tick {freeDescent + 1}, the resisting drive's at tick {descended + 1}");
        Assert.True(ticks[descended].State.Work <= LoadSlack, $"Back-driven to {ticks[descended].State.Travel:F3} rad, the drive did {ticks[descended].State.Work:E2} J");
        Assert.All(ticks.Zip(ticks.Skip(1)), pair => Assert.True(pair.Second.State.Work >= pair.First.State.Work,
            $"tick {pair.Second.Index}: work fell from {pair.First.State.Work} to {pair.Second.State.Work} J"));
        Assert.All(ticks.Zip(gains), pair => Assert.True(pair.Second <= pair.First.State.Work + LoadSlack,
            $"tick {pair.First.Index}: the load gained {pair.Second:F4} J against {pair.First.State.Work:F4} J of work"));
    }

    private static (Vector3 Position, Vector3 Velocity) WeightState(WorkshopRead read, GpuBodyId weight)
    {
        Assert.True(read.Bodies.TryGet(weight, out var body));
        return (new((body.Body.Cell.X + body.Body.Local.X) / 16f, (body.Body.Cell.Y + body.Body.Local.Y) / 16f,
            (body.Body.Cell.Z + body.Body.Local.Z) / 16f), new(body.Body.Velocity.X, body.Body.Velocity.Y, body.Body.Velocity.Z));
    }

    // Stall against a rim rope (CAT-042 harvest row 12; engine-tensile-drive-coupling): the rotor's rim point is tied by a 1.001 m span
    // to the static housing point 1 m below it, and the drive at its defaults pulls the rim up into the rope. Taking the ticks from
    // 0.25 s on as held, the shaft turns at most 0.01 rad over the held second and its travel stays below 0.02 rad at every held tick,
    // and the work grows over the held second by at most τ_d·|turn| + slack. 120 J (τ_d·ω_t·1 s) is only the drive's power bound; a
    // free rotor at the defaults pays 0.405 J of spin-up plus 0.3 W of bearing loss, so the slack is what separates a held shaft from a
    // turning one. The energy oracle holds at every tick.
    //
    // Guards (review pass 1): the drive is supplied at every tick, and the rim really took up the rope's 1 mm of slack, so a drive that
    // never pulled (unsupplied, or turning away from the rope) fails. Taut, the rim has turned the slack over the radius, 1 mm / 0.3 m =
    // 3.33 mrad (the chord's second-order term is below 1e-8 rad); each held tick's committed pose is past 90 % of that. The rope pulls
    // only at or beyond its length, so a held rotor cannot sit short of it. The pose, not only the ledger, is bounded: the committed
    // rotor turns at most the same 0.01 rad about its axis over the held second. The ledger samples the velocity after relax, which a
    // rigid rope row has projected to rest, so only the pose shows a creep in the solve pass.
    [Fact]
    public async Task AStalledMotorDoesNoWorkOnceItsShaftIsHeld()
    {
        const float slack = .001f;
        var motor = MotorPart();
        var construction = Supplied(motor);
        var housing = new Vector3(RotorRadius, -1f, RotorCentre.Z);
        var (harness, probe) = await Start(construction, motor.Id, fixture: WithSpans(physics =>
            [Span(physics, Rotor(construction, motor.Id), Rim, motor.Id, housing, 1f + slack)]));
        await using var _ = harness;
        var ticks = await probe.Advance(TicksPerSecond * 5 / 4);
        var held = ticks.Skip(TicksPerSecond / 4 - 1).ToList();

        Assert.All(ticks, tick => Assert.True(tick.State.Supplied, $"tick {tick.Index}: the stalled drive is unsupplied"));
        Assert.All(held, tick => Assert.True(PoseTurn(probe.Initial, tick, Vector3.UnitZ) >= .9f * slack / RotorRadius,
            $"tick {tick.Index}: the rim has turned only {PoseTurn(probe.Initial, tick, Vector3.UnitZ):E3} rad, short of the rope's slack"));
        var poseTurn = PoseTurn(held[0], held[^1], Vector3.UnitZ);
        Assert.True(MathF.Abs(poseTurn) <= .01f, $"The tied rotor's pose turned {poseTurn:F4} rad while held");

        var turn = held[^1].State.Travel - held[0].State.Travel;
        Assert.True(MathF.Abs(turn) <= .01f, $"The tied shaft turned {turn:F4} rad while held (travel {ticks[^1].State.Travel:F4} rad)");
        Assert.All(held, tick => Assert.True(MathF.Abs(tick.State.Travel) < .02f,
            $"tick {tick.Index}: the held shaft is at {tick.State.Travel:F4} rad"));
        var paid = held[^1].State.Work - held[0].State.Work;
        Assert.True(paid <= DefaultTorque * MathF.Abs(turn) + EnergySlack(DefaultSpeed),
            $"Over the held second the stalled drive did {paid:F4} J for a {turn:F4} rad turn");
        AssertEnergy(ticks, DefaultSpeed, DefaultTorque);
    }

    // Stall, as a locked shaft (CAT-042 harvest row 12; the rope-tied stall is AStalledMotorDoesNoWorkOnceItsShaftIsHeld): a second,
    // Limited hinge appended between the rotor and the static Battery turns about X
    // through the rotor centre, so its angular rows lock the rotor's spin about Z. The supplied drive saturates against the lock and the
    // shaft does not turn: over 1 s its travel stays within 0.01 rad and its work within the slack. A ledger counting τ_d·ω_t instead of
    // λ·ω_rel would show up to 120 J (the power bound over 1 s); a free rotor at the defaults pays only 0.405 J of spin-up plus 0.3 W of
    // bearing loss, so even a free-spinning ledger far exceeds the slack.
    [Fact]
    public async Task ALockedMotorShaftDoesNoWork()
    {
        var motor = MotorPart();
        var construction = Supplied(motor);
        var battery = construction.Instances[1].Id;
        var rotor = Rotor(construction, motor.Id);
        var turn = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f);
        var batteryAt = new Vector3(3f, 4f, 0f);
        var (harness, probe) = await Start(construction, motor.Id, fixture: compiled => Rebuilt(compiled, [], [new RevoluteConstraintDeclaration(
            new(compiled.Physics.NextIdentity), battery, rotor, new(MotorAt + RotorCentre - batteryAt, turn), new(Vector3.Zero, turn), -.5f, .5f,
            RevoluteTravel.Limited, 0f, 0f, 0f, ConnectedCollision.Disabled)], []));
        await using var _ = harness;
        var ticks = await probe.Advance(TicksPerSecond);

        Assert.All(ticks, tick => Assert.True(tick.State.Supplied && MathF.Abs(tick.State.Travel) <= .01f && tick.State.Work <= EnergySlack(DefaultSpeed),
            $"tick {tick.Index}: supplied {tick.State.Supplied}, travel {tick.State.Travel:E2} rad, work {tick.State.Work:E2} J"));
    }

    // ---- mount, lifecycle and saves -------------------------------------------------------------------------------------------------

    // A Motor turned 90° about X spins about its turned axis (R·Z = −Y) at the same ω_t, with no visible spin about any other axis.
    [Fact]
    public async Task ARotatedMotorSpinsAboutItsTurnedAxisAtTheSameSpeed()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f);
        var motor = MotorPart(rotation: rotation);
        var (harness, probe) = await Start(Supplied(motor), motor.Id, rotation);
        await using var _ = harness;
        var ticks = await probe.Advance(TicksPerSecond / 2);
        var axis = Vector3.Transform(Vector3.UnitZ, rotation);

        Assert.True(axis.Y < -.999f);
        Assert.All(ticks.Skip(RampTicks(DefaultSpeed, DefaultTorque) - 1), tick =>
            Assert.True(MathF.Abs(tick.Spin - DefaultSpeed) <= SpeedAllowance(DefaultSpeed) &&
                (tick.Angular - tick.Spin * axis).Length() <= SpeedAllowance(DefaultSpeed),
                $"tick {tick.Index}: spin {tick.Angular} about the turned axis {axis}"));
        AssertEnergy(ticks, DefaultSpeed, DefaultTorque);
    }

    // Reset after driven ticks restores the authored world exactly: the rotor's body read equals the built one, and the committed drive
    // reads unsupplied with zero travel and work; the lamp returns to slate.
    [Fact]
    public async Task ResetRestoresTheAuthoredRotorAndZeroesTheDriveStateAndLamp()
    {
        var motor = MotorPart();
        var construction = Supplied(motor);
        var rotor = Rotor(construction, motor.Id);
        await using var harness = await WorkshopIntegrationHarness.Building(construction);
        Assert.True(harness.Committed.Bodies.TryGet(rotor, out var built));
        await harness.Run();
        var probe = new Probe(harness, motor.Id, rotor, Vector3.UnitZ);
        var driven = await probe.Advance(TicksPerSecond / 2);
        Assert.True(driven[^1].State.Work > 0f && MathF.Abs(driven[^1].Spin - DefaultSpeed) <= SpeedAllowance(DefaultSpeed));
        AssertColour(Gold, driven[^1].Lamp, "while driven");

        await harness.Reset();

        Assert.True(harness.Committed.Bodies.TryGet(rotor, out var reset));
        // Reset installs a new world epoch; everything else the read carries is the authored rotor's.
        Assert.Equal(built with { Body = built.Body with { Epoch = reset.Body.Epoch } }, reset);
        Assert.Equal(new RevoluteStateRead(false, 0f, 0f), PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.ToArray(), 0));
        AssertColour(Slate, probe.LampAfter(.2), "after Reset");
    }

    // Save/Load: a Motor at speed 12 and torque 40, yawed 30°, wired from a Battery round-trips its parameters, pose and typed
    // connection through the save codec, and the loaded Motor reaches its saved speed.
    [Fact]
    public async Task ASavedMotorLoadsWithItsSpeedTorquePoseAndWireAndSpinsAtItsSavedSpeed()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 6f);
        var motor = MotorPart(speed: 12f, torque: 40f, rotation: rotation);
        var construction = Supplied(motor);

        var decoded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(construction, new(10))));

        Assert.Equal(construction.Instances, decoded.Construction.Instances);
        Assert.Equal(construction.Connections, decoded.Construction.Connections);
        var loaded = Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances[0]);
        Assert.Equal((12f, 40f), (loaded.Parameters[new("speed")], loaded.Parameters[new("torque")]));
        var (harness, probe) = await Start(decoded.Construction, motor.Id, rotation);
        await using var _ = harness;
        var ticks = await probe.Advance(TicksPerSecond / 2);
        Assert.True(MathF.Abs(ticks[^1].Spin - 12f) <= SpeedAllowance(12f), $"The loaded Motor spins at {ticks[^1].Spin:F3} rad/s");
    }

    // ---- the committed drive read ---------------------------------------------------------------------------------------------------

    // The committed actuator read has one row per drive slot, keyed by its typed consumer: the Powered Gate's prismatic slot first, then
    // the Motor's revolute slot, although the Motor is placed first (its hinge's identity is the lower). Each row's supply is its own
    // wire's: one Battery wires only the part under test, the other part's PowerIn stays open. The Motor's row reads no moving contact,
    // its committed travel as its coordinate, its relative spin about its turned axis (a 90° turn about X) as its speed, and its ±π
    // limits. Every read here crossed the response wire, so the kind round-trips too.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EachDriveSlotReadsItsOwnSupplyWithPrismaticSlotsBeforeRevoluteSlots(bool motorWired)
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f);
        var motor = MotorPart(rotation: rotation);
        var gate = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("powered_gate"), new(3), -4, 4, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(2), 3, 4, 0);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(motor, battery, gate))
            { Connections = new(Power(battery.Id, motorWired ? motor.Id : gate.Id)) };
        var hinge = construction.Instances.Identities[motor.Id].Hinges[0].Joint;
        var slider = construction.Instances.Identities[gate.Id].Sliders[0].Joint;
        Assert.True(hinge.Value < slider.Value, "The Motor's hinge identity is the lower");
        var (harness, probe) = await Start(construction, motor.Id, rotation);
        await using var _ = harness;
        var tick = (await probe.Advance(TicksPerSecond / 4))[^1];

        var rows = harness.Committed.Presentation.Actuators.ToArray();
        Assert.Equal([SupplyConsumer.Prismatic(slider), SupplyConsumer.Revolute(hinge)], rows.Select(row => row.Drive));
        Assert.Equal((gate.Id, !motorWired), (rows[0].Owner, rows[0].SupplyClosed));
        Assert.Equal((motor.Id, motorWired, false, -MathF.PI, MathF.PI), (rows[1].Owner, rows[1].SupplyClosed, rows[1].MovingContact, rows[1].Lower,
            rows[1].Upper));
        Assert.Equal(tick.State.Travel, rows[1].Coordinate);
        Assert.True(MathF.Abs(rows[1].Speed - tick.Spin) <= SpeedAllowance(DefaultSpeed) && (!motorWired || MathF.Abs(rows[1].Speed - DefaultSpeed) <= SpeedAllowance(DefaultSpeed)),
            $"The Motor's row reads {rows[1].Speed} rad/s; the rotor spins at {tick.Spin} rad/s about its turned axis");
    }

    // A scene mixing an undriven hinge (the Impact Lever), the Motor and a Powered Gate, with only the Motor wired, plus an undriven
    // slider appended to the compiled scene whose base is the Motor's housing and whose moving body is a separate Weight. Every slot has
    // a row: the prismatic slots first (the gate's, then the appended one), then the two revolute slots by identity. The lever's row names
    // its own undriven hinge as a revolute slot and is never supplied. Each lamp reads its own owner's driven slot: the Motor owns an
    // undriven prismatic row that sorts before its driven revolute row, so a lamp reading the owner's first row would stay slate; the
    // Motor's lamp is gold. The unsupplied gate's indicator stays slate.
    [Fact]
    public async Task InAMixedSceneEachLampReadsItsOwnDrivenSlotAndAnUndrivenHingeHasItsOwnRow()
    {
        var motor = MotorPart();
        var gate = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("powered_gate"), new(3), -4, 4, 0, 0, 0, 0, 1);
        var lever = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("impact_lever"), new(4), -10, 4, 0, 0, 0, 0, 1);
        var weight = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("weight"), new(5), -3, 4, 4, 0, 0, 0, 1, new([new(new("mass"), 1f)]));
        var battery = PlacedParts.Battery(new(2), 3, 4, 0);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(motor, battery, gate, lever, weight))
            { Connections = new(Power(battery.Id, motor.Id)) };
        var identities = construction.Instances.Identities;
        var motorHinge = identities[motor.Id].Hinges[0].Joint;
        var leverHinge = identities[lever.Id].Hinges[0].Joint;
        PrismaticConstraintId appended = default;
        var (harness, probe) = await Start(construction, motor.Id, fixture: compiled =>
        {
            appended = new(compiled.Physics.NextIdentity);
            // The Weight slides along the housing's +Y between ±1 m from where it is placed, on the Powered Gate's 14 N/m, 4 N·s/m
            // spring (a slider requires one); nothing drives the slot.
            return Rebuilt(compiled, [], [], [new PrismaticConstraintDeclaration(appended, motor.Id, weight.Id,
                new(new Vector3(-3f, 0f, 4f), Quaternion.Identity), new(Vector3.Zero, Quaternion.Identity), -1f, 1f, 14f, 4f,
                ConnectedCollision.Disabled)]);
        });
        await using var _ = harness;
        var tick = (await probe.Advance(TicksPerSecond / 4))[^1];

        var rows = harness.Committed.Presentation.Actuators.ToArray();
        SupplyConsumer[] revolutes = motorHinge.Value < leverHinge.Value
            ? [SupplyConsumer.Revolute(motorHinge), SupplyConsumer.Revolute(leverHinge)]
            : [SupplyConsumer.Revolute(leverHinge), SupplyConsumer.Revolute(motorHinge)];
        Assert.Equal([SupplyConsumer.Prismatic(identities[gate.Id].Sliders[0].Joint), SupplyConsumer.Prismatic(appended), .. revolutes],
            rows.Select(row => row.Drive));
        Assert.Equal((motor.Id, false), (rows[1].Owner, rows[1].SupplyClosed));
        var leverRow = Assert.Single(rows, row => row.Drive == SupplyConsumer.Revolute(leverHinge));
        Assert.Equal((lever.Id, false, false), (leverRow.Owner, leverRow.SupplyClosed, leverRow.MovingContact));
        Assert.True(Assert.Single(rows, row => row.Drive == SupplyConsumer.Revolute(motorHinge)).SupplyClosed);
        AssertColour(Gold, tick.Lamp, "the Motor's lamp");
        Assert.True(MathF.Abs(probe.ChannelOf(gate.Id, 0) - Slate.X) <= ColourStep, "the unsupplied gate's indicator is slate");
    }

    // The read refuses a revolute row that claims moving contact, and slots out of order: a revolute slot before a prismatic one (whatever
    // their identities) or the same slot twice.
    [Fact]
    public void TheDriveReadRefusesARevoluteContactAndOutOfOrderSlots()
    {
        var prismatic = new ActuatorDriveRead(SupplyConsumer.Prismatic(new(9)), new(1), false, false, 0f, 0f, 0f, 1.55f);
        var revolute = new ActuatorDriveRead(SupplyConsumer.Revolute(new(4)), new(2), true, false, 1f, 6f, -MathF.PI, MathF.PI);
        _ = new PhysicsPresentationRead([prismatic, revolute], []);

        Assert.Throws<ArgumentException>(() => new PhysicsPresentationRead([prismatic, revolute with { MovingContact = true }], []));
        Assert.Throws<ArgumentException>(() => new PhysicsPresentationRead([revolute, prismatic], []));
        Assert.Throws<ArgumentException>(() => new PhysicsPresentationRead([revolute, revolute], []));
    }

    // A response whose drive row names an undefined consumer kind is refused at the response decode boundary.
    [Fact]
    public async Task AResponseWithAnUndefinedDriveKindIsRefusedAtDecode()
    {
        var motor = MotorPart();
        var (harness, probe) = await Start(Supplied(motor), motor.Id);
        await using var _ = harness;
        await probe.Advance(1);
        var bytes = WorkshopWireFlatBuffers.EncodeResponse(harness.Latest);
        var row = Assert.Single(harness.Committed.Presentation.Actuators.ToArray());
        var pattern = new byte[20];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(pattern, row.Slot.Joint);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(pattern.AsSpan(8), row.Owner.Value);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(pattern.AsSpan(16), (uint)SupplyConsumerKind.RevoluteDrive);
        var at = bytes.AsSpan().IndexOf(pattern);
        Assert.True(at >= 0 && bytes.AsSpan(at + 1).IndexOf(pattern) < 0, "The drive record is found exactly once");
        Assert.Equal(row, Assert.Single(WorkshopWireFlatBuffers.DecodeResponse(bytes).Read.Presentation.Actuators.ToArray()));

        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(at + 16), 2);

        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(bytes)).Reason);
    }

    // ---- compiler hookup and admission ----------------------------------------------------------------------------------------------

    // The package compiles through the generic hinge path at non-default parameters: one Continuous revolute with limits [−π, π], its
    // drive evaluated from the parameters (20 rad/s, 100 N·m), its declared 0.05 N·m friction, and the Motor's own consumer node naming
    // exactly that drive as a typed revolute consumer.
    [Fact]
    public void TheMotorCompilesAContinuousDrivenHingeConsumedByItsOwnPowerInput()
    {
        var motor = MotorPart(speed: 20f, torque: 100f);
        var construction = Supplied(motor);
        var scene = CompiledSceneFixture.Compile(construction).Physics;

        var hinge = Assert.Single(scene.Revolutes.ToArray());
        Assert.Equal((motor.Id, Rotor(construction, motor.Id)), (hinge.BodyA, hinge.BodyB));
        Assert.Equal((RevoluteTravel.Continuous, -MathF.PI, MathF.PI, FrictionTorque, 0f), (hinge.Travel, hinge.Lower, hinge.Upper,
            hinge.FrictionTorque, hinge.Stiffness));
        Assert.Equal([new RevoluteDriveDeclaration(hinge.Id, 20f, 100f)], scene.RevoluteDrives.ToArray());
        var consumer = Assert.Single(scene.SupplyGraph.Nodes.ToArray(), node => node.Owner == motor.Id);
        Assert.Equal(SupplyNodeKind.Consumer, consumer.Kind);
        Assert.Equal([SupplyConsumer.Revolute(hinge.Id)], consumer.Consumers.ToArray());
    }

    // The bundle validator refuses the Motor's driven hinge when the package declares no electrical PowerIn input to supply it.
    [Fact]
    public void TheMotorWithoutItsPowerInputIsRefusedByTheBundleValidator()
    {
        var entry = TestBundle.Entry(Motor);
        Assert.Empty(BundleValidator.Validate([new("motor", entry)]));
        var unpowered = entry with { Ports = entry.Ports.Where(port => port.Id != PortId.PowerIn).ToArray() };

        var error = Assert.Single(BundleValidator.Validate([new("motor", unpowered)]));
        Assert.Equal(BundleErrorCode.OutOfRange, error.Code);
        Assert.Contains("SupplyGraph.Port.Reference", error.Detail);
    }

    public static TheoryData<string, float, WorkshopSaveRejection> RefusedParameters() => new()
    {
        { "speed", MathF.BitDecrement(0f), WorkshopSaveRejection.OutOfRange }, { "speed", MathF.BitIncrement(20f), WorkshopSaveRejection.OutOfRange },
        { "speed", float.NaN, WorkshopSaveRejection.NonFinite }, { "speed", float.PositiveInfinity, WorkshopSaveRejection.NonFinite },
        { "speed", float.NegativeInfinity, WorkshopSaveRejection.NonFinite },
        { "torque", MathF.BitDecrement(.1f), WorkshopSaveRejection.OutOfRange }, { "torque", MathF.BitIncrement(100f), WorkshopSaveRejection.OutOfRange },
        { "torque", float.NaN, WorkshopSaveRejection.NonFinite }, { "torque", float.PositiveInfinity, WorkshopSaveRejection.NonFinite },
        { "torque", float.NegativeInfinity, WorkshopSaveRejection.NonFinite },
    };

    // The declared bounds are admitted inclusively (speed 0 and 20, torque 0.1 and 100).
    [Theory]
    [InlineData(0f, .1f)]
    [InlineData(20f, 100f)]
    public void TheDeclaredParameterBoundsAreAdmitted(float speed, float torque)
    {
        var motor = MotorPart(speed: speed, torque: torque);
        Assert.Equal((speed, torque), (motor.Parameters[new("speed")], motor.Parameters[new("torque")]));
        Supplied(motor).Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle);
    }

    // An out-of-range or non-finite speed or torque is refused atomically at every seam: the parameter collection, placement, an edit of
    // an admitted construction and the save decode. Nothing is clamped or defaulted, and the admitted construction is unchanged.
    [Theory]
    [MemberData(nameof(RefusedParameters))]
    public void AnOutOfRangeOrNonFiniteParameterIsRefusedAtEverySeamAndChangesNothing(string key, float value, WorkshopSaveRejection save)
    {
        var speed = key == "speed" ? value : DefaultSpeed; var torque = key == "torque" ? value : DefaultTorque;
        var refused = new InstanceParameterCollection([new(new("speed"), speed), new(new("torque"), torque)]);
        Assert.Throws<ArgumentException>(() => refused.Validate(Motor));
        Assert.Throws<ArgumentException>(() => MotorPart(speed: speed, torque: torque));
        var admitted = Supplied(MotorPart());
        var current = Assert.IsType<WorkshopGenericInstance>(admitted.Instances[0]);
        Assert.ThrowsAny<ArgumentException>(() => admitted.WithInstance(RuntimeTestServices.Bundle, current with { Parameters = refused }).Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle));
        Assert.Equal(DefaultSpeed, Assert.IsType<WorkshopGenericInstance>(admitted.Instances[0]).Parameters[new("speed")]);
        var bytes = PayloadInstanceTests.GenericSave("motor", [("speed", speed), ("torque", torque)]);
        SaveFixture.Refused(save, () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, bytes));
    }
}

/// <summary>Story 11.1a (CAT-042 harvest row 22): the Motor's rotor art (<c>Visual/Rotor</c>, with its flush index) is the presented
/// visual of the compiled rotor body. The production pose application places it at the committed rotor pose while it spins and while a
/// speed-0 Motor holds it, and Reset returns it to its authored scene pose.</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class MotorRotorArtTests(NativeSceneFixture godot)
{
    /// <summary>Drawn-pose agreement: 0.1 mm and 1 mrad are far below a visible offset of a 0.6 m wheel, and far above the f32 rounding
    /// of a pose composed at 4 m from the origin.</summary>
    private const float PositionTolerance = 1e-4f;
    private const float AngleTolerance = 1e-3f;
    /// <summary>The angular rest envelope (rad/s), as in <see cref="MotorTests"/>: slower shaft motion reads as still.</summary>
    private const float MotorRestSpin = .02f;

    [Theory]
    [InlineData(6f)]
    [InlineData(0f)]
    public async Task TheRotorArtFollowsTheCommittedRotorPoseAndResetsToItsScenePose(float speed)
    {
        var motor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("motor"), new(1), 0, 4, 0, 0, 0, 0, 1,
            new([new(new("speed"), speed), new(new("torque"), 20f)]));
        var battery = PlacedParts.Battery(new(2), 3, 4, 0);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(motor, battery))
        {
            Connections = new(new WorkshopConnection(battery.Id, PortId.Supply, motor.Id, PortId.PowerIn,
                WorkshopConnectionDomain.Electrical))
        };
        var rotor = construction.Instances.Identities[motor.Id].Hinges[0].Body;
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle); registry.Load();
        var root = new Godot.Node3D();
        var part = Assert.IsType<MachinePart>(registry.Create(new PartId("motor")));
        try
        {
            godot.Tree.Root.AddChild(root); root.AddChild(part); part.EnsureConstructed();
            part.Position = new Godot.Vector3(0, 4, 0);
            part.AuthoredId = motor.Id;
            part.InstallPhysicalIdentity(new PackageBodyIndex(1), rotor, construction.Instances.Identities);
            var art = Assert.IsAssignableFrom<Godot.Node3D>(part.PhysicalVisual(rotor));
            Assert.Same(part.GetNode<Godot.Node3D>("Visual/Rotor"), art);
            var authored = art.Transform;
            var presentation = new WorkshopPhysicalPresentation();
            await using var harness = await WorkshopIntegrationHarness.Running(construction);
            const int ticks = 30;
            await harness.Advance(ticks);

            Assert.True(harness.Committed.Bodies.TryGet(rotor, out var body));
            var q = body.Rotation;
            presentation.Apply([part], root.GlobalTransform, new PresentedBodySet([new PresentedBody(rotor, body.Body.Cell, body.Body.Local, q)]));
            var committed = new Godot.Vector3((body.Body.Cell.X + body.Body.Local.X) / 16f, (body.Body.Cell.Y + body.Body.Local.Y) / 16f,
                (body.Body.Cell.Z + body.Body.Local.Z) / 16f);
            Assert.True(art.GlobalPosition.DistanceTo(committed) <= PositionTolerance, $"The rotor art is at {art.GlobalPosition}; the body at {committed}");
            var drawn = art.GlobalTransform.Basis.GetRotationQuaternion();
            var angle = drawn.AngleTo(new Godot.Quaternion(q.X, q.Y, q.Z, q.W));
            Assert.True(angle <= AngleTolerance, $"The rotor art is turned {angle} rad from the committed rotor");
            // While spinning the committed rotor has turned (0.25 s at 6 rad/s is about 1.5 rad); at speed 0 it is held at its scene pose,
            // turned no further than a shaft at the angular rest envelope (0.02 rad/s) moves in the same 0.25 s.
            var turned = authored.Basis.GetRotationQuaternion().AngleTo(art.Transform.Basis.GetRotationQuaternion());
            Assert.True(speed == 0f ? turned <= MotorRestSpin * ticks / 120f : turned > .5f, $"At speed {speed} the rotor art turned {turned} rad");

            await harness.Reset();
            part.ResetPhysicalVisuals();
            Assert.Equal(authored, art.Transform);
            Assert.True(harness.Committed.Bodies.TryGet(rotor, out var reset));
            presentation.Apply([part], root.GlobalTransform, new PresentedBodySet([new PresentedBody(rotor, reset.Body.Cell, reset.Body.Local, reset.Rotation)]));
            Assert.True(art.Transform.Origin.DistanceTo(authored.Origin) <= PositionTolerance &&
                art.Transform.Basis.GetRotationQuaternion().AngleTo(authored.Basis.GetRotationQuaternion()) <= AngleTolerance,
                $"After Reset the rotor art is at {art.Transform}; its scene pose is {authored}");
            Assert.Empty(godot.Errors.Drain());
        }
        finally { root.Free(); }
    }
}
