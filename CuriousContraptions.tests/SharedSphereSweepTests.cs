using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class SharedSphereSweepTests
{
    private static ConvexMotion Motion(CollisionVector position,double radius,CollisionVector velocity,double duration)=>
        new(new(new ConvexSphere(radius),AffineTransform.Identity),
            new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.At(position),velocity,default).CreateTrajectory(duration,default));
    private static void Near(double expected,double actual,double tolerance=1e-7)=>
        Assert.InRange(Math.Abs(expected-actual),0,tolerance);

    [Theory]
    [InlineData(10d)]
    [InlineData(100d)]
    [InlineData(10000d)]
    public void OpposingFastBodiesCannotCrossUndetected(double speed)
    {
        var duration=10/speed;
        var hit=ConvexSweep.Cast(Motion(new(-5,0,0),.5,new(speed,0,0),duration),
            Motion(new(5,0,0),.5,new(-speed,0,0),duration),duration,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Near((9-ConvexSweep.ContactDistance)/2,hit.Time*speed);
        Assert.Equal(new CollisionVector(-1,0,0),hit.Separation.Normal);
        Assert.True(10-2*speed*hit.Time>=1);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(.7853981633974483)]
    [InlineData(1.5707963267948966)]
    public void PairOrderRotationAndSharedTranslationPreserveContact(double angle)
    {
        var rotation=RigidRotation.FromRotationVector(new(0,angle,0));
        var a=rotation.Apply(new(-3,0,0)); var b=rotation.Apply(new(3,0,0));
        var va=rotation.Apply(new(4,0,0)); var vb=rotation.Apply(new(-2,0,0));
        var shared=new CollisionVector(3,-5,7);
        var first=Motion(a,.3,va,2); var second=Motion(b,.7,vb,2);
        var hit=ConvexSweep.Cast(first,second,2,ConvexSweep.ContactDistance);
        var reverse=ConvexSweep.Cast(second,first,2,ConvexSweep.ContactDistance);
        var shifted=ConvexSweep.Cast(Motion(a,.3,va+shared,2),Motion(b,.7,vb+shared,2),2,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Near(hit.Time,reverse.Time); Near(hit.Time,shifted.Time);
        Near(0,(hit.Separation.Normal+reverse.Separation.Normal).Length);
    }

    [Theory]
    [InlineData(.99,ConvexSweepStatus.Contact)]
    [InlineData(1.01,ConvexSweepStatus.Clear)]
    public void GlancingIntersectionAndNearMissAreDistinguished(double height,ConvexSweepStatus expected)
    {
        var hit=ConvexSweep.Cast(Motion(new(-2,height,0),.5,new(4,0,0),1),
            Motion(default,.5,default,1),1,ConvexSweep.ContactDistance);
        Assert.Equal(expected,hit.Status);
        if(expected==ConvexSweepStatus.Contact)
        {
            Assert.InRange(hit.Time,.46,.47);
            Assert.True(hit.Separation.Normal.X<0); Assert.True(hit.Separation.Normal.Y>.98);
        }
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(-1e-20)]
    [InlineData(-1.401298464324817e-45)]
    public void InitialContactIsGeometricAndTheSignedSweepCertifiesSubsequentMotion(double velocity)
    {
        var a=Motion(new(1,0,0),.5,new(velocity,0,0),1);
        var b=Motion(default,.5,default,1);
        var initial=ConvexSweep.Cast(a,b,1,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.InitialContact,initial.Status); Assert.Equal(0,initial.Time);
        // Contact response uses velocity; the geometric query does not erase
        // stationary/tiny-speed pairs. Persistent travel uses an explicit signed boundary.
        var next=ConvexSweep.Cast(a,b,1,-1e-6);
        Assert.Equal(velocity < -1e-6?ConvexSweepStatus.Contact:ConvexSweepStatus.Clear,next.Status);
        if(next.Status==ConvexSweepStatus.Contact) Assert.True(next.Time>0);
    }

    [Theory]
    [InlineData(1e-10)]
    [InlineData(1d)]
    [InlineData(1e10)]
    public void ObliqueTangentAndInwardMotionUseTheSameSignedBoundary(double scale)
    {
        var normal=new CollisionVector(0,.951,.309);
        normal/=normal.Length;
        var tangent=CollisionVector.Cross(new(1,0,0),normal)*scale;
        var center=normal*.66;
        var duration=1/scale;
        foreach(var reverse in new[]{false,true})
        {
            var fixedBody=Motion(default,.32,default,duration);
            var moving=Motion(center,.34,tangent,duration);
            var clear=reverse?ConvexSweep.Cast(moving,fixedBody,duration,-1e-6):
                ConvexSweep.Cast(fixedBody,moving,duration,-1e-6);
            Assert.Equal(ConvexSweepStatus.Clear,clear.Status);
            moving=Motion(center,.34,tangent-normal*(.01*scale),duration);
            var contact=reverse?ConvexSweep.Cast(moving,fixedBody,duration,-1e-6):
                ConvexSweep.Cast(fixedBody,moving,duration,-1e-6);
            Assert.Equal(ConvexSweepStatus.Contact,contact.Status);
            Assert.True(contact.Time>0);
        }
    }

    [Theory]
    [InlineData(.5,-.5)]
    [InlineData(0d,-1d)]
    public void OverlapHasCertifiedDepthWithoutRepairOrInventedVelocityNormal(double distance,double depth)
    {
        var a=Motion(new(distance,0,0),.5,new(1,0,0),1);
        var b=Motion(default,.5,new(-1,0,0),1);
        var beforeA=a.At(0); var beforeB=b.At(0);
        var hit=ConvexSweep.Cast(a,b,0,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.InitialContact,hit.Status);
        Assert.Equal(ConvexSeparationStatus.Penetrating,hit.Separation.Status);
        Near(depth,hit.Separation.LowerBound); Near(depth,hit.Separation.UpperBound);
        Near(1,hit.Separation.Normal.Length);
        Assert.Equal(beforeA,a.At(0)); Assert.Equal(beforeB,b.At(0));
    }

    [Fact]
    public void EndpointContactAndLaterContactHaveDifferentResults()
    {
        var atEnd=ConvexSweep.Cast(Motion(new(-3,0,0),.5,new(2,0,0),1),Motion(default,.5,default,1),1,0);
        Assert.Equal(ConvexSweepStatus.Contact,atEnd.Status); Near(1,atEnd.Time);
        var later=ConvexSweep.Cast(Motion(new(-3,0,0),.5,new(1,0,0),1),Motion(default,.5,default,1),1,0);
        Assert.Equal(ConvexSweepStatus.Clear,later.Status);
    }

    [Fact]
    public void IdenticalVelocityAndRecedingBodiesStayClear()
    {
        var shared=new CollisionVector(1,1,1);
        Assert.Equal(ConvexSweepStatus.Clear,ConvexSweep.Cast(Motion(new(-3,0,0),.5,shared,10),
            Motion(default,.5,shared,10),10,ConvexSweep.ContactDistance).Status);
        Assert.Equal(ConvexSweepStatus.Clear,ConvexSweep.Cast(Motion(new(-3,0,0),.5,new(-1,0,0),10),
            Motion(default,.5,new(1,0,0),10),10,ConvexSweep.ContactDistance).Status);
    }

    [Fact]
    public void LargeFiniteCoordinatesDoNotOverflowFloatIntermediates()
    {
        var hit=ConvexSweep.Cast(Motion(new(-1e20,0,0),1e19,new(1e20,0,0),2),
            Motion(new(1e20,0,0),1e19,new(-1e20,0,0),2),2,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Near(.9,hit.Time,1e-6);
        Assert.Equal(new CollisionVector(-1,0,0),hit.Separation.Normal);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidRadiiRejectAtTheGeometryBoundary(double radius)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ConvexSphere(radius));

    [Theory]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidDurationRejectsAtTheCapturedTrajectoryBoundary(double duration)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>Motion(default,1,default,duration));

    [Fact]
    public void InvalidMotionRejectsBeforeTheSweep()
    {
        Assert.Throws<ArgumentException>(()=>Motion(new(double.NaN,0,0),1,default,1));
        Assert.Throws<ArgumentException>(()=>Motion(default,1,new(0,double.PositiveInfinity,0),1));
    }
}
