using System;
using Godot;

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

internal readonly record struct HintDurationSeconds
{
    public Half Value { get; }
    public HintDurationSeconds(Half value)
    {
        if (!Half.IsFinite(value) || value <= (Half)0) throw new ArgumentOutOfRangeException(nameof(value));
        Value = value;
    }
}

internal readonly record struct HintOpacity
{
    public Half Value { get; }
    public HintOpacity(Half value)
    {
        if (!Half.IsFinite(value) || value < (Half)0 || value > (Half)1) throw new ArgumentOutOfRangeException(nameof(value));
        Value = value;
    }
}

/// <summary>CPU cosmetic opacity with canonical Half authored/state values and an external presentation clock.</summary>
public partial class Workshop
{
    private static readonly HintDurationSeconds HintDuration = new((Half).16);
    private HintOpacity _hintOpacity = new((Half)1);
    private double _hintElapsedSeconds;
    private bool _hintPlaying;
    private bool _hintNeedsPresentation;

    private void InitializeUiAnimations() => ResetUiAnimations();
    private void RevealHint()
    {
        if (!_hint.IsInsideTree()) return;
        _hintElapsedSeconds = 0;
        _hintPlaying = true;
        _hintNeedsPresentation = false;
        ApplyHintOpacity(new((Half)0));
    }
    private void HideHint()
    {
        ResetUiAnimations();
        _hint.Visible = false;
    }
    private void ResetUiAnimations()
    {
        _hintPlaying = false;
        _hintNeedsPresentation = false;
        _hintElapsedSeconds = 0;
        ApplyHintOpacity(new((Half)1));
    }
    private void PresentUiAnimations(double delta)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        if (!_hint.IsInsideTree() || (!_hintPlaying && !_hintNeedsPresentation)) return;
        if (_hintPlaying)
        {
            var elapsed = _hintElapsedSeconds + delta;
            if (!double.IsFinite(elapsed)) throw new ArgumentOutOfRangeException(nameof(delta));
            // External presentation-clock seconds advance while hidden, then retire at completion.
            _hintElapsedSeconds = Math.Min(elapsed, (double)HintDuration.Value);
            _hintNeedsPresentation = true;
            if (_hintElapsedSeconds == (double)HintDuration.Value) _hintPlaying = false;
        }
        if (!_hint.IsVisibleInTree()) return;
        var progress = _hintElapsedSeconds / (double)HintDuration.Value;
        var p = Math.Min(progress, 1 - progress);
        var smooth = p * p * p * (p * (p * 6 - 15) + 10);
        // Preserve the authored symmetric quintic; narrow the cosmetic result once.
        ApplyHintOpacity(new((Half)(progress <= .5 ? smooth : 1 - smooth)));
        _hintNeedsPresentation = false;
    }
    private void ApplyHintOpacity(HintOpacity opacity)
    {
        _hintOpacity = opacity;
        var color = _hint.Modulate;
        color.A = (float)_hintOpacity.Value; // Named Godot presentation adapter.
        _hint.Modulate = color;
    }
    private void RemoveUiAnimations() => ResetUiAnimations();
}
