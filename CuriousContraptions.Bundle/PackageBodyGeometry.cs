using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Bundle;

/// <summary>The one resolver of a package body's actual collider list in the canonical root/slider/hinge body ordering: the entry's
/// colliders for body zero, the payload's baked list for a moving body. Null when the index names no moving body or no baked list.</summary>
internal static class PackageBodyGeometry
{
    public static IReadOnlyList<ColliderDescriptor>? Colliders(BundleEntry entry, PackageBodyIndex body)
    {
        if (body.Value == 0) return entry.Colliders;
        if (entry.Mechanical is not { } mechanical) return null;
        var index = body.Value - 1;
        var moving = Math.Min(mechanical.Sliders.Count + mechanical.Hinges.Count, mechanical.MovingBodyColliders.Count);
        return index >= 0 && index < moving ? mechanical.MovingBodyColliders[index] : null;
    }
}
