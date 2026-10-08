using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class RoundedSeparationTests
{
    private static ConvexPose At(ConvexGeometry geometry,RigidPose pose)=>
        new(new(geometry,AffineTransform.Identity),pose);
    private static void Near(CollisionVector expected,CollisionVector actual,double tolerance=1e-8)=>
        Assert.InRange((expected-actual).Length,0,tolerance);

    [Theory]
    [InlineData(83)]
    [InlineData(142)]
    public void RandomSphereContactsHaveAnalyticSurfaceAnchorsAndNormals(int seed)
    {
        var random=new Random(seed);
        for(var i=0;i<300;i++)
        {
            var direction=new CollisionVector(random.NextDouble()-.5,random.NextDouble()-.5,random.NextDouble()-.5);
            direction/=direction.Length;
            var distance=.3+random.NextDouble()*1.10005;
            var a=At(new ConvexSphere(.7),RigidPose.Identity);
            var b=At(new ConvexSphere(.7),RigidPose.At(direction*distance));
            var result=ConvexSeparation.Query(a,b);
            Assert.InRange(Math.Abs(result.LowerBound-(distance-1.4)),0,1e-10);
            Assert.InRange(Math.Abs(result.UpperBound-(distance-1.4)),0,1e-10);
            Near(-direction,result.Normal);
            Near(direction*.7,result.PointA); Near(direction*(distance-.7),result.PointB);
            var manifold=ContactManifold.Query(a,b);
            var point=Assert.Single(manifold.Points.ToArray());
            Near(direction*.7,point.PointA); Near(direction*(distance-.7),point.PointB);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.00005)]
    [InlineData(-.05)]
    public void CapsuleSideContactsRetainTheirLineExtent(double gap)
    {
        var capsule=At(new Capsule(.5,1),RigidPose.At(new(0,.5+gap,0)));
        var floor=At(new ConvexBox(new(5,.5,5)),RigidPose.At(new(0,-.5,0)));
        var manifold=ContactManifold.Query(capsule,floor);
        Assert.Equal(2,manifold.Points.Length);
        foreach(var point in manifold.Points)
        {
            Assert.InRange(Math.Abs(point.PointA.X),.999999999,1.000000001);
            Assert.InRange(Math.Abs(point.PointA.Y-gap),0,1e-8);
            Assert.InRange(Math.Abs(point.PointB.Y),0,1e-8);
        }
    }

    [Fact]
    public void RoundingDecompositionPreservesFloatBasisDistortionAndConvexSupport()
    {
        var instance=new ConvexInstance(new ConvexSphere(.7),
            new(SceneGeometryAdapter.CaptureBasis(Basis.FromEuler(new(.2f,.4f,.7f))),new(3,2,1)));
        Assert.True(instance.RoundingRadius>0);
        Assert.True(instance.RoundingRadius<=instance.InteriorBall.Radius);
        var random=new Random(703);
        CollisionVector Core(CollisionVector n)=>instance.Support(n)-n*(instance.RoundingRadius/n.Length);
        for(var i=0;i<500;i++)
        {
            var a=new CollisionVector(random.NextDouble()-.5,random.NextDouble()-.5,random.NextDouble()-.5);
            var b=new CollisionVector(random.NextDouble()-.5,random.NextDouble()-.5,random.NextDouble()-.5);
            var p=Core(a); var q=Core(b);
            Assert.True(CollisionVector.Dot(a,p-q)>=-1e-12);
            Near(instance.Support(a),p+a*(instance.RoundingRadius/a.Length),1e-12);
        }
    }

    [Fact]
    public void ZeroRadiusKeepsPolyhedralSignedBoundsAndWitnesses()
    {
        var a=At(new ConvexBox(new(1,1,1)),RigidPose.Identity);
        var b=At(new ConvexBox(new(1,1,1)),RigidPose.At(new(.3,1.8,.1)));
        var signed=ConvexSeparation.Query(a,b); var penetration=ConvexPenetration.Query(a,b);
        Assert.Equal(-penetration.UpperDepth,signed.LowerBound);
        Assert.Equal(-penetration.LowerDepth,signed.UpperBound);
        Assert.Equal(penetration.Normal,signed.Normal);
        Assert.Equal(penetration.PointA,signed.PointA);
        Assert.Equal(penetration.PointB,signed.PointB);
    }

    [Fact]
    public void DynamicSphereImpactConservesMomentumEnergyAndHasNoArtificialSpin()
    {
        var direction=new CollisionVector(.3,.4,.5); direction/=direction.Length;
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(-direction*2),direction*100,default,2,new(.2,.2,.2));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(direction*2),-direction*50,default,3,new(.3,.3,.3));
        var geometry=new CompoundGeometry([new(new ConvexSphere(.5),AffineTransform.Identity)]);
        var world=new PhysicsWorld([],[new(a,geometry,new(1,0,0)),new(b,geometry,new(1,0,0))],[],new(default,maximumStep:1));
        var momentum=a.LinearVelocity*2+b.LinearVelocity*3;
        var energy=a.LinearVelocity.LengthSquared+1.5*b.LinearVelocity.LengthSquared;
        Assert.Equal(1,world.Step([],[],.03).Events);
        Near(momentum,a.LinearVelocity*2+b.LinearVelocity*3);
        Assert.InRange(Math.Abs(energy-a.LinearVelocity.LengthSquared-1.5*b.LinearVelocity.LengthSquared),0,1e-8);
        Near(default,a.AngularVelocity); Near(default,b.AngularVelocity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1e-10)]
    public void CoincidentRoundedCoresProduceCertifiedSurfaceWitnesses(double offset)
    {
        var a=At(new ConvexSphere(.7),RigidPose.Identity);
        var b=At(new ConvexSphere(.3),RigidPose.At(new(offset,0,0)));
        var result=ConvexSeparation.Query(a,b);
        Assert.Equal(ConvexSeparationStatus.Penetrating,result.Status);
        Assert.InRange(result.Normal.Length,1-1e-12,1+1e-12);
        Assert.InRange(offset-1,result.LowerBound,result.UpperBound);
        Near(result.PointA-result.PointB,-result.Normal*(1-offset));
        Assert.Equal(ContactManifoldStatus.Contact,ContactManifold.Query(a,b).Status);
    }

    private sealed class Capsule(double radius,double halfLength) : ConvexGeometry
    {
        public override CollisionVector CoreSupport(CollisionVector direction)=>new(direction.X<0?-halfLength:halfLength,0,0);
        public override double BoundingRadius=>radius+halfLength;
        public override double RoundingRadius=>radius;
        public override InteriorBall InteriorBall=>new(default,radius);
        public override CollisionVector Support(CollisionVector direction)
        {
            var normal=SupportFeature.UnitDirection(direction,0);
            return normal*radius+new CollisionVector(direction.X<0?-halfLength:halfLength,0,0);
        }
        public override SupportFeature SupportingFeature(CollisionVector direction,double planeTolerance)
        {
            var normal=SupportFeature.UnitDirection(direction,planeTolerance);
            return SupportFeature.FromPoints([normal*radius+new CollisionVector(-halfLength,0,0),
                normal*radius+new CollisionVector(halfLength,0,0)],normal,planeTolerance);
        }
    }
}
