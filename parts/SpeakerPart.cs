using Godot;
using System;
using CuriousContraptions.Presentation;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Powered trigger-to-sound transducer. A tick-coalesced request expires without supply.</summary>
public partial class SpeakerPart : MachinePart
{
    private RuntimeCheckpoint? _runtimeCheckpoint;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState =>
        [_runtimeCheckpoint ??= new(this)];
    private sealed class RuntimeCheckpoint(SpeakerPart owner) : SimulationTransactionParticipant
    {
        private readonly List<AcousticPulse> _pulses = new();
        private readonly List<int> _requests = new();
        private int _savedPulseCount;
        private int _savedLastPulseTick;
        private int _savedsampledTick;
        protected override void CaptureCheckpoint()
        {
            _pulses.Clear(); _pulses.AddRange(owner._pulses);
            _requests.Clear(); _requests.AddRange(owner._requests);
            _savedPulseCount=owner.PulseCount;
            _savedLastPulseTick=owner.LastPulseTick;
            _savedsampledTick=owner._sampledTick;
        }
        protected override void RestoreCheckpoint()
        {
            owner._pulses.Clear(); owner._pulses.AddRange(_pulses);
            owner._requests.Clear(); foreach(var tick in _requests)owner._requests.Add(tick);
            owner.PulseCount=_savedPulseCount;
            owner.LastPulseTick=_savedLastPulseTick;
            owner._sampledTick=_savedsampledTick;
        }
    }
    [Export] public ToneBand Tone { get; set; }=ToneBand.Mid;
    public const int MinimumIntervalTicks=24;
    public static readonly Vector3 Mouth=new(.72f,0,0);
    private readonly List<AcousticPulse> _pulses=[];
    public override IReadOnlyList<AcousticPulse> AcousticPulses=>_pulses;
    public int PulseCount { get; private set; }
    public int LastPulseTick { get; private set; }=-MinimumIntervalTicks;
    private readonly SortedSet<int> _requests=[];
    private int _sampledTick=-1;
    private Node3D _cone=null!;
    private static readonly AnimationOscillationDefinition Diaphragm=new(18,24*Math.PI,.07*24*Math.PI,AnimationClock.Presentation);
    public override IReadOnlyList<SceneAcousticMotion> AcousticMotions=>
        [new(_cone,Diaphragm,SceneMotionProperty.Translation,SceneMotionAxis.X,AnimationDirection.Reverse)];
    private AudioStreamPlayer3D _audio=null!;
    private Presentation.SceneAcousticBinding? _playback;
    public override Presentation.SceneAcousticBinding? AcousticPlayback => _playback;
    private readonly List<MeshInstance3D> _rings=[];
    public override SceneAcousticWavefronts? AcousticWavefronts=>new(_rings,AcousticPattern.Cone,0,.5f,.7f,.012f,.5f,SceneWavefrontOpacity.Uniform,64);
    public override bool CanReceiveActivation=>true;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.8f,0,0)),
        new(SocketId.ActivationIn,ConnectionDomain.Activation,PortDirection.Input,new(0,.85f,0))
    ];
    protected override void ValidateParameters(PartParameterValues parameters)
    {
        if(!Enum.IsDefined(Tone))throw new ArgumentOutOfRangeException(nameof(Tone));
    }
    public override ActivationDisposition HandleActivation(MachineWorld world,ActivationCommand command)
    {
        if(command!=ActivationCommand.Trigger)throw new ArgumentException("Speaker accepts Trigger only.");
        _requests.Add(world.Ticks);
        return ActivationDisposition.Deferred;
    }
    public override void PreparePhysics(MachineWorld world,float delta)
    {
        if(_sampledTick==world.Ticks)return;
        _sampledTick=world.Ticks;
        _pulses.RemoveAll(p=>world.Ticks>=p.ExpiresTick);
        var requested=false;
        while(_requests.Count>0&&_requests.Min<world.Ticks)
        {
            _requests.Remove(_requests.Min);requested=true;
        }
        if(requested&&HasElectricalPower(SocketId.PowerIn)&&world.Ticks-LastPulseTick>=MinimumIntervalTicks)
        {
            var transform=WorldGeometry.CaptureSpatialState(world,new(this,RootBody)).Pose.ToScene();
            _pulses.Add(new(transform*Mouth,transform.Basis*Vector3.Right,Tone,world.Ticks,AcousticPattern.Cone,1));
            PulseCount++;LastPulseTick=world.Ticks;
        }
        Active=world.Ticks-LastPulseTick<AcousticPulse.Duration/MachineWorld.Tick;
    }
    protected override void Build()
    {
        PickRadius=1.2f;
        AddBox(Vector3.Zero,new(1.25f,1.5f,1.25f),new("#fff8e9"));
        AddBox(new(0,-.85f,0),new(1.6f,.18f,1.55f),new("#293954"));
        _cone=new Node3D {Position=Mouth};Visual.AddChild(_cone);
        var rim=PartArt.Ring(_cone,.58f,.06f,new("#e8b764"),Vector3.Zero);
        rim.RotationDegrees=new(0,0,90);
        var diaphragm=PartArt.Cylinder(_cone,.49f,.08f,new("#293954"),Vector3.Zero);
        diaphragm.RotationDegrees=new(0,0,90);
        PartArt.Sphere(_cone,.17f,new("#66b8c9"),new(.06f,0,0));
        for(var mark=0;mark<=(int)Tone;mark++)
            PartArt.Box(Visual,new(.13f,.06f,.02f),new("#293954"),new(-.2f+mark*.2f,.45f,.635f));
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
        for(var i=0;i<5;i++)
        {
            var ring=PartArt.Ring(Visual,1,.012f,new("#e8b764"),Vector3.Zero);
            ring.RotationDegrees=new(0,0,90);ring.Visible=false;
            ring.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
            ((TorusMesh)ring.Mesh).Rings=64;
            var material=(StandardMaterial3D)ring.MaterialOverride;
            material.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;
            material.ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;
            _rings.Add(ring);
        }
        _audio=new AudioStreamPlayer3D {Stream=AcousticAudio.Create(Tone,AcousticVoice.Speaker),VolumeDb=-15,MaxDistance=20,MaxPolyphony=2};
        AddChild(_audio);
        _playback=new(_audio,new Dictionary<ToneBand,AudioStreamWav>{{Tone,(AudioStreamWav)_audio.Stream}});
    }
}
