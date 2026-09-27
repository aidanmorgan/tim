using Godot;

namespace CuriousContraptions;

public static class PipeArt
{
    public static MeshInstance3D Frustum(Node3D parent,float halfLength,float inlet,float outlet,float thickness,Color color)
    {
        const int sides=48;
        var mesh=new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        Vector3 Point(float x,float radius,int step)=>new(x,Mathf.Cos(step*Mathf.Tau/sides)*radius,Mathf.Sin(step*Mathf.Tau/sides)*radius);
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
        {
            mesh.SurfaceSetNormal(normal);
            foreach(var vertex in new[]{a,b,c,a,c,d})mesh.SurfaceAddVertex(vertex);
        }
        var slope=(inlet-outlet)/(2*halfLength);
        for(var i=0;i<sides;i++)
        {
            var normal=(Vector3.Right*slope+Point(0,1,i)).Normalized();
            Quad(Point(-halfLength,inlet+thickness,i),Point(halfLength,outlet+thickness,i),
                Point(halfLength,outlet+thickness,i+1),Point(-halfLength,inlet+thickness,i+1),normal);
            Quad(Point(-halfLength,inlet,i+1),Point(halfLength,outlet,i+1),
                Point(halfLength,outlet,i),Point(-halfLength,inlet,i),-normal);
            Quad(Point(-halfLength,inlet,i),Point(-halfLength,inlet+thickness,i),
                Point(-halfLength,inlet+thickness,i+1),Point(-halfLength,inlet,i+1),Vector3.Left);
            Quad(Point(halfLength,outlet,i),Point(halfLength,outlet+thickness,i),
                Point(halfLength,outlet+thickness,i+1),Point(halfLength,outlet,i+1),Vector3.Right);
        }
        mesh.SurfaceEnd();
        var node=PartArt.Mesh(parent,mesh,color);
        var material=(StandardMaterial3D)node.MaterialOverride;
        material.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        material.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;
        node.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
        return node;
    }

    public static MeshInstance3D Cylinder(Node3D parent, Transform3D pose, float halfLength, float inner, float outer, Color color, bool opaque)
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
        var node = PartArt.Mesh(parent, mesh, color);
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
