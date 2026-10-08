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

/// <summary>CPU cosmetic opacity with canonical Half authored/state values and an external presentation clock.</summary>
public partial class Workshop
{
    private static readonly Half HintDuration = (Half).16;
    private Half _hintOpacity = (Half)1;
    private double _hintElapsedSeconds;
    private bool _hintPlaying;

    private void InitializeUiAnimations() => ApplyHintOpacity((Half)1);
    private void RevealHint()
    {
        _hintElapsedSeconds = 0;
        _hintPlaying = true;
        ApplyHintOpacity((Half)0);
    }
    private void HideHint()
    {
        _hintPlaying = false;
        _hint.Visible = false;
    }
    private void ResetUiAnimations()
    {
        _hintPlaying = false;
        _hintElapsedSeconds = 0;
        ApplyHintOpacity((Half)1);
    }
    private void PresentUiAnimations(double delta)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        if (!_hintPlaying || !_hint.IsVisibleInTree() || _workshopUiRemoved) return;
        _hintElapsedSeconds += delta; // External presentation-clock seconds, not retained game value.
        var progress = Math.Clamp(_hintElapsedSeconds / (double)HintDuration, 0, 1);
        // Permitted CPU cosmetic computation is rounded once at the canonical state boundary.
        ApplyHintOpacity((Half)(progress * progress * (3 - 2 * progress)));
        if (progress == 1) _hintPlaying = false;
    }
    private void ApplyHintOpacity(Half opacity)
    {
        _hintOpacity = opacity;
        var color = _hint.Modulate;
        color.A = (float)_hintOpacity; // Named Godot presentation adapter.
        _hint.Modulate = color;
    }
    private void RemoveUiAnimations() => _hintPlaying = false;
}
