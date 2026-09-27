using Godot;
namespace CuriousContraptions;

/// <summary>Prototype deterministic topple rule; rigid lever dynamics are future work.</summary>
public partial class DominoPart : MachinePart
{
    private float _topple;
    public override bool CanSendActivation => true;
    public override bool CanReceiveActivation => true;
    protected override void Build()
    {
        PickRadius = .6f;
        AddBox(new(0, .45f, 0), new(.25f, 1.1f, .65f), Definition.Color);
        foreach (var height in new[] { .2f, .7f })
            PartArt.Sphere(Visual, .045f, new("#384757"), new(.14f, height, 0));
    }
    public override void OnContact(MachinePart body, float speed, MachineWorld world)
    {
        if (speed > Assistance(world.Precision).TriggerThreshold && !Active) world.Activate(this);
    }
    public override void AfterStep(MachineWorld world, float delta)
    {
        if (!Active) return;
        _topple = Mathf.Min(_topple + delta * 4, 1.4f);
        Visual.Rotation = new(0, 0, -_topple);
        if (_topple <= .7f) return;
        foreach (var other in world.Parts)
        {
            var local = ToLocal(other.Position);
            if (other is DominoPart && other != this && local.X > 0 && local.X < 1 && Mathf.Abs(local.Y) < .4f && Mathf.Abs(local.Z) < .4f)
                world.Activate(other);
        }
    }
}
