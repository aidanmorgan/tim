using System;

namespace CuriousContraptions.Gpu;

/// <summary>Explicit player input; a committed electrical phase applies this enable state.</summary>
public readonly record struct WorkshopElectricalControl(GpuBodyId Owner, ElectricalEnable Enabled)
{
    public void Validate()
    {
        if (Owner.Value == 0 || !Enum.IsDefined(Enabled)) throw new ArgumentException("Invalid source enable control.");
    }
}
