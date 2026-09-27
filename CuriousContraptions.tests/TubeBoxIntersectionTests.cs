using Godot;

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
        Assert.Equal(expected,TubeBoxIntersection.Intersects(pose,half,Tube));
        var basis = Basis.FromEuler(new(.31f,-.67f,.28f));
        var moved = new Transform3D(basis,new(3,4,-2));
        if (arrangement == Arrangement.OuterTouch)
            Assert.InRange(Math.Abs(TubeBoxIntersection.SignedMargin(moved * pose,half,Tube with { Pose = moved })),0,.00001);
        else Assert.Equal(expected,TubeBoxIntersection.Intersects(moved * pose,half,Tube with { Pose = moved }));
    }

    [Theory]
    [InlineData(0, 3, 0, .75)]
    [InlineData(1.5f, 1.5f, 0, .25)]
    public void AxisAlignedClearanceHasAnIndependentAnalyticBound(float x,float y,float z,double expected)
    {
        var pose = new Transform3D(Basis.Identity,new(x,y,z));
        var margin = TubeBoxIntersection.SignedMargin(pose,new(.25f,.25f,.25f),Tube);
        Assert.InRange(margin,expected - TubeBoxIntersection.MarginResolution,expected);
        Assert.False(TubeBoxIntersection.Intersects(pose,new(.25f,.25f,.25f),Tube,margin - .00001));
        Assert.True(TubeBoxIntersection.Intersects(pose,new(.25f,.25f,.25f),Tube,margin + .00001));
    }

    [Fact]
    public void BoreClearanceUsesFarthestCornerRatherThanDistanceToTheAxis()
    {
        var margin = TubeBoxIntersection.SignedMargin(Transform3D.Identity,new(.5f,.5f,.5f),Tube);
        var expected = 1 - Math.Sqrt(.5);
        Assert.InRange(margin,expected - TubeBoxIntersection.MarginResolution,expected);
        Assert.InRange(TubeBoxIntersection.SignedMargin(new(Basis.Identity,new(0,1.5f,0)),new(.1f,.1f,.1f),Tube),-.5,-.49999);
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
            var intersects = TubeBoxIntersection.Intersects(pose,half,Tube);
            var found = false;
            for (var x=0;x<=8 && !found;x++)
            for (var y=0;y<=8 && !found;y++)
            for (var z=0;z<=8 && !found;z++)
            {
                var p = pose * new Vector3(half.X*(x/4f-1),half.Y*(y/4f-1),half.Z*(z/4f-1));
                if (Tube.Surface(p).Distance < -.005f) found = true;
            }
            if (found)
            {
                witnessed++;
                Assert.True(intersects,$"Missed interior witness: seed={seed}, trial={trial}");
            }
            var margin = TubeBoxIntersection.SignedMargin(pose,half,Tube);
            if (margin > .00001) Assert.False(intersects);
            if (margin < -.00001) Assert.True(intersects);
        }
        Assert.True(witnessed > 20);
    }

    [Fact]
    public void InvalidAndNonRigidGeometryIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TubeBoxIntersection.Intersects(Transform3D.Identity,Vector3.One,Tube,double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => TubeBoxIntersection.SignedMargin(Transform3D.Identity,Vector3.One,Tube with { InnerRadius = 2 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => TubeBoxIntersection.SignedMargin(Transform3D.Identity,Vector3.Zero,Tube));
        Assert.Throws<ArgumentException>(() => TubeBoxIntersection.Intersects(new(Basis.Identity.Scaled(new(2,1,1)),Vector3.Zero),Vector3.One,Tube));
        Assert.Throws<ArgumentException>(() => TubeBoxIntersection.Intersects(Transform3D.Identity,Vector3.One,Tube with { Pose = new(Basis.Identity,new(float.NaN,0,0)) }));
    }
}
