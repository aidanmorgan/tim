using Godot;
using System.Collections.Generic;
namespace CuriousContraptions;

public partial class SpringPart : MachinePart
{
    private readonly Dictionary<string, int> _cooldown = new();
    protected override void Build()
    {
        PickRadius = .7f;
        AddBox(new(0, .14f, 0), new(1.3f, .15f, 1.2f), Definition.Color);
        PartArt.Box(Visual, new(1.3f, .12f, 1.2f), new("#273446"), new(0, -.36f, 0));
        foreach (var height in new[] { -.23f, -.13f, -.03f })
            PartArt.Ring(Visual, .27f, .035f, new("#ccd9df"), new(0, height, 0));
    }
    public override void OnContact(MachinePart body, float speed, MachineWorld world)
    {
        if (speed < .05f || _cooldown.GetValueOrDefault(body.Uid, -1000) + 18 > world.Ticks) return;
        _cooldown[body.Uid] = world.Ticks;
        var direction = Basis.Y.Normalized();
        body.Velocity = direction * Parameter("strength", 8.5f);
        world.Events.TryAdd("bounced:" + Uid, world.Ticks);
    }
}
