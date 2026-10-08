using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class SupportFootprintTests
{
    [Fact]
    public void SphereReportsExactBoundsAndLowestPoint()
    {
        var shape=new CompoundGeometry([new(new ConvexSphere(.25),AffineTransform.Identity)]);
        var sample=SupportFootprint.Sample(shape,RigidPose.At(new(1,2,3)),RigidPose.Identity);
        Assert.Equal(new CollisionVector(1,1.75,3),sample.LowestPoint);
        Assert.Equal(.75,sample.MinimumX);Assert.Equal(1.25,sample.MaximumX);
        Assert.Equal(2.75,sample.MinimumZ);Assert.Equal(3.25,sample.MaximumZ);
        Assert.Equal(.25,sample.RoundingRadius);
        Assert.True(sample.Fits(2,4));Assert.False(sample.Fits(1,4));
    }

    [Fact]
    public void RotatedBoxUsesSupportingFaceCentroidRatherThanAnArbitraryCorner()
    {
        var shape=new CompoundGeometry([new(new ConvexBox(new(.2,.7,.3)),AffineTransform.Identity)]);
        var pose=new RigidPose(new(1,2,3),RigidRotation.FromRotationVector(new(0,0,Math.PI/2)));
        var sample=SupportFootprint.Sample(shape,pose,RigidPose.Identity);
        Assert.InRange((sample.LowestPoint-new CollisionVector(1,1.8,3)).Length,0,1e-12);
        Assert.InRange(Math.Abs(sample.MinimumX-.3),0,1e-12);
        Assert.InRange(Math.Abs(sample.MaximumX-1.7),0,1e-12);
        Assert.Equal(0,sample.RoundingRadius);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompoundAggregatesTiedFeaturesAndSelectsDeepestFeature(bool lowered)
    {
        var shape=new CompoundGeometry([
            new(new ConvexSphere(.1),new(AffineBasis.Identity,new(-.4f,0,0))),
            new(new ConvexSphere(.1),new(AffineBasis.Identity,new(.4f,lowered?-.2f:0,0)))]);
        var sample=SupportFootprint.Sample(shape,RigidPose.Identity,RigidPose.Identity);
        Assert.InRange(Math.Abs(sample.LowestPoint.X-(lowered?.4:0)),0,1e-7);
        Assert.InRange(Math.Abs(sample.LowestPoint.Y-(lowered?-.3:-.1)),0,1e-7);
        Assert.InRange(Math.Abs(sample.MinimumX+.5),0,1e-7);
        Assert.InRange(Math.Abs(sample.MaximumX-.5),0,1e-7);
    }

    [Fact]
    public void CommonRigidTransformPreservesFrameLocalFootprint()
    {
        var shape=new CompoundGeometry([new(new ConvexBox(new(.2,.7,.3)),AffineTransform.Identity)]);
        var frame=new RigidPose(new(4,-2,7),RigidRotation.FromRotationVector(new(.2,.5,-.3)));
        var local=new RigidPose(new(.1,2,-.2),RigidRotation.FromRotationVector(new(.1,.3,.2)));
        var world=new RigidPose(frame.TransformPoint(local.Center),frame.Rotation*local.Rotation);
        var a=SupportFootprint.Sample(shape,local,RigidPose.Identity);
        var b=SupportFootprint.Sample(shape,world,frame);
        Assert.InRange((a.LowestPoint-b.LowestPoint).Length,0,1e-12);
        Assert.InRange(Math.Abs(a.MinimumX-b.MinimumX)+Math.Abs(a.MaximumX-b.MaximumX)+
            Math.Abs(a.MinimumZ-b.MinimumZ)+Math.Abs(a.MaximumZ-b.MaximumZ),0,1e-12);
    }

    [Fact]
    public void InvalidFramesRejectInsteadOfInventingSupport()
    {
        var shape=new CompoundGeometry([new(new ConvexSphere(.25),AffineTransform.Identity)]);
        Assert.Throws<ArgumentException>(()=>SupportFootprint.Sample(shape,default,RigidPose.Identity));
        Assert.Throws<ArgumentException>(()=>SupportFootprint.Sample(shape,RigidPose.Identity,default));
        Assert.Throws<ArgumentNullException>(()=>SupportFootprint.Sample(null!,RigidPose.Identity,RigidPose.Identity));
    }
}
