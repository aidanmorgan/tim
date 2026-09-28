using Godot;

namespace CuriousContraptions.Tests;

public class RotatingFrustumSweepTests
{
    public enum End { Inlet, Outlet }
    private static readonly Vector3 Half=new(1.8f,.12f,.55f);
    private static readonly FrustumProxy Funnel=new(new(Basis.Identity,new(1.5f,1.6f,0)),.9f,.65f,1.3f,.05f);
    private static RotatingShellHit Cast(double speed,double duration,FrustumProxy? frustum=null,
        Transform3D? pose=null,Vector3? pivot=null,Vector3? axis=null) =>
        RotatingShellSweep.Cast(pivot??Vector3.Zero,axis??Vector3.Back,pose??Transform3D.Identity,
            Half,speed,frustum??Funnel,duration);

    [Theory]
    [InlineData(1)]
    [InlineData(20000)]
    public void OuterSlopeContactMatchesIndependentCornerEquation(double speed)
    {
        var k=(1.3-.65)/1.8;
        var a=1.8-k*.12; var b=.12+k*1.8;
        var threshold=1.6-.975+k*1.5-.05;
        var expected=Math.Asin(threshold/Math.Sqrt(a*a+b*b))-Math.Atan(b/a);
        var hit=Cast(speed,.5/speed);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time*speed,expected-.0002,expected+.0001);
        Assert.InRange(hit.Margin,-.0001,.0001);
        Assert.InRange(hit.Iterations,1,1000);
    }

    [Fact]
    public void ClearEndpointsDoNotHideIntermediateThinShellContact()
    {
        Assert.False(FrustumBoxIntersection.Intersects(Transform3D.Identity,Half,Funnel));
        Assert.False(FrustumBoxIntersection.Intersects(new(new Basis(Vector3.Back,Mathf.Pi),Vector3.Zero),Half,Funnel));
        var hit=Cast(10000,Math.PI/10000);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time*10000,.2,.23);
    }

    [Fact]
    public void OpenBoreAllowsTravelUntilTheInnerSlopingWall()
    {
        var frustum=Funnel with { Pose=new(Basis.Identity,new(1.5f,0,0)),InletRadius=1.3f,OutletRadius=.65f };
        Assert.Equal(SphereSweepStatus.Clear,Cast(1,.01,frustum).Status);
        double lo=0,hi=.5;
        for(var i=0;i<80;i++)
        {
            var angle=(lo+hi)*.5;
            var x=1.8*Math.Cos(angle)-.12*Math.Sin(angle);
            var y=1.8*Math.Sin(angle)+.12*Math.Cos(angle);
            var radius=.975-(1.3-.65)/1.8*(x-1.5);
            if(Math.Sqrt(y*y+.55*.55)<radius) lo=angle; else hi=angle;
        }
        var hit=Cast(1,.5,frustum);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time,lo-.0002,hi+.0001);
    }

    [Fact]
    public void DepthMissAndExactCoaxialTravelStayClear()
    {
        Assert.Equal(SphereSweepStatus.Clear,Cast(1,Math.PI,
            Funnel with { Pose=new(Basis.Identity,new(1.5f,1.6f,2)) }).Status);
        var axial=Cast(1000,10,new(Transform3D.Identity,2,1.3f,.65f,.05f),axis:Vector3.Right);
        Assert.Equal(SphereSweepStatus.Clear,axial.Status);
        Assert.Equal(1,axial.Iterations);
    }

    [Fact]
    public void ContactCanReleaseAndWholeSetupCanRotateInThreeDimensions()
    {
        var hit=Cast(1,.5);
        var stopped=new Transform3D(new Basis(Vector3.Back,(float)hit.Time),Vector3.Zero);
        Assert.Equal(SphereSweepStatus.Clear,Cast(-1,.1,pose:stopped).Status);
        var moved=new Transform3D(Basis.FromEuler(new(.31f,-.67f,.28f)),new(3,4,-2));
        var rotated=Cast(1,.5,Funnel with { Pose=moved*Funnel.Pose },moved,moved.Origin,moved.Basis.Z);
        Assert.Equal(hit.Status,rotated.Status);
        Assert.InRange(Math.Abs(hit.Time-rotated.Time),0,.00002);
    }

    [Fact]
    public void InitialOverlapIsRejectedButStationaryOpenBoreIsClear()
    {
        Assert.Equal(SphereSweepStatus.Overlapping,Cast(0,0,
            Funnel with { Pose=new(Basis.Identity,new(1.5f,.8f,0)) }).Status);
        Assert.Equal(SphereSweepStatus.Clear,Cast(0,0,
            new(Transform3D.Identity,2,1.3f,.65f,.05f)).Status);
    }

    [Theory]
    [InlineData(17)]
    [InlineData(43)]
    public void DensePoseSamplesFindNoMissedTaperedShell(int seed)
    {
        var random=new Random(seed);
        float Between(float a,float b)=>a+random.NextSingle()*(b-a);
        var witnessed=0;
        for(var trial=0;trial<40;trial++)
        {
            var frustum=Funnel with { Pose=new(Basis.FromEuler(new(Between(-1,1),Between(-1,1),Between(-1,1))),
                new(Between(-2,2),Between(-2,2),Between(-1,1))),InletRadius=Between(.5f,1.5f),OutletRadius=Between(.5f,1.5f) };
            var speed=Between(-5,5);
            var hit=Cast(speed,.5,frustum);
            for(var step=0;step<=1024;step++)
            {
                var time=.5*step/1024;
                var pose=new Transform3D(new Basis(Vector3.Back,(float)(speed*time)),Vector3.Zero);
                if(!FrustumBoxIntersection.Intersects(pose,Half,frustum,-.001)) continue;
                witnessed++;
                Assert.NotEqual(SphereSweepStatus.Clear,hit.Status);
                Assert.True(hit.Time<=time,$"seed={seed}, trial={trial}, hit={hit.Time}, sample={time}");
                break;
            }
            Assert.InRange(hit.Iterations,1,10000);
        }
        Assert.True(witnessed>10);
    }

    [Fact]
    public void CurvedTangentApproachStopsAndReverseMotionReleases()
    {
        var frustum=new FrustumProxy(new(new Basis(Vector3.Up,Mathf.Pi/2),Vector3.Zero),2,.6f,1,.2f);
        var pose=new Transform3D(Basis.Identity,new(2.01f,.1f,0));
        var half=new Vector3(1,.1f,.1f);
        var pivot=new Vector3(2.01f,0,0);
        var hit=RotatingShellSweep.Cast(pivot,Vector3.Back,pose,half,1,frustum,.1);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        var rotation=new Basis(Vector3.Back,(float)hit.Time);
        var stopped=new Transform3D(rotation*pose.Basis,pivot+rotation*(pose.Origin-pivot));
        Assert.False(FrustumBoxIntersection.Intersects(stopped,half,frustum,-SphereSweep.ContactTolerance));
        var release=RotatingShellSweep.Cast(pivot,Vector3.Back,stopped,half,-1,frustum,.1);
        Assert.True(release.Status==SphereSweepStatus.Clear,$"hit={hit}; release={release}");
    }

    [Theory]
    [InlineData(End.Inlet)]
    [InlineData(End.Outlet)]
    public void AnnularEndFaceBlocksAxialEntryAtTheIndependentCapTime(End end)
    {
        var inlet=end==End.Inlet;
        var frustum=new FrustumProxy(Transform3D.Identity,1,1.3f,.65f,.2f);
        var pose=new Transform3D(Basis.Identity,new(inlet?-1.3f:1.3f,inlet?1.4f:.75f,0));
        var pivot=pose.Origin-Vector3.Up;
        var half=new Vector3(.1f,.1f,.1f);
        var hit=RotatingShellSweep.Cast(pivot,Vector3.Back,pose,half,inlet?-1:1,frustum,.4);
        var expected=Math.Asin(.3/Math.Sqrt(1.22))-Math.Atan(.1/1.1);
        Assert.Equal(SphereSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time,expected-.0002,expected+.0001);
    }

    [Fact]
    public void InvalidMotionIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>Cast(double.NaN,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Cast(1,-1));
        Assert.Throws<ArgumentException>(()=>Cast(1,1,axis:Vector3.Back*2));
    }
}
