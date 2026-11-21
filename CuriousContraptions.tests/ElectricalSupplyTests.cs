using CuriousContraptions.Gpu;
using System.Buffers.Binary;

namespace CuriousContraptions.Tests;

public sealed class ElectricalSupplyTests
{
    private static readonly float[] ProportionalShares = [.25f, .75f, 0f];
    private static readonly float[] UntouchedOutput = [123f, 456f];

    [Fact]
    public void ImmediateWorkSharesChargingAllowanceWithoutBecomingStoredEnergy()
    {
        float[] grants = new float[2];
        ElectricalSupply.Reserve(ElectricalEnable.Enabled, 1.5f, [1f, 2f], grants);
        Assert.Equal(.5f, grants[0]); Assert.Equal(1f, grants[1]);
        var source = new FiniteEnergyBalance(4, 0).Debit(grants[0]);
        Assert.Equal(3.5f, source.Available); // Only the accepted charge spends energy.
        ElectricalSupply.Reserve(ElectricalEnable.Enabled, 1, [2f, 2f], grants);
        Assert.Equal(.5f, grants[0]); Assert.Equal(.5f, grants[1]);
        Assert.Equal(3.5f, source.Available); // Reservations alone are not payments.
    }

    [Fact]
    public void ImmediateReservationsRefuseBalancesAtomicallyAndDoNotCreatePower()
    {
        float[] grants = [123f, 456f];
        Assert.Throws<ArgumentException>(() => ElectricalSupply.Reserve(ElectricalEnable.Enabled, 1.5f, [1f, -1f], grants));
        Assert.Equal(UntouchedOutput, grants);
        ElectricalSupply.Reserve(ElectricalEnable.Disabled, 1.5f, [1f, 2f], grants);
        Assert.All(grants, x => Assert.Equal(0, x));
        ElectricalSupply.Reserve(ElectricalEnable.Enabled, 0, [1f, 2f], grants);
        Assert.All(grants, x => Assert.Equal(0, x));
    }

