using Godot;
using System;

namespace CuriousContraptions;

/// <summary>Closed annular frustum along local X. Positive distance is free space;
/// negative distance is penetration into the sloping shell, including its end faces.</summary>
public readonly record struct FrustumProxy(Transform3D Pose,float HalfLength,float InletRadius,float OutletRadius,float Thickness)
{
    public (Vector3 Normal,float Distance) Surface(Vector3 point)
    {
        var radial=new Vector3(0,point.Y,point.Z);
        var radius=radial.Length();
        var direction=radius>.000001f?radial/radius:Vector3.Up;
        var p=new Vector2(point.X,radius);
        ReadOnlySpan<Vector2> vertices=[
            new(-HalfLength,InletRadius),new(HalfLength,OutletRadius),
            new(HalfLength,OutletRadius+Thickness),new(-HalfLength,InletRadius+Thickness)];
        var inside=true;
        var nearest=float.PositiveInfinity;
        var closest=Vector2.Zero;
        var edgeNormal=Vector2.Zero;
        for(var i=0;i<4;i++)
        {
            var a=vertices[i];
            var edge=vertices[(i+1)%4]-a;
            if(edge.Cross(p-a)<0)inside=false;
            var q=a+edge*Mathf.Clamp((p-a).Dot(edge)/edge.LengthSquared(),0,1);
            var distance=p.DistanceSquaredTo(q);
            if(distance>=nearest)continue;
            nearest=distance;closest=q;
            edgeNormal=new Vector2(edge.Y,-edge.X).Normalized();
        }
        var length=Mathf.Sqrt(nearest);
        var n=length>.000001f?(inside?closest-p:p-closest)/length:edgeNormal;
        return (Vector3.Right*n.X+direction*n.Y,inside?-length:length);
    }
}
