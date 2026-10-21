using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>
/// Catalogue elements that are not delivered: they have no package and no instance path (capture, ball material, workbench
/// footprint and save decode). This set is the single admission authority: the registry never registers them and the Free palette
/// never offers them, even if a bundle declares one. Story 9A.14 deleted their catalogue resources and <see cref="PartId"/>
/// constants; Story 12.1 ships their packages and removes them from this set.
/// </summary>
public enum UndeliveredPart { Tennis, Balloon }

public static class UndeliveredParts
{
    public static IReadOnlyList<UndeliveredPart> All { get; } = new ReadOnlyCollection<UndeliveredPart>(Enum.GetValues<UndeliveredPart>());

    public static IReadOnlySet<PartId> Ids { get; } = All.Select(Id).ToFrozenSet();

    /// <summary>The element id the part's Story 12.1 package will declare.</summary>
    public static PartId Id(UndeliveredPart part) => part switch
    {
        UndeliveredPart.Tennis => new("tennis"),
        UndeliveredPart.Balloon => new("balloon"),
        _ => throw new ArgumentOutOfRangeException(nameof(part), part, "Unknown undelivered part.")
    };

    public static bool Contains(PartId id) => Ids.Contains(id);
}
