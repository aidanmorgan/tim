using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ConvexSweepTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    [InlineData(20000)]
    public void PureRotationFindsTheObstacleMissedByPackagedBackendCcd(double spin)
    {
        var beam=new ConvexInstance(new ConvexBox(new(2,.05,.05)),Transform3D.Identity);
        var obstaclePose=new Transform3D(Basis.Identity,new(1.5f*Mathf.Cos(.4f),1.5f*Mathf.Sin(.4f),0));
        var obstacle=new ConvexInstance(new ConvexBox(new(.05,.05,.05)),obstaclePose);
        var rotating=new ConvexMotion(beam,default,default,new(0,0,spin));
        var fixedBody=new ConvexMotion(obstacle,CollisionVector.From(obstaclePose.Origin),default,default);
        var hit=ConvexSweep.Cast(rotating,fixedBody,1/spin);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        // Retained analytic solver is an independent test oracle, not runtime dispatch.
        var reference=RotatingBoxObstacleSweep.Cast(Vector3.Zero,Vector3.Back,Transform3D.Identity,
            new(2,.05f,.05f),spin,obstaclePose,new(.05f,.05f,.05f),1/spin);
        Assert.Equal(SphereSweepStatus.Contact,reference.Status);
        Assert.InRange(Math.Abs(hit.Time-reference.Time)*spin,0,.0002);
        Assert.InRange(hit.Time*spin,.3,.4);
        Assert.InRange(hit.Separation.UpperBound,0,ConvexSweep.ContactDistance+ConvexDistance.DefaultTolerance);
        Assert.InRange(hit.Iterations,1,100);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(400)]
    public void BothMovingBodiesContributeToTheSameTimeOfImpact(double speed)
    {
        var geometry=new ConvexSphere(.5);
        var a=new ConvexMotion(new(geometry,new(Basis.Identity,new(-2,0,0))),new(-2,0,0),new(speed,0,0),default);
        var b=new ConvexMotion(new(geometry,new(Basis.Identity,new(2,0,0))),new(2,0,0),new(-speed,0,0),default);
        var hit=ConvexSweep.Cast(a,b,2/speed);
        var expected=(3-ConvexSweep.ContactDistance)/(2*speed);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Assert.InRange(Math.Abs(hit.Time-expected)*speed,0,.000001);
        var swapped=ConvexSweep.Cast(b,a,2/speed);
        Assert.InRange(Math.Abs(hit.Time-swapped.Time),0,1e-10);
    }

    [Fact]
    public void MovingApartAndDepthMissAreClearWithoutEndpointAssumptions()
    {
        var geometry=new ConvexBox(new(2,.05,.05));
        var beam=new ConvexMotion(new(geometry,Transform3D.Identity),default,default,new(0,0,10));
        var missed=new ConvexMotion(new(new ConvexBox(new(.05,.05,.05)),new(Basis.Identity,new(1,1,2))),new(1,1,2),default,default);
        Assert.Equal(ConvexSweepStatus.Clear,ConvexSweep.Cast(beam,missed,.2).Status);
        var a=new ConvexMotion(new(new ConvexSphere(.5),Transform3D.Identity),default,new(-2,0,0),default);
        var b=new ConvexMotion(new(new ConvexSphere(.5),new(Basis.Identity,new(2,0,0))),new(2,0,0),new(2,0,0),default);
        Assert.Equal(ConvexSweepStatus.Clear,ConvexSweep.Cast(a,b,2).Status);
    }

    [Fact]
    public void InitialContactDoesNotPretendToSupplyPenetrationDepth()
    {
        var a=new ConvexMotion(new(new ConvexBox(new(1,1,1)),Transform3D.Identity),default,default,default);
        var b=new ConvexMotion(new(new ConvexSphere(.5),Transform3D.Identity),default,default,default);
        var result=ConvexSweep.Cast(a,b,1);
        Assert.Equal(ConvexSweepStatus.InitialContact,result.Status);
        Assert.Equal(0,result.Time);
        Assert.Equal(ConvexDistanceStatus.WithinTolerance,result.Separation.Status);
    }

    [Fact]
    public void RotatedHullUsesTheSameSweepWithoutAddingAnAlgorithm()
    {
        var hull=new ConvexHull([new(-1,0,0),new(1,0,0),new(0,1,0),new(0,0,1)]);
        var moving=new ConvexMotion(new(hull,new(Basis.FromEuler(new(.3f,.2f,.1f)),Vector3.Zero)),default,new(8,0,0),new(.2,.4,.6));
        var stationary=new ConvexMotion(new(new ConvexBox(new(.1,5,5)),new(Basis.Identity,new(4,0,0))),new(4,0,0),default,default);
        var hit=ConvexSweep.Cast(moving,stationary,1);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Assert.InRange(hit.Time,.3,.6);
    }

    [Fact]
    public void InvalidMotionCannotReturnClear()
    {
        Assert.Throws<ArgumentException>(()=>new ConvexMotion(default,default,default,default));
        var shape=new ConvexInstance(new ConvexSphere(1),Transform3D.Identity);
        Assert.Throws<ArgumentException>(()=>new ConvexMotion(shape,default,new(double.NaN,0,0),default));
        var motion=new ConvexMotion(shape,default,default,default);
        Assert.Throws<ArgumentOutOfRangeException>(()=>ConvexSweep.Cast(motion,motion,-1));
    }
}
