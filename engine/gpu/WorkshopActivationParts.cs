using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

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
