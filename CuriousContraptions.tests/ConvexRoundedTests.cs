using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ConvexRoundedTests
{
    [Fact]
    public void RoundedGeometryAddsBallSupportAndRetainsFeatureIdentities()
    {
        var core=new ConvexBox(new(1,2,3)); var rounded=new ConvexRounded(core,.2);
        var random=new Random(811);
        for(var i=0;i<200;i++)
        {
            var direction=new CollisionVector(random.NextDouble()-.5,random.NextDouble()-.5,random.NextDouble()-.5);
            var expected=core.Support(direction)+direction*(.2/direction.Length);
            Assert.InRange((rounded.Support(direction)-expected).Length,0,1e-12);
        }
        var normal=new CollisionVector(0,1,0);
        var before=core.SupportingFeature(normal,1e-10); var after=rounded.SupportingFeature(normal,1e-10);
        Assert.Equal(before.Vertices.Length,after.Vertices.Length);
        for(var i=0;i<before.Vertices.Length;i++)
        {
            Assert.Equal(before.Vertices[i].Id,after.Vertices[i].Id);
            Assert.Equal(before.Vertices[i].Point+normal*.2,after.Vertices[i].Point);
        }
        Assert.Equal(.2,rounded.RoundingRadius); Assert.Equal(1.2,rounded.InteriorBall.Radius);
        Assert.Equal(core.BoundingRadius+.2,rounded.BoundingRadius);
    }

    [Fact]
    public void NestedRoundedShapesUseTheSameDistanceAndPenetrationPipeline()
    {
        var rounded=new ConvexRounded(new ConvexSphere(.3),.2);
        var a=new ConvexInstance(rounded,AffineTransform.Identity);
        var b=new ConvexInstance(new ConvexSphere(.2),new(AffineBasis.Identity,new(1,0,0)));
        var distance=ConvexSeparation.Query(a,b);
        Assert.InRange(Math.Abs(distance.UpperBound-.3),0,1e-7);
        var overlap=new ConvexInstance(new ConvexSphere(.2),new(AffineBasis.Identity,new(.6f,0,0)));
        var separation=ConvexSeparation.Query(a,overlap);
        Assert.InRange(Math.Abs(separation.UpperBound+.1),0,1e-6);
        Assert.Equal(.5,rounded.RoundingRadius); Assert.Equal(.5,rounded.InteriorBall.Radius);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidRoundingIsRejected(double radius)
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ConvexRounded(new ConvexBox(new(1,1,1)),radius));
    }
}
