using System;

namespace CuriousContraptions.Gpu;

public readonly record struct WorkshopPressurePlate(
    GpuBodyId Id,
    CellOrigin Cell,
    LocalPosition Local,
    CanonicalRotation Rotation,
    float MinimumMass = 0.5f,
    bool Locked = false) : IWorkshopInstance
{
    public PartId PartId => PartId.PressurePlate;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.None;

    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate();
        if (!float.IsFinite(MinimumMass) || MinimumMass < 0.1f || MinimumMass > 16.0f)
            throw new ArgumentException($"Pressure plate minimum mass '{MinimumMass}' is outside admitted range [0.1, 16].");
    }
}
