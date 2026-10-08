using System;

namespace CuriousContraptions.Physics;

/// <summary>Exact Minkowski sum of a declared convex core and a ball.
/// Rounding is geometry, not a contact solver or shape-pair special case.</summary>
public sealed class ConvexRounded : ConvexGeometry
{
    public ConvexGeometry Core { get; }
    public double Radius { get; }
    public override CollisionVector CoreSupport(CollisionVector direction)=>Core.CoreSupport(direction);
    public override double BoundingRadius=>Core.BoundingRadius+Radius;
    public override double RoundingRadius=>Core.RoundingRadius+Radius;
    public override InteriorBall InteriorBall=>new(Core.InteriorBall.Center,Core.InteriorBall.Radius+Radius);
    public ConvexRounded(ConvexGeometry core,double radius)
    {
        ArgumentNullException.ThrowIfNull(core);
        if(!double.IsFinite(radius)||radius<=0||!double.IsFinite(core.BoundingRadius+radius))
            throw new ArgumentOutOfRangeException(nameof(radius));
        Core=core; Radius=radius;
    }
    public override CollisionVector Support(CollisionVector direction)
    {
        if(!direction.IsFinite||!double.IsFinite(direction.Length)) throw new ArgumentOutOfRangeException(nameof(direction));
        var length=direction.Length;
        return Core.Support(direction)+(length==0?new CollisionVector(Radius,0,0):direction*(Radius/length));
    }
    public override SupportFeature SupportingFeature(CollisionVector direction,double planeTolerance)
    {
        var normal=SupportFeature.UnitDirection(direction,planeTolerance);
        var feature=Core.SupportingFeature(normal,planeTolerance);
        var vertices=new SupportVertex[feature.Vertices.Length];
        for(var i=0;i<vertices.Length;i++)
            vertices[i]=new(feature.Vertices[i].Id,feature.Vertices[i].Point+normal*Radius);
        return new(vertices);
    }
}
