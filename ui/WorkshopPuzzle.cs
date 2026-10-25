using Godot;
using System;
using System.Threading.Tasks;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

public partial class Workshop
{
    private Label _success = null!;
    private PuzzleId[] _puzzleChoices = [];
    private int CurrentPuzzleIndex => Array.IndexOf(_puzzleChoices, World.Construction.Puzzle.Id);

    private async void SelectModeFromPicker(int index)
    {
        try { await SelectModeFromPickerAsync(index); }
        catch (Exception error) { HandlerFailed(nameof(SelectModeFromPicker), error); }
    }

    private async Task SelectModeFromPickerAsync(int index)
    {
        if (!CanEdit) { _picker.Select(CurrentPuzzleIndex); return; }
        try
        {
            if ((uint)index >= (uint)_puzzleChoices.Length) throw new ArgumentOutOfRangeException(nameof(index));
            var id = _puzzleChoices[index];
            var count = id.IsAuthored ? (ulong)PuzzleCatalogue.Definition(id).Placements.Length : 0;
            if (_nextId.Value > ulong.MaxValue - count)
                throw new WorkshopEditRejectedException(WorkshopEditRejection.IdentityExhausted, "Puzzle identities are exhausted.");
            var nextId = new GpuBodyId(_nextId.Value + count);
            var candidate = id.IsAuthored
                ? PuzzleCatalogue.Create(id, World.Construction.Revision, World.Construction.Settings,
                    _nextId, new((float)(_precision.Value / 100)))
                : new WorkshopConstruction(World.Construction.Revision, World.Construction.Settings, WorkshopInstances.Empty);
            if (!await SubmitConstruction(candidate)) { PresentMode(); return; }
            _nextId = nextId;
            Select(null); _tool = null; ClearPreview(); _undo.Clear();
            ResetUiAnimations(); PresentMode();
        }
        catch (Exception error) { var failure = WorkshopUiFailure.Report(error); if (!_workshopUiRemoved) { PresentMode(); _status.Text = failure; } }
    }

    private void PresentMode()
    {
        var id = World.Construction.Puzzle.Id;
        var authored = id.IsAuthored;
        var presentation = authored ? PuzzleCatalogue.Presentation(id) : null;
        _picker.Select(CurrentPuzzleIndex);
        _inventory = authored ? WorkshopInventoryPolicy.Authored(World.Construction.Puzzle) : World.Registry.Free;
        _title.Text = presentation?.Title ?? "Free workshop";
        _task.Text = presentation?.Task ?? "Place parts and connect activation sockets. Run tries the machine; Reset restores its starting arrangement.";
        _hint.Visible = false; _task.Visible = _hintButton.Visible = authored;
        _optionsPanel.Visible = false; _objectivePanel.Visible = authored;
        if (authored) _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100);
        _precisionText.Text = _precision.Value < 33 ? "Forgiving" : _precision.Value > 66 ? "Precise" : "Balanced";
        SetBuildUi(); RefreshPalette(); RefreshLayers();
        _placementHeight = new(3f); SetBuildView(authored);
        if (presentation is not null) _status.Text = presentation.Status;
    }

    private async void ChangePrecision(double value)
    {
        try { await ChangePrecisionAsync(value); }
        catch (Exception error) { HandlerFailed(nameof(ChangePrecision), error); }
    }

    private async Task ChangePrecisionAsync(double value)
    {
        if (!World.Construction.Puzzle.Id.IsAuthored)
        {
            _precisionText.Text = value < 33 ? "Forgiving" : value > 66 ? "Precise" : "Balanced";
            _status.Text = "Free Workshop keeps the Receiver’s capture settings unchanged."; return;
        }
        if (!CanEdit) { _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100); return; }
        try
        {
            var precision = new PuzzlePrecision((float)(value / 100));
            var candidate = PuzzleCatalogue.WithPrecision(World.Construction, precision);
            if (!await SubmitConstruction(candidate)) { _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100); return; }
            _precisionText.Text = value < 33 ? "Forgiving" : value > 66 ? "Precise" : "Balanced";
            _status.Text = PuzzleCatalogue.Presentation(World.Construction.Puzzle.Id).PrecisionStatus;
        }
        catch (Exception error) { var failure = WorkshopUiFailure.Report(error); _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100); _status.Text = failure; }
    }
}
