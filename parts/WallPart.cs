using Godot;

namespace CuriousContraptions;

/// <summary>A physical toy panel; dimensions are local-axis lengths, never node scale.</summary>
public partial class WallPart : MachinePart
{
    public Vector3 Dimensions { get; private set; }
    public static readonly Vector3 Minimum = new(.4f, .4f, .12f);
    public static readonly Vector3 Maximum = new(8, 6, 2);
    protected override void Build()
    {
        PartArt.Box(Visual, new(1, 1, .998f), Definition.Color);
        // Inset cream bands give the panel a toy-block silhouette without exceeding its collider.
        foreach (var side in new[] { -1, 1 })
            PartArt.Box(Visual, new(.035f, .94f, 1), new("#f9e8c9"), new(side * .46f, 0, 0));
        SetDimensions(new(Parameter("width", 3), Parameter("height", 2), Parameter("thickness", .25f)));
    }

    public void SetDimensions(Vector3 size)
    {
        if (!size.IsFinite()) return;
        Dimensions = size.Clamp(Minimum, Maximum);
        Properties["width"] = Dimensions.X;
        Properties["height"] = Dimensions.Y;
        Properties["thickness"] = Dimensions.Z;
        Boxes.Clear();
        Boxes.Add(new(Vector3.Zero, Dimensions * .5f));
        Visual.Scale = Dimensions;
        UpdateSelectionRadius(Dimensions.Length() * .5f);
    }
}
