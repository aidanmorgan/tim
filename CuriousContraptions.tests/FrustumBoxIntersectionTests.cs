using Godot;

namespace CuriousContraptions.Tests;

public class FrustumBoxIntersectionTests
{
    public enum Arrangement { Bore, Shell, InletRim, OutletRim, InletHole, OutletHole, BeyondEnd, SurroundingBox, CoupledMiss }
    private static readonly FrustumProxy Funnel = new(Transform3D.Identity,1,2,1,.25f);

    [Theory]
    [InlineData(Arrangement.Bore,false)]
    [InlineData(Arrangement.Shell,true)]
    [InlineData(Arrangement.InletRim,true)]
    [InlineData(Arrangement.OutletRim,true)]
    [InlineData(Arrangement.InletHole,false)]
    [InlineData(Arrangement.OutletHole,false)]
    [InlineData(Arrangement.BeyondEnd,false)]
    [InlineData(Arrangement.SurroundingBox,true)]
    [InlineData(Arrangement.CoupledMiss,false)]
    public void TaperedShellAndBothAnnularEndsPreserveOpenings(Arrangement arrangement,bool expected)
    {
        var (pose,half) = arrangement switch
        {
            Arrangement.Bore => (Transform3D.Identity,new Vector3(.5f,.25f,.25f)),
            Arrangement.Shell => (new Transform3D(Basis.Identity,new(0,1.625f,0)),new Vector3(.05f,.05f,.05f)),
            Arrangement.InletRim => (new Transform3D(Basis.Identity,new(-1.05f,2.125f,0)),new Vector3(.1f,.05f,.05f)),
            Arrangement.OutletRim => (new Transform3D(Basis.Identity,new(1.05f,1.125f,0)),new Vector3(.1f,.05f,.05f)),
            Arrangement.InletHole => (new Transform3D(Basis.Identity,new(-1,0,0)),new Vector3(.1f,.5f,.5f)),
            Arrangement.OutletHole => (new Transform3D(Basis.Identity,new(1,0,0)),new Vector3(.1f,.5f,.5f)),
            Arrangement.BeyondEnd => (new Transform3D(Basis.Identity,new(1.5f,1.125f,0)),new Vector3(.1f,.1f,.1f)),
            Arrangement.SurroundingBox => (Transform3D.Identity,new Vector3(3,3,3)),
            Arrangement.CoupledMiss => (new Transform3D(new Basis(Vector3.Back,-Mathf.Pi/4),new(1.2f,2.2f,0)),new Vector3(1,.025f,.025f)),
            _ => throw new ArgumentOutOfRangeException(nameof(arrangement))
        };
        Assert.Equal(expected,FrustumBoxIntersection.Intersects(pose,half,Funnel));
        var moved = new Transform3D(Basis.FromEuler(new(.31f,-.67f,.28f)),new(3,4,-2));
        Assert.Equal(expected,FrustumBoxIntersection.Intersects(moved*pose,half,Funnel with { Pose=moved }));
    }

    [Fact]
    public void SlopedOuterClearanceUsesTheNormalDistanceNotTheRadialGap()
    {
        var pose = new Transform3D(Basis.Identity,new(0,2.5f,0));
        var half = new Vector3(.25f,.25f,.25f);
        // Minimum r + .5*x is 2.25 - .125, at the middle of a box edge.
        var expected = (2.125-1.75)/Math.Sqrt(1.25);
        var margin = FrustumBoxIntersection.SignedMargin(pose,half,Funnel);
        Assert.InRange(margin,expected-FrustumBoxIntersection.MarginResolution,expected);
        Assert.False(FrustumBoxIntersection.Intersects(pose,half,Funnel,margin-.00001));
        Assert.True(FrustumBoxIntersection.Intersects(pose,half,Funnel,margin+.00001));
    }

    [Fact]
    public void BoreClearanceUsesMaximumOverCorners()
    {
        var half = new Vector3(.5f,.5f,.5f);
        var expected = (1.5-Math.Sqrt(.5)-.25)/Math.Sqrt(1.25);
        Assert.InRange(FrustumBoxIntersection.SignedMargin(Transform3D.Identity,half,Funnel),
            expected-FrustumBoxIntersection.MarginResolution,expected);
    }

