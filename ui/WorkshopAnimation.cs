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
    private readonly List<WorkshopUiBinding> _uiBindings = new(16);
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
        UiBinding(WorkshopUiTarget.Hint)?.Apply(new(0f));
    }
    private void HideHint()
    {
        World.ControlUi(WorkshopUiTarget.Hint, AnimationControlKind.Hide, false);
        _hint.Visible = false;
        UiBinding(WorkshopUiTarget.Hint)?.ApplyNeutral();
    }
    private void ClearGoalFeedback()
    {
        UiBinding(WorkshopUiTarget.Goal)?.ApplyNeutral();
    }
    private void ResetUiAnimations()
    {
        _runControlQueued.Clear();
        ClearGoalFeedback();
        HideHint();
    }
    private void ApplyUiBindings()
    {
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
        }
    }
    private void RemoveUiAnimations() => ResetUiAnimations();
}
