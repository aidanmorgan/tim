using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ConvexDistanceTests
{
    public enum Shape { Sphere, Box, Hull }
    private static ConvexGeometry Geometry(Shape shape)=>shape switch
    {
        Shape.Sphere=>new ConvexSphere(.5),
        Shape.Box=>new ConvexBox(new(.5,.5,.5)),
        Shape.Hull=>new ConvexHull([
            new(-.5,-.5,-.5),new(.5,-.5,-.5),new(-.5,.5,-.5),new(.5,.5,-.5),
            new(-.5,-.5,.5),new(.5,-.5,.5),new(-.5,.5,.5),new(.5,.5,.5)]),
        _=>throw new ArgumentOutOfRangeException(nameof(shape))
    };

    [Theory]
    [InlineData(Shape.Sphere,Shape.Sphere)]
    [InlineData(Shape.Sphere,Shape.Box)]
    [InlineData(Shape.Sphere,Shape.Hull)]
    [InlineData(Shape.Box,Shape.Sphere)]
    [InlineData(Shape.Box,Shape.Box)]
    [InlineData(Shape.Box,Shape.Hull)]
    [InlineData(Shape.Hull,Shape.Sphere)]
    [InlineData(Shape.Hull,Shape.Box)]
    [InlineData(Shape.Hull,Shape.Hull)]
    public void SameQueryHandlesEveryShapePairAndProvidesDistanceBounds(Shape first,Shape second)
    {
        var a=new ConvexInstance(Geometry(first),Transform3D.Identity);
        var b=new ConvexInstance(Geometry(second),new(Basis.Identity,new(3,0,0)));
        var result=ConvexDistance.Query<ConvexInstance,ConvexInstance>(a,b);
        Assert.Equal(ConvexDistanceStatus.Separated,result.Status);
        Assert.InRange(result.LowerBound,2-1e-7,2+1e-12);
        Assert.InRange(result.UpperBound,2-1e-12,2+1e-7);
        Assert.InRange(result.UpperBound-result.LowerBound,0,1e-7);
        Assert.InRange(result.Normal.X,-1,-.99999);
        Assert.InRange((result.PointA-result.PointB).Length,result.UpperBound-1e-12,result.UpperBound+1e-12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.7)]
    [InlineData(1)]
    public void TouchingAndPenetrationAreReportedOnlyAsWithinTolerance(double offset)
    {
        var a=new ConvexInstance(new ConvexSphere(.5),Transform3D.Identity);
        var b=new ConvexInstance(new ConvexSphere(.5),new(Basis.Identity,new((float)offset,0,0)));
        var result=ConvexDistance.Query<ConvexInstance,ConvexInstance>(a,b);
        Assert.Equal(ConvexDistanceStatus.WithinTolerance,result.Status);
        Assert.InRange(result.UpperBound,0,ConvexDistance.DefaultTolerance);
        Assert.Equal(default,result.Normal); // No invented penetration normal.
    }

    [Theory]
    [InlineData(13)]
    [InlineData(71)]
    public void RandomSpherePairsAgreeWithIndependentAnalyticDistance(int seed)
    {
        var random=new Random(seed);
        float Between(float lo,float hi)=>lo+random.NextSingle()*(hi-lo);
        for(var i=0;i<200;i++)
        {
            var p=new Vector3(Between(-3,3),Between(-3,3),Between(-3,3));
            var q=new Vector3(Between(-3,3),Between(-3,3),Between(-3,3));
            var ra=Between(.1f,1); var rb=Between(.1f,1);
            var a=new ConvexInstance(new ConvexSphere(ra),new(Basis.Identity,p));
            var b=new ConvexInstance(new ConvexSphere(rb),new(Basis.Identity,q));
            var expected=Math.Max(0,(CollisionVector.From(p)-CollisionVector.From(q)).Length-ra-rb);
            var result=ConvexDistance.Query<ConvexInstance,ConvexInstance>(a,b);
            Assert.True(result.LowerBound<=expected+1e-10,$"lower bound seed={seed}, index={i}");
            Assert.True(result.UpperBound>=expected-1e-10,$"upper bound seed={seed}, index={i}");
            Assert.InRange(result.UpperBound-result.LowerBound,0,ConvexDistance.DefaultTolerance);
        }
    }

    [Theory]
    [InlineData(17)]
    [InlineData(83)]
    public void ArbitraryHullQueriesAreSymmetricAndRigidTransformInvariant(int seed)
    {
        var random=new Random(seed);
        float Between(float lo,float hi)=>lo+random.NextSingle()*(hi-lo);
        for(var i=0;i<100;i++)
        {
            var points=Enumerable.Range(0,12).Select(_=>new CollisionVector(Between(-1,1),Between(-1,1),Between(-1,1))).ToArray();
            var shape=new ConvexHull(points);
            var a=new ConvexInstance(shape,new(Basis.FromEuler(new(.2f,.4f,.1f)),new(0,0,0)));
            var b=new ConvexInstance(new ConvexBox(new(.4,.7,.2)),new(Basis.FromEuler(new(-.3f,.2f,.6f)),new(Between(2,4),Between(-2,2),Between(-2,2))));
            var ab=ConvexDistance.Query<ConvexInstance,ConvexInstance>(a,b);
            var ba=ConvexDistance.Query<ConvexInstance,ConvexInstance>(b,a);
            Assert.InRange(Math.Abs(ab.UpperBound-ba.UpperBound),0,2e-7);
            var moved=new Transform3D(Basis.FromEuler(new(.31f,-.67f,.28f)),new(3,4,-2));
            var transformed=ConvexDistance.Query<ConvexInstance,ConvexInstance>(new(shape,moved*a.Pose),new(b.Geometry,moved*b.Pose));
            Assert.InRange(Math.Abs(ab.UpperBound-transformed.UpperBound),0,.000002);
            Assert.InRange(ab.Iterations,1,256);
        }
    }

    [Fact]
    public void HullOwnsItsInputAndSupportsDegenerateFeatures()
    {
        CollisionVector[] points=[new(0,0,0),new(1,0,0),new(2,0,0),new(2,0,0)];
        var hull=new ConvexHull(points);
        points[0]=new(100,0,0);
        var result=ConvexDistance.Query<ConvexInstance,ConvexInstance>(new(hull,Transform3D.Identity),
            new(new ConvexHull([new(0,0,0)]),new(Basis.Identity,new(1,3,0))));
        Assert.InRange(result.LowerBound,3-1e-7,3);
        Assert.InRange(result.UpperBound,3,3+1e-7);
    }

    [Fact]
    public void InvalidGeometryAndUndefinedInstancesAreExplicitErrors()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ConvexSphere(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ConvexBox(new(1,0,1)));
        Assert.Throws<ArgumentException>(()=>new ConvexHull([]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ConvexHull([new(double.NaN,0,0)]));
        Assert.Throws<ArgumentException>(()=>new ConvexInstance(new ConvexSphere(1),new(Basis.Identity.Scaled(new(2,1,1)),Vector3.Zero)));
        Assert.Throws<InvalidOperationException>(()=>ConvexDistance.Query<ConvexInstance,ConvexInstance>(default,new(new ConvexSphere(1),Transform3D.Identity)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>ConvexDistance.Query<ConvexInstance,ConvexInstance>(
            new(new ConvexSphere(1),Transform3D.Identity),new(new ConvexSphere(1),Transform3D.Identity),0));
    }
}
