using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

public partial class Workshop
{
    private VBoxContainer _connectionChoices = null!;
    private (GpuBodyId Owner, WorkshopPort Port)? _linkSource;
    private Node3D? _connectionArtwork;
    private readonly List<MeshInstance3D> _connectionSegments = new();
    private readonly List<MeshInstance3D> _connectionKnots = new();
    private readonly HashSet<(SimulationEpoch Epoch, RopeSpanId Span)> _queuedCuts = new();

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
        WorkshopSocket.Tie => "Tie",
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
        if (!CanEdit)
        {
            if (World.WorkshopPhase is WorkshopSimulationPhase.Running or WorkshopSimulationPhase.Paused)
                foreach (var rope in World.WorkshopRead.Presentation.Ropes)
                {
                    if (rope.BodyA != part.AuthoredId && rope.BodyB != part.AuthoredId) continue;
                    var span = rope.Id;
                    var queued = _queuedCuts.Contains((World.WorkshopRead.Epoch, span));
                    var button = SignalButton(queued ? "Cut queued" : rope.Integrity == RopeIntegrity.Cut ? "Rope cut" : "Cut rope",
                        () => ChangeConnection(World.Construction.Connections.Single(link => link.Domain == WorkshopConnectionDomain.Rope &&
                            ((link.Source == rope.BodyA && link.Target == rope.BodyB) || (link.Source == rope.BodyB && link.Target == rope.BodyA))), false));
                    button.Disabled = queued || rope.Integrity == RopeIntegrity.Cut;
                    _connectionChoices.AddChild(button);
                }
            return;
        }
        foreach (var port in World.Registry.Ports.Of(part.PartId))
        {
            if (port.Direction is not (WorkshopPortDirection.Output or WorkshopPortDirection.Bidirectional)) continue;
            var owner = part.AuthoredId;
            var button = SignalButton("Connect " + SocketLabel(port.Socket), () =>
            {
                if (!CanEdit) return;
                EndGizmo(false); _tool = null; ClearPreview(); _dragging = _lifting = false;
                _linkSource = (owner, port); ClearConnectionChoices();
                _status.Text = port.Domain == WorkshopConnectionDomain.Rope ? "Click another Tie socket. Rope length is measured when connected. Escape cancels." : port.Domain == WorkshopConnectionDomain.Electrical ? "Click a part with a PowerIn socket. Escape cancels." : "Click a part with an ActivationIn socket. Escape cancels.";
            });
            button.Icon = null; button.Disabled = !CanEdit; _connectionChoices.AddChild(button);
        }
        foreach (var link in World.Construction.Connections)
        {
            if (link.Source != part.AuthoredId && link.Target != part.AuthoredId) continue;
            var button = SignalButton(link.Domain == WorkshopConnectionDomain.Rope ? "Disconnect rope" : link.Domain == WorkshopConnectionDomain.Electrical ? "Disconnect supply" : "Disconnect signal", () => ChangeConnection(link, false));
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
            if (port.Direction is not (WorkshopPortDirection.Input or WorkshopPortDirection.Bidirectional) || port.Domain != source.Port.Domain) continue;
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
        if (!add && link.Domain == WorkshopConnectionDomain.Rope &&
            World.WorkshopPhase is WorkshopSimulationPhase.Running or WorkshopSimulationPhase.Paused)
        {
            var span = World.WorkshopRead.Presentation.Ropes.ToArray().Single(rope =>
                (rope.BodyA == link.Source && rope.BodyB == link.Target) || (rope.BodyA == link.Target && rope.BodyB == link.Source)).Id;
            var epoch = World.WorkshopRead.Epoch;
            var completion = await World.CutRope(span);
            if (_workshopUiRemoved || !completion.Applicable) return;
            if (completion.Result.Outcome == WorkshopCommandOutcome.Applied)
            {
                _queuedCuts.Add((epoch, span));
                _status.Text = "Cut queued. It takes effect on the next physical step.";
            }
            else _status.Text = "Rope cut refused: " + completion.Result.Reason;
            RefreshConnectionChoices();
            return;
        }
        if (!CanEdit) return;
        try
        {
            if (add && link.Domain == WorkshopConnectionDomain.Rope)
            {
                link = CuriousContraptions.Gpu.WorkshopConnections.MeasureRope(World.Construction.Instances, link.Source, link.Target);
            }
            var next = World.Construction with { Connections = add ? World.Construction.Connections.With(link) : World.Construction.Connections.Without(link) };
            next.Validate(); PushUndo();
            if (!await SubmitConstruction(next)) { if (_undo.Count > 0) _undo.RemoveAt(_undo.Count - 1); return; }
            if (_workshopUiRemoved) return;
            _linkSource = null; RefreshConnectionChoices(); RefreshConnectionArtwork(World.WorkshopRead.Presentation.Ropes);
            _status.Text = link.Domain == WorkshopConnectionDomain.Rope ? (add ? "Rope connected." : "Rope disconnected.") : link.Domain == WorkshopConnectionDomain.Electrical
                ? (add ? "Supply connected." : "Supply disconnected.")
                : (add ? "Activation connected." : "Activation disconnected.");
        }
        catch (Exception error) { var failure = WorkshopUiFailure.Report(error, Diagnostics); if (!_workshopUiRemoved) _status.Text = failure; }
    }
    private Vector3 SocketWorld(MachinePart part, WorkshopSocket socket)
    {
        var instance = (WorkshopGenericInstance)World.Construction.Instances.Single(p => p.Id == part.AuthoredId);
        var position = World.Registry.Ports.LocalPosition(instance, socket);
        return part.ToGlobal(new(position.X, position.Y, position.Z));
    }

