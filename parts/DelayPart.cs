using Godot;
using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Source artwork and canonical authoring only. The shared discrete network owns time.</summary>
public partial class DelayPart : MachinePart
{
    public DelayDuration CanonicalDuration { get; private set; } = DelayDuration.Default;
    internal void ApplyDuration(DelayDuration duration) { duration.Validate(); CanonicalDuration = duration; }
    protected override void Build()
    {
        ApplyDuration(Definition.Delay!.Capture());
        PickRadius = .9f;
        PartArt.Box(Visual, new(1.25f,1.25f,.6f), Definition.Color);
        var housing = PartArt.Cylinder(Visual,.65f,.6f,Definition.Color);
        housing.RotationDegrees = new(90,0,0);
        var face = PartArt.Cylinder(Visual,.55f,.035f,new("#fff8e9"),new(0,0,.32f));
        face.RotationDegrees = new(90,0,0);
        PartArt.Box(Visual,new(1.35f,.15f,.8f),new("#293954"),new(0,-.7f,0));
        for (var i=0;i<12;i++)
        {
            var angle=i*Mathf.Tau/12;
            PartArt.Sphere(Visual,.025f,new("#293954"),new(Mathf.Sin(angle)*.46f,Mathf.Cos(angle)*.46f,.35f));
        }
        var hand = new Node3D {Name="CountdownHand",Position=new(0,0,.37f)};
        Visual.AddChild(hand);
        PartArt.Box(hand,new(.055f,.38f,.03f),new("#293954"),new(0,.16f,0));
        PartArt.Sphere(Visual,.075f,new("#f7cb52"),new(0,0,.4f));
        var indicator=PartArt.Sphere(Visual,.065f,new("#556573"),new(0,-.35f,.37f));
        PartArt.Sphere(Visual,.075f,new("#e8b764"),new(-.72f,0,0));
        PartArt.Sphere(Visual,.075f,new("#e8b764"),new(.72f,0,0));
        BindTimerProgress(hand, WorkshopVisualProperty.LocalRotationZ, (Half)0, (Half)(-Math.Tau));
        BindTimerColour(indicator, WorkshopVisualProperty.AlbedoRed, (Half)(85d/255), (Half)(232d/255), (Half)(247d/255));
        BindTimerColour(indicator, WorkshopVisualProperty.AlbedoGreen, (Half)(101d/255), (Half)(183d/255), (Half)(203d/255));
        BindTimerColour(indicator, WorkshopVisualProperty.AlbedoBlue, (Half)(115d/255), (Half)(100d/255), (Half)(82d/255));
    }
}
