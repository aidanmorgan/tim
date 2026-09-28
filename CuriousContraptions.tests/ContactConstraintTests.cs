using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ContactConstraintTests
{
    private static readonly CollisionVector Y=new(0,1,0);
    private static ImpulseBody Body(CollisionVector velocity,InertiaTensor? inertia=null)=>
        new(new(0),PhysicsMotionType.Dynamic,default,velocity,default,1,inertia??new InertiaTensor(1,1,1));
    private static ImpulseBody Ground()=>new(new(1),PhysicsMotionType.Static,default,default,default);
    private static void Near(CollisionVector a,CollisionVector b,double tolerance=1e-8)=>Assert.InRange((a-b).Length,0,tolerance);

    [Theory]
    [InlineData(0)]
    [InlineData(.3)]
    [InlineData(.8)]
    [InlineData(1.5)]
    public void CoulombDiskIsInvariantToSlidingDirection(double angle)
    {
        var direction=new CollisionVector(Math.Cos(angle),0,Math.Sin(angle));
        var body=Body(direction*4-Y*2);
        var contact=new ContactConstraint(body,Ground(),default,Y,0,0,.5);
        ImpulseSolver.Solve([contact]);
        Near(direction*3,body.LinearVelocity);
        Near(-direction,contact.TangentImpulse);
        Assert.InRange(contact.Residual,0,1e-8);
    }

    [Fact]
    public void StickingStopsSlipWithoutExceedingTheCone()
    {
        var body=Body(new(.2,-2,.3));
        var contact=new ContactConstraint(body,Ground(),default,Y,0,0,.5);
        ImpulseSolver.Solve([contact]);
        Near(default,body.LinearVelocity);
        Assert.True(contact.TangentImpulse.Length<contact.Friction*contact.Normal.AccumulatedImpulse);
    }

    [Fact]
    public void ZeroNormalImpulseAndZeroFrictionCannotApplyDrag()
    {
        foreach(var friction in new[]{0.0,.6})
        {
            var velocity=new CollisionVector(2,1,3);
            var body=Body(velocity);
            var contact=new ContactConstraint(body,Ground(),default,Y,0,0,friction);
            ImpulseSolver.Solve([contact]);
            Near(velocity,body.LinearVelocity); Near(default,contact.TangentImpulse);
        }
        var falling=Body(new(2,-1,3));
        ImpulseSolver.Solve([new ContactConstraint(falling,Ground(),default,Y,0,0,0)]);
        Near(new(2,0,3),falling.LinearVelocity);
    }

    [Fact]
    public void OffCentreAnisotropicSlidingObeysMaximumDissipation()
    {
        var body=Body(new(5,-3,4),new InertiaTensor(2,3,4,.3,.2,.4));
        var point=new CollisionVector(.4,-.7,.3);
        var contact=new ContactConstraint(body,Ground(),point,Y,0,0,.4);
        var initialEnergy=body.LinearVelocity.LengthSquared/2;
        ImpulseSolver.Solve([contact]);
        var velocity=body.PointVelocity(point);
        var tangent=velocity-Y*CollisionVector.Dot(velocity,Y);
        Assert.True(tangent.Length>0);
        Near(-tangent/tangent.Length*contact.Friction*contact.Normal.AccumulatedImpulse,contact.TangentImpulse,1e-7);
        var inertia=new InertiaTensor(2,3,4,.3,.2,.4);
        var energy=(body.LinearVelocity.LengthSquared+CollisionVector.Dot(body.AngularVelocity,inertia.Apply(body.AngularVelocity)))/2;
        Assert.True(energy<initialEnergy);
    }

    [Fact]
    public void FrictionBudgetShrinksWhenAnotherConstraintRemovesNormalImpulse()
    {
        var body=Body(new(3,-2,0));
        var contact=new ContactConstraint(body,Ground(),default,Y,0,0,.5);
        ImpulseSolver.Solve([contact]);
        Near(new(-1,0,0),contact.TangentImpulse);
        body.ApplyImpulse(Y,default);
        ImpulseSolver.Solve([contact]);
        Assert.InRange(contact.Normal.AccumulatedImpulse,.99999999,1.00000001);
        Near(new(-.5,0,0),contact.TangentImpulse);
        Near(new(2.5,0,0),body.LinearVelocity);
    }

    [Fact]
    public void DynamicPairFrictionPreservesTotalMomentum()
    {
        var a=Body(new(4,-2,1)); var b=new ImpulseBody(new(1),PhysicsMotionType.Dynamic,
            new(0,-2,0),default,default,2,new InertiaTensor(2,2,2));
        var initial=a.LinearVelocity;
        ImpulseSolver.Solve([new ContactConstraint(a,b,new(0,-1,0),Y,0,0,.7)]);
        Near(initial,a.LinearVelocity+b.LinearVelocity*2);
        var angular=CollisionVector.Cross(a.Center,a.LinearVelocity)+a.AngularVelocity+
            CollisionVector.Cross(b.Center,b.LinearVelocity*2)+b.AngularVelocity*2;
        Near(default,angular);
    }

    [Fact]
    public void InvalidFrictionIsRejected()
    {
        foreach(var invalid in new[]{-.1,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new ContactConstraint(Body(default),Ground(),default,Y,0,0,invalid));
    }
}
