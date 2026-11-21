using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Gpu;

internal static class CoordinateTravelSceneValidation
{
    public static void Validate(IPhysicsSceneDeclaration scene, ulong nextIdentity, HashSet<ulong> ids)
    {
        if (scene.CoordinateTravelObservers.IsEmpty) return;
        var membership = new PackageBodyMembership(scene);
        var bodies = scene.Bodies.ToArray().Select(b => b.Id).ToHashSet();
        var hinges = scene.Revolutes.ToArray().ToDictionary(h => h.Id);
        foreach (var observer in scene.CoordinateTravelObservers)
        {
            observer.Validate();
            if (observer.Id.Value >= nextIdentity || !ids.Add(observer.Id.Value) ||
                !bodies.Contains(observer.Owner) || !hinges.TryGetValue(observer.Coordinate, out var hinge) ||
                !membership.Owns(observer.Owner, hinge.BodyB))
                throw new ArgumentException("Coordinate travel observer requires a unique identity and exact owned hinge.");
        }
    }
}
