using Godot;
using System;
using CuriousContraptions.Presentation;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Physical impact to omnidirectional sound. No electrical supply or scripted trigger.</summary>
public partial class BellPart : MachinePart
{
    private RuntimeCheckpoint? _runtimeCheckpoint;
    public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState =>
        [_runtimeCheckpoint ??= new(this)];
    private sealed class RuntimeCheckpoint(BellPart owner) : SimulationTransactionParticipant
    {
        private readonly List<AcousticPulse> _pulses = new();
        private int _savedPulseCount;
        private int _savedLastPulseTick;
        protected override void CaptureCheckpoint()
        {
            _pulses.Clear(); _pulses.AddRange(owner._pulses);
            _savedPulseCount=owner.PulseCount;
            _savedLastPulseTick=owner.LastPulseTick;
        }
        protected override void RestoreCheckpoint()
        {
            owner._pulses.Clear(); owner._pulses.AddRange(_pulses);
            owner.PulseCount=_savedPulseCount;
            owner.LastPulseTick=_savedLastPulseTick;
        }
    }
    public const float CollisionRadius=.75f;
    public const float MinimumImpactSpeed=.8f;
    public const int MinimumIntervalTicks=24;
    [Export] public ToneBand Tone { get; set; }=ToneBand.Mid;
    public int PulseCount { get; private set; }
    public int LastPulseTick { get; private set; }=-MinimumIntervalTicks;
    private readonly List<AcousticPulse> _pulses=[];
    public override IReadOnlyList<AcousticPulse> AcousticPulses=>_pulses;
    private readonly List<MeshInstance3D> _rings=[];
    public override SceneAcousticWavefronts? AcousticWavefronts=>new(_rings,AcousticPattern.Omnidirectional,CollisionRadius,0,1,.009f,.35f,SceneWavefrontOpacity.Strength,64);
    private Node3D _bell=null!;
    private AudioStreamPlayer3D _audio=null!;
    private Presentation.SceneAcousticBinding? _playback;
    public override Presentation.SceneAcousticBinding? AcousticPlayback => _playback;
    private static readonly AnimationOscillationDefinition Wobble=new(7,48,5,AnimationClock.Presentation);
    public override IReadOnlyList<SceneAcousticMotion> AcousticMotions=>
        [new(_bell,Wobble,SceneMotionProperty.Rotation,SceneMotionAxis.Z,AnimationDirection.Forward)];

    protected override void ValidateParameters(PartParameterValues parameters)
    {
        if(!Enum.IsDefined(Tone))throw new ArgumentOutOfRangeException(nameof(Tone));
    }
    public override void BeforeNetworks(MachineWorld world)
    {
        _pulses.RemoveAll(p=>world.Ticks>=p.ExpiresTick);
        Active=_pulses.Count>0&&world.Ticks-LastPulseTick<AcousticPulse.Duration/MachineWorld.Tick;
    }
    public override void ObserveContact(SceneContact contact,MachineWorld world)
    {
        var speed=(float)contact.ApproachSpeed;
        if(contact.OtherMass<=0||!float.IsFinite(speed)||speed<0)return;
        // The shared engine reports actual impacts, not proximity/overlap guesses.
        // Resting support is silent; pulse cooldown below bounds real repeated strikes.
        if(speed<MinimumImpactSpeed)return;
        // Impact momentum sets loudness; hard hits saturate rather than extending range.
        var strength=Mathf.Clamp((float)contact.OtherMass*speed/6,.05f,1);
        var previous=0f;
        if(world.Ticks==LastPulseTick)
        {
            previous=_pulses[^1].Strength;
            if(strength<=previous)return;
            // Simultaneous hits coalesce to the strongest, independent of body order.
            _pulses.RemoveAt(_pulses.Count-1);
        }
        else
        {
            if(world.Ticks-LastPulseTick<MinimumIntervalTicks)return;
            PulseCount++;LastPulseTick=world.Ticks;
        }
        var transform=WorldGeometry.CaptureSpatialState(world,new(this,RootBody)).Pose.ToScene();
        _pulses.Add(new(transform.Origin,transform.Basis*Vector3.Up,Tone,world.Ticks,AcousticPattern.Omnidirectional,strength));
        Active=true;
        // Never add velocity: the ordinary collision solver alone supplies rebound.
    }
    protected override void Build()
    {
        PickRadius=.85f;
        Spheres.Add(new(Vector3.Zero,CollisionRadius,MachinePart.RootBody));
        _bell=new Node3D {Name="BellBody"};Visual.AddChild(_bell);
        var shell=new CylinderMesh {TopRadius=.25f,BottomRadius=.59f,Height=.7f,RadialSegments=32};
        PartArt.Mesh(_bell,shell,new("#e8b764"),new(0,.025f,0));
        var crown=PartArt.Sphere(_bell,.26f,new("#e8b764"),new(0,.32f,0));crown.Scale=new(1,.6f,1);
        PartArt.Ring(_bell,.6f,.035f,new("#fff8e9"),new(0,-.325f,0));
        PartArt.Cylinder(_bell,.06f,.24f,new("#293954"),new(0,-.37f,0));
        PartArt.Sphere(_bell,.1f,new("#66b8c9"),new(0,-.49f,0));
        PartArt.Cylinder(Visual,.07f,.22f,new("#293954"),new(0,.55f,0));
        for(var pulse=0;pulse<5;pulse++)
        for(var axis=0;axis<3;axis++)
        {
            var ring=PartArt.Ring(Visual,1,.009f,new("#e8b764"));
            ring.RotationDegrees=axis switch {0=>Vector3.Zero,1=>new(90,0,0),_=>new(0,0,90)};
            ring.Visible=false;ring.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
            ((TorusMesh)ring.Mesh).Rings=64;
            var material=(StandardMaterial3D)ring.MaterialOverride;
            material.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;
            material.ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;
            _rings.Add(ring);
        }
        _audio=new AudioStreamPlayer3D {Stream=AcousticAudio.Create(Tone,AcousticVoice.Bell),VolumeDb=-15,MaxDistance=20,MaxPolyphony=2};
        AddChild(_audio);
        _playback=new(_audio,new Dictionary<ToneBand,AudioStreamWav>{{Tone,(AudioStreamWav)_audio.Stream}});
    }
}
