using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private static FiniteEnergyBalance StoreBalance(ReadOnlySpan<byte> world, int offset) =>
        new(ReadF32(world, offset + PhysicsGpuAbi.EnergyStoreBalanceOffset),
            ReadF32(world, offset + PhysicsGpuAbi.EnergyStoreRemainderOffset));

    private static FiniteEnergyBalance SourceBalance(ReadOnlySpan<byte> world, int offset) =>
        new(ReadF32(world, offset + PhysicsGpuAbi.ElectricalSourceRemainingOffset),
            ReadF32(world, offset + PhysicsGpuAbi.ElectricalSourceRemainderOffset));

    private static FiniteEnergyBalance PhaseBalance(ReadOnlySpan<byte> world, int offset) =>
        new(ReadF32(world, offset + PhysicsGpuAbi.ElectricalPhaseAllowanceOffset),
            ReadF32(world, offset + PhysicsGpuAbi.ElectricalPhaseRemainderOffset));

    private static void WriteBalance(Span<byte> world, int bulk, int remainder, FiniteEnergyBalance value)
    {
        WriteF32(world, bulk, value.Bulk);
        WriteF32(world, remainder, value.Remainder);
    }

    private static void StorePayment(Span<byte> world, int offset, float work, bool close = false)
    {
        if (work == 0) return;
        var balance = StoreBalance(world, offset);
        if (!float.IsFinite(work) || work < 0 || work > balance.Available)
            throw new ArgumentException("Accepted work exceeds its shared store.");
        var remaining = close ? default : balance.Debit(work);
        var dissipated = close ? MathF.Max(0, balance.DecreaseTo(new(work, 0))) : 0f;
        WriteBalance(world, offset + PhysicsGpuAbi.EnergyStoreBalanceOffset,
            offset + PhysicsGpuAbi.EnergyStoreRemainderOffset, remaining);
        AddEnergy(world, offset + PhysicsGpuAbi.EnergyStoreReleasedOffset, work);
        AddEnergy(world, offset + PhysicsGpuAbi.EnergyStoreCommitReleasedOffset, work);
        AddEnergy(world, offset + PhysicsGpuAbi.EnergyStoreDissipatedOffset, dissipated);
        AddEnergy(world, offset + PhysicsGpuAbi.EnergyStoreCommitDissipatedOffset, dissipated);
        AddTransaction(world, offset + PhysicsGpuAbi.EnergyStoreTransactionsOffset);
    }

    private static void StoreCredit(Span<byte> world, int offset, float work)
    {
        if (work == 0) return;
        var balance = StoreBalance(world, offset);
        var capacity = ReadF32(world, offset + PhysicsGpuAbi.EnergyStoreCapacityOffset);
        WriteBalance(world, offset + PhysicsGpuAbi.EnergyStoreBalanceOffset,
            offset + PhysicsGpuAbi.EnergyStoreRemainderOffset, balance.Credit(work, capacity));
        AddEnergy(world, offset + PhysicsGpuAbi.EnergyStoreSuppliedOffset, work);
        AddEnergy(world, offset + PhysicsGpuAbi.EnergyStoreCommitSuppliedOffset, work);
        AddTransaction(world, offset + PhysicsGpuAbi.EnergyStoreTransactionsOffset);
    }

    private static void SourcePayment(Span<byte> world, int offset, float work)
    {
        if (work == 0) return;
        var remaining = SourceBalance(world, offset).Debit(work);
        var allowance = PhaseBalance(world, offset).Debit(work);
        if (allowance.DecreaseTo(remaining) > 0) allowance = remaining;
        WriteBalance(world, offset + PhysicsGpuAbi.ElectricalSourceRemainingOffset,
            offset + PhysicsGpuAbi.ElectricalSourceRemainderOffset, remaining);
        WriteBalance(world, offset + PhysicsGpuAbi.ElectricalPhaseAllowanceOffset,
            offset + PhysicsGpuAbi.ElectricalPhaseRemainderOffset, allowance);
        AddEnergy(world, offset + PhysicsGpuAbi.ElectricalSourceDebitOffset, work);
        AddTransaction(world, offset + PhysicsGpuAbi.ElectricalSourceTransactionsOffset);
    }

    private static void AddEnergy(Span<byte> world, int offset, float work) =>
        WriteF32(world, offset, ReadF32(world, offset) + work);

    private static void AddTransaction(Span<byte> world, int offset) =>
        WriteU32(world, offset, checked(ReadU32(world, offset) + 1));

    private static void PrepareStoreCommit(Span<byte> world, PhysicsWorldLayout layout)
    {
        for (var i = 0; i < layout.EnergyStores; i++)
        {
            var offset = layout.EnergyStoreOffset(i);
            WriteF32(world, offset + PhysicsGpuAbi.EnergyStoreCommitSuppliedOffset, 0);
            WriteF32(world, offset + PhysicsGpuAbi.EnergyStoreCommitReleasedOffset, 0);
            WriteF32(world, offset + PhysicsGpuAbi.EnergyStoreCommitDissipatedOffset, 0);
            WriteU32(world, offset + PhysicsGpuAbi.EnergyStoreTransactionsOffset, 0);
        }
    }

    private static void PrepareEnergyCommit(Span<byte> world, PhysicsWorldLayout layout)
    {
        PrepareStoreCommit(world, layout);
        for (var i = 0; i < layout.ElectricalSources; i++)
        {
            var offset = layout.ElectricalSourceOffset(i);
            WriteF32(world, offset + PhysicsGpuAbi.ElectricalSourceDebitOffset, 0);
            WriteU32(world, offset + PhysicsGpuAbi.ElectricalSourceTransactionsOffset, 0);
        }
        for (var i = 0; i < layout.AirflowSources; i++)
        {
            var offset = layout.AirflowSourceOffset(i);
            WriteF32(world, offset + PhysicsGpuAbi.AirflowCommitWorkOffset, 0);
            WriteF32(world, offset + PhysicsGpuAbi.AirflowCommitLossOffset, 0);
        }
    }
}
