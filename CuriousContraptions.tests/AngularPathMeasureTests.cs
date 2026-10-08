using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AngularPathMeasureTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    private static BodyTrajectory Spin(double speed,double duration,RigidRotation? rotation=null)=>
        new PhysicsBody(new(0),PhysicsMotionType.Kinematic,new(default,rotation??RigidRotation.Identity),
            default,Z*speed).CreateTrajectory(duration,default);
    private static AngularPathTravel Measure(BodyTrajectory a,BodyTrajectory b,double duration)=>
        AngularPathMeasure.Measure(a,RigidRotation.Identity,b,RigidRotation.Identity,duration,1e-7);
    private static void Near(double expected,double actual,double tolerance=1e-9)=>
        Assert.InRange(Math.Abs(expected-actual),0,tolerance);

    public enum RotationSense { Positive, Negative }
    [Theory]
    [InlineData(RotationSense.Positive)]
    [InlineData(RotationSense.Negative)]
    public void MultipleTurnsSurviveEqualEndpointOrientations(RotationSense sense)
    {
        var direction=sense switch
        {
            RotationSense.Positive=>1, RotationSense.Negative=>-1,
            _=>throw new ArgumentOutOfRangeException(nameof(sense))
        };
        var travel=Measure(Spin(direction*12*Math.Tau,1),Spin(0,1),1);
        Near(direction*12*Math.Tau,travel.Winding);
        Near(12*Math.Tau,travel.Distance);
        Assert.Equal(0,travel.DistanceError);
    }

    [Fact]
    public void MovingReferenceSubtractsItsOwnWinding()
    {
        var travel=Measure(Spin(20*Math.Tau,1),Spin(17*Math.Tau,1),1);
        Near(3*Math.Tau,travel.Winding);
        Near(3*Math.Tau,travel.Distance);
        Assert.Equal(default,Measure(Spin(20*Math.Tau,1),Spin(20*Math.Tau,1),1));
    }

    [Fact]
    public void LocalFramesCanRotateTheHingeAxisIntoWorldSpace()
    {
        var turn=RigidRotation.FromRotationVector(new(Math.PI/2,0,0));
        var a=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,new(default,turn),default,
            turn.Apply(Z)*8).CreateTrajectory(1,default);
        var b=Spin(0,1,turn);
        var travel=Measure(a,b,1);
        Near(8,travel.Winding);Near(8,travel.Distance);
    }

    [Fact]
    public void TorqueReversalCountsBothDirectionsAcrossCapturedSegments()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,Z*4,1,new(1,1,1));
        var path=body.CreateTrajectory(1,new(default,Z*-8));
        var fixedPath=Spin(0,1);
        Assert.True(path.SegmentCount>1);
        // Expected distance follows the actual piecewise-constant geometric path,
        // whose midpoint discretisation is deliberately distinct from physical velocity.
        double expected=0,time=0;
        while(time<1)
        {
            var end=path.SegmentEndAfter(time);
            expected+=Math.Abs(path.AngularVelocityAt((time+end)*.5).Z)*(end-time);time=end;
        }
        var travel=Measure(path,fixedPath,1);
        Near(0,travel.Winding);
        Near(expected,travel.Distance);
        Assert.InRange(travel.DistanceError,0,1e-7);
        Assert.True(travel.Distance>1.9);
    }

    [Fact]
    public void PrescribedEaseRetainsFullTurnsAndPrefix()
    {
        var ease=new QuinticRigidTrajectory(RigidPose.Identity,default,Z*(4*Math.Tau),1);
        var motion=new PrescribedBodyMotion(ease,RigidPose.Identity,0);
        var a=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,motion.At(0),default,default,
            prescribedMotion:motion).CreateTrajectory(1,default);
        var half=Measure(a,Spin(0,1),.5);
        Near(2*Math.Tau,half.Winding);
        Near(2*Math.Tau,half.Distance,1e-7);
        var whole=Measure(a,Spin(0,1),1);
        Near(4*Math.Tau,whole.Winding);
        Near(4*Math.Tau,whole.Distance,1e-7);
        Assert.InRange(whole.DistanceError,0,1e-7);
    }

    [Fact]
    public void TwoReversalsInsideOnePrescribedSegmentPreserveDistance()
    {
        var ease=new QuinticRigidTrajectory(RigidPose.Identity,default,Z*(4*Math.Tau),1);
        var motion=new PrescribedBodyMotion(ease,RigidPose.Identity,0);
        var a=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,motion.At(0),default,default,
            prescribedMotion:motion).CreateTrajectory(2,default);
        var result=Measure(a,Spin(2*Math.Tau,2),1);
        var root=(1-Math.Sqrt(1-4/Math.Sqrt(60)))*.5;
        var blend=root*root*root*(10+root*(-15+6*root));
        var minimum=4*Math.Tau*blend-2*Math.Tau*root;
        var expected=2*Math.Tau-4*minimum;
        Near(2*Math.Tau,result.Winding);
        Assert.InRange(expected-result.Distance,-1e-12,result.DistanceError+1e-12);
        Assert.InRange(result.DistanceError,0,1e-7);
        // The held endpoint after t=1 is a separate zero-spin segment.
        var held=Measure(a,Spin(0,2),2);
        Near(4*Math.Tau,held.Winding);Near(4*Math.Tau,held.Distance,1e-7);
    }

    [Fact]
    public void SharedPrescribedFrameHasNoRelativeTravelWithDifferentOffsets()
    {
        var ease=new QuinticRigidTrajectory(RigidPose.Identity,new(1,2,3),new(20,30,40),1);
        BodyTrajectory Path(int index,RigidPose local)
        {
            var motion=new PrescribedBodyMotion(ease,local,0);
            return new PhysicsBody(new(index),PhysicsMotionType.Kinematic,motion.At(0),default,default,
                prescribedMotion:motion).CreateTrajectory(1,default);
        }
        var a=Path(0,RigidPose.Identity);
        var b=Path(1,new(new(2,3,4),RigidRotation.FromRotationVector(new(.2,.3,.4))));
        Assert.Equal(default,Measure(a,b,1));
    }

    [Fact]
    public void InvalidInputsAndUndefinedTwistAreRejected()
    {
        var path=Spin(0,1);
        Assert.Throws<ArgumentOutOfRangeException>(()=>Measure(path,path,-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Measure(path,path,2));
        Assert.Throws<ArgumentOutOfRangeException>(()=>
            AngularPathMeasure.Measure(path,RigidRotation.Identity,path,RigidRotation.Identity,1,0));
        var antiparallel=Spin(0,1,RigidRotation.FromRotationVector(new(Math.PI,0,0)));
        Assert.Throws<InvalidOperationException>(()=>Measure(antiparallel,path,1));
        Assert.Equal(default,Measure(path,path,0));
    }
}
