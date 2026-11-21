using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int ElectricalSourceBytes = 64;
    // Source row: identity U64 at 0, owner body slot, declared enable, f32 capacity, maximum power and initial energy, zero
    // padding; then the state: f32 remaining energy, enable, f32 debit and zero padding to the end.
    public const int ElectricalSourceOwnerOffset = 8;
    public const int ElectricalSourceDeclaredEnableOffset = 12;
    public const int ElectricalSourceCapacityOffset = 16;
    public const int ElectricalSourceMaximumPowerOffset = 20;
    public const int ElectricalSourceInitialEnergyOffset = 24;
    private const int ElectricalSourceDeclarationPaddingOffset = 28;
    public const int ElectricalSourceDeclarationBytes = 32;
    public const int ElectricalSourceRemainingOffset = 32;
    public const int ElectricalSourceEnableOffset = 36;
    public const int ElectricalSourceDebitOffset = 40;
    public const int ElectricalPhaseAllowanceOffset = 44;
    public const int ElectricalPhaseOrdinalOffset = 48;
    public const int ElectricalSourceRemainderOffset = 52;
    public const int ElectricalPhaseRemainderOffset = 56;
    public const int ElectricalSourceTransactionsOffset = 60;
    public const int ElectricalBindingBytes = 16;
    // Binding row: source slot, contact-work storage slot, zero padding.
    public const int ElectricalBindingSourceOffset = 0;
    public const int ElectricalBindingStorageOffset = 4;
    private const int ElectricalBindingPaddingOffset = 8;

    private static void WriteEnergy(Span<byte> bytes, int offset, float value) =>
        BinaryPrimitives.WriteSingleLittleEndian(bytes[offset..], value);

    private static void WriteElectricalAdmission(Span<byte> data, IPhysicsSceneDeclaration scene)
    {
        for (var i = 0; i < scene.Electrical.Sources.Length; i++)
        {
            var source = scene.Electrical.Sources[i];
            var record = data.Slice(scene.Layout.GetElectricalSourceOffset(source.Id), ElectricalSourceBytes);
            U64(record, RecordIdentityOffset, source.Id.Value);
            U32(record, ElectricalSourceOwnerOffset, (uint)scene.Layout.GetBodySlot(source.Owner));
            U32(record, ElectricalSourceDeclaredEnableOffset, (uint)source.Enabled);
            WriteEnergy(record, ElectricalSourceCapacityOffset, source.Capacity.Value);
            WriteEnergy(record, ElectricalSourceMaximumPowerOffset, source.MaximumPower.Value);
            WriteEnergy(record, ElectricalSourceInitialEnergyOffset, source.InitialEnergy.Value);
            WriteEnergy(record, ElectricalSourceRemainingOffset, source.InitialEnergy.Value);
            U32(record, ElectricalSourceEnableOffset, (uint)source.Enabled);
        }
        for (var i = 0; i < scene.Electrical.Bindings.Length; i++)
        {
            var binding = scene.Electrical.Bindings[i];
            var record = data.Slice(scene.World.ElectricalBindingOffset(i), ElectricalBindingBytes);
            U32(record, ElectricalBindingSourceOffset, (uint)scene.Layout.GetElectricalSourceSlot(binding.Source));
            U32(record, ElectricalBindingStorageOffset, (uint)scene.Layout.GetEnergyStoreSlot(binding.Storage));
        }
    }

    public static PhysicsElectricalRead ReadElectrical(ReadOnlySpan<byte> data)
    {
        Header(data);
        var layout = Layout(data);
        var count = layout.ElectricalSources;
        var values = new ElectricalSourceRead[count];
        for (var i = 0; i < count; i++)
        {
            var record = data.Slice(layout.ElectricalSourceOffset(i), ElectricalSourceBytes);
            var owner = R32(record, ElectricalSourceOwnerOffset);
            if (owner >= R32(data, HeaderBodyCountOffset) ||
                !AllZero(record[ElectricalSourceDeclarationPaddingOffset..ElectricalSourceDeclarationBytes]) ||
                R32(record, ElectricalSourceDeclaredEnableOffset) > (uint)ElectricalEnable.Enabled ||
                R32(record, ElectricalSourceEnableOffset) > (uint)ElectricalEnable.Enabled)
                throw new ArgumentException("Invalid electrical source record.");
            ElectricalSourceDeclaration.ValidateStore(new(RF(record, ElectricalSourceCapacityOffset)),
                new(RF(record, ElectricalSourceMaximumPowerOffset)), 1f, (ElectricalEnable)R32(record, ElectricalSourceDeclaredEnableOffset));
            var initial = RF(record, ElectricalSourceInitialEnergyOffset);
            var balance = new FiniteEnergyBalance(RF(record, ElectricalSourceRemainingOffset),
                RF(record, ElectricalSourceRemainderOffset));
            balance.Validate(initial);
            var allowance = new FiniteEnergyBalance(RF(record, ElectricalPhaseAllowanceOffset),
                RF(record, ElectricalPhaseRemainderOffset));
            allowance.Validate(RF(record, ElectricalSourceMaximumPowerOffset) * ElectricalSupply.PhaseSeconds);
            var phaseOrdinal = R32(record, ElectricalPhaseOrdinalOffset);
            if (allowance.DecreaseTo(balance) > 0 || phaseOrdinal % 4 != 0 ||
                phaseOrdinal > R32(data, HeaderSourceOrdinalOffset) ||
                initial > RF(record, ElectricalSourceCapacityOffset))
                throw new ArgumentException("Invalid finite electrical phase allowance or initial charge.");
            values[i] = new(new(R64(record, RecordIdentityOffset)), new(R64(data, layout.BodyOffset((int)owner))),
                new(RF(record, ElectricalSourceRemainingOffset), RF(record, ElectricalSourceRemainderOffset)),
                new(RF(record, ElectricalSourceDebitOffset)), (ElectricalEnable)R32(record, ElectricalSourceEnableOffset),
                R32(record, ElectricalSourceTransactionsOffset));
        }
        var seen = new bool[layout.EnergyStores];
        for (var i = 0; i < layout.ElectricalBindings; i++)
        {
            var record = data.Slice(layout.ElectricalBindingOffset(i), ElectricalBindingBytes);
            var source = R32(record, ElectricalBindingSourceOffset); var storage = R32(record, ElectricalBindingStorageOffset);
            if (source >= count || storage >= (uint)layout.EnergyStores || seen[(int)storage] ||
                !AllZero(record[ElectricalBindingPaddingOffset..]))
                throw new ArgumentException("Invalid or ambiguous electrical storage route.");
            seen[(int)storage] = true;
        }
        return PhysicsElectricalRead.FromImmutable(System.Runtime.InteropServices.ImmutableCollectionsMarshal.AsImmutableArray(values));
    }

    private static void ValidateElectricalCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> previous, bool instantaneous)
    {
        var before = ReadElectrical(previous); var after = ReadElectrical(candidate);
        if (instantaneous)
        {
            if (!after.HasSameBits(before)) throw new ArgumentException("A no-time transaction changed electrical state.");
            return; // Supply rows/routes and cumulative store credits remain unchanged under ValidateNoTimeState.
        }
        after.ValidateAdvance(before);
        var layout = Layout(previous);
        if (!candidate[layout.ElectricalBindingsOffset..layout.PrismaticsOffset].SequenceEqual(previous[layout.ElectricalBindingsOffset..layout.PrismaticsOffset]))
            throw new ArgumentException("Electrical routes changed during a Run.");
        var phaseCount = (uint)(((ulong)R32(candidate, HeaderSourceOrdinalOffset) + 3) / 4 -
            ((ulong)R32(previous, HeaderSourceOrdinalOffset) + 3) / 4);
        var stores = ReadEnergyStores(candidate);
        var supplied = new bool[stores.Length];
        for (var i = 0; i < after.Count; i++)
        {
            var offset = layout.ElectricalSourceOffset(i);
            if (!candidate.Slice(offset, ElectricalSourceDeclarationBytes).SequenceEqual(previous.Slice(offset, ElectricalSourceDeclarationBytes)))
                throw new ArgumentException("Electrical source declaration changed.");

            if (R32(candidate, offset + ElectricalPhaseOrdinalOffset) < R32(previous, offset + ElectricalPhaseOrdinalOffset) ||
                phaseCount == 0 && new FiniteEnergyBalance(RF(previous, offset + ElectricalPhaseAllowanceOffset),
                    RF(previous, offset + ElectricalPhaseRemainderOffset)).DecreaseTo(new(
                    RF(candidate, offset + ElectricalPhaseAllowanceOffset), RF(candidate, offset + ElectricalPhaseRemainderOffset))) < 0)
                throw new ArgumentException("Electrical phase allowance increased without a phase boundary.");

            var steps = R32(candidate, HeaderSourceOrdinalOffset) - R32(previous, HeaderSourceOrdinalOffset);
            if (after[i].Transactions > (ulong)steps * (uint)layout.AirflowSources + (ulong)phaseCount * (uint)layout.ElectricalBindings)
                throw new ArgumentException("Electrical transaction count exceeds admitted consumers.");
            var priorPhase = R32(previous, HeaderSourceOrdinalOffset) % 4 == 0 ? default :
                new FiniteEnergyBalance(RF(previous, offset + ElectricalPhaseAllowanceOffset),
                    RF(previous, offset + ElectricalPhaseRemainderOffset));
            var quantum = after[i].Enabled == ElectricalEnable.Enabled
                ? RF(candidate, offset + ElectricalSourceMaximumPowerOffset) * ElectricalSupply.PhaseSeconds : 0f;
            PhysicsReadValidation.ValidateElectricalBudget(after[i].Debit.Value, after[i].Transactions,
                before[i].Balance, priorPhase, quantum, phaseCount);
            ValidateElectricalCredits(candidate, layout, stores, supplied, i, after[i]);
        }
        for (var i = 0; i < stores.Length; i++)
            if (!supplied[i] && stores[i].CommitSupplied.Value != 0)
                throw new ArgumentException("Unconnected storage received electrical work.");
    }
    private static void ValidateElectricalCredits(ReadOnlySpan<byte> candidate, PhysicsWorldLayout layout,
        EnergyStoreRead[] stores, bool[] supplied, int supply, ElectricalSourceRead source)
    {
        float maximum = 0, sum = 0;
        ulong terms = 0;
        var count = (long)layout.ElectricalBindings + layout.AirflowSources;
        for (long row = 0; row < count; row++)
            maximum = MathF.Max(maximum, ConnectedCredit(candidate, layout, stores, supplied, supply, row));
        var scale = MathF.Max(source.Debit.Value, maximum);
        for (long row = 0; row < count; row++)
        {
            var value = ConnectedCredit(candidate, layout, stores, supplied, supply, row);
            if (value == 0) continue;
            terms++;
            sum += value / scale;
        }
        PhysicsReadValidation.ValidateConnectedCredits(source.Debit.Value, source.Transactions, maximum, sum, terms);
    }

    private static float ConnectedCredit(ReadOnlySpan<byte> candidate, PhysicsWorldLayout layout,
        EnergyStoreRead[] stores, bool[] supplied, int supply, long row)
    {
        if (row < layout.ElectricalBindings)
        {
            var binding = layout.ElectricalBindingOffset((int)row);
            if (R32(candidate, binding + ElectricalBindingSourceOffset) != supply) return 0;
            var slot = (int)R32(candidate, binding + ElectricalBindingStorageOffset);
            supplied[slot] = true;
            return stores[slot].CommitSupplied.Value;
        }
        var airflow = layout.AirflowSourceOffset((int)(row - layout.ElectricalBindings));
        return (ContinuousWorkInputKind)R32(candidate, airflow + AirflowInputKindOffset) == ContinuousWorkInputKind.Electrical &&
            R32(candidate, airflow + AirflowElectricalOffset) == supply
            ? RF(candidate, airflow + AirflowCommitWorkOffset) : 0;
    }

}
