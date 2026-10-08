using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

public partial class Workshop
{
    private Label _success = null!;

    private async void SelectModeFromPicker(int index)
    {
        if (!CanEdit)
        {
            _picker.Select(World.Construction.Puzzle.Id switch
            {
                WorkshopPuzzleId.FirstPrinciples => 0,
                WorkshopPuzzleId.DelayedSignal => DelayedSignalIndex,
                _ => FreeWorkshopIndex
            });
            return;
        }
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
            else if (index == DelayedSignalIndex)
            {
                if (first.Value >= ulong.MaxValue - 2) throw new ArgumentException("Part identity is exhausted.");
                candidate = DelayedSignal.Create(World.Construction.Revision, World.Construction.Settings,
                    first, new(first.Value + 1), new(first.Value + 2), new((Half)(_precision.Value / 100)));
            }
            else throw new ArgumentException("Unsupported Workshop puzzle.");
            if (!await SubmitConstruction(candidate)) { PresentMode(); return; }
            if (index == 0) _nextId = new(first.Value + 2);
            else if (index == DelayedSignalIndex) _nextId = new(first.Value + 3);
            Select(null); _tool = null; ClearPreview(); _undo.Clear();
            ResetUiAnimations(); PresentMode();
        }
        catch (Exception error) { if (!_workshopUiRemoved) { PresentMode(); _status.Text = error.Message; } }
    }
    private void PresentMode()
    {
        var first = World.Construction.Puzzle.Id == WorkshopPuzzleId.FirstPrinciples;
        var delayed = World.Construction.Puzzle.Id == WorkshopPuzzleId.DelayedSignal;
        var authored = first || delayed;
        _picker.Select(first ? 0 : delayed ? DelayedSignalIndex : FreeWorkshopIndex);
        _inventory = authored ? WorkshopInventoryPolicy.Authored(World.Construction.Puzzle) : WorkshopInventoryPolicy.Free;
        _title.Text = first ? "First principles" : delayed ? "Wait for it" : "Free workshop";
        _task.Text = first ? "Guide the orange ball into the green receiver. Place the two ramps to build a path through the air."
            : delayed ? "Light the lamp only after the delay box finishes its countdown."
            : "Place parts and connect activation sockets. Run tries the machine; Reset restores its starting arrangement.";
        _hint.Visible = false; _task.Visible = _hintButton.Visible = authored;
        _optionsPanel.Visible = false; _objectivePanel.Visible = authored;
        if (authored) _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100);
        _precisionText.Text = _precision.Value < 33 ? "Forgiving" : _precision.Value > 66 ? "Precise" : "Balanced";
        SetBuildUi(); RefreshPalette(); RefreshLayers();
        _placementHeight = new((Half)3); SetBuildView(authored);
        if (delayed) _status.Text = "Place the Delay and connect its activation sockets. Run tests the signal; Reset lets you retry.";
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
            var candidate = World.Construction.Puzzle.Id switch
            {
                WorkshopPuzzleId.FirstPrinciples => FirstPrinciples.WithPrecision(World.Construction, precision),
                WorkshopPuzzleId.DelayedSignal => DelayedSignal.WithPrecision(World.Construction, precision),
                _ => throw new ArgumentException("Unsupported authored assistance.")
            };
            if (!await SubmitConstruction(candidate)) { _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100); return; }
            _precisionText.Text = value < 33 ? "Forgiving" : value > 66 ? "Precise" : "Balanced";
            _status.Text = World.Construction.Puzzle.Id == WorkshopPuzzleId.DelayedSignal
                ? "Switch sensitivity updated. Delay placement remains manual."
                : "Receiver assistance updated. Ramp placement remains manual.";
        }
        catch (Exception error) { _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100); _status.Text = error.Message; }
    }
}
