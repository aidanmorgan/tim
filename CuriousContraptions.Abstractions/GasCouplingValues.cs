using System;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public readonly record struct LocalColliderReference(PackageBodyIndex Body, BodyColliderIndex Collider);

/// <summary>Physical sphere volume is one exact compliant store plus a positive dry core.</summary>
public readonly record struct GasShapeDescriptor(LocalGasStoreId Store, PackageBodyIndex Body,
    BodyColliderIndex Collider, float DryRadius);

/// <summary>Final approaching-contact impulse opens one explicitly bound outlet.</summary>
public readonly record struct ContactIntegrityDescriptor(LocalIntegrityObservationId Id, PackageBodyIndex Body,
    BodyColliderIndex Collider, LocalGasStoreId Store, float Threshold, float MinimumApproachSpeed);

/// <summary>Declared ambient forces are distinct from body gravity. Inventory modulation applies only to acceleration.</summary>
public readonly record struct AmbientLoadDescriptor(PackageBodyIndex Body, WorldAccelerationDeclaration Acceleration,
    MassScalar LinearDrag, LocalGasStoreId? InventoryFraction);

public readonly record struct GasStoreDeclaration(GasStoreId Id, GpuBodyId Body, GasMaterial Material,
    GasEnvelopeState Reference, float BoundaryPressure, float GaugePressure, float Conductance)
{
    public GasPreparation Prepared => IsobaricGas.Prepare(Material, Reference, BoundaryPressure, GaugePressure);

    public void Validate()
    {
        if (Id.Value == 0 || Body.Value == 0) throw new ArgumentException("Invalid gas store identity.");
        Reference.Validate(Material);
        IsobaricGas.ValidateEquilibrium(Material, Prepared.State, BoundaryPressure, GaugePressure);
        if (Reference.Phase != GasInventoryPhase.Charged || Prepared.State.Mass != Reference.Mass)
            throw new ArgumentException("Prepared inventory must preserve the positive reference charge.");
        GasMaterial.Nonnegative(BoundaryPressure); GasMaterial.Positive(GaugePressure); GasMaterial.Nonnegative(Conductance);
        if (!float.IsFinite(Prepared.BoundaryWorkOut) || !float.IsFinite(Prepared.ElasticEnergyChange))
            throw new ArgumentException("Nonfinite preparation work.");
    }
}

public readonly record struct GasShapeDeclaration(GasStoreId Store, GpuColliderId Collider, float DryRadius)
{
    public void Validate()
    {
        if (Store.Value == 0 || Collider.Value == 0) throw new ArgumentException("Invalid gas shape binding.");
        PhysicsDeclarationBounds.Range(DryRadius, 1f / 32f, 16f);
    }
}

public readonly record struct InventoryMassDeclaration(GpuBodyId Body, float DryMass)
{
    public void Validate()
    {
        if (Body.Value == 0) throw new ArgumentException("Invalid inventory mass body.");
        PhysicsDeclarationBounds.Range(DryMass, 1f / 1024f, 1024f);
    }
}

public readonly record struct AmbientLoadDeclaration(GpuBodyId Body, Vector3 Acceleration, float LinearDrag,
    GasStoreId? InventoryFraction)
{
    public void Validate()
    {
        if (Body.Value == 0 || InventoryFraction is { Value: 0 } ||
            !float.IsFinite(Acceleration.X) || !float.IsFinite(Acceleration.Y) || !float.IsFinite(Acceleration.Z) ||
            Acceleration.LengthSquared() > 256f)
            throw new ArgumentException("Invalid ambient load binding.");
        GasMaterial.Nonnegative(LinearDrag);
    }
}

public readonly record struct ContactIntegrityDeclaration(IntegrityObservationId Id, GpuColliderId Collider,
    GasStoreId Store, float Threshold, float MinimumApproachSpeed)
{
    public void Validate()
    {
        if (Id.Value == 0 || Collider.Value == 0 || Store.Value == 0)
            throw new ArgumentException("Invalid integrity binding.");
        GasMaterial.Nonnegative(Threshold); GasMaterial.Nonnegative(MinimumApproachSpeed);
    }
}

public readonly record struct IntegrityRead(IntegrityObservationId Id, IntegrityPhase Phase, uint Ordinal, float EventPhase)
{
    public void Validate()
    {
        if (Id.Value == 0 || !Enum.IsDefined(Phase) || !float.IsFinite(EventPhase) || EventPhase < 0 || EventPhase > 1 ||
            EventPhase != 0 || (Phase == IntegrityPhase.Intact ? Ordinal != 0 : Ordinal == 0))
            throw new ArgumentException("Invalid integrity observation.");
    }
}
