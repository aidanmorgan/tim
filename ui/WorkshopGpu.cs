using Godot;
using CuriousContraptions.Gpu;
using System;
using System.Threading.Tasks;

namespace CuriousContraptions;

public partial class Workshop
{
    private static Button PlaybackButton(string label, Action action)
    {
        var button = Button(label, action);
        button.Text = label;
        button.Icon = null;
        return button;
    }
    private void AddPlaybackControls(VBoxContainer parent)
    {
        parent.AddChild(Text("Playback", 14));
        var playback = new HBoxContainer();
        playback.AddChild(PlaybackButton("Pause", () => Playback(WorkshopCommandKind.Pause)));
        playback.AddChild(PlaybackButton("Resume", () => Playback(WorkshopCommandKind.Resume)));
        playback.AddChild(PlaybackButton("Step", () => Playback(WorkshopCommandKind.Step)));
        parent.AddChild(playback);
    }
    private async void Playback(WorkshopCommandKind kind)
    {
        if (_gpuPending) return;
        _gpuPending = true;
        try
        {
            var result = await World.PlaybackWorkshop(kind);
            if (!_workshopUiRemoved) _status.Text = result.Result.Outcome == WorkshopCommandOutcome.Applied
                ? "Playback updated." : $"Playback rejected: {result.Result.Reason}";
        }
        catch (Exception error) { if (!_workshopUiRemoved) _status.Text = error.Message; }
        finally { if (!_workshopUiRemoved) _gpuPending = false; }
    }

    private async Task<bool> SubmitConstruction(WorkshopConstruction construction)
    {
        if (!CanEdit) return false;
        _gpuPending = true;
        _state.Text = "CHECKING PLACEMENT";
        RefreshPalette();
        try
        {
            var result = await World.ReplaceConstruction(construction.Ball, construction.Receiver);
            if (_workshopUiRemoved) return false;
            if (!result.Applicable) return false;
            if (result.Result.Outcome != WorkshopCommandOutcome.Applied)
            {
                _status.Text = $"Placement rejected: {result.Result.Reason}";
                World.RestoreConstructionPresentation();
                return false;
            }
            _state.Text = "BUILD MODE";
            return true;
        }
        catch (Exception error)
        {
            if (_workshopUiRemoved) return false;
            World.RestoreConstructionPresentation();
            _status.Text = error.Message;
            return false;
        }
        finally { if (!_workshopUiRemoved) { _gpuPending = false; RefreshPalette(); RefreshLayers(); } }
    }

    private async void CommitSelected()
    {
        if (!CanEdit || _selected is null) return;
        try
        {
            var proposed = _selected.Definition.WorkshopKind switch
            {
                WorkshopPartKind.Basketball when World.Construction.Ball is { } ball =>
                    World.Construction with { Ball = World.CaptureBasketball(ball.Id, _selected.Position, _selected.Quaternion) },
                WorkshopPartKind.Receiver when World.Construction.Receiver is { } receiver =>
                    World.Construction with { Receiver = World.CaptureReceiver(receiver.Id, _selected.Position, _selected.Quaternion) },
                _ => throw new ArgumentException("Selected part is not in the construction.")
            };
            // Live pointer motion is an input preview, never retained canonical construction.
            World.RestoreConstructionPresentation();
            var accepted = await SubmitConstruction(proposed);
            if (!_workshopUiRemoved && !accepted && _undo.Count > 0) _undo.RemoveAt(_undo.Count - 1);
        }
        catch (Exception error) { if (_workshopUiRemoved) return; World.RestoreConstructionPresentation(); _status.Text = error.Message; }
    }
}
