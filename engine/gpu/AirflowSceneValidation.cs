using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>Validates resolved airflow handles before a scene can be installed.</summary>
internal static class AirflowSceneValidation
{
    public static void Validate(IPhysicsSceneDeclaration scene, ulong nextIdentity, HashSet<ulong> ids)
    {
        ValidateConsumerReferences(scene);
        if (scene.AirflowSources.IsEmpty && scene.AirflowReceivers.IsEmpty) return;
        var bodies = scene.Bodies;
        var colliders = scene.Colliders;
        var stores = scene.EnergyStores;
        var nodes = scene.SupplyGraph.Nodes;
        var hinges = scene.Revolutes;
        var sources = scene.AirflowSources;
        var airflowTargets = scene.AirflowReceivers;
        var bodyMap = bodies.ToArray().ToDictionary(b => b.Id);
        var colliderMap = colliders.ToArray().ToDictionary(c => c.Id);
        var membership = new PackageBodyMembership(scene);
        var storeMap = stores.ToArray().ToDictionary(s => s.Id);
        var nodeMap = nodes.ToArray().ToDictionary(n => n.Id);
        var hingeMap = hinges.ToArray().ToDictionary(h => h.Id);
        var senses = new Dictionary<RevoluteConstraintId, float>();
        foreach (var source in sources)
        {
            ArgumentNullException.ThrowIfNull(source);
            source.Validate();
            Identity(source.Id.Value, nextIdentity, ids);
            if (!bodyMap.ContainsKey(source.Owner) || !bodyMap.ContainsKey(source.Frame) ||
                !membership.Owns(source.Owner, source.Frame))
                throw new ArgumentException("Unresolved airflow owner/frame.");
            foreach (var excluded in source.InternalOccluders)
                if (!colliderMap.TryGetValue(excluded, out var collider) ||
                    !membership.Owns(source.Owner, collider.Body) || collider.Participation != CollisionParticipation.Enabled)
                    throw new ArgumentException("Airflow internal occluder is not an active owned collider.");
            Input(source, storeMap, nodeMap, hingeMap, membership, senses);
        }
        foreach (var receiver in airflowTargets)
        {
            ArgumentNullException.ThrowIfNull(receiver);
            receiver.Validate();
            Identity(receiver.Id.Value, nextIdentity, ids);
            if (!colliderMap.TryGetValue(receiver.Collider, out var collider) ||
                collider.Body != receiver.Owner || !bodyMap.TryGetValue(receiver.Owner, out var body) ||
                body.Motion != RigidMotionKind.Dynamic || collider.Participation != CollisionParticipation.Enabled)
                throw new ArgumentException("Airflow receiver requires its exact active dynamic collider.");
        }
    }


    private static void ValidateConsumerReferences(IPhysicsSceneDeclaration scene)
    {
        var sources = scene.AirflowSources.ToArray().ToDictionary(source => source.Id);
        var referenced = new HashSet<AirflowSourceId>();
        foreach (var node in scene.SupplyGraph.Nodes)
        {
            if (node.ContinuousWork.IsDefaultOrEmpty) continue;
            if (node.Kind != SupplyNodeKind.Consumer)
                throw new ArgumentException("Continuous work requires an electrical consumer node.");
            foreach (var id in node.ContinuousWork)
                if (!sources.TryGetValue(id, out var source) || source.Owner != node.Owner || !referenced.Add(id) ||
                    source.Input.Kind == ContinuousWorkInputKind.Electrical && source.Input.Electrical != node.Id)
                    throw new ArgumentException("Continuous-work consumer has a missing, foreign, duplicate or misrouted source.");
        }
    }


    private static void Identity(ulong id, ulong next, HashSet<ulong> ids)
    {
        if (id >= next || !ids.Add(id)) throw new ArgumentException("Airflow identity is not uniquely allocated.");
    }

    private static void Input(AirflowSourceDeclaration source,
        Dictionary<EnergyStoreId, EnergyStoreDeclaration> stores, Dictionary<SupplyNodeId, SupplyNodeDeclaration> nodes,
        Dictionary<RevoluteConstraintId, RevoluteConstraintDeclaration> hinges,
        PackageBodyMembership membership, Dictionary<RevoluteConstraintId, float> senses)
    {
        var input = source.Input;
        switch (input.Kind)
        {
            case ContinuousWorkInputKind.Store:
                if (!stores.ContainsKey(input.Store)) throw new ArgumentException("Unresolved airflow store.");
                break;
            case ContinuousWorkInputKind.Electrical:
                if (!nodes.TryGetValue(input.Electrical, out var consumer) || consumer.Kind != SupplyNodeKind.Consumer ||
                    consumer.ContinuousWork.IsDefault || !consumer.ContinuousWork.Contains(source.Id))
                    throw new ArgumentException("Unresolved exact airflow electrical consumer.");
                break;
            case ContinuousWorkInputKind.Coordinate:
                if (!hinges.TryGetValue(input.Coordinate, out var hinge) || !membership.Owns(source.Owner, hinge.BodyB))
                    throw new ArgumentException("Unresolved owned airflow coordinate.");
                if (senses.TryGetValue(input.Coordinate, out var sense) && sense != input.Sense)
                    throw new ArgumentException("Conflicting load senses.");
                senses[input.Coordinate] = input.Sense;
                break;
            default: throw new ArgumentException("Undefined airflow work input.");
        }
    }
}
