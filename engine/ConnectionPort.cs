using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CuriousContraptions;

public enum PortDirection { Input, Output, Bidirectional }
public enum RopeAttachmentKind { None, Load, Anchor, Guide }
public enum RopeState { Open, Slack, Taut }

/// <summary>Stable local socket identity; positions follow the part's transform.</summary>
public readonly record struct ConnectionPort(
    string Id, ConnectionDomain Domain, PortDirection Direction, Vector3 LocalPosition);

public readonly record struct ElectricalRoute(string Input, string Output);

/// <summary>Signed shaft ratio; negative ratios reverse the local shaft direction.</summary>
public readonly record struct MechanicalRoute(string Input, string Output, float Ratio);
public readonly record struct MechanicalSource(string Output, float RadiansPerSecond);

public static class ConnectionRules
{
    public static bool TryResolve(ConnectionSpec link, IEnumerable<ConnectionPort> sources,
        IEnumerable<ConnectionPort> targets, out ConnectionPort source, out ConnectionPort target)
    {
        source = target = default;
        if (link.Type == ConnectionDomain.Rope)
        {
            if (link.RopeLength is not { } length || !float.IsFinite(length) || length < .05f || length > 200)
                return false;
        }
        else if (link.RopeLength != null) return false;
        if (string.IsNullOrWhiteSpace(link.From) || string.IsNullOrWhiteSpace(link.To)
            || link.From == link.To) return false;
        var fromId = link.FromPort;
        var toId = link.ToPort;
        if (string.IsNullOrWhiteSpace(fromId) || string.IsNullOrWhiteSpace(toId)) return false;
        var from = sources.Where(p => p.Id == fromId).ToArray();
        var to = targets.Where(p => p.Id == toId).ToArray();
        if (from.Length != 1 || to.Length != 1) return false;
        source = from[0];
        target = to[0];
        return source.Domain != ConnectionDomain.Unknown && source.Domain == target.Domain && link.Type == source.Domain
            && source.Direction is PortDirection.Output or PortDirection.Bidirectional
            && target.Direction is PortDirection.Input or PortDirection.Bidirectional;
    }
}
