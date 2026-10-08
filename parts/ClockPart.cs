using Godot;
using CuriousContraptions.Presentation;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Powered periodic trigger. No startup edge, no catch-up events: loss of power resets phase.</summary>
public partial class ClockPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<ClockParameter>(fields);
    public static readonly OscillatorSlot PulseSchedule=new();
    private SimulationOscillatorState ReadState=>GetParent() is MachineWorld {HasPhysicsState:true} world
        ?world.ReadOscillator(new(this,PulseSchedule)):new(default,SimulationOscillatorPhase.Stopped,-1,0,-1);
    public SimulationOscillatorPhase State=>ReadState.Phase;
    public int DueTick=>ReadState.DueTick;
    public int PulseCount=>ReadState.PulseCount;
    public int LastPulseTick=>ReadState.LastPulseTick;
    public float Interval=>ReadParameter(ClockParameter.IntervalSeconds);
    public int IntervalTicks=>checked((int)Math.Ceiling(Interval/MachineWorld.Tick));
    public float Progress=>GetParent() is MachineWorld {HasPhysicsState:true} world
        ?(float)world.OscillatorProgress(new(this,PulseSchedule)):0;
    public override IReadOnlyList<SceneOscillatorDeclaration> SimulationOscillators=>
        [new(new(this,PulseSchedule),Interval,SocketId.PowerIn)];
    public static readonly Bridge.ScalarObservationSlot ProgressOutput=new(0);
    public override IReadOnlyList<SceneOscillatorObservation> OscillatorObservations=>
        [new(ProgressOutput,new(this,PulseSchedule),SimulationOscillatorQuantity.ProgressFraction)];
    private static readonly AnimationFollowDefinition PendulumResponse=new(-.42,.42,0,30,AnimationClock.Presentation);
    public override IReadOnlyList<SceneScalarRotationAnimation> ScalarRotationAnimations=>
        [new(_pendulum,new(this,ProgressOutput),Bridge.ScalarUnit.Dimensionless,0,1,
            PendulumResponse,AnimationRotationAxis.Z,ScalarAnimationMapping.SineCycle)];
    private Node3D _pendulum=null!;
    private MeshInstance3D _indicator=null!;
    private static readonly AnimationImpulseDefinition PulseFeedback=new(.25,AnimationImpulseCurve.LinearDecay,
        AnimationImpulseOverlap.Maximum,AnimationImpulseVisibility.DeferUntilVisible,AnimationClock.Presentation,64,
        AnimationImpulseTiming.EventTimeRetainFirstFrame,0);
    public override IReadOnlyList<SceneOscillatorColour> OscillatorColourAnimations=>
        [new(_indicator,new(this,PulseSchedule),PulseFeedback,new("#556573"),new("#f7cb52"))];
    public override bool CanSendActivation=>true;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.78f,0,0)),
        new(SocketId.ActivationOut,ConnectionDomain.Activation,PortDirection.Output,new(.78f,0,0))
    ];
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var Interval=parameters.Read(ClockParameter.IntervalSeconds);
        if(!float.IsFinite(Interval)||Interval<.1f||Interval>12)
            throw new ArgumentException("Clock interval must be finite and between 0.1 and 12 seconds.");
    }
    public override void PreparePhysics(MachineWorld world,float delta)
    {
        Active=State==SimulationOscillatorPhase.Running&&LastPulseTick==world.Ticks;
    }
    protected override void Build()
    {
        PickRadius=1.1f;
        AddBox(Vector3.Zero,new(1.4f,1.7f,.6f),new("#e8b764"));
        AddBox(new(0,-.95f,0),new(1.6f,.2f,.85f),new("#293954"));
        PartArt.Box(Visual,new(1.14f,1.44f,.04f),new("#fff8e9"),new(0,0,.32f));
        PartArt.Box(Visual,new(.84f,1.1f,.02f),new("#556573"),new(0,-.06f,.35f));
        _pendulum=new Node3D {Position=new(0,.4f,.39f)};
        Visual.AddChild(_pendulum);
        PartArt.Box(_pendulum,new(.045f,.7f,.035f),new("#fff8e9"),new(0,-.35f,0));
        PartArt.Sphere(_pendulum,.15f,new("#f7cb52"),new(0,-.7f,0));
        PartArt.Sphere(Visual,.055f,new("#293954"),new(0,.4f,.44f));
        _indicator=PartArt.Sphere(Visual,.065f,new("#556573"),new(0,.63f,.39f));
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
    }
}
