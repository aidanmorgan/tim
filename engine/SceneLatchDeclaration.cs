using System;

namespace CuriousContraptions;

public sealed class LatchSlot;
public readonly record struct SceneLatchKey(MachinePart Owner,LatchSlot Slot);
public readonly record struct SceneLatchDeclaration(SceneLatchKey Key)
{
    internal void Validate()
    {
        if(Key.Owner is null||Key.Slot is null)
            throw new ArgumentException("Latch declaration requires an owner and slot.");
    }
}
