using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsBodyTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static PhysicsBody Body(CollisionVector velocity=default,CollisionVector angular=default,
        InertiaTensor? inertia=null,RigidPose? pose=null)=>new(new(0),PhysicsMotionType.Dynamic,pose??RigidPose.Identity,
            velocity,angular,2,inertia??new InertiaTensor(2,2,2));
    private static void Near(CollisionVector expected,CollisionVector actual,double tolerance=1e-8)=>
        Assert.InRange((expected-actual).Length,0,tolerance);

    [Fact]
    public void ForceAndTorqueChangeMomentaBeforePoseIntegration()
    {
        var body=Body();
        body.ApplyWrench(X*4,Z*8,.25);
        Near(X*.5,body.LinearVelocity); Near(Z*2,body.AngularMomentum);
        Near(Z,body.AngularVelocity); Near(default,body.Center);
        body.Advance(body.CreateTrajectory(.5),.5);
        Near(X*.25,body.Center);
        Near(RigidRotation.FromRotationVector(Z*.5).Apply(X),body.Pose.Rotation.Apply(X));
    }
    [Fact]
    public void OffCentreImpulseUpdatesAngularMomentumWithoutAnIntermediateVelocityConversion()
    {
        var body=Body(pose:RigidPose.At(X));
        body.ApplyImpulse(Y*3,X*2);
        Near(Y*1.5,body.LinearVelocity);
        Assert.Equal(Z*3,body.AngularMomentum);
    }
    [Fact]
    public void BodyLocalInertiaRotatesWithPose()
    {
        var rotation=RigidRotation.FromRotationVector(Z*(Math.PI/2));
        var body=Body(inertia:new InertiaTensor(1,2,3),pose:new(default,rotation));
        Near(X*.5,body.InverseInertia(X));
        Near(Y,body.InverseInertia(Y));
        body.ApplyImpulse(Z,Y);
        Near(X*.5,body.AngularVelocity);
    }
    [Fact]
    public void TorqueFreeAnisotropicBodyConservesWorldAngularMomentumAndHasBoundedEnergyError()
    {
        var body=Body(new(.2,.3,.4),new(.7,1.1,1.6),new InertiaTensor(1,2,3));
        var momentum=body.AngularMomentum; var energy=body.KineticEnergy; var initialOmega=body.AngularVelocity;
        double maximumError=0;
        for(var i=0;i<4800;i++)
        {
            body.Advance(body.CreateTrajectory(1.0/480),1.0/480);
            Assert.Equal(momentum,body.AngularMomentum);
            maximumError=Math.Max(maximumError,Math.Abs(body.KineticEnergy-energy)/energy);
        }
        Assert.InRange(maximumError,0,2e-6);
        Near(new(2,3,4),body.Center,1e-10);
        Assert.True((body.AngularVelocity-initialOmega).Length>.1);
        Assert.True(body.Pose.Rotation.IsValid);
    }
    [Fact]
    public void FreeRotationIsTimeReversibleWithinIntegrationTolerance()
    {
        var body=Body(angular:new(.7,1.1,1.6),inertia:new InertiaTensor(1,2,3));
        for(var i=0;i<240;i++) body.Advance(body.CreateTrajectory(1.0/240),1.0/240);
        var state=body.Snapshot();
        body.Restore(state with { AngularMomentum=-state.AngularMomentum });
        for(var i=0;i<240;i++) body.Advance(body.CreateTrajectory(1.0/240),1.0/240);
        Near(X,body.Pose.Rotation.Apply(X),1e-9);
        Near(Y,body.Pose.Rotation.Apply(Y),1e-9);
    }
    [Fact]
    public void SnapshotRestoreIsExactAndReplaysIdentically()
    {
        var body=Body(new(1,2,3),new(.7,1.1,1.6),new InertiaTensor(1,2,3));
        var initial=body.Snapshot();
        void Run()
        {
            for(var i=0;i<100;i++)
            {
                body.ApplyWrench(Y*-19.62,X*.1,1.0/240);
                body.Advance(body.CreateTrajectory(1.0/240),1.0/240);
            }
        }
        Run(); var expected=body.Snapshot();
        body.Restore(initial); Assert.Equal(initial,body.Snapshot());
        Run(); Assert.Equal(expected,body.Snapshot());
    }
    [Fact]
    public void StaticAndKinematicMotionHaveExplicitContracts()
    {
        var stationary=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(X),default,default);
        stationary.Advance(stationary.CreateTrajectory(1),1); Near(X,stationary.Center);
        Assert.Throws<InvalidOperationException>(()=>stationary.ApplyWrench(X,default,.1));
        Assert.Throws<InvalidOperationException>(()=>stationary.SetKinematicVelocity(X,default));
        var moving=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,X,Z);
        moving.Advance(moving.CreateTrajectory(.5),.5); Near(X*.5,moving.Center);
        Near(RigidRotation.FromRotationVector(Z*.5).Apply(X),moving.Pose.Rotation.Apply(X));
        moving.SetKinematicVelocity(Y,default); moving.Advance(moving.CreateTrajectory(.5),.5);
        Near(new(.5,.5,0),moving.Center);
    }
    [Fact]
    public void StaleConstraintsCannotUseMassAndGeometryFromBeforeMotion()
    {
        var body=Body(Y*-1); var floor=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var row=ImpulseConstraint.Contact(body,floor,default,Y,0,0);
        body.Advance(body.CreateTrajectory(.1),.1);
        Assert.Throws<InvalidOperationException>(()=>row.Solve());
        Assert.Throws<InvalidOperationException>(()=>ImpulseSolver.Solve([row]));
    }
    [Fact]
    public void InvalidOrOverBudgetOperationsLeaveTheBodyUnchanged()
    {
        var body=Body(X,new(2,3,4),new InertiaTensor(1,2,3));
        var before=body.Snapshot();
        Assert.Throws<ArgumentOutOfRangeException>(()=>body.Advance(body.CreateTrajectory(-1),-1));
        Assert.Throws<InvalidOperationException>(()=>body.Advance(body.CreateTrajectory(1000000),1000000));
        Assert.Throws<ArgumentException>(()=>body.Restore(before with { Id=new(3) }));
        Assert.Throws<ArgumentException>(()=>body.Restore(before with { Pose=default }));
        Assert.Throws<ArgumentException>(()=>body.ApplyWrench(new(double.NaN,0,0),default,.1));
        Assert.Equal(before,body.Snapshot());
    }
    [Fact]
    public void PoseTransformsRoundTrip()
    {
        var pose=new RigidPose(new(3,4,5),RigidRotation.FromRotationVector(new(.4,.6,.9)));
        var point=new CollisionVector(-2,1,4);
        Near(point,pose.InverseTransformPoint(pose.TransformPoint(point)));
        Assert.Throws<ArgumentException>(()=>new RigidPose(default,default));
    }
}
