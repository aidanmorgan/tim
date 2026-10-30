using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.Gpu;

/// <summary>Why a port declaration, or a question about one, is refused (Story 9A.17). Closed set; the message names the element
/// and the port.</summary>
public enum ElementPortRejection : uint
{
    /// <summary>The element is not in the loaded bundle.</summary>
    UnknownElement = 1,
    /// <summary>Two declarations name the same element.</summary>
    DuplicateElement = 2,
    /// <summary>One element declares the same socket twice.</summary>
    DuplicateSocket = 3,
    /// <summary>The socket is not a defined <see cref="PortSocket"/>.</summary>
    UndefinedSocket = 4,
    /// <summary>The engine connects only the Signal (activation) and Electrical domains.</summary>
    UnsupportedDomain = 5,
    /// <summary>A port is an input or an output; the engine has no bidirectional port.</summary>
    UnsupportedDirection = 6,
    /// <summary>The declared domain or direction is not the socket's own (an activation output is Signal and Output, and so on).</summary>
    SocketDisagrees = 7,
    /// <summary>A Signal port without a defined signal kind, or an Electrical port with one.</summary>
    SignalKindDisagrees = 8,
    /// <summary>The anchor position is not finite.</summary>
    NonFinitePosition = 9,
    /// <summary>The element does not declare the socket asked about.</summary>
    UndeclaredSocket = 10,
}

/// <summary>A refused port declaration or port question. The registry reports a refused declaration as an invalid bundle, with this
/// exception as its cause, so start-up stops as for any other invalid bundle.</summary>
public sealed class ElementPortRejectedException : Exception
{
    public ElementPortRejection Reason { get; }
    public ElementPortRejectedException(ElementPortRejection reason, string message) : base(message) => Reason = reason;
}

/// <summary>Every loaded element's declared ports, in declaration (drawer) order, read only from the element bundle's
/// <see cref="PortDescriptor"/> lists (Story 9A.17). It is immutable and has no other source: there is no per-element table and
/// no default port set.</summary>
public sealed class ElementPortCatalogue : IEquatable<ElementPortCatalogue>
{
    private readonly ImmutableSortedDictionary<PartId, ImmutableArray<WorkshopPort>> _ports;

    private ElementPortCatalogue(ImmutableSortedDictionary<PartId, ImmutableArray<WorkshopPort>> ports) => _ports = ports;

    /// <summary>The ports of every delivered element in these bundle entries (an undelivered element is never admitted).</summary>
    public static ElementPortCatalogue FromBundle(IEnumerable<BundleEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return Declare(entries.Select(entry => (Element: new PartId(entry.Metadata.Id), entry.Ports)));
    }

    /// <summary>Admits each element's declared ports, or refuses the whole set naming the first invalid port.</summary>
    public static ElementPortCatalogue Declare(IEnumerable<(PartId Element, IReadOnlyList<PortDescriptor> Ports)> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        var ports = ImmutableSortedDictionary.CreateBuilder<PartId, ImmutableArray<WorkshopPort>>();
        foreach (var (element, declared) in elements)
        {
            ArgumentNullException.ThrowIfNull(declared);
            if (ports.ContainsKey(element))
                throw new ElementPortRejectedException(ElementPortRejection.DuplicateElement, $"The element '{element}' declares its ports twice.");
            var admitted = ImmutableArray.CreateBuilder<WorkshopPort>(declared.Count);
            for (var i = 0; i < declared.Count; i++)
            {
                var port = Admit(element, i, declared[i]);
                if (admitted.Any(previous => previous.Socket == port.Socket))
                    throw Refused(ElementPortRejection.DuplicateSocket, element, i, $"declares {port.Socket} more than once");
                admitted.Add(port);
            }
            ports.Add(element, admitted.MoveToImmutable());
        }
        return new(ports.ToImmutable());
    }

