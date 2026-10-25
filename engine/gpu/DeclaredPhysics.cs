using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CuriousContraptions.Abstractions.Mechanical;
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
        // A package whose colliders would take an identity one of its own rows holds is a package defect, refused here, once, when
        // the bundle loads.
        var conflicts = entries.Select(entry => (entry.Metadata.Id, Conflict: WorkshopPhysicsCompiler.ColliderSlots(entry)))
            .Where(item => item.Conflict is not null).Select(item => (item.Id, Conflict: item.Conflict!.Value)).ToArray();
        if (conflicts.Length != 0)
            throw new ArgumentException(string.Join("; ", conflicts.Select(item =>
                $"'{item.Id}': colliders at part base + {item.Conflict.FirstCollider} to + {item.Conflict.LastCollider}" +
                (item.Conflict.MovingBody is { } slider ? $" (slider {slider}'s moving body)" : "") +
                $" reach its {item.Conflict.Row} row at + {item.Conflict.Slot}")));
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
        WorkshopGenericInstance generic => generic.Parameters.ToBundleValues(),
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
            CuriousContraptions.Abstractions.ColliderShapeKind.Cylinder => ColliderShapeKind.Cylinder,
            _ => throw new ArgumentException("Unsupported baked collider shape.")
        };
        var p = descriptor.Offset;
        var q = descriptor.Rotation;
        var h = descriptor.HalfExtents;
        // A baked cylinder is (r, h, r) about its local +Y axis; unequal radial lanes are no cylinder (as a sphere is uniform).
        if (shape == ColliderShapeKind.Cylinder && !(h.X == h.Z))
            throw new ArgumentException("A baked cylinder's radial half-extents must be equal.");
        return new(shape, new(new(p.X, p.Y, p.Z), new(q.X, q.Y, q.Z, q.W)),
            shape is ColliderShapeKind.Sphere or ColliderShapeKind.Cylinder ? h.X : 0f,
            shape switch
            {
                ColliderShapeKind.Box => new(h.X, h.Y, h.Z),
                ColliderShapeKind.Cylinder => new(0f, h.Y, 0f),
                _ => default
            });
    }

    public static bool Contains(PartId part) => _entries?.ContainsKey(part) == true;

    /// <summary>The finite store the part's Source supply capability declares; null for every other part.</summary>
    public static CuriousContraptions.Abstractions.Electrical.SupplySourceStoreDescriptor? SourceStore(PartId part) =>
        Of(part).Electrical?.Supply?.Store;

    /// <summary>Whether the part declares a run-time control: its Source store's enable state is a RunControl parameter.</summary>
    public static bool HasRunControlledSource(PartId part) => HasRunControlledSource(Of(part));

    /// <summary>Whether this declaration's Source store declares its enable state run-editable.</summary>
    public static bool HasRunControlledSource(BundleEntry entry) => entry.Electrical?.Supply?.Store is { } store &&
        entry.Metadata.Parameters.Any(parameter => parameter.Key == store.Enable &&
            parameter.RunEdit == CuriousContraptions.Abstractions.ParameterRunEdit.RunControl);

    /// <summary>The payload: the rolling thing the player delivers, which a capture owner guides and captures and a lesson's contact
    /// triggers and finite-work storage act on. Derived from the admitted declaration, never declared: a dynamic body of exactly one
    /// sphere collider and no slider. A dynamic box (the domino tile) or a slider body is dynamic but no payload.</summary>
    public static bool IsPayload(PartId part)
    {
        var entry = Of(part);
        return entry.Mechanical is { KinematicMode: KinematicMode.Dynamic, Sliders.Count: 0 } && entry.Colliders.Count == 1 &&
            entry.Colliders[0].ShapeKind == CuriousContraptions.Abstractions.ColliderShapeKind.Sphere;
    }

    public static BundleEntry Of(PartId part) =>
        _entries is { } entries && entries.TryGetValue(part, out var entry) ? entry
            : throw new InvalidOperationException($"No baked physics declaration exists for '{part}'.");
}
