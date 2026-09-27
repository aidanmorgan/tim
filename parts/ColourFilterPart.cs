using Godot;
using System.Collections.Generic;
using System;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Two-sided absorbing channel filter; never creates a missing colour.</summary>
public partial class ColourFilterPart : MachinePart
{
    [Export] public OpticalColour Colour { get; set; }=OpticalColour.Red;
    public override IReadOnlyList<OpticalSurface> OpticalSurfaces=>[new(OpticalPortId.Main,new(Vector3.Zero,Vector3.Left,.65f),
        OpticalInteraction.Filter,OpticalColours.Mask(Colour))];
    private OpticalPathVisual _preview=null!;
    public override void ValidateParameters()
    {
        if(Colour is not (OpticalColour.Red or OpticalColour.Green or OpticalColour.Blue))
            throw new ArgumentException("A filter requires a red, green or blue channel.");
    }
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
        var ink=OpticalColours.Ink(Colour);
        var pane=PartArt.Box(Visual,new(.03f,1.3f,1.3f),ink);
        ink.A=.3f;
        pane.MaterialOverride=new StandardMaterial3D
        {
            AlbedoColor=ink,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode=BaseMaterial3D.CullModeEnum.Disabled
        };
        pane.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
        foreach(var x in new[]{-.1f,.1f})OpticalColours.Marks(Visual,Colour,new(x,.74f,0));
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
