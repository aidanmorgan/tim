using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.39 (CAT-005, EL-196): the Battery is a generic instance of its package. Its finite store, its enable state and
/// its run-time control come only from the declaration; the shared compiler builds the store and the production WorkshopSimulation
/// (the worker engine) runs it, tick by tick.</summary>
public sealed class SourceStoreInstanceTests
{
    private static readonly GpuBodyId Gate = new(1), Source = new(3), Ball = new(5), Bumper = new(6);
    private static readonly WorkshopCadenceSettings Settings = WorkshopCadenceSettings.Default();

    private static WorkshopConnection Power(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.Supply, target, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);

    /// <summary>A Powered Gate at the origin and a source beside it, wired Supply → PowerIn unless <paramref name="wired"/> is false.</summary>
    private static WorkshopConstruction GateBench(WorkshopGenericInstance source, bool wired = true) =>
        new(new(2), Settings, new(WorkshopInput.Generic(PartId.PoweredGate, Gate, 0, 3, 0, 0, 0, 0, 1), source),
            Connections: wired ? new(Power(source.Id, Gate)) : WorkshopConnections.Empty);

    /// <summary>A heavy ball that empties a Bumper's contact-work store, the store recharged by the source wired to its PowerIn.</summary>
    private static WorkshopConstruction RechargeBench(WorkshopGenericInstance source, float strength = 8f) =>
        new(new(2), Settings, new(WorkshopInput.Generic(PartId.BowlingBall, Ball, 0, 6, 0, 0, 0, 0, 1), PlacedParts.Bumper(Bumper, 0, 3, 0, (float)(strength)), source),
            Connections: new(Power(source.Id, Bumper)));

