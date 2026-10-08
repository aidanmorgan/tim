using System;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Bridge;

public readonly record struct BooleanObservationSlot
{
    public int Index { get; }
    public BooleanObservationSlot(int index)
    {
        if(index<0)throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}
public readonly record struct BooleanReadKey(PhysicsBodyId Owner,BooleanObservationSlot Slot);
/// <summary>A typed Boolean observation, never encoded as a numeric scalar or display string.</summary>
public readonly record struct BooleanRead(BooleanReadKey Key,bool Value)
{
    internal void Validate(ReadOnlySpan<BodyPublicationRead> bodies)
    {
        if(Key.Owner.Index>=bodies.Length||bodies[Key.Owner.Index].Query.Owner!=Key.Owner)
            throw new ArgumentException("Boolean observation requires a root owner.");
    }
}
