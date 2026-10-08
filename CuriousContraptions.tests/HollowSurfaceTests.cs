using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class HollowSurfaceTests
{
    public enum BendAngle { FortyFive, Ninety }
    private static double Radians(BendAngle angle) => angle switch
    {
        BendAngle.FortyFive => Math.PI / 4,
        BendAngle.Ninety => Math.PI / 2,
        _ => throw new ArgumentOutOfRangeException(nameof(angle))
    };
    private static readonly HollowGeometrySettings Precision = new(.005);

    // Outside a compound, the nearest child gives its distance to the union.
    // An embedded result proves overlap only; its depth/normal is not a union exit.
    private static ConvexSeparationResult Query(CompoundGeometry geometry, Vector3 point)
    {
        var probe = new ConvexInstance(new ConvexHull([default]), SceneGeometryAdapter.CaptureAffine(new Transform3D(Basis.Identity,point)));
        ConvexSeparationResult? nearest = null;
        for (var i=0;i<geometry.Count;i++)
        {
            var separation=ConvexSeparation.Query(probe,geometry[new(i)]);
            if (nearest is null || separation.UpperBound < nearest.Value.UpperBound) nearest=separation;
        }
        return nearest!.Value;
    }

    private static ConvexSeparationResult Clearance(HollowGeometryResult geometry,Vector3 point,double ideal)
    {
        var result=Query(geometry.Geometry,point);
        Assert.Equal(ConvexSeparationStatus.Separated,result.Status);
        Assert.InRange(result.LowerBound,ideal-geometry.MaximumSurfaceError-1e-6,ideal+1e-6);
        Assert.InRange(result.UpperBound,ideal-geometry.MaximumSurfaceError-1e-6,ideal+1e-6);
        return result;
    }

    [Fact]
    public void AnnularProfileHasSignedOverlapSlopeNormalsAndOpenEnds()
    {
        var shell=HollowGeometry.Frustum(.9,1.3,.65,.05,Precision);
        // Place this point inside a wall child, not on the shared radial seam
        // where point-vs-child has zero depth despite being inside the union.
        var middle=Math.PI/shell.RingSegments;
        var inside=Query(shell.Geometry,new(0,(float)(.99*Math.Cos(middle)),(float)(.99*Math.Sin(middle))));
        Assert.Equal(ConvexSeparationStatus.Penetrating,inside.Status);
        Assert.True(inside.UpperBound<0);
        var slope=(1.3-.65)/1.8;
        var outside=Clearance(shell,new(0,1.2f,0),(1.2-1.025)/Math.Sqrt(1+slope*slope));
        Assert.True(outside.Normal.X>0 && outside.Normal.Y>0);
        var bore=Clearance(shell,new(0,.9f,0),(.975-.9)/Math.Sqrt(1+slope*slope));
        Assert.True(bore.Normal.X<0 && bore.Normal.Y<0);
        Assert.InRange(Query(shell.Geometry,Vector3.Zero).LowerBound,.8,1);
        Assert.True(Clearance(shell,new(-1.1f,1.32f,0),.2).Normal.X<-.99);
        Assert.True(Clearance(shell,new(1.1f,.67f,0),.2).Normal.X>.99);
    }

    [Theory]
    [InlineData(BendAngle.FortyFive)]
    [InlineData(BendAngle.Ninety)]
    public void CurvedCompoundDistinguishesBoreShellExteriorAndOpenEnd(BendAngle angle)
    {
        var sweep=Radians(angle);
        var bend=HollowGeometry.Bend(2.4,sweep,.65,.70,Precision);
        var radial=new Vector3((float)Math.Sin(sweep/2),(float)Math.Cos(sweep/2),0);
        var centre=radial*2.4f;
        Clearance(bend,centre,.65);
        var bore=Clearance(bend,centre+radial*.3f,.35);
        Assert.True(CollisionVector.Dot(bore.Normal,SceneGeometryAdapter.CaptureVector(-radial))>.99);
        var inside=Query(bend.Geometry,centre+radial*.67f);
        Assert.Equal(ConvexSeparationStatus.Penetrating,inside.Status);
        Assert.True(inside.UpperBound<0);
        var outside=Clearance(bend,centre+radial,.30);
        Assert.True(CollisionVector.Dot(outside.Normal,SceneGeometryAdapter.CaptureVector(radial))>.99);
        Assert.True(Query(bend.Geometry,new(-1,2.4f,0)).LowerBound>1);
    }

    [Fact]
    public void UnknownBendAngleRejectsInsteadOfSelectingGeometry() =>
        Assert.Throws<ArgumentOutOfRangeException>(()=>Radians((BendAngle)999));
}
