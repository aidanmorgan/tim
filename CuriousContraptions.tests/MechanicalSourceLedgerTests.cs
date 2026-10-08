using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class MechanicalSourceLedgerTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConcurrentBranchesSumBeforeSequentialPeaksAndRestore(bool reverse)
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(new(-4,0,0)),default,default);
        var sources=new[]{
            new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.Identity,Z*10,default,1,new(1,1,1)),
            new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(10,0,0)),Z*20,default,1,new(1,1,1))};
        var bodies=new List<PhysicsBody>{frame};bodies.AddRange(sources);
        var transfers=new List<MechanicalTransferLoad>();
        for(var i=0;i<2;i++)
        {
            var source=new MechanicalTransferSource(new(i),new PointPowerPort(sources[i].Id,frame.Id,default,Z),new(1000,100000));
            for(var j=0;j<2;j++)
            {
                var receiver=new PhysicsBody(new(3+i*2+j),PhysicsMotionType.Dynamic,
                    RigidPose.At(new(i*10+2+j*2,0,0)),Z*(4+4*i),default,1,new(1,1,1));
                bodies.Add(receiver);
                transfers.Add(new(new(i*2+j),source,new PointPowerPort(receiver.Id,frame.Id,default,Z),new(2,100),1e-10));
            }
        }
        PhysicsObject Object(PhysicsBody body)=>new(body,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        if(reverse){bodies.Reverse();transfers.Reverse();}
        var world=new PhysicsWorld([],bodies.Select(Object).ToArray(),[],new(default,maximumStep:.005));
        world.ReplaceLoads(new(){Transfers=transfers});
        var initial=world.Capture();var energy=sources.Select(body=>body.KineticEnergy).ToArray();
        var result=world.Step([],[],.01);
        var totals=world.SourceTotals.ToArray();
        Assert.Equal(2,totals.Length);
        for(var i=0;i<2;i++)
        {
            var total=totals[i];
            Assert.Equal(new MechanicalSourceId(i),total.Source);
            Assert.Equal(2UL,total.Intervals);
            Assert.InRange(total.Impulse-((i==0?10:20)-sources[i].LinearVelocity.Z),-1e-10,1e-10);
            Assert.InRange(total.Extraction.Supplied,energy[i]-sources[i].KineticEnergy-1e-8,energy[i]-sources[i].KineticEnergy+1e-8);
            var branches=world.TransferTotals.ToArray().Where(value=>value.Source==total.Source).ToArray();
            Assert.InRange(total.Extraction.Supplied-branches.Sum(value=>value.SourceExtraction.Supplied),-1e-10,1e-10);
            Assert.InRange(total.Impulse-branches.Sum(value=>value.Impulse),-1e-10,1e-10);
            // Each branch's highest power is in the first interval: source
            // and receiver slip both decrease. Their simultaneous peaks add.
            Assert.InRange(total.Extraction.SuppliedPowerUpperBound-
                branches.Sum(value=>value.SourceExtraction.SuppliedPowerUpperBound),-1e-10,1e-10);
            Assert.True(total.Extraction.SuppliedPowerUpperBound>
                branches.Max(value=>value.SourceExtraction.SuppliedPowerUpperBound));
        }
        var saved=world.Capture();
        world.Step([],[],.01);
        Assert.All(world.SourceTotals.ToArray(),value=>Assert.Equal(4UL,value.Intervals));
        Assert.Equal(totals,saved.SourceTotals.ToArray());
        world.ReplaceLoads(new());world.Step([],[],.005);
        Assert.Empty(world.SourceUse.ToArray());
        Assert.Equal(2,world.SourceTotals.Length);
        world.Restore(initial);
        Assert.Empty(world.SourceTotals.ToArray());
        Assert.Equal(result,world.Step([],[],.01));
        Assert.Equal(totals,world.SourceTotals.ToArray());
        Assert.Equal(saved.SourceUse.ToArray(),world.SourceUse.ToArray());
    }
}
