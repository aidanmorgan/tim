using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

public partial class Workshop
{
    private VBoxContainer _connectionChoices = null!;
    private (GpuBodyId Owner, WorkshopPort Port)? _linkSource;
    private Node3D? _connectionArtwork;

    private static WorkshopButton SignalButton(string label, Action action)
    {
        var button = Button(label, action); button.Text = label; button.Icon = null;
        button.AddThemeFontSizeOverride("font_size", 12); return button;
    }
    private static string SocketLabel(WorkshopSocket socket) => socket switch
    {
        WorkshopSocket.ActivationOut => "ActivationOut",
        WorkshopSocket.ActivationIn => "ActivationIn",
        WorkshopSocket.PowerIn => "PowerIn",
        WorkshopSocket.FirstIn => "FirstIn (one mark)",
        WorkshopSocket.SecondIn => "SecondIn (two marks)",
        WorkshopSocket.Supply => "Supply",
        _ => throw new ArgumentOutOfRangeException(nameof(socket), socket, "Unknown socket.")
    };
    /// <summary>The State Latch's two activation inputs are chosen by icon only, matching the on-part marks (DESIGN.md Set/Reset latch).</summary>
    private static string SocketIcon(WorkshopSocket socket) => socket switch
    {
        WorkshopSocket.SetIn => "socket_set",
        WorkshopSocket.ResetIn => "socket_reset",
        WorkshopSocket.FirstIn => "first_input",
        WorkshopSocket.SecondIn => "second_input",
        WorkshopSocket.PowerIn => "socket_power",
        _ => throw new ArgumentOutOfRangeException(nameof(socket), socket, "Socket has no icon-only choice.")
    };
    private static WorkshopButton SocketIconButton(WorkshopSocket socket, Action action)
    {
        var button = SignalButton("", action);
        button.Text = ""; button.Icon = WorkshopIcons.Pictogram(SocketIcon(socket));
        button.IconAlignment = HorizontalAlignment.Center;
        button.AddThemeConstantOverride("icon_max_width", 28);
        button.SetMeta("socket", (int)socket);
        return button;
    }
    private void ClearConnectionChoices()
    {
        _runControlToggle = null;
        if (_connectionChoices is null) return;
        foreach (var child in _connectionChoices.GetChildren()) { _connectionChoices.RemoveChild(child); child.QueueFree(); }
    }
    private void RefreshConnectionChoices()
    {
        ClearConnectionChoices();
        if (_connectionChoices is null || _selected is not { } part) return;
        AddConfigurationChoices(part);
        if (!CanEdit) return;
        foreach (var port in World.Registry.Ports.Of(part.PartId))
        {
            if (port.Direction != WorkshopPortDirection.Output) continue;
            var owner = part.AuthoredId;
            var button = SignalButton("Connect " + SocketLabel(port.Socket), () =>
            {
                if (!CanEdit) return;
                EndGizmo(false); _tool = null; ClearPreview(); _dragging = _lifting = false;
                _linkSource = (owner, port); ClearConnectionChoices();
                _status.Text = port.Domain == WorkshopConnectionDomain.Electrical ? "Click a part with a PowerIn socket. Escape cancels." : "Click a part with an ActivationIn socket. Escape cancels.";
            });
            button.Icon = null; button.Disabled = !CanEdit; _connectionChoices.AddChild(button);
        }
        foreach (var link in World.Construction.Connections)
        {
            if (link.Source != part.AuthoredId && link.Target != part.AuthoredId) continue;
            var button = SignalButton(link.Domain == WorkshopConnectionDomain.Electrical ? "Disconnect supply" : "Disconnect signal", () => ChangeConnection(link, false));
            button.Icon = null; button.Disabled = !CanEdit; _connectionChoices.AddChild(button);
        }
    }
    private void ChooseConnectionTarget(MachinePart? target)
    {
        if (!CanEdit || _linkSource is not { } source) return;
        ClearConnectionChoices();
        if (target is null || target.AuthoredId == source.Owner)
        { _status.Text = "Choose another part with a matching input socket."; return; }
        var choices = 0;
        foreach (var port in World.Registry.Ports.Of(target.PartId))
        {
            if (port.Direction != WorkshopPortDirection.Input || port.Domain != source.Port.Domain) continue;
            var link = new WorkshopConnection(source.Owner, source.Port.Socket, target.AuthoredId, port.Socket, port.Domain);
            var numbered = World.Registry.Ports.Has(target.PartId,WorkshopSocket.FirstIn,WorkshopConnectionDomain.Electrical,WorkshopPortDirection.Input);
            var button = port.Socket is WorkshopSocket.SetIn or WorkshopSocket.ResetIn or WorkshopSocket.FirstIn or WorkshopSocket.SecondIn ||
                (numbered && port.Socket==WorkshopSocket.PowerIn)
                ? SocketIconButton(port.Socket, () => ChangeConnection(link, true))
                // The UI font has no arrow glyph; "to" reads the same and always renders.
                : SignalButton(SocketLabel(source.Port.Socket) + " to " + SocketLabel(port.Socket), () => ChangeConnection(link, true));
            _connectionChoices.AddChild(button); choices++;
        }
        _status.Text = choices == 0 ? "These parts have no matching sockets. Choose another part."
            : "Connect the selected sockets.";
    }
    private async void ChangeConnection(WorkshopConnection link, bool add)
    {
        try { await ChangeConnectionAsync(link, add); }
        catch (Exception error) { HandlerFailed(nameof(ChangeConnection), error); }
    }
    internal async Task ChangeConnectionAsync(WorkshopConnection link, bool add)
    {
        if (!CanEdit) return;
        try
        {
            var next = World.Construction with { Connections = add ? World.Construction.Connections.With(link) : World.Construction.Connections.Without(link) };
            next.Validate(); PushUndo();
            if (!await SubmitConstruction(next)) { if (_undo.Count > 0) _undo.RemoveAt(_undo.Count - 1); return; }
            if (_workshopUiRemoved) return;
            _linkSource = null; RefreshConnectionChoices(); RefreshConnectionArtwork();
            _status.Text = link.Domain == WorkshopConnectionDomain.Electrical
                ? (add ? "Supply connected." : "Supply disconnected.")
                : (add ? "Activation connected." : "Activation disconnected.");
        }
        catch (Exception error) { var failure = WorkshopUiFailure.Report(error); if (!_workshopUiRemoved) _status.Text = failure; }
    }
    private void RefreshConnectionArtwork()
    {
        if (_connectionArtwork is { } previous) { RemoveChild(previous); previous.Free(); }
        _connectionArtwork = new Node3D { Name = "WorkshopConnections" }; AddChild(_connectionArtwork);
        foreach (var link in World.Construction.Connections)
        {
            var source = World.Parts.Single(part => part.AuthoredId == link.Source);
            var target = World.Parts.Single(part => part.AuthoredId == link.Target);
            // The declared anchors from the element bundle (Story 9A.17) are the wire artwork's endpoints.
            var from = World.Registry.Ports.LocalPosition(source.Definition.PartId, link.Output);
            var to = World.Registry.Ports.LocalPosition(target.Definition.PartId, link.Input);
            var start = source.ToGlobal(new(from.X, from.Y, from.Z));
            var end = target.ToGlobal(new(to.X, to.Y, to.Z));
            WorkshopArtwork.Line(_connectionArtwork, start, end,
                link.Domain == WorkshopConnectionDomain.Electrical ? new("#293954") : new("#ffd899"),
                link.Domain == WorkshopConnectionDomain.Electrical ? .035f : .025f);
        }
    }
}
