using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class BilateralConstraintBlockTests
{
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity,CollisionVector angular)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,angular,2,new InertiaTensor(2,3,4,.2,.1,.3));
    private static PhysicsBody Ground()=>new(new(2),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    [Theory]
    [InlineData(1e-8,false)]
    [InlineData(1d,false)]
    [InlineData(1e8,false)]
    [InlineData(1e-8,true)]
    [InlineData(1d,true)]
    [InlineData(1e8,true)]
    public void RankRevealingMassSolveRetainsEveryDependentEquation(double scale,bool reverse)
    {
        var body=Body(0,default,default,default);
        var rows=new[]{
            new ConstraintGradient([new(body,X*scale,default)]),
            new ConstraintGradient([new(body,Y/scale,default)]),
            new ConstraintGradient([new(body,X+Y,default)])};
        if(reverse) Array.Reverse(rows);
        var matrix=new ConstraintMassMatrix(rows,new double[rows.Length]);
        Assert.Equal(2,matrix.Rank);
        var target=X*2+Y*3;
        var rhs=rows.Select(row=>CollisionVector.Dot(row.Terms[0].Linear,target)).ToArray();
        var result=new double[rows.Length];
        matrix.Solve(rhs,result);
        for(var i=0;i<rows.Length;i++)
        {
            var actual=Enumerable.Range(0,rows.Length).Sum(j=>rows[i].Coupling(rows[j])*result[j]);
            Assert.InRange(Math.Abs(actual-rhs[i]),0,1e-12*Math.Abs(rhs[i]));
        }
        Assert.Equal(default,body.LinearVelocity);
    }

    [Theory]
    [InlineData(1e-20,false)]
    [InlineData(1d,false)]
    [InlineData(1e20,false)]
    [InlineData(1e-20,true)]
    [InlineData(1d,true)]
    [InlineData(1e20,true)]
    public void FactorCancellationPreservesZeroRightHandSideEquations(double scale,bool reverse)
    {
        var body=Body(0,default,default,default);
        var rows=new[]{
            new ConstraintGradient([new(body,X+Y,default)]),
            new ConstraintGradient([new(body,X-Y,default)]),
            new ConstraintGradient([new(body,X+Z,default)])};
        if(reverse) Array.Reverse(rows);
        var matrix=new ConstraintMassMatrix(rows,new double[3]);
        Assert.Equal(3,matrix.Rank);
        for(var selected=0;selected<rows.Length;selected++)
        {
            var rhs=rows.Select(row=>row.Coupling(rows[selected])*scale).ToArray();
            var result=new double[3];matrix.Solve(rhs,result);
            for(var i=0;i<3;i++)
            {
                Assert.InRange(Math.Abs(result[i]-(i==selected?scale:0)),0,Math.Abs(scale)*1e-12);
                var actual=Enumerable.Range(0,3).Sum(j=>rows[i].Coupling(rows[j])*result[j]);
                Assert.InRange(Math.Abs(actual-rhs[i]),0,Math.Abs(scale)*1e-12);
            }
        }
        Assert.Equal(default,body.LinearVelocity);
    }

    [Theory]
    [InlineData(1e-310)]
    [InlineData(1e-320)]
    [InlineData(double.Epsilon)]
    public void SubnormalRightHandSidesSolveWithoutErasingInconsistentEquations(double scale)
    {
        var body=Body(0,default,default,default);
        var rows=new[]{
            new ConstraintGradient([new(body,X+Y,default)]),
            new ConstraintGradient([new(body,X-Y,default)]),
            new ConstraintGradient([new(body,X+Z,default)])};
        var mass=new ConstraintMassMatrix(rows,new double[3]);
        var expected=new[]{2*scale,-3*scale,4*scale};
        var rhs=rows.Select(row=>Enumerable.Range(0,3).Sum(j=>row.Coupling(rows[j])*expected[j])).ToArray();
        var result=new double[3];mass.Solve(rhs,result);
        for(var i=0;i<3;i++)
            Assert.InRange(Math.Abs(result[i]-expected[i]),0,Math.Max(scale*1e-12,4*double.Epsilon));
        var duplicate=new ConstraintMassMatrix([rows[0],rows[0]],[0d,0d]);
        var unchanged=new[]{7d,8d};
        Assert.Throws<InvalidOperationException>(()=>duplicate.Solve([scale,2*scale],unchanged));
        Assert.Equal(new[]{7d,8d},unchanged);
        var zero=new ConstraintMassMatrix([new ConstraintGradient([new(body,default,default)])],[0d]);
        var sentinel=new[]{7d};
        Assert.Throws<InvalidOperationException>(()=>zero.Solve([scale],sentinel));
        Assert.Equal(7,sentinel[0]);
        Assert.Equal(default,body.LinearVelocity);
    }

    [Theory]
    [InlineData(1e-310,false,false)]
    [InlineData(1e-320,false,false)]
    [InlineData(1e-310,true,false)]
    [InlineData(1e-320,true,false)]
    [InlineData(1e-310,false,true)]
    [InlineData(1e-320,false,true)]
    [InlineData(1e-310,true,true)]
    [InlineData(1e-320,true,true)]
    public void IndependentMassGroupsPreserveTinyEquationsBesideOrdinaryLoads(double scale,bool reverse,bool sharedBody)
    {
        var large=Body(0,default,default,default);var small=sharedBody?large:Body(1,default,default,default);
        var major=new ConstraintGradient([new(large,sharedBody?default:X,sharedBody?Z:default)]);
        var rows=new[]{
            major,
            new ConstraintGradient([new(small,X+Y,default)]),
            new ConstraintGradient([new(small,X-Y,default)]),
            new ConstraintGradient([new(small,X+Z,default)])};
        var expected=new[]{6d,2*scale,-3*scale,4*scale};
        if(reverse){Array.Reverse(rows);Array.Reverse(expected);}
        var rhs=rows.Select(row=>Enumerable.Range(0,4).Sum(j=>row.Coupling(rows[j])*expected[j])).ToArray();
        var matrix=new ConstraintMassMatrix(rows,new double[4]);
        Assert.Equal(4,matrix.Rank);
        var result=new double[4];matrix.Solve(rhs,result);
        for(var i=0;i<4;i++)
            Assert.InRange(Math.Abs(result[i]-expected[i]),0,Math.Max(Math.Abs(expected[i])*1e-12,4*double.Epsilon));
        var repeated=new double[4];matrix.Solve(rhs,repeated);Assert.Equal(result,repeated);
        var minor=new ConstraintGradient([new(small,X,default)]);
        var dependent=new ConstraintMassMatrix([major,minor,minor],[0d,0d,0d]);
        var sentinel=new[]{7d,8d,9d};
        Assert.Throws<InvalidOperationException>(()=>dependent.Solve([3d,scale,2*scale],sentinel));
        Assert.Equal(new[]{7d,8d,9d},sentinel);
        Assert.Equal(default,large.LinearVelocity);Assert.Equal(default,small.LinearVelocity);
    }

    [Fact]
    public void InconsistentDependentEquationRejectsWithoutPublishingPartialSolution()
    {
        var body=Body(0,default,default,default);
        var rows=new[]{
            new ConstraintGradient([new(body,X,default)]),
            new ConstraintGradient([new(body,Y,default)]),
            new ConstraintGradient([new(body,X+Y,default)])};
        var matrix=new ConstraintMassMatrix(rows,new double[3]);
        var result=new[]{7d,8d,9d};
        Assert.Throws<InvalidOperationException>(()=>matrix.Solve(new[]{1d,1d,3d},result));
        Assert.Equal(new[]{7d,8d,9d},result);
        var zero=new ConstraintMassMatrix([new ConstraintGradient([new(body,default,default)])],[0d]);
        Assert.Equal(0,zero.Rank);
        var answer=new[]{7d};zero.Solve([0d],answer);Assert.Equal(0,answer[0]);
        Assert.Throws<InvalidOperationException>(()=>zero.Solve([1d],answer));
        Assert.Equal(0,answer[0]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void OffsetJointBlockConvergesWithoutInflatingIterationBudget(double length)
    {
        var a=Body(0,Z*length,new(3,2,1),new(2,3,4)); var ground=Ground();
        var fa=new JointFrame(default,RigidRotation.Identity);
        var rows=JointConstraints.Slider(a,ground,fa,fa);
        var result=ImpulseSolver.Solve(rows,8,1e-7);
        Assert.InRange(result.MaximumResidual,0,1e-7);
        Assert.InRange(a.AngularVelocity.Length,0,1e-7);
        Assert.InRange(Math.Abs(a.LinearVelocity.X)+Math.Abs(a.LinearVelocity.Y),0,1e-7);
        Assert.InRange(Math.Abs(a.LinearVelocity.Z-1),0,1e-7);
    }
    [Theory]
    [InlineData(2,false)]
    [InlineData(8,false)]
    [InlineData(32,false)]
    [InlineData(2,true)]
    [InlineData(8,true)]
    [InlineData(32,true)]
    public void ClosedMultiBodyBlockSharesMomentumInOneCoupledSolve(int count,bool reverse)
    {
        var bodies=Enumerable.Range(0,count).Select(i=>Body(i,X*i,Z*i,default)).ToArray();
        var rows=Enumerable.Range(0,count).Select(i=>new ImpulseConstraint(
            new ConstraintGradient([new(bodies[i],Z,default),new(bodies[(i+1)%count],-Z,default)]),
            0,double.NegativeInfinity,double.PositiveInfinity)).ToArray();
        if(reverse) Array.Reverse(rows);
        var energy=bodies.Sum(b=>b.KineticEnergy);
        var initial=bodies.Select(b=>b.Snapshot()).ToArray();
        var block=new BilateralConstraintBlock(rows);
        block.Solve();
        Assert.InRange(block.Residual,0,1e-10);
        foreach(var body in bodies)
            Assert.InRange(Math.Abs(body.LinearVelocity.Z-(count-1)*.5),0,1e-10);
        Assert.True(bodies.Sum(b=>b.KineticEnergy)<=energy);
        var after=bodies.Select(b=>b.Snapshot()).ToArray();
        for(var i=0;i<count;i++) bodies[i].Restore(initial[i]);
        var replay=rows.Select(row=>new ImpulseConstraint(row.Gradient,0,
            double.NegativeInfinity,double.PositiveInfinity)).ToArray();
        new BilateralConstraintBlock(replay).Solve();
        Assert.Equal(after,bodies.Select(b=>b.Snapshot()).ToArray());
    }

    [Fact]
    public void HingeAndFrictionContactShareOneCoupledSolve()
    {
        var beam=Body(0,X,default,default); var ground=Ground();
        var ball=Body(1,X*2+Y,new(1,-3,.5),default);
        var frame=new JointFrame(default,RigidRotation.Identity);
        var constraints=new List<IImpulseConstraint>(JointConstraints.Hinge(beam,ground,frame,frame));
        var contact=new ContactConstraint(ContactKinematics.AtPoint(ball,beam,X*2,Y),0,0,.4);
        constraints.Add(contact);
        var solved=ImpulseSolver.Solve(constraints);
        Assert.InRange(solved.MaximumResidual,0,1e-8);
        Assert.InRange(beam.PointVelocity(default).Length,0,1e-8);
        Assert.True(contact.Normal.AccumulatedImpulse>0);
        Assert.True(contact.TangentImpulse.Length>0);
        Assert.InRange(Math.Abs(beam.AngularVelocity.X)+Math.Abs(beam.AngularVelocity.Y),0,1e-8);
    }
    [Fact]
    public void RedundantRowsSolveButBoundedRowsAreRejected()
    {
        var a=Body(0,default,default,default); var b=Ground();
        ImpulseConstraint Row()=>new(new ConstraintJacobian(X,default,-X,default).Bind(a,b),0,double.NegativeInfinity,double.PositiveInfinity);
        a.ApplyImpulse(X*2,a.Center);
        var block=new BilateralConstraintBlock([Row(),Row()]);
        block.Solve();
        Assert.InRange(block.Residual,0,1e-12);
        Assert.InRange(a.LinearVelocity.Length,0,1e-12);
        Assert.Throws<ArgumentException>(()=>new BilateralConstraintBlock([ImpulseConstraint.Contact(a,b,default,X,0,0)]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BilateralConstraintBlock([]));
    }
}
