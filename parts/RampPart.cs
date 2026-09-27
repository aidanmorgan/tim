using Godot;
namespace CuriousContraptions;

public partial class RampPart : MachinePart
{
    protected override void Build()
    {
        var length = Parameter("length", 3);
        var width = Parameter("width", 1.3f);
        PickRadius = length * .5f;
        AddBox(Vector3.Zero, new(length, .18f, width), Definition.Color);
        foreach (var sign in new[] { -1, 1 })
        {
            PartArt.Box(Visual, new(length, .055f, .055f), new("#f9e8c9"), new(0, .11f, sign * width * .46f));
            PartArt.Sphere(Visual, .035f, new("#364354"), new(sign * length * .4f, .12f, width * .38f));
        }
    }
}

