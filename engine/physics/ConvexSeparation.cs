using System;

namespace CuriousContraptions.Physics;

public enum ConvexSeparationStatus { Separated, WithinTolerance, Penetrating }
public readonly record struct ConvexSeparationResult(ConvexSeparationStatus Status,double LowerBound,double UpperBound,
    CollisionVector PointA,CollisionVector PointB,CollisionVector Normal,int Iterations);

/// <summary>Signed separation interval: positive outside, negative inside.
/// Exact Minkowski rounding is removed before the shared GJK/EPA query and
/// restored analytically for every caller, including sweeps and constraints.</summary>
public static class ConvexSeparation
{
    private readonly struct Core<T>(T shape) : IConvexSupport where T:IConvexFeatureSupport
    {
        public InteriorBall InteriorBall=>new(shape.InteriorBall.Center,shape.InteriorBall.Radius-shape.RoundingRadius);
        public CollisionVector Support(CollisionVector direction)
        {
            var normal=SupportFeature.UnitDirection(direction,0);
            return shape.Support(direction)-normal*shape.RoundingRadius;
        }
    }

    public static ConvexSeparationResult Query<TA,TB>(TA a,TB b,double tolerance=ConvexDistance.DefaultTolerance)
        where TA:IConvexFeatureSupport where TB:IConvexFeatureSupport
    {
        Validate(a.RoundingRadius,a.InteriorBall);
        Validate(b.RoundingRadius,b.InteriorBall);
        var core=QueryCore(new Core<TA>(a),new Core<TB>(b),tolerance);
        var radius=a.RoundingRadius+b.RoundingRadius;
        var lower=core.LowerBound-radius; var upper=core.UpperBound-radius;
        var status=lower>tolerance?ConvexSeparationStatus.Separated:
            upper < -tolerance?ConvexSeparationStatus.Penetrating:ConvexSeparationStatus.WithinTolerance;
        return new(status,lower,upper,core.PointA-core.Normal*a.RoundingRadius,
            core.PointB+core.Normal*b.RoundingRadius,core.Normal,core.Iterations);
    }

    private static void Validate(double radius,InteriorBall ball)
    {
        if(!double.IsFinite(radius)||radius<0||radius>ball.Radius)
            throw new ArgumentException("Rounding radius requires a finite contained interior ball.");
    }

    private static ConvexSeparationResult QueryCore<TA,TB>(TA a,TB b,double tolerance=ConvexDistance.DefaultTolerance)
        where TA:IConvexSupport where TB:IConvexSupport
    {
        var distance=ConvexDistance.Query(a,b,tolerance);
        if(distance.Status==ConvexDistanceStatus.Separated)
            return new(ConvexSeparationStatus.Separated,distance.LowerBound,distance.UpperBound,
                distance.PointA,distance.PointB,distance.Normal,distance.Iterations);
        var penetration=ConvexPenetration.Query(a,b,tolerance);
        if(penetration.Status==ConvexPenetrationStatus.Separated)
            throw new InvalidOperationException("Distance and penetration queries disagree.");
        var inside=penetration.Status==ConvexPenetrationStatus.Penetrating;
        return new(inside?ConvexSeparationStatus.Penetrating:ConvexSeparationStatus.WithinTolerance,
            -penetration.UpperDepth,inside?-penetration.LowerDepth:distance.UpperBound,
            penetration.PointA,penetration.PointB,penetration.Normal,distance.Iterations+penetration.Iterations);
    }
}
