using System;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public readonly record struct FiniteWorkActuatorId(ulong Value);
public readonly record struct LocalFiniteWorkActuatorId(uint Value);
public enum FiniteWorkClearance : byte { NotApplicable, Clear, Obstructed, GeometryUnresolved }
public enum FiniteWorkRequestResult : byte
{
    Empty, Busy, Ambiguous, Unseated, Uncharged, Obstructed, SpeedLimited, NoResponse, Fired, GeometryUnresolved, TargetMismatch
}

/// <summary>Exact installed capability and payload for a no-time physical transaction.</summary>
public readonly record struct OwnedFiniteWorkRequest(SimulationEpoch Epoch, FiniteWorkActuatorId Actuator, GpuBodyId Payload);
public enum FiniteWorkTransactionRefusal
{
    Busy, NoWorld, StaleEpoch, StaleCommit, InvalidSequence, UnknownActuator, ForeignTarget, NonDynamicTarget, IdentityExhausted
}
public sealed class FiniteWorkTransactionException(FiniteWorkTransactionRefusal reason) : Exception("Owned finite-work transaction refused.")
{
    public FiniteWorkTransactionRefusal Reason { get; } = reason;
}

/// <summary>A paid axial COM effort with equal opposite reaction in the named region's frame.</summary>
public readonly record struct FiniteWorkActuatorDescriptor(LocalFiniteWorkActuatorId Id,
    LocalEnergyStoreId Store, LocalChamberRegionId Region, MechanicalValue TargetSpeed, float ExitPadding);

public readonly record struct FiniteWorkActuatorDeclaration(FiniteWorkActuatorId Id, EnergyStoreId Store,
    ChamberRegionId Region, ContactSpeed TargetSpeed, float ExitPadding)
{
    public void Validate()
    {
        if (Id.Value == 0 || Store.Value == 0 || Region.Value == 0 || !float.IsFinite(ExitPadding) || ExitPadding < 0f)
            throw new ArgumentException("Invalid finite-work actuator declaration.");
        TargetSpeed.Validate(LinearVelocity.MaximumSpeed);
    }
}

public readonly record struct FiniteWorkActuatorRead(FiniteWorkActuatorId Id, EnergyStoreId Store, ChamberRegionId Region,
    uint OccurrenceCount, FiniteWorkRequestResult? LastResult, GpuBodyId LastPayload, Joules LastDebit, FiniteWorkClearance Clearance)
{
    public void Validate()
    {
        LastDebit.Validate();
        if (Id.Value == 0 || Store.Value == 0 || Region.Value == 0 || !Enum.IsDefined(Clearance) ||
            (LastResult is { } result && !Enum.IsDefined(result)) ||
            (LastResult is null && (LastPayload.Value != 0 || LastDebit.Value != 0f || OccurrenceCount != 0)) ||
            (LastResult == FiniteWorkRequestResult.Fired && (LastPayload.Value == 0 || LastDebit.Value <= 0f)) ||
            (LastResult != FiniteWorkRequestResult.Fired && LastDebit.Value != 0f))
            throw new ArgumentException("Invalid committed finite-work actuator state.");
    }
}
