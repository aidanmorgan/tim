using Godot;
using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Ramp geometry/input adapter; the common Box collider owns contact physics.</summary>
public partial class RampPart : MachinePart, IResizablePart
{
    public RampDimensions CanonicalDimensions { get; private set; }
    public Vector3 Dimensions => new((float)CanonicalDimensions.Length.Value, (float)RampDimensions.Thickness.Value,
        (float)CanonicalDimensions.Width.Value);
    public ResizeAxes ResizableAxes => ResizeAxes.X | ResizeAxes.Z;
    protected override void Build()
    {
        CanonicalDimensions = Definition.Ramp!.Capture();
        var length = (float)CanonicalDimensions.Length.Value;
        var width = (float)CanonicalDimensions.Width.Value;
        PickRadius = length * .5f;
        PartArt.Box(Visual, new(length, (float)RampDimensions.Thickness.Value, width), Definition.Color, Vector3.Zero);
        foreach (var sign in new[] { -1, 1 })
        {
            PartArt.Box(Visual, new(length, .055f, .055f), new("#f9e8c9"), new(0, .11f, sign * width * .46f));
            PartArt.Sphere(Visual, .035f, new("#364354"), new(sign * length * .4f, .12f, width * .38f));
        }
    }
    public void SetDimensions(Vector3 dimensions)
    {
        if (!dimensions.IsFinite()) throw new ArgumentException("Nonfinite ramp dimensions.");
        ApplyDimensions(new(new((Half)dimensions.X), new((Half)dimensions.Z)));
    }
    public void ApplyDimensions(RampDimensions dimensions)
    {
        dimensions.Validate();
        CanonicalDimensions = dimensions;
        var original = Definition.Ramp!.Capture();
        Visual.Scale = new((float)dimensions.Length.Value / (float)original.Length.Value, 1,
            (float)dimensions.Width.Value / (float)original.Width.Value);
        PickRadius = (float)dimensions.Length.Value * .5f;
    }
}
