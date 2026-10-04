using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

public partial class Workshop
{
    private Label _success = null!;
    private SimulationEpoch _solvedPresentationEpoch;

    private void ClearGoalFeedback()
    {
        _solvedPresentationEpoch = default;
        if (_success is not null) _success.Visible = false;
    }

    private async void SelectModeFromPicker(int index)
    {
        if (!CanEdit) { _picker.Select(World.Construction.Puzzle.Id == WorkshopPuzzleId.Free ? FreeWorkshopIndex : 0); return; }
        try
        {
            WorkshopConstruction candidate;
            var first = _nextId;
            if (index == FreeWorkshopIndex)
                candidate = new(World.Construction.Revision, World.Construction.Settings, WorkshopInstances.Empty);
            else if (index == 0)
            {
                if (first.Value >= ulong.MaxValue - 1) throw new ArgumentException("Part identity is exhausted.");
                candidate = FirstPrinciples.Create(World.Construction.Revision, World.Construction.Settings,
                    first, new(first.Value + 1), new((Half)(_precision.Value / 100)));
            }
            else throw new ArgumentException("Unsupported Workshop puzzle.");
            if (!await SubmitConstruction(candidate)) { PresentMode(); return; }
            if (index == 0) _nextId = new(first.Value + 2);
            Select(null); _tool = null; ClearPreview(); _undo.Clear();
            ResetUiAnimations(); PresentMode();
        }
        catch (Exception error) { if (!_workshopUiRemoved) { PresentMode(); _status.Text = error.Message; } }
    }
    private void PresentMode()
    {
        var first = World.Construction.Puzzle.Id == WorkshopPuzzleId.FirstPrinciples;
        _picker.Select(first ? 0 : FreeWorkshopIndex);
        _inventory = first ? new Dictionary<WorkshopPartKind, int> { [WorkshopPartKind.Ramp] = checked((int)World.Construction.Puzzle.RampInventory) }
            : new Dictionary<WorkshopPartKind, int> { [WorkshopPartKind.Basketball] = 1, [WorkshopPartKind.Receiver] = 1, [WorkshopPartKind.ImpactSwitch] = 1, [WorkshopPartKind.SignalLamp] = 1, [WorkshopPartKind.Wall] = 1 };
        _title.Text = first ? "First principles" : "Free workshop";
        _task.Text = first ? "Guide the orange ball into the green receiver. Place the two ramps to build a path through the air."
            : "Place parts and connect activation sockets. Run tries the machine; Reset restores its starting arrangement.";
        _hint.Visible = false; _task.Visible = _hintButton.Visible = first;
        _optionsPanel.Visible = false; _objectivePanel.Visible = first;
        if (first) _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100);
        _precisionText.Text = _precision.Value < 33 ? "Forgiving" : _precision.Value > 66 ? "Precise" : "Balanced";
        SetBuildUi(); RefreshPalette(); RefreshLayers();
        _placementHeight = new((Half)3); SetBuildView(first);
        if (first) _status.Text = "Place and rotate both ramps. Manual placement; physical nudging is not yet supported.";
    }
    private async void ChangePrecision(double value)
    {
        if (World.Construction.Puzzle.Id == WorkshopPuzzleId.Free)
        {
            _precisionText.Text = value < 33 ? "Forgiving" : value > 66 ? "Precise" : "Balanced";
            _status.Text = "Free Workshop keeps the Receiver’s capture settings unchanged."; return;
        }
        if (!CanEdit) { _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100); return; }
        try
        {
            var precision = new PuzzlePrecision((Half)(value / 100));
            var candidate = FirstPrinciples.WithPrecision(World.Construction, precision);
            if (!await SubmitConstruction(candidate)) { _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100); return; }
            _precisionText.Text = value < 33 ? "Forgiving" : value > 66 ? "Precise" : "Balanced";
            _status.Text = "Receiver assistance updated. Ramp placement remains manual.";
        }
        catch (Exception error) { _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100); _status.Text = error.Message; }
    }
    private void PresentGoalFeedback()
    {
        if (!_inRun || World.GoalPhase != WorkshopGoalPhase.Solved ||
            !World.TryGoalOpacity(Engine.GetProcessFrames(), out var opacity)) return;
        // Goal truth is the committed named event; this shared animation sample only schedules visible feedback.
        if (_solvedPresentationEpoch != World.WorkshopRead.Epoch)
        {
            _solvedPresentationEpoch = World.WorkshopRead.Epoch;
            _success.Text = "SOLVED!"; _success.Visible = true;
            GD.Print("CCGOAL_SOLVED " + World.WorkshopRead.Epoch.Value);
        }
        var color = _success.Modulate; color.A = (float)opacity; _success.Modulate = color;
    }
}
