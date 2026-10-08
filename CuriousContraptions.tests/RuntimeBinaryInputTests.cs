using Godot;
using CuriousContraptions.Bridge;
using System.Text.Json;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class RuntimeBinaryInputTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery"),Motor=new("motor");
    private partial class Source : BatteryPart
    {
        public Action? Observed;
        public override void ObservePhysics(MachineWorld world,float delta)=>Observed?.Invoke();
    }
    private (MachineWorld World,Source Supply,MachinePart Motor) Rig()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        var supply=new Source {Definition=world.Registry.Definitions[Battery.Value]};
        supply.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[-3,6,0]});
        world.AttachPart(supply);
        var motor=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Motor.Value,Position=[3,6,0]});
        Assert.True(world.Connect(supply,SocketId.Supply,motor,SocketId.PowerIn,ConnectionDomain.Electrical));
        return(world,supply,motor);
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    [Theory]
    [InlineData(0,CommandOutcome.Applied)]
    [InlineData(20,CommandOutcome.Applied)]
    [InlineData(-1,CommandOutcome.OutOfRange)]
    [InlineData(20.001,CommandOutcome.OutOfRange)]
    public void ScalarBoundsAndMixedControlOrderRollbackTogether(double speed,CommandOutcome expected)
    {
        var (world,supply,motor)=Rig();
        try
        {
            var saved=Saved(world);world.Start();
            var key=new SceneScalarInputKey(motor,MotorPart.SpeedInput);
            var initial=world.ReadScalarInput(key);
            var first=world.QueueScalarInput(key,speed);
            var second=world.QueueBinaryInput(new(supply,BatteryPart.EnableInput),BinaryInputState.Disabled);
            var physics=world.Physics.Capture();
            supply.Observed=()=>
            {
                Assert.Equal(expected==CommandOutcome.Applied?speed:initial,world.ReadScalarInput(key));
                Assert.False(motor.HasElectricalPower(SocketId.PowerIn));
                throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(initial,world.ReadScalarInput(key));
            Assert.True(world.ReadBinaryInput(new(supply,BatteryPart.EnableInput))==BinaryInputState.Enabled);
            Assert.Equal(physics.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.False(world.TryPeekControlResult(out _));
            supply.Observed=null;world.Step();
            Assert.True(world.TryPeekControlResult(out var scalar));Assert.Equal(first,scalar.Sequence);
            world.AcknowledgeControlResult(scalar.Id);
            Assert.Equal(expected,scalar.Outcome);
            Assert.True(world.TryPeekControlResult(out var binary));Assert.Equal(second,binary.Sequence);
            world.AcknowledgeControlResult(binary.Id);
            Assert.Equal(CommandOutcome.Applied,binary.Outcome);
            Assert.Equal(scalar.Revision,binary.Revision);
            Assert.Equal(0,((MotorPart)motor).SuppliedWork);
            Assert.Throws<ArgumentException>(()=>world.QueueScalarInput(key,double.NaN));
            Assert.Throws<ArgumentException>(()=>world.QueueScalarInput(key,double.PositiveInfinity));
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally {world.Free();}
    }
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void PoweredMotorUsesCommittedSpeedInput(double speed)
    {
        var (world,_,motor)=Rig();
        try
        {
            world.Start();
            world.QueueScalarInput(new(motor,MotorPart.SpeedInput),speed);
            for(var tick=0;tick<10;tick++)world.Step();
            Assert.InRange(Math.Abs(((MotorPart)motor).ShaftSpeed-speed),0,1e-5);
            Assert.Equal(speed>0,((MotorPart)motor).SuppliedWork>0);
        }
        finally {world.Free();}
    }
    [Fact]
    public void SupplyCommandRollsBackWithTickAndRetryCommitsOneResult()
    {
        var (world,supply,motor)=Rig();
        try
        {
            var saved=Saved(world);world.Start();world.Step();Assert.True(motor.HasElectricalPower(SocketId.PowerIn));
            var id=world.QueueBinaryInput(new(supply,BatteryPart.EnableInput),BinaryInputState.Disabled);
            Assert.True(world.ReadBinaryInput(new(supply,BatteryPart.EnableInput))==BinaryInputState.Enabled);
            supply.Observed=()=>{Assert.False(motor.HasElectricalPower(SocketId.PowerIn));throw new InvalidOperationException();};
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.True(world.ReadBinaryInput(new(supply,BatteryPart.EnableInput))==BinaryInputState.Enabled);Assert.True(motor.HasElectricalPower(SocketId.PowerIn));
            Assert.Equal(1,world.Ticks);Assert.False(world.TryPeekControlResult(out _));
            supply.Observed=null;world.Step();
            Assert.False(motor.HasElectricalPower(SocketId.PowerIn));Assert.False(world.ReadBinaryInput(new(supply,BatteryPart.EnableInput))==BinaryInputState.Enabled);
            Assert.True(world.TryPeekControlResult(out var result));Assert.Equal(id,result.Sequence);
            world.AcknowledgeControlResult(result.Id);
            Assert.Equal(CommandOutcome.Applied,result.Outcome);Assert.Equal(new SimulationRevision(2),result.Revision);
            Assert.False(world.TryPeekControlResult(out _));
            world.Restore();Assert.Equal(saved,Saved(world));world.Start();world.Step();
            Assert.True(world.FindPart(FixtureParts.Id(FixturePartId.Second))!.HasElectricalPower(SocketId.PowerIn));
        }
        finally {world.Free();}
    }
    [Fact]
    public void ApplicationChecksRevisionAndTargetAndRejectsDuplicateAdmission()
    {
        var (world,supply,motor)=Rig();
        try
        {
            world.Start();var target=world.BinaryInput(new(supply,BatteryPart.EnableInput));
            var command=new CommandEnvelope<RuntimeControlCommand>(world.ControlGeneration,new(1),new(9),new(target,BinaryInputState.Disabled));
            Assert.Equal(CommandAdmission.Accepted,world.AdmitControl(command));
            Assert.Equal(CommandAdmission.OutOfOrder,world.AdmitControl(command));
            Assert.Equal(CommandAdmission.Accepted,world.AdmitControl(command with
                {Sequence=new(2),ExpectedRevision=null,Payload=new(new(999),BinaryInputState.Disabled)}));
            world.Step();Assert.True(motor.HasElectricalPower(SocketId.PowerIn));
            Assert.True(world.TryPeekControlResult(out var stale));Assert.Equal(CommandOutcome.StaleRevision,stale.Outcome);
            world.AcknowledgeControlResult(stale.Id);
            Assert.True(world.TryPeekControlResult(out var missing));Assert.Equal(CommandOutcome.UnsupportedTarget,missing.Outcome);
            world.AcknowledgeControlResult(missing.Id);
            Assert.Throws<ArgumentException>(()=>world.AdmitControl(command with
                {Sequence=new(3),Payload=new(target,(BinaryInputState)999)}));
        }
        finally {world.Free();}
    }
    [Fact]
    public void PausedFullQueueResetRetainsEveryInvalidationAndRejectsOldGeneration()
    {
        var (world,supply,_)=Rig();
        try
        {
            world.Start();world.Running=false;
            var generation=world.ControlGeneration;
            var target=world.BinaryInput(new(supply,BatteryPart.EnableInput));
            for(var i=0;i<MachineWorld.ControlCommandCapacity;i++)
                world.QueueBinaryInput(new(supply,BatteryPart.EnableInput),BinaryInputState.Disabled);
            Assert.Throws<InvalidOperationException>(()=>world.QueueBinaryInput(new(supply,BatteryPart.EnableInput),BinaryInputState.Enabled));
            world.Step();Assert.Equal(0,world.Ticks);
            world.Restore();Assert.NotEqual(generation,world.ControlGeneration);
            for(var i=1;i<=MachineWorld.ControlCommandCapacity;i++)
            {
                Assert.True(world.TryPeekControlResult(out var result));
                world.AcknowledgeControlResult(result.Id);
                Assert.Equal(generation,result.Generation);Assert.Equal(new CommandSequence(i),result.Sequence);
                Assert.Equal(CommandOutcome.InvalidatedByBarrier,result.Outcome);
            }
            Assert.False(world.TryPeekControlResult(out _));world.Start();
            Assert.Equal(CommandAdmission.WrongGeneration,world.AdmitControl(new(generation,new(129),null,new(target,BinaryInputState.Disabled))));
        }
        finally {world.Free();}
    }

    [Fact]
    public void CommittedResultSurvivesSkippedReadsResetAndConsumerFailure()
    {
        var (world,supply,_)=Rig();
        try
        {
            var saved=Saved(world);world.Start();
            var generation=world.ControlGeneration;
            var command=world.QueueBinaryInput(new(supply,BatteryPart.EnableInput),BinaryInputState.Disabled);
            world.Step();
            Assert.True(world.TryPeekControlResult(out var applied));
            Assert.Equal(new CommandId(generation,command),applied.Id);
            Assert.Equal(CommandOutcome.Applied,applied.Outcome);
            for(var tick=0;tick<3;tick++)world.Step();
            void FailingConsumer()
            {
                Assert.True(world.TryPeekControlResult(out var same));Assert.Equal(applied,same);
                throw new InvalidOperationException();
            }
            Assert.Throws<InvalidOperationException>(FailingConsumer);
            world.Running=false;
            var pending=world.QueueBinaryInput(new(supply,BatteryPart.EnableInput),BinaryInputState.Enabled);
            world.Restore();Assert.Equal(saved,Saved(world));
            Assert.True(world.TryPeekControlResult(out var retained));Assert.Equal(applied,retained);
            Assert.Throws<InvalidOperationException>(()=>world.AcknowledgeControlResult(new(generation,pending)));
            world.AcknowledgeControlResult(applied.Id);
            Assert.True(world.TryPeekControlResult(out var cancelled));
            Assert.Equal(new CommandId(generation,pending),cancelled.Id);
            Assert.Equal(CommandOutcome.InvalidatedByBarrier,cancelled.Outcome);
            world.AcknowledgeControlResult(cancelled.Id);
            Assert.False(world.TryPeekControlResult(out _));
            world.Start();
            var replacement=world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            var reused=world.QueueBinaryInput(new(replacement,BatteryPart.EnableInput),BinaryInputState.Disabled);
            Assert.Equal(command,reused);world.Step();
            Assert.Throws<InvalidOperationException>(()=>world.AcknowledgeControlResult(applied.Id));
            Assert.True(world.TryPeekControlResult(out var current));
            Assert.NotEqual(applied.Id,current.Id);Assert.Equal(CommandOutcome.Applied,current.Outcome);
            world.AcknowledgeControlResult(current.Id);
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally {world.Free();}
    }
}
