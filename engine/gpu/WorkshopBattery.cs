using System;

namespace CuriousContraptions.Gpu;

public readonly record struct ElectricalSourceSettings(Joules Capacity, Watts MaximumPower,
    float InitialFraction, ElectricalEnable Enabled)
{
    public static ElectricalSourceSettings Default => new(new(3600f), new(120f), 1f, ElectricalEnable.Enabled);
    public void Validate()
    {
        Capacity.Validate(ElectricalSupply.MaximumCapacity);
        if (Capacity.Value < 60f || !float.IsFinite(MaximumPower.Value) || MaximumPower.Value is < 10f or > 480f ||
            !float.IsFinite(InitialFraction) || InitialFraction is < 0f or > 1f || !Enum.IsDefined(Enabled))
            throw new ArgumentException("Invalid electrical source settings.");
    }
}

public readonly record struct WorkshopBattery(GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, ElectricalSourceSettings Settings, bool Locked = false) : IWorkshopInstance
{
    public WorkshopPartKind Kind => WorkshopPartKind.Battery;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.None;
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate();
        Settings.Validate();
    }
}
