using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Presentation;

namespace CuriousContraptions;

public enum LogicInputState { Neither, FirstOnly, SecondOnly, Both }

/// <summary>A continuous two-input electrical interlock, not a source or memory module.</summary>
public partial class ElectricalLogicPart : MachinePart
{
    [Export] public LogicGateKind Operation { get; set; }
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        if(!Enum.IsDefined(Operation))throw new ArgumentOutOfRangeException(nameof(Operation));
    }
    public bool Truth => LogicGate.Evaluate(Operation,HasElectricalPower(SocketId.FirstIn),HasElectricalPower(SocketId.SecondIn));
    public LogicInputState State=>HasElectricalPower(SocketId.FirstIn)
        ?HasElectricalPower(SocketId.SecondIn)?LogicInputState.Both:LogicInputState.FirstOnly
        :HasElectricalPower(SocketId.SecondIn)?LogicInputState.SecondOnly:LogicInputState.Neither;
    private enum Indicator { FirstInput, SecondInput, Output }
    private readonly List<SceneColourAnimation> _indicators=new();
    private static readonly Color InactiveColour=new("#556573"),ActiveColour=new("#f7cb52");
    private static readonly AnimationDefinition IndicatorTransition=new(0,1,.1,
        AnimationCurve.SmoothStep,AnimationRepeat.Once,AnimationClock.Presentation);
    public override IReadOnlyList<SceneColourAnimation> ColourAnimations=>_indicators;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.FirstIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.94f,.35f,0)),
        new(SocketId.SecondIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.94f,-.35f,0)),
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(0,-.75f,.55f)),
        new(SocketId.Supply,ConnectionDomain.Electrical,PortDirection.Output,new(.94f,0,0))
    ];
    public override IEnumerable<ElectricalGate> ElectricalGates=>
        [new(Operation,SocketId.FirstIn,SocketId.SecondIn,SocketId.PowerIn,SocketId.Supply)];
    public override void PreparePhysics(MachineWorld world,float delta)
    {
        Active=Truth && HasElectricalPower(SocketId.PowerIn);
        if(Active)world.Events.TryAdd(new(MachineEventKind.Powered,Uid),world.Ticks);
    }
    protected override void Build()
    {
        PickRadius=1.1f;
        AddBox(Vector3.Zero,new(1.7f,1.3f,.65f),new("#66b8c9"));
        AddBox(new(0,-.75f,0),new(1.85f,.16f,.85f),new("#293954"));
        PartArt.Box(Visual,new(1.45f,1.05f,.04f),new("#fff8e9"),new(0,0,.35f));
        // Two paths converge onto one output. One/two raised ticks identify the input rows.
        if(Operation==LogicGateKind.And)
        foreach(var y in new[]{.3f,-.3f})
        {
            var line=PartArt.Box(Visual,new(.65f,.035f,.025f),new("#293954"),new(-.05f,y*.5f,.395f));
            line.RotationDegrees=new(0,0,y>0?-27:27);
        }
        if(Operation!=LogicGateKind.And)
        {
            Visual.AddChild(new MeshInstance3D
            {
                Position=new(0,0,.41f),Mesh=new QuadMesh {Size=new(.65f,.65f)},
                MaterialOverride=new StandardMaterial3D
                {
                    AlbedoTexture=WorkshopIcons.Pictogram(Definition.Id),
                    Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
                    ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded
                }
            });
            // Distinct raised truth-row markers, not a colour-only operation label.
            for(var row=0;row<4;row++)
                if(LogicGate.Evaluate(Operation,(row&2)!=0,(row&1)!=0))
                    PartArt.Sphere(Visual,.035f,new("#e8b764"),new(-.21f+row*.14f,-.4f,.42f));
        }
        foreach(var indicator in Enum.GetValues<Indicator>())
        {
            var at=indicator switch
            {
                Indicator.FirstInput=>new Vector3(-.48f,.3f,.42f),
                Indicator.SecondInput=>new Vector3(-.48f,-.3f,.42f),
                Indicator.Output=>new Vector3(.48f,0,.42f),
                _=>throw new InvalidOperationException("Unexpected logic indicator.")
            };
            var lamp=PartArt.Sphere(Visual,.09f,InactiveColour,at);
            var signal=indicator switch
            {
                Indicator.FirstInput=>SceneAnimationSignal.InputAvailable(SocketId.FirstIn),
                Indicator.SecondInput=>SceneAnimationSignal.InputAvailable(SocketId.SecondIn),
                Indicator.Output=>SceneAnimationSignal.OwnerActive,
                _=>throw new InvalidOperationException("Unexpected logic indicator.")
            };
            _indicators.Add(new(lamp,IndicatorTransition,InactiveColour,ActiveColour,signal,SceneAnimationDrive.Endpoint));
        }
        foreach(var x in new[]{-.58f,-.42f})
            PartArt.Box(Visual,new(.025f,.08f,.025f),new("#293954"),new(x,-.47f,.4f));
        PartArt.Box(Visual,new(.025f,.08f,.025f),new("#293954"),new(-.48f,.47f,.4f));
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
    }
}
