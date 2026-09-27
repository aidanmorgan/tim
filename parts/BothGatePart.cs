using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum BothGateState { Neither, FirstOnly, SecondOnly, Both }

/// <summary>A continuous two-input electrical interlock, not a source or memory module.</summary>
public partial class BothGatePart : MachinePart
{
    public BothGateState State=>HasElectricalPower(SocketIds.FirstIn)
        ?HasElectricalPower(SocketIds.SecondIn)?BothGateState.Both:BothGateState.FirstOnly
        :HasElectricalPower(SocketIds.SecondIn)?BothGateState.SecondOnly:BothGateState.Neither;
    private readonly List<StandardMaterial3D> _indicators=new();
    private readonly float[] _levels=new float[3];
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketIds.FirstIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.94f,.35f,0)),
        new(SocketIds.SecondIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.94f,-.35f,0)),
        new(SocketIds.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(0,-.75f,.55f)),
        new(SocketIds.Supply,ConnectionDomain.Electrical,PortDirection.Output,new(.94f,0,0))
    ];
    public override IEnumerable<ElectricalGate> ElectricalGates=>
        [new(LogicGateKind.And,SocketIds.FirstIn,SocketIds.SecondIn,SocketIds.PowerIn,SocketIds.Supply)];
    public override void BeforeStep(MachineWorld world,float delta)
    {
        Active=State==BothGateState.Both && HasElectricalPower(SocketIds.PowerIn);
        if(Active)world.Events.TryAdd(new(MachineEventKind.Powered,Uid),world.Ticks);
    }
    protected override void Build()
    {
        PickRadius=1.1f;
        AddBox(Vector3.Zero,new(1.7f,1.3f,.65f),new("#66b8c9"));
        AddBox(new(0,-.75f,0),new(1.85f,.16f,.85f),new("#293954"));
        PartArt.Box(Visual,new(1.45f,1.05f,.04f),new("#fff8e9"),new(0,0,.35f));
        // Two paths converge onto one output. One/two raised ticks identify the input rows.
        foreach(var y in new[]{.3f,-.3f})
        {
            var line=PartArt.Box(Visual,new(.65f,.035f,.025f),new("#293954"),new(-.05f,y*.5f,.395f));
            line.RotationDegrees=new(0,0,y>0?-27:27);
        }
        for(var i=0;i<3;i++)
        {
            var at=i<2?new Vector3(-.48f,i==0?.3f:-.3f,.42f):new Vector3(.48f,0,.42f);
            _indicators.Add((StandardMaterial3D)PartArt.Sphere(Visual,.09f,new("#556573"),at).MaterialOverride);
        }
        foreach(var x in new[]{-.58f,-.42f})
            PartArt.Box(Visual,new(.025f,.08f,.025f),new("#293954"),new(x,-.47f,.4f));
        PartArt.Box(Visual,new(.025f,.08f,.025f),new("#293954"),new(-.48f,.47f,.4f));
        foreach(var port in ConnectionPorts)PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
    }
    public override void _Process(double delta)
    {
        for(var i=0;i<3;i++)
        {
            var on=i==0?HasElectricalPower(SocketIds.FirstIn):i==1?HasElectricalPower(SocketIds.SecondIn):Active;
            _levels[i]=Mathf.MoveToward(_levels[i],on?1:0,(float)delta*10);
            var level=_levels[i];
            _indicators[i].AlbedoColor=new Color("#556573").Lerp(new("#f7cb52"),level*level*(3-2*level));
        }
    }
}
