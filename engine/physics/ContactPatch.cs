using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

public readonly record struct ContactPatchPoint(CollisionVector PointA,CollisionVector PointB,double Separation);

/// <summary>Convex contact-feature intersection in a shared tangent plane.
/// All anchors are convex combinations of the supplied surface vertices.
/// Returns the entire patch, including point/edge contacts, without shape dispatch.</summary>
public sealed class ContactPatch
{
    private readonly ContactPatchPoint[] _points;
    public CollisionVector Normal { get; }
    public ReadOnlySpan<ContactPatchPoint> Points=>_points;
    private ContactPatch(CollisionVector normal,ContactPatchPoint[] points) { Normal=normal; _points=points; }

    private readonly record struct PlanePoint(double X,double Y)
    {
        public double LengthSquared=>X*X+Y*Y;
        public static PlanePoint operator +(PlanePoint a,PlanePoint b)=>new(a.X+b.X,a.Y+b.Y);
        public static PlanePoint operator -(PlanePoint a,PlanePoint b)=>new(a.X-b.X,a.Y-b.Y);
        public static PlanePoint operator *(PlanePoint a,double s)=>new(a.X*s,a.Y*s);
    }
    private readonly record struct Vertex(PlanePoint Plane,CollisionVector World,CollisionVector PairedWorld,SupportVertexId Id);
    private static double Cross(PlanePoint a,PlanePoint b)=>a.X*b.Y-a.Y*b.X;
    private static double Dot(PlanePoint a,PlanePoint b)=>a.X*b.X+a.Y*b.Y;

    public static ContactPatch Clip(SupportFeature a,SupportFeature b,CollisionVector normal,
        double tolerance=ConvexDistance.DefaultTolerance)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        normal=SupportFeature.UnitDirection(normal,tolerance);
        if(tolerance==0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        var origin=a.Vertices[0].Point;
        var axis=Math.Abs(normal.X)<Math.Abs(normal.Y)?
            (Math.Abs(normal.X)<Math.Abs(normal.Z)?new CollisionVector(1,0,0):new(0,0,1)):
            (Math.Abs(normal.Y)<Math.Abs(normal.Z)?new CollisionVector(0,1,0):new(0,0,1));
        var tangent=CollisionVector.Cross(normal,axis); tangent/=tangent.Length;
        var bitangent=CollisionVector.Cross(normal,tangent);
        var polygonA=Project(a,origin,normal,tangent,bitangent,tolerance);
        var polygonB=Project(b,origin,normal,tangent,bitangent,tolerance);
        var contacts=new List<ContactPatchPoint>();
        void Add(CollisionVector pointA,CollisionVector pointB)
        {
            var delta=pointA-pointB;
            var separation=CollisionVector.Dot(normal,delta);
            if(!double.IsFinite(separation)||(delta-normal*separation).Length>tolerance*2)
                throw new InvalidOperationException("Contact anchors exceed the tangent error bound.");
            foreach(var contact in contacts)
                if((contact.PointA-pointA).Length<=tolerance&&(contact.PointB-pointB).Length<=tolerance) return;
            contacts.Add(new(pointA,pointB,separation));
        }
        foreach(var vertex in polygonA)
            if(Lift(polygonB,vertex.Plane,tolerance,out var point)) Add(vertex.World,point);
        foreach(var vertex in polygonB)
            if(Lift(polygonA,vertex.Plane,tolerance,out var point)) Add(point,vertex.World);
        for(var i=0;i<EdgeCount(polygonA);i++)
        for(var j=0;j<EdgeCount(polygonB);j++)
        {
            var a0=polygonA[i]; var a1=polygonA[(i+1)%polygonA.Count];
            var b0=polygonB[j]; var b1=polygonB[(j+1)%polygonB.Count];
            var da=a1.Plane-a0.Plane; var db=b1.Plane-b0.Plane;
            var determinant=Cross(da,db);
            // Collinear overlaps are bounded by contained endpoints, already
            // handled above. No arbitrary normal or artificial thickness.
            if(determinant==0) continue;
            var offset=b0.Plane-a0.Plane;
            var ta=Cross(offset,db)/determinant; var tb=Cross(offset,da)/determinant;
            if(ta<0||ta>1||tb<0||tb>1) continue;
            Add(a0.World+(a1.World-a0.World)*ta,b0.World+(b1.World-b0.World)*tb);
        }
        if(contacts.Count>2)
        {
            var boundary=new List<Vertex>();
            for(var i=0;i<contacts.Count;i++)
            {
                var point=contacts[i]; var delta=point.PointA-origin;
                boundary.Add(new(new(CollisionVector.Dot(delta,tangent),CollisionVector.Dot(delta,bitangent)),
                    point.PointA,point.PointB,new(i)));
            }
            var reduced=new List<ContactPatchPoint>();
            foreach(var vertex in Hull(boundary,tolerance)) reduced.Add(contacts[vertex.Id.Value]);
            contacts=reduced;
        }
        contacts.Sort((x,y)=>
        {
            var dx=x.PointA-origin; var dy=y.PointA-origin;
            var first=CollisionVector.Dot(dx,tangent).CompareTo(CollisionVector.Dot(dy,tangent));
            return first!=0?first:CollisionVector.Dot(dx,bitangent).CompareTo(CollisionVector.Dot(dy,bitangent));
        });
        return new(normal,contacts.ToArray());
    }

    private static int EdgeCount(List<Vertex> polygon)=>polygon.Count<2?0:polygon.Count==2?1:polygon.Count;