    private sealed class Harness : IAsyncDisposable
    {
        private readonly WorkshopGpuDevice _device = new(new CuriousContraptions.Simulation.BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()), new(101, 206));
        public WorkshopSimulation Simulation { get; }
        private Harness() => Simulation = new(_device, new WorkshopPhysicsEngineTests.StepClock(), Settings, new(1),
            new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        public static async Task<Harness> Built(WorkshopConstruction construction)
        {
            var harness = new Harness();
            Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Initialize()).Outcome);
            Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Construct(construction)).Outcome);
            return harness;
        }
        public async Task<List<WorkshopRead>> Advance(int ticks)
        {
            var reads = new List<WorkshopRead>(ticks);
            for (var tick = 0; tick < ticks; tick++)
            {
                Assert.Equal(WorkshopCommandOutcome.Applied, (await Simulation.Advance()).Outcome);
                reads.Add(Simulation.Committed);
            }
            return reads;
        }
        public async ValueTask DisposeAsync()
        {
            await Simulation.DisposeAsync();
            await _device.DisposeAsync();
        }
    }

    private static ElectricalSourceRead SourceRead(WorkshopRead read) =>
        Enumerable.Range(0, read.Electrical.Count).Select(i => read.Electrical[i]).Single(row => row.Owner == Source);
    private static ActuatorDriveRead Blade(WorkshopRead read) => read.Presentation.Actuators.ToArray().Single(row => row.Owner == Gate);

    // ---------- supply ----------

    // CAT-005 §5: an enabled source wired to the gate supplies it from the first committed tick after Run and the blade opens; a
    // disabled one never does and its charge never moves. The gate's drive debits no source energy (coordinator decision 1: the
    // per-load demand law is Story 11.1's), so the charge stays whole in both.
    [Theory]
    [InlineData(ElectricalEnable.Enabled)]
    [InlineData(ElectricalEnable.Disabled)]
    public async Task AnEnabledStoreSuppliesTheGateFromTheFirstCommittedTickAndADisabledOneNeverDoes(ElectricalEnable enabled)
    {
        await using var harness = await Harness.Built(GateBench(PlacedParts.Battery(Source, 3, 3, 0, enabled: enabled)));
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Run()).Outcome);
        var reads = await harness.Advance(240);
        Assert.All(reads, read => Assert.Equal((3600f, 0f, enabled),
            (SourceRead(read).Remaining.Value, SourceRead(read).Debit.Value, SourceRead(read).Enabled)));
        if (enabled == ElectricalEnable.Enabled)
        {
            Assert.All(reads, read => Assert.True(Blade(read).SupplyClosed));
            Assert.True(Blade(reads[^1]).Coordinate > 1.5f, $"open blade at {Blade(reads[^1]).Coordinate} m");
        }
        else
        {
            Assert.All(reads, read => Assert.False(Blade(read).SupplyClosed));
            Assert.All(reads, read => Assert.InRange(Blade(read).Coordinate, -1e-3f, 1e-3f));
        }
    }

    // B6 and the CAT-005 negative controls: an unwired source supplies nothing; a consumer cannot feed a source; an activation link
    // cannot stand in for the electrical wire into the source or the gate.
    [Fact]
    public async Task NoWireAReversedWireOrAnActivationLinkGivesNoSupply()
    {
        await using (var harness = await Harness.Built(GateBench(PlacedParts.Battery(Source, 3, 3, 0), wired: false)))
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Run()).Outcome);
            var reads = await harness.Advance(120);
            Assert.All(reads, read => Assert.False(Blade(read).SupplyClosed));
            Assert.All(reads, read => Assert.InRange(Blade(read).Coordinate, -1e-3f, 1e-3f));
        }
        var unwired = GateBench(PlacedParts.Battery(Source, 3, 3, 0), wired: false);
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => (unwired with { Connections = new(Power(Gate, Source)) }).Validate());
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => (unwired with { Connections = new(
            new WorkshopConnection(Source, WorkshopSocket.Supply, Gate, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Activation)) }).Validate());
        var withSwitch = unwired with { Instances = unwired.Instances.With(WorkshopInput.Generic(PartId.ImpactSwitch, new(7), -3, 1, 0, 0, 0, 0, 1)) };
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => (withSwitch with { Connections = new(
            new WorkshopConnection(new(7), WorkshopSocket.ActivationOut, Source, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)) }).Validate());
    }

    // ---------- the finite store ----------

    // EL-196: each committed tick the store pays at most its power limit for one 1/120 s phase, never more than it holds; the energy it
    // delivers never exceeds its initial charge (capacity × initial charge); a store that starts empty delivers nothing. The heavy
    // ball empties the Bumper's store so the source has demand to meet.
    [Theory]
    [InlineData(3600f, 10f, 1f, 8f)]
    [InlineData(60f, 480f, 1f, 20f)]
    [InlineData(120f, 30f, .5f, 8f)]
    [InlineData(14400f, 480f, 1f, 20f)]
    [InlineData(600f, 10f, 0f, 8f)]
    public async Task TheStorePaysWithinItsPowerLimitAndNeverMoreThanItsInitialCharge(float capacity, float power, float charge, float strength)
    {
        await using var harness = await Harness.Built(RechargeBench(PlacedParts.Battery(Source, 3, 1, 0, capacity, power, charge), strength));
        var scene = WorkshopPhysicsCompiler.Compile(harness.Simulation.Construction, new(1, 2));
        var declared = Assert.Single(scene.Electrical.Sources.ToArray());
        Assert.Equal((capacity, power, charge, ElectricalEnable.Enabled), (declared.Capacity.Value, declared.MaximumPower.Value, declared.InitialFraction, declared.Enabled));
        var initial = capacity * charge;
        Assert.Equal(initial, SourceRead(harness.Simulation.Committed).Remaining.Value);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Run()).Outcome);
        var reads = await harness.Advance(960);
        var limit = power * ElectricalSupply.PhaseSeconds;
        double delivered = 0;
        foreach (var read in reads)
        {
            var source = SourceRead(read);
            Assert.True(source.Debit.Value <= limit + PhysicsElectricalRead.Residual(limit), $"debit {source.Debit.Value} J above {limit} J");
            Assert.True(source.Debit.Value <= source.Remaining.Value + source.Debit.Value);
            delivered += source.Debit.Value;
            Assert.True(delivered <= initial + 1e-3, $"delivered {delivered} J of {initial} J");
            Assert.InRange(source.Remaining.Value, 0f, initial);
        }
        if (charge == 0f) Assert.All(reads, read => Assert.Equal((0f, 0f), (SourceRead(read).Debit.Value, read.ContactWorks[0].SuppliedEnergy.Value)));
        else Assert.Contains(reads, read => SourceRead(read).Debit.Value > 0f);
        // The 60 J store at 480 W runs dry while the Bumper still wants work (its last positive balance is paid on the tick it empties),
        // and from then on pays nothing; in all it delivered exactly its 60 J.
        if (capacity == 60f)
        {
            var empty = reads.FindIndex(read => SourceRead(read).Remaining.Value == 0f);
            Assert.True(empty > 0, "the small store depletes");
            Assert.True(SourceRead(reads[empty]).Debit.Value > 0f);
            Assert.All(reads.Skip(empty + 1), read => Assert.Equal((0f, 0f), (SourceRead(read).Remaining.Value, SourceRead(read).Debit.Value)));
            Assert.InRange(delivered, 60 - 1e-3, 60 + 1e-3);
        }
    }

    // One compile law for every declared Source: a synthetic store names its own keys, and its values compile as given (f32, the
    // initial energy capacity × charge in f32); the enable state is exactly the two Choice indices.
    [Fact]
    public void ASyntheticStoreCompilesItsOwnKeysAndValues()
    {
        var store = new SupplySourceStoreDescriptor("q", "p", "f", "on");
        InstanceParameterCollection Values(float on) => new([new(new("q"), 777.5f), new(new("p"), 33.25f), new(new("f"), .375f), new(new("on"), on)]);
        var off = WorkshopPhysicsCompiler.ElectricalSourceFor(new(10), new(9), store, Values(0f));
        Assert.Equal((new GpuBodyId(9), 777.5f, 33.25f, .375f, ElectricalEnable.Disabled),
            (off.Owner, off.Capacity.Value, off.MaximumPower.Value, off.InitialFraction, off.Enabled));
        Assert.Equal(BitConverter.SingleToInt32Bits(777.5f * .375f), BitConverter.SingleToInt32Bits(off.InitialEnergy.Value));
        Assert.Equal(ElectricalEnable.Enabled, WorkshopPhysicsCompiler.ElectricalSourceFor(new(10), new(9), store, Values(1f)).Enabled);
        foreach (var undefined in new[] { .5f, 2f, -1f, float.NaN })
            Assert.Throws<ArgumentException>(() => WorkshopPhysicsCompiler.ElectricalSourceFor(new(10), new(9), store, Values(undefined)));
        // The Battery's compiled store is exactly its package's store over its own parameters.
        var battery = PlacedParts.Battery(Source, 3, 1, 0, 1234.5f, 77.25f, .625f, ElectricalEnable.Disabled);
        var compiled = Assert.Single(WorkshopPhysicsCompiler.Compile(GateBench(battery), new(1, 2)).Electrical.Sources.ToArray());
        Assert.Equal(WorkshopPhysicsCompiler.ElectricalSourceFor(GateBench(battery).Instances.Identities[Source].Source, Source, DeclaredPhysics.SourceStore(PartId.Battery)!.Value, battery.Parameters), compiled);
        // Only a declared Source compiles a store: no other part does.
        Assert.All(TestBundle.Entries.Where(entry => entry.Metadata.Id != "battery"), entry => Assert.Null(entry.Electrical?.Supply?.Store));
    }

    // The run-time control is the declaration's: a Source store whose enable state is RunControl has one; the same store declared
    // BuildOnly, a store-less supply part and a part without a supply have none.
    [Fact]
    public void ARunTimeControlExistsOnlyWhereTheStoreDeclaresItsEnableStateRunEditable()
    {
        var entry = DeclaredPhysics.Of(PartId.Battery);
        Assert.True(DeclaredPhysics.HasRunControlledSource(entry));
        Assert.True(DeclaredPhysics.HasRunControlledSource(PartId.Battery));
        var buildOnly = entry with { Metadata = entry.Metadata with { Parameters = entry.Metadata.Parameters
            .Select(parameter => parameter with { RunEdit = ParameterRunEdit.BuildOnly }).ToArray() } };
        Assert.False(DeclaredPhysics.HasRunControlledSource(buildOnly));
        Assert.False(DeclaredPhysics.HasRunControlledSource(PartId.PoweredGate));
        Assert.False(DeclaredPhysics.HasRunControlledSource(PartId.ImpactSwitch));
        Assert.False(DeclaredPhysics.HasRunControlledSource(PartId.Basketball));
    }

    // ---------- the run-time control (ConfigureElectrical) ----------

    // A Running control is applied for the next physics tick: the committed source reports it, the gate's supply opens or closes with
    // it, and the authored construction, its revision and its value are untouched.
    [Fact]
    public async Task ARunningControlIsAppliedOnTheNextTickAndLeavesTheAuthoredConstructionAlone()
    {
        await using var harness = await Harness.Built(GateBench(PlacedParts.Battery(Source, 3, 3, 0)));
        var authored = harness.Simulation.Construction;
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Run()).Outcome);
        var open = await harness.Advance(120);
        Assert.True(Blade(open[^1]).SupplyClosed);
        var receipt = harness.Simulation.ConfigureElectrical(new(Source, ElectricalEnable.Disabled));
        Assert.Equal((WorkshopCommandOutcome.Applied, WorkshopRejection.None), (receipt.Outcome, receipt.Reason));
        Assert.Equal(authored, harness.Simulation.Construction);
        var next = await harness.Advance(1);
        Assert.Equal(ElectricalEnable.Disabled, SourceRead(next[0]).Enabled);
        Assert.False(Blade(next[0]).SupplyClosed);
        var closing = await harness.Advance(360);
        Assert.All(closing, read => Assert.False(Blade(read).SupplyClosed));
        Assert.InRange(Blade(closing[^1]).Coordinate, -1e-2f, 1e-2f);
        Assert.Equal(WorkshopCommandOutcome.Applied, harness.Simulation.ConfigureElectrical(new(Source, ElectricalEnable.Enabled)).Outcome);
        var again = await harness.Advance(1);
        Assert.Equal(ElectricalEnable.Enabled, SourceRead(again[0]).Enabled);
        Assert.True(Blade(again[0]).SupplyClosed);
        Assert.Equal(authored, harness.Simulation.Construction);
        Assert.Equal(authored.Revision, harness.Simulation.Construction.Revision);
    }

    // A Paused control is held until Resume; a second control while paused reverses it (the run is then bit-identical to one never
    // controlled); a held control is applied on the first tick after Resume.
    [Fact]
    public async Task APausedControlIsHeldReversedByASecondAndAppliedOnResume()
    {
        var bench = GateBench(PlacedParts.Battery(Source, 3, 3, 0));
        List<WorkshopRead> plain;
        await using (var control = await Harness.Built(bench))
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await control.Simulation.Run()).Outcome);
            plain = await control.Advance(120);
        }
        await using var harness = await Harness.Built(bench);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Run()).Outcome);
        var first = await harness.Advance(30);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Pause()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, harness.Simulation.ConfigureElectrical(new(Source, ElectricalEnable.Disabled)).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, harness.Simulation.ConfigureElectrical(new(Source, ElectricalEnable.Enabled)).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Resume()).Outcome);
        var reversed = first.Concat(await harness.Advance(90)).ToList();
        for (var i = 0; i < plain.Count; i++) NoFixedCapacityTests.AssertSameState(plain[i], reversed[i], $"reversed tick {i}");
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Pause()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, harness.Simulation.ConfigureElectrical(new(Source, ElectricalEnable.Disabled)).Outcome);
        Assert.Equal(ElectricalEnable.Enabled, SourceRead(harness.Simulation.Committed).Enabled);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Resume()).Outcome);
        var resumed = await harness.Advance(1);
        Assert.Equal(ElectricalEnable.Disabled, SourceRead(resumed[0]).Enabled);
        Assert.False(Blade(resumed[0]).SupplyClosed);
    }

    // The control is refused outside a run, and for any owner but an unlocked part whose package declares a run-controlled store:
    // a locked source, the gate, a ball, an unknown identity.
    [Fact]
    public async Task TheControlIsRefusedOutsideARunAndForAnyOwnerWithoutARunControlledStore()
    {
        var bench = GateBench(PlacedParts.Battery(Source, 3, 3, 0));
        bench = bench with { Instances = bench.Instances.With(WorkshopInput.Generic(PartId.Basketball, Ball, -3, 3, 0, 0, 0, 0, 1))
            .With(PlacedParts.Battery(new(8), 6, 3, 0) with { Locked = true }) };
        await using var harness = await Harness.Built(bench);
        var build = harness.Simulation.ConfigureElectrical(new(Source, ElectricalEnable.Disabled));
        Assert.Equal((WorkshopCommandOutcome.Rejected, WorkshopRejection.WrongPhase), (build.Outcome, build.Reason));
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Run()).Outcome);
        foreach (var owner in new GpuBodyId[] { new(8), Gate, Ball, new(99) })
        {
            var refused = harness.Simulation.ConfigureElectrical(new(owner, ElectricalEnable.Disabled));
            Assert.Equal((WorkshopCommandOutcome.Rejected, WorkshopRejection.InvalidConstruction), (refused.Outcome, refused.Reason));
        }
        var reads = await harness.Advance(2);
        Assert.All(reads, read => Assert.Equal(ElectricalEnable.Enabled, SourceRead(read).Enabled));
    }

    // ---------- lifecycle ----------

    // Reset restores the built state exactly and retires the control; a save made during the run holds the authored value, never the
    // control; that save loads to the same construction, and a run of it replays the never-controlled run bit for bit.
    [Fact]
    public async Task ResetIsExactAndClearsTheControlAndASaveDuringRunKeepsTheAuthoredValue()
    {
        var bench = GateBench(PlacedParts.Battery(Source, 3, 3, 0, 1800f, 60f, .75f));
        List<WorkshopRead> plain;
        await using (var control = await Harness.Built(bench))
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await control.Simulation.Run()).Outcome);
            plain = await control.Advance(180);
        }
        await using var harness = await Harness.Built(bench);
        var built = harness.Simulation.Committed;
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Run()).Outcome);
        await harness.Advance(60);
        Assert.Equal(WorkshopCommandOutcome.Applied, harness.Simulation.ConfigureElectrical(new(Source, ElectricalEnable.Disabled)).Outcome);
        var disabled = await harness.Advance(60);
        Assert.All(disabled, read => Assert.Equal(ElectricalEnable.Disabled, SourceRead(read).Enabled));
        var saved = WorkshopSaveCodec.Encode(new(harness.Simulation.Construction, new(9)));
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, saved).Construction;
        Assert.Equal(bench, loaded);
        Assert.Equal(1f, Assert.IsType<WorkshopGenericInstance>(loaded.Instances[1]).Parameters[new("enabled")]);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Reset()).Outcome);
        NoFixedCapacityTests.AssertSameState(built, harness.Simulation.Committed, "Reset");
        Assert.Equal(ElectricalEnable.Enabled, harness.Simulation.ElectricalState(Source, ElectricalEnable.Enabled));
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Simulation.Run()).Outcome);
        var replay = await harness.Advance(180);
        for (var i = 0; i < plain.Count; i++) NoFixedCapacityTests.AssertSameState(plain[i], replay[i], $"replay tick {i}");
        await using var reloaded = await Harness.Built(loaded);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await reloaded.Simulation.Run()).Outcome);
        var fromSave = await reloaded.Advance(180);
        for (var i = 0; i < plain.Count; i++) NoFixedCapacityTests.AssertSameState(plain[i], fromSave[i], $"loaded tick {i}");
    }
}
