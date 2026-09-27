using Godot;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Passive three-input coupler. Routing preserves per-ray budgets; no stored/new light.</summary>
public partial class BeamCombinerPart : MachinePart
{
    public const float Retention=.9f;
    public static readonly Vector3 Exit=new(.76f,0,0);
    public override OpticalOutlet? OpticalOutput=>new(Exit,Vector3.Right);
    public override IReadOnlyList<OpticalSurface> OpticalSurfaces=>
    [
        new(OpticalPortId.First,new(new(-.76f,0,0),Vector3.Left,.43f),OpticalInteraction.Route,Vector3.One*Retention),
        new(OpticalPortId.Second,new(new(0,0,.76f),Vector3.Back,.43f),OpticalInteraction.Route,Vector3.One*Retention),
        new(OpticalPortId.Third,new(new(0,.76f,0),Vector3.Up,.43f),OpticalInteraction.Route,Vector3.One*Retention)
    ];
    public Vector3 InputPower { get; private set; }
    public Vector3 OutputPower { get; private set; }
    private readonly List<StandardMaterial3D> _lenses=[];
    private OpticalPathVisual _preview=null!;
    public override void ReceiveOpticalPower(IReadOnlyDictionary<OpticalPortId,Vector3> power)
    {
        InputPower=power.Values.Aggregate(Vector3.Zero,(a,b)=>a+b);

    }
    public override void ReceiveOpticalPath(IReadOnlyList<OpticalSegment> path)
    {
        var exit=Transform*Exit;
        OutputPower=path.Where(s=>s.From.DistanceSquaredTo(exit)<1e-6f)
            .Aggregate(Vector3.Zero,(power,s)=>power+s.Power);
        Active=OutputPower.LengthSquared()>1e-8f;
    }
    protected override void Build()
    {
        PickRadius=1.3f;
        AddBox(Vector3.Zero,new(1.4f,1.4f,1.4f),new("#66b8c9"));
        AddBox(new(0,-.85f,0),new(1.8f,.18f,1.8f),new("#293954"));
        foreach(var surface in OpticalSurfaces)
        {
            var t=surface.Aperture;
            var ring=PartArt.Ring(Visual,.48f,.055f,new("#fff8e9"),t.At);
            ring.Quaternion=new Quaternion(Vector3.Up,t.Normal);
            var lens=PartArt.Cylinder(Visual,.43f,.018f,new("#556573"),t.At);
            lens.Quaternion=new Quaternion(Vector3.Up,t.Normal);
            // Engraved count marks identify inputs, not required colours: all accept any RGB.
            var marks=new Node3D {Position=t.At+t.Normal*.03f,Quaternion=new Quaternion(Vector3.Left,t.Normal)};
            Visual.AddChild(marks);
            for(var i=0;i<(int)surface.Id;i++)
                PartArt.Box(marks,new(.01f,.1f,.035f),new("#fff8e9"),new(0,.3f,(i-((int)surface.Id-1)*.5f)*.1f));
        }
        var output=PartArt.Cylinder(Visual,.34f,.04f,new("#556573"),Exit);
        output.RotationDegrees=new(0,0,90);
        _lenses.Add((StandardMaterial3D)output.MaterialOverride);
        var rim=PartArt.Ring(Visual,.4f,.045f,new("#e8b764"),Exit);
        rim.RotationDegrees=new(0,0,90);
        _preview=new OpticalPathVisual {Name="OutgoingAimPreview",Preview=true};
        Visual.AddChild(_preview);
    }
    public override void _Process(double delta)
    {
        var ink=Active?OpticalColours.BeamInk(OutputPower):new Color("#556573");
        foreach(var material in _lenses)
            material.AlbedoColor=material.AlbedoColor.Lerp(ink,1-Mathf.Exp(-(float)delta*12));
        _preview.Visible=false;
        if(!IsSelected||GetParent() is not MachineWorld world||world.Running||world.Won)return;
        _preview.Visible=true;
        _preview.Refresh(OpticalPathVisual.Merge(world.Parts.Where(p=>p.Visible&&p.OpticalPreviewSource.HasValue)
            .SelectMany(p=>OpticalNetwork.Trace(world,p,p.OpticalPreviewSource!.Value).Segments)
            .Where(s=>s.OriginPart==Uid).ToArray()));
    }
}