    /// <summary>The engine's typed port for one declaration: its socket fixes its domain and direction, which the declaration must
    /// state the same way, and a Signal port declares the signal kind it emits or admits.</summary>
    private static WorkshopPort Admit(PartId element, int index, PortDescriptor port)
    {
        if (!Enum.IsDefined(port.Socket))
            throw Refused(ElementPortRejection.UndefinedSocket, element, index, $"names the undefined socket {(uint)port.Socket}");
        var domain = port.Domain switch
        {
            SimulationDomain.Signal => WorkshopConnectionDomain.Activation,
            SimulationDomain.Electrical => WorkshopConnectionDomain.Electrical,
            SimulationDomain.Rope => WorkshopConnectionDomain.Rope,
            _ => throw Refused(ElementPortRejection.UnsupportedDomain, element, index, $"declares the domain {port.Domain}, which the engine does not connect")
        };
        var direction = port.Direction switch
        {
            PortDirection.Input => WorkshopPortDirection.Input,
            PortDirection.Output => WorkshopPortDirection.Output,
            PortDirection.Bidirectional => WorkshopPortDirection.Bidirectional,
            _ => throw Refused(ElementPortRejection.UnsupportedDirection, element, index, $"declares the direction {port.Direction}, which the engine does not connect")
        };
        var socket = Socket(port.Socket);
        if (Own(socket) != (domain, direction))
            throw Refused(ElementPortRejection.SocketDisagrees, element, index, $"declares {port.Socket} as {port.Domain} {port.Direction}");
        WorkshopSignalKind? signal = (domain, port.Signal) switch
        {
            (WorkshopConnectionDomain.Activation, PortSignalKind.Pulse) => WorkshopSignalKind.Pulse,
            (WorkshopConnectionDomain.Activation, PortSignalKind.Level) => WorkshopSignalKind.Level,
            (WorkshopConnectionDomain.Electrical, null) => null,
            (WorkshopConnectionDomain.Rope, null) => null,
            _ => throw Refused(ElementPortRejection.SignalKindDisagrees, element, index,
                $"declares the signal kind '{port.Signal}' on a {port.Domain} port")
        };
        if (!float.IsFinite(port.LocalPosition.X) || !float.IsFinite(port.LocalPosition.Y) || !float.IsFinite(port.LocalPosition.Z))
            throw Refused(ElementPortRejection.NonFinitePosition, element, index, "declares a non-finite anchor");
        if (!Enum.IsDefined(port.AttachmentRole) ||
            (domain == WorkshopConnectionDomain.Rope) != (port.AttachmentRole != RopeAttachmentRole.None))
            throw Refused(ElementPortRejection.SocketDisagrees, element, index, "declares an invalid attachment role");
        return new(socket, domain, direction, signal, port.LocalPosition, port.AttachmentRole, port.PositionBinding);
    }

    /// <summary>The domain and direction each socket is: the activation sockets carry signals, Supply feeds and PowerIn draws.</summary>
    private static (WorkshopConnectionDomain, WorkshopPortDirection) Own(WorkshopSocket socket) => socket switch
    {
        WorkshopSocket.ActivationOut => (WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output),
        WorkshopSocket.SetIn or WorkshopSocket.ResetIn or WorkshopSocket.ActivationIn => (WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input),
        WorkshopSocket.Supply => (WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Output),
        WorkshopSocket.PowerIn or WorkshopSocket.FirstIn or WorkshopSocket.SecondIn => (WorkshopConnectionDomain.Electrical, WorkshopPortDirection.Input),
        WorkshopSocket.Tie => (WorkshopConnectionDomain.Rope, WorkshopPortDirection.Bidirectional),
        _ => throw new ArgumentOutOfRangeException(nameof(socket), socket, "Undefined socket.")
    };

    /// <summary>The engine socket a declared port socket names: the two closed sets share their values. An undefined one is refused.</summary>
    public static WorkshopSocket Socket(PortSocket socket) => Enum.IsDefined(socket)
        ? (WorkshopSocket)(uint)socket
        : throw new ArgumentOutOfRangeException(nameof(socket), socket, "Undefined socket.");

    private static ElementPortRejectedException Refused(ElementPortRejection reason, PartId element, int index, string detail) =>
        new(reason, $"'{element}'.Ports[{index}] {detail}.");

