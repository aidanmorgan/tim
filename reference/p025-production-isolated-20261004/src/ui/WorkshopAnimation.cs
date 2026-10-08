using System;
using Godot;
using CuriousContraptions.Presentation;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Closed identities for the controls used by presentation integration.
/// Node names are an explicit Godot/automation boundary, never behaviour selectors.</summary>
public enum WorkshopAnimationControl { Hint, ShowHint, Goal, Run, LevelPicker, Menu }

public static class WorkshopAnimationControlBoundary
{
    public static string NodeName(WorkshopAnimationControl control)=>control switch
    {
        WorkshopAnimationControl.Hint=>"PuzzleHint",
        WorkshopAnimationControl.ShowHint=>"ShowHint",
        WorkshopAnimationControl.Goal=>"Goal",
        WorkshopAnimationControl.Run=>"RunMachine",
        WorkshopAnimationControl.LevelPicker=>"LevelPicker",
        WorkshopAnimationControl.Menu=>"Menu",
        _=>throw new ArgumentOutOfRangeException(nameof(control))
    };
}

/// <summary>Browser target application for the portable animation owner; no second hint evaluator.</summary>
public partial class Workshop
{
    private bool _hintAnimationVisible;
    private void InitializeUiAnimations() => ApplyHintOpacity(new((Half)1));
    private void RevealHint()
    {
        if (!_hint.IsInsideTree()) return;
        _hintAnimationVisible = _hint.IsVisibleInTree();
        World.ControlHint(HintControlKind.Reveal, _hintAnimationVisible);
        ApplyHintOpacity(new((Half)0));
    }
    private void HideHint()
    {
        World.ControlHint(HintControlKind.Hide, false);
        _hint.Visible = false;
        ApplyHintOpacity(new((Half)1));
    }
    private void ResetUiAnimations() => HideHint();
    private void PresentUiAnimations(double delta)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        if (!_hint.IsInsideTree()) return;
        var visible = _hint.IsVisibleInTree();
        if (visible != _hintAnimationVisible)
        {
            World.ControlHint(HintControlKind.Visibility, visible);
            _hintAnimationVisible = visible;
        }
        if (World.TryHint(out var sample))
        {
            ApplyHintOpacity(new(sample.Opacity));
            World.RecordHintPresentation();
        }
    }
    private void ApplyHintOpacity(AnimationOpacity opacity)
    {
        var color = _hint.Modulate;
        color.A = (float)opacity.Value; // Named Godot presentation adapter.
        _hint.Modulate = color;
    }
    private void RemoveUiAnimations() => ResetUiAnimations();
}
