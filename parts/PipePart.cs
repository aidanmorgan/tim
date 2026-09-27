using Godot;

namespace CuriousContraptions;

/// <summary>Clear hollow gravity tube. No capture, teleport, scripted transport or added energy.</summary>
public partial class PipePart : MachinePart
{
    public const float HalfLength = 1.8f;
    public const float BoreRadius = .65f;
    public override float SurfaceBounce => .15f;
    protected override void Build()
    {
        PickRadius = 1.9f;
        AddTube(Transform3D.Identity, HalfLength, BoreRadius, .70f, new Color(.40f, .72f, .79f, .16f), false);
        foreach (var x in new[] { -HalfLength, HalfLength })
            AddTube(new(Basis.Identity, new(x, 0, 0)), .09f, BoreRadius, .78f, new("#fff8e9"), true);
        // Thin established-navy rails show the tube direction without hiding its contents.
        foreach (var z in new[] { -.70f, .70f })
            PartArt.Line(Visual, new(-HalfLength, 0, z), new(HalfLength, 0, z), new("#293954"), .018f);
    }
    private void AddTube(Transform3D pose, float halfLength, float inner, float outer, Color color, bool opaque)
    {
        Tubes.Add(new(pose, halfLength, inner, outer, opaque));
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
    }
}
