using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Fail-closed rectangular optical shutter; its moving physical OBB is the beam blocker.</summary>
public partial class BeamShutterPart : MachinePart
{
    public const float Stroke=1.3f;
    public static readonly Vector3 BladeHalf=new(.035f,.6f,.6f);
    private readonly SlidingBlade _motion=new(BladeHalf,Stroke);
    public float Opening=>_motion.Opening;
    public float BladeSpeed=>_motion.Speed;
    public GateState State=>_motion.State;
    private int _bladeIndex;
    private MeshInstance3D _blade=null!;
    private StandardMaterial3D _indicator=null!;
    public override IEnumerable<ConnectionPort> ConnectionPorts=>
        [new(SocketIds.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,new(0,1.45f,.86f))];
    protected override void Build()
    {
        PickRadius=1.4f;
        AddBox(new(0,1.45f,0),new(.45f,.85f,1.65f),new("#293954"));
        AddBox(new(0,-.78f,0),new(.65f,.2f,1.65f),new("#293954"));
        foreach(var z in new[]{-.7f,.7f})
            AddBox(new(0,.2f,z),new(.18f,1.9f,.12f),new("#fff8e9"));
        _bladeIndex=Boxes.Count;
        AddBox(Vector3.Zero,BladeHalf*2,new("#f7cb52"),false);
        _blade=PartArt.Box(Visual,BladeHalf*2,new("#f7cb52"));
        // Narrow cream witness stripe makes the blade's travel visible from either side.
        foreach(var x in new[]{-.037f,.037f})
            PartArt.Box(_blade,new(.008f,.04f,1.05f),new("#fff8e9"),new(x,-.45f,0));
        PartArt.Sphere(Visual,.075f,new("#e8b764"),new(0,1.45f,.86f));
        _indicator=(StandardMaterial3D)PartArt.Sphere(Visual,.07f,new("#556573"),new(.24f,1.45f,0)).MaterialOverride;
    }
    public override void BeforeStep(MachineWorld world,float delta)
    {
        var powered=HasElectricalPower(SocketIds.PowerIn);
        _motion.Step(world,this,powered,delta);
        _blade.Position=_motion.Position;
        Boxes[_bladeIndex]=new(_motion.Position,BladeHalf);
        Active=powered;
        _indicator.AlbedoColor=State==GateState.Blocked?new("#e8b764"):powered?new("#f7cb52"):new("#556573");
        if(powered)world.Events.TryAdd(new(MachineEventKind.Powered,Uid),world.Ticks);
    }
}
