using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum ClockState { Stopped, Running }

/// <summary>Powered periodic trigger. No startup edge, no catch-up events: loss of power resets phase.</summary>
public partial class ClockPart : MachinePart
{
    public ClockState State { get; private set; }
    public int DueTick { get; private set; }=-1;
    public int PulseCount { get; private set; }
    public int LastPulseTick { get; private set; }=-1;
    public float Interval=>Properties[ClockParameters.Seconds];
    public int IntervalTicks=>(int)Math.Ceiling(Interval/MachineWorld.Tick);
    public float Progress { get; private set; }
    private int _sampledTick=-1;
    private Node3D _pendulum=null!;
    private StandardMaterial3D _indicator=null!;
    private float _pulse;
    public override bool CanSendActivation=>true;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.78f,0,0)),
        new(SocketId.ActivationOut,ConnectionDomain.Activation,PortDirection.Output,new(.78f,0,0))
    ];
    public override void ValidateParameters()
    {
        if(!float.IsFinite(Interval)||Interval<.1f||Interval>12)
            throw new ArgumentException("Clock interval must be finite and between 0.1 and 12 seconds.");
    }
    public override void BeforeStep(MachineWorld world,float delta)
    {
        if(_sampledTick==world.Ticks)return;
        _sampledTick=world.Ticks;
        if(!HasElectricalPower(SocketId.PowerIn))
        {
            State=ClockState.Stopped;DueTick=-1;Progress=0;Active=false;
            return;
        }
        if(State==ClockState.Stopped)
        {
            State=ClockState.Running;
            DueTick=world.Ticks+IntervalTicks;
        }
        if(world.Ticks>=DueTick)
        {
            PulseCount++;
            LastPulseTick=world.Ticks;
            DueTick+=IntervalTicks;
            _pulse=1;
            world.EmitActivation(this);
        }
        Progress=1-(float)(DueTick-world.Ticks)/IntervalTicks;
        Active=LastPulseTick==world.Ticks;
    }
    protected override void Build()
    {
        PickRadius=1.1f;
        AddBox(Vector3.Zero,new(1.4f,1.7f,.6f),new("#e8b764"));
        AddBox(new(0,-.95f,0),new(1.6f,.2f,.85f),new("#293954"));
        PartArt.Box(Visual,new(1.14f,1.44f,.04f),new("#fff8e9"),new(0,0,.32f));
        PartArt.Box(Visual,new(.84f,1.1f,.02f),new("#556573"),new(0,-.06f,.35f));
        _pendulum=new Node3D {Position=new(0,.4f,.39f)};
        Visual.AddChild(_pendulum);
        PartArt.Box(_pendulum,new(.045f,.7f,.035f),new("#fff8e9"),new(0,-.35f,0));
        PartArt.Sphere(_pendulum,.15f,new("#f7cb52"),new(0,-.7f,0));
        PartArt.Sphere(Visual,.055f,new("#293954"),new(0,.4f,.44f));
        _indicator=(StandardMaterial3D)PartArt.Sphere(Visual,.065f,new("#556573"),new(0,.63f,.39f)).MaterialOverride;
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
    }
    public override void _Process(double delta)
    {
        // Presentation settles after shutdown; simulation ticks alone determine all events.
        var target=State==ClockState.Running?Mathf.Sin(Progress*Mathf.Tau)*.42f:0;
        _pendulum.Rotation=new(0,0,Mathf.Lerp(_pendulum.Rotation.Z,target,1-Mathf.Exp(-(float)delta*30)));
        _pulse=Mathf.MoveToward(_pulse,0,(float)delta*4);
        _indicator.AlbedoColor=new Color("#556573").Lerp(new("#f7cb52"),_pulse);
    }
}
