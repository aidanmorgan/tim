using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class MotorIndicatorTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Motor=new("motor"),Battery=new("battery");
    private partial class Supply : BatteryPart
    {
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {
            if(++Visits==FailAt)throw new InvalidOperationException("Injected motor indicator failure.");
        }
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    [Theory]
    [InlineData(0,1)]
    [InlineData(0,4)]
    [InlineData(8,1)]
    [InlineData(8,4)]
    public void SupplyLampUsesCommittedActivityWithoutChangingFunctionalShaft(int speed,int failingSubstep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var motor=(MotorPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Motor.Value,
                Position=[0,6,0],Properties=new(){[PartParameterName.Of(MotorParameter.Speed)]=speed}});
            var supply=new Supply {Definition=world.Registry.Definitions[Battery.Value]};
            supply.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[4,6,0]});world.AttachPart(supply);
            Assert.True(world.Connect(supply,SocketId.Supply,motor,SocketId.PowerIn,ConnectionDomain.Electrical));
            var lamp=Assert.Single(motor.ColourAnimations);var material=(StandardMaterial3D)lamp.Target.MaterialOverride;
            var saved=Saved(world);var pose=motor.Transform;
            world.Start();world.Step();
            Assert.True(motor.Active);Assert.Equal(lamp.From,material.AlbedoColor);
            var physics=world.Physics.Capture().BodyStates.ToArray();var ticks=world.Ticks;
            var work=motor.SuppliedWork;var travel=motor.ShaftTravel;
            motor.Active=false;world.Running=false;world.PresentFrame(.05,1);
            Assert.Equal(lamp.From.Lerp(lamp.To,.5f),material.AlbedoColor);
            Assert.Equal(physics,world.Physics.Capture().BodyStates.ToArray());Assert.Equal(ticks,world.Ticks);
            Assert.Equal(work,motor.SuppliedWork);Assert.Equal(travel,motor.ShaftTravel);
            if(speed==0){Assert.Equal(0,motor.ShaftSpeed);Assert.Equal(0,work);}
            else{Assert.True(motor.ShaftSpeed>0);Assert.True(work>0);}
            motor.Active=true;world.Running=true;
            var command=world.QueueBinaryInput(new(supply,BatteryPart.EnableInput),BinaryInputState.Disabled);
            supply.FailAt=supply.Visits+failingSubstep;
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.True(motor.Active);Assert.Equal(work,motor.SuppliedWork);
            Assert.Equal(physics,world.Physics.Capture().BodyStates.ToArray());Assert.Equal(ticks,world.Ticks);
            Assert.False(world.TryPeekControlResult(out _));
            world.PresentFrame(0,1);Assert.Equal(lamp.From.Lerp(lamp.To,.5f),material.AlbedoColor);
            supply.FailAt=0;world.Step();world.Step();Assert.False(motor.Active);
            Assert.True(world.TryPeekControlResult(out var result));Assert.Equal(command,result.Id.Sequence);
            Assert.Equal(CommandOutcome.Applied,result.Outcome);world.AcknowledgeControlResult(result.Id);
            world.Running=false;world.PresentFrame(.025,1);
            Assert.Equal(lamp.From.Lerp(lamp.To,.103515625f),material.AlbedoColor);
            world.PresentFrame(.1,1);Assert.Equal(lamp.From,material.AlbedoColor);
            world.Restore();Assert.Equal(saved,Saved(world));
            var restored=(MotorPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(pose,restored.Transform);Assert.Equal(0,restored.SuppliedWork);
            Assert.Equal(lamp.From,((StandardMaterial3D)Assert.Single(restored.ColourAnimations).Target.MaterialOverride).AlbedoColor);
        }
        finally{world.Free();}
    }
}
