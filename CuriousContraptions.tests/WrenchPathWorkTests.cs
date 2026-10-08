using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class WrenchPathWorkTests
{
    private static readonly CollisionVector X=new(1,0,0),Z=new(0,0,1);
    private static PhysicsBody Body(int id,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.Identity,velocity,spin,1,new(1,1,1));
    private static void Near(double expected,double actual,double tolerance=1e-9)=>
        Assert.InRange(Math.Abs(expected-actual),0,tolerance);

    [Theory]
    [InlineData(1,4,0)]
    [InlineData(-1,0,4)]
    public void ConstantForceWorkUsesCapturedDisplacement(int sign,double supplied,double dissipated)
    {
        var body=Body(0,X*2);var path=body.CreateTrajectory(1,default);
        var before=body.Snapshot();
        var terms=new[]{new WrenchPathTerm(body,path,new(X*(2*sign),default))};
        var work=WrenchPathWork.Measure(terms,1,1e-10,0);
        Near(supplied,work.Supplied);Near(dissipated,work.Dissipated);Assert.Equal(0,work.SuppliedErrorBound);Assert.Equal(0,work.DissipatedErrorBound);
        Assert.Equal(work,WrenchPathWork.Measure(terms,1,1e-10,0));Assert.Equal(before,body.Snapshot());
        Near(CollisionVector.Dot(terms[0].Wrench.Force,path.At(1).Center-path.At(0).Center),work.Supplied-work.Dissipated);
    }

    [Fact]
    public void ReversalRetainsBrakingAndSupplyDespiteZeroNetWork()
    {
        var body=Body(0,X*2);var wrench=new BodyWrench(-X*4,default);
        var path=body.CreateTrajectory(1,wrench);
        var terms=new[]{new WrenchPathTerm(body,path,wrench)};
        var result=WrenchPathWork.Measure(terms,1,1e-10,0);
        Near(2,result.Supplied);Near(2,result.Dissipated);Assert.Equal(0,result.SuppliedErrorBound);Assert.Equal(0,result.DissipatedErrorBound);
        var prefix=WrenchPathWork.Measure(terms,.5,1e-10,0);
        Near(0,prefix.Supplied);Near(2,prefix.Dissipated);
    }

    [Fact]
    public void ParticipantPowerIsSummedBeforeSeparatingSupplyAndLoss()
    {
        var a=Body(0,X*3);var b=Body(1,X*3);
        var terms=new[]{new WrenchPathTerm(a,a.CreateTrajectory(1,default),new(X*10,default)),
            new WrenchPathTerm(b,b.CreateTrajectory(1,default),new(-X*10,default))};
        Assert.Equal(default,WrenchPathWork.Measure(terms,1,1e-10,0));
    }

    [Fact]
    public void TorqueWorkRetainsFullTurns()
    {
        var body=Body(0,spin:Z*(4*Math.Tau));var path=body.CreateTrajectory(1,default);
        var work=WrenchPathWork.Measure([new(body,path,new(default,Z*3))],1,1e-10,0);
        Near(12*Math.Tau,work.Supplied);Near(0,work.Dissipated);Assert.Equal(0,work.SuppliedErrorBound);
        Assert.True(path.SegmentCount>1);
    }

    [Fact]
    public void TorqueReversalAccountsForBothSidesOfEachGeometricSegment()
    {
        var body=Body(0,spin:Z*4);var wrench=new BodyWrench(default,-Z*8);
        var path=body.CreateTrajectory(1,wrench);
        var work=WrenchPathWork.Measure([new(body,path,wrench)],1,1e-10,0);
        // Isotropic inertia makes midpoint rotation segments equally sized and
        // the zero of spin occurs on a segment boundary: each half transfers 8 J.
        Near(8,work.Supplied);Near(8,work.Dissipated);Assert.Equal(0,work.SuppliedErrorBound);
        var end=path.PhysicalAngularVelocityAt(1).Z;
        Near(.5*(end*end-16),work.Supplied-work.Dissipated);
    }

    [Theory]
    [InlineData(.5,.5)]
    [InlineData(1,1)]
    [InlineData(1.5,1)]
    public void PrescribedEaseIncludesTranslationRotationAndEndpointHold(double duration,double fraction)
    {
        var ease=new QuinticRigidTrajectory(RigidPose.Identity,X*2,Z*(2*Math.Tau),1);
        var motion=new PrescribedBodyMotion(ease,RigidPose.Identity,0);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,motion.At(0),default,default,prescribedMotion:motion);
        var path=body.CreateTrajectory(1.5,default);
        var work=WrenchPathWork.Measure([new(body,path,new(X*3,Z*2))],duration,1e-6,0);
        Near((6+4*Math.Tau)*fraction,work.Supplied,work.SuppliedErrorBound+1e-9);
        Assert.InRange(work.SuppliedErrorBound,0,1e-6);Assert.InRange(work.DissipatedErrorBound,0,1e-6);Near(0,work.Dissipated);
    }

    [Theory]
    [InlineData(1,2,0)]
    [InlineData(2,2,2)]
    [InlineData(4,4,4)]
    public void RotatingOffsetUsesActualCenterMotionAndInternalReversals(int halfTurns,double supplied,double dissipated)
    {
        var ease=new QuinticRigidTrajectory(RigidPose.Identity,default,Z*(halfTurns*Math.PI),1);
        var motion=new PrescribedBodyMotion(ease,RigidPose.At(X),0);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,motion.At(0),default,default,prescribedMotion:motion);
        var work=WrenchPathWork.Measure([new(body,body.CreateTrajectory(1,default),new(-X,default))],1,1e-6,0);
        Near(supplied,work.Supplied,work.SuppliedErrorBound+1e-9);Near(dissipated,work.Dissipated,work.DissipatedErrorBound+1e-9);
    }


    [Fact]
    public void UnrepresentablePowerAndUnattainableAccuracyFailWithoutMutation()
    {
        var body=Body(0,X*2);var before=body.Snapshot();
        Assert.Throws<InvalidOperationException>(()=>WrenchPathWork.Measure(
            [new(body,body.CreateTrajectory(1,default),new(X*double.MaxValue,default))],1,1e-6,0));
        Assert.Equal(before,body.Snapshot());
        var ease=new QuinticRigidTrajectory(RigidPose.Identity,X,default,1);
        var motion=new PrescribedBodyMotion(ease,RigidPose.Identity,0);
        var prescribed=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,motion.At(0),default,default,prescribedMotion:motion);
        var initial=prescribed.Snapshot();
        Assert.Throws<InvalidOperationException>(()=>WrenchPathWork.Measure(
            [new(prescribed,prescribed.CreateTrajectory(1,default),new(X,default))],1,1e-20,0));
        Assert.Equal(initial,prescribed.Snapshot());
    }


    [Fact]
    public void OneGeometricSegmentCannotHidePhysicalBrakingAndReversal()
    {
        var body=Body(0,spin:-Z);var wrench=new BodyWrench(default,Z*400);
        var path=body.CreateTrajectory(.005,wrench);
        Assert.Equal(1,path.SegmentCount);
        Near(0,path.AngularVelocityAt(.0025).Length);
        Near(-1,path.PhysicalAngularVelocityAt(0).Z);Near(1,path.PhysicalAngularVelocityAt(.005).Z);
        var work=WrenchPathWork.Measure([new(body,path,wrench)],.005,1e-10,0);
        Near(.5,work.Supplied);Near(.5,work.Dissipated);
    }


    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void PeakPowerRetainsBothSignsOfAffineReversal(int sign)
    {
        var body=Body(0,X*(2*sign));var wrench=new BodyWrench(X*(-4*sign),default);
        var path=body.CreateTrajectory(1,wrench);var before=body.Snapshot();
        var result=WrenchPathWork.Measure([new(body,path,wrench)],1,1e-10,0);
        Near(8,result.SuppliedPowerUpperBound);Near(8,result.DissipatedPowerUpperBound);
        Assert.True(result.SuppliedPowerUpperBound>result.Supplied);
        var prefix=WrenchPathWork.Measure([new(body,path,wrench)],.5,1e-10,0);
        Near(0,prefix.SuppliedPowerUpperBound);Near(8,prefix.DissipatedPowerUpperBound);
        Assert.Equal(before,body.Snapshot());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void CurvedInteriorPowerPeakIsBoundedDespiteZeroEndpointPower(int sign)
    {
        var motion=new PrescribedBodyMotion(new QuinticRigidTrajectory(
            RigidPose.Identity,X*2,Z*4,1),RigidPose.Identity,0);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,motion.At(0),
            default,default,prescribedMotion:motion);
        var path=body.CreateTrajectory(1.5,default);
        var work=WrenchPathWork.Measure([new(body,path,new(X*(3*sign),Z*(2*sign)))],1.5,1e-6,0);
        // Quintic ease derivative is 30*t^2*(1-t)^2; its maximum is 15/8 at t=1/2.
        // Net unit-ease work is force*distance + torque*angle = 14 J.
        const double exactPeak=14*15.0/8;
        var bound=sign>0?work.SuppliedPowerUpperBound:work.DissipatedPowerUpperBound;
        var opposite=sign>0?work.DissipatedPowerUpperBound:work.SuppliedPowerUpperBound;
        Assert.InRange(bound,exactPeak,exactPeak+1e-4);
        Assert.InRange(opposite,0,1e-4);
        for(var i=0;i<=1000;i++)
        {
            var time=i/1000.0;var power=14*30*time*time*(1-time)*(1-time);
            Assert.True(power<=bound);
        }
    }

    [Fact]
    public void PeakTorquePowerUsesPhysicalSpinWithinStationaryGeometricSegment()
    {
        var body=Body(0,spin:-Z);var wrench=new BodyWrench(default,Z*400);
        var path=body.CreateTrajectory(.005,wrench);
        var work=WrenchPathWork.Measure([new(body,path,wrench)],.005,1e-10,0);
        Assert.Equal(1,path.SegmentCount);
        Near(400,work.SuppliedPowerUpperBound);Near(400,work.DissipatedPowerUpperBound);
    }

    [Fact]
    public void InvalidForeignAndStaleSourcesReject()
    {
        var body=Body(0);var path=body.CreateTrajectory(1,default);
        var term=new WrenchPathTerm(body,path,default);
        Assert.Throws<ArgumentNullException>(()=>WrenchPathWork.Measure(null!,1,1e-6,0));
        Assert.Throws<ArgumentNullException>(()=>WrenchPathWork.Measure([default],1,1e-6,0));
        Assert.Throws<ArgumentException>(()=>WrenchPathWork.Measure([term,term],1,1e-6,0));
        Assert.Throws<InvalidOperationException>(()=>WrenchPathWork.Measure([new(Body(0),path,default)],1,1e-6,0));
        foreach(var value in new[]{-1,double.NaN,double.PositiveInfinity})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>WrenchPathWork.Measure([term],value,1e-6,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>WrenchPathWork.Measure([term],1,value,0));
        }
        Assert.Throws<ArgumentOutOfRangeException>(()=>WrenchPathWork.Measure([term],2,1e-6,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>WrenchPathWork.Measure([term],1,0,0));
        Assert.Equal(default,WrenchPathWork.Measure([term],0,1e-6,0));
        body.ApplyImpulse(X,default);
        Assert.Throws<InvalidOperationException>(()=>WrenchPathWork.Measure([term],1,1e-6,0));
    }

    [Theory]
    [InlineData(3,6,0)]
    [InlineData(-3,0,6)]
    [InlineData(0,0,0)]
    public void AlgebraicPowerNeedsNoPhysicalBody(double power,double supplied,double dissipated)
    {
        var work=WrenchPathWork.Measure([],2,1e-10,power);
        Near(supplied,work.Supplied);Near(dissipated,work.Dissipated);
        Assert.Equal(Math.Max(0,power),work.SuppliedPowerUpperBound);
        Assert.Equal(Math.Max(0,-power),work.DissipatedPowerUpperBound);
        Assert.Equal(0,work.SuppliedErrorBound);Assert.Equal(0,work.DissipatedErrorBound);
        Assert.Equal(default,WrenchPathWork.Measure([],0,1e-10,power));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void AlgebraicOffsetPreservesBothSignsAcrossPhysicalPowerCrossing(int sign)
    {
        var body=Body(0);var before=body.Snapshot();
        var path=body.CreateTrajectory(1,new(X*4,default));
        // Physical power is sign*4t; subtract sign*2 W before splitting signs.
        var work=WrenchPathWork.Measure([new(body,path,new(X*sign,default))],1,1e-10,-2*sign);
        Near(.5,work.Supplied);Near(.5,work.Dissipated);
        Near(2,work.SuppliedPowerUpperBound);Near(2,work.DissipatedPowerUpperBound);
        Assert.Equal(0,work.SuppliedErrorBound);Assert.Equal(0,work.DissipatedErrorBound);
        var prefix=WrenchPathWork.Measure([new(body,path,new(X*sign,default))],.5,1e-10,-2*sign);
        Near(sign>0?0:.5,prefix.Supplied);Near(sign>0?.5:0,prefix.Dissipated);
        Assert.Equal(before,body.Snapshot());
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonfiniteAlgebraicPowerRejects(double power)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>WrenchPathWork.Measure([],1,1e-10,power));

    [Fact]
    public void UnrepresentableAlgebraicWorkRejects()=>
        Assert.Throws<InvalidOperationException>(()=>WrenchPathWork.Measure([],2,1e-10,double.MaxValue));
}
