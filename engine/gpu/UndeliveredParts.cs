using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>
/// Catalogue parts whose resources still ship but whose instance path (capture, ball material, workbench footprint and save
/// decode) is not delivered. This set is the single admission authority: the registry does not register them and the Free
/// palette does not offer them. Story 9A.14 deletes their resources and <see cref="PartId"/> members; Story 12.1 returns them
/// as real element packages by removing them from this set.
/// </summary>
public enum UndeliveredPart { Tennis, Balloon }

public static class UndeliveredParts
{
    public static IReadOnlyList<UndeliveredPart> All { get; } = new ReadOnlyCollection<UndeliveredPart>(Enum.GetValues<UndeliveredPart>());

    public static IReadOnlySet<PartId> Ids { get; } = All.Select(Id).ToFrozenSet();

    public static PartId Id(UndeliveredPart part) => part switch
    {
        UndeliveredPart.Tennis => PartId.Tennis,
        UndeliveredPart.Balloon => PartId.Balloon,
        _ => throw new ArgumentOutOfRangeException(nameof(part), part, "Unknown undelivered part.")
    };

    public static bool Contains(PartId id) => Ids.Contains(id);
}
