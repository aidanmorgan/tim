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
    private readonly record struct Vertex(CollisionVector A,CollisionVector B)
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
        double lower=0;
        for(var iteration=1;iteration<=MaximumIterations;iteration++)
        {
            var (pointA,pointB)=Reduce(simplex,ref count);
            var closest=pointA-pointB;
            var upper=closest.Length;
            if(!double.IsFinite(upper)) throw new InvalidOperationException("Convex distance exceeds numerical range.");
            if(upper<=tolerance)
                return new(ConvexDistanceStatus.WithinTolerance,0,upper,pointA,pointB,default,iteration);
            var normal=closest/upper;
            var next=Support(a,b,-normal);
            // This support plane separates the entire Minkowski difference,
            // not merely the current simplex. Retain the best known lower bound.
            lower=Math.Max(lower,Math.Max(0,CollisionVector.Dot(normal,next.Difference)));
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
        throw new InvalidOperationException("Convex distance did not converge; separation was not inferred.");
    }

    private static Vertex Support<TA,TB>(TA a,TB b,CollisionVector direction)
        where TA : IConvexSupport where TB : IConvexSupport =>
        new(a.Support(direction),b.Support(-direction));

    // Project the origin onto every simplex feature. A rank-deficient affine
    // feature has the same hull as lower-dimensional features already visited.
    // Feasible barycentric weights provide witnesses inside both input shapes.
    private static (CollisionVector A,CollisionVector B) Reduce(Span<Vertex> simplex,ref int count)
    {
        Span<double> bestWeights=stackalloc double[4];
        Span<double> weights=stackalloc double[4];
        Span<int> indices=stackalloc int[4];
        Span<CollisionVector> edges=stackalloc CollisionVector[3];
        Span<double> matrix=stackalloc double[9];
        Span<double> rhs=stackalloc double[3];
        var best=double.PositiveInfinity;
        for(var mask=1;mask<(1<<count);mask++)
        {
            var size=0;
            for(var i=0;i<count;i++) if((mask&(1<<i))!=0) indices[size++]=i;
            weights.Clear(); matrix.Clear(); rhs.Clear();
            var origin=simplex[indices[0]].Difference;
            var dimensions=size-1;
            for(var i=0;i<dimensions;i++) edges[i]=simplex[indices[i+1]].Difference-origin;
            for(var row=0;row<dimensions;row++)
            {
                rhs[row]=-CollisionVector.Dot(edges[row],origin);
                for(var col=0;col<dimensions;col++) matrix[row*3+col]=CollisionVector.Dot(edges[row],edges[col]);
            }
            if(!Solve(matrix,rhs,dimensions)) continue;
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
        return (pointA,pointB);
    }

    private static bool Solve(Span<double> matrix,Span<double> rhs,int size)
    {
        for(var col=0;col<size;col++)
        {
            var pivot=col;
            for(var row=col+1;row<size;row++)
                if(Math.Abs(matrix[row*3+col])>Math.Abs(matrix[pivot*3+col])) pivot=row;
            if(matrix[pivot*3+col]==0) return false;
            if(pivot!=col)
            {
                for(var j=0;j<size;j++) (matrix[pivot*3+j],matrix[col*3+j])=(matrix[col*3+j],matrix[pivot*3+j]);
                (rhs[pivot],rhs[col])=(rhs[col],rhs[pivot]);
            }
            var divisor=matrix[col*3+col];
            for(var j=col;j<size;j++) matrix[col*3+j]/=divisor;
            rhs[col]/=divisor;
            for(var row=0;row<size;row++)
            {
                if(row==col) continue;
                var factor=matrix[row*3+col];
                for(var j=col;j<size;j++) matrix[row*3+j]-=factor*matrix[col*3+j];
                rhs[row]-=factor*rhs[col];
            }
        }
        return true;
    }
}
