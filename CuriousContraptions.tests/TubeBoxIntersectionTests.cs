using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class TubeBoxIntersectionTests
{
    public enum Arrangement { Bore, Shell, Rim, EndHole, BeyondEnd, SurroundingBox, OuterTouch, CoupledMiss }
    private static readonly TubeProxy Tube = new(Transform3D.Identity, 1, 1, 2, true);

    [Theory]
    [InlineData(Arrangement.Bore, false)]
    [InlineData(Arrangement.Shell, true)]
    [InlineData(Arrangement.Rim, true)]
    [InlineData(Arrangement.EndHole, false)]
    [InlineData(Arrangement.BeyondEnd, false)]
    [InlineData(Arrangement.SurroundingBox, true)]
    [InlineData(Arrangement.OuterTouch, true)]
    [InlineData(Arrangement.CoupledMiss, false)]
    public void ShellAndAnnularRimDoNotFillTheBore(Arrangement arrangement, bool expected)
    {
        var (pose, half) = arrangement switch
        {
            Arrangement.Bore => (new Transform3D(Basis.Identity,Vector3.Zero), new Vector3(.5f,.25f,.25f)),
            Arrangement.Shell => (new Transform3D(Basis.Identity,new(0,1.5f,0)), new Vector3(.1f,.1f,.1f)),
            Arrangement.Rim => (new Transform3D(Basis.Identity,new(1.05f,1.5f,0)), new Vector3(.1f,.1f,.1f)),
            Arrangement.EndHole => (new Transform3D(Basis.Identity,new(1,0,0)), new Vector3(.1f,.25f,.25f)),
            Arrangement.BeyondEnd => (new Transform3D(Basis.Identity,new(1.5f,1.5f,0)), new Vector3(.1f,.1f,.1f)),
            Arrangement.SurroundingBox => (Transform3D.Identity,new Vector3(3,3,3)),
            Arrangement.OuterTouch => (new Transform3D(Basis.Identity,new(0,2.25f,0)), new Vector3(.25f,.25f,.25f)),
            Arrangement.CoupledMiss => (new Transform3D(new Basis(Vector3.Back,-Mathf.Pi/4),new(1.2f,2.2f,0)),new Vector3(1,.05f,.05f)),
            _ => throw new ArgumentOutOfRangeException(nameof(arrangement))
        };
        Assert.Equal(expected, new HollowBoxTestProbe(pose,half,Tube).Query().Status != ConvexSweepStatus.Clear);
        var basis = Basis.FromEuler(new(.31f,-.67f,.28f));
        var moved = new Transform3D(basis,new(3,4,-2));
        Assert.Equal(expected, new HollowBoxTestProbe(moved*pose,half,Tube with { Pose=moved }).Query().Status != ConvexSweepStatus.Clear);
    }

    [Theory]
    [InlineData(0, 3, 0, .75)]
    [InlineData(1.5f, 1.5f, 0, .25)]
    public void AxisAlignedClearanceHasAnIndependentAnalyticBound(float x,float y,float z,double expected)
    {
        var pose = new Transform3D(Basis.Identity,new(x,y,z));
        new HollowBoxTestProbe(pose,new(.25f,.25f,.25f),Tube).AssertAnalyticClearance(expected);
    }

    [Fact]
    public void BoreClearanceUsesFarthestCornerRatherThanDistanceToTheAxis()
    {
        new HollowBoxTestProbe(Transform3D.Identity,new(.5f,.5f,.5f),Tube)
            .AssertAnalyticClearance(1 - Math.Sqrt(.5));
        var overlap = new HollowBoxTestProbe(new(Basis.Identity,new(0,1.5f,0)),new(.1f,.1f,.1f),Tube).Query();
        Assert.Equal(ConvexSweepStatus.InitialContact,overlap.Status);
        Assert.Equal(ConvexSeparationStatus.Penetrating,overlap.Separation!.Value.Status);
        Assert.True(overlap.Separation.Value.UpperBound < 0);
    }

    [Theory]
    [InlineData(23)]
    [InlineData(61)]
    public void IndependentInteriorSamplesNeverFindAMissedShell(int seed)
    {
        var random = new Random(seed);
        float Between(float a,float b) => a + random.NextSingle() * (b-a);
        var witnessed = 0;
        for (var trial=0;trial<80;trial++)
        {
            var pose = new Transform3D(Basis.FromEuler(new(Between(-2,2),Between(-2,2),Between(-2,2))),
                new(Between(-2,2),Between(-3,3),Between(-3,3)));
            var half = new Vector3(Between(.05f,1),Between(.05f,1),Between(.05f,1));
            var probe = new HollowBoxTestProbe(pose,half,Tube);
            var result = probe.Query();
            if (HollowBoxTestProbe.InteriorWitness(pose,half,Tube,8,.005))
            {
                witnessed++;
                Assert.True(result.Status != ConvexSweepStatus.Clear,$"Missed interior witness: seed={seed}, trial={trial}");
            }
            if (result.Status == ConvexSweepStatus.Clear) probe.AssertThreshold(probe.Clearance());
        }
        Assert.True(witnessed > 20);
    }

    [Fact]
    public void InvalidAndNonRigidGeometryIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HollowBoxTestProbe(Transform3D.Identity,Vector3.One,Tube).Query(double.NaN));
        Assert.Throws<ArgumentException>(() => new HollowBoxTestProbe(Transform3D.Identity,Vector3.One,Tube with { InnerRadius = 2 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HollowBoxTestProbe(Transform3D.Identity,Vector3.Zero,Tube));
        Assert.Throws<ArgumentException>(() => new HollowBoxTestProbe(new(Basis.Identity.Scaled(new(2,1,1)),Vector3.Zero),Vector3.One,Tube));
        Assert.Throws<ArgumentException>(() => new HollowBoxTestProbe(Transform3D.Identity,Vector3.One,Tube with { Pose = new(Basis.Identity,new(float.NaN,0,0)) }));
    }
}
