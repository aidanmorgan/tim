using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ReceiverAnimationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Laser=new("laser"),Battery=new("battery"),Gate=new("powered_gate");
    private static CatalogueId Receiver(OpticalColour colour)=>colour switch
    {
        OpticalColour.Broadband=>new("light_receiver"),OpticalColour.Red=>new("red_receiver"),
        OpticalColour.Green=>new("green_receiver"),OpticalColour.Blue=>new("blue_receiver"),
        OpticalColour.Yellow=>new("yellow_receiver"),OpticalColour.Cyan=>new("cyan_receiver"),
        OpticalColour.Magenta=>new("magenta_receiver"),OpticalColour.White=>new("white_receiver"),
        _=>throw new ArgumentOutOfRangeException(nameof(colour))
    };
    private partial class Source : LaserPart
    {
        public Vector3 TestPower;
        public override OpticalEmitter? OpticalSource=>base.OpticalSource is { } source?source with {Power=TestPower}:null;
    }
    private partial class AmbientReceiver : LightReceiverPart
    {
        public override IReadOnlyList<SceneColourAnimation> ColourAnimations=>
            [base.ColourAnimations[0] with
            {
                Feedback=SceneAnimationSignal.Autonomous,
                Drive=SceneAnimationDrive.StartStop,
                Definition=new(0,1,.125,AnimationCurve.Linear,AnimationRepeat.PingPong,AnimationClock.Presentation)
            }];
    }
    private partial class Supply : BatteryPart
    {
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {
            if(++Visits==FailAt)throw new InvalidOperationException();
        }
    }
    public static IEnumerable<object[]> Cases()
    {
        foreach(var colour in Enum.GetValues<OpticalColour>())
        foreach(var substep in new[]{1,4})yield return [colour,substep];
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [MemberData(nameof(Cases))]
    public void EachReceiverUsesCommittedFeedbackAcrossFailurePauseReverseAndReset(OpticalColour colour,int substep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var source=new Source {Definition=world.Registry.Definitions[Laser.Value],TestPower=OpticalColours.Mask(colour)};
            source.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Laser.Value,Position=[-3,6,0]});world.AttachPart(source);
            var receiver=(LightReceiverPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Receiver(colour).Value,Position=[3,6,0]});
            Assert.Equal(colour,receiver.Colour);
            var supply=new Supply {Definition=world.Registry.Definitions[Battery.Value]};
            supply.Configure(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Battery.Value,Position=[0,10,4]});world.AttachPart(supply);
            var gate=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Fourth),Kind=Gate.Value,Position=[7,6,0]});
            Assert.True(world.Connect(supply,SocketId.Supply,source,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(supply,SocketId.Supply,receiver,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(receiver,SocketId.Supply,gate,SocketId.PowerIn,ConnectionDomain.Electrical));
            var declaration=Assert.Single(receiver.ColourAnimations);
            var material=(StandardMaterial3D)declaration.Target.MaterialOverride;
            var initial=material.AlbedoColor;var construction=Saved(world);var pose=receiver.Transform;
            world.Start();world.Activate(source);world.Step();world.Step();
            Assert.True(receiver.Active);Assert.True(gate.HasElectricalPower(SocketId.PowerIn));
            Assert.Equal(initial,material.AlbedoColor);
            var physics=world.Physics.Capture().BodyStates.ToArray();var ticks=world.Ticks;
            receiver.Active=false; // Deliberately unpublished state must not drive presentation.
            world.Running=false;world.PresentFrame(.0625,1);
            Assert.Equal(declaration.From.Lerp(declaration.To,.5f),material.AlbedoColor);
            Assert.Equal(physics,world.Physics.Capture().BodyStates.ToArray());Assert.Equal(ticks,world.Ticks);
            receiver.Active=true;world.Running=true;
            var generation=world.ControlGeneration;
            var command=world.QueueBinaryInput(new(supply,BatteryPart.EnableInput),BinaryInputState.Disabled);
            supply.FailAt=supply.Visits+substep;
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(ticks,world.Ticks);Assert.Equal(physics,world.Physics.Capture().BodyStates.ToArray());
            Assert.False(world.TryPeekControlResult(out _));Assert.True(receiver.Active);
            world.PresentFrame(0,1);
            Assert.Equal(declaration.From.Lerp(declaration.To,.5f),material.AlbedoColor);
            supply.FailAt=0;world.Step();world.Step();
            Assert.False(receiver.Active);Assert.False(gate.HasElectricalPower(SocketId.PowerIn));
            Assert.True(world.TryPeekControlResult(out var result));
            Assert.Equal(new CommandId(generation,command),result.Id);Assert.Equal(CommandOutcome.Applied,result.Outcome);
            world.AcknowledgeControlResult(result.Id);
            world.Running=false;world.PresentFrame(.03125,1);
            Assert.Equal(declaration.From.Lerp(declaration.To,.103515625f),material.AlbedoColor);
            world.PresentFrame(.125,1);Assert.Equal(initial,material.AlbedoColor);
            world.Restore();Assert.Equal(construction,Saved(world));
            var restored=(LightReceiverPart)world.FindPart(FixtureParts.Id(FixturePartId.Second))!;
            Assert.Equal(pose,restored.Transform);Assert.False(restored.Active);
            Assert.Equal(initial,((StandardMaterial3D)Assert.Single(restored.ColourAnimations).Target.MaterialOverride).AlbedoColor);
        }
        finally{world.Free();}
    }

    [Fact]
    public void SharedAndReplacedMaterialBindingsRejectExplicitly()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var first=(LightReceiverPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Receiver(OpticalColour.Broadband).Value,Position=[-3,6,0]});
            var second=(LightReceiverPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Receiver(OpticalColour.Red).Value,Position=[3,6,0]});
            var a=Assert.Single(first.ColourAnimations).Target;var b=Assert.Single(second.ColourAnimations).Target;
            var baseline=b.MaterialOverride;b.MaterialOverride=a.MaterialOverride;
            Assert.Throws<ArgumentException>(world.Start);Assert.False(world.HasPhysicsState);
            b.MaterialOverride=baseline;world.Start();
            var original=a.MaterialOverride;a.MaterialOverride=baseline;
            Assert.Throws<InvalidOperationException>(()=>world.PresentFrame(.1,1));
            a.MaterialOverride=original;world.PresentFrame(.1,1);world.Restore();
        }
        finally{world.Free();}
    }

    [Fact]
    public void UndefinedCatalogueChoiceRejects()=>Assert.Throws<ArgumentOutOfRangeException>(()=>Receiver((OpticalColour)999));

    [Fact]
    public void AutonomousColourContinuesWithoutPhysicsTicks()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var receiver=new AmbientReceiver {Definition=world.Registry.Definitions[Receiver(OpticalColour.Broadband).Value]};
            receiver.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Receiver(OpticalColour.Broadband).Value,Position=[0,6,0]});
            world.AttachPart(receiver);
            var declaration=Assert.Single(receiver.ColourAnimations);
            var material=(StandardMaterial3D)declaration.Target.MaterialOverride;
            world.Start();world.Running=false;
            var physical=world.Physics.Capture().BodyStates.ToArray();
            world.PresentFrame(.0625,1);Assert.Equal(declaration.From.Lerp(declaration.To,.5f),material.AlbedoColor);
            Assert.False(receiver.Active);Assert.Equal(0,world.Ticks);
            Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            world.Restore();
        }
        finally{world.Free();}
    }
}
