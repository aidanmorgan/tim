using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Powered trigger-to-sound transducer. A tick-coalesced request expires without supply.</summary>
public partial class SpeakerPart : MachinePart
{
    [Export] public ToneBand Tone { get; set; }=ToneBand.Mid;
    public const int MinimumIntervalTicks=24;
    public static readonly Vector3 Mouth=new(.72f,0,0);
    private readonly List<AcousticPulse> _pulses=[];
    public override IReadOnlyList<AcousticPulse> AcousticPulses=>_pulses;
    public int PulseCount { get; private set; }
    public int LastPulseTick { get; private set; }=-MinimumIntervalTicks;
    private readonly SortedSet<int> _requests=[];
    private int _sampledTick=-1;
    private float _visualKick;
    private Node3D _cone=null!;
    private AudioStreamPlayer3D _audio=null!;
    private readonly List<MeshInstance3D> _rings=[];
    public override bool CanReceiveActivation=>true;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.8f,0,0)),
        new(SocketId.ActivationIn,ConnectionDomain.Activation,PortDirection.Input,new(0,.85f,0))
    ];
    public override void ValidateParameters()
    {
        if(!Enum.IsDefined(Tone))throw new ArgumentOutOfRangeException(nameof(Tone));
    }
    public override ActivationDisposition HandleActivation(MachineWorld world,ActivationCommand command)
    {
        if(command!=ActivationCommand.Trigger)throw new ArgumentException("Speaker accepts Trigger only.");
        _requests.Add(world.Ticks);
        return ActivationDisposition.Deferred;
    }
    public override void BeforeStep(MachineWorld world,float delta)
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
            _pulses.Add(new(Transform*Mouth,Transform.Basis*Vector3.Right,Tone,world.Ticks,AcousticPattern.Cone,1));
            PulseCount++;LastPulseTick=world.Ticks;
            _visualKick=1;
            _audio.Play(); // presentation only; no playback state is read by simulation
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
    }
    public override void _Process(double delta)
    {
        _visualKick=Mathf.MoveToward(_visualKick,0,(float)delta*6);
        _cone.Position=Mouth+Vector3.Right*(Mathf.Sin(_visualKick*Mathf.Tau*2)*_visualKick*.07f);
        for(var i=0;i<_rings.Count;i++)
        {
            var ring=_rings[i];ring.Visible=false;
            if(i>=_pulses.Count||GetParent() is not MachineWorld world)continue;
            var distance=(world.Ticks-_pulses[i].EmissionTick)*MachineWorld.Tick*AcousticPulse.Speed;
            if(distance<0||distance>AcousticPulse.Range)continue;
            ring.Visible=true;
            ring.Position=Mouth+Vector3.Right*distance;
            var radius=.5f+distance*.7f;
            var mesh=(TorusMesh)ring.Mesh;
            mesh.InnerRadius=radius-.012f;mesh.OuterRadius=radius+.012f;
            ((StandardMaterial3D)ring.MaterialOverride).AlbedoColor=new Color(1,.94f,.65f,.5f*(1-distance/AcousticPulse.Range));
        }
    }
}
