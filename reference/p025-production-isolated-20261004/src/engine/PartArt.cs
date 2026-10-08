using Godot;

namespace CuriousContraptions;

public static class PartArt
{
    public static StandardMaterial3D Material(Color color, float metal = 0) =>
        new() { AlbedoColor = color, Roughness = .48f, Metallic = metal };

    public static MeshInstance3D Mesh(Node3D parent, Mesh shape, Color color, Vector3 at = default)
    {
        var node = new MeshInstance3D { Mesh = shape, MaterialOverride = Material(color), Position = at };
        parent.AddChild(node);
        return node;
    }
    public static MeshInstance3D Box(Node3D parent, Vector3 size, Color color, Vector3 at = default) =>
        Mesh(parent, new BoxMesh { Size = size }, color, at);
    public static MeshInstance3D Sphere(Node3D parent, float radius, Color color, Vector3 at = default) =>
        Mesh(parent, new SphereMesh { Radius = radius, Height = radius * 2, RadialSegments = 24, Rings = 12 }, color, at);
    public static MeshInstance3D Cylinder(Node3D parent, float radius, float height, Color color, Vector3 at = default) =>
        Mesh(parent, new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 24 }, color, at);
    public static MeshInstance3D Ring(Node3D parent, float radius, float thickness, Color color, Vector3 at = default) =>
        Mesh(parent, new TorusMesh { InnerRadius = radius - thickness, OuterRadius = radius + thickness, Rings = 32, RingSegments = 8 }, color, at);
    public static MeshInstance3D Line(Node3D parent, Vector3 start, Vector3 end, Color color, float width = .035f)
    {
        var offset = end - start;
        var node = Cylinder(parent, width, Mathf.Max(offset.Length(), .001f), color, (start + end) * .5f);
        if (offset.LengthSquared() > .00001f) node.Quaternion = new Quaternion(Vector3.Up, offset.Normalized());
        return node;
    }
}

