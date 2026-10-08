using Godot;
using CuriousContraptions.Presentation;
using CuriousContraptions.Bridge;
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
    private readonly SimulationState<Vector3> _inputPower = new(Vector3.Zero), _outputPower = new(Vector3.Zero);
    public Vector3 InputPower => _inputPower.Value;
    public Vector3 OutputPower => _outputPower.Value;
    public static readonly ScalarObservationSlot RedOutput=new(0),GreenOutput=new(1),BlueOutput=new(2);
    public override IReadOnlyList<SceneScalarObservation> ScalarObservations=>
    [
        new(RedOutput,ScalarUnit.GameOpticalPower,new(_outputPower,ScalarVectorComponent.X)),
        new(GreenOutput,ScalarUnit.GameOpticalPower,new(_outputPower,ScalarVectorComponent.Y)),
        new(BlueOutput,ScalarUnit.GameOpticalPower,new(_outputPower,ScalarVectorComponent.Z))
    ];
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState => [_inputPower, _outputPower];
    private MeshInstance3D _outputLamp=null!;
    private OpticalPathVisual _preview=null!;
    public override Presentation.SceneOpticalPreview? OpticalPreview=>new(_preview,Presentation.OpticalPreviewComposition.MergeCollinear);
    public override void ReceiveOpticalPower(IReadOnlyDictionary<OpticalPortId,Vector3> power)
    {
        _inputPower.Value=power.Values.Aggregate(Vector3.Zero,(a,b)=>a+b);

    }
    public override void ReceiveOpticalOutputPower(Vector3 power)
    {
        _outputPower.Value=power;
        Active=OutputPower.LengthSquared()>1e-8f;
    }
    public override IReadOnlyList<SceneSpectralColour> SpectralColours=>
    [
        new(_outputLamp,new(this,RedOutput),new(this,GreenOutput),new(this,BlueOutput),
            new("#556573"),12,AnimationClock.Presentation)
    ];
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
        _outputLamp=output;
        var rim=PartArt.Ring(Visual,.4f,.045f,new("#e8b764"),Exit);
        rim.RotationDegrees=new(0,0,90);
        _preview=new OpticalPathVisual {Name="OutgoingAimPreview",Preview=true,Visible=false};
        Visual.AddChild(_preview);
    }
}
