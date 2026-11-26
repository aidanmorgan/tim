using System.Collections.Generic;

namespace CuriousContraptions.Abstractions;

public interface IPuzzleElementDeclaration : ISceneGeometryElement
{
    ElementPresentation Presentation { get; }
    ElementMetadata Metadata { get; }
    ElementFootprint Footprint { get; }
    IReadOnlyList<PortDescriptor> Ports { get; }
    IReadOnlyList<EventDescriptor> Events { get; }
    SimulationDomain SupportedDomains { get; }
}
