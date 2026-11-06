using System.Collections.Generic;

namespace CuriousContraptions.Abstractions.Fluid;

public interface IFluidElement : IPuzzleElementDeclaration
{
    FluidProperties FluidProperties { get; }
    IReadOnlyList<FluidPortDescriptor> FluidPorts { get; }
}
