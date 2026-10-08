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
        var a=new ConvexInstance(Geometry(first),AffineTransform.Identity);
        var b=new ConvexInstance(Geometry(second),new(AffineBasis.Identity,new(3,0,0)));
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
        var a=new ConvexInstance(new ConvexSphere(.5),AffineTransform.Identity);
        var b=new ConvexInstance(new ConvexSphere(.5),new(AffineBasis.Identity,new((float)offset,0,0)));
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
            var a=new ConvexInstance(new ConvexSphere(ra),new(AffineBasis.Identity,SceneGeometryAdapter.CaptureVector(p)));
            var b=new ConvexInstance(new ConvexSphere(rb),new(AffineBasis.Identity,SceneGeometryAdapter.CaptureVector(q)));
            var expected=Math.Max(0,(SceneGeometryAdapter.CaptureVector(p)-SceneGeometryAdapter.CaptureVector(q)).Length-ra-rb);
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
            var a=new ConvexInstance(shape,new(SceneGeometryAdapter.CaptureBasis(Basis.FromEuler(new(.2f,.4f,.1f))),new(0,0,0)));
            var b=new ConvexInstance(new ConvexBox(new(.4,.7,.2)),new(SceneGeometryAdapter.CaptureBasis(Basis.FromEuler(new(-.3f,.2f,.6f))),new(Between(2,4),Between(-2,2),Between(-2,2))));
            var ab=ConvexDistance.Query<ConvexInstance,ConvexInstance>(a,b);
            var ba=ConvexDistance.Query<ConvexInstance,ConvexInstance>(b,a);
            Assert.InRange(Math.Abs(ab.UpperBound-ba.UpperBound),0,2e-7);
            var moved=new Transform3D(Basis.FromEuler(new(.31f,-.67f,.28f)),new(3,4,-2));
            var transformed=ConvexDistance.Query<ConvexInstance,ConvexInstance>(new(shape,SceneGeometryAdapter.CaptureAffine(moved*a.Pose.ToScene())),new(b.Geometry,SceneGeometryAdapter.CaptureAffine(moved*b.Pose.ToScene())));
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
        var result=ConvexDistance.Query<ConvexInstance,ConvexInstance>(new(hull,AffineTransform.Identity),
            new(new ConvexHull([new(0,0,0)]),new(AffineBasis.Identity,new(1,3,0))));
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
        Assert.Throws<ArgumentException>(()=>new ConvexInstance(new ConvexSphere(1),new(SceneGeometryAdapter.CaptureBasis(Basis.Identity.Scaled(new(2,1,1))),SceneGeometryAdapter.CaptureVector(Vector3.Zero))));
        Assert.Throws<InvalidOperationException>(()=>ConvexDistance.Query<ConvexInstance,ConvexInstance>(default,new(new ConvexSphere(1),AffineTransform.Identity)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>ConvexDistance.Query<ConvexInstance,ConvexInstance>(
            new(new ConvexSphere(1),AffineTransform.Identity),new(new ConvexSphere(1),AffineTransform.Identity),0));
    }
    public enum FaceRegression { Light, Wall }
    [Theory]
    [InlineData(FaceRegression.Light)]
    [InlineData(FaceRegression.Wall)]
    public void NearDiagonalFaceProjectionConvergesWithoutDemotingToItsEdge(FaceRegression fixture)
    {
        var point=fixture switch
        {
            FaceRegression.Light=>new CollisionVector(2.899999948509883,4.896184427805594,-.10381557219440532),
            FaceRegression.Wall=>new CollisionVector(.6953250882215798,4,.6953250882215798),
            _=>throw new ArgumentOutOfRangeException(nameof(fixture))
        };
        CollisionVector[] vertices=fixture switch
        {
            FaceRegression.Light=>[
                new(2.8999999985098834,7,1.9999999999999996),
                new(2.8999999985098843,3,-1.9999999999999996),
                new(2.8999999985098834,3,1.9999999999999996)],
            FaceRegression.Wall=>[
                new(1.555634895752624,2.5,-1.2727922345413707),
                new(-1.2727921735155152,5.5,1.5556349456828693),
                new(-1.2727921735155152,2.5,1.5556349456828693)],
            _=>throw new ArgumentOutOfRangeException(nameof(fixture))
        };
        var normal=CollisionVector.Cross(vertices[1]-vertices[0],vertices[2]-vertices[0]);
        normal/=normal.Length;
        var analytic=Math.Abs(CollisionVector.Dot(point-vertices[0],normal));
        var a=new ConvexInstance(new ConvexHull([point]),AffineTransform.Identity);
        foreach(var reverse in new[]{false,true})
        {
            if(reverse) Array.Reverse(vertices);
            var b=new ConvexInstance(new ConvexHull(vertices),AffineTransform.Identity);
            var result=ConvexDistance.Query(a,b,2.5e-8);
            Assert.Equal(ConvexDistanceStatus.Separated,result.Status);
            Assert.InRange(analytic,result.LowerBound-1e-12,result.UpperBound+1e-12);
            Assert.InRange(result.UpperBound-result.LowerBound,0,2.5e-8);
            Assert.InRange(result.Iterations,1,16);
        }
    }

}
