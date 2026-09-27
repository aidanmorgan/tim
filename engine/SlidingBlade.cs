using Godot;

namespace CuriousContraptions;

public enum GateState { Closed, Opening, Open, Closing, Blocked }

/// <summary>Shared accelerated local-Y blade motion. Closing stops before intersecting a visible body.</summary>
public sealed class SlidingBlade(Vector3 half,float stroke)
{
    public Vector3 Half { get; }=half;
    public float Stroke { get; }=stroke;
    public float Opening { get; private set; }
    public float Speed { get; private set; }
    public GateState State { get; private set; }
    public Vector3 Position=>Vector3.Up*Opening;
    public void Step(MachineWorld world,MachinePart owner,bool powered,float delta)
    {
        var target=powered?Stroke:0;
        var distance=target-Opening;
        var targetSpeed=Mathf.Sign(distance)*Mathf.Min(2.8f,Mathf.Sqrt(2*14*Mathf.Abs(distance)));
        Speed=Mathf.MoveToward(Speed,targetSpeed,14*delta);
        var next=Mathf.Clamp(Opening+Speed*delta,0,Stroke);
        var blocked=next<Opening&&Obstructed(world,owner,next);
        if(blocked){next=Opening;Speed=0;}
        Opening=next;
        if(Mathf.Abs(target-Opening)<.0001f){Opening=target;Speed=0;}
        State=blocked?GateState.Blocked:Opening==0?GateState.Closed:
            Opening==Stroke?GateState.Open:powered?GateState.Opening:GateState.Closing;
    }
    private bool Obstructed(MachineWorld world,MachinePart owner,float opening)
    {
        var inverse=owner.Transform.AffineInverse();
        foreach(var body in world.Bodies)
        {
            if(!body.Visible)continue;
            var local=inverse*body.Position-Vector3.Up*opening;
            if(local.DistanceSquaredTo(local.Clamp(-Half,Half))<=body.Radius*body.Radius)return true;
        }
        return false;
    }
}
