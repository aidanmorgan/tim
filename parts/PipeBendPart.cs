using Godot;
using System.Collections.Generic;

namespace CuriousContraptions;

public partial class PipeBendPart : MachinePart, ITubePart
{
    [Export] public TubeBendAngle BendAngle { get; set; } = TubeBendAngle.Degrees90;
    public const float CentrelineRadius = 2.4f;
    public float Sweep => Mathf.DegToRad((int)BendAngle);
    public Vector3 ArcOffset => -new Vector3(Mathf.Sin(Sweep * .5f), Mathf.Cos(Sweep * .5f), 0) * CentrelineRadius;
    public override float SurfaceBounce => .15f;
    public IEnumerable<TubeMouth> Mouths
    {
        get
        {
            yield return Mouth(TubeMouthId.Start, 0, -1);
            yield return Mouth(TubeMouthId.End, Sweep, 1);
        }
    }
    private Vector3 Centre(float angle) => BendProxy.Radial(angle) * CentrelineRadius + ArcOffset;
    private TubeMouth Mouth(TubeMouthId id, float angle, float sign)
    {
        var normal = BendProxy.Tangent(angle) * sign;
        return new(id, Centre(angle) + normal * .09f, normal, PipePart.BoreRadius);
    }
    protected override void Build()
    {
        if (!System.Enum.IsDefined(BendAngle)) throw new System.ArgumentException("Unsupported tube bend angle.");
        Bends.Add(new(new Transform3D(Basis.Identity, ArcOffset), CentrelineRadius, Sweep, PipePart.BoreRadius, .70f));
        foreach (var angle in new[] { 0f, Sweep })
        {
            var pose = new Transform3D(new Basis(BendProxy.Tangent(angle), BendProxy.Radial(angle), Vector3.Back), Centre(angle));
            PipeArt.Cylinder(Visual, pose, .09f, PipePart.BoreRadius, .78f, new("#fff8e9"), true);
            Tubes.Add(new(pose, .09f, PipePart.BoreRadius, .78f, true));
        }
        const int steps = 32, sides = 48;
        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        Vector3 Point(int along, int around, float radius)
        {
            var angle = Sweep * along / steps;
            var ring = Mathf.Tau * around / sides;
            return Centre(angle) + radius * (BendProxy.Radial(angle) * Mathf.Cos(ring) + Vector3.Back * Mathf.Sin(ring));
        }
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            mesh.SurfaceSetNormal(normal);
            foreach (var point in new[] { a, b, c, a, c, d }) mesh.SurfaceAddVertex(point);
        }
        for (var along = 0; along < steps; along++)
        {
            foreach (var z in new[] { -.70f, .70f })
                PartArt.Line(Visual, Centre(Sweep * along / steps) + Vector3.Back * z,
                    Centre(Sweep * (along + 1) / steps) + Vector3.Back * z, new("#293954"), .018f);
            for (var around = 0; around < sides; around++)
                foreach (var radius in new[] { PipePart.BoreRadius, .70f })
                {
                    var normal = (Point(along, around, radius) - Centre(Sweep * along / steps)).Normalized();
                    if (radius == PipePart.BoreRadius) normal = -normal;
                    Quad(Point(along, around, radius), Point(along + 1, around, radius),
                        Point(along + 1, around + 1, radius), Point(along, around + 1, radius), normal);
                }
        }
        mesh.SurfaceEnd();
        var shell = PartArt.Mesh(Visual, mesh, new(.40f, .72f, .79f, .16f));
        var material = (StandardMaterial3D)shell.MaterialOverride;
        material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        shell.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        UpdateSelectionRadius(2.2f);
    }
}
