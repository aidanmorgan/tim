using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

public static partial class PhysicsGpuAbi
{
    public const int EnergyStoreBytes = 80;
    public const int EnergyStoreOwnerOffset = 8;
    public const int EnergyStoreCapacityOffset = 12;
    public const int EnergyStoreInitialOffset = 16;
    public const int EnergyStorePowerOffset = 20;
    public const int EnergyStoreReservedOffset = 24;
    public const int EnergyStoreBalanceOffset = 32;
    public const int EnergyStoreSuppliedOffset = 36;
    public const int EnergyStoreReleasedOffset = 40;
    public const int EnergyStoreDissipatedOffset = 44;
    public const int EnergyStoreRemainderOffset = 48;
    public const int EnergyStoreCommitSuppliedOffset = 52;
    public const int EnergyStoreCommitReleasedOffset = 56;
    public const int EnergyStoreCommitDissipatedOffset = 60;
    public const int EnergyStoreTransactionsOffset = 64;
    public const int EnergyStorePaddingOffset = 68;

    private static void AdmitEnergyStores(Span<byte> data, IPhysicsSceneDeclaration scene)
    {
        foreach (var store in scene.EnergyStores)
        {
            var row = data.Slice(scene.Layout.GetEnergyStoreOffset(store.Id), EnergyStoreBytes);
            U64(row, RecordIdentityOffset, store.Id.Value);
            U32(row, EnergyStoreOwnerOffset, BodySlot(scene, store.Owner));
            BinaryPrimitives.WriteSingleLittleEndian(row[EnergyStoreCapacityOffset..], store.Capacity.Value);
            BinaryPrimitives.WriteSingleLittleEndian(row[EnergyStoreInitialOffset..], store.InitialEnergy.Value);
            BinaryPrimitives.WriteSingleLittleEndian(row[EnergyStorePowerOffset..], store.MaximumChargePower.Value);
            BinaryPrimitives.WriteSingleLittleEndian(row[EnergyStoreBalanceOffset..], store.InitialEnergy.Value);
        }
    }

    public static EnergyStoreRead[] ReadEnergyStores(ReadOnlySpan<byte> data)
    {
        if (ReadFailure(data) != PhysicsFailure.None) throw new ArgumentException("Invalid energy-store world.");
        var layout = Layout(data);
        var result = new EnergyStoreRead[layout.EnergyStores];
        var previous = 0UL;
        for (var i = 0; i < result.Length; i++)
        {
            var row = data.Slice(layout.EnergyStoreOffset(i), EnergyStoreBytes);
            var identity = R64(row, RecordIdentityOffset);
            var owner = R32(row, EnergyStoreOwnerOffset);
            if (identity <= previous || owner >= layout.Bodies ||
                !AllZero(row[EnergyStoreReservedOffset..EnergyStoreBalanceOffset]) ||
                !AllZero(row[EnergyStorePaddingOffset..]))
                throw new ArgumentException("Invalid energy-store identity, owner or padding.");
            previous = identity;
            var body = new GpuBodyId(R64(data, layout.BodyOffset((int)owner)));
            var declaration = new EnergyStoreDeclaration(new(identity), body,
                new(RF(row, EnergyStoreCapacityOffset)), new(RF(row, EnergyStoreInitialOffset)),
                new(RF(row, EnergyStorePowerOffset)));
            declaration.Validate();
            var value = new EnergyStoreRead(declaration.Id, body, new(RF(row, EnergyStoreBalanceOffset), RF(row, EnergyStoreRemainderOffset)),
                new(RF(row, EnergyStoreSuppliedOffset)), new(RF(row, EnergyStoreReleasedOffset)),
                new(RF(row, EnergyStoreDissipatedOffset)), new(RF(row, EnergyStoreCommitSuppliedOffset)),
                new(RF(row, EnergyStoreCommitReleasedOffset)), new(RF(row, EnergyStoreCommitDissipatedOffset)),
                R32(row, EnergyStoreTransactionsOffset));
            value.Validate();
            if (value.Balance.CompareTo(declaration.Capacity.Value) > 0)
                throw new ArgumentException("Energy-store capacity exceeded.");
            result[i] = value;
        }
        return result;
    }

    private static void ValidateEnergyStoreCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        var before = ReadEnergyStores(source);
        var after = ReadEnergyStores(candidate);
        new PhysicsEnergyStoreRead(after).ValidateAdvance(new PhysicsEnergyStoreRead(before));
        if (before.Length != after.Length) throw new ArgumentException("Energy-store population changed.");
        var layout = Layout(source);
        for (var i = 0; i < before.Length; i++)
        {
            var offset = layout.EnergyStoreOffset(i);
            var steps = R32(candidate, HeaderSourceOrdinalOffset) - R32(source, HeaderSourceOrdinalOffset);
            ulong maximumTransactions = (1UL + (uint)layout.AirflowSources) * steps;
            for (var contact = 0; contact < layout.ContactWorks; contact++)
            {
                var row = layout.ContactWorkOffset(contact);
                if (R32(candidate, row + ContactWorkStoreOffset) == i)
                    maximumTransactions += R32(candidate, row + ContactWorkOccurrenceCountOffset) -
                        R32(source, row + ContactWorkOccurrenceCountOffset);
            }
            for (var actuator = 0; actuator < layout.FiniteWorkActuators; actuator++)
            {
                var row = layout.FiniteWorkActuatorOffset(actuator);
                if (R32(candidate, row + ActuatorStoreOffset) == i)
                    maximumTransactions += R32(candidate, row + ActuatorCountOffset) -
                        R32(source, row + ActuatorCountOffset);
            }
            if (after[i].Transactions > maximumTransactions)
                throw new ArgumentException("Store transaction count exceeds admitted consumers.");
            if (!source.Slice(offset, EnergyStoreBalanceOffset).SequenceEqual(candidate.Slice(offset, EnergyStoreBalanceOffset)) ||
                after[i].Supplied.Value < before[i].Supplied.Value ||
                after[i].Released.Value < before[i].Released.Value ||
                after[i].Dissipated.Value < before[i].Dissipated.Value)
                throw new ArgumentException("Energy-store declaration or cumulative work changed illegally.");
        }
    }
}
