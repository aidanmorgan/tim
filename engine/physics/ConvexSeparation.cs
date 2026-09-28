using System;

namespace CuriousContraptions.Physics;

public enum ConvexSeparationStatus { Separated, WithinTolerance, Penetrating }
public readonly record struct ConvexSeparationResult(ConvexSeparationStatus Status,double LowerBound,double UpperBound,
    CollisionVector PointA,CollisionVector PointB,CollisionVector Normal,int Iterations);

/// <summary>Signed separation interval: positive outside, negative inside.
/// The query never interprets a zero-distance GJK result as penetration depth.</summary>
public static class ConvexSeparation
{
    public static ConvexSeparationResult Query<TA,TB>(TA a,TB b,double tolerance=ConvexDistance.DefaultTolerance)
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
