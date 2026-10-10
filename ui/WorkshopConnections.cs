using Godot;
using System;
using System.Linq;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

public partial class Workshop
{
    private VBoxContainer _connectionChoices = null!;
    private (GpuBodyId Owner, WorkshopPort Port)? _linkSource;
    private Node3D? _connectionArtwork;

    private static Button SignalButton(string label, Action action)
    {
        var button = Button(label, action); button.Text = label; button.Icon = null;
        button.AddThemeFontSizeOverride("font_size", 12); return button;
    }
    private static string SocketLabel(WorkshopSocket socket) => socket switch
    {
        WorkshopSocket.ActivationOut => "ActivationOut",
        WorkshopSocket.ActivationIn => "ActivationIn",
        WorkshopSocket.PowerIn => "PowerIn",
        WorkshopSocket.Supply => "Supply",
        _ => throw new ArgumentException("Unknown socket.")
    };
    private void ClearConnectionChoices()
    {
        _batteryCharge = null; _batteryToggle = null;
        if (_connectionChoices is null) return;
        foreach (var child in _connectionChoices.GetChildren()) { _connectionChoices.RemoveChild(child); child.QueueFree(); }
    }
    private void RefreshConnectionChoices()
    {
        ClearConnectionChoices();
        if (_connectionChoices is null || _selected is not { } part) return;
        AddConfigurationChoices(part);
        if (!CanEdit) return;
        foreach (var port in WorkshopPorts.For(part.Definition.WorkshopKind))
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
        foreach (var port in WorkshopPorts.For(target.Definition.WorkshopKind))
        {
            if (port.Direction != WorkshopPortDirection.Input || port.Domain != source.Port.Domain) continue;
            var link = new WorkshopConnection(source.Owner, source.Port.Socket, target.AuthoredId, port.Socket, port.Domain);
            var button = SignalButton(SocketLabel(source.Port.Socket) + " → " + SocketLabel(port.Socket), () => ChangeConnection(link, true));
            button.Icon = null; _connectionChoices.AddChild(button); choices++;
        }
        _status.Text = choices == 0 ? "These parts have no matching sockets. Choose another part."
            : "Connect the selected sockets.";
    }
    private async void ChangeConnection(WorkshopConnection link, bool add)
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
        catch (Exception error) { if (!_workshopUiRemoved) _status.Text = error.Message; }
    }
    private void RefreshConnectionArtwork()
    {
        if (_connectionArtwork is { } previous) { RemoveChild(previous); previous.Free(); }
        _connectionArtwork = new Node3D { Name = "WorkshopConnections" }; AddChild(_connectionArtwork);
        foreach (var link in World.Construction.Connections)
        {
            var source = World.Parts.Single(part => part.AuthoredId == link.Source);
            var target = World.Parts.Single(part => part.AuthoredId == link.Target);
            var from = WorkshopPorts.LocalPosition(source.Definition.WorkshopKind, link.Output);
            var to = WorkshopPorts.LocalPosition(target.Definition.WorkshopKind, link.Input);
            var start = source.ToGlobal(new((float)from.X, (float)from.Y, (float)from.Z));
            var end = target.ToGlobal(new((float)to.X, (float)to.Y, (float)to.Z));
            PartArt.Line(_connectionArtwork, start, end,
                link.Domain == WorkshopConnectionDomain.Electrical ? new("#293954") : new("#ffd899"),
                link.Domain == WorkshopConnectionDomain.Electrical ? .035f : .025f);
        }
    }
}
