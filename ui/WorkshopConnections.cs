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
            var choice = ConnectionChoice.Describe(option);
            var button = Button(choice.Label, () => CompleteLink(target, option), icon: WorkshopIcons.ConnectionPictogram(choice));
            if (choice.OutputIcon != null)
            {
                button.CustomMinimumSize = new(64,40);
                button.AddThemeConstantOverride("icon_max_width",42);
            }
            _linkChoices.AddChild(button);
        }
        _status.Text = "Choose the connection sockets.";
    }

    private void CompleteLink(MachinePart target, ConnectionSpec option)
    {
        if (_inRun || _linkSource == null) return;
        PushUndo();
        var connected = World.Connect(_linkSource, option.FromPort!.Value, target, option.ToPort!.Value, option.Type);
        _status.Text = connected ? _linkSource.Definition.Title + " → " + target.Definition.Title
            : "Cannot connect these sockets. Choose another part or Cancel.";
        if (connected) { _linkSource = null; ClearLinkChoices(); }
        RefreshLayerAppearance();
        RefreshCables();
    }
}
