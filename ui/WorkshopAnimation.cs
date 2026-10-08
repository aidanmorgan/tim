using System;
using System.Collections.Generic;
using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Closed identities for the controls used by presentation integration.
/// Node names are an explicit Godot/automation boundary, never behaviour selectors.</summary>
public enum WorkshopUiControl { Hint, ShowHint, Goal, Run, LevelPicker, Menu, Solved }

public static class WorkshopUiControlBoundary
{
    public static string NodeName(WorkshopUiControl control)=>control switch
    {
        WorkshopUiControl.Hint=>"PuzzleHint",
        WorkshopUiControl.ShowHint=>"ShowHint",
        WorkshopUiControl.Goal=>"Goal",
        WorkshopUiControl.Run=>"RunMachine",
        WorkshopUiControl.LevelPicker=>"LevelPicker",
        WorkshopUiControl.Menu=>"Menu",
        WorkshopUiControl.Solved=>"PuzzleSolved",
        _=>throw new ArgumentOutOfRangeException(nameof(control))
    };
}

/// <summary>Declared UI bindings applied by one loop from the shared animation worker; no UI evaluator or direct Modulate write lives here.</summary>
public partial class Workshop
{
    private bool _hintAnimationVisible;
    private readonly List<WorkshopUiBinding> _uiBindings = new();
    private SimulationEpoch _solvedPresentationEpoch;
    private void InitializeUiAnimations()
    {
        _uiBindings.Clear();
        _uiBindings.Add(new(WorkshopUiTarget.Hint, _hint));
        _uiBindings.Add(new(WorkshopUiTarget.Goal, _success));
        foreach (var binding in _uiBindings) binding.ApplyNeutral();
    }
    private WorkshopUiBinding? UiBinding(WorkshopUiTarget target)
    {
        foreach (var binding in _uiBindings) if (binding.Target == target) return binding;
        return null;
    }
    private void RevealHint()
    {
        if (!_hint.IsInsideTree()) return;
        _hintAnimationVisible = _hint.IsVisibleInTree();
        World.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Reveal, _hintAnimationVisible);
        UiBinding(WorkshopUiTarget.Hint)?.Apply(new((Half)0));
    }
    private void HideHint()
    {
        World.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Hide, false);
        _hint.Visible = false;
        UiBinding(WorkshopUiTarget.Hint)?.ApplyNeutral();
    }
    private void ClearGoalFeedback()
    {
        _solvedPresentationEpoch = default;
        UiBinding(WorkshopUiTarget.Goal)?.ApplyNeutral();
    }
    private void ResetUiAnimations()
    {
        ClearGoalFeedback();
        HideHint();
    }
    private void ApplyUiBindings(double delta)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        var hintInTree = _hint.IsInsideTree();
        if (hintInTree)
        {
            var visible = _hint.IsVisibleInTree();
            if (visible != _hintAnimationVisible)
            {
                World.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Visibility, visible);
                _hintAnimationVisible = visible;
            }
        }
        foreach (var binding in _uiBindings)
        {
            if (binding.Target == WorkshopUiTarget.Hint && !hintInTree) continue;
            if (!World.TryUiFrame(binding.Target, out var opacity)) continue;
            binding.Apply(opacity);
            if (binding.Target == WorkshopUiTarget.Goal) AnnounceSolved();
        }
    }
    private void AnnounceSolved()
    {
        // Goal truth is the committed named event; the worker sample only schedules visible feedback.
        // The console line is the e2e driver's solve boundary, emitted once per world epoch.
        if (_solvedPresentationEpoch == World.WorkshopRead.Epoch) return;
        _solvedPresentationEpoch = World.WorkshopRead.Epoch;
        GD.Print("CCGOAL_SOLVED " + World.WorkshopRead.Epoch.Value);
    }
    private void RemoveUiAnimations() => ResetUiAnimations();
}