    /// <summary>Whether the bundle declares this element (with or without ports).</summary>
    public bool Declares(PartId element) => _ports.ContainsKey(element);

    /// <summary>The element's declared ports in declaration order.</summary>
    public ReadOnlySpan<WorkshopPort> Of(PartId element) =>
        _ports.TryGetValue(element, out var ports) ? ports.AsSpan()
            : throw new ElementPortRejectedException(ElementPortRejection.UnknownElement, $"The element '{element}' is not in the loaded bundle.");

    /// <summary>Whether the element declares this socket in this domain and direction; an element the bundle does not declare owns
    /// no port.</summary>
    public bool Has(PartId element, WorkshopSocket socket, WorkshopConnectionDomain domain, WorkshopPortDirection direction)
    {
        if (!_ports.TryGetValue(element, out var ports)) return false;
        foreach (var port in ports)
            if (port.Socket == socket && port.Domain == domain && port.Direction == direction) return true;
        return false;
    }

    /// <summary>The declared signal kind of one activation port: what an output emits, or the strongest an input admits.</summary>
    public WorkshopSignalKind Signal(PartId element, WorkshopSocket socket, WorkshopPortDirection direction)
    {
        foreach (var port in Of(element))
            if (port.Socket == socket && port.Domain == WorkshopConnectionDomain.Activation && port.Direction == direction && port.Signal is { } signal)
                return signal;
        throw new ElementPortRejectedException(ElementPortRejection.UndeclaredSocket, $"'{element}' declares no activation {direction} {socket}.");
    }

    /// <summary>The declared f32 anchor of one socket in the element's local frame (metres).</summary>
    public Vector3 LocalPosition(PartId element, WorkshopSocket socket)
    {
        foreach (var port in Of(element))
            if (port.Socket == socket) return port.LocalPosition;
        throw new ElementPortRejectedException(ElementPortRejection.UndeclaredSocket, $"'{element}' declares no {socket} socket.");
    }

    /// <summary>Evaluates the socket using this instance's admitted parameter values.</summary>
    public Vector3 LocalPosition(WorkshopGenericInstance instance, WorkshopSocket socket)
    {
        foreach (var port in Of(instance.PartId))
            if (port.Socket == socket)
            {
                var position = port.PositionBinding is { } binding
                    ? binding.Evaluate(instance.Parameters[binding.Parameter]) : port.LocalPosition;
                if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
                    throw new ElementPortRejectedException(ElementPortRejection.NonFinitePosition, "Socket evaluation is not finite.");
                return position;
            }
        throw new ElementPortRejectedException(ElementPortRejection.UndeclaredSocket, "The instance does not declare this socket.");
    }

    public bool Equals(ElementPortCatalogue? other) =>
        other is not null && _ports.Count == other._ports.Count &&
        _ports.All(pair => other._ports.TryGetValue(pair.Key, out var ports) && ports.SequenceEqual(pair.Value));
    public override bool Equals(object? obj) => Equals(obj as ElementPortCatalogue);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var (element, ports) in _ports)
        {
            hash.Add(element);
            foreach (var port in ports) hash.Add(port);
        }
        return hash.ToHashCode();
    }
}

/// <summary>The ports of the bundle this process loaded, installed once: by the Workshop after its registry loads the bundle, and by
/// the Simulation worker from the bundle it carries. Construction validation and the activation compiler read them here. A second
/// installation must be the same catalogue; a different one is refused and the installed one is kept.</summary>
public static class DeclaredPorts
{
    private static ElementPortCatalogue? _current;
    private static readonly object Gate = new();

    public static ElementPortCatalogue Current =>
        Volatile.Read(ref _current) ?? throw new InvalidOperationException("No element bundle's ports are installed in this process.");

    public static void Install(ElementPortCatalogue catalogue)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        lock (Gate)
        {
            if (_current is { } installed && !installed.Equals(catalogue))
                throw new InvalidOperationException("A different element bundle's ports are already installed in this process.");
            Volatile.Write(ref _current, catalogue);
        }
    }
}
