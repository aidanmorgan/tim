using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.Gpu;

/// <summary>The immutable physical declarations baked into the process's element bundle.</summary>
public static class DeclaredPhysics
{
    private static ImmutableDictionary<PartId, BundleEntry>? _entries;
    private static string? _identity;
    private static readonly object Gate = new();

    public static void Install(IReadOnlyList<BundleEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var errors = BundleValidator.Validate(entries.Select(entry => new NamedEntry(entry.Metadata.Id, entry)).ToArray());
        if (errors.Count != 0) throw new ArgumentException(string.Join("; ", errors));
        var identity = BundleSchema.WriteEntries(entries);
        var admitted = entries.ToImmutableDictionary(entry => new PartId(entry.Metadata.Id));
        lock (Gate)
        {
            if (_identity is not null && _identity != identity)
                throw new InvalidOperationException("A different physical element bundle is already installed.");
            _entries = admitted;
            _identity = identity;
        }
    }

    public static IReadOnlyDictionary<string, float> Parameters(IWorkshopInstance instance) => instance switch
    {
        WorkshopPipe pipe => new Dictionary<string, float> { ["length"] = pipe.Length.Metres },
        WorkshopRamp ramp => new Dictionary<string, float> { ["length"] = (float)ramp.Dimensions.Length.Value, ["width"] = (float)ramp.Dimensions.Width.Value },
        WorkshopGenericInstance generic => generic.Parameters.ToBundleValues(),
        WorkshopSpringboard spring => new Dictionary<string, float> { ["stiffness"] = spring.Settings.Stiffness,
            ["damping"] = spring.Settings.Damping, ["initial_compression"] = spring.Settings.InitialCompression },
        _ => BundleGeometry.Parameters(Of(instance.PartId))
    };

    public static IReadOnlyList<ExtractedCollider> Colliders(PartId part, IReadOnlyDictionary<string, float>? parameters = null) =>
        BundleGeometry.Colliders(Of(part), parameters).Select(Admit).ToArray();

    public static ExtractedCollider Admit(CuriousContraptions.Abstractions.ColliderDescriptor descriptor)
    {
        var shape = descriptor.ShapeKind switch
        {
            CuriousContraptions.Abstractions.ColliderShapeKind.Box => ColliderShapeKind.Box,
            CuriousContraptions.Abstractions.ColliderShapeKind.Sphere => ColliderShapeKind.Sphere,
            _ => throw new ArgumentException("Unsupported baked collider shape.")
        };
        var p = descriptor.Offset;
        var q = descriptor.Rotation;
        var h = descriptor.HalfExtents;
        return new(shape, new(new((Half)p.X, (Half)p.Y, (Half)p.Z), new(q.X, q.Y, q.Z, q.W)),
            shape == ColliderShapeKind.Sphere ? (Half)h.X : (Half)0,
            shape == ColliderShapeKind.Box ? new((Half)h.X, (Half)h.Y, (Half)h.Z) : default);
    }

    public static bool Contains(PartId part) => _entries?.ContainsKey(part) == true;

    public static BundleEntry Of(PartId part) =>
        _entries is { } entries && entries.TryGetValue(part, out var entry) ? entry
            : throw new InvalidOperationException($"No baked physics declaration exists for '{part}'.");
}
