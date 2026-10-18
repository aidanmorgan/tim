using System.Collections.Generic;

namespace CuriousContraptions.Abstractions.Fluid;

public enum FluidPortRole : byte
{
    Intake = 0,
    Discharge = 1,
    Bidirectional = 2
}

public readonly record struct FluidPortDescriptor(
    string PortId,
    FluidPortRole Role,
    float MaxFlowRate,
    float OperatingPressure);

public readonly record struct FluidProperties(
    float BuoyancyCoefficient,
    float Volume,
    float FlowResistance);

public interface IFluidElement : IPuzzleElementDeclaration
{
    FluidProperties FluidProperties { get; }
    IReadOnlyList<FluidPortDescriptor> FluidPorts { get; }
}
