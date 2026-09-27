using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public enum LatchState { Off, On }

/// <summary>Reset-dominant memory contact. Commands settle at the next tick boundary,
/// so every delivery in a simulation tick participates before the electrical solve.</summary>
public partial class LatchPart : MachinePart
{
    [Flags] private enum Requests { None=0, Set=1, Reset=2 }
    private readonly SortedDictionary<int,Requests> _pending=new();
    public LatchState State { get; private set; }
    private MeshInstance3D _rocker=null!;
    private StandardMaterial3D _indicator=null!;
    private float _level;
    public override bool CanReceiveActivation=>true;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketIds.SetIn,ConnectionDomain.Activation,PortDirection.Input,new(-.45f,.72f,0),ActivationCommand.Set),
        new(SocketIds.ResetIn,ConnectionDomain.Activation,PortDirection.Input,new(.45f,.72f,0),ActivationCommand.Reset),
        new(SocketIds.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.93f,0,0)),
        new(SocketIds.Supply,ConnectionDomain.Electrical,PortDirection.Output,new(.93f,0,0))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes=>
        State==LatchState.On?[new(SocketIds.PowerIn,SocketIds.Supply)]:[];
    public override ActivationDisposition HandleActivation(MachineWorld world,ActivationCommand command)
    {
        var request=command switch
        {
            ActivationCommand.Set=>Requests.Set,
            ActivationCommand.Reset=>Requests.Reset,
            _=>throw new ArgumentException("Latch requires an explicit Set or Reset input.")
        };
        _pending.TryGetValue(world.Ticks,out var previous);
        _pending[world.Ticks]=previous|request;
        return ActivationDisposition.Deferred;
    }
    public override void BeforeNetworks(MachineWorld world)
    {
        foreach(var tick in _pending.Keys.TakeWhile(t=>t<world.Ticks).ToArray())
        {
            State=(_pending[tick]&Requests.Reset)!=0?LatchState.Off:LatchState.On;
            _pending.Remove(tick);
        }
        Active=State==LatchState.On;
        if(Active)world.Events.TryAdd(new(MachineEventKind.Activated,Uid),world.Ticks);
    }
    protected override void Build()
    {
        PickRadius=1.1f;
        AddBox(Vector3.Zero,new(1.65f,1.25f,.65f),new("#66b8c9"));
        AddBox(new(0,-.72f,0),new(1.8f,.16f,.85f),new("#293954"));
        PartArt.Box(Visual,new(1.4f,1.02f,.04f),new("#fff8e9"),new(0,0,.35f));
        _rocker=PartArt.Box(Visual,new(.65f,.28f,.12f),new("#293954"),new(0,0,.44f));
        _indicator=(StandardMaterial3D)PartArt.Sphere(Visual,.09f,new("#556573"),new(0,-.33f,.42f)).MaterialOverride;
        // Raised bar and hollow ring distinguish Set and Reset without text or colour dependence.
        PartArt.Box(Visual,new(.055f,.20f,.04f),new("#293954"),new(-.45f,.34f,.4f));
        for(var i=0;i<12;i++)
        {
            var angle=i*Mathf.Tau/12;
            PartArt.Sphere(Visual,.025f,new("#293954"),new(.45f+Mathf.Cos(angle)*.1f,.34f+Mathf.Sin(angle)*.1f,.4f));
        }
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
    }
    public override void _Process(double delta)
    {
        _level=Mathf.MoveToward(_level,State==LatchState.On?1:0,(float)delta*8);
        var eased=_level*_level*(3-2*_level);
        _rocker.Rotation=new(0,0,Mathf.Lerp(-.25f,.25f,eased));
        _indicator.AlbedoColor=new Color("#556573").Lerp(new("#f7cb52"),eased);
    }
}
