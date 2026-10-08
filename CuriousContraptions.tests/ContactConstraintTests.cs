using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ContactConstraintTests
{
    private static readonly CollisionVector Y=new(0,1,0);
    private static PhysicsBody Body(CollisionVector velocity,InertiaTensor? inertia=null)=>
        new(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,velocity,default,1,inertia??new InertiaTensor(1,1,1));
    private static PhysicsBody Ground()=>new(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static void Near(CollisionVector a,CollisionVector b,double tolerance=1e-8)=>Assert.InRange((a-b).Length,0,tolerance);


    [Fact]
    public void WarmStartMatchesOrderedGradientAcrossDistinctParticipantsAndRejectsRepeatedUse()
    {
        var a=Body(new(.4,-2,.7));var b=Ground();
        var shaft=new PhysicsBody(new(7),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,2,new(3,4,5));
        var normal=new ConstraintGradient([new(a,Y,default),new(b,-Y,default)]);
        var u=new ConstraintGradient([new(shaft,default,Y*.3),new(a,new(0,0,1),default)]);
        var v=new ConstraintGradient([new(b,new(-1,0,0),default),new(shaft,default,Y*-.2)]);
        var kinematics=new ContactKinematics(Y,normal,u,v);
        var contact=new ContactConstraint(kinematics,0,0,1);
        const double normalImpulse=.7,uImpulse=.12,vImpulse=-.09;
        var combined=new ConstraintGradient([
            ..normal.Terms.ToArray().Select(t=>new ConstraintTerm(t.Body,t.Linear*normalImpulse,t.Angular*normalImpulse)),
            ..u.Terms.ToArray().Select(t=>new ConstraintTerm(t.Body,t.Linear*uImpulse,t.Angular*uImpulse)),
            ..v.Terms.ToArray().Select(t=>new ConstraintTerm(t.Body,t.Linear*vImpulse,t.Angular*vImpulse))]);
        var expected=combined.Terms.ToArray().Select(t=>t.Body.AfterImpulse(t.Linear,t.Angular)).ToArray();
        var impulse=new ContactImpulse(normalImpulse,kinematics.U*uImpulse+kinematics.V*vImpulse);
        contact.WarmStart(impulse);
        for(var i=0;i<contact.Bodies.Length;i++)
        {
            Assert.Equal(expected[i].Linear,contact.Bodies[i].LinearVelocity);
            Assert.Equal(expected[i].AngularMomentum,contact.Bodies[i].AngularMomentum);
        }
        var retained=contact.Bodies.ToArray().Select(body=>body.Snapshot()).ToArray();
        Assert.Throws<InvalidOperationException>(()=>contact.WarmStart(impulse));
        Assert.Equal(retained,contact.Bodies.ToArray().Select(body=>body.Snapshot()).ToArray());
    }

    [Fact]
    public void RepeatedFrictionCorrectionsAvoidCombinedGradientAllocation()
    {
        var body=Body(new(3,-2,1));
        var contact=new ContactConstraint(ContactKinematics.AtPoint(body,Ground(),default,Y),0,0,.5);
        for(var i=0;i<100;i++)contact.Solve();
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<100;i++)contact.Solve();
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        // The normal scalar row still allocates its two-body velocity array.
        // Tangent corrections must no longer build dictionaries/gradients/lists.
        Assert.InRange(allocated,1,256*100);
        Assert.InRange(contact.Residual,0,1e-8);
    }

    [Fact]
    public void OverflowingWarmStartRejectsEveryParticipantAndScratchCanBeReused()
    {
        var a=Body(default);var b=Ground();
        var normal=new ConstraintGradient([new(a,Y*1e100,default),new(b,-Y*1e100,default)]);
        var (u,v)=ContactKinematics.Axes(Y);
        var kinematics=new ContactKinematics(Y,normal,
            new([new(a,u,default),new(b,-u,default)]),
            new([new(a,v,default),new(b,-v,default)]));
        var contact=new ContactConstraint(kinematics,0,0,.5);
        var before=new[]{a.Snapshot(),b.Snapshot()};
        Assert.Throws<ArgumentException>(()=>contact.WarmStart(new(1e250,default)));
        Assert.Equal(before,new[]{a.Snapshot(),b.Snapshot()});
        contact.WarmStart(new(1e-100,default));
        Assert.Equal(Y,a.LinearVelocity);
        Assert.Equal(before[1],b.Snapshot());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.3)]
    [InlineData(.8)]
    [InlineData(1.5)]
    public void CoulombDiskIsInvariantToSlidingDirection(double angle)
    {
        var direction=new CollisionVector(Math.Cos(angle),0,Math.Sin(angle));
        var body=Body(direction*4-Y*2);
        var contact=new ContactConstraint(ContactKinematics.AtPoint(body,Ground(),default,Y),0,0,.5);
        ImpulseSolver.Solve([contact]);
        Near(direction*3,body.LinearVelocity);
        Near(-direction,contact.TangentImpulse);
        Assert.InRange(contact.Residual,0,1e-8);
    }

    [Theory]
    [InlineData(.1,0)]
    [InlineData(1e-100,0)]
    [InlineData(1e-180,0)]
    [InlineData(1e-280,0)]
    [InlineData(.1,.6)]
    [InlineData(1e-100,.6)]
    [InlineData(1e-180,.6)]
    [InlineData(1e-280,.6)]
    public void TinyNormalBudgetRetainsFiniteOpposingFrictionAndExactReplay(double approach,double angle)
    {
        var direction=new CollisionVector(Math.Cos(angle),0,Math.Sin(angle));
        var body=Body(direction*4-Y*approach);var ground=Ground();
        var before=body.Snapshot();
        (PhysicsBodySnapshot Body,ContactImpulse Impulse) Run()
        {
            var row=new ContactConstraint(ContactKinematics.AtPoint(body,ground,default,Y),0,0,.5);
            row.Solve();
            Assert.Equal(approach,row.Normal.AccumulatedImpulse);
            var radius=.5*approach;
            // Normalize before taking the norm: squaring a tiny impulse itself
            // would underflow and hide an absent or oversized friction response.
            var relative=row.TangentImpulse/radius;
            Assert.InRange(relative.Length,1-1e-12,1+1e-14);
            Near(-direction,relative,1e-12);
            Assert.Equal(0,body.LinearVelocity.Y);
            Assert.True(body.KineticEnergy<=.5*(16+approach*approach));
            return(body.Snapshot(),row.Impulse);
        }
        var first=Run();body.Restore(before);
        Assert.Equal(first,Run());
    }

    [Fact]
    public void StickingStopsSlipWithoutExceedingTheCone()
    {
        var body=Body(new(.2,-2,.3));
        var contact=new ContactConstraint(ContactKinematics.AtPoint(body,Ground(),default,Y),0,0,.5);
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
            var contact=new ContactConstraint(ContactKinematics.AtPoint(body,Ground(),default,Y),0,0,friction);
            ImpulseSolver.Solve([contact]);
            Near(velocity,body.LinearVelocity); Near(default,contact.TangentImpulse);
        }
        var falling=Body(new(2,-1,3));
        ImpulseSolver.Solve([new ContactConstraint(ContactKinematics.AtPoint(falling,Ground(),default,Y),0,0,0)]);
        Near(new(2,0,3),falling.LinearVelocity);
    }

    [Fact]
    public void OffCentreAnisotropicSlidingObeysMaximumDissipation()
    {
        var body=Body(new(5,-3,4),new InertiaTensor(2,3,4,.3,.2,.4));
        var point=new CollisionVector(.4,-.7,.3);
        var contact=new ContactConstraint(ContactKinematics.AtPoint(body,Ground(),point,Y),0,0,.4);
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
        var contact=new ContactConstraint(ContactKinematics.AtPoint(body,Ground(),default,Y),0,0,.5);
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
        var a=Body(new(4,-2,1)); var b=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(0,-2,0)),default,default,2,new InertiaTensor(2,2,2));
        var initial=a.LinearVelocity;
        ImpulseSolver.Solve([new ContactConstraint(ContactKinematics.AtPoint(a,b,new(0,-1,0),Y),0,0,.7)]);
        Near(initial,a.LinearVelocity+b.LinearVelocity*2);
        var angular=CollisionVector.Cross(a.Center,a.LinearVelocity)+a.AngularVelocity+
            CollisionVector.Cross(b.Center,b.LinearVelocity*2)+b.AngularVelocity*2;
        Near(default,angular);
    }

    [Fact]
    public void InvalidFrictionIsRejected()
    {
        foreach(var invalid in new[]{-.1,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new ContactConstraint(ContactKinematics.AtPoint(Body(default),Ground(),default,Y),0,0,invalid));
    }
}
