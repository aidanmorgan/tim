using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AdmissibleImpulseResponseTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    private static PhysicsBody Body(int id)=>new(new(id),PhysicsMotionType.Dynamic,
        RigidPose.At(new(id*2,0,0)),new(.1,.2,.3),new(.2,.3,.4),1,new(1,1,1));
    private static ConstraintGradient Axis(PhysicsBody body,double sign=1)=>new([new(body,Z*sign,default)]);

    [Theory]
    [InlineData(ImpulseResponseRelation.Nonnegative,-1,0)]
    [InlineData(ImpulseResponseRelation.Nonnegative,1,1)]
    [InlineData(ImpulseResponseRelation.Nonpositive,-1,-1)]
    [InlineData(ImpulseResponseRelation.Nonpositive,1,0)]
    [InlineData(ImpulseResponseRelation.Equal,-1,0)]
    [InlineData(ImpulseResponseRelation.Equal,1,0)]
    public void OneWayLimitsBlockOnlyForbiddenResponseWithoutMutatingLiveMotion(
        ImpulseResponseRelation relation,int direction,int expected)
    {
        var body=Body(0);var before=body.Snapshot();
        var response=new AdmissibleImpulseResponse(Axis(body,direction),[new(Axis(body),relation)],1e-10);
        Assert.Equal(before,body.Snapshot());
        Assert.InRange(Math.Abs(Axis(body).Coupling(response.Gradient)-expected),0,1e-12);
        Assert.InRange(Math.Abs(Assert.Single(response.ReactionsPerUnitImpulse.ToArray())-(expected-direction)),0,1e-12);
        var repeat=new AdmissibleImpulseResponse(Axis(body,direction),[new(Axis(body),relation)],1e-10);
        Assert.Equal(response.Gradient.Terms.ToArray(),repeat.Gradient.Terms.ToArray());
        Assert.Equal(response.ReactionsPerUnitImpulse.ToArray(),repeat.ReactionsPerUnitImpulse.ToArray());
    }

    [Theory]
    [InlineData(-1,false)]
    [InlineData(1,false)]
    [InlineData(-1,true)]
    [InlineData(1,true)]
    public void TransmissionSharesMassAndPropagatesARemoteStop(int direction,bool reverse)
    {
        var a=Body(0);var b=Body(1);var before=new[]{a.Snapshot(),b.Snapshot()};
        var transmission=new ConstraintGradient([new(a,Z,default),new(b,-Z,default)]);
        ImpulseResponseConstraint[] rows=[new(transmission,ImpulseResponseRelation.Equal),
            new(Axis(b),ImpulseResponseRelation.Nonnegative)];
        if(reverse) Array.Reverse(rows);
        var response=new AdmissibleImpulseResponse(Axis(a,direction),rows,1e-10);
        var expected=direction<0?0:.5;
        Assert.InRange(Math.Abs(Axis(a).Coupling(response.Gradient)-expected),0,1e-10);
        Assert.InRange(Math.Abs(Axis(b).Coupling(response.Gradient)-expected),0,1e-10);
        Assert.Equal(before,new[]{a.Snapshot(),b.Snapshot()});
        for(var i=0;i<rows.Length;i++)
            if(rows[i].Relation==ImpulseResponseRelation.Nonnegative)
                Assert.True(response.ReactionsPerUnitImpulse[i]>=0);
    }

    [Fact]
    public void UndefinedRelationsForeignIdentitiesAndStalePosesReject()
    {
        var body=Body(0);var gradient=Axis(body);var before=body.Snapshot();
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ImpulseResponseConstraint(gradient,(ImpulseResponseRelation)99));
        Assert.Throws<ArgumentException>(()=>new AdmissibleImpulseResponse(gradient,
            [new(Axis(Body(0)),ImpulseResponseRelation.Equal)],1e-10));
        var response=new AdmissibleImpulseResponse(gradient,[],1e-10);
        body.Advance(body.CreateTrajectory(.01,default),.01);
        Assert.Throws<InvalidOperationException>(()=>response.Gradient);
        body.Restore(before);
    }
}
