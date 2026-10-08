using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Finite, front-silvered disc: actual transformed normal determines reflection.</summary>
public partial class MirrorPart : MachinePart
{
    public override IReadOnlyList<OpticalSurface> OpticalSurfaces=>[new(OpticalPortId.Main,new(new(-.18f,0,0),Vector3.Left,.65f),OpticalInteraction.Mirror,Vector3.One)];
    private OpticalPathVisual _preview=null!;
    public override Presentation.SceneOpticalPreview? OpticalPreview=>new(_preview,Presentation.OpticalPreviewComposition.Separate);
    protected override void Build()
    {
        PickRadius=1.2f;
        AddBox(Vector3.Zero,new(.26f,1.55f,1.55f),new("#fff8e9"));
        AddBox(new(0,-1,0),new(.9f,.2f,1.65f),new("#293954"));
        var face=PartArt.Cylinder(Visual,.65f,.035f,new("#66b8c9"),new(-.18f,0,0));
        face.RotationDegrees=new(0,0,90);
        var rim=PartArt.Ring(Visual,.69f,.025f,new("#e8b764"),new(-.18f,0,0));
        rim.RotationDegrees=new(0,0,90);
        // Two quiet cream glints distinguish a reflective face from a receiver's target rings.
        foreach(var z in new[]{-.17f,.17f})
        {
            var glint=PartArt.Box(Visual,new(.018f,.5f,.035f),new("#fff8e9"),new(-.205f,0,z));
            glint.RotationDegrees=new(25,0,0);
        }
        _preview=new OpticalPathVisual {Name="OutgoingAimPreview",Preview=true,Visible=false};
        Visual.AddChild(_preview);
    }
}
