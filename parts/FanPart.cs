using Godot;
namespace CuriousContraptions;

public partial class FanPart : MachinePart
{
    private Node3D _rotor = null!;
    public override bool CanReceivePower => true;
    protected override void Build()
    {
        Active = Parameter("powered", 1) > .5f;
        PickRadius = .75f;
        AddBox(new(0, -.55f, 0), new(.65f, .18f, 1), new("#263d4b"));
        PartArt.Box(Visual, new(.13f, .5f, .15f), new("#ccd8dc"), new(0, -.3f, 0));
        var housing = PartArt.Ring(Visual, .56f, .07f, Definition.Color);
        housing.RotationDegrees = new(0, 0, 90);
        _rotor = new Node3D();
        Visual.AddChild(_rotor);
        PartArt.Sphere(_rotor, .14f, new("#f4d089"));
        foreach (var angle in new[] { 0, 120, 240 })
        {
            var blade = PartArt.Box(_rotor, new(.1f, .6f, .16f), Definition.Color.Lightened(.15f));
            blade.RotationDegrees = new(angle, 0, 0);
        }
        PartArt.Line(Visual, new(.6f, 0, 0), new(1.15f, 0, 0), new("#a9e7e0"), .025f);
    }
    public override void BeforeStep(MachineWorld world, float delta)
    {
        if (!Active) return;
        _rotor.RotateX(delta * 18);
        foreach (var body in world.Bodies)
        {
            if (!body.Visible) continue;
            var point = ToLocal(body.Position);
            var width = Parameter("width", .85f);
            if (point.X > 0 && point.X < Parameter("reach", 5) && new Vector2(point.Y, point.Z).Length() < width)
                body.Velocity += Basis.X * Parameter("force", 9) * world.Pressure * delta / body.Mass;
        }
    }
}
