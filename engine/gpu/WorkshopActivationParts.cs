using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>Typed authored impact threshold. The default comes from PartDifficulty.TriggerThreshold.</summary>
public readonly record struct ContactTriggerSettings(LinearSpeed Threshold)
{
    public static ContactTriggerSettings Default => new(new((Half).8));
    public void Validate() => PhysicsDeclarationBounds.Range(Threshold.Value, (Half)0, (Half)64);
}

public readonly record struct WorkshopSwitch(GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, ContactTriggerSettings Trigger, bool Locked = false) : IWorkshopInstance
{
    public PartId PartId => PartId.ImpactSwitch;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.For(PartId);
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate(); Trigger.Validate();
    }
}

public readonly record struct WorkshopLamp(GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, bool Locked = false) : IWorkshopInstance
{
    public PartId PartId => PartId.SignalLamp;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.For(PartId);
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate();
    }
}

public readonly record struct WorkshopBallDetector(
    GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, bool Locked = false) : IWorkshopDeclaredColliders
{
    public IReadOnlyList<ExtractedCollider> DeclaredColliders => DeclaredPhysics.Colliders(PartId);
    public PartId PartId => PartId.BallDetector;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.For(PartId);
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate();
        _ = DeclaredColliders;
    }
}
