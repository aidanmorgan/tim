using System;
using System.Collections.Generic;
using Vertex=CuriousContraptions.Physics.ConvexDistance.Vertex;

namespace CuriousContraptions.Physics;

public enum ConvexPenetrationStatus { Separated, WithinTolerance, Penetrating }
public readonly record struct ConvexPenetrationResult(ConvexPenetrationStatus Status,double LowerDepth,
    double UpperDepth,CollisionVector Normal,CollisionVector PointA,CollisionVector PointB,int Iterations);

/// <summary>Support-mapped expanding-polytope penetration query. Normal points
/// from B toward A's separating direction. Witnesses remain in their respective
/// convex shapes. Exhausted precision/budgets throw, never imply nonintersection.</summary>
public static class ConvexPenetration
{
    private const int MaximumIterations=512;
    private const int MaximumFaces=2048;
    private readonly record struct Face(int A,int B,int C,CollisionVector Normal,double Distance);
    private readonly record struct Edge(int A,int B);

    public static ConvexPenetrationResult Query<TA,TB>(TA a,TB b,double tolerance=ConvexDistance.DefaultTolerance)
        where TA:IConvexSupport where TB:IConvexSupport
    {
        var distance=ConvexDistance.Query(a,b,tolerance);
        if(distance.Status==ConvexDistanceStatus.Separated)
            return new(ConvexPenetrationStatus.Separated,0,0,distance.Normal,distance.PointA,distance.PointB,0);
        var ballA=a.InteriorBall; var ballB=b.InteriorBall;
        var offset=ballA.Center-ballB.Center;
        var length=offset.Length;
        if(!double.IsFinite(length)) throw new InvalidOperationException("Interior bounds exceed numeric range.");
        var direction=length>0?-offset/length:new CollisionVector(1,0,0);
        var lower=Math.Max(0,ballA.Radius+ballB.Radius-length);
        var upper=CollisionVector.Dot(direction,ConvexDistance.Support(a,b,direction).Difference);
        if(lower>upper+tolerance) throw new InvalidOperationException("Interior ball is inconsistent with support geometry.");
        if(upper-lower<=tolerance&&lower>tolerance)
            return new(ConvexPenetrationStatus.Penetrating,lower,Math.Max(lower,upper),-direction,
                ballA.Center+direction*ballA.Radius,ballB.Center-direction*ballB.Radius,0);

        var vertices=new List<Vertex>();
        void Add(CollisionVector n)
        {
            var v=ConvexDistance.Support(a,b,n);
            if(!v.Difference.IsFinite) throw new InvalidOperationException("Support exceeds numeric range.");
            foreach(var prior in vertices) if(prior.Difference==v.Difference) return;
            vertices.Add(v);
        }
        foreach(var axis in new[]{new CollisionVector(1,0,0),new(0,1,0),new(0,0,1)})
        { Add(axis); Add(-axis); }
        var first=0; var second=0; double span=0;
        for(var i=0;i<vertices.Count;i++)
        for(var j=i+1;j<vertices.Count;j++)
        {
            var squared=(vertices[i].Difference-vertices[j].Difference).LengthSquared;
            if(squared>span) { span=squared; first=i; second=j; }
        }
        if(!double.IsFinite(span)) throw new InvalidOperationException("Polytope scale exceeds numeric range.");
        if(span==0)
        {
            // A singleton Minkowski difference has no unique normal at the
            // origin. Every direction is supporting there; choose a stable
            // unit axis so an exact rounding radius can restore its surface.
            var delta=distance.PointA-distance.PointB;
            return Touch(distance,delta.Length>0?delta/delta.Length:new CollisionVector(-1,0,0),0);
        }
        var edge=(vertices[second].Difference-vertices[first].Difference)/Math.Sqrt(span);
        var seed=Math.Abs(edge.X)<.5773502691896258?new CollisionVector(1,0,0):
            Math.Abs(edge.Y)<.5773502691896258?new CollisionVector(0,1,0):new CollisionVector(0,0,1);
        var perpendicular=CollisionVector.Cross(edge,seed);
        perpendicular/=perpendicular.Length;
        Add(perpendicular); Add(-perpendicular);
        var other=CollisionVector.Cross(edge,perpendicular); Add(other); Add(-other);
        var third=first; double area=0;
        for(var i=0;i<vertices.Count;i++)
        {
            var squared=CollisionVector.Cross(edge,vertices[i].Difference-vertices[first].Difference).LengthSquared;
            if(squared>area) { area=squared; third=i; }
        }
        if(area==0) return Touch(distance,-perpendicular,0);
        var normal=CollisionVector.Cross(edge,vertices[third].Difference-vertices[first].Difference);
        normal/=normal.Length;
        Add(normal); Add(-normal);
        var fourth=first; double height=0;
        for(var i=0;i<vertices.Count;i++)
        {
            var value=Math.Abs(CollisionVector.Dot(normal,vertices[i].Difference-vertices[first].Difference));
            if(value>height) { height=value; fourth=i; }
        }
        if(height==0) return Touch(distance,-normal,0);
        var seedVertices=new[]{vertices[first],vertices[second],vertices[third],vertices[fourth]};
        vertices=new(seedVertices);
        var interior=(vertices[0].Difference+vertices[1].Difference+vertices[2].Difference+vertices[3].Difference)*.25;
        var faces=new List<Face>
        {
            CreateFace(0,1,2,vertices,interior),CreateFace(0,3,1,vertices,interior),
            CreateFace(0,2,3,vertices,interior),CreateFace(1,3,2,vertices,interior)
        };
        for(var iteration=1;iteration<=MaximumIterations;iteration++)
        {
            var closest=faces[0];
            foreach(var face in faces) if(face.Distance<closest.Distance) closest=face;
            var support=ConvexDistance.Support(a,b,closest.Normal);
            upper=CollisionVector.Dot(closest.Normal,support.Difference);
            if(!double.IsFinite(upper)||upper<closest.Distance-tolerance)
                throw new InvalidOperationException("Inconsistent penetration bounds.");
            if(upper < -tolerance) throw new InvalidOperationException("Penetration support plane contradicts the overlap query.");
            if(upper<=tolerance) return Touch(distance,-closest.Normal,Math.Max(0,upper),iteration);
            if(closest.Distance>=0&&upper-closest.Distance<=tolerance)
            {
                var witness=Witness(vertices,faces,closest.Normal*closest.Distance,tolerance);
                return new(closest.Distance>tolerance?ConvexPenetrationStatus.Penetrating:ConvexPenetrationStatus.WithinTolerance,
                    Math.Max(0,closest.Distance),Math.Max(0,upper),-closest.Normal,witness.A,witness.B,iteration);
            }
            Expand(vertices,faces,support,interior,tolerance);
            if(faces.Count>MaximumFaces) throw new InvalidOperationException("Penetration polytope exceeded its face budget.");
        }
        throw new InvalidOperationException("Penetration bounds did not converge.");
    }

