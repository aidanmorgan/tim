using Godot;
using System;

namespace CuriousContraptions;

/// <summary>Clear hollow gravity tube. No capture, teleport, scripted transport or added energy.</summary>
public partial class PipePart : MachinePart, IResizablePart, ITubePart
{
    public float Length => Properties[PipeParameters.Length];
    public Vector3 Dimensions => new(Length, PipeParameters.BoreDiameter, PipeParameters.BoreDiameter);
    public ResizeAxes ResizableAxes => ResizeAxes.X;
    public System.Collections.Generic.IEnumerable<TubeMouth> Mouths
    {
        get
        {
            yield return new(TubeMouthId.Start, Vector3.Left * (Length * .5f + .09f), Vector3.Left, BoreRadius);
            yield return new(TubeMouthId.End, Vector3.Right * (Length * .5f + .09f), Vector3.Right, BoreRadius);
        }
    }
    private MeshInstance3D _shell = null!;
    private readonly MeshInstance3D[] _collars = new MeshInstance3D[2], _rails = new MeshInstance3D[2];
    public override void ValidateParameters()
    {
        if (!float.IsFinite(Length) || Length < PipeParameters.MinimumLength || Length > PipeParameters.MaximumLength)
            throw new ArgumentException("Pipe length must be finite and between one and eight units.");
    }
    public const float BoreRadius = .65f;
    public override float SurfaceBounce => .15f;
    protected override void Build()
    {
        _shell = AddTube(Transform3D.Identity, .5f, BoreRadius, .70f, new Color(.40f, .72f, .79f, .16f), false);
        for (var i = 0; i < 2; i++)
        {
            _collars[i] = AddTube(Transform3D.Identity, .09f, BoreRadius, .78f, new("#fff8e9"), true);
            var z = i == 0 ? -.70f : .70f;
            _rails[i] = PartArt.Line(Visual, new(-.5f, 0, z), new(.5f, 0, z), new("#293954"), .018f);
        }
        SetDimensions(Dimensions);
    }
    public void SetDimensions(Vector3 size)
    {
        if (!size.IsFinite() || !Mathf.IsEqualApprox(size.Y, PipeParameters.BoreDiameter)
            || !Mathf.IsEqualApprox(size.Z, PipeParameters.BoreDiameter))
            throw new ArgumentException("Only pipe length can be resized; the bore is fixed.");
        Properties[PipeParameters.Length] = Mathf.Clamp(size.X, PipeParameters.MinimumLength, PipeParameters.MaximumLength);
        _shell.Scale = new(Length, 1, 1);
        Tubes.Clear();
        Tubes.Add(new(Transform3D.Identity, Length * .5f, BoreRadius, .70f, false));
        for (var i = 0; i < 2; i++)
        {
            _collars[i].Position = new((i == 0 ? -1 : 1) * Length * .5f, 0, 0);
            Tubes.Add(new(_collars[i].Transform, .09f, BoreRadius, .78f, true));
            ((CylinderMesh)_rails[i].Mesh).Height = Length;
        }
        UpdateSelectionRadius(Mathf.Sqrt(Length * Length + 1.56f * 1.56f) * .5f);
    }
    private MeshInstance3D AddTube(Transform3D pose, float halfLength, float inner, float outer, Color color, bool opaque)
    {
        const int sides = 48;
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        Vector3 Point(float x, float radius, int step) => new(x, Mathf.Cos(step * Mathf.Tau / sides) * radius,
            Mathf.Sin(step * Mathf.Tau / sides) * radius);
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            mesh.SurfaceSetNormal(normal);
            foreach (var vertex in new[] { a, b, c, a, c, d }) mesh.SurfaceAddVertex(vertex);
        }
        for (var i = 0; i < sides; i++)
        {
            var normal = Point(0, 1, i);
            Quad(Point(-halfLength, outer, i), Point(halfLength, outer, i), Point(halfLength, outer, i + 1), Point(-halfLength, outer, i + 1), normal);
            Quad(Point(-halfLength, inner, i + 1), Point(halfLength, inner, i + 1), Point(halfLength, inner, i), Point(-halfLength, inner, i), -normal);
            foreach (var x in new[] { -halfLength, halfLength })
                Quad(Point(x, inner, i), Point(x, outer, i), Point(x, outer, i + 1), Point(x, inner, i + 1), x < 0 ? Vector3.Left : Vector3.Right);
        }
        mesh.SurfaceEnd();
        var node = PartArt.Mesh(Visual, mesh, color);
        node.Transform = pose;
        var material = (StandardMaterial3D)node.MaterialOverride;
        material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        if (!opaque)
        {
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            node.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        }
        return node;
    }
}
