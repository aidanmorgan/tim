using System;

namespace CuriousContraptions.Gpu;

public readonly record struct SpringboardSettings(float Stiffness, float Damping)
{
    public static SpringboardSettings Default => new(400f, .2f);
    public void Validate()
    {
        if (!float.IsFinite(Stiffness) || Stiffness is < 120f or > 1200f ||
            !float.IsFinite(Damping) || Damping is < 0f or > 8f)
            throw new ArgumentException("Unsupported passive spring settings.");
    }
}

public readonly record struct WorkshopSpringboard(GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, SpringboardSettings Settings, bool Locked = false) : IWorkshopInstance
{
    public WorkshopPartKind Kind => WorkshopPartKind.Springboard;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.None;
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate(); Settings.Validate();
    }
}
