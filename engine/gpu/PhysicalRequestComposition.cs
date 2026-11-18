using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>Consumes only committed occurrence sinks, once, at the next physical boundary.</summary>
public static class PhysicalRequestComposition
{
    public static PhysicalRequestInput[] Compose(IActivationProgram network, PhysicsActivationRead committed)
    {
        // A tick without a latched request allocates nothing.
        if (!AnyLatchedRequest(network, committed)) return [];
        var requests = new Dictionary<PhysicalRequestTarget, PhysicalRequestInput>();
        foreach (var node in network.Nodes)
        {
            if (node.Kind != ActivationNodeKind.PhysicalRequest) continue;
            for (var i = 0; i < committed.Count; i++)
            {
                var latch = committed[i];
                if (latch.Node != node.Id || latch.Phase != ActivationPhase.Latched) continue;
                var request = new PhysicalRequestInput(node.RequestTarget, node.RequestOperation!.Value, latch.Occurrence);
                if (!requests.TryGetValue(node.RequestTarget, out var prior) || Earlier(request.Occurrence, prior.Occurrence))
                    requests[node.RequestTarget] = request;
            }
        }
        return requests.Values.OrderBy(request => request.Target.Kind).ThenBy(request => request.Target.Retention.Value).ThenBy(request => request.Target.Actuator.Value).ToArray();
    }

    private static bool AnyLatchedRequest(IActivationProgram network, PhysicsActivationRead committed)
    {
        foreach (var node in network.Nodes)
        {
            if (node.Kind != ActivationNodeKind.PhysicalRequest) continue;
            for (var i = 0; i < committed.Count; i++)
                if (committed[i].Node == node.Id && committed[i].Phase == ActivationPhase.Latched) return true;
        }
        return false;
    }

    private static bool Earlier(ActivationOccurrence a, ActivationOccurrence b)
    {
        var time = a.Time.CompareTo(b.Time);
        if (time != 0) return time < 0;
        var emitter = a.Emitter.Value.CompareTo(b.Emitter.Value);
        if (emitter != 0) return emitter < 0;
        var body = a.Cause.Body.Value.CompareTo(b.Cause.Body.Value);
        return body != 0 ? body < 0 : a.Cause.Collider.Value < b.Cause.Collider.Value;
    }
}
