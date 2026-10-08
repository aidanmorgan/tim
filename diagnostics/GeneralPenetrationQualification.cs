#if PLAYTEST
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions;

internal enum GeneralPenetrationProbe { Sphere, Box, Hull }
internal sealed record GeneralPenetrationReport(GeneralPenetrationProbe Probe,ConvexPenetrationStatus Status,
    double LowerDepth,double UpperDepth,double WitnessError,int Iterations);
internal static class GeneralPenetrationQualification
{
    internal static GeneralPenetrationReport Run(GeneralPenetrationProbe probe)
    {
        ConvexGeometry geometry=probe switch
        {
            GeneralPenetrationProbe.Sphere=>new ConvexSphere(.8),
            GeneralPenetrationProbe.Box=>new ConvexBox(new(1,1,1)),
            GeneralPenetrationProbe.Hull=>new ConvexHull([new(-1,-1,-1),new(1,-1,-1),new(-1,1,-1),new(1,1,-1),
                new(-1,-1,1),new(1,-1,1),new(-1,1,1),new(1,1,1)]),
            _=>throw new System.ArgumentOutOfRangeException(nameof(probe))
        };
        var a=new ConvexInstance(geometry,AffineTransform.Identity);
        var b=new ConvexInstance(geometry,new(AffineBasis.Identity,new(.4f,.2f,.1f)));
        var result=ConvexPenetration.Query(a,b);
        return new(probe,result.Status,result.LowerDepth,result.UpperDepth,
            (result.PointA-result.PointB+result.Normal*result.LowerDepth).Length,result.Iterations);
    }
}
#endif
