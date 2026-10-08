using System;

namespace CuriousContraptions.Gpu;

public enum WorkshopGpuOperation { Admit, Advance }
public readonly record struct WorkshopGpuProfile(
    SimulationCadence Cadence, PhysicalStepProfile Physical, CadenceRevision Revision)
{
    public uint Substeps => Cadence switch
    {
        SimulationCadence.Hz60 => 8,
        SimulationCadence.Hz120 => 4,
        SimulationCadence.Hz240 => 2,
        _ => throw new ArgumentException("Undefined GPU simulation cadence.")
    };
    public ulong RunTickLimit => WorkshopCadenceSettings.PhysicalOrdinalLimit / Substeps;
    public void Validate()
    {
        Revision.Validate();
        if (!Enum.IsDefined(Cadence) || Physical != PhysicalStepProfile.Canonical480Hz)
            throw new ArgumentException("Unsupported GPU physical cadence profile.");
    }
}
