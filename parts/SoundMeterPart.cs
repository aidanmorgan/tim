using Godot;
using System;
using System.Collections.Generic;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions;

public partial class SoundMeterPart : MachinePart
{
    protected override PartParameterValues BindParameters(System.Collections.Generic.IReadOnlyDictionary<string,float> fields) =>
        PartParameterValues.Bind<ReceiverParameter>(fields);
    private RuntimeCheckpoint? _runtimeCheckpoint;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState =>
        [_level,_thresholdState,_runtimeCheckpoint ??= new(this)];
    private sealed class RuntimeCheckpoint(SoundMeterPart owner) : SimulationTransactionParticipant
    {
        private bool _pending;
        private int _triggerCount, _sampledTick;
        protected override void CaptureCheckpoint()
        {
            _pending=owner._pending; _triggerCount=owner.TriggerCount; _sampledTick=owner._sampledTick;
        }
        protected override void RestoreCheckpoint()
        {
            owner._pending=_pending; owner.TriggerCount=_triggerCount; owner._sampledTick=_sampledTick;
        }
    }
    public float Threshold=>ReadParameter(ReceiverParameter.Threshold);
    private readonly SimulationState<float> _level=new(0);
    public float Level=>_level.Value;
    public static readonly ScalarObservationSlot LevelOutput=new(0);
    public override IReadOnlyList<SceneScalarObservation> ScalarObservations=>
        [new(LevelOutput,ScalarUnit.Dimensionless,new(_level))];
    private readonly SimulationState<bool> _thresholdState=new(false);
    public bool AboveThreshold=>_thresholdState.Value;
    public int TriggerCount { get; private set; }
    private bool _pending;
    private int _sampledTick=-1;
    private Node3D _needle=null!;
    private MeshInstance3D _lamp=null!;
    private static readonly AnimationFollowDefinition NeedleResponse=new(0,-2,0,18,AnimationClock.Presentation);
    private static readonly AnimationDefinition LampTransition=new(0,1,.1,AnimationCurve.SmoothStep,AnimationRepeat.Once,AnimationClock.Presentation);
    public override IReadOnlyList<SceneScalarRotationAnimation> ScalarRotationAnimations=>
        [new(_needle,new(this,LevelOutput),ScalarUnit.Dimensionless,0,1,NeedleResponse,AnimationRotationAxis.Z,ScalarAnimationMapping.Linear)];
    public override IReadOnlyList<SceneColourAnimation> ColourAnimations=>
        [new(_lamp,LampTransition,new("#556573"),new("#f7cb52"),SceneAnimationSignal.OwnerActive,SceneAnimationDrive.Endpoint)];
    public override Vector3? AcousticTarget=>Vector3.Zero;
    public override bool CanSendActivation=>true;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.92f,-.5f,0)),
        new(SocketId.Supply,ConnectionDomain.Electrical,PortDirection.Output,new(.92f,-.5f,0)),
        new(SocketId.ActivationOut,ConnectionDomain.Activation,PortDirection.Output,new(.92f,.45f,0))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes=>
        [new(SocketId.PowerIn,SocketId.Supply,ElectricalContactSignal.BooleanState(_thresholdState))];
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        var Threshold=parameters.Read(ReceiverParameter.Threshold);
        if(!float.IsFinite(Threshold)||Threshold<.05f||Threshold>1)
            throw new ArgumentException("Sound meter threshold must be finite and between 0.05 and 1.");
    }
    public override void ReceiveAcousticLevel(float level)
    {
        if(!float.IsFinite(level)||level<0||level>1)throw new ArgumentException("Sound level must be between zero and one.");
        _level.Value=level;
        var above=AboveThreshold?level>Threshold*.9f:level>=Threshold;
        _pending=above&&!AboveThreshold;
        _thresholdState.Value=above;
    }
    public override void PreparePhysics(MachineWorld world,float delta)
    {
        if(_sampledTick==world.Ticks)return;
        _sampledTick=world.Ticks;
        Active=AboveThreshold&&HasElectricalPower(SocketId.PowerIn);
        if(_pending&&HasElectricalPower(SocketId.PowerIn))
        {
            TriggerCount++;
            world.EmitActivation(this);
        }
        _pending=false; // no delayed replay when supply returns during an existing sound
    }
    protected override void Build()
    {
        PickRadius=1.2f;
        AddBox(Vector3.Zero,new(1.6f,1.5f,.65f),new("#66b8c9"));
        AddBox(new(0,-.85f,0),new(1.8f,.18f,.85f),new("#293954"));
        PartArt.Box(Visual,new(1.35f,1.15f,.04f),new("#fff8e9"),new(0,.04f,.35f));
        for(var i=0;i<7;i++)
        {
            var angle=Mathf.Lerp(-1,1,i/6f);
            PartArt.Sphere(Visual,.025f,new("#293954"),new(Mathf.Sin(angle)*.5f,Mathf.Cos(angle)*.5f-.2f,.4f));
        }
        _needle=new Node3D {Position=new(0,-.2f,.42f),Rotation=new(0,0,1)};Visual.AddChild(_needle);
        PartArt.Box(_needle,new(.035f,.44f,.035f),new("#e8b764"),new(0,.22f,0));
        PartArt.Sphere(Visual,.065f,new("#293954"),new(0,-.2f,.46f));
        _lamp=PartArt.Sphere(Visual,.065f,new("#556573"),new(0,-.43f,.41f));
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
    }
}
