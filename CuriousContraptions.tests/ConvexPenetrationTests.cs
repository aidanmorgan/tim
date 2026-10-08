using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ConvexPenetrationTests
{
    public enum Shape { Sphere, Box, Hull }
    private static ConvexGeometry Geometry(Shape shape)=>shape switch
    {
        Shape.Sphere=>new ConvexSphere(.8),
        Shape.Box=>new ConvexBox(new(1,.9,.7)),
        Shape.Hull=>new ConvexHull([new(-1,-.9,-.7),new(1,-.9,-.7),new(-1,.9,-.7),new(1,.9,-.7),
            new(-1,-.9,.7),new(1,-.9,.7),new(-1,.9,.7),new(1,.9,.7)]),
        _=>throw new ArgumentOutOfRangeException(nameof(shape))
    };
    private readonly record struct Offset(IConvexSupport Shape,CollisionVector Translation) : IConvexSupport
    {
        public CollisionVector Support(CollisionVector direction)=>Shape.Support(direction)+Translation;
        public InteriorBall InteriorBall=>new(Shape.InteriorBall.Center+Translation,Shape.InteriorBall.Radius);
    }

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
    public void OnePenetrationAlgorithmHandlesEveryShapePair(Shape first,Shape second)
    {
        var a=new ConvexInstance(Geometry(first),AffineTransform.Identity);
        var b=new ConvexInstance(Geometry(second),new(SceneGeometryAdapter.CaptureBasis(Basis.FromEuler(new(.1f,.2f,.3f))),new(.4f,.2f,.1f)));
        var result=ConvexPenetration.Query(a,b);
        Assert.Equal(ConvexPenetrationStatus.Penetrating,result.Status);
        Assert.InRange(result.UpperDepth-result.LowerDepth,0,ConvexDistance.DefaultTolerance+1e-12);
        Assert.InRange(Math.Abs(result.Normal.Length-1),0,1e-12);
        Assert.InRange((result.PointA-result.PointB+result.Normal*result.LowerDepth).Length,0,2e-7);
        var moved=new Offset(a,result.Normal*(result.UpperDepth+.00001));
        Assert.True(ConvexDistance.Query(moved,b).LowerBound>.000009);
        var reversed=ConvexPenetration.Query(b,a);
        Assert.InRange(Math.Abs(result.UpperDepth-reversed.UpperDepth),0,2e-7);
    }

    [Theory]
    [InlineData(13)]
    [InlineData(91)]
    public void SphereDepthBoundsMatchAnIndependentAnalyticAnswerIncludingConcentric(int seed)
    {
        var random=new Random(seed);
        for(var i=0;i<100;i++)
        {
            var center=i==0?default:new CollisionVector(random.NextDouble()-.5,random.NextDouble()-.5,random.NextDouble()-.5);
            var a=new ConvexInstance(new ConvexSphere(.6),AffineTransform.Identity);
            var b=new Offset(new ConvexInstance(new ConvexSphere(.7),AffineTransform.Identity),center);
            var expected=1.3-center.Length;
            var result=ConvexPenetration.Query(a,b);
            Assert.Equal(ConvexPenetrationStatus.Penetrating,result.Status);
            Assert.InRange(result.LowerDepth,expected-1e-10,expected+1e-10);
            Assert.InRange(result.UpperDepth,expected-1e-10,expected+1e-10);
            Assert.Equal(0,result.Iterations); // Geometric bound is exact, not a pair-specific branch.
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.2)]
    [InlineData(.8)]
    public void RotatedBoxesAgreeWithSeparatingAxisDepth(float angle)
    {
        var halfA=new Vector3(1,2,3); var halfB=new Vector3(.5f,.7f,.8f);
        var poseA=Transform3D.Identity;
        var poseB=new Transform3D(Basis.FromEuler(new(angle,.17f,-.12f)),new(.8f,.2f,.3f));
        var a=new ConvexInstance(new ConvexBox(SceneGeometryAdapter.CaptureVector(halfA)),SceneGeometryAdapter.CaptureAffine(poseA));
        var b=new ConvexInstance(new ConvexBox(SceneGeometryAdapter.CaptureVector(halfB)),SceneGeometryAdapter.CaptureAffine(poseB));
        var axesA=new[]{Vector3.Right,Vector3.Up,Vector3.Back}.Select(SceneGeometryAdapter.CaptureVector).ToArray();
        var axesB=new[]{poseB.Basis.X,poseB.Basis.Y,poseB.Basis.Z}.Select(SceneGeometryAdapter.CaptureVector).ToArray();
        var axes=axesA.Concat(axesB).Concat(axesA.SelectMany(x=>axesB.Select(y=>CollisionVector.Cross(x,y))));
        double expected=double.PositiveInfinity;
        foreach(var axis in axes)
        {
            if(axis.Length<1e-12) continue;
            var n=axis/axis.Length;
            var ra=Math.Abs(CollisionVector.Dot(n,axesA[0]))*halfA.X+Math.Abs(CollisionVector.Dot(n,axesA[1]))*halfA.Y+Math.Abs(CollisionVector.Dot(n,axesA[2]))*halfA.Z;
            var rb=Math.Abs(CollisionVector.Dot(n,axesB[0]))*halfB.X+Math.Abs(CollisionVector.Dot(n,axesB[1]))*halfB.Y+Math.Abs(CollisionVector.Dot(n,axesB[2]))*halfB.Z;
            expected=Math.Min(expected,ra+rb-Math.Abs(CollisionVector.Dot(SceneGeometryAdapter.CaptureVector(poseB.Origin),n)));
        }
        var result=ConvexPenetration.Query(a,b);
        Assert.Equal(ConvexPenetrationStatus.Penetrating,result.Status);
        Assert.InRange(result.LowerDepth,expected-2e-7,expected+2e-7);
        Assert.InRange(result.UpperDepth,expected-2e-7,expected+2e-7);
    }

    [Theory]
    [InlineData(42)]
    [InlineData(791)]
    public void RandomRotatedBoxesMatchIndependentSatAndSeparateWithReportedTranslation(int seed)
    {
        var random=new Random(seed);
        double Between(double lo,double hi)=>lo+random.NextDouble()*(hi-lo);
        var axes=new[]{new CollisionVector(1,0,0),new(0,1,0),new(0,0,1)};
        for(var i=0;i<250;i++)
        {
            var halfA=new CollisionVector(Between(.03,2),Between(.03,2),Between(.03,2));
            var halfB=new CollisionVector(Between(.03,2),Between(.03,2),Between(.03,2));
            var qa=RigidRotation.FromRotationVector(new(Between(-3,3),Between(-3,3),Between(-3,3)));
            var qb=RigidRotation.FromRotationVector(new(Between(-3,3),Between(-3,3),Between(-3,3)));
            var position=new CollisionVector(Between(-2,2),Between(-2,2),Between(-2,2));
            var bodyA=new PhysicsBody(new(0),PhysicsMotionType.Static,new(default,qa),default,default);
            var bodyB=new PhysicsBody(new(1),PhysicsMotionType.Static,new(position,qb),default,default);
            var a=new ConvexPose(new(new ConvexBox(halfA),AffineTransform.Identity),bodyA.Pose);
            var b=new ConvexPose(new(new ConvexBox(halfB),AffineTransform.Identity),bodyB.Pose);
            var aa=axes.Select(qa.Apply).ToArray(); var bb=axes.Select(qb.Apply).ToArray();
            double expected=double.PositiveInfinity;
            foreach(var axis in aa.Concat(bb).Concat(aa.SelectMany(x=>bb.Select(y=>CollisionVector.Cross(x,y)))))
            {
                if(axis.Length<1e-12) continue;
                var n=axis/axis.Length;
                double Radius(CollisionVector half,CollisionVector[] basis)=>
                    Math.Abs(CollisionVector.Dot(n,basis[0]))*half.X+
                    Math.Abs(CollisionVector.Dot(n,basis[1]))*half.Y+
                    Math.Abs(CollisionVector.Dot(n,basis[2]))*half.Z;
                expected=Math.Min(expected,Radius(halfA,aa)+Radius(halfB,bb)-Math.Abs(CollisionVector.Dot(n,position)));
            }
            var result=ConvexPenetration.Query(a,b);
            if(expected<0) Assert.Equal(ConvexPenetrationStatus.Separated,result.Status);
            else
            {
                Assert.Equal(ConvexPenetrationStatus.Penetrating,result.Status);
                Assert.True(Math.Abs(result.LowerDepth-expected)<2e-7,$"seed={seed}, sample={i}, lower={result.LowerDepth}, expected={expected}");
                Assert.InRange(Math.Abs(result.UpperDepth-expected),0,2e-7);
                Assert.True(ConvexDistance.Query(new Offset(a,result.Normal*(result.UpperDepth+.00001)),b).LowerBound>.000009);
            }
        }
    }

    [Fact]
    public void SeparatedAndTouchingCasesDoNotInventPenetration()
    {
        var a=new ConvexInstance(new ConvexBox(new(1,1,1)),AffineTransform.Identity);
        var separated=new Offset(a,new(3,0,0));
        var touch=new Offset(a,new(2,0,0));
        Assert.Equal(ConvexPenetrationStatus.Separated,ConvexPenetration.Query(a,separated).Status);
        var result=ConvexPenetration.Query(a,touch);
        Assert.Equal(ConvexPenetrationStatus.WithinTolerance,result.Status);
        Assert.InRange(result.UpperDepth,0,ConvexDistance.DefaultTolerance);
        Assert.True(result.Normal.X<-.99);
    }

    private readonly record struct InvalidInteriorBound : IConvexSupport
    {
        public InteriorBall InteriorBall=>new(default,2);
        public CollisionVector Support(CollisionVector direction)=>direction/direction.Length;
    }

    [Fact]
    public void InvalidBoundsAndToleranceFailExplicitly()
    {
        Assert.Throws<ArgumentException>(()=>new InteriorBall(default,-1));
        Assert.Throws<ArgumentException>(()=>new InteriorBall(new(double.NaN,0,0),1));
        Assert.Throws<InvalidOperationException>(()=>ConvexPenetration.Query(new InvalidInteriorBound(),new InvalidInteriorBound()));
        var shape=new ConvexInstance(new ConvexSphere(1),AffineTransform.Identity);
        Assert.Throws<ArgumentOutOfRangeException>(()=>ConvexPenetration.Query(shape,shape,0));
    }

    [Fact]
    public void LowerDimensionalHullHasZeroPenetrationRatherThanArtificialThickness()
    {
        var plane=new ConvexInstance(new ConvexHull([new(-1,-1,0),new(1,-1,0),new(1,1,0),new(-1,1,0)]),AffineTransform.Identity);
        var result=ConvexPenetration.Query(plane,plane);
        Assert.Equal(ConvexPenetrationStatus.WithinTolerance,result.Status);
        Assert.Equal(0,result.UpperDepth);
        Assert.InRange(Math.Abs(result.Normal.Z),.999999,1);
        var point=new ConvexInstance(new ConvexHull([default]),AffineTransform.Identity);
        var coincident=ConvexPenetration.Query(point,point);
        Assert.Equal(0,coincident.UpperDepth);
        Assert.Equal(new CollisionVector(-1,0,0),coincident.Normal); // Deterministic member of the non-unique supporting normals.
    }
}
