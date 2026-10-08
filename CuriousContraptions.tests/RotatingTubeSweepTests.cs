using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class RotatingTubeSweepTests
{
    private static readonly Vector3 Half = new(1.8f,.12f,.55f);
    private static readonly TubeProxy Tube = new(new(Basis.Identity,new(1.5f,1.1f,0)),.5f,.65f,.7f,false);
    private static readonly HollowGeometrySettings Precision=new(.005);
    private static HollowGeometryResult Geometry(TubeProxy tube)=>
        HollowGeometry.Tube(tube.HalfLength,tube.InnerRadius,tube.OuterRadius,Precision);
    private static CompoundSweepResult Cast(double speed,double duration,TubeProxy? tube=null,
        Transform3D? box=null,Vector3? pivot=null,Vector3? axis=null,Vector3? half=null,
        double minimumSeparation=ConvexSweep.ContactDistance)
    {
        var sceneBox=box??Transform3D.Identity;
        var pose=SceneGeometryAdapter.CaptureRigidPose(sceneBox);
        var origin=pivot??Vector3.Zero;
        var moving=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,
            new(SceneGeometryAdapter.CaptureVector(origin),pose.Rotation),default,SceneGeometryAdapter.CaptureVector(axis??Vector3.Back)*speed);
        var offset=sceneBox.Basis.Inverse()*(sceneBox.Origin-origin);
        var beam=new CompoundMotion(new([new(new ConvexBox(SceneGeometryAdapter.CaptureVector(half??Half)),
            SceneGeometryAdapter.CaptureAffine(new Transform3D(Basis.Identity,offset)))]),moving.CreateTrajectory(duration,default));
        var declaration=tube??Tube;
        var shell=new PhysicsBody(new(1),PhysicsMotionType.Static,SceneGeometryAdapter.CaptureRigidPose(declaration.Pose),default,default);
        var wall=new CompoundMotion(Geometry(declaration).Geometry,shell.CreateTrajectory(duration,default));
        return CompoundCollision.Cast(beam,wall,duration,minimumSeparation);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(20000)]
    public void OuterShellHitMatchesAnalyticCornerTime(double speed)
    {
        var expected = Math.Asin(.4/Math.Sqrt(1.8*1.8+.12*.12))-Math.Atan(.12/1.8);
        var hit = Cast(speed,.5/speed);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        var padding=Geometry(Tube).MaximumSurfaceError+ConvexSweep.ContactDistance;
        var earliest=Math.Asin((.4-padding)/Math.Sqrt(1.8*1.8+.12*.12))-Math.Atan(.12/1.8);
        Assert.InRange(hit.Time*speed,earliest-1e-6,expected+1e-6);
        Assert.InRange(hit.Separation!.Value.UpperBound,0,ConvexSweep.ContactDistance+ConvexDistance.DefaultTolerance);
        Assert.InRange(hit.NarrowPhaseCalls,1,Geometry(Tube).Geometry.Count);
        Assert.Equal(new ColliderChildId(0),hit.ChildA);
        Assert.InRange(hit.ChildB!.Value.Index,0,Geometry(Tube).Geometry.Count-1);
    }

    [Fact]
    public void ClearEndpointPosesCannotHideThinShellContact()
    {
        var hit = Cast(10000,Math.PI/10000);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time*10000,.15,.17);
    }

    [Fact]
    public void BeamInsideOpenBoreCanMoveUntilItTouchesTheInnerShell()
    {
        var tube = new TubeProxy(Transform3D.Identity,2,.65f,.7f,false);
        Assert.Equal(ConvexSweepStatus.Clear,Cast(1,.01,tube).Status);
        var hit = Cast(1,.5,tube);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        var expected = Math.Asin(Math.Sqrt(.65*.65-.55*.55)/Math.Sqrt(1.8*1.8+.12*.12))-Math.Atan(.12/1.8);
        var inner=.65-Geometry(tube).MaximumSurfaceError-ConvexSweep.ContactDistance;
        var earliest=Math.Asin(Math.Sqrt(inner*inner-.55*.55)/Math.Sqrt(1.8*1.8+.12*.12))-Math.Atan(.12/1.8);
        Assert.InRange(hit.Time,earliest-1e-6,expected+1e-6);
    }

    [Fact]
    public void DepthMissAndCoaxialRotationRemainClear()
    {
        Assert.Equal(ConvexSweepStatus.Clear,Cast(1,Math.PI,Tube with { Pose = new(Basis.Identity,new(1.5f,1.1f,2)) }).Status);
        var tube = new TubeProxy(Transform3D.Identity,2,.65f,.7f,false);
        var axial = Cast(1000,10,tube,axis:Vector3.Right);
        Assert.Equal(ConvexSweepStatus.Clear,axial.Status);
        Assert.InRange(axial.NarrowPhaseCalls,0,Geometry(tube).Geometry.Count);
    }

    [Fact]
    public void ContactCanSeparateAndReturnLater()
    {
        var hit = Cast(1,.5);
        var pose = new Transform3D(new Basis(Vector3.Back,(float)hit.Time),Vector3.Zero);
        Assert.Equal(ConvexSweepStatus.Clear,Cast(-1,.1,box:pose,minimumSeparation:-1e-6).Status);
        var later = Cast(-1,Math.PI,box:pose,minimumSeparation:-1e-6);
        Assert.Equal(ConvexSweepStatus.Contact,later.Status);
        Assert.True(later.Time>1);
    }

    [Fact]
    public void InitialShellOverlapIsDistinctFromStationaryBoreClearance()
    {
        var overlap = Cast(0,0,Tube with { Pose = new(Basis.Identity,new(1.5f,.5f,0)) });
        Assert.Equal(ConvexSweepStatus.InitialContact,overlap.Status);
        Assert.Equal(ConvexSeparationStatus.Penetrating,overlap.Separation!.Value.Status);
        Assert.True(overlap.Separation.Value.UpperBound<0);
        Assert.Equal(ConvexSweepStatus.Clear,Cast(0,0,new(Transform3D.Identity,2,.65f,.7f,false)).Status);
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
                if(HollowBoxTestProbe.InteriorWitness(pose,Half,tube,4,.001)){reference=time;break;}
            }
            if(reference is {} expected)
            {
                witnessed++;
                Assert.NotEqual(ConvexSweepStatus.Clear,hit.Status);
                Assert.True(hit.Time<=expected,$"seed={seed}, trial={trial}, hit={hit.Time}, sample={expected}");
            }
            Assert.InRange(hit.NarrowPhaseCalls,0,Geometry(tube).Geometry.Count);
        }
        Assert.True(witnessed>10);
    }

    [Fact]
    public void CurvingNearATangentFaceStopsBeforeDeepOverlapAndCanRelease()
    {
        var tube = new TubeProxy(new(new Basis(Vector3.Up,Mathf.Pi/2),Vector3.Zero),2,.8f,1,false);
        var pose = new Transform3D(Basis.Identity,new(2.01f,.1f,0));
        var half = new Vector3(1,.1f,.1f);
        var pivot = new Vector3(2.01f,0,0);
        var hit = Cast(1,.1,tube,pose,pivot,Vector3.Back,half);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        var rotation = new Basis(Vector3.Back,(float)hit.Time);
        var stopped = new Transform3D(rotation*pose.Basis,pivot+rotation*(pose.Origin-pivot));
        Assert.Equal(ConvexSweepStatus.Clear,Cast(0,0,tube,stopped,pivot,Vector3.Back,half,minimumSeparation:-1e-6).Status);
        var release = Cast(-1,.1,tube,stopped,pivot,Vector3.Back,half,minimumSeparation:-1e-6);
        Assert.True(release.Status == ConvexSweepStatus.Clear,$"hit={hit}; release={release}");
    }

    [Fact]
    public void InvalidMotionIsRejected()
    {
        Assert.Throws<ArgumentException>(()=>Cast(double.NaN,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Cast(1,-1));
        Assert.Throws<ArgumentException>(()=>Cast(1,1,axis:new(float.PositiveInfinity,0,0)));
    }
}
