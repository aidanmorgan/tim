using System;
using CuriousContraptions.Abstractions.Electrical;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

public interface ISupplyTopology
{
    IReadOnlyList<SupplyNodeDeclaration> Nodes { get; }
    IReadOnlyList<SupplyEdge> Edges { get; }
    GpuBodyId[] UpstreamSources(GpuBodyId target);
}

public interface ISupplyQueries
{
    ISupplyTopology CreateTopology(SupplyNodeDeclaration[] nodes, SupplyEdge[] edges);
    GpuBodyId[] NonmonotoneCycle(IEnumerable<(GpuBodyId Source, GpuBodyId Target)> edges, Func<GpuBodyId, LogicGateOperation?> operation);
}
