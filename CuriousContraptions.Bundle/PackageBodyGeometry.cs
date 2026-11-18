using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Bundle;

/// <summary>The package body's actual collider list in the canonical root/slider/hinge body ordering.</summary>
internal static class PackageBodyGeometry
{
    public static IReadOnlyList<ColliderDescriptor>? Colliders(BundleEntry entry, PackageBodyIndex body)
    {
        if (body.Value == 0) return entry.Colliders;
        if (entry.Mechanical is not { } mechanical) return null;
        var index = body.Value - 1;
        if (index < 0) return null;
        if (index < mechanical.Sliders.Count) return mechanical.Sliders[index].MovingBody.Colliders;
        index -= mechanical.Sliders.Count;
        return index < mechanical.Hinges.Count ? mechanical.Hinges[index].MovingBody.Colliders : null;
    }
}
