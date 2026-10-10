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
                WorkshopPuzzleId.DominoEffect => DominoEffectIndex,
                WorkshopPuzzleId.BumperSidekick => BumperSidekickIndex,
                WorkshopPuzzleId.BumperDepth => BumperDepthIndex,
                WorkshopPuzzleId.WallAndBumper => WallAndBumperIndex,
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
            else if (index == BumperSidekickIndex)
            {
                if (first.Value >= ulong.MaxValue - 1) throw new ArgumentException("Part identity is exhausted.");
                candidate = BumperSidekick.Create(World.Construction.Revision, World.Construction.Settings,
                    first, new(first.Value + 1), new((Half)(_precision.Value / 100)));
            }
            else if (index is BumperDepthIndex or WallAndBumperIndex)
            {
                if (first.Value >= ulong.MaxValue - 1) throw new ArgumentException("Part identity is exhausted.");
                candidate = BumperAdvanced.Create(index == BumperDepthIndex ? WorkshopPuzzleId.BumperDepth : WorkshopPuzzleId.WallAndBumper,
                    World.Construction.Revision, World.Construction.Settings, first, new(first.Value + 1), new((Half)(_precision.Value / 100)));
            }
            else if (index == DelayedSignalIndex)
            {
                if (first.Value >= ulong.MaxValue - 2) throw new ArgumentException("Part identity is exhausted.");
                candidate = DelayedSignal.Create(World.Construction.Revision, World.Construction.Settings,
                    first, new(first.Value + 1), new(first.Value + 2), new((Half)(_precision.Value / 100)));
            }
            else if (index == DominoEffectIndex)
            {
                if (first.Value >= ulong.MaxValue - 2) throw new ArgumentException("Part identity is exhausted.");
                candidate = DominoEffect.Create(World.Construction.Revision, World.Construction.Settings,
                    first, new(first.Value + 1), new(first.Value + 2), new((Half)(_precision.Value / 100)));
            }
            else throw new ArgumentException("Unsupported Workshop puzzle.");
            if (!await SubmitConstruction(candidate)) { PresentMode(); return; }
            if (index is 0 or BumperSidekickIndex or BumperDepthIndex or WallAndBumperIndex) _nextId = new(first.Value + 2);
            else if (index is DelayedSignalIndex or DominoEffectIndex) _nextId = new(first.Value + 3);
            Select(null); _tool = null; ClearPreview(); _undo.Clear();
            ResetUiAnimations(); PresentMode();
        }
        catch (Exception error) { if (!_workshopUiRemoved) { PresentMode(); _status.Text = error.Message; } }
    }
    private void PresentMode()
    {
        var first = World.Construction.Puzzle.Id == WorkshopPuzzleId.FirstPrinciples;
        var delayed = World.Construction.Puzzle.Id == WorkshopPuzzleId.DelayedSignal;
        var dominoes = World.Construction.Puzzle.Id == WorkshopPuzzleId.DominoEffect;
        var sidekick = World.Construction.Puzzle.Id == WorkshopPuzzleId.BumperSidekick;
        var depth = World.Construction.Puzzle.Id == WorkshopPuzzleId.BumperDepth;
        var wall = World.Construction.Puzzle.Id == WorkshopPuzzleId.WallAndBumper;
        var authored = first || delayed || dominoes || sidekick || depth || wall;
        _picker.Select(first ? 0 : delayed ? DelayedSignalIndex : dominoes ? DominoEffectIndex : sidekick ? BumperSidekickIndex : depth ? BumperDepthIndex : wall ? WallAndBumperIndex : FreeWorkshopIndex);
        _inventory = authored ? WorkshopInventoryPolicy.Authored(World.Construction.Puzzle) : WorkshopInventoryPolicy.Free;
        _title.Text = first ? "First principles" : delayed ? "Wait for it" : dominoes ? "The domino effect" : sidekick ? "A little sidekick" : depth ? "Bounce into depth" : wall ? "Build the rebound" : "Free workshop";
        _task.Text = first ? "Guide the orange ball into the green receiver. Place the two ramps to build a path through the air."
            : depth ? "Use the bumper to bounce the ball through depth into the receiver."
            : wall ? "Build a wall and bumper route to return the ball to the receiver."
            : sidekick ? "Use the pinball bumper to bounce the orange ball into the receiver."
            : delayed ? "Light the lamp only after the delay box finishes its countdown."
            : dominoes ? "Make the fixed end domino fall and light the lamp. Fill the gap with four dominoes and connect the end domino to the lamp."
            : "Place parts and connect activation sockets. Run tries the machine; Reset restores its starting arrangement.";
        _hint.Visible = false; _task.Visible = _hintButton.Visible = authored;
        _optionsPanel.Visible = false; _objectivePanel.Visible = authored;
        if (authored) _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100);
        _precisionText.Text = _precision.Value < 33 ? "Forgiving" : _precision.Value > 66 ? "Precise" : "Balanced";
        SetBuildUi(); RefreshPalette(); RefreshLayers();
        _placementHeight = new((Half)3); SetBuildView(authored);
        if (delayed) _status.Text = "Place the Delay and connect its activation sockets. Run tests the signal; Reset lets you retry.";
        if (dominoes) _status.Text = "Stand the four dominoes between the ball and the end domino, then connect the end domino to the lamp.";
        if (sidekick) _status.Text = "Place the bumper just left of the falling ball. Run tests the rebound; Reset lets you retry.";
        if (depth) _status.Text = "Orbit with Q/E and place the bumper in depth. Manual placement; physical nudging is not yet supported.";
        if (wall) _status.Text = "Resize the wall and place the bumper to build the return route. Placement remains manual.";
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
                WorkshopPuzzleId.DominoEffect => DominoEffect.WithPrecision(World.Construction, precision),
                WorkshopPuzzleId.BumperSidekick => BumperSidekick.WithPrecision(World.Construction, precision),
                WorkshopPuzzleId.BumperDepth or WorkshopPuzzleId.WallAndBumper => BumperAdvanced.WithPrecision(World.Construction, precision),
                _ => throw new ArgumentException("Unsupported authored assistance.")
            };
            if (!await SubmitConstruction(candidate)) { _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100); return; }
            _precisionText.Text = value < 33 ? "Forgiving" : value > 66 ? "Precise" : "Balanced";
            _status.Text = World.Construction.Puzzle.Id switch
            {
                WorkshopPuzzleId.DelayedSignal => "Switch sensitivity updated. Delay placement remains manual.",
                WorkshopPuzzleId.DominoEffect => "Difficulty recorded. Domino placement remains manual.",
                WorkshopPuzzleId.BumperSidekick or WorkshopPuzzleId.BumperDepth or WorkshopPuzzleId.WallAndBumper => "Receiver assistance updated. Placement remains manual.",
                _ => "Receiver assistance updated. Ramp placement remains manual."
            };
        }
        catch (Exception error) { _precision.SetValueNoSignal((double)World.Construction.Puzzle.Precision.Value * 100); _status.Text = error.Message; }
    }
}
