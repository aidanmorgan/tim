using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ImpulseSolverTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static PhysicsBody Dynamic(int id,CollisionVector center,CollisionVector velocity,
        double mass=1,CollisionVector angular=default,InertiaTensor? inertia=null)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,angular,mass,inertia??new InertiaTensor(1,1,1));
    private static PhysicsBody Fixed(int id)=>new(new(id),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static void Near(double expected,double actual,double tolerance=1e-8)=>Assert.InRange(Math.Abs(expected-actual),0,tolerance);
    private static void Near(CollisionVector expected,CollisionVector actual,double tolerance=1e-8)=>Near(0,(expected-actual).Length,tolerance);

    [Theory]
    [InlineData(0)]
    [InlineData(.5)]
    [InlineData(1)]
    public void HeadOnContactConservesMomentumAndUsesRestitutionOnce(double bounce)
    {
        var a=Dynamic(0,-X,X*3,2); var b=Dynamic(1,X,-X,3);
        var row=ImpulseConstraint.Contact(a,b,default,-X,bounce,0);
        var report=ImpulseSolver.Solve([row]);
        Near(3,a.LinearVelocity.X*2+b.LinearVelocity.X*3);
        Near(4*bounce,b.LinearVelocity.X-a.LinearVelocity.X);
        Assert.True(report.MaximumResidual<=1e-8);
        var firstImpulse=row.AccumulatedImpulse;
        ImpulseSolver.Solve([row]);
        Near(firstImpulse,row.AccumulatedImpulse);
    }

    [Fact]
    public void OffCentreContactConservesLinearAndAngularMomentum()
    {
        var a=Dynamic(0,-X,X*3,2); var b=Dynamic(1,X,-X,3);
        var point=Y*.6;
        var before=CollisionVector.Cross(a.Center,a.LinearVelocity*2)+CollisionVector.Cross(b.Center,b.LinearVelocity*3);
        var energyBefore=(a.LinearVelocity.LengthSquared*2+b.LinearVelocity.LengthSquared*3)/2;
        ImpulseSolver.Solve([ImpulseConstraint.Contact(a,b,point,-X,1,0)]);
        Near(X*3,a.LinearVelocity*2+b.LinearVelocity*3);
        Near(before,CollisionVector.Cross(a.Center,a.LinearVelocity*2)+CollisionVector.Cross(b.Center,b.LinearVelocity*3)
            +a.AngularVelocity+b.AngularVelocity);
        var energyAfter=(a.LinearVelocity.LengthSquared*2+b.LinearVelocity.LengthSquared*3+
            a.AngularVelocity.LengthSquared+b.AngularVelocity.LengthSquared)/2;
        Near(energyBefore,energyAfter);
        Assert.True(a.AngularVelocity.Length>0);
    }

    [Fact]
    public void KinematicContactUsesPrescribedVelocityWithoutChangingTheDriver()
    {
        var load=Dynamic(0,X,default);
        var driver=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,X*2,default);
        ImpulseSolver.Solve([ImpulseConstraint.Contact(load,driver,default,X,0,0)]);
        Near(X*2,load.LinearVelocity); Near(X*2,driver.LinearVelocity);
    }

    [Fact]
    public void SeparatingContactNeverPullsBodiesTogether()
    {
        var a=Dynamic(0,X,X); var b=Fixed(1);
        var row=ImpulseConstraint.Contact(a,b,default,X,.8,0);
        ImpulseSolver.Solve([row]);
        Near(0,row.AccumulatedImpulse); Near(X,a.LinearVelocity);
    }

    [Fact]
    public void HingeAnchorAndContactShareTheSameRowsAndFiniteInertia()
    {
        var beam=Dynamic(0,X,default,2);
        var anchor=Fixed(1);
        var ball=Dynamic(2,X*2+Y,-Y*3);
        var pivot=ConstraintJacobian.AtPoint(beam,anchor,default,Y);
        var anchorRow=new ImpulseConstraint(pivot.Bind(beam,anchor),0,double.NegativeInfinity,double.PositiveInfinity);
        var hit=ImpulseConstraint.Contact(ball,beam,X*2,Y,0,0);
        ImpulseSolver.Solve([anchorRow,hit]);
        Near(default(CollisionVector),beam.PointVelocity(default));
        Near(ball.PointVelocity(X*2).Y,beam.PointVelocity(X*2).Y);
        // Pivot angular momentum: initial -6; beam inertia about pivot 1+2*1^2=3.
        Near(-6,3*beam.AngularVelocity.Z+2*ball.LinearVelocity.Y);
        Near(-12.0/7,ball.LinearVelocity.Y);
        Near(-6.0/7,beam.AngularVelocity.Z);
    }

    [Fact]
    public void CoupledContactsConvergeInsteadOfUsingPairwiseFinalAnswers()
    {
        var a=Dynamic(0,X*2,-X*3); var b=Dynamic(1,X,default); var wall=Fixed(2);
        var first=ImpulseConstraint.Contact(a,b,X*1.5,X,0,0);
        var second=ImpulseConstraint.Contact(b,wall,X*.5,X,0,0);
        var result=ImpulseSolver.Solve([first,second]);
        Near(0,a.LinearVelocity.X); Near(0,b.LinearVelocity.X);
        Assert.Equal(1,result.Iterations);
    }

    [Fact]
    public void AccumulatedImpulseCanDecreaseWithoutBecomingAttractive()
    {
        var body=Dynamic(0,default,-Y); var ground=Fixed(1);
        var row=ImpulseConstraint.Contact(body,ground,default,Y,0,0);
        row.Solve(); Near(1,row.AccumulatedImpulse);
        body.ApplyImpulse(Y*.4,default);
        row.Solve(); Near(.6,row.AccumulatedImpulse); Near(0,body.LinearVelocity.Y);
    }

    [Fact]
    public void BoundedMotorAndSoftConstraintUseTheSameSolver()
    {
        var body=Dynamic(0,default,default); var anchor=Fixed(1);
        var motor=new ImpulseConstraint(new ConstraintJacobian(default,Z,default,-Z).Bind(body,anchor),10,-2,2);
        ImpulseSolver.Solve([motor]); Near(2,body.AngularVelocity.Z); Near(0,motor.Residual);
        var soft=new ImpulseConstraint(new ConstraintJacobian(X,default,-X,default).Bind(body,anchor),3,
            double.NegativeInfinity,double.PositiveInfinity,2);
        ImpulseSolver.Solve([soft]); Near(1,body.LinearVelocity.X); Near(1,soft.AccumulatedImpulse);
    }

    [Fact]
    public void RotatedAnisotropicInertiaIsNotReducedToAScalar()
    {
        var inertia=new InertiaTensor(2,2,4,1);
        var inverse=inertia.Inverse();
        Near(X,inertia.Apply(inverse.Apply(X)));
        Near(Y,inertia.Apply(inverse.Apply(Y)));
        var body=Dynamic(0,default,default,inertia:inertia);
        body.ApplyImpulse(Z,Y); // Angular impulse X.
        Near(new CollisionVector(2.0/3,-1.0/3,0),body.AngularVelocity);
    }

    [Fact]
    public void UnsolvableAndExhaustedConstraintsFailExplicitly()
    {
        var fixedA=Fixed(0); var fixedB=Fixed(1);
        var impossible=new ImpulseConstraint(new ConstraintJacobian(X,default,-X,default).Bind(fixedA,fixedB),1,
            double.NegativeInfinity,double.PositiveInfinity);
        Assert.Throws<InvalidOperationException>(()=>ImpulseSolver.Solve([impossible]));
        var body=Dynamic(2,default,default);
        var positive=new ImpulseConstraint(new ConstraintJacobian(X,default,-X,default).Bind(body,fixedA),1,
            double.NegativeInfinity,double.PositiveInfinity);
        var negative=new ImpulseConstraint(new ConstraintJacobian(X,default,-X,default).Bind(body,fixedB),-1,
            double.NegativeInfinity,double.PositiveInfinity);
        Assert.Throws<InvalidOperationException>(()=>ImpulseSolver.Solve([positive,negative],4));
    }

    [Fact]
    public void NonFiniteResidualCannotBeHiddenByAnImpulseLimit()
    {
        var velocity=new CollisionVector(double.MaxValue,0,0);
        var body=Dynamic(0,default,velocity); var anchor=Fixed(1);
        var row=new ImpulseConstraint(new ConstraintJacobian(X*2,default,-X*2,default).Bind(body,anchor),0,-1,1);
        Assert.Throws<InvalidOperationException>(()=>row.Solve());
        Assert.Equal(velocity,body.LinearVelocity);
        Assert.Equal(0,row.AccumulatedImpulse);
    }

    [Fact]
    public void InvalidDeclarationsAndDuplicateIdentitiesAreRejected()
    {
        Assert.Throws<ArgumentException>(()=>new InertiaTensor(1,1,-1));
        Assert.Throws<ArgumentException>(()=>new InertiaTensor(1,1,1,2));
        Assert.Throws<ArgumentException>(()=>Dynamic(0,default,default,mass:0));
        Assert.Throws<ArgumentException>(()=>new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,X,default));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsBodyId(-1));
        var a=Dynamic(0,default,default); var b=Fixed(1); var copy=Dynamic(0,default,default);
        Assert.Throws<ArgumentException>(()=>ImpulseConstraint.Contact(a,b,default,X*2,0,0));
        Assert.Throws<ArgumentException>(()=>ImpulseSolver.Solve([
            ImpulseConstraint.Contact(a,b,default,X,0,0),ImpulseConstraint.Contact(copy,b,default,Y,0,0)]));
    }
}
