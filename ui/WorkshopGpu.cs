using Godot;
using CuriousContraptions.Gpu;
using System;
using System.Threading.Tasks;

namespace CuriousContraptions;

public partial class Workshop
{
    private static WorkshopButton PlaybackButton(string label, Action action)
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
        try { await PlaybackAsync(kind); }
        catch (Exception error) { HandlerFailed(nameof(Playback), error); }
    }
    private async Task PlaybackAsync(WorkshopCommandKind kind)
    {
        if (_gpuPending) return;
        _gpuPending = true;
        try
        {
            var result = await World.PlaybackWorkshop(kind);
            if (!_workshopUiRemoved) _status.Text = result.Result.Outcome == WorkshopCommandOutcome.Applied
                ? "Playback updated." : $"Playback rejected: {result.Result.Reason}";
        }
        catch (Exception error) { var failure = WorkshopUiFailure.Report(error, Diagnostics); if (!_workshopUiRemoved) _status.Text = failure; }
        finally { if (!_workshopUiRemoved) _gpuPending = false; }
    }

    /// <summary>The boundary of every async void Workshop handler. In the browser an exception escaping one is posted to the thread
    /// pool, where it terminates the .NET runtime (Story 9R.0c F1), so it is logged with its handler and shown, never rethrown.</summary>
    private void HandlerFailed(string handler, Exception error)
    {
        GD.PushError($"Workshop handler '{handler}' failed: {error}");
        if (_workshopUiRemoved) return;
        try { _status.Text = $"That action failed and was logged: {error.Message}"; }
        catch (Exception statusError) { GD.PushError($"Workshop handler '{handler}' could not show its failure: {statusError}"); }
    }

    private async Task<bool> SubmitConstruction(WorkshopConstruction construction)
    {
        if (!CanEdit) return false;
        _gpuPending = true;
        _state.Text = "CHECKING PLACEMENT";
        RefreshPalette();
        try
        {
            var result = await World.ReplaceConstruction(construction);
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
        // Every construction edit ends here, including from async void handlers with no catch of their own (place,
        // delete, undo): a declared refusal is a rejection, anything else a logged fault with a fault status
        // (WorkshopUiFailure.Report). Either way the presentation is restored and the edit is not accepted.
        catch (Exception error)
        {
            var failure = WorkshopUiFailure.Report(error, Diagnostics);
            if (_workshopUiRemoved) return false;
            World.RestoreConstructionPresentation();
            _status.Text = failure;
            return false;
        }
        finally { if (!_workshopUiRemoved) { _gpuPending = false; RefreshPalette(); RefreshLayers(); RefreshConnectionChoices(); RefreshConnectionArtwork(World.WorkshopRead.Presentation.Ropes); } }
    }

    private async void CommitSelected()
    {
        try { await CommitSelectedAsync(); }
        catch (Exception error) { HandlerFailed(nameof(CommitSelected), error); }
    }
    private async Task CommitSelectedAsync()
    {
        if (!CanEdit || _selected is null) return;
        try
        {
            if (_selected.Locked) throw new WorkshopEditRejectedException(WorkshopEditRejection.FixedPart, "Fixed puzzle instances cannot be edited.");
            var proposed = World.Construction.WithInstance(World.CaptureInstance(_selected.PartId,
                _selected.AuthoredId, _selected.Position, _selected.Quaternion,
                _selected.Dimensions));
            // Live pointer motion is an input preview, never retained canonical construction.
            World.RestoreConstructionPresentation();
            var accepted = await SubmitConstruction(proposed);
            if (!_workshopUiRemoved && !accepted && _undo.Count > 0) _undo.RemoveAt(_undo.Count - 1);
        }
        catch (Exception error) { var failure = WorkshopUiFailure.Report(error, Diagnostics); if (_workshopUiRemoved) return; World.RestoreConstructionPresentation(); _status.Text = failure; }
    }
}
