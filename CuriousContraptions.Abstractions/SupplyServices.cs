using System;
using CuriousContraptions.Abstractions.Electrical;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

public interface ISupplyTopology
{
    IReadOnlyList<SupplyNodeDeclaration> Nodes { get; }
    IReadOnlyList<SupplyEdge> Edges { get; }
    ElectricalSourceId[] UpstreamSources(SupplyNodeId target);
}

public interface ISupplyQueries
{
    ISupplyTopology CreateTopology(SupplyNodeDeclaration[] nodes, SupplyEdge[] edges);
}

public enum SupplyTopologyRefusal : byte { InvalidConnection, InputSupplied, NonmonotoneFeedback }
public sealed class SupplyTopologyException : ArgumentException
{
    public SupplyTopologyRefusal Reason { get; }
    public SupplyTopologyException(SupplyTopologyRefusal reason, string message) : base(message) => Reason = reason;
}
