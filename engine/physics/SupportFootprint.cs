using System;

namespace CuriousContraptions.Physics;

/// <summary>Exact projected bounds and centroid of the lowest supporting features
/// in a declared frame. Uses support mappings, never sphere/box type dispatch.
/// For compounds the centroid is the resultant location of equal support-vertex loads.</summary>
public readonly record struct SupportFootprint(CollisionVector LowestPoint,
    double MinimumX,double MaximumX,double MinimumZ,double MaximumZ,double RoundingRadius)
{
    public bool Fits(double halfX,double halfZ)=>
        MinimumX> -halfX&&MaximumX<halfX&&MinimumZ> -halfZ&&MaximumZ<halfZ;

    public static SupportFootprint Sample(CompoundGeometry geometry,RigidPose body,RigidPose frame)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if(!body.Rotation.IsValid||!frame.Rotation.IsValid)
            throw new ArgumentException("Support poses must be explicit rigid frames.");
        const double planeTolerance=1e-9;
        var minimumX=double.PositiveInfinity; var maximumX=double.NegativeInfinity;
        var minimumZ=double.PositiveInfinity; var maximumZ=double.NegativeInfinity;
        var lowest=double.PositiveInfinity; var count=0;
        var rounding=0.0; CollisionVector total=default;
        for(var i=0;i<geometry.Count;i++)
        {
            var child=geometry[new(i)];
            var pose=body;
            CollisionVector Direction(CollisionVector direction)=>pose.Rotation.Inverse().Apply(frame.Rotation.Apply(direction));
            CollisionVector Point(CollisionVector point)=>frame.InverseTransformPoint(pose.TransformPoint(point));
            minimumX=Math.Min(minimumX,Point(child.Support(Direction(new(-1,0,0)))).X);
            maximumX=Math.Max(maximumX,Point(child.Support(Direction(new(1,0,0)))).X);
            minimumZ=Math.Min(minimumZ,Point(child.Support(Direction(new(0,0,-1)))).Z);
            maximumZ=Math.Max(maximumZ,Point(child.Support(Direction(new(0,0,1)))).Z);
            var down=Direction(new(0,-1,0));
            var height=Point(child.Support(down)).Y;
            if(height>lowest+planeTolerance) continue;
            if(height<lowest-planeTolerance)
            {
                lowest=height; total=default; count=0; rounding=child.RoundingRadius;
            }
            else rounding=Math.Min(rounding,child.RoundingRadius);
            foreach(var vertex in child.SupportingFeature(down,planeTolerance).Vertices)
            {
                total+=Point(vertex.Point);
                count++;
            }
        }
        if(count==0) throw new InvalidOperationException("Geometry has no supporting feature.");
        return new(total/count,minimumX,maximumX,minimumZ,maximumZ,rounding);
    }
}
