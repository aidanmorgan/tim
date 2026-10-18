using System;

namespace CuriousContraptions.Gpu;

public readonly record struct WorkshopCounter(
    GpuBodyId Id,
    CellOrigin Cell,
    LocalPosition Local,
    CanonicalRotation Rotation,
    int TargetCount = WorkshopCounter.DefaultTargetCount,
    bool Locked = false) : IWorkshopInstance
{
    public const int DefaultTargetCount = 3;
    public PartId PartId => PartId.Counter;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.None;

    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate();
        if (TargetCount < 1 || TargetCount > 9)
            throw new ArgumentException($"Counter target count '{TargetCount}' is outside admitted range [1, 9].");
    }
}
