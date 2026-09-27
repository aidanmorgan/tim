using Godot;

namespace CuriousContraptions.Tests;

public class RotatingTubeSweepTests
{
    private static readonly Vector3 Half = new(1.8f,.12f,.55f);
    private static readonly TubeProxy Tube = new(new(Basis.Identity,new(1.5f,1.1f,0)),.5f,.65f,.7f,false);
    private static RotatingTubeHit Cast(double speed,double duration,TubeProxy? tube = null,
        Transform3D? box = null,Vector3? pivot = null,Vector3? axis = null) =>
        RotatingTubeSweep.Cast(pivot ?? Vector3.Zero,axis ?? Vector3.Back,box ?? Transform3D.Identity,Half,
            speed,tube ?? Tube,duration);

    [Theory]
    [InlineData(1)]
    [InlineData(20000)]
    public void OuterShellHitMatchesAnalyticCornerTime(double speed)
    {
        var expected = Math.Asin(.4/Math.Sqrt(1.8*1.8+.12*.12))-Math.Atan(.12/1.8);
        var hit = Cast(speed,.5/speed);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time*speed,expected-.00015,expected+.0001);
        Assert.InRange(hit.Margin,-.0001,.0001);
        Assert.InRange(hit.Iterations,1,1000);
    }

    [Fact]
    public void ClearEndpointPosesCannotHideThinShellContact()
    {
        var hit = Cast(10000,Math.PI/10000);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time*10000,.15,.17);
    }

    [Fact]
    public void BeamInsideOpenBoreCanMoveUntilItTouchesTheInnerShell()
    {
        var tube = new TubeProxy(Transform3D.Identity,2,.65f,.7f,false);
        Assert.Equal(SphereSweepStatus.Clear,Cast(1,.01,tube).Status);
        var hit = Cast(1,.5,tube);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        var expected = Math.Asin(Math.Sqrt(.65*.65-.55*.55)/Math.Sqrt(1.8*1.8+.12*.12))-Math.Atan(.12/1.8);
        Assert.InRange(hit.Time,expected-.0002,expected+.0001);
    }

    [Fact]
    public void DepthMissAndCoaxialRotationRemainClear()
    {
        Assert.Equal(SphereSweepStatus.Clear,Cast(1,Math.PI,Tube with { Pose = new(Basis.Identity,new(1.5f,1.1f,2)) }).Status);
        var tube = new TubeProxy(Transform3D.Identity,2,.65f,.7f,false);
        var axial = Cast(1000,10,tube,axis:Vector3.Right);
        Assert.Equal(SphereSweepStatus.Clear,axial.Status);
        Assert.Equal(1,axial.Iterations);
    }

    [Fact]
    public void ContactCanSeparateAndReturnLater()
    {
        var hit = Cast(1,.5);
        var pose = new Transform3D(new Basis(Vector3.Back,(float)hit.Time),Vector3.Zero);
        Assert.Equal(SphereSweepStatus.Clear,Cast(-1,.1,box:pose).Status);
        var later = Cast(-1,Math.PI,box:pose);
        Assert.Equal(SphereSweepStatus.Contact,later.Status);
        Assert.True(later.Time>1);
    }

    [Fact]
    public void InitialShellOverlapIsDistinctFromStationaryBoreClearance()
    {
        var overlap = Cast(0,0,Tube with { Pose = new(Basis.Identity,new(1.5f,.5f,0)) });
        Assert.Equal(SphereSweepStatus.Overlapping,overlap.Status);
        Assert.Equal(SphereSweepStatus.Clear,Cast(0,0,new(Transform3D.Identity,2,.65f,.7f,false)).Status);
    }

    [Fact]
    public void WholeSetupRotationPreservesHitTime()
    {
        var original = Cast(1,.5);
        var moved = new Transform3D(Basis.FromEuler(new(.31f,-.67f,.28f)),new(3,4,-2));
        var rotated = Cast(1,.5,Tube with { Pose = moved*Tube.Pose },moved,moved.Origin,moved.Basis.Z);
        Assert.Equal(original.Status,rotated.Status);
        Assert.InRange(Math.Abs(original.Time-rotated.Time),0,.00002);
    }

    [Theory]
    [InlineData(13)]
    [InlineData(31)]
    public void DensePoseSamplesDetectNoSkippedShell(int seed)
    {
        var random = new Random(seed);
        float Between(float a,float b) => a+random.NextSingle()*(b-a);
        var witnessed=0;
        for(var trial=0;trial<40;trial++)
        {
            var tube=Tube with { Pose = new(Basis.FromEuler(new(Between(-1,1),Between(-1,1),Between(-1,1))),
                new(Between(-2,2),Between(-2,2),Between(-1,1))) };
            var speed=Between(-5,5);
            var hit=Cast(speed,.5,tube);
            double? reference=null;
            for(var step=0;step<=1024;step++)
            {
                var time=.5*step/1024;
                var pose=new Transform3D(new Basis(Vector3.Back,(float)(speed*time)),Vector3.Zero);
                if(TubeBoxIntersection.Intersects(pose,Half,tube,-.001)){reference=time;break;}
            }
            if(reference is {} expected)
            {
                witnessed++;
                Assert.NotEqual(SphereSweepStatus.Clear,hit.Status);
                Assert.True(hit.Time<=expected,$"seed={seed}, trial={trial}, hit={hit.Time}, sample={expected}");
            }
            Assert.InRange(hit.Iterations,1,10000);
        }
        Assert.True(witnessed>10);
    }

    [Fact]
    public void CurvingInFromAnInitiallyTangentFaceStopsBeforeDeepOverlapAndCanRelease()
    {
        var tube = new TubeProxy(new(new Basis(Vector3.Up,Mathf.Pi/2),Vector3.Zero),2,.8f,1,false);
        var pose = new Transform3D(Basis.Identity,new(2,.1f,0));
        var half = new Vector3(1,.1f,.1f);
        var pivot = new Vector3(2,0,0);
        var hit = RotatingTubeSweep.Cast(pivot,Vector3.Back,pose,half,1,tube,.1);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        var rotation = new Basis(Vector3.Back,(float)hit.Time);
        var stopped = new Transform3D(rotation*pose.Basis,pivot+rotation*(pose.Origin-pivot));
        Assert.False(TubeBoxIntersection.Intersects(stopped,half,tube,-SphereSweep.ContactTolerance));
        var release = RotatingTubeSweep.Cast(pivot,Vector3.Back,stopped,half,-1,tube,.1);
        Assert.True(release.Status == SphereSweepStatus.Clear,$"hit={hit}; release={release}");
    }

    [Fact]
    public void InvalidMotionIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>Cast(double.NaN,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Cast(1,-1));
        Assert.Throws<ArgumentException>(()=>Cast(1,1,axis:Vector3.Back*2));
    }
}
