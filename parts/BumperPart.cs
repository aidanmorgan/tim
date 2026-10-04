using Godot;
using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Source artwork and typed calibration; shared physics and Animation own every response.</summary>
public partial class BumperPart : MachinePart
{
    public BumperWork CanonicalWork { get; private set; } = BumperWork.Default;
    public uint HitCount => ContactWorkCount;
    public Half PulseAmount => ContactWorkBlend;
    internal void ApplyWork(BumperWork work) { work.Validate(); CanonicalWork = work; }

    protected override void Build()
    {
        ApplyWork(Definition.Bumper!.Capture());
        const float radius = .65f;
        PickRadius = .8f;
        var head = PartArt.Sphere(Visual, radius, Definition.Color);
        head.Name = "BumperHead";
        PartArt.Cylinder(Visual, .3f, .12f, new("#273744"), new(0, -.46f, 0));
        PartArt.Cylinder(Visual, .25f, .12f, new("#f7cb52"), new(0, .59f, 0));
        var pulseRing = PartArt.Ring(Visual, .66f, .035f, new("#fff0c2"));
        pulseRing.Name = "ImpactRing";
        PartArt.Ring(Visual, .49f, .018f, new("#fff0c2"), new(0, .43f, 0));
        BindContactWork(pulseRing, WorkshopVisualProperty.UniformScale, (Half)1, (Half)1.24);
        BindContactWork(pulseRing, WorkshopVisualProperty.AlbedoRed, (Half)1, (Half)(247d / 255));
        BindContactWork(pulseRing, WorkshopVisualProperty.AlbedoGreen, (Half)(240d / 255), (Half)(203d / 255));
        BindContactWork(pulseRing, WorkshopVisualProperty.AlbedoBlue, (Half)(194d / 255), (Half)(82d / 255));
    }
}
