using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public static class WindmillParameters
{
    public const string RadiansPerForce="radians_per_force";
}

/// <summary>Air-to-shaft transducer for the existing ideal speed network, not a torque/load model.</summary>
public partial class WindmillPart : MachinePart
{
    public const float MaximumSpeed=12;
    public const float Acceleration=18;
    public const float CutInForce=.05f;
    private static readonly Vector3 RotorAt=new(-.12f,0,0);
    private static readonly AirflowSample[] RotorSamples=
    [
        new(new(-.12f,.42f,0),.25f),new(new(-.12f,-.42f,0),.25f),
        new(new(-.12f,0,.42f),.25f),new(new(-.12f,0,-.42f),.25f)
    ];
    public override IReadOnlyList<AirflowSample> AirflowSamples=>RotorSamples;
    public float AxialForce { get; private set; }
    public float ShaftSpeed { get; private set; }
    public float ShaftAngle { get; private set; }
    public float ShaftTravel { get; private set; }
    private Node3D _rotor=null!;
    private Node3D _pulley=null!;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketIds.Drive,ConnectionDomain.Mechanical,PortDirection.Output,new(.25f,-.1f,.52f))
    ];
    public override IEnumerable<MechanicalSource> MechanicalSources=>[new(SocketIds.Drive,ShaftSpeed)];
    public override void ValidateParameters()
    {
        var gain=Properties[WindmillParameters.RadiansPerForce];
        if(!float.IsFinite(gain)||gain<.1f||gain>4)throw new ArgumentException("Windmill response must be between 0.1 and 4 radians per unit force.");
    }
    public override void AirflowStep(MachineWorld world,Vector3 force,float delta)
    {
        AxialForce=force.Dot(Basis.X.Normalized());
        var target=Mathf.Abs(AxialForce)<CutInForce?0:Mathf.Clamp(AxialForce*Properties[WindmillParameters.RadiansPerForce],-MaximumSpeed,MaximumSpeed);
        ShaftSpeed=Mathf.MoveToward(ShaftSpeed,target,Acceleration*delta);
        ShaftTravel+=Mathf.Abs(ShaftSpeed)*delta;
        ShaftAngle=Mathf.PosMod(ShaftAngle+ShaftSpeed*delta,Mathf.Tau);
        Active=ShaftSpeed!=0;
        if(ShaftTravel>=Mathf.Tau)world.Events.TryAdd(new(MachineEventKind.Turned,Uid),world.Ticks);
        _rotor.Rotation=new(ShaftAngle,0,0);
        _pulley.Rotation=new(0,0,-ShaftAngle);
    }
    protected override void Build()
    {
        ClearMechanicalDrive();PickRadius=1.2f;
        AddBox(new(.25f,-1.08f,0),new(1.1f,.16f,.9f),new("#293954"));
        AddBox(new(.25f,-.55f,0),new(.22f,1.1f,.25f),new("#fff8e9"));
        AddBox(new(.25f,-.1f,0),new(.65f,.45f,.55f),new("#fff8e9"));
        var axle=PartArt.Cylinder(Visual,.11f,.55f,new("#e8b764"),new(.14f,0,0));
        axle.RotationDegrees=new(0,0,90);
        var guard=PartArt.Ring(Visual,.78f,.035f,new("#fff8e9"),RotorAt);
        guard.RotationDegrees=new(0,0,90);
        Tubes.Add(new(new Transform3D(Basis.Identity,RotorAt),.035f,.745f,.815f,true));
        Spheres.Add(new(RotorAt,.16f));
        _rotor=new Node3D {Name="WindRotor",Position=RotorAt};Visual.AddChild(_rotor);
        for(var i=0;i<4;i++)
        {
            var arm=new Node3D {Rotation=new(i*Mathf.Pi*.5f,0,0)};_rotor.AddChild(arm);
            var blade=PartArt.Box(arm,new(.075f,.52f,.21f),new("#66b8c9"),new(0,.4f,0));
            blade.RotationDegrees=new(0,30,0);
            PartArt.Box(arm,new(.08f,.07f,.21f),new("#e8b764"),new(0,.63f,0));
        }
        PartArt.Sphere(_rotor,.14f,new("#e8b764"),Vector3.Zero);
        _pulley=new Node3D {Name="OutputPulley",Position=new(.25f,-.1f,.44f)};Visual.AddChild(_pulley);
        var wheel=PartArt.Cylinder(_pulley,.24f,.12f,new("#fff8e9"));
        wheel.RotationDegrees=new(90,0,0);
        PartArt.Box(_pulley,new(.38f,.05f,.035f),new("#e8b764"),new(0,0,.08f));
    }
}
