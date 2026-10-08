using Godot;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>Hollow-tube input/art adapter; shared annular contact owns all physical motion.</summary>
public partial class PipePart : MachinePart, IResizablePart
{
    public PipeDimensions CanonicalDimensions { get; private set; }
    public Vector3 Dimensions => new((float)CanonicalDimensions.Length.Value,
        (float)PipeDimensions.BoreDiameter.Value, (float)PipeDimensions.BoreDiameter.Value);
    public ResizeAxes ResizableAxes => ResizeAxes.X;
    private MeshInstance3D _shell = null!;
    private readonly MeshInstance3D[] _collars = new MeshInstance3D[2], _rails = new MeshInstance3D[2];
    protected override void Build()
    {
        var dimensions = Definition.Pipe!.Capture();
        var profile = dimensions.Profile;
        var bore = (float)profile.InnerRadius.Value;
        var outer = (float)profile.MiddleRadius.Value;
        _shell = PipeArt.Cylinder(Visual, Transform3D.Identity, .5f, bore, outer, new Color(.40f, .72f, .79f, .16f), false);
        for (var i = 0; i < 2; i++)
        {
            _collars[i] = PipeArt.Cylinder(Visual, Transform3D.Identity, (float)profile.EndHalfWidth.Value,
                bore, (float)profile.EndRadius.Value, new("#fff8e9"), true);
            var z = i == 0 ? -outer : outer;
            _rails[i] = PartArt.Line(Visual, new(-.5f, 0, z), new(.5f, 0, z), new("#293954"), .018f);
        }
        ApplyDimensions(dimensions);
    }
    public void SetDimensions(Vector3 size) => ApplyDimensions(PipeDimensions.FromInput(size.X, size.Y, size.Z));
    public void ApplyDimensions(PipeDimensions dimensions)
    {
        dimensions.Validate();
        CanonicalDimensions = dimensions;
        var length = (float)dimensions.Length.Value;
        _shell.Scale = new(length, 1, 1);
        for (var i = 0; i < 2; i++)
        {
            _collars[i].Position = new((i == 0 ? -1 : 1) * length * .5f, 0, 0);
            ((CylinderMesh)_rails[i].Mesh).Height = length;
        }
        var profile = dimensions.Profile;
        var axial = (float)profile.HalfLength.Value + (float)profile.EndHalfWidth.Value;
        var radial = (float)profile.EndRadius.Value;
        PickRadius = Mathf.Sqrt(axial * axial + radial * radial);
    }
}
