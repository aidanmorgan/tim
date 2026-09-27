using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public partial class SoundMeterPart : MachinePart
{
    public float Threshold=>Properties[ReceiverParameters.Threshold];
    public float Level { get; private set; }
    public bool AboveThreshold { get; private set; }
    public int TriggerCount { get; private set; }
    private bool _pending;
    private int _sampledTick=-1;
    private Node3D _needle=null!;
    private StandardMaterial3D _lamp=null!;
    private float _displayLevel;
    public override Vector3? AcousticTarget=>Vector3.Zero;
    public override bool CanSendActivation=>true;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketIds.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.92f,-.5f,0)),
        new(SocketIds.Supply,ConnectionDomain.Electrical,PortDirection.Output,new(.92f,-.5f,0)),
        new(SocketIds.ActivationOut,ConnectionDomain.Activation,PortDirection.Output,new(.92f,.45f,0))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes=>
        [new(SocketIds.PowerIn,SocketIds.Supply,AboveThreshold)];
    public override void ValidateParameters()
    {
        if(!float.IsFinite(Threshold)||Threshold<.05f||Threshold>1)
            throw new ArgumentException("Sound meter threshold must be finite and between 0.05 and 1.");
    }
    public override void ReceiveAcousticLevel(float level)
    {
        if(!float.IsFinite(level)||level<0||level>1)throw new ArgumentException("Sound level must be between zero and one.");
        Level=level;
        var above=AboveThreshold?level>Threshold*.9f:level>=Threshold;
        _pending=above&&!AboveThreshold;
        AboveThreshold=above;
    }
    public override void BeforeStep(MachineWorld world,float delta)
    {
        if(_sampledTick==world.Ticks)return;
        _sampledTick=world.Ticks;
        Active=AboveThreshold&&HasElectricalPower(SocketIds.PowerIn);
        if(_pending&&HasElectricalPower(SocketIds.PowerIn))
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
        _needle=new Node3D {Position=new(0,-.2f,.42f)};Visual.AddChild(_needle);
        PartArt.Box(_needle,new(.035f,.44f,.035f),new("#e8b764"),new(0,.22f,0));
        PartArt.Sphere(Visual,.065f,new("#293954"),new(0,-.2f,.46f));
        _lamp=(StandardMaterial3D)PartArt.Sphere(Visual,.065f,new("#556573"),new(0,-.43f,.41f)).MaterialOverride;
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
    }
    public override void _Process(double delta)
    {
        _displayLevel=Mathf.Lerp(_displayLevel,Level,1-Mathf.Exp(-(float)delta*18));
        _needle.Rotation=new(0,0,Mathf.Lerp(1,-1,_displayLevel));
        _lamp.AlbedoColor=_lamp.AlbedoColor.Lerp(Active?new("#f7cb52"):new("#556573"),1-Mathf.Exp(-(float)delta*16));
    }
}
