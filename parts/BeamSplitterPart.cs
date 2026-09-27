using Godot;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Two-sided half-silvered aperture. Its solid glass is optically transparent, frame opaque.</summary>
public partial class BeamSplitterPart : MachinePart
{
    public override IReadOnlyList<OpticalSurface> OpticalSurfaces=>[new(OpticalPortId.Main,new(Vector3.Zero,Vector3.Left,.65f),OpticalInteraction.Split,Vector3.One)];
    private OpticalPathVisual _preview=null!;
    protected override void Build()
    {
        PickRadius=1.2f;
        foreach(var sign in new[]{-1,1})
        {
            AddBox(new(0,sign*.74f,0),new(.18f,.18f,1.65f),new("#fff8e9"));
            AddBox(new(0,0,sign*.74f),new(.18f,1.3f,.18f),new("#fff8e9"));
        }
        AddBox(new(0,-1,0),new(.9f,.2f,1.65f),new("#293954"));
        Boxes.Add(new(Vector3.Zero,new(.015f,.65f,.65f),false));
        var pane=PartArt.Box(Visual,new(.03f,1.3f,1.3f),new("#66b8c9"));
        pane.MaterialOverride=new StandardMaterial3D
        {
            AlbedoColor=new Color("#66b8c9"){A=.25f},
            Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode=BaseMaterial3D.CullModeEnum.Disabled
        };
        pane.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
        var ring=PartArt.Ring(Visual,.64f,.015f,new("#e8b764"));
        ring.RotationDegrees=new(0,0,90);
        // Matched gold studs visually mark the pair of equal output paths.
        foreach(var z in new[]{-.24f,.24f})PartArt.Sphere(Visual,.055f,new("#f7cb52"),new(0,.74f,z));
        _preview=new OpticalPathVisual{Name="OutgoingAimPreview",Preview=true};
        Visual.AddChild(_preview);
    }
    public override void _Process(double delta)
    {
        _preview.Visible=false;
        if(!IsSelected||GetParent() is not MachineWorld world||world.Running||world.Won)return;
        _preview.Visible=true;
        _preview.Refresh(world.Parts.Where(p=>p.Visible&&p.OpticalPreviewSource.HasValue)
            .SelectMany(p=>OpticalNetwork.Trace(world,p,p.OpticalPreviewSource!.Value).Segments)
            .Where(s=>s.OriginPart==Uid).ToArray());
    }
}
