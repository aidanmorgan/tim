using System.Buffers.Binary;
using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>
/// engine-revolute-drive: the generic revolute hinge's declared <see cref="RevoluteTravel"/> and its supplied velocity drive (target
/// relative speed ω_t, maximum torque τ_d), proved on unframed engine-direct hinges (no element package) run through the production
/// <see cref="WorkshopGpuDevice"/>, so every committed tick also passes candidate admission. Supply reaches each drive through the
/// compiled supply graph: a source on a separate static body, wired through PowerIn to a consumer node on the hinge's base.
///
/// The rotor is a 0.5 kg sphere of radius 0.3 m centred on the hinge (the CAT-042 rotor's mass and radius). Its axial inertia is
/// I = (2/5)·m·r² = 0.018 kg·m², and gravity exerts no torque about its centre. With ω_t = 6 rad/s and τ_d = 2 N·m it reaches
/// ω_t after I·ω_t/τ_d = 0.054 s (6.5 ticks at 120 Hz), and holds 0.324 J of kinetic energy there.
/// </summary>
public sealed class RevoluteDriveTests
{
    private const float RotorMass = .5f;
    private const float RotorRadius = .3f;
    private const float TargetSpeed = 6f;
    private const float MaxTorque = 2f;
    private const int TicksPerSecond = 120;
    private const float TickSeconds = 1f / TicksPerSecond;
    private static float RotorInertia => .4f * RotorMass * RotorRadius * RotorRadius;
    /// <summary>The ticks the drive needs to reach ω_t from rest at τ_d (I·ω_t/τ_d), plus one.</summary>
    private static int RampTicks(float maxTorque, float frictionTorque = 0f) =>
        (int)MathF.Ceiling(RotorInertia * TargetSpeed / (maxTorque - frictionTorque) * TicksPerSecond) + 1;

    /// <summary>The speed allowance (rad/s): 2 % of ω_t. A player reads a shaft's speed by eye; a drive that settles 2 % off its target
    /// is not visibly wrong, while each plausible law defect misses by far more: a target dropped in the relax pass leaves the shaft
    /// fighting itself below ω_t, a missing drive leaves it at rest, and an unbounded or per-tick bound reaches ω_t in one tick (caught by
    /// the ramp check instead).</summary>
    private const float SpeedAllowance = .02f * TargetSpeed;

    /// <summary>The energy slack (J): about 3 % of the rotor's 0.324 J at ω_t, covering the soft joint rows' position correction and
    /// f32 rounding when kinetic energy is compared with the committed work. A drive that created energy (a work ledger that missed a
    /// substep, or a warm-started kick) shows a gain of at least one substep's τ_d·h·ω_t = 0.025 J. The ledger itself only over-counts:
    /// it prices each substep's impulse at the substep's final speed, so over a spin-up to ω_t it exceeds the kinetic energy it paid for
    /// by ½·Σλ·Δω = ½·τ_d·h·|ω_t| (<see cref="LedgerOvercount"/>), which bounds the opposite comparison.</summary>
    private const float EnergySlack = .01f;

    /// <summary>The ledger's over-count (J) for a spin-up to |ω_t| at τ_d: ½·τ_d·h·|ω_t|.</summary>
    private static float LedgerOvercount(float maxTorque, float targetSpeed) => .5f * maxTorque * RevoluteRows.SubstepSeconds * MathF.Abs(targetSpeed);

    /// <summary>The spin (rad/s) one substep of the drive at its bound gives the rotor: τ_d·h/I = 0.23 rad/s. An unsupplied rotor
    /// that ends slower than this cannot have taken even one bounded drive substep; its drive lane is also asserted exactly zero.</summary>
    private static float OneSubstepSpin => MaxTorque * RevoluteRows.SubstepSeconds / RotorInertia;

    // ---- the rig ---------------------------------------------------------------------------------------------------------------------

    private static readonly Vector3 Hinge = new(0f, 2f, 0f);

    /// <summary>One committed tick's observation of one hinge: the rotor body, its wrapped hinge angle, its axial spin, the committed
    /// drive state and the drive and stop impulse lanes.</summary>
    private readonly record struct Sample(int Tick, LawBody Rotor, float Angle, float Spin, RevoluteStateRead State, float DriveImpulse,
        float LowerStopImpulse, float UpperStopImpulse);

    /// <summary>The production device over an engine-direct scene: Admit, then Advance and Commit one tick at a time.</summary>
    private sealed class Rig : IAsyncDisposable
    {
        private static readonly WorkshopGpuProfile Profile = new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
        private readonly WorkshopGpuDevice _device;
        private readonly WorkshopCompiledScene _scene;
        private WorkshopRead _read;
        private ulong _sequence;

        private Rig(IPhysicsSceneDeclaration physics)
        {
            _device = new WorkshopGpuDevice(new BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()), physics.Document);
            var empty = CompiledSceneFixture.Compile(new(new(1), WorkshopCadenceSettings.Default(), WorkshopInstances.Empty));
            // LawScene.Source/Consumer explicitly author the logical source/base owners.
            // Rotor bodies remain physical children, not additional logical owners.
            var owners = physics.SupplyGraph.Nodes.ToArray().Select(node => node.Owner);
            _scene = new WorkshopCompiledScene(empty.Identity, empty.Revision, empty.Settings, physics, empty.Activation, [], owners, []);
        }

        public IPhysicsSceneDeclaration Physics => _scene.Physics;
        public byte[] World => _device.CommittedWorld.ToArray();

        public static async Task<Rig> Admit(IPhysicsSceneDeclaration physics)
        {
            var rig = new Rig(physics);
            await rig.Reset();
            return rig;
        }

        /// <summary>Admits the scene afresh: the Reset path (WorkshopSimulation re-admits its compiled scene).</summary>
        public async Task Reset()
        {
            var candidate = await _device.Admit(_scene, new(1), Profile, new(++_sequence));
            _device.Commit(candidate.Sequence); _device.Discard(candidate.Sequence);
            _read = candidate.Read;
        }

        /// <summary>Advances one committed tick with the given source enables (one per declared source, in source order).</summary>
        public async Task Advance(ElectricalEnable[] enables)
        {
            var candidate = await _device.Advance(_read, new(++_sequence),
                new WorkshopTickControls(enables) { DriveTarget = TickControlComposition.For(_read, []).DriveTarget });
            _device.Commit(candidate.Sequence); _device.Discard(candidate.Sequence);
            _read = candidate.Read;
        }

        /// <summary>Advances and commits one tick with exactly <paramref name="controls"/>: a boundary or refusal case's raw controls.</summary>
        public async Task AdvanceWith(WorkshopTickControls controls)
        {
            var candidate = await _device.Advance(_read, new(++_sequence), controls);
            _device.Commit(candidate.Sequence); _device.Discard(candidate.Sequence);
            _read = candidate.Read;
        }

        public Sample Observe(int tick, int hinge, GpuBodyId rotor)
        {
            var world = World;
            var body = LawState.Read(Physics, world, rotor);
            var offset = Physics.Layout.GetRevoluteOffset(Physics.Revolutes[hinge].Id.Value) + PhysicsGpuAbi.RevoluteImpulseOffset;
            float Lane(RevoluteRowKind kind) => BinaryPrimitives.ReadSingleLittleEndian(world.AsSpan(offset + 4 * (int)kind));
            // The hinge angle wrapped into [−π, π] as RevoluteRows measures it (the anchor's frame is the identity). A quaternion covers
            // a turn twice, so 2·atan2 alone spans ±2π.
            var angle = 2f * MathF.Atan2(body.Rotation.Z, body.Rotation.W);
            if (angle > MathF.PI) angle -= 2f * MathF.PI;
            else if (angle < -MathF.PI) angle += 2f * MathF.PI;
            return new(tick, body, angle, body.Angular.Z,
                PhysicsGpuAbi.ReadRevoluteState(world, hinge), Lane(RevoluteRowKind.Drive), Lane(RevoluteRowKind.LowerStop),
                Lane(RevoluteRowKind.UpperStop));
        }