    private static ConvexPenetrationResult Touch(ConvexDistanceResult distance,CollisionVector normal,double upper,int iterations=0)=>
        new(ConvexPenetrationStatus.WithinTolerance,0,upper,normal,distance.PointA,distance.PointB,iterations);

    private static Face CreateFace(int a,int b,int c,List<Vertex> vertices,CollisionVector interior)
    {
        var p=vertices[a].Difference;
        var normal=CollisionVector.Cross(vertices[b].Difference-p,vertices[c].Difference-p);
        var length=normal.Length;
        if(!double.IsFinite(length)||length==0) throw new InvalidOperationException("Penetration hull contains a degenerate facet.");
        normal/=length;
        if(CollisionVector.Dot(normal,interior-p)>0) { (b,c)=(c,b); normal=-normal; }
        var distance=CollisionVector.Dot(normal,p);
        if(!double.IsFinite(distance)) throw new InvalidOperationException("Penetration facet exceeds numeric range.");
        return new(a,b,c,normal,distance);
    }

    private static void Expand(List<Vertex> vertices,List<Face> faces,Vertex support,CollisionVector interior,double tolerance)
    {
        var horizon=new List<Edge>();
        void AddEdge(int a,int b)
        {
            var reversed=horizon.IndexOf(new(b,a));
            if(reversed>=0) horizon.RemoveAt(reversed); else horizon.Add(new(a,b));
        }
        for(var i=faces.Count-1;i>=0;i--)
        {
            var face=faces[i];
            if(CollisionVector.Dot(face.Normal,support.Difference)-face.Distance<=tolerance*.001) continue;
            AddEdge(face.A,face.B); AddEdge(face.B,face.C); AddEdge(face.C,face.A);
            faces.RemoveAt(i);
        }
        if(horizon.Count==0) throw new InvalidOperationException("Penetration hull stalled before meeting its error bound.");
        var index=vertices.Count; vertices.Add(support);
        foreach(var edge in horizon) faces.Add(CreateFace(edge.A,edge.B,index,vertices,interior));
    }

    private static (CollisionVector A,CollisionVector B) Witness(List<Vertex> vertices,List<Face> faces,
        CollisionVector target,double tolerance)
    {
        // Coplanar facets may triangulate one support polygon. Find the triangle
        // containing the projected point, rather than extrapolating its weights.
        Span<Vertex> triangle=stackalloc Vertex[3];
        double best=double.PositiveInfinity; CollisionVector pointA=default,pointB=default;
        foreach(var face in faces)
        {
            var va=vertices[face.A]; var vb=vertices[face.B]; var vc=vertices[face.C];
            triangle[0]=new(va.A-target,va.B); triangle[1]=new(vb.A-target,vb.B); triangle[2]=new(vc.A-target,vc.B);
            var count=3;
            var witness=ConvexDistance.Reduce(triangle,ref count);
            var error=(witness.A-witness.B).Length;
            if(error>=best) continue;
            best=error; pointA=witness.A+target; pointB=witness.B;
        }
        if(best>tolerance) throw new InvalidOperationException("Penetration witnesses do not meet the facet error bound.");
        return (pointA,pointB);
    }
}
