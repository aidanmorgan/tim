using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ConstrainedPoweredImpulseTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    private static PhysicsBody Body(int id,CollisionVector velocity=default)=>new(new(id),PhysicsMotionType.Dynamic,
        RigidPose.At(new(id*2,0,0)),velocity,default,1,new(1,1,1));
    private static ConstraintGradient Axis(PhysicsBody body)=>new([new(body,Z,default)]);

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void MovingAwayCanBrakeAtTheVelocityBoundaryThenReleaseWithoutInventedWork(int sign)
    {
        var body=Body(0,-Z*(2*sign));var axis=Axis(body);
        ImpulseResponseConstraint[] limits=[new(axis,sign>0?ImpulseResponseRelation.Nonpositive:ImpulseResponseRelation.Nonnegative)];
        var initial=body.Snapshot();
        var use=ConstrainedPoweredImpulse.Apply(axis,sign,10,10,limits,1e-10);
        Assert.InRange(body.LinearVelocity.Length,0,1e-10);
        Assert.InRange(use.SuppliedWork,0,1e-10);
        Assert.InRange(Math.Abs(use.DissipatedWork-2),0,1e-10);
        Assert.InRange(Math.Abs(use.Impulse-2*sign),0,1e-10);
        var stopped=body.Snapshot();
        Assert.Equal(default,ConstrainedPoweredImpulse.Apply(axis,sign,10,10,limits,1e-10));
        Assert.Equal(stopped,body.Snapshot());
        var release=ConstrainedPoweredImpulse.Apply(axis,-2*sign,10,2,limits,1e-10);
        Assert.InRange((body.LinearVelocity+Z*(2*sign)).Length,0,1e-10);
        Assert.InRange(release.SuppliedWork,1.9999999999,2);
        body.Restore(initial);
        Assert.Equal(use,ConstrainedPoweredImpulse.Apply(axis,sign,10,10,limits,1e-10));
        Assert.Equal(stopped,body.Snapshot());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void ImpulseBudgetCanEndBeforeTheNextVelocityBoundary(int sign)
    {
        var body=Body(0,-Z*(2*sign));var axis=Axis(body);
        var use=ConstrainedPoweredImpulse.Apply(axis,sign,.5,0,
            [new(axis,sign>0?ImpulseResponseRelation.Nonpositive:ImpulseResponseRelation.Nonnegative)],1e-10);
        Assert.InRange((body.LinearVelocity+Z*(1.5*sign)).Length,0,1e-12);
        Assert.Equal(0,use.SuppliedWork);
        Assert.InRange(Math.Abs(use.Impulse-.5*sign),0,1e-12);
        Assert.InRange(Math.Abs(use.DissipatedWork-.875),0,1e-12);
    }

    [Fact]
    public void RemoteTransmissionStopSharesBrakingAndPreservesBothBodies()
    {
        var a=Body(0,Z);var b=Body(1,Z);
        var linked=new ConstraintGradient([new(a,Z,default),new(b,-Z,default)]);
        var use=ConstrainedPoweredImpulse.Apply(Axis(a),-1,10,10,
            [new(linked,ImpulseResponseRelation.Equal),new(Axis(b),ImpulseResponseRelation.Nonnegative)],1e-10);
        Assert.InRange(a.LinearVelocity.Length+b.LinearVelocity.Length,0,1e-10);
        Assert.InRange(use.SuppliedWork,0,1e-10);
        Assert.InRange(Math.Abs(use.DissipatedWork-1),0,1e-10);
        Assert.InRange(Math.Abs(use.Impulse+2),0,1e-10);
    }

    [Fact]
    public void InfeasibleInputRejectsWithoutPartialVelocityChanges()
    {
        var a=Body(0,-Z);var before=a.Snapshot();var revision=a.PoseRevision;
        Assert.Throws<ArgumentException>(()=>ConstrainedPoweredImpulse.Apply(Axis(a),1,10,10,
            [new(Axis(a),ImpulseResponseRelation.Nonnegative)],1e-10));
        Assert.Equal(before,a.Snapshot());
        Assert.Equal(revision,a.PoseRevision);
    }
}
