using CuriousContraptions.Gpu;
using System.Buffers.Binary;

namespace CuriousContraptions.Tests;

public sealed class ElectricalSupplyTests
{
    [Fact]
    public void F32AuthoringWiringAndSaveRoundTripWithAtomicAmbiguousSourceRejection()
    {
        var settings = new ElectricalSourceSettings(new(3600.0002f), new(120.00001f), .1234567f, ElectricalEnable.Disabled);
        var battery = new WorkshopBattery(new(1), default, default, CanonicalRotation.Identity, settings);
        var bumper = WorkshopInput.Bumper(new(2), 3, 4, 0, 0, 0, 0, 1, BumperWork.FromCanonicalStrength(8f));
        var link = new WorkshopConnection(battery.Id, WorkshopSocket.Supply, bumper.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(battery, bumper), Connections: new(link));
        construction.Validate();
        var saved = new WorkshopSavedConstruction(construction, new(3));
        var original = WorkshopSaveCodec.Encode(saved);
        var restored = WorkshopSaveCodec.Decode(original);
        Assert.Equal(settings, Assert.IsType<WorkshopBattery>(restored.Construction.Instances[0]).Settings);
        Assert.Equal(original, WorkshopSaveCodec.Encode(restored));
        var scene = WorkshopPhysicsCompiler.Compile(restored.Construction, new(1, 2));
        Assert.Single(scene.Electrical.Bindings.ToArray());
        Assert.Equal(0, WorkshopActivationCompiler.Compile(restored.Construction).Clear().Count);
        Assert.Equal(settings.Capacity, scene.Electrical.Sources[0].Capacity);
        Assert.Equal(settings.MaximumPower, scene.Electrical.Sources[0].MaximumPower);
        var second = battery with { Id = new(3) };
        var ambiguous = construction.WithInstance(second) with { Connections = new(link, link with { Source = second.Id }) };
        Assert.Throws<ArgumentException>(() => ambiguous.Validate());
        Assert.Equal(original, WorkshopSaveCodec.Encode(saved));
        var wrongDomain = construction with { Connections = new(link with { Domain = WorkshopConnectionDomain.Activation }) };
        Assert.Throws<ArgumentException>(() => wrongDomain.Validate());
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
            new(new WorkshopBattery(new(1), default, default, CanonicalRotation.Identity, ElectricalSourceSettings.Default)));
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var initial = PhysicsGpuAbi.ReadElectrical(bytes);
        Assert.Equal(3600f, initial[0].Remaining.Value);
        Assert.True(initial[0].Available);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(PhysicsGpuAbi.ElectricalSourcesOffset + field), invalid);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadElectrical(bytes));
    }

    [Theory]
    [InlineData(0f, ElectricalEnable.Enabled)]
    [InlineData(3600f, ElectricalEnable.Disabled)]
    public void UnavailableSourcePreservesBothStores(float source, ElectricalEnable enabled)
    {
        float[] before = [0f, 4f], capacity = [32f, 32f], after = [-1f, -1f];
        var result = ElectricalSupply.Allocate(enabled, source, 120f, before, capacity, after);
        Assert.Equal(before, after);
        Assert.Equal(source, result.Remaining);
        Assert.Equal(0f, result.Debit);
    }

    [Fact]
    public void EqualFourLoadDemandSharesOneSimdBudget()
    {
        float[] before = [0f, 0f, 0f, 0f], capacity = [32f, 32f, 32f, 32f], after = new float[4];
        var result = ElectricalSupply.Allocate(ElectricalEnable.Enabled, 3600f, 120f, before, capacity, after);
        Assert.Equal(1f, result.Debit);
        Assert.All(after, value => Assert.Equal(.25f, value));
        Assert.Equal(3599f, result.Remaining);
    }

    [Fact]
    public void FinalPositiveSourceBalanceTransfersBeforeAvailabilityClears()
    {
        float[] before = [0f], capacity = [32f], after = [-1f];
        var result = ElectricalSupply.Allocate(ElectricalEnable.Enabled, .125f, 120f, before, capacity, after);
        Assert.Equal(0f, result.Remaining);
        Assert.Equal(.125f, result.Debit);
        Assert.Equal(.125f, after[0]);
    }

    [Fact]
    public void UnrepresentableSourceDebitDoesNotCreditSmallDemand()
    {
        float[] before = [0f], capacity = [.000001f], after = [-1f];
        var result = ElectricalSupply.Allocate(ElectricalEnable.Enabled, 3600f, 120f, before, capacity, after);
        Assert.Equal(3600f, result.Remaining);
        Assert.Equal(0f, result.Debit);
        Assert.Equal(0f, after[0]);
    }

    [Fact]
    public void CrossingRecipientBinadeDoesNotRoundCreditAboveFundedGrant()
    {
        float[] before = [MathF.BitDecrement(16f), MathF.BitDecrement(16f)];
        float[] capacity = [32f, 32f], after = new float[2];
        var result = ElectricalSupply.Allocate(ElectricalEnable.Enabled, 3600f, 120f, before, capacity, after);
        Assert.Equal(1f, result.Debit);
        for (var i = 0; i < after.Length; i++)
            Assert.InRange((double)after[i] - before[i], .49999, .5);
        Assert.True(after.Sum(value => (double)value) - before.Sum(value => (double)value) <= result.Debit);
    }

    [Fact]
    public void UnequalDemandSharesProportionallyAndFullLoadReceivesNothing()
    {
        float[] before = [31f, 29f, 32f], capacity = [32f, 32f, 32f], after = new float[3];
        var result = ElectricalSupply.Allocate(ElectricalEnable.Enabled, 3600f, 120f, before, capacity, after);
        Assert.Equal(new[] {31.25f, 29.75f, 32f}, after);
        Assert.Equal(1f, result.Debit);
    }

    [Fact]
    public void AlmostFullLoadRetainsUnrequestedSourceWork()
    {
        float[] before = [31.75f], capacity = [32f], after = new float[1];
        var result = ElectricalSupply.Allocate(ElectricalEnable.Enabled, 3600f, 120f, before, capacity, after);
        Assert.Equal(32f, after[0]);
        Assert.Equal(.25f, result.Debit);
        Assert.Equal(3599.75f, result.Remaining);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(-1f)]
    [InlineData(14401f)]
    public void InvalidSourceRejectsBeforeWriting(float balance)
    {
        float[] before = [0f], capacity = [32f], after = [123f];
        Assert.Throws<ArgumentException>(() => ElectricalSupply.Allocate(ElectricalEnable.Enabled, balance, 120f, before, capacity, after));
        Assert.Equal(123f, after[0]);
    }

    [Fact]
    public void InvalidLateRecipientOrEnableRejectsAtomically()
    {
        float[] before = [0f, 33f], capacity = [32f, 32f], after = [123f, 456f];
        Assert.Throws<ArgumentException>(() => ElectricalSupply.Allocate(ElectricalEnable.Enabled, 3600f, 120f, before, capacity, after));
        Assert.Equal(new[] {123f, 456f}, after);
        before[1] = 0;
        Assert.Throws<ArgumentException>(() => ElectricalSupply.Allocate((ElectricalEnable)2, 3600f, 120f, before, capacity, after));
        Assert.Equal(new[] {123f, 456f}, after);
    }

    [Fact]
    public void RepeatedAllocationCannotOverfillOrSpendMoreThanInitialSource()
    {
        float[] balances = [0f, MathF.BitDecrement(16f), .000001f, 31.9f];
        float[] capacities = [32f, 32f, 32f, 32f], next = new float[4];
        var initialTotal = balances.Sum(value => (double)value);
        var source = 60f;
        for (var phase = 0; phase < 1000; phase++)
        {
            var oldSource = source;
            var oldTotal = balances.Sum(value => (double)value);
            var result = ElectricalSupply.Allocate(ElectricalEnable.Enabled, source, 120f, balances, capacities, next);
            Assert.InRange(result.Debit, 0f, Math.Min(source, 120f * ElectricalSupply.PhaseSeconds));
            Assert.Equal(oldSource - result.Remaining, result.Debit);
            var credited = next.Sum(value => (double)value) - oldTotal;
            Assert.InRange(credited, 0, result.Debit + 4 * Math.Pow(2, -18));
            if (result.Debit == 0) Assert.Equal(balances, next);
            for (var i = 0; i < next.Length; i++) Assert.InRange(next[i], balances[i], capacities[i]);
            source = result.Remaining;
            (balances, next) = (next, balances);
        }
        Assert.InRange(balances.Sum(value => (double)value) - initialTotal, 0, 60 - source + .0001);
    }

    [Fact]
    public void SourceDeclarationAdmitsApprovedF32DefaultsAndPreservesContactBound()
    {
        var source = new ElectricalSourceDeclaration(new(10), new(1), new(3600f), new(120f), 1f, ElectricalEnable.Enabled);
        source.Validate();
        Assert.Equal(3600f, source.InitialEnergy.Value);
        Assert.Throws<ArgumentException>(() => new Joules(201f).Validate());
        new Joules(14400f).Validate(ElectricalSupply.MaximumCapacity);
        Assert.Throws<ArgumentException>(() => (source with { InitialFraction = float.NaN }).Validate());
        Assert.Throws<ArgumentException>(() => (source with { Capacity = new(59f) }).Validate());
    }

    [Fact]
    public void PlanAdmitsFanoutAndRejectsForeignDuplicateOrAmbiguousSupplier()
    {
        RigidBodyDeclaration[] bodies =
        [
            new(new(1), RigidMotionKind.Static, default, default, CanonicalRotation.Identity, default, default, default, default, default),
            new(new(2), RigidMotionKind.Static, default, default, CanonicalRotation.Identity, default, default, default, default, default)
        ];
        ElectricalSourceDeclaration[] sources =
        [
            new(new(10), new(1), new(3600f), new(120f), 1f, ElectricalEnable.Enabled),
            new(new(11), new(2), new(60f), new(10f), 0f, ElectricalEnable.Disabled)
        ];
        ContactWorkDeclaration[] stores =
        [
            new(new(20), new(3), new(BodyTargetKind.AllDynamic, default), new(8f), new(32f), new(.05f), 72),
            new(new(21), new(4), new(BodyTargetKind.AllDynamic, default), new(8f), new(32f), new(.05f), 72)
        ];
        ElectricalStorageBinding[] routes = [new(new(10), new(20)), new(new(10), new(21))];
        var plan = new ElectricalSupplyPlan(sources, routes, bodies, stores);
        Assert.Equal(2, plan.Bindings.Length);
        routes[1] = new(new(11), new(20));
        Assert.Throws<ArgumentException>(() => new ElectricalSupplyPlan(sources, routes, bodies, stores));
        Assert.Equal(new GpuContactWorkId(21), plan.Bindings[1].Storage);
        routes[1] = new(new(99), new(21));
        Assert.Throws<ArgumentException>(() => new ElectricalSupplyPlan(sources, routes, bodies, stores));
        routes[1] = new(new(10), new(99));
        Assert.Throws<ArgumentException>(() => new ElectricalSupplyPlan(sources, routes, bodies, stores));
        sources[1] = sources[0];
        Assert.Throws<ArgumentException>(() => new ElectricalSupplyPlan(sources, [], bodies, stores));
        sources[1] = sources[0] with { Id = new(11), Owner = new(99) };
        Assert.Throws<ArgumentException>(() => new ElectricalSupplyPlan(sources, [], bodies, stores));
    }
}
