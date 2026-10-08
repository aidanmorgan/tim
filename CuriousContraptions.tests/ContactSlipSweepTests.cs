using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ContactSlipSweepTests
{
    [Fact]
    public void CapturedSlipRetainsHeldMotionBelowParticipantVelocityResolution()
    {
        var a=Ball(1);
        var b=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.At(new(0,-.5,0)),new(1,0,0),default);
        var gap=Gap(a,b);
        var pa=a.CreateTrajectory(.5,new(new(1e-20,0,0),default));
        var pb=b.CreateTrajectory(.5,default);
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>{{a.Id,pa},{b.Id,pb}};
        var material=new MaterialContact(gap,[]);
        var expected=1e-20*.5;
        Assert.Equal(expected,material.Kinematics.V.X*material.Kinematics.TangentV.SpeedAlong(paths,.5));
        var sample=new ContactSlipPath(material,paths).At(.5);
        Assert.Equal(expected,sample.Slip.X);
        Assert.Equal(1e-20,sample.Derivative.X);
        Assert.Equal(expected,gap.SlipAt(pa,pb,.5).Slip.X);
    }

    private static ContactSlipPath Path(ContactGap gap,BodyTrajectory a,BodyTrajectory b)=>
        new(new MaterialContact(gap,[]),new Dictionary<PhysicsBodyId,BodyTrajectory>{{gap.A.Id,a},{gap.B.Id,b}});
    private static readonly ConvexInstance Sphere=new(new ConvexSphere(.5),AffineTransform.Identity);
    private static readonly ConvexInstance Floor=new(new ConvexBox(new(10,.5,10)),AffineTransform.Identity);
    private static PhysicsBody Ball(double speed)=>new(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,.5,0)),
        new(speed,0,0),default,1,new(.1,.1,.1));
    private static PhysicsBody Ground()=>new(new(1),PhysicsMotionType.Static,RigidPose.At(new(0,-.5,0)),default,default);
    private static ContactGap Gap(PhysicsBody a,PhysicsBody b)=>Assert.Single(ContactGap.Query(a,Sphere,b,Floor,.001,1e-9));
    private static void Near(double expected,double actual,double tolerance=1e-8)=>Assert.InRange(Math.Abs(expected-actual),0,tolerance);

    [Theory]
    [InlineData(1,.01,ContactSlipStatus.Clear)]
    [InlineData(1,.2,ContactSlipStatus.Boundary)]
    [InlineData(-1,.2,ContactSlipStatus.Boundary)]
    public void CapturedSlidingPathStopsAtAnalyticSlipBoundary(int direction,double duration,ContactSlipStatus expected)
    {
        var a=Ball(2*direction); var b=Ground(); var gap=Gap(a,b);
        var pa=a.CreateTrajectory(duration,new(new(-4.9*direction,0,0),new(0,0,-2.45*direction)));
        var pb=b.CreateTrajectory(duration,default);
        var result=ContactSlipSweep.Cast(Path(gap,pa,pb),duration,1e-8);
        Assert.Equal(expected,result.Status);
        if(expected==ContactSlipStatus.Clear) Assert.Equal(duration,result.Time);
        else
        {
            Near(2/17.15,result.Time,1e-9);
            Assert.InRange(gap.SlipAt(pa,pb,result.Time).Slip.Length,0,1e-8);
            var before=a.Snapshot(); a.Advance(pa,result.Time);
            Near(0,a.PointVelocity(new(a.Center.X,0,0)).X,1e-8);
            Assert.NotEqual(before,a.Snapshot());
        }
    }

    [Fact]
    public void RotatingFeatureCannotHideAnInteriorBoundaryBehindClearEndpoints()
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.At(new(1,.5,0)),default,default);
        var b=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.At(new(0,-.5,0)),default,new(0,0,2*Math.PI));
        var gap=Gap(a,b); var pa=a.CreateTrajectory(1,default); var pb=b.CreateTrajectory(1,default);
        var start=gap.SlipAt(pa,pb,0).Slip; var end=gap.SlipAt(pa,pb,1).Slip;
        Assert.True(CollisionVector.Dot(start,end)>0);
        var result=ContactSlipSweep.Cast(Path(gap,pa,pb),1,1e-8);
        Assert.Equal(ContactSlipStatus.Boundary,result.Status);
        Assert.InRange(result.Time,0.0,.5);
    }

    [Fact]
    public void SlipDerivativeAndBoundsFollowTheCommittedAnisotropicState()
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,.5,0)),
            new(.7,.1,.2),new(.3,.4,-.6),1,new(.1,.2,.3));
        var b=Ground(); var gap=Gap(a,b);
        var pa=a.CreateTrajectory(.04,new(new(1,-2,.5),new(.3,.7,-.2))); var pb=b.CreateTrajectory(.04,default);
        var bound=gap.SlipBounds(pa,pb,0,.04);
        Assert.NotNull(bound);
        for(var i=1;i<20;i++)
        {
            var time=i*.002; const double h=1e-6;
            var sample=gap.SlipAt(pa,pb,time);
            var difference=(gap.SlipAt(pa,pb,time+h).Slip-gap.SlipAt(pa,pb,time-h).Slip)/(2*h);
            Near(0,(difference-sample.Derivative).Length,1e-7);
            Assert.InRange(sample.Derivative.Length,0,bound.Value.Rate);
            var curvature=(gap.SlipAt(pa,pb,time+h).Derivative-gap.SlipAt(pa,pb,time-h).Derivative)/(2*h);
            Assert.InRange(curvature.Length,0,bound.Value.Curvature+1e-7);
            var omegaDifference=(pa.PhysicalAngularVelocityAt(time+h)-pa.PhysicalAngularVelocityAt(time-h))/(2*h);
            Near(0,(omegaDifference-pa.PhysicalAngularAccelerationAt(time)).Length,1e-7);
            Assert.InRange(pa.PhysicalAngularVelocityAt(time).Length,0,pa.PhysicalAngularSpeedBound);
            Assert.InRange(pa.PhysicalAngularAccelerationAt(time).Length,0,pa.PhysicalAngularAccelerationBound);
            var angularCurvature=(pa.PhysicalAngularAccelerationAt(time+h)-pa.PhysicalAngularAccelerationAt(time-h))/(2*h);
            Assert.InRange(angularCurvature.Length,0,pa.PhysicalAngularCurvatureBound+1e-7);
        }
    }

    [Theory]
    [InlineData(ContactGapTests.GeometryCase.RoundedPointFace,false)]
    [InlineData(ContactGapTests.GeometryCase.RoundedPointFace,true)]
    [InlineData(ContactGapTests.GeometryCase.RoundedPoints,false)]
    [InlineData(ContactGapTests.GeometryCase.RoundedPoints,true)]
    [InlineData(ContactGapTests.GeometryCase.RoundedPointEdge,false)]
    [InlineData(ContactGapTests.GeometryCase.RoundedPointEdge,true)]
    [InlineData(ContactGapTests.GeometryCase.CrossedRoundedEdges,false)]
    [InlineData(ContactGapTests.GeometryCase.CrossedRoundedEdges,true)]
    [InlineData(ContactGapTests.GeometryCase.ParallelRoundedEdges,false)]
    [InlineData(ContactGapTests.GeometryCase.ParallelRoundedEdges,true)]
    public void FeatureBoundsEncloseEverySampledCommittedSlipDerivative(ContactGapTests.GeometryCase geometry,bool reversed)
    {
        ConvexInstance Segment(CollisionVector axis)=>new(new ConvexRounded(new ConvexHull([-axis,axis]),.25),AffineTransform.Identity);
        var (sa,sb,height)=geometry switch
        {
            ContactGapTests.GeometryCase.RoundedPointFace=>(Sphere,Floor,1.0),
            ContactGapTests.GeometryCase.RoundedPoints=>(Sphere,Sphere,1.0),
            ContactGapTests.GeometryCase.RoundedPointEdge=>(Sphere,Segment(new(2,0,0)),.75),
            ContactGapTests.GeometryCase.CrossedRoundedEdges=>(Segment(new(2,0,0)),Segment(new(0,0,2)),.5),
            ContactGapTests.GeometryCase.ParallelRoundedEdges=>(Segment(new(2,0,0)),Segment(new(2,0,0)),.5),
            _=>throw new ArgumentOutOfRangeException(nameof(geometry))
        };
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,height,0)),
            new(.2,.1,.3),new(.3,.4,-.6),1,new(.1,.2,.3));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.At(default),new(-.1,.2,.1),new(.1,-.3,.2));
        var gaps=reversed?ContactGap.Query(b,sb,a,sa,.001,1e-9):ContactGap.Query(a,sa,b,sb,.001,1e-9); Assert.NotEmpty(gaps);
        var pa=a.CreateTrajectory(.01,new(new(1,-2,.5),new(.3,.7,-.2))); var pb=b.CreateTrajectory(.01,default);
        if(reversed) (pa,pb)=(pb,pa);
        foreach(var gap in gaps)
        {
            var bound=gap.SlipBounds(pa,pb,0,.01); Assert.NotNull(bound);
            for(var i=1;i<100;i++)
            {
                var t=i*.0001; const double h=1e-7;
                var sample=gap.SlipAt(pa,pb,t);
                var derivative=(gap.SlipAt(pa,pb,t+h).Slip-gap.SlipAt(pa,pb,t-h).Slip)/(2*h);
                Near(0,(sample.Derivative-derivative).Length,1e-6);
                Assert.InRange(sample.Derivative.Length,0,bound.Value.Rate);
                var curvature=(gap.SlipAt(pa,pb,t+h).Derivative-gap.SlipAt(pa,pb,t-h).Derivative)/(2*h);
                Assert.InRange(curvature.Length,0,bound.Value.Curvature+1e-6);
            }
        }
    }

    [Theory]
    [InlineData(10,.00001)]
    [InlineData(10,.0000001)]
    [InlineData(1000,.00001)]
    [InlineData(1000,.0000001)]
    public void AlmostRollingCancellationIsCertifiedWithoutTinyTimeSteps(double speed,double slip)
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.At(new(0,.5,0)),
            new(speed+slip,0,0),new(0,0,-2*speed));
        var b=Ground(); var gap=Gap(a,b);
        var pa=a.CreateTrajectory(.01,default); var pb=b.CreateTrajectory(.01,default);
        var bounds=gap.SlipBounds(pa,pb,0,.01)!.Value;
        Assert.True(bounds.Rate>100);
        Assert.InRange(bounds.Curvature,0,1e-10);
        var result=ContactSlipSweep.Cast(Path(gap,pa,pb),.01,1e-8);
        Assert.Equal(ContactSlipStatus.Clear,result.Status);
        Assert.Equal(.01,result.Time); Assert.Equal(1,result.Iterations);
        Near(slip,gap.SlipAt(pa,pb,.01).Slip.X,1e-10);
    }

    [Fact]
    public void CurvatureCertificatesCannotCrossSpinSegmentsButSweepTraversesThem()
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,.5,0)),
            new(2,0,0),new(0,20,0),1,new(.1,.1,.1));
        var b=Ground(); var gap=Gap(a,b);
        var pa=a.CreateTrajectory(.02,new(default,new(0,.4,0))); var pb=b.CreateTrajectory(.02,default);
        Assert.True(pa.SegmentCount>1);
        Assert.Throws<ArgumentException>(()=>gap.SlipBounds(pa,pb,0,.02));
        var end=pa.SegmentEndAfter(0);
        Assert.NotNull(gap.SlipBounds(pa,pb,0,end));
        Assert.NotNull(gap.SlipBounds(pa,pb,end,pa.SegmentEndAfter(end)));
        var result=ContactSlipSweep.Cast(Path(gap,pa,pb),.02,1e-8);
        Assert.Equal(ContactSlipStatus.Clear,result.Status);
        Assert.True(result.Iterations>=pa.SegmentCount);
        Near(2,gap.SlipAt(pa,pb,.02).Slip.Length);
    }

    [Fact]
    public void UncertifiedWholeIntervalIsSubdividedRatherThanAssumedClear()
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.At(new(0,1,0)),new(1,-4,0),default);
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var gap=Assert.Single(ContactGap.Query(a,Sphere,b,Sphere,.001,1e-9));
        var pa=a.CreateTrajectory(.5,default); var pb=b.CreateTrajectory(.5,default);
        Assert.Null(gap.SlipBounds(pa,pb,0,.5));
        var hit=ContactSlipSweep.Cast(Path(gap,pa,pb),.5,1e-8);
        Assert.Equal(ContactSlipStatus.Boundary,hit.Status);
        Near(.25,hit.Time,1e-8);
    }

    [Fact]
    public void PredictionLocatesStoppingBeforeItsOriginalMidpointWithoutChangingTheSource()
    {
        var a=Ball(.001); var b=Ground();
        var pair=new PersistentContactPair(a,Sphere,b,Floor,new(0,0,.5),.01,.1,[]);
        var before=a.Snapshot();
        var prediction=AccelerationSolver.Predict(new PhysicsLoadSet {},new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),[a,b],[],[pair],
            new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,new(new(0,-9.8,0),default)},{b.Id,default}},
            .01,.01,1e-7,1e-8,1e-9,[],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>());
        Assert.Equal(ForcePredictionBoundary.Friction,prediction.Boundary);
        Near(.001/17.15,prediction.Duration,1e-9);
        Assert.True(prediction.Duration<.005); Assert.Equal(before,a.Snapshot());
        Assert.All(prediction.Trajectories.Values,path=>Assert.Equal(prediction.Duration,path.Duration));
        a.Advance(prediction.Trajectories[a.Id],prediction.Duration);
        Near(0,a.LinearVelocity.X+.5*a.AngularVelocity.Z,1e-8);
    }

    [Fact]
    public void ClearIntervalsAndInvalidOwnershipAreExplicit()
    {
        var a=Ball(2); var b=Ground(); var gap=Gap(a,b);
        var pa=a.CreateTrajectory(.1,default); var pb=b.CreateTrajectory(.1,default);
        Assert.Equal(ContactSlipStatus.Clear,ContactSlipSweep.Cast(Path(gap,pa,pb),.1,1e-8).Status);
        Assert.Throws<ArgumentOutOfRangeException>(()=>ContactSlipSweep.Cast(Path(gap,pa,pb),.2,1e-8));
        Assert.Throws<ArgumentOutOfRangeException>(()=>ContactSlipSweep.Cast(Path(gap,pa,pb),.1,0));
        Assert.Throws<InvalidOperationException>(()=>ContactSlipSweep.Cast(Path(gap,Ball(2).CreateTrajectory(.1,default),pb),.1,1e-8));
    }
}
