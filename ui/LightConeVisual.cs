using Godot;

namespace CuriousContraptions;

public readonly record struct ConeRay(Vector3 Direction, float Distance);

/// <summary>Render-only layered cone. Ray clipping uses the same opaque proxies as illumination.
/// Sampling approximates curved silhouettes; this is not volumetric scattering or the power solver.</summary>
public partial class LightConeVisual : MeshInstance3D
{
    public const int Sectors = 48;
    public const int ShellCount = 4;
    private static readonly Color BeamColor = new("#fff0a5");
    private readonly ImmediateMesh _surface = new();
    private readonly StandardMaterial3D _material = new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        VertexColorUseAsAlbedo = true,
        AlbedoColor = Colors.White
    };
    public override void _Ready()
    {
        Mesh = _surface;
        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
    }
    public static ConeRay[] Sample(MachineWorld world, MachinePart part, LightEmitter source, float fraction)
    {
        var axis = source.Direction.Normalized();
        var tangent = axis.Cross(Mathf.Abs(axis.Dot(Vector3.Up)) < .95f ? Vector3.Up : Vector3.Right).Normalized();
        var bitangent = axis.Cross(tangent);
        var angle = Mathf.Acos(source.ConeCosine) * fraction;
        var rays = new ConeRay[Sectors];
        for (var i = 0; i < Sectors; i++)
        {
            var azimuth = Mathf.Tau * i / Sectors;
            var direction = axis * Mathf.Cos(angle) +
                (tangent * Mathf.Cos(azimuth) + bitangent * Mathf.Sin(azimuth)) * Mathf.Sin(angle);
            var distance = WorldGeometry.Trace(TraceMedium.Light,world, part.Transform * source.At,
                (part.Basis * direction).Normalized(), source.Range, part);
            rays[i] = new(direction, distance);
        }
        return rays;
    }
    public void Refresh(MachineWorld world, MachinePart part, LightEmitter source)
    {
        _surface.ClearSurfaces();
        _surface.SurfaceBegin(Godot.Mesh.PrimitiveType.Triangles, _material);
        void Vertex(Vector3 point, float alpha)
        {
            var color = BeamColor;
            color.A = alpha;
            _surface.SurfaceSetColor(color);
            _surface.SurfaceAddVertex(point);
        }
        for (var layer = 1; layer <= ShellCount; layer++)
        {
            var rays = Sample(world, part, source, (float)layer / ShellCount);
            var alpha = .065f * (1 - .7f * layer / ShellCount);
            for (var i = 0; i < Sectors; i++)
            {
                var a = rays[i];
                var b = rays[(i + 1) % Sectors];
                // Use the nearer sampled depth to avoid long triangles across silhouette depth jumps.
                var distance = Mathf.Min(a.Distance, b.Distance);
                var middle = distance * .72f;
                var nearA = source.At + a.Direction * middle;
                var nearB = source.At + b.Direction * middle;
                var farA = source.At + a.Direction * distance;
                var farB = source.At + b.Direction * distance;
                Vertex(source.At, alpha);
                Vertex(nearA, alpha * .75f);
                Vertex(nearB, alpha * .75f);
                var endAlpha = distance < source.Range - .001f ? alpha * .5f : 0;
                Vertex(nearA, alpha * .75f); Vertex(farA, endAlpha); Vertex(farB, endAlpha);
                Vertex(nearA, alpha * .75f); Vertex(farB, endAlpha); Vertex(nearB, alpha * .75f);
            }
        }
        _surface.SurfaceEnd();
    }
}
