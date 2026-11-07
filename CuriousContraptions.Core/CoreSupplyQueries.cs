using System;
using CuriousContraptions.Abstractions.Electrical;
using System.Collections.Generic;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Runtime;

public sealed class CoreSupplyQueries : ISupplyQueries
{
    public ISupplyTopology CreateTopology(SupplyNodeDeclaration[] nodes, SupplyEdge[] edges) => new SupplyTopology(nodes, edges);
}
