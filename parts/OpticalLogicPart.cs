using Godot;
using CuriousContraptions.Presentation;
using CuriousContraptions.Bridge;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

/// <summary>Two absorbing controls switch an independent, lossy optical carrier.</summary>
public partial class OpticalLogicPart : MachinePart
{
    [Export] public LogicGateKind Operation { get; set; }
    public const float Retention = .9f;
    public static readonly Vector3 Exit = new(.76f,0,0);
    private OpticalLogicControl Control=>ReadParameterState<OpticalLogicControl>();
    public bool First => Control.First;
    public bool Second => Control.Second;
    public bool IsOpen => Control.IsOpen;
    private readonly SimulationState<Vector3> _outputPower = new(Vector3.Zero);
    public Vector3 OutputPower => _outputPower.Value;
    public static readonly ScalarObservationSlot RedOutput=new(0),GreenOutput=new(1),BlueOutput=new(2);
    public override IReadOnlyList<SceneScalarObservation> ScalarObservations=>
    [
        new(RedOutput,ScalarUnit.GameOpticalPower,new(_outputPower,ScalarVectorComponent.X)),
        new(GreenOutput,ScalarUnit.GameOpticalPower,new(_outputPower,ScalarVectorComponent.Y)),
        new(BlueOutput,ScalarUnit.GameOpticalPower,new(_outputPower,ScalarVectorComponent.Z))
    ];
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState => [Control, _outputPower];
    public static readonly BooleanObservationSlot FirstOutput=new(0),SecondOutput=new(1),OpenOutput=new(2);
    public override IReadOnlyList<SceneBooleanObservation> BooleanObservations=>
    [
        new(FirstOutput,new(Control,OpticalControlQuantity.First)),
        new(SecondOutput,new(Control,OpticalControlQuantity.Second)),
        new(OpenOutput,new(Control,OpticalControlQuantity.IsOpen))
    ];
    private readonly Dictionary<OpticalPortId,MeshInstance3D> _controlLamps=[];
    private MeshInstance3D _outputLamp=null!;
    private static readonly AnimationFollowDefinition ControlResponse=new(0,1,0,12,AnimationClock.Presentation);
    public override IReadOnlyList<SceneColourFollow> FollowingColours=>
    [
        new(_controlLamps[OpticalPortId.First],ControlResponse,new("#556573"),new("#f7cb52"),SceneColourFollowSignal.Boolean(new(this,FirstOutput))),
        new(_controlLamps[OpticalPortId.Second],ControlResponse,new("#556573"),new("#f7cb52"),SceneColourFollowSignal.Boolean(new(this,SecondOutput)))
    ];
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        if(!System.Enum.IsDefined(Operation))throw new System.ArgumentOutOfRangeException(nameof(Operation));
    }
    protected override PartParameterState PrepareParameterState(PartParameterValues parameters) =>
        PartParameterState.Create(new OpticalLogicControl(Operation));
    public override void BeforeNetworks(MachineWorld world) => Control.Advance();
    public override OpticalOutlet? OpticalOutput => new(Exit,Vector3.Right);
    public override IReadOnlyList<OpticalSurface> OpticalSurfaces =>
    [
        new(OpticalPortId.First,new(new(-.76f,0,0),Vector3.Left,.43f),OpticalInteraction.Absorb,Vector3.One),
        new(OpticalPortId.Second,new(new(0,.76f,0),Vector3.Up,.43f),OpticalInteraction.Absorb,Vector3.One),
        new(OpticalPortId.Carrier,new(new(0,0,.76f),Vector3.Back,.43f),
            IsOpen ? OpticalInteraction.Route : OpticalInteraction.Absorb,Vector3.One*Retention)
    ];
    public override void ReceiveOpticalPower(IReadOnlyDictionary<OpticalPortId,Vector3> power) =>
        Control.Sample(OpticalColours.Strength(power[OpticalPortId.First],OpticalColour.Broadband),
            OpticalColours.Strength(power[OpticalPortId.Second],OpticalColour.Broadband));
    public override void ReceiveOpticalOutputPower(Vector3 power)
    {
        _outputPower.Value = power;
        Active = OutputPower.LengthSquared()>1e-8f;
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
            var carrier=surface.Id==OpticalPortId.Carrier;
            var ring=PartArt.Ring(Visual,.48f,.055f,new(carrier?"#e8b764":"#fff8e9"),t.At);
            ring.Quaternion=new Quaternion(Vector3.Up,t.Normal);
            var lens=PartArt.Cylinder(Visual,.43f,.018f,new("#556573"),t.At);
            lens.Quaternion=new Quaternion(Vector3.Up,t.Normal);
            if(!carrier)
            {
                _controlLamps.Add(surface.Id,lens);
                var marks=new Node3D {Position=t.At+t.Normal*.03f,Quaternion=new Quaternion(Vector3.Left,t.Normal)};
                Visual.AddChild(marks);
                var count=surface.Id==OpticalPortId.First?1:2;
                for(var i=0;i<count;i++)
                    PartArt.Box(marks,new(.01f,.1f,.035f),new("#fff8e9"),new(0,.3f,(i-(count-1)*.5f)*.1f));
            }
        }
        var output=PartArt.Cylinder(Visual,.34f,.04f,new("#556573"),Exit);
        output.RotationDegrees=new(0,0,90);
        _outputLamp=output;
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
}
