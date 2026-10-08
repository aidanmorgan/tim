using Godot;
using System.Diagnostics;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class BodyBoundsTreeTests(ITestOutputHelper output)
{
    private static PhysicsObject Object(int id,CollisionVector center,PhysicsMotionType motion)
    {
        var body=motion==PhysicsMotionType.Dynamic?
            new PhysicsBody(new(id),motion,RigidPose.At(center),default,default,1,new(1,1,1)):
            new PhysicsBody(new(id),motion,RigidPose.At(center),default,default);
        return new(body,new([new(new ConvexSphere(.1),AffineTransform.Identity)]),new(0,0,0));
    }
    private static CollisionBounds Box(CollisionVector center,double radius)=>new(
        center-new CollisionVector(radius,radius,radius),center+new CollisionVector(radius,radius,radius));
    private static bool Eligible(PhysicsMotionType a,PhysicsMotionType b,BodyPairDomain domain)
    {
        var dynamic=a==PhysicsMotionType.Dynamic||b==PhysicsMotionType.Dynamic;
        var prescribed=!dynamic&&(a==PhysicsMotionType.Kinematic||b==PhysicsMotionType.Kinematic);
        return domain switch
        {
            BodyPairDomain.Dynamic=>dynamic,BodyPairDomain.Prescribed=>prescribed,
            BodyPairDomain.Moving=>dynamic||prescribed,
            _=>throw new ArgumentOutOfRangeException(nameof(domain))
        };
    }

    [Fact] public void DynamicRefitMatchesExhaustiveBounds()=>VerifyRefit(BodyPairDomain.Dynamic);
    [Fact] public void PrescribedRefitMatchesExhaustiveBounds()=>VerifyRefit(BodyPairDomain.Prescribed);
    [Fact] public void MovingRefitMatchesExhaustiveBounds()=>VerifyRefit(BodyPairDomain.Moving);
    private static void VerifyRefit(BodyPairDomain domain)
    {
        var random=new Random(48291);
        var motions=Enum.GetValues<PhysicsMotionType>();
        var objects=Enumerable.Range(0,48).Select(i=>Object(i,new(i*2,0,0),motions[i%motions.Length])).ToArray();
        var tree=new BodyBoundsTree(objects);
        CollisionBounds?[] Bounds()=>objects.Select((o,i)=>(CollisionBounds?)
            Box(new(random.NextDouble()*20,random.NextDouble()*3,random.NextDouble()*5),.7)).ToArray();
        var initial=Bounds();const double margin=.2;
        BodyPairIndices[] Expected(CollisionBounds?[] bounds)
        {
            var pairs=new List<BodyPairIndices>();
            for(var a=0;a<bounds.Length;a++)
            for(var b=a+1;b<bounds.Length;b++)
                if(bounds[a] is { } first&&bounds[b] is { } second&&
                    Eligible(objects[a].Body.MotionType,objects[b].Body.MotionType,domain)&&
                    first.DistanceLowerBound(second)<=margin)pairs.Add(new(a,b));
            return pairs.ToArray();
        }
        var retained=tree.Query(i=>initial[i],domain,margin);var expected=Expected(initial);
        Assert.Equal(expected,retained.Pairs);
        for(var trial=0;trial<30;trial++)
        {
            var moved=Bounds();
            for(var i=trial%3;i<moved.Length;i+=3)moved[i]=null;
            Assert.Equal(Expected(moved),tree.Query(i=>moved[i],domain,margin).Pairs);
        }
        Assert.Equal(expected,retained.Pairs);
        Assert.Equal(expected,tree.Query(i=>initial[i],domain,margin).Pairs);
    }

    [Fact]
    public void SparsePopulationPrunesBeforeQuadraticLeafPairsAndDensePopulationIsComplete()
    {
        var sparse=Enumerable.Range(0,4096).Select(i=>Object(i,new(i*3,0,0),PhysicsMotionType.Dynamic)).ToArray();
        var tree=new BodyBoundsTree(sparse);
        var allocated=GC.GetAllocatedBytesForCurrentThread();var start=Stopwatch.GetTimestamp();
        var query=tree.Query(i=>Box(sparse[i].Body.Center,.1),BodyPairDomain.Dynamic,0);
        var elapsed=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
        output.WriteLine($"Sparse bodies={sparse.Length}, exhaustive pairs={(long)sparse.Length*(sparse.Length-1)/2}, node tests={query.NodeTests}, candidates={query.Pairs.Count}, query ms={elapsed:R}, allocated bytes={bytes}");
        Assert.Empty(query.Pairs);Assert.InRange(query.NodeTests,1,4096*4);
        var dense=sparse.Take(64).ToArray();
        var result=new BodyBoundsTree(dense).Query(_=>Box(default,1),BodyPairDomain.Dynamic,0);
        output.WriteLine($"Dense bodies={dense.Length}, node tests={result.NodeTests}, candidates={result.Pairs.Count}");
        Assert.Equal(64*63/2,result.Pairs.Count);
        Assert.Equal(result.Pairs.Count,result.Pairs.Distinct().Count());
    }

    [Fact]
    public void EmptyDisabledStaticAndInvalidQueriesAreExplicit()
    {
        var empty=new BodyBoundsTree([]);
        Assert.Empty(empty.Query(_=>throw new InvalidOperationException(),BodyPairDomain.Moving,0).Pairs);
        var objects=new[]{Object(0,default,PhysicsMotionType.Static),Object(1,default,PhysicsMotionType.Static)};
        var tree=new BodyBoundsTree(objects);
        Assert.Empty(tree.Query(_=>Box(default,1),BodyPairDomain.Moving,0).Pairs);
        Assert.Empty(tree.Query(_=>null,BodyPairDomain.Dynamic,0).Pairs);
        Assert.Throws<ArgumentOutOfRangeException>(()=>tree.Query(_=>null,(BodyPairDomain)99,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>tree.Query(_=>null,BodyPairDomain.Dynamic,double.NaN));
        Assert.Throws<ArgumentException>(()=>tree.Query(_=>new(new(1,0,0),default),BodyPairDomain.Moving,0));
        Assert.Empty(tree.Query(_=>Box(default,1),BodyPairDomain.Moving,0).Pairs);
    }
}