    [Theory]
    [InlineData(19)]
    [InlineData(73)]
    public void EqualEndRadiiAgreeWithIndependentCylinderQuery(int seed)
    {
        var random = new Random(seed);
        float Between(float a,float b) => a+random.NextSingle()*(b-a);
        var cylinder = Funnel with { InletRadius=1,OutletRadius=1 };
        var tube = new TubeProxy(Transform3D.Identity,1,1,1.25f,true);
        for (var trial=0;trial<100;trial++)
        {
            var pose = new Transform3D(Basis.FromEuler(new(Between(-2,2),Between(-2,2),Between(-2,2))),
                new(Between(-2,2),Between(-2,2),Between(-2,2)));
            var half = new Vector3(Between(.05f,1),Between(.05f,1),Between(.05f,1));
            Assert.Equal(TubeBoxIntersection.Intersects(pose,half,tube),
                FrustumBoxIntersection.Intersects(pose,half,cylinder));
            Assert.InRange(Math.Abs(TubeBoxIntersection.SignedMargin(pose,half,tube)-
                FrustumBoxIntersection.SignedMargin(pose,half,cylinder)),0,.000007);
        }
    }

    [Theory]
    [InlineData(31)]
    [InlineData(67)]
    public void IndependentSurfaceWitnessesAndMonotoneExpansion(int seed)
    {
        var random = new Random(seed);
        float Between(float a,float b) => a+random.NextSingle()*(b-a);
        var witnessed=0;
        for (var trial=0;trial<100;trial++)
        {
            var frustum = Funnel with { InletRadius=Between(.2f,2.5f),OutletRadius=Between(.2f,2.5f) };
            var pose = new Transform3D(Basis.FromEuler(new(Between(-2,2),Between(-2,2),Between(-2,2))),
                new(Between(-2,2),Between(-2,2),Between(-2,2)));
            var half = new Vector3(Between(.05f,1),Between(.05f,1),Between(.05f,1));
            var intersects = FrustumBoxIntersection.Intersects(pose,half,frustum);
            var found=false;
            for (var x=0;x<=8 && !found;x++)
            for (var y=0;y<=8 && !found;y++)
            for (var z=0;z<=8 && !found;z++)
            {
                var p=pose*new Vector3(half.X*(x/4f-1),half.Y*(y/4f-1),half.Z*(z/4f-1));
                if (frustum.Surface(p).Distance < -.005f) found=true;
            }
            if (found)
            {
                witnessed++;
                Assert.True(intersects,$"Missed interior witness: seed={seed}, trial={trial}");
            }
            var margin=FrustumBoxIntersection.SignedMargin(pose,half,frustum);
            Assert.False(FrustumBoxIntersection.Intersects(pose,half,frustum,margin-.00001));
            Assert.True(FrustumBoxIntersection.Intersects(pose,half,frustum,margin+.00001));
            if (margin>.00001) Assert.False(intersects);
            if (margin<-.00001) Assert.True(intersects);
        }
        Assert.True(witnessed>20);
    }

    [Theory]
    [InlineData(2f,true)]
    [InlineData(2.04f,false)]
    public void SlopedEdgeInteriorExtremumIsNotReplacedByVertexTests(float height,bool expected)
    {
        // On the long box edge y is approximately x. The minimum of
        // sqrt(x²+height²)+.25*x lies inside the clipped edge, not at a vertex.
        // The thin width deliberately keeps every clipped vertex outside.
        var frustum=Funnel with { InletRadius=2,OutletRadius=1.5f,Thickness=.2f };
        var pose=new Transform3D(new Basis(Vector3.Back,Mathf.Pi/4),new(0,0,height));
        Assert.Equal(expected,FrustumBoxIntersection.Intersects(pose,new(2,.001f,.001f),frustum));
    }

    [Fact]
    public void ThickBoxContainingAxisAndShellHasNoVertexInTheSolidCone()
    {
        // The shell pierces the interior of a broad face; testing box vertices
        // or original box edges against the un-clipped cone is insufficient.
        var pose=new Transform3D(Basis.Identity,new(.95f,.27f,.41f));
        Assert.True(FrustumBoxIntersection.Intersects(pose,new(.1f,4,5),Funnel));
    }

    [Fact]
    public void InvalidAndNonRigidGeometryIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>FrustumBoxIntersection.Intersects(Transform3D.Identity,Vector3.One,Funnel,double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>FrustumBoxIntersection.SignedMargin(Transform3D.Identity,Vector3.One,Funnel with { OutletRadius=0 }));
        Assert.Throws<ArgumentOutOfRangeException>(()=>FrustumBoxIntersection.SignedMargin(Transform3D.Identity,Vector3.Zero,Funnel));
        Assert.Throws<ArgumentOutOfRangeException>(()=>FrustumBoxIntersection.SignedMargin(Transform3D.Identity,Vector3.One,Funnel with { Thickness=float.PositiveInfinity }));
        Assert.Throws<ArgumentException>(()=>FrustumBoxIntersection.Intersects(new(Basis.Identity.Scaled(new(2,1,1)),Vector3.Zero),Vector3.One,Funnel));
        Assert.Throws<ArgumentException>(()=>FrustumBoxIntersection.Intersects(Transform3D.Identity,Vector3.One,Funnel with { Pose=new(Basis.Identity,new(float.NaN,0,0)) }));
    }
}
