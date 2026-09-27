using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Externally powered laser, enabled by a trigger until workshop Reset.</summary>
public partial class LaserPart : MachinePart
{
    public const float Range=16;
    public static readonly Vector3 LensPosition=new(.72f,0,0);
    public static readonly Vector3 BeamPower=new(1,.78f,.32f);
    public bool Enabled { get; private set; }
    public float BeamLength { get; private set; }
    private MeshInstance3D _beam=null!;
    private StandardMaterial3D _lens=null!;
    public override bool CanReceiveActivation=>true;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketIds.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.7f,0,0)),
        new(SocketIds.ActivationIn,ConnectionDomain.Activation,PortDirection.Input,new(0,.52f,0))
    ];
    public override OpticalEmitter? OpticalSource=>Enabled&&HasElectricalPower(SocketIds.PowerIn)
        ?new(LensPosition,Vector3.Right,Range,BeamPower):null;
    public override ActivationDisposition HandleActivation(MachineWorld world,ActivationCommand command)
    {
        if(command!=ActivationCommand.Trigger)throw new ArgumentException("Unsupported laser command.");
        Enabled=true;
        return ActivationDisposition.Deferred;
    }
    public override void ReceiveBeamLength(float distance){BeamLength=distance;Active=distance>0;}
    protected override void Build()
    {
        PickRadius=1;
        AddBox(Vector3.Zero,new(1.25f,.85f,.8f),new("#e8b764"));
        AddBox(new(0,-.55f,0),new(1.5f,.16f,1),new("#293954"));
        var rim=PartArt.Cylinder(Visual,.38f,.12f,new("#fff8e9"),new(.66f,0,0));
        rim.RotationDegrees=new(0,0,90);
        var lens=PartArt.Cylinder(Visual,.23f,.035f,new("#556573"),LensPosition);
        lens.RotationDegrees=new(0,0,90);_lens=(StandardMaterial3D)lens.MaterialOverride;
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
        _beam=PartArt.Cylinder(Visual,.025f,1,new("#fff0a5"));
        _beam.RotationDegrees=new(0,0,90);
        _beam.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
        _beam.MaterialOverride=new StandardMaterial3D
        {
            ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor=new Color("#fff0a5") {A=.7f}
        };
        _beam.Visible=false;
    }
    public override void _Process(double delta)
    {
        _beam.Visible=BeamLength>.0001f;
        if(_beam.Visible)
        {
            _beam.Position=LensPosition+Vector3.Right*BeamLength*.5f;
            _beam.Scale=new(1,BeamLength,1);
        }
        _lens.AlbedoColor=_lens.AlbedoColor.Lerp(Active?new("#fff0a5"):new("#556573"),1-Mathf.Exp(-(float)delta*12));
    }
}
