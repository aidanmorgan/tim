using System;

namespace CuriousContraptions.Gpu;

public static class ActivationMemoryTransitions
{
    public static (ActivationMemory Memory, bool Crossed) CountInput(this ActivationMemory memory, uint target, ActivationTime time)
    {
        memory.Validate();
        time.Validate();
        if (memory.Kind != ActivationNodeKind.Counter || target == 0 || time < memory.Last)
            throw new ArgumentException("Invalid counter transition.");
        var count = memory.Count == uint.MaxValue ? memory.Count : memory.Count + 1;
        return (memory with { Count = count, Last = time }, memory.Count < target && count >= target);
    }
}
