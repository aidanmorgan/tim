using System.Collections.Generic;

namespace CuriousContraptions.Abstractions;

public interface IPuzzleElementDeclaration
{
    ElementMetadata Metadata { get; }
    ElementFootprint Footprint { get; }
    IReadOnlyList<PortDescriptor> Ports { get; }
    IReadOnlyList<ColliderDescriptor> Colliders { get; }
    IReadOnlyList<EventDescriptor> Events { get; }
    SimulationDomain SupportedDomains { get; }
}
