using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace CuriousContraptions.Gpu;

internal static class GasSceneValidation
{
    public static void Validate(ReadOnlySpan<RigidBodyDeclaration> bodies, ReadOnlySpan<ColliderDeclaration> colliders,
        ReadOnlySpan<GasStoreDeclaration> stores, ReadOnlySpan<GasShapeDeclaration> shapes,
        ReadOnlySpan<InventoryMassDeclaration> masses, ReadOnlySpan<AmbientLoadDeclaration> loads,
        ReadOnlySpan<ContactIntegrityDeclaration> observers, ulong nextIdentity, HashSet<ulong> identities)
    {
        var bodyMap = bodies.ToArray().ToDictionary(b => b.Id);
        var colliderMap = colliders.ToArray().ToDictionary(c => c.Id);
        var storeMap = new Dictionary<GasStoreId, GasStoreDeclaration>();
        foreach (var store in stores)
        {
            store.Validate();
            if (store.Id.Value >= nextIdentity || !identities.Add(store.Id.Value) || !storeMap.TryAdd(store.Id, store))
                throw new ArgumentException("Gas store identity must be allocated exactly once.");
            DynamicBody(bodyMap, store.Body);
        }
        ValidateMasses(masses, storeMap, bodyMap);
        ValidateShapes(shapes, storeMap, colliderMap);
        ValidateLoads(loads, storeMap, bodyMap);
        ValidateObservers(observers, storeMap, colliderMap, nextIdentity, identities);
    }

    private static void ValidateMasses(ReadOnlySpan<InventoryMassDeclaration> masses,
        Dictionary<GasStoreId, GasStoreDeclaration> stores, Dictionary<GpuBodyId, RigidBodyDeclaration> bodies)
    {
        var owners = new HashSet<GpuBodyId>();
        foreach (var source in masses)
        {
            source.Validate();
            var body = DynamicBody(bodies, source.Body);
            var gas = stores.Values.Where(s => s.Body == source.Body).OrderBy(s => s.Id.Value).ToArray();
            var referenceMass = 0f;
            foreach (var store in gas) referenceMass += store.Reference.Mass;
            if (!owners.Add(source.Body) || gas.Length == 0 || source.DryMass != body.Mass.Value - referenceMass)
                throw new ArgumentException("Inventory mass must own exactly its body's reference inventories.");
        }
        foreach (var store in stores.Values)
            if (!owners.Contains(store.Body)) throw new ArgumentException("Gas inventory has no mass source.");
    }

    private static void ValidateShapes(ReadOnlySpan<GasShapeDeclaration> shapes,
        Dictionary<GasStoreId, GasStoreDeclaration> stores, Dictionary<GpuColliderId, ColliderDeclaration> colliders)
    {
        var writers = new HashSet<GpuColliderId>();
        foreach (var shape in shapes)
        {
            shape.Validate();
            if (!stores.TryGetValue(shape.Store, out var store) || !colliders.TryGetValue(shape.Collider, out var collider) ||
                collider.Body != store.Body || collider.Shape != ColliderShapeKind.Sphere ||
                collider.Extrusion is not null || !writers.Add(shape.Collider))
                throw new ArgumentException("Gas shape requires a unique sphere on its inventory's body.");
            var radius = IsobaricGas.OuterRadius(shape.DryRadius, store.Prepared.State.Volume);
            if (collider.Radius.Value != radius)
                throw new ArgumentException("Gas collider must use its prepared physical dimensions.");
        }
    }

    private static void ValidateLoads(ReadOnlySpan<AmbientLoadDeclaration> loads,
        Dictionary<GasStoreId, GasStoreDeclaration> stores, Dictionary<GpuBodyId, RigidBodyDeclaration> bodies)
    {
        var writers = new HashSet<GpuBodyId>();
        foreach (var load in loads)
        {
            load.Validate();
            var body = DynamicBody(bodies, load.Body);
            if (!writers.Add(load.Body) || (load.InventoryFraction is { } id &&
                (!stores.TryGetValue(id, out var store) || store.Body != load.Body)))
                throw new ArgumentException("Ambient load requires one exact body and optional owned inventory.");
            var gravity = new Vector3(body.Gravity.X, body.Gravity.Y, body.Gravity.Z);
            if ((gravity + load.Acceleration).LengthSquared() > 256f ||
                !float.IsFinite(body.LinearDrag.Value + load.LinearDrag))
                throw new ArgumentException("Combined ambient load leaves the physical domain.");
        }
    }

    private static void ValidateObservers(ReadOnlySpan<ContactIntegrityDeclaration> observers,
        Dictionary<GasStoreId, GasStoreDeclaration> stores, Dictionary<GpuColliderId, ColliderDeclaration> colliders,
        ulong nextIdentity, HashSet<ulong> identities)
    {
        var outlets = new HashSet<GasStoreId>();
        foreach (var observer in observers)
        {
            observer.Validate();
            if (observer.Id.Value >= nextIdentity || !identities.Add(observer.Id.Value) || !outlets.Add(observer.Store) ||
                !stores.TryGetValue(observer.Store, out var store) ||
                !colliders.TryGetValue(observer.Collider, out var collider) || collider.Body != store.Body)
                throw new ArgumentException("Integrity requires an exact collider and uniquely opened owned outlet.");
        }
    }

    private static RigidBodyDeclaration DynamicBody(Dictionary<GpuBodyId, RigidBodyDeclaration> bodies, GpuBodyId id)
    {
        if (!bodies.TryGetValue(id, out var body) || body.Motion != RigidMotionKind.Dynamic)
            throw new ArgumentException("Gas coupling requires its admitted dynamic body.");
        return body;
    }
}
