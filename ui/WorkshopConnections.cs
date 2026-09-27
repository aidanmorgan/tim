using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public partial class Workshop
{
    private void ClearLinkChoices()
    {
        foreach (var child in _linkChoices.GetChildren())
        {
            _linkChoices.RemoveChild(child);
            child.QueueFree();
        }
        _linkChoices.Visible = false;
    }

    private void ShowLinkChoices(MachinePart target, List<ConnectionSpec> options)
    {
        ClearLinkChoices();
        foreach (var option in options)
        {
            var action = option.Type switch
            {
                ConnectionDomain.Activation => option.ToPort switch
                {
                    SocketIds.SetIn => "Connect set",
                    SocketIds.ResetIn => "Connect reset",
                    SocketIds.ActivationIn => "Connect activation",
                    _ => throw new InvalidOperationException("Unsupported activation input.")
                },
                ConnectionDomain.Electrical => option.ToPort switch
                {
                    SocketIds.FirstIn => "Connect first input",
                    SocketIds.SecondIn => "Connect second input",
                    SocketIds.PowerIn => "Connect electricity",
                    _ => throw new InvalidOperationException("Unsupported electrical input.")
                },
                ConnectionDomain.Mechanical => "Connect drive",
                ConnectionDomain.Rope => "Connect rope",
                _ => throw new InvalidOperationException("Unsupported selectable connection domain.")
            };
            var button = Button(action, () => CompleteLink(target, option));
            _linkChoices.AddChild(button);
        }
        _status.Text = "Choose the input socket.";
    }

    private void CompleteLink(MachinePart target, ConnectionSpec option)
    {
        if (_inRun || _linkSource == null) return;
        PushUndo();
        var connected = World.Connect(_linkSource, option.FromPort!, target, option.ToPort!, option.Type);
        _status.Text = connected ? _linkSource.Definition.Title + " → " + target.Definition.Title
            : "Cannot connect these sockets. Choose another part or Cancel.";
        if (connected) { _linkSource = null; ClearLinkChoices(); }
        RefreshLayerAppearance();
        RefreshCables();
    }
}
