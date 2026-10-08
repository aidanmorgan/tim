using CuriousContraptions.Physics;
using Godot;

namespace CuriousContraptions.Tests;

public class TransferBodyImpulseTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    private static void Near(CollisionVector expected,CollisionVector actual)=>
        Assert.InRange((expected-actual).Length,0,1e-9);

    [Theory]
    [InlineData(TransferSupplyKind.Mechanical,false)]
    [InlineData(TransferSupplyKind.Mechanical,true)]
    [InlineData(TransferSupplyKind.StoredFlow,false)]
    [InlineData(TransferSupplyKind.StoredFlow,true)]
    public void OffsetPortsReportPhysicalMomentumAndOwnedReplay(TransferSupplyKind kind,bool rotated)
    {
        var rotation=rotated?RigidRotation.FromRotationVector(new(.2,.4,-.3)):RigidRotation.Identity;
        var direction=rotation.Apply(Z);
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,new(new(-5,0,0),rotation),default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,new(default,rotation),direction*10,default,1,new(1,1,1));
        var receiver=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,new(new(3,0,0),rotation),direction*4,default,2,new(1,1,1));
        PhysicsBody[] bodies=[frame,source,receiver];
        PhysicsObject Object(PhysicsBody body)=>new(body,
            new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],bodies.Select(Object).ToArray(),[],new(default,maximumStep:.005));
        world.InstallEnergyStores([new(frame.Id,100,100)]);
        var supply=kind switch
        {
            TransferSupplyKind.Mechanical=>new MechanicalTransferSource(new(0),
                new PointPowerPort(source.Id,frame.Id,new(1,0,0),Z),new(6,72)),
            TransferSupplyKind.StoredFlow=>new MechanicalTransferSource(
                new StoredFlowSource(new(0),frame.Id,12,new(6,72))),
            _=>throw new ArgumentOutOfRangeException(nameof(kind))
        };
        world.ReplaceLoads(new(){Transfers=[new(new(0),supply,
            new PointPowerPort(receiver.Id,frame.Id,new(0,1,0),Z),new(2,6),1e-8)]});
        var initial=world.Capture();
        world.Step([],[],.02);
        var reports=world.TransferBodyImpulses.ToArray();
        Assert.Equal(kind==TransferSupplyKind.Mechanical?4:2,reports.Length);
        Assert.All(reports,report=>Assert.Equal(new MechanicalSourceId(0),report.Source));
        foreach(var body in new[]{source,receiver})
        {
            var before=initial.BodyStates.ToArray().Single(value=>value.Id==body.Id);
            var own=reports.Where(value=>value.Key.Body==body.Id).ToArray();
            var linear=own.Aggregate(default(CollisionVector),(sum,value)=>sum+value.Linear);
            var angular=own.Aggregate(default(CollisionVector),(sum,value)=>sum+value.Angular);
            var mass=body.Id==source.Id?1:2;
            Near((body.LinearVelocity-before.LinearVelocity)*mass,linear);
            Near(body.AngularMomentum-before.AngularMomentum,angular);
        }
        var received=Assert.Single(reports,value=>value.Key.Body==receiver.Id);
        Assert.Equal(TransferPortRole.Receiver,received.Key.Role);
        Assert.True(received.Angular.Length>0);
        Near(direction*.12,received.Linear);
        foreach(var role in Enum.GetValues<TransferPortRole>())
            Near(default,reports.Where(value=>value.Key.Role==role)
                .Aggregate(default(CollisionVector),(sum,value)=>sum+value.Linear));
        if(kind==TransferSupplyKind.StoredFlow)
            Assert.DoesNotContain(reports,value=>value.Key.Role==TransferPortRole.Source);
        var final=world.Capture();
        world.ReplaceLoads(new());world.Step([],[],.02);
        Assert.Empty(world.TransferBodyImpulses.ToArray());
        Assert.Equal(reports,final.TransferBodyImpulses.ToArray());
        world.Restore(initial);Assert.Empty(world.TransferBodyImpulses.ToArray());
        world.Step([],[],.02);
        Assert.Equal(reports,world.TransferBodyImpulses.ToArray());
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
    }

    [Fact]
    public void InvalidRoleIdentityAndNumericInputsReject()
    {
        var key=new TransferBodyKey(new(0),TransferPortRole.Receiver,new(0));
        var report=new MechanicalTransferBodyImpulse(key,new(0),Z,Z);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalTransferBodyImpulse(
            key with {Role=(TransferPortRole)99},new(0),default,default));
        foreach(var value in new[]{double.NaN,double.PositiveInfinity})
        {
            Assert.Throws<ArgumentException>(()=>new MechanicalTransferBodyImpulse(key,new(0),new(value,0,0),default));
            Assert.Throws<ArgumentException>(()=>new MechanicalTransferBodyImpulse(key,new(0),default,new(value,0,0)));
        }
        var large=new MechanicalTransferBodyImpulse(key,new(0),new(double.MaxValue,0,0),default);
        Assert.Throws<ArgumentException>(()=>large.Add(large));
        Assert.Throws<ArgumentException>(()=>report.Add(new(key,new(1),Z,Z)));
        Assert.Throws<ArgumentException>(()=>report.Add(new(key with {Role=TransferPortRole.Source},new(0),Z,Z)));
    }
}
