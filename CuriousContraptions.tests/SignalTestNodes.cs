using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Existing single-node fixtures resolve their sole declared node, never equate node and owner IDs.
/// Composed fixtures must select their explicit package-local identities instead.</summary>
internal static class SignalTestNodes
{
    public static ActivationNodeId Only(ActivationNetwork network, GpuBodyId owner) =>
        Assert.Single(network.Nodes.ToArray(), node => node.Owner == owner).Id;
    public static ActivationNodeId Only(PhysicsActivationRead read, GpuBodyId owner) =>
        Assert.Single(Enumerable.Range(0, read.Count).Select(i => read[i]), node => node.Owner == owner).Node;
}
