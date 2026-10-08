using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
using Godot;
using System.Text.Json;

namespace CuriousContraptions.Tests;

public class CommittedVelocityBufferTests
{
    private static readonly CompoundGeometry Shape=new([new(new ConvexSphere(1),AffineTransform.Identity)]);
    private static PoseReadStamp Stamp(long revision)=>new(new(1),new(revision),revision);
    private static BodyPublicationRead Body(BodyVelocityRead velocity)=>new(
        new(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,RigidPose.Identity),
        new(new(0),new(0),CollisionParticipation.Enabled,Shape,Shape),OwnerActivity.Inactive,velocity);
    [Fact]
    public void VelocityIsAnOwnedCoherentValueWithLeaseAndRevisionBoundaries()
    {
        var initial=new BodyVelocityRead(new(1,2,3),new(-4,5,-6));
        var next=new BodyVelocityRead(new(-7,8,9),new(10,-11,12));
        var input=new[]{Body(initial)};var buffer=new CommittedPoseBuffer(Stamp(0),input,[],[],[],[],[]);
        input[0]=Body(next);
        using(var seed=buffer.Acquire())Assert.Equal(initial,seed.ReadVelocity(PoseSample.Current,new(0)));
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),input,[],[],[],[],[]);input[0]=Body(default);buffer.Publish();
        var lease=buffer.Acquire();var owned=lease.ReadVelocity(PoseSample.Current,new(0));
        Assert.Equal(next,owned);Assert.Equal(initial,lease.ReadVelocity(PoseSample.Previous,new(0)));
        Assert.Equal(Stamp(1),lease.Stamp(PoseSample.Current));
        Assert.Throws<InvalidOperationException>(()=>buffer.BeginWrite(0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>lease.ReadVelocity(PoseSample.Current,new(1)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>lease.ReadVelocity((PoseSample)999,new(0)));
        lease.Dispose();
        buffer.BeginWrite(0);buffer.Stage(Stamp(2),input,[],[],[],[],[]);buffer.Discard();
        var retained=buffer.Acquire();Assert.Equal(next,retained.ReadVelocity(PoseSample.Current,new(0)));
        buffer.Remove();Assert.Throws<InvalidOperationException>(()=>retained.ReadVelocity(PoseSample.Current,new(0)));
        retained.Dispose();Assert.Equal(next,owned);
        Assert.Throws<InvalidOperationException>(()=>default(PoseReadLease).ReadVelocity(PoseSample.Current,new(0)));
    }
    [Theory]
    [InlineData(false,double.NaN)]
    [InlineData(false,double.PositiveInfinity)]
    [InlineData(true,double.NaN)]
    [InlineData(true,double.NegativeInfinity)]
    public void NonfiniteVelocityRejectsBeforePublication(bool angular,double invalid)
    {
        var vector=new CollisionVector(invalid,0,0);
        var state=Body(angular?new(default,vector):new(vector,default));
        Assert.Throws<ArgumentException>(()=>new CommittedPoseBuffer(Stamp(0),[state],[],[],[],[],[]));
        var buffer=new CommittedPoseBuffer(Stamp(0),[Body(default)],[],[],[],[],[]);
        buffer.BeginWrite(0);Assert.Throws<ArgumentException>(()=>buffer.Stage(Stamp(1),[state],[],[],[],[],[]));buffer.Discard();
        using var read=buffer.Acquire();Assert.Equal(default,read.ReadVelocity(PoseSample.Current,new(0)));
        Assert.Equal(Stamp(0),read.Stamp(PoseSample.Current));
    }
    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    public void WarmedVelocityPublicationAndReadAllocateNothing(int count)
    {
        var input=Enumerable.Range(0,count).Select(i=>Body(new(new(i,0,0),new(0,0,-i))) with
        {
            Pose=new(new(i),PhysicsMotionType.Dynamic,RigidPose.Identity,RigidPose.Identity),
            Query=new(new(i),new(0),CollisionParticipation.Enabled,Shape,Shape)
        }).ToArray();
        var buffer=new CommittedPoseBuffer(Stamp(0),input,[],[],[],[],[]);
        for(var revision=1;revision<=100;revision++)
        {
            buffer.BeginWrite(0);buffer.Stage(Stamp(revision),input,[],[],[],[],[]);buffer.Publish();
            using var read=buffer.Acquire();for(var i=0;i<count;i++)_=read.ReadVelocity(PoseSample.Current,new(i));
        }
        var bytes=GC.GetAllocatedBytesForCurrentThread();double sum=0;
        for(var revision=101;revision<=1100;revision++)
        {
            buffer.BeginWrite(0);buffer.Stage(Stamp(revision),input,[],[],[],[],[]);buffer.Publish();
            using var read=buffer.Acquire();for(var i=0;i<count;i++)sum+=read.ReadVelocity(PoseSample.Current,new(i)).LinearMetresPerSecond.X;
        }
        Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-bytes);Assert.Equal(count*(count-1)*500d,sum);
    }
}

