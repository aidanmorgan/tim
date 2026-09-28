using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class SignedSweepTests
{
    private const double MinimumSeparation=-1e-6;
    private static ConvexMotion Motion(ConvexGeometry shape,RigidPose pose,CollisionVector velocity=default,
        CollisionVector spin=default,double duration=1,CollisionVector localOffset=default)=>
        new(new(shape,new(Basis.Identity,new((float)localOffset.X,(float)localOffset.Y,(float)localOffset.Z))),
            new PhysicsBody(new(0),PhysicsMotionType.Kinematic,pose,velocity,spin).CreateTrajectory(duration));

    [Theory]
    [InlineData(3,1,ConvexSeparationStatus.Separated)]
    [InlineData(2,0,ConvexSeparationStatus.WithinTolerance)]
    [InlineData(1.5,-.5,ConvexSeparationStatus.Penetrating)]
    public void SignedIntervalsContainTheAnalyticSphereSeparation(double offset,double expected,ConvexSeparationStatus status)
    {
        var shape=new ConvexSphere(1);
        var result=ConvexSeparation.Query(Motion(shape,RigidPose.Identity).At(0),Motion(shape,RigidPose.At(new(offset,0,0))).At(0));
        Assert.Equal(status,result.Status);
        Assert.InRange(result.LowerBound,expected-1e-7,expected+1e-12);
        Assert.InRange(result.UpperBound,expected-1e-12,expected+1e-7);
        Assert.True(result.Normal.X<-.99);
    }

    [Fact]
    public void FastTangentialSlidingAndCentredSpinAreCertifiedWithoutTinySteps()
    {
        var floor=Motion(new ConvexBox(new(100,1,100)),RigidPose.At(new(0,-1,0)));
        var ball=Motion(new ConvexSphere(.5),RigidPose.At(new(0,.5,0)),new(50,0,0),new(0,0,1000000));
        Assert.Equal(0,ball.RotationalReach);
        var spin=ConvexSweep.Cast(ball,floor,1,MinimumSeparation);
        Assert.Equal(ConvexSweepStatus.Clear,spin.Status); Assert.Equal(1,spin.Iterations);
        var box=Motion(new ConvexBox(new(.5,.5,.5)),RigidPose.At(new(0,.5,0)),new(50,0,0));
        var slide=ConvexSweep.Cast(box,floor,1,MinimumSeparation);
        Assert.Equal(ConvexSweepStatus.Clear,slide.Status); Assert.Equal(1,slide.Iterations);
        Assert.Equal(1,slide.Time);
    }

    [Fact]
    public void SeparationAndFurtherPenetrationAreDifferentEvents()
    {
        var shape=new ConvexSphere(1);
        var obstacle=Motion(shape,RigidPose.At(new(2,0,0)));
        var apart=Motion(shape,RigidPose.Identity,new(-2,0,0));
        var clear=ConvexSweep.Cast(apart,obstacle,1,MinimumSeparation);
        Assert.Equal(ConvexSweepStatus.Clear,clear.Status);
        Assert.InRange(clear.Separation.UpperBound,1.9999999,2.0000001);
        var closing=Motion(shape,RigidPose.Identity,new(2,0,0));
        var hit=ConvexSweep.Cast(closing,obstacle,1,MinimumSeparation);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status); Assert.True(hit.Time>0);
        Assert.InRange(hit.Time,(-MinimumSeparation-1e-7)/2,(-MinimumSeparation+1e-7)/2);
        Assert.InRange(hit.Separation.LowerBound,MinimumSeparation-1e-7,MinimumSeparation+1e-7);
        var deep=Motion(shape,RigidPose.At(new(.01,0,0)));
        Assert.Equal(ConvexSweepStatus.InitialContact,ConvexSweep.Cast(deep,obstacle,1,MinimumSeparation).Status);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    [InlineData(20000)]
    public void InitiallyTouchingOrbitReleasesThenFindsItsLaterCollision(double spin)
    {
        var shape=new ConvexSphere(.2);
        var duration=2*Math.PI/spin;
        var orbit=Motion(shape,RigidPose.Identity,spin:new(0,0,spin),duration:duration,localOffset:new(1,0,0));
        var obstacle=Motion(shape,RigidPose.At(new(1,-.4,0)),duration:duration);
        Assert.Equal(1,orbit.RotationalReach);
        var initial=ConvexSeparation.Query(orbit.At(0),obstacle.At(0));
        Assert.InRange(initial.UpperBound,0,1e-7);
        Assert.True(ConvexSeparation.Query(orbit.At(Math.PI/spin),obstacle.At(Math.PI/spin)).LowerBound>1);
        var hit=ConvexSweep.Cast(orbit,obstacle,duration,MinimumSeparation);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        var analyticAngle=2*Math.PI-2*Math.Atan(.4);
        Assert.InRange(hit.Time*spin,analyticAngle-1e-6,analyticAngle+1e-5);
        Assert.InRange(hit.Separation.UpperBound,MinimumSeparation-1e-7,MinimumSeparation+1e-7);
        Assert.InRange(hit.Iterations,2,300);
        // The signed distance stays above the requested boundary at all sampled
        // earlier points, including after the initial contact has released.
        for(var i=1;i<200;i++)
            Assert.True(ConvexSeparation.Query(orbit.At(hit.Time*i/200),obstacle.At(hit.Time*i/200)).LowerBound>MinimumSeparation);
    }

    [Fact]
    public void TangentialRotationalReleaseAndReturnDoNotInventPenetration()
    {
        var duration=2*Math.PI;
        var shape=new ConvexSphere(.2);
        var orbit=Motion(shape,RigidPose.Identity,spin:new(0,0,1),duration:duration,localOffset:new(0,1,0));
        var obstacle=Motion(shape,RigidPose.At(new(0,1.4,0)),duration:duration);
        var result=ConvexSweep.Cast(orbit,obstacle,duration,MinimumSeparation);
        Assert.Equal(ConvexSweepStatus.Clear,result.Status);
        Assert.InRange(result.Separation.UpperBound,-1e-7,1e-7);
    }

    [Fact]
    public void RotationRadiusBoundsCoverRoundedGeometryAndFloatBasisAnisotropy()
    {
        var random=new Random(901);
        ConvexGeometry[] shapes=[new ConvexSphere(.7),new ConvexBox(new(1,.8,.3)),
            new ConvexHull([new(-1,0,0),new(1,0,0),new(0,1,0),new(0,0,1)])];
        foreach(var shape in shapes)
        {
            var basis=Basis.FromEuler(new(.3f,.6f,.1f));
            var instance=new ConvexInstance(shape,new(basis,new(.2f,.1f,.3f)));
            var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.Identity,default,new(.2,.7,-.3));
            var path=body.CreateTrajectory(1); var motion=new ConvexMotion(instance,path);
            var bound=motion.AngularSpeedBound*motion.RotationalReach;
            for(var i=0;i<200;i++)
            {
                var n=new CollisionVector(random.NextDouble()-.5,random.NextDouble()-.5,random.NextDouble()-.5); n/=n.Length;
                var t=random.NextDouble()*.99; const double step=1e-5;
                var from=CollisionVector.Dot(n,motion.At(t).Support(n));
                var to=CollisionVector.Dot(n,motion.At(t+step).Support(n));
                Assert.InRange(Math.Abs(to-from),0,bound*step+1e-12);
            }
        }
        var slightlyScaled=new Basis(new Vector3(1.000001f,0,0),Vector3.Up,Vector3.Back);
        var ellipsoid=new ConvexInstance(new ConvexSphere(1),new(slightlyScaled,Vector3.Zero));
        Assert.True(ellipsoid.RotationRadiusBound>0);
        Assert.InRange(ellipsoid.RotationRadiusBound,0,1e-5);
    }

    [Fact]
    public void CompoundSignedSweepKeepsChildIdentityAcrossAnExistingContact()
    {
        var duration=2*Math.PI;
        var geometry=new CompoundGeometry([new(new ConvexSphere(.2),new(Basis.Identity,new(1,0,0)))]);
        var rotating=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.Identity,default,new(0,0,1));
        var fixedBody=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(1,-.4,0)),default,default);
        var hit=CompoundCollision.Cast(new(geometry,rotating.CreateTrajectory(duration)),
            new(new([new(new ConvexSphere(.2),Transform3D.Identity)]),fixedBody.CreateTrajectory(duration)),duration,MinimumSeparation);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Assert.Equal(new ColliderChildId(0),hit.ChildA); Assert.Equal(new ColliderChildId(0),hit.ChildB);
        Assert.InRange(hit.Time,5.5,5.6);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void BroadFlatFacesResolveTouchAndTinyClearanceWithoutWideningTolerance(double halfWidth)
    {
        var floor=Motion(new ConvexBox(new(halfWidth,1,halfWidth)),RigidPose.At(new(0,-1,0)));
        foreach(var gap in new[]{0.0,1e-5})
        {
            var ball=Motion(new ConvexSphere(.5),RigidPose.At(new(halfWidth*.5,.5+gap,0)));
            var result=ConvexDistance.Query(ball.At(0),floor.At(0),2.5e-8);
            Assert.InRange(result.UpperBound,gap-1e-12,gap+2.5e-8);
            if(gap>0)
            {
                var plane=CollisionVector.Dot(result.Normal,ball.At(0).Support(-result.Normal)-floor.At(0).Support(result.Normal));
                Assert.InRange(result.UpperBound-plane,0,2.5e-8+1e-12);
            }
        }
    }

    private sealed class InvalidRounding : ConvexGeometry
    {
        public override double BoundingRadius=>1;
        public override double RoundingRadius=>2;
        public override InteriorBall InteriorBall=>new(default,1);
        public override CollisionVector Support(CollisionVector direction)=>direction/direction.Length;
        public override SupportFeature SupportingFeature(CollisionVector direction,double tolerance)=>new([new(new(0),Support(direction))]);
    }
    [Fact]
    public void InconsistentRoundedGeometryIsRejectedAtTheDeclarationBoundary()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ConvexInstance(new InvalidRounding(),Transform3D.Identity));
    }

    [Fact]
    public void NonfiniteThresholdAndUndefinedPlaneCannotReturnClear()
    {
        var shape=Motion(new ConvexSphere(1),RigidPose.Identity);
        Assert.Throws<ArgumentOutOfRangeException>(()=>ConvexSweep.Cast(shape,shape,1,double.NaN));
        var point=Motion(new ConvexHull([default]),RigidPose.Identity);
        Assert.Throws<InvalidOperationException>(()=>ConvexSweep.Cast(point,point,1,MinimumSeparation));
    }
}
