using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Presentation;

namespace CuriousContraptions;

/// <summary>Counts activation deliveries, emits once at its target and latches an electrical contact.
/// Workshop Reset clears the count; this module never generates electrical supply.</summary>
public partial class CounterPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<CounterParameter>(fields);
    public static readonly CounterSlot Deliveries=new();
    private SimulationCounterState ReadState=>GetParent() is MachineWorld {HasPhysicsState:true} world
        ?world.ReadCounter(new(this,Deliveries)):new(default,0,Target);
    public int Count=>ReadState.Count;
    public int Target=>checked((int)ReadParameter(CounterParameter.TargetCount));
    public SimulationCounterPhase State=>ReadState.Phase;
    public override IReadOnlyList<SceneCounterDeclaration> SimulationCounters=>[new(new(this,Deliveries),Target)];
    private readonly List<SceneColourAnimation> _lights=new();
    private static readonly Color InactiveColour=new("#556573"),ActiveColour=new("#f7cb52");
    private static readonly AnimationDefinition IndicatorTransition=new(0,1,.1,
        AnimationCurve.SmoothStep,AnimationRepeat.Once,AnimationClock.Presentation);
    public override IReadOnlyList<SceneColourAnimation> ColourAnimations=>_lights;
    public override bool CanReceiveActivation=>true;
    public override bool CanSendActivation=>true;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.ActivationIn,ConnectionDomain.Activation,PortDirection.Input,new(0,.72f,.1f)),
        new(SocketId.ActivationOut,ConnectionDomain.Activation,PortDirection.Output,new(0,-.72f,.1f)),
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.93f,0,0)),
        new(SocketId.Supply,ConnectionDomain.Electrical,PortDirection.Output,new(.93f,0,0))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes=>
        [new(SocketId.PowerIn,SocketId.Supply,ElectricalContactSignal.CounterReached(new(this,Deliveries)))];
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var target=parameters.Read(CounterParameter.TargetCount);
        if(!float.IsFinite(target)||target<1||target>9||target!=Mathf.Floor(target))
            throw new ArgumentException("Counter target must be an integer between 1 and 9.");
    }
    public override ActivationDisposition HandleActivation(MachineWorld world, ActivationCommand command)
    {
        if (command != ActivationCommand.Trigger) throw new ArgumentException("Unsupported activation command.");
        return world.IncrementCounter(new(this,Deliveries)) switch
        {
            SimulationCounterResult.Reached=>ActivationDisposition.Immediate,
            SimulationCounterResult.Accumulated or SimulationCounterResult.Saturated=>ActivationDisposition.Deferred,
            _=>throw new InvalidOperationException("Unsupported counter result.")
        };
    }
    protected override void Build()
    {
        PickRadius=1.1f;
        AddBox(Vector3.Zero,new(1.65f,1.25f,.65f),new("#e8b764"));
        AddBox(new(0,-.72f,0),new(1.8f,.16f,.85f),new("#293954"));
        PartArt.Box(Visual,new(1.4f,1.02f,.04f),new("#fff8e9"),new(0,0,.35f));
        var rows=(Target+2)/3;
        for(var i=0;i<Target;i++)
        {
            var row=i/3;
            var columns=Math.Min(3,Target-row*3);
            var at=new Vector3((i%3-(columns-1)*.5f)*.36f,((rows-1)*.5f-row)*.3f,.4f);
            var lamp=PartArt.Sphere(Visual,.10f,InactiveColour,at);
            _lights.Add(new(lamp,IndicatorTransition,InactiveColour,ActiveColour,
                SceneAnimationSignal.CounterAtLeast(new(this,Deliveries),i+1),SceneAnimationDrive.Endpoint));
        }
        foreach(var port in ConnectionPorts)
            PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
    }
}
