using System;

namespace CuriousContraptions.Gpu;

public readonly record struct WorkshopClock(
    GpuBodyId Id,
    CellOrigin Cell,
    LocalPosition Local,
    CanonicalRotation Rotation,
    float IntervalSeconds = WorkshopClock.DefaultIntervalSeconds,
    bool Locked = false) : IWorkshopInstance
{
    public const float DefaultIntervalSeconds = 1.0f;
    public const float MinIntervalSeconds = 0.1f;
    public const float MaxIntervalSeconds = 12.0f;

    public PartId PartId => PartId.Clock;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.None;

    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate();
        if (float.IsNaN(IntervalSeconds) || float.IsInfinity(IntervalSeconds) ||
            IntervalSeconds < MinIntervalSeconds || IntervalSeconds > MaxIntervalSeconds)
        {
            throw new ArgumentException($"Clock interval seconds '{IntervalSeconds}' is outside admitted range [{MinIntervalSeconds}, {MaxIntervalSeconds}].");
        }
    }

    public uint Ticks(SimulationCadence cadence)
    {
        Validate();
        var rate = cadence switch
        {
            SimulationCadence.Hz60 => 60u,
            SimulationCadence.Hz120 => 120u,
            SimulationCadence.Hz240 => 240u,
            _ => throw new ArgumentException("Unsupported timer cadence.")
        };
        return (uint)Math.Ceiling(IntervalSeconds * rate);
    }
}
