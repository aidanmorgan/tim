using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Presentation;
using CuriousContraptions.Bridge;

namespace CuriousContraptions;

/// <summary>A self-timed electrical contact, not a power source. Busy triggers are ignored.</summary>
public partial class HoldTimerPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<HoldTimerParameter>(fields);
    public static readonly TimerSlot ContactWindow=new();
    private SimulationTimerState ReadState=>GetParent() is MachineWorld {HasPhysicsState:true} world
        ?world.ReadTimer(new(this,ContactWindow)):new(default,SimulationTimerPhase.Ready,-1,-1);
    public SimulationTimerPhase State=>ReadState.Phase;
    public int StartedTick=>ReadState.StartedTick;
    public int DueTick=>ReadState.DueTick;
    public float Duration=>ReadParameter(HoldTimerParameter.HoldSeconds);
    public float Remaining=>State==SimulationTimerPhase.Counting&&GetParent() is MachineWorld world
        ?(float)(1-world.TimerProgress(new(this,ContactWindow))):0;
    public override IReadOnlyList<SceneTimerDeclaration> SimulationTimers=>
        [new(new(this,ContactWindow),Duration,TimerCompletionPolicy.Rearm,TimerBoundary.BeforeNetworks,TimerElapsedSignal.None)];
    public static readonly Bridge.ScalarObservationSlot RemainingOutput=new(0);
    public override IReadOnlyList<SceneTimerObservation> TimerObservations=>
        [new(RemainingOutput,new(this,ContactWindow),SimulationTimerQuantity.RemainingFraction)];
    private MeshInstance3D _bar = null!;
    private MeshInstance3D _indicator = null!;
    private static readonly ScalarExtentDefinition BarExtent=new(ScalarExtentAxis.X,-.5,.001,1);
    public override IReadOnlyList<SceneScalarExtent> ScalarExtents=>
        [new(_bar,new(this,RemainingOutput),ScalarUnit.Dimensionless,0,1,BarExtent,ScalarExtentVisibility.OwnerActive)];
    public override IReadOnlyList<SceneColourAnimation> ColourAnimations=>
        [new(_indicator,new(0,1,.1,AnimationCurve.SmoothStep,AnimationRepeat.Once,AnimationClock.Presentation),
            new("#556573"),new("#f7cb52"),SceneAnimationSignal.OwnerActive,SceneAnimationDrive.Endpoint)];
    public override bool CanReceiveActivation => true;
    public override IEnumerable<ConnectionPort> ConnectionPorts =>
    [
        new(SocketId.ActivationIn, ConnectionDomain.Activation, PortDirection.Input, new(0,.65f,.2f)),
        new(SocketId.PowerIn, ConnectionDomain.Electrical, PortDirection.Input, new(-.78f,0,0)),
        new(SocketId.Supply, ConnectionDomain.Electrical, PortDirection.Output, new(.78f,0,0))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes =>
        [new(SocketId.PowerIn, SocketId.Supply, ElectricalContactSignal.TimerCounting(new(this,ContactWindow)))];
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var Duration=parameters.Read(HoldTimerParameter.HoldSeconds);
        if (!float.IsFinite(Duration) || Duration < .1f || Duration > 12)
            throw new ArgumentException("Hold duration must be finite and between 0.1 and 12 seconds.");
    }
    public override ActivationDisposition HandleActivation(MachineWorld world, ActivationCommand command)
    {
        if (command != ActivationCommand.Trigger) throw new ArgumentException("Unsupported activation command.");
        return world.TriggerTimer(new(this,ContactWindow))
            ?ActivationDisposition.Immediate:ActivationDisposition.Deferred;
    }
    public override void BeforeNetworks(MachineWorld world)=>Active=State==SimulationTimerPhase.Counting;
    protected override void Build()
    {
        PickRadius = .95f;
        AddBox(Vector3.Zero, new(1.4f,1.1f,.65f), new("#66b8c9"));
        AddBox(new(0,-.62f,0), new(1.6f,.14f,.85f), new("#293954"));
        PartArt.Box(Visual, new(1.18f,.72f,.045f), new("#fff8e9"), new(0,0,.35f));
        PartArt.Box(Visual, new(1,.14f,.025f), new("#293954"), new(0,.1f,.39f));
        _bar = PartArt.Box(Visual, new(1,.10f,.035f), new("#f7cb52"), new(0,.1f,.415f));
        _bar.Visible = false;
        // Two contact pads distinguish the hold relay from the round delay clock.
        foreach (var x in new[] { -.78f,.78f })
            PartArt.Sphere(Visual,.08f,new("#e8b764"),new(x,0,0));
        PartArt.Sphere(Visual,.08f,new("#e8b764"),new(0,.65f,.2f));
        _indicator = PartArt.Sphere(Visual,.07f,new("#556573"),new(0,-.22f,.40f));
    }
}