    private static List<Vertex> Project(SupportFeature feature,CollisionVector origin,CollisionVector normal,
        CollisionVector tangent,CollisionVector bitangent,double tolerance)
    {
        var vertices=new List<Vertex>();
        double minimum=double.PositiveInfinity,maximum=double.NegativeInfinity;
        foreach(var vertex in feature.Vertices)
        {
            var delta=vertex.Point-origin;
            var height=CollisionVector.Dot(delta,normal);
            var point=new PlanePoint(CollisionVector.Dot(delta,tangent),CollisionVector.Dot(delta,bitangent));
            if(!double.IsFinite(height)||!double.IsFinite(point.LengthSquared))
                throw new InvalidOperationException("Contact projection exceeds numeric range.");
            minimum=Math.Min(minimum,height); maximum=Math.Max(maximum,height);
            vertices.Add(new(point,vertex.Point,vertex.Point,vertex.Id));
        }
        if(maximum-minimum>tolerance)
            throw new ArgumentException("Supporting feature exceeds its plane tolerance.",nameof(feature));
        return Hull(vertices,tolerance);
    }

    private static List<Vertex> Hull(List<Vertex> vertices,double tolerance)
    {
        vertices.Sort((a,b)=>
        {
            var x=a.Plane.X.CompareTo(b.Plane.X); if(x!=0) return x;
            var y=a.Plane.Y.CompareTo(b.Plane.Y); return y!=0?y:a.Id.Value.CompareTo(b.Id.Value);
        });
        var unique=new List<Vertex>();
        foreach(var vertex in vertices)
            if(unique.Count==0||vertex.Plane!=unique[^1].Plane) unique.Add(vertex);
        if(unique.Count<3) return unique;
        var hull=new List<Vertex>();
        foreach(var vertex in unique)
        {
            while(hull.Count>=2&&Cross(hull[^1].Plane-hull[^2].Plane,vertex.Plane-hull[^1].Plane)<=0)
                hull.RemoveAt(hull.Count-1);
            hull.Add(vertex);
        }
        var lower=hull.Count;
        for(var i=unique.Count-2;i>=0;i--)
        {
            var vertex=unique[i];
            while(hull.Count>lower&&Cross(hull[^1].Plane-hull[^2].Plane,vertex.Plane-hull[^1].Plane)<=0)
                hull.RemoveAt(hull.Count-1);
            hull.Add(vertex);
        }
        hull.RemoveAt(hull.Count-1);
        return Simplify(hull,tolerance);
    }

    private static List<Vertex> Simplify(List<Vertex> hull,double tolerance)
    {
        // Remove numerically redundant boundary samples only when every original
        // vertex on the replaced arc is within tolerance of the retained chord.
        // Checking the entire original arc prevents accumulated simplification error.
        var kept=new List<int>();
        for(var i=0;i<hull.Count;i++) kept.Add(i);
        var changed=true;
        while(changed&&kept.Count>2)
        {
            changed=false;
            for(var k=0;k<kept.Count;k++)
            {
                var previous=kept[(k+kept.Count-1)%kept.Count]; var next=kept[(k+1)%kept.Count];
                var a=hull[previous]; var b=hull[next]; var edge=b.Plane-a.Plane;
                if(edge.LengthSquared==0) continue;
                var valid=true;
                for(var i=(previous+1)%hull.Count;i!=next;i=(i+1)%hull.Count)
                {
                    var t=Math.Clamp(Dot(hull[i].Plane-a.Plane,edge)/edge.LengthSquared,0,1);
                    if((hull[i].World-(a.World+(b.World-a.World)*t)).Length>tolerance||
                        (hull[i].PairedWorld-(a.PairedWorld+(b.PairedWorld-a.PairedWorld)*t)).Length>tolerance)
                    { valid=false; break; }
                }
                if(!valid) continue;
                kept.RemoveAt(k); changed=true; break;
            }
        }
        var result=new List<Vertex>();
        foreach(var index in kept) result.Add(hull[index]);
        return result;
    }

    private static bool Lift(List<Vertex> polygon,PlanePoint target,double tolerance,out CollisionVector world)
    {
        // Evaluate feasible convex combinations on every simplex dimension.
        // Reconstruction error, not an ill-conditioned barycentric solve alone,
        // certifies that a point belongs to the projected feature.
        var best=double.PositiveInfinity;
        world=default;
        if(polygon.Count>=3)
        {
            var first=polygon[0];
            for(var i=1;i<polygon.Count-1;i++)
            {
                var second=polygon[i]; var third=polygon[i+1];
                var u=second.Plane-first.Plane; var v=third.Plane-first.Plane; var w=target-first.Plane;
                var determinant=Cross(u,v);
                // A zero-area triangle has no 2D interior; its segments are
                // tested below. Barycentric coordinates work in either winding.
                if(determinant==0) continue;
                var b=Cross(w,v)/determinant; var c=Cross(u,w)/determinant; var a=1-b-c;
                if(a<0||b<0||c<0) continue;
                var reconstructed=first.Plane*a+second.Plane*b+third.Plane*c;
                var squared=(target-reconstructed).LengthSquared;
                if(squared>=best) continue;
                best=squared; world=first.World*a+second.World*b+third.World*c;
            }
        }
        foreach(var vertex in polygon)
        {
            var squared=(target-vertex.Plane).LengthSquared;
            if(squared>=best) continue;
            best=squared; world=vertex.World;
        }
        for(var i=0;i<EdgeCount(polygon);i++)
        {
            var first=polygon[i]; var second=polygon[(i+1)%polygon.Count];
            var edge=second.Plane-first.Plane;
            var t=Math.Clamp(Dot(target-first.Plane,edge)/edge.LengthSquared,0,1);
            var squared=(target-(first.Plane+edge*t)).LengthSquared;
            if(squared>=best) continue;
            best=squared; world=first.World+(second.World-first.World)*t;
        }
        return best<=tolerance*tolerance;
    }
}
