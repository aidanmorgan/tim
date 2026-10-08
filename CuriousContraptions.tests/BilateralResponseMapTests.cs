using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class BilateralResponseMapTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0);
    private static PhysicsBody Body(int id,double mass=1)=>new(new(id),PhysicsMotionType.Dynamic,
        RigidPose.Identity,default,default,mass,new(1,2,3));
    private static ConstraintGradient Axis(PhysicsBody body,CollisionVector axis)=>new([new(body,axis,default)]);

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void SharedMassProjectionRetainsEarlierDirectionsAndDoesNotMutateBodies(double mass)
    {
        var a=Body(0);var b=Body(1,mass);
        var equality=new ConstraintGradient([new(a,X,default),new(b,-X,default)]);
        var map=new BilateralResponseMap([equality]);
        var before=new[]{a.Snapshot(),b.Snapshot()};
        var first=map.Project(Axis(a,X),1e-13);
        var retained=first.Reactions.ToArray();
        var other=map.Project(Axis(a,Y),1e-13);
        Assert.Equal(retained,first.Reactions);
        Assert.Equal(before,new[]{a.Snapshot(),b.Snapshot()});
        Assert.InRange(Math.Abs(equality.Coupling(first.Gradient)),0,1e-13);
        Assert.InRange(Math.Abs(first.Gradient.Coupling(first.Gradient)-1/(1+mass)),0,1e-13);
        Assert.Equal(1,other.Gradient.Coupling(other.Gradient));
        Assert.Equal(0,first.Gradient.Coupling(other.Gradient));
        first.Gradient.Apply(1);
        Assert.InRange(Math.Abs(a.LinearVelocity.X-1/(1+mass)),0,1e-13);
        Assert.InRange(Math.Abs(b.LinearVelocity.X-1/(1+mass)),0,1e-13);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DependentRowsAndLockedTangentProduceZeroResponse(bool reverse)
    {
        var body=Body(0);var x=Axis(body,X);var y=Axis(body,Y);
        ConstraintGradient[] rows=[x,y,x];
        if(reverse)Array.Reverse(rows);
        var projected=new BilateralResponseMap(rows).Project(x,1e-13);
        Assert.InRange(projected.Gradient.Coupling(projected.Gradient),0,1e-26);
        Assert.InRange(Math.Abs(projected.Reactions.Sum()+1),0,1e-13);
        Assert.Equal(default,body.LinearVelocity);
    }

    [Fact]
    public void MapRejectsStalePosesAndForeignIdentityWithoutMutation()
    {
        var body=Body(0);var other=Body(0);var map=new BilateralResponseMap([Axis(body,X)]);
        var before=body.Snapshot();
        Assert.Throws<ArgumentException>(()=>map.Project(Axis(other,Y),1e-13));
        Assert.Equal(before,body.Snapshot());
        body.Restore(before with {Pose=RigidPose.At(new(2,3,4))});
        var changed=body.Snapshot();
        Assert.Throws<InvalidOperationException>(()=>map.Project(Axis(body,Y),1e-13));
        Assert.Equal(changed,body.Snapshot());
        Assert.Throws<ArgumentException>(()=>new BilateralResponseMap([Axis(body,X),Axis(other,Y)]));
    }

    [Fact]
    public void EmptyAndZeroMassSpacesRetainUnconstrainedResponseAndRejectInvalidBudget()
    {
        var body=Body(0);var axis=Axis(body,X);
        var ground=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        foreach(var map in new[]{new BilateralResponseMap([]),new BilateralResponseMap([Axis(ground,Y)])})
        {
            var result=map.Project(axis,1e-13);
            Assert.Equal(1,result.Gradient.Coupling(result.Gradient));
            Assert.All(result.Reactions,value=>Assert.Equal(0,value));
            Assert.Throws<ArgumentOutOfRangeException>(()=>map.Project(axis,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>map.Project(axis,double.NaN));
        }
    }
}

