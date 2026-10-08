using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class MidpointConstraintEvaluationTests
{
    private static readonly CollisionVector X=new(1,0,0),Z=new(0,0,1);

    public enum MotionCoordinate { Translation, Rotation }
    [Theory]
    [InlineData(MotionCoordinate.Translation)]
    [InlineData(MotionCoordinate.Rotation)]
    public void HeldAccelerationCancelsBeforeSubUlpVelocityIncrement(MotionCoordinate coordinate)
    {
        if(!Enum.IsDefined(coordinate))throw new ArgumentOutOfRangeException(nameof(coordinate));
        var angular=coordinate==MotionCoordinate.Rotation;
        var time=Math.ScaleB(1,-60);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            angular?default:X,angular?Z:default,1,new(1,1,1));
        var path=body.CreateTrajectory(time,new(angular?default:X*13,angular?Z*13:default));
        var linear=angular?default:X;var rotation=angular?Z:default;
        var roundedSpeed=CollisionVector.Dot(linear,path.LinearVelocityAt(time))+
            CollisionVector.Dot(rotation,path.PhysicalAngularVelocityAt(time));
        var trial=CollisionVector.Dot(linear,path.LinearAccelerationAt(time))+
            CollisionVector.Dot(rotation,path.PhysicalAngularAccelerationAt(time));
        // A second identical initial-speed participant cancels the initial 1.
        // Exact affine velocity is 1+13t, so (v(t)-1)/t-13 is zero.
        Assert.Equal(1,roundedSpeed);
        Assert.Equal(-13,(roundedSpeed-1)/time-trial);
        var terms=path.ConstraintVelocityTerms(linear,rotation,time);
        Assert.Equal(0,(terms.Velocity-1)/time-terms.Acceleration);
        Assert.Equal(body.Snapshot().Pose,path.StartPose);
    }

    [Theory]
    [InlineData(.001)]
    [InlineData(.01)]
    [InlineData(.1)]
    public void AnisotropicHeldWrenchMatchesOriginalEquationWhenWellConditioned(double time)
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            new(.2,-.4,.3),new(.7,1.1,.8),2,new(1,2,3));
        var path=body.CreateTrajectory(time,new(new(3,-2,1),new(.4,-.3,.2)));
        var linear=new CollisionVector(.3,-.2,.5);var angular=new CollisionVector(-.4,.6,.2);
        var terms=path.ConstraintVelocityTerms(linear,angular,time);
        var direct=(CollisionVector.Dot(linear,path.LinearVelocityAt(time))+
            CollisionVector.Dot(angular,path.PhysicalAngularVelocityAt(time)))/time-
            CollisionVector.Dot(linear,path.LinearAccelerationAt(time))-
            CollisionVector.Dot(angular,path.PhysicalAngularAccelerationAt(time));
        // Agreement bound is binary64 rounding at the largest tested 1/t scale;
        // authoritative captures and Reset remain exact elsewhere.
        Assert.InRange(Math.Abs(terms.Velocity/time-terms.Acceleration-direct),0,2e-12);
    }

    [Fact]
    public void TorqueFreeAnisotropicGyroscopicTermMatchesEulerEquation()
    {
        // I=diag(1,2,3), omega=(1,1,1), L=(1,2,3).
        // At the identity, -I^-1(omega cross L)=(-1,1,-1/3).
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            default,new(1,1,1),1,new(1,2,3));
        var path=body.CreateTrajectory(0,default);
        Assert.Equal(-1,path.ConstraintVelocityTerms(default,X,0).Acceleration);
        Assert.Equal(1,path.ConstraintVelocityTerms(default,new(0,1,0),0).Acceleration);
        Assert.Equal(-1.0/3,path.ConstraintVelocityTerms(default,Z,0).Acceleration);
        Assert.Throws<ArgumentOutOfRangeException>(()=>path.ConstraintVelocityTerms(X,Z,-1));
        Assert.Throws<ArgumentException>(()=>path.ConstraintVelocityTerms(new(double.NaN,0,0),Z,0));
    }

    [Fact]
    public void PrescribedCurveUsesItsDeclaredVelocityAndAcceleration()
    {
        var motion=new PrescribedBodyMotion(new QuinticRigidTrajectory(RigidPose.Identity,X,Z*2,.8),
            RigidPose.Identity,0);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,motion.At(0),
            motion.LinearVelocityAt(0),motion.AngularVelocityAt(0),prescribedMotion:motion);
        var path=body.CreateTrajectory(.4,default);
        var terms=path.ConstraintVelocityTerms(X,Z,.2);
        Assert.Equal(CollisionVector.Dot(X,motion.LinearVelocityAt(.2))+
            CollisionVector.Dot(Z,motion.AngularVelocityAt(.2)),terms.Velocity);
        Assert.Equal(CollisionVector.Dot(X,motion.LinearAccelerationAt(.2))+
            CollisionVector.Dot(Z,motion.AngularAccelerationAt(.2)),terms.Acceleration);
    }
}
