using System;

namespace CuriousContraptions.Physics;

public readonly record struct PhysicsGasNodeId
{
    public int Index { get; }
    public PhysicsGasNodeId(int index)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        Index = index;
    }
}

/// <summary>One immutable gas inventory associated with an owned mechanical body.
/// Distinct chambers on the same body retain distinct typed identities.</summary>
public sealed record PhysicsGasNode
{
    public PhysicsGasNodeId Id { get; }
    public PhysicsBodyId Owner { get; }
    public SealedGasState State { get; }

    public PhysicsGasNode(PhysicsGasNodeId id, PhysicsBodyId owner, SealedGasState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Id = id;
        Owner = owner;
        State = state;
    }
}
