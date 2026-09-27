using Godot;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Two absorbing controls switch an independent, lossy optical carrier.</summary>
public partial class OpticalLogicPart : MachinePart
{
    [Export] public LogicGateKind Operation { get; set; }
    public const float Retention = .9f;
    public static readonly Vector3 Exit = new(.76f,0,0);
    private OpticalLogicControl _control = null!;
    public bool First => _control.First;
    public bool Second => _control.Second;
    public bool IsOpen => _control.IsOpen;
    public Vector3 OutputPower { get; private set; }
    private readonly List<StandardMaterial3D> _lamps = [];
    public override void ValidateParameters() => _control = new(Operation);
    public override void BeforeNetworks(MachineWorld world) => _control.Advance();
    public override OpticalOutlet? OpticalOutput => new(Exit,Vector3.Right);
    public override IReadOnlyList<OpticalSurface> OpticalSurfaces =>
    [
        new(OpticalPortId.First,new(new(-.76f,0,0),Vector3.Left,.43f),OpticalInteraction.Absorb,Vector3.One),
        new(OpticalPortId.Second,new(new(0,.76f,0),Vector3.Up,.43f),OpticalInteraction.Absorb,Vector3.One),
        new(OpticalPortId.Carrier,new(new(0,0,.76f),Vector3.Back,.43f),
            IsOpen ? OpticalInteraction.Route : OpticalInteraction.Absorb,Vector3.One*Retention)
    ];
    public override void ReceiveOpticalPower(IReadOnlyDictionary<OpticalPortId,Vector3> power) =>
        _control.Sample(OpticalColours.Strength(power[OpticalPortId.First],OpticalColour.Broadband),
            OpticalColours.Strength(power[OpticalPortId.Second],OpticalColour.Broadband));
    public override void ReceiveOpticalPath(IReadOnlyList<OpticalSegment> path)
    {
        var exit = Transform*Exit;
        OutputPower = path.Where(s=>s.From.DistanceSquaredTo(exit)<1e-6f)
            .Aggregate(Vector3.Zero,(sum,s)=>sum+s.Power);
        Active = OutputPower.LengthSquared()>1e-8f;
    }
    protected override void Build()
    {
        PickRadius=1.3f;
        AddBox(Vector3.Zero,new(1.4f,1.4f,1.4f),new("#66b8c9"));
        AddBox(new(0,-.85f,0),new(1.8f,.18f,1.8f),new("#293954"));
        foreach(var surface in OpticalSurfaces)
        {
            var t=surface.Aperture;
            var carrier=surface.Id==OpticalPortId.Carrier;
            var ring=PartArt.Ring(Visual,.48f,.055f,new(carrier?"#e8b764":"#fff8e9"),t.At);
            ring.Quaternion=new Quaternion(Vector3.Up,t.Normal);
            var lens=PartArt.Cylinder(Visual,.43f,.018f,new("#556573"),t.At);
            lens.Quaternion=new Quaternion(Vector3.Up,t.Normal);
            if(!carrier)
            {
                _lamps.Add((StandardMaterial3D)lens.MaterialOverride);
                var marks=new Node3D {Position=t.At+t.Normal*.03f,Quaternion=new Quaternion(Vector3.Left,t.Normal)};
                Visual.AddChild(marks);
                var count=surface.Id==OpticalPortId.First?1:2;
                for(var i=0;i<count;i++)
                    PartArt.Box(marks,new(.01f,.1f,.035f),new("#fff8e9"),new(0,.3f,(i-(count-1)*.5f)*.1f));
            }
        }
        var output=PartArt.Cylinder(Visual,.34f,.04f,new("#556573"),Exit);
        output.RotationDegrees=new(0,0,90);
        _lamps.Add((StandardMaterial3D)output.MaterialOverride);
        var rim=PartArt.Ring(Visual,.4f,.045f,new("#e8b764"),Exit);
        rim.RotationDegrees=new(0,0,90);
        // A tiny truth-table relief identifies operation without relying on colour:
        // rows 00,01,10,11; raised gold output means the carrier is permitted.
        for(var row=0;row<4;row++)
        {
            var at=new Vector3(-.42f+row*.28f,-.42f,.72f);
            PartArt.Box(Visual,new(.2f,.32f,.035f),new("#fff8e9"),at);
            for(var bit=0;bit<2;bit++)
                PartArt.Sphere(Visual,.025f,new((row&(bit==0?2:1))!=0?"#293954":"#fff8e9"),
                    at+new Vector3((bit-.5f)*.07f,.08f,.025f));
            if(LogicGate.Evaluate(Operation,(row&2)!=0,(row&1)!=0))
                PartArt.Sphere(Visual,.045f,new("#e8b764"),at+new Vector3(0,-.07f,.045f));
        }
    }
    public override void _Process(double delta)
    {
        for(var i=0;i<_lamps.Count;i++)
        {
            var ink=i==2 ? Active?OpticalColours.BeamInk(OutputPower):new Color("#556573")
                : (i==0?First:Second)?new Color("#f7cb52"):new Color("#556573");
            _lamps[i].AlbedoColor=_lamps[i].AlbedoColor.Lerp(ink,1-Mathf.Exp(-(float)delta*12));
        }
    }
}
