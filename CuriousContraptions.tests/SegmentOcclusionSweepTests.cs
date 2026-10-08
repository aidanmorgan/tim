using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class SegmentOcclusionSweepTests
{
    private sealed class LinearSegment(CollisionVector start,CollisionVector end,CollisionVector startVelocity,
        CollisionVector endVelocity,double duration) : SegmentTrajectory
    {
        public override double Duration=>duration;
        private void Validate(double time)
        {
            if(!double.IsFinite(time)||time<0||time>duration)throw new ArgumentOutOfRangeException(nameof(time));
        }
        public override CollisionSegment At(double time){Validate(time);return new(start+startVelocity*time,end+endVelocity*time);}
        public override double SegmentEndAfter(double time){Validate(time);return duration;}
        public override double SpeedBound(double first,double last)
        {
            Validate(first);Validate(last);
            if(last<first)throw new ArgumentOutOfRangeException(nameof(last));
            return Math.Max(startVelocity.Length,endVelocity.Length);
        }
    }
    private static ConvexMotion Sphere(CollisionVector center,CollisionVector velocity,double duration)
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.At(center),velocity,default);
        return new(new(new ConvexSphere(.1),AffineTransform.Identity),body.CreateTrajectory(duration,default));
    }
    [Theory]
    [InlineData(.3,-1,.2)]
    [InlineData(0,1,.1)]
    public void MovingBlockerFindsEntryAndExit(double y,double speed,double expected)
    {
        var segment=new LinearSegment(default,new(2,0,0),default,default,.6);
        var obstacle=Sphere(new(1,y,0),new(0,speed,0),.6);
        var hit=SegmentOcclusionSweep.Cast(segment,obstacle,.6,1e-7);
        Assert.Equal(ScalarSweepStatus.Boundary,hit.Status);
        Assert.InRange(hit.Time,expected,expected+1e-7);
        var at=ConvexSeparation.Query(segment.At(hit.Time),obstacle.At(hit.Time),1e-9);
        if(y==0)Assert.True(at.LowerBound>0);
        else Assert.True(at.UpperBound<0);
    }
    [Fact]
    public void MovingAndDeformingSegmentsUseTheirEndpointBounds()
    {
        var obstacle=Sphere(new(1,0,0),default,.6);
        var moving=new LinearSegment(new(0,.3,0),new(2,.3,0),new(0,-1,0),new(0,-1,0),.6);
        var moveHit=SegmentOcclusionSweep.Cast(moving,obstacle,.6,1e-7);
        Assert.Equal(ScalarSweepStatus.Boundary,moveHit.Status);
        Assert.InRange(moveHit.Time,.2,.2+1e-7);
        var deforming=new LinearSegment(default,new(2,.6,0),default,new(0,-2,0),.6);
        var hit=SegmentOcclusionSweep.Cast(deforming,obstacle,.6,1e-7);
        var expected=(.6-.2/Math.Sqrt(.99))/2;
        Assert.Equal(ScalarSweepStatus.Boundary,hit.Status);
        Assert.InRange(hit.Time,expected,expected+2e-7);
    }
    [Fact]
    public void StationaryClearAndBlockedSegmentsDoNotInventEvents()
    {
        var segment=new LinearSegment(default,new(2,0,0),default,default,1);
        foreach(var y in new[]{0.0,.5})
        {
            var hit=SegmentOcclusionSweep.Cast(segment,Sphere(new(1,y,0),default,1),1,1e-7);
            Assert.Equal(ScalarSweepStatus.Clear,hit.Status);Assert.Equal(1,hit.Time);
        }
        Assert.Throws<ArgumentOutOfRangeException>(()=>SegmentOcclusionSweep.Cast(segment,Sphere(default,default,1),2,1e-7));
        Assert.Throws<ArgumentOutOfRangeException>(()=>SegmentOcclusionSweep.Cast(segment,Sphere(default,default,1),1,0));
        Assert.Throws<ArgumentException>(()=>new CollisionSegment(default,new(double.NaN,0,0)));
    }

    [Fact]
    public void RotatingBoxUsesConvexSupportWithoutShapeSpecificSweep()
    {
        var segment=new LinearSegment(default,new(2,0,0),default,default,1);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.At(new(1,.4,0)),default,new(0,0,1));
        var obstacle=new ConvexMotion(new(new ConvexBox(new(.5,.02,.02)),AffineTransform.Identity),body.CreateTrajectory(1,default));
        var hit=SegmentOcclusionSweep.Cast(segment,obstacle,1,1e-7);
        var expected=Math.Asin(.4/Math.Sqrt(.25+.0004))-Math.Atan2(.02,.5);
        Assert.Equal(ScalarSweepStatus.Boundary,hit.Status);
        Assert.InRange(hit.Time,expected,expected+1e-6);
    }

    [Fact]
    public void TangentialTranslationDoesNotInventAnOcclusionChange()
    {
        var segment=new LinearSegment(default,new(2,0,0),default,default,.01);
        var obstacle=Sphere(new(1,.1,0),new(.1,0,0),.01);
        var hit=SegmentOcclusionSweep.Cast(segment,obstacle,.01,1e-7);
        Assert.Equal(ScalarSweepStatus.Clear,hit.Status);
        Assert.Equal(.01,hit.Time);
    }

    [Fact]
    public void ParallelMotionPastEndpointStillFindsExit()
    {
        var segment=new LinearSegment(default,new(2,0,0),default,default,1);
        var hit=SegmentOcclusionSweep.Cast(segment,Sphere(new(1,0,0),new(2,0,0),1),1,1e-7);
        Assert.Equal(ScalarSweepStatus.Boundary,hit.Status);
        Assert.InRange(hit.Time,.55,.55+1e-7);
    }
}
