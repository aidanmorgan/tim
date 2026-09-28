using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class MultiBodyConstraintTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static PhysicsBody Body(int id,CollisionVector velocity=default,double mass=1,CollisionVector center=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,default,mass,new(2,3,4,.2,.1,.3));
    private static void Near(double a,double b,double tolerance=1e-10)=>Assert.InRange(Math.Abs(a-b),0,tolerance);
    private static void Near(CollisionVector a,CollisionVector b,double tolerance=1e-10)=>Near(0,(a-b).Length,tolerance);
    private static ImpulseConstraint Tension(ConstraintGradient gradient)=>new(gradient,0,double.NegativeInfinity,0);

    [Fact]
    public void MovingGuideReceivesBothReactionsAndTotalMomentumIsConserved()
    {
        var a=Body(0,Y*3); var b=Body(1,Y); var guide=Body(2);
        var gradient=new ConstraintGradient([new(a,Y,default),new(guide,-Y,Z),
            new(b,Y,default),new(guide,-Y,-Z)]);
        var initial=a.KineticEnergy+b.KineticEnergy+guide.KineticEnergy;
        var row=Tension(gradient); var result=ImpulseSolver.Solve([row]);
        Near(6,row.InverseEffectiveMass); Near(-2.0/3,row.AccumulatedImpulse);
        Near(Y*(7.0/3),a.LinearVelocity); Near(Y/3,b.LinearVelocity); Near(Y*(4.0/3),guide.LinearVelocity);
        Near(Y*4,a.LinearVelocity+b.LinearVelocity+guide.LinearVelocity);
        Near(default(CollisionVector),guide.AngularMomentum);
        Near(0,gradient.Speed); Assert.True(result.MaximumResidual<1e-10);
        Assert.True(a.KineticEnergy+b.KineticEnergy+guide.KineticEnergy<initial);
    }

    [Fact]
    public void ShorteningMultiBodyRopeDoesNotPush()
    {
        var a=Body(0,-Y*3); var b=Body(1,-Y); var guide=Body(2);
        var row=Tension(new([new(a,Y,default),new(b,Y,default),new(guide,-Y*2,default)]));
        var before=new[]{a.Snapshot(),b.Snapshot(),guide.Snapshot()};
        ImpulseSolver.Solve([row]);
        Assert.Equal(0,row.AccumulatedImpulse);
        Assert.Equal(before,new[]{a.Snapshot(),b.Snapshot(),guide.Snapshot()});
    }

    [Fact]
    public void ThreeBodyBlockWithDifferentRowParticipantsFindsSharedMassWeightedSpeed()
    {
        var a=Body(0,X*6,1); var b=Body(1,-X*3,2); var c=Body(2,X,3);
        ImpulseConstraint Equality(PhysicsBody first,PhysicsBody second)=>
            new(new([new(first,X,default),new(second,-X,default)]),0,double.NegativeInfinity,double.PositiveInfinity);
        var ab=Equality(a,b); var bc=Equality(b,c);
        var block=new BilateralConstraintBlock([ab,bc]);
        var solve=ImpulseSolver.Solve([block],1);
        Assert.Equal(3,block.Bodies.Length); Assert.Equal(1,solve.Iterations);
        Near(X*.5,a.LinearVelocity); Near(X*.5,b.LinearVelocity); Near(X*.5,c.LinearVelocity);
    }

    [Fact]
    public void MultiBodyTensionAndOrdinaryContactConvergeTogether()
    {
        var a=Body(0,-Y*3); var b=Body(1,-Y); var guide=Body(2);
        var ground=new PhysicsBody(new(3),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var row=Tension(new([new(a,-Y,default),new(b,-Y,default),new(guide,Y*2,default)]));
        var contact=ImpulseConstraint.Contact(a,ground,default,Y,0,0);
        var result=ImpulseSolver.Solve([row,contact]);
        Near(default(CollisionVector),a.LinearVelocity,1e-8);
        Near(-Y*.8,b.LinearVelocity,1e-8); Near(-Y*.4,guide.LinearVelocity,1e-8);
        Assert.Equal(1,result.Iterations); Assert.True(contact.AccumulatedImpulse>0);
        Near(0,row.Speed,1e-8);
    }

    [Fact]
    public void SparseOffCentreRouteConservesBothMomentaAcrossFiveBodies()
    {
        var random=new Random(7023);
        for(var sample=0;sample<200;sample++)
        {
            CollisionVector Vector()=>new(random.NextDouble()*2-1,random.NextDouble()*2-1,random.NextDouble()*2-1);
            var bodies=Enumerable.Range(0,5).Select(i=>Body(i,Vector(),1+i,Vector())).ToArray();
            var points=bodies.Select(b=>b.Center+Vector()).ToArray();
            var terms=new List<ConstraintTerm>();
            for(var i=1;i<bodies.Length;i++)
            {
                var direction=(points[i]-points[i-1]); direction/=direction.Length;
                terms.Add(new(bodies[i],direction,CollisionVector.Cross(points[i]-bodies[i].Center,direction)));
                terms.Add(new(bodies[i-1],-direction,CollisionVector.Cross(points[i-1]-bodies[i-1].Center,-direction)));
            }
            CollisionVector Momentum()=>bodies.Aggregate(default(CollisionVector),(sum,b)=>sum+b.LinearVelocity/b.InverseMass);
            CollisionVector Angular()=>bodies.Aggregate(default(CollisionVector),(sum,b)=>
                sum+CollisionVector.Cross(b.Center,b.LinearVelocity/b.InverseMass)+b.AngularMomentum);
            var p=Momentum(); var l=Angular(); var energy=bodies.Sum(b=>b.KineticEnergy);
            var gradient=new ConstraintGradient(terms.ToArray());
            var row=new ImpulseConstraint(gradient,0,double.NegativeInfinity,double.PositiveInfinity);
            var expected=-gradient.Speed/row.InverseEffectiveMass;
            ImpulseSolver.Solve([row]);
            Near(expected,row.AccumulatedImpulse); Near(0,row.Speed);
            Near(p,Momentum()); Near(l,Angular());
            Assert.True(bodies.Sum(b=>b.KineticEnergy)<=energy+1e-10);
        }
    }

    [Fact]
    public void AnyParticipantPoseChangeInvalidatesScalarAndBlockRows()
    {
        var bodies=Enumerable.Range(0,3).Select(i=>Body(i,X)).ToArray();
        var row=new ImpulseConstraint(new([new(bodies[0],X,default),new(bodies[1],-X,default),
            new(bodies[2],Y,default)]),0,double.NegativeInfinity,double.PositiveInfinity);
        var block=new BilateralConstraintBlock([row]);
        bodies[2].Advance(bodies[2].CreateTrajectory(.1),.1);
        var before=bodies.Select(b=>b.Snapshot()).ToArray();
        Assert.Throws<InvalidOperationException>(()=>row.Solve());
        Assert.Throws<InvalidOperationException>(()=>block.Solve());
        Assert.Equal(before,bodies.Select(b=>b.Snapshot()).ToArray());
    }

    [Fact]
    public void DeclarationCopiesInputsAndRejectsConflictingBodyIdentities()
    {
        var a=Body(0); var b=Body(1); var impostor=Body(1);
        ConstraintTerm[] terms=[new(b,-X,default),new(a,X,default)];
        var gradient=new ConstraintGradient(terms); terms[0]=default;
        Assert.Same(a,gradient.Bodies[0]); Assert.Same(b,gradient.Bodies[1]);
        Assert.Throws<ArgumentException>(()=>new ConstraintGradient([]));
        Assert.Throws<ArgumentNullException>(()=>new ConstraintGradient([default]));
        Assert.Throws<ArgumentException>(()=>new ConstraintGradient([new(b,X,default),new(impostor,Y,default)]));
        Assert.Throws<ArgumentException>(()=>new ConstraintGradient([new(a,new(double.NaN,0,0),default)]));
        var first=Tension(gradient); var second=Tension(new([new(impostor,X,default)]));
        Assert.Throws<ArgumentException>(()=>ImpulseSolver.Solve([first,second]));
        Assert.Throws<ArgumentException>(()=>gradient.Coupling(second.Gradient));
        var bilateral=new ImpulseConstraint(second.Gradient,0,double.NegativeInfinity,double.PositiveInfinity);
        var other=new ImpulseConstraint(gradient,0,double.NegativeInfinity,double.PositiveInfinity);
        Assert.Throws<ArgumentException>(()=>new BilateralConstraintBlock([other,bilateral]));
    }

    [Fact]
    public void CancelledRepeatedAttachmentsHaveZeroEffectiveMass()
    {
        var body=Body(0);
        var gradient=new ConstraintGradient([new(body,X,Z),new(body,-X,-Z)]);
        var row=Tension(gradient); Near(0,row.InverseEffectiveMass);
        ImpulseSolver.Solve([row]);
        var impossible=new ImpulseConstraint(gradient,1,double.NegativeInfinity,double.PositiveInfinity);
        Assert.Throws<InvalidOperationException>(()=>impossible.Solve());
    }

    [Fact]
    public void OverflowInLastParticipantDoesNotPartiallyApplyEarlierUpdates()
    {
        var a=Body(0); var b=Body(1); var c=Body(2);
        // Finite effective mass, but the angular impulse on a huge-inertia
        // participant exceeds the representable momentum.
        c=new(new(2),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(1e100,1e100,1e100));
        var gradient=new ConstraintGradient([new(a,X,default),new(b,Y,default),new(c,default,Z*1e60)]);
        var row=new ImpulseConstraint(gradient,1e300,double.NegativeInfinity,double.PositiveInfinity);
        var before=new[]{a.Snapshot(),b.Snapshot(),c.Snapshot()};
        Assert.Throws<ArgumentException>(()=>row.Solve());
        var block=new BilateralConstraintBlock([row]);
        Assert.Throws<ArgumentException>(()=>block.Solve());
        Assert.Equal(before,new[]{a.Snapshot(),b.Snapshot(),c.Snapshot()});
        Assert.Equal(0,row.AccumulatedImpulse);
    }
}
