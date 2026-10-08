using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

/// <summary>Friction budgets belong to shared contact impulses, not per-part velocity-loss counters.</summary>
public class ContactFrictionBudgetTests
{
    private static readonly CollisionVector Normal=new(0,1,0);
    private static PhysicsBody Body(CollisionVector velocity)=>new(new(0),PhysicsMotionType.Dynamic,
        RigidPose.Identity,velocity,default,1,new InertiaTensor(1,1,1));
    private static PhysicsBody Ground()=>new(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static ContactConstraint Contact(PhysicsBody body,CollisionVector normal,double coefficient)=>
        new(ContactKinematics.AtPoint(body,Ground(),default,normal),0,0,coefficient);
    private static void Near(double expected,double actual)=>Assert.InRange(Math.Abs(expected-actual),0,1e-8);
    private static void Near(CollisionVector expected,CollisionVector actual)=>Assert.InRange((expected-actual).Length,0,1e-8);

    [Theory]
    [InlineData(0,.3)]
    [InlineData(.001,.3)]
    [InlineData(1,.3)]
    [InlineData(100,.3)]
    [InlineData(1,0)]
    public void TangentialLossIsBoundedBySolvedNormalImpulseAndAvailableSlip(double approach,double coefficient)
    {
        var body=Body(new(6,-approach,0));var energy=body.KineticEnergy;
        var contact=Contact(body,Normal,coefficient);
        ImpulseSolver.Solve([contact]);
        var loss=Math.Min(6,coefficient*approach);
        Near(approach,contact.Normal.AccumulatedImpulse);
        Near(new CollisionVector(6-loss,0,0),body.LinearVelocity);
        Near(new CollisionVector(-loss,0,0),contact.TangentImpulse);
        Near(default,body.AngularMomentum); // Centre-of-mass contact has no artificial torque.
        Assert.InRange(body.KineticEnergy,0,energy);
        Assert.InRange(contact.TangentImpulse.Length,0,coefficient*contact.Normal.AccumulatedImpulse+1e-8);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(47)]
    [InlineData(90)]
    public void RotatedContactSeparatesNormalAndTangentialResponse(double degrees)
    {
        var rotation=RigidRotation.FromRotationVector(new(0,0,degrees*Math.PI/180));
        var body=Body(rotation.Apply(new(6,-.01,0)));
        var normal=rotation.Apply(Normal);
        var contact=Contact(body,normal,.3);
        ImpulseSolver.Solve([contact]);
        Near(new CollisionVector(5.997,0,0),rotation.Inverse().Apply(body.LinearVelocity));
        Near(0,CollisionVector.Dot(contact.TangentImpulse,normal));
        Near(.01,contact.Normal.AccumulatedImpulse);
    }

    [Theory]
    [InlineData(.05,.3)]
    [InlineData(30,.3)]
    public void SubdividedNormalLoadingDoesNotMultiplyTangentialImpulse(double impulse,double coefficient)
    {
        var single=Body(new(6,-impulse,0));var oneContact=Contact(single,Normal,coefficient);
        ImpulseSolver.Solve([oneContact]);
        var split=Body(new(6,0,0));var tangent=default(CollisionVector);var totalNormal=0.0;
        for(var i=0;i<100;i++)
        {
            split.ApplyImpulse(-Normal*(impulse/100),split.Center);
            var contact=Contact(split,Normal,coefficient);
            ImpulseSolver.Solve([contact]);
            tangent+=contact.TangentImpulse;totalNormal+=contact.Normal.AccumulatedImpulse;
        }
        Near(single.LinearVelocity,split.LinearVelocity);
        Near(oneContact.TangentImpulse,tangent);Near(impulse,totalNormal);
        Assert.True(split.LinearVelocity.X>=-1e-8);
    }

    [Fact]
    public void ReSolvingAnUnchangedContactDoesNotSpendFrictionTwice()
    {
        var body=Body(new(6,-.05,0));var contact=Contact(body,Normal,.3);
        ImpulseSolver.Solve([contact]);
        var velocity=body.LinearVelocity;var impulse=contact.Impulse;
        for(var i=0;i<100;i++) ImpulseSolver.Solve([contact]);
        Near(velocity,body.LinearVelocity);Near(impulse.Normal,contact.Impulse.Normal);
        Near(impulse.Tangent,contact.Impulse.Tangent);
    }

    [Fact]
    public void SeparatingContactCannotInventNormalSupportOrFriction()
    {
        var velocity=new CollisionVector(6,2,0);var body=Body(velocity);
        var contact=Contact(body,Normal,.3);ImpulseSolver.Solve([contact]);
        Near(velocity,body.LinearVelocity);Near(0,contact.Impulse.Normal);
        Near(default,contact.Impulse.Tangent);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidCoefficientIsRejectedAtTheSharedBoundary(double coefficient)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>Contact(Body(default),Normal,coefficient));

    [Fact]
    public void InvalidGeometryAndVelocityAreRejectedAtTheSharedBoundary()
    {
        Assert.Throws<ArgumentException>(()=>Contact(Body(default),default,.3));
        Assert.Throws<ArgumentException>(()=>Contact(Body(default),new(0,2,0),.3));
        Assert.Throws<ArgumentException>(()=>Body(new(double.NaN,0,0)));
        Assert.Throws<ArgumentException>(()=>new ContactImpulse(-1,default));
    }
}
