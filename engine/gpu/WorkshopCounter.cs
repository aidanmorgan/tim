using System;

namespace CuriousContraptions.Gpu;

public readonly record struct WorkshopCounter(
    GpuBodyId Id,
    CellOrigin Cell,
    LocalPosition Local,
    CanonicalRotation Rotation,
    int TargetCount,
    bool Locked = false) : IWorkshopInstance
{
    /// <summary>The target count a newly placed counter starts with in the Workshop UI; no decoder or factory defaults to it.</summary>
    public const int PlacedTargetCount = 3;
    public const int MinimumTargetCount = 1;
    public const int MaximumTargetCount = 9;
    public PartId PartId => PartId.Counter;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.For(PartId);

    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate();
        if (TargetCount < MinimumTargetCount || TargetCount > MaximumTargetCount)
            throw new ArgumentException($"Counter {Id.Value} target count '{TargetCount}' is outside admitted range [{MinimumTargetCount}, {MaximumTargetCount}].");
    }
}
