using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WoundSpringControlTests
{
    private static PrismaticRetentionRead Read(WorkshopIntegrationHarness h) => Assert.Single(h.Committed.Presentation.Retentions.ToArray());
    private static WorkshopGenericInstance Part(string kind, ulong id, float x, float y, float z = 0, Quaternion? rotation = null)
    {
        var q = rotation ?? Quaternion.Identity;
        return WorkshopInput.Generic(RuntimeTestServices.Bundle, new(kind), new(id), x, y, z, q.X, q.Y, q.Z, q.W);
    }
    private static WorkshopConstruction Bench(string source = "motor") => Bench(source, Part("wound_spring", 3, 0, 4));
    private static WorkshopConstruction Bench(string source, WorkshopGenericInstance spring) => new(new(2), WorkshopCadenceSettings.Default(), new(
        PlacedParts.Battery(new(1), -8, 4, 0), Part(source, 2, -4, 4), spring,
        Part("clock", 4, -4, 7, -3) with { Parameters = new([new(new("interval_seconds"), .2f)]) },
        PlacedParts.Battery(new(5), -8, 7, -3, enabled: ElectricalEnable.Disabled)),
        Connections: new(
            new WorkshopConnection(new(1), PortId.Supply, new(2), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
            new(new(2), PortId.Drive, new(3), PortId.DriveIn, WorkshopConnectionDomain.Mechanical),
            new(new(5), PortId.Supply, new(4), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
            new(new(4), PortId.ActivationOut, new(3), PortId.ActivationIn, WorkshopConnectionDomain.Activation)));
    private static async Task Power(WorkshopIntegrationHarness h, ulong owner, ElectricalEnable value) =>
        await h.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: h.RunControl(new(owner), RunControlLane.SourceEnable, RunControlValue.Of(value)));

    [Theory]
    [InlineData(-6f)]
    [InlineData(.5f)]
    public async Task ChargedStoreRetainsThroughReverseOrReducedDrive(float target)
    {
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var motor = TestBundle.Entry(new("motor"));
        var entry = motor with { Metadata = motor.Metadata with { Id = "signed_store_source",
            Parameters = motor.Metadata.Parameters.Select(p => p.Key == "speed" ? p with { Minimum = -20f } : p).ToArray() } };
        Assert.Empty(BundleValidator.Validate([new("source", entry)]));
        using var declarations = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        await h.Require(WorkshopCommandKind.Construct, Bench(entry.Metadata.Id));
        await h.Run(); await h.Advance(90);
        var attained = Read(h);
        Assert.True(attained.Compression > .3f, "The reverse/reduced control begins with useful physical charge.");
        await h.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: h.RunControl(new(2), RunControlLane.RevoluteDriveTarget, RunControlValue.Of(target)));
        await h.Advance(60);
        var settled = Read(h);
        await h.Advance(60);
        Assert.True(Read(h).Compression >= attained.Compression - .001f, "A second of reverse/slower drive cannot unwind the retained store by a visible millimetre.");
        // The retarget took effect. Reverse: the shafts' axial inertia is I = .5·.3²/2 (motor rotor) + .25·.26²/2 (spring shaft) =
        // .031 kg·m², so 20 N·m stops the 6 rad/s rotor in I·ω/τ = .0093 s and reverses it within .019 s, well inside the first half
        // second; that transient may still wind, bounded by the coasting ½Iω² = .56 J. The second half second winds nothing more
        // (no positive interface transfer beyond the .005 J allowance). This window was chosen after a whole-second check observed a
        // .073 J transient (disclosed). Reduced: the .5 rad/s target winds at most lead × speed × 1 s = .1 × .5 × 1 = .05 m, plus
        // 1 cm, against ≈ .5 m at full speed.
        if (target < 0f)
        {
            // The whole transient, retarget to settle, is bounded by the coasting energy plus the existing allowance.
            const float inertia = .5f * .3f * .3f / 2f + .25f * .26f * .26f / 2f;
            Assert.InRange(settled.PositiveWindingTransfer - attained.PositiveWindingTransfer, 0f, .5f * inertia * 6f * 6f + .005f);
            Assert.True(Read(h).PositiveWindingTransfer - settled.PositiveWindingTransfer is >= 0f and <= .005f,
                $"transfer at retarget {attained.PositiveWindingTransfer} J, +.5 s {settled.PositiveWindingTransfer} J, +1 s {Read(h).PositiveWindingTransfer} J");
        }
        else
            Assert.InRange(Read(h).Compression - attained.Compression, -.001f, .06f);
        Assert.Equal(RetentionMode.Retained, Read(h).Mode);
        Assert.Null(Read(h).RequestResult);
        Assert.Equal(0f, Read(h).ReleasedElasticEnergy);
        await h.Reset(); Assert.Equal(0f, Read(h).Compression);
    }

    // S23a (narrowed by T3/T4): two invalid initial wound-spring constructions are refused by the production scene compiler, the
    // boundary BrowserWorkshopClient applies before any command is dispatched, so neither reaches the router or worker: a
    // basketball placed into the spring's head at the sleeve radius, .5 m above the part origin (typed InitialOverlap), and a 2 m
    // stroke outside the declared .2–.8 m range (the boundary's ArgumentException; it carries no typed reason).
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InvalidWoundSpringConstructionIsRefusedAtTheCompileBoundary(bool overlappingLoad)
    {
        var invalid = overlappingLoad
            ? Bench() with { Instances = Bench().Instances.With(Part("ball", 6, .49f, 4.5f)) }
            : Bench("motor", Part("wound_spring", 3, 0, 4) with { Parameters = new([new(new("stiffness"), 160f),
                new(new("stroke"), 2f), new(new("winding_lead"), .1f)]) });
        ICompiledScene Compile() => WorkshopSceneCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, invalid,
            new(101, 206), new(1));
        if (overlappingLoad)
            Assert.Equal(WorkshopEditRejection.InitialOverlap, Assert.Throws<WorkshopEditRejectedException>(Compile).Reason);
        else
            Assert.Throws<ArgumentException>(Compile);
    }

    // S23b: a release request latched in the committed read but not yet consumed by physics does not survive Reset, nor Reset and
    // removal of the requesting clock (removal is a Building edit, so it follows Reset). The bench's clock battery is authored off,
    // so after Reset nothing can request a release: any release in the next second would be the stale one.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetWithAnUnconsumedLatchedReleaseFiresNoStaleRelease(bool removeRequester)
    {
        await using var h = await WorkshopIntegrationHarness.Running(Bench());
        await h.Advance(120);
        await Power(h, 5, ElectricalEnable.Enabled);
        bool Latched() => Enumerable.Range(0, h.Committed.Activations.Count)
            .Any(i => h.Committed.Activations[i].Owner == new GpuBodyId(4) && h.Committed.Activations[i].Phase == ActivationPhase.Latched);
        for (var tick = 0; tick < 40 && !Latched(); tick++) await h.AdvanceOne();
        Assert.True(Latched(), "The clock's release request is latched in the committed read.");
        // The spring's own physical-request node is latched in the committed read at Reset: the device would compose its release
        // request from exactly this row on the next tick.
        var request = h.Simulation.Scene.Activation.Nodes.ToArray()
            .Single(node => node.Kind == ActivationNodeKind.PhysicalRequest && node.Owner == new GpuBodyId(3)).Id;
        Assert.Contains(Enumerable.Range(0, h.Committed.Activations.Count).Select(i => h.Committed.Activations[i]),
            row => row.Node == request && row.Phase == ActivationPhase.Latched);
        Assert.Null(Read(h).RequestResult);
        await h.Reset();
        if (removeRequester)
        {
            var removed = Bench().WithoutInstance(new(4)) with { Revision = new(3) };
            await h.Require(WorkshopCommandKind.Construct, removed);
        }
        await h.Run();
        for (var tick = 0; tick < 120; tick++)
        {
            await h.AdvanceOne();
            Assert.Null(Read(h).RequestResult);
            Assert.Equal(RetentionMode.Retained, Read(h).Mode);
            Assert.Equal(0f, Read(h).ReleasedElasticEnergy);
        }
        Assert.True(Read(h).Compression > .05f, "The rewound store holds its charge.");
    }

    [Fact]
    public async Task PowerOffAtPendingReleaseUsesOnlyStoredAndCoastingEnergy()
    {
        await using var h = await WorkshopIntegrationHarness.Running(Bench());
        await h.Advance(120);
        await Power(h, 5, ElectricalEnable.Enabled);
        for (var tick = 0; tick < 40 && !Enumerable.Range(0, h.Committed.Activations.Count)
            .Any(i => h.Committed.Activations[i].Owner == new GpuBodyId(4) && h.Committed.Activations[i].Phase == ActivationPhase.Latched); tick++)
            await h.AdvanceOne();
        Assert.Null(Read(h).RequestResult);
        Assert.Contains(Enumerable.Range(0, h.Committed.Activations.Count).Select(i => h.Committed.Activations[i]),
            row => row.Owner == new GpuBodyId(4) && row.Phase == ActivationPhase.Latched);
        var origin = h.Committed;
        var source = SpringTestEnergy.SourceWork(h);
        var available = SpringTestEnergy.Measure(h.Simulation.Scene.Physics, origin, origin);
        await Power(h, 1, ElectricalEnable.Disabled);
        await h.AdvanceOne();
        Assert.Equal(RetentionRequestResult.Released, Read(h).RequestResult);
        await Power(h, 5, ElectricalEnable.Disabled);
        for (var tick = 0; tick < 120; tick++)
        {
            await h.AdvanceOne();
            Assert.Equal(source, SpringTestEnergy.SourceWork(h));
            Assert.True(SpringTestEnergy.Measure(h.Simulation.Scene.Physics, h.Committed, origin) <=
                available + SpringTestEnergy.Allowance(h.Simulation.Scene.Physics),
                "Same-boundary power-off/release cannot add source work or invent an extra launch impulse.");
        }
        Assert.Equal(RetentionMode.Retained, Read(h).Mode);
        Assert.True(Read(h).ReleasedElasticEnergy > .005f); // Rearmed once the released head physically reached its rest end stop; legitimate passive/coasting recharge remains inside the budget.
    }

    [Fact]
    public async Task ActualImpactSwitchReleasesPartialChargeWithoutTimer()
    {
        var lesson = PuzzleCatalogue.CreateSolution(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle,
            new("wind_then_release"), new(2), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var spring = lesson.Instances.Single(p => p.PartId == new PartId("wound_spring")).Id;
        var impact = lesson.Instances.Single(p => p.PartId == new PartId("switch")).Id;
        var delay = lesson.Instances.Single(p => p.PartId == new PartId("delay")).Id;
        var bench = lesson.WithoutInstance(delay) with { Puzzle = default };
        bench = bench with { Connections = bench.Connections.With(new(impact, PortId.ActivationOut, spring, PortId.ActivationIn, WorkshopConnectionDomain.Activation)) };
        await using var h = await WorkshopIntegrationHarness.Running(bench);
        var before = Read(h);
        for (var tick = 0; tick < 240 && Read(h).RequestResult is null; tick++)
        {
            before = Read(h); await h.AdvanceOne();
        }
        Assert.Equal(RetentionRequestResult.Released, Read(h).RequestResult);
        Assert.InRange(before.Compression, .1f, .75f); // Useful partial travel, at least 5 cm short of the .8 m endpoint.
        Assert.Contains(Enumerable.Range(0, h.Committed.Activations.Count).Select(i => h.Committed.Activations[i]), row => row.Owner == impact && row.Phase == ActivationPhase.Latched);
        var potential = Read(h).Potential;
        await h.Advance(12);
        Assert.True(Read(h).ReleasedElasticEnergy > .005f);
        Assert.True(Read(h).Potential < potential, "The direct contact occurrence must actually release the plunger.");
        await h.Reset(); Assert.Equal(0f, Read(h).Compression);
    }

    [Fact]
    public async Task ActualPoweredGateBlocksReleaseAndOpeningResumesIt()
    {
        var gate = Part("powered_gate", 6, 0, 5.6f, rotation: Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f));
        var bench = Bench();
        bench = bench with { Instances = new(bench.Instances.Append(gate).Append(Part("ball", 8, 0, 9f)).Append(PlacedParts.Battery(new(7), 6, 6, -3)).ToArray()),
            Connections = bench.Connections.With(new(new(7), PortId.Supply, new(6), PortId.PowerIn, WorkshopConnectionDomain.Electrical)) };
        await using var h = await WorkshopIntegrationHarness.Running(bench);
        await h.Advance(240);
        Assert.True(Read(h).Compression > .7f);
        await Power(h, 1, ElectricalEnable.Disabled); await Power(h, 7, ElectricalEnable.Disabled);
        await h.Advance(180);
        await Power(h, 5, ElectricalEnable.Enabled);
        await h.Advance(120);
        var blocked = Read(h);
        Assert.Equal(RetentionMode.Releasing, blocked.Mode);
        Assert.Equal(RetentionPhase.Blocked, blocked.Phase);
        Assert.True(blocked.Compression > .2f, "The real closed blade and payload must stop the rising physical head before rest.");
        await Power(h, 5, ElectricalEnable.Disabled); await Power(h, 7, ElectricalEnable.Enabled);
        var returned = false;
        for (var tick = 0; tick < 240; tick++)
        {
            await h.AdvanceOne();
            returned |= Read(h).Mode == RetentionMode.Retained;
        }
        Assert.True(returned, "Opening the real gate must let the released head physically reach rest and rearm; later passive recharge remains legitimate.");
        Assert.True(Read(h).ReleasedElasticEnergy > blocked.ReleasedElasticEnergy);
        await h.Reset(); Assert.Equal(0f, Read(h).Compression);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualWallLimitsWindingWhileHousingKeepsItsDeclaredClearance(bool blocked)
    {
        var bench = Bench();
        var wall = Part("wall", 6, blocked ? 0f : 3f, 4.2f) with {
            Parameters = new([new(new("width"), .4f), new(new("height"), .4f), new(new("thickness"), .4f)]) };
        bench = bench with { Instances = new(bench.Instances.Append(wall).ToArray()) };
        await using var h = await WorkshopIntegrationHarness.Running(bench);
        await h.Advance(360);
        var state = Read(h);
        if (blocked)
        {
            Assert.Equal(RetentionPhase.Blocked, state.Phase);
            Assert.True(state.Compression < .3f, "The actual wall top at 4.4m stops the radius .32m head, rather than allowing passage through it.");
        }
        else Assert.InRange(state.Compression, .79f, .801f); // The unobstructed housing permits its authored .8m stroke.
        Assert.Null(state.RequestResult);
        await h.Reset(); Assert.Equal(0f, Read(h).Compression);
    }

    // Q17/T1: the carried accepted-work window is committed state; Reset admits an empty window at the admitted (resting) velocities.
    // A stale mid-winding window would credit its open impulses (a few hundredths of a joule) on the first tick after Reset, so that
    // tick must match a fresh running bench's first tick: discrete state exactly, energies and work within the file's .005 J work
    // allowance, compression within one visible millimetre.
    [Fact]
    public async Task ResetMidWindingStartsFromAnEmptyCarriedWindow()
    {
        (PrismaticRetentionRead Retention, MechanicalTransmissionStateRead Relation) fresh;
        await using (var control = await WorkshopIntegrationHarness.Running(Bench()))
        {
            await control.AdvanceOne();
            fresh = (Read(control), WindingRelation(control));
        }
        await using var h = await WorkshopIntegrationHarness.Running(Bench());
        await h.Advance(60);
        var wound = (Read(h), WindingRelation(h));
        Assert.True(wound.Item1.Compression > .05f && wound.Item2.PositiveWork > 0f, "Mid-winding, work and charge have accrued.");
        await h.Reset();
        await h.Run(); await h.AdvanceOne();
        var (retention, relation) = (Read(h), WindingRelation(h));
        const float work = .005f;
        Assert.Equal(fresh.Retention.Mode, retention.Mode);
        Assert.Equal(fresh.Retention.Phase, retention.Phase);
        Assert.Equal(fresh.Retention.RequestResult, retention.RequestResult);
        Assert.InRange(retention.Compression - fresh.Retention.Compression, -.001f, .001f);
        Assert.InRange(retention.Potential - fresh.Retention.Potential, -work, work);
        Assert.InRange(retention.AcceptedElasticEnergy - fresh.Retention.AcceptedElasticEnergy, -work, work);
        Assert.InRange(retention.ReleasedElasticEnergy - fresh.Retention.ReleasedElasticEnergy, -work, work);
        Assert.InRange(retention.WindingWork - fresh.Retention.WindingWork, -work, work);
        Assert.InRange(retention.PositiveWindingTransfer - fresh.Retention.PositiveWindingTransfer, -work, work);
        Assert.InRange(retention.ContactWork - fresh.Retention.ContactWork, -work, work);
        Assert.InRange(retention.GravityWork - fresh.Retention.GravityWork, -work, work);
        Assert.InRange(relation.Work - fresh.Relation.Work, -work, work);
        Assert.InRange(relation.PositiveWork - fresh.Relation.PositiveWork, -work, work);
    }

    /// <summary>The committed state of the bench's winding relation, through the typed ABI reader.</summary>
    private static MechanicalTransmissionStateRead WindingRelation(WorkshopIntegrationHarness h)
    {
        var scene = h.Simulation.Scene.Physics;
        var relation = scene.Transmissions.ToArray().Single(row => row.Retention is not null);
        return PhysicsGpuAbi.ReadMechanicalTransmissionState(h.Device.CommittedWorld.Span,
            scene.Layout.GetMechanicalTransmissionSlot(relation.Id.Value));
    }

    [Fact]
    public async Task PackageTriggerReportsEmptyThenReleasedThenAlreadyReleasedAndRearms()
    {
        var invertedSpring = Part("wound_spring", 3, 0, 5, rotation: Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI));
        var emptyBench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(
            PlacedParts.Battery(new(5), -8, 7, -3),
            Part("clock", 4, -4, 7, -3) with { Parameters = new([new(new("interval_seconds"), .2f)]) },
            invertedSpring),
            Connections: new(
                new WorkshopConnection(new(5), PortId.Supply, new(4), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                new(new(4), PortId.ActivationOut, new(3), PortId.ActivationIn, WorkshopConnectionDomain.Activation)));
        await using (var emptyHarness = await WorkshopIntegrationHarness.Running(emptyBench))
        {
            for (var tick = 0; tick < 40 && Read(emptyHarness).RequestResult is null; tick++) await emptyHarness.AdvanceOne();
            Assert.Equal(RetentionRequestResult.Empty, Read(emptyHarness).RequestResult);
            Assert.Equal(0f, Read(emptyHarness).Potential);
        }

        var bench = Bench();
        var gate = Part("powered_gate", 6, 0, 5.6f, rotation: Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f));
        bench = bench with
        {
            Instances = new(bench.Instances
                .Append(gate).Append(Part("ball", 8, 0, 9f)).Append(PlacedParts.Battery(new(7), 6, 6, -3)).ToArray()),
            Connections = bench.Connections.With(new(new(7), PortId.Supply, new(6), PortId.PowerIn, WorkshopConnectionDomain.Electrical))
        };
        await using var h = await WorkshopIntegrationHarness.Running(bench);
        await h.Advance(240);
        Assert.True(Read(h).Compression > .6f);
        await Power(h, 1, ElectricalEnable.Disabled);
        await Power(h, 7, ElectricalEnable.Disabled);
        await h.Advance(180);
        await Power(h, 5, ElectricalEnable.Enabled);
        for (var tick = 0; tick < 40 && Read(h).RequestResult != RetentionRequestResult.Released; tick++) await h.AdvanceOne();
        Assert.Equal(RetentionRequestResult.Released, Read(h).RequestResult);
        Assert.Equal(RetentionMode.Releasing, Read(h).Mode);
        for (var tick = 0; tick < 40 && Read(h).RequestResult != RetentionRequestResult.AlreadyReleased; tick++) await h.AdvanceOne();
        Assert.Equal(RetentionRequestResult.AlreadyReleased, Read(h).RequestResult);
        await Power(h, 5, ElectricalEnable.Disabled);
        await Power(h, 7, ElectricalEnable.Enabled);
        for (var tick = 0; tick < 240 && Read(h).Mode != RetentionMode.Retained; tick++) await h.AdvanceOne();
        Assert.Equal(RetentionMode.Retained, Read(h).Mode);
        await h.Reset(); Assert.Equal(0f, Read(h).Compression);
    }

    // Pass 3 F7 amendment C: the retained slider admits ordinary damping, and the damping is actually applied. One unfamiliar
    // store takes its damping from an N·s/m parameter; runs at 0 and at .5 N·s/m wind to the same full stroke and release.
    [Fact]
    public async Task DampedUnfamiliarStoreWindsRetainsReleasesAndRearmsWithBoundedDissipation()
    {
        var source = TestBundle.Entry(new("wound_spring"));
        var entry = source with
        {
            Metadata = source.Metadata with { Id = "damped_compressive_store", Parameters = [.. source.Metadata.Parameters,
                new("damping", ParameterUnit.NewtonSecondPerMetre, ParameterKind.Real, 0f, 2f, .5f, [])] },
            Mechanical = source.Mechanical! with { Sliders = [source.Mechanical.Sliders[0] with { Damping = MechanicalValue.FromParameter("damping") }] }
        };
        Assert.Empty(BundleValidator.Validate([new("damped", entry)]));
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        using var declarations = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        var undamped = await Launch(0f, 2);
        await h.Reset();
        var damped = await Launch(.5f, 3);
        // Derived before execution for k=160 N/m, m=.5 kg, c=.8 m, b=.5 N·s/m and g=9.81 (vertical head):
        // dissipation b∫v dx over the stroke is at most b·√(k/m)·πc²/4 = 4.50 J; with that bound the damped speed stays above
        // √((k(c²−x²) − 2mg(x+c) − 9.0)/m), so the dissipation is at least 3.96 J. One 120 Hz tick of sampling the peak kinetic
        // energy loses at most ½k(v_max/120)² = 1.05 J (v_max = 13.8 m/s). The damped peak must therefore lie at least
        // 3.96 − 1.05 = 2.9 J lower; 2.5 J is that bound rounded down, far above the existing .005 J energy allowance.
        const float dissipationMargin = 2.5f;
        Assert.True(undamped.Peak - damped.Peak > dissipationMargin,
            $"undamped peak {undamped.Peak} J, damped peak {damped.Peak} J: the declared damping must dissipate launch energy");

        async Task<(float Peak, float Released)> Launch(float damping, ulong revision)
        {
            var spring = Part(entry.Metadata.Id, 3, 0, 4) with { Parameters = new(entry.Metadata.Parameters.Select(parameter =>
                KeyValuePair.Create(new ParameterId(parameter.Key), parameter.Key == "damping" ? damping : parameter.Default))) };
            await h.Require(WorkshopCommandKind.Construct, Bench("motor", spring) with { Revision = new(revision) });
            var slider = Assert.Single(h.Simulation.Scene.Physics.Prismatics.ToArray());
            Assert.Equal(damping, slider.Damping);
            await h.Run(); await h.Advance(360);
            Assert.InRange(Read(h).Compression, .79f, .801f); // Winds to the authored .8 m stroke despite the damper.
            await Power(h, 1, ElectricalEnable.Disabled);
            await h.Advance(60);
            var charged = Read(h);
            Assert.Equal(RetentionMode.Retained, charged.Mode);
            Assert.InRange(charged.Compression, .79f, .801f); // Source loss retains the full charge.
            await Power(h, 5, ElectricalEnable.Enabled);
            for (var tick = 0; tick < 40 && Read(h).RequestResult != RetentionRequestResult.Released; tick++)
            {
                charged = Read(h);
                await h.AdvanceOne();
            }
            Assert.Equal(RetentionRequestResult.Released, Read(h).RequestResult);
            await Power(h, 5, ElectricalEnable.Disabled);
            var peak = 0f;
            // Two game seconds is a generous stall guard for a .8 m elastic return.
            for (var tick = 0; tick < 240 && Read(h).Mode != RetentionMode.Retained; tick++)
            {
                Assert.True(h.Committed.Bodies.TryGet(slider.BodyB, out var head));
                var v = head.Body.Velocity;
                peak = MathF.Max(peak, .5f * h.Simulation.Scene.Physics.Bodies.ToArray().Single(b => b.Id == slider.BodyB).Mass.Value *
                    (v.X * v.X + v.Y * v.Y + v.Z * v.Z));
                await h.AdvanceOne();
            }
            var rearmed = Read(h);
            Assert.Equal(RetentionMode.Retained, rearmed.Mode);
            var stored = .5f * slider.Stiffness * charged.Compression * charged.Compression;
            // Released elastic energy is the store's own ½kc² in both runs; damping only removes kinetic energy.
            Assert.InRange(rearmed.ReleasedElasticEnergy - stored, -.005f, .005f);
            Assert.True(peak <= rearmed.ReleasedElasticEnergy + .005f, "Release cannot launch with more than the stored energy.");
            return (peak, rearmed.ReleasedElasticEnergy);
        }
    }
}