        public ValueTask DisposeAsync() => _device.DisposeAsync();
    }

    /// <summary>A sphere rotor hinged at its centre to a static anchor, its drive (ω_t, τ_d) wired from one source on a separate static
    /// battery body. Connected collision is disabled, so the rotor's only interaction is the joint.</summary>
    private static (IPhysicsSceneDeclaration Scene, GpuBodyId Rotor) Rotor(float targetSpeed = TargetSpeed, float maxTorque = MaxTorque,
        float frictionTorque = 0f, RevoluteTravel travel = RevoluteTravel.Continuous, float lower = -MathF.PI, float upper = MathF.PI)
    {
        var scene = new LawScene(bench: false);
        var anchor = scene.StaticSphere(Hinge, .05f, Quaternion.Identity, LawMaterial.Bench);
        var battery = scene.StaticBox(Hinge + new Vector3(3f, 0f, 0f), new(.1f));
        var rotor = scene.Sphere(Hinge, RotorRadius, RotorMass, LawMaterial.Bench, Vector3.Zero, Vector3.Zero, 0f, LawScene.Gravity);
        var joint = scene.Revolute(anchor, rotor, ConstraintFrame.Identity, ConstraintFrame.Identity, lower, upper, frictionTorque,
            travel: travel);
        scene.RevoluteDrive(joint, targetSpeed, maxTorque);
        scene.Consumer(anchor, scene.Source(battery), SupplyConsumer.Revolute(joint));
        return (scene.Admit().Scene, rotor);
    }

    /// <summary>Runs <paramref name="ticks"/> ticks of a one-source scene, enabled while <paramref name="supplied"/> holds for the tick
    /// (1-based), and returns every committed sample of hinge 0.</summary>
    private static async Task<List<Sample>> Run(IPhysicsSceneDeclaration scene, GpuBodyId rotor, int ticks, Func<int, bool> supplied)
    {
        await using var rig = await Rig.Admit(scene);
        var samples = new List<Sample>(ticks);
        for (var tick = 1; tick <= ticks; tick++)
        {
            await rig.Advance([supplied(tick) ? ElectricalEnable.Enabled : ElectricalEnable.Disabled]);
            samples.Add(rig.Observe(tick, 0, rotor));
        }
        return samples;
    }

    private static float Kinetic(LawBody body) =>
        .5f * RotorMass * body.Velocity.LengthSquared() + .5f * RotorInertia * body.Angular.LengthSquared();

    // ---- supplied drive ------------------------------------------------------------------------------------------------------------

    // Spin-up and reverse. The drive's torque is bounded by τ_d: after 3 ticks (0.025 s) the shaft spins at τ_d·t/I = 2.78 rad/s
    // (within 10 %: a bound applied per tick instead of per substep, or not at all, would already be at ω_t). It reaches ω_t within the
    // ramp time I·ω_t/τ_d plus a tick, then holds it within SpeedAllowance at every tick of a second. ω_t = −6 is the same law reversed.
    [Theory]
    [InlineData(6f)]
    [InlineData(-6f)]
    public async Task ASuppliedDriveSpinsTheRotorUpAtItsTorqueBoundThenHoldsTheTargetSpeed(float targetSpeed)
    {
        var (scene, rotor) = Rotor(targetSpeed);
        var samples = await Run(scene, rotor, TicksPerSecond, _ => true);

        var rampSpin = MathF.Sign(targetSpeed) * MaxTorque * 3 * TickSeconds / RotorInertia;
        Assert.True(MathF.Abs(samples[2].Spin - rampSpin) <= .1f * MathF.Abs(rampSpin),
            $"After 3 ticks the rotor spins at {samples[2].Spin:F3} rad/s; τ_d·t/I is {rampSpin:F3} rad/s");
        Assert.All(samples.Skip(RampTicks(MaxTorque) - 1), sample => Assert.True(MathF.Abs(sample.Spin - targetSpeed) <= SpeedAllowance,
            $"tick {sample.Tick}: the rotor spins at {sample.Spin:F3} rad/s, target {targetSpeed} rad/s"));
    }

    // Continuous travel and reverse. Over 3 s the rotor turns about 17.8 rad in its own direction, so it passes ±π three times (at π, 3π
    // and 5π of travel): the wrapped hinge angle jumps by about 2π between ticks at each pass, both stop rows carry exactly zero impulse at every tick, the spin stays at ω_t after the ramp
    // (a Limited stop at the wrap would stop it), and the committed travel is ω_t·(t − T/2) for the ramp time T, within 2 %. A travel read
    // from the wrapped angle could never exceed π in magnitude.
    [Theory]
    [InlineData(6f)]
    [InlineData(-6f)]
    public async Task AContinuousHingeTurnsPastTheFullCircleWithNoStopImpulseAndAccumulatesItsTravel(float targetSpeed)
    {
        var (scene, rotor) = Rotor(targetSpeed);
        const int ticks = 3 * TicksPerSecond;
        var samples = await Run(scene, rotor, ticks, _ => true);

        var passes = samples.Zip(samples.Skip(1)).Count(pair => MathF.Abs(pair.Second.Angle - pair.First.Angle) > MathF.PI);
        Assert.True(passes == 3, $"The wrapped angle passed ±π {passes} times in 3 s; 17.8 rad of travel passes it three times");
        Assert.All(samples, sample => Assert.True((sample.LowerStopImpulse, sample.UpperStopImpulse) == (0f, 0f),
            $"tick {sample.Tick}: stop impulses ({sample.LowerStopImpulse}, {sample.UpperStopImpulse}) on a continuous hinge"));
        Assert.All(samples.Skip(RampTicks(MaxTorque) - 1), sample => Assert.True(MathF.Abs(sample.Spin - targetSpeed) <= SpeedAllowance,
            $"tick {sample.Tick}: the rotor spins at {sample.Spin:F3} rad/s, target {targetSpeed} rad/s"));
        var ramp = RotorInertia * TargetSpeed / MaxTorque;
        var expected = targetSpeed * (ticks * TickSeconds - ramp / 2f);
        var travel = samples[^1].State.Travel;
        Assert.True(MathF.Abs(travel - expected) <= .02f * MathF.Abs(expected),
            $"The committed travel is {travel:F3} rad; ω_t·(t − T/2) is {expected:F3} rad");
    }

    // Bearing friction. Gauss-Seidel solves the friction row before the drive row; with τ_f = 0.05 N·m well below τ_d = 2 N·m the
    // drive still holds ω_t within SpeedAllowance at every tick after its slower ramp I·ω_t/(τ_d − τ_f). It then pays the friction
    // loss: from 1 s to 2 s its committed work grows by τ_f·ω_t·1 s = 0.3 J, within 10 % (a ledger counting τ_d·ω_t would show 12 J,
    // one that missed the drive impulse would show 0).
    [Fact]
    public async Task ADriveHoldsItsTargetSpeedAgainstBearingFrictionAndPaysItsLoss()
    {
        const float frictionTorque = .05f;
        var (scene, rotor) = Rotor(frictionTorque: frictionTorque);
        var samples = await Run(scene, rotor, 2 * TicksPerSecond, _ => true);

        Assert.All(samples.Skip(RampTicks(MaxTorque, frictionTorque) - 1), sample => Assert.True(MathF.Abs(sample.Spin - TargetSpeed) <= SpeedAllowance,
            $"tick {sample.Tick}: the rotor spins at {sample.Spin:F3} rad/s against τ_f = {frictionTorque} N·m"));
        var paid = samples[^1].State.Work - samples[TicksPerSecond - 1].State.Work;
        var loss = frictionTorque * TargetSpeed * 1f;
        Assert.True(MathF.Abs(paid - loss) <= .1f * loss, $"From 1 s to 2 s the drive did {paid:F4} J; τ_f·ω_t·1 s is {loss:F4} J");
    }

    // Energy oracle on every driven case above: at every tick the rotor's kinetic energy never exceeds the committed drive work plus
    // EnergySlack (the drive creates no energy), and the work never exceeds τ_d·|ω_t|·t plus EnergySlack (the drive's power is bounded).
    [Theory]
    [InlineData(6f, 0f)]
    [InlineData(-6f, 0f)]
    [InlineData(6f, .05f)]
    public async Task ADriveNeverDeliversMoreEnergyThanItsWorkOrMoreWorkThanItsPowerBound(float targetSpeed, float frictionTorque)
    {
        var (scene, rotor) = Rotor(targetSpeed, frictionTorque: frictionTorque);
        var samples = await Run(scene, rotor, 2 * TicksPerSecond, _ => true);

        Assert.All(samples, sample =>
        {
            Assert.True(Kinetic(sample.Rotor) <= sample.State.Work + EnergySlack,
                $"tick {sample.Tick}: kinetic energy {Kinetic(sample.Rotor):F4} J exceeds the drive's work {sample.State.Work:F4} J");
            var bound = MaxTorque * MathF.Abs(targetSpeed) * sample.Tick * TickSeconds;
            Assert.True(sample.State.Work <= bound + EnergySlack,
                $"tick {sample.Tick}: work {sample.State.Work:F4} J exceeds τ_d·|ω_t|·t = {bound:F4} J");
        });
    }

    // ---- unsupplied drive ----------------------------------------------------------------------------------------------------------

    // The control: the same rotor with its source disabled from tick 0. The drive applies no impulse at any tick (its lane reads exactly
    // 0), the committed supplied flag reads false and the work exactly 0, and the rotor stays at rest: slower than one bounded drive
    // substep would leave it (OneSubstepSpin).
    [Fact]
    public async Task AnUnsuppliedDriveAppliesNoImpulseAndDoesNoWork()
    {
        var (scene, rotor) = Rotor();
        var samples = await Run(scene, rotor, TicksPerSecond, _ => false);

        Assert.All(samples, sample => Assert.True((sample.DriveImpulse, sample.State.Supplied, sample.State.Work) == (0f, false, 0f),
            $"tick {sample.Tick}: drive impulse {sample.DriveImpulse}, supplied {sample.State.Supplied}, work {sample.State.Work}"));
        Assert.True(MathF.Abs(samples[^1].Spin) < OneSubstepSpin, $"The unsupplied rotor spins at {samples[^1].Spin} rad/s");
    }

    // Supply cut mid-run. After 0.5 s supplied the source opens for 1 s. From the cut tick on the drive lane reads exactly 0, the
    // supplied flag false and the work exactly its value at the cut. There is no kick on the cut tick: the spin does not rise by more
    // than 0.01 rad/s (one tick of τ_d would add τ_d·4h/I = 0.93 rad/s). Without bearing friction the rotor coasts, keeping its speed
    // within 1 % over the second; with τ_f = 0.05 N·m it spins down at τ_f/I, to ω_t − τ_f·1 s/I = 3.22 rad/s within 5 %. At every tick
    // the rotor's kinetic energy never exceeds the committed work plus EnergySlack.
    // This proves the cut law (the [0, 0] bound and the frozen ledger), not the warm-start clamp: at ω_t the stored drive impulse is
    // near zero, and a [0, 0]-bounded drive row cancels any stale warm start within the same sweep, so the clamp's effect is not
    // observable here. The clamp is kept as a warm-start invariant (the warm-started value is always one the row could hold now).
    [Theory]
    [InlineData(0f)]
    [InlineData(.05f)]
    public async Task CuttingTheSupplyStopsTheDriveWithoutAKick(float frictionTorque)
    {
        var (scene, rotor) = Rotor(frictionTorque: frictionTorque);
        const int cut = TicksPerSecond / 2 + 1;
        var samples = await Run(scene, rotor, cut - 1 + TicksPerSecond, tick => tick < cut);
        var before = samples[cut - 2];
        var after = samples.Skip(cut - 1).ToArray();

        Assert.True(MathF.Abs(before.Spin - TargetSpeed) <= SpeedAllowance && before.State.Supplied, $"Before the cut: {before}");
        Assert.True(after[0].Spin <= before.Spin + .01f, $"The cut tick took the spin from {before.Spin:F4} to {after[0].Spin:F4} rad/s");
        Assert.All(after, sample => Assert.True((sample.DriveImpulse, sample.State.Supplied, sample.State.Work) == (0f, false, before.State.Work),
            $"tick {sample.Tick}: drive impulse {sample.DriveImpulse}, supplied {sample.State.Supplied}, work {sample.State.Work} (at the cut {before.State.Work})"));
        var expected = before.Spin - frictionTorque / RotorInertia * 1f;
        var allowance = frictionTorque == 0f ? .01f * before.Spin : .05f * expected;
        Assert.True(MathF.Abs(after[^1].Spin - expected) <= allowance,
            $"One second after the cut the rotor spins at {after[^1].Spin:F3} rad/s; expected {expected:F3} rad/s");
        Assert.All(samples, sample => Assert.True(Kinetic(sample.Rotor) <= sample.State.Work + EnergySlack,
            $"tick {sample.Tick}: kinetic energy {Kinetic(sample.Rotor):F4} J exceeds the drive's work {sample.State.Work:F4} J"));
    }

    // ---- stall, overload and brake -------------------------------------------------------------------------------------------------

    // Stall. A Limited hinge with stops at ±0.1 rad and the drive pushing toward +ω_t: the rotor reaches the upper stop within 0.25 s and
    // stays there (within 0.01 rad of it at every later tick). While blocked the drive saturates at τ_d but the shaft does not turn, so
    // its work grows by less than 1 % of the τ_d·ω_t·1 s = 12 J a free-running drive would deliver in the same second (a ledger
    // counting τ_d·ω_t rather than λ·ω_rel would grow by the whole 12 J).
    [Fact]
    public async Task AStalledDriveDoesNoWorkWhileItsShaftIsBlocked()
    {
        var (scene, rotor) = Rotor(travel: RevoluteTravel.Limited, lower: -.1f, upper: .1f);
        var samples = await Run(scene, rotor, TicksPerSecond * 5 / 4, _ => true);
        var blocked = samples.Skip(TicksPerSecond / 4 - 1).ToArray();

        Assert.All(blocked, sample => Assert.True(MathF.Abs(sample.Angle - .1f) <= .01f,
            $"tick {sample.Tick}: the stalled rotor is at {sample.Angle:F4} rad, its stop at 0.1 rad"));
        var growth = blocked[^1].State.Work - blocked[0].State.Work;
        Assert.True(growth <= .01f * MaxTorque * TargetSpeed * 1f, $"While blocked for 1 s the drive's work grew by {growth:F4} J");
        Assert.All(samples, sample => Assert.True(Kinetic(sample.Rotor) <= sample.State.Work + EnergySlack,
            $"tick {sample.Tick}: kinetic energy {Kinetic(sample.Rotor):F4} J exceeds the drive's work {sample.State.Work:F4} J"));
    }

    // The overload and brake cases use the RevoluteHingeFrictionTests bar: a 1 kg box (0.6 × 0.05 × 0.05 m half extents) hinged d = 0.5 m
    // from its centre, so gravity turns it clockwise with m·g·d = 4.905 N·m while level, with moment I = m(hx² + hy²)/3 + m·d² about the
    // hinge. The hinge is Limited to ±0.3 rad.
    private const float BarMass = 1f;
    private const float BarArm = .5f;
    private static readonly Vector3 BarExtents = new(.6f, .05f, .05f);
    private static float GravityTorque => BarMass * -LawScene.Gravity * BarArm;
    private static Vector3 BarMoments => BarMass / 3f * new Vector3(BarExtents.Y * BarExtents.Y + BarExtents.Z * BarExtents.Z,
        BarExtents.X * BarExtents.X + BarExtents.Z * BarExtents.Z, BarExtents.X * BarExtents.X + BarExtents.Y * BarExtents.Y);

    /// <summary>The bar's mechanical energy above its initial rest: kinetic energy plus m·g·Δy.</summary>
    private static float BarEnergyGain(LawBody body) =>
        .5f * BarMass * body.Velocity.LengthSquared() + .5f * Vector3.Dot(BarMoments, body.Angular * body.Angular) +
        BarMass * -LawScene.Gravity * (body.Position.Y - Hinge.Y);

    private static (IPhysicsSceneDeclaration Scene, GpuBodyId Bar) Bar(float targetSpeed, float maxTorque)
    {
        var scene = new LawScene(bench: false);
        var anchor = scene.StaticSphere(Hinge, .05f, Quaternion.Identity, LawMaterial.Bench);
        var battery = scene.StaticBox(Hinge + new Vector3(3f, 0f, 0f), new(.1f));
        var bar = scene.Box(Hinge + new Vector3(BarArm, 0f, 0f), BarExtents, BarMass, LawMaterial.Bench);
        var joint = scene.Revolute(anchor, bar, ConstraintFrame.Identity, new(new(-BarArm, 0f, 0f), Quaternion.Identity), -.3f, .3f, 0f);
        scene.RevoluteDrive(joint, targetSpeed, maxTorque);
        scene.Consumer(anchor, scene.Source(battery), SupplyConsumer.Revolute(joint));
        return (scene.Admit().Scene, bar);
    }

    // Overload. The drive pushes the bar counter-clockwise toward +6 rad/s with τ_d = 2 N·m, below the 4.9 N·m gravity load: the bar is
    // back-driven clockwise to its lower stop (below −0.25 rad within 1 s). The drive only resists that motion, so its committed work
    // never exceeds 0.01 J and never decreases; the bar's mechanical energy never exceeds that work plus 0.01 J.
    [Fact]
    public async Task AnOverloadedDriveIsBackDrivenAndAddsNoWork()
    {
        var (scene, bar) = Bar(TargetSpeed, MaxTorque);
        var samples = await Run(scene, bar, TicksPerSecond, _ => true);

        Assert.True(samples[^1].Angle <= -.25f, $"The overloaded bar ends at {samples[^1].Angle:F4} rad");
        Assert.All(samples.Zip(samples.Skip(1)), pair => Assert.True(pair.Second.State.Work >= pair.First.State.Work,
            $"tick {pair.Second.Tick}: work fell from {pair.First.State.Work} to {pair.Second.State.Work} J"));
        Assert.All(samples, sample => Assert.True(sample.State.Work <= EnergySlack && BarEnergyGain(sample.Rotor) <= sample.State.Work + EnergySlack,
            $"tick {sample.Tick}: work {sample.State.Work:E2} J, bar energy gain {BarEnergyGain(sample.Rotor):E2} J"));
    }

    // Brake. A supplied drive with ω_t = 0 and τ_d = 2·m·g·d holds the bar within 0.01 rad of level (the lever's held-tilt bound) at
    // every tick of 2 s, does at most EnergySlack of work, and the bar's mechanical energy never exceeds that work plus EnergySlack. The
    // control is the same drive unsupplied: the bar falls to its lower stop.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ASuppliedDriveWithZeroTargetSpeedHoldsItsShaftAsABrake(bool supplied)
    {
        var (scene, bar) = Bar(0f, 2f * GravityTorque);
        var samples = await Run(scene, bar, 2 * TicksPerSecond, _ => supplied);

        if (!supplied)
        {
            Assert.True(samples[^1].Angle <= -.25f, $"The unbraked bar ends at {samples[^1].Angle:F4} rad");
            return;
        }
        Assert.All(samples, sample => Assert.True(MathF.Abs(sample.Angle) <= .01f && sample.State.Work <= EnergySlack &&
                BarEnergyGain(sample.Rotor) <= sample.State.Work + EnergySlack,
            $"tick {sample.Tick}: the braked bar is at {sample.Angle:F4} rad with work {sample.State.Work:E2} J, energy gain {BarEnergyGain(sample.Rotor):E2} J"));
    }

    // ---- SIMD lanes ----------------------------------------------------------------------------------------------------------------

    // Four rotors on their own anchors fill one SIMD group: joints 0, 3, 2, 1 take lanes 0, 1, 2, 3. Each rotor's consumer node has its
    // own route, wired to the one enabled source only when that rotor is supplied. The first arrangement supplies joints 1 and 3 (lanes 3
    // and 1), the second joints 0 and 2 (lanes 0 and 2), so every lane both drives and stays inert. After 1 s each supplied rotor spins at
    // ω_t within 2 % of its own target; each unsupplied one is at rest with no drive impulse and no work. Each rotor declares its own
    // mixed-sign target and torque, so a lane that read another lane's values is caught: after 3 ticks a supplied rotor spins at its own
    // sign(ω_t)·τ_d·t/I within 10 % (the four values differ by at least 25 %), and at 1 s at its own ω_t. Every bound is derived from
    // the rotor's own values: at 1 s its kinetic energy is at most its own committed work plus its own slack (EnergySlack scaled to its
    // own kinetic energy at ω_t, the same 3 %), and an unsupplied rotor spins slower than its own τ_d·h/I.
    [Theory]
    [InlineData(false, true, false, true)]
    [InlineData(true, false, true, false)]
    public async Task EachDriveInOneSimdGroupFollowsItsOwnSupply(bool first, bool second, bool third, bool fourth)
    {
        bool[] supplied = [first, second, third, fourth];
        float[] targets = [6f, -4f, 2f, -8f];
        float[] torques = [2f, 1.5f, 1f, 3f];
        var scene = new LawScene(bench: false);
        var battery = scene.StaticBox(new(0f, 2f, -3f), new(.1f));
        var source = scene.Source(battery);
        var rotors = new GpuBodyId[4];
        for (var i = 0; i < 4; i++)
        {
            var at = Hinge + new Vector3(2f * i, 0f, 0f);
            var anchor = scene.StaticSphere(at, .05f, Quaternion.Identity, LawMaterial.Bench);
            rotors[i] = scene.Sphere(at, RotorRadius, RotorMass, LawMaterial.Bench, Vector3.Zero, Vector3.Zero, 0f, LawScene.Gravity);
            var joint = scene.Revolute(anchor, rotors[i], ConstraintFrame.Identity, ConstraintFrame.Identity, -MathF.PI, MathF.PI, 0f,
                travel: RevoluteTravel.Continuous);
            scene.RevoluteDrive(joint, targets[i], torques[i]);
            scene.Consumer(anchor, supplied[i] ? source : null, SupplyConsumer.Revolute(joint));
        }
        await using var rig = await Rig.Admit(scene.Admit().Scene);
        var ramp = new Sample[4];
        for (var tick = 1; tick <= TicksPerSecond; tick++)
        {
            await rig.Advance([ElectricalEnable.Enabled]);
            if (tick == 3) for (var i = 0; i < 4; i++) ramp[i] = rig.Observe(tick, i, rotors[i]);
        }

        for (var i = 0; i < 4; i++)
        {
            var sample = rig.Observe(TicksPerSecond, i, rotors[i]);
            var slack = EnergySlack * (targets[i] / TargetSpeed) * (targets[i] / TargetSpeed);
            var oneSubstepSpin = torques[i] * RevoluteRows.SubstepSeconds / RotorInertia;
            Assert.True(Kinetic(sample.Rotor) <= sample.State.Work + slack,
                $"Joint {i}: kinetic energy {Kinetic(sample.Rotor):F4} J exceeds its drive's work {sample.State.Work:F4} J plus {slack:F4} J");
            if (supplied[i])
            {
                var rampSpin = MathF.Sign(targets[i]) * torques[i] * 3 * TickSeconds / RotorInertia;
                Assert.True(MathF.Abs(ramp[i].Spin - rampSpin) <= .1f * MathF.Abs(rampSpin),
                    $"Supplied joint {i} spins at {ramp[i].Spin:F3} rad/s after 3 ticks; its own τ_d·t/I is {rampSpin:F3} rad/s");
                Assert.True(MathF.Abs(sample.Spin - targets[i]) <= .02f * MathF.Abs(targets[i]) && sample.State.Supplied,
                    $"Supplied joint {i} spins at {sample.Spin:F3} rad/s, its target {targets[i]} rad/s ({sample.State})");
            }
            else
                Assert.True((sample.DriveImpulse, sample.State.Supplied, sample.State.Work) == (0f, false, 0f) && MathF.Abs(sample.Spin) < oneSubstepSpin,
                    $"Unsupplied joint {i} spins at {sample.Spin} rad/s with drive impulse {sample.DriveImpulse} ({sample.State})");
        }
    }

    // A driven prismatic and a driven revolute in one scene, each on its own source: prismatic slots come first in the drive-slot order,
    // then revolute slots. With only source A enabled the slider is driven up toward its 1.2 m target (above 0.5 m within 1 s) while the
    // rotor stays slower than OneSubstepSpin; with only source B enabled the rotor reaches ω_t while the slider never rises above its
    // start (it is the unsupplied control of DeclaredPrismaticDriveTests). A lost slot offset would hand one joint's supply to the other.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EachSourceSuppliesOnlyItsOwnJointInASceneWithBothDriveKinds(bool prismaticSource)
    {
        var scene = new LawScene(bench: false);
        var batteryA = scene.StaticBox(new(0f, 5f, -4f), new(.1f));
        var batteryB = scene.StaticBox(new(2f, 5f, -4f), new(.1f));
        var sourceA = scene.Source(batteryA);
        var sourceB = scene.Source(batteryB);
        var frame = scene.StaticBox(new(-3f, 5f, 0f), new(.1f));
        var slider = scene.Box(new(-1f, 5f, 0f), new(.1f), 1f, LawMaterial.Domino);
        var prismatic = scene.Prismatic(frame, slider, new(new(2f, 0f, 0f), Quaternion.Identity), ConstraintFrame.Identity, -.2f, 2f, 3f, .5f,
            ConnectedCollision.Disabled);
        scene.Drive(prismatic, 1.2f, 1.6f, 8f, 16f, 12f);
        scene.Consumer(frame, sourceA, SupplyConsumer.Prismatic(prismatic));
        var anchor = scene.StaticSphere(Hinge + new Vector3(3f, 0f, 0f), .05f, Quaternion.Identity, LawMaterial.Bench);
        var rotor = scene.Sphere(Hinge + new Vector3(3f, 0f, 0f), RotorRadius, RotorMass, LawMaterial.Bench, Vector3.Zero, Vector3.Zero, 0f,
            LawScene.Gravity);
        var revolute = scene.Revolute(anchor, rotor, ConstraintFrame.Identity, ConstraintFrame.Identity, -MathF.PI, MathF.PI, 0f,
            travel: RevoluteTravel.Continuous);
        scene.RevoluteDrive(revolute, TargetSpeed, MaxTorque);
        scene.Consumer(anchor, sourceB, SupplyConsumer.Revolute(revolute));
        var physics = scene.Admit().Scene;
        Assert.Equal(2, physics.SupplyGraph.DriveSlots);
        await using var rig = await Rig.Admit(physics);
        ElectricalEnable[] enables = prismaticSource ? [ElectricalEnable.Enabled, ElectricalEnable.Disabled] : [ElectricalEnable.Disabled, ElectricalEnable.Enabled];
        var highest = float.NegativeInfinity;
        for (var tick = 1; tick <= TicksPerSecond; tick++)
        {
            await rig.Advance(enables);
            highest = MathF.Max(highest, LawState.Read(physics, rig.World, slider).Position.Y - 5f);
        }
        var spin = rig.Observe(TicksPerSecond, 0, rotor).Spin;

        if (prismaticSource)
            Assert.True(highest > .5f && MathF.Abs(spin) < OneSubstepSpin, $"Source A: the slider rose to {highest:F3} m, the rotor spins at {spin} rad/s");
        else
            Assert.True(highest <= .001f && MathF.Abs(spin - TargetSpeed) <= SpeedAllowance,
                $"Source B: the slider rose to {highest:F3} m, the rotor spins at {spin:F3} rad/s");
    }

    // A dynamic base free about the axis: two dynamic spheres with no gravity, the drive between them. The drive's reaction turns the
    // base the other way, so the pair's angular momentum about the axis, I_A·ω_A + I_B·ω_B, stays zero within 2 % of the rotor's
    // momentum at ω_t (no external torque acts; a drive that pushed only the rotor would create the whole I_B·ω_t). The relative speed
    // ω_B − ω_A reaches ω_t: the rotor ends at ω_t·I_A/(I_A + I_B) = 4 rad/s and the base at −2 rad/s. The work is counted from the
    // relative speed, so it pays for both bodies' kinetic energy (0.216 J): at least that less EnergySlack, at most that plus the ledger's
    // over-count ½·τ_d·h·ω_t and EnergySlack (0.239 J). A ledger reading the rotor's own speed would count only the rotor's 0.144 J.
    [Fact]
    public async Task ADriveOnAFreeBaseTurnsItTheOtherWayAndCountsWorkFromTheRelativeSpeed()
    {
        const float baseMass = 1f;
        var baseInertia = .4f * baseMass * RotorRadius * RotorRadius;
        var scene = new LawScene(bench: false);
        var battery = scene.StaticBox(Hinge + new Vector3(3f, 0f, 0f), new(.1f));
        var driveBase = scene.Sphere(Hinge, RotorRadius, baseMass, LawMaterial.Bench, Vector3.Zero, Vector3.Zero, 0f, 0f);
        var rotor = scene.Sphere(Hinge + new Vector3(0f, 0f, 1f), RotorRadius, RotorMass, LawMaterial.Bench, Vector3.Zero, Vector3.Zero, 0f, 0f);
        var joint = scene.Revolute(driveBase, rotor, new(new(0f, 0f, .5f), Quaternion.Identity), new(new(0f, 0f, -.5f), Quaternion.Identity),
            -MathF.PI, MathF.PI, 0f, travel: RevoluteTravel.Continuous);
        scene.RevoluteDrive(joint, TargetSpeed, MaxTorque);
        scene.Consumer(driveBase, scene.Source(battery), SupplyConsumer.Revolute(joint));
        var physics = scene.Admit().Scene;
        await using var rig = await Rig.Admit(physics);
        var momentumSlack = .02f * RotorInertia * TargetSpeed;
        for (var tick = 1; tick <= TicksPerSecond; tick++)
        {
            await rig.Advance([ElectricalEnable.Enabled]);
            var world = rig.World;
            var momentum = baseInertia * LawState.Read(physics, world, driveBase).Angular.Z + RotorInertia * LawState.Read(physics, world, rotor).Angular.Z;
            Assert.True(MathF.Abs(momentum) <= momentumSlack, $"tick {tick}: angular momentum about the axis is {momentum:E3} kg·m²/s");
        }

        var end = rig.World;
        var baseBody = LawState.Read(physics, end, driveBase);
        var rotorBody = LawState.Read(physics, end, rotor);
        var relative = rotorBody.Angular.Z - baseBody.Angular.Z;
        Assert.True(MathF.Abs(relative - TargetSpeed) <= SpeedAllowance, $"The relative speed is {relative:F3} rad/s");
        var kinetic = .5f * baseInertia * baseBody.Angular.LengthSquared() + .5f * baseMass * baseBody.Velocity.LengthSquared() + Kinetic(rotorBody);
        var work = PhysicsGpuAbi.ReadRevoluteState(end, 0).Work;
        Assert.True(kinetic <= work + EnergySlack && work <= kinetic + LedgerOvercount(MaxTorque, TargetSpeed) + EnergySlack,
            $"The drive's work is {work:F4} J; the pair's kinetic energy is {kinetic:F4} J");
    }

    // ---- lifecycle -----------------------------------------------------------------------------------------------------------------

    // Reset re-admits the compiled scene: after half a second of driven travel and work, the committed supplied flag, travel and work
    // read false, 0 and 0, and the rotor is back at its authored pose and at rest.
    [Fact]
    public async Task ResetZeroesTheTravelWorkAndSuppliedFlagAndRestoresTheAuthoredPose()
    {
        var (scene, rotor) = Rotor();
        await using var rig = await Rig.Admit(scene);
        var authored = rig.Observe(0, 0, rotor);
        for (var tick = 1; tick <= TicksPerSecond / 2; tick++) await rig.Advance([ElectricalEnable.Enabled]);
        var driven = rig.Observe(TicksPerSecond / 2, 0, rotor);
        Assert.True(driven.State.Travel > 1f && driven.State.Work > 0f && driven.State.Supplied, $"guard: the rotor was driven, {driven.State}");

        await rig.Reset();

        var reset = rig.Observe(0, 0, rotor);
        Assert.Equal(new RevoluteStateRead(false, 0f, 0f), reset.State);
        Assert.Equal(authored.Rotor, reset.Rotor);
    }

    // ---- refusals ------------------------------------------------------------------------------------------------------------------

    // A drive's target speed is finite with |ω_t| at most the 128 rad/s angular envelope, and its maximum torque finite and positive.
    // Each invalid value is refused by the declaration and the scene that carries it is not admitted; the boundaries ±128 rad/s and a
    // small positive torque are admitted.
    [Theory]
    [InlineData(float.NaN, MaxTorque)]
    [InlineData(float.PositiveInfinity, MaxTorque)]
    [InlineData(float.NegativeInfinity, MaxTorque)]
    [InlineData(128.00002f, MaxTorque)]
    [InlineData(-128.00002f, MaxTorque)]
    [InlineData(TargetSpeed, 0f)]
    [InlineData(TargetSpeed, -1f)]
    [InlineData(TargetSpeed, float.NaN)]
    [InlineData(TargetSpeed, float.PositiveInfinity)]
    public void AnInvalidDriveIsRefusedByTheDeclarationAndTheScene(float targetSpeed, float maxTorque)
    {
        Assert.Equal(128.00002f, MathF.BitIncrement(128f)); // guard: the out-of-envelope rows are one f32 step beyond ±128
        Assert.Throws<ArgumentException>(() => Rotor(targetSpeed, maxTorque));
        _ = Rotor(128f, MaxTorque);
        _ = Rotor(-128f, MaxTorque);
        _ = Rotor(TargetSpeed, 1e-3f);
    }

    public enum TravelDefect { ContinuousNarrowLower, ContinuousNarrowUpper, ContinuousWithSpring, UndefinedTravel }

    // Continuous travel requires exactly the full-turn limits [−π, π] and no axial spring (whose pull toward the wrapped angle would
    // reverse at ±π); an undefined travel is refused. A Limited hinge with the same values is admitted.
    [Theory]
    [InlineData(TravelDefect.ContinuousNarrowLower)]
    [InlineData(TravelDefect.ContinuousNarrowUpper)]
    [InlineData(TravelDefect.ContinuousWithSpring)]
    [InlineData(TravelDefect.UndefinedTravel)]
    public void AContinuousHingeOutsideTheFullTurnOrWithASpringIsRefused(TravelDefect defect)
    {
        var valid = new RevoluteConstraintDeclaration(new(1), new(2), new(3), ConstraintFrame.Identity, ConstraintFrame.Identity,
            -MathF.PI, MathF.PI, RevoluteTravel.Continuous, 0f, 0f, 0f, ConnectedCollision.Disabled);
        valid.Validate();
        var invalid = defect switch
        {
            TravelDefect.ContinuousNarrowLower => valid with { Lower = -3f },
            TravelDefect.ContinuousNarrowUpper => valid with { Upper = 3f },
            TravelDefect.ContinuousWithSpring => valid with { Stiffness = 10f },
            TravelDefect.UndefinedTravel => valid with { Travel = (RevoluteTravel)2 },
            _ => throw new ArgumentOutOfRangeException(nameof(defect))
        };
        Assert.Throws<ArgumentException>(() => invalid.Validate());
        if (defect != TravelDefect.UndefinedTravel) (invalid with { Travel = RevoluteTravel.Limited }).Validate();
    }

    /// <summary>A static anchor and a dynamic rotor on a continuous hinge, with no drive or consumer yet. <paramref name="staticMoving"/>
    /// swaps the bodies, so the hinge's moving body (BodyB) is the static anchor.</summary>
    private static (LawScene Scene, GpuBodyId Anchor, GpuBodyId Rotor, RevoluteConstraintId Joint) Hinged(bool staticMoving = false)
    {
        var scene = new LawScene(bench: false);
        var anchor = scene.StaticSphere(Hinge, .05f, Quaternion.Identity, LawMaterial.Bench);
        var rotor = scene.Sphere(Hinge, RotorRadius, RotorMass, LawMaterial.Bench, Vector3.Zero, Vector3.Zero, 0f, LawScene.Gravity);
        var joint = staticMoving
            ? scene.Revolute(rotor, anchor, ConstraintFrame.Identity, ConstraintFrame.Identity, -MathF.PI, MathF.PI, 0f, travel: RevoluteTravel.Continuous)
            : scene.Revolute(anchor, rotor, ConstraintFrame.Identity, ConstraintFrame.Identity, -MathF.PI, MathF.PI, 0f, travel: RevoluteTravel.Continuous);
        return (scene, anchor, rotor, joint);
    }

    // A drive must name one admitted joint whose moving body is dynamic, at most once. The same drive with the bodies the right way
    // round is admitted.
    [Fact]
    public void ADriveOnAnAbsentJointTwiceOnOneJointOrMovingAStaticBodyIsRefused()
    {
        var absent = Hinged().Scene;
        absent.RevoluteDrive(new(999), TargetSpeed, MaxTorque);
        Assert.Throws<ArgumentException>(() => absent.Admit());
        var (twice, _, _, joint) = Hinged();
        twice.RevoluteDrive(joint, TargetSpeed, MaxTorque);
        twice.RevoluteDrive(joint, -TargetSpeed, MaxTorque);
        Assert.Equal("A revolute drive requires one admitted joint whose moving body is dynamic.",
            Assert.Throws<ArgumentException>(() => twice.Admit()).Message);
        var (staticMoving, _, _, swapped) = Hinged(staticMoving: true);
        staticMoving.RevoluteDrive(swapped, TargetSpeed, MaxTorque);
        Assert.Equal("A revolute drive requires one admitted joint whose moving body is dynamic.",
            Assert.Throws<ArgumentException>(() => staticMoving.Admit()).Message);
        var (once, _, _, only) = Hinged();
        once.RevoluteDrive(only, TargetSpeed, MaxTorque);
        _ = once.Admit();
    }

    public enum ConsumerDefect { NoConsumer, NoPrismaticConsumer, UndrivenHinge, NotTheOwner, NamedTwice }

    // Every declared drive is consumed by exactly one consumer node, which must be the joint's base (BodyA) and name a declared drive.
    // A revolute or prismatic drive no node consumes, a revolute consumer naming an undriven hinge, one on a node that does not own the
    // joint, and one node naming the same drive twice are each refused with their own reason; the owned, driven, singly consumed control
    // is admitted.
    [Theory]
    [InlineData(ConsumerDefect.NoConsumer)]
    [InlineData(ConsumerDefect.NoPrismaticConsumer)]
    [InlineData(ConsumerDefect.UndrivenHinge)]
    [InlineData(ConsumerDefect.NotTheOwner)]
    [InlineData(ConsumerDefect.NamedTwice)]
    public void ARevoluteConsumerMustNameItsOwnDrivenHingeExactlyOnce(ConsumerDefect defect)
    {
        var (control, anchor, _, joint) = Hinged();
        control.RevoluteDrive(joint, TargetSpeed, MaxTorque);
        control.Consumer(anchor, null, SupplyConsumer.Revolute(joint));
        _ = control.Admit(consumeUnnamedDrives: false);

        var (scene, owner, _, hinge) = Hinged();
        var other = scene.StaticBox(Hinge + new Vector3(3f, 0f, 0f), new(.1f));
        if (defect != ConsumerDefect.UndrivenHinge) scene.RevoluteDrive(hinge, TargetSpeed, MaxTorque);
        switch (defect)
        {
            case ConsumerDefect.NoConsumer: break;
            case ConsumerDefect.NoPrismaticConsumer:
            {
                // The revolute drive is consumed; a driven slider beside it is not.
                scene.Consumer(owner, null, SupplyConsumer.Revolute(hinge));
                var frame = scene.StaticBox(Hinge + new Vector3(-3f, 0f, 0f), new(.1f));
                var slider = scene.Box(Hinge + new Vector3(-3f, 0f, 0f), new(.1f), 1f, LawMaterial.Domino);
                var prismatic = scene.Prismatic(frame, slider, ConstraintFrame.Identity, ConstraintFrame.Identity, -.2f, 2f, 3f, .5f,
                    ConnectedCollision.Disabled);
                scene.Drive(prismatic, 1.2f, 1.6f, 8f, 16f, 12f);
                break;
            }
            case ConsumerDefect.UndrivenHinge: scene.Consumer(owner, null, SupplyConsumer.Revolute(hinge)); break;
            case ConsumerDefect.NotTheOwner: scene.Consumer(other, null, SupplyConsumer.Revolute(hinge)); break;
            case ConsumerDefect.NamedTwice: scene.Consumer(owner, null, SupplyConsumer.Revolute(hinge), SupplyConsumer.Revolute(hinge)); break;
            default: throw new ArgumentOutOfRangeException(nameof(defect));
        }
        var message = Assert.Throws<ArgumentException>(() => scene.Admit(consumeUnnamedDrives: false)).Message;
        Assert.Equal(defect switch
        {
            ConsumerDefect.NoConsumer => "A declared revolute drive has no consumer node.",
            ConsumerDefect.NoPrismaticConsumer => "A declared prismatic drive has no consumer node.",
            ConsumerDefect.NamedTwice => "Electrical consumer names a driven joint more than once.",
            _ => "Electrical consumer names no driven joint its node owns."
        }, message);
    }

    // The bundle validator's target-speed envelope is the engine's angular envelope: a driven hinge declaring exactly
    // AngularVelocity.MaximumSpeed is admitted and one f32 step beyond it refused.
    [Fact]
    public void TheValidatorsTargetSpeedEnvelopeIsTheEngineAngularEnvelope()
    {
        var lever = TestBundle.Entry(new PartId("impact_lever"));
        CuriousContraptions.Bundle.BundleEntry Driven(float target) => lever with
        {
            Ports = [.. lever.Ports, new(CuriousContraptions.Abstractions.PortId.PowerIn, CuriousContraptions.Abstractions.SimulationDomain.Electrical,
                CuriousContraptions.Abstractions.PortDirection.Input, CuriousContraptions.Abstractions.PortRole.PowerIn, null, Vector3.Zero, Vector3.UnitZ)],
            Electrical = new CuriousContraptions.Bundle.ElectricalPayload(
                [new("drive", CuriousContraptions.Abstractions.Electrical.ElectricalRole.Consumer, 12f, 1f, 0f, 100f)],
                new CuriousContraptions.Abstractions.Electrical.SupplyGraphDescriptor(
                    [new CuriousContraptions.Abstractions.Electrical.SupplyConsumerDescriptor(new("drive_cons"), [new CuriousContraptions.Abstractions.Electrical.SupplyDriveTarget(new(CuriousContraptions.Abstractions.CoordinateKind.Revolute, 0))])],
                    [],
                    [new(CuriousContraptions.Abstractions.PortId.PowerIn, new(new("drive_cons"), CuriousContraptions.Abstractions.Electrical.SupplyTerminal.PowerIn))])),
            Mechanical = lever.Mechanical! with
            {
                Hinges = [lever.Mechanical.Hinges[0] with
                {
                    LowerLimit = -MathF.PI, UpperLimit = MathF.PI, Travel = RevoluteTravel.Continuous,
                    Drive = new(MechanicalValue.Fixed(target), MechanicalValue.Fixed(MaxTorque))
                }]
            }
        };
        IReadOnlyList<CuriousContraptions.Bundle.BundleError> Check(float target) =>
            CuriousContraptions.Bundle.BundleValidator.Validate([new("lever", Driven(target))]);

        Assert.Empty(Check(AngularVelocity.MaximumSpeed));
        Assert.Empty(Check(-AngularVelocity.MaximumSpeed));
        Assert.NotEmpty(Check(MathF.BitIncrement(AngularVelocity.MaximumSpeed)));
        Assert.NotEmpty(Check(MathF.BitDecrement(-AngularVelocity.MaximumSpeed)));
    }

    public enum RecordDefect
    {
        UndefinedTravel, UndefinedDriveKind, UndrivenWithDriveValues, SuppliedZeroTorque, SuppliedOverspeed, SuppliedNaNTarget, Padding
    }

    // The ABI read refuses a revolute record whose travel or drive kind is undefined, an undriven record carrying drive values, a
    // supplied record whose drive is invalid (zero torque, a target beyond the envelope or not finite), and non-zero trailing padding; the
    // unpoisoned record reads back its declared drive.
    [Theory]
    [InlineData(RecordDefect.UndefinedTravel)]
    [InlineData(RecordDefect.UndefinedDriveKind)]
    [InlineData(RecordDefect.UndrivenWithDriveValues)]
    [InlineData(RecordDefect.SuppliedZeroTorque)]
    [InlineData(RecordDefect.SuppliedOverspeed)]
    [InlineData(RecordDefect.SuppliedNaNTarget)]
    [InlineData(RecordDefect.Padding)]
    public void TheAbiReadRefusesAnInvalidTravelOrDriveRecord(RecordDefect defect)
    {
        var (scene, _) = Rotor();
        var admitted = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        Assert.Equal(scene.RevoluteDrives[0], PhysicsGpuAbi.ReadRevoluteDrive(admitted, 0));
        Assert.Equal(RevoluteTravel.Continuous, PhysicsGpuAbi.ReadRevolute(admitted, 0).Travel);
        var offset = scene.Layout.GetRevoluteOffset(scene.Revolutes[0].Id.Value);
        var poisoned = (byte[])admitted.Clone();
        var record = poisoned.AsSpan(offset, PhysicsGpuAbi.RevoluteBytes);
        switch (defect)
        {
            case RecordDefect.UndefinedTravel: BinaryPrimitives.WriteUInt32LittleEndian(record[PhysicsGpuAbi.RevoluteTravelKindOffset..], 2); break;
            case RecordDefect.UndefinedDriveKind: BinaryPrimitives.WriteUInt32LittleEndian(record[PhysicsGpuAbi.RevoluteDriveKindOffset..], 2); break;
            case RecordDefect.UndrivenWithDriveValues: BinaryPrimitives.WriteUInt32LittleEndian(record[PhysicsGpuAbi.RevoluteDriveKindOffset..], (uint)RevoluteDriveKind.None); break;
            case RecordDefect.SuppliedZeroTorque: BinaryPrimitives.WriteSingleLittleEndian(record[PhysicsGpuAbi.RevoluteDriveMaxTorqueOffset..], 0f); break;
            case RecordDefect.SuppliedOverspeed: BinaryPrimitives.WriteSingleLittleEndian(record[PhysicsGpuAbi.RevoluteDriveTargetSpeedOffset..], 129f); break;
            case RecordDefect.SuppliedNaNTarget: BinaryPrimitives.WriteSingleLittleEndian(record[PhysicsGpuAbi.RevoluteDriveTargetSpeedOffset..], float.NaN); break;
            case RecordDefect.Padding: record[PhysicsGpuAbi.RevoluteBytes - 1] = 1; break;
            default: throw new ArgumentOutOfRangeException(nameof(defect));
        }
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadRevolute(poisoned, 0));
    }

    public enum CandidateEdit { CommittedTarget, DriveKind, MaxTorque, FrictionTorque, TargetBeyondEnvelope, NonFiniteTarget }

    // engine-revolute-drive-target: the drive target (byte 104) is committed state, so the candidate lock covers every other
    // declaration byte. A candidate may commit a new finite target within the ±128 rad/s envelope; a changed drive kind, maximum torque
    // or friction torque is refused as a changed declaration, and a target beyond the envelope or not finite by the record rule.
    [Theory]
    [InlineData(CandidateEdit.CommittedTarget)]
    [InlineData(CandidateEdit.DriveKind)]
    [InlineData(CandidateEdit.MaxTorque)]
    [InlineData(CandidateEdit.FrictionTorque)]
    [InlineData(CandidateEdit.TargetBeyondEnvelope)]
    [InlineData(CandidateEdit.NonFiniteTarget)]
    public void TheCandidateLockCoversEveryDeclarationByteButTheCommittedTarget(CandidateEdit edit)
    {
        var (scene, _) = Rotor();
        var source = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var offset = scene.Layout.GetRevoluteOffset(scene.Revolutes[0].Id.Value);
        var candidate = (byte[])source.Clone();
        var record = candidate.AsSpan(offset, PhysicsGpuAbi.RevoluteBytes);
        switch (edit)
        {
            case CandidateEdit.CommittedTarget: BinaryPrimitives.WriteSingleLittleEndian(record[PhysicsGpuAbi.RevoluteDriveTargetSpeedOffset..], -AngularVelocity.MaximumSpeed); break;
            case CandidateEdit.DriveKind: BinaryPrimitives.WriteUInt32LittleEndian(record[PhysicsGpuAbi.RevoluteDriveKindOffset..], (uint)RevoluteDriveKind.None);
                record.Slice(PhysicsGpuAbi.RevoluteDriveTargetSpeedOffset, PhysicsGpuAbi.RevoluteDriveBytes).Clear(); break;
            case CandidateEdit.MaxTorque: BinaryPrimitives.WriteSingleLittleEndian(record[PhysicsGpuAbi.RevoluteDriveMaxTorqueOffset..], MaxTorque * 2f); break;
            case CandidateEdit.FrictionTorque: BinaryPrimitives.WriteSingleLittleEndian(record[PhysicsGpuAbi.RevoluteFrictionTorqueOffset..], .5f); break;
            case CandidateEdit.TargetBeyondEnvelope: BinaryPrimitives.WriteSingleLittleEndian(record[PhysicsGpuAbi.RevoluteDriveTargetSpeedOffset..], MathF.BitIncrement(AngularVelocity.MaximumSpeed)); break;
            case CandidateEdit.NonFiniteTarget: BinaryPrimitives.WriteSingleLittleEndian(record[PhysicsGpuAbi.RevoluteDriveTargetSpeedOffset..], float.PositiveInfinity); break;
            default: throw new ArgumentOutOfRangeException(nameof(edit));
        }
        void Validate() => PhysicsGpuAbi.ValidateCandidate(candidate, source, new(0), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty);
        if (edit == CandidateEdit.CommittedTarget)
        {
            Validate();
            Assert.Equal(-AngularVelocity.MaximumSpeed, PhysicsGpuAbi.ReadRevoluteDrive(candidate, 0)!.Value.TargetSpeed);
        }
        else Assert.Throws<ArgumentException>(Validate);
    }

    // The committed drive-target read has one row per driven hinge, in drive-table order: the hinge's base body as owner and its
    // committed target, the admitted declaration's at admission. An undriven hinge has no row.
    [Fact]
    public void TheDriveTargetReadCarriesEachDrivenHingesOwnerAndCommittedTarget()
    {
        var (scene, _) = Rotor(targetSpeed: -3.5f);
        var admitted = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var read = PhysicsGpuAbi.ReadDriveTargets(admitted);
        Assert.Equal((1, new RevoluteDriveTargetRead(scene.Revolutes[0].BodyA, -3.5f)), (read.Count, read[0]));

        var (undriven, _, _, _) = Hinged();
        Assert.Equal(0, PhysicsGpuAbi.ReadDriveTargets(undriven.Admit().Admitted).Count);
    }

    public enum TargetDefect { Missing, Extra, NaN, PositiveInfinity, NegativeInfinity, BeyondEnvelope, BelowEnvelope }

    // Every tick input names exactly one finite target within the ±128 rad/s envelope per compiled drive. A missing or extra target, NaN,
    // ±∞ and a value one step beyond either envelope bound are refused with an ArgumentException at the engine's Advance and at the
    // device's Advance. At the device nothing is committed: the committed target is still the declared one, and the scene then advances
    // with its declared target. Boundary controls: exactly ±128 rad/s advance at both the engine and the device, and the device commits
    // each.
    [Theory]
    [InlineData(TargetDefect.Missing)]
    [InlineData(TargetDefect.Extra)]
    [InlineData(TargetDefect.NaN)]
    [InlineData(TargetDefect.PositiveInfinity)]
    [InlineData(TargetDefect.NegativeInfinity)]
    [InlineData(TargetDefect.BeyondEnvelope)]
    [InlineData(TargetDefect.BelowEnvelope)]
    public async Task TickInputsWithoutOneAdmittedTargetPerDriveAreRefusedAtTheEngineAndTheDevice(TargetDefect defect)
    {
        float[] targets = defect switch
        {
            TargetDefect.Missing => [],
            TargetDefect.Extra => [TargetSpeed, TargetSpeed],
            TargetDefect.NaN => [float.NaN],
            TargetDefect.PositiveInfinity => [float.PositiveInfinity],
            TargetDefect.NegativeInfinity => [float.NegativeInfinity],
            TargetDefect.BeyondEnvelope => [MathF.BitIncrement(AngularVelocity.MaximumSpeed)],
            TargetDefect.BelowEnvelope => [MathF.BitDecrement(-AngularVelocity.MaximumSpeed)],
            _ => throw new ArgumentOutOfRangeException(nameof(defect))
        };
        var (scene, _) = Rotor();
        var admitted = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var engine = new WorkshopPhysicsEngine();
        engine.Reset();
        PhysicsInteractionSnapshot Step(float[] values) => engine.Advance(admitted, new byte[admitted.Length],
            new PhysicsTickInputs([ElectricalEnable.Enabled], [true]) { DriveTarget = values }, PhysicsInteractionSnapshot.Empty);
        Assert.Throws<ArgumentException>(() => Step(targets));
        _ = Step([TargetSpeed]);
        _ = Step([AngularVelocity.MaximumSpeed]);
        _ = Step([-AngularVelocity.MaximumSpeed]);

        await using var rig = await Rig.Admit(scene);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            rig.AdvanceWith(new WorkshopTickControls([ElectricalEnable.Enabled]) { DriveTarget = targets }));
        Assert.Equal(TargetSpeed, PhysicsGpuAbi.ReadDriveTargets(rig.World)[0].TargetSpeed);
        await rig.Advance([ElectricalEnable.Enabled]);
        Assert.Equal(TargetSpeed, PhysicsGpuAbi.ReadDriveTargets(rig.World)[0].TargetSpeed);
        foreach (var bound in new[] { AngularVelocity.MaximumSpeed, -AngularVelocity.MaximumSpeed })
        {
            await rig.AdvanceWith(new WorkshopTickControls([ElectricalEnable.Enabled]) { DriveTarget = [bound] });
            Assert.Equal(bound, PhysicsGpuAbi.ReadDriveTargets(rig.World)[0].TargetSpeed);
        }
    }

    public enum StateDefect
    {
        SuppliedFlagTwo, DriveBeyondItsBound, DriveWhileUnsupplied, WorkDecreased, WorkNegative, WorkNaN, TravelNaN, TravelInfinite,
        ContinuousLowerStop, ContinuousUpperStop
    }

    // A committed candidate is refused when its supplied flag is not 0 or 1, its drive impulse lies beyond the one bound the solver clamps
    // to for that flag (±τ_d·h supplied, 0 unsupplied), its work fell, is negative or non-finite, its travel is non-finite, or a
    // continuous hinge's stop carries impulse. Each refused value has an admitted neighbour: the flag 1, exactly ±τ_d·h, the source's
    // work, any finite travel and a zero stop impulse.
    [Theory]
    [InlineData(StateDefect.SuppliedFlagTwo)]
    [InlineData(StateDefect.DriveBeyondItsBound)]
    [InlineData(StateDefect.DriveWhileUnsupplied)]
    [InlineData(StateDefect.WorkDecreased)]
    [InlineData(StateDefect.WorkNegative)]
    [InlineData(StateDefect.WorkNaN)]
    [InlineData(StateDefect.TravelNaN)]
    [InlineData(StateDefect.TravelInfinite)]
    [InlineData(StateDefect.ContinuousLowerStop)]
    [InlineData(StateDefect.ContinuousUpperStop)]
    public void ACommittedDriveStateOutsideItsBoundsIsRefused(StateDefect defect)
    {
        var (scene, _) = Rotor();
        var source = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var offset = scene.Layout.GetRevoluteOffset(scene.Revolutes[0].Id.Value);
        var limit = RevoluteRows.DriveImpulseLimit(MaxTorque, supplied: true);
        const float sourceWork = .5f;
        BinaryPrimitives.WriteSingleLittleEndian(source.AsSpan(offset + PhysicsGpuAbi.RevoluteWorkOffset), sourceWork);

        bool Admitted(Action<Span<byte>> write)
        {
            var candidate = (byte[])source.Clone();
            write(candidate.AsSpan(offset, PhysicsGpuAbi.RevoluteBytes));
            try
            {
                PhysicsGpuAbi.ValidateCandidate(candidate, source, new(0), PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
        static void U32(Span<byte> record, int at, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(record[at..], value);
        static void F32(Span<byte> record, int at, float value) => BinaryPrimitives.WriteSingleLittleEndian(record[at..], value);
        var drive = PhysicsGpuAbi.RevoluteImpulseOffset + 4 * (int)RevoluteRowKind.Drive;
        var lower = PhysicsGpuAbi.RevoluteImpulseOffset + 4 * (int)RevoluteRowKind.LowerStop;
        var upper = PhysicsGpuAbi.RevoluteImpulseOffset + 4 * (int)RevoluteRowKind.UpperStop;

        var (refused, admitted) = defect switch
        {
            StateDefect.SuppliedFlagTwo => (Admitted(r => U32(r, PhysicsGpuAbi.RevoluteSuppliedOffset, 2)), Admitted(r => U32(r, PhysicsGpuAbi.RevoluteSuppliedOffset, 1))),
            StateDefect.DriveBeyondItsBound => (Admitted(r => { U32(r, PhysicsGpuAbi.RevoluteSuppliedOffset, 1); F32(r, drive, MathF.BitDecrement(-limit)); }),
                Admitted(r => { U32(r, PhysicsGpuAbi.RevoluteSuppliedOffset, 1); F32(r, drive, -limit); })),
            StateDefect.DriveWhileUnsupplied => (Admitted(r => F32(r, drive, limit)), Admitted(r => { U32(r, PhysicsGpuAbi.RevoluteSuppliedOffset, 1); F32(r, drive, limit); })),
            StateDefect.WorkDecreased => (Admitted(r => F32(r, PhysicsGpuAbi.RevoluteWorkOffset, MathF.BitDecrement(sourceWork))), Admitted(_ => { })),
            StateDefect.WorkNegative => (Admitted(r => F32(r, PhysicsGpuAbi.RevoluteWorkOffset, -1f)), Admitted(r => F32(r, PhysicsGpuAbi.RevoluteWorkOffset, 1f))),
            StateDefect.WorkNaN => (Admitted(r => F32(r, PhysicsGpuAbi.RevoluteWorkOffset, float.NaN)), Admitted(r => F32(r, PhysicsGpuAbi.RevoluteWorkOffset, 1f))),
            StateDefect.TravelNaN => (Admitted(r => F32(r, PhysicsGpuAbi.RevoluteTravelOffset, float.NaN)), Admitted(r => F32(r, PhysicsGpuAbi.RevoluteTravelOffset, -50f))),
            StateDefect.TravelInfinite => (Admitted(r => F32(r, PhysicsGpuAbi.RevoluteTravelOffset, float.PositiveInfinity)), Admitted(r => F32(r, PhysicsGpuAbi.RevoluteTravelOffset, 50f))),
            StateDefect.ContinuousLowerStop => (Admitted(r => F32(r, lower, 1e-3f)), Admitted(r => F32(r, lower, 0f))),
            StateDefect.ContinuousUpperStop => (Admitted(r => F32(r, upper, 1e-3f)), Admitted(r => F32(r, upper, 0f))),
            _ => throw new ArgumentOutOfRangeException(nameof(defect))
        };
        Assert.Equal((false, true), (refused, admitted));
    }

    // The engine refuses tick inputs that close the supply slot of a hinge without a declared drive, before stepping; the same slot open
    // advances, and a driven hinge's slot may close.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheEngineRefusesAClosedSupplySlotWithoutADeclaredDrive(bool driven)
    {
        var (scene, anchor, _, joint) = Hinged();
        if (driven)
        {
            scene.RevoluteDrive(joint, TargetSpeed, MaxTorque);
            scene.Consumer(anchor, null, SupplyConsumer.Revolute(joint));
        }
        var world = scene.Admit(consumeUnnamedDrives: false);
        PhysicsInteractionSnapshot Advance(bool closed)
        {
            var engine = new WorkshopPhysicsEngine();
            engine.Reset();
            return engine.Advance(world.Admitted, new byte[world.Admitted.Length],
                new PhysicsTickInputs([], [closed]) { DriveTarget = PhysicsGpuAbi.ReadDriveTargets(world.Admitted).TargetSpeeds() }, PhysicsInteractionSnapshot.Empty);
        }

        _ = Advance(false);
        if (driven) _ = Advance(true);
        else Assert.Equal("Physics tick inputs close the supply of a slot that declares no drive.",
            Assert.Throws<ArgumentException>(() => Advance(true)).Message);
    }

    // An undriven hinge never commits a supplied flag: its committed state read refuses one.
    [Fact]
    public void AnUndrivenHingeRefusesACommittedSuppliedFlag()
    {
        var scene = new LawScene(bench: false);
        var anchor = scene.StaticSphere(Hinge, .05f, Quaternion.Identity, LawMaterial.Bench);
        var rotor = scene.Sphere(Hinge, RotorRadius, RotorMass, LawMaterial.Bench, Vector3.Zero, Vector3.Zero, 0f, LawScene.Gravity);
        var joint = scene.Revolute(anchor, rotor, ConstraintFrame.Identity, ConstraintFrame.Identity, -1f, 1f, 0f);
        var world = scene.Admit();
        Assert.Null(PhysicsGpuAbi.ReadRevoluteDrive(world.Admitted, 0));
        Assert.Equal(new RevoluteStateRead(false, 0f, 0f), PhysicsGpuAbi.ReadRevoluteState(world.Admitted, 0));
        var poisoned = (byte[])world.Admitted.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(poisoned.AsSpan(world.Scene.Layout.GetRevoluteOffset(joint.Value) + PhysicsGpuAbi.RevoluteSuppliedOffset), 1);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadRevoluteState(poisoned, 0));
    }
}
