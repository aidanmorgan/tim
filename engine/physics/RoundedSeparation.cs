using System;

namespace CuriousContraptions.Physics;

/// <summary>Signed core separation translated by exact Minkowski radii.
/// Curved surface witnesses come from the core witnesses plus their radial
/// offsets, not a simplex chord lying inside the rounded surface.</summary>
public static class RoundedSeparation
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
        var core=ConvexSeparation.Query(new Core<TA>(a),new Core<TB>(b),tolerance);
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
}
