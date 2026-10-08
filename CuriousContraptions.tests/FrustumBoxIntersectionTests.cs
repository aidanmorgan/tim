using Godot;
using CuriousContraptions.Physics;

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
        Assert.Equal(expected,new HollowBoxTestProbe(pose,half,Funnel).Query().Status != ConvexSweepStatus.Clear);
        var moved = new Transform3D(Basis.FromEuler(new(.31f,-.67f,.28f)),new(3,4,-2));
        Assert.Equal(expected,new HollowBoxTestProbe(moved*pose,half,Funnel with { Pose=moved }).Query().Status != ConvexSweepStatus.Clear);
    }

    [Fact]
    public void SlopedOuterClearanceUsesTheNormalDistanceNotTheRadialGap()
    {
        var pose = new Transform3D(Basis.Identity,new(0,2.5f,0));
        var half = new Vector3(.25f,.25f,.25f);
        // Minimum r + .5*x is 2.25 - .125, at the middle of a box edge.
        var expected = (2.125-1.75)/Math.Sqrt(1.25);
        new HollowBoxTestProbe(pose,half,Funnel).AssertAnalyticClearance(expected);
    }

    [Fact]
    public void BoreClearanceUsesMaximumOverCorners()
    {
        var half = new Vector3(.5f,.5f,.5f);
        var expected = (1.5-Math.Sqrt(.5)-.25)/Math.Sqrt(1.25);
        new HollowBoxTestProbe(Transform3D.Identity,half,Funnel).AssertAnalyticClearance(expected);
    }

    [Theory]
    [InlineData(19)]
    [InlineData(73)]
    public void EqualEndRadiiProduceEquivalentSharedCompounds(int seed)
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
            var tubeProbe = new HollowBoxTestProbe(pose,half,tube);
            var coneProbe = new HollowBoxTestProbe(pose,half,cylinder);
            Assert.Equal(tubeProbe.Query().Status,coneProbe.Query().Status);
            if (tubeProbe.Query().Status == ConvexSweepStatus.Clear)
                Assert.Equal(tubeProbe.Clearance(),coneProbe.Clearance());
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
            var probe = new HollowBoxTestProbe(pose,half,frustum);
            var result = probe.Query();
            if (HollowBoxTestProbe.InteriorWitness(pose,half,frustum,8,.005))
            {
                witnessed++;
                Assert.True(result.Status != ConvexSweepStatus.Clear,$"Missed interior witness: seed={seed}, trial={trial}");
            }
            if (result.Status == ConvexSweepStatus.Clear) probe.AssertThreshold(probe.Clearance());
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
        Assert.Equal(expected,new HollowBoxTestProbe(pose,new(2,.001f,.001f),frustum).Query().Status != ConvexSweepStatus.Clear);
    }

    [Fact]
    public void ThickBoxContainingAxisAndShellHasNoVertexInTheSolidCone()
    {
        // The shell pierces the interior of a broad face; testing box vertices
        // or original box edges against the un-clipped cone is insufficient.
        var pose=new Transform3D(Basis.Identity,new(.95f,.27f,.41f));
        Assert.Equal(ConvexSweepStatus.InitialContact,new HollowBoxTestProbe(pose,new(.1f,4,5),Funnel).Query().Status);
    }

    [Fact]
    public void InvalidAndNonRigidGeometryIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new HollowBoxTestProbe(Transform3D.Identity,Vector3.One,Funnel).Query(double.NaN));
        Assert.Throws<ArgumentException>(()=>new HollowBoxTestProbe(Transform3D.Identity,Vector3.One,Funnel with { OutletRadius=0 }));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new HollowBoxTestProbe(Transform3D.Identity,Vector3.Zero,Funnel));
        Assert.Throws<ArgumentException>(()=>new HollowBoxTestProbe(Transform3D.Identity,Vector3.One,Funnel with { Thickness=float.PositiveInfinity }));
        Assert.Throws<ArgumentException>(()=>new HollowBoxTestProbe(new(Basis.Identity.Scaled(new(2,1,1)),Vector3.Zero),Vector3.One,Funnel));
        Assert.Throws<ArgumentException>(()=>new HollowBoxTestProbe(Transform3D.Identity,Vector3.One,Funnel with { Pose=new(Basis.Identity,new(float.NaN,0,0)) }));
    }
}
