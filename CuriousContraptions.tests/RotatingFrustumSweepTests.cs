using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class RotatingFrustumSweepTests
{
    public enum End { Inlet, Outlet }
    private static readonly Vector3 Half=new(1.8f,.12f,.55f);
    private static readonly FrustumProxy Funnel=new(new(Basis.Identity,new(1.5f,1.6f,0)),.9f,.65f,1.3f,.05f);
    private static readonly HollowGeometrySettings Precision=new(.005);
    private static HollowGeometryResult Geometry(FrustumProxy frustum)=>
        HollowGeometry.Frustum(frustum.HalfLength,frustum.InletRadius,frustum.OutletRadius,frustum.Thickness,Precision);
    private static CompoundSweepResult Cast(double speed,double duration,FrustumProxy? frustum=null,
        Transform3D? pose=null,Vector3? pivot=null,Vector3? axis=null,Vector3? half=null,
        double minimumSeparation=ConvexSweep.ContactDistance)
    {
        var sceneBox=pose??Transform3D.Identity;
        var rigid=SceneGeometryAdapter.CaptureRigidPose(sceneBox);
        var origin=pivot??Vector3.Zero;
        var moving=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,
            new(SceneGeometryAdapter.CaptureVector(origin),rigid.Rotation),default,SceneGeometryAdapter.CaptureVector(axis??Vector3.Back)*speed);
        var offset=sceneBox.Basis.Inverse()*(sceneBox.Origin-origin);
        var beam=new CompoundMotion(new([new(new ConvexBox(SceneGeometryAdapter.CaptureVector(half??Half)),
            SceneGeometryAdapter.CaptureAffine(new Transform3D(Basis.Identity,offset)))]),moving.CreateTrajectory(duration,default));
        var declaration=frustum??Funnel;
        var shell=new PhysicsBody(new(1),PhysicsMotionType.Static,SceneGeometryAdapter.CaptureRigidPose(declaration.Pose),default,default);
        var wall=new CompoundMotion(Geometry(declaration).Geometry,shell.CreateTrajectory(duration,default));
        return CompoundCollision.Cast(beam,wall,duration,minimumSeparation);
    }

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
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        var padding=(Geometry(Funnel).MaximumSurfaceError+ConvexSweep.ContactDistance)*Math.Sqrt(1+k*k);
        var earliest=Math.Asin((threshold-padding)/Math.Sqrt(a*a+b*b))-Math.Atan(b/a);
        Assert.InRange(hit.Time*speed,earliest-1e-6,expected+1e-6);
        Assert.InRange(hit.Separation!.Value.UpperBound,0,ConvexSweep.ContactDistance+ConvexDistance.DefaultTolerance);
        Assert.InRange(hit.NarrowPhaseCalls,1,Geometry(Funnel).Geometry.Count);
        Assert.Equal(new ColliderChildId(0),hit.ChildA);
        Assert.InRange(hit.ChildB!.Value.Index,0,Geometry(Funnel).Geometry.Count-1);
    }

    [Fact]
    public void ClearEndpointsDoNotHideIntermediateThinShellContact()
    {
        Assert.Equal(ConvexSweepStatus.Clear,Cast(0,0).Status);
        Assert.Equal(ConvexSweepStatus.Clear,Cast(0,0,pose:new(new Basis(Vector3.Back,Mathf.Pi),Vector3.Zero)).Status);
        var hit=Cast(10000,Math.PI/10000);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time*10000,.2,.23);
    }

    [Fact]
    public void OpenBoreAllowsTravelUntilTheInnerSlopingWall()
    {
        var frustum=Funnel with { Pose=new(Basis.Identity,new(1.5f,0,0)),InletRadius=1.3f,OutletRadius=.65f };
        Assert.Equal(ConvexSweepStatus.Clear,Cast(1,.01,frustum).Status);
        double CornerTime(double padding)
        {
            double lo=0,hi=.5;
            for(var i=0;i<80;i++)
            {
                var angle=(lo+hi)*.5;
                var x=1.8*Math.Cos(angle)-.12*Math.Sin(angle);
                var y=1.8*Math.Sin(angle)+.12*Math.Cos(angle);
                var radius=.975-(1.3-.65)/1.8*(x-1.5)-padding;
                if(Math.Sqrt(y*y+.55*.55)<radius) lo=angle; else hi=angle;
            }
            return (lo+hi)*.5;
        }
        var hit=Cast(1,.5,frustum);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        var padding=(Geometry(frustum).MaximumSurfaceError+ConvexSweep.ContactDistance)*Math.Sqrt(1+Math.Pow((1.3-.65)/1.8,2));
        Assert.InRange(hit.Time,CornerTime(padding)-1e-6,CornerTime(0)+1e-6);
    }

    [Fact]
    public void DepthMissAndExactCoaxialTravelStayClear()
    {
        Assert.Equal(ConvexSweepStatus.Clear,Cast(1,Math.PI,
            Funnel with { Pose=new(Basis.Identity,new(1.5f,1.6f,2)) }).Status);
        var axial=Cast(1000,10,new(Transform3D.Identity,2,1.3f,.65f,.05f),axis:Vector3.Right);
        Assert.Equal(ConvexSweepStatus.Clear,axial.Status);
        Assert.InRange(axial.NarrowPhaseCalls,0,Geometry(new(Transform3D.Identity,2,1.3f,.65f,.05f)).Geometry.Count);
    }

    [Fact]
    public void ContactCanReleaseAndWholeSetupCanRotateInThreeDimensions()
    {
        var hit=Cast(1,.5);
        var stopped=new Transform3D(new Basis(Vector3.Back,(float)hit.Time),Vector3.Zero);
        Assert.Equal(ConvexSweepStatus.Clear,Cast(-1,.1,pose:stopped,minimumSeparation:-1e-6).Status);
        var moved=new Transform3D(Basis.FromEuler(new(.31f,-.67f,.28f)),new(3,4,-2));
        var rotated=Cast(1,.5,Funnel with { Pose=moved*Funnel.Pose },moved,moved.Origin,moved.Basis.Z);
        Assert.Equal(hit.Status,rotated.Status);
        Assert.InRange(Math.Abs(hit.Time-rotated.Time),0,.00002);
    }

    [Fact]
    public void InitialOverlapRetainsSignedDepthWhileStationaryOpenBoreIsClear()
    {
        var overlap=Cast(0,0,Funnel with { Pose=new(Basis.Identity,new(1.5f,.8f,0)) });
        Assert.Equal(ConvexSweepStatus.InitialContact,overlap.Status);
        Assert.Equal(ConvexSeparationStatus.Penetrating,overlap.Separation!.Value.Status);
        Assert.True(overlap.Separation.Value.UpperBound<0);
        Assert.Equal(ConvexSweepStatus.Clear,Cast(0,0,
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
                if(!HollowBoxTestProbe.InteriorWitness(pose,Half,frustum,4,.001)) continue;
                witnessed++;
                Assert.NotEqual(ConvexSweepStatus.Clear,hit.Status);
                Assert.True(hit.Time<=time,$"seed={seed}, trial={trial}, hit={hit.Time}, sample={time}");
                break;
            }
            Assert.InRange(hit.NarrowPhaseCalls,0,Geometry(frustum).Geometry.Count);
        }
        Assert.True(witnessed>10);
    }

    [Fact]
    public void CurvedTangentApproachStopsAndReverseMotionReleases()
    {
        var frustum=new FrustumProxy(new(new Basis(Vector3.Up,Mathf.Pi/2),Vector3.Zero),2,.6f,1,.2f);
        // Start outside the declared compound envelope, not on the ideal
        // cone surface that the bounded-error wall conservatively covers.
        var startX=(float)(2.01+Geometry(frustum).MaximumSurfaceError+ConvexSweep.ContactDistance);
        var pose=new Transform3D(Basis.Identity,new(startX,.1f,0));
        var half=new Vector3(1,.1f,.1f);
        var pivot=new Vector3(startX,0,0);
        var hit=Cast(1,.1,frustum,pose,pivot,Vector3.Back,half);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        var rotation=new Basis(Vector3.Back,(float)hit.Time);
        var stopped=new Transform3D(rotation*pose.Basis,pivot+rotation*(pose.Origin-pivot));
        Assert.Equal(ConvexSweepStatus.Clear,Cast(0,0,frustum,stopped,pivot,Vector3.Back,half,minimumSeparation:-1e-6).Status);
        var release=Cast(-1,.1,frustum,stopped,pivot,Vector3.Back,half,minimumSeparation:-1e-6);
        Assert.True(release.Status==ConvexSweepStatus.Clear,$"hit={hit}; release={release}");
    }

    [Theory]
    [InlineData(End.Inlet)]
    [InlineData(End.Outlet)]
    public void AnnularEndFaceBlocksAxialEntryAtTheIndependentCapTime(End end)
    {
        var inlet=end switch
        {
            End.Inlet=>true,End.Outlet=>false,
            _=>throw new ArgumentOutOfRangeException(nameof(end))
        };
        var frustum=new FrustumProxy(Transform3D.Identity,1,1.3f,.65f,.2f);
        var pose=new Transform3D(Basis.Identity,new(inlet?-1.3f:1.3f,inlet?1.4f:.75f,0));
        var pivot=pose.Origin-Vector3.Up;
        var half=new Vector3(.1f,.1f,.1f);
        var hit=Cast(inlet?-1:1,.4,frustum,pose,pivot,Vector3.Back,half);
        var expected=Math.Asin(.3/Math.Sqrt(1.22))-Math.Atan(.1/1.1);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time,expected-.0002,expected+.0001);
    }

    [Fact]
    public void UnknownEndRejectsInsteadOfChoosingAnOutlet()=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>AnnularEndFaceBlocksAxialEntryAtTheIndependentCapTime((End)999));

    [Fact]
    public void InvalidMotionIsRejected()
    {
        Assert.Throws<ArgumentException>(()=>Cast(double.NaN,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Cast(1,-1));
        Assert.Throws<ArgumentException>(()=>Cast(1,1,axis:new(float.PositiveInfinity,0,0)));
    }
}
