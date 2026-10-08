using System.Diagnostics;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ConstraintMassScratchTests(ITestOutputHelper output)
{
    private static ConstraintMassMatrix Matrix()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(1,1,1));
        var x=new ConstraintGradient([new(body,new(1,0,0),default)]);
        var y=new ConstraintGradient([new(body,new(0,1,0),default)]);
        return new([x,y,x],[0,0,0]);
    }
    private static void Check(double[] result,double x,double y)
    {
        Assert.Equal(x,result[0]+result[2]);
        Assert.Equal(y,result[1]);
    }

    [Fact]
    public void WarmSolveAllocationMeasurementAndRetainedResults()
    {
        var matrix=Matrix();double[] rhs=[2,3,2];var result=new double[3];
        for(var i=0;i<20;i++)matrix.Solve(rhs,result);
        var retained=(double[])result.Clone();
        const int count=10000;
        var bytes=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();
        for(var i=0;i<count;i++)matrix.Solve(rhs,result);
        var elapsed=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
        output.WriteLine($"Solves={count}; bytes={bytes}; elapsed ms={elapsed:R}");
        Assert.Equal(0,bytes);
        Assert.Equal(retained,result);Check(result,2,3);
    }

    [Fact]
    public void InconsistentSolveKeepsCallerOutputAndCannotContaminateNextSolve()
    {
        var matrix=Matrix();
        double[] bad=[2,3,4],good=[-5,7,-5],result=[11,12,13];
        for(var i=0;i<40;i++)
        {
            result[0]=11;result[1]=12;result[2]=13;
            Assert.Throws<InvalidOperationException>(()=>matrix.Solve(bad,result));
            Assert.Equal(new double[]{11,12,13},result);
            matrix.Solve(good,result);Check(result,-5,7);
        }
    }

    [Fact]
    public void CallerMayAliasRightHandSideAndResult()
    {
        var matrix=Matrix();double[] values=[2,3,2];
        matrix.Solve(values,values);Check(values,2,3);
    }

    [Fact]
    public void RankZeroUsesEmptyFactorScratchAndRejectsNonzeroLoadAtomically()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var gradient=new ConstraintGradient([new(body,new(1,0,0),default)]);
        var matrix=new ConstraintMassMatrix([gradient],[0]);
        Assert.Equal(0,matrix.Rank);
        double[] result=[11];
        matrix.Solve(new double[]{0},result);Assert.Equal(0,result[0]);
        result[0]=11;
        Assert.Throws<InvalidOperationException>(()=>matrix.Solve(new double[]{1},result));
        Assert.Equal(11,result[0]);
        matrix.Solve(new double[]{0},result);Assert.Equal(0,result[0]);
    }

    [Fact]
    public async Task SharedFactorizationKeepsConcurrentScratchIndependent()
    {
        var matrix=Matrix();
        await Task.WhenAll(Enumerable.Range(1,8).Select(worker=>Task.Run(()=>
        {
            double[] rhs=[worker,worker*2,worker];var result=new double[3];
            for(var i=0;i<100;i++){matrix.Solve(rhs,result);Check(result,worker,worker*2);}
        })));
    }
}
