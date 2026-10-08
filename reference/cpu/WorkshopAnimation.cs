using System;
using CuriousContraptions.Presentation;
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

public partial class Workshop
{
    private static readonly AnimationDefinition HintReveal=new(0,1,.16,
        AnimationCurve.SmoothStep,AnimationRepeat.Once,AnimationClock.Presentation);
    private SceneAnimationAdapter? _uiAnimations;
    private SceneAnimationTargetHandle _hintTarget;
    private AnimationHandle? _hintAnimation;
    private double _uiPresentationTime;
    private ulong _uiFrame;
    private bool _hintAnimationVisible;
    internal AnimationGeneration UiAnimationGeneration=>_uiAnimations!.Generation;
    internal SceneAnimationFrameWork UiAnimationWork {get;private set;}

    public override void _EnterTree()
    {
        if(_uiAnimations is null)return;
        RegisterUiAnimationTargets();
        GetWindow().FocusExited+=ClearCameraMotion;
    }

    private void InitializeUiAnimations()
    {
        _uiAnimations=new(1);
        RegisterUiAnimationTargets();
    }

    private void RegisterUiAnimationTargets()
    {
        _hintTarget=_uiAnimations!.Register(_hint);
        _hintAnimationVisible=_hint.IsVisibleInTree();
        _uiAnimations.SetVisible(_hintTarget,_hintAnimationVisible);
    }

    private void RevealHint()
    {
        CancelHintAnimation();
        _hintAnimation=_uiAnimations!.BindOpacity(_hintTarget,HintReveal);
        _uiAnimations.StartAt(_hintAnimation.Value,_uiPresentationTime);
    }

    private void CancelHintAnimation()
    {
        if(_hintAnimation is not { } animation)return;
        _uiAnimations!.RemoveAnimation(animation);
        _hintAnimation=null;
    }

    private void HideHint()
    {
        _hint.Visible=false;
        CancelHintAnimation();
    }

    private void ResetUiAnimations()
    {
        _uiAnimations!.Reset();
        _hintAnimation=null;
        _uiPresentationTime=0;_uiFrame=0;UiAnimationWork=default;
        RegisterUiAnimationTargets();
    }

    private void PresentUiAnimations(double delta)
    {
        if(!double.IsFinite(delta)||delta<0||!double.IsFinite(_uiPresentationTime+delta))
            throw new ArgumentOutOfRangeException(nameof(delta));
        var frame=checked(_uiFrame+1);
        var visible=_hint.IsVisibleInTree();
        if(visible!=_hintAnimationVisible)
        {
            _uiAnimations!.SetVisible(_hintTarget,visible);
            _hintAnimationVisible=visible;
        }
        var time=_uiPresentationTime+delta;
        UiAnimationWork=_uiAnimations!.Present(frame,time,0);
        _uiFrame=frame;_uiPresentationTime=time;
    }

    private void RemoveUiAnimations()
    {
        if(_uiAnimations is null)return;
        // Children can already have left the tree, but remain live until their owner is freed.
        _uiAnimations.Reset();
        _hintAnimation=null;_uiFrame=0;_uiPresentationTime=0;UiAnimationWork=default;
    }
}

