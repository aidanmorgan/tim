using System;
using CuriousContraptions.Abstractions.Electrical;
using System.Collections.Generic;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Runtime;

public sealed class CoreSupplyQueries : ISupplyQueries
{
    public ISupplyTopology CreateTopology(SupplyNodeDeclaration[] nodes, SupplyEdge[] edges) => new SupplyTopology(nodes, edges);
    public GpuBodyId[] NonmonotoneCycle(IEnumerable<(GpuBodyId Source, GpuBodyId Target)> edges, Func<GpuBodyId, LogicGateOperation?> operation) => SupplyCycles.NonmonotoneCycle(edges, operation);
}
