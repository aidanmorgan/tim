using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Front-facing laser target closes a separately supplied electrical contact.</summary>
public partial class LightReceiverPart : MachinePart
{
    public float Threshold=>Properties[ReceiverParameters.Threshold];
    public Vector3 ReceivedPower { get; private set; }
    public float Power=>(ReceivedPower.X+ReceivedPower.Y+ReceivedPower.Z)/3;
    public override OpticalSurface? OpticalSurface=>new(new(new(-.18f,0,0),Vector3.Left,.55f),OpticalInteraction.Absorb);
    private StandardMaterial3D _target=null!;
    private float _level;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketIds.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(0,-.6f,.65f)),
        new(SocketIds.Supply,ConnectionDomain.Electrical,PortDirection.Output,new(0,-.6f,-.65f))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes=>
        Power>=Threshold?[new(SocketIds.PowerIn,SocketIds.Supply)]:[];
    public override void ReceiveOpticalPower(Vector3 power){ReceivedPower=power;Active=Power>=Threshold;}
    public override void ValidateParameters()
    {
        if(!float.IsFinite(Threshold)||Threshold<.05f||Threshold>2)
            throw new ArgumentException("Receiver threshold must be finite and between 0.05 and 2.");
    }
    protected override void Build()
    {
        PickRadius=1.1f;
        AddBox(Vector3.Zero,new(.26f,1.4f,1.4f),new("#fff8e9"));
        AddBox(new(0,-.88f,0),new(.85f,.18f,1.6f),new("#293954"));
        var disc=PartArt.Cylinder(Visual,.55f,.035f,new("#556573"),new(-.18f,0,0));
        disc.RotationDegrees=new(0,0,90);_target=(StandardMaterial3D)disc.MaterialOverride;
        var ring=PartArt.Ring(Visual,.33f,.025f,new("#fff8e9"),new(-.21f,0,0));
        ring.RotationDegrees=new(0,0,90);
        PartArt.Sphere(Visual,.08f,new("#f7cb52"),new(-.23f,0,0));
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
    }
    public override void _Process(double delta)
    {
        _level=Mathf.MoveToward(_level,Active?1:0,(float)delta*8);
        _target.AlbedoColor=new Color("#556573").Lerp(new("#f7cb52"),_level*_level*(3-2*_level));
    }
}
