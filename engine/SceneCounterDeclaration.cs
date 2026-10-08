using System;

namespace CuriousContraptions;

public sealed class CounterSlot;
public readonly record struct SceneCounterKey(MachinePart Owner,CounterSlot Slot);
public readonly record struct SceneCounterDeclaration(SceneCounterKey Key,int Target)
{
    internal void Validate()
    {
        if(Key.Owner is null||Key.Slot is null||Target<1)
            throw new ArgumentException("Counter declaration requires an owner, slot and positive target.");
    }
}
