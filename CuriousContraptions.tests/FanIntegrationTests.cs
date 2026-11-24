using System.Buffers.Binary;
using System.Numerics;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class FanIntegrationTests
{

    [Theory]
    [InlineData(Supply.SelfContained, true)]
    [InlineData(Supply.SelfContained, false)]
    [InlineData(Supply.Electrical, true)]
    [InlineData(Supply.Electrical, true, false)]
    [InlineData(Supply.Electrical, false, false)]
    [InlineData(Supply.Electrical, false)]
    [InlineData(Supply.Shaft, true)]
    [InlineData(Supply.Shaft, true, false)]
    [InlineData(Supply.Shaft, false, false)]
    [InlineData(Supply.Shaft, false)]
    public async Task ExposedPayloadLifecyclePreservesPaidStateAndRestoresAuthoredInputs(Supply mode, bool payable, bool powered = true)
    {
        var fan = Fan(mode, powered: powered && (mode != Supply.SelfContained || payable));
        var parameters = fan.Parameters.ToDictionary();
        parameters[new("force")] = .01f;
        fan = fan with { Parameters = new(parameters) };
        var ball = TennisBallTests.Ball(id: 2, x: 2, y: 5, drag: 0);
        var battery = PlacedParts.Battery(new(3), -6, 5, 0);
        var motor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("motor"), new(4), -3, 5, 0, 0, 0, 0, 1);
        var parts = new List<IWorkshopInstance> { fan, ball };
        var links = new List<WorkshopConnection>();
        if (mode == Supply.Electrical)
        {
            parts.Add(battery);
            if (payable) links.Add(new(battery.Id, PortId.Supply, fan.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical));
        }
        if (mode == Supply.Shaft)
        {
            parts.Add(motor);
            if (payable) links.Add(new(motor.Id, PortId.Drive, fan.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical));
        }
        var construction = TennisBallTests.Scene(parts.ToArray()) with { Connections = new(links.ToArray()) };
        await using var h = await WorkshopIntegrationHarness.Initialized(construction.Settings);
        var entry = TestBundle.Entry(new("fan"));
        // A declared elastic coordinate supplies the shaft fixture's finite .5 J;
        // the disconnected Motor has no electrical route or powered drive.
        if (mode == Supply.Shaft)
            entry = entry with { Mechanical = entry.Mechanical! with { Hinges = entry.Mechanical.Hinges.Select(hinge =>
                hinge with { Travel = CuriousContraptions.Abstractions.Mechanical.RevoluteTravel.Limited, InitialAngle = CuriousContraptions.Abstractions.Mechanical.MechanicalValue.Fixed(180f / MathF.PI * .5f),
                    Stiffness = CuriousContraptions.Abstractions.Mechanical.MechanicalValue.Fixed(4f) }).ToArray() } };
        using var physicsScope = DeclaredPhysics.Scoped([entry]);
        await h.Require(WorkshopCommandKind.Construct, construction);
        var initial = h.Committed;
        await h.Run();
        await h.Advance(4);
        var work = h.Committed.Airflow[0].Work.Value;
        Assert.Equal(payable, work > 0);
        Assert.Equal(payable, Body(h, 2).Velocity.X > 0);
        if (mode == Supply.Shaft) Assert.InRange(work, 0, .505f);
        await h.Require(WorkshopCommandKind.Pause);
        var paused = h.Committed;
        Assert.Equal(WorkshopRejection.WrongPhase, (await h.AdvanceOne()).Reason);
        Assert.Equal(paused.Tick, h.Committed.Tick);
        Assert.True(paused.Airflow.HasSameValues(h.Committed.Airflow));
        AssertStoreAccounting(paused.EnergyStores, h.Committed.EnergyStores);
        AssertSourceAccounting(paused.Electrical, h.Committed.Electrical);
        Assert.Equal(paused.Bodies[0], h.Committed.Bodies[0]);
        await h.Require(WorkshopCommandKind.Step);
        Assert.Equal(paused.Tick.Value + 1, h.Committed.Tick.Value);
        Assert.Equal(payable, h.Committed.Airflow[0].Work.Value > work);
        var postStepWork = h.Committed.Airflow[0].Work.Value;
        await h.Require(WorkshopCommandKind.Resume);
        await h.Advance(4);
        Assert.Equal(payable, h.Committed.Airflow[0].Work.Value > postStepWork);
        await h.Reset();
        AssertStoreAccounting(initial.EnergyStores, h.Committed.EnergyStores);
        AssertSourceAccounting(initial.Electrical, h.Committed.Electrical);
        Assert.Equal(initial.Presentation.Actuators.ToArray(), h.Committed.Presentation.Actuators.ToArray());
        Assert.Equal(0, h.Committed.Airflow[0].Work.Value);
        var saved = new WorkshopSavedConstruction(construction, new(5));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements,
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
        await h.DisposeAsync();
        await using var fresh = await WorkshopIntegrationHarness.Initialized(loaded.Construction.Settings);
        using var freshPhysics = DeclaredPhysics.Scoped([entry]);
        await fresh.Require(WorkshopCommandKind.Construct, loaded.Construction);
        Assert.Equal(construction, fresh.Construction);
        AssertStoreAccounting(initial.EnergyStores, fresh.Committed.EnergyStores);
        AssertSourceAccounting(initial.Electrical, fresh.Committed.Electrical);
        Assert.Equal(initial.Presentation.Actuators.ToArray(), fresh.Committed.Presentation.Actuators.ToArray());
        Assert.Equal(0, fresh.Committed.Airflow[0].Work.Value);
        await fresh.Run(); await fresh.Advance(4);
        Assert.Equal(payable, fresh.Committed.Airflow[0].Work.Value > 0);
        Assert.Equal(payable, Body(fresh, 2).Velocity.X > 0);
    }



    [Theory]
    [InlineData(Supply.SelfContained, true, false)]
    [InlineData(Supply.SelfContained, false, true)]
    [InlineData(Supply.Electrical, true, true)]
    [InlineData(Supply.Electrical, false, false)]
    public async Task ThousandSharedConsumersCommitPaidWorkThroughInstalledValidation(Supply mode, bool enabled, bool reverse)
    {
        var fan = Fan(mode, enabled);
        var parameters = fan.Parameters.ToDictionary();
        parameters[new("force")] = 4;
        fan = fan with { Parameters = new(parameters) };
        var ball = TennisBallTests.Ball(id: 2, x: 2, y: 5, drag: 0);
        var battery = LargeTestBattery(48000, enabled);
        var construction = TennisBallTests.Scene(fan, ball, battery) with
        {
            Connections = mode == Supply.Electrical
                ? new([new(battery.Id, PortId.Supply, fan.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical)]) : new([])
        };
        await using var h = await WorkshopIntegrationHarness.Initialized(construction.Settings);
        var entry = TestBundle.Entry(new("fan"));
        var sources = Enumerable.Range(0, 1000).Select(i => entry.Mechanical!.AirflowSources[0] with
            { Id = new((uint)(1 + i * 3)) }).ToArray();
        if (reverse) Array.Reverse(sources);
        entry = entry with
        {
            Mechanical = entry.Mechanical! with { AirflowSources = sources },
            Electrical = entry.Electrical! with { SupplyGraph = entry.Electrical.SupplyGraph! with
                { Nodes = [new SupplyConsumerDescriptor(new("consumer"), sources.Select(s =>
                    (SupplyTarget)new SupplyContinuousWorkTarget(s.Id)).ToArray())] } }
        };
        var payload = TestBundle.Entry(new("tennis"));
        payload = payload with { Mechanical = payload.Mechanical! with
            { MassFrame = MassFrameDeclaration.Constant(1000, Vector3.Zero, new(25), Quaternion.Identity, 0, Vector3.Zero) } };
        using var declared = DeclaredPhysics.Scoped([entry, payload, LargeBatteryEntry(48000)]);
        await h.Require(WorkshopCommandKind.Construct, construction);
        await h.Run();
        await h.Advance(1);
        var transactions = mode == Supply.Electrical ? h.Committed.Electrical[0].Transactions :
            h.Committed.EnergyStores[0].Transactions;
        Assert.Equal(enabled ? 4000u : 0u, transactions);
        Assert.Equal(enabled, Body(h, 2).Velocity.X > 0);
        var paid = Enumerable.Range(0, h.Committed.Airflow.Count).Sum(i => (double)h.Committed.Airflow[i].CommitWork.Value);
        Assert.Equal(enabled, paid > 0);
        if (enabled)
        {
            // Authored 4000 N / 1000 kg acts for 1/120 s; appreciable guidance and ~400 J source work.
            Assert.InRange(Body(h, 2).Velocity.X, .02f, .04f);
            Assert.InRange(paid, 390, 401);
            var remaining = mode == Supply.Electrical ? h.Committed.Electrical[0].Balance : h.Committed.EnergyStores[0].Balance;
            Assert.InRange(14400d - remaining.Bulk - remaining.Remainder - paid, -.05, .05);
            var prior = h.Committed;
            h.ChangeNextCandidate = world =>
            {
                var layout = PhysicsGpuAbi.Layout(world);
                var row = mode == Supply.Electrical ? layout.ElectricalSourceOffset(0) : layout.EnergyStoreOffset(0);
                var bulk = row + (mode == Supply.Electrical ? PhysicsGpuAbi.ElectricalSourceRemainingOffset : PhysicsGpuAbi.EnergyStoreBalanceOffset);
                var remainder = row + (mode == Supply.Electrical ? PhysicsGpuAbi.ElectricalSourceRemainderOffset : PhysicsGpuAbi.EnergyStoreRemainderOffset);
                var unpaid = new FiniteEnergyBalance(BinaryPrimitives.ReadSingleLittleEndian(world.AsSpan(bulk)),
                    BinaryPrimitives.ReadSingleLittleEndian(world.AsSpan(remainder))).Credit(1, 14400);
                BinaryPrimitives.WriteSingleLittleEndian(world.AsSpan(bulk), unpaid.Bulk);
                BinaryPrimitives.WriteSingleLittleEndian(world.AsSpan(remainder), unpaid.Remainder);
            };
            var refused = await h.AdvanceOne();
            Assert.Equal(WorkshopRejection.InvalidRead, refused.Reason);
            Assert.Equal(prior.Tick, h.Committed.Tick);
            Assert.Equal(prior.Revision, h.Committed.Revision);
            AssertSourceAccounting(prior.Electrical, h.Committed.Electrical);
            AssertStoreAccounting(prior.EnergyStores, h.Committed.EnergyStores);
            await h.Reset(); await h.Run(); await h.Advance(1);
            Assert.True(Body(h, 2).Velocity.X > 0);
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ThousandChargingPaymentsRespectTheActualPhaseBudgetAndRefuseExcess(bool enabled, bool reverse)
    {
        var fan = Fan(Supply.Electrical, powered: false);
        var battery = LargeTestBattery(1200, enabled);
        var construction = TennisBallTests.Scene(fan, battery) with
        { Connections = new([new(battery.Id, PortId.Supply, fan.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical)]) };
        await using var h = await WorkshopIntegrationHarness.Initialized(construction.Settings);
        var entry = TestBundle.Entry(new("fan"));
        var stores = Enumerable.Range(0, 1000).Select(i => new EnergyStoreDescriptor(new((uint)(3 * i + 11)), new(0),
            EnergyExpression.Direct(MechanicalValue.Fixed(10)), EnergyExpression.Direct(MechanicalValue.Fixed(0)),
            MechanicalValue.Fixed(1.2f))).ToArray();
        if (reverse) Array.Reverse(stores);
        entry = entry with
        {
            Mechanical = entry.Mechanical! with { AirflowSources = [], EnergyStores = stores },
            Presentation = entry.Presentation with { Bindings = [] },
            Electrical = entry.Electrical! with { SupplyGraph = entry.Electrical.SupplyGraph! with
                { Nodes = [new SupplyConsumerDescriptor(new("consumer"), stores.Select(s =>
                    (SupplyTarget)new SupplyEnergyStoreTarget(s.Id)).ToArray())] } }
        };
        using var declared = DeclaredPhysics.Scoped([entry, LargeBatteryEntry(1200)]);
        await h.Require(WorkshopCommandKind.Construct, construction);
        await h.Run();
        await h.Advance(1);
        var source = Assert.Single(Enumerable.Range(0, h.Committed.Electrical.Count).Select(i => h.Committed.Electrical[i]));
        Assert.Equal(enabled ? 1000u : 0u, source.Transactions);
        if (!enabled) { Assert.Equal(0, source.Debit.Value); return; }
        Assert.True(source.Debit.Value > 10, "Ordinary upward-rounded summary must exercise the former two-ULP budget rejection.");
        Assert.InRange(14400d - source.Balance.Bulk - source.Balance.Remainder, 9.99, 10.01);
        Assert.All(Enumerable.Range(0, h.Committed.EnergyStores.Count).Select(i => h.Committed.EnergyStores[i]), s => Assert.InRange(s.Energy.Value, .009f, .011f));
        var before = h.Committed;
        h.ChangeNextCandidate = AddUnfundedPhaseJoule;
        var refused = await h.AdvanceOne();
        Assert.Equal(WorkshopCommandOutcome.Faulted, refused.Outcome);
        Assert.Equal(WorkshopRejection.InvalidRead, refused.Reason);
        Assert.Contains("Electrical source exceeded its phase power allowance.",
            h.Simulation.Diagnostics.Snapshot().OfType<WorkshopFaultDiagnostic>().Last().Detail, StringComparison.Ordinal);
        Assert.Equal(before.Tick, h.Committed.Tick);
        Assert.Equal(before.Revision, h.Committed.Revision);
        AssertSourceAccounting(before.Electrical, h.Committed.Electrical);
        AssertStoreAccounting(before.EnergyStores, h.Committed.EnergyStores);
        // Invalid worker reads fault the session; the supported recovery is Reset, then Run.
        await h.Reset(); await h.Run(); await h.Advance(1);
        Assert.Equal(1000u, h.Committed.Electrical[0].Transactions);
        Assert.True(h.Committed.Electrical[0].Debit.Value > 0);
    }


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConnectedStoreCreditWithoutItsSourcePaymentIsAtomicallyRefused(bool enabled)
    {
        var fan = Fan(Supply.Electrical, powered: false);
        var battery = LargeTestBattery(1200, enabled);
        var construction = TennisBallTests.Scene(fan, battery) with
        { Connections = new([new(battery.Id, PortId.Supply, fan.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical)]) };
        await using var h = await WorkshopIntegrationHarness.Initialized(construction.Settings);
        var entry = TestBundle.Entry(new("fan"));
        entry = entry with
        {
            Mechanical = entry.Mechanical! with
            {
                AirflowSources = [],
                EnergyStores = [new(new(11), new(0), EnergyExpression.Direct(MechanicalValue.Fixed(100)),
                    EnergyExpression.Direct(MechanicalValue.Fixed(0)), MechanicalValue.Fixed(120))]
            },
            Presentation = entry.Presentation with { Bindings = [] },
            Electrical = entry.Electrical! with { SupplyGraph = entry.Electrical.SupplyGraph! with
                { Nodes = [new SupplyConsumerDescriptor(new("consumer"), [new SupplyEnergyStoreTarget(new(11))])] } }
        };
        using var declared = DeclaredPhysics.Scoped([entry, LargeBatteryEntry(1200)]);
        await h.Require(WorkshopCommandKind.Construct, construction);
        await h.Run(); await h.Advance(1);
        Assert.Equal(enabled, h.Committed.Electrical[0].Debit.Value > 0);
        Assert.Equal(enabled, h.Committed.EnergyStores[0].CommitSupplied.Value > 0);
        var before = h.Committed;
        h.ChangeNextCandidate = world =>
        {
            // One extra joule is coherent within the child's own ledger and capacity,
            // but has no source payment. Neither source balance nor phase budget changes.
            var row = PhysicsGpuAbi.Layout(world).EnergyStoreOffset(0);
            var balance = new FiniteEnergyBalance(Read(PhysicsGpuAbi.EnergyStoreBalanceOffset),
                Read(PhysicsGpuAbi.EnergyStoreRemainderOffset)).Credit(1, 100);
            Write(PhysicsGpuAbi.EnergyStoreBalanceOffset, balance.Bulk);
            Write(PhysicsGpuAbi.EnergyStoreRemainderOffset, balance.Remainder);
            Write(PhysicsGpuAbi.EnergyStoreSuppliedOffset, Read(PhysicsGpuAbi.EnergyStoreSuppliedOffset) + 1);
            Write(PhysicsGpuAbi.EnergyStoreCommitSuppliedOffset, Read(PhysicsGpuAbi.EnergyStoreCommitSuppliedOffset) + 1);
            var count = row + PhysicsGpuAbi.EnergyStoreTransactionsOffset;
            BinaryPrimitives.WriteUInt32LittleEndian(world.AsSpan(count),
                BinaryPrimitives.ReadUInt32LittleEndian(world.AsSpan(count)) + 1);
            float Read(int offset) => BinaryPrimitives.ReadSingleLittleEndian(world.AsSpan(row + offset));
            void Write(int offset, float value) => BinaryPrimitives.WriteSingleLittleEndian(world.AsSpan(row + offset), value);
        };
        var refused = await h.AdvanceOne();
        Assert.Equal(WorkshopCommandOutcome.Faulted, refused.Outcome);
        Assert.Equal(WorkshopRejection.InvalidRead, refused.Reason);
        Assert.Contains("Electrical debit does not fund its connected storage credits.",
            h.Simulation.Diagnostics.Snapshot().OfType<WorkshopFaultDiagnostic>().Last().Detail, StringComparison.Ordinal);
        Assert.Equal(before.Tick, h.Committed.Tick);
        Assert.Equal(before.Revision, h.Committed.Revision);
        AssertStoreAccounting(before.EnergyStores, h.Committed.EnergyStores);
        AssertSourceAccounting(before.Electrical, h.Committed.Electrical);
        await h.Reset(); await h.Run(); await h.Advance(1);
        Assert.Equal(enabled, h.Committed.Electrical[0].Debit.Value > 0);
        Assert.Equal(enabled, h.Committed.EnergyStores[0].CommitSupplied.Value > 0);
        Assert.Equal(enabled, h.Committed.EnergyStores[0].Energy.Value > 0);
    }

    private static WorkshopGenericInstance LargeTestBattery(float power, bool enabled) =>
        PlacedParts.Battery(new(3), -6, 5, 0) with
        {
            Parameters = new([new(new("capacity"), 14400), new(new("power_limit"), power),
                new(new("initial_charge"), 1), new(new("enabled"), enabled ? 1 : 0)])
        };

    private static BundleEntry LargeBatteryEntry(float maximumPower)
    {
        var entry = TestBundle.Entry(new("battery"));
        return entry with { Metadata = entry.Metadata with { Parameters = entry.Metadata.Parameters.Select(p =>
            p.Key == "power_limit" ? p with { Maximum = maximumPower } : p).ToArray() } };
    }

    private static void AssertStoreAccounting(PhysicsEnergyStoreRead expected, PhysicsEnergyStoreRead actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            var a = expected[i]; var b = actual[i];
            Assert.Equal(a.Id, b.Id); Assert.Equal(a.Owner, b.Owner);
            Assert.Equal(a.Balance.Bulk, b.Balance.Bulk); Assert.Equal(a.Balance.Remainder, b.Balance.Remainder);
            Assert.Equal(a.Supplied.Value, b.Supplied.Value); Assert.Equal(a.Released.Value, b.Released.Value);
            Assert.Equal(a.Dissipated.Value, b.Dissipated.Value);
            Assert.Equal(a.CommitSupplied.Value, b.CommitSupplied.Value); Assert.Equal(a.CommitReleased.Value, b.CommitReleased.Value);
            Assert.Equal(a.CommitDissipated.Value, b.CommitDissipated.Value); Assert.Equal(a.Transactions, b.Transactions);
        }
    }

    private static void AssertSourceAccounting(PhysicsElectricalRead expected, PhysicsElectricalRead actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            var a = expected[i]; var b = actual[i];
            Assert.Equal(a.Id, b.Id); Assert.Equal(a.Owner, b.Owner); Assert.Equal(a.Enabled, b.Enabled);
            Assert.Equal(a.Balance.Bulk, b.Balance.Bulk); Assert.Equal(a.Balance.Remainder, b.Balance.Remainder);
            Assert.Equal(a.Debit.Value, b.Debit.Value); Assert.Equal(a.Transactions, b.Transactions);
        }
    }

    private static void AddUnfundedPhaseJoule(byte[] world)
    {
        var layout = PhysicsGpuAbi.Layout(world);
        var source = layout.ElectricalSourceOffset(0);
        var remaining = new FiniteEnergyBalance(Read(source + PhysicsGpuAbi.ElectricalSourceRemainingOffset),
            Read(source + PhysicsGpuAbi.ElectricalSourceRemainderOffset)).Debit(1);
        Write(source + PhysicsGpuAbi.ElectricalSourceRemainingOffset, remaining.Bulk);
        Write(source + PhysicsGpuAbi.ElectricalSourceRemainderOffset, remaining.Remainder);
        Add(source + PhysicsGpuAbi.ElectricalSourceDebitOffset);
        var store = layout.EnergyStoreOffset(0);
        var balance = new FiniteEnergyBalance(Read(store + PhysicsGpuAbi.EnergyStoreBalanceOffset),
            Read(store + PhysicsGpuAbi.EnergyStoreRemainderOffset)).Credit(1, 10);
        Write(store + PhysicsGpuAbi.EnergyStoreBalanceOffset, balance.Bulk);
        Write(store + PhysicsGpuAbi.EnergyStoreRemainderOffset, balance.Remainder);
        Add(store + PhysicsGpuAbi.EnergyStoreSuppliedOffset);
        Add(store + PhysicsGpuAbi.EnergyStoreCommitSuppliedOffset);
        float Read(int offset) => BinaryPrimitives.ReadSingleLittleEndian(world.AsSpan(offset));
        void Write(int offset, float value) => BinaryPrimitives.WriteSingleLittleEndian(world.AsSpan(offset), value);
        void Add(int offset) => Write(offset, Read(offset) + 1);
    }

    [Fact]
    public async Task DeclaredFlowBindingSamplesQueuedInteriorsAndStopsWithItsActualSource()
    {
        var fan = Fan(Supply.Electrical, powered: false);
        var inactive = Fan(powered: false) with { Id = new(7), Cell = new(0, 1, 0) };
        var battery = PlacedParts.Battery(new(3), -6, 5, 0);
        var construction = TennisBallTests.Scene(fan, inactive, battery) with
        {
            Connections = new([new(battery.Id, PortId.Supply, fan.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical)])
        };
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        await h.Advance(2);
        var stopAt = new PhysicalTime(h.Committed.Tick.Value * 4);
        await h.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: h.RunControl(battery.Id, RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        await h.Advance(2);
        Assert.Equal(0, h.Committed.Airflow[0].FlowSpeed);
        float Sample(PhysicalTime time, GpuBodyId owner) => BitConverter.UInt32BitsToSingle(
            Assert.Single(h.Presentation!.Advance(time), x => x.Owner == owner).Value.CanonicalBits);
        // The declared mapping is 1.5 rad/m times the actual 12 m/s source, hence 18 rad/s.
        // These tolerances protect the rate integral interface, not a simulated trajectory.
        Assert.InRange(Sample(new PhysicalTime(2), fan.Id), .075f - 1e-6f, .075f + 1e-6f);
        Assert.Equal(0, Sample(new PhysicalTime(2), inactive.Id));
        Assert.InRange(Sample(new PhysicalTime(6), fan.Id), .225f - 1e-6f, .225f + 1e-6f);
        var stopped = 18f * stopAt.Ordinal / 480f;
        Assert.InRange(Sample(new PhysicalTime(10), fan.Id), stopped - 1e-6f, stopped + 1e-6f);
        Assert.InRange(Sample(new PhysicalTime(16), fan.Id), stopped - 1e-6f, stopped + 1e-6f);
        Assert.InRange(Sample(new PhysicalTime(20), fan.Id), stopped - 1e-6f, stopped + 1e-6f);
        await h.Reset();
        Assert.Equal(0, Sample(new PhysicalTime(0), fan.Id));
    }

    [Fact]
    public async Task IndependentSourcePhasesRejectStaleAndForeignObservationsAtomically()
    {
        var on = Fan();
        var off = Fan(powered: false) with { Id = new(7), Cell = new(0, WorkshopInput.Position(8).Cell, 0), Local = new(0, 0, 0) };
        await using var h = await WorkshopIntegrationHarness.Running(TennisBallTests.Scene(on, off));
        await h.Advance(8);
        var at = new PhysicalTime(h.Committed.Tick.Value * 4);
        var values = h.Presentation!.Advance(at);
        var active = Assert.Single(values, value => value.Owner == on.Id);
        var stopped = Assert.Single(values, value => value.Owner == off.Id);
        Assert.True(BitConverter.UInt32BitsToSingle(active.Value.CanonicalBits) > 0);
        Assert.Equal(0f, BitConverter.UInt32BitsToSingle(stopped.Value.CanonicalBits));
        Assert.Throws<ArgumentException>(() => h.Presentation.Accept(h.Latest));
        var observations = Enumerable.Range(0, h.Committed.Airflow.Count).Select(i => h.Committed.Airflow[i]).ToArray();
        observations[1] = observations[1] with { Owner = on.Id };
        var invalid = h.Latest with { Publication = new(h.Latest.Publication.Value + 1),
            Read = h.Committed with { Airflow = new(observations) } };
        Assert.Throws<ArgumentException>(() => h.Presentation.Accept(invalid));
        var after = h.Presentation.Advance(at);
        Assert.Equal(active, Assert.Single(after, value => value.Owner == on.Id));
        Assert.Equal(stopped, Assert.Single(after, value => value.Owner == off.Id));
        await h.Reset();
        Assert.All(h.Presentation!.Advance(new PhysicalTime(0)),
            value => Assert.Equal(0f, BitConverter.UInt32BitsToSingle(value.Value.CanonicalBits)));
    }


    [Theory]
    [InlineData(Supply.SelfContained, true)]
    [InlineData(Supply.SelfContained, false)]
    [InlineData(Supply.Shaft, true)]
    [InlineData(Supply.Shaft, false)]
    public async Task PassiveHubReceivesExternalContactWithoutEmittingUnpaidFlow(Supply mode, bool hitsHub)
    {
        // The ring is solid, so a ball reaches the hub only through its 0.49 m opening. The hitting ball (radius 0.1 m) starts at rest
        // inside the opening, 0.3 m above the hub's centre and 0.1 m off its axis, and falls onto the hub's top. The other starts
        // 1 m above the hub and 0.8 m off the axis, 0.17 m beyond the ring's 0.63 m outer edge, and falls past.
        var cargo = hitsHub ? TennisBallTests.Ball(2, 0, 5.3f, radius: .1f, z: .1f) : TennisBallTests.Ball(2, 0, 6, radius: .1f, z: .8f);
        await using var h = await WorkshopIntegrationHarness.Running(TennisBallTests.Scene(Fan(mode, powered: false), cargo));
        var moved = false;
        for (var tick = 0; tick < 120; tick++)
        {
            await h.Advance(1);
            var joint = h.Construction.Instances.Identities[new(1)].Hinges[0].Joint;
            var coordinate = Assert.Single(h.Committed.Presentation.Actuators.ToArray(), row => row.Drive.RevoluteJoint == joint);
            moved |= MathF.Abs(coordinate.Speed) > .1f;
            Assert.Equal(0f, h.Committed.Airflow[0].Work.Value);
        }
        Assert.Equal(hitsHub, moved);
        await h.Reset();
        Assert.All(h.Committed.Presentation.Actuators.ToArray(), row => Assert.Equal(0f, row.Coordinate));
    }

    /// <summary>The Fan's own post and ring are internal occluders: they do not shadow its stream (only the base, post, ring and hub are
    /// listed, and a collider that is not the Fan's own still does). A Tennis ball of radius 0.0625 m (the declared minimum) and no
    /// drag starts 4.2 m downstream directly behind the ring's tube (the cone's radius there is 0.714 m, and the tube spans 0.49 to
    /// 0.63 m from the axis), or 2.5 m downstream directly behind the post (cone radius 0.425 m). After 12 ticks (0.1 s) it has been
    /// pushed along the axis and the source has paid work. Without the exemption each sample reaching the ball starts inside the
    /// ring or the post and is shadowed, so both would be exactly zero. The control puts a default Wall, which is not the Fan's,
    /// at 2 m, z 0.435 to 0.685: it covers every streamline that could reach the first ball, and both stay exactly zero.</summary>
    [Theory]
    [InlineData(4.2f, 0f, .56f, false)]
    [InlineData(2.5f, -.3f, 0f, false)]
    [InlineData(4.2f, 0f, .56f, true)]
    public async Task TheFansOwnRingAndPostDoNotShadowItsStream(float x, float y, float z, bool behindAWall)
    {
        IWorkshopInstance[] parts = behindAWall
            ? [Fan(), TennisBallTests.Ball(2, x, 5 + y, radius: 1f / 16, drag: 0, z: z), WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wall"), new(3), 2, 5, .56f, 0, 0, 0, 1, WallFixture.Default)]
            : [Fan(), TennisBallTests.Ball(2, x, 5 + y, radius: 1f / 16, drag: 0, z: z)];
        await using var h = await WorkshopIntegrationHarness.Running(TennisBallTests.Scene(parts));
        await h.Advance(12);
        var (speed, work) = (Body(h, 2).Velocity.X, h.Committed.Airflow[0].Work.Value);
        if (behindAWall)
        {
            Assert.Equal(0f, speed);
            Assert.Equal(0f, work);
        }
        else
        {
            Assert.True(speed > 0, "the ball behind the Fan's own ring or post is pushed along the axis");
            Assert.True(work > 0, "the Fan pays work for the push");
        }
    }

    public enum Supply { SelfContained, Electrical, Shaft }
    internal static WorkshopGenericInstance Fan(Supply supply = Supply.SelfContained, bool powered = true) =>
        WorkshopInput.Generic(RuntimeTestServices.Bundle, new("fan"), new(1), 0, 5, 0, 0, 0, 0, 1,
            new([new(new("supply"), (float)supply), new(new("powered"), powered ? 1 : 0),
                new(new("force"), 9), new(new("reach"), 5), new(new("width"), .85f)]));

    private static CanonicalBody Body(WorkshopIntegrationHarness h, ulong id)
    {
        Assert.True(h.Committed.Bodies.TryGet(new(id), out var value));
        return value.Body;
    }

    [Theory]
    [InlineData(Supply.SelfContained)]
    [InlineData(Supply.Electrical)]
    [InlineData(Supply.Shaft)]
    public void SelectedModeCompilesOneWorkInputAndOneBladeWriterWithoutHiddenStores(Supply mode)
    {
        var construction = TennisBallTests.Scene(Fan(mode));
        var scene = CompiledSceneFixture.Compile(construction);
        var source = Assert.Single(scene.Physics.AirflowSources.ToArray());
        var expected = mode switch
        {
            Supply.SelfContained => ContinuousWorkInputKind.Store,
            Supply.Electrical => ContinuousWorkInputKind.Electrical,
            Supply.Shaft => ContinuousWorkInputKind.Coordinate,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        Assert.Equal(expected, source.Input.Kind);
        Assert.Equal(4, source.InternalOccluders.Length);
        var hinge = Assert.Single(scene.Physics.Revolutes.ToArray());
        var hub = Assert.Single(scene.Physics.Bodies.ToArray(), body => body.Id == hinge.BodyB);
        Assert.Equal(.15f, hub.Mass.Value);
        Assert.Equal(.001176f, hub.MassProperties.X.Moment);
        if (mode == Supply.SelfContained) Assert.Equal(14400f, Assert.Single(scene.Physics.EnergyStores.ToArray()).InitialEnergy.Value);
        else Assert.Empty(scene.Physics.EnergyStores.ToArray());
        var track = Assert.Single(scene.Presentation.ToArray());
        Assert.Equal(mode == Supply.Shaft ? PresentationDriver.CommittedRevoluteAngle : PresentationDriver.CommittedFlowSpeed,
            track.Declaration.Driver);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer(bool powered, bool vacuum)
    {
        var construction = TennisBallTests.Scene(Fan(powered: powered), TennisBallTests.Ball(id: 2, x: 2, y: 5, drag: 0));
        if (vacuum) construction = construction with { Atmosphere = AtmospherePreset.Vacuum };
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        await h.Advance(12);
        var source = h.Committed.Airflow[0];
        if (powered && !vacuum)
        {
            // Over a tenth-second clear exposure the ball must acquire gameplay-significant lateral speed.
            Assert.True(Body(h, 2).Velocity.X > 1);
            Assert.True(source.Work.Value > 1);
            Assert.True(h.Committed.EnergyStores[0].Energy.Value < 14400);
        }
        else
        {
            Assert.Equal(0f, Body(h, 2).Velocity.X);
            Assert.Equal(0f, source.Work.Value);
            Assert.Equal(0f, source.FlowSpeed);
            Assert.Equal(14400f, h.Committed.EnergyStores[0].Energy.Value);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy(bool wired)
    {
        var battery = PlacedParts.Battery(new(3), -3, 5, 0);
        var construction = TennisBallTests.Scene(Fan(Supply.Electrical), TennisBallTests.Ball(id: 2, x: 2, y: 5), battery);
        if (wired) construction = construction with
        {
            Connections = new(new WorkshopConnection(battery.Id, PortId.Supply, new(1), PortId.PowerIn, WorkshopConnectionDomain.Electrical))
        };
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        var initial = h.Committed.Electrical[0].Remaining.Value;
        await h.Advance(12);
        Assert.Equal(0, h.Committed.EnergyStores.Count);
        var work = h.Committed.Airflow[0].Work.Value;
        var debit = initial - h.Committed.Electrical[0].Remaining.Value;
        if (wired)
        {
            Assert.True(Body(h, 2).Velocity.X > 1);
            Assert.True(work > 1);
            // No unpaid output; representable debits must not shorten battery-guidance duration by over 1%.
            Assert.InRange(debit, work, work * 1.01f);
        }
        else
        {
            Assert.Equal(0f, Body(h, 2).Velocity.X);
            Assert.Equal(0f, work);
            Assert.Equal(0f, debit);
        }
    }

    [Theory]
    [InlineData(true, 6f, true)]
    [InlineData(true, 6f, false)]
    [InlineData(false, 6f, true)]
    [InlineData(true, 0f, true)]
    public async Task MotorLinkUsesDeclaredInputSenseAndActualShaftTravel(bool connected, float speed, bool exposed)
    {
        var motor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("motor"), new(3), -3, 5, 0, 0, 0, 0, 1,
            new([new(new("speed"), speed), new(new("torque"), 20)]));
        var battery = PlacedParts.Battery(new(4), -6, 5, 0);
        var links = new List<WorkshopConnection>
        {
            new(battery.Id, PortId.Supply, motor.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical)
        };
        if (connected) links.Add(new(motor.Id, PortId.Drive, new(1), PortId.DriveIn, WorkshopConnectionDomain.Mechanical));
        await using var h = await WorkshopIntegrationHarness.Running(
            TennisBallTests.Scene(Fan(Supply.Shaft), TennisBallTests.Ball(id: 2, x: exposed ? 2 : -2, y: 5, drag: 0), motor, battery)
                with { Connections = new(links.ToArray()) });
        await h.Advance(12);
        var source = h.Committed.Airflow[0];
        var coordinate = Assert.Single(h.Committed.Presentation.Actuators.ToArray(), row =>
            row.Drive.RevoluteJoint == h.Construction.Instances.Identities[new(1)].Hinges[0].Joint);
        if (connected && speed > 0 && exposed)
        {
            Assert.True(source.Work.Value > 1);
            // From rest, this constant-work load can spend the Motor's spin-up work and hold it below
            // target. Require useful positive transfer and the independent u*m*dv work identity.
            Assert.True(Body(h, 2).Velocity.X > .1f);
            Assert.InRange(source.Work.Value - 12f * .35f * Body(h, 2).Velocity.X, -.01f, .01f);
            Assert.True(coordinate.Coordinate < -.1f);
            Assert.True(MathF.Abs(coordinate.Speed) < speed * .98f);
        }
        else
        {
            Assert.Equal(0f, source.Work.Value);
            Assert.Equal(0f, Body(h, 2).Velocity.X);
        }
        if (connected && speed > 0 && !exposed)
            Assert.InRange(coordinate.Speed, -speed * 1.02f, -speed * .98f); // Existing Motor speed envelope.
        var presentation = Assert.Single(h.Presentation!.Advance(new PhysicalTime(h.Committed.Tick.Value * 4)),
            value => value.Owner == new GpuBodyId(1));
        Assert.Equal(coordinate.Coordinate, BitConverter.UInt32BitsToSingle(presentation.Value.CanonicalBits));
        // The current Motor intentionally has no electrical work drain; this proves shaft loading only.
        Assert.Equal(0f, h.Committed.Electrical[0].Debit.Value);
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public async Task CommittedFlowPhaseHoldsOnPauseStepsOnceAndResetsWithoutUnexposedDebit(SimulationCadence cadence)
    {
        var settings = WorkshopCadenceSettings.Default() with { Simulation = cadence };
        await using var h = await WorkshopIntegrationHarness.Running(TennisBallTests.Scene(Fan()) with { Settings = settings });
        await h.Advance(30);
        float Phase() => BitConverter.UInt32BitsToSingle(Assert.Single(h.Presentation!.Advance(
            new PhysicalTime(h.Committed.Tick.Value * h.Construction.Settings.PhysicalStepsPerCommit))).Value.CanonicalBits);
        var expected = 18f * (h.Committed.Tick.Value * settings.PhysicalStepsPerCommit / 480f) % MathF.Tau;
        Assert.InRange(Phase() - expected, -.0001f, .0001f); // declared rate integration, not a physical trajectory
        Assert.Equal(0f, h.Committed.Airflow[0].Work.Value);
        Assert.Equal(14400f, h.Committed.EnergyStores[0].Energy.Value);
        await h.Require(WorkshopCommandKind.Pause);
        var paused = Phase();
        Assert.Equal(paused, Phase());
        await h.Require(WorkshopCommandKind.Step);
        var stepAngle = 18f * settings.PhysicalStepsPerCommit / 480f;
        Assert.InRange(Phase() - paused, stepAngle - .0001f, stepAngle + .0001f);
        await h.Reset();
        Assert.Equal(0f, Phase());
        Assert.Equal(14400f, h.Committed.EnergyStores[0].Energy.Value);
    }
}

[Collection<NativeSceneCollectionDefinition>]
public sealed class FanArtworkTests(NativeSceneFixture godot)
{
    /// <summary>The flow arrow is flat printing on the base's top face (owner decision, 21 November 2026), not a free-standing solid in the
    /// air: the old Arrow and Tip are gone, and the three surfaces of the new mark lie between 0 and 0.5 mm above the face (the
    /// bound the printed marks of FaceMarkingTests use), facing up. Read on the X, Z projection, ink covers (0.29, 0) at the tip,
    /// and (0.25, 0.04) on a head stroke 4 cm beside the axis, and not (0.15, 0.04), a point beside the shaft where a reversed
    /// arrow's head would be. A headless print leaves (0.25, 0.04) bare; a reversed one covers (0.15, 0.04).</summary>
    [Fact]
    public void TheFlowArrowIsPrintedOnTheBaseAndPointsAlongTheStream()
    {
        var root = WorkshopResources.LoadScene(TestBundle.Entry(new("fan")).Metadata.ScenePath).Instantiate<Godot.Node3D>();
        try
        {
            Assert.Null(root.GetNodeOrNull("Visual/Arrow"));
            Assert.Null(root.GetNodeOrNull("Visual/Tip"));
            var faceTop = VisibleSolidColliderTests.VerticesInRoot(root, root.GetNode<Godot.MeshInstance3D>("Visual/Base")).Max(v => v.Y);
            var surfaces = root.GetNode("Visual/FlowMark").GetChildren().Cast<Godot.MeshInstance3D>().ToArray();
            Assert.Equal(3, surfaces.Length);
            var ink = new List<Godot.Vector3>();
            foreach (var surface in surfaces)
            {
                var vertices = VisibleSolidColliderTests.VerticesInRoot(root, surface);
                Assert.All(vertices, v => Assert.InRange(v.Y - faceTop, 0f, .0005f));
                VisibleSolidColliderTests.AssertFacesUp(vertices, surface.Name);
                ink.AddRange(vertices);
            }
            Assert.True(Covers(ink, .29f, 0), "the arrow's tip is missing");
            Assert.True(Covers(ink, .25f, .04f), "the arrow has no head stroke");
            Assert.False(Covers(ink, .15f, .04f), "the arrow's head is reversed");
        }
        finally { root.Free(); }
    }

    /// <summary>Whether a triangle of the surfaces covers the point (x, z) on the X, Z projection.</summary>
    private static bool Covers(List<Godot.Vector3> triangles, float x, float z)
    {
        for (var i = 0; i < triangles.Count; i += 3)
        {
            float Side(Godot.Vector3 p, Godot.Vector3 q) => (q.X - p.X) * (z - p.Z) - (q.Z - p.Z) * (x - p.X);
            var (first, second, third) = (Side(triangles[i], triangles[i + 1]), Side(triangles[i + 1], triangles[i + 2]), Side(triangles[i + 2], triangles[i]));
            if ((first >= 0 && second >= 0 && third >= 0) || (first <= 0 && second <= 0 && third <= 0)) return true;
        }
        return false;
    }

    [Fact]
    public async Task NativeBladesUseCommittedPhaseAndPrivateArtworkRestoresOnReset()
    {
        var registry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        registry.Load();
        var root = new Godot.Node3D();
        var first = registry.Create(new("fan"));
        // Each private registry load owns its artwork resources; immutable instances within one load may share meshes.
        var otherRegistry = new PartRegistry(RuntimeTestServices.Core.Presentation, RuntimeTestServices.Bundle);
        otherRegistry.Load();
        var second = otherRegistry.Create(new("fan"));
        await using var h = await WorkshopIntegrationHarness.Running(TennisBallTests.Scene(FanIntegrationTests.Fan()));
        try
        {
            godot.Tree.Root.AddChild(root);
            root.AddChild(first); root.AddChild(second);
            first.EnsureConstructed(); second.EnsureConstructed();
            var blade = first.GetNode<Godot.MeshInstance3D>("Visual/BladeAxis/Blades/Blade0");
            var other = second.GetNode<Godot.MeshInstance3D>("Visual/BladeAxis/Blades/Blade0");
            Assert.NotEqual(blade.Mesh.GetInstanceId(), other.Mesh.GetInstanceId());
            Assert.NotEqual(blade.GetActiveMaterial(0).GetInstanceId(), other.GetActiveMaterial(0).GetInstanceId());
            Assert.Equal(.14f, Assert.IsType<Godot.SphereShape3D>(first.GetNode<Godot.CollisionShape3D>("Visual/Hub/col_hub").Shape).Radius);
            var blades = first.GetNode<Godot.Node3D>("Visual/BladeAxis/Blades");
            var otherBlades = second.GetNode<Godot.Node3D>("Visual/BladeAxis/Blades");
            var rest = blades.Transform;
            await h.Advance(12);
            foreach (var sample in h.Presentation!.Advance(new PhysicalTime(h.Committed.Tick.Value * 4)))
                first.ApplyDeclaredProperty(sample.Binding, sample.Value);
            Assert.True(MathF.Abs(blades.Rotation.Z) > .1f);
            Assert.Equal(rest, otherBlades.Transform);
            await h.Reset();
            first.ResetPhysicalVisuals();
            Assert.Equal(rest, blades.Transform);
            Assert.Empty(godot.Errors.Drain());
        }
        finally
        {
            first.GetParent()?.RemoveChild(first); first.Free();
            second.GetParent()?.RemoveChild(second); second.Free();
            root.GetParent()?.RemoveChild(root); root.Free();
        }
    }
}
