using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Presentation;
namespace CuriousContraptions;

/// <summary>A self-contained battery torch, latched on by a physical button or activation command.</summary>
public partial class FlashlightPart : MachinePart
{
    public static readonly Vector3 LensPosition = new(.66f, 0, 0);
    public const float Range = 8;
    public const float ConeCosine = .9659258f; // 15 degree half-angle.
    public const float Intensity = 24;
    private LightConeVisual _beam = null!;
    private MeshInstance3D _lens = null!;
    private MeshInstance3D _button = null!;
    private static readonly LightEmitter Emitter=new(LensPosition,Vector3.Right,Range,ConeCosine,Intensity);
    private static readonly AnimationDefinition ButtonTravel=new(0,-.06,.06,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation);
    private static readonly AnimationDefinition LensTransition=new(0,1,.06,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation);
    public override IReadOnlyList<SceneTranslationAnimation> TranslationAnimations=>
        [new(_button,ButtonTravel,AnimationTranslationAxis.Y,SceneAnimationSignal.OwnerActive,SceneAnimationDrive.Endpoint)];
    public override IReadOnlyList<SceneColourAnimation> ColourAnimations=>
        [new(_lens,LensTransition,new("#556573"),new("#fff0a5"),SceneAnimationSignal.OwnerActive,SceneAnimationDrive.Endpoint)];
    public override IReadOnlyList<SceneLightCone> LightCones=>[new(_beam,Emitter)];
    public override bool CanReceiveActivation => true;
    public override LightEmitter? LightSource => Active
        ? Emitter : null;
    protected override void Build()
    {
        PickRadius = .9f;
        AddBox(new(-.1f, 0, 0), new(1, .6f, .6f), Definition.Color, false);
        var body = PartArt.Cylinder(Visual, .3f, 1, Definition.Color, new(-.1f, 0, 0));
        body.RotationDegrees = new(0, 0, 90);
        var collar = PartArt.Cylinder(Visual, .43f, .25f, new("#fff8e9"), new(.5f, 0, 0));
        collar.RotationDegrees = new(0, 0, 90);
        Boxes.Add(new(new(.5f, 0, 0), new(.125f, .43f, .43f), MachinePart.RootBody));
        _lens = PartArt.Cylinder(Visual, .34f, .03f, new("#556573"), LensPosition);
        _lens.RotationDegrees = new(0, 0, 90);
        AddBox(new(-.15f, .36f, 0), new(.4f, .14f, .35f), Definition.Color, false);
        _button = PartArt.Cylinder(Visual, .18f, .14f, new("#f7cb52"), new(-.15f, .36f, 0));
        PartArt.Box(Visual, new(1.2f, .12f, .85f), new("#293954"), new(0, -.38f, 0));
        _beam = new LightConeVisual { Name = "LightCone", Visible = false };
        Visual.AddChild(_beam);
    }
    public override void ObserveContact(SceneContact contact,MachineWorld world)
    {
        var speed=contact.ApproachSpeed;
        var point=contact.SelfLocalPoint;
        if (speed >= Assistance(world.Precision).TriggerThreshold &&
            point.Y > .3f && Math.Abs(point.X + .15) < .45f && Math.Abs(point.Z) < .4f)
            world.Activate(this);
    }
}