    [Theory]
    [InlineData(ElectricalEnable.Disabled)]
    [InlineData(ElectricalEnable.Enabled)]
    public void F32AuthoringWiringAndSaveRoundTripWithAtomicAmbiguousSourceRejection(ElectricalEnable enabled)
    {
        var battery = PlacedParts.Battery(new(1), 0, 0, 0, 3600.0002f, 120.00001f, .1234567f, enabled);
        var bumper = WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("bumper"), new(2), 3, 4, 0, 0, 0, 0, 1);
        var link = new WorkshopConnection(battery.Id, PortId.Supply, bumper.Id, PortId.PowerIn, WorkshopConnectionDomain.Electrical);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(battery, bumper), Connections: new(link));
        construction.Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle);
        var saved = new WorkshopSavedConstruction(construction, new(3));
        var original = WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved);
        // An enable index that names no declared option, or a fraction of one, is refused (built through the schema, not a byte
        // offset): the declared Choice admits exactly 0 (Disabled) and 1 (Enabled).
        foreach (var undefined in new[] { 2f, .5f, -1f })
        {
            var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
            var values = battery.Parameters.ToDictionary(); values[new("enabled")] = undefined;
            var record = SaveFixture.Record(builder, construction with
                { Instances = new(battery with { Parameters = new(values) }, bumper) });
            var savedBytes = SaveFixture.Finish(builder, record, 3);
            SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, savedBytes));
        }
        var restored = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements,original);
        Assert.Equal(battery, Assert.IsType<WorkshopGenericInstance>(restored.Construction.Instances[0]));
        Assert.Equal(saved, restored);
        Assert.Equal(new GpuBodyId(3), restored.NextBodyId);
        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, restored.Construction, new(1, 2));
        Assert.Single(scene.Electrical.Bindings.ToArray());
        Assert.Equal(0, WorkshopActivationCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, restored.Construction).Clear().Count);
        // The compiled store is the declared parameters' f32 values, bit for bit; the initial energy is capacity × charge in f32.
        Assert.Equal(BitConverter.SingleToInt32Bits(3600.0002f), BitConverter.SingleToInt32Bits(scene.Electrical.Sources[0].Capacity.Value));
        Assert.Equal(BitConverter.SingleToInt32Bits(120.00001f), BitConverter.SingleToInt32Bits(scene.Electrical.Sources[0].MaximumPower.Value));
        Assert.Equal(BitConverter.SingleToInt32Bits(.1234567f), BitConverter.SingleToInt32Bits(scene.Electrical.Sources[0].InitialFraction));
        Assert.Equal(BitConverter.SingleToInt32Bits(3600.0002f * .1234567f), BitConverter.SingleToInt32Bits(scene.Electrical.Sources[0].InitialEnergy.Value));
        Assert.Equal(enabled, scene.Electrical.Sources[0].Enabled);
        var second = battery with { Id = new(3) };
        var ambiguous = construction.WithInstance(RuntimeTestServices.Bundle, second) with { Connections = new(link, link with { Source = second.Id }) };
        EditRefusal.Is(WorkshopEditRejection.PowerInputSupplied, () => ambiguous.Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle));
        var wrongDomain = construction with { Connections = new(link with { Domain = WorkshopConnectionDomain.Activation }) };
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => wrongDomain.Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle));
        var afterRefusals = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements,
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
        Assert.Equal(saved, afterRefusals); // Independently authored content survives both rejected edits.
        Assert.Equal(restored, afterRefusals);
        Assert.Equal(new GpuBodyId(3), afterRefusals.NextBodyId);
    }

    [Theory]
    [InlineData(12, 256u)]
    [InlineData(12, 257u)]
    [InlineData(36, 256u)]
    [InlineData(36, 257u)]
    [InlineData(8, uint.MaxValue)]
    public void RawGpuFieldsRejectBeforeNarrowing(int field, uint invalid)
    {
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(PlacedParts.Battery(new(1), 0, 0, 0)));
        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var initial = PhysicsGpuAbi.ReadElectrical(bytes);
        Assert.Equal(3600f, initial[0].Remaining.Value);
        Assert.True(initial[0].Available);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(scene.World.ElectricalSourceOffset(0) + field), invalid);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadElectrical(bytes));
    }

    [Theory]
    [InlineData(0f, ElectricalEnable.Enabled)]
    [InlineData(3600f, ElectricalEnable.Disabled)]
    public void UnavailableSourcePreservesBothStores(float source, ElectricalEnable enabled)
    {
        float[] grants = new float[2];
        var first = new FiniteEnergyBalance(0, 0); var second = new FiniteEnergyBalance(4, 0);
        ElectricalSupply.Reserve(enabled, MathF.Min(source, 1), [32f, 28f], grants);
        Assert.All(grants, x => Assert.Equal(0, x));
        Assert.Equal(first, first.Credit(grants[0], 32));
        Assert.Equal(second, second.Credit(grants[1], 32));
    }

    [Theory]
    [InlineData(30f, 5f)]
    [InlineData(20000f, 1200f)]
    public void UnfamiliarSourcesShareTheSameFiniteAllocationLaw(float energy, float power)
    {
        new ElectricalSourceDeclaration(new(1), new(2), new(energy), new(power), 1f, ElectricalEnable.Enabled).Validate();
        float[] grants = new float[2];
        ElectricalSupply.Reserve(ElectricalEnable.Enabled, MathF.Min(energy, power * ElectricalSupply.PhaseSeconds), [32f, 0f], grants);
        Assert.True(grants[0] > 0); Assert.Equal(0, grants[1]);
        Assert.InRange(grants[0], 0, power * ElectricalSupply.PhaseSeconds);
        var remaining = new FiniteEnergyBalance(energy, 0).Debit(grants[0]);
        var credited = new FiniteEnergyBalance(0, 0).Credit(grants[0], 32);
        Assert.Equal(grants[0], credited.Available);
        Assert.InRange(energy - (double)remaining.Bulk - remaining.Remainder - grants[0],
            0, FiniteEnergyBalance.LossBound(energy, 0, grants[0], 0, 1));
        if (power == 1200f) Assert.InRange(grants[0], 10f, 10f + PhysicsReadValidation.Residual(10f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OversizedRequestedTotalStillSharesAFiniteAllowanceAndFullRecipientsRequestNothing(bool full)
    {
        float[] requests = full ? [0f, 0f] : [float.MaxValue, float.MaxValue];
        float[] grants = [123f, 456f];
        ElectricalSupply.Reserve(ElectricalEnable.Enabled, 1, requests, grants);
        if (full) Assert.All(grants, x => Assert.Equal(0, x));
        else
        {
            // Equal requests share a finite joule even though their mathematical sum exceeds scalar range.
            Assert.InRange(grants[0], .499999f, .5f);
            Assert.Equal(grants[0], grants[1]);
            Assert.True(grants[0] + grants[1] <= 1);
        }
    }

    [Fact]
    public void EqualFourLoadDemandSharesOneSimdBudget()
    {
        float[] grants = new float[4];
        ElectricalSupply.Reserve(ElectricalEnable.Enabled, 1, [32f, 32f, 32f, 32f], grants);
        Assert.All(grants, value => Assert.Equal(.25f, value));
        Assert.Equal(1f, grants.Sum());
    }

    [Fact]
    public void FinalPositiveSourceBalanceTransfersBeforeAvailabilityClears()
    {
        float[] grants = new float[1];
        var source = new FiniteEnergyBalance(.125f, 0);
        ElectricalSupply.Reserve(ElectricalEnable.Enabled, source.Available, [32f], grants);
        Assert.Equal(.125f, grants[0]);
        Assert.Equal(default, source.Debit(grants[0]));
    }

    [Theory]
    [InlineData(44.9999f, 45f, 120f, true)]
    [InlineData(0f, .000001f, 120f, true)]
    [InlineData(44.9999f, 45f, .001f, false)]
    public void SmallTerminalDemandRequiresAnAffordableRealSourceDebit(float balance, float limit, float power, bool affordable)
    {
        var recipient = new FiniteEnergyBalance(balance, 0);
        var source = new FiniteEnergyBalance(3600, 0);
        float[] grants = new float[1];
        ElectricalSupply.Reserve(ElectricalEnable.Enabled, power * ElectricalSupply.PhaseSeconds,
            [recipient.Headroom(limit)], grants);
        Assert.True(grants[0] > 0); // A tiny phase budget still funds partial progress, without a new force/payment cutoff.
        var remaining = source.Debit(grants[0]);
        var credited = recipient.Credit(grants[0], limit);
        Assert.True(remaining.DecreaseTo(source) < 0);
        Assert.True(credited.DecreaseTo(recipient) > 0);
        Assert.InRange(grants[0], 0, power * ElectricalSupply.PhaseSeconds);
        Assert.Equal(affordable ? 0 : -1, credited.CompareTo(limit));
    }

    [Fact]
    public void CrossingRecipientBinadeDoesNotRoundCreditAboveFundedGrant()
    {
        var before = new FiniteEnergyBalance(MathF.BitDecrement(16f), 0);
        float[] grants = new float[2];
        ElectricalSupply.Reserve(ElectricalEnable.Enabled, 1, [16f, 16f], grants);
        Assert.All(grants, grant => Assert.Equal(.5f, grant));
        var after = before.Credit(grants[0], 32);
        var credited = after.Bulk + (double)after.Remainder - before.Bulk;
        Assert.InRange(credited, .499999, .5); // Local half-joule credit, including its low remainder.
    }

    [Fact]
    public void UnequalDemandSharesProportionallyAndFullLoadReceivesNothing()
    {
        float[] grants = new float[3];
        ElectricalSupply.Reserve(ElectricalEnable.Enabled, 1, [1f, 3f, 0f], grants);
        Assert.Equal(ProportionalShares, grants);
    }

    [Fact]
    public void AlmostFullLoadRetainsUnrequestedSourceWork()
    {
        float[] grants = new float[1];
        ElectricalSupply.Reserve(ElectricalEnable.Enabled, 1, [.25f], grants);
        Assert.Equal(.25f, grants[0]);
        Assert.Equal(32, new FiniteEnergyBalance(31.75f, 0).Credit(grants[0], 32).Available);
        Assert.Equal(3599.75f, new FiniteEnergyBalance(3600, 0).Debit(grants[0]).Available);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(-1f)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidSourceRejectsBeforeWriting(float balance)
    {
        float[] grants = [123f];
        Assert.Throws<ArgumentException>(() => ElectricalSupply.Reserve(ElectricalEnable.Enabled, balance, [32f], grants));
        Assert.Equal(123f, grants[0]);
    }

    [Fact]
    public void InvalidLateRecipientOrEnableRejectsAtomically()
    {
        float[] grants = [123f, 456f];
        Assert.Throws<ArgumentException>(() => ElectricalSupply.Reserve(ElectricalEnable.Enabled, 1, [32f, -1f], grants));
        Assert.Equal(UntouchedOutput, grants);
        Assert.Throws<ArgumentException>(() => ElectricalSupply.Reserve((ElectricalEnable)2, 1, [32f, 32f], grants));
        Assert.Equal(UntouchedOutput, grants);
    }

    // Story 9.4b (CAT-015 ledger class): the electrical ledger never debits work that no store was credited with,
    // including the source-limited final phase, so f32 dust cannot leak out of the battery as an unearned debit.
    [Fact]
    public void EverySourceDebitInTheSweepIsBackedByARealizedStoreCredit()
    {
        float[] grants = new float[2];
        for (var s = 0; s < 40; s++)
        {
            var source = new FiniteEnergyBalance(s == 0 ? float.Epsilon : s * .0013f, 0);
            var first = new FiniteEnergyBalance(0, 0); var second = new FiniteEnergyBalance(16, 0);
            ElectricalSupply.Reserve(ElectricalEnable.Enabled, source.Available, [32f, 16f], grants);
            var original = source;
            first = first.Credit(grants[0], 32); source = source.Debit(grants[0]);
            var accepted = MathF.Min(grants[1], source.Available);
            second = second.Credit(accepted, 32); source = source.Debit(accepted);
            var credited = first.Bulk + (double)first.Remainder + second.Bulk - 16d + second.Remainder;
            var spent = original.Bulk - (double)source.Bulk - source.Remainder;
            var bound = FiniteEnergyBalance.LossBound(original.Bulk, grants[0] + accepted, grants[0] + accepted, 0, 4);
            Assert.InRange(spent - credited, -bound, bound);
            Assert.True(credited <= original.Bulk);
        }
    }

    [Fact]
    public void RepeatedAllocationCannotOverfillOrSpendMoreThanInitialSource()
    {
        var source = new FiniteEnergyBalance(60, 0);
        FiniteEnergyBalance[] stores = [new(0, 0), new(16, 0), new(0, 0), new(31.5f, 0)];
        var initial = stores.Sum(x => (double)x.Bulk);
        float[] requests = new float[4], grants = new float[4];
        for (var phase = 0; phase < 120; phase++)
        {
            for (var i = 0; i < stores.Length; i++) requests[i] = stores[i].Headroom(32);
            ElectricalSupply.Reserve(ElectricalEnable.Enabled, MathF.Min(1, source.Available), requests, grants);
            for (var i = 0; i < stores.Length; i++)
            {
                var accepted = MathF.Min(grants[i], source.Available);
                stores[i] = stores[i].Credit(accepted, 32); source = source.Debit(accepted);
                stores[i].Validate(32);
            }
        }
        var gain = stores.Sum(x => x.Bulk + (double)x.Remainder) - initial;
        var spent = 60d - source.Bulk - source.Remainder;
        Assert.InRange(gain, 0, spent);
        Assert.InRange(spent - gain, 0, .001); // Local conservation comfortably below a millijoule for this 60 J transfer.
        Assert.InRange(source.Available, 0, .001f); // No material payable budget remains after the transfer window.
    }

    [Fact]
    public void SourceDeclarationAdmitsFiniteEnergyIndependentlyOfProductCapacity()
    {
        var source = new ElectricalSourceDeclaration(new(10), new(1), new(3600f), new(120f), 1f, ElectricalEnable.Enabled);
        source.Validate();
        Assert.Equal(3600f, source.InitialEnergy.Value);
        new Joules(201f).Validate();
        Assert.Throws<ArgumentException>(() => new Joules(float.PositiveInfinity).Validate());
        new Joules(14400f).Validate(float.MaxValue);
        Assert.Throws<ArgumentException>(() => (source with { InitialFraction = float.NaN }).Validate());
        Assert.Throws<ArgumentException>(() => (source with { Capacity = new(0f) }).Validate());
    }

    [Fact]
    public void PlanAdmitsFanoutAndRejectsForeignDuplicateOrAmbiguousSupplier()
    {
        RigidBodyDeclaration[] bodies =
        [
            new(new(1), new(1), RigidMotionKind.Static, default, default, CanonicalRotation.Identity, default, default, default, default, default, default),
            new(new(2), new(2), RigidMotionKind.Static, default, default, CanonicalRotation.Identity, default, default, default, default, default, default)
        ];
        ElectricalSourceDeclaration[] sources =
        [
            new(new(10), new(1), new(3600f), new(120f), 1f, ElectricalEnable.Enabled),
            new(new(11), new(2), new(60f), new(10f), 0f, ElectricalEnable.Disabled)
        ];
        EnergyStoreDeclaration[] stores =
        [
            new(new(20), new(3), new(32f), new(32f), new(float.MaxValue)),
            new(new(21), new(4), new(32f), new(32f), new(float.MaxValue))
        ];
        ElectricalStorageBinding[] routes = [new(new(10), new(20), new(1)), new(new(10), new(21), new(2))];
        var plan = new ElectricalSupplyPlan(sources, routes, bodies, stores);
        Assert.Equal(2, plan.Bindings.Length);
        routes[1] = new(new(11), new(20), new(3));
        Assert.Throws<ArgumentException>(() => new ElectricalSupplyPlan(sources, routes, bodies, stores));
        Assert.Equal(new EnergyStoreId(21), plan.Bindings[1].Storage);
        routes[1] = new(new(99), new(21), new(4));
        Assert.Throws<ArgumentException>(() => new ElectricalSupplyPlan(sources, routes, bodies, stores));
        routes[1] = new(new(10), new(99), new(5));
        Assert.Throws<ArgumentException>(() => new ElectricalSupplyPlan(sources, routes, bodies, stores));
        sources[1] = sources[0];
        Assert.Throws<ArgumentException>(() => new ElectricalSupplyPlan(sources, [], bodies, stores));
        sources[1] = sources[0] with { Id = new(11), Owner = new(99) };
        Assert.Throws<ArgumentException>(() => new ElectricalSupplyPlan(sources, [], bodies, stores));
    }
}
