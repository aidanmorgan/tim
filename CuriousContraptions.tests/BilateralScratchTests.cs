using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class BilateralScratchTests
{
    private static readonly CollisionVector Axis=new(0,0,1);

    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    [InlineData(32)]
    public void RepeatedLoadedSolvesReuseScratchAndConserveMomentum(int count)
    {
        var bodies=Enumerable.Range(0,count).Select(i=>new PhysicsBody(new(i),
            PhysicsMotionType.Dynamic,RigidPose.At(new(i,0,0)),default,default,2,new(2,3,4))).ToArray();
        var rows=Enumerable.Range(0,count).Select(i=>new ImpulseConstraint(
            new([new(bodies[i],Axis,default),new(bodies[(i+1)%count],-Axis,default)]),
            0,double.NegativeInfinity,double.PositiveInfinity)).ToArray();
        var block=new BilateralConstraintBlock(rows);
        void LoadedSolve(int index)
        {
            var selected=bodies[index%count];
            selected.ApplyImpulse(Axis*(index%2==0?1:-1),selected.Center);
            block.Solve();
        }
        for(var i=0;i<256;i++) LoadedSolve(i);
        var before=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<1000;i++) LoadedSolve(i);
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Console.WriteLine($"bodies={count}, solves=1000, allocated={allocated}");
        Assert.Equal(0,allocated);
        Assert.InRange(block.Residual,0,1e-12);
        Assert.InRange(bodies.Sum(b=>b.LinearVelocity.Z*2),-1e-12,1e-12);
        foreach(var body in bodies)
        {
            Assert.InRange(body.LinearVelocity.Length,0,1e-12);
            Assert.Equal(default,body.AngularMomentum);
        }
    }

    [Fact]
    public void AlternatingLinearAndAngularLoadsDoNotRetainPriorScratch()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            default,default,2,new(2,3,4));
        var linear=new ImpulseConstraint(new([new(body,Axis,default)]),0,
            double.NegativeInfinity,double.PositiveInfinity);
        var angular=new ImpulseConstraint(new([new(body,default,Axis)]),0,
            double.NegativeInfinity,double.PositiveInfinity);
        var block=new BilateralConstraintBlock([linear,angular]);
        for(var iteration=0;iteration<100;iteration++)
        {
            if(iteration%2==0) body.ApplyImpulse(Axis,body.Center);
            else body.CommitVelocity(body.AfterImpulse(default,Axis));
            block.Solve();
            Assert.InRange(body.LinearVelocity.Length,0,1e-14);
            Assert.InRange(body.AngularMomentum.Length,0,1e-14);
            Assert.InRange(Math.Abs(linear.AccumulatedImpulse+(iteration/2+1)),0,1e-12);
            Assert.InRange(Math.Abs(angular.AccumulatedImpulse+(iteration+1)/2),0,1e-12);
        }
    }

    [Fact]
    public void NumericFailurePreservesStateAndTheBlockCanBeRetried()
    {
        var x=new CollisionVector(1,0,0);var y=new CollisionVector(0,1,0);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            default,default,1,new(1,1,1));
        var rows=new[]{
            new ImpulseConstraint(new([new(body,x,default)]),0,double.NegativeInfinity,double.PositiveInfinity),
            new ImpulseConstraint(new([new(body,y*2,default)]),0,double.NegativeInfinity,double.PositiveInfinity)};
        var block=new BilateralConstraintBlock(rows);
        body.CommitVelocity(new(x+y,default));block.Solve();
        var prior=rows.Select(row=>row.AccumulatedImpulse).ToArray();
        body.CommitVelocity(new(x+y*1e308,default));
        var failedState=body.Snapshot();
        Assert.Throws<InvalidOperationException>(()=>block.Solve());
        Assert.Equal(failedState,body.Snapshot());
        Assert.Equal(prior,rows.Select(row=>row.AccumulatedImpulse).ToArray());
        body.CommitVelocity(new(x*2+y*4,default));
        block.Solve();
        Assert.Equal(default,body.LinearVelocity);
        Assert.Equal(default,body.AngularMomentum);
        Assert.Equal(-3,rows[0].AccumulatedImpulse);
        Assert.Equal(-2.5,rows[1].AccumulatedImpulse);
    }
}
