using System.Collections.Generic;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Abstractions.Electrical;

public interface IElectricalElement : IPuzzleElementDeclaration
{
    IReadOnlyList<ElectricalNodeDescriptor> ElectricalNodes { get; }
}

public interface ISupplyElement : IElectricalElement
{
    SupplyGraphDescriptor SupplyGraph { get; }
}
