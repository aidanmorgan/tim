using Godot;
using System;
using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;

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
    private readonly struct ConeQuery : IDisposable
    {
        private readonly MachineWorld _world;
        private readonly MachinePart _part;
        private readonly PoseReadLease? _read;
        private readonly PhysicsBodyId _owner;
        private readonly double _simulationTime;
        internal Transform3D Transform { get; }
        internal ConeQuery(MachineWorld world,MachinePart part)
        {
            _world=world;_part=part;_owner=default;_read=null;_simulationTime=0;
            if(!world.HasPhysicsState)
            {
                Transform=WorldGeometry.CaptureSpatialState(world,new(part,MachinePart.RootBody)).Pose.ToScene();
                return;
            }
            var id=world.PhysicsAssembly.Body(new(part,MachinePart.RootBody)).Id;
            var read=world.ReadCommittedPoses();
            try
            {
                _simulationTime=world.DisplaySimulationTime;
                Transform=read.SampleAcceptedPose(id,_simulationTime).ToScene();
                _owner=read.ReadQuery(PoseSample.Current,id.Index).Owner;
                _read=read;
            }
            catch {read.Dispose();throw;}
        }
        internal float Trace(Vector3 origin,Vector3 direction,float range)=>_read is { } read
            ?(float)read.Trace(_simulationTime,TraceMedium.Light,SceneGeometryAdapter.CaptureVector(origin),
                SceneGeometryAdapter.CaptureVector(direction),range,_owner)
            :WorldGeometry.Trace(TraceMedium.Light,_world,origin,direction,range,_part);
        public void Dispose()=>_read?.Dispose();
    }
    public static ConeRay[] Sample(MachineWorld world, MachinePart part, LightEmitter source, float fraction)
    {
        using var query=new ConeQuery(world,part);
        return Sample(query,source,fraction);
    }
    private static ConeRay[] Sample(ConeQuery query,LightEmitter source,float fraction)
    {
        var axis = source.Direction.Normalized();
        var tangent = axis.Cross(Mathf.Abs(axis.Dot(Vector3.Up)) < .95f ? Vector3.Up : Vector3.Right).Normalized();
        var bitangent = axis.Cross(tangent);
        var angle = Mathf.Acos(source.ConeCosine) * fraction;
        var rays = new ConeRay[Sectors];
        var transform=query.Transform;
        for (var i = 0; i < Sectors; i++)
        {
            var azimuth = Mathf.Tau * i / Sectors;
            var direction = axis * Mathf.Cos(angle) +
                (tangent * Mathf.Cos(azimuth) + bitangent * Mathf.Sin(azimuth)) * Mathf.Sin(angle);
            var distance = query.Trace(transform * source.At,
                (transform.Basis * direction).Normalized(), source.Range);
            rays[i] = new(direction, distance);
        }
        return rays;
    }
    public void Refresh(MachineWorld world, MachinePart part, LightEmitter source)
    {
        using var query=new ConeQuery(world,part);
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
            var rays = Sample(query, source, (float)layer / ShellCount);
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