[Collection<NativeSceneCollection>]
public class CommittedVelocityWorldTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Motor=new("motor"),Battery=new("battery"),Ball=new("ball");
    private partial class Supply : BatteryPart
    {
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected velocity publication failure.");}
    }
    private static BodyVelocityRead[] Reads(MachineWorld world)
    {
        using var read=world.ReadCommittedPoses();var result=new BodyVelocityRead[read.Count];
        foreach(var body in world.PhysicsAssembly.Bodies)
        {
            result[body.Id.Index]=read.ReadVelocity(PoseSample.Current,body.Id);
            Assert.Equal(new BodyVelocityRead(body.LinearVelocity,body.AngularVelocity),result[body.Id.Index]);
            Assert.Equal(body.Pose,read.Read(PoseSample.Current,body.Id.Index).Pose);
        }
        return result;
    }
    [Theory]
    [InlineData(0f,1)]
    [InlineData(0f,4)]
    [InlineData(37f,1)]
    [InlineData(37f,4)]
    public void MotorAndFallingBodyVelocitiesPublishWithRollbackPauseAndReset(float angle,int substep)
    {
        var world=new MachineWorld {Gravity=9.8f,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var motor=(MotorPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Motor.Value,
                Position=[0,8,0],Orientation=PartOrientation.FromEulerDegrees(angle,angle,angle)});
            var supply=new Supply {Definition=world.Registry.Definitions[Battery.Value]};
            supply.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[4,8,0]});world.AttachPart(supply);
            var ball=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Ball.Value,Position=[-4,10,0]});
            Assert.True(world.Connect(supply,SocketId.Supply,motor,SocketId.PowerIn,ConnectionDomain.Electrical));
            var saved=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();var initial=Reads(world);world.Step();var accepted=Reads(world);
            var shaft=world.PhysicsAssembly.Body(new(motor,MotorPart.ShaftBody)).Id;
            var free=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).Id;
            Assert.True(accepted[shaft.Index].AngularRadiansPerSecond.LengthSquared>0);
            Assert.True(accepted[free.Index].LinearMetresPerSecond.Y<0);
            using(var read=world.ReadCommittedPoses())
                for(var i=0;i<read.Count;i++)Assert.Equal(initial[i],read.ReadVelocity(PoseSample.Previous,new(i)));
            var snapshot=world.Physics.Capture();world.Running=false;world.PresentFrame(.1,1);
            Assert.Equal(accepted,Reads(world));Assert.Equal(snapshot.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            world.Running=true;world.QueueBinaryInput(new(supply,BatteryPart.EnableInput),BinaryInputState.Disabled);
            supply.FailAt=supply.Visits+substep;var ticks=world.Ticks;
            Assert.Throws<InvalidOperationException>(world.Step);Assert.Equal(ticks,world.Ticks);Assert.Equal(accepted,Reads(world));
            Assert.False(world.TryPeekControlResult(out _));
            supply.FailAt=0;world.Step();world.Step();var coast=Reads(world);
            Assert.False(motor.Active);Assert.True(coast[shaft.Index].AngularRadiansPerSecond.LengthSquared>0);
            var old=world.ReadCommittedPoses();var generation=old.Stamp(PoseSample.Current).Generation;
            world.Restore();Assert.Throws<InvalidOperationException>(()=>old.ReadVelocity(PoseSample.Current,shaft));old.Dispose();
            Assert.Equal(saved,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            world.Start();Assert.Equal(initial,Reads(world));
            using var fresh=world.ReadCommittedPoses();Assert.NotEqual(generation,fresh.Stamp(PoseSample.Current).Generation);
        }
        finally{world.Free();}
    }
}
