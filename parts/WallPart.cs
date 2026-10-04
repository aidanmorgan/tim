using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Panel input/art adapter; the common Box declaration owns all contact physics.</summary>
public partial class WallPart : MachinePart, IResizablePart
{
    public WallDimensions CanonicalDimensions { get; private set; }
    public ResizeAxes ResizableAxes => ResizeAxes.All;
    public Vector3 Dimensions => new((float)CanonicalDimensions.Width.Value,
        (float)CanonicalDimensions.Height.Value, (float)CanonicalDimensions.Thickness.Value);
    protected override void Build()
    {
        PartArt.Box(Visual, new(1, 1, .998f), Definition.Color);
        foreach (var side in new[] { -1, 1 })
            PartArt.Box(Visual, new(.035f, .94f, 1), new("#f9e8c9"), new(side * .46f, 0, 0));
        ApplyDimensions(Definition.Wall!.Capture());
    }
    public void SetDimensions(Vector3 size) => ApplyDimensions(WallDimensions.FromInput(size.X, size.Y, size.Z));
    public void ApplyDimensions(WallDimensions dimensions)
    {
        dimensions.Validate();
        CanonicalDimensions = dimensions;
        Visual.Scale = Dimensions;
        PickRadius = Dimensions.Length() * .5f;
    }
}
