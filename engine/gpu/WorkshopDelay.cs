using System;

namespace CuriousContraptions.Gpu;

/// <summary>Authored Half seconds; exact integer conversion exists only at the discrete scheduling boundary.</summary>
public readonly record struct DelayDuration(DurationSeconds Seconds)
{
    public static DelayDuration Default => new(new((Half)1));
    public void Validate() => PhysicsDeclarationBounds.Range(Seconds.Value, (Half).1, (Half)12);
    public static DelayDuration FromInput(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < (double)(Half).1 || seconds > 12)
            throw new ArgumentException("Delay duration is outside its authored range.");
        var result = new DelayDuration(new((Half)seconds)); result.Validate(); return result;
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
        var bits = BitConverter.HalfToUInt16Bits(Seconds.Value);
        var exponent = (bits >> 10) & 31;
        var significand = checked((uint)((bits & 1023) | 1024));
        // All admitted durations are positive normal Half values below 16.
        var denominator = 1u << (25 - exponent);
        var numerator = checked(significand * rate);
        return checked((numerator + denominator - 1) / denominator);
    }
}

public readonly record struct WorkshopDelay(GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, DelayDuration Duration, bool Locked = false) : IWorkshopInstance
{
    public WorkshopPartKind Kind => WorkshopPartKind.Delay;
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate(); Duration.Validate();
    }
}
