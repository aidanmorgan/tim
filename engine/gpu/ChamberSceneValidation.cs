using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Gpu;

internal static class ChamberSceneValidation
{
    public static (ChamberRegionDeclaration[] Regions, FiniteWorkActuatorDeclaration[] Actuators) Validate(
        ReadOnlySpan<ChamberRegionDeclaration> regions, ReadOnlySpan<FiniteWorkActuatorDeclaration> actuators,
        ReadOnlySpan<RigidBodyDeclaration> bodies, ReadOnlySpan<ColliderDeclaration> colliders,
        ReadOnlySpan<EnergyStoreDeclaration> stores, ulong nextIdentity, HashSet<ulong> identities)
    {
        var bodyMap = bodies.ToArray().ToDictionary(body => body.Id);
        var colliderMap = colliders.ToArray().ToDictionary(collider => collider.Id);
        var regionIds = new HashSet<ChamberRegionId>();
        foreach (var region in regions)
        {
            region.Validate();
            if (region.Id.Value >= nextIdentity || !identities.Add(region.Id.Value) || !regionIds.Add(region.Id))
                throw new ArgumentException("Duplicate or unowned chamber identity.");
            if (!bodyMap.ContainsKey(region.Frame) || !colliderMap.TryGetValue(region.ParticipationCollider, out var collider) ||
                collider.Body != region.Frame)
                throw new ArgumentException("Chamber frame does not own its participation collider.");
            if (region.Targets.Kind == BodyTargetKind.NamedBody &&
                (!bodyMap.TryGetValue(region.Targets.Body, out var target) || target.Motion != RigidMotionKind.Dynamic))
                throw new ArgumentException("Chamber names no dynamic target.");
        }
        var storeIds = stores.ToArray().Select(store => store.Id).ToHashSet();
        foreach (var actuator in actuators)
        {
            actuator.Validate();
            if (actuator.Id.Value >= nextIdentity || !identities.Add(actuator.Id.Value))
                throw new ArgumentException("Duplicate or unowned finite-work actuator identity.");
            if (!regionIds.Contains(actuator.Region) || !storeIds.Contains(actuator.Store))
                throw new ArgumentException("Finite-work actuator names an absent region or reservoir.");
        }
        return (regions.ToArray().OrderBy(region => region.Id.Value).ToArray(),
            actuators.ToArray().OrderBy(actuator => actuator.Id.Value).ToArray());
    }
}
