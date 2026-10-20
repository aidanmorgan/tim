using System.Collections.Frozen;

namespace Ownership;

// Bounded current semantic examples from docs/engine-contracts.md#ownership-and-assemblies.
// These guards do not assign or qualify the complete member inventory.
public static class OwnershipPolicy
{
    public static readonly IReadOnlyDictionary<MemberId, AssemblyOwner> Expected =
        new Dictionary<MemberId, AssemblyOwner>
        {
            [new("F:CuriousContraptions.Presentation.AnimationBatch._slots")] = AssemblyOwner.AnimationKernel,
            [new("F:CuriousContraptions.Presentation.AnimationBatch._free")] = AssemblyOwner.AnimationKernel,
            [new("F:CuriousContraptions.Gpu.WorkshopSimulation._operation")] = AssemblyOwner.SimulationHost,
            [new("F:CuriousContraptions.MachineWorld._parts")] = AssemblyOwner.GodotPresenter,
            [new("F:CuriousContraptions.MachineWorld._bodies")] = AssemblyOwner.GodotPresenter
        };

    private static readonly MemberId AnimationFree = new("F:CuriousContraptions.Presentation.AnimationBatch._free");
    private static readonly FrozenSet<CallerId> AnimationFreeCallers = new HashSet<CallerId>
    {
        new("M:CuriousContraptions.Presentation.AnimationBatch.#ctor(System.Int32)"),
        new("M:CuriousContraptions.Presentation.AnimationBatch.RegisterSlot(CuriousContraptions.Presentation.AnimationBinding,CuriousContraptions.Presentation.AnimationBatch.Slot,CuriousContraptions.Presentation.AnimationValue,CuriousContraptions.Presentation.AnimationValue)~CuriousContraptions.Presentation.AnimationHandle"),
        new("M:CuriousContraptions.Presentation.AnimationBatch.Remove(CuriousContraptions.Presentation.AnimationHandle)"),
        new("M:CuriousContraptions.Presentation.AnimationBatch.FillFree"),
        // Story 9R.5b (reviewed): the registry's own growth resizes the free stack and pushes the new slots; the Animation kernel
        // still owns the storage and no reference leaves AnimationBatch.
        new("M:CuriousContraptions.Presentation.AnimationBatch.EnsureCapacity(System.Int32)")
    }.ToFrozenSet();

    public static void Validate(StateMember member, OwnershipAssignment assignment)
    {
        if (Expected.TryGetValue(member.Id, out var owner) && assignment.Owner != owner)
            throw new InvalidDataException("Source-derived ownership policy rejects this otherwise valid placement.");
        if (member.Id != AnimationFree) return;
        if (member.Mutability != StorageMutability.ReferencedStorage)
            throw new InvalidDataException("Readonly animation free-list array still has mutable contents.");
        if (member.Uses.Any(site => !AnimationFreeCallers.Contains(site.Caller)))
            throw new InvalidDataException("Animation free-list storage has an unreviewed caller or reference recipient.");
    }
}