    internal void RefreshConnectionArtwork(ReadOnlySpan<RopeSpanRead> ropes)
    {
        if (_queuedCuts.Count != 0)
        {
            var committed = World.WorkshopRead;
            var cutSpans = committed.Presentation.Ropes.ToArray().Where(row => row.Integrity == RopeIntegrity.Cut)
                .Select(row => row.Id).ToHashSet();
            if (_queuedCuts.RemoveWhere(pending => pending.Epoch != committed.Epoch || cutSpans.Contains(pending.Span)) != 0)
                RefreshConnectionChoices();
        }
        if (_connectionArtwork is null)
        {
            _connectionArtwork = new Node3D { Name = "WorkshopConnections" };
            AddChild(_connectionArtwork);
        }
        foreach (var node in _connectionSegments) node.Visible = false;
        foreach (var node in _connectionKnots) node.Visible = false;
        var segmentCount = 0; var knotCount = 0;
        foreach (var link in World.Construction.Connections)
        {
            if (link.Domain != WorkshopConnectionDomain.Rope)
            {
                var source = World.Parts.Single(part => part.AuthoredId == link.Source);
                var target = World.Parts.Single(part => part.AuthoredId == link.Target);
                Segment(SocketWorld(source, link.Output), SocketWorld(target, link.Input), link.Domain == WorkshopConnectionDomain.Electrical ? new("#293954") : new("#ffd899"),
                    link.Domain == WorkshopConnectionDomain.Electrical ? .035f : .025f, true);
                continue;
            }
            var sourcePart = World.Parts.Single(part => part.AuthoredId == link.Source);
            var targetPart = World.Parts.Single(part => part.AuthoredId == link.Target);
            var isSourceGuide = IsGuide(sourcePart.Definition.PartId, link.Output);
            var isTargetGuide = IsGuide(targetPart.Definition.PartId, link.Input);
            RopeSpanRead? observed = null;
            foreach (var rope in ropes)
                if ((rope.BodyA == link.Source && rope.BodyB == link.Target) ||
                    (rope.BodyA == link.Target && rope.BodyB == link.Source)) { observed = rope; break; }
            if (observed is not { } committed)
            {
                _status.Text = WorkshopUiFailure.Report(new WireFaultException(WireFaultReason.MissingField,
                    "Missing committed rope observation; rope artwork cannot be displayed."), Diagnostics);
                continue;
            }
            if (committed.Integrity == RopeIntegrity.Cut && committed.CutSpan == committed.Id) continue;
            var start = new Vector3(committed.AttachmentA.X, committed.AttachmentA.Y, committed.AttachmentA.Z);
            var end = new Vector3(committed.AttachmentB.X, committed.AttachmentB.Y, committed.AttachmentB.Z);
            var state = committed.State;
            var points = RopeGeometry.Evaluate(committed.AttachmentA, committed.AttachmentB, committed.MaximumLength, state);
            for (var i = 1; i < points.Length; i++)
                Segment(new(points[i - 1].X, points[i - 1].Y, points[i - 1].Z),
                    new(points[i].X, points[i].Y, points[i].Z), new("#d69c47"), .025f, state != RopeState.Open || i % 2 == 1);
            var startIsGuide = committed.BodyA == link.Source ? isSourceGuide : isTargetGuide;
            var endIsGuide = committed.BodyB == link.Target ? isTargetGuide : isSourceGuide;
            if (!startIsGuide) Knot(start);
            if (!endIsGuide) Knot(end);
        }
        for (var i = segmentCount; i < _connectionSegments.Count; i++) _connectionSegments[i].Visible = false;
        for (var i = knotCount; i < _connectionKnots.Count; i++) _connectionKnots[i].Visible = false;

        bool IsGuide(PartId partId, WorkshopSocket socket)
        {
            foreach (var p in DeclaredPorts.Current.Of(partId))
                if (p.Socket == socket && p.AttachmentRole == RopeAttachmentRole.Guide) return true;
            return false;
        }

        void Segment(Vector3 start, Vector3 end, Color colour, float width, bool visible)
        {
            while (_connectionSegments.Count <= segmentCount)
                _connectionSegments.Add(WorkshopArtwork.Cylinder(_connectionArtwork, width, 1f, colour));
            var node = _connectionSegments[segmentCount++];
            var offset = end - start; var length = offset.Length();
            node.Visible = visible && length > 0f;
            node.Position = (start + end) * .5f;
            node.Quaternion = length > 0f ? new Quaternion(Vector3.Up, offset / length) : Quaternion.Identity;
            var mesh = (CylinderMesh)node.Mesh;
            mesh.TopRadius = width; mesh.BottomRadius = width; mesh.Height = Mathf.Max(length, .001f);
            ((StandardMaterial3D)node.MaterialOverride).AlbedoColor = colour;
        }
        void Knot(Vector3 position)
        {
            while (_connectionKnots.Count <= knotCount)
                _connectionKnots.Add(WorkshopArtwork.Sphere(_connectionArtwork, .055f, new("#f7cb52")));
            var node = _connectionKnots[knotCount++]; node.Position = position; node.Visible = true;
        }
    }
}
