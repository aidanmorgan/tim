using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Physical impact to omnidirectional sound. No electrical supply or scripted trigger.</summary>
public partial class BellPart : MachinePart
{
    public const float CollisionRadius=.75f;
    public const float MinimumImpactSpeed=.8f;
    public const int MinimumIntervalTicks=24;
    [Export] public ToneBand Tone { get; set; }=ToneBand.Mid;
    public int PulseCount { get; private set; }
    public int LastPulseTick { get; private set; }=-MinimumIntervalTicks;
    private readonly HashSet<string> _touching=[];
    private readonly List<AcousticPulse> _pulses=[];
    public override IReadOnlyList<AcousticPulse> AcousticPulses=>_pulses;
    private readonly List<MeshInstance3D> _rings=[];
    private Node3D _bell=null!;
    private AudioStreamPlayer3D _audio=null!;
    private float _angle;
    private float _angularVelocity;

    public override void ValidateParameters()
    {
        if(!Enum.IsDefined(Tone))throw new ArgumentOutOfRangeException(nameof(Tone));
    }
    public override void BeforeStep(MachineWorld world,float delta)
    {
        // Check each physics substep, so recontact requires actual geometric separation.
        _touching.RemoveWhere(id=>
        {
            var body=world.FindPart(id);
            return body==null||!body.Visible||body.Position.DistanceTo(Position)>CollisionRadius+body.Radius+.04f;
        });
        _pulses.RemoveAll(p=>world.Ticks>=p.ExpiresTick);
        Active=world.Ticks-LastPulseTick<AcousticPulse.Duration/MachineWorld.Tick;
    }
    public override void OnContact(MachinePart body,float speed,MachineWorld world)
    {
        if(!body.Dynamic||!body.Visible||!float.IsFinite(speed)||speed<0)return;
        if(!_touching.Add(body.Uid)||speed<MinimumImpactSpeed)return;
        // Impact momentum sets loudness; hard hits saturate rather than extending range.
        var strength=Mathf.Clamp(body.Mass*speed/6,.05f,1);
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
        _pulses.Add(new(Position,Transform.Basis*Vector3.Up,Tone,world.Ticks,AcousticPattern.Omnidirectional,strength));
        Active=true;
        _angularVelocity+=5*(strength-previous); // continuous pose across repeated strikes
        _audio.VolumeDb=-15+20*Mathf.Log(strength)/Mathf.Log(10);
        if(previous==0)_audio.Play();
        // Never add velocity: the ordinary collision solver alone supplies rebound.
    }
    protected override void Build()
    {
        PickRadius=.85f;
        Spheres.Add(new(Vector3.Zero,CollisionRadius));
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
    }
    public override void _Process(double delta)
    {
        // Exact damped oscillator step: smooth and independent of render frame rate.
        var time=(float)delta;var decay=Mathf.Exp(-7*time);
        var cosine=Mathf.Cos(48*time);var sine=Mathf.Sin(48*time);
        var a=_angle;var b=(_angularVelocity+7*a)/48;
        _angle=decay*(a*cosine+b*sine);
        _angularVelocity=decay*((48*b-7*a)*cosine+(-48*a-7*b)*sine);
        _bell.Rotation=new(0,0,_angle);
        for(var i=0;i<_rings.Count;i++)
        {
            var ring=_rings[i];ring.Visible=false;
            if(i/3>=_pulses.Count||GetParent() is not MachineWorld world)continue;
            var pulse=_pulses[i/3];
            var distance=(world.Ticks-pulse.EmissionTick)*MachineWorld.Tick*AcousticPulse.Speed;
            if(distance<CollisionRadius||distance>AcousticPulse.Range)continue;
            ring.Visible=true;
            var mesh=(TorusMesh)ring.Mesh;mesh.InnerRadius=distance-.009f;mesh.OuterRadius=distance+.009f;
            ((StandardMaterial3D)ring.MaterialOverride).AlbedoColor=new Color(1,.94f,.65f,.35f*pulse.Strength*(1-distance/AcousticPulse.Range));
        }
    }
}
