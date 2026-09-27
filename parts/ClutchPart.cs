using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum ClutchPhase { Open, Closing, Engaged, Opening }
public static class ClutchParameters
{
    public const string CloseSeconds="close_seconds";
}

/// <summary>Electrically engaged, normally open coupling in the ideal speed network.</summary>
public partial class ClutchPart : MachinePart
{
    public ClutchPhase Phase { get; private set; }
    public float Closure { get; private set; }
    public float InputSpeed { get; private set; }
    public float OutputSpeed { get; private set; }
    public float InputAngle { get; private set; }
    public float OutputAngle { get; private set; }
    private Node3D _input=null!,_output=null!,_leftPlate=null!,_rightPlate=null!;
    private StandardMaterial3D _indicator=null!;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.DriveIn,ConnectionDomain.Mechanical,PortDirection.Input,new(-.65f,0,.55f)),
        new(SocketId.Drive,ConnectionDomain.Mechanical,PortDirection.Output,new(.65f,0,.55f)),
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(0,-.35f,.5f))
    ];
    // Disabled routes remain structural edges: an open clutch cannot hide a loop or competing drive.
    public override IEnumerable<MechanicalRoute> MechanicalRoutes=>[new(SocketId.DriveIn,SocketId.Drive,1,Phase==ClutchPhase.Engaged)];
    public override void ValidateParameters()
    {
        var seconds=Properties[ClutchParameters.CloseSeconds];
        if(!float.IsFinite(seconds)||seconds<.05f||seconds>2)throw new ArgumentException("Clutch closing time must be between 0.05 and 2 seconds.");
    }
    public override void BeforeStep(MachineWorld world,float delta)
    {
        var supplied=HasElectricalPower(SocketId.PowerIn);
        Closure=Mathf.MoveToward(Closure,supplied?1:0,delta/Properties[ClutchParameters.CloseSeconds]);
        Phase=supplied?(Closure==1?ClutchPhase.Engaged:ClutchPhase.Closing):(Closure==0?ClutchPhase.Open:ClutchPhase.Opening);
        Active=Phase==ClutchPhase.Engaged;
        _leftPlate.Position=new(-.09f-.16f*(1-Closure),0,0);
        _rightPlate.Position=new(.09f+.16f*(1-Closure),0,0);
        _indicator.AlbedoColor=Active?new("#f7cb52"):supplied?new("#66b8c9"):new("#556573");
        if(supplied)world.Events.TryAdd(new(MachineEventKind.Powered,Uid),world.Ticks);
    }
    public override void MechanicalStep(MachineWorld world,float delta)
    {
        InputSpeed=MechanicalSpeed(SocketId.DriveIn);OutputSpeed=MechanicalSpeed(SocketId.Drive);
        InputAngle=Mathf.PosMod(InputAngle+InputSpeed*delta,Mathf.Tau);
        OutputAngle=Mathf.PosMod(OutputAngle+OutputSpeed*delta,Mathf.Tau);
        _input.Rotation=new(0,0,-InputAngle);_output.Rotation=new(0,0,-OutputAngle);
        _leftPlate.Rotation=new(InputAngle,0,0);_rightPlate.Rotation=new(OutputAngle,0,0);
    }
    protected override void Build()
    {
        PickRadius=1.1f;ClearMechanicalDrive();
        AddBox(new(0,-.48f,0),new(1.9f,.16f,1.1f),new("#293954"));
        AddBox(new(-.65f,-.1f,0),new(.35f,.7f,.65f),new("#fff8e9"));
        AddBox(new(.65f,-.1f,0),new(.35f,.7f,.65f),new("#fff8e9"));
        _input=Pulley(-.65f,"InputPulley");_output=Pulley(.65f,"OutputPulley");
        _leftPlate=Plate(-.25f,"InputPlate");_rightPlate=Plate(.25f,"OutputPlate");
        var axle=PartArt.Cylinder(Visual,.055f,1.3f,new("#293954"));axle.RotationDegrees=new(0,0,90);
        var coil=PartArt.Ring(Visual,.38f,.065f,new("#66b8c9"));coil.RotationDegrees=new(0,0,90);
        PartArt.Sphere(Visual,.08f,new("#e8b764"),new(0,-.35f,.5f));
        _indicator=(StandardMaterial3D)PartArt.Sphere(Visual,.065f,new("#556573"),new(0,.4f,.18f)).MaterialOverride;
    }
    private Node3D Pulley(float x,string name)
    {
        var node=new Node3D {Name=name,Position=new(x,0,.43f)};Visual.AddChild(node);
        var wheel=PartArt.Cylinder(node,.28f,.13f,new("#fff8e9"));wheel.RotationDegrees=new(90,0,0);
        PartArt.Box(node,new(.44f,.065f,.04f),new("#e8b764"),new(0,0,.08f));return node;
    }
    private Node3D Plate(float x,string name)
    {
        var node=new Node3D {Name=name,Position=new(x,0,0)};Visual.AddChild(node);
        var disc=PartArt.Cylinder(node,.3f,.18f,new("#e8b764"));disc.RotationDegrees=new(0,0,90);
        PartArt.Box(node,new(.19f,.43f,.04f),new("#293954"));return node;
    }
}
