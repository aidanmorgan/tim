using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class CylindricalRegionTests
{
    public enum Shape { Sphere, Box, Hull, RoundedBox, Compound }
    private static CompoundGeometry Geometry(Shape shape)=>shape switch
    {
        Shape.Sphere=>new([new(new ConvexSphere(.5),AffineTransform.Identity)]),
        Shape.Box=>new([new(new ConvexBox(new(.2,.3,.4)),AffineTransform.Identity)]),
        Shape.Hull=>new([new(new ConvexHull([new(-.2,.3,.4),new(.2,-.3,.4),new(0,0,-.4)]),AffineTransform.Identity)]),
        Shape.RoundedBox=>new([new(new ConvexRounded(new ConvexBox(new(.2,.3,.4)),.1),AffineTransform.Identity)]),
        Shape.Compound=>new([
            new(new ConvexSphere(.1),new(AffineBasis.Identity,new(0,.5f,0))),
            new(new ConvexSphere(.1),new(AffineBasis.Identity,new(0,-.5f,0)))]),
        _=>throw new ArgumentOutOfRangeException(nameof(shape))
    };

    [Theory]
    [InlineData(Shape.Sphere)]
    [InlineData(Shape.Box)]
    [InlineData(Shape.Hull)]
    [InlineData(Shape.RoundedBox)]
    [InlineData(Shape.Compound)]
    public void RadialBoundsMatchFullGeometryAndContainmentHasANegativeControl(Shape shape)
    {
        var geometry=Geometry(shape);
        var expected=shape is Shape.RoundedBox or Shape.Compound?.6:.5;
        var extent=CylindricalRegion.Measure(geometry,RigidPose.Identity,RigidPose.Identity);
        Assert.InRange(expected,extent.RadialLowerBound-1e-12,extent.RadialUpperBound+1e-12);
        Assert.InRange(extent.RadialUpperBound-extent.RadialLowerBound,0,1e-7);
        Assert.True(new CylindricalRegion(1,expected).Contains(extent,1e-7));
        Assert.False(new CylindricalRegion(1,expected-.01).Contains(extent,1e-7));
        Assert.True(new CylindricalRegion(1,expected).Overlaps(geometry,RigidPose.Identity,RigidPose.Identity));
    }

    [Theory]
    [InlineData(Shape.Sphere)]
    [InlineData(Shape.Box)]
    [InlineData(Shape.Hull)]
    [InlineData(Shape.RoundedBox)]
    [InlineData(Shape.Compound)]
    public void RigidFrameChangesPreserveExtentAndRegionOccupancy(Shape shape)
    {
        var geometry=Geometry(shape);
        var frame=new RigidPose(new(2,7,-3),RigidRotation.FromRotationVector(new(.2,.4,-.6)));
        var relative=new RigidPose(new(.2,.1,.15),RigidRotation.FromRotationVector(new(.6,0,0)));
        var body=new RigidPose(frame.TransformPoint(relative.Center),frame.Rotation*relative.Rotation);
        var expected=CylindricalRegion.Measure(geometry,relative,RigidPose.Identity);
        var actual=CylindricalRegion.Measure(geometry,body,frame);
        Assert.InRange((actual.Bounds.Minimum-expected.Bounds.Minimum).Length,0,1e-12);
        Assert.InRange((actual.Bounds.Maximum-expected.Bounds.Maximum).Length,0,1e-12);
        Assert.InRange(Math.Abs(actual.RadialLowerBound-expected.RadialLowerBound),0,1e-7);
        Assert.InRange(Math.Abs(actual.RadialUpperBound-expected.RadialUpperBound),0,1e-7);
        var region=new CylindricalRegion(.8,.8);
        Assert.Equal(region.Overlaps(geometry,relative,RigidPose.Identity),region.Overlaps(geometry,body,frame));
    }

    [Theory]
    [InlineData(.65,0,true,false)]
    [InlineData(.8,0,false,false)]
    [InlineData(0,.6,true,false)]
    [InlineData(0,.8,false,false)]
    [InlineData(0,0,true,true)]
    public void RegionHasFiniteCapsAndDistinguishesOccupancyFromContainment(double x,double y,bool overlaps,bool contains)
    {
        var geometry=new CompoundGeometry([new(new ConvexSphere(.2),AffineTransform.Identity)]);
        var pose=RigidPose.At(new(x,y,0));
        var region=new CylindricalRegion(.5,.5);
        Assert.Equal(overlaps,region.Overlaps(geometry,pose,RigidPose.Identity));
        Assert.Equal(contains,region.Contains(CylindricalRegion.Measure(geometry,pose,RigidPose.Identity),1e-7));
    }

    [Fact]
    public void CompoundGapsRemainEmptyAndOffsetChildrenExtendAxialBounds()
    {
        var gap=new CompoundGeometry([
            new(new ConvexSphere(.1),new(AffineBasis.Identity,new(0,2,0))),
            new(new ConvexSphere(.1),new(AffineBasis.Identity,new(0,-2,0)))]);
        var region=new CylindricalRegion(.5,.5);
        Assert.False(region.Overlaps(gap,RigidPose.Identity,RigidPose.Identity));
        var longBody=new CompoundGeometry([
            new(new ConvexSphere(.1),AffineTransform.Identity),
            new(new ConvexBox(new(.1,.1,.1)),new(AffineBasis.Identity,new(1,0,0)))]);
        Assert.True(region.Overlaps(longBody,RigidPose.Identity,RigidPose.Identity));
        var extent=CylindricalRegion.Measure(longBody,RigidPose.Identity,RigidPose.Identity);
        Assert.Equal(1.1,extent.Bounds.Maximum.X);
        Assert.Equal(-.1,extent.Bounds.Minimum.X);
        Assert.False(region.Contains(extent,1e-7));
    }

    [Fact]
    public void TransverseCornersAreNotReplacedByAnInscribedOrBoundingSphere()
    {
        var box=new CompoundGeometry([new(new ConvexBox(new(.2,.35,.35)),AffineTransform.Identity)]);
        var extent=CylindricalRegion.Measure(box,RigidPose.Identity,RigidPose.Identity);
        Assert.True(extent.RadialLowerBound>.49);
        Assert.False(new CylindricalRegion(.5,.48).Contains(extent,1e-7));
        var thin=new CompoundGeometry([new(new ConvexBox(new(.45,.1,.1)),AffineTransform.Identity)]);
        var slender=CylindricalRegion.Measure(thin,RigidPose.Identity,RigidPose.Identity);
        Assert.True(new CylindricalRegion(.5,.2).Contains(slender,1e-7));
    }

    [Fact]
    public void InvalidRegionsFramesBoundsAndPrecisionRejectExplicitly()
    {
        var geometry=Geometry(Shape.Box);var region=new CylindricalRegion(1,1);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new CylindricalRegion(0,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new CylindricalRegion(1,double.NaN));
        Assert.Throws<InvalidOperationException>(()=>default(CylindricalRegion).Support(new(1,0,0)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>region.Support(new(double.NaN,0,0)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>CylindricalRegion.Measure(geometry,RigidPose.Identity,RigidPose.Identity,0));
        Assert.Throws<ArgumentNullException>(()=>CylindricalRegion.Measure(null!,RigidPose.Identity,RigidPose.Identity));
        Assert.Throws<ArgumentException>(()=>region.Overlaps(geometry,default,RigidPose.Identity));
        Assert.Throws<ArgumentException>(()=>region.Contains(new(new(new(double.NaN,0,0),default),0,0),0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Geometry((Shape)int.MaxValue));
        Assert.Equal(double.MaxValue,new CylindricalRegion(1,double.MaxValue).Support(new(0,2,0)).Y);
    }
}
