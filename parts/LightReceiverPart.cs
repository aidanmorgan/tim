using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Presentation;

namespace CuriousContraptions;

/// <summary>Front-facing laser target closes a separately supplied electrical contact.</summary>
public partial class LightReceiverPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<ReceiverParameter>(fields);
    [Export] public OpticalColour Colour { get; set; }=OpticalColour.Broadband;
    public float Threshold=>ReadParameter(ReceiverParameter.Threshold);
    private readonly SimulationState<Vector3> _receivedPower = new(Vector3.Zero);
    private readonly SimulationState<bool> _matched = new(false);
    public Vector3 ReceivedPower => _receivedPower.Value;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState => [_receivedPower,_matched];
    public float Power=>OpticalColours.Strength(ReceivedPower,Colour);
    public bool Matches=>_matched.Value;
    public override IReadOnlyList<OpticalSurface> OpticalSurfaces=>[new(OpticalPortId.Main,new(new(-.18f,0,0),Vector3.Left,.55f),OpticalInteraction.Absorb,Vector3.One)];
    private MeshInstance3D _target=null!;
    private static readonly Color InactiveColour=new("#556573"), ActiveColour=new("#f7cb52");
    private static readonly AnimationDefinition IndicatorTransition=new(0,1,.125,
        AnimationCurve.SmoothStep,AnimationRepeat.Once,AnimationClock.Presentation);
    public override IReadOnlyList<SceneColourAnimation> ColourAnimations =>
        [new(_target,IndicatorTransition,InactiveColour,ActiveColour,SceneAnimationSignal.OwnerActive,SceneAnimationDrive.Endpoint)];
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(0,-.6f,.65f)),
        new(SocketId.Supply,ConnectionDomain.Electrical,PortDirection.Output,new(0,-.6f,-.65f))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes=>
        [new(SocketId.PowerIn,SocketId.Supply,ElectricalContactSignal.BooleanState(_matched))];
    public override void ReceiveOpticalPower(IReadOnlyDictionary<OpticalPortId,Vector3> power){
        var received=power[OpticalPortId.Main];
        var matched=OpticalColours.Accepts(received,Colour,Threshold);
        _receivedPower.Value=received;_matched.Value=matched;Active=matched;
    }
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var Threshold=parameters.Read(ReceiverParameter.Threshold);
        if(!Enum.IsDefined(Colour))throw new ArgumentException("Unsupported receiver colour.");
        if(!float.IsFinite(Threshold)||Threshold<.05f||Threshold>2)
            throw new ArgumentException("Receiver threshold must be finite and between 0.05 and 2.");
    }
    protected override void Build()
    {
        PickRadius=1.1f;
        AddBox(Vector3.Zero,new(.26f,1.4f,1.4f),new("#fff8e9"));
        AddBox(new(0,-.88f,0),new(.85f,.18f,1.6f),new("#293954"));
        var disc=PartArt.Cylinder(Visual,.55f,.035f,InactiveColour,new(-.18f,0,0));
        disc.RotationDegrees=new(0,0,90);_target=disc;
        var ring=PartArt.Ring(Visual,.33f,.025f,new("#fff8e9"),new(-.21f,0,0));
        ring.RotationDegrees=new(0,0,90);
        PartArt.Sphere(Visual,.08f,new("#f7cb52"),new(-.23f,0,0));
        OpticalColours.Marks(Visual,Colour,new(-.15f,.6f,0));
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
    }
}
