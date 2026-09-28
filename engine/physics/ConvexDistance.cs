using System;

namespace CuriousContraptions.Physics;

public enum ConvexDistanceStatus { Separated, WithinTolerance }
public readonly record struct ConvexDistanceResult(ConvexDistanceStatus Status,double LowerBound,
    double UpperBound,CollisionVector PointA,CollisionVector PointB,CollisionVector Normal,int Iterations);

/// <summary>GJK distance bounds and witness points for any two support-mapped
/// convex shapes. No shape-pair dispatch. WithinTolerance does not distinguish
/// touching from penetration and must not be used as a penetration-depth query.</summary>
public static class ConvexDistance
{
    public const double DefaultTolerance=1e-7;
    private const int MaximumIterations=256;
    internal readonly record struct Vertex(CollisionVector A,CollisionVector B)
    {
        public CollisionVector Difference=>A-B;
    }

    public static ConvexDistanceResult Query<TA,TB>(TA a,TB b,double tolerance=DefaultTolerance)
        where TA : IConvexSupport where TB : IConvexSupport
    {
        if(!double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        Span<Vertex> simplex=stackalloc Vertex[4];
        simplex[0]=Support(a,b,new(1,0,0));
        var count=1;
        double lastLower=0,lastUpper=0;
        for(var iteration=1;iteration<=MaximumIterations;iteration++)
        {
            var projection=Reduce(simplex,ref count);
            var pointA=projection.A; var pointB=projection.B;
            var upper=(pointA-pointB).Length;
            if(!double.IsFinite(upper)) throw new InvalidOperationException("Convex distance exceeds numerical range.");
            if(upper<=tolerance)
                return new(ConvexDistanceStatus.WithinTolerance,0,upper,pointA,pointB,default,iteration);
            var directionLength=projection.Closest.Length;
            if(!double.IsFinite(directionLength)||directionLength==0)
                throw new InvalidOperationException("Simplex projection cannot resolve the witness direction.");
            var normal=projection.Closest/directionLength;
            var next=Support(a,b,-normal);
            // Certify the current normal as well as the distance interval.
            // A bound retained from another plane cannot certify this normal.
            var lower=Math.Max(0,CollisionVector.Dot(normal,next.Difference));
            lastLower=lower; lastUpper=upper;
            if(lower>upper+tolerance)
                throw new InvalidOperationException("Inconsistent convex distance bounds.");
            if(upper-lower<=tolerance)
                return new(ConvexDistanceStatus.Separated,Math.Min(lower,upper),upper,pointA,pointB,normal,iteration);
            if(count==4) throw new InvalidOperationException("Convex simplex exceeds available precision.");
            for(var i=0;i<count;i++)
                if(simplex[i].Difference==next.Difference)
                    throw new InvalidOperationException("Convex distance stalled before its error bound was met.");
            simplex[count++]=next;
        }
        throw new InvalidOperationException($"Convex distance did not converge (lower={lastLower:R}, upper={lastUpper:R}); separation was not inferred.");
    }

    internal static Vertex Support<TA,TB>(TA a,TB b,CollisionVector direction)
        where TA : IConvexSupport where TB : IConvexSupport =>
        new(a.Support(direction),b.Support(-direction));

    // Project the origin onto every simplex feature. A rank-deficient affine
    // feature has the same hull as lower-dimensional features already visited.
    // Feasible barycentric weights provide witnesses inside both input shapes.
    internal static (CollisionVector A,CollisionVector B,CollisionVector Closest) Reduce(Span<Vertex> simplex,ref int count)
    {
        Span<double> bestWeights=stackalloc double[4];
        Span<double> weights=stackalloc double[4];
        Span<int> indices=stackalloc int[4];
        Span<CollisionVector> edges=stackalloc CollisionVector[3];
        Span<double> rhs=stackalloc double[3];
        var best=double.PositiveInfinity;
        CollisionVector closest=default;
        for(var mask=1;mask<(1<<count);mask++)
        {
            var size=0;
            for(var i=0;i<count;i++) if((mask&(1<<i))!=0) indices[size++]=i;
            weights.Clear(); rhs.Clear();
            var origin=simplex[indices[0]].Difference;
            var dimensions=size-1;
            for(var i=0;i<dimensions;i++) edges[i]=simplex[indices[i+1]].Difference-origin;
            if(!ProjectCoordinates(edges,origin,rhs,dimensions)) continue;
            double first=1;
            var feasible=true;
            for(var i=0;i<dimensions;i++)
            {
                if(rhs[i]<0) { feasible=false; break; }
                first-=rhs[i]; weights[indices[i+1]]=rhs[i];
            }
            if(!feasible||first<0) continue;
            weights[indices[0]]=first;
            CollisionVector point=default;
            for(var i=0;i<count;i++) point+=simplex[i].Difference*weights[i];
            var squared=point.LengthSquared;
            if(squared>=best) continue;
            best=squared; weights.CopyTo(bestWeights);
            closest=AffineClosest(edges,origin,dimensions);
        }
        if(!double.IsFinite(best)) throw new InvalidOperationException("Convex simplex has no finite closest feature.");
        CollisionVector pointA=default,pointB=default;
        var retained=0;
        for(var i=0;i<count;i++)
        {
            var weight=bestWeights[i];
            if(weight<=0) continue;
            pointA+=simplex[i].A*weight; pointB+=simplex[i].B*weight;
            simplex[retained++]=simplex[i];
        }
        count=retained;
        return (pointA,pointB,closest);
    }

    private static CollisionVector AffineClosest(ReadOnlySpan<CollisionVector> edges,CollisionVector origin,int dimensions)
    {
        // Search along the affine feature's geometric normal. Subtracting two
        // reconstructed world witnesses can destroy tiny tangential components
        // and repeatedly select the wrong support corner near a broad flat face.
        if(dimensions==0) return origin;
        if(dimensions==3) return default;
        if(dimensions==1)
        {
            var axis=edges[0]/edges[0].Length;
            return CollisionVector.Cross(axis,CollisionVector.Cross(origin,axis));
        }
        var normal=CollisionVector.Cross(edges[0],edges[1]); normal/=normal.Length;
        return normal*CollisionVector.Dot(normal,origin);
    }

    private static bool ProjectCoordinates(ReadOnlySpan<CollisionVector> edges,CollisionVector origin,Span<double> weights,int size)
    {
        // QR with re-orthogonalization avoids squaring the condition number of
        // skinny simplices, as normal equations E^T E would do.
        Span<CollisionVector> basis=stackalloc CollisionVector[3];
        Span<double> upper=stackalloc double[9]; upper.Clear();
        for(var col=0;col<size;col++)
        {
            var vector=edges[col];
            for(var pass=0;pass<2;pass++)
            for(var row=0;row<col;row++)
            {
                var projection=CollisionVector.Dot(basis[row],vector);
                upper[row*3+col]+=projection; vector-=basis[row]*projection;
            }
            var length=vector.Length;
            if(length==0||!double.IsFinite(length)) return false;
            basis[col]=vector/length; upper[col*3+col]=length;
            weights[col]=-CollisionVector.Dot(basis[col],origin);
        }
        for(var row=size-1;row>=0;row--)
        {
            var value=weights[row];
            for(var col=row+1;col<size;col++) value-=upper[row*3+col]*weights[col];
            weights[row]=value/upper[row*3+row];
            if(!double.IsFinite(weights[row])) return false;
        }
        return true;
    }
}
