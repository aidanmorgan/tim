using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

public enum CounterState { Counting, Reached }

/// <summary>Counts activation deliveries, emits once at its target and latches an electrical contact.
/// Workshop Reset clears the count; this module never generates electrical supply.</summary>
public partial class CounterPart : MachinePart
{
    public int Count { get; private set; }
    public int Target=>(int)Properties[CounterParameters.Target];
    public CounterState State=>Count==Target?CounterState.Reached:CounterState.Counting;
    private readonly List<StandardMaterial3D> _lights=new();
    private readonly List<float> _levels=new();
    public override bool CanReceiveActivation=>true;
    public override bool CanSendActivation=>true;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
    [
        new(SocketIds.ActivationIn,ConnectionDomain.Activation,PortDirection.Input,new(0,.72f,.1f)),
        new(SocketIds.ActivationOut,ConnectionDomain.Activation,PortDirection.Output,new(0,-.72f,.1f)),
        new(SocketIds.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(-.93f,0,0)),
        new(SocketIds.Supply,ConnectionDomain.Electrical,PortDirection.Output,new(.93f,0,0))
    ];
    public override IEnumerable<ElectricalRoute> ElectricalRoutes=>
        [new(SocketIds.PowerIn,SocketIds.Supply,State==CounterState.Reached)];
    public override void ValidateParameters()
    {
        var target=Properties[CounterParameters.Target];
        if(!float.IsFinite(target)||target<1||target>9||target!=Mathf.Floor(target))
            throw new ArgumentException("Counter target must be an integer between 1 and 9.");
    }
    public override ActivationDisposition HandleActivation(MachineWorld world, ActivationCommand command)
    {
        if (command != ActivationCommand.Trigger) throw new ArgumentException("Unsupported activation command.");
        if(State==CounterState.Reached)return ActivationDisposition.Deferred;
        Count++;
        return State==CounterState.Reached?ActivationDisposition.Immediate:ActivationDisposition.Deferred;
    }
    protected override void Build()
    {
        PickRadius=1.1f;
        AddBox(Vector3.Zero,new(1.65f,1.25f,.65f),new("#e8b764"));
        AddBox(new(0,-.72f,0),new(1.8f,.16f,.85f),new("#293954"));
        PartArt.Box(Visual,new(1.4f,1.02f,.04f),new("#fff8e9"),new(0,0,.35f));
        var rows=(Target+2)/3;
        for(var i=0;i<Target;i++)
        {
            var row=i/3;
            var columns=Math.Min(3,Target-row*3);
            var at=new Vector3((i%3-(columns-1)*.5f)*.36f,((rows-1)*.5f-row)*.3f,.4f);
            _lights.Add((StandardMaterial3D)PartArt.Sphere(Visual,.10f,new("#556573"),at).MaterialOverride);
            _levels.Add(0);
        }
        foreach(var port in ConnectionPorts)
            PartArt.Sphere(Visual,.075f,new("#f7cb52"),port.LocalPosition);
    }
    // Presentation finishes even if reaching the target also completes the puzzle.
    public override void _Process(double delta)
    {
        for(var i=0;i<_lights.Count;i++)
        {
            _levels[i]=Mathf.MoveToward(_levels[i],i<Count?1:0,(float)delta*10);
            var level=_levels[i];
            _lights[i].AlbedoColor=new Color("#556573").Lerp(new("#f7cb52"),level*level*(3-2*level));
        }
    }
}
