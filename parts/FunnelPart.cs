using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Passive wide-mouth inlet. The sloping shell redirects actual sphere contacts.</summary>
public partial class FunnelPart : MachinePart,ITubePart
{
    public const float HalfLength=.9f;
    public const float InletRadius=1.3f;
    public const float ShellThickness=.05f;
    public override float SurfaceBounce=>.15f;
    public IEnumerable<TubeMouth> Mouths=>
    [
        new(TubeMouthId.Start,Vector3.Left*(HalfLength+.09f),Vector3.Left,InletRadius),
        new(TubeMouthId.End,Vector3.Right*(HalfLength+.09f),Vector3.Right,PipePart.BoreRadius)
    ];
    protected override void Build()
    {
        PickRadius=1.75f;
        PipeArt.Frustum(Visual,HalfLength,InletRadius,PipePart.BoreRadius,ShellThickness,new(.40f,.72f,.79f,.16f));
        Frustums.Add(new(Transform3D.Identity,HalfLength,InletRadius,PipePart.BoreRadius,ShellThickness));
        foreach(var inlet in new[]{true,false})
        {
            var x=inlet?-HalfLength:HalfLength;
            var radius=inlet?InletRadius:PipePart.BoreRadius;
            var pose=new Transform3D(Basis.Identity,new(x,0,0));
            PipeArt.Cylinder(Visual,pose,.09f,radius,radius+.13f,new("#fff8e9"),true);
            Tubes.Add(new(pose,.09f,radius,radius+.13f,true));
        }
        foreach(var side in new[]{-1,1})
            PartArt.Line(Visual,new(-HalfLength,0,side*(InletRadius+ShellThickness)),
                new(HalfLength,0,side*(PipePart.BoreRadius+ShellThickness)),new("#293954"),.018f);
    }
}
