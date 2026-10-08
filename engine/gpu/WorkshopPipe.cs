using System;

namespace CuriousContraptions.Gpu;

/// <summary>Canonical straight-tube authoring; only axial length is editable.</summary>
public readonly record struct PipeDimensions(Metres Length)
{
    public static PipeDimensions Default => new(new((Half)3.6));
    public static Metres BoreRadius => new((Half).65);
    public static Metres BoreDiameter => new((Half)1.3);
    public void Validate() => PhysicsDeclarationBounds.Range(Length.Value, (Half)1, (Half)8);
    public AnnularProfile Profile
    {
        get
        {
            Validate();
            return new(new((Half)(Length.Value * (Half).5)), BoreRadius,
                new((Half).70), new((Half).78), new((Half).09));
        }
    }
    public static PipeDimensions FromInput(float length, float diameterY, float diameterZ)
    {
        if (!float.IsFinite(length) || !float.IsFinite(diameterY) || !float.IsFinite(diameterZ) ||
            (Half)diameterY != BoreDiameter.Value || (Half)diameterZ != BoreDiameter.Value)
            throw new ArgumentException("Only finite pipe length may change; the bore is fixed.");
        var result = new PipeDimensions(new((Half)Math.Clamp(length, 1, 8)));
        result.Validate(); return result;
    }
}

public readonly record struct WorkshopPipe(GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, PipeDimensions Dimensions, bool Locked = false) : IWorkshopInstance
{
    public WorkshopPartKind Kind => WorkshopPartKind.Pipe;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.None;
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate(); Dimensions.Validate();
    }
}
