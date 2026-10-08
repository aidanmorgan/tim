using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CuriousContraptions;

public enum ActivationCommand { Trigger, Set, Reset }

public enum PortDirection { Input, Output, Bidirectional }
public enum RopeAttachmentKind { None, Load, Anchor, Guide }
public enum RopeState { Open, Slack, Taut }

/// <summary>Stable local socket identity; positions follow the part's transform.</summary>
public readonly record struct ConnectionPort(
    SocketId Id, ConnectionDomain Domain, PortDirection Direction, Vector3 LocalPosition,
    ActivationCommand Command = ActivationCommand.Trigger);

public readonly record struct ElectricalGate(LogicGateKind Operation, SocketId First, SocketId Second, SocketId Supply, SocketId Output);

public readonly record struct ElectricalRoute(SocketId Input, SocketId Output, ElectricalContactSignal Signal);

/// <summary>Socket radians map to an owned axial coordinate. Ratios/signs are
/// declared once; the runtime never propagates speeds or work allowances.</summary>
public sealed record MechanicalBinding
{
    public SocketId Port { get; }
    public JointSlot Joint { get; }
    public double CoordinatePerRadian { get; }
    public MechanicalBinding(SocketId port,JointSlot joint,double coordinatePerRadian)
    {
        if(!Enum.IsDefined(port)) throw new ArgumentOutOfRangeException(nameof(port));
        ArgumentNullException.ThrowIfNull(joint);
        if(!double.IsFinite(coordinatePerRadian)||coordinatePerRadian==0)
            throw new ArgumentOutOfRangeException(nameof(coordinatePerRadian));
        Port=port;Joint=joint;CoordinatePerRadian=coordinatePerRadian;
    }
}

public static class ConnectionRules
{
    public static bool TryResolve(ConnectionSpec link, IEnumerable<ConnectionPort> sources,
        IEnumerable<ConnectionPort> targets, out ConnectionPort source, out ConnectionPort target)
    {
        source = target = default;
        if(!Enum.IsDefined(link.Type) || link.Type==ConnectionDomain.Unknown) return false;
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
        if (fromId is not { } fromSocket || toId is not { } toSocket
            || !Enum.IsDefined(fromSocket) || !Enum.IsDefined(toSocket)) return false;
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
