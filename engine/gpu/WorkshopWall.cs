using System;

namespace CuriousContraptions.Gpu;

/// <summary>Canonical local-axis panel lengths, independent of scene scale.</summary>
public readonly record struct WallDimensions(Metres Width, Metres Height, Metres Thickness)
{
    public static WallDimensions Default => new(new((Half)3), new((Half)2), new((Half).25));
    public static WallDimensions Minimum => new(new((Half).4), new((Half).4), new((Half).12));
    public static WallDimensions Maximum => new(new((Half)8), new((Half)6), new((Half)2));
    public void Validate()
    {
        PhysicsDeclarationBounds.Range(Width.Value, Minimum.Width.Value, Maximum.Width.Value);
        PhysicsDeclarationBounds.Range(Height.Value, Minimum.Height.Value, Maximum.Height.Value);
        PhysicsDeclarationBounds.Range(Thickness.Value, Minimum.Thickness.Value, Maximum.Thickness.Value);
    }
    /// <summary>Named pointer-input boundary: preserve finite source clamp before binary16 conversion.</summary>
    public static WallDimensions FromInput(float width, float height, float thickness)
    {
        if (!float.IsFinite(width) || !float.IsFinite(height) || !float.IsFinite(thickness))
            throw new ArgumentException("Nonfinite wall dimensions.");
        var result = new WallDimensions(
            new((Half)Math.Clamp(width, (float)Minimum.Width.Value, (float)Maximum.Width.Value)),
            new((Half)Math.Clamp(height, (float)Minimum.Height.Value, (float)Maximum.Height.Value)),
            new((Half)Math.Clamp(thickness, (float)Minimum.Thickness.Value, (float)Maximum.Thickness.Value)));
        result.Validate(); return result;
    }
}

public readonly record struct WorkshopWall(GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, WallDimensions Dimensions, bool Locked = false) : IWorkshopInstance
{
    public WorkshopPartKind Kind => WorkshopPartKind.Wall;
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate(); Dimensions.Validate();
    }
}
