using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class BilateralResponseProjectionTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    private static PhysicsBody Body(int id)=>new(new(id),PhysicsMotionType.Dynamic,
        RigidPose.At(new(id*2,0,0)),default,default,1,new(1,1,1));
    private static ConstraintGradient Axis(PhysicsBody body)=>new([new(body,Z,default)]);

    [Theory]
    [InlineData(2,-1)]
    [InlineData(2,1)]
    [InlineData(8,-1)]
    [InlineData(8,1)]
    [InlineData(16,-1)]
    [InlineData(16,1)]
    public void ConnectedMassesShareTheAppliedResponseAndWork(int count,int sign)
    {
        var bodies=Enumerable.Range(0,count).Select(Body).ToArray();
        var constraints=Enumerable.Range(1,count-1).Select(i=>new ConstraintGradient(
            [new(bodies[i-1],Z,default),new(bodies[i],-Z,default)])).ToArray();
        var applied=Axis(bodies[0]);
        var before=bodies.Select(body=>body.Snapshot()).ToArray();
        var response=new AdmissibleImpulseResponse(applied,constraints.Select(row=>new ImpulseResponseConstraint(row,ImpulseResponseRelation.Equal)).ToArray(),1e-13);
        Assert.Equal(before,bodies.Select(body=>body.Snapshot()).ToArray());
        Assert.Equal(count-1,response.ReactionsPerUnitImpulse.Length);
        foreach(var row in constraints)
            Assert.InRange(Math.Abs(row.Coupling(response.Gradient)),0,1e-12);
        Assert.InRange(Math.Abs(applied.Coupling(response.Gradient)-1.0/count),0,1e-12);
        Assert.InRange(Math.Abs(response.Gradient.Coupling(response.Gradient)-1.0/count),0,1e-12);
        var use=PoweredImpulse.Apply(response.Gradient,sign,100,count*.5);
        foreach(var body in bodies) Assert.InRange(Math.Abs(body.LinearVelocity.Z-sign),0,1e-10);
        Assert.InRange(Math.Abs(use.SuppliedWork-count*.5),0,1e-10);
        Assert.InRange(Math.Abs(bodies.Sum(body=>body.KineticEnergy)-use.SuppliedWork),0,1e-10);
        foreach(var row in constraints) Assert.InRange(Math.Abs(row.Speed),0,1e-10);
    }

    [Fact]
    public void FixedCoordinateHasZeroResponseAndNoMutation()
    {
        var body=Body(0);var applied=Axis(body);var before=body.Snapshot();
        var response=new AdmissibleImpulseResponse(applied,[new(applied,ImpulseResponseRelation.Equal)],1e-13);
        Assert.Equal(0,response.Gradient.Coupling(response.Gradient));
        Assert.Equal(-1,Assert.Single(response.ReactionsPerUnitImpulse.ToArray()));
        Assert.Equal(before,body.Snapshot());
    }

    [Fact]
    public void UnconstrainedResponsePreservesTheAppliedGradient()
    {
        var body=Body(0);var applied=Axis(body);
        var response=new AdmissibleImpulseResponse(applied,[],1e-13);
        Assert.Empty(response.ReactionsPerUnitImpulse.ToArray());
        Assert.Equal(applied.Terms.ToArray(),response.Gradient.Terms.ToArray());
    }

    [Fact]
    public void DependentConstraintsShareOneResponseAndConflictingIdentitiesReject()
    {
        var body=Body(0);var other=Body(0);var applied=Axis(body);var before=body.Snapshot();
        var response=new AdmissibleImpulseResponse(applied,[new(applied,ImpulseResponseRelation.Equal),new(applied,ImpulseResponseRelation.Equal)],1e-13);
        Assert.InRange(response.Gradient.Coupling(response.Gradient),0,1e-24);
        Assert.InRange(Math.Abs(response.ReactionsPerUnitImpulse.ToArray().Sum()+1),0,1e-12);
        Assert.Throws<ArgumentException>(()=>new AdmissibleImpulseResponse(applied,[new(Axis(other),ImpulseResponseRelation.Equal)],1e-13));
        Assert.Equal(before,body.Snapshot());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UncoupledRedundantRowsCannotChangeTheDrivenResponse(bool sameBody)
    {
        var body=Body(0);var other=sameBody?body:Body(1);
        var applied=Axis(body);
        var unrelated=new ConstraintGradient([new(other,new(1,0,0),default)]);
        var before=other.Snapshot();
        var response=new AdmissibleImpulseResponse(applied,[new(unrelated,ImpulseResponseRelation.Equal),new(unrelated,ImpulseResponseRelation.Equal)],1e-13);
        Assert.Equal(new[]{0d,0d},response.ReactionsPerUnitImpulse.ToArray());
        var use=PoweredImpulse.Apply(response.Gradient,1,10,.5);
        Assert.Equal(new CollisionVector(0,0,1),body.LinearVelocity);
        Assert.InRange(Math.Abs(use.SuppliedWork-.5),0,1e-12);
        if(!sameBody) Assert.Equal(before,other.Snapshot());
        other.Restore(other.Snapshot() with {Pose=RigidPose.At(new(7,8,9))});
        Assert.Throws<InvalidOperationException>(()=>response.Gradient);
    }

    [Fact]
    public void UncoupledConflictingIdentitiesStillReject()
    {
        var body=Body(0);var first=Body(1);var second=Body(1);
        Assert.Throws<ArgumentException>(()=>new AdmissibleImpulseResponse(Axis(body),[new(Axis(first),ImpulseResponseRelation.Equal),new(Axis(second),ImpulseResponseRelation.Equal)],1e-13));
    }

    [Fact]
    public void ChangedParticipantPoseInvalidatesResponse()
    {
        var body=Body(0);var applied=Axis(body);
        var response=new AdmissibleImpulseResponse(applied,[],1e-13);
        body.Restore(body.Snapshot() with {Pose=RigidPose.At(new(1,2,3))});
        Assert.Throws<InvalidOperationException>(()=>response.Gradient);
        Assert.Throws<InvalidOperationException>(()=>response.ReactionsPerUnitImpulse.ToArray());
    }
}
