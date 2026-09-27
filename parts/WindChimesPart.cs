using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public partial class WindChimesPart : MachinePart
{
    public const int MinimumIntervalTicks=24;
    public int PulseCount { get; private set; }
    public int LastPulseTick { get; private set; }=-MinimumIntervalTicks;
    public ChimePendulum Motion { get; private set; }=null!;
    public Vector3 LastAirForce { get; private set; }
    private readonly List<AcousticPulse> _pulses=[];
    public override IReadOnlyList<AcousticPulse> AcousticPulses=>_pulses;
    public override Vector3? AirflowTarget=>Motion.LocalTip(Basis);
    private Node3D _pendulum=null!;
    private AudioStreamPlayer3D _audio=null!;
    private readonly Dictionary<ToneBand,AudioStreamWav> _tones=[];
    private readonly Dictionary<(string Body,ChimeTubeId Tube),MachinePart> _bodyContacts=[];
    private readonly List<MeshInstance3D> _rings=[];
    private Vector3 _drawDirection=Vector3.Down;
    private Basis _restBasis;
    private ChimeTubeId _lastTube;
    private int _playedTick=-1;
    public override void BeforeStep(MachineWorld world,float delta)
    {
        // Editing rotation must define the initial hanging pose; Reset rebuilds it.
        if(world.Ticks==0&&Motion.Velocity==Vector3.Zero&&_restBasis!=Basis)
        { Motion=new(Basis);_restBasis=Basis; }
        if(LastPulseTick>_playedTick&&LastPulseTick<world.Ticks)
        {
            var pulse=_pulses[^1];
            _audio.Stream=_tones[pulse.Tone];
            _audio.VolumeDb=-15+20*Mathf.Log(pulse.Strength)/Mathf.Log(10);
            _audio.Play();_playedTick=LastPulseTick;
        }
        _pulses.RemoveAll(p=>world.Ticks>=p.ExpiresTick);
        foreach(var entry in _bodyContacts.ToArray())
        {
            var body=entry.Value;var tube=ChimePendulum.Tubes.Single(t=>t.Id==entry.Key.Tube);
            if(!GodotObject.IsInstanceValid(body)||!body.Visible||TubeDistance(ToLocal(body.Position),tube)>body.Radius+.04f)
                _bodyContacts.Remove(entry.Key);
        }
        Active=world.Ticks-LastPulseTick<AcousticPulse.Duration/MachineWorld.Tick;
    }
    public override void AirflowStep(MachineWorld world,Vector3 force,float delta)
    {
        LastAirForce=force;
        foreach(var strike in Motion.Step(Basis,force,world.Gravity,delta))
            Ring(world,strike.Tube,Transform*strike.At,strike.Speed);
    }
    private static float TubeDistance(Vector3 point,ChimeTube tube)
    {
        var half=new Vector3(ChimePendulum.TubeRadius,tube.Length*.5f,ChimePendulum.TubeRadius);
        var closest=(point-tube.Center).Clamp(-half,half)+tube.Center;
        return point.DistanceTo(closest);
    }
    public override void OnContact(MachinePart body,float speed,MachineWorld world)
    {
        if(!body.Dynamic||speed<.8f)return;
        foreach(var tube in ChimePendulum.Tubes)
        {
            if(TubeDistance(ToLocal(body.Position),tube)>body.Radius+.02f)continue;
            if(_bodyContacts.TryAdd((body.Uid,tube.Id),body))
                Ring(world,tube.Id,Transform*tube.Center,speed*body.Mass);
        }
    }
    private void Ring(MachineWorld world,ChimeTubeId id,Vector3 origin,float speed)
    {
        var strength=Mathf.Clamp(speed/1.5f,.08f,1);
        var simultaneous=world.Ticks==LastPulseTick;
        if(simultaneous)
        {
            var previous=_pulses[^1].Strength;
            if(strength<previous||(strength==previous&&id>=_lastTube))return;
            _pulses.RemoveAt(_pulses.Count-1);
        }
        else
        {
            if(world.Ticks-LastPulseTick<MinimumIntervalTicks)return;
            PulseCount++;LastPulseTick=world.Ticks;
        }
        var tone=ChimePendulum.Tubes.Single(t=>t.Id==id).Tone;
        _lastTube=id;
        _pulses.Add(new(origin,Vector3.Up,tone,world.Ticks,AcousticPattern.Omnidirectional,strength));
        Active=true;
        // Audio waits until the next tick so simultaneous contacts play the final selected tone once.
    }
    protected override void Build()
    {
        PickRadius=1.3f;Motion=new(Basis);_restBasis=Basis;
        AddBox(new(0,.93f,0),new(1.2f,.12f,1.2f),new("#fff8e9"),false);
        PartArt.Cylinder(Visual,.6f,.12f,new("#fff8e9"),new(0,.93f,0));
        PartArt.Sphere(Visual,.1f,new("#293954"),new(0,1.06f,0));
        foreach(var tube in ChimePendulum.Tubes)
        {
            PartArt.Line(Visual,new(tube.Center.X,.9f,tube.Center.Z),new(tube.Center.X,.65f,tube.Center.Z),new("#293954"),.008f);
            PartArt.Cylinder(Visual,ChimePendulum.TubeRadius,tube.Length,new("#e8b764"),tube.Center);
            PartArt.Ring(Visual,ChimePendulum.TubeRadius,.008f,new("#293954"),tube.Center+Vector3.Down*(tube.Length*.5f));
            Boxes.Add(new(tube.Center,new(ChimePendulum.TubeRadius,tube.Length*.5f,ChimePendulum.TubeRadius)));
        }
        _pendulum=new Node3D {Name="Pendulum",Position=ChimePendulum.Pivot};Visual.AddChild(_pendulum);
        PartArt.Line(_pendulum,Vector3.Zero,Vector3.Down*ChimePendulum.Length,new("#293954"),.01f);
        PartArt.Sphere(_pendulum,ChimePendulum.ClapperRadius,new("#fff8e9"),Vector3.Down*(ChimePendulum.Length*ChimePendulum.ClapperFraction));
        var sail=PartArt.Box(_pendulum,new(.3f,.4f,.055f),new("#66b8c9"),Vector3.Down*ChimePendulum.Length);
        sail.RotationDegrees=new(0,0,35);
        foreach(var tone in Enum.GetValues<ToneBand>())_tones[tone]=AcousticAudio.Create(tone,AcousticVoice.Bell);
        _audio=new AudioStreamPlayer3D {VolumeDb=-15,MaxDistance=20,MaxPolyphony=2};AddChild(_audio);
        for(var i=0;i<15;i++)
        {
            var ring=PartArt.Ring(Visual,1,.008f,new("#e8b764"));ring.Visible=false;
            ring.RotationDegrees=(i%3) switch {0=>Vector3.Zero,1=>new(90,0,0),_=>new(0,0,90)};
            ring.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;((TorusMesh)ring.Mesh).Rings=64;
            var material=(StandardMaterial3D)ring.MaterialOverride;
            material.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;material.ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;
            _rings.Add(ring);
        }
    }
    public override void _Process(double delta)
    {
        // Build-mode rotation previews the authored local rest pose, not a stale world-space swing.
        var resting=GetParent() is MachineWorld {Running:false,Ticks:0};
        var direction=resting?Vector3.Down:(Basis.Inverse()*Motion.Offset).Normalized();
        _drawDirection=_drawDirection.Lerp(direction,1-Mathf.Exp(-30*(float)delta)).Normalized();
        _pendulum.Quaternion=new Quaternion(Vector3.Down,_drawDirection);
        for(var i=0;i<_rings.Count;i++)
        {
            var ring=_rings[i];ring.Visible=false;
            if(i/3>=_pulses.Count||GetParent() is not MachineWorld world)continue;
            var pulse=_pulses[i/3];var distance=(world.Ticks-pulse.EmissionTick)*MachineWorld.Tick*AcousticPulse.Speed;
            if(distance<.6f||distance>AcousticPulse.Range)continue;
            ring.Visible=true;ring.Position=ToLocal(pulse.Origin);
            var mesh=(TorusMesh)ring.Mesh;mesh.InnerRadius=distance-.008f;mesh.OuterRadius=distance+.008f;
            ((StandardMaterial3D)ring.MaterialOverride).AlbedoColor=new Color(1,.94f,.65f,.3f*pulse.Strength*(1-distance/AcousticPulse.Range));
        }
    }
}
