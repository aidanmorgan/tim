using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>
/// Catalogue parts whose resources still ship but whose instance path (capture, ball material, workbench footprint and save
/// decode) is not delivered. They never enter the registry or the Free palette. Story 9A.14 deletes their resources and
/// <see cref="PartId"/> members; Story 12.1 returns them as real element packages and removes them from this set.
/// </summary>
public enum UndeliveredPart { Tennis, Balloon }

public static class UndeliveredParts
{
    public static IReadOnlyList<UndeliveredPart> All { get; } = Enum.GetValues<UndeliveredPart>();

    public static PartId Id(UndeliveredPart part) => part switch
    {
        UndeliveredPart.Tennis => PartId.Tennis,
        UndeliveredPart.Balloon => PartId.Balloon,
        _ => throw new ArgumentOutOfRangeException(nameof(part), part, "Unknown undelivered part.")
    };

    public static bool Contains(PartId id) => All.Any(part => Id(part) == id);
}
