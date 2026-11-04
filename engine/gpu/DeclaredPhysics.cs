using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.Gpu;

/// <summary>One declared run-time control of a part: its RunControl parameter and the one lane that parameter binds.</summary>
public readonly record struct DeclaredRunControl(CuriousContraptions.Abstractions.ParameterId Parameter, CuriousContraptions.Abstractions.RunControlLane Lane);

/// <summary>The immutable physical declarations baked into the process's element bundle.</summary>
public static class DeclaredPhysics
{
    private static ImmutableDictionary<PartId, BundleEntry>? _entries;
    private static string? _identity;
    private static readonly object Gate = new();

    public static IReadOnlyDictionary<PartId, BundleEntry> Entries => _entries ??
        throw new InvalidOperationException("The physical element bundle is not installed.");

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

    public static IReadOnlyDictionary<string, float> Parameters(IWorkshopInstance instance) =>
        ((WorkshopGenericInstance)instance).Parameters.ToBundleValues();

    public static IReadOnlyList<ExtractedCollider> Colliders(PartId part, IReadOnlyDictionary<string, float>? parameters = null) =>
        BundleGeometry.Colliders(Of(part), parameters).Select(Admit).ToArray();

    public static ExtractedCollider Admit(CuriousContraptions.Abstractions.ColliderDescriptor descriptor)
    {
        var shape = descriptor.ShapeKind switch
        {
            CuriousContraptions.Abstractions.ColliderShapeKind.Box => ColliderShapeKind.Box,
            CuriousContraptions.Abstractions.ColliderShapeKind.Sphere => ColliderShapeKind.Sphere,
            CuriousContraptions.Abstractions.ColliderShapeKind.Cylinder => ColliderShapeKind.Cylinder,
            CuriousContraptions.Abstractions.ColliderShapeKind.Tube => ColliderShapeKind.Tube,
            CuriousContraptions.Abstractions.ColliderShapeKind.Torus => ColliderShapeKind.Torus,
            CuriousContraptions.Abstractions.ColliderShapeKind.Frustum => ColliderShapeKind.Frustum,
            _ => throw new ArgumentException("Unsupported baked collider shape.")
        };
        var p = descriptor.Offset;
        var q = descriptor.Rotation;
        var h = descriptor.HalfExtents;
        // A baked cylinder is (r, h, r) about its local +Y axis; unequal radial lanes are no cylinder (as a sphere is uniform).
        if (shape == ColliderShapeKind.Cylinder && !(h.X == h.Z))
            throw new ArgumentException("A baked cylinder's radial half-extents must be equal.");
        // The hollow kinds carry their lanes unchanged: the descriptor's radius and half-extents are the declaration's.
        var hollow = shape is ColliderShapeKind.Tube or ColliderShapeKind.Torus or ColliderShapeKind.Frustum;
        return new(shape, new(new(p.X, p.Y, p.Z), new(q.X, q.Y, q.Z, q.W)),
            hollow ? descriptor.Radius : shape is ColliderShapeKind.Sphere or ColliderShapeKind.Cylinder ? h.X : 0f,
            shape switch
            {
                ColliderShapeKind.Box => new(h.X, h.Y, h.Z),
                ColliderShapeKind.Cylinder => new(0f, h.Y, 0f),
                ColliderShapeKind.Tube or ColliderShapeKind.Torus or ColliderShapeKind.Frustum => new(h.X, h.Y, h.Z),
                _ => default
            });
    }

    public static bool Contains(PartId part) => _entries?.ContainsKey(part) == true;

    /// <summary>The finite store the part's Source supply capability declares; null for every other part.</summary>
    public static CuriousContraptions.Abstractions.Electrical.SupplySourceStoreDescriptor? SourceStore(PartId part) =>
        Of(part).Electrical?.Supply?.Store;

    /// <summary>The part's declared run-time controls, in parameter order: each RunControl parameter with the one lane it binds.</summary>
    public static IReadOnlyList<DeclaredRunControl> RunControls(PartId part) => RunControls(Of(part));

    /// <summary>This declaration's run-time controls. The admitted bundle binds each RunControl parameter to exactly one lane.</summary>
    public static IReadOnlyList<DeclaredRunControl> RunControls(BundleEntry entry) => entry.Metadata.Parameters
        .Where(parameter => parameter.RunEdit == CuriousContraptions.Abstractions.ParameterRunEdit.RunControl)
        .Select(parameter => new DeclaredRunControl(new(parameter.Key), RunControlBindings.Lanes(entry, parameter.Key).Single())).ToArray();
    /// <summary>Explicit package capability; collision geometry never determines capture eligibility.</summary>
    public static bool IsPayload(PartId part) => Of(part).Mechanical?.Payload == PayloadPolicy.CaptureTarget;

    public static BundleEntry Of(PartId part) =>
        _entries is { } entries && entries.TryGetValue(part, out var entry) ? entry
            : throw new InvalidOperationException($"No baked physics declaration exists for '{part}'.");
}
