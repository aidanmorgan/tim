using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class BodyDynamicsTests
{
    [Fact]
    public void ExplicitDynamicDeclarationCreatesIndependentOwnedState()
    {
        var dynamics=new BodyDynamics(PhysicsMotionType.Dynamic,3,new(2,3,4),new(1,2,3),new(0,0,2));
        var pose=new RigidPose(new(4,5,6),RigidRotation.FromRotationVector(new(.2,.3,.4)));
        var first=dynamics.CreateBody(new(4),pose,null);
        var second=dynamics.CreateBody(new(5),pose,null);
        Assert.Equal(1.0/3,first.InverseMass);
        Assert.Equal(dynamics.Inertia,first.LocalInertia);
        Assert.Equal(dynamics.LinearVelocity,first.LinearVelocity);
        Assert.InRange((first.AngularVelocity-dynamics.AngularVelocity).Length,0,1e-12);
        first.ApplyImpulse(new(3,0,0),first.Center);
        Assert.Equal(new CollisionVector(2,2,3),first.LinearVelocity);
        Assert.Equal(dynamics.LinearVelocity,second.LinearVelocity);
        Assert.Equal(new CollisionVector(1,2,3),dynamics.LinearVelocity);
    }

    [Theory]
    [InlineData(PhysicsMotionType.Static)]
    [InlineData(PhysicsMotionType.Kinematic)]
    public void PrescribedDeclarationsHaveNoInventedDynamicMass(PhysicsMotionType motion)
    {
        var linear=motion==PhysicsMotionType.Kinematic?new CollisionVector(1,2,3):default;
        var angular=motion==PhysicsMotionType.Kinematic?new CollisionVector(.2,.3,.4):default;
        var body=new BodyDynamics(motion,0,default,linear,angular).CreateBody(new(1),RigidPose.Identity,null);
        Assert.Equal(motion,body.MotionType); Assert.Equal(0,body.InverseMass);
        Assert.Equal(linear,body.LinearVelocity); Assert.Equal(angular,body.AngularVelocity);
        Assert.Throws<ArgumentException>(()=>new BodyDynamics(motion,1,new(1,1,1),linear,angular));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.Epsilon)]
    public void InvalidMassRejects(double mass)=>
        Assert.Throws<ArgumentException>(()=>new BodyDynamics(PhysicsMotionType.Dynamic,mass,new(1,1,1),default,default));

    [Fact]
    public void InvalidTypeInertiaAndVelocityReject()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BodyDynamics((PhysicsMotionType)99,0,default,default,default));
        Assert.Throws<ArgumentException>(()=>new BodyDynamics(PhysicsMotionType.Dynamic,1,default,default,default));
        Assert.Throws<ArgumentException>(()=>new BodyDynamics(PhysicsMotionType.Static,0,default,new(1,0,0),default));
        Assert.Throws<ArgumentException>(()=>new BodyDynamics(PhysicsMotionType.Static,0,default,default,new(1,0,0)));
        Assert.Throws<ArgumentException>(()=>new BodyDynamics(PhysicsMotionType.Kinematic,0,default,new(double.NaN,0,0),default));
        Assert.Throws<ArgumentException>(()=>new BodyDynamics(PhysicsMotionType.Dynamic,1,new(1,1,1),default,new(0,double.PositiveInfinity,0)));
    }

    [Fact]
    public void SolidSphereMassModelHasAnalyticInertiaAndRejectsInvalidRadius()
    {
        var declaration=BodyDynamics.SolidSphere(5,2,default,default);
        Assert.Equal(new InertiaTensor(8,8,8),declaration.Inertia);
        foreach(var radius in new[]{0,-1,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>BodyDynamics.SolidSphere(5,radius,default,default));
    }

    [Fact]
    public void UniformBoxInertiaUsesDeclaredMassAndDimensions()
    {
        var dynamics=BodyDynamics.SolidBox(3,new(1,2,3),default,default);
        Assert.Equal(new InertiaTensor(13,10,5),dynamics.Inertia);
        foreach(var half in new CollisionVector[]{new(0,1,1),new(-1,1,1),
            new(double.NaN,1,1),new(1,double.PositiveInfinity,1)})
            Assert.Throws<ArgumentOutOfRangeException>(()=>BodyDynamics.SolidBox(3,half,default,default));
        Assert.Throws<ArgumentException>(()=>BodyDynamics.SolidBox(0,new(1,1,1),default,default));
    }
}
