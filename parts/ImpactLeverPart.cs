using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum ImpactLeverParameter { BeamMass, InitialAngle }

/// <summary>Passive fixed-pivot beam. Its pose follows finite-inertia contact, never a scripted flip.</summary>
public partial class ImpactLeverPart : MachinePart
{
    public const string CatalogId = "impact_lever";
    public static readonly Vector3 BeamHalf = new(1.8f, .12f, .55f);
    public const double LimitAngle = Math.PI / 6;
    private Node3D _beamVisual = null!;
    public HingedBody Beam { get; private set; } = null!;
    private IReadOnlyList<HingedBody> _hinges = [];
    public override IReadOnlyList<HingedBody> HingedBodies => _hinges;
    public override float SurfaceBounce => 0;
    public int ImpactCount { get; private set; }

    public override void ValidateParameters()
    {
        var mass = ReadParameter(ImpactLeverParameter.BeamMass);
        var angle = ReadParameter(ImpactLeverParameter.InitialAngle);
        if (!float.IsFinite(mass) || mass < .5f || mass > 20 ||
            !float.IsFinite(angle) || angle < -30 || angle > 30)
            throw new ArgumentException("Lever beam mass must be 0.5–20 and initial angle −30–30 degrees.");
    }

    protected override void Build()
    {
        PickRadius = 2;
        var mass = ReadParameter(ImpactLeverParameter.BeamMass);
        var inertia = mass * (BeamHalf.X * BeamHalf.X + BeamHalf.Y * BeamHalf.Y) / 3;
        var initialAngle = ReadParameter(ImpactLeverParameter.InitialAngle) * Math.PI / 180;
        Beam = new(this, HingeRole.Beam, Vector3.Zero, BeamHalf, inertia,
            -LimitAngle, LimitAngle, initialAngle, .1f);
        _hinges = [Beam];
        AddBox(new(0,-1.18f,0), new(1.4f,.16f,1), new("#293954"));
        PartArt.Cylinder(Visual,.22f,1.02f,new("#e8b764"),new(0,-.59f,0));
        var pin = PartArt.Cylinder(Visual,.18f,1.25f,new("#e8b764"));
        pin.RotationDegrees = new(90,0,0);
        // Fixed physical stops meet the underside at the authored angular bounds.
        foreach (var side in new[] { -1f, 1f })
        {
            var at = new Vector3(side * 1.5f,-1.004f,0);
            PartArt.Cylinder(Visual,.13f,.3f,new("#e8b764"),at);
            Boxes.Add(new(at,new(.13f,.15f,.3f)));
        }
        _beamVisual = new Node3D();
        Visual.AddChild(_beamVisual);
        PartArt.Box(_beamVisual, BeamHalf * 2, new("#fff8e9"));
        PartArt.Box(_beamVisual, new(3.38f,.014f,.82f), new("#66b8c9"),new(0,.121f,0));
        // Gold witness marks expose lever arms without a ruler/inspector overlay.
        foreach (var x in new[] { -1.2f, -.6f, .6f, 1.2f })
            PartArt.Box(_beamVisual,new(.035f,.016f,.3f),new("#e8b764"),new(x,.122f,0));
        _beamVisual.Transform = Beam.LocalPose;
    }

    public override void BeforeStep(MachineWorld world, float delta) => Active = false;
    public override void AfterStep(MachineWorld world, float delta) => _beamVisual.Transform = Beam.LocalPose;
    public override void OnContact(MachinePart body, float speed, MachineWorld world)
    {
        Active = true;
        if (speed < .45f) return;
        ImpactCount++;
        world.Events.TryAdd(new(MachineEventKind.Bounced, Uid, body.Uid),world.Ticks);
    }
}
