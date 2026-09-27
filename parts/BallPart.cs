using Godot;
namespace CuriousContraptions;

public partial class BallPart : MachinePart
{
    protected override void Build()
    {
        Dynamic = true;
        Radius = Parameter("radius", .34f);
        Mass = Parameter("mass", 1);
        Bounce = Parameter("bounce", .45f);
        Buoyancy = Parameter("buoyancy", 0);
        Drag = Parameter("drag", .04f);
        PickRadius = Radius + .12f;
        PartArt.Sphere(Visual, Radius, Definition.Color);
        var stripe = PartArt.Ring(Visual, Radius * .99f, .014f, Definition.Color.Darkened(.4f));
        stripe.RotationDegrees = new(0, 0, 35);
        if (Buoyancy > 0)
            PartArt.Line(Visual, new(0, -Radius, 0), new(.06f, -Radius - .5f, 0), new("#e9dfcc"), .015f);
    }
    public override void AfterStep(MachineWorld world, float delta)
    {
        Visual.RotateZ(-Velocity.X * delta / Radius);
        Visual.RotateX(Velocity.Z * delta / Radius);
    }
}

