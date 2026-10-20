using System;

namespace CuriousContraptions.Gpu;

public readonly record struct WorkshopPressurePlate(
    GpuBodyId Id,
    CellOrigin Cell,
    LocalPosition Local,
    CanonicalRotation Rotation,
    float MinimumMass,
    bool Locked = false) : IWorkshopInstance
{
    /// <summary>The minimum mass a newly placed plate starts with in the Workshop UI; no decoder or factory defaults to it.</summary>
    public const float PlacedMinimumMass = 0.5f;
    /// <summary>CAT-052 §2 contact distance d: the x footprint margin and the largest contact gap that counts as resting.</summary>
    public const float ContactDistance = 0.0001f;
    /// <summary>CAT-052 §2: the z footprint margin (10⁻⁶ m).</summary>
    public const float FootprintDepthMargin = 1e-6f;
    /// <summary>CAT-052 §2: the minimum alignment of a qualifying contact normal with the face's +Y (1 − 10⁻⁶).</summary>
    public const float MinimumAlignment = 1f - 1e-6f;
    public PartId PartId => PartId.PressurePlate;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.None;

    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate();
        if (!float.IsFinite(MinimumMass) || MinimumMass < 0.1f || MinimumMass > 16.0f)
            throw new ArgumentException($"Pressure plate {Id.Value} minimum mass '{MinimumMass:R}' is outside admitted range [0.1, 16].");
    }
}
